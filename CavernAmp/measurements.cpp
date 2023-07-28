#include <math.h>

#include "measurements.h"
#include "qmath.h"

void DLL_EXPORT ProcessFFT(Complex *samples, int sampleCount, FFTCache *cache, int depth) {
    if (sampleCount < 8) {
        if (sampleCount == 4) {
            Complex evenValue = samples[0],
                oddValue = samples[2];
            Complex evenValue1 { evenValue.real + oddValue.real, evenValue.imaginary + oddValue.imaginary };
            Complex evenValue2 { evenValue.real - oddValue.real, evenValue.imaginary - oddValue.imaginary };

            evenValue = samples[1];
            oddValue = samples[3];
            Complex oddValue1 { evenValue.real + oddValue.real, evenValue.imaginary + oddValue.imaginary };
            Complex oddValue2 { evenValue.real - oddValue.real, evenValue.imaginary - oddValue.imaginary };

            samples[0] = { evenValue1.real + oddValue1.real, evenValue1.imaginary + oddValue1.imaginary };
            samples[1] = { evenValue2.real + oddValue2.imaginary, evenValue2.imaginary - oddValue2.real };
            samples[2] = { evenValue1.real - oddValue1.real, evenValue1.imaginary - oddValue1.imaginary };
            samples[3] = { evenValue2.real - oddValue2.imaginary, evenValue2.imaginary + oddValue2.real };
        } else if (sampleCount == 2) {
            Complex evenValue = samples[0],
                oddValue = samples[1];
            samples[0] = { evenValue.real + oddValue.real, evenValue.imaginary + oddValue.imaginary };
            samples[1] = { evenValue.real - oddValue.real, evenValue.imaginary - oddValue.imaginary };
        }
        return;
    }
    int halfLength = sampleCount / 2;
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

void DLL_EXPORT ProcessIFFT(Complex *samples, int sampleCount, FFTCache *cache, int depth) {
    if (sampleCount == 1)
        return;
    Complex *even = cache->even[depth], *odd = cache->odd[depth];
    int halfLength = sampleCount / 2;
    for (int sample = 0, pair = 0; sample < halfLength; ++sample, pair += 2) {
        even[sample] = samples[pair];
        odd[sample] = samples[pair + 1];
    }
    ProcessIFFT(even, halfLength, cache, --depth);
    ProcessIFFT(odd, halfLength, cache, depth);
    int stepMul = cache->size() / halfLength;
    for (int i = 0; i < halfLength; ++i) {
        float oddReal = odd[i].real * cache->cos[i * stepMul] + odd[i].imaginary * cache->sin[i * stepMul],
            oddImag = odd[i].imaginary * cache->cos[i * stepMul] - odd[i].real * cache->sin[i * stepMul];
        samples[i].real = even[i].real + oddReal;
        samples[i].imaginary = even[i].imaginary + oddImag;
        samples[i + halfLength].real = even[i].real - oddReal;
        samples[i + halfLength].imaginary = even[i].imaginary - oddImag;
    }
}

void DLL_EXPORT InPlaceIFFT(Complex *samples, int sampleCount, FFTCache *cache) {
    if (!cache) {
        cache = FFTCache_Create(sampleCount);
        ProcessIFFT(samples, sampleCount, cache, log2(sampleCount) - 1);
        FFTCache_Dispose(cache);
    } else
        ProcessIFFT(samples, sampleCount, cache, log2(sampleCount) - 1);
    float multiplier = 1.f / sampleCount;
    for (int i = 0; i < sampleCount; ++i) {
        samples[i].real *= multiplier;
        samples[i].imaginary *= multiplier;
    }
}
