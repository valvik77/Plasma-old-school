#ifndef MyAppVersion
  #define MyAppVersion "1.0.1"
#endif

#define MyAppName "Plasma Old School"
#define MyAppPublisher "Victor Valenzuela Vega"
#define MyAppExeName "PlasmaOldSchool.scr"

[Setup]
AppId={{D624AC7C-DA31-47D5-AB6A-1D42ED1502CE}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Instalador de {#MyAppName}
DefaultDirName={autopf}\Plasma Old School
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
UsePreviousAppDir=no
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir=..\release
OutputBaseFilename=PlasmaOldSchoolSetup
SetupIconFile=..\assets\plasma-old-school.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
SignedUninstaller=no

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\dist\PlasmaOldSchool.scr"; DestDir: "{sys}"; DestName: "PlasmaOldSchool.scr"; Flags: ignoreversion restartreplace

[InstallDelete]
Type: filesandordirs; Name: "{localappdata}\Programs\PlasmaOldSchool"

[Registry]
Root: HKLM64; Subkey: "Software\Valvik\PlasmaOldSchool"; ValueType: string; ValueName: "InstallLocation"; ValueData: "{app}"; Flags: uninsdeletekey

[Icons]
Name: "{group}\Configurar {#MyAppName}"; Filename: "{app}\PlasmaOldSchool.exe"; Parameters: "/c"
Name: "{group}\Probar {#MyAppName}"; Filename: "{app}\PlasmaOldSchool.exe"; Parameters: "/s"
Name: "{group}\Desinstalar {#MyAppName}"; Filename: "{uninstallexe}"

[Run]
Filename: "{sys}\rundll32.exe"; Parameters: "desk.cpl,InstallScreenSaver ""{sys}\{#MyAppExeName}"""; Description: "Abrir la configuración de salvapantallas de Windows"; Flags: postinstall nowait skipifsilent

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    RegWriteStringValue(HKCU, 'Control Panel\Desktop', 'SCRNSAVE.EXE',
      ExpandConstant('{sys}\{#MyAppExeName}'));
    RegWriteStringValue(HKCU, 'Control Panel\Desktop', 'ScreenSaveActive', '1');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  CurrentSaver: String;
  InstalledSaver: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    InstalledSaver := ExpandConstant('{sys}\{#MyAppExeName}');
    if RegQueryStringValue(HKCU, 'Control Panel\Desktop', 'SCRNSAVE.EXE', CurrentSaver) and
       (CompareText(CurrentSaver, InstalledSaver) = 0) then
    begin
      RegDeleteValue(HKCU, 'Control Panel\Desktop', 'SCRNSAVE.EXE');
      RegWriteStringValue(HKCU, 'Control Panel\Desktop', 'ScreenSaveActive', '0');
    end;
    RegDeleteKeyIncludingSubkeys(HKCU, 'Software\PlasmaOldSchoolScreenSaver');
  end;
end;
