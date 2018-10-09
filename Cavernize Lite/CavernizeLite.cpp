#include <cstring>
#include <fstream>
#include <iostream>
#include <math.h>
#include <stdlib.h>
#include "CavernizeLite.h"
#include "Utils.h"
using namespace std;

/**********************************************************
* Mixing
***********************************************************/

Format* CavernizeLite::renderTarget = NULL;

void CavernizeLite::Normalize(float* target, int64_t sampleCount) {
    float maxSample = 1.f, absSample;
    float* sample = target;
    int64_t samplesToProcess = sampleCount;
    while (samplesToProcess--) {
        absSample = fabs(*sample);
        if (maxSample < absSample)
            maxSample = absSample;
        ++sample;
    }
    if (maxSample * normalizerGain > 1) // Kick in
        normalizerGain = .9f / maxSample;
    samplesToProcess = sampleCount;
    while (samplesToProcess--)
        *(target++) *= normalizerGain; // Normalize last samples
    // Release
    normalizerGain += (float)sampleCount / (float)channelCount / (float)sampleRate;
    if (normalizerGain > 1.f)
        normalizerGain = 1.f;
}

void CavernizeLite::ChannelHeightCheck(float* source, int64_t samplesPerChannel, int32_t channels, int32_t channel, int32_t arrayPosition, float smoothFactor) {
    float depth = .0001f, height = .0001f;
    for (int64_t s = channel; s < samplesPerChannel * channels; s += channels) {
        lastHighs[arrayPosition] = .9f * (lastHighs[arrayPosition] + source[s] - lastNormals[arrayPosition]);
        height = max(height, (float)fabs(lastHighs[arrayPosition]));
        lastLows[arrayPosition] = lastLows[arrayPosition] * .99f + lastHighs[arrayPosition] * .01f;
        depth = max(depth, (float)fabs(lastLows[arrayPosition]));
        lastNormals[arrayPosition] = source[s];
    }
    height = max(min(-(depth * 1.2f - height) * effectScale, 1.f), 0.f);
    heights[arrayPosition] = max(min((height - heights[arrayPosition]) * smoothFactor + heights[arrayPosition], 1.f), 0.f);
}

/**********************************************************
* Cavernize Lite
***********************************************************/

#define EXTRA_CACHE 2

CavernizeLite::CavernizeLite(float effect, float smooth, float lfev, bool keepCenter, int32_t sampling, int32_t channels)
    : centerStays(keepCenter), effectScale(effect * 15.f), smoothness(smooth), lfeVolume(lfev), normalizerGain(1), sampleRate(sampling), channelCount(channels) {
    int32_t cacheSize = channelCount + EXTRA_CACHE; // +2 for the matrix upmixer
    lastLows = new float[cacheSize + 1]; // +1 for LFE separation
    lastNormals = new float[cacheSize];
    lastHighs = new float[cacheSize];
    heights = new float[cacheSize];
    memset(lastLows, 0, (cacheSize + 1) * sizeof(float));
    memset(lastNormals, 0, cacheSize * sizeof(float));
    memset(lastHighs, 0, cacheSize * sizeof(float));
    memset(heights, 0, cacheSize * sizeof(float));
}

void CavernizeLite::Upconvert(float* source, Format* sourceFormat, float* target, int64_t samplesPerChannel, bool lfeSeparation, bool matrixUpmix) {
    float smoothFactor = 1.f - ((sampleRate - samplesPerChannel) * pow(smoothness, .1f) + samplesPerChannel) / sampleRate * .999f;
    for (int32_t c = 0; c < channelCount; ++c)
        if (sourceFormat->channels[c]->getX() != 0 || sourceFormat->channels[c]->getY() != 0 || !centerStays)
            ChannelHeightCheck(source, samplesPerChannel, channelCount, c, c, smoothFactor);
    int64_t targetLength = AudioChannel::channels.size() * samplesPerChannel;
    for (int64_t i = 0; i < targetLength; ++i)
        target[i] = 0;
    for (int32_t c = 0; c < channelCount; ++c) {
        if (!sourceFormat->channels[c]->LFE)
            AudioChannel::render(source + c, channelCount, samplesPerChannel, Vector3(sourceFormat->channels[c]->getCubicalPos().x, heights[c], sourceFormat->channels[c]->getCubicalPos().z), target);
        else
            AudioChannel::renderLFE(source + c, lfeVolume, channelCount, samplesPerChannel, target);
    }
    if (!lfeSeparation) {
        float* monoMix = new float[samplesPerChannel];
        for (int64_t sample = 0; sample < samplesPerChannel; ++sample)
            monoMix[sample] = 0;
        for (int32_t channel = 0; channel < channelCount; ++channel) {
            int64_t sample = 0;
            for (int64_t s = channel; s < samplesPerChannel * channelCount; s += channelCount)
                monoMix[sample++] += source[s];
        }
        int32_t cachePos = samplesPerChannel + EXTRA_CACHE;
        for (int64_t sample = 0; sample < samplesPerChannel; ++sample) {
            lastLows[cachePos] = monoMix[sample] = .9995f * lastLows[cachePos] + .0005f * monoMix[sample]; // TODO: biquad
            monoMix[sample] *= 6;
        }
        AudioChannel::renderLFE(monoMix, lfeVolume, 1, samplesPerChannel, target);
        delete[] monoMix;
    }
    if (matrixUpmix) {
        if (channelCount == 2 || channelCount == 4) { // Create center for stereo and quadro
            float* centerMix = new float[samplesPerChannel];
            for (int64_t centerMixPos = 0; centerMixPos < samplesPerChannel; ++centerMixPos) {
                int64_t leftPos = channelCount * centerMixPos;
                centerMix[centerMixPos] = (source[leftPos] + source[leftPos + 1]) * .5f;
            }
            if (!centerStays)
                ChannelHeightCheck(centerMix, samplesPerChannel, 1, 0, channelCount, smoothFactor);
            AudioChannel::render(centerMix, 1, samplesPerChannel, Vector3(0, heights[channelCount], 1), target);
            delete[] centerMix;
        }
        if (channelCount == 2 || channelCount == 3) { // Create surround for stereo and 3.0
            float* surroundMix = new float[samplesPerChannel];
            for (int64_t surroundMixPos = 0; surroundMixPos < samplesPerChannel; ++surroundMixPos) {
                int64_t leftPos = channelCount * surroundMixPos;
                surroundMix[surroundMixPos] = (source[leftPos] - source[leftPos + 1]) * .5f;
            }
            ChannelHeightCheck(surroundMix, samplesPerChannel, 1, 0, channelCount + 1, smoothFactor);
            AudioChannel::render(surroundMix, 1, samplesPerChannel, Vector3(-1, heights[channelCount + 1], -.5f), target);
            for (int64_t surroundMixPos = 0; surroundMixPos < samplesPerChannel; ++surroundMixPos)
                surroundMix[surroundMixPos] = -surroundMix[surroundMixPos];
            AudioChannel::render(surroundMix, 1, samplesPerChannel, Vector3(1, heights[channelCount + 1], -.5f), target);
            delete[] surroundMix;
        }
    }
    Normalize(target, targetLength);
}

