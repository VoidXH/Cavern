#ifndef QMATH_H
#define QMATH_H

#include "../../export.h"

/// Clamp a double between limits.
double Clamp(double value, double min, double max);
/// Compute the base 2 logarithm of a number faster than a generic Log function.
int Log2Int(const int x);
/// Ceiling of the base 2 logarithm.
int Log2Ceil(const int x);
/// Sum absolute values of elements in an array.
float SumAbs(float* array, int arrayLength);
/// Convert decibels to voltage gain (double precision).
double DbToGain(double gain);
/// Convert decibels to voltage gain (single precision).
float DbToGainF(float gain);
/// Convert voltage gain to decibels (double precision).
double GainToDb(double gain);
/// Convert voltage gain to decibels (single precision).
float GainToDbF(float gain);

#ifdef __cplusplus
extern "C" {
#endif

/// Multiply the values of both arrays together and add these multiples together.
float DLL_EXPORT MultiplyAndAdd_Sum(float* lhs, float* rhs, int count);
/// Multiply the values of both arrays together to the corresponding element of the target.
void DLL_EXPORT MultiplyAndAdd_PPP(float* lhs, float* rhs, float* target, int count);
/// Multiply the values of an array with a constant to the corresponding element of the target.
void DLL_EXPORT MultiplyAndAdd_PFP(float* lhs, float rhs, float* target, int count);
/// Do MultiplyAndAdd(float*, float*, float*, int) simultaneously for two different pairs of arrays.
void DLL_EXPORT MultiplyAndAdd_PPPPP(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count);
/// Do MultiplyAndAdd(float*, float, float*, int) simultaneously for two different arrays.
void DLL_EXPORT MultiplyAndAdd_PFPFP(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count);
/// Clear the target, then do MultiplyAndAdd(float*, float*, float*, int).
void DLL_EXPORT MultiplyAndSet_PPP(float* lhs, float* rhs, float* target, int count);
/// Clear the target, then do MultiplyAndAdd(float*, float*, float*, float*, float*, int).
void DLL_EXPORT MultiplyAndSet_PPPPP(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count);
/// Clear the target, then do MultiplyAndAdd(float*, float, float*, float, float*, int).
void DLL_EXPORT MultiplyAndSet_PFPFP(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count);

#ifdef __cplusplus
}
#endif

#endif // QMATH_H
