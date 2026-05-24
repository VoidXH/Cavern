#ifndef GAIN_H
#define GAIN_H

#include <cmath>
#include "filter.h"
#include "../Utilities/qmath.h"

/// \brief Signal level multiplier filter.
class Gain : public Filter {
private:
    /// Filter gain as a linear multiplier.
    float gainValue;

public:
    /// Constructs a Gain filter with the specified gain in decibels.
    Gain(double db) : gainValue(DbToGain(db)) { }

    /// Copy constructor.
    Gain(const Gain& other) : gainValue(other.gainValue) { }

    /// Returns the gain in decibels.
    double GetGainValue() const {
        return GainToDb(gainValue);
    }

    /// Sets the gain in decibels.
    void SetGainValue(double db) {
        gainValue = DbToGain(db);
    }

    /// Returns whether the phase is inverted.
    bool GetInvert() const {
        return gainValue < 0.0f;
    }

    /// Sets whether the phase is inverted.
    void SetInvert(bool invert) {
        gainValue = invert ? -std::abs(gainValue) : std::abs(gainValue);
    }

    /// Apply gain on an array of samples. One filter should be applied to only one continuous stream of samples.
    void Process(float *samples, int len);
    void Process(float *samples, int len, int channel, int channels);
    Filter* Clone() const override;
};

#ifdef __cplusplus
extern "C" {
#endif

/// Constructs a Gain filter with the specified gain in decibels.
Gain* DLL_EXPORT Gain_Create(double db);
/// Returns the gain in decibels.
double DLL_EXPORT Gain_GetGainValue(Gain* instance);
/// Sets the gain in decibels.
void DLL_EXPORT Gain_SetGainValue(Gain* instance, double db);
/// Returns whether the phase is inverted.
bool DLL_EXPORT Gain_GetInvert(Gain* instance);
/// Sets whether the phase is inverted.
void DLL_EXPORT Gain_SetInvert(Gain* instance, bool invert);

#ifdef __cplusplus
}
#endif

#endif // GAIN_H
