#include "graphUtils.h"

float* ConvertToGraph(float* response, int responseLength, double startFreq, double endFreq, int sampleRate, int resultSize) {
    float* graph = (float*)malloc(resultSize * sizeof(float));
    double step = pow(10, (log10(endFreq) - log10(startFreq)) / (resultSize - 1)), positioner = responseLength * 2 / (double)sampleRate;
    for (int i = 0; i < resultSize; ++i) {
        graph[i] = response[(int)(startFreq * positioner)];
        startFreq *= step;
    }
    return graph;
}

void ConvertToGraph(float* response, int responseLength, double startFreq, double endFreq, int sampleRate, float* result, int resultSize) {
    double step = pow(10, (log10(endFreq) - log10(startFreq)) / (resultSize - 1)), positioner = responseLength * 2 / (double)sampleRate;
    for (int i = 0; i < resultSize; ++i) {
        result[i] = response[(int)(startFreq * positioner)];
        startFreq *= step;
    }
}

void ConvertToDecibels(float* curve, int curveLength, float minimum) {
    for (int i = 0; i < curveLength; ++i) {
        curve[i] = 20 * log10f(curve[i]);
        if (curve[i] < minimum) // this is also true if curve[i] == 0
            curve[i] = minimum;
    }
}
