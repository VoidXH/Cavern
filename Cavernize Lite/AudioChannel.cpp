#include "AudioChannel.h"

std::vector<AudioChannel> AudioChannel::channels = std::vector<AudioChannel>();

void AudioChannel::Recalculate() {
    float XRad = X * deg2Rad, YRad = Y * deg2Rad, SinX = sinf(XRad), CosX = cosf(XRad), SinY = sinf(YRad), CosY = cosf(YRad);
    //SphericalPos = Vector3(SinY * CosX, -SinX, CosY * CosX);
    if (fabsf(SinY) > fabsf(CosY)) {
        SinY = SinY > 0 ? sqrt2p2 : sqrt2pm2;
    } else
        CosY = CosY > 0 ? sqrt2p2 : sqrt2pm2;
    SinY /= sqrt2p2;
    CosY /= sqrt2p2;
    if (fabsf(SinX) >= sqrt2p2) {
        SinX = SinX > 0 ? sqrt2p2 : sqrt2pm2;
        CosX /= sqrt2p2;
        SinY *= CosX;
        CosY *= CosX;
    }
    SinX /= sqrt2p2;
    CubicalPos = Vector3(SinY, -SinX, CosY);
}

float AudioChannel::widthRatio(int Left, int Right, float Pos) {
    if (Left == Right)
        return .5f;
    float LeftX = channels.at(Left).CubicalPos.x;
    return (Pos - LeftX) / (channels.at(Right).CubicalPos.x - LeftX);
}

float AudioChannel::lengthRatio(int Rear, int Front, float Pos) {
    if (Rear == Front)
        return .5f;
    float RearZ = channels.at(Rear).CubicalPos.z;
    return (Pos - RearZ) / (channels.at(Front).CubicalPos.z - RearZ);
}

void AudioChannel::assignLR(int Channel, int& Left, int& Right, Vector3 Position, Vector3 ChannelPos) {
    if (ChannelPos.x == Position.x) { // Exact match
        Left = Channel;
        Right = Channel;
    } else if (ChannelPos.x < Position.x) { // Left
        if (Left == -1 || channels.at(Left).CubicalPos.x < ChannelPos.x) Left = Channel;
    } else if (Right == -1 || channels.at(Right).CubicalPos.x > ChannelPos.x) Right = Channel; // Right
}

void AudioChannel::assignHorizontalLayer(int Channel, int& FL, int& FR, int& RL, int& RR, float& ClosestFront, float& ClosestRear,
    Vector3 Position, Vector3 ChannelPos) {
    if (ChannelPos.z > Position.z) { // Front
        if (ChannelPos.z < ClosestFront) { // Front layer selection
            ClosestFront = ChannelPos.z; FL = -1; FR = -1;
        }
        if (ChannelPos.z == ClosestFront)
            assignLR(Channel, FL, FR, Position, ChannelPos);
    } else { // Rear
        if (ChannelPos.z > ClosestRear) { // Rear layer selection
            ClosestRear = ChannelPos.z; RL = -1; RR = -1;
        }
        if (ChannelPos.z == ClosestRear)
            assignLR(Channel, RL, RR, Position, ChannelPos);
    }
}

void AudioChannel::fixIncompleteLayer(int& FL, int& FR, int& RL, int& RR) {
    if (FL == -1 || FR == -1 || RL == -1 || RR == -1) {
        if (FL != -1 || FR != -1) {
            if (FL == -1) FL = FR;
            if (FR == -1) FR = FL;
            if (RL == -1 && RR == -1) { RL = FL; RR = FR; }
        }
        if (RL != -1 || RR != -1) {
            if (RL == -1) RL = RR;
            if (RR == -1) RR = RL;
            if (FL == -1 && FR == -1) { FL = RL; FR = RR; }
        }
    }
}

void AudioChannel::copy(float* samples, float* output, int64_t sampleCount, int64_t inStep, int64_t outStep, float gain) {
    float constantPower = sinf(gain * M_PI_2);
    while (sampleCount-- != 0) {
        *output += *samples * constantPower;
        samples += inStep;
        output += outStep;
    }
}

AudioChannel::AudioChannel(float iX, float iY) {
    X = iX;
    Y = iY;
    LFE = false;
    Recalculate();
}

AudioChannel::AudioChannel(float iX, float iY, bool iLFE) {
    X = iX;
    Y = iY;
    LFE = iLFE;
    Recalculate();
}

