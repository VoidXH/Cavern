#include <windows.h>
#include <cstdio>
#include "test.h"
#include "Tests/Filters/FastConvolver.h"

int main() {
    // Load DLL from same directory as executable
    // Compute the absolute path so it works regardless of CWD.
    wchar_t exePath[MAX_PATH];
    GetModuleFileNameW(nullptr, exePath, MAX_PATH);

    wchar_t dir[MAX_PATH];
    wcsncpy_s(dir, exePath, _TRUNCATE);
    size_t lastSlash = 0;
    for (size_t i = 0; exePath[i]; ++i) {
        if (exePath[i] == L'\\' || exePath[i] == L'/') lastSlash = i + 1;
    }
    dir[lastSlash] = L'\0';
    wchar_t dllPath[MAX_PATH];
    swprintf_s(dllPath, L"%s..\\..\\CavernAmp\\bin\\Release\\CavernAmp.dll", dir);

    // Run tests
    FastConvolverTests tests;
    if (!tests.LoadLibrary(dllPath)) {
        fprintf(stderr, "Failed to load CavernAmp.dll from: %ls\n", dllPath);
        return 1;
    }

    bool allPassed = tests.Run();

    // Results
    printf("\n=== Results ===\n");
    printf("Passed: %d\n", tests.GetPassed());
    printf("Failed: %d\n", tests.GetFailed());
    printf("Total:  %d\n", tests.GetTotal());

    if (allPassed) {
        printf("\nAll tests PASSED.\n");
        return 0;
    } else {
        printf("\nSome tests FAILED.\n");
        return 1;
    }
}
