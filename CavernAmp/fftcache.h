#ifndef FFTCACHE_H
#define FFTCACHE_H

#include "complex.h"
#include "export.h"

/// Class
// Precalculated constants and preallocated recursion arrays for a given FFT size.
class FFTCache {
    int depth;

public:
    float *sin, *cos;
    Complex **even, **odd;

    // FFT cache constructor.
    FFTCache(const int fftSize);
    // Get the creation size of the FFT cache.
    int size() const;
    // FFT cache destructor.
    ~FFTCache();
};

#ifdef __cplusplus
extern "C" {
#endif

/// Exports
// FFT cache constructor.
FFTCache* DLL_EXPORT FFTCache_Create(const int fftSize);
// Get the creation size of the FFT cache.
int DLL_EXPORT FFTCache_Size(const FFTCache *cache);
// Dispose an FFT cache.
void DLL_EXPORT FFTCache_Dispose(FFTCache *cache);

#ifdef __cplusplus
}
#endif

#endif // FFTCACHE_H
