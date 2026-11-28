#!/usr/bin/env sh
# Compiles both executables and the tests with Mono's C# 5 compiler, then runs the tests.
# Windows builds use scripts/build.ps1; this script lets Linux and CI catch errors early.
set -e
root=$(cd "$(dirname "$0")/.." && pwd)
out=${1:-"$root/build/check"}
mkdir -p "$out"
refs="-r:System.Windows.Forms.dll -r:System.Drawing.dll -r:System.Web.Extensions.dll -r:System.Core.dll"
mcs -nologo -langversion:5 -warnaserror+ -target:winexe -out:"$out/Volti.exe" $refs "$root"/src/Shared/*.cs "$root"/src/Monitor/*.cs
mcs -nologo -langversion:5 -warnaserror+ -target:winexe -out:"$out/Volti.UI.exe" $refs "$root"/src/Shared/*.cs "$root"/src/Panel/*.cs
mcs -nologo -langversion:5 -warnaserror+ -target:exe -out:"$out/Volti.Tests.exe" $refs "$root"/src/Shared/*.cs "$root"/src/Monitor/*.cs "$root"/tests/*.cs -main:Volti.Tests.Runner
mcs -nologo -langversion:5 -target:exe -main:Volti.StingerToolkit -out:"$out/StingerToolkit.exe" -r:System.Web.Extensions.dll "$root"/src/Shared/Win32.cs "$root"/src/Monitor/HyperXBattery.cs "$root"/tools/StingerToolkit/StingerToolkit.cs
cp -r "$root/assets" "$out/"
mono "$out/Volti.Tests.exe" "$root"
