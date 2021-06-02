#ifndef MEASUREMENTS_H
#define MEASUREMENTS_H

#include "complex.h"
#include "export.h"
#include "fftcache.h"

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
// Outputs IFFT(X) * N.
void DLL_EXPORT ProcessIFFT(Complex *samples, int sampleCount, FFTCache *cache, int depth);
// Inverse Fast Fourier Transform of a transformed signal, while keeping the source array allocation.
void DLL_EXPORT InPlaceIFFT(Complex *samples, int sampleCount, FFTCache *cache);

#ifdef __cplusplus
}
#endif

#endif // MEASUREMENTS_H
