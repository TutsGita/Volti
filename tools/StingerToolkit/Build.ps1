param()
$ErrorActionPreference='Stop'
$root=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$out=Join-Path $root 'build/StingerToolkit'
New-Item -ItemType Directory -Force $out | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$files=@('src/Shared/Win32.cs','src/Monitor/HyperXBattery.cs') | ForEach-Object {Join-Path $root $_}
$entry=Join-Path $PSScriptRoot 'StingerToolkit.cs'
$exe=Join-Path $out 'StingerToolkit.exe'
& $compiler /nologo /target:exe /main:Volti.StingerToolkit /optimize+ /reference:System.Web.Extensions.dll "/out:$exe" $files $entry
if($LASTEXITCODE){throw 'Falha na compilação do toolkit.'}
Copy-Item (Join-Path $root 'LICENSE'),(Join-Path $root 'THIRD_PARTY_NOTICES.md'),(Join-Path $PSScriptRoot 'README.md'),(Join-Path $PSScriptRoot 'Comparar.ps1') $out -Force
$dist=Join-Path $root 'dist'
New-Item -ItemType Directory -Force $dist | Out-Null
Compress-Archive -Path (Join-Path $out '*') -DestinationPath (Join-Path $dist 'StingerToolkit.zip') -Force
$source=@('tools','src','LICENSE','THIRD_PARTY_NOTICES.md') | ForEach-Object {Join-Path $root $_}
Compress-Archive -Path $source -DestinationPath (Join-Path $dist 'StingerToolkit-source.zip') -Force
