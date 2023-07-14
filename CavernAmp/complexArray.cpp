#include "complexArray.h"

void Convolve(Complex* source, Complex* other, int len) {
    Complex* end = source + len;
    while (source != end) {
        float oldReal = source->real;
        source->real = source->real * other->real - source->imaginary * other->imaginary;
        source->imaginary = oldReal * other->imaginary + source->imaginary * other->real;
        source++;
        other++;
    }
}
