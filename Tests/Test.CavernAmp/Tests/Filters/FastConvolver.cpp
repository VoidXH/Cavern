#include "FastConvolver.h"
#include "../../test.h"
#include <cstdio>
#include <cstring>

// Global pointer to the current test instance (for C-style wrapper functions)
static FastConvolverTests* g_currentTests = nullptr;

// --- C-style wrapper functions ---
// These bridge between runTest(function pointer) and our member methods.
static bool staticTest_FastConvolverMono() {
    return g_currentTests ? g_currentTests->testFastConvolverMono() : false;
}
static bool staticTest_FastConvolverStereo() {
    return g_currentTests ? g_currentTests->testFastConvolverStereo() : false;
}
static bool staticTest_FastConvolverProcess() {
    return g_currentTests ? g_currentTests->testFastConvolverProcess() : false;
}
static bool staticTest_FastConvolverGetLength() {
    return g_currentTests ? g_currentTests->testFastConvolverGetLength() : false;
}
static bool staticTest_MultipleCycles() {
    return g_currentTests ? g_currentTests->testMultipleCycles() : false;
}
static bool staticTest_FastConvolverWithDelay() {
    return g_currentTests ? g_currentTests->testFastConvolverWithDelay() : false;
}

FastConvolverTests::FastConvolverTests() {}
FastConvolverTests::~FastConvolverTests() {}

bool FastConvolverTests::LoadLibrary(const wchar_t* dllPath) {
    return m_loader.Load(dllPath);
}

bool FastConvolverTests::Run() {
    printf("Tests:\n");

    g_currentTests = this;
    runTest("FastConvolverMono",      staticTest_FastConvolverMono);
    runTest("FastConvolverStereo",    staticTest_FastConvolverStereo);
    runTest("FastConvolverProcess",   staticTest_FastConvolverProcess);
    runTest("FastConvolverGetLength", staticTest_FastConvolverGetLength);
    runTest("MultipleCycles",         staticTest_MultipleCycles);
    runTest("FastConvolverWithDelay", staticTest_FastConvolverWithDelay);
    g_currentTests = nullptr;

    return g_testFailed == 0;
}

int FastConvolverTests::GetPassed() const {
    return g_testPassed;
}

int FastConvolverTests::GetFailed() const {
    return g_testFailed;
}

int FastConvolverTests::GetTotal() const {
    return g_testPassed + g_testFailed;
}

// ============================================================
// Test 1: FastConvolverMono
//
// Meaning: Convolution of a Dirac delta with a signal returns the
// signal unchanged. We create a FastConvolver from 'step', then
// process 'dirac' through it.
// ============================================================
bool FastConvolverTests::testFastConvolverMono() {
    float dirac[]  = {1.0f, 0.0f, 0.0f, 0.0f};
    float step[]   = {1.0f, 0.75f, 0.5f, 2.0f};
    const int len = 4;

    void* conv = m_loader.Create(step, len, 0);
    if (!conv) return false;

    // Process the Dirac delta through the convolver
    m_loader.Process(conv, dirac, len, 0, 1);

    m_loader.Dispose(conv);

    // After convolution with Dirac delta, the output should equal the step filter
    for (int i = 0; i < len; ++i) {
        char desc[256];
        snprintf(desc, sizeof(desc), "dirac[%d]: expected %f", i, step[i]);
        ASSERT_APPROX_EQUAL(step[i], dirac[i], desc);
    }
    return true;
}

