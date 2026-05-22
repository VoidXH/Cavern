@echo off
setlocal enabledelayedexpansion

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

set NEEDS_LINK=0
for %%F in (%SOURCES%) do (
    set "OBJ=%OBJ_DIR%\%%~nF.o"
    
    set REBUILD=0
    if not exist "!OBJ!" (
        set REBUILD=1
    ) else (
        powershell -Command "$objTime = (Get-Item '!OBJ!').LastWriteTime; if ((Get-Item '%%F').LastWriteTime -gt $objTime -or (Get-ChildItem -Filter *.h* | Where-Object {$_.LastWriteTime -gt $objTime})) { exit 1 } else { exit 0 }" >nul 2>&1
        if errorlevel 1 set REBUILD=1
    )

    if "!REBUILD!"=="1" (
        echo   Compiling %%F ...
        set NEEDS_LINK=1
        if /I "%%~xF"==".c" (
            gcc.exe -c %%F -o "!OBJ!" %CFLAGS%
        ) else (
            g++.exe -c %%F -o "!OBJ!" %CFLAGS%
        )
        if errorlevel 1 (echo ERROR: build failed. & exit /b 1)
    )
)

echo === Linking DLL ===

if not exist "%OUTPUT_DIR%\CavernAmp.dll" set NEEDS_LINK=1
if "!NEEDS_LINK!"=="1" (
    g++.exe -shared -o "%OUTPUT_DIR%/CavernAmp.dll" "%OBJ_DIR%/*.o" -Wl,--output-def,"%OUTPUT_DIR%/libCavernAmp.def" -s -static-libstdc++ -static-libgcc -static -m64 -luser32
) else (
    echo   No changes detected. Nothing to link.
)

if errorlevel 1 (echo ERROR: build failed. & exit /b 1)

echo === CavernAmp build complete ===
exit /b 0
