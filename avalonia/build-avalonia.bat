@echo off
cd /d "%~dp0"
taskkill /F /IM RBWStack.exe >nul 2>&1
dotnet publish RBWStack.Avalonia.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
if %ERRORLEVEL% EQU 0 (
    copy /Y "publish\RBWStack.exe" "..\RBWStack.exe" >nul
    echo.
    echo Build xong! File chay tai: D:\RBWStack\RBWStack.exe
) else (
    echo Build that bai!
)
pause
