# Gera src\ZeroDash\zerodash.ico a partir de assets\icons\zerodash.png (PNG dentro do ICO, tamanhos 16 a 256).
# Chamado automaticamente pelo build.ps1; pode ser rodado sozinho. Para trocar o ícone, basta substituir o PNG.
param(
    [string]$Png = (Join-Path (Split-Path $PSScriptRoot -Parent) "assets\icons\zerodash.png"),
    [string]$Ico = (Join-Path (Split-Path $PSScriptRoot -Parent) "src\ZeroDash\zerodash.ico")
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Png)) { throw "Ícone não encontrado: $Png" }
Add-Type -AssemblyName System.Drawing
$src = [System.Drawing.Image]::FromFile((Resolve-Path $Png).Path)
try {
    $max = [Math]::Max($src.Width, $src.Height)
    $sizes = @(16, 24, 32, 48, 64, 128, 256) | Where-Object { $_ -le $max }
    if (-not $sizes) { $sizes = @(16) }
    $imgs = @(foreach ($s in $sizes) {
        $bmp = [System.Drawing.Bitmap]::new($s, $s, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = 'HighQualityBicubic'; $g.SmoothingMode = 'HighQuality'; $g.PixelOffsetMode = 'HighQuality'
        $g.Clear([System.Drawing.Color]::Transparent)
        $k = [Math]::Min($s / $src.Width, $s / $src.Height)
        $w = [int][Math]::Round($src.Width * $k); $h = [int][Math]::Round($src.Height * $k)
        $g.DrawImage($src, [int](($s - $w) / 2), [int](($s - $h) / 2), $w, $h)
        $g.Dispose()
        $ms = [System.IO.MemoryStream]::new()
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $bmp.Dispose()
        [pscustomobject]@{ Size = $s; Data = $ms.ToArray() }
    })
} finally { $src.Dispose() }
$fs = [System.IO.File]::Create($Ico)
$bw = [System.IO.BinaryWriter]::new($fs)
$bw.Write([uint16]0); $bw.Write([uint16]1); $bw.Write([uint16]$imgs.Count)
$offset = 6 + 16 * $imgs.Count
foreach ($i in $imgs) {
    $b = if ($i.Size -ge 256) { 0 } else { $i.Size }
    $bw.Write([byte]$b); $bw.Write([byte]$b); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([uint16]1); $bw.Write([uint16]32)
    $bw.Write([uint32]$i.Data.Length); $bw.Write([uint32]$offset)
    $offset += $i.Data.Length
}
foreach ($i in $imgs) { $bw.Write([byte[]]$i.Data) }
$bw.Close(); $fs.Close()
Write-Host "Ícone gerado: $Ico ($($imgs.Count) tamanhos, de $($sizes[0]) a $($sizes[-1]) px)"
