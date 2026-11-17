$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'The .NET Framework C# compiler is required to build this Windows widget.' }
$releaseDir = Join-Path $PSScriptRoot 'release'
$downloadDir = Join-Path $projectRoot 'dist\downloads'
New-Item -ItemType Directory -Path $releaseDir, $downloadDir -Force | Out-Null
$exePath = Join-Path $releaseDir 'CistercianClock.exe'
& $compilerPath /nologo /target:winexe /optimize+ /platform:anycpu "/out:$exePath" "/win32manifest:$(Join-Path $PSScriptRoot 'app.manifest')" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll (Join-Path $PSScriptRoot 'CistercianClock.cs')
if ($LASTEXITCODE -ne 0) { throw 'The widget build failed.' }
$testReport = Join-Path $releaseDir 'test-report.txt'
$testRun = Start-Process -FilePath $exePath -ArgumentList @('--self-test', ('"' + $testReport + '"')) -Wait -PassThru -WindowStyle Hidden
if ($testRun.ExitCode -ne 0) { throw (Get-Content -LiteralPath $testReport -Raw) }
Get-Content -LiteralPath $testReport
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.txt') -Destination $releaseDir -Force
$archivePath = Join-Path $downloadDir 'CistercianClock-Windows.zip'
Compress-Archive -LiteralPath $exePath, (Join-Path $releaseDir 'README.txt') -DestinationPath $archivePath -Force
$checksum = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
Set-Content -LiteralPath (Join-Path $downloadDir 'CistercianClock-Windows.sha256') -Value ($checksum.Hash.ToLower() + '  CistercianClock-Windows.zip') -Encoding ascii
Write-Output $archivePath
