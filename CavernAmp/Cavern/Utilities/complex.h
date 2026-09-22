#ifndef COMPLEX_H
#define COMPLEX_H

#include <math.h>

struct Complex {
    float real;
    float imaginary;

    Complex() : real(0), imaginary(0) {}
    Complex(float real) : real(real), imaginary(0) {}
    Complex(float real, float imaginary) : real(real), imaginary(imaginary) {}

    float getMagnitude() {
        return sqrtf(real * real + imaginary * imaginary);
    }

    /// Complex addition.
    Complex operator +(const Complex& rhs) const {
        return Complex(real + rhs.real, imaginary + rhs.imaginary);
    }

    /// Complex multiplication.
    Complex operator *(const Complex& rhs) const {
        return Complex(real * rhs.real - imaginary * rhs.imaginary,
            real * rhs.imaginary + imaginary * rhs.real);
    }

    /// Scalar complex multiplication.
    Complex operator *(float rhs) const {
        return Complex(real * rhs, imaginary * rhs);
    }

    /// Complex division.
    Complex operator /(const Complex& rhs) const {
        float multiplier = 1.0f / (rhs.real * rhs.real + rhs.imaginary * rhs.imaginary);
        return Complex((real * rhs.real + imaginary * rhs.imaginary) * multiplier,
            (imaginary * rhs.real - real * rhs.imaginary) * multiplier);
    }

    /// Get the complex number with a magnitude of 1 and a desired phase.
    static Complex UnitPhase(float phase) {
        return Complex(cosf(phase), sinf(phase));
    }
};

#endif // COMPLEX_H
