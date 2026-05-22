#include "graphUtils.h"

float* ConvertToGraph(float* response, int responseLength, double startFreq, double endFreq, int sampleRate, int resultSize) {
    float* graph = new float[resultSize];
    double step = pow(10, (log10(endFreq) - log10(startFreq)) / (resultSize - 1));
    startFreq *= responseLength * 2 / (double)sampleRate; // Positioning
    for (int i = 0; i < resultSize; i++) {
        if (startFreq >= responseLength) {
            startFreq = responseLength;
        }
        graph[i] = response[(int)startFreq];
        startFreq *= step;
    }
    return graph;
}

void ConvertToGraph(float* response, int responseLength, double startFreq, double endFreq, int sampleRate, float* result, int resultSize) {
    double step = pow(10, (log10(endFreq) - log10(startFreq)) / (resultSize - 1));
    startFreq *= responseLength * 2 / (double)sampleRate;
    for (int i = 0; i < resultSize; i++) {
        if (startFreq >= responseLength) {
            startFreq = responseLength;
        }
        result[i] = response[(int)startFreq];
        startFreq *= step;
    }
}

void ConvertToDecibels(float* curve, int curveLength, float minimum) {
    for (int i = 0; i < curveLength; i++) {
        curve[i] = 20 * log10f(curve[i]);
        if (curve[i] < minimum) { // this is also true if curve[i] == 0
            curve[i] = minimum;
        }
    }
}
