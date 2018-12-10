#ifndef __OBAE_H__
#define __OBAE_H__

#include "Format.h"

enum KLVType {
    Unknown,
    ObjectFrame,
    Footer
};

struct KLV {
    KLVType key;
    std::string rawKey;
    uint32_t length;
    std::string value;
};

class OBAE : public Format {
    bool Compare(const char* a, const char* b, int32_t length);
    std::string ToString(const char* a, int32_t length);
    KLV NextKLV();

public:
    OBAE(std::string fileName = "", bool write = false) : Format(fileName, write) {};
    void ReadHeader();
    void WriteHeader();
    void Read(float* samples, int64_t sampleCount);
    void Write(float* samples, int64_t sampleCount);
};

#endif
