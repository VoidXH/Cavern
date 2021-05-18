#ifndef QMATH_H
#define QMATH_H

inline int log2(const int x) {
  int y;
  asm ("\tbsr %1, %0\n"
      : "=r"(y)
      : "r" (x));
  return y;
}

#endif // QMATH_H
