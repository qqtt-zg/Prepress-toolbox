[Setup]
; 应用程序基本信息
AppName=大诚重命名工具
AppId={{D5F3E9B0-FEAT-ADD-PERF-MONITOR-FORM}}
AppVersion=2.6.9
AppVerName=大诚重命名工具 v2.6.9
AppPublisher=大诚软件
AppPublisherURL=
AppSupportURL=
AppUpdatesURL=
AppCopyright=Copyright (C) 2026 大诚软件
; 版本更新说明
AppComments=V2.6.9更新内容：全面重构材料选择框V5工作台——卡片4重构为四大一体化InputGroup组合胶囊框（前缀浅灰标签+分割线+无缝内嵌，彻底消除杂乱框线与空白杂音）；卡片6预设药丸增加平张浅蓝/卷装琥珀橙微型语义印标；卡片2目录切换升级为Ant Design线性展开文件夹图标；卡片5排版控制行标识页升级为无边框大线性图标并与连拼/折手等距靠右排版，新增大版开料非默认尺寸指示胶囊（平张卡尺印张/卷装高度突出显示，光学精确居中）；底部批量处理升级为低饱和浅蓝流光滑块，铺满时平滑覆盖文字且不抢主按钮焦点；预设管理新增快捷保存按钮与预设持久化恢复；彻底隔离材料选择框与主界面层级，杜绝主窗体跟随置顶。

; 默认安装目录
DefaultDirName={autopf}\大诚重命名工具
DefaultGroupName=大诚重命名工具

; 输出设置
; 注意：文件名包含版本号，确保每次版本更新都会生成不同的文件名，从而保留旧版本的安装包
; 请确保在更新版本号时同步更新此处的文件名版本号
OutputBaseFilename=大诚重命名工具_v2.6.9_安装包
OutputDir=.\安装包
; SetupIconFile=dc.ico

; 安装程序设置
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
MinVersion=6.1sp1
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible

; .NET Framework 4.8 要求
WizardImageFile=
WizardSmallImageFile=
DisableStartupPrompt=yes
DisableReadyPage=yes
DisableFinishedPage=yes

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; 主程序和所有依赖DLL（使用win-x64子目录）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\大诚重命名工具.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\大诚重命名工具.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\*.xml"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\dc.ico"; DestDir: "{app}\Resources"; Flags: ignoreversion

; PDF.js 资源文件（必需）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\pdfjs\*"; DestDir: "{app}\Resources\pdfjs"; Flags: ignoreversion recursesubdirs

; 字体资源文件（可选）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\Fonts\*"; DestDir: "{app}\Resources\Fonts"; Flags: ignoreversion

; 图标资源文件（可选）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\Icons\*"; DestDir: "{app}\Resources\Icons"; Flags: ignoreversion

; Ghostscript 便携版（必需 - 用于PDF转曲和字体检测）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\gswin64c.exe"; DestDir: "{app}\ghostscript"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\gsdll64.dll"; DestDir: "{app}\ghostscript"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\ghostscript\lib\*"; DestDir: "{app}\ghostscript\lib"; Flags: ignoreversion recursesubdirs

; Poppler 工具箱 (必需 - 用于精确字体检测)
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\poppler\*"; DestDir: "{app}\poppler"; Flags: ignoreversion recursesubdirs

; CefSharp 浏览器子进程（必需）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\CefSharp.BrowserSubprocess.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\CefSharp.BrowserSubprocess.Core.dll"; DestDir: "{app}"; Flags: ignoreversion

; CefSharp 资源文件（必需）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\*.pak"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\*.bin"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\*.dat"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\*.json"; DestDir: "{app}"; Flags: ignoreversion

; CefSharp 语言包（必需）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\locales\*"; DestDir: "{app}\locales"; Flags: ignoreversion recursesubdirs

; PdfiumViewer所需的pdfium本地库（必须放在x64子目录）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\x64\pdfium.dll"; DestDir: "{app}\x64"; Flags: ignoreversion
; LogConfig.json由程序自动创建，无需打包

; 不再打包默认配置文件，允许用户完全自定义配置

; 数据文件夹（如果存在）- 注释掉空文件夹引用
; Source: "src\WindowsFormsApp3\bin\Debug\net48\SavedGrids\*"; DestDir: "{app}\SavedGrids"; Flags: recursesubdirs createallsubdirs external ignoreversion

[Icons]
Name: "{group}\大诚重命名工具"; Filename: "{app}\大诚重命名工具.exe"; IconFilename: "{app}\Resources\dc.ico"
Name: "{group}\卸载大诚重命名工具"; Filename: "{uninstallexe}"
Name: "{commondesktop}\大诚重命名工具"; Filename: "{app}\大诚重命名工具.exe"; Tasks: desktopicon; IconFilename: "{app}\Resources\dc.ico"

[Run]
Filename: "{app}\大诚重命名工具.exe"; Description: "{cm:LaunchProgram,大诚重命名工具}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}\SavedGrids"
Type: filesandordirs; Name: "{app}\logs"
Type: dirifempty; Name: "{app}"

[Code]
// 检查.NET Framework 4.8是否已安装
function IsDotNet48Installed: Boolean;
var
  Version: Cardinal;
begin
  // .NET Framework 4.8 的Release版本是528040
  if RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Version) then
    Result := (Version >= 528040)
  else
    Result := False;
end;

// 安装.NET Framework 4.8
procedure InstallDotNetFramework48;
var
  ResultCode: Integer;
begin
  if not IsDotNet48Installed then
  begin
    if MsgBox('大诚重命名工具需要.NET Framework 4.8，是否现在下载安装？', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet-framework/thank-you/net48-web-installer', '', '', SW_SHOW, ewNoWait, ResultCode);
      MsgBox('请先安装.NET Framework 4.8，然后重新运行安装程序。', mbInformation, MB_OK);
      Abort;
    end
    else
    begin
      MsgBox('没有.NET Framework 4.8，程序无法运行。安装程序将退出。', mbError, MB_OK);
      Abort;
    end;
  end;
end;

// 安装前检查
function InitializeSetup(): Boolean;
begin
  Result := True;
  InstallDotNetFramework48;
end;

// 保护用户配置文件不被替换
procedure ProtectUserConfigFiles;
var
  ConfigPath: string;  
begin
  ConfigPath := ExpandConstant('{userappdata}\大诚重命名工具');
  
  // 如果配置文件夹存在，创建保护标记文件
  if DirExists(ConfigPath) then
  begin
    // 创建保护标记，告知用户配置文件已受保护
    SaveStringToFile(ConfigPath + '\protected.txt', '用户配置文件受保护 - ' + GetDateTimeString('yyyy/mm/dd hh:nn:ss', '-', ':'), False);
  end;
end;

// 安装完成后创建必要文件夹
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // 创建日志文件夹
    if not DirExists(ExpandConstant('{app}\logs')) then
      CreateDir(ExpandConstant('{app}\logs'));

    // 创建AppData配置文件夹（不包含默认配置文件）
    if not DirExists(ExpandConstant('{userappdata}\大诚重命名工具')) then
      CreateDir(ExpandConstant('{userappdata}\大诚重命名工具'));
      
    // 保护用户配置文件
    ProtectUserConfigFiles;
  end;
end;

// 自定义卸载确认页面
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  if CurPageID = wpSelectDir then
  begin
    // 简化空间检查 - Inno Setup 会自动检查磁盘空间
    // 如果空间不足，安装程序会自动提示用户
  end;
  Result := True;
end;
