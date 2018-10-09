#include "AudioChannel.h"
#include "Limitless.h"
#include <iostream>
#include <algorithm>
using namespace std;

int8_t QualityToHeader(Quality q) {
    switch (q) {
        case Int8: return 0;
        case Int16: return 1;
        default: return 2;
    }
}

Quality HeaderToQuality(int8_t q) {
    switch (q) {
        case 0: return Int8;
        case 1: return Int16;
        default: return Float32;
    }
}

void Limitless::ReadHeader() {
    char placeholder[9];
    fileHandler.read(placeholder, sizeof(char) * 9); // LIMITLESS marker
    while (placeholder[0] != 'H' && placeholder[1] != 'E' && placeholder[2] != 'A' && placeholder[3] != 'D') {
        for (int8_t i = 0; i < 3; ++i)
            placeholder[i] = placeholder[i + 1];
        fileHandler.read(placeholder + 3, sizeof(char)); // HEAD marker
    }
    int8_t readQuality;
    fileHandler.read(reinterpret_cast<char*>(&readQuality), sizeof(int8_t)); // Quality
    quality = HeaderToQuality((readQuality));
    fileHandler.read(placeholder, sizeof(int8_t)); // Channel mode - nobody cares yet
    fileHandler.read(reinterpret_cast<char*>(&channelCount), sizeof(int32_t)); // Channel count
    channels = new AudioChannel*[channelCount];
    for (int32_t channel = 0; channel < channelCount; ++channel) {
        float x, y;
        char lfe;
        fileHandler.read(reinterpret_cast<char*>(&x), sizeof(float));
        fileHandler.read(reinterpret_cast<char*>(&y), sizeof(float));
        fileHandler.read(reinterpret_cast<char*>(&lfe), sizeof(char));
        channels[channel] = new AudioChannel(x, y, lfe);
    }
    fileHandler.read(reinterpret_cast<char*>(&sampleRate), sizeof(int32_t)); // Sample rate
    int64_t readTotal;
    fileHandler.read(reinterpret_cast<char*>(&readTotal), sizeof(int64_t)); // Sample count
    totalSamples = readTotal / channelCount;
    writtenChannels = 0;
    samplesThisSecond = 0;
}

void Limitless::WriteHeader() {
    cachePosition = 0;
    samplesCached = 0;
    cache = new float[cacheLimit = (int64_t)channelCount * (int64_t)sampleRate];
    // Header
    char limitless[10] = "LIMITLESS", head[5] = "HEAD";
    fileHandler.write(limitless, sizeof(char) * 9); // LIMITLESS marker
    fileHandler.write(head, sizeof(char) * 4); // HEAD marker
    int8_t qualityDump = QualityToHeader(quality), channelMode = 0;
    fileHandler.write(reinterpret_cast<char*>(&qualityDump), sizeof(int8_t)); // Quality
    fileHandler.write(reinterpret_cast<char*>(&channelMode), sizeof(int8_t)); // Channel mode
    fileHandler.write(reinterpret_cast<char*>(&channelCount), sizeof(int32_t)); // Channel count
    for (int32_t channel = 0; channel < channelCount; ++channel) {
        float x = channels[channel]->getX();
        float y = channels[channel]->getY();
        fileHandler.write(reinterpret_cast<char*>(&x), sizeof(float));
        fileHandler.write(reinterpret_cast<char*>(&y), sizeof(float));
        fileHandler.write(reinterpret_cast<char*>(&channels[channel]->LFE), sizeof(char));
    }
    fileHandler.write(reinterpret_cast<char*>(&sampleRate), sizeof(int32_t)); // Sample rate
    int64_t actualTotal = totalSamples * channelCount;
    fileHandler.write(reinterpret_cast<char*>(&actualTotal), sizeof(int64_t)); // Sample count
}

void Limitless::GetLayout() {
    int32_t layoutByteCount = channelCount % 8 == 0 ? channelCount / 8 : (channelCount / 8 + 1);
    int8_t* layoutBytes = new int8_t[layoutByteCount];
    fileHandler.read(reinterpret_cast<char*>(layoutBytes), layoutByteCount);
    if (writtenChannels)
        delete[] writtenChannels;
    writtenChannels = new bool[channelCount];
    channelsToRead = 0;
    for (int32_t channel = 0; channel < channelCount; channel++) {
        writtenChannels[channel] = (layoutBytes[channel / 8] >> (channel % 8)) % 2;
        if (writtenChannels[channel])
            ++channelsToRead;
    }
    samplesThisSecond = sampleRate * channelCount;
    delete[] layoutBytes;
}

