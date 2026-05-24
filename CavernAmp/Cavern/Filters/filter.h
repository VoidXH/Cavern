#ifndef FILTER_H
#define FILTER_H

#include "../../export.h"

// Abstract base class for filters.
class Filter {
public:
    virtual void Process(float* samples, int len) = 0;
    virtual void Process(float* samples, int len, int channel, int channels) = 0;
    virtual Filter* Clone() const = 0;
    virtual ~Filter() { };
};

#ifdef __cplusplus
extern "C" {
#endif

/// Apply a filter to an array of samples (single channel).
void DLL_EXPORT Filter_Process(Filter* instance, float* samples, int len);
/// Apply a filter to an array of samples (interleaved channels).
void DLL_EXPORT Filter_ProcessChannel(Filter* instance, float* samples, int len, int channel, int channels);
/// Create a copy of the filter.
Filter* DLL_EXPORT Filter_Clone(Filter* instance);
/// Free up a filter's memory.
void DLL_EXPORT Filter_Dispose(Filter* instance);

#ifdef __cplusplus
}
#endif

#endif // FILTER_H
