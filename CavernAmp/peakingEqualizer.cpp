#include <cstring>
#include <new>

#include "graphUtils.h"
#include "peakingEqualizer.h"
#include "peakingFilter.h"
#include "qmath.h"
#include "waveformUtils.h"

float BruteForceStepInternal(float *target, int targetLength, float *&changedTarget, FilterAnalyzer *analyzer) {
    changedTarget = ConvertToGraph(analyzer->GetSpectrum(), analyzer->GetResolution() / 2, 20, analyzer->GetSampleRate() * .5, analyzer->GetSampleRate(), targetLength);
    ConvertToDecibels(changedTarget, targetLength);
    Mix(target, changedTarget, targetLength);
    return SumAbs(changedTarget, targetLength);
}

float DLL_EXPORT BruteForceStep(float *target, int targetLength, float *changedTarget, FilterAnalyzer *analyzer) {
    ConvertToGraph(analyzer->GetSpectrum(), analyzer->GetResolution() / 2, 20, analyzer->GetSampleRate() * .5, analyzer->GetSampleRate(), changedTarget, targetLength);
    ConvertToDecibels(changedTarget, targetLength);
    Mix(target, changedTarget, targetLength);
    return SumAbs(changedTarget, targetLength);
}

PeakingEQ DLL_EXPORT BruteForceQ(float *target, int targetLength, FilterAnalyzer *analyzer, double freq, double gain) {
    double q = analyzer->GetStartQ(), qStep = q * .5;
    gain = roundf(Clamp(-gain, -analyzer->GetMaxGain(), -analyzer->GetMinGain()) / analyzer->GetGainPrecision()) * analyzer->GetGainPrecision();
    float targetSum = SumAbs(target, targetLength);
    float* targetSource = (float*)malloc(targetLength * sizeof(float));
    memcpy(targetSource, target, targetLength * sizeof(float));
    PeakingFilter *newFilter = NULL;
    for (int i = 0; i < analyzer->GetIterations(); ++i) {
        double lowerQ = q - qStep, upperQ = q + qStep;
        newFilter = (PeakingFilter*)malloc(sizeof(PeakingFilter));
        new(newFilter) PeakingFilter(analyzer->GetSampleRate(), freq, lowerQ, gain);
        analyzer->Reset(newFilter, analyzer->GetSampleRate());

        float *lowerTarget, lowerSum = BruteForceStepInternal(targetSource, targetLength, lowerTarget, analyzer);
        if (targetSum > lowerSum) {
            targetSum = lowerSum;
            memcpy(target, lowerTarget, targetLength * sizeof(float));
            q = lowerQ;
        }
        free(lowerTarget);

        newFilter = (PeakingFilter*)malloc(sizeof(PeakingFilter));
        new(newFilter) PeakingFilter(analyzer->GetSampleRate(), freq, upperQ, gain);
        analyzer->Reset(newFilter, analyzer->GetSampleRate());
        float *upperTarget, upperSum = BruteForceStepInternal(targetSource, targetLength, upperTarget, analyzer);
        if (targetSum > upperSum) {
            targetSum = upperSum;
            memcpy(target, upperTarget, targetLength * sizeof(float));
            q = upperQ;
        }
        free(upperTarget);
        qStep *= .5;
    }
    analyzer->ClearFilter();
    free(targetSource);
    return PeakingEQ { freq, q, -gain };
}

PeakingEQ DLL_EXPORT BruteForceBand(float *target, int targetLength, FilterAnalyzer *analyzer, int startPos, int stopPos) {
    double powRange = log10(analyzer->GetSampleRate() * .5) - LOG10_20;
    float max = fabsf(target[startPos]), abs;
    int maxAt = startPos;
    for (int i = startPos + 1; i < stopPos; ++i) {
        abs = fabsf(target[i]);
        if (max < abs) {
            max = abs;
            maxAt = i;
        }
    }
    return BruteForceQ(target, targetLength, analyzer, pow(10, LOG10_20 + powRange * maxAt / targetLength), target[maxAt]);
}
