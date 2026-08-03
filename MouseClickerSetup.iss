#define MyAppName "鼠标连点器"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "J4s"
#define MyAppExeName "MouseClicker.exe"

[Setup]
AppId={{1F3B9C8A-4D5E-4F6A-8B7C-2D3E4F5A6B7C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\MouseClicker
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=MouseClickerSetup-{#MyAppVersion}
OutputDir=dist
SetupIconFile=icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
CloseApplications=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务:"; Flags: unchecked

[Files]
Source: "bin\Release\net8.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\icon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动鼠标连点器"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet8RuntimeInstalled(): Boolean;
var
  ver: String;
begin
  Result := RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App', 'Version', ver) or
            RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x86\sharedfx\Microsoft.NETCore.App', 'Version', ver);
end;

function InitializeSetup(): Boolean;
var
  errCode: Integer;
begin
  if not IsDotNet8RuntimeInstalled then
  begin
    if MsgBox('检测到未安装 .NET 8 运行时，程序可能无法启动。是否现在打开下载页面？', mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/8.0/runtime', '', '', SW_SHOWNORMAL, ewNoWait, errCode);
  end;
  Result := True;
end;