// ============================================================
// Test 2: FastConvolverStereo
//
// Meaning: Stereo (interlaced) processing extracts channel 0 correctly.
// Input is interlaced: [ch0, ch1, ch0, ch1, ...].
// After convolution on channel 0, channel 0 samples should equal the step filter.
// ============================================================
bool FastConvolverTests::testFastConvolverStereo() {
    // Interlaced stereo input: ch0=1, ch1=0.5, ch0=0, ch1=1, ch0=0, ch1=1, ch0=0, ch1=0.5
    float stereoInput[] = {1.0f, 0.5f, 0.0f, 1.0f, 0.0f, 1.0f, 0.0f, 0.5f};
    float step[]        = {1.0f, 0.75f, 0.5f, 2.0f};
    const int len       = 8;  // total samples (4 per channel)
    const int channels  = 2;
    const int stepLen   = 4;

    void* conv = m_loader.Create(step, stepLen, 0);
    if (!conv) return false;

    // Process stereo input on channel 0
    m_loader.Process(conv, stereoInput, len, 0, channels);

    m_loader.Dispose(conv);

    // Extract channel 0 output and verify it matches the step filter
    float left[4];
    for (int i = 0; i < stepLen; ++i) {
        left[i] = stereoInput[i * channels];
    }

    for (int i = 0; i < stepLen; ++i) {
        char desc[256];
        snprintf(desc, sizeof(desc), "stereo left[%d]: expected %f", i, step[i]);
        ASSERT_APPROX_EQUAL(step[i], left[i], desc);
    }
    return true;
}

// ============================================================
// Additional tests for thorough coverage
// ============================================================

// Test: Create, process, dispose - verify output is not corrupted
// Uses the same pattern as the original C# tests: Dirac delta convolution
// with a larger impulse so the internal block size matches the signal length.
bool FastConvolverTests::testFastConvolverProcess() {
    // Impulse with identity at position 0, sized so filterLength == 2*len
    // filterLength = 2 << Log2Ceil(len). For len=32, filterLength = 64, blockSize = 32.
    const int len   = 32;
    float impulse[32] = {0};
    impulse[0] = 1.0f;  // identity impulse (delta at position 0)

    float signal[32];
    for (int i = 0; i < len; ++i) {
        signal[i] = static_cast<float>(i + 1);
    }

    void* conv = m_loader.Create(impulse, len, 0);
    if (!conv) return false;

    // Process signal through identity impulse response
    m_loader.Process(conv, signal, len, 0, 1);

    m_loader.Dispose(conv);

    // With an identity impulse (delta at position 0), output should equal input
    for (int i = 0; i < len; ++i) {
        char desc[256];
        snprintf(desc, sizeof(desc), "signal[%d]: expected %f", i, static_cast<float>(i + 1));
        ASSERT_APPROX_EQUAL(static_cast<float>(i + 1), signal[i], desc);
    }

    return true;
}

// Test: Verify GetLength returns correct filter length
bool FastConvolverTests::testFastConvolverGetLength() {
    float impulse[] = {1.0f, 0.5f, 0.25f};
    const int len   = 3;

    void* conv = m_loader.Create(impulse, len, 0);
    if (!conv) return false;

    int actualLength = m_loader.GetLength(conv);

    // FastConvolver formula: filterLength = 2 << Log2Ceil(len)
    // len=3 -> Log2Ceil(3)=2 -> 2 << 2 = 8
    int expectedLength = 8;

    m_loader.Dispose(conv);

    if (actualLength != expectedLength) {
        fprintf(stderr, "\n  GetLength expected %d, got %d\n", expectedLength, actualLength);
        return false;
    }
    return true;
}

// Test: Multiple create/dispose cycles don't crash
bool FastConvolverTests::testMultipleCycles() {
    float impulse[] = {1.0f, 0.0f, 0.0f, 0.0f};
    float signal[]  = {1.0f, 2.0f, 3.0f, 4.0f};
    const int len   = 4;

    for (int cycle = 0; cycle < 10; ++cycle) {
        void* conv = m_loader.Create(impulse, len, 0);
        if (!conv) return false;

        m_loader.Process(conv, signal, len, 0, 1);
        m_loader.Dispose(conv);
    }
    return true;
}

// Test: Convolver with delay
bool FastConvolverTests::testFastConvolverWithDelay() {
    float impulse[] = {1.0f, 0.0f, 0.0f, 0.0f};
    float signal[]  = {1.0f, 0.0f, 0.0f, 0.0f};
    const int len   = 4;
    const int delay = 2;

    void* conv = m_loader.Create(impulse, len, delay);
    if (!conv) return false;

    m_loader.Process(conv, signal, len, 0, 1);
    m_loader.Dispose(conv);

    // Should not crash or corrupt memory
    return true;
}
