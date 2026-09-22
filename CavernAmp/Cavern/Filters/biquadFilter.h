#ifndef BIQUADFILTER_H
#define BIQUADFILTER_H

#include "filter.h"
#include "../Utilities/complex.h"

/// Base class for biquad (second-order IIR) filters.
class BiquadFilter : public Filter {
protected:
    float x1, x2, y1, y2;
    float a1, a2, b0, b1, b2;

public:
    BiquadFilter() : x1(0), x2(0), y1(0), y2(0), a1(0), a2(0), b0(1), b1(0), b2(0) {}

    void ResetState() {
        x1 = x2 = y1 = y2 = 0;
    }

    void Process(float* samples, int len) override {
        Process(samples, len, 0, 1);
    }

    void Process(float* samples, int len, int channel, int channels) override {
        for (int sample = channel; sample < len; sample += channels) {
            float thisSample = samples[sample];
            samples[sample] = b2 * x2 + b1 * x1 + b0 * thisSample - a1 * y1 - a2 * y2;
            y2 = y1;
            y1 = samples[sample];
            x2 = x1;
            x1 = thisSample;
        }
    }

    /// Get the transfer function of this biquad filter.
    /// \param bins Number of frequency bins (linear spacing from 0 to Nyquist)
    /// \param output Pre-allocated array of Complex, size = bins
    void GetTransferFunction(int bins, Complex* output) const;

    /// Get the frequency response (magnitude) of this biquad filter.
    /// \param bins Number of frequency bins (linear spacing from 0 to Nyquist)
    /// \param output Pre-allocated array of float, size = bins
    void GetFrequencyResponse(int bins, float* output) const;

    virtual ~BiquadFilter() = default;
};

#ifdef __cplusplus
extern "C" {
#endif

/// Get the transfer function of a biquad filter instance.
/// \param instance Native BiquadFilter instance
/// \param bins Number of frequency bins (linear spacing from 0 to Nyquist)
/// \param output Pre-allocated array of Complex, size = bins
void DLL_EXPORT BiquadFilter_GetTransferFunction(const BiquadFilter* instance, int bins, Complex* output);

/// Get the frequency response (magnitude) of a biquad filter instance.
/// \param instance Native BiquadFilter instance
/// \param bins Number of frequency bins (linear spacing from 0 to Nyquist)
/// \param output Pre-allocated array of float, size = bins
void DLL_EXPORT BiquadFilter_GetFrequencyResponse(const BiquadFilter* instance, int bins, float* output);

#ifdef __cplusplus
}
#endif

#endif // BIQUADFILTER_H