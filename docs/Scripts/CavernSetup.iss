; Inno Setup installer script for Cavern suite
; Includes CavernizeGUI and FilterStudio with zh-CN localization

#define MyAppName "Cavern"
#define MyAppVersion "1.0"
#define MyAppPublisher "Bence Sgánetz"
#define MyAppURL "https://cavern.sbence.hu/"

[Setup]
AppId={{A7F9771D-BEA0-4DE3-B267-46E1B87FE612}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\Cavern
DefaultGroupName=Cavern
AllowNoIcons=yes
OutputDir=..\..\Output
OutputBaseFilename=Cavern_Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\..\CHANGELOG.md
UninstallDisplayIcon={app}\CavernizeGUI.exe
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; CavernizeGUI
Source: "..\..\CavernSamples\CavernizeGUI\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; FilterStudio
Source: "..\..\CavernSamples\FilterStudio\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Cavernize"; Filename: "{app}\CavernizeGUI.exe"
Name: "{group}\Filter Studio"; Filename: "{app}\FilterStudio.exe"
Name: "{group}\{cm:UninstallProgram,Cavern}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Cavernize"; Filename: "{app}\CavernizeGUI.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CavernizeGUI.exe"; Description: "{cm:LaunchProgram,Cavernize}"; Flags: nowait postinstall skipifsilent
