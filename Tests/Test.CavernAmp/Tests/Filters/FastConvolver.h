#ifndef FASTCONVOLVER_TESTS_H
#define FASTCONVOLVER_TESTS_H

#include "../../Loaders/Filters/FastConvolver.h"

class FastConvolverTests {
public:
    FastConvolverTests();
    ~FastConvolverTests();

    // Load the DLL before running tests
    bool LoadLibrary(const wchar_t* dllPath);

    // Run all tests, returns true if all passed
    bool Run();

    // Get test results
    int GetPassed() const;
    int GetFailed() const;
    int GetTotal() const;

    // Individual tests (called via C-style wrappers)
    bool testFastConvolverMono();
    bool testFastConvolverStereo();
    bool testFastConvolverProcess();
    bool testFastConvolverGetLength();
    bool testMultipleCycles();
    bool testFastConvolverWithDelay();

private:
    FastConvolverLoader m_loader;
};

#endif // FASTCONVOLVER_TESTS_H
