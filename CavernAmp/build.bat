@echo off
setlocal enabledelayedexpansion

echo === Building CavernAmp ===

set OUTPUT_DIR=bin/Release
set OBJ_DIR=obj/Release
set CFLAGS=-march=corei7-avx -fexpensive-optimizations -O2 -pedantic -Wextra -Wall -m64 -DBUILD_DLL

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%OBJ_DIR%" mkdir "%OBJ_DIR%"

echo === Compiling sources ===

set NEEDS_LINK=0

for /R %%F in (*.cpp) do (
    call :CompileFile "%%F" "g++.exe"
    if errorlevel 1 exit /b 1
)

for /R %%F in (*.c) do (
    call :CompileFile "%%F" "gcc.exe"
    if errorlevel 1 exit /b 1
)

echo === Linking DLL ===

if not exist "%OUTPUT_DIR%\CavernAmp.dll" set NEEDS_LINK=1

if "!NEEDS_LINK!"=="1" (
    set "OBJ_FILES="
    for %%O in ("%OBJ_DIR%\*.o") do (
        set "OBJ_FILES=!OBJ_FILES! "%%O""
    )
    
    g++.exe -shared -o "%OUTPUT_DIR%/CavernAmp.dll" !OBJ_FILES! -Wl,--output-def,"%OUTPUT_DIR%/libCavernAmp.def" -s -static-libstdc++ -static-libgcc -static -m64 -luser32
) else (
    echo    No changes detected. Nothing to link.
)

if errorlevel 1 (echo ERROR: build failed. & exit /b 1)

echo === CavernAmp build complete ===
exit /b 0

:CompileFile
setlocal enabledelayedexpansion
set "SRC_FILE=%~1"
set "COMPILER=%~2"
set "OBJ=%OBJ_DIR%\%~n1.o"

set REBUILD=0
if not exist "!OBJ!" (
    set REBUILD=1
) else (
    set "PS_SRC=!SRC_FILE:\=\\!"
    set "PS_OBJ=!OBJ:\=\\!"
    powershell -Command "$objTime = (Get-Item '!PS_OBJ!').LastWriteTime; if ((Get-Item '!PS_SRC!').LastWriteTime -gt $objTime -or (Get-ChildItem -Recurse -Include *.h,*.hpp,*.hxx | Where-Object {$_.LastWriteTime -gt $objTime})) { exit 1 } else { exit 0 }" >nul 2>&1
    if errorlevel 1 set REBUILD=1
)

set NEW_NEEDS_LINK=%NEEDS_LINK%
if "!REBUILD!"=="1" (
    echo    Compiling !SRC_FILE! ...
    set NEW_NEEDS_LINK=1
    %COMPILER% -c "!SRC_FILE!" -o "!OBJ!" %CFLAGS%
    if errorlevel 1 (
        echo ERROR: build failed on !SRC_FILE!.
        exit /b 1
    )
)

endlocal & set "NEEDS_LINK=%NEW_NEEDS_LINK%"
exit /b 0
