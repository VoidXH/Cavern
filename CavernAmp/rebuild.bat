@echo off
setlocal enabledelayedexpansion

echo === Building CavernAmp ===

set OUTPUT_DIR=bin/Release
set OBJ_DIR=obj/Release
set CFLAGS=-march=corei7-avx -fexpensive-optimizations -O2 -pedantic -Wextra -Wall -m64 -DBUILD_DLL

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%OBJ_DIR%" mkdir "%OBJ_DIR%"

echo === Compiling sources ===

for /R %%F in (*.cpp) do (
    set "OBJ=%OBJ_DIR%\%%~nF.o"
    echo   Compiling %%F ...
    g++.exe -c "%%F" -o "!OBJ!" %CFLAGS%
    if errorlevel 1 (echo ERROR: build failed. & exit /b 1)
)

for /R %%F in (*.c) do (
    set "OBJ=%OBJ_DIR%\%%~nF.o"
    echo   Compiling %%F ...
    gcc.exe -c "%%F" -o "!OBJ!" %CFLAGS%
    if errorlevel 1 (echo ERROR: build failed. & exit /b 1)
)

echo === Linking DLL ===

set "OBJ_FILES="
for %%O in ("%OBJ_DIR%\*.o") do (
    set "OBJ_FILES=!OBJ_FILES! "%%O""
)

g++.exe -shared -o "%OUTPUT_DIR%/CavernAmp.dll" %OBJ_FILES% -Wl,--output-def,"%OUTPUT_DIR%/libCavernAmp.def" -s -static-libstdc++ -static-libgcc -static -m64 -luser32

if errorlevel 1 (echo ERROR: build failed. & exit /b 1)

echo === CavernAmp build complete ===
exit /b 0
