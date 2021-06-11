#include <new>

#include "graphUtils.h"
#include "peakingEqualizer.h"
#include "peakingFilter.h"
#include "qmath.h"
#include "waveformUtils.h"

float BruteForceStep(float* target, int targetLength, float* &changedTarget, FilterAnalyzer* analyzer) {
    changedTarget = ConvertToGraph(analyzer->GetSpectrum(), targetLength, 20, analyzer->GetSampleRate() * .5,
                                   analyzer->GetSampleRate(), targetLength);
    ConvertToDecibels(changedTarget, targetLength);
    Mix(target, changedTarget, targetLength);
    return SumAbs(changedTarget, targetLength);
}

PeakingEQ BruteForceQ(float* target, int targetLength, FilterAnalyzer* analyzer, double freq, double gain) {
    double q = analyzer->GetStartQ(), qStep = q * .5;
    gain = roundf(Clamp(-gain, -analyzer->GetMaxGain(), -analyzer->GetMinGain()) / analyzer->GetGainPrecision()) * analyzer->GetGainPrecision();
    float targetSum = SumAbs(target, targetLength);
    float* targetSource = (float*)malloc(targetLength * sizeof(float));
    memcpy(targetSource, target, targetLength * sizeof(float));
    for (int i = 0; i < analyzer->GetIterations(); ++i) {
        double lowerQ = q - qStep, upperQ = q + qStep;
        PeakingFilter* newFilter = (PeakingFilter*)malloc(sizeof(PeakingFilter));
        new(newFilter) PeakingFilter(analyzer->GetSampleRate(), freq, lowerQ, gain);
        analyzer->Reset(newFilter, analyzer->GetSampleRate());

        float *lowerTarget, lowerSum = BruteForceStep(targetSource, targetLength, lowerTarget, analyzer);
        if (targetSum > lowerSum) {
            targetSum = lowerSum;
            memcpy(target, lowerTarget, targetLength * sizeof(float));
            q = lowerQ;
        }
        free(lowerTarget);

        new(newFilter) PeakingFilter(analyzer->GetSampleRate(), freq, upperQ, gain);
        float *upperTarget, upperSum = BruteForceStep(targetSource, targetLength, upperTarget, analyzer);
        if (targetSum > upperSum) {
            targetSum = upperSum;
            memcpy(target, upperTarget, targetLength * sizeof(float));
            q = upperQ;
        }
        free(upperTarget);
        free(newFilter);
        qStep *= .5;
    }
    free(targetSource);
    return PeakingEQ { freq, q, -gain };
}

PeakingEQ BruteForceBand(float* target, int targetLength, FilterAnalyzer* analyzer, int startFreq, int stopFreq) {
    double startPow = log10(20), powRange = log10(analyzer->GetSampleRate() * .5) - startPow;
    float max = fabsf(target[startFreq]), abs;
    int maxAt = startFreq;
    for (int i = startFreq + 1; i < stopFreq; ++i) {
        abs = fabsf(target[i]);
        if (max < abs) {
            max = abs;
            maxAt = i;
        }
    }
    return BruteForceQ(target, targetLength, analyzer, pow(10, startPow + powRange * maxAt / targetLength), target[maxAt]);
}
