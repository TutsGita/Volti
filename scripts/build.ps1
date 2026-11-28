param([string]$OutputPath='build/Volti')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root $OutputPath
New-Item -ItemType Directory -Force $output | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(!(Test-Path $compiler)){throw 'The .NET Framework 4.x C# compiler is required.'}
function Sources([string[]]$folders){$folders | ForEach-Object {Get-ChildItem (Join-Path $root "src/$_") -Filter '*.cs'} | Select-Object -ExpandProperty FullName}
$common=@('/nologo','/target:winexe','/platform:anycpu','/optimize+','/reference:System.Web.Extensions.dll',"/win32icon:$root/assets/Volti.ico","/win32manifest:$root/src/Volti.manifest")
# Volti.exe is the tray monitor. Volti.UI.exe is the panel. They share src/Shared.
& $compiler @common "/out:$output/Volti.exe" (Sources @('Shared','Monitor'))
if($LASTEXITCODE -ne 0){throw 'Monitor compilation failed.'}
& $compiler @common "/out:$output/Volti.UI.exe" (Sources @('Shared','Panel'))
if($LASTEXITCODE -ne 0){throw 'Panel compilation failed.'}
Copy-Item (Join-Path $root 'assets') $output -Recurse -Force
New-Item -ItemType Directory -Force (Join-Path $output 'docs') | Out-Null
Copy-Item (Join-Path $root 'docs/banner.png'),(Join-Path $root 'docs/preview-panel.png'),(Join-Path $root 'docs/release-0.6.0.md') (Join-Path $output 'docs') -Force
Copy-Item (Join-Path $root 'LICENSE'),(Join-Path $root 'THIRD_PARTY_NOTICES.md'),(Join-Path $root 'README.md'),(Join-Path $root 'CHANGELOG.md') $output -Force
Write-Output "Built $output/Volti.exe and $output/Volti.UI.exe"
