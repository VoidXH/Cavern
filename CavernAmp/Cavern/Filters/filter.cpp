#include "filter.h"

void DLL_EXPORT Filter_Process(Filter* instance, float* samples, int len) {
    instance->Process(samples, len);
}

void DLL_EXPORT Filter_ProcessChannel(Filter* instance, float* samples, int len, int channel, int channels) {
    instance->Process(samples, len, channel, channels);
}

Filter* DLL_EXPORT Filter_Clone(Filter* instance) {
    return instance->Clone();
}

void DLL_EXPORT Filter_Dispose(Filter* instance) {
    delete instance;
}
