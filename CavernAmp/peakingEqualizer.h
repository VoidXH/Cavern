#ifndef PEAKINGEQUALIZER_H
#define PEAKINGEQUALIZER_H

#include "filterAnalyzer.h"

struct PeakingEQ {
    double centerFreq;
    double q;
    double gain;
};

// Measure a filter candidate for "BruteForceQ".
float BruteForceStep(float* target, int targetLength, float* &changedTarget, FilterAnalyzer* analyzer);

// Find the filter with the best Q for the given frequency and gain in "target".
// Correct "target" to the frequency response with the inverse of the found filter.
PeakingEQ BruteForceQ(float* target, int targetLength, FilterAnalyzer* analyzer, double freq, double gain);

// Finds a PeakingEQ to correct the worst problem on the input spectrum.
PeakingEQ BruteForceBand(float* target, int targetLength, FilterAnalyzer* analyzer, int startFreq, int stopFreq);

#endif // PEAKINGEQUALIZER_H
