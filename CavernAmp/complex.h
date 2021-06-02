#ifndef COMPLEX_H
#define COMPLEX_H

#include <math.h>

struct Complex {
    float real;
    float imaginary;

    float getMagnitude() {
        return sqrtf(real * real + imaginary * imaginary);
    }
};

#endif // COMPLEX_H
