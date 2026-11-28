; SPDX-License-Identifier: AGPL-3.0-only
#ifndef AppVersion
  #define AppVersion "0.6.0"
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
; Updates started by Volti into a portable folder do not register an uninstaller.
Uninstallable=not IsPortableUpdate
MinVersion=10.0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked; Check: not IsPortableUpdate
Name: "startup"; Description: "Start Volti with Windows"; Flags: unchecked; Check: not IsPortableUpdate

[Files]
Source: "{#BuildDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Volti"; Filename: "{app}\Volti.exe"; Check: not IsPortableUpdate
Name: "{autodesktop}\Volti"; Filename: "{app}\Volti.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "Volti"; ValueData: """{app}\Volti.exe"" --background"; Tasks: startup; Flags: uninsdeletevalue

[Run]
Filename: "{app}\Volti.exe"; Description: "Launch Volti in the system tray"; Flags: nowait postinstall skipifsilent
; After an update started from Volti, bring the monitor back in the background.
Filename: "{app}\Volti.exe"; Parameters: "--background"; Flags: nowait runasoriginaluser; Check: IsVoltiUpdate

[Code]
function HasSwitch(Name: String): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), Name) = 0 then
      Result := True;
end;

function IsVoltiUpdate: Boolean;
begin
  Result := HasSwitch('/VOLTIUPDATE');
end;

function IsPortableUpdate: Boolean;
begin
  Result := HasSwitch('/PORTABLEUPDATE');
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
begin
  if CurUninstallStep = usUninstall then
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Volti', StartupCommand) then
      if (CompareText(StartupCommand, '"' + ExpandConstant('{app}\Volti.exe') + '"') = 0) or
         (CompareText(StartupCommand, '"' + ExpandConstant('{app}\Volti.exe') + '" --background') = 0) then
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Volti');
end;
