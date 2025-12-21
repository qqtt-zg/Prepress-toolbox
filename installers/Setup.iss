[Setup]
; 应用程序基本信息
AppName=大诚重命名工具
AppId={{D5F3E9B0-FEAT-ADD-PERF-MONITOR-FORM}}
AppVersion=2.2.0
AppVerName=大诚重命名工具 v2.2.0
AppPublisher=大诚软件
AppPublisherURL=
AppSupportURL=
AppUpdatesURL=
AppCopyright=Copyright (C) 2025 大诚软件
; 版本更新说明
AppComments=V2.2.0更新内容：全新PDF预览功能，支持悬浮页码显示；窗体延迟显示优化，内容与窗体同时呈现；修复PDF预览初始化显示问题；优化最佳适应缩放算法；右键菜单添加最佳适应选项；修复PDF文件占用问题；V2.0.6更新内容：修复新文件数据匹配和列组合显示问题；1. **新文件数据匹配修复**：修复新文件处理时未进行数据匹配的问题，确保文件名去除扩展名后进行正则匹配，提取准确的regexResult用于数据库查询；2. **参数验证增强**：当Excel正则和主正则都为空时，提示用户缺少必要的参数，避免静默失败导致的数据丢失；3. **列组合自动补全**：改进列组合功能，当用户未在Excel导入中选择组合列时，系统自动尝试从Excel行数据中提取行数和列数列，智能生成列组合值，提升易用性；4. **未分组项目分隔符修复**：修复当只使用未分组且多个项目时，项目之间没有显示分隔符的问题，确保与状态栏规则显示一致；5. **日志记录完善**：增强数据匹配和列组合过程的日志记录，便于问题诊断和追踪；6. **测试验证通过**：经过用户测试验证（包括Windows7环境），确保新文件处理、数据匹配和列组合生成的稳定性和正确性；V2.0.5更新内容：优化返单文件处理流程，完善数据匹配机制；1. **智能返单文件识别**：实现自动识别返单文件的功能，检测文件名中的保留格式前缀（&ID-、&MT-等），无需手动切换正则表达式；2. **分隔符处理规则优化**：修复分组间分隔符显示问题，确保Windows7和Windows10系统显示一致；分组间不显示分隔符，分组内仅多个项目时才使用分隔符；3. **混合逻辑处理增强**：优化混合场景（返单+新文件）的分隔符处理，避免混合逻辑中产生不需要的分隔符；4. **状态栏规则显示完善**：修复状态栏中规则显示的分隔符问题，确保与重命名结果保持一致；5. **代码可维护性提升**：重构返单文件处理逻辑，提升代码清晰度和易维护性；V2.0.4更新内容：版本信息同步更新，完善用户界面版本显示；1. **版本号统一**：修复程序集版本号与安装包版本号不一致问题，确保所有位置显示统一版本号2.0.4；2. **关于界面优化**：精简版本说明内容，解决历史版本说明过长需要滚动显示的问题；3. **功能说明整合**：将核心功能以简洁的两列布局展示，提升用户阅读体验；4. **版本信息完善**：确保菜单栏关于选项、程序集属性、安装包版本信息完全一致；V2.0.2更新内容：完善重命名规则保留功能，实现结果与UI显示一致性；1. **数据同步机制优化**：修复保留功能中文件重命名后UI显示不一致问题，确保dgvFiles控件显示正确的保留字段数据；2. **保留值恢复逻辑增强**：完善RestorePreservedFields方法，移除条件检查限制，直接将BackupData中的保留值恢复到FileRenameInfo对应属性；3. **UI刷新机制改进**：优化RefreshDgvFiles方法，确保保留功能处理完成后UI能够准确显示混合后的数据结果；4. **数据一致性保证**：实现文件系统重命名结果与界面显示数据的完全一致性，提升用户操作体验；V2.0.1更新内容：修复保留功能字段提取正则表达式，优化返单场景下的数据识别准确性；1. **正则表达式优化**：更新订单号、材料、工艺、数量等关键字段的提取正则表达式，提升对复杂文件命名格式的识别能力；2. **字段识别准确性提升**：改进多格式兼容性，支持带前缀和无前缀两种文件名格式，确保保留功能的可靠性；3. **数据提取稳定性增强**：优化正则匹配逻辑，减少提取失败情况，提升返单处理的稳定性；4. **兼容性修复**：确保新旧文件命名格式的向后兼容，保护用户历史数据；V2.0.0更新内容：全新重大版本升级！1. **保留分组冲突检测功能**：实现智能冲突检测机制，确保每个分组只能保留一个项目，避免数据备份位置混乱；提供友好的冲突提示对话框，让用户清晰了解冲突原因和解决方案；支持自动清理现有保留状态，确保系统稳定性；2. **事件分组管理功能升级**：实现chkLstEvents分组功能，支持9个专业预定义分组（订单组、材料组、数量组、工艺组、客户组、备注组、行数组、列数组、未分组）；使用TreeView替换CheckedListBox实现层级化管理，支持拖拽排序和项目移动；每个分组支持独立前缀配置和启用/禁用控制；3. **数据模型重构优化**：重构FileRenameInfo核心模型，新增OriginalNameDataExtractor和PreserveFieldMapper工具类，提升数据处理效率和准确性；优化BatchProcessingService架构，实现更清晰的职责分离；4. **自动化测试覆盖增强**：新增全面的单元测试和集成测试，包括AutoSaveTests、BatchProcessingServiceTests、EndToEndTests等，确保系统稳定性；5. **Chrome MCP自动化扩展**：集成Chrome DevTools自动化测试功能，支持Web自动化测试和浏览器扩展集成；6. **UI现代化优化**：实现SVG图标资源支持，优化控件视觉效果；增强MaterialSelectFormModern响应式布局，提升用户体验；7. **性能优化和稳定性提升**：优化内存使用，提升处理效率；完善异常处理机制，增强系统健壮性；修复多项已知问题，确保生产环境稳定运行；V1.7.5更新内容：修复卷装布局算法逻辑矛盾问题，统一选择策略；调整布局计算优先级：1.列数最大 2.宽度利用率 3.空间利用率；优化卷装材料布局选择，优先选择能放置更多列的方案；解决标准卷装与一式两联算法选择逻辑不一致问题；提升生产效率和材料利用率的平衡优化；V1.7.4更新内容：修复状态栏规则显示问题，确保&符号正确显示；优化前缀格式显示，使用&ID-订单号&MT-材料等清晰格式；增强分组和项目关系的可视化效果；完善WinForms中特殊字符的转义处理机制；改进用户界面的专业性和可读性；V1.7.3更新内容：新增印刷排版"一式两联"功能，支持折手印刷流水码；实现偶数列自动布局优化，确保相同号码在同一列；修复卷装材料布局计算逻辑，提升宽度利用率；优化布局选择算法，优先选择实际使用宽度最优方案；修复取消启用排版时的状态同步问题

