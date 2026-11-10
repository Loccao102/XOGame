$port = 8088
$dir = $PSScriptRoot
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://127.0.0.1:$port/")

try {
    $listener.Start()
    Write-Host "======================================================================"
    Write-Host "  CARO XO PRO - WEBGL PREVIEW SERVER (POWERSHELL)"
    Write-Host "  URL: http://localhost:$port/index.html"
    Write-Host "  (Nhan Ctrl+C de dung server)"
    Write-Host "======================================================================"
    Start-Process "http://localhost:$port/index.html"

    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $request = $context.Request
        $response = $context.Response
        $relPath = $request.Url.LocalPath.TrimStart('/')
        if ([string]::IsNullOrEmpty($relPath) -or $relPath -eq "index.htm") {
            $relPath = "index.html"
        }
        $fullPath = Join-Path $dir $relPath

        if (Test-Path $fullPath -PathType Leaf) {
            $bytes = [System.IO.File]::ReadAllBytes($fullPath)
            if ($fullPath.EndsWith(".wasm.unityweb")) {
                $response.ContentType = "application/wasm"
                $response.AddHeader("Content-Encoding", "gzip")
            } elseif ($fullPath.EndsWith(".data.unityweb")) {
                $response.ContentType = "application/octet-stream"
                $response.AddHeader("Content-Encoding", "gzip")
            } elseif ($fullPath.EndsWith(".framework.js.unityweb")) {
                $response.ContentType = "application/javascript"
                $response.AddHeader("Content-Encoding", "gzip")
            } elseif ($fullPath.EndsWith(".wasm")) {
                $response.ContentType = "application/wasm"
            } elseif ($fullPath.EndsWith(".js")) {
                $response.ContentType = "application/javascript"
            } elseif ($fullPath.EndsWith(".html") -or $fullPath.EndsWith(".htm")) {
                $response.ContentType = "text/html; charset=utf-8"
            } elseif ($fullPath.EndsWith(".css")) {
                $response.ContentType = "text/css"
            } else {
                $response.ContentType = "application/octet-stream"
            }

            $response.AddHeader("Access-Control-Allow-Origin", "*")
            $response.ContentLength64 = $bytes.Length
            $response.OutputStream.Write($bytes, 0, $bytes.Length)
        } else {
            $response.StatusCode = 404
        }
        $response.OutputStream.Close()
    }
} catch {
    Write-Host "Loi khoi tao server: $($_.Exception.Message)"
}

