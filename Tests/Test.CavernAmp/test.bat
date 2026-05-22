@echo off
setlocal enabledelayedexpansion

echo === Building Test.CavernAmp ===

g++.exe -o Test.CavernAmp.exe ^
    Loaders/DllLoader.cpp ^
    Loaders/Filters/FastConvolver.cpp ^
    Tests/Filters/FastConvolver.cpp ^
    main.cpp ^
    -std=c++17 -O2 -static-libgcc -static-libstdc++ -static -m64 -lpsapi

if errorlevel 1 (
    echo ERROR: build failed.
    exit /b 1
)

echo === Running tests ===
Test.CavernAmp.exe
exit /b !errorlevel!
