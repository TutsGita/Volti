# Compiles and runs the parser and state checks in tests/. Exit code 0 means all checks passed.
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root 'build/tests'
New-Item -ItemType Directory -Force $output | Out-Null
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$sources=@('src/Shared','src/Monitor','tests') | ForEach-Object {Get-ChildItem (Join-Path $root $_) -Filter '*.cs'} | Select-Object -ExpandProperty FullName
& $compiler /nologo /target:exe /main:Volti.Tests.Runner /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "/out:$output/Volti.Tests.exe" $sources
if($LASTEXITCODE -ne 0){throw 'Test compilation failed.'}
& (Join-Path $output 'Volti.Tests.exe') $root
if($LASTEXITCODE -ne 0){throw 'Tests failed.'}
