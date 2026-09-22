#include "biquadFilter.h"

#ifndef PEAKINGEQ_H
#define PEAKINGEQ_H

#define Q_REF 0.7071067811865475

/// Simple first-order biquad filter.
class PeakingEQ : public BiquadFilter {
private:
    double centerFreq, q, gain;
    int sampleRate;

public:
    PeakingEQ(int sampleRate, double centerFreq, double q = Q_REF, double gain = 0);
    void Reset(double centerFreq, double q = Q_REF, double gain = 0);
    virtual Filter* Clone() const override;
    virtual ~PeakingEQ() = default;
};

#endif // PEAKINGEQ_H
