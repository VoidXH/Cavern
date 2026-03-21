#include <cstring>
#include <new>
#include <stdlib.h>

#include "filterAnalyzer.h"
#include "measurements.h"

FilterAnalyzer::FilterAnalyzer(PeakingFilter *filter, const int sampleRate) : filter(filter), sampleRate(sampleRate),
    startQ(10), gainPrecision(.01), minGain(-100), maxGain(20), iterations(8), impulseReference(nullptr) {
    SetResolution(65536);
}

void FilterAnalyzer::Reset(PeakingFilter *filter, const int sampleRate) {
    if (this->filter) {
        delete this->filter;
    }
    this->filter = filter;
    this->sampleRate = sampleRate;
}

void FilterAnalyzer::ClearFilter() {
    if (filter) {
        delete filter;
        filter = nullptr;
    }
}

float* FilterAnalyzer::GetSpectrum() {
    memcpy(spectrum, impulseReference, resolution * sizeof(float));
    filter->Process(spectrum, resolution);
    InPlaceFFT1D(spectrum, resolution, cache);
    return spectrum;
}

FilterAnalyzer::~FilterAnalyzer() {
    delete[] impulseReference;
    delete cache;
    delete[] spectrum;
}

void FilterAnalyzer::SetResolution(const int value) {
    resolution = value;
    if (impulseReference) {
        this->~FilterAnalyzer();
    }
    impulseReference = new float[resolution];
    memset(impulseReference, 0, resolution * sizeof(float));
    impulseReference[0] = 1;
    cache = new FFTCache(resolution);
    spectrum = new float[resolution];
}

FilterAnalyzer* DLL_EXPORT FilterAnalyzer_Create(const int sampleRate, const double maxGain, const double minGain, const double gainPrecision, const double startQ, const int iterations) {
    FilterAnalyzer* analyzer = new FilterAnalyzer(nullptr, sampleRate);
    analyzer->SetMaxGain(maxGain);
    analyzer->SetMinGain(minGain);
    analyzer->SetGainPrecision(gainPrecision);
    analyzer->SetStartQ(startQ);
    analyzer->SetIterations(iterations);
    return analyzer;
}

void DLL_EXPORT FilterAnalyzer_AddPEQ(FilterAnalyzer *analyzer, double centerFreq, double q, double gain) {
    PeakingFilter *newFilter = new PeakingFilter(analyzer->GetSampleRate(), centerFreq, q, gain);
    analyzer->Reset(newFilter, analyzer->GetSampleRate());
}

void DLL_EXPORT FilterAnalyzer_Dispose(FilterAnalyzer *analyzer) {
    analyzer->ClearFilter();
    delete analyzer;
}
