param([string]$OutputPath='build/Volti')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root $OutputPath
New-Item -ItemType Directory -Force $output | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if(!(Test-Path $compiler)){throw 'The .NET Framework 4.x C# compiler is required.'}
$sources=Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' | Select-Object -ExpandProperty FullName
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$output/Volti.exe" "/win32icon:$root/assets/Volti.ico" "/win32manifest:$root/src/Volti.manifest" $sources
if($LASTEXITCODE -ne 0){throw 'Compilation failed.'}
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /define:VOLTI_PANEL "/out:$output/Volti.UI.exe" "/win32icon:$root/assets/Volti.ico" "/win32manifest:$root/src/Volti.manifest" $sources
if($LASTEXITCODE -ne 0){throw 'Panel compilation failed.'}
Copy-Item (Join-Path $root 'assets') $output -Recurse -Force
New-Item -ItemType Directory -Force (Join-Path $output 'docs') | Out-Null
Copy-Item (Join-Path $root 'docs/banner.png'),(Join-Path $root 'docs/preview-Nord.png') (Join-Path $output 'docs') -Force
Copy-Item (Join-Path $root 'LICENSE'),(Join-Path $root 'THIRD_PARTY_NOTICES.md'),(Join-Path $root 'README.md'),(Join-Path $root 'CHANGELOG.md') $output -Force
Write-Output "Built $output/Volti.exe"
