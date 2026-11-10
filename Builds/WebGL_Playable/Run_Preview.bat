@echo off
title Caro XO Pro - WebGL Playable Preview Server
cd /d "%~dp0"
echo ======================================================================
echo   CARO XO PRO - WEBGL PLAYABLE PREVIEW SERVER
echo ======================================================================
echo   Local Web URL: http://localhost:8088/index.html
echo   (Note: Must access http://localhost:8088 - not plain localhost!)
echo ======================================================================

where node >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    node server.js
) else (
    echo Starting PowerShell Web Server at http://localhost:8088 ...
    powershell -ExecutionPolicy Bypass -NoProfile -Command "$p=8088; $s=New-Object System.Net.HttpListener; $s.Prefixes.Add('http://127.0.0.1:'+$p+'/'); $s.Start(); Start-Process ('http://localhost:'+$p+'/index.html'); while($s.IsListening){$c=$s.GetContext(); $f=Join-Path '%~dp0' ($c.Request.Url.LocalPath.TrimStart('/') -replace '^$','index.html'); if(Test-Path $f -PathType Leaf){$b=[IO.File]::ReadAllBytes($f); if($f -like '*.wasm.unityweb'){$c.Response.ContentType='application/wasm'; $c.Response.AddHeader('Content-Encoding','gzip')}elseif($f -like '*.data.unityweb'){$c.Response.ContentType='application/octet-stream'; $c.Response.AddHeader('Content-Encoding','gzip')}elseif($f -like '*.framework.js.unityweb'){$c.Response.ContentType='application/javascript'; $c.Response.AddHeader('Content-Encoding','gzip')}; $c.Response.AddHeader('Access-Control-Allow-Origin','*'); $c.Response.ContentLength64=$b.Length; $c.Response.OutputStream.Write($b,0,$b.Length)}else{$c.Response.StatusCode=404}; $c.Response.OutputStream.Close()}"
)
pause
