#include "filter.h"

#ifndef PEAKINGFILTER_H
#define PEAKINGFILTER_H

#define Q_REF 0.7071067811865475

// Simple first-order biquad filter.
class PeakingFilter /*: public Filter*/ {
private:
    double centerFreq, q, gain;
    int sampleRate;
    float x1, x2, y1, y2, a1, a2, b0, b1, b2;

public:
    PeakingFilter(int sampleRate, double centerFreq, double q = Q_REF, double gain = 0);
    void Reset(double centerFreq, double q = Q_REF, double gain = 0);
    void Process(float* samples, int len);
    void Process(float* samples, int len, int channel, int channels);
};

#endif // PEAKINGFILTER_H
