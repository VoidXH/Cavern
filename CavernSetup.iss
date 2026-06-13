; Inno Setup 安装脚本 - Cavern 套件
; 包含 Cavernize 和 FilterStudio

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
OutputDir=Output
OutputBaseFilename=Cavern_Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
LicenseFile=CHANGELOG.md
UninstallDisplayIcon={app}\CavernizeGUI.exe
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; CavernizeGUI
Source: "CavernSamples\CavernizeGUI\publish\CavernizeGUI.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "CavernSamples\CavernizeGUI\publish\*.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "CavernSamples\CavernizeGUI\publish\*.config"; DestDir: "{app}"; Flags: ignoreversion
; FilterStudio
Source: "CavernSamples\FilterStudio\publish\FilterStudio.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "CavernSamples\FilterStudio\publish\*.config"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Cavernize"; Filename: "{app}\CavernizeGUI.exe"; Comment: "空间音频上混渲染器"
Name: "{group}\Filter Studio"; Filename: "{app}\FilterStudio.exe"; Comment: "音频滤波器设计工具"
Name: "{group}\{cm:UninstallProgram,Cavern}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Cavernize"; Filename: "{app}\CavernizeGUI.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CavernizeGUI.exe"; Description: "{cm:LaunchProgram,Cavernize}"; Flags: nowait postinstall skipifsilent
