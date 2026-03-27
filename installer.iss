[Setup]
AppName=SchoolSchedule
AppVersion=1.0
AppPublisher=Laptev Andrey
DefaultDirName={autopf}\SchoolSchedule
DefaultGroupName=SchoolSchedule
OutputBaseFilename=SchoolSchedule_Setup
Compression=lzma
SolidCompression=yes
SetupIconFile=Assets\appicon.ico
WizardStyle=modern

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительно:"

[Files]
Source: "bin\Release\net48-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\SchoolSchedule";      Filename: "{app}\SchoolSchedule.exe"
Name: "{autodesktop}\SchoolSchedule"; Filename: "{app}\SchoolSchedule.exe"; Tasks: desktopicon


[Run]
Filename: "{app}\SchoolSchedule.exe"; Description: "Запустить SchoolSchedule"; Flags: nowait postinstall skipifsilent
