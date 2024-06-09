@echo off  
setlocal enabledelayedexpansion  
  
:RESTART  
tasklist /FI "IMAGENAME eq BandicamPortable.exe" | find /C "BandicamPortable.exe" > temp.txt  
set /p num=<temp.txt  
del /F temp.txt  
echo %num%  
if "%num%"=="0" (  
    start "" /D "d:\Users\huangzt\Desktop\Bandicam\Bandicam-7.1.0.2151-x64-Portable\Bandicam" BandicamPortable.exe  
)  
ping -n 10 -w 2000 0.0.0.1 > nul  
goto RESTART