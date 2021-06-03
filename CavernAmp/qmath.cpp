#include <math.h>

#include "qmath.h"

float SumAbs(float* array, int arrayLength) {
    float sum = 0;
    for (int i = 0; i < arrayLength; ++i)
        sum += fabsf(array[i]);
    return sum;
}
