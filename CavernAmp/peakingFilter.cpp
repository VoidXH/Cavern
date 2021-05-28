#include <math.h>

#include "peakingFilter.h"

PeakingFilter::PeakingFilter(int sampleRate, double centerFreq, double q, double gain) {
    this->sampleRate = sampleRate;
    Reset(centerFreq, q, gain);
}

void PeakingFilter::Reset(double centerFreq, double q, double gain) {
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

void PeakingFilter::Process(float* samples, int len) {
    Process(samples, len, 0, 1);
}

void PeakingFilter::Process(float* samples, int len, int channel, int channels) {
    for (int sample = channel; sample < len; sample += channels) {
        float thisSample = samples[sample];
        samples[sample] = b2 * x2 + b1 * x1 + b0 * thisSample - a1 * y1 - a2 * y2;
        y2 = y1;
        y1 = samples[sample];
        x2 = x1;
        x1 = thisSample;
    }
}
