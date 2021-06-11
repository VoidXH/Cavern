#ifndef FILTERANALYZER_H
#define FILTERANALYZER_H

#include "complex.h"
#include "fftcache.h"
#include "peakingFilter.h"

class FilterAnalyzer {
    PeakingFilter* filter;
    int sampleRate;

    int resolution;
    double startQ;
    double gainPrecision;
    double minGain;
    double maxGain;
    int iterations;

    float* impulseReference;
    FFTCache* cache;
    float* spectrum;

public:
    FilterAnalyzer(PeakingFilter* filter, int sampleRate);
    void Reset(PeakingFilter* filter, int sampleRate);
    int GetSampleRate() { return sampleRate; }
    float* GetSpectrum();
    ~FilterAnalyzer();

    int GetResolution() { return resolution; }
    void SetResolution(int value) { resolution = value; }
    double GetStartQ() { return startQ; }
    void SetStartQ(double value) { startQ = value; }
    double GetGainPrecision() { return gainPrecision; }
    void SetGainPrecision(double value) { gainPrecision = value; }
    double GetMinGain() { return minGain; }
    void SetMinGain(double value) { minGain = value; }
    double GetMaxGain() { return maxGain; }
    void SetMaxGain(double value) { maxGain = value; }
    int GetIterations() { return iterations; }
    void SetIterations(int value) { iterations = value; }
};

#endif // FILTERANALYZER_H
