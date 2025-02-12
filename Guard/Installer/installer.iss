﻿#define MyAppName "2FAGuard"
#define MyAppVersion "1.2.0"
#define MyAppPublisher "Timo Kössler"
#define MyAppURL "https://2faguard.app"
#define MyAppExeName "2FAGuard.exe"

#include "CodeDependencies.iss"

[Setup]
AppId={{E975C7D9-79F6-47D9-9597-014331FC3C0F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=..\bin\installer
OutputBaseFilename=2FAGuard-Installer-{#MyAppVersion}
SetupIconFile=..\totp.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64
UninstallDisplayIcon={app}\{#MyAppExeName}
AppCopyright=Timo Kössler and Open Source Contributors
MinVersion=10.0.18362
ShowLanguageDialog=auto
DisableReadyPage=yes
UsePreviousTasks=yes
DisableFinishedPage=yes
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64
SignTool=mysigntool sign /n $qOpen Source Developer, Timo Kössler$q /t $qhttp://time.certum.pl/$q /fd sha256 /d $q2FAGuard Installer$q /du $qhttps://2faguard.app$q $f
SignedUninstaller=yes
SignToolRetryCount=0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
Source: "..\bin\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall; Parameters: setup

[Code]
function InitializeSetup: Boolean;
begin
  Dependency_AddVC2015To2022;
  Result := True;
end;
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
end;