#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ---- Build (tichy, vypise jen errory) ----
if ! dotnet build "$SCRIPT_DIR/PLC_Project/PLC_Project.sln" -v q -nologo > /dev/null; then
    echo ""
    echo "=== BUILD FAILED ==="
    dotnet build "$SCRIPT_DIR/PLC_Project/PLC_Project.sln" -v q -nologo
    exit 1
fi

# ---- Spust EXE/DLL s predanymi argumenty ----
# Na Linuxu nemusi byt .exe, pouzijeme dotnet rovnou
EXE="$SCRIPT_DIR/PLC_Project/PLC_Project/bin/Debug/net6.0/PLC_Project"
if [ -f "${EXE}.exe" ]; then
    "${EXE}.exe" "$@"
elif [ -f "${EXE}.dll" ]; then
    dotnet "${EXE}.dll" "$@"
else
    echo "Executable not found: $EXE"
    exit 1
fi
