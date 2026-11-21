; SPDX-License-Identifier: AGPL-3.0-only
#ifndef AppVersion
  #define AppVersion "0.3.3"
#endif
#ifndef BuildDir
  #define BuildDir "..\build\Volti"
#endif
[Setup]
AppId={{C09F5E69-BD13-420C-AAB7-79630B2658CF}
AppName=Volti
AppVersion={#AppVersion}
AppVerName=Volti {#AppVersion}
DefaultDirName={localappdata}\Programs\Volti
DefaultGroupName=Volti
PrivilegesRequired=lowest
OutputDir=..\dist
OutputBaseFilename=Volti-{#AppVersion}-Setup
SetupIconFile=..\assets\Volti.ico
UninstallDisplayIcon={app}\Volti.exe
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
Name: "startup"; Description: "Start Volti with Windows"; Flags: unchecked

[Files]
Source: "{#BuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Volti"; Filename: "{app}\Volti.exe"
Name: "{autodesktop}\Volti"; Filename: "{app}\Volti.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Volti"; ValueData: """{app}\Volti.exe"""; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\Volti.exe"; Description: "Launch Volti in the system tray"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Volti', StartupCommand) then
      if CompareText(StartupCommand, '"' + ExpandConstant('{app}\Volti.exe') + '"') = 0 then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Volti');
end;
