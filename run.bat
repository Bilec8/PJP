@echo off
setlocal

REM ---- Build (tichy, vypise jen errory) ----
dotnet build "%~dp0PLC_Project\PLC_Project.sln" -v q -nologo > nul
if errorlevel 1 (
    echo.
    echo === BUILD FAILED ===
    dotnet build "%~dp0PLC_Project\PLC_Project.sln" -v q -nologo
    exit /b 1
)

REM ---- Spust EXE s predanymi argumenty ----
"%~dp0PLC_Project\PLC_Project\bin\Debug\net6.0\PLC_Project.exe" %*
