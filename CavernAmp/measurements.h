#include "complex.h"
#include "export.h"

#ifndef MEASUREMENTS_H
#define MEASUREMENTS_H

#ifdef __cplusplus
extern "C" {
#endif

// Actual FFT processing, somewhat in-place.
void DLL_EXPORT ProcessFFT(Complex *samples, int sampleCount, FFTCache *cache, int depth);

#ifdef __cplusplus
}
#endif

#endif // MEASUREMENTS_H
