#include "qmath.h"

float MultiplyAndAdd_Sum(float* lhs, float* rhs, int count) {
    float sum = 0;
    while (count--) {
        sum += *lhs++ * *rhs++;
    }
    return sum;
}

void MultiplyAndAdd_PPP(float* lhs, float* rhs, float* target, int count) {
    while (count--) {
        *target++ += *lhs++ * *rhs++;
    }
}

void MultiplyAndAdd_PFP(float* lhs, float rhs, float* target, int count) {
    while (count--) {
        *target++ += *lhs++ * rhs;
    }
}

void MultiplyAndAdd_PPPPP(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count) {
    while (count--) {
        *target++ += *lhs1++ * *rhs1++ + *lhs2++ * *rhs2++;
    }
}

void MultiplyAndAdd_PFPFP(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count) {
    while (count--) {
        *target++ += *lhs1++ * rhs1 + *lhs2++ * rhs2;
    }
}

void MultiplyAndSet_PPP(float* lhs, float* rhs, float* target, int count) {
    while (count--) {
        *target++ = *lhs++ * *rhs++;
    }
}

void MultiplyAndSet_PPPPP(float* lhs1, float* rhs1, float* lhs2, float* rhs2, float* target, int count) {
    while (count--) {
        *target++ = *lhs1++ * *rhs1++ + *lhs2++ * *rhs2++;
    }
}

void MultiplyAndSet_PFPFP(float* lhs1, float rhs1, float* lhs2, float rhs2, float* target, int count) {
    while (count--) {
        *target++ = *lhs1++ * rhs1 + *lhs2++ * rhs2;
    }
}
