#include <stdlib.h>

#include "filterAnalyzer.h"
#include "measurements.h"

FilterAnalyzer::FilterAnalyzer(PeakingFilter* filter, int sampleRate) : filter(filter), sampleRate(sampleRate),
    resolution(65536), startQ(10), gainPrecision(.01), minGain(-100), maxGain(20), iterations(8) {
    impulseReference = (float*)malloc(resolution * sizeof(float));
    memset(impulseReference, 0, resolution * sizeof(float));
    impulseReference[0] = 1;
    cache = FFTCache_Create(resolution);
    spectrum = (float*)malloc(resolution * sizeof(float));
}

void FilterAnalyzer::Reset(PeakingFilter* filter, int sampleRate) {
    free(filter);
    this->filter = filter;
    this->sampleRate = sampleRate;
}

float* FilterAnalyzer::GetSpectrum() {
    memcpy(spectrum, impulseReference, resolution * sizeof(float));
    filter->Process(spectrum, resolution);
    InPlaceFFT1D(spectrum, resolution, cache);
    return spectrum;
}

FilterAnalyzer::~FilterAnalyzer() {
    free(impulseReference);
    // Calling FFTCache_Dispose would break DLL import.
    cache->~FFTCache();
    free(cache);
    free(spectrum);
}
