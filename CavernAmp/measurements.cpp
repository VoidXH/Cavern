#include "fftcache.h"
#include "measurements.h"

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