void CavernizeLite::Setup(Format* target, SpatialTarget upmix) {
    switch (upmix) {
        case user: {
            std::ifstream save((std::string(getenv("APPDATA")) + "\\Cavern\\Save.dat").c_str());
            if (save.is_open()) {
                save >> target->channelCount;
                target->channels = new AudioChannel*[target->channelCount];
                for (int32_t c = 0; c < target->channelCount; ++c) {
                    float x, y;
                    std::string lfe;
                    save >> x >> y >> lfe;
                    target->channels[c] = CreateChannel(x, y, lfe.compare("True") == 0);
                    AudioChannel::channels.push_back(*target->channels[c]);
                }
                save.close();
            } else {
                std::cout << "Cavern is not configured on this computer. A 5.1.2 output (L,R,C,LFE,SL,SR,TL,TR) will be used." << std::endl;
                Setup(target, f5_1_2);
            }
            break;
        }
        case f3_0_1: {
            target->channels = new AudioChannel*[target->channelCount = 4];
            target->channels[0] = CreateChannel(0, -45, false);
            target->channels[1] = CreateChannel(0, 45, false);
            target->channels[2] = CreateChannel(0, 180, false);
            target->channels[3] = CreateChannel(-90, 0, false);
            break;
        }
        case f3_1_2: {
            target->channels = new AudioChannel*[target->channelCount = 6];
            target->channels[0] = CreateChannel(0, -45, false);
            target->channels[1] = CreateChannel(0, 45, false);
            target->channels[2] = CreateChannel(0, 180, false);
            target->channels[3] = CreateChannel(0, 0, true);
            target->channels[4] = CreateChannel(-45, -70, false);
            target->channels[5] = CreateChannel(-45, 70, false);
            break;
        }
        case f4_0_2: {
            target->channels = new AudioChannel*[target->channelCount = 6];
            target->channels[0] = CreateChannel(0, -45, false);
            target->channels[1] = CreateChannel(0, 45, false);
            target->channels[2] = CreateChannel(0, -135, false);
            target->channels[3] = CreateChannel(0, 135, false);
            target->channels[4] = CreateChannel(-45, -90, false);
            target->channels[5] = CreateChannel(-45, 90, false);
            break;
        }
        case f4_0_4: {
            target->channels = new AudioChannel*[target->channelCount = 8];
            target->channels[0] = CreateChannel(0, -45, false);
            target->channels[1] = CreateChannel(0, 45, false);
            target->channels[2] = CreateChannel(0, -135, false);
            target->channels[3] = CreateChannel(0, 135, false);
            target->channels[4] = CreateChannel(-45, -45, false);
            target->channels[5] = CreateChannel(-45, 45, false);
            target->channels[6] = CreateChannel(-45, -135, false);
            target->channels[7] = CreateChannel(-45, 135, false);
            break;
        }
        case f5_1_2: {
            target->channels = new AudioChannel*[target->channelCount = 8];
            target->channels[0] = CreateChannel(0, -30, false);
            target->channels[1] = CreateChannel(0, 30, false);
            target->channels[2] = CreateChannel(0, 0, false);
            target->channels[3] = CreateChannel(0, 0, true);
            target->channels[4] = CreateChannel(0, -110, false);
            target->channels[5] = CreateChannel(0, 110, false);
            target->channels[6] = CreateChannel(-45, -70, false);
            target->channels[7] = CreateChannel(-45, 70, false);
            break;
        }
        default: break;
    }
    renderTarget = target;
    AudioChannel::channels.clear();
    for (int32_t c = 0; c < target->channelCount; ++c)
        AudioChannel::channels.push_back(*target->channels[c]);
}

CavernizeLite::~CavernizeLite() {
    delete[] lastLows;
    delete[] lastNormals;
    delete[] lastHighs;
    delete[] heights;
}
