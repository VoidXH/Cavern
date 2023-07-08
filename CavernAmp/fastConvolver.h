#ifndef FASTCONVOLVER_H
#define FASTCONVOLVER_H

#include "complex.h"
#include "export.h"
#include "fftcache.h"
#include "filter.h"

#define Q_REF 0.7071067811865475

/// Class
// Performs an optimized convolution.
class FastConvolver /*: public Filter*/ {
private:
    // Created convolution filter in Fourier-space.
    Complex *filter;

    // Cache to perform the FFT in.
    Complex *present;

    // Length of filter and present.
    int filterLength;

    // Overlap samples from previous runs.
    float *future;

    // FFT optimization.
    FFTCache *cache;

    // Delay applied with the convolution.
    int delay;

    // Internal constructor behavior.
    void Initialize(const float *impulse, const int len, const int delay);

    // In case there are more input samples than the size of the filter, split it in parts.
    void ProcessTimeslot(float *samples, int channel, int channels, int from, int to);

    // When present is filled with the source samples, it will be convolved and put into the future.
    void ProcessCache(const int maxResultLength);

public:
    // Constructs an optimized convolution with no delay.
    FastConvolver(const float *impulse, const int len);

    // Constructs an optimized convolution with added delay.
    FastConvolver(const float *impulse, const int len, const int delay);

    // Apply convolution on an array of samples. One filter should be applied to only one continuous stream of samples.
    void Process(float *samples, int len);
    void Process(float *samples, int len, int channel, int channels);
    ~FastConvolver();
};

#ifdef __cplusplus
extern "C" {
#endif

/// Exports
// FastConvolver constructor.
FastConvolver* DLL_EXPORT FastConvolver_Create(const float *impulse, const int len, const int delay);
// Process a block of samples with a FastConvolver.
void DLL_EXPORT FastConvolver_Process(FastConvolver *instance, float *samples, int len, int channel, int channels);
// Dispose a FastConvolver.
void DLL_EXPORT FastConvolver_Dispose(FastConvolver *instance);

#ifdef __cplusplus
}
#endif

#endif // FASTCONVOLVER_H
