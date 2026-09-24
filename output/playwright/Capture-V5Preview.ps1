param(
    [string]$OutFile = "D:\vibe_codeing\Prepress-toolbox\output\playwright\app-preview.png",
    [int]$ClickX = -1,
    [int]$ClickY = -1
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class Win32Api {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
"@

$script:found = [IntPtr]::Zero

function Get-PreviewWindow {
    $script:found = [IntPtr]::Zero
    $callback = [Win32Api+EnumWindowsProc]{
        param($hWnd, $lParam)
        if (-not [Win32Api]::IsWindowVisible($hWnd)) { return $true }
        $rect = New-Object Win32Api+RECT
        if (-not [Win32Api]::GetWindowRect($hWnd, [ref]$rect)) { return $true }
        $width = $rect.Right - $rect.Left
        $height = $rect.Bottom - $rect.Top
        if ($width -ge 340 -and $width -le 380 -and $height -gt 280) {
            $script:found = $hWnd
            return $false
        }
        return $true
    }
    [void][Win32Api]::EnumWindows($callback, [IntPtr]::Zero)
    return $script:found
}

$handle = [IntPtr]::Zero
for ($i = 0; $i -lt 40 -and $handle -eq [IntPtr]::Zero; $i++) {
    Start-Sleep -Milliseconds 500
    $handle = Get-PreviewWindow
}

if ($handle -eq [IntPtr]::Zero) {
    Write-Host "preview window not found"
    exit 1
}

$rect = New-Object Win32Api+RECT
[void][Win32Api]::GetWindowRect($handle, [ref]$rect)
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top

if ($ClickX -ge 0 -and $ClickY -ge 0) {
    [void][Win32Api]::SetForegroundWindow($handle)
    Start-Sleep -Milliseconds 400
    [void][Win32Api]::SetCursorPos($rect.Left + $ClickX, $rect.Top + $ClickY)
    Start-Sleep -Milliseconds 150
    [Win32Api]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [Win32Api]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 700
}

[void][Win32Api]::SetForegroundWindow($handle)
Start-Sleep -Milliseconds 600

$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($width, $height)))
$graphics.Dispose()
$bitmap.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

Write-Host "saved $OutFile window ${width}x${height} at ($($rect.Left),$($rect.Top))"
