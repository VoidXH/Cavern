#include <math.h>
#include <stdlib.h>

#include "complex.h"

#ifndef GRAPHUTILS_H
#define GRAPHUTILS_H

// Convert a response to logarithmically scaled cut frequency range.
float* ConvertToGraph(float* response, int responseLength, double startFreq, double endFreq, int sampleRate, int resultSize);
void ConvertToGraph(float* response, int responseLength, double startFreq, double endFreq, int sampleRate, float* result, int resultSize);
// Convert a response curve to decibel scale.
void ConvertToDecibels(float* curve, int curveLength, float minimum = -100);

#endif // GRAPHUTILS_H
