@echo off
title GAURAV AIMBOT COLLIDER - Small EXE Build

cd /d "%~dp0"

echo ---------------------------------------
echo Publishing SMALL EXE (Below 10 MB)
echo ---------------------------------------

dotnet publish "GAURAV EXTERNAL\GAURAV EXTERNAL.csproj" ^
-c Release ^
-r win-x64 ^
--self-contained false ^
-o "D:\GAURAV EXTERNAL\GAURAV EXTERNAL\bin\Release\net9.0-windows\exe" ^
/p:PublishSingleFile=true ^
/p:DebugType=None ^
/p:DebugSymbols=false ^
/p:InvariantGlobalization=true

echo.
echo ---------------------------------------
echo DONE!
echo Output:
echo D:\GAURAV EXTERNAL\GAURAV EXTERNAL\bin\Release\net9.0-windows\exe
echo ---------------------------------------
pause
