@echo off
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /out:RBWStack.exe /win32icon:app.ico /r:System.IO.Compression.FileSystem.dll /r:System.IO.Compression.dll src\*.cs
echo Build complete!
pause
