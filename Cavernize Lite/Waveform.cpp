#include "Waveform.h"
#include "Utils.h"
#include <iostream>
using namespace std;

void Waveform::ReadHeader() {
    char placeholder[4];
    int16_t formatFlags, channelCount16, blockAlign, bitDepth;
    int32_t fileLength, BPS, dataLength;
    fileHandler.read(placeholder, sizeof(char) * 4); // RIFF marker
    fileHandler.read(reinterpret_cast<char*>(&fileLength), sizeof(int32_t));
    fileHandler.read(placeholder, sizeof(char) * 4); // WAVE marker
    fileHandler.read(placeholder, sizeof(char) * 4); // FMT marker
    fileHandler.read(placeholder, sizeof(char) * 4); // FMT size
    fileHandler.read(reinterpret_cast<char*>(&formatFlags), sizeof(int16_t)); // Format flags
    fileHandler.read(reinterpret_cast<char*>(&channelCount16), sizeof(int16_t)); // Channel count in 16 bits
    fileHandler.read(reinterpret_cast<char*>(&sampleRate), sizeof(int32_t)); // Sample rate
    fileHandler.read(reinterpret_cast<char*>(&BPS), sizeof(int32_t)); // Bytes per second
    fileHandler.read(reinterpret_cast<char*>(&blockAlign), sizeof(int16_t)); // Block size in bytes
    fileHandler.read(reinterpret_cast<char*>(&bitDepth), sizeof(int16_t)); // Bit depth
    fileHandler.read(placeholder, sizeof(char) * 4); // Data marker
    while (placeholder[0] != 'd' || placeholder[1] != 'a' || placeholder[2] != 't' || placeholder[3] != 'a') { // Find the data block
        for (int i = 1; i < 4; ++i)
            placeholder[i - 1] = placeholder[i];
        fileHandler.read(reinterpret_cast<char*>(placeholder + 3), sizeof(char));
    }
    fileHandler.read(reinterpret_cast<char*>(&dataLength), sizeof(int32_t)); // Data length
    quality = formatFlags == 3 ? Float32 : (Quality)bitDepth;
    format = Channel; // WAVE can't be object-based
    channelCount = channelCount16;
    channels = DefaultChannelSet(channelCount);
    totalSamples = dataLength / ((bitDepth / 8) * channelCount);
}

void Waveform::WriteHeader() {
    char riff[5] = "RIFF", wave[5] = "WAVE", fmt[10] = {'f', 'm', 't', ' ', 16 /* fmt size */, 0, 0, 0, quality == Float32 ? (char)3 : (char)1 /* sample format */, 0}, data[5] = "data";
    fileHandler.write(riff, sizeof(char) * 4); // RIFF marker
    int16_t bits = (int16_t)quality;
    int32_t dataLength = totalSamples * channelCount * (bits / 8), fileLength = 36 /* header size */ + dataLength;
    fileHandler.write(reinterpret_cast<char*>(&fileLength), sizeof(int32_t)); // File length
    fileHandler.write(wave, sizeof(char) * 4); // WAVE marker
    // FMT header
    fileHandler.write(fmt, sizeof(char) * 10);
    int16_t channelCountOut = channelCount;
    fileHandler.write(reinterpret_cast<char*>(&channelCountOut), sizeof(int16_t)); // Audio channels
    fileHandler.write(reinterpret_cast<char*>(&sampleRate), sizeof(int32_t)); // Sample rate
    int16_t blockAlign = channelCount * (bits / 8);
    int32_t BPS = sampleRate * (int32_t)blockAlign;
    fileHandler.write(reinterpret_cast<char*>(&BPS), sizeof(int32_t)); // Bytes per second
    fileHandler.write(reinterpret_cast<char*>(&blockAlign), sizeof(int16_t)); // Block size in bytes
    fileHandler.write(reinterpret_cast<char*>(&bits), sizeof(int16_t)); // Bit depth
    // Data header
    fileHandler.write(data, sizeof(char) * 4);
    fileHandler.write(reinterpret_cast<char*>(&dataLength), sizeof(int32_t)); // Data length
}

void Waveform::Read(float* samples, int64_t sampleCount) {
    switch (quality) {
    case Int8: {
        uint8_t* sample = new uint8_t[sampleCount];
        fileHandler.read(reinterpret_cast<char*>(sample), sizeof(uint8_t) * sampleCount);
        for (int i = 0; i < sampleCount; ++i)
            samples[i] = (float)sample[i] / 127.0f - 1.0f;
        delete[] sample;
        break;
    }
    case Int16: {
        int16_t* sample = new int16_t[sampleCount];
        fileHandler.read(reinterpret_cast<char*>(sample), sizeof(int16_t) * sampleCount);
        for (int i = 0; i < sampleCount; ++i)
            samples[i] = (float)sample[i] / 32767.0f;
        delete[] sample;
        break;
    }
    case Float32:
        fileHandler.read(reinterpret_cast<char*>(samples), sizeof(float) * sampleCount);
        break;
    }
}

void Waveform::Write(float* samples, int64_t sampleCount) {
    switch (quality) {
    case Int8: {
        uint8_t* sample = new uint8_t[sampleCount];
        for (int i = 0; i < sampleCount; ++i)
            sample[i] = (samples[i] + 1.0f) * 127;
        fileHandler.write(reinterpret_cast<char*>(sample), sizeof(uint8_t) * sampleCount);
        delete[] sample;
        break;
    }
    case Int16: {
        int16_t* sample = new int16_t[sampleCount];
        for (int i = 0; i < sampleCount; ++i)
            sample[i] = samples[i] * 32767;
        fileHandler.write(reinterpret_cast<char*>(sample), sizeof(int16_t) * sampleCount);
        delete[] sample;
        break;
    }
    case Float32:
        fileHandler.write(reinterpret_cast<char*>(samples), sizeof(float) * sampleCount);
        break;
    }
}