void Limitless::Read(float* samples, int64_t sampleCount) {
    int64_t outPos = 0;
    while (sampleCount) {
        if (samplesThisSecond == 0)
            GetLayout();
        int64_t samplesToRead = min(samplesThisSecond, sampleCount);
        int64_t toReadPerChannel = samplesToRead / channelCount;
        int64_t samplesNeeded = toReadPerChannel * channelsToRead;
        int64_t inputCachePos = 0;
        switch (quality) {
            case Int8: {
                uint8_t* inputCache = new uint8_t[samplesNeeded];
                fileHandler.read(reinterpret_cast<char*>(inputCache), sizeof(uint8_t) * samplesNeeded);
                for (int64_t sample = 0; sample < toReadPerChannel; ++sample)
                    for (int32_t channel = 0; channel < channelCount; ++channel)
                        samples[outPos++] = writtenChannels[channel] ? inputCache[inputCachePos++] / 127.0f - 1.0f : 0;
                delete[] inputCache;
                break;
            }
            case Int16: {
                int16_t* inputCache = new int16_t[samplesNeeded];
                fileHandler.read(reinterpret_cast<char*>(inputCache), sizeof(int16_t) * samplesNeeded);
                for (int64_t sample = 0; sample < toReadPerChannel; ++sample)
                    for (int32_t channel = 0; channel < channelCount; ++channel)
                        samples[outPos++] = writtenChannels[channel] ? inputCache[inputCachePos++] / 32767.0f : 0;
                delete[] inputCache;
                break;
            }
            case Float32: {
                float* inputCache = new float[samplesNeeded];
                fileHandler.read(reinterpret_cast<char*>(inputCache), sizeof(float) * samplesNeeded);
                for (int64_t sample = 0; sample < toReadPerChannel; ++sample)
                    for (int32_t channel = 0; channel < channelCount; ++channel)
                        samples[outPos++] = writtenChannels[channel] ? inputCache[inputCachePos++] : 0;
                delete[] inputCache;
                break;
            }
        }
        samplesThisSecond -= samplesToRead;
        sampleCount -= samplesToRead;
    }
}

void Limitless::DumpBlock(int64_t until = -1) {
    if (until == -1)
        until = cacheLimit;
    bool* toWrite = new bool[channelCount];
    for (int32_t channel = 0; channel < channelCount; ++channel)
        for (int32_t sample = channel; !toWrite[channel] && sample < sampleRate; sample += channelCount)
            if (cache[sample] != 0)
                toWrite[channel] = true;
    int32_t layoutByteCount = channelCount % 8 == 0 ? channelCount / 8 : (channelCount / 8 + 1);
    char* layoutBytes = new char[layoutByteCount];
    for (int32_t layoutByte = 0; layoutByte < layoutByteCount; ++layoutByte)
        layoutBytes[layoutByte] = 0;
    for (int32_t channel = 0; channel < channelCount; ++channel) {
        if (toWrite[channel])
            layoutBytes[channel / 8] += (char)(1 << (channel % 8));
    }
    fileHandler.write(layoutBytes, sizeof(char) * layoutByteCount);
    switch (quality) {
        case Int8: {
            int8_t* sample = new int8_t[channelCount * sampleRate];
            int64_t samplePointer = 0;
            for (int64_t i = 0; i < until; ++i)
                if (toWrite[i % channelCount])
                    sample[samplePointer++] = (int8_t)((cache[i] + 1.0f) * 127);
            fileHandler.write(reinterpret_cast<char*>(sample), sizeof(int8_t) * samplePointer);
            delete[] sample;
            break;
        }
        case Int16: {
            int16_t* sample = new int16_t[channelCount * sampleRate];
            int64_t samplePointer = 0;
            for (int64_t i = 0; i < until; ++i)
                if (toWrite[i % channelCount])
                    sample[samplePointer++] = (int16_t)(cache[i] * 32767);
            fileHandler.write(reinterpret_cast<char*>(sample), sizeof(int16_t) * samplePointer);
            delete[] sample;
            break;
        }
        case Float32: {
            float* sample = new float[channelCount * sampleRate];
            int64_t samplePointer = 0;
            for (int64_t i = 0; i < until; ++i)
                if (toWrite[i % channelCount])
                    sample[samplePointer++] = cache[i];
            fileHandler.write(reinterpret_cast<char*>(sample), sizeof(float) * samplePointer);
            delete[] sample;
            break;
        }
    }
    cachePosition = 0;
}

void Limitless::Write(float* samples, int64_t sampleCount) {
    int64_t from = 0;
    while (from < sampleCount) {
        for (; from < sampleCount && cachePosition < cacheLimit; ++from)
            cache[cachePosition++] = samples[from];
        if (cachePosition == cacheLimit)
            DumpBlock();
    }
    samplesCached += sampleCount;
    if (samplesCached == totalSamples * channelCount)
        DumpBlock(cachePosition);
}

Limitless::~Limitless() {
    delete[] cache;
}
