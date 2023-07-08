#include <cstring>
#include <new>
#include <stdlib.h>

#include "filterAnalyzer.h"
#include "measurements.h"

FilterAnalyzer::FilterAnalyzer(PeakingFilter *filter, const int sampleRate) : filter(filter), sampleRate(sampleRate),
    startQ(10), gainPrecision(.01), minGain(-100), maxGain(20), iterations(8), impulseReference(NULL) {
    SetResolution(65536);
}

void FilterAnalyzer::Reset(PeakingFilter *filter, const int sampleRate) {
    if (this->filter)
        free(this->filter);
    this->filter = filter;
    this->sampleRate = sampleRate;
}

void FilterAnalyzer::ClearFilter() {
    if (filter) {
        free(filter);
        filter = NULL;
    }
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

void FilterAnalyzer::SetResolution(const int value) {
    resolution = value;
    if (impulseReference)
        this->~FilterAnalyzer();
    impulseReference = (float*)malloc(resolution * sizeof(float));
    memset(impulseReference, 0, resolution * sizeof(float));
    impulseReference[0] = 1;
    cache = FFTCache_Create(resolution);
    spectrum = (float*)malloc(resolution * sizeof(float));
}

FilterAnalyzer* DLL_EXPORT FilterAnalyzer_Create(const int sampleRate, const double maxGain, const double minGain, const double gainPrecision, const double startQ, const int iterations) {
    FilterAnalyzer* analyzer = (FilterAnalyzer*)malloc(sizeof(FilterAnalyzer));
    new(analyzer) FilterAnalyzer(NULL, sampleRate);
    analyzer->SetMaxGain(maxGain);
    analyzer->SetMinGain(minGain);
    analyzer->SetGainPrecision(gainPrecision);
    analyzer->SetStartQ(startQ);
    analyzer->SetIterations(iterations);
    return analyzer;
}

void DLL_EXPORT FilterAnalyzer_AddPEQ(FilterAnalyzer *analyzer, double centerFreq, double q, double gain) {
    PeakingFilter *newFilter = (PeakingFilter*)malloc(sizeof(PeakingFilter));
    new(newFilter) PeakingFilter(analyzer->GetSampleRate(), centerFreq, q, gain);
    analyzer->Reset(newFilter, analyzer->GetSampleRate());
}

void DLL_EXPORT FilterAnalyzer_Dispose(FilterAnalyzer *analyzer) {
    analyzer->ClearFilter();
    analyzer->~FilterAnalyzer();
    free(analyzer);
}
