#define MyAppName "PAMP!"
#define MyAppVersion "1.2.0"
#define MyAppPublisher "Szymon Jarecki"
#define MyAppURL "https://github.com/szyjare/PAMP"
#define MyAppExeName "PAMP.exe"

[Setup]
AppId={{C18F1202-986D-46F2-8116-F1A9346E8203}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\PAMP
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE.txt
OutputDir=..\dist
OutputBaseFilename=PAMP-Setup-{#MyAppVersion}
SetupIconFile=..\PAMP\pamp_icon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Pozwala użytkownikowi wybrać tryb instalacji: dla bieżącego użytkownika lub dla wszystkich (system)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog commandline

[Languages]
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Dirs]
; Nadaje uprawnienia zapisu dla folderu bin (aby pobieranie modułów działało nawet przy instalacji w Program Files)
Name: "{app}\bin"; Permissions: users-modify

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  DownloadPage: TDownloadWizardPage;
  DotNetNeeded: Boolean;

function IsDotNet10DesktopInstalled: Boolean;
var
  FindRec: TFindRec;
begin
  Result := False;
  if FindFirst(ExpandConstant('{commonpf64}\dotnet\shared\Microsoft.WindowsDesktop.App\10.*'), FindRec) then
  begin
    try
      Result := True;
    finally
      FindClose(FindRec);
    end;
  end;
end;

procedure InitializeWizard;
begin
  // Jeśli PAMP skompilowano w trybie self-contained, .NET 10 jest już wbudowany w PAMP.exe
  if not FileExists(ExpandConstant('{src}\..\publish\PAMP.dll')) then
    DotNetNeeded := False
  else
    DotNetNeeded := not IsDotNet10DesktopInstalled;

  if DotNetNeeded then
  begin
    DownloadPage := CreateDownloadPage(SetupMessage(msgWizardPreparing), SetupMessage(msgPreparingDesc), nil);
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;

  if (CurPageID = wpReady) and DotNetNeeded then
  begin
    DownloadPage.Clear;
    DownloadPage.Add('https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe', 'dotnet10-runtime-installer.exe', '');
    DownloadPage.Show;
    try
      try
        DownloadPage.Download;
        Result := True;
      except
        if DownloadPage.AbortedByUser then
        begin
          Log('Download aborted by user.');
          Result := False;
        end
        else
        begin
          SuppressibleMsgBox(AddPeriod(GetExceptionMessage), mbCriticalError, MB_OK, IDOK);
          Result := False;
        end;
      end;
    finally
      DownloadPage.Hide;
    end;

    if Result then
    begin
      WizardForm.StatusLabel.Caption := 'Instalowanie Microsoft .NET Desktop Runtime 10...';
      if not Exec(ExpandConstant('{tmp}\dotnet10-runtime-installer.exe'), '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ErrorCode) then
      begin
        SuppressibleMsgBox('Nie udalo sie zainstalowac .NET 10. Pobierz go recznie ze strony Microsoftu.', mbError, MB_OK, IDOK);
      end;
    end;
  end;
end;
