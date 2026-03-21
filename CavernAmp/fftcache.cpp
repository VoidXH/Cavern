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
    sin = new float[depth];
    cos = new float[depth];
    for (int i = 0; i < depth; ++i) {
        float rotation = i * step;
        cos[i] = cosf(rotation);
        sin[i] = sinf(rotation);
    }

    int maxDepth = log2(fftSize);
    even = new Complex*[maxDepth];
    odd = new Complex*[maxDepth];
    for (int idepth = 0; idepth < maxDepth; idepth++) {
        even[idepth] = new Complex[1 << idepth];
        odd[idepth] = new Complex[1 << idepth];
    }
}

int FFTCache::size() const {
    return depth;
}

FFTCache::~FFTCache() {
    delete[] sin;
    delete[] cos;
    for (int idepth = 0, maxDepth = log2(depth * 2); idepth < maxDepth; idepth++) {
        delete[] even[idepth];
        delete[] odd[idepth];
    }
    delete[] even;
    delete[] odd;
}

FFTCache* DLL_EXPORT FFTCache_Create(const int fftSize) {
    return new FFTCache(fftSize);
}

int DLL_EXPORT FFTCache_Size(const FFTCache *cache) {
    return cache->size();
}

void DLL_EXPORT FFTCache_Dispose(FFTCache *cache) {
    delete cache;
}
