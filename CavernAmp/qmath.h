#ifndef QMATH_H
#define QMATH_H

#include "export.h"

// Clamp a double between limits.
double Clamp(double value, double min, double max);

// Compute the base 2 logarithm of a number faster than a generic Log function.
inline int log2(const int x) {
  int y;
  asm ("\tbsr %1, %0\n"
      : "=r"(y)
      : "r" (x));
  return y;
}

inline int log2Ceil(const int x) {
    int y = log2(x);
    return y + ((1 << y) != x);
}

// Sum absolute values of elements in an array.
float SumAbs(float* array, int arrayLength);

#ifdef __cplusplus
extern "C" {
#endif

/// Exports
// Multiply the values of both arrays together and add these multiples together.
float DLL_EXPORT MultiplyAndAdd_Sum(float* lhs, float* rhs, int count);
// Multiply the values of both arrays together to the corresponding element of the target.
void DLL_EXPORT MultiplyAndAdd_PPP(float* lhs, float* rhs, float* target, int count);
// Multiply the values of an array with a constant to the corresponding element of the target.
void DLL_EXPORT MultiplyAndAdd_PFP(float* lhs, float rhs, float* target, int count);
// Do MultiplyAndAdd(float*, float*, float*, int) simultaneously for two different pairs of arrays.
void DLL_EXPORT MultiplyAndAdd_PPPPP(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count);
// Do MultiplyAndAdd(float*, float, float*, int) simultaneously for two different arrays.
void DLL_EXPORT MultiplyAndAdd_PFPFP(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count);
// Clear the target, then do MultiplyAndAdd(float*, float*, float*, int).
void DLL_EXPORT MultiplyAndSet_PPP(float* lhs, float* rhs, float* target, int count);
// Clear the target, then do MultiplyAndAdd(float*, float*, float*, float*, float*, int).
void DLL_EXPORT MultiplyAndSet_PPPPP(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count);
// Clear the target, then do MultiplyAndAdd(float*, float, float*, float, float*, int).
void DLL_EXPORT MultiplyAndSet_PFPFP(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count);

#ifdef __cplusplus
}
#endif

#endif // QMATH_H
