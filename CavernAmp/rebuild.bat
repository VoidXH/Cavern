@echo off
if exist obj\Release rmdir /s /q obj\Release
if exist bin\Release rmdir /s /q bin\Release
call build.bat

