#ifndef __Limitless_H__
#define __Limitless_H__

#include "Format.h"

class Limitless : public Format {
private:
    // Read helpers
    bool* writtenChannels;
    int64_t channelsToRead;
    int64_t samplesThisSecond;
    void GetLayout();
    // Write helpers
    float* cache;
    int64_t samplesCached;
    int64_t cachePosition;
    int64_t cacheLimit;
    void DumpBlock(int64_t);

public:
    Limitless(std::string fileName = "", bool write = false) : Format(fileName, write) {};
    void ReadHeader();
    void WriteHeader();
    void Read(float* samples, int64_t sampleCount);
    void Write(float* samples, int64_t sampleCount);
    ~Limitless();
};

#endif
