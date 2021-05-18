#include <math.h>

#include "measurements.h"
#include "qmath.h"

void DLL_EXPORT ProcessFFT(Complex *samples, int sampleCount, FFTCache *cache, int depth) {
    int halfLength = sampleCount / 2;
    if (sampleCount == 1)
        return;
    Complex *even = cache->even[depth], *odd = cache->odd[depth];
    for (int sample = 0, pair = 0; sample < halfLength; ++sample, pair += 2) {
        even[sample] = samples[pair];
        odd[sample] = samples[pair + 1];
    }
    ProcessFFT(even, halfLength, cache, --depth);
    ProcessFFT(odd, halfLength, cache, depth);
    int stepMul = cache->size() / halfLength;
    for (int i = 0; i < halfLength; ++i) {
        float oddReal = odd[i].real * cache->cos[i * stepMul] - odd[i].imaginary * cache->sin[i * stepMul],
            oddImag = odd[i].real * cache->sin[i * stepMul] + odd[i].imaginary * cache->cos[i * stepMul];
        samples[i].real = even[i].real + oddReal;
        samples[i].imaginary = even[i].imaginary + oddImag;
        samples[i + halfLength].real = even[i].real - oddReal;
        samples[i + halfLength].imaginary = even[i].imaginary - oddImag;
    }
}

void DLL_EXPORT ProcessFFT1D(float *samples, int sampleCount, FFTCache *cache) {
    int halfLength = sampleCount / 2, depth = log2(sampleCount) - 1;
    if (sampleCount == 1)
        return;
    Complex *even = cache->even[depth], *odd = cache->odd[depth];
    for (int sample = 0, pair = 0; sample < halfLength; ++sample, pair += 2) {
        even[sample].real = samples[pair];
        even[sample].imaginary = 0;
        odd[sample].real = samples[pair + 1];
        odd[sample].imaginary = 0;
    }
    ProcessFFT(even, halfLength, cache, --depth);
    ProcessFFT(odd, halfLength, cache, depth);
    int stepMul = cache->size() / halfLength;
    for (int i = 0; i < halfLength; ++i) {
        float oddReal = odd[i].real * cache->cos[i * stepMul] - odd[i].imaginary * cache->sin[i * stepMul],
            oddImag = odd[i].real * cache->sin[i * stepMul] + odd[i].imaginary * cache->cos[i * stepMul];
        float real = even[i].real + oddReal, imaginary = even[i].imaginary + oddImag;
        samples[i] = sqrtf(real * real + imaginary * imaginary);
        real = even[i].real - oddReal;
        imaginary = even[i].imaginary - oddImag;
        samples[i + halfLength] = sqrtf(real * real + imaginary * imaginary);
    }
}

void DLL_EXPORT InPlaceFFT(Complex *samples, int sampleCount, FFTCache *cache) {
    if (!cache) {
        cache = FFTCache_Create(sampleCount);
        ProcessFFT(samples, sampleCount, cache, log2(sampleCount) - 1);
        FFTCache_Dispose(cache);
    } else
        ProcessFFT(samples, sampleCount, cache, log2(sampleCount) - 1);
}

void DLL_EXPORT InPlaceFFT1D(float *samples, int sampleCount, FFTCache *cache) {
    if (!cache) {
        cache = FFTCache_Create(sampleCount);
        ProcessFFT1D(samples, sampleCount, cache);
        FFTCache_Dispose(cache);
    } else
        ProcessFFT1D(samples, sampleCount, cache);
}
