@echo off
cd /d "%~dp0"
powershell -NoProfile -Command "Get-ChildItem -LiteralPath . -Recurse -File | Unblock-File"
dotnet clean
dotnet build
if errorlevel 1 goto end
dotnet run --no-build
:end
pause
