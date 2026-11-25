param(
    [switch]$NoZip
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}
if (-not (Test-Path 'assets\app.ico')) {
    throw "assets\app.ico not found"
}

$outDir = Join-Path $root 'dist\MiniScan'
if (Test-Path $outDir) {
    Remove-Item $outDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$sources = Get-ChildItem -Path (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object { $_.FullName }
$cscArgs = @(
    '/nologo',
    '/utf8output',
    '/target:winexe',
    '/platform:x86',
    '/optimize+',
    '/codepage:65001',
    ('/out:' + (Join-Path $outDir 'MiniScan.exe')),
    ('/win32manifest:' + (Join-Path $root 'src\app.manifest')),
    ('/win32icon:' + (Join-Path $root 'assets\app.ico')),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    ('/reference:' + (Join-Path $root 'lib\NTwain.dll'))
)
$cscArgs += $sources

& $csc @cscArgs
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Copy-Item -Path 'lib\NTwain.dll' -Destination $outDir -Force
Copy-Item -Path 'src\MiniScan.exe.config' -Destination (Join-Path $outDir 'MiniScan.exe.config') -Force
Copy-Item -Path 'README.md' -Destination $outDir -Force

if (-not $NoZip) {
    $zip = Join-Path $root 'dist\MiniScan-win32.zip'
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $outDir '*') -DestinationPath $zip -CompressionLevel Optimal
    Write-Host ("Archive: " + $zip)
}

Write-Host ("Output: " + $outDir)
Get-ChildItem $outDir | Select-Object Name, Length | Format-Table -AutoSize