; 默认安装目录
DefaultDirName={autopf}\大诚重命名工具
DefaultGroupName=大诚重命名工具

; 输出设置
OutputBaseFilename=大诚重命名工具_v2.2.0_安装包
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
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\Resources\dc.ico"; DestDir: "{app}"; Flags: ignoreversion
; PdfiumViewer所需的pdfium本地库（必须放在x64子目录）
Source: "..\src\WindowsFormsApp3\bin\Release\net48\win-x64\x64\pdfium.dll"; DestDir: "{app}\x64"; Flags: ignoreversion
; LogConfig.json由程序自动创建，无需打包

; 不再打包默认配置文件，允许用户完全自定义配置

; 数据文件夹（如果存在）- 注释掉空文件夹引用
; Source: "src\WindowsFormsApp3\bin\Debug\net48\SavedGrids\*"; DestDir: "{app}\SavedGrids"; Flags: recursesubdirs createallsubdirs external ignoreversion

[Icons]
Name: "{group}\大诚重命名工具"; Filename: "{app}\大诚重命名工具.exe"; IconFilename: "{app}\dc.ico"
Name: "{group}\卸载大诚重命名工具"; Filename: "{uninstallexe}"
Name: "{commondesktop}\大诚重命名工具"; Filename: "{app}\大诚重命名工具.exe"; Tasks: desktopicon; IconFilename: "{app}\dc.ico"

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