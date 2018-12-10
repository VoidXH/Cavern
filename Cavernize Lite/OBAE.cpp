#include "OBAE.h"
#include "Utils.h"
#include <iostream>
#include <fstream>
using namespace std;

bool OBAE::Compare(const char* a, const char* b, int32_t length) {
    for (int32_t i = 0; i < length; ++i)
        if (a[i] != b[i])
            return false;
    return true;
}

string OBAE::ToString(const char* a, int32_t length) {
    string ret;
    for (int32_t i = 0; i < length; ++i)
        ret.push_back(a[i]);
    return ret;
}

KLV OBAE::NextKLV() {
    const char objectFrame[14] = { 0x2B, 0x34, 0x01, 0x02, 0x01, 0x05, 0x0E, 0x09, 0x06, 0x01, 0x00, 0x00, 0x00, 0x01 };
    const char footer[14] = { 0x2B, 0x34, 0x02, 0x05, 0x01, 0x01, 0x0D, 0x01, 0x02, 0x01, 0x01, 0x11, 0x01, 0x00 };
    char OID, keyLength;
    fileHandler.read(&OID, sizeof(char));
    fileHandler.read(&keyLength, sizeof(char));
    char rawKey[keyLength];
    fileHandler.read(rawKey, sizeof(char) * keyLength);
    uint32_t length = 0;
    uint8_t lengthMarker;
    fileHandler.read(reinterpret_cast<char*>(&lengthMarker), sizeof(uint8_t));
    if (lengthMarker < 0x80)
        length = lengthMarker;
    else {
        lengthMarker -= 0x80;
        while (lengthMarker-- > 0) {
            uint8_t byte;
            fileHandler.read(reinterpret_cast<char*>(&byte), sizeof(uint8_t));
            length = length * 256 + byte;
        }
    }
    char* value = new char[length];
    fileHandler.read(value, length);
    KLV ret;
    ret.key = Unknown;
    if (keyLength >= 14) {
        if (Compare(rawKey, objectFrame, keyLength))
            ret.key = ObjectFrame;
        if (Compare(rawKey, footer, keyLength))
            ret.key = Footer;
    }
    ret.rawKey = ToString(rawKey, keyLength);
    ret.length = length;
    ret.value = ToString(value, length);
    delete[] value;
    return ret;
}

void OBAE::ReadHeader() {
}

void OBAE::WriteHeader() {
}

void OBAE::Read(float* samples, int64_t sampleCount) {
    // Dummy to suppress warnings
    for (int64_t i = 0; i < sampleCount; ++i)
        samples[i] = 0;
}

void OBAE::Write(float* samples, int64_t sampleCount) {
    // Dummy to suppress warnings
    float t;
    for (int64_t i = 0; i < sampleCount; ++i)
        t += samples[i];
}
