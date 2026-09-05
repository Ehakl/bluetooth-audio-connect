@echo off
setlocal
cd /d "%~dp0"
if not exist bin mkdir bin
"%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /optimize+ /out:bin\BluetoothAudioConnect.exe src\Program.cs src\Native.cs
if errorlevel 1 exit /b 1
bin\BluetoothAudioConnect.exe --self-test
exit /b %errorlevel%
