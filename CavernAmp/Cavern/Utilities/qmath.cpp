#include <math.h>

#include "qmath.h"

double Clamp(double value, double min, double max) {
    if (value < min) {
        return min;
    }
    if (value > max) {
        return max;
    }
    return value;
}

int Log2Int(const int x) {
    int y;
    asm ("\tbsr %1, %0\n"
        : "=r"(y)
        : "r" (x));
    return y;
}

int Log2Ceil(const int x) {
    int y = Log2Int(x);
    return y + ((1 << y) != x);
}

float SumAbs(float* array, int arrayLength) {
    float sum = 0;
    for (int i = 0; i < arrayLength; i++) {
        sum += fabsf(array[i]);
    }
    return sum;
}

double DbToGain(double gain) {
    return pow(10.0, gain * 0.05);
}

float DbToGainF(float gain) {
    return powf(10.0f, gain * 0.05f);
}

double GainToDb(double gain) {
    return 20.0 * log10(gain);
}

float GainToDbF(float gain) {
    return 20.0f * log10f(gain);
}
