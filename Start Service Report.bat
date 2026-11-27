@echo off
title MQuest Service Report
cd /d "%~dp0"

if not exist "%~dp0index.html" (
  echo index.html was not found next to this file.
  echo Keep "Start Service Report.bat" in the same folder as index.html, logo.jpeg, customers.js and server.ps1.
  pause
  exit /b 1
)

rem Remove the "downloaded from internet" mark from the app files so Windows does not block them.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -LiteralPath '%~dp0' -File | Unblock-File" >nul 2>&1

rem Start the local server (opens the report in the browser). Close this window to stop.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0server.ps1"

if errorlevel 1 pause
