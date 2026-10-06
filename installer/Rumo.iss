#define ProductName "Rumo"
#define ProductExecutable "Navegador.exe"
#ifndef AppVersion
  #define AppVersion "0.6.1"
#endif

[Setup]
AppId={{7D56A7A4-5F8B-4A91-971D-6F42B00443B2}
AppName={#ProductName}
AppVersion={#AppVersion}
AppPublisher=joaoldsxyzbr
DefaultDirName={localappdata}\Programs\Rumo
DefaultGroupName=Rumo
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=Rumo-v{#AppVersion}-windows-x64-setup
SetupIconFile=..\assets\rumo.ico
UninstallDisplayIcon={app}\Navegador.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Criar atalho na área de trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked

[Files]
Source: "..\publish\Navegador\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\assets\rumo.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: ".rumo-installed"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\Rumo"; Filename: "{app}\{#ProductExecutable}"; IconFilename: "{app}\rumo.ico"
Name: "{autodesktop}\Rumo"; Filename: "{app}\{#ProductExecutable}"; IconFilename: "{app}\rumo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\VC_redist.x64.exe"; Parameters: "/install /quiet /norestart"; StatusMsg: "Instalando o runtime Microsoft Visual C++ 2022 x64..."; Flags: waituntilterminated; Check: not IsVc2022RuntimeInstalled
Filename: "{app}\{#ProductExecutable}"; Description: "Abrir o Rumo"; Flags: nowait postinstall skipifsilent

[Code]
function IsVc2022RuntimeInstalled: Boolean;
var
  Installed, Major, Minor: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', Installed) and (Installed = 1) then
  begin
    if RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Major', Major) and
       RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Minor', Minor) then
    begin
      Result := (Major > 14) or ((Major = 14) and (Minor >= 30));
    end;
  end;
end;