void AudioChannel::render(float* Samples, int32_t sourceChannels, int64_t sampleCount, Vector3 Position, float* output) {
    int BFL = -1, BFR = -1, BRL = -1, BRR = -1, TFL = -1, TFR = -1, TRL = -1, TRR = -1; // Each direction (bottom/top, front/rear, left/right)
    float ClosestTop = 1.1f, ClosestBottom = -1.1f, ClosestTF = 1.1f, ClosestTR = -1.1f, ClosestBF = 1.1f, ClosestBR = -1.1f; // Closest layers on y/z
    for (size_t Channel = 0; Channel < channels.size(); ++Channel) { // Find closest horizontal layers
        if (!channels.at(Channel).LFE) {
            float ChannelY = channels.at(Channel).CubicalPos.y;
            if (ChannelY < Position.y) {
                if (ChannelY > ClosestBottom)
                    ClosestBottom = ChannelY;
            } else if (ChannelY < ClosestTop)
                ClosestTop = ChannelY;
        }
    }
    for (size_t Channel = 0; Channel < channels.size(); ++Channel) {
        if (!channels.at(Channel).LFE) {
            Vector3 ChannelPos = Vector3(channels.at(Channel).CubicalPos);
            if (ChannelPos.y == ClosestBottom) // Bottom layer
                assignHorizontalLayer(Channel, BFL, BFR, BRL, BRR, ClosestBF, ClosestBR, Position, ChannelPos);
            if (ChannelPos.y == ClosestTop) // Top layer
                assignHorizontalLayer(Channel, TFL, TFR, TRL, TRR, ClosestTF, ClosestTR, Position, ChannelPos);
        }
    }
    fixIncompleteLayer(TFL, TFR, TRL, TRR); // Fix incomplete top layer
    if (BFL == -1 && BFR == -1 && BRL == -1 && BRR == -1) { // Fully incomplete bottom layer, use top
        BFL = TFL; BFR = TFR; BRL = TRL; BRR = TRR;
    } else
        fixIncompleteLayer(BFL, BFR, BRL, BRR); // Fix incomplete bottom layer
    if (TFL == -1 || TFR == -1 || TRL == -1 || TRR == -1) { // Fully incomplete top layer, use bottom
        TFL = BFL; TFR = BFR; TRL = BRL; TRR = BRR;
    }
    // Spatial mix
    float TopVol, BottomVol;
    if (TFL != BFL) { // Height ratio calculation
        float BottomY = channels.at(BFL).CubicalPos.y;
        TopVol = (Position.y - BottomY) / (channels.at(TFL).CubicalPos.y - BottomY);
        BottomVol = 1.f - TopVol;
    } else
        TopVol = BottomVol = .5f;
    float BFVol = lengthRatio(BRL, BFL, Position.z), TFVol = lengthRatio(TRL, TFL, Position.z), // Length ratios
        BFRVol = widthRatio(BFL, BFR, Position.x), BRRVol = widthRatio(BRL, BRR, Position.x), // Width ratios
        TFRVol = widthRatio(TFL, TFR, Position.x), TRRVol = widthRatio(TRL, TRR, Position.x);
    float BRVol = 1. - BFVol, TRVol = 1. - TFVol; // Remaining length ratios
    BFVol *= BottomVol; BRVol *= BottomVol; TFVol *= TopVol; TRVol *= TopVol;
    // Output
    copy(Samples, output + BFL, sampleCount, sourceChannels, channels.size(), BFVol * (1. - BFRVol));
    copy(Samples, output + BFR, sampleCount, sourceChannels, channels.size(), BFVol * BFRVol);
    copy(Samples, output + BRL, sampleCount, sourceChannels, channels.size(), BRVol * (1. - BRRVol));
    copy(Samples, output + BRR, sampleCount, sourceChannels, channels.size(), BRVol * BRRVol);
    copy(Samples, output + TFL, sampleCount, sourceChannels, channels.size(), TFVol * (1. - TFRVol));
    copy(Samples, output + TFR, sampleCount, sourceChannels, channels.size(), TFVol * TFRVol);
    copy(Samples, output + TRL, sampleCount, sourceChannels, channels.size(), TRVol * (1. - TRRVol));
    copy(Samples, output + TRR, sampleCount, sourceChannels, channels.size(), TRVol * TRRVol);
}

void AudioChannel::renderLFE(float* samples, float lfeGain, int32_t sourceChannels, int64_t sampleCount, float* output) {
    for (size_t channel = 0; channel < channels.size(); ++channel)
        if (channels.at(channel).LFE)
            copy(samples, output + channel, sampleCount, sourceChannels, channels.size(), lfeGain);
}
