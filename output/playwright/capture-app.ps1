param(
    [string]$OutFile = "D:\vibe_codeing\Prepress-toolbox\output\playwright\app-preview.png",
    [int]$ClickX = -1,
    [int]$ClickY = -1
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class WinApi {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, uint dx, uint dy, uint d, UIntPtr e);
    [StructLayout(LayoutKind.Sequential)] public struct R { public int Left; public int Top; public int Right; public int Bottom; }
}
"@

$proc = Get-Process -Name ([char]0x5927 + [char]0x8BDA + [char]0x91CD + [char]0x547D + [char]0x540D + [char]0x5DE5 + [char]0x5177) -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $proc) {
    Write-Host "app process not found"
    exit 1
}

$handle = $proc.MainWindowHandle
[void][WinApi]::ShowWindow($handle, 9)
[void][WinApi]::SetForegroundWindow($handle)
Start-Sleep -Milliseconds 900

$rect = New-Object WinApi+R
[void][WinApi]::GetWindowRect($handle, [ref]$rect)
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top

if ($ClickX -ge 0 -and $ClickY -ge 0) {
    [void][WinApi]::SetCursorPos($rect.Left + $ClickX, $rect.Top + $ClickY)
    Start-Sleep -Milliseconds 200
    [WinApi]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [WinApi]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 900
    [void][WinApi]::SetForegroundWindow($handle)
    Start-Sleep -Milliseconds 400
}

$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($width, $height)))
$graphics.Dispose()
$bitmap.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

Write-Host "saved $OutFile  ${width}x${height} at ($($rect.Left),$($rect.Top))"
