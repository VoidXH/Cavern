#include "complex.h"
#include "export.h"
#include "fftcache.h"

#ifndef MEASUREMENTS_H
#define MEASUREMENTS_H

#ifdef __cplusplus
extern "C" {
#endif

// Actual FFT processing, somewhat in-place.
void DLL_EXPORT ProcessFFT(Complex *samples, int sampleCount, FFTCache *cache, int depth);
// Fourier-transform a signal in 1D. The result is the spectral power.
void DLL_EXPORT ProcessFFT1D(float *samples, int sampleCount, FFTCache *cache);
// Fast Fourier transform a 2D signal while keeping the source array allocation.
void DLL_EXPORT InPlaceFFT(Complex *samples, int sampleCount, FFTCache *cache);
// Spectrum of a signal's FFT while keeping the source array allocation.
void DLL_EXPORT InPlaceFFT1D(float *samples, int sampleCount, FFTCache *cache);

#ifdef __cplusplus
}
#endif

#endif // MEASUREMENTS_H
