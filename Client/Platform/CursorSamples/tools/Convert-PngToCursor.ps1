<#
.SYNOPSIS
Converts a PNG into a Windows .cur cursor file.

.DESCRIPTION
SysDVR can only change the mouse pointer while it is over its own window. To get the same
pointer everywhere else in Windows (desktop, overlays, other apps) the image has to become a
real cursor file and be set as the system pointer.

This script does the conversion. Applying it is a separate step, see Apply-WindowsCursor.ps1.

.PARAMETER Path
The source image. Any format System.Drawing can read, PNG with transparency is what you want.

.PARAMETER Size
Pixel size of the cursor, 32 is the Windows default. Max 128.

.PARAMETER CenterHotspot
Put the click point in the middle of the image instead of the top left corner. Use this for
crosshairs, rings and other symmetric shapes, exactly like the _center suffix in SysDVR.

.EXAMPLE
.\Convert-PngToCursor.ps1 -Path ..\arrow.png
.\Convert-PngToCursor.ps1 -Path ..\Zelda\sword.png -Size 48
.\Convert-PngToCursor.ps1 -Path ..\crosshair_center.png -CenterHotspot
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Path,
    [ValidateRange(8, 128)][int]$Size = 32,
    [switch]$CenterHotspot,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$source = (Resolve-Path $Path).Path
if (-not $OutputPath) {
    $OutputPath = [IO.Path]::ChangeExtension($source, '.cur')
}

# _center in the file name means the same thing here as it does in SysDVR
if ([IO.Path]::GetFileNameWithoutExtension($source).EndsWith('_center', 'OrdinalIgnoreCase')) {
    $CenterHotspot = $true
}

# Draw the image into a square of the requested size, keeping its aspect ratio
$src = New-Object System.Drawing.Bitmap($source)
$scale = [Math]::Min($Size / $src.Width, $Size / $src.Height)
$w = [Math]::Max(1, [int][Math]::Round($src.Width * $scale))
$h = [Math]::Max(1, [int][Math]::Round($src.Height * $scale))

$bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$g.Clear([System.Drawing.Color]::Transparent)
$g.DrawImage($src, 0, 0, $w, $h)
$g.Dispose()
$src.Dispose()

$hotX = if ($CenterHotspot) { [int]($w / 2) } else { 0 }
$hotY = if ($CenterHotspot) { [int]($h / 2) } else { 0 }

# A cursor holds a bottom-up 32bpp DIB of double height: the colour rows, then the AND mask.
# With an alpha channel the mask is all zeros and transparency comes from the alpha itself.
$maskStride = [int][Math]::Floor(($w + 31) / 32) * 4
$pixels = New-Object byte[] ($w * $h * 4)
$mask = New-Object byte[] ($maskStride * $h)

$data = $bmp.LockBits(
    (New-Object System.Drawing.Rectangle(0, 0, $w, $h)),
    [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
    [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

try {
    for ($y = 0; $y -lt $h; $y++) {
        # bottom-up
        $srcRow = [IntPtr]::Add($data.Scan0, ($h - 1 - $y) * $data.Stride)
        [System.Runtime.InteropServices.Marshal]::Copy($srcRow, $pixels, $y * $w * 4, $w * 4)
    }
}
finally {
    $bmp.UnlockBits($data)
    $bmp.Dispose()
}

$stream = [IO.File]::Create($OutputPath)
$writer = New-Object IO.BinaryWriter($stream)
try {
    $dibSize = 40 + $pixels.Length + $mask.Length

    # ICONDIR
    $writer.Write([UInt16]0)        # reserved
    $writer.Write([UInt16]2)        # 2 = cursor
    $writer.Write([UInt16]1)        # one image

    # ICONDIRENTRY
    $writer.Write([byte]($(if ($w -ge 256) { 0 } else { $w })))
    $writer.Write([byte]($(if ($h -ge 256) { 0 } else { $h })))
    $writer.Write([byte]0)          # palette size
    $writer.Write([byte]0)          # reserved
    $writer.Write([UInt16]$hotX)
    $writer.Write([UInt16]$hotY)
    $writer.Write([UInt32]$dibSize)
    $writer.Write([UInt32]22)       # offset of the image data

    # BITMAPINFOHEADER, height is doubled because of the mask
    $writer.Write([UInt32]40)
    $writer.Write([Int32]$w)
    $writer.Write([Int32]($h * 2))
    $writer.Write([UInt16]1)        # planes
    $writer.Write([UInt16]32)       # bits per pixel
    $writer.Write([UInt32]0)        # BI_RGB
    $writer.Write([UInt32]($pixels.Length + $mask.Length))
    $writer.Write([Int32]0); $writer.Write([Int32]0)
    $writer.Write([UInt32]0); $writer.Write([UInt32]0)

    $writer.Write($pixels)
    $writer.Write($mask)
}
finally {
    $writer.Dispose()
    $stream.Dispose()
}

# Ask Windows to parse it, so a broken file is caught here and not when it is applied
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class CursorCheck {
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr LoadCursorFromFile(string path);
    [DllImport("user32.dll")] public static extern bool DestroyCursor(IntPtr h);
}
"@ -ErrorAction SilentlyContinue

$handle = [CursorCheck]::LoadCursorFromFile($OutputPath)
if ($handle -eq [IntPtr]::Zero) {
    throw "Windows rejected the cursor file that was just written: $OutputPath"
}
[void][CursorCheck]::DestroyCursor($handle)

$hotspot = if ($CenterHotspot) { "centre ($hotX,$hotY)" } else { "top left" }
Write-Host "Wrote $OutputPath  -  ${w}x${h}, hotspot $hotspot, accepted by Windows."
Write-Host "To use it everywhere in Windows, see Apply-WindowsCursor.ps1 in this folder."
