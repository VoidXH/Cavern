#include <math.h>

#include "peakingEQ.h"

PeakingEQ::PeakingEQ(int sampleRate, double centerFreq, double q, double gain) {
    this->sampleRate = sampleRate;
    Reset(centerFreq, q, gain);
}

void PeakingEQ::Reset(double centerFreq, double q, double gain) {
    this->centerFreq = centerFreq;
    this->q = q;
    this->gain = gain;
    float w0 = M_PI * 2 * centerFreq / sampleRate, cos = cosf(w0), alpha = sinf(w0) / (q + q),
        a = powf(10, gain * 0.025f), // gain is doubled for some reason
        divisor = 1 / (1 + alpha / a); // 1 / a0
    b0 = (1 + alpha * a) * divisor;
    b2 = (1 - alpha * a) * divisor;
    a1 = b1 = -2 * cos * divisor;
    a2 = (1 - alpha / a) * divisor;
}

Filter* PeakingEQ::Clone() const {
    return new PeakingEQ(sampleRate, centerFreq, q, gain);
}
