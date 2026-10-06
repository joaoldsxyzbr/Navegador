#define ProductName "Rumo"
#define ProductExecutable "Navegador.exe"
#ifndef AppVersion
  #define AppVersion "0.6.0"
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
Filename: "{app}\{#ProductExecutable}"; Description: "Abrir o Rumo"; Flags: nowait postinstall skipifsilent
