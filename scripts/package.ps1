param([string]$IsccPath='', [string]$Version='0.6.0')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$buildPath="build/release-$Version-$([Guid]::NewGuid().ToString('N'))"
& (Join-Path $PSScriptRoot 'test.ps1')
& (Join-Path $PSScriptRoot 'build.ps1') -OutputPath $buildPath
if($LASTEXITCODE -ne 0){throw 'Application build failed.'}
foreach($exe in 'Volti.exe','Volti.UI.exe'){
 $built=(Get-Item (Join-Path $root "$buildPath/$exe")).VersionInfo.ProductVersion
 if($built -ne $Version){throw "$exe reports version $built, expected $Version. Update src/Shared/AppInfo.cs and src/Volti.manifest."}
}
if(!$IsccPath){
 $command=Get-Command ISCC.exe -ErrorAction SilentlyContinue
 if($command){$IsccPath=$command.Source}
 else {foreach($candidate in @("${env:ProgramFiles(x86)}/Inno Setup 6/ISCC.exe","$env:LOCALAPPDATA/Programs/Inno Setup 6/ISCC.exe")){if(Test-Path $candidate){$IsccPath=$candidate;break}}}
}
if(!(Test-Path $IsccPath)){throw 'Install Inno Setup 6 or pass -IsccPath to its ISCC.exe.'}
& $IsccPath "/DAppVersion=$Version" "/DBuildDir=$(Join-Path $root $buildPath)" (Join-Path $root 'installer/Volti.iss')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed.'}

$dist=Join-Path $root "dist/release/$Version"
New-Item -ItemType Directory -Force $dist | Out-Null
Copy-Item (Join-Path $root "dist/Volti-$Version-Setup.exe") $dist -Force
Compress-Archive -Path (Join-Path $root "$buildPath/*") -DestinationPath (Join-Path $dist "Volti-$Version-portable.zip") -Force
$sourceFiles=@('src','assets','docs','scripts','installer','tools','tests','README.md','CHANGELOG.md','LICENSE','THIRD_PARTY_NOTICES.md','.gitignore','.gitattributes') | ForEach-Object {Join-Path $root $_}
Compress-Archive -Path $sourceFiles -DestinationPath (Join-Path $dist "Volti-$Version-source.zip") -Force
$files=@("Volti-$Version-Setup.exe","Volti-$Version-portable.zip","Volti-$Version-source.zip") |
  ForEach-Object { Get-Item -LiteralPath (Join-Path $dist $_) }
$files | Get-FileHash -Algorithm SHA256 | ForEach-Object {"$($_.Hash.ToLower())  $(Split-Path $_.Path -Leaf)"} | Set-Content (Join-Path $dist 'SHA256SUMS.txt') -Encoding ASCII
Write-Output "Release assets created in $dist"
