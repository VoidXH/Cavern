#ifndef __Waveform_H__
#define __Waveform_H__

#include "Format.h"

class Waveform : public Format {
public:
    Waveform(std::string fileName = "", bool write = false) : Format(fileName, write) {};
    void ReadHeader();
    void ForceDCPStandardOrder();
    void WriteHeader();
    void Read(float* samples, int64_t sampleCount);
    void Write(float* samples, int64_t sampleCount);
};

#endif
