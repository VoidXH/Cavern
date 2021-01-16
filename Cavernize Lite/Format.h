#ifndef __Format_H__
#define __Format_H__

#include <fstream>

#include "AudioChannel.h"
#include "Enums.h"

class Format {
protected:
    std::fstream fileHandler;
    bool write;

public:
    Quality quality;
    SpatialFormat format;
    AudioChannel** channels;
    int32_t channelCount;
    int32_t sampleRate;
    int64_t totalSamples; // For a single channel

    Format(std::string filename = "", bool write = false);
    virtual ~Format();

    virtual void ReadHeader() = 0;
    virtual void ForceDCPStandardOrder() {}
    virtual void WriteHeader() = 0;
    virtual void Read(float* samples, int64_t sampleCount) = 0;
    virtual void Write(float* samples, int64_t sampleCount) = 0;
};

#endif
