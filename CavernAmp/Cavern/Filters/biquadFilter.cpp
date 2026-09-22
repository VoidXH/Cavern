#include <math.h>
#include "biquadFilter.h"

void BiquadFilter::GetTransferFunction(int bins, Complex* output) const {
    if (bins <= 0 || !output) return;

    for (int i = 0; i < bins; i++) {
        // z = e^(jω), so z⁻¹ = e^(-jω)
        Complex zInv = Complex::UnitPhase(-(2.0f * M_PI * i / bins));
        Complex zInvSq = zInv * zInv;

        // Numerator: b0 + b1·z⁻¹ + b2·z⁻²
        Complex numerator = Complex(b0) + zInv * b1 + zInvSq * b2;

        // Denominator: 1 + a1·z⁻¹ + a2·z⁻²
        Complex denominator = Complex(1.0f) + zInv * a1 + zInvSq * a2;

        // H = numerator / denominator
        output[i] = numerator / denominator;
    }
}

void BiquadFilter::GetFrequencyResponse(int bins, float* output) const {
    if (bins <= 0 || !output) return;

    for (int i = 0; i < bins; i++) {
        float omega = 2.0f * M_PI * i / bins;
        float cos = cosf(omega);
        float sin = -sinf(omega); // z⁻¹ = e^(-jω)

        // Numerator: b0 + b1·z⁻¹ + b2·z⁻²
        float numReal = b0 + b1 * cos + b2 * (cos * cos - sin * sin);
        float numImag = b1 * sin + b2 * (2 * cos * sin);

        // Denominator: 1 + a1·z⁻¹ + a2·z⁻²
        float denReal = 1.0f + a1 * cos + a2 * (cos * cos - sin * sin);
        float denImag = a1 * sin + a2 * (2 * cos * sin);

        // |H| = sqrt(numReal² + numImag²) / sqrt(denReal² + denImag²)
        float numMag = sqrtf(numReal * numReal + numImag * numImag);
        float denMag = sqrtf(denReal * denReal + denImag * denImag);
        output[i] = (denMag > 0) ? numMag / denMag : 0;
    }
}

#ifdef __cplusplus
extern "C" {
#endif

void DLL_EXPORT BiquadFilter_GetTransferFunction(const BiquadFilter* instance, int bins, Complex* output) {
    if (instance) {
        instance->GetTransferFunction(bins, output);
    }
}

void DLL_EXPORT BiquadFilter_GetFrequencyResponse(const BiquadFilter* instance, int bins, float* output) {
    if (instance) {
        instance->GetFrequencyResponse(bins, output);
    }
}

#ifdef __cplusplus
}
#endif