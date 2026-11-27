# Builds ..\app.exe using the C# compiler that ships with Windows (.NET Framework 4).
# Run again after changing index.html, logo.jpeg or customers.js so the built-in copies update.

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$csc  = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe" }

# Make app.ico from the MQ logo (square, 256px PNG inside an .ico).
Add-Type -AssemblyName System.Drawing
$ico = Join-Path $here "app.ico"
$src = [System.Drawing.Image]::FromFile((Join-Path $root "logo.jpeg"))
$bmp = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = "HighQualityBicubic"; $g.Clear([System.Drawing.Color]::White)
# Crop to the "MQ" letters (top part of the logo, without the tagline).
$crop = New-Object System.Drawing.Rectangle 0, 0, $src.Width, ([int]($src.Height * 0.78))
$scale = [Math]::Min(240 / $crop.Width, 240 / $crop.Height)
$w = [int]($crop.Width * $scale); $h = [int]($crop.Height * $scale)
$dest = New-Object System.Drawing.Rectangle ([int]((256 - $w) / 2)), ([int]((256 - $h) / 2)), $w, $h
$g.DrawImage($src, $dest, $crop, [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose(); $src.Dispose()
$png = New-Object System.IO.MemoryStream
$bmp.Save($png, [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
$pngBytes = $png.ToArray()
$fs = [System.IO.File]::Create($ico); $bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]1)          # header: 1 image
$bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([byte]0); $bw.Write([byte]0)  # 256x256
$bw.Write([uint16]1); $bw.Write([uint16]32); $bw.Write([uint32]$pngBytes.Length); $bw.Write([uint32]22)
$bw.Write($pngBytes); $bw.Close()

$out = Join-Path $root "app.exe"
& $csc /nologo /target:winexe /optimize+ "/out:$out" "/win32icon:$ico" `
  /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  "/resource:$(Join-Path $root 'index.html'),index.html" `
  "/resource:$(Join-Path $root 'logo.jpeg'),logo.jpeg" `
  "/resource:$(Join-Path $root 'customers.js'),customers.js" `
  (Join-Path $here "App.cs")
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "Built $out" -ForegroundColor Green
