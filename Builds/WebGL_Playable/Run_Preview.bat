@echo off
title Caro XO Pro - WebGL Playable Preview Server
cd /d "%~dp0"
echo ======================================================================
echo   CARO XO PRO - WEBGL PLAYABLE PREVIEW SERVER
echo ======================================================================
echo   Starting Web Server at: http://localhost:8088 ...
echo ======================================================================

where node >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    node server.js
    goto end
)

powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0server.ps1"

:end
pause
