#ifndef FILTERANALYZER_H
#define FILTERANALYZER_H

#include "complex.h"
#include "fftcache.h"
#include "peakingFilter.h"

/// Class
// Measures properties of a filter, like frequency/impulse response, gain, or delay.
class FilterAnalyzer {
    PeakingFilter *filter;
    int sampleRate;

    int resolution;
    double startQ;
    double gainPrecision;
    double minGain;
    double maxGain;
    int iterations;

    float *impulseReference;
    FFTCache *cache;
    float *spectrum;

public:
    FilterAnalyzer(PeakingFilter *filter, const int sampleRate);
    void Reset(PeakingFilter *filter, const int sampleRate);
    void ClearFilter();
    int GetSampleRate() { return sampleRate; }
    float* GetSpectrum();
    ~FilterAnalyzer();

    int GetResolution() { return resolution; }
    void SetResolution(const int value);
    // TODO: move this to PeakingEqualizer
    double GetStartQ() { return startQ; }
    void SetStartQ(const double value) { startQ = value; }
    double GetGainPrecision() { return gainPrecision; }
    void SetGainPrecision(const double value) { gainPrecision = value; }
    double GetMinGain() { return minGain; }
    void SetMinGain(const double value) { minGain = value; }
    double GetMaxGain() { return maxGain; }
    void SetMaxGain(const double value) { maxGain = value; }
    int GetIterations() { return iterations; }
    void SetIterations(const int value) { iterations = value; }
};

#ifdef __cplusplus
extern "C" {
#endif

/// Exports
// Filter analyzer constructor.
FilterAnalyzer* DLL_EXPORT FilterAnalyzer_Create(const int sampleRate, const double maxGain, const double minGain, const double gainPrecision, const double startQ, const int iterations);
// Reset a filter with a PeakingEQ.
void DLL_EXPORT FilterAnalyzer_AddPEQ(FilterAnalyzer *analyzer, double centerFreq, double q, double gain);
// Dispose a filter analyzer.
void DLL_EXPORT FilterAnalyzer_Dispose(FilterAnalyzer *analyzer);

#ifdef __cplusplus
}
#endif

#endif // FILTERANALYZER_H
