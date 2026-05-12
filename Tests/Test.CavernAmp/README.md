# Test.CavernAmp

A minimal C++ test runner for the **CavernAmp** DLL. It loads the compiled `CavernAmp.dll` at runtime and executes a suite of unit tests.

## ⚠️ Disclaimer

> **This project is entirely AI-generated using [Poor Man's AI](https://github.com/VoidXH/Poor-Man-s-AI).**
> May contain errors, incomplete implementations, or missing edge-case handling.
> Static analyzer findings and critical issues were addressed manually.
> Review all code thoroughly before use.

## Overview

This project provides a standalone test executable (`Test.CavernAmp.exe`) that:

1. Locates and dynamically loads `CavernAmp.dll` from `..\\..\\CavernAmp\\bin\\Release\\` relative to the executable.
2. Runs a suite of tests against the API.
3. Reports pass/fail counts with floating-point assertions (tolerance: `0.00001`).

## Project Structure

```
Test.CavernAmp/
├── main.cpp                    # Entry point — loads DLL, runs tests, prints results
├── test.h                      # Header-only minimal test framework (assertions, macros, counters)
├── test.bat                    # Build-and-run script (g++)
├── Test.CavernAmp.exe          # Compiled test runner
├── Loaders/
│   ├── DllLoader.h             # Dynamic DLL loading utilities
│   ├── DllLoader.cpp
│   └── ...                     # Per-class API function calls
└── Tests                       # Test fixtures for API classes
```

## Building & Running

### Prerequisites

- **g++** (GCC for Windows, e.g. MinGW-w64, MSYS2, or WSL)
- **CavernAmp.dll** compiled in Release mode at `../CavernAmp/bin/Release/CavernAmp.dll`

### Build & Run

From this directory, run the provided batch file:

```cmd
test.bat
```

This will:

1. Compile the test runner with `g++`.
2. Launch `Test.CavernAmp.exe` and display results.

## Test Framework (`test.h`)

A lightweight header-only unit testing library. Key features:

| Macro | Description |
|---|---|
| `ASSERT_TRUE(cond, desc)` | Asserts a boolean condition |
| `ASSERT_APPROX_EQUAL(a, b, desc)` | Asserts two `float` values within tolerance (`0.00001`) |
| `ASSERT_NOT_NULL(ptr, desc)` | Asserts a pointer is not `nullptr` |

### Writing a Test

```cpp
bool MyTest(void) {
    float expected = 1.0f;
    float actual = DoSomething();
    ASSERT_APPROX_EQUAL(actual, expected, "Result should equal 1.0");
    return true;  // if assertions pass
}

// In your test suite:
runTest("My Test", MyTest);
```

### Global State

- `g_testPassed` — number of passed tests
- `g_testFailed` — number of failed tests
- `DELTA` — floating-point comparison tolerance (`0.00001f`)

## How It Works

1. **DLL Discovery** — `main.cpp` uses `GetModuleFileNameW` to resolve the executable directory, then constructs a path to `CavernAmp.dll` relative to itself.
2. **Dynamic Loading** — `DllLoader.cpp/h` loads the DLL via `LoadLibraryW` and resolves symbols via `GetProcAddress`.
3. **Test Execution** — Each test is a `bool`-returning function registered via `runTest()`. The test runner iterates through all registered tests and tallies results.
4. **Results** — A summary is printed to `stdout` with pass/fail/total counts.

## Configuration

- **DLL Path** — Hardcoded in `main.cpp` (line 20). Adjust if your DLL is located elsewhere:
  ```cpp
  swprintf_s(dllPath, L"%s..\\..\\CavernAmp\\bin\\Release\\CavernAmp.dll", dir);
  ```
- **Tolerance** — Change `DELTA` in `test.h` (currently `0.00001f`) to tighten or loosen floating-point comparisons.

## License

The Cavern licence from the project root applies.
