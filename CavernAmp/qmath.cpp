#include <math.h>

#include "qmath.h"

double Clamp(double value, double min, double max) {
    if (value < min)
        return min;
    if (value > max)
        return max;
    return value;
}

float SumAbs(float* array, int arrayLength) {
    float sum = 0;
    for (int i = 0; i < arrayLength; ++i)
        sum += fabsf(array[i]);
    return sum;
}
