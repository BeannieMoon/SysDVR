<#
.SYNOPSIS
Sets a .cur file as the Windows mouse pointer, so the same pointer is used everywhere and not
only inside SysDVR.

.DESCRIPTION
SysDVR can only control the pointer while it is over the SysDVR window. Everywhere else, the
shape is decided by Windows and by whichever app the pointer is over, which is why it turns
into a text I-beam over text, a hand over links and so on.

This script points the Windows pointer roles at a cursor file you made with
Convert-PngToCursor.ps1. By default it changes the two shapes you see the most: normal select
and text select, so the pointer stops changing under you.

It writes a backup of your current settings before changing anything, and -Revert puts the
Windows defaults back.

This only touches your own user (HKEY_CURRENT_USER), never the machine, and needs no admin
rights. Nothing is applied until you run it with -Apply.

.PARAMETER CursorPath
The .cur file to use. Keep it somewhere permanent, Windows reads it on every logon, so do not
leave it in a temp folder or on a drive that is not always connected.

.PARAMETER Roles
Which pointer shapes to replace. Default: Arrow (normal select) and IBeam (text select).
Valid: Arrow, IBeam, Hand, AppStarting, Wait, Crosshair, Help, No, SizeAll, UpArrow.

.PARAMETER Apply
Actually write the change. Without it the script only prints what it would do.

.PARAMETER Revert
Restore the Windows default pointers.

.EXAMPLE
.\Apply-WindowsCursor.ps1 -CursorPath C:\cursors\sword.cur
.\Apply-WindowsCursor.ps1 -CursorPath C:\cursors\sword.cur -Apply
.\Apply-WindowsCursor.ps1 -CursorPath C:\cursors\sword.cur -Roles Arrow,IBeam,Hand -Apply
.\Apply-WindowsCursor.ps1 -Revert -Apply
#>
[CmdletBinding()]
param(
    [string]$CursorPath,
    [ValidateSet('Arrow', 'IBeam', 'Hand', 'AppStarting', 'Wait', 'Crosshair', 'Help', 'No', 'SizeAll', 'UpArrow')]
    [string[]]$Roles = @('Arrow', 'IBeam'),
    [switch]$Apply,
    [switch]$Revert
)

$ErrorActionPreference = 'Stop'
$key = 'HKCU:\Control Panel\Cursors'

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class CursorApply {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SystemParametersInfo(uint action, uint param, IntPtr ptr, uint winIni);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadCursorFromFile(string path);
    [DllImport("user32.dll")] public static extern bool DestroyCursor(IntPtr h);
}
"@ -ErrorAction SilentlyContinue

function Refresh-Cursors {
    # SPI_SETCURSORS = 0x0057, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE = 3
    [void][CursorApply]::SystemParametersInfo(0x0057, 0, [IntPtr]::Zero, 3)
}

function Backup-Current {
    $stamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $file = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "cursor-backup-$stamp.reg"
    $lines = @('Windows Registry Editor Version 5.00', '', '[HKEY_CURRENT_USER\Control Panel\Cursors]')
    $current = Get-ItemProperty -Path $key
    foreach ($name in @('(default)') + $Roles) {
        $value = if ($name -eq '(default)') { $current.'(default)' } else { $current.$name }
        if ($null -eq $value) { $value = '' }
        $escaped = $value -replace '\\', '\\\\'
        $lines += '"' + $name + '"="' + $escaped + '"'
    }
    $lines | Set-Content -Path $file -Encoding utf8
    return $file
}

if ($Revert) {
    Write-Host "Will restore the Windows default pointers for: $($Roles -join ', ')"
    if (-not $Apply) { Write-Host "`nNothing changed. Add -Apply to do it." -ForegroundColor Yellow; return }

    $backup = Backup-Current
    foreach ($role in $Roles) { Set-ItemProperty -Path $key -Name $role -Value '' }
    Set-ItemProperty -Path $key -Name '(default)' -Value 'Windows default'
    Refresh-Cursors
    Write-Host "Done, defaults restored. Previous values saved to $backup"
    return
}

if (-not $CursorPath) { throw "Pass -CursorPath, or -Revert to undo." }

$full = (Resolve-Path $CursorPath).Path
$handle = [CursorApply]::LoadCursorFromFile($full)
if ($handle -eq [IntPtr]::Zero) { throw "Windows cannot read this cursor file: $full" }
[void][CursorApply]::DestroyCursor($handle)

if ($full -like "$env:TEMP*") {
    Write-Warning "That file is in a temp folder. Move it somewhere permanent first, Windows reads it on every logon."
}

Write-Host "Cursor file : $full"
Write-Host "Roles       : $($Roles -join ', ')"
Write-Host "Scope       : your user account only (HKEY_CURRENT_USER), no admin rights needed"
Write-Host "Undo        : .\Apply-WindowsCursor.ps1 -Revert -Apply, or Settings > Bluetooth & devices > Mouse >"
Write-Host "              Additional mouse settings > Pointers > scheme 'Windows Default'"

if (-not $Apply) {
    Write-Host "`nNothing changed. Add -Apply to do it." -ForegroundColor Yellow
    return
}

$backup = Backup-Current
foreach ($role in $Roles) { Set-ItemProperty -Path $key -Name $role -Value $full }
Set-ItemProperty -Path $key -Name '(default)' -Value 'SysDVR custom'
Refresh-Cursors

Write-Host "`nDone. Previous values saved to $backup"
