#ifndef PEAKINGEQUALIZER_H
#define PEAKINGEQUALIZER_H

#include "filterAnalyzer.h"

#define LOG10_20 1.3010299956639811952137388947245

struct PeakingEQ {
    double centerFreq;
    double q;
    double gain;
};

// Measure a filter candidate for "BruteForceQ".
float BruteForceStepInternal(float *target, int targetLength, float *&changedTarget, FilterAnalyzer *analyzer);

#ifdef __cplusplus
extern "C" {
#endif

// Measure a filter candidate for "BruteForceQ".
float DLL_EXPORT BruteForceStep(float *target, int targetLength, float *changedTarget, FilterAnalyzer *analyzer);
// Find the filter with the best Q for the given frequency and gain in "target".
// Correct "target" to the frequency response with the inverse of the found filter.
PeakingEQ DLL_EXPORT BruteForceQ(float *target, int targetLength, FilterAnalyzer *analyzer, double freq, double gain);
// Finds a PeakingEQ to correct the worst problem on the input spectrum.
PeakingEQ DLL_EXPORT BruteForceBand(float *target, int targetLength, FilterAnalyzer *analyzer, int startPos, int stopPos);

#ifdef __cplusplus
}
#endif

#endif // PEAKINGEQUALIZER_H
