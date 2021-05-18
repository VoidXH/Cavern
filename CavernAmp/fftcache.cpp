#include <new>
#include <math.h>
#include <stdint.h>
#include <stdlib.h>

#include "fftcache.h"
#include "main.h"
#include "qmath.h"

FFTCache::FFTCache(const int fftSize) {
    depth = fftSize / 2;
    double step = -2 * M_PI / fftSize;
    sin = (float*)malloc(sizeof(float) * depth);
    cos = (float*)malloc(sizeof(float) * depth);
    for (int i = 0; i < depth; ++i) {
        float rotation = i * step;
        cos[i] = cosf(rotation);
        sin[i] = sinf(rotation);
    }

    int maxDepth = log2(fftSize);
    even = (Complex**)malloc(sizeof(Complex*) * maxDepth);
    odd = (Complex**)malloc(sizeof(Complex*) * maxDepth);
    for (int idepth = 0; idepth < maxDepth; ++idepth) {
        even[idepth] = (Complex*)malloc(sizeof(Complex) * (1 << idepth));
        odd[idepth] = (Complex*)malloc(sizeof(Complex) * (1 << idepth));
    }
}

int FFTCache::size() const {
    return depth;
}

FFTCache::~FFTCache() {
    free(sin);
    free(cos);
    for (int idepth = 0, maxDepth = log2(depth * 2); idepth < maxDepth; ++idepth) {
        free(even[idepth]);
        free(odd[idepth]);
    }
    free(even);
    free(odd);
}

FFTCache* DLL_EXPORT FFTCache_Create(const int fftSize) {
    FFTCache* cache = (FFTCache*)malloc(sizeof(FFTCache));
    new(cache) FFTCache(fftSize);
    return cache;
}

int DLL_EXPORT FFTCache_Size(const FFTCache *cache) {
    return cache->size();
}

void DLL_EXPORT FFTCache_Dispose(FFTCache *cache) {
    cache->~FFTCache();
    free(cache);
}
