# Small web server for the MQuest Service Report app.
# Serves this folder so the computer AND phones/tablets on the same Wi-Fi can open the app.
# Nothing to install - uses built-in Windows PowerShell. Close this window to stop.

param([int]$Port = 8080, [switch]$NoBrowser)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$types = @{
  ".html" = "text/html; charset=utf-8"; ".js" = "application/javascript; charset=utf-8"
  ".css" = "text/css; charset=utf-8"; ".jpeg" = "image/jpeg"; ".jpg" = "image/jpeg"
  ".png" = "image/png"; ".ico" = "image/x-icon"; ".svg" = "image/svg+xml"
}

# Use the first free port from $Port upwards (in case another program already uses 8080).
$listener = $null
for ($p = $Port; $p -lt $Port + 20; $p++) {
  try {
    $l = New-Object System.Net.Sockets.TcpListener ([System.Net.IPAddress]::Any), $p
    $l.Start(); $listener = $l; $Port = $p; break
  } catch { }
}
if (-not $listener) {
  Write-Host "Could not start: ports $Port to $($Port + 19) are all busy." -ForegroundColor Red
  Read-Host "Press Enter to close"; exit 1
}

$ips = [System.Net.Dns]::GetHostAddresses([System.Net.Dns]::GetHostName()) |
  Where-Object { $_.AddressFamily -eq 'InterNetwork' -and -not $_.ToString().StartsWith('169.254') } |
  ForEach-Object { $_.ToString() }

Write-Host ""
Write-Host "  MQuest Service Report is running" -ForegroundColor Green
Write-Host ""
Write-Host "  On this computer:   http://localhost:$Port"
foreach ($ip in $ips) { Write-Host "  On phone / tablet:  http://${ip}:$Port   (same Wi-Fi)" -ForegroundColor Cyan }
Write-Host ""
Write-Host "  Keep this window open while using the app. Close it to stop."
Write-Host ""

if (-not $NoBrowser) { Start-Process "http://localhost:$Port" }

while ($true) {
  $client = $listener.AcceptTcpClient()
  try {
    $stream = $client.GetStream()
    $stream.ReadTimeout = 5000
    $reader = [System.IO.StreamReader]::new($stream)
    $requestLine = $reader.ReadLine()
    while (($line = $reader.ReadLine()) -ne $null -and $line -ne "") { }   # skip headers

    $path = "/"
    if ($requestLine -match '^[A-Z]+ ([^ ?#]+)') { $path = [Uri]::UnescapeDataString($Matches[1]) }
    if ($path -eq "/") { $path = "/index.html" }

    $file = [System.IO.Path]::GetFullPath((Join-Path $root $path.TrimStart('/')))
    if ($file.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path $file -PathType Leaf)) {
      $body = [System.IO.File]::ReadAllBytes($file)
      $ext = [System.IO.Path]::GetExtension($file).ToLower()
      $ct = if ($types.ContainsKey($ext)) { $types[$ext] } else { "application/octet-stream" }
      $status = "200 OK"
    } else {
      $body = [System.Text.Encoding]::UTF8.GetBytes("Not found")
      $ct = "text/plain"; $status = "404 Not Found"
    }
    $head = "HTTP/1.1 $status`r`nContent-Type: $ct`r`nContent-Length: $($body.Length)`r`nCache-Control: no-cache`r`nConnection: close`r`n`r`n"
    $hb = [System.Text.Encoding]::ASCII.GetBytes($head)
    $stream.Write($hb, 0, $hb.Length)
    $stream.Write($body, 0, $body.Length)
    $stream.Flush()
  } catch { } finally { $client.Close() }
}
