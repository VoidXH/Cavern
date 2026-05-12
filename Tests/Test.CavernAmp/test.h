#ifndef TEST_H
#define TEST_H

#include <cstdio>
#include <cmath>
#include <vector>

// ============================================================
// Test Framework - A minimal header-only testing library
//
// Usage:
//   1. #include "test.h" in your test file
//   2. Write test functions returning bool (true = pass)
//   3. Call runTest(name, fn) for each test
//   4. Check g_testPassed / g_testFailed after all tests
// ============================================================

// --- Global test counters (inline = single definition across TUs) ---
inline int g_testPassed = 0;
inline int g_testFailed = 0;

// --- Floating-point comparison tolerance ---
// Matches C# Constants.delta. Increased from 1e-6 to 1e-5 to account for
// accumulated floating-point errors in FFT-based convolution.
inline constexpr float DELTA = 0.00001f;

// --- Assertion helpers ---
inline bool assertEqual(bool cond, const char* file, int line, const char* desc) {
    if (!cond) {
        fprintf(stderr, "\n  ASSERTION FAILED at %s:%d\n", file, line);
        fprintf(stderr, "    %s\n", desc);
        return false;
    }
    return true;
}

inline bool assertApproxEqual(float a, float b, const char* file, int line, const char* desc) {
    float diff = std::fabsf(a - b);
    if (diff <= DELTA) return true;
    fprintf(stderr, "\n  ASSERTION FAILED at %s:%d\n", file, line);
    fprintf(stderr, "    %s\n", desc);
    fprintf(stderr, "    expected: %f, got: %f (diff: %e)\n", a, b, diff);
    return false;
}

// --- Test macros ---
#define ASSERT_TRUE(cond, desc) do { \
    if (!assertEqual(!!(cond), __FILE__, __LINE__, desc)) { \
        return false; \
    } \
} while(0)

#define ASSERT_APPROX_EQUAL(a, b, desc) do { \
    if (!assertApproxEqual((a), (b), __FILE__, __LINE__, desc)) { \
        return false; \
    } \
} while(0)

#define ASSERT_NOT_NULL(ptr, desc) do { \
    if (!(ptr)) { \
        fprintf(stderr, "\n  ASSERT_NOT_NULL FAILED at %s:%d\n", __FILE__, __LINE__); \
        fprintf(stderr, "    %s\n", desc); \
        return false; \
    } \
} while(0)

// --- Test runner ---
inline bool runTest(const char* name, bool (*fn)(void)) {
    printf("  %s ... ", name);
    if (fn()) {
        printf("PASS\n");
        ++g_testPassed;
        return true;
    } else {
        printf("FAIL\n");
        ++g_testFailed;
        return false;
    }
}

// --- Array comparison ---
inline bool arraysEqual(const float* a, const float* b, int len) {
    for (int i = 0; i < len; ++i) {
        if (std::fabsf(a[i] - b[i]) > DELTA) return false;
    }
    return true;
}

#endif // TEST_H
