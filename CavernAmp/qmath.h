#ifndef QMATH_H
#define QMATH_H

// Compute the base 2 logarithm of a number faster than a generic Log function.
inline int log2(const int x) {
  int y;
  asm ("\tbsr %1, %0\n"
      : "=r"(y)
      : "r" (x));
  return y;
}

// Sum absolute values of elements in an array.
float SumAbs(float* array, int arrayLength);

#endif // QMATH_H
