@echo off
setlocal enabledelayedexpansion

echo === Building CavernAmp ===

set OUTPUT_DIR=bin\Release
set OBJ_DIR=obj\Release
set CFLAGS=-march=corei7-avx -fexpensive-optimizations -O2 -pedantic -Wextra -Wall -m64 -DBUILD_DLL

if not exist "%OUTPUT_DIR%" mkdir "%OUTPUT_DIR%"
if not exist "%OBJ_DIR%" mkdir "%OBJ_DIR%"

set NEEDS_LINK=0

echo === Compiling sources ===

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
    if errorlevel 1 (echo ERROR: linking failed. & exit /b 1)
) else (
    echo    No changes detected. Nothing to link.
)

echo === CavernAmp build complete ===
exit /b 0

:CompileFile
setlocal enabledelayedexpansion
set "SRC_FILE=%~1"
set "COMPILER=%~2"

rem Generate unique object filename from relative path
set "REL_PATH=%SRC_FILE%"
set "REL_PATH=!REL_PATH:%CD%\=!"
set "REL_PATH=!REL_PATH:\=_!"
set "REL_PATH=!REL_PATH:/=_!"
set "OBJ=%OBJ_DIR%\!REL_PATH!.o"

set REBUILD=0

if not exist "!OBJ!" (
    set REBUILD=1
) else (
    rem Check if source file is newer than object file
    xcopy "!SRC_FILE!" "!OBJ!" /d /l /y 2>nul | find "1 File(s)" >nul 2>&1
    if not errorlevel 1 (
        set REBUILD=1
    ) else (
        rem Check if any header file is newer than object file
        for /R %%H in (*.h *.hpp *.hxx) do (
            if "!REBUILD!"=="0" (
                xcopy "%%H" "!OBJ!" /d /l /y 2>nul | find "1 File(s)" >nul 2>&1
                if not errorlevel 1 set REBUILD=1
            )
        )
    )
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
