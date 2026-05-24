#include "gain.h"

using namespace std;

void Gain::Process(float* samples, int len) {
    for (int sample = 0; sample < len; sample++) {
        samples[sample] *= gainValue;
    }
}

void Gain::Process(float* samples, int len, int channel, int channels) {
    for (int sample = channel; sample < len; sample += channels) {
        samples[sample] *= gainValue;
    }
}

Filter* Gain::Clone() const {
    return new Gain(*this);
}

Gain* DLL_EXPORT Gain_Create(double db) {
    return new Gain(db);
}

double DLL_EXPORT Gain_GetGainValue(Gain* instance) {
    return instance->GetGainValue();
}

void DLL_EXPORT Gain_SetGainValue(Gain* instance, double db) {
    instance->SetGainValue(db);
}

bool DLL_EXPORT Gain_GetInvert(Gain* instance) {
    return instance->GetInvert();
}

void DLL_EXPORT Gain_SetInvert(Gain* instance, bool invert) {
    instance->SetInvert(invert);
}
