[Setup]
AppName=Game Plugin Kit
AppVerName=Game Plugin Kit
DefaultDirName={localappdata}\GamePluginKit
DefaultGroupName=Game Plugin Kit
OutputBaseFilename=gpk-setup
PrivilegesRequired=lowest

[Types]
Name: "full"; Description: "Full"
Name: "custom"; Description: "Custom"; Flags: iscustom

[Components]
Name: "app"; Description: "Game Plugin Kit"; Types: full custom; Flags: fixed
Name: "cli"; Description: "Developer CLI"; Types: full
Name: "csp"; Description: "C# Source Plugin Support"; Types: full

[Files]
; Game Plugin Kit
Source: "Core\GamePluginKit.API.dll"; DestDir: "{app}\Core"; Components: app
Source: "Tools\Patcher\*"; DestDir: "{app}\Tools\Patcher"; Components: app; Flags: recursesubdirs

; Developer CLI
Source: "Tools\CLI\*"; DestDir: "{app}\Tools\CLI"; Components: cli; Flags: recursesubdirs

; C# Source Plugin Support
Source: "Core\ScriptPluginLoader.dll"; DestDir: "{app}\Core"; Components: csp
Source: "Tools\Compiler\*"; DestDir: "{app}\Tools\Compiler"; Components: csp; Flags: recursesubdirs

[Dirs]
Name: "{app}\Plugins"; Flags: uninsneveruninstall

[Icons]
Name: "{group}\Game Patcher"; Filename: "{app}\Tools\Patcher\GamePluginKit.Patcher.exe"; Components: app
Name: "{group}\Uninstall"; Filename: "{uninstallexe}"

[Registry]
Root: HKCU; Subkey: "Environment"; \
  ValueType: string;               \
  ValueName: "GamePluginKitDir";   \
  ValueData: "{app}\";             \
  Components: app;                 \
  Flags: preservestringtype

Root: HKCU; Subkey: "Environment";        \
  ValueType: expandsz;                    \
  ValueName: "Path";                      \
  ValueData: "{app}\Tools\CLI;{olddata}"; \
  Components: cli;                        \
  Check: CheckPathEntry(ExpandConstant('{app}\Tools\CLI'))

[Code]
function CheckPathEntry(Path : String) : Boolean;
  var EnvPath : String;
begin
  if not RegQueryStringValue(HKCU, 'Environment', 'Path', EnvPath) then begin
    Result := True;
    exit;
  end;

  Path    := ';' + UpperCase(Path);
  EnvPath := ';' + UpperCase(EnvPath) + ';';

  Result :=
    (Pos(Path +  ';', EnvPath) = 0) and
    (Pos(Path + '\;', EnvPath) = 0);
end;
