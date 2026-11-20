param([string]$IsccPath='')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1')
if(!$IsccPath){
 $command=Get-Command ISCC.exe -ErrorAction SilentlyContinue
 if($command){$IsccPath=$command.Source}
 else {foreach($candidate in @("${env:ProgramFiles(x86)}/Inno Setup 6/ISCC.exe","$env:LOCALAPPDATA/Programs/Inno Setup 6/ISCC.exe")){if(Test-Path $candidate){$IsccPath=$candidate;break}}}
}
if(!(Test-Path $IsccPath)){throw 'Install Inno Setup 6 or pass -IsccPath to its ISCC.exe.'}
& $IsccPath (Join-Path $root 'installer/Volti.iss')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed.'}
$dist=Join-Path $root 'dist'
Compress-Archive -Path (Join-Path $root 'build/Volti/*') -DestinationPath (Join-Path $dist 'Volti-0.1.0-preview.1-portable.zip') -Force
$sourceFiles=@('src','assets','docs','scripts','installer','README.md','LICENSE','THIRD_PARTY_NOTICES.md','.gitignore','.gitattributes') | ForEach-Object {Join-Path $root $_}
Compress-Archive -Path $sourceFiles -DestinationPath (Join-Path $dist 'Volti-0.1.0-preview.1-source.zip') -Force
Get-ChildItem $dist -File | Where-Object Extension -in '.exe','.zip' | Get-FileHash -Algorithm SHA256 | ForEach-Object {"$($_.Hash.ToLower())  $(Split-Path $_.Path -Leaf)"} | Set-Content (Join-Path $dist 'SHA256SUMS.txt') -Encoding ASCII
