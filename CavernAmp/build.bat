@echo off

echo === Building CavernAmp ===

set OUTPUT_DIR=bin/Release
set OBJ_DIR=obj/Release
set CFLAGS=-march=corei7-avx -fexpensive-optimizations -O2 -pedantic -Wextra -Wall -m64 -DBUILD_DLL

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%OBJ_DIR%" mkdir "%OBJ_DIR%"

echo === Compiling sources ===

set SOURCES=main.cpp
set SOURCES=%SOURCES% Cavern.QuickEQ\Equalization\peakingEqualizer.cpp
set SOURCES=%SOURCES% Cavern.QuickEQ\Utilities\filterAnalyzer.cpp
set SOURCES=%SOURCES% Cavern\Filters\fastConvolver.cpp
set SOURCES=%SOURCES% Cavern\Filters\peakingFilter.cpp
set SOURCES=%SOURCES% Cavern\Utilities\complexArray.cpp
set SOURCES=%SOURCES% Cavern\Utilities\fftcache.cpp
set SOURCES=%SOURCES% Cavern\Utilities\graphUtils.cpp
set SOURCES=%SOURCES% Cavern\Utilities\measurements.cpp
set SOURCES=%SOURCES% Cavern\Utilities\qmath.cpp
set SOURCES=%SOURCES% Cavern\Utilities\qmath_vector.c
set SOURCES=%SOURCES% Cavern\Utilities\waveformUtils.cpp

for %%F in (%SOURCES%) do (
    echo   Compiling %%F ...
    if "%%~xF"==".c" (
        gcc.exe -c %%F -o "%OBJ_DIR%/%%~nF.o" %CFLAGS%
    ) else (
        g++.exe -c %%F -o "%OBJ_DIR%/%%~nF.o" %CFLAGS%
    )
    if errorlevel 1 (echo ERROR: build failed. & exit /b 1)
)

echo === Linking DLL ===

g++.exe -shared -o "%OUTPUT_DIR%/CavernAmp.dll" "%OBJ_DIR%/*.o" -Wl,--output-def,"%OUTPUT_DIR%/libCavernAmp.def" -s -static-libstdc++ -static-libgcc -static -m64 -luser32

if errorlevel 1 (echo ERROR: build failed. & exit /b 1)

echo === CavernAmp build complete ===
exit /b 0
