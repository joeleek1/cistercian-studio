param(
    [string]$SourceDirectory = (Join-Path $PSScriptRoot 'release\1.2.0'),
    [string]$InstallDirectory = (Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\CistercianClock'),
    [switch]$DesktopShortcut,
    [switch]$StartWithWindows,
    [switch]$Launch
)
$ErrorActionPreference = 'Stop'
$sourceExe = Join-Path $SourceDirectory 'CistercianClock.exe'
if (-not (Test-Path -LiteralPath $sourceExe -PathType Leaf)) { throw 'Build the clock first with desktop/build.ps1.' }
$targetExe = Join-Path $InstallDirectory 'CistercianClock.exe'
$running = Get-Process -Name CistercianClock -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $targetExe }
if ($running) { throw 'Close the installed Cistercian Clock before upgrading it. Your settings will be retained.' }
New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
Copy-Item -LiteralPath $sourceExe -Destination $targetExe -Force
Copy-Item -LiteralPath (Join-Path $SourceDirectory 'README.txt') -Destination (Join-Path $InstallDirectory 'README.txt') -Force
$sourceSettings = Join-Path $SourceDirectory 'CistercianClock.settings'
$targetSettings = Join-Path $InstallDirectory 'CistercianClock.settings'
if ((Test-Path -LiteralPath $sourceSettings) -and -not (Test-Path -LiteralPath $targetSettings)) { Copy-Item -LiteralPath $sourceSettings -Destination $targetSettings }
if ((Get-FileHash -LiteralPath $sourceExe).Hash -ne (Get-FileHash -LiteralPath $targetExe).Hash) { throw 'Installed executable does not match the build.' }
$configurationArgs = @()
if ($DesktopShortcut) { $configurationArgs += '--desktop-shortcut' }
if ($StartWithWindows) { $configurationArgs += '--enable-startup' }
if ($configurationArgs.Count) {
    $reportPath = Join-Path $InstallDirectory 'setup-result.txt'
    $configurationArgs += '--integration-report', ('"' + $reportPath + '"')
    $configure = Start-Process -FilePath $targetExe -ArgumentList $configurationArgs -WindowStyle Hidden -Wait -PassThru
    if ($configure.ExitCode -ne 0) { throw (Get-Content -LiteralPath $reportPath -Raw) }
    Get-Content -LiteralPath $reportPath
}
Write-Output ('Installed: ' + $targetExe)
if ($Launch) { Start-Process -FilePath $targetExe -WorkingDirectory $InstallDirectory | Out-Null }
