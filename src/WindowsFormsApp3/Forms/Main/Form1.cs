using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using Ookii.Dialogs.WinForms;
using System.Configuration;
using WindowsFormsApp3.Properties;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Commands;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Interfaces;
// 使用别名避免命名冲突
using LogHelper = WindowsFormsApp3.Utils.LogHelper;
// 解决ValidationResult命名冲突 - 明确使用Models.ValidationResult
using ValidationResult = WindowsFormsApp3.Models.ValidationResult;
// 已移除重复的命名空间引用



namespace WindowsFormsApp3.Forms.Main
{
    public partial class Form1 : Form, IForm1View
    {
        private string _currentConfigName = "标准配置";
        private WindowsFormsApp3.Services.IUndoRedoService _undoRedoService;
        private IEnhancedUndoRedoService _enhancedUndoRedoService;
        private DgvContextMenu _dgvContextMenu; // 类级别的成员变量
        private DgvContextMenu _dgvExcelContextMenu; // dgvExcelData的右键菜单
        private List<DataRow> _matchedRows = new();
        private bool isMonitoring = false;
        private readonly FileSystemWatcher watcher = new();
        private readonly List<FileInfo> pendingFiles = new();
        private Dictionary<string, string> regexPatterns = new();
        private List<string> materials = new();
        private int failCount = 0;
        private bool isImmediateRenameActive = true;
        private bool isCopyMode = true;
        private readonly IPdfDimensionService _pdfDimensionService;
        private DataTable _excelImportedData;
        private int _excelSearchColumnIndex = -1;
        private int _excelReturnColumnIndex = -1;
        private int _excelNewColumnIndex = -1;
        private ExcelImportHelper _excelImportHelper;
        private string currentRegexPattern; // 当前选中的正则表达式模式
        private readonly NotifyIcon trayIcon = new();
        private ContextMenuStrip trayMenu;
        private IForm1Presenter _presenter;
        private ICompositeColumnService _compositeColumnService;

        // 状态栏滚动相关字段
        private Timer _statusScrollTimer;

        public int ExcelNewColumnIndex
        {
            get { return _excelNewColumnIndex; }
        }

        // 热键相关常量和变量
        private const int WM_HOTKEY = 0x0312;
        private int toggleHotkeyId = 1;
        private IntPtr toggleHotkeyAtom = IntPtr.Zero;

        // Windows API函数声明
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAddAtom(string lpString);

        [DllImport("kernel32.dll")]
        private static extern ushort GlobalDeleteAtom(IntPtr nAtom);

      private FileRenameService _fileRenameService;

        public Form1()
        {
            try
            {
                LogDebugInfo("Form1构造函数: 开始初始化...");

                // 初始化服务
                _undoRedoService = ServiceLocator.Instance.GetUndoRedoService();
                _enhancedUndoRedoService = ServiceLocator.Instance.GetService<IEnhancedUndoRedoService>();
                _pdfDimensionService = PdfDimensionServiceFactory.GetInstance();
                _fileRenameService = ServiceLocator.Instance.GetFileRenameService() as FileRenameService;

                // 修复TextItems配置，确保序号被禁用
                FixTextItemsConfiguration();

                InitializeComponent();
                // 绑定参数设置按钮点击事件
                // 已移除txtSeparator控件，分隔符设置已迁移至SettingsForm管理

                // 初始化状态栏滚动Timer
                InitializeStatusScrollTimer();



                this.Text = "大诚工具箱"; // 设置窗口标题
                // 初始化系统托盘图标
                var iconPath = Path.Combine(Application.StartupPath, "dc.ico");
                if (File.Exists(iconPath))
                {
                    trayIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    // 如果图标文件不存在，使用系统默认图标
                    LogDebugInfo($"警告: 图标文件不存在: {iconPath}");
                }
                trayIcon.Text = "大诚工具箱";
                // 初始化按钮文本
                if (isImmediateRenameActive)
                {
                    btnImmediateRename.Text = "已启动手动模式";
                    btnStopImmediateRename.Text = "批量模式";
                }
                else
                {
                    btnStopImmediateRename.Text = "已启动批量模式";
                    btnImmediateRename.Text = "手动模式";
                }
                trayIcon.Visible = false;
                trayIcon.DoubleClick += TrayIcon_DoubleClick;
                this.Resize += Form1_Resize;
                // 移除直接绑定，让Presenter完全控制FormClosing事件
                // this.FormClosing += Form1_FormClosing;
                // 设置窗口图标
                var windowIconPath = Path.Combine(Application.StartupPath, "dc.ico");
                if (File.Exists(windowIconPath))
                {
                    this.Icon = new Icon(windowIconPath);
                }
                else
                {
                    LogDebugInfo($"警告: 窗口图标文件不存在: {windowIconPath}");
                }

                // 初始化Presenter
                LogDebugInfo("Form1构造函数: 初始化Presenter...");
                _presenter = new Form1Presenter(this);
                
                // 获取列组合服务
                _compositeColumnService = ServiceLocator.Instance.GetCompositeColumnService();
                
                // 添加调试信息 - 验证FormClosing事件绑定
                LogDebugInfo("Form1构造函数: Presenter已初始化，FormClosing事件应该已绑定");
                
                // 重要：绑定原生FormClosing事件到我们的自定义事件
                LogDebugInfo("Form1构造函数: 绑定原生FormClosing事件...");
                this.FormClosing += OnNativeFormClosing;
                LogDebugInfo("Form1构造函数: 原生FormClosing事件绑定完成");

                // 添加应用程序退出事件监听
                Application.ApplicationExit += Application_ApplicationExit;
                
                // 订阅材料更改事件
                MaterialManager.Instance.MaterialsChanged += OnMaterialsChanged;
                
                InitializeTrayMenu();
                InitializeEvents();
                // 移除LoadSettings()调用，因为它会在Form1_Load → OnConfigLoaded中被调用，避免递归
                InitializeDataGridView();
                dgvFiles.Resize += DgvFiles_Resize;
                cmbRegex.SelectedIndexChanged += cmbRegex_SelectedIndexChanged; // 绑定选择变化事件
                RegisterHotkeys();
                
                // 初始化Excel导入助手
                _excelImportHelper = new ExcelImportHelper(this);

                // 初始化JSON文件管理
                var jsonDir = Path.Combine(Application.StartupPath, "SavedGrids");
                if (!Directory.Exists(jsonDir)) Directory.CreateDirectory(jsonDir);
                PopulateJsonFilesDropdown();
                
                LogDebugInfo("Form1构造函数: 初始化完成");
            }
            catch (Exception ex)
            {
                LogDebugInfo("Form1构造函数: 初始化异常: " + ex.Message);
                LogDebugInfo("Form1构造函数: 异常堆栈: " + ex.StackTrace);
                throw;
            }
        }

        /// <summary>
        /// 应用程序退出事件处理
        /// </summary>
        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            LogDebugInfo("=== Application_ApplicationExit: 应用程序正在退出 ===");
        }


        /// <summary>
        /// 处理原生FormClosing事件，并通知Presenter
        /// </summary>
        private void OnNativeFormClosing(object sender, FormClosingEventArgs e)
        {
            LogDebugInfo("=== OnNativeFormClosing: 原生FormClosing事件被触发! ===");
            LogDebugInfo("OnNativeFormClosing: CloseReason=" + e.CloseReason + ", Cancel=" + e.Cancel + ", _allowClose=" + _allowClose);
            LogDebugInfo("OnNativeFormClosing: sender=" + (sender?.GetType().Name ?? "null") + ", 窗口状态=" + this.WindowState);
            
            // 检查FormClosing事件是否有订阅者
            if (FormClosing == null)
            {
                LogDebugInfo("OnNativeFormClosing: 警告! FormClosing事件没有订阅者，直接执行后备处理!");
            }
            else
            {
                LogDebugInfo("OnNativeFormClosing: FormClosing事件有订阅者，即将通知Presenter...");
                
                // 触发我们的自定义FormClosing事件，通知Presenter
                try
                {
                    FormClosing?.Invoke(this, e);
                    LogDebugInfo("OnNativeFormClosing: Presenter通知完成，事件状态: Cancel=" + e.Cancel);
                }
                catch (Exception ex)
                {
                    LogDebugInfo("OnNativeFormClosing: Presenter通知异常: " + ex.Message);
                }
            }
            
            // 后备处理逻辑：如果Presenter没有取消关闭，我们在这里处理
            if (!e.Cancel)
            {
                LogDebugInfo("OnNativeFormClosing: Presenter没有取消关闭，执行后备处理...");
                
                // 如果是用户点击X按钮且不允许直接关闭，则最小化到托盘
                if (e.CloseReason == CloseReason.UserClosing && !_allowClose)
                {
                    LogDebugInfo("OnNativeFormClosing: 执行后备托盘逻辑 - 用户点击X按钮，最小化到托盘");
                    
                    // 先执行自动保存
                    PerformAutoSave();
                    
                    // 取消关闭并最小化到托盘
                    e.Cancel = true;
                    this.WindowState = FormWindowState.Minimized;
                    this.Hide();
                    trayIcon.Visible = true;
                    
                    LogDebugInfo("OnNativeFormClosing: 后备托盘逻辑完成 - WindowState=" + this.WindowState + ", trayIcon.Visible=" + trayIcon.Visible);
                }
                else if (e.CloseReason == CloseReason.UserClosing && _allowClose)
                {
                    LogDebugInfo("OnNativeFormClosing: 允许关闭，执行后备自动保存...");
                    PerformAutoSave();
                }
            }
            else
            {
                LogDebugInfo("OnNativeFormClosing: Presenter已取消关闭，最小化到托盘");
            }
            
            LogDebugInfo("OnNativeFormClosing: 最终事件状态: Cancel=" + e.Cancel);
            LogDebugInfo("=== OnNativeFormClosing: 处理完成 ===");
        }
        
        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 设置允许关闭标志，确保真正退出
            _allowClose = true;
            
            // 添加调试信息
            LogDebugInfo("退出菜单点击: 设置_allowClose=true，即将调用this.Close()");
            
            // 检查窗口当前状态
            LogDebugInfo("退出菜单点击: 窗口当前状态 - WindowState=" + this.WindowState + ", Visible=" + this.Visible + ", Enabled=" + this.Enabled);
            LogDebugInfo("退出菜单点击: 窗口句柄信息 - Handle=" + this.Handle + ", IsHandleCreated=" + this.IsHandleCreated);
            
            // 添加更多调试信息来跟踪Close()调用
            LogDebugInfo("退出菜单点击: 开始调用this.Close()方法...");
            
            try
            {
                // 尝试不同的关闭方法
                LogDebugInfo("退出菜单点击: 调用this.Close()...");
                this.Close();
                
                // 如果执行到这里说明Close()没有真正关闭应用程序
                LogDebugInfo("退出菜单点击: this.Close()调用完成，但应用程序仍在运行");
                
                // 尝试强制关闭
                LogDebugInfo("退出菜单点击: 尝试Application.Exit()强制退出...");
                
                // 在强制退出前先执行自动保存
                LogDebugInfo("退出菜单点击: 在Application.Exit()前先执行自动保存...");
                PerformAutoSave();
                
                Application.Exit();
                
                LogDebugInfo("退出菜单点击: Application.Exit()调用完成");
            }
            catch (Exception ex)
            {
                LogDebugInfo("退出菜单点击: 调用异常: " + ex.Message);
            }
        }
        
        private void 打开日志ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 获取日志文件夹路径（与FileLogger中的默认路径一致）
                string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string logFolderPath = AppDataPathManager.LogsDirectory;
                
                // 检查日志文件夹是否存在
                if (Directory.Exists(logFolderPath))
                {
                    // 打开日志文件夹
                    System.Diagnostics.Process.Start("explorer.exe", logFolderPath);
                    LogDebugInfo("已打开日志文件夹: " + logFolderPath);
                }
                else
                {
                    // 如果文件夹不存在，尝试创建
                    Directory.CreateDirectory(logFolderPath);
                    
                    // 再次检查是否创建成功
                    if (Directory.Exists(logFolderPath))
                    {
                        // 打开创建的文件夹
                        System.Diagnostics.Process.Start("explorer.exe", logFolderPath);
                        LogDebugInfo("日志文件夹不存在，已创建并打开: " + logFolderPath);
                        MessageBox.Show("日志文件夹已创建并打开", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // 显示错误消息
                        string errorMessage = "无法访问日志文件夹: " + logFolderPath;
                        LogDebugInfo(errorMessage);
                        MessageBox.Show(errorMessage, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "打开日志文件夹时发生错误: " + ex.Message;
                LogDebugInfo(errorMessage);
                MessageBox.Show(errorMessage, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Form1_Resize(object sender, EventArgs e)
        {
            // 触发Resize事件
            Resize?.Invoke(this, e);
            
            // 当窗口最小化时
            if (WindowState == FormWindowState.Minimized)
            {
                // 调试输出
                LogHelper.Debug($"Form1_Resize: 窗口最小化，隐藏主窗口，显示托盘图标");
                LogHelper.Debug($"Form1_Resize: WindowState={WindowState}, Visible={this.Visible}, trayIcon.Visible={trayIcon.Visible}");
                LogDebugInfo($"Form1_Resize: 窗口最小化，隐藏主窗口，显示托盘图标");
                LogDebugInfo($"Form1_Resize: WindowState={WindowState}, Visible={this.Visible}, trayIcon.Visible={trayIcon.Visible}");
                
                // 隐藏主窗口并隐藏任务栏图标
                this.Hide();
                // 显示系统托盘图标
                trayIcon.Visible = true;
                
                LogHelper.Debug("Form1_Resize: 操作后 - Visible=" + this.Visible + ", trayIcon.Visible=" + trayIcon.Visible);
                LogDebugInfo("Form1_Resize: 操作后 - Visible=" + this.Visible + ", trayIcon.Visible=" + trayIcon.Visible);
            }
        }

        /// <summary>
        /// 正则表达式选择变化时触发
        /// </summary>
        private void cmbRegex_SelectedIndexChanged(object sender, EventArgs e)
        {
            LogHelper.Debug("cmbRegex_SelectedIndexChanged事件被触发");

            if (cmbRegex.SelectedItem != null)
            {
                string selectedPatternName = cmbRegex.SelectedItem.ToString();
                LogHelper.Debug($"选择的正则表达式模式名称: '{selectedPatternName}'");

                // 立即保存选择到Properties.Settings
                AppSettings.LastSelectedRegex = selectedPatternName;
                AppSettings.Save();
                LogHelper.Debug($"已保存LastSelectedRegex = '{selectedPatternName}'");

                if (regexPatterns.TryGetValue(selectedPatternName, out string pattern))
                {
                    currentRegexPattern = pattern;
                    LogHelper.Debug($"CurrentRegexPattern已更新为: '{currentRegexPattern}'");
                    // 显示当前使用的重命名规则
                    LogHelper.Debug($"当前使用的重命名规则 (来自cmbRegex_SelectedIndexChanged):");
                    LogHelper.Debug($"  - 正则表达式模式: '{pattern}'");
                    LogHelper.Debug($"  - 模式名称: '{selectedPatternName}'");
                    LogHelper.Debug($"  - 模式来源: cmbRegex_SelectedIndexChanged事件");
                }
                else
                {
                    LogHelper.Debug($"未找到正则表达式模式: '{selectedPatternName}'");
                }
            }
            else
            {
                LogHelper.Debug("cmbRegex.SelectedItem为null");
            }
        }

        private void btnImmediateRename_Click(object sender, EventArgs e)
        {
            isImmediateRenameActive = true;
            btnImmediateRename.Text = "已启动手动模式";
            btnStopImmediateRename.Text = "批量模式";
            UpdateTrayMenuItems();
            UpdateStatusStrip();
            UpdateDgvFilesEditMode();
            
            // 触发接口事件
            ImmediateRenameClick?.Invoke(this, EventArgs.Empty);
        }

        private void btnStopImmediateRename_Click(object sender, EventArgs e)
        {
            isImmediateRenameActive = false;
            btnStopImmediateRename.Text = "已启动批量模式";
            btnImmediateRename.Text = "手动模式";
            // 这里可以添加更新文件列表的逻辑
            UpdateTrayMenuItems();
            UpdateStatusStrip();
            UpdateDgvFilesEditMode();
            
            // 触发接口事件
            StopImmediateRenameClick?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// 管理正则表达式按钮点击事件处理程序
        /// </summary>
        private void BtnManageRegex_Click(object sender, EventArgs e)
        {
            try
            {
                // 将业务逻辑完全委托给Presenter处理
                _presenter.HandleManageRegexClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("管理正则表达式时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void btnBatchProcess_Click(object sender, EventArgs e)
        {
            try
            {
                // 触发批量处理事件，让Presenter处理业务逻辑
                BatchProcessClick?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("批量处理发生异常: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // 实现IForm1View接口的UpdateFileList方法
        public void UpdateFileList()
        {
            try
            {
                // 获取当前数据源
                var bindingList = FileBindingList;
                
                if (bindingList != null)
                {
                    // 刷新数据网格视图
                    dgvFiles.Refresh();
                    
                    // 更新状态栏信息
                    UpdateStatusStrip();
                    
                    // 如果有任何选择，保持选择状态
                    if (dgvFiles.SelectedRows.Count > 0)
                    {
                        int selectedIndex = dgvFiles.SelectedRows[0].Index;
                        if (selectedIndex >= 0 && selectedIndex < bindingList.Count)
                        {
                            dgvFiles.ClearSelection();
                            dgvFiles.Rows[selectedIndex].Selected = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录异常但不中断程序流程
                Console.WriteLine("UpdateFileList failed: " + ex.Message);
            }
        }
        
        public void RefreshDgvFiles()
        {
            try
            {
                LogDebugInfo("执行RefreshDgvFiles: 强制刷新dgvFiles控件");
                
                // 多种刷新方式组合使用
                dgvFiles.Invalidate(); // 标记为需要重绘
                dgvFiles.Update();     // 立即更新
                dgvFiles.Refresh();    // 刷新显示
                
                // 尝试重新设置数据源
                if (FileBindingList != null)
                {
                    var currentDataSource = dgvFiles.DataSource;
                    dgvFiles.DataSource = null;
                    dgvFiles.DataSource = currentDataSource;
                    
                    LogDebugInfo($"RefreshDgvFiles: 重新设置数据源，当前行数: {FileBindingList.Count}");
                }
                
                Application.DoEvents(); // 处理所有待处理的窗口消息
            }
            catch (Exception ex)
            {
                LogDebugInfo($"RefreshDgvFiles失败: {ex.Message}");
                LogHelper.Error($"刷新数据网格视图时发生错误: {ex}");
                // 如果是UI刷新失败，不影响主要功能，只记录日志
            }
        }

        /// <summary>
        /// 实现IForm1View接口的DataGrid属性
        /// </summary>
        public DataGridView DataGrid => dgvFiles;

        // 实现IForm1View接口的UpdateExcelData方法
        public void UpdateExcelData()
        {
            try
            {
                // 获取Excel导入服务
                var excelImportService = WindowsFormsApp3.Services.ServiceLocator.Instance.GetExcelImportService();
                
                // 检查是否有有效的导入数据
                if (excelImportService.HasValidData())
                {
                    // 更新ExcelImportedData属性
                    ExcelImportedData = excelImportService.ImportedData;
                    
                    // 同时更新列索引字段
                    _excelSearchColumnIndex = excelImportService.SearchColumnIndex;
                    _excelReturnColumnIndex = excelImportService.ReturnColumnIndex;
                    _excelNewColumnIndex = excelImportService.SerialColumnIndex;
                    
                    // 显示导入的数据
                    DisplayImportedExcelData(ExcelImportedData);
                    
                    // 显示数据摘要信息
                    string summary = excelImportService.GetDataSummary();
                    if (!string.IsNullOrEmpty(summary))
                    {
                        // 可以将摘要信息显示在状态栏或专门的标签中
                        statusStrip.Items[0].Text = "Excel数据已导入: " + summary.Split('\n')[0];
                    }
                    
                    // 强制刷新UI
                    this.Refresh();
                    Application.DoEvents();
                    
                    // 特别刷新dgvExcelData控件
                    dgvExcelData.Refresh();
                    Application.DoEvents();
                }
                else
                {
                    // 没有有效数据，清空显示
                    dgvExcelData.DataSource = null;
                    statusStrip.Items[0].Text = "未导入Excel数据";
                    
                    // 清空列索引
                    _excelSearchColumnIndex = -1;
                    _excelReturnColumnIndex = -1;
                    _excelNewColumnIndex = -1;
                    
                    // 强制刷新UI
                    dgvExcelData.Refresh();
                    Application.DoEvents();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新Excel数据时出错: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 记录详细错误信息
                System.Diagnostics.Debug.WriteLine($"更新Excel数据时出错: {ex}");
                
                // 尝试备选方案
                try
                {
                    // 清除现有数据
                    dgvExcelData.DataSource = null;
                    dgvExcelData.Columns.Clear();
                    
                    // 如果有数据，尝试直接绑定
                    if (ExcelImportedData != null)
                    {
                        dgvExcelData.AutoGenerateColumns = true;
                        dgvExcelData.DataSource = ExcelImportedData;
                        dgvExcelData.Refresh();
                        Application.DoEvents();
                    }
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"备选方案更新Excel数据时出错: {fallbackEx}");
                    MessageBox.Show("无法更新Excel数据显示，请重新导入数据。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 刷新Excel数据显示
        /// </summary>
        public void RefreshExcelDataDisplay()
        {
            // 刷新Excel数据网格视图
            dgvExcelData.Refresh();
            Application.DoEvents();
        }
        
        /// <summary>
        /// 刷新视图
        /// </summary>
        public new void Refresh()
        {
            // 调用基类的Refresh方法
            base.Refresh();
            Application.DoEvents();
        }
        
        /// <summary>
        /// 更新输入目录显示
        /// </summary>
        /// <param name="selectedPath">选中的路径</param>
        public void UpdateInputDirDisplay(string selectedPath)
        {
            try
            {
                // 添加选中路径到下拉框并选中
                if (!txtInputDir.Items.Contains(selectedPath))
                {
                    txtInputDir.Items.Add(selectedPath);
                }
                txtInputDir.SelectedItem = selectedPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新输入目录显示时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 显示导出路径管理对话框
        /// </summary>
        /// <param name="exportPaths">当前导出路径</param>
        /// <returns>用户设置的导出路径，如果用户取消则返回null</returns>
        public string ShowExportPathManagerDialog(string exportPaths)
        {
            try
            {
                // 打开SettingsForm并定位到导出路径管理选项卡
                using var settingsForm = new SettingsForm();
                
                // 如果有传入的导出路径数据，先更新到设置中
                if (!string.IsNullOrEmpty(exportPaths))
                {
                    var settings = ApplicationSettingsService.LoadSettings();
                    var pathList = exportPaths.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    settings.ExportPaths = pathList;
                    ApplicationSettingsService.SaveSettings(settings);
                }
                
                // 显示设置窗体
                if (settingsForm.ShowDialog() == DialogResult.OK)
                {
                    // 从设置中获取更新后的导出路径
                    var updatedSettings = ApplicationSettingsService.LoadSettings();
                    return string.Join("|", updatedSettings.ExportPaths);
                }
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("显示导出路径管理对话框时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
        
        /// <summary>
        /// 获取切换模式按钮的文本
        /// </summary>
        /// <returns>按钮文本</returns>
        public string GetToggleModeButtonText()
        {
            return btnToggleMode.Text;
        }
        
        /// <summary>
        /// 设置切换模式按钮的文本
        /// </summary>
        /// <param name="text">按钮文本</param>
        public void SetToggleModeButtonText(string text)
        {
            btnToggleMode.Text = text;
        }
        
        /// <summary>
        /// 获取监控按钮的文本
        /// </summary>
        /// <returns>按钮文本</returns>
        public string GetMonitorButtonText()
        {
            return btnMonitor.Text;
        }
        
        /// <summary>
        /// 设置监控按钮的文本
        /// </summary>
        /// <param name="text">按钮文本</param>
        public void SetMonitorButtonText(string text)
        {
            btnMonitor.Text = text;
        }
        
        /// <summary>
        /// 获取输入目录路径
        /// </summary>
        /// <returns>输入目录路径</returns>
        public string GetInputDirPath()
        {
            return txtInputDir.Text;
        }
        
        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>目录是否存在</returns>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }
        
        /// <summary>
        /// 显示材料设置对话框
        /// </summary>
        public void ShowMaterialSettingsDialog()
        {
            // 重新加载材料设置
            string materialStr = AppSettings.Material;
            materials = materialStr.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrEmpty(m))
                .ToList();
        }

        /// <summary>
        /// 处理材料变更事件
        /// </summary>
        private void OnMaterialsChanged(object sender, EventArgs e)
        {
            try
            {
                LogDebugInfo("OnMaterialsChanged: 材料列表已变更，更新DgvContextMenu材料设置");
                UpdateDgvContextMenuMaterials();
            }
            catch (Exception ex)
            {
                LogDebugInfo("OnMaterialsChanged: 处理材料变更事件时发生错误: " + ex.Message);
            }
        }

        /// <summary>
        /// 更新所有DgvContextMenu的材料列表
        /// </summary>
        private void UpdateDgvContextMenuMaterials()
        {
            // 从MaterialManager获取最新的材料列表
            materials = MaterialManager.Instance.GetMaterials();
            
            // 如果材料列表为空，尝试从设置中加载
            if (materials.Count == 0)
            {
                string materialStr = AppSettings.Material;
                if (!string.IsNullOrEmpty(materialStr))
                {
                    materials = materialStr.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => m.Trim())
                        .Where(m => !string.IsNullOrEmpty(m))
                        .ToList();
                }
            }
            
            // 更新主文件列表的右键菜单材料
            if (_dgvContextMenu != null)
            {
                _dgvContextMenu.SetMaterials(materials);
            }
            
            // 更新Excel数据网格的右键菜单材料
            if (_dgvExcelContextMenu != null)
            {
                _dgvExcelContextMenu.SetMaterials(materials);
            }
            
            LogDebugInfo($"UpdateDgvContextMenuMaterials: 已更新材料列表，共{materials.Count}项");
        }
        
        /// <summary>
        /// 设置文件系统监控器的属性
        /// </summary>
        /// <param name="path">监控路径</param>
        /// <param name="filter">文件过滤器</param>
        /// <param name="includeSubdirectories">是否包含子目录</param>
        /// <param name="enableRaisingEvents">是否启用事件</param>
        public void SetWatcherProperties(string path, string filter, bool includeSubdirectories, bool enableRaisingEvents)
        {
            // 当路径为空时，设置为当前工作目录以避免"目录名无效"错误
            if (string.IsNullOrEmpty(path))
            {
                watcher.Path = Environment.CurrentDirectory;
            }
            else
            {
                watcher.Path = path;
            }
            watcher.Filter = filter;
            watcher.IncludeSubdirectories = includeSubdirectories;
            watcher.EnableRaisingEvents = enableRaisingEvents;
        }
        
        /// <summary>
        /// 显示导入的Excel数据
        /// </summary>
        /// <param name="data">导入的数据</param>
        public void DisplayImportedExcelData(DataTable data)
        {
            if (data != null)
            {
                // 保存导入的数据用于后续生成组合列值
                _excelImportedData = data;
                
                // 获取Excel导入服务并显示数据
                var excelImportService = WindowsFormsApp3.Services.ServiceLocator.Instance.GetExcelImportService();
                if (excelImportService != null)
                {
                    // 先更新数据源
                    dgvExcelData.DataSource = data;
                    // 使用服务来配置和调整列显示
                    excelImportService.DisplayImportedData(dgvExcelData);
                }
                else
                {
                    // 如果服务不可用，直接绑定数据
                    dgvExcelData.DataSource = data;
                }
                
                // 强制刷新UI以确保数据显示
                dgvExcelData.Refresh();
                Application.DoEvents();
                
                // 为Excel数据网格添加列组合列
                AddCompositeColumnToExcelGrid();
                
                // 调整列宽
                AdjustColumnWidths();
                
                // 再次刷新确保所有更改生效
                dgvExcelData.Refresh();
                Application.DoEvents();
            }
        }
        
        /// <summary>
        /// 为Excel数据网格视图添加列组合列
        /// </summary>
        private void AddCompositeColumnToExcelGrid()
        {
            try
            {
                // 检查dgvExcelData是否已有数据
                if (dgvExcelData.DataSource == null || !(dgvExcelData.DataSource is DataTable dataTable))
                    return;
                
                // 使用列组合服务获取设置和处理列组合功能
                (List<string> selectedColumns, string separator) = _compositeColumnService.GetCompositeColumnSettings();
                DataTable tableWithCompositeColumn = _compositeColumnService.AddCompositeColumnToDataTable(dataTable, selectedColumns, separator);
                
                // 更新数据源
                dgvExcelData.DataSource = tableWithCompositeColumn;
                
                // 重要：同时更新用于数据匹配的_excelImportedData字段
                // 这确保在批量模式下使用列组合功能后，数据匹配也能使用包含组合列的数据
                _excelImportedData = tableWithCompositeColumn;
                
                // 确保列组合列可见并设置属性
                if (dgvExcelData.Columns["列组合"] != null)
                {
                    dgvExcelData.Columns["列组合"].Width = 150;
                    dgvExcelData.Columns["列组合"].ReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                _logger?.LogError(ex, "添加列组合列失败");
            }
        }
        
        /// <summary>
        /// 获取Excel导入表单实例
        /// </summary>
        /// <returns>Excel导入表单实例</returns>
        public object GetExcelImportFormInstance()
        {
            return ExcelImportFormInstance;
        }
        
        /// <summary>
        /// 获取Excel数据网格视图控件
        /// </summary>
        /// <returns>Excel数据网格视图控件</returns>
        public DataGridView GetDgvExcelData()
        {
            return dgvExcelData;
        }
        
        /// <summary>
        /// 设置Excel导入表单实例
        /// </summary>
        /// <param name="instance">Excel导入表单实例</param>
        public void SetExcelImportFormInstance(object instance)
        {
            ExcelImportFormInstance = instance as ExcelImportForm;
        }

        private void InitializeTrayMenu()
        {
            trayMenu = new ContextMenuStrip();

            // 开始监控菜单项
            var startMonitorItem = new ToolStripMenuItem("开始监控");
            startMonitorItem.Click += (sender, e) => BtnMonitor_Click(sender, e);
            trayMenu.Items.Add(startMonitorItem);

            // 停止监控菜单项
            var stopMonitorItem = new ToolStripMenuItem("停止监控");
            stopMonitorItem.Click += (sender, e) => BtnMonitor_Click(sender, e);
            trayMenu.Items.Add(stopMonitorItem);

            // 分隔线
            trayMenu.Items.Add(new ToolStripSeparator());

            // 复制/剪切模式切换
            var toggleModeItem = new ToolStripMenuItem("切换复制/剪切模式");
            toggleModeItem.Click += (sender, e) => BtnToggleMode_Click(sender, e);
            trayMenu.Items.Add(toggleModeItem);

            // 手动模式模式切换
            var immediateRenameItem = new ToolStripMenuItem("切换手动模式模式");
            immediateRenameItem.Click += (sender, e) => btnImmediateRename_Click(sender, e);
            trayMenu.Items.Add(immediateRenameItem);

            // 分隔线
            trayMenu.Items.Add(new ToolStripSeparator());

            // 撤销操作菜单项
            var undoItem = new ToolStripMenuItem("撤销 (Ctrl+Z)");
            undoItem.Click += (sender, e) => OnUndoClick(sender, e);
            trayMenu.Items.Add(undoItem);

            // 重做操作菜单项
            var redoItem = new ToolStripMenuItem("重做 (Ctrl+Y)");
            redoItem.Click += (sender, e) => OnRedoClick(sender, e);
            trayMenu.Items.Add(redoItem);

            // 操作历史菜单项
            var historyItem = new ToolStripMenuItem("操作历史...");
            historyItem.Click += (sender, e) => OnOperationHistoryClick(sender, e);
            trayMenu.Items.Add(historyItem);

            // 分隔线
            trayMenu.Items.Add(new ToolStripSeparator());

            // 退出菜单项
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (sender, e) =>
            {
                // 设置允许关闭标志，确保真正退出
                _allowClose = true;
                
                LogDebugInfo($"托盘退出菜单点击: 设置_allowClose=true，尝试关闭窗口...");
                
                // 先尝试正常关闭
                this.Close();
                
                // 如果正常关闭失败，先执行自动保存再强制退出
                LogDebugInfo($"托盘退出菜单点击: this.Close()无效，执行自动保存后强制退出...");
                PerformAutoSave();
                Application.Exit();
            };
            trayMenu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = trayMenu;
            UpdateTrayMenuItems();
        }

        public void UpdateTrayMenuItems()
        {
            if (trayMenu == null || trayMenu.Items.Count < 6) return;

            // 更新监控状态菜单项
            trayMenu.Items[0].Enabled = !isMonitoring;
            trayMenu.Items[1].Enabled = isMonitoring;



            // 更新手动模式模式菜单项文本
            var immediateRenameItem = (ToolStripMenuItem)trayMenu.Items[4];
            immediateRenameItem.Text = isImmediateRenameActive ? "关闭手动模式模式" : "开启手动模式模式";
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            // 调试输出
            LogHelper.Debug("TrayIcon_DoubleClick: 双击托盘图标恢复窗口");
            LogHelper.Debug("TrayIcon_DoubleClick: 操作前 - WindowState=" + WindowState + ", Visible=" + this.Visible + ", trayIcon.Visible=" + trayIcon.Visible);
            LogDebugInfo($"TrayIcon_DoubleClick: 双击托盘图标恢复窗口");
            LogDebugInfo($"TrayIcon_DoubleClick: 操作前 - WindowState={WindowState}, Visible={this.Visible}, trayIcon.Visible={trayIcon.Visible}");
            
            // 双击托盘图标恢复窗口
            this.Show();
            WindowState = FormWindowState.Normal;
            trayIcon.Visible = false;
            
            LogHelper.Debug("TrayIcon_DoubleClick: 操作后 - WindowState=" + WindowState + ", Visible=" + this.Visible + ", trayIcon.Visible=" + trayIcon.Visible);
            LogDebugInfo($"TrayIcon_DoubleClick: 操作后 - WindowState={WindowState}, Visible={this.Visible}, trayIcon.Visible={trayIcon.Visible}");
        }

        /// <summary>
        /// 处理键盘快捷键
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl+Z: 撤销
            if (keyData == (Keys.Control | Keys.Z))
            {
                OnUndoClick(this, EventArgs.Empty);
                return true;
            }

            // Ctrl+Y: 重做
            if (keyData == (Keys.Control | Keys.Y))
            {
                OnRedoClick(this, EventArgs.Empty);
                return true;
            }

            // Ctrl+Shift+Z: 也可以重做（一些用户的习惯）
            if (keyData == (Keys.Control | Keys.Shift | Keys.Z))
            {
                OnRedoClick(this, EventArgs.Empty);
                return true;
            }

            // 其他快捷键交给基类处理
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_CLOSE = 0x0010;
            
            // 拦截窗口关闭消息
            if (m.Msg == WM_CLOSE)
            {
                LogDebugInfo($"=== WndProc: 捕获到WM_CLOSE消息! ===");
                LogDebugInfo($"WndProc: 用户点击X按钮，开始处理关闭逻辑...");
                
                // 不调用base.WndProc，手动处理关闭逻辑
                HandleWindowClose();
                return; // 不调用基类处理，防止默认关闭行为
            }
            
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == toggleHotkeyId)
                {
                    // 切换窗口最小化状态
                    if (this.WindowState == FormWindowState.Minimized || !this.Visible)
                    {
                        // 调试输出
                        LogHelper.Debug("WndProc: 恢复窗口 - WindowState=" + this.WindowState + ", Visible=" + this.Visible);
                        LogDebugInfo($"WndProc: 恢复窗口 - WindowState={this.WindowState}, Visible={this.Visible}");
                        
                        this.Show();
                        this.WindowState = FormWindowState.Normal;
                        this.Activate(); // 激活窗口并置于前台
                        trayIcon.Visible = false;
                        
                        LogHelper.Debug("WndProc: 恢复后 - WindowState=" + this.WindowState + ", Visible=" + this.Visible + ", trayIcon.Visible=" + trayIcon.Visible);
                        LogDebugInfo($"WndProc: 恢复后 - WindowState={this.WindowState}, Visible={this.Visible}, trayIcon.Visible={trayIcon.Visible}");
                    }
                    else
                    {
                        // 调试输出
                        LogHelper.Debug("WndProc: 最小化窗口 - WindowState=" + this.WindowState + ", Visible=" + this.Visible);
                        LogDebugInfo($"WndProc: 最小化窗口 - WindowState={this.WindowState}, Visible={this.Visible}");
                        
                        this.WindowState = FormWindowState.Minimized;
                        this.Hide(); // 隐藏窗口以移除任务栏图标
                        trayIcon.Visible = true;
                        
                        LogHelper.Debug("WndProc: 最小化后 - WindowState=" + this.WindowState + ", Visible=" + this.Visible + ", trayIcon.Visible=" + trayIcon.Visible);
                        LogDebugInfo($"WndProc: 最小化后 - WindowState={this.WindowState}, Visible={this.Visible}, trayIcon.Visible={trayIcon.Visible}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理窗口关闭逻辑
        /// </summary>
        private void HandleWindowClose()
        {
            LogDebugInfo($"HandleWindowClose: 开始处理窗口关闭...");
            
            if (!_allowClose)
            {
                LogDebugInfo($"HandleWindowClose: 不允许直接关闭，执行最小化到托盘逻辑");
                
                // 先执行自动保存
                PerformAutoSave();
                
                // 最小化到托盘
                this.WindowState = FormWindowState.Minimized;
                this.Hide();
                trayIcon.Visible = true;
                
                LogDebugInfo($"HandleWindowClose: 最小化到托盘完成 - WindowState={this.WindowState}, trayIcon.Visible={trayIcon.Visible}");
            }
            else
            {
                LogDebugInfo($"HandleWindowClose: 允许关闭，执行自动保存后退出");
                
                // 执行自动保存
                PerformAutoSave();
                
                // 真正退出应用程序
                Application.Exit();
            }
        }

        private bool _allowClose = false;

        // Form1_FormClosing方法已移动到Presenter.HandleFormClosing中处理
        // 这里保留原有方法作为备用，但不再使用
        /*
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 调试输出 - 记录FormClosing事件开始
            System.Diagnostics.Debug.WriteLine($"Form1_FormClosing: 事件开始 - CloseReason={e.CloseReason}, _allowClose={_allowClose}, Cancel={e.Cancel}");
            LogDebugInfo($"Form1_FormClosing: 事件开始 - CloseReason={e.CloseReason}, _allowClose={_allowClose}, Cancel={e.Cancel}");
            
            // 触发FormClosing事件（通知Presenter）
            FormClosing?.Invoke(this, e);
            
            // 调试输出 - 记录Presenter处理后的状态
            System.Diagnostics.Debug.WriteLine($"Form1_FormClosing: Presenter处理后 - CloseReason={e.CloseReason}, _allowClose={_allowClose}, Cancel={e.Cancel}");
            LogDebugInfo($"Form1_FormClosing: Presenter处理后 - CloseReason={e.CloseReason}, _allowClose={_allowClose}, Cancel={e.Cancel}");
            
            if (_isSaving)
            {
                LogDebugInfo($"Form1_FormClosing: 跳过，正在保存中");
                return;
            }
            
            // 如果Presenter已经取消了关闭（最小化到托盘），则不再处理
            if (e.Cancel)
            {
                LogDebugInfo($"Form1_FormClosing: Presenter已取消关闭，最小化到托盘，显示托盘图标");
                // 确保托盘图标显示
                trayIcon.Visible = true;
                return;
            }

            // 如果是真正的退出（如通过菜单退出），先执行自动保存
            PerformAutoSave();
            
            // 允许程序退出
            System.Diagnostics.Debug.WriteLine($"Form1_FormClosing: 允许程序退出 - CloseReason={e.CloseReason}, _allowClose={_allowClose}");
            LogDebugInfo($"Form1_FormClosing: 允许程序退出 - CloseReason={e.CloseReason}, _allowClose={_allowClose}");
        }
        */
        
        /// <summary>
        /// 手动测试FormClosing事件触发方法
        /// </summary>
        public void TestFormClosingEvent()
        {
            LogDebugInfo($"TestFormClosingEvent: 开始手动测试FormClosing事件");
            
            // 创建测试事件参数
            var testEventArgs = new FormClosingEventArgs(CloseReason.UserClosing, false);
            
            // 手动触发FormClosing事件
            LogDebugInfo($"TestFormClosingEvent: 触发FormClosing事件...");
            FormClosing?.Invoke(this, testEventArgs);
            
            LogDebugInfo($"TestFormClosingEvent: FormClosing事件已触发，事件参数: Cancel={testEventArgs.Cancel}");
        }
        public void PerformAutoSave()
        {
            LogDebugInfo($"PerformAutoSave: 开始执行自动保存检查");
            
            // 清理MaterialSelectForm实例
            CleanupMaterialSelectForm();
            
            if (cmbJsonFiles.SelectedItem != null && dgvFiles.DataSource is BindingList<FileRenameInfo> data && data.Count > 0)
            {
                try
                {
                    var jsonPath = Path.Combine(AppDataPathManager.SavedGridsDirectory, $"{cmbJsonFiles.SelectedItem}.json");
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(jsonPath, json, System.Text.Encoding.UTF8);
                    
                    // 调试输出
                    LogHelper.Debug("PerformAutoSave: 自动保存成功 - 文件: " + cmbJsonFiles.SelectedItem + ".json, 数据行数: " + data.Count);
                    LogDebugInfo($"PerformAutoSave: 自动保存成功 - 文件: {cmbJsonFiles.SelectedItem}.json, 数据行数: {data.Count}");
                }
                catch (Exception ex)
                {
                    LogHelper.Debug("PerformAutoSave: 自动保存失败 - " + ex.Message);
                    LogDebugInfo($"PerformAutoSave: 自动保存失败 - {ex.Message}");
                    MessageBox.Show($"自动保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                // 调试输出 - 没有数据需要保存
                string reason = "";
                if (cmbJsonFiles.SelectedItem == null)
                    reason = "未选择JSON文件";
                else if (dgvFiles.DataSource == null)
                    reason = "数据源为空";
                else if (!(dgvFiles.DataSource is BindingList<FileRenameInfo>))
                    reason = "数据源类型不匹配";
                else if ((dgvFiles.DataSource as BindingList<FileRenameInfo>).Count == 0)
                    reason = "数据为空";
                
                LogHelper.Debug("PerformAutoSave: 跳过自动保存 - 原因: " + reason);
                LogDebugInfo($"PerformAutoSave: 跳过自动保存 - 原因: {reason}");
            }
        }

        // 获取日志服务实例
        private WindowsFormsApp3.Interfaces.ILogger _logger => ServiceLocator.Instance.Logger;
        
        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="message">日志消息</param>
        private void LogDebugInfo(string message)
        {
            try
            {
                // 使用统一的LogHelper记录日志
                LogHelper.Debug(message);
            }
            catch (Exception ex)
            {
                // 日志记录失败，但不影响程序运行
                System.Diagnostics.Debug.WriteLine($"日志记录失败: {ex.Message}");
            }
        }

        private void UnregisterHotkeys()
        {
            try
            {
                if (toggleHotkeyId > 0)
                {
                    UnregisterHotKey(this.Handle, toggleHotkeyId);
                    if (toggleHotkeyAtom != IntPtr.Zero)
                    {
                        GlobalDeleteAtom(toggleHotkeyAtom);
                    }
                }
            }
            catch (Exception ex)
            {
                // 热键注销失败，但不影响程序退出
                System.Diagnostics.Debug.WriteLine($"热键注销失败: {ex.Message}");
            }
        }

        private void RegisterHotkeys()
        {
            try
            {
                // 从设置中获取切换最小化快捷键
                string toggleHotkey = AppSettings.ToggleMinimizeHotkey;
                if (!string.IsNullOrEmpty(toggleHotkey))
                {
                    // 生成唯一的原子值
                    toggleHotkeyAtom = GlobalAddAtom(Guid.NewGuid().ToString());
                    if (toggleHotkeyAtom == IntPtr.Zero)
                    {
                        MessageBox.Show("无法创建全局原子，热键注册失败。");
                        return;
                    }
                    // 在Win7系统上，GlobalAddAtom返回的原子值可能大于int.MaxValue，直接使用原子值作为热键ID
                    // 而不是转换为int，以避免算术运算溢出
                    int hotkeyId = (int)(toggleHotkeyAtom.ToInt64() & 0xFFFF); // 只取低16位，确保在int范围内

                    // 解析快捷键
                    if (ParseHotkey(toggleHotkey, out uint modifiers, out uint key))
                    {
                        // 注册热键
                        bool registerSuccess = RegisterHotKey(this.Handle, hotkeyId, modifiers, key);
                        if (!registerSuccess)
                        {
                            MessageBox.Show("快捷键注册失败，可能已被其他程序占用。");
                        }
                        else
                        {
                            toggleHotkeyId = hotkeyId; // 只有在注册成功后才更新toggleHotkeyId
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("热键注册失败: " + ex.Message);
            }
        }

        private bool ParseHotkey(string hotkey, out uint modifiers, out uint key)
        {
            modifiers = 0;
            key = 0;

            if (string.IsNullOrEmpty(hotkey))
                return false;

            string[] parts = hotkey.Split('+');
            foreach (string part in parts)
            {
                string lowerPart = part.Trim().ToLower();
                switch (lowerPart)
                {
                    case "ctrl":
                        modifiers |= 2;
                        break;
                    case "alt":
                        modifiers |= 1;
                        break;
                    case "shift":
                        modifiers |= 4;
                        break;
                    default:
                        if (lowerPart.Length > 0)
                        {
                            key = (uint)Char.ToUpper(lowerPart[0]);
                        }
                        break;
                }
            }

            return key != 0;
        }

        private void InitializeEvents()
        {
            // 文件系统监控事件
            watcher.Created += Watcher_Created;
            watcher.Renamed += Watcher_Renamed;
            watcher.Error += Watcher_Error;
        }

        public void InitializeDataGridView()
        {
            // 初始化dgvExcelData的空数据
            _excelImportedData = new DataTable();
            _excelImportedData.Columns.Add("请导入Excel文件"); // 提示性列名
            for (int i = 0; i < 100; i++)
            {
                _excelImportedData.Rows.Add(_excelImportedData.NewRow());
            }
            dgvExcelData.DataSource = _excelImportedData;
            dgvExcelData.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvFiles.AutoGenerateColumns = false;
            // 初始化999行空数据
            var fileBindingList = new BindingList<FileRenameInfo>();
            fileBindingList.ListChanged += FileBindingList_ListChanged;
            for (int i = 0; i < 999; i++)
            {
                fileBindingList.Add(new FileRenameInfo()); // 假设FileRenameInfo有默认构造函数
            }
            dgvFiles.DataSource = fileBindingList;

            // 应用性能优化
            OptimizeDataGridViewPerformance();
            dgvFiles.CellFormatting += DataGridView_CellFormatting;
            dgvExcelData.CellFormatting += DataGridView_CellFormatting;
            dgvExcelData.RowHeadersWidth = 30; // 固定行头宽度为30
            dgvExcelData.AllowUserToAddRows = false; // 禁用添加新行，隐藏星号
            dgvExcelData.RowPostPaint += dgvExcelData_RowPostPaint;
            dgvFiles.RowHeadersWidth = 30; // 固定行头宽度为40
            dgvFiles.RowPostPaint += dgvFiles_RowPostPaint;
            dgvFiles.UserDeletingRow += dgvFiles_UserDeletingRow; // 添加删除确认事件
            dgvFiles.RowHeadersVisible = true; // 确保行头可见
            dgvFiles.CellPainting += DataGridView_CellPainting; // 重命名事件处理方法
            dgvExcelData.CellPainting += DataGridView_CellPainting; // 添加dgvExcelData的事件订阅

            // 初始化右键菜单
            _dgvContextMenu = new DgvContextMenu(dgvFiles, _logger, () => !isImmediateRenameActive);
            _dgvExcelContextMenu = new DgvContextMenu(dgvExcelData, _logger, () => !isImmediateRenameActive);

            // 设置材料列表
            UpdateDgvContextMenuMaterials();
            // 保留您现有的RowPostPaint事件
            dgvExcelData.CellPainting += (sender, e) =>
            {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0) // 仅处理列标题，隐藏排序三角形
                {
                    e.PaintBackground(e.CellBounds, true);
                    TextRenderer.DrawText(
                        e.Graphics,
                        dgvExcelData.Columns[e.ColumnIndex].HeaderText,
                        dgvExcelData.ColumnHeadersDefaultCellStyle.Font,
                        e.CellBounds,
                        dgvExcelData.ColumnHeadersDefaultCellStyle.ForeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                    );
                    e.Handled = true;
                }
            };

            // 确保序号列绑定到SerialNumber属性
            if (dgvFiles.Columns["colSerialNumber"] is DataGridViewTextBoxColumn serialNumberColumn)
            {
                serialNumberColumn.DataPropertyName = "SerialNumber";
            }

            // 添加页数列（如果不存在）
            if (dgvFiles.Columns["colPageCount"] == null)
            {
                DataGridViewTextBoxColumn pageCountColumn = new DataGridViewTextBoxColumn();
                pageCountColumn.Name = "colPageCount";
                pageCountColumn.HeaderText = "页数";
                pageCountColumn.DataPropertyName = "PageCount";
                pageCountColumn.Width = 60;
                pageCountColumn.ReadOnly = true;
                pageCountColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // 设置为固定宽度模式
                dgvFiles.Columns.Add(pageCountColumn);
            }

            // 添加列组合列（如果不存在）
            if (dgvFiles.Columns["colCompositeColumn"] == null)
            {
                DataGridViewTextBoxColumn compositeColumn = new DataGridViewTextBoxColumn();
                compositeColumn.Name = "colCompositeColumn";
                compositeColumn.HeaderText = "列组合";
                compositeColumn.DataPropertyName = "CompositeColumn";
                compositeColumn.Width = 150;
                compositeColumn.ReadOnly = true;
                compositeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // 设置为固定宽度模式
                dgvFiles.Columns.Add(compositeColumn);
            }

            // 设置所有列的排序模式为Automatic
            foreach (DataGridViewColumn column in dgvFiles.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.Automatic;
            }

            // 批量模式下允许编辑单元格
            UpdateDgvFilesEditMode();

            // 添加单元格值变化事件处理
            dgvFiles.CellValueChanged += DgvFiles_CellValueChanged;
            dgvFiles.CellBeginEdit += DgvFiles_CellBeginEdit;
            dgvFiles.ColumnHeaderMouseClick += dgvFiles_ColumnHeaderMouseClick;
            // 添加撤销和恢复快捷键
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        // 单元格开始编辑事件
        private void DgvFiles_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            try
            {
                // 在批量模式下允许编辑特定列
                if (!isImmediateRenameActive) // 批量模式
                {
                    LogDebugInfo($"CellBeginEdit: 行={e.RowIndex}, 列={e.ColumnIndex}, 列名={dgvFiles.Columns[e.ColumnIndex].Name}");

                    // 序号列和原文件名列通常不允许直接编辑
                    if (dgvFiles.Columns[e.ColumnIndex].Name == "colSerialNumber" ||
                        dgvFiles.Columns[e.ColumnIndex].Name == "colOriginalName" ||
                        dgvFiles.Columns[e.ColumnIndex].Name == "ColTime")
                    {
                        e.Cancel = true;
                        LogDebugInfo($"取消编辑列: {dgvFiles.Columns[e.ColumnIndex].Name}");
                    }
                }
                else // 手动模式
                {
                    // 手动模式下不允许编辑任何单元格
                    e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                LogDebugInfo($"CellBeginEdit 处理异常: {ex.Message}");
                e.Cancel = true;
            }
        }

        // 单元格值变化事件
        private void DgvFiles_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // 触发CellValueChanged事件
            CellValueChanged?.Invoke(this, e);

            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                if (dgvFiles.DataSource is BindingList<FileRenameInfo> bindingList)
                {
                    var item = bindingList[e.RowIndex];
                    var column = dgvFiles.Columns[e.ColumnIndex];
                    var propertyName = column.DataPropertyName;

                    // 获取旧值和新值
                    object oldValue = null;
                    object newValue = dgvFiles.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;

                    // 创建编辑命令
                    var command = new EditCellCommand(dgvFiles, e.RowIndex, e.ColumnIndex, oldValue, newValue);
                    _undoRedoService.ExecuteCommand(command);
                }
            }
        }

        // 列标题点击事件 - 实现排序功能
        private void dgvFiles_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            // 触发ColumnHeaderMouseClick事件
            ColumnHeaderMouseClick?.Invoke(this, e);
            
            if (dgvFiles.DataSource is BindingList<FileRenameInfo> bindingList && e.ColumnIndex >= 0)
            {
                var column = dgvFiles.Columns[e.ColumnIndex];
                string propertyName = column.DataPropertyName;

                if (string.IsNullOrEmpty(propertyName))
                    return;

                // 保存当前排序前的数据顺序
                List<FileRenameInfo> originalOrder = new List<FileRenameInfo>(bindingList);
                List<int> originalIndices = Enumerable.Range(0, bindingList.Count).ToList();

                // 确定排序方向
                ListSortDirection direction = ListSortDirection.Ascending;
                if (column.HeaderCell.SortGlyphDirection == SortOrder.Ascending)
                {
                    direction = ListSortDirection.Descending;
                }

                // 创建排序命令
                var sortOrder = direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;
                var command = new SortCommand(dgvFiles, e.ColumnIndex, sortOrder);
                _undoRedoService.ExecuteCommand(command);

                // 执行排序
                command.Execute();
            }
        }

        // 键盘按键事件
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // 触发KeyDown事件
            KeyDown?.Invoke(this, e);
            
            // 撤销: Ctrl+Z
            if (e.Control && e.KeyCode == Keys.Z)
            {
                _undoRedoService.Undo();
                e.Handled = true;
            }
            // 恢复: Ctrl+Y
            else if (e.Control && e.KeyCode == Keys.Y)
            {
                _undoRedoService.Redo();
                e.Handled = true;
            }
            // 复制: Ctrl+C
            else if (e.Control && e.KeyCode == Keys.C)
            {
                _dgvContextMenu?.Copy();
                e.Handled = true;
            }
            // 剪切: Ctrl+X
            else if (e.Control && e.KeyCode == Keys.X)
            {
                _dgvContextMenu?.Cut();
                e.Handled = true;
            }
            // 粘贴: Ctrl+V
            else if (e.Control && e.KeyCode == Keys.V)
            {
                _dgvContextMenu?.Paste();
                e.Handled = true;
            }
            // 测试布局重命名功能: Ctrl+Shift+L
            else if (e.Control && e.Shift && e.KeyCode == Keys.L)
            {
                // 测试设置布局结果为 3行 x 4列
                SettingsForm.TestSetLayoutResults(3, 4);
                e.Handled = true;
            }
        }

        public void UpdateDgvFilesEditMode()
        {
            // 批量模式下允许编辑单元格
            dgvFiles.ReadOnly = isImmediateRenameActive;

            // 确保在批量模式下DataGridView能够正确响应事件
            if (!isImmediateRenameActive) // 批量模式
            {
                dgvFiles.Enabled = true;
                dgvFiles.AllowUserToDeleteRows = true;
                dgvFiles.AllowUserToResizeRows = true;
                dgvFiles.AllowUserToOrderColumns = true;

                // 确保编辑模式设置正确
                dgvFiles.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
                dgvFiles.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgvFiles.MultiSelect = true;

                // 在批量模式下，设置列的编辑权限
                foreach (DataGridViewColumn column in dgvFiles.Columns)
                {
                    // 在批量模式下，只允许编辑特定列
                    if (column.Name == "colMaterial" ||
                        column.Name == "colQuantity" ||
                        column.Name == "colDimensions" ||
                        column.Name == "colOrderNumber" ||
                        column.Name == "ColNotes")
                    {
                        column.ReadOnly = false;
                    }
                    else
                    {
                        // 序号列、原文件名列、新文件名列、正则结果列、时间列保持只读
                        column.ReadOnly = true;
                    }
                }

                // 确保右键菜单正常工作
                if (_dgvContextMenu == null)
                {
                    _dgvContextMenu = new DgvContextMenu(dgvFiles, _logger, () => !isImmediateRenameActive);
                    UpdateDgvContextMenuMaterials();
                }

                LogDebugInfo("DataGridView已切换到批量模式：允许编辑，启用右键菜单，已设置列编辑权限");
            }
            else // 手动模式
            {
                // 手动模式下，所有列都保持只读
                foreach (DataGridViewColumn column in dgvFiles.Columns)
                {
                    column.ReadOnly = true;
                }

                dgvFiles.SelectionMode = DataGridViewSelectionMode.CellSelect;
                dgvFiles.MultiSelect = true;

                LogDebugInfo("DataGridView已切换到手动模式：只读模式，已设置所有列为只读");
            }
        }

        // 查找第一个空文件行
        private int FindFirstEmptyFileRow(BindingList<FileRenameInfo> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (string.IsNullOrEmpty(item.OriginalName) &&
                    string.IsNullOrEmpty(item.NewName) &&
                    string.IsNullOrEmpty(item.Status) &&
                    string.IsNullOrEmpty(item.ErrorMessage) &&
                    string.IsNullOrEmpty(item.FullPath) &&
                    string.IsNullOrEmpty(item.RegexResult) &&
                    string.IsNullOrEmpty(item.OrderNumber) &&
                    string.IsNullOrEmpty(item.Material) &&
                    string.IsNullOrEmpty(item.Quantity) &&
                    string.IsNullOrEmpty(item.Dimensions) &&
                    string.IsNullOrEmpty(item.Process) &&
                    string.IsNullOrEmpty(item.Time))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 更新网格数据
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="processedData">处理后的数据</param>
        private void UpdateGridData(FileInfo fileInfo, ProcessedFileData processedData)
        {
            try
            {
                if (dgvFiles.DataSource is BindingList<FileRenameInfo> bindingList)
                {
                    int emptyRowIndex = FindFirstEmptyFileRow(bindingList);
                    if (emptyRowIndex != -1)
                    {
                        // 更新现有空行
                        UpdateExistingRow(bindingList[emptyRowIndex], fileInfo, processedData);
                    }
                    else
                    {
                        // 添加新行
                        var newRow = CreateNewFileRenameInfo(fileInfo, processedData);
                        bindingList.Add(newRow);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新网格数据时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新现有的行数据
        /// </summary>
        /// <param name="row">网格行对象</param>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="processedData">处理后的数据</param>
        private void UpdateExistingRow(FileRenameInfo row, FileInfo fileInfo, ProcessedFileData processedData)
        {
            row.OriginalName = fileInfo.Name;
            row.NewName = processedData.NewFileName;
            row.FullPath = processedData.DestinationPath;
            row.RegexResult = processedData.RegexResult;
            row.OrderNumber = processedData.OrderNumber;
            row.Material = processedData.Material;
            row.Quantity = processedData.Quantity;
            row.Dimensions = processedData.Dimensions;
            row.Process = processedData.Process;
            row.Time = DateTime.Now.ToString("MM-dd");
            row.SerialNumber = processedData.SerialNumber;
            row.CompositeColumn = processedData.CompositeColumn; // 修复：添加列组合值的设置
            row.LayoutRows = processedData.LayoutRows;    // ✅ 新增：设置行数
            row.LayoutColumns = processedData.LayoutColumns; // ✅ 新增：设置列数
            
            LogDebugInfo($"UpdateExistingRow: 设置 CompositeColumn = '{processedData.CompositeColumn}'");
            
            // 处理PDF文件的特殊需求
            if (fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                row.PageCount = GetPdfPageCount(fileInfo.FullName);
            }
        }

        /// <summary>
        /// 创建新的文件重命名信息对象
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="processedData">处理后的数据</param>
        /// <returns>新的FileRenameInfo对象</returns>
        private FileRenameInfo CreateNewFileRenameInfo(FileInfo fileInfo, ProcessedFileData processedData)
        {
            var newRow = new FileRenameInfo
            {
                OriginalName = fileInfo.Name,
                NewName = processedData.NewFileName,
                FullPath = processedData.DestinationPath,
                RegexResult = processedData.RegexResult,
                OrderNumber = processedData.OrderNumber,
                Material = processedData.Material,
                Quantity = processedData.Quantity,
                Dimensions = processedData.Dimensions,
                Process = processedData.Process,
                Time = DateTime.Now.ToString("MM-dd"),
                SerialNumber = processedData.SerialNumber,
                LayoutRows = processedData.LayoutRows,    // ✅ 新增：设置行数
                LayoutColumns = processedData.LayoutColumns, // ✅ 新增：设置列数
                CompositeColumn = processedData.CompositeColumn, // 修复：添加列组合值的设置
                PageCount = fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) 
                    ? GetPdfPageCount(fileInfo.FullName) 
                    : null
            };
            
            LogDebugInfo($"CreateNewFileRenameInfo: 创建新行, CompositeColumn = '{processedData.CompositeColumn}'");
            return newRow;
        }

        // 添加或更新文件行（保留原有的签名以兼容现有代码）
        private void AddOrUpdateFileRow(FileInfo fileInfo, string newFileName, string destPath, string regexPart, string orderNumber, string selectedMaterial, string quantity, string finalDimensions, string fixedField, string serialNumber)
        {
            // 创建 ProcessedFileData 对象
            var processedData = new ProcessedFileData
            {
                NewFileName = newFileName,
                DestinationPath = destPath,
                RegexResult = regexPart,
                OrderNumber = orderNumber,
                Material = selectedMaterial,
                Quantity = quantity,
                Dimensions = finalDimensions,
                Process = fixedField,
                SerialNumber = serialNumber,
                CompositeColumn = string.Empty // 此方法主要用于即时重命名，列组合值可能为空
            };

            // 使用新的网格更新方法
            UpdateGridData(fileInfo, processedData);
        }

        // 获取有数据行的数量
        private int GetDataRowCount(BindingList<FileRenameInfo> list)
        {
            int count = 0;
            foreach (var item in list)
            {
                if (!string.IsNullOrEmpty(item.OriginalName) ||
                    !string.IsNullOrEmpty(item.NewName) ||
                    !string.IsNullOrEmpty(item.FullPath))
                {
                    count++;
                }
            }
            return count;
        }




        // 处理单元格绘制，确保列头文本居中且排序三角形正确显示
        private void DataGridView_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // 处理行头单元格 - 隐藏三角形
            if (e.RowIndex >= 0 && e.ColumnIndex == -1)
            {
                // 绘制默认背景（包括选中状态背景色）
                e.PaintBackground(e.CellBounds, true);

                // 阻止绘制默认内容（包括三角形）
                e.Handled = true;
            }
            // 处理列头单元格 - 确保文本居中和三角形显示
            else if (e.RowIndex == -1 && e.ColumnIndex >= 0)
            {
                // 绘制默认背景
                e.PaintBackground(e.CellBounds, true);

                // 获取列头文本
                string columnHeaderText = dgvFiles.Columns[e.ColumnIndex].HeaderText;

                // 计算文本居中的位置
                SizeF textSize = e.Graphics.MeasureString(columnHeaderText, dgvFiles.ColumnHeadersDefaultCellStyle.Font);
                float textX = e.CellBounds.Left + (e.CellBounds.Width - textSize.Width) / 2;
                float textY = e.CellBounds.Top + (e.CellBounds.Height - textSize.Height) / 2;

                // 绘制文本
                e.Graphics.DrawString(
                    columnHeaderText,
                    dgvFiles.ColumnHeadersDefaultCellStyle.Font,
                    new SolidBrush(dgvFiles.ColumnHeadersDefaultCellStyle.ForeColor),
                    new PointF(textX, textY));

                // 处理排序三角形
                if (dgvFiles.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection != SortOrder.None)
                {
                    // 获取三角形的大小和位置
                    int glyphSize = 10;
                    int glyphX = e.CellBounds.Right - glyphSize - 5;
                    int glyphY = e.CellBounds.Top + (e.CellBounds.Height - glyphSize) / 2;

                    // 绘制排序三角形
                    using (Pen pen = new Pen(dgvFiles.ColumnHeadersDefaultCellStyle.ForeColor))
                    {
                        Point[] points = new Point[3];
                        if (dgvFiles.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.Ascending)
                        {
                            points[0] = new Point(glyphX, glyphY);
                            points[1] = new Point(glyphX + glyphSize, glyphY);
                            points[2] = new Point(glyphX + glyphSize / 2, glyphY + glyphSize);
                        }
                        else
                        {
                            points[0] = new Point(glyphX, glyphY + glyphSize);
                            points[1] = new Point(glyphX + glyphSize, glyphY + glyphSize);
                            points[2] = new Point(glyphX + glyphSize / 2, glyphY);
                        }
                        e.Graphics.FillPolygon(new SolidBrush(dgvFiles.ColumnHeadersDefaultCellStyle.ForeColor), points);
                    }
                }

                // 阻止默认绘制
                e.Handled = true;
            }
        }

        // 您原有的行号绘制代码（保留不变）
        private void dgvFiles_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            string rowNumber = (e.RowIndex + 1).ToString();

            using var centerFormat = new StringFormat();
            centerFormat.Alignment = StringAlignment.Center;
            centerFormat.LineAlignment = StringAlignment.Center;

            var headerBounds = new Rectangle(
                e.RowBounds.Left,
                e.RowBounds.Top,
                grid.RowHeadersWidth,
                e.RowBounds.Height
            );

            e.Graphics.DrawString(
                rowNumber,
                this.Font,
                SystemBrushes.ControlDark,
                headerBounds,
                centerFormat
            );
        }


        /// <summary>
        /// 删除行时的确认处理
        /// </summary>
        private void dgvFiles_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            // 检查选中的行数
            int selectedRowCount = dgvFiles.SelectedRows.Count;
            
            // 根据选中的行数显示不同的确认消息
            string message = selectedRowCount > 1 
                ? "确定要删除所有选中的行吗？"
                : "确定要删除选中的行吗？";
            
            // 显示确认对话框
            DialogResult result = MessageBox.Show(
                message,
                "确认删除",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            // 如果用户选择取消，则取消删除操作
            if (result == DialogResult.No)
            {
                e.Cancel = true;
            }
            else if (dgvFiles.DataSource is BindingList<FileRenameInfo> bindingList)
            {
                if (selectedRowCount > 1)
                {
                    // 取消当前删除事件
                    e.Cancel = true;
                    
                    // 记录要删除的行和索引
                    List<FileRenameInfo> deletedRows = new List<FileRenameInfo>();
                    List<int> deletedIndices = new List<int>();
                    
                    // 创建要删除的行的集合
                    List<DataGridViewRow> rowsToDelete = new List<DataGridViewRow>();
                    foreach (DataGridViewRow row in dgvFiles.SelectedRows)
                    {
                        if (!row.IsNewRow && row.Index < bindingList.Count)
                        {
                            rowsToDelete.Add(row);
                            deletedRows.Add(bindingList[row.Index]);
                            deletedIndices.Add(row.Index);
                        }
                    }
                    
                    // 删除所有选中的行
                    foreach (DataGridViewRow row in rowsToDelete)
                    {
                        dgvFiles.Rows.Remove(row);
                    }
                    
                    // 创建删除命令并添加到撤销栈
                    var command = new DeleteRowCommand(dgvFiles, rowsToDelete.ToArray());
                    _undoRedoService.ExecuteCommand(command);
                }
                else
                {
                    // 单行删除
                    int rowIndex = e.Row.Index;
                    if (rowIndex >= 0 && rowIndex < bindingList.Count)
                    {
                        // 记录要删除的行和索引
                        List<FileRenameInfo> deletedRows = new List<FileRenameInfo> { bindingList[rowIndex] };
                        List<int> deletedIndices = new List<int> { rowIndex };
                        
                        // 创建删除命令并添加到撤销栈
                        var command = new DeleteRowCommand(dgvFiles, new DataGridViewRow[] { e.Row });
                        _undoRedoService.ExecuteCommand(command);
                    }
                }
            }
        }

        private void dgvExcelData_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            // 绘制行头序号
            var grid = sender as DataGridView;
            var rowNumber = (e.RowIndex + 1).ToString();

            var centerFormat = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // 使用较小字体适应30像素宽度
            using var font = new Font(this.Font.FontFamily, 8f);
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowNumber, font, SystemBrushes.ControlDark, headerBounds, centerFormat);
        }

        // 手动模式模式按钮点击事件
        private void btnStartImmediateRename_Click(object sender, EventArgs e)
        {
            if (!isImmediateRenameActive)
            {
                isImmediateRenameActive = true;
                UpdateTrayMenuItems();
            }
        }



        private void DataGridView_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null || e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var dgv = sender as DataGridView;
            var cell = dgv[e.ColumnIndex, e.RowIndex];
            if (cell == null) return;

            // 仅在单元格可见时执行格式化
            if (!dgv.Rows[e.RowIndex].Displayed || !dgv.Columns[e.ColumnIndex].Visible)
                return;

            // 保存原始字体和最小字体大小
            Font originalFont = cell.Style.Font ?? dgv.DefaultCellStyle.Font ?? SystemFonts.DefaultFont;
            float minFontSize = 6f;
            float currentFontSize = originalFont.Size;
            string text = e.Value.ToString();

            // 获取单元格内容区域宽度
            int cellWidth = dgv.Columns[e.ColumnIndex].Width - 10; // 减去内边距
            if (cellWidth <= 0) return;

            // 快速检查文本长度，避免不必要的字体调整
            if (text.Length <= 10) // 短文本通常不需要调整字体
            {
                // 恢复默认字体（如果之前有调整）
                if (cell.Style.Font != null && cell.Style.Font.Size != originalFont.Size)
                {
                    cell.Style.Font = originalFont;
                }
            }
            else
            {
                // 创建临时Graphics对象用于测量文本
                using Graphics g = dgv.CreateGraphics();

                // 测量当前字体下文本宽度
                SizeF textSize = g.MeasureString(text, originalFont);

                // 如果文本宽度超过单元格宽度，逐步缩小字体
                if (textSize.Width > cellWidth)
                {
                    // 计算需要缩小的大致比例
                    float scaleFactor = cellWidth / textSize.Width;
                    currentFontSize = Math.Max(minFontSize, originalFont.Size * scaleFactor);

                    // 设置最终字体
                    cell.Style.Font = new Font(originalFont.FontFamily, currentFontSize, originalFont.Style);
                }
                else
                {
                    // 恢复默认字体
                    if (cell.Style.Font != null && cell.Style.Font.Size != originalFont.Size)
                    {
                        cell.Style.Font = originalFont;
                    }
                }
            }

            // 设置匹配行高亮颜色
            if (dgv.Rows[e.RowIndex].DataBoundItem is DataRowView dataRowView && _matchedRows.Contains(dataRowView.Row))
            {
                e.CellStyle.BackColor = Color.FromArgb(255, 192, 0); // 橙色R255,G192,B0
            }
        }

        // 启用双缓冲和优化绘制的方法
        private void OptimizeDataGridViewPerformance()
        {
            // 启用双缓冲
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null,
                dgvFiles,
                new object[] { true });

            // 减少绘制复杂度
            dgvFiles.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dgvFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // 优化单元格样式继承
            dgvFiles.EnableHeadersVisualStyles = false;

            // 减少不必要的刷新
            dgvFiles.AutoGenerateColumns = false;
            dgvFiles.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            // 提高绘制性能
            dgvFiles.ScrollBars = ScrollBars.Both;
            dgvFiles.RowTemplate.Height = 24; // 设置固定行高
            
            // 设置dgvExcelData行高与dgvFiles一致
            typeof(DataGridView).InvokeMember(
                "DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty,
                null,
                dgvExcelData,
                new object[] { true });
            
            dgvExcelData.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dgvExcelData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvExcelData.EnableHeadersVisualStyles = false;
            dgvExcelData.AutoGenerateColumns = false;
            dgvExcelData.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvExcelData.ScrollBars = ScrollBars.Both;
            dgvExcelData.RowTemplate.Height = 24; // 设置固定行高与dgvFiles一致
        }

        private void FileBindingList_ListChanged(object sender, ListChangedEventArgs e)
        {
            // 仅在数据发生变化且有选中的JSON文件时自动保存
            if (cmbJsonFiles.SelectedItem != null && !string.IsNullOrEmpty(cmbJsonFiles.SelectedItem.ToString()))
            {
                string selectedFileName = cmbJsonFiles.SelectedItem.ToString();
                string jsonPath = Path.Combine(Application.StartupPath, "SavedGrids", selectedFileName + ".json");
                SaveToJsonFile(jsonPath);
            }
        }

        // 判断FileRenameInfo是否为空行
        private bool IsEmptyFileRenameInfo(FileRenameInfo item)
        {
            return string.IsNullOrEmpty(item.OriginalName) &&
                   string.IsNullOrEmpty(item.NewName) &&
                   string.IsNullOrEmpty(item.Status) &&
                   string.IsNullOrEmpty(item.ErrorMessage) &&
                   string.IsNullOrEmpty(item.FullPath) &&
                   string.IsNullOrEmpty(item.RegexResult) &&
                   string.IsNullOrEmpty(item.OrderNumber) &&
                   string.IsNullOrEmpty(item.Material) &&
                   string.IsNullOrEmpty(item.Quantity) &&
                   string.IsNullOrEmpty(item.Dimensions) &&
                   string.IsNullOrEmpty(item.Process) &&
                   string.IsNullOrEmpty(item.Time);
        }

        private void SaveToJsonFile(string filePath)
        {
            if (dgvFiles.DataSource is BindingList<FileRenameInfo> dataSource)
            {
                try
            {
                // 确保保存目录存在
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // 保留所有数据行（包括空行）
            var json = JsonConvert.SerializeObject(dataSource.ToList(), Formatting.Indented);
            
            File.WriteAllText(filePath, json, Encoding.UTF8);
            }
                catch (IOException ioEx)
            {
                MessageBox.Show($"文件操作错误: {ioEx.Message}\n路径: {filePath}\n可能文件被其他程序占用，请关闭后重试。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"自动保存失败: {ex}\n路径: {filePath}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadRegexPatterns()
        {
            // 从配置加载正则表达式
            string savedPatterns = AppSettings.RegexPatterns;
            regexPatterns.Clear();
            if (!string.IsNullOrEmpty(savedPatterns))
            {
                foreach (var item in savedPatterns.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = item.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        regexPatterns[parts[0]] = parts[1];
                    }
                }
            }
        }

        // 已合并到上方的regexPatterns变量，避免重复定义
        // private Dictionary<string, string> RegexPatterns = new Dictionary<string, string>();


        private void 参数设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 创建并显示设置窗口
            using var settingsForm = new SettingsForm();
            settingsForm.ShowDialog();
            
            // 设置保存后的处理
            OnSettingsSaved();
            
            // 重新加载正则表达式配置和材料列表
            LoadRegexPatterns();
            UpdateRegexComboBox(); // 更新正则表达式下拉框
                                   // 加载材料列表
            string materialStr = AppSettings.Material;
            materials = materialStr.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrEmpty(m))
                .ToList();
            // 更新右键菜单中的材料列表
            if (_dgvContextMenu != null)
            {
                _dgvContextMenu.SetMaterials(materials);
            }
            
            // 同时更新dgvExcelData右键菜单中的材料列表
            if (_dgvExcelContextMenu != null)
            {
                _dgvExcelContextMenu.SetMaterials(materials);
            }
        }
        
        /// <summary>
        /// 配置管理菜单项点击事件，触发ExportSettingsClick事件
        /// </summary>
        private void 配置管理ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // 触发ExportSettingsClick事件
                ExportSettingsClick?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"配置管理错误: {ex.Message}");
                MessageBox.Show($"配置管理时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 关于菜单项点击事件，显示版本信息
        /// </summary>
        private void 关于ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string versionInfo = $"大诚重命名工具\n\n版本: {version.Major}.{version.Minor}.{version.Build}\n\n" +
                                   "V2.2.0 更新内容：\n" +
                                   "• 全新PDF预览功能，支持悬浮页码显示\n" +
                                   "• 窗体延迟显示优化，内容与窗体同时呈现\n" +
                                   "• 修复PDF预览初始化显示问题\n" +
                                   "• 优化最佳适应缩放算法\n" +
                                   "• 右键菜单添加最佳适应选项\n" +
                                   "• 修复PDF文件占用问题\n\n" +
                                   "核心功能：\n" +
                                   "• 文件智能重命名 • Excel数据导入导出\n" +
                                   "• PDF尺寸识别处理 • 批量处理模式\n" +
                                   "• 实时监控模式 • 多种正则表达式支持\n" +
                                   "• 导出路径智能管理 • 保留功能支持返单\n\n" +
                                   "开发者: 大诚团队\n" +
                                   "© 2025 版权所有";

                MessageBox.Show(versionInfo, "关于大诚工具箱", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"关于窗口错误: {ex.Message}");
                MessageBox.Show($"显示关于信息时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

/// <summary>
        /// 性能监控菜单项点击事件，打开性能监控窗口
        /// </summary>
        private void 性能监控ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                using (var performanceForm = new PerformanceMonitorForm())
                {
                    performanceForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开性能监控窗口错误: {ex.Message}");
                MessageBox.Show($"打开性能监控窗口时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 撤销操作点击事件
        /// </summary>
        private void OnUndoClick(object sender, EventArgs e)
        {
            try
            {
                if (_enhancedUndoRedoService?.CanUndo() == true)
                {
                    var result = _enhancedUndoRedoService.Undo();
                    if (!string.IsNullOrEmpty(result))
                    {
                        UpdateStatus($"已撤销: {result}");
                        RefreshUI();
                    }
                }
                else
                {
                    MessageBox.Show("没有可撤销的操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"撤销操作失败: {ex.Message}", ex);
                MessageBox.Show($"撤销操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 重做操作点击事件
        /// </summary>
        private void OnRedoClick(object sender, EventArgs e)
        {
            try
            {
                if (_enhancedUndoRedoService?.CanRedo() == true)
                {
                    var result = _enhancedUndoRedoService.Redo();
                    if (!string.IsNullOrEmpty(result))
                    {
                        UpdateStatus($"已重做: {result}");
                        RefreshUI();
                    }
                }
                else
                {
                    MessageBox.Show("没有可重做的操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"重做操作失败: {ex.Message}", ex);
                MessageBox.Show($"重做操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 操作历史点击事件
        /// </summary>
        private void OnOperationHistoryClick(object sender, EventArgs e)
        {
            try
            {
                if (_enhancedUndoRedoService != null)
                {
                    var historyForm = new OperationHistoryForm(_enhancedUndoRedoService);
                    historyForm.ShowDialog(this);
                }
                else
                {
                    MessageBox.Show("增强撤销/重做服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"打开操作历史窗口失败: {ex.Message}", ex);
                MessageBox.Show($"打开操作历史窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 更新状态显示
        /// </summary>
        private void UpdateStatus(string message)
        {
            // 这里可以更新状态栏或状态显示
            System.Diagnostics.Debug.WriteLine($"[状态] {message}");
        }

        /// <summary>
        /// 刷新UI显示
        /// </summary>
        private void RefreshUI()
        {
            // 这里可以刷新相关的UI组件
            if (dgvFiles.InvokeRequired)
            {
                dgvFiles.Invoke(new Action(() => dgvFiles.Refresh()));
            }
            else
            {
                dgvFiles.Refresh();
            }
        }

        private void OnSettingsSaved()
        {
            // 注销当前热键
            UnregisterHotKey(this.Handle, toggleHotkeyId);
            if (toggleHotkeyAtom != IntPtr.Zero)
            {
                GlobalDeleteAtom(toggleHotkeyAtom);
                toggleHotkeyAtom = IntPtr.Zero;
            }
            // 重新注册热键
            RegisterHotkeys();
            
            // 设置保存后重新加载配置名称
            string newConfig = AppSettings.LastUsedConfigName;
            OnConfigLoaded(newConfig);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 添加窗体激活事件处理器，用于在从SettingsForm返回时更新配置名
            this.Activated += Form1_Activated;

            // 触发FormLoad事件
            FormLoad?.Invoke(this, e);
            
            // 应用启动时加载最后使用的配置
            string lastConfig = AppSettings.LastUsedConfigName;
            // 处理可能存储的旧“默认”值
            if (string.IsNullOrEmpty(lastConfig) || lastConfig == "默认")
            {
                OnConfigLoaded("标准配置");
            }
            else
            {
                OnConfigLoaded(lastConfig);
            }

            // 加载最近5次导入路径
            if (!string.IsNullOrEmpty(AppSettings.LastInputDir))
            {
                var recentPaths = AppSettings.LastInputDir.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in recentPaths)
                {
                    if (!txtInputDir.Items.Contains(path))
                    {
                        txtInputDir.Items.Add(path);
                    }
                }
                // 选中最后一次使用的路径
                if (recentPaths.Length > 0)
                {
                    txtInputDir.SelectedItem = recentPaths[0];
                }
            }
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            // 检查配置名是否已更新，如果不同则更新状态栏
            string currentConfig = AppSettings.LastUsedConfigName;
            if (!string.IsNullOrEmpty(currentConfig) && currentConfig != _currentConfigName)
            {
                LogHelper.Debug($"Form1激活时检测到配置名变化: {_currentConfigName} -> {currentConfig}");
                OnConfigLoaded(currentConfig);
            }
        }

        private void OnConfigLoaded(string configName)
        {
            // 合并显示配置名称和状态信息
            // 处理空配置名称情况
            _currentConfigName = string.IsNullOrEmpty(configName) ? "标准配置" : configName;
            // 保存当前配置名称到设置
            AppSettings.LastUsedConfigName = _currentConfigName;
            AppSettings.Save();
            
            // 实际加载配置设置
            try
            {
                // 重新加载所有设置，包括材料设置
                LoadSettings();
                
                // 刷新UI以反映加载的设置
                // 移除重复调用，因为LoadSettings()内部已经调用了UpdateRegexComboBox()
                _dgvContextMenu?.SetMaterials(materials);
                
                toolStripStatusLabel.Text = $"当前配置: {_currentConfigName} | 状态: 就绪";
            }
            catch (Exception)
            {
                toolStripStatusLabel.Text = $"当前配置: {_currentConfigName} | 状态: 配置加载失败";
                // 可以选择是否显示错误信息
                // MessageBox.Show("配置加载失败: " + ex.Message);
            }
        }

        #region 目录选择
        private void BtnSelectInputDir_Click(object sender, EventArgs e)
        {
            try
            {
                // 将业务逻辑委托给Presenter处理
                // Presenter会调用ShowFolderBrowserDialog和UpdateInputDirDisplay方法
                _presenter.HandleSelectInputDirClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("选择输入目录时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsTemporaryFile()
        {
            // 仅保留PDF文件识别逻辑，移除其他临时文件检查
            return false;
        }

        #endregion

        #region 自动保存和加载
        private void PopulateJsonFilesDropdown()
        {
            cmbJsonFiles.Items.Clear();
            var jsonDir = AppDataPathManager.SavedGridsDirectory;
            // 确保目录存在
            Directory.CreateDirectory(jsonDir);
            foreach (var file in Directory.GetFiles(jsonDir, "*.json"))
            {
                cmbJsonFiles.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
        }

        private void btnSaveJson_Click(object sender, EventArgs e)
        {
            if (dgvFiles.DataSource is not BindingList<FileRenameInfo> data || data.Count == 0)
            {
                MessageBox.Show("没有数据可保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var sfd = new SaveFileDialog();
            sfd.Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*";
            sfd.InitialDirectory = AppDataPathManager.SavedGridsDirectory;
            sfd.FileName = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                    File.WriteAllText(sfd.FileName, json);
                    PopulateJsonFilesDropdown();
                    MessageBox.Show("保存成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void cmbJsonFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbJsonFiles.SelectedItem == null)
                return;

            var jsonPath = Path.Combine(AppDataPathManager.SavedGridsDirectory, $"{cmbJsonFiles.SelectedItem}.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    string json = File.ReadAllText(jsonPath);
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<BindingList<FileRenameInfo>>(json);
                    if (data != null)
                    {
                        dgvFiles.DataSource = data;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }




        #endregion

        private string GetPdfDimensions(string filePath)
        {
            try
            {
                using var document = new Spire.Pdf.PdfDocument();
                document.LoadFromFile(filePath);
                if (document.Pages.Count > 0)
                {
                    var page = document.Pages[0];
                    var width = page.Size.Width / 72 * 25.4; // 转换为毫米
                    var height = page.Size.Height / 72 * 25.4;
                    return $"{width:F1}x{height:F1}mm";
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取PDF页面尺寸失败 - 文件: {filePath}, 错误: {ex.Message}");
            }
            return string.Empty;
        }

        private int? GetPdfPageCount(string filePath)
        {
            // 使用统一的PDF尺寸服务来获取PDF页数
            return _pdfDimensionService.GetPageCount(filePath);
        }




        private void ClearList()
        {
            if (dgvFiles.DataSource is BindingList<FileRenameInfo> bindingList)
            {
                bindingList.Clear();
            }
            pendingFiles.Clear();
            SaveMaterialSettings();
        }

        #region 正则表达式管理

        private void UpdateRegexComboBox()
        {
            // 保存当前选中的正则表达式名称
            string currentSelected = cmbRegex.SelectedItem?.ToString();
            UpdateRegexComboBox(currentSelected);
        }

        private void UpdateRegexComboBox(string selectedItem)
        {
            LogHelper.Debug($"UpdateRegexComboBox: 开始更新，尝试恢复选择 '{selectedItem}'");
            cmbRegex.Items.Clear();
            foreach (var name in regexPatterns.Keys)
            {
                cmbRegex.Items.Add(name);
                LogHelper.Debug($"UpdateRegexComboBox: 添加项目 '{name}'");
            }

            // 尝试恢复指定的选择
            if (!string.IsNullOrEmpty(selectedItem) && cmbRegex.Items.Contains(selectedItem))
            {
                cmbRegex.SelectedItem = selectedItem;
                LogHelper.Debug($"UpdateRegexComboBox: 成功恢复选择 '{selectedItem}'");
            }
            else if (cmbRegex.Items.Count > 0)
            {
                cmbRegex.SelectedIndex = 0;
                LogHelper.Debug($"UpdateRegexComboBox: 无法恢复 '{selectedItem}'，选择第一项 '{cmbRegex.SelectedItem?.ToString()}'");
            }
            else
            {
                LogHelper.Debug("UpdateRegexComboBox: ComboBox为空，无项目可选择");
            }
        }

        #endregion

        #region 材料设置


        #endregion

        #region 文件监控


        private async void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            await AddFileToPendingList(e.FullPath);
        }

        private async void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            await AddFileToPendingList(e.FullPath);
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            Invoke(new Action(() =>
            {
                toolStripStatusLabel.Text = "监控错误：" + e.GetException().Message;
            }));
        }

        // 添加一个标志来跟踪是否正在处理手动模式下的文件
        private bool isProcessingManualFiles = false;

        private async Task AddFileToPendingList(string filePath)
        {
            try
            {
                // 步骤1：验证文件
                var validationResult = ValidateFileForProcessing(filePath);
                if (!validationResult.IsValid)
                {
                    // 静默跳过临时文件
                    if (validationResult.ErrorType == ValidationErrorType.TemporaryFile)
                        return;
                        
                    // 显示其他错误
                    Invoke(new Action(() =>
                    {
                        toolStripStatusLabel.Text = validationResult.ErrorMessage;
                    }));
                    return;
                }

                var fileInfo = new FileInfo(filePath);
                
                // 步骤2：等待文件完全写入
                await Task.Delay(1000);

                // 步骤3：异步计算PDF尺寸
                var (width, height, tetBleed) = await CalculatePdfDimensionsAsync(fileInfo);

                // 在UI线程中执行剩余操作
                Invoke(new Action(() =>
                {
                    // 添加到待处理文件列表
                    pendingFiles.Add(fileInfo);
                    
                    // 步骤4：计算调整后的尺寸
                    string adjustedDimensions = string.Empty;
                    using (var settingsForm = new SettingsForm())
                    {
                        settingsForm.RecognizePdfDimensions(fileInfo.FullName);
                        // 批量模式下使用设置中的出血值，如果没有设置则默认为2
                        double tetBleed = 2; // 默认出血值
                        try
                        {
                            string bleedValues = AppSettings.Get("TetBleedValues")?.ToString() ?? "2";
                            string[] bleedParts = bleedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (bleedParts.Length > 0 && double.TryParse(bleedParts[0].Trim(), out double bleedValue))
                            {
                                tetBleed = bleedValue;
                            }
                        }
                        catch (Exception ex)
                        {
                            LogDebugInfo($"加载出血值设置失败: {ex.Message}，使用默认值2");
                        }
                        adjustedDimensions = settingsForm.CalculateFinalDimensions(settingsForm.PdfWidth, settingsForm.PdfHeight, tetBleed);
                    }

                    // 步骤5：应用选中的正则表达式获取匹配结果
                    string regexResult = string.Empty;
                    string pattern = string.Empty;

                    // 用于数据匹配：优先使用ExcelImportForm中的正则表达式
                    string matchPattern = string.Empty;
                    if (ExcelImportFormInstance != null && ExcelImportFormInstance.RegexComboBox != null && ExcelImportFormInstance.RegexComboBox.SelectedItem != null)
                    {
                        string selectedPatternName = ExcelImportFormInstance.RegexComboBox.SelectedItem.ToString();
                        if (regexPatterns.TryGetValue(selectedPatternName, out string selectedPattern))
                        {
                            matchPattern = selectedPattern;
                        }
                    }
                    // 如果Excel正则表达式未选择或为空，则使用主窗体的正则表达式进行数据匹配
                    else if (cmbRegex.SelectedItem != null && regexPatterns.TryGetValue(cmbRegex.SelectedItem.ToString(), out string mainPattern))
                    {
                        matchPattern = mainPattern;
                    }
                    
                    // 用于重命名：始终使用主窗体的正则表达式(cmbRegex)
                    if (cmbRegex.SelectedItem != null && regexPatterns.TryGetValue(cmbRegex.SelectedItem.ToString(), out string renamePattern))
                    {
                        pattern = renamePattern;
                    }

                    // 执行正则匹配（用于数据匹配）
                    if (!string.IsNullOrEmpty(matchPattern))
                    {
                        Match match = Regex.Match(fileInfo.Name, matchPattern);
                        if (match.Success)
                        {
                            if (match.Groups.Count > 1)
                            {
                                // 提取第一个捕获组的内容
                                regexResult = match.Groups[1].Value.Trim();
                            }
                            else
                            {
                                // 如果没有捕获组，使用整个匹配结果
                                regexResult = match.Value.Trim();
                            }
                        }
                    }

                    // 步骤6：匹配Excel数据
                    var bindingList = dgvFiles.DataSource as BindingList<FileRenameInfo>;
                    var (matchedRows, quantity, serialNumber) = MatchExcelData(regexResult, bindingList);

                    // 步骤7：根据操作模式处理文件
                    ProcessFileByMode(fileInfo, adjustedDimensions, regexResult, pattern, matchedRows, quantity, serialNumber);
                }));
            }
            catch (Exception ex)
            {
                Invoke(new Action(() =>
                {
                    toolStripStatusLabel.Text = "添加文件错误：" + ex.Message;
                }));
            }
        }
        #endregion

        #region 待处理文件处理

        /// <summary>
        /// 验证文件是否可以处理
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>验证结果</returns>
        private ValidationResult ValidateFileForProcessing(string filePath)
        {
            var result = new ValidationResult();
            
            try
            {
                var fileInfo = new FileInfo(filePath);
                
                // 检查文件是否存在
                if (!fileInfo.Exists)
                {
                    return ValidationResult.Failure("文件不存在", ValidationErrorType.FileNotFound);
                }
                
                // 仅处理PDF文件
                if (!fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationResult.Failure("仅支持PDF文件", ValidationErrorType.InvalidFileInfo);
                }
                
                // 检查是否为临时文件
                if (IsTemporaryFile())
                {
                    return ValidationResult.Failure("跳过临时文件处理", ValidationErrorType.TemporaryFile);
                }
                
                return ValidationResult.Success("文件验证通过");
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"文件验证失败: {ex.Message}", ValidationErrorType.General);
            }
        }

        /// <summary>
        /// 异步计算PDF尺寸信息
        /// </summary>
        private async Task<(string width, string height, double tetBleed)> CalculatePdfDimensionsAsync(FileInfo fileInfo)
        {
            return await Task.Run(() =>
            {
                string w = "0";
                string h = "0";
                double tb = 0;
                using var settingsForm = new SettingsForm();
                settingsForm.RecognizePdfDimensions(fileInfo.FullName);
                string bleedValues = AppSettings.Get("TetBleedValues")?.ToString() ?? "0";
                string[] bleedParts = bleedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (bleedParts.Length > 0 && double.TryParse(bleedParts[0].Trim(), out double bleedValue))
                {
                    tb = bleedValue;
                }
                // 传递原始PDF尺寸，不在此处应用出血值
                w = settingsForm.PdfWidth.ToString();
                h = settingsForm.PdfHeight.ToString();
                return (w, h, tb);
            });
        }

        /// <summary>
        /// 匹配Excel数据
        /// </summary>
        private (List<DataRow> matchedRows, string quantity, string serialNumber) MatchExcelData(string regexResult, BindingList<FileRenameInfo> bindingList)
        {
            var matchedRows = new List<DataRow>();
            string quantity = string.Empty;
            string serialNumber = string.Empty;

            // 检查是否有导入的Excel数据
            if (_excelImportedData != null && _excelSearchColumnIndex >= 0 && _excelReturnColumnIndex >= 0 && _excelNewColumnIndex >= 0)
            {
                // 收集所有匹配的行
                // 只有当regexResult非空时才进行匹配
                if (!string.IsNullOrEmpty(regexResult))
                {
                    // 百分百精确匹配：不进行任何预处理，使用原始字符串进行精确比较
                    // 添加详细的调试信息
                    System.Diagnostics.Debug.WriteLine($"=== MatchExcelData 百分百精确匹配 ===");
                    System.Diagnostics.Debug.WriteLine($"正则结果: '{ShowInvisibleChars(regexResult)}' (长度:{regexResult.Length})");

                    foreach (DataRow row in _excelImportedData.Rows)
                    {
                        if (row[_excelSearchColumnIndex] != null)
                        {
                            string tableValue = row[_excelSearchColumnIndex].ToString();

                            // 百分百精确匹配：不进行任何Trim或标准化处理
                            // 只有当表格中的值与正则结果完全相等时才匹配
                            bool isExactMatch = string.Equals(tableValue, regexResult, StringComparison.Ordinal);

                            // 调试信息：显示每行的匹配情况
                            System.Diagnostics.Debug.WriteLine($"表格行: '{ShowInvisibleChars(tableValue)}' (长度:{tableValue.Length})");
                            System.Diagnostics.Debug.WriteLine($"  百分百精确匹配: {isExactMatch}");

                            if (isExactMatch)
                            {
                                matchedRows.Add(row);
                                System.Diagnostics.Debug.WriteLine($"  ✓ 百分百精确匹配成功！");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"  ✗ 匹配失败");
                            }
                            System.Diagnostics.Debug.WriteLine("");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"=== MatchExcelData 调试结束 ===");
                }

                // 调试信息：显示匹配到的行数
                System.Diagnostics.Debug.WriteLine($"找到 {matchedRows.Count} 行匹配 '{regexResult}'");
                
                // 处理所有匹配的行
                if (matchedRows.Count > 0)
                {
                    // 收集匹配行并添加到高亮集合
                    foreach (DataRow row in matchedRows)
                    {
                        if (!_matchedRows.Contains(row))
                        {
                            _matchedRows.Add(row);
                        }
                    }
                    
                    // 获取第一个匹配行的数据（保持向后兼容）
                    var firstRow = matchedRows.First();
                    quantity = firstRow[_excelReturnColumnIndex]?.ToString() ?? string.Empty;
                    serialNumber = firstRow[_excelNewColumnIndex]?.ToString() ?? string.Empty;
                    
                    // 调试信息：显示获取到的数量和序号
                    System.Diagnostics.Debug.WriteLine($"数量: '{quantity}', 序号: '{serialNumber}'");
                }
                else
                {
                    // 调试信息：没有找到匹配的行
                    System.Diagnostics.Debug.WriteLine($"没有找到匹配 '{regexResult}' 的行");
                }
            }
            
            // 如果序号为空，使用默认序号生成逻辑
            if (string.IsNullOrEmpty(serialNumber))
            {
                if (bindingList != null && bindingList.Any())
                {
                    // 修复序号比较逻辑，确保按数值比较而不是字符串比较
                    int maxSerial = 0;
                    foreach (var item in bindingList)
                    {
                        if (int.TryParse(item.SerialNumber, out int currentSerial))
                        {
                            if (currentSerial > maxSerial)
                            {
                                maxSerial = currentSerial;
                            }
                        }
                    }
                    serialNumber = (maxSerial + 1).ToString("D2");
                }
                else
                {
                    serialNumber = "01";
                }
            }
            
            return (matchedRows, quantity, serialNumber);
        }

        /// <summary>
        /// 显示字符串中的不可见字符（用于百分百精确匹配调试）
        /// </summary>
        private string ShowInvisibleChars(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "[空]";

            // 显示所有不可见字符，用于精确匹配调试
            string result = input
                .Replace(" ", "[空格]")
                .Replace("\t", "[制表符]")
                .Replace("\n", "[换行符]")
                .Replace("\r", "[回车符]")
                .Replace("\u00A0", "[不间断空格]")
                .Replace("\u200B", "[零宽空格]")
                .Replace("\u200C", "[零宽非连字符]")
                .Replace("\u200D", "[零宽连字符]")
                .Replace("\uFEFF", "[BOM]");

            // 如果没有发现不可见字符，显示原始字符串
            return result == input ? input : result;
        }

    
        /// <summary>
        /// 根据操作模式处理文件
        /// </summary>
        private void ProcessFileByMode(FileInfo fileInfo, string adjustedDimensions, string regexResult, string pattern, 
            List<DataRow> matchedRows, string quantity, string serialNumber)
        {
            // 批量模式下不显示材料选择窗口
            if (isImmediateRenameActive)
            {
                // 不对单个文件立即显示对话框，而是调用处理方法
                if (!isProcessingManualFiles)
                {
                    ProcessPendingFilesManually();
                }
            }
            else
            {
                // 批量模式下直接添加到表格
                // 收集匹配行并添加到高亮集合
                foreach (DataRow row in matchedRows)
                {
                    if (!_matchedRows.Contains(row))
                    {
                        _matchedRows.Add(row);
                    }
                }
                dgvExcelData.Invalidate(); // 刷新高亮显示

                // 处理所有匹配的行
                if (matchedRows.Count > 0)
                {
                    foreach (DataRow row in matchedRows)
                    {
                        // 获取数量值和序号值
                        string rowQuantity = row[_excelReturnColumnIndex]?.ToString() ?? string.Empty;
                        string rowSerialNumber = row[_excelNewColumnIndex]?.ToString() ?? string.Empty;
                        
                        // 如果序号为空，使用默认序号生成逻辑
                        if (string.IsNullOrEmpty(rowSerialNumber))
                        {
                            var bindingList = dgvFiles.DataSource as BindingList<FileRenameInfo>;
                            if (bindingList != null && bindingList.Any())
                            {
                                // 修复序号比较逻辑，确保按数值比较而不是字符串比较
                                int maxSerial = 0;
                                foreach (var item in bindingList)
                                {
                                    if (int.TryParse(item.SerialNumber, out int currentSerial))
                                    {
                                        if (currentSerial > maxSerial)
                                        {
                                            maxSerial = currentSerial;
                                        }
                                    }
                                }
                                rowSerialNumber = (maxSerial + 1).ToString("D2");
                            }
                            else
                            {
                                rowSerialNumber = "01";
                            }
                        }
                        
                        // 获取列组合值
                        string compositeColumnValue = string.Empty;
                        if (SettingsForm.IsEventEnabled("列组合"))
                        {
                            try
                            {
                                compositeColumnValue = _compositeColumnService.GetCompositeColumnValueForRow(row);
                            }
                            catch (Exception ex)
                            {
                                LogDebugInfo("批量模式获取列组合值失败: " + ex.Message);
                            }
                        }
                        
                        // 直接添加到表格，传递列组合值
                        AddFileToGrid(fileInfo, "", "", rowQuantity, "", adjustedDimensions, "", rowSerialNumber, compositeColumnValue);
                    }
                }
                else
                {
                    // 没有匹配行时使用默认值
                    if (string.IsNullOrEmpty(quantity)) quantity = "1";
                    
                    // 即使没有匹配行，如果启用了列组合功能，也尝试获取列组合值（虽然通常为空）
                    string compositeColumnValue = string.Empty;
                    if (SettingsForm.IsEventEnabled("列组合"))
                    {
                        // 没有匹配行时列组合值为空，但保持一致的逻辑
                        compositeColumnValue = string.Empty;
                    }
                    
                    AddFileToGrid(fileInfo, "", "", quantity, "", adjustedDimensions, "", serialNumber, compositeColumnValue);
                }
            }
        }
        // 实现Windows资源管理器风格的自然排序比较器
        private class NaturalSortComparer : IComparer<string>
        {
            // 改进的正则表达式，支持浮点数
            private static readonly Regex _regex = new Regex(@"(\d+\.\d+|\d+)");

            public int Compare(string x, string y)
            {
                if (x == null && y == null)
                    return 0;
                if (x == null)
                    return -1;
                if (y == null)
                    return 1;

                // 分割字符串和数字（包括浮点数）
                var xParts = _regex.Split(x);
                var yParts = _regex.Split(y);

                int minLength = Math.Min(xParts.Length, yParts.Length);
                for (int i = 0; i < minLength; i++)
                {
                    // 比较当前部分
                    if (i % 2 == 0) // 非数字部分
                    {
                        // 使用与Windows资源管理器一致的不区分大小写比较
                        int result = string.Compare(xParts[i], yParts[i], StringComparison.CurrentCultureIgnoreCase);
                        if (result != 0)
                            return result;
                    }
                    else // 数字部分（可能是整数或浮点数）
                    {
                        // 尝试解析为浮点数
                        if (double.TryParse(xParts[i], out double xNum) && double.TryParse(yParts[i], out double yNum))
                        {
                            int result = xNum.CompareTo(yNum);
                            if (result != 0)
                                return result;
                        }
                        else
                        {
                            // 如果不能解析为数字，则按字符串比较
                            int result = string.Compare(xParts[i], yParts[i], StringComparison.CurrentCultureIgnoreCase);
                            if (result != 0)
                                return result;
                        }
                    }
                }

                // 如果前面的部分都相同，则比较长度
                return xParts.Length.CompareTo(yParts.Length);
            }
        }

        // 手动处理待处理文件的方法
        private void ProcessPendingFilesManually()
        {
            if (isProcessingManualFiles || !isImmediateRenameActive || pendingFiles.Count == 0)
                return;

            isProcessingManualFiles = true;

            // 先创建副本以避免并发修改问题
            var pendingFilesCopy = pendingFiles.ToList();
            // 按文件名自然排序（包含数字排序）
            var sortedFiles = pendingFilesCopy.OrderBy(f => f.Name, new NaturalSortComparer()).ToList();
            var fileInfo = sortedFiles.First();

            // 在处理前先从原始列表中移除，避免重复处理
            if (pendingFiles.Contains(fileInfo))
            {
                pendingFiles.Remove(fileInfo);
            }

            // 异步计算PDF尺寸（含出血值）
            Task.Run(async () =>
            {
                (string width, string height, double tetBleed) = await Task.Run(() =>
                {
                    string w = "0";
                    string h = "0";
                    double tb = 0;
                    using var settingsForm = new SettingsForm();
                    settingsForm.RecognizePdfDimensions(fileInfo.FullName);
                    string bleedValues = AppSettings.Get("TetBleedValues")?.ToString() ?? "0";
                    string[] bleedParts = bleedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (bleedParts.Length > 0 && double.TryParse(bleedParts[0].Trim(), out double bleedValue))
                    {
                        tb = bleedValue;
                    }
                    // 传递原始PDF尺寸，不在此处应用出血值
                    w = settingsForm.PdfWidth.ToString();
                    h = settingsForm.PdfHeight.ToString();
                    return (w, h, tb);
                });

                // 在UI线程显示对话框
                Invoke(new Action(() =>
                {
                    ShowMaterialSelectionDialog(fileInfo, width, height);
                }));
            });
        }

        private void ShowMaterialSelectionDialog(FileInfo fileInfo, string width, string height)
        {
            // 计算应用出血值后的尺寸
            string adjustedDimensions = string.Empty;
            using (var settingsForm = new SettingsForm())
            {
                settingsForm.RecognizePdfDimensions(fileInfo.FullName);
                double tetBleed = 0;
                string bleedValues = AppSettings.Get("TetBleedValues")?.ToString() ?? "0";
                string[] bleedParts = bleedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (bleedParts.Length > 0 && double.TryParse(bleedParts[0].Trim(), out double bleedValue))
                {
                    tetBleed = bleedValue;
                }
                adjustedDimensions = settingsForm.CalculateFinalDimensions(settingsForm.PdfWidth, settingsForm.PdfHeight, tetBleed);
            }

            // 应用选中的正则表达式获取匹配结果
            string regexResult = string.Empty;  // 用于数据匹配的正则结果
            string renamePattern = string.Empty;  // 用于重命名的主正则
            bool isReturnOrder = false;  // 是否为返单文件
    
            // 获取用于重命名的主正则表达式
            renamePattern = GetSelectedRegexPattern();
            
            // ✅ 检测是否为返单文件：通过检查文件名中是否包含保留格式前缀（&ID-、&MT- 等）
            string returnOrderRegexResult = ExtractReturnOrderRegexResult(fileInfo.Name);
            if (!string.IsNullOrEmpty(returnOrderRegexResult))
            {
                // 这是返单文件，直接使用保留的正则结果，无需使用Excel正则
                regexResult = returnOrderRegexResult;
                isReturnOrder = true;
                LogHelper.Debug("ShowMaterialSelectionDialog: 检测到返单文件，使用保留的正则结果: '" + regexResult + "'");
            }
            else
            {
                // 这是新文件，使用Excel正则进行数据匹配
                // 获取用于数据匹配的Excel正则表达式
                LogHelper.Debug("ShowMaterialSelectionDialog: 检测到新文件，准备得到Excel正则表达式");
                string matchPattern = GetExcelRegexPattern();
                LogHelper.Debug("ShowMaterialSelectionDialog: GetExcelRegexPattern()的返回值 = '" + (string.IsNullOrEmpty(matchPattern) ? "[empty]" : matchPattern) + "'");
                
                if (string.IsNullOrEmpty(matchPattern))
                {
                    // Excel正则为空，检查主正则是否为空
                    LogHelper.Debug("ShowMaterialSelectionDialog: Excel正则为空，检查主正则是否为空");
                    
                    // 两个正则都为空，但仍然需要打开材料选择窗口，让用户手动选择
                    if (string.IsNullOrEmpty(renamePattern))
                    {
                        LogHelper.Debug("ShowMaterialSelectionDialog: Excel正则和主正则都为空，在材料选择窗口中手动选择");
                        // 加载窗口，眉正正则为空不根据正则选择材料，但用户可以手动输入正则等信息
                    }
                    else
                    {
                        // 有主正则但没有Excel正则时，仍然使用主正则进行匹配
                        matchPattern = renamePattern;
                        LogHelper.Debug("ShowMaterialSelectionDialog: 主正则不为空，使用主正则进行数据匹配: '" + matchPattern + "'");
                    }
                }
        
                // 添加详细的调试信息
                LogHelper.Debug("ShowMaterialSelectionDialog: 主正则（用于重命名）: '" + (cmbRegex.SelectedItem?.ToString() ?? "null") + "' -> '" + renamePattern + "'");
                LogHelper.Debug("ShowMaterialSelectionDialog: 最终使用的数据匹配正则: '" + matchPattern + "'");
                LogHelper.Debug("ShowMaterialSelectionDialog: 处理文件: '" + fileInfo.Name + "'");
        
                // 使用正则进行数据匹配
                if (!string.IsNullOrEmpty(matchPattern))
                {
                    // 先去除文件扩展名，然后再匹配
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    LogHelper.Debug($"ShowMaterialSelectionDialog: 使用正则 '{matchPattern}' 匹配文件名 '{fileNameWithoutExtension}'");
                    
                    Match match = Regex.Match(fileNameWithoutExtension, matchPattern);
                    if (match.Success)
                    {
                        if (match.Groups.Count > 1)
                        {
                            // 提取第一个捕获组的内容
                            regexResult = match.Groups[1].Value.Trim();
                        }
                        else
                        {
                            // 如果没有捕获组，使用整个匹配结果
                            regexResult = match.Value.Trim();
                        }
                        // 调试信息：显示匹配结果
                        LogHelper.Debug($"新文件数据匹配成功: '{regexResult}' (文件名: '{fileNameWithoutExtension}', 正则: '{matchPattern}')");
                    }
                    else
                    {
                        // 调试信息：匹配失败
                        LogHelper.Warn($"新文件数据匹配失败: 文件名 '{fileNameWithoutExtension}' 不匹配正则 '{matchPattern}'");
                    }
                }
                else
                {
                    // 调试信息：pattern为空（不应该到这里）
                    LogHelper.Warn("新文件数据匹配错误: 正则表达式模式为空");
                }
            }
            // 生成序号
            string serialNumber = string.Empty;
            if (dgvFiles.DataSource is BindingList<FileRenameInfo> bindingList && bindingList.Any())
            {
                // 修复序号比较逻辑，确保按数值比较而不是字符串比较
                int maxSerial = 0;
                foreach (var item in bindingList)
                {
                    if (int.TryParse(item.SerialNumber, out int currentSerial))
                    {
                        if (currentSerial > maxSerial)
                        {
                            maxSerial = currentSerial;
                        }
                    }
                }
                serialNumber = (maxSerial + 1).ToString("D2");
            }
            else
            {
                serialNumber = "01";
            }
            
            // 匹配Excel数据（使用正则结果）
            var matchedRows = new List<DataRow>();
            LogHelper.Debug($"ShowMaterialSelectionDialog: 准备进行数据匹配，regexResult='{regexResult}', isReturnOrder={isReturnOrder}");
            
            if (!string.IsNullOrEmpty(regexResult))
            {
                LogHelper.Debug($"ShowMaterialSelectionDialog: 开始调用MatchExcelData进行数据匹配，regexResult='{regexResult}'");
                var (rows, quantity, serial) = MatchExcelData(regexResult, dgvFiles.DataSource as BindingList<FileRenameInfo>);
                matchedRows = rows;
                LogHelper.Debug($"ShowMaterialSelectionDialog: MatchExcelData返回，匹配行数={rows?.Count ?? 0}, quantity='{quantity}', serial='{serial}'");
                
                if (!string.IsNullOrEmpty(serial))
                {
                    serialNumber = serial;
                }
            }
            else
            {
                LogHelper.Warn($"ShowMaterialSelectionDialog: regexResult为空，跳过数据匹配。isReturnOrder={isReturnOrder}");
            }
            
            // 释放之前的实例（如果存在）
            _lastMaterialSelectForm?.Dispose();
            
            // 传递完整路径而不是仅文件名，以便获取真实的PDF旋转角度
            _lastMaterialSelectForm = new MaterialSelectFormModern(materials, fileInfo.FullName, regexResult, AppSettings.Opacity, width, height, _excelImportedData, _excelSearchColumnIndex, _excelReturnColumnIndex, _excelNewColumnIndex, _excelNewColumnIndex, serialNumber, matchedRows);

            // 设置Owner属性，以便MaterialSelectFormModern可以访问主窗体的dgvFiles
            _lastMaterialSelectForm.Owner = this;

            // 设置SettingsForm的MaterialSelectFormModern引用
            SettingsForm.SetMaterialSelectFormReference(_lastMaterialSelectForm);

            // 如果有Excel数据，加载到MaterialSelectFormModern中
            if (_excelImportedData != null)
            {
                _lastMaterialSelectForm.LoadExcelData(_excelImportedData, _excelSearchColumnIndex, _excelReturnColumnIndex, _excelNewColumnIndex, _excelNewColumnIndex);
            }
            _lastMaterialSelectForm.TopMost = true;
            // ✨ 关键修复：不要强制设置StartPosition，让WindowPositionManager控制位置
            // _lastMaterialSelectForm.StartPosition = FormStartPosition.CenterScreen;
            if (_lastMaterialSelectForm.ShowDialog() == DialogResult.OK)
            {
                // 收集匹配行并添加到高亮集合
                if (_lastMaterialSelectForm.MatchedRows != null && _lastMaterialSelectForm.MatchedRows.Count > 0)
                {
                    foreach (var row in _lastMaterialSelectForm.MatchedRows)
                    {
                        if (!_matchedRows.Contains(row))
                        {
                            _matchedRows.Add(row);
                        }
                    }
                    dgvExcelData.Invalidate(); // 刷新高亮显示
                }
                var selectedMaterial = _lastMaterialSelectForm.SelectedMaterial;
                var orderNumber = _lastMaterialSelectForm.OrderNumber;
                string unit = AppSettings.Unit ?? "";
                var exportPath = _lastMaterialSelectForm.SelectedExportPath;
                var quantities = _lastMaterialSelectForm.Quantities;
                string updatedSerialNumber = _lastMaterialSelectForm.SerialNumber;
                var serialNumbers = !string.IsNullOrEmpty(updatedSerialNumber) ? updatedSerialNumber.Split(',').Select(s => s.Trim()).ToList() : new List<string>();

                // 处理列组合数据 - 注意：这里不再预先获取单一值，而是在循环中为每个匹配行单独获取
                LogDebugInfo($"手动模式: 准备处理列组合数据，匹配行数量 = {_lastMaterialSelectForm.MatchedRows?.Count ?? 0}");

                if (isImmediateRenameActive)
                {
                    // 使用正则表达式安全提取宽高维度
                    var input = _lastMaterialSelectForm.AdjustedDimensions ?? "";
                    var matches = System.Text.RegularExpressions.Regex.Matches(input, @"\d+\.?\d*");
                    var dimensionsParts = matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToArray();
                    string parsedWidth = dimensionsParts.Length >= 1 ? dimensionsParts[0] : string.Empty;
                    string parsedHeight = dimensionsParts.Length >= 2 ? dimensionsParts[1] : string.Empty;

                    // 循环处理每个数量值，与匹配行一一对应
                    for (int i = 0; i < quantities.Count; i++)
                    {
                        var quantity = quantities[i];
                        string currentSerialNumber = string.Empty;
                        string compositeColumnValue = string.Empty; // 为每个循环单独获取列组合值
                        
                        if (_lastMaterialSelectForm.MatchedRows != null && i < _lastMaterialSelectForm.MatchedRows.Count && _excelNewColumnIndex >= 0)
                        {
                            var excelRow = _lastMaterialSelectForm.MatchedRows[i];
                            if (_excelNewColumnIndex < excelRow.ItemArray.Length)
                            {
                                if (int.TryParse(excelRow[_excelNewColumnIndex].ToString(), out int num)) 
                                    currentSerialNumber = num.ToString("D2"); 
                                else 
                                    currentSerialNumber = excelRow[_excelNewColumnIndex].ToString();
                            }
                            
                            // 为当前匹配行获取列组合值
                            if (SettingsForm.IsEventEnabled("列组合"))
                            {
                                try
                                {
                                    compositeColumnValue = _compositeColumnService.GetCompositeColumnValueForRow(excelRow);
                                    LogDebugInfo($"手动模式即时重命名: 第{i+1}个匹配行的列组合值 = '{compositeColumnValue}'");
                                }
                                catch (Exception ex)
                                {
                                    LogDebugInfo($"获取第{i+1}个匹配行的列组合值失败: " + ex.Message);
                                }
                            }
                        }
                        
                        // 确保有足够的序号
                        string serialNum = i < serialNumbers.Count ? serialNumbers[i] : (i + 1).ToString("D2");
                  #pragma warning disable CS0618 // 禁用过时API警告
                        RenameFileImmediately(fileInfo, selectedMaterial, orderNumber, quantity, unit, exportPath, _lastMaterialSelectForm.SelectedTetBleed, parsedWidth, parsedHeight, _lastMaterialSelectForm.FixedField, serialNum, _lastMaterialSelectForm.GetCompatibleCornerRadius(), _lastMaterialSelectForm.GetUsePdfLastPage(), _lastMaterialSelectForm.GetIsShapeSelected(), compositeColumnValue);
#pragma warning restore CS0618 // 恢复警告
                    }
                }
                else
                {
                    // 循环处理每个数量值，与匹配行一一对应
                    for (int i = 0; i < serialNumbers.Count; i++)
                    {
                        string currentSerialNumber = string.Empty;
                        var quantity = quantities[i];
                        string compositeColumnValue = string.Empty; // 为每个循环单独获取列组合值
                        
                        if (_lastMaterialSelectForm.MatchedRows != null && i < _lastMaterialSelectForm.MatchedRows.Count && _excelNewColumnIndex >= 0)
                        {
                            var excelRow = _lastMaterialSelectForm.MatchedRows[i];
                            if (_excelNewColumnIndex < excelRow.ItemArray.Length)
                            {
                                // 直接使用Excel中的序号值
                                currentSerialNumber = excelRow[_excelNewColumnIndex]?.ToString() ?? string.Empty;
                            }
                            
                            // 为当前匹配行获取列组合值
                            if (SettingsForm.IsEventEnabled("列组合"))
                            {
                                try
                                {
                                    compositeColumnValue = _compositeColumnService.GetCompositeColumnValueForRow(excelRow);
                                    LogDebugInfo($"手动模式批量处理: 第{i+1}个匹配行的列组合值 = '{compositeColumnValue}'");
                                }
                                catch (Exception ex)
                                {
                                    LogDebugInfo($"获取第{i+1}个匹配行的列组合值失败: " + ex.Message);
                                }
                            }
                        }
                        
                        AddFileToGrid(fileInfo, selectedMaterial, orderNumber, quantity, unit, _lastMaterialSelectForm.AdjustedDimensions, _lastMaterialSelectForm.FixedField, serialNumbers[i], compositeColumnValue);
                    }
                }
            }
            else
            {
                // 用户取消选择，不添加到列表
                // 注意：文件已经在ProcessPendingFilesManually中移除
            }

            // 处理完当前文件后，继续处理下一个
            isProcessingManualFiles = false;
            if (isImmediateRenameActive && pendingFiles.Count > 0)
            {
                ProcessPendingFilesManually();
            }
        }

        /// <summary>
        /// 获取列组合值
        /// </summary>
        /// <param name="row">Excel数据行</param>
        // 注意：GetCompositeColumnValue方法已移除，使用ICompositeColumnService服务替代

        /// <summary>
        /// 验证文件添加到网格的输入参数
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="material">材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="serialNumber">序号</param>
        /// <returns>验证结果</returns>
        private ValidationResult ValidateFileGridInput(FileInfo fileInfo, string material, string orderNumber, string quantity, string serialNumber)
        {
            try
            {
                // 验证文件信息
                if (fileInfo == null)
                {
                    return ValidationResult.Failure("文件信息不能为空", ValidationErrorType.InvalidFileInfo, "ValidateFileGridInput");
                }

                if (!fileInfo.Exists)
                {
                    return ValidationResult.Failure($"文件不存在: {fileInfo.FullName}", ValidationErrorType.InvalidFileInfo, "ValidateFileGridInput");
                }

                // 验证数量和序号至少有一个不为空
                if (string.IsNullOrEmpty(quantity) && string.IsNullOrEmpty(serialNumber))
                {
                    return ValidationResult.Failure("数量和序号不能同时为空", ValidationErrorType.InvalidParameters, "ValidateFileGridInput");
                }

                // 验证临时文件
                if (IsTemporaryFile())
                {
                    return ValidationResult.Failure("不能添加临时文件", ValidationErrorType.InvalidFileInfo, "ValidateFileGridInput");
                }

                // 验证正则表达式选择
                if (cmbRegex.SelectedItem == null)
                {
                    return ValidationResult.Failure("请先选择正则表达式", ValidationErrorType.InvalidParameters, "ValidateFileGridInput");
                }

                var patternName = cmbRegex.SelectedItem.ToString();
                if (!regexPatterns.TryGetValue(patternName, out string pattern))
                {
                    return ValidationResult.Failure("所选正则表达式不存在", ValidationErrorType.InvalidParameters, "ValidateFileGridInput");
                }

                // 验证序号格式（如果不为空）
                if (!string.IsNullOrEmpty(serialNumber))
                {
                    if (!int.TryParse(serialNumber, out int serial) || serial <= 0)
                    {
                        return ValidationResult.Failure($"序号格式无效: {serialNumber}", ValidationErrorType.InvalidSerialNumber, "ValidateFileGridInput");
                    }
                }

                // 验证数量格式（如果不为空）
                if (!string.IsNullOrEmpty(quantity))
                {
                    if (!int.TryParse(quantity, out int qty) || qty <= 0)
                    {
                        return ValidationResult.Failure($"数量格式无效: {quantity}", ValidationErrorType.InvalidQuantity, "ValidateFileGridInput");
                    }
                }

                return ValidationResult.Success("ValidateFileGridInput");
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"验证过程中发生异常: {ex.Message}", ValidationErrorType.General, "ValidateFileGridInput");
            }
        }

        /// <summary>
        /// 处理正则表达式匹配
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="pattern">正则表达式模式</param>
        /// <param name="patternName">模式名称</param>
        /// <returns>正则匹配结果</returns>
        private RegexMatchResult ProcessRegexMatch(string fileName, string pattern, string patternName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return RegexMatchResult.Failure("", "", "文件名不能为空", "ProcessRegexMatch");
                }

                if (string.IsNullOrEmpty(pattern))
                {
                    return RegexMatchResult.Failure(fileName, "", "正则表达式模式不能为空", "ProcessRegexMatch");
                }

                var originalName = Path.GetFileNameWithoutExtension(fileName);
                var regexMatch = Regex.Match(originalName, pattern);
                
                if (regexMatch.Success)
                {
                    // 针对特定模式增加特殊处理
                    if (pattern == "^(.*?)-\\d+\\+.*$")
                    {
                        // 确保只获取第一个捕获组
                        if (regexMatch.Groups.Count > 1)
                        {
                            var result = RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                            result.MatchedText = regexMatch.Groups[1].Value;
                            return result;
                        }
                        else
                        {
                            return RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                        }
                    }
                    else
                    {
                        return RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                    }
                }
                else
                {
                    return RegexMatchResult.Failure(originalName, pattern, $"正则表达式匹配失败: 模式='{pattern}', 文本='{originalName}'", "ProcessRegexMatch");
                }
            }
            catch (Exception ex)
            {
                return RegexMatchResult.Failure(fileName, pattern, $"正则匹配过程中发生异常: {ex.Message}", "ProcessRegexMatch");
            }
        }

        /// <summary>
        /// 构建新文件名
        /// </summary>
        /// <param name="components">文件名组件</param>
        /// <returns>构建的新文件名</returns>
        private string BuildNewFileName(FileNameComponents components)
        {
            try
            {
                // 首先验证组件
                var validationResult = components.Validate();
                if (!validationResult.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine($"文件名组件验证失败: {validationResult.ErrorMessage}");
                    return $"未命名{components.FileExtension}";
                }

                // 使用FileNameComponents的内置方法构建文件名
                string newFileName = components.BuildFileName();
                
                // 如果构建失败，使用备用方案
                if (string.IsNullOrEmpty(newFileName) || newFileName == components.FileExtension)
                {
                    return $"未命名{components.FileExtension}";
                }

                return newFileName;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"构建文件名时发生异常: {ex.Message}");
                return $"未命名{components.FileExtension}";
            }
        }

        /// <summary>
        /// 计算和格式化显示尺寸
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="adjustedDimensions">调整后的尺寸字符串</param>
        /// <returns>格式化的尺寸字符串</returns>
        private string CalculateDisplayDimensions(FileInfo fileInfo, string adjustedDimensions)
        {
            try
            {
                string displayDimensions = "未知尺寸";

                // 尝试从adjustedDimensions提取尺寸
                if (!string.IsNullOrEmpty(adjustedDimensions))
                {
                    var matches = System.Text.RegularExpressions.Regex.Matches(adjustedDimensions, @"\d+(?:\.\d+)?");
                    var dimensionsParts = matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToArray();

                    if (dimensionsParts.Length >= 2 &&
                        double.TryParse(dimensionsParts[0], out double width) &&
                        double.TryParse(dimensionsParts[1], out double height))
                    {
                        // 确保大数在前
                        double maxDim = Math.Max(width, height);
                        double minDim = Math.Min(width, height);
                        displayDimensions = $"{maxDim}x{minDim}";
                    }
                    else if (dimensionsParts.Length == 1)
                    {
                        displayDimensions = dimensionsParts[0];
                    }
                }

                // 如果adjustedDimensions为空或提取失败，直接从SettingsForm获取尺寸
                if (displayDimensions == "未知尺寸")
                {
                    displayDimensions = CalculatePdfDimensions(fileInfo.FullName);
                }

                return displayDimensions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"计算显示尺寸时发生异常: {ex.Message}");
                return "未知尺寸";
            }
        }

        /// <summary>
        /// 从 PDF 文件计算尺寸
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>计算的尺寸字符串</returns>
        private string CalculatePdfDimensions(string filePath)
        {
            try
            {
                using (var settingsForm = new SettingsForm())
                {
                    settingsForm.RecognizePdfDimensions(filePath);
                    double tetBleed = 0;
                    string bleedValues = AppSettings.Get("TetBleedValues")?.ToString() ?? "0";
                    string[] bleedParts = bleedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (bleedParts.Length > 0 && double.TryParse(bleedParts[0].Trim(), out double bleedValue))
                    {
                        tetBleed = bleedValue;
                    }
                    string pdfDimensions = settingsForm.CalculateFinalDimensions(settingsForm.PdfWidth, settingsForm.PdfHeight, tetBleed);
                    if (!string.IsNullOrEmpty(pdfDimensions))
                    {
                        var matches = System.Text.RegularExpressions.Regex.Matches(pdfDimensions, @"\d+(?:\.\d+)?");
                        var dimensionsParts = matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToArray();

                        if (dimensionsParts.Length >= 2 &&
                            double.TryParse(dimensionsParts[0], out double width) &&
                            double.TryParse(dimensionsParts[1], out double height))
                        {
                            // 确保大数在前
                            double maxDim = Math.Max(width, height);
                            double minDim = Math.Min(width, height);
                            return $"{maxDim}x{minDim}";
                        }
                        else if (dimensionsParts.Length == 1)
                        {
                            return dimensionsParts[0];
                        }
                        else
                        {
                            return pdfDimensions;
                        }
                    }
                }
                return "未知尺寸";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"计算PDF尺寸时发生异常: {ex.Message}");
                return "未知尺寸";
            }
        }

        /// <summary>
        /// 从设置中解析文件名组件配置
        /// </summary>
        /// <returns>文件名组件配置</returns>
        private FileNameComponentsConfig ParseFileNameConfig()
        {
            var config = new FileNameComponentsConfig();
            
            try
            {
                string eventItems = AppSettings.EventItems ?? string.Empty;
                var eventItemPairs = eventItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < eventItemPairs.Length; i += 2)
                {
                    if (i + 1 < eventItemPairs.Length)
                    {
                        string itemText = eventItemPairs[i];
                        if (bool.TryParse(eventItemPairs[i + 1], out bool isEnabled))
                        {
                            switch (itemText)
                            {
                                case "正则结果":
                                    config.RegexResultEnabled = isEnabled;
                                    break;
                                case "订单号":
                                    config.OrderNumberEnabled = isEnabled;
                                    break;
                                case "材料":
                                    config.MaterialEnabled = isEnabled;
                                    break;
                                case "数量":
                                    config.QuantityEnabled = isEnabled;
                                    break;
                                case "尺寸":
                                    config.DimensionsEnabled = isEnabled;
                                    break;
                                case "工艺":
                                    config.ProcessEnabled = isEnabled;
                                    break;
                                case "序号":
                                    config.SerialNumberEnabled = isEnabled;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析文件名配置时发生异常: {ex.Message}");
                // 异常情况下返回默认配置（全部启用）
            }
            
            return config;
        }

        /// <summary>
        /// 从EventGroup配置中获取前缀字典
        /// </summary>
        /// <returns>前缀字典，键为组件名称，值为前缀</returns>
        private Dictionary<string, string> GetPrefixesFromEventGroupConfig()
        {
            var prefixes = new Dictionary<string, string>();

            try
            {
                // 获取EventGroup配置
                var eventGroupConfig = SettingsForm.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups == null)
                {
                    System.Diagnostics.Debug.WriteLine("EventGroup配置为空，使用空前缀字典");
                    return prefixes;
                }

                // 遍历所有分组，建立组件名到前缀的映射
                foreach (var group in eventGroupConfig.Groups)
                {
                    // 获取该分组下的所有项目
                    var groupItems = eventGroupConfig.Items.Where(item => item.GroupId == group.Id);

                    foreach (var item in groupItems)
                    {
                        // 如果没有前缀则跳过
                        if (string.IsNullOrEmpty(group.Prefix))
                            continue;

                        // 建立组件名到前缀的映射
                        if (!prefixes.ContainsKey(item.Name))
                        {
                            prefixes[item.Name] = group.Prefix;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"获取到的前缀配置: {string.Join(", ", prefixes.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取前缀配置时发生异常: {ex.Message}");
            }

            return prefixes;
        }

        /// <summary>
        /// 生成或验证序号
        /// </summary>
        /// <param name="providedSerialNumber">提供的序号（可能为空）</param>
        /// <param name="bindingList">数据绑定列表</param>
        /// <param name="isFromExcel">是否来自Excel导入</param>
        /// <returns>最终使用的序号</returns>
        private string GenerateSerialNumber(string providedSerialNumber, BindingList<FileRenameInfo> bindingList, bool isFromExcel)
        {
            try
            {
                // 如果已提供序号且来自Excel，直接使用
                if (!string.IsNullOrEmpty(providedSerialNumber) && isFromExcel)
                {
                    return providedSerialNumber;
                }

                // 如果提供了序号但不是来自Excel，验证并使用
                if (!string.IsNullOrEmpty(providedSerialNumber))
                {
                    if (int.TryParse(providedSerialNumber, out int provided) && provided > 0)
                    {
                        return provided.ToString("D2");
                    }
                }

                // 需要自动生成序号
                int maxSerial = 0;
                if (bindingList?.Any() == true)
                {
                    // 遍历现有数据，找到最大序号
                    foreach (var item in bindingList)
                    {
                        if (int.TryParse(item.SerialNumber, out int currentSerial))
                        {
                            if (currentSerial > maxSerial)
                            {
                                maxSerial = currentSerial;
                            }
                        }
                    }
                }

                // 返回下一个序号，格式化为两位数
                return (maxSerial + 1).ToString("D2");
            }
            catch (Exception ex)
            {
                // 异常情况下返回默认序号
                System.Diagnostics.Debug.WriteLine($"生成序号时发生异常: {ex.Message}");
                return "01";
            }
        }

        /// <summary>
        /// 处理PDF文件，包括图层添加和文件操作
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <param name="options">PDF处理选项</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <returns>处理是否成功</returns>
        private bool ProcessPdfFile(string sourceFilePath, string destPath, PdfProcessingOptions options, bool isCopyMode)
        {
            string backupFilePath = null;
            try
            {
                // 检查是否需要PDF处理
                if (!options.RequiresPdfProcessing())
                {
                    // 不需要PDF特殊处理，直接执行文件操作
                    return ExecuteSimpleFileOperation(sourceFilePath, destPath, isCopyMode);
                }

                // 生成缓存文件夹和临时文件路径
                var cacheFolder = options.GenerateCacheFolder();
                Directory.CreateDirectory(cacheFolder);
                var tempFilePath = options.GenerateTempFilePath(cacheFolder);

                // 验证PDF处理选项
                var validationResult = options.Validate();
                if (!validationResult.IsValid)
                {
                    System.Diagnostics.Debug.WriteLine($"PDF处理选项验证失败: {validationResult.ErrorMessage}");
                    return false;
                }

                try
                {
                    // 复制原始文件到临时路径进行处理
                    File.Copy(sourceFilePath, tempFilePath, true);

                    // 检查临时文件是否已经存在所需图层
                    if (options.RequireLayerCheck)
                    {
                        bool layersExist = _pdfDimensionService.CheckPdfLayersExist(
                            tempFilePath,
                            options.TargetLayerNames);
                            
                        if (!layersExist)
                        {
                            // 对临时文件添加图层
                            var pdfProcessingService = ServiceLocator.Instance.GetPdfProcessingService();
#pragma warning disable CS0618 // 禁用过时API警告
                            bool layerAdded = ((WindowsFormsApp3.Interfaces.IPdfProcessingService)pdfProcessingService).AddDotsAddCounterLayer(
                                tempFilePath,
                                options.FinalDimensions,
                                options.ShapeType,
                                options.RoundRadius);
#pragma warning restore CS0618 // 恢复警告
                                
                            if (!layerAdded)
                            {
                                System.Diagnostics.Debug.WriteLine($"为临时文件添加Dots_AddCounter图层失败: {tempFilePath}");
                                return false;
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"临时文件中已存在所需图层，跳过添加: {tempFilePath}");
                        }
                    }

                    // 验证处理后的临时文件完整性
                    if (!File.Exists(tempFilePath) || new FileInfo(tempFilePath).Length == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理后的临时文件无效: {tempFilePath}");
                        return false;
                    }

                    // 确保目标目录存在
                    string destDirectory = Path.GetDirectoryName(destPath);
                    if (!Directory.Exists(destDirectory))
                    {
                        Directory.CreateDirectory(destDirectory);
                    }

                    // 处理目标文件冲突
                    var (newFileName, finalDestPath) = ResolveFileNameConflict(Path.GetDirectoryName(destPath), Path.GetFileName(destPath));

                    // 对于移动模式，先备份原文件（如果目标文件已存在）
                    if (!isCopyMode && File.Exists(finalDestPath))
                    {
                        backupFilePath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.pdf");
                        File.Copy(finalDestPath, backupFilePath, true);
                        File.Delete(finalDestPath);
                    }

                    // 将处理完成的临时文件移动到目标路径
                    // 使用File.Move而不是File.Copy，确保原子性操作
                    File.Move(tempFilePath, finalDestPath);

                    // 对于移动模式，删除源文件
                    if (!isCopyMode)
                    {
                        File.Delete(sourceFilePath);
                    }

                    // 验证最终文件操作成功
                    if (!File.Exists(finalDestPath) || new FileInfo(finalDestPath).Length == 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"最终文件操作失败: {finalDestPath}");
                        // 尝试从备份恢复（如果有）
                        if (backupFilePath != null && File.Exists(backupFilePath))
                        {
                            File.Copy(backupFilePath, finalDestPath, true);
                        }
                        return false;
                    }

                    System.Diagnostics.Debug.WriteLine($"文件处理成功: {sourceFilePath} -> {finalDestPath}");
                    return true;
                }
                finally
                {
                    // 清理临时文件
                    CleanupTempFile(tempFilePath);
                    
                    // 清理备份文件
                    if (backupFilePath != null && File.Exists(backupFilePath))
                    {
                        try
                        {
                            File.Delete(backupFilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"清理备份文件失败: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理PDF文件时出错: {ex.Message}");
                
                // 尝试从备份恢复
                if (backupFilePath != null && File.Exists(backupFilePath))
                {
                    try
                    {
                        File.Copy(backupFilePath, destPath, true);
                        System.Diagnostics.Debug.WriteLine($"从备份恢复文件成功: {destPath}");
                    }
                    catch (Exception restoreEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"从备份恢复失败: {restoreEx.Message}");
                    }
                }
                
                return false;
            }
        }

        /// <summary>
        /// 执行简单文件操作（非PDF或不需要特殊处理的PDF）
        /// </summary>
        /// <param name="sourceFilePath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <returns>操作是否成功</returns>
        private bool ExecuteSimpleFileOperation(string sourceFilePath, string destPath, bool isCopyMode)
        {
            string backupFilePath = null;
            try
            {
                // 确保源文件存在且有效
                if (!File.Exists(sourceFilePath) || new FileInfo(sourceFilePath).Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"源文件无效: {sourceFilePath}");
                    return false;
                }

                // 确保目标目录存在
                string destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory))
                {
                    Directory.CreateDirectory(destDirectory);
                }

                // 处理目标文件冲突
                var (newFileName, finalDestPath) = ResolveFileNameConflict(Path.GetDirectoryName(destPath), Path.GetFileName(destPath));

                // 对于移动模式，先备份原文件（如果目标文件已存在）
                if (!isCopyMode && File.Exists(finalDestPath))
                {
                    backupFilePath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.pdf");
                    File.Copy(finalDestPath, backupFilePath, true);
                    File.Delete(finalDestPath);
                }

                // 执行文件操作
                if (isCopyMode)
                {
                    File.Copy(sourceFilePath, finalDestPath, false); // 不覆盖，因为我们已经处理了冲突
                }
                else
                {
                    File.Move(sourceFilePath, finalDestPath);
                }

                // 验证操作结果
                if (!File.Exists(finalDestPath) || new FileInfo(finalDestPath).Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"文件操作失败: {sourceFilePath} -> {finalDestPath}");
                    // 尝试从备份恢复
                    if (backupFilePath != null && File.Exists(backupFilePath))
                    {
                        File.Copy(backupFilePath, finalDestPath, true);
                    }
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"简单文件操作成功: {sourceFilePath} -> {finalDestPath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"执行文件操作时出错: {ex.Message}");
                
                // 尝试从备份恢复
                if (backupFilePath != null && File.Exists(backupFilePath))
                {
                    try
                    {
                        File.Copy(backupFilePath, destPath, true);
                        System.Diagnostics.Debug.WriteLine($"从备份恢复文件成功: {destPath}");
                    }
                    catch (Exception restoreEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"从备份恢复失败: {restoreEx.Message}");
                    }
                }
                
                return false;
            }
            finally
            {
                // 清理备份文件
                if (backupFilePath != null && File.Exists(backupFilePath))
                {
                    try
                    {
                        File.Delete(backupFilePath);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"清理备份文件失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        /// <param name="tempFilePath">临时文件路径</param>
        private void CleanupTempFile(string tempFilePath)
        {
            if (File.Exists(tempFilePath))
            {
                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"清理临时文件失败: {ex.Message}");
                    // 不抛出异常，以免影响主流程
                }
            }
        }

        /// <summary>
        /// 解决文件名冲突
        /// </summary>
        /// <param name="exportPath">导出路径</param>
        /// <param name="originalFileName">原始文件名</param>
        /// <returns>解决冲突后的文件名和完整路径</returns>
        private (string newFileName, string destPath) ResolveFileNameConflict(string exportPath, string originalFileName)
        {
            try
            {
                // 构建初始目标路径
                var destPath = Path.Combine(exportPath, originalFileName);
                var newFileName = originalFileName;

                // 检查文件是否已存在
                if (File.Exists(destPath))
                {
                    var fileName = Path.GetFileNameWithoutExtension(destPath);
                    var extension = Path.GetExtension(destPath);
                    var counter = 1;
                    
                    // 循环尝试新的文件名，直到找到不冲突的名称
                    while (File.Exists(destPath))
                    {
                        newFileName = $"{fileName}_{counter}{extension}";
                        destPath = Path.Combine(exportPath, newFileName);
                        counter++;
                        
                        // 防止无限循环，设置最大尝试次数
                        if (counter > 9999)
                        {
                            throw new InvalidOperationException("无法生成唯一的文件名，尝试次数过多");
                        }
                    }
                }

                return (newFileName, destPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解决文件名冲突时发生异常: {ex.Message}");
                // 异常情况下返回原始文件名和路径
                return (originalFileName, Path.Combine(exportPath, originalFileName));
            }
        }

        /// <summary>
        /// 验证文件重命名操作的前置条件
        /// </summary>
        /// <returns>重命名验证结果</returns>
        private RenameValidationResult ValidateRenameOperation()
        {
            try
            {
                // 验证临时文件
                if (IsTemporaryFile())
                {
                    return RenameValidationResult.TemporaryFile("检测到临时文件，跳过处理");
                }

                // 验证正则表达式选择
                if (cmbRegex.SelectedItem == null)
                {
                    return RenameValidationResult.InvalidRegexSelection("未选择正则表达式");
                }

                var patternName = cmbRegex.SelectedItem.ToString();
                if (!regexPatterns.TryGetValue(patternName, out string pattern))
                {
                    return RenameValidationResult.InvalidRegexSelection($"正则表达式模式不存在: {patternName}");
                }

                // 验证导出路径（这里需要传入exportPath参数才能验证）
                // 暂时返回成功，在调用时再验证具体路径
                return RenameValidationResult.Success(patternName, pattern, "重命名操作验证通过");
            }
            catch (Exception ex)
            {
                return RenameValidationResult.Failure($"验证过程中发生异常: {ex.Message}", ValidationErrorType.General, $"ValidateRenameOperation异常: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 验证文件重命名操作的前置条件（包含导出路径验证）
        /// </summary>
        /// <param name="exportPath">导出路径</param>
        /// <returns>重命名验证结果</returns>
        private RenameValidationResult ValidateRenameOperation(string exportPath)
        {
            try
            {
                // 先执行基础验证
                var baseValidation = ValidateRenameOperation();
                if (!baseValidation.IsValid)
                {
                    return baseValidation;
                }

                // 验证导出路径
                if (string.IsNullOrEmpty(exportPath) || !Directory.Exists(exportPath))
                {
                    return RenameValidationResult.InvalidExportPath(exportPath, $"导出路径无效或不存在: {exportPath}");
                }

                // 返回成功结果，包含验证通过的模式信息
                return RenameValidationResult.Success(baseValidation.PatternName, baseValidation.Pattern, $"重命名操作验证通过，导出路径: {exportPath}");
            }
            catch (Exception ex)
            {
                return RenameValidationResult.Failure($"验证过程中发生异常: {ex.Message}", ValidationErrorType.General, $"ValidateRenameOperation(exportPath)异常: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 协调方法：将文件添加到网格
        /// 这是一个高层次的协调方法，调用各个子方法完成复杂的处理逻辑
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="material">材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="unit">单位</param>
        /// <param name="adjustedDimensions">调整后的尺寸</param>
        /// <param name="fixedField">固定字段</param>
        /// <param name="serialNumber">序号</param>
        /// <param name="compositeColumnValue">列组合值</param>
        private void AddFileToGrid(FileInfo fileInfo, string material, string orderNumber, string quantity, string unit, string adjustedDimensions, string fixedField, string serialNumber, string compositeColumnValue = "")
        {
            try
            {
                LogDebugInfo($"AddFileToGrid被调用: 文件={fileInfo.Name}, 列组合值='{compositeColumnValue}'");
                
                // 将业务逻辑委托给Presenter处理
                _presenter.HandleAddFileToGrid(fileInfo, material, orderNumber, quantity, unit, adjustedDimensions, fixedField, serialNumber, compositeColumnValue);
                
                // 注意：Presenter已经处理了列组合值的设置，这里不需要重复设置
                // 移除重复的列组合值设置逻辑以避免产生无效数据
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddFileToGrid处理失败: {ex.Message}");
                MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理文件名中的正则表达式匹配
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>正则匹配结果，如果用户取消则返回null</returns>
        private string ProcessRegexForFileName(string fileName)
        {
            var config = ParseFileNameConfig();
            if (!config.RegexResultEnabled)
            {
                return string.Empty;
            }

            var patternName = cmbRegex.SelectedItem.ToString();
            regexPatterns.TryGetValue(patternName, out string pattern);

            var regexResult = ProcessRegexMatch(fileName, pattern, patternName);
            if (!regexResult.IsMatch)
            {
                DialogResult result = MessageBox.Show(
                    "正则表达式匹配失败，是否使用完整原文件名代替正则部分？", 
                    "匹配失败", 
                    MessageBoxButtons.OKCancel, 
                    MessageBoxIcon.Question);
                    
                if (result == DialogResult.OK)
                {
                    return Path.GetFileNameWithoutExtension(fileName);
                }
                else
                {
                    return null; // 表示用户取消
                }
            }
            
            return regexResult.MatchedText;
        }

        /// <summary>
        /// 创建文件名组件对象
        /// </summary>
        private FileNameComponents CreateFileNameComponents(FileInfo fileInfo, string material, string orderNumber, 
            string quantity, string unit, string dimensions, string notes, string serialNumber, string regexPart,
            string separator, FileNameComponentsConfig enabledComponents)
        {
            try
            {
                LogHelper.Debug($"CreateFileNameComponents: 开始创建文件名组件 - 文件: {fileInfo.Name}");

                // 获取EventGroup配置
                var eventGroupConfig = SettingsForm.GetEventGroupConfiguration();

                // 构建保留分组配置字典
                var preserveGroupConfig = new Dictionary<string, bool>();
                if (eventGroupConfig?.Groups != null)
                {
                    foreach (var group in eventGroupConfig.Groups)
                    {
                        preserveGroupConfig[group.Id] = group.IsPreserved;
                        LogHelper.Debug($"CreateFileNameComponents: 保留分组配置: {group.DisplayName} ({group.Id}) -> {group.IsPreserved}");
                    }
                }

                var components = new FileNameComponents
                {
                    RegexResult = regexPart,
                    OrderNumber = orderNumber,
                    Material = material,
                    Quantity = quantity,
                    Unit = unit,
                    Dimensions = dimensions,
                    Process = notes,
                    SerialNumber = serialNumber,
                    FileExtension = fileInfo.Extension,
                    Separator = separator,
                    EnabledComponents = enabledComponents,
                    // 从EventGroup配置中获取前缀
                    Prefixes = GetPrefixesFromEventGroupConfig(),
                    // 设置保留分组配置
                    PreserveGroupConfig = preserveGroupConfig,
                    // 设置原始文件名用于保留分组检测
                    OriginalFileName = Path.GetFileNameWithoutExtension(fileInfo.Name)
                };

                LogHelper.Debug($"CreateFileNameComponents: 原始文件名: '{components.OriginalFileName}'");
                LogHelper.Debug($"CreateFileNameComponents: 组件值 - 正则结果:{regexPart}, 订单号:{orderNumber}, 材料:{material}, 数量:{quantity}{unit}, 尺寸:{dimensions}, 工艺:{notes}, 序号:{serialNumber}");
                LogHelper.Debug($"CreateFileNameComponents: 启用状态 - 正则结果:{enabledComponents.RegexResultEnabled}, 订单号:{enabledComponents.OrderNumberEnabled}, 材料:{enabledComponents.MaterialEnabled}, 数量:{enabledComponents.QuantityEnabled}, 工艺:{enabledComponents.ProcessEnabled}, 尺寸:{enabledComponents.DimensionsEnabled}, 序号:{enabledComponents.SerialNumberEnabled}");

                // 设置组件顺序
                SetComponentOrderFromEventItems(components);

                // 确保分隔符为合法文件名字符（允许空字符串）
                if (!string.IsNullOrEmpty(components.Separator) && Path.GetInvalidFileNameChars().Contains(components.Separator[0]))
                {
                    LogHelper.Warn($"CreateFileNameComponents: 分隔符 '{components.Separator}' 包含非法字符，自动替换为 '_'");
                    components.Separator = "_";
                }

                LogHelper.Debug($"CreateFileNameComponents: 最终分隔符 = '{components.Separator}'");
                
                return components;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"CreateFileNameComponents: 创建文件名组件时发生异常: {ex.Message}");
                LogHelper.Debug($"CreateFileNameComponents: 异常详情: {ex.StackTrace}");
                
                // 返回基本的组件对象
                return new FileNameComponents
                {
                    RegexResult = regexPart ?? string.Empty,
                    OrderNumber = orderNumber ?? string.Empty,
                    Material = material ?? string.Empty,
                    Quantity = quantity ?? string.Empty,
                    Unit = unit ?? string.Empty,
                    Dimensions = dimensions ?? string.Empty,
                    Process = notes ?? string.Empty,
                    SerialNumber = serialNumber ?? string.Empty,
                    FileExtension = fileInfo.Extension,
                    Separator = separator ?? "-",
                    EnabledComponents = enabledComponents,
                    Prefixes = new Dictionary<string, string>()
                };
            }
        }

        /// <summary>
        /// 创建处理后的数据对象
        /// </summary>
        private ProcessedFileData CreateProcessedFileData(string newFileName, string destinationPath, string regexResult, string orderNumber, string material, string quantity, string dimensions, string notes, string serialNumber, string compositeColumnValue = null)
        {
            return new ProcessedFileData
            {
                NewFileName = newFileName,
                DestinationPath = destinationPath,
                RegexResult = regexResult,
                OrderNumber = orderNumber,
                Material = material,
                Quantity = quantity,
                Dimensions = dimensions,
                Process = notes,
                SerialNumber = serialNumber,
                CompositeColumn = compositeColumnValue
            };
        }

        /// <summary>
        /// 构建新文件名
        /// </summary>
        private string BuildNewFileNameForRename(FileInfo fileInfo, string selectedMaterial, string orderNumber, 
            string quantity, string unit, string fixedField, string serialNumber, string pattern, 
            double tetBleed, string cornerRadius, bool addPdfLayers)
        {
            try
            {
                // 获取正则结果
                string regexPart = string.Empty;
                var originalName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                
                // 检查"正则结果"是否启用
                bool isRegexEnabled = false;
                string eventItems = AppSettings.EventItems ?? string.Empty;
                var regexEventItems = eventItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < regexEventItems.Length; i += 2)
                {
                    if (i + 1 < regexEventItems.Length && regexEventItems[i] == "正则结果")
                    {
                        isRegexEnabled = bool.Parse(regexEventItems[i + 1]);
                        break;
                    }
                }

                if (isRegexEnabled)
                {
                    // 使用ProcessRegexForFileName方法处理正则匹配
                    regexPart = _fileRenameService.ProcessRegexForFileName(pattern, originalName);
                    if (string.IsNullOrEmpty(regexPart))
                    {
                        // 正则匹配失败，询问用户是否使用完整原文件名
                        DialogResult result = MessageBox.Show($"正则表达式匹配失败：模式='{pattern}', 文本='{originalName}'\n\n是否使用完整原文件名替代正则部分？", "匹配失败", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
                        if (result == DialogResult.OK)
                        {
                            regexPart = originalName;
                        }
                        else
                        {
                            return string.Empty; // 用户取消
                        }
                    }
                }

                // 获取尺寸信息
                string dimensions = string.Empty;
                bool dimensionsEnabled = false;
                var renameEventItems = eventItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < renameEventItems.Length; i += 2)
                {
                    if (i + 1 < renameEventItems.Length && renameEventItems[i] == "尺寸")
                    {
                        dimensionsEnabled = bool.Parse(renameEventItems[i + 1]);
                        break;
                    }
                }

                if (dimensionsEnabled)
                {
                    // 获取PDF尺寸信息
                    using (var settingsForm = new SettingsForm())
                    {
                        settingsForm.RecognizePdfDimensions(fileInfo.FullName);
                        dimensions = settingsForm.CalculateFinalDimensions(settingsForm.PdfWidth, settingsForm.PdfHeight, tetBleed, cornerRadius, addPdfLayers);
                        
                        if (!string.IsNullOrEmpty(dimensions))
                        {
                            var dimensionsParts = dimensions.Split('x');
                            if (dimensionsParts.Length >= 2)
                            {
                                // 检查是否有形状部分
                                string shapePart = string.Empty;
                                if (dimensionsParts[1].Contains('R') || dimensionsParts[1].Contains('Y'))
                                {
                                    var parts = dimensionsParts[1].Split(new char[] { 'R', 'Y' }, 2);
                                    if (parts.Length > 0 && double.TryParse(parts[0], out double dim2))
                                    {
                                        if (double.TryParse(dimensionsParts[0], out double dim1))
                                        {
                                            // 确保大数在前，但保留形状部分
                                            double maxDim = Math.Max(dim1, dim2);
                                            double minDim = Math.Min(dim1, dim2);
                                            shapePart = dimensionsParts[1].Substring(parts[0].Length);
                                            dimensions = $"{maxDim}x{minDim}{shapePart}";
                                        }
                                    }
                                }
                                else if (double.TryParse(dimensionsParts[0], out double dim1) &&
                                         double.TryParse(dimensionsParts[1], out double dim2))
                                {
                                    // 确保大数在前
                                    double maxDim = Math.Max(dim1, dim2);
                                    double minDim = Math.Min(dim1, dim2);
                                    dimensions = $"{maxDim}x{minDim}";
                                }
                            }
                        }
                    }
                }

                // 获取行数和列数
                string layoutRows = SettingsForm.GetLayoutRowsForRenaming();
                string layoutColumns = SettingsForm.GetLayoutColumnsForRenaming();

                // 创建FileNameComponents配置
                var enabledComponents = CreateFileNameComponentsConfigFromEventItems();

                // 使用FileRenameService创建FileNameComponents
                var components = _fileRenameService.CreateFileNameComponents(
                    fileInfo, 
                    selectedMaterial, 
                    orderNumber, 
                    quantity, 
                    unit, 
                    dimensions, 
                    fixedField, 
                    serialNumber, 
                    regexPart,
                    AppSettings.Separator ?? string.Empty,
                    enabledComponents
                );

                // 设置组件顺序（如果有的话）
                SetComponentOrderFromEventItems(components);

                // 构建新文件名
                return BuildNewFileName(components);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"构建重命名文件名时发生异常: {ex.Message}");
                LogHelper.Debug($"构建重命名文件名时发生异常: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 修复TextItems配置，确保序号被禁用
        /// </summary>
        private void FixTextItemsConfiguration()
        {
            try
            {
                string textItems = AppSettings.Instance["TextItems"]?.ToString() ?? string.Empty;
                LogHelper.Debug($"FixTextItemsConfiguration: 原始TextItems = '{textItems}'");

                if (!string.IsNullOrEmpty(textItems))
                {
                    var items = textItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    bool textItemsModified = false;

                    // 验证配置格式的正确性
                    for (int i = 0; i < items.Count; i += 2)
                    {
                        if (i + 1 < items.Count)
                        {
                            string itemText = items[i];
                            string enabledValue = items[i + 1];
                            
                            // 验证启用值是否为有效的布尔值
                            if (!bool.TryParse(enabledValue, out bool enabled))
                            {
                                LogHelper.Warn($"FixTextItemsConfiguration: 发现无效的布尔值 '{enabledValue}' 对于项目 '{itemText}'，自动修复为False");
                                items[i + 1] = "False";
                                textItemsModified = true;
                            }
                            
                            LogHelper.Debug($"FixTextItemsConfiguration: 项目 '{itemText}' = {enabledValue}");
                        }
                    }

                    // 如果修改了配置，保存修复后的结果
                    if (textItemsModified)
                    {
                        string fixedTextItems = string.Join("|", items);
                        AppSettings.Set("TextItems", fixedTextItems);
                        LogHelper.Debug($"FixTextItemsConfiguration: 修复后的TextItems = '{fixedTextItems}'");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("修复TextItems配置失败: " + ex.Message);
            }
        }

      private FileNameComponentsConfig CreateFileNameComponentsConfigFromEventItems()
        {
            var config = new FileNameComponentsConfig();

            try
            {
                // 首先尝试从EventGroup配置获取组件启用状态
                var eventGroupConfig = SettingsForm.GetEventGroupConfiguration();
                if (eventGroupConfig?.Items != null)
                {
                    LogHelper.Debug("CreateFileNameComponentsConfigFromEventItems: 使用EventGroup配置设置组件启用状态");
                    
                    foreach (var item in eventGroupConfig.Items)
                    {
                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 处理EventGroup项目 '{item.Name}' = {item.IsEnabled}");

                        switch (item.Name)
                        {
                            case "正则结果":
                                config.RegexResultEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 正则结果启用状态 = {item.IsEnabled}");
                                break;
                            case "订单号":
                                config.OrderNumberEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 订单号启用状态 = {item.IsEnabled}");
                                break;
                            case "材料":
                                config.MaterialEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 材料启用状态 = {item.IsEnabled}");
                                break;
                            case "数量":
                                config.QuantityEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 数量启用状态 = {item.IsEnabled}");
                                break;
                            case "尺寸":
                                config.DimensionsEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 尺寸启用状态 = {item.IsEnabled}");
                                break;
                            case "工艺":
                                config.ProcessEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 工艺启用状态 = {item.IsEnabled}");
                                break;
                            case "序号":
                                config.SerialNumberEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 序号启用状态 = {item.IsEnabled}");
                                break;
                            case "列组合":
                                config.CompositeColumnEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 列组合启用状态 = {item.IsEnabled}");
                                break;
                            case "行数":
                                config.LayoutRowsEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 行数启用状态 = {item.IsEnabled}");
                                break;
                            case "列数":
                                config.LayoutColumnsEnabled = item.IsEnabled;
                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 列数启用状态 = {item.IsEnabled}");
                                break;
                            default:
                                LogHelper.Warn($"CreateFileNameComponentsConfigFromEventItems: EventGroup中未知的组件类型 '{item.Name}'");
                                break;
                        }
                    }
                }
                else
                {
                    LogHelper.Warn("CreateFileNameComponentsConfigFromEventItems: EventGroup配置为空，尝试使用TextItems配置");
                    
                    // 修复TextItems配置中的格式问题
                    FixTextItemsConfiguration();

                    // 从CustomSettings.TextItems获取组件配置
                    string textItems = AppSettings.Instance["TextItems"]?.ToString() ?? string.Empty;
                    LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: TextItems = '{textItems}'");
                    
                    if (!string.IsNullOrEmpty(textItems))
                    {
                        var items = textItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 从TextItems解析到 {items.Length / 2} 个配置项");

                        for (int i = 0; i < items.Length; i += 2)
                        {
                            if (i + 1 < items.Length)
                            {
                                string itemText = items[i];
                                bool isChecked = bool.Parse(items[i + 1]);

                                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 处理TextItems项目 '{itemText}' = {isChecked}");

                                switch (itemText)
                                {
                                    case "正则结果":
                                        config.RegexResultEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 正则结果启用状态 = {isChecked}");
                                        break;
                                    case "订单号":
                                        config.OrderNumberEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 订单号启用状态 = {isChecked}");
                                        break;
                                    case "材料":
                                        config.MaterialEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 材料启用状态 = {isChecked}");
                                        break;
                                    case "数量":
                                        config.QuantityEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 数量启用状态 = {isChecked}");
                                        break;
                                    case "尺寸":
                                        config.DimensionsEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 尺寸启用状态 = {isChecked}");
                                        break;
                                    case "工艺":
                                        config.ProcessEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 工艺启用状态 = {isChecked}");
                                        break;
                                    case "序号":
                                        config.SerialNumberEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 序号启用状态 = {isChecked}");
                                        break;
                                    case "列组合":
                                        config.CompositeColumnEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 列组合启用状态 = {isChecked}");
                                        break;
                                    case "行数":
                                        config.LayoutRowsEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 行数启用状态 = {isChecked}");
                                        break;
                                    case "列数":
                                        config.LayoutColumnsEnabled = isChecked;
                                        LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 列数启用状态 = {isChecked}");
                                        break;
                                    default:
                                        LogHelper.Warn($"CreateFileNameComponentsConfigFromEventItems: TextItems中未知的组件类型 '{itemText}'");
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Warn("CreateFileNameComponentsConfigFromEventItems: TextItems也为空，尝试使用旧的EventItems作为后备");
                        
                        // 如果TextItems为空，尝试使用旧的EventItems作为后备
                        string eventItems = AppSettings.EventItems ?? string.Empty;
                        if (!string.IsNullOrEmpty(eventItems))
                        {
                            var oldItems = eventItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 从EventItems解析到 {oldItems.Length / 2} 个配置项");

                            for (int i = 0; i < oldItems.Length; i += 2)
                            {
                                if (i + 1 < oldItems.Length)
                                {
                                    string itemText = oldItems[i];
                                    bool isChecked = bool.Parse(oldItems[i + 1]);

                                    LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 从EventItems处理项目 '{itemText}' = {isChecked}");

                                    switch (itemText)
                                    {
                                        case "正则结果":
                                            config.RegexResultEnabled = isChecked;
                                            break;
                                        case "订单号":
                                            config.OrderNumberEnabled = isChecked;
                                            break;
                                        case "材料":
                                            config.MaterialEnabled = isChecked;
                                            break;
                                        case "数量":
                                            config.QuantityEnabled = isChecked;
                                            break;
                                        case "尺寸":
                                            config.DimensionsEnabled = isChecked;
                                            break;
                                        case "工艺":
                                            config.ProcessEnabled = isChecked;
                                            break;
                                        case "序号":
                                            config.SerialNumberEnabled = isChecked;
                                            break;
                                        case "列组合":
                                            config.CompositeColumnEnabled = isChecked;
                                            break;
                                        case "行数":
                                            config.LayoutRowsEnabled = isChecked;
                                            break;
                                        case "列数":
                                            config.LayoutColumnsEnabled = isChecked;
                                            break;
                                        default:
                                            LogHelper.Warn($"CreateFileNameComponentsConfigFromEventItems: EventItems中未知的组件类型 '{itemText}'");
                                            break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            LogHelper.Warn("CreateFileNameComponentsConfigFromEventItems: EventItems也为空，使用默认配置（全部启用）");
                        }
                    }
                }

                LogHelper.Info($"CreateFileNameComponentsConfigFromEventItems: 最终配置 - 正则结果:{config.RegexResultEnabled}, 订单号:{config.OrderNumberEnabled}, 材料:{config.MaterialEnabled}, 数量:{config.QuantityEnabled}, 工艺:{config.ProcessEnabled}, 尺寸:{config.DimensionsEnabled}, 序号:{config.SerialNumberEnabled}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"CreateFileNameComponentsConfigFromEventItems: 创建配置时发生异常: {ex.Message}");
                LogHelper.Debug($"CreateFileNameComponentsConfigFromEventItems: 异常详情: {ex.StackTrace}");
            }

            return config;
        }

        private void SetComponentOrderFromEventItems(FileNameComponents components)
        {
            var componentOrder = new List<string>();
            
            try
            {
                // 首先尝试从EventGroup配置获取组件顺序
                var eventGroupConfig = SettingsForm.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups != null && eventGroupConfig.Items != null)
                {
                    LogHelper.Debug("SetComponentOrderFromEventItems: 使用EventGroup配置设置组件顺序");
                    
                    // 按照组的SortOrder排序
                    var sortedGroups = eventGroupConfig.Groups
                        .Where(g => g.IsEnabled)
                        .OrderBy(g => g.SortOrder)
                        .ToList();
                    
                    LogHelper.Debug($"SetComponentOrderFromEventItems: 启用的组数量: {sortedGroups.Count}");
                    
                    foreach (var group in sortedGroups)
                    {
                        // 获取该组下启用的项目
                        var groupItems = eventGroupConfig.Items
                            .Where(item => item.GroupId == group.Id && item.IsEnabled)
                            .OrderBy(item => item.SortOrder)
                            .ToList();
                        
                        LogHelper.Debug($"SetComponentOrderFromEventItems: 组 '{group.DisplayName}'({group.Id}) 包含 {groupItems.Count} 个启用项目");
                        
                        foreach (var item in groupItems)
                        {
                            // 将EventGroup项目名称映射到FileNameComponents期望的组件类型
                            string componentType = MapEventItemToComponentType(item.Name);
                            if (!string.IsNullOrEmpty(componentType) && !componentOrder.Contains(componentType))
                            {
                                componentOrder.Add(componentType);
                                LogHelper.Debug($"SetComponentOrderFromEventItems: 从EventGroup添加组件 '{componentType}' (原项目名: {item.Name}, 组: {group.DisplayName})");
                            }
                        }
                    }
                }
                else
                {
                    LogHelper.Warn("SetComponentOrderFromEventItems: EventGroup配置为空，尝试使用TextItems配置");
                    
                    // 如果EventGroup配置为空，则回退到TextItems配置
                    string textItems = AppSettings.Instance["TextItems"]?.ToString() ?? string.Empty;
                    LogHelper.Debug($"SetComponentOrderFromEventItems: TextItems = '{textItems}'");
                    
                    if (!string.IsNullOrEmpty(textItems))
                    {
                        var items = textItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        LogHelper.Debug($"SetComponentOrderFromEventItems: 从TextItems解析到 {items.Length / 2} 个配置项");
                        
                        for (int i = 0; i < items.Length; i += 2)
                        {
                            if (i + 1 < items.Length)
                            {
                                string itemText = items[i];
                                bool isChecked = bool.Parse(items[i + 1]);

                                LogHelper.Debug($"SetComponentOrderFromEventItems: TextItems项目 '{itemText}' = {isChecked}");

                                if (isChecked)
                                {
                                    // 将TextItems项目名称映射到FileNameComponents期望的组件类型
                                    string componentType = MapEventItemToComponentType(itemText);
                                    if (!string.IsNullOrEmpty(componentType) && !componentOrder.Contains(componentType))
                                    {
                                        componentOrder.Add(componentType);
                                        LogHelper.Debug($"SetComponentOrderFromEventItems: 从TextItems添加组件 '{componentType}' (原项目名: {itemText})");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Warn("SetComponentOrderFromEventItems: TextItems也为空，尝试使用旧的EventItems作为后备");
                        
                        // 最后的备选方案：使用旧的EventItems
                        string eventItems = AppSettings.EventItems ?? string.Empty;
                        if (!string.IsNullOrEmpty(eventItems))
                        {
                            var oldItems = eventItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                            LogHelper.Debug($"SetComponentOrderFromEventItems: 从EventItems解析到 {oldItems.Length / 2} 个配置项");

                            for (int i = 0; i < oldItems.Length; i += 2)
                            {
                                if (i + 1 < oldItems.Length)
                                {
                                    string itemText = oldItems[i];
                                    bool isChecked = bool.Parse(oldItems[i + 1]);

                                    LogHelper.Debug($"SetComponentOrderFromEventItems: EventItems项目 '{itemText}' = {isChecked}");

                                    if (isChecked)
                                    {
                                        // 将EventItems项目名称映射到FileNameComponents期望的组件类型
                                        string componentType = MapEventItemToComponentType(itemText);
                                        if (!string.IsNullOrEmpty(componentType) && !componentOrder.Contains(componentType))
                                        {
                                            componentOrder.Add(componentType);
                                            LogHelper.Debug($"SetComponentOrderFromEventItems: 从EventItems添加组件 '{componentType}' (原项目名: {itemText})");
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            LogHelper.Warn("SetComponentOrderFromEventItems: 所有配置都为空，将使用默认顺序");
                        }
                    }
                }

                if (componentOrder.Count > 0)
                {
                    components.ComponentOrder = componentOrder;
                    LogHelper.Info($"SetComponentOrderFromEventItems: 最终组件顺序 = [{string.Join(", ", componentOrder)}]");
                }
                else
                {
                    LogHelper.Warn("SetComponentOrderFromEventItems: 没有设置组件顺序，将使用默认顺序");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"SetComponentOrderFromEventItems: 设置组件顺序时发生异常: {ex.Message}");
                LogHelper.Debug($"SetComponentOrderFromEventItems: 异常详情: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 将EventGroup项目名称映射到FileNameComponents期望的组件类型
        /// </summary>
        /// <param name="itemName">EventGroup中的项目名称</param>
        /// <returns>FileNameComponents期望的组件类型名称</returns>
        private string MapEventItemToComponentType(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return string.Empty;
            
            // 根据项目名称映射到对应的组件类型
            switch (itemName)
            {
                case "订单号":
                    return "订单号";
                case "材料":
                    return "材料";
                case "数量":
                    return "数量";
                case "工艺":
                    return "工艺";
                case "尺寸":
                    return "尺寸";
                case "序号":
                    return "序号";
                case "行数":
                    return "行数";
                case "列数":
                    return "列数";
                case "列组合":
                    return "列组合";
                case "正则结果":
                    return "正则结果";
                default:
                    LogHelper.Warn($"MapEventItemToComponentType: 未知的项目名称 '{itemName}'");
                    return string.Empty;
            }
        }
        #endregion

        /// <summary>
        /// 立即重命名文件（协调方法）
        /// </summary>
        private void RenameFileImmediately(FileInfo fileInfo, string selectedMaterial, string orderNumber, string quantity, string unit, string exportPath, double tetBleed, string width, string height, string fixedField, string serialNumber, string cornerRadius = "0", bool usePdfLastPage = false, bool addPdfLayers = true, string compositeColumnValue = "")
        {
            try
            {
                // 将业务逻辑委托给Presenter处理
                _presenter.HandleRenameFileImmediately(fileInfo, selectedMaterial, orderNumber, quantity, unit, exportPath, tetBleed, width, height, fixedField, serialNumber, cornerRadius, usePdfLastPage, addPdfLayers, compositeColumnValue);
            }
            catch (Exception ex)
            {
                failCount++;
                toolStripStatusLabel.Text = "文件重命名失败: " + ex.Message;
                MessageBox.Show("重命名失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UpdateStatusStrip();
            }
        }

        /// <summary>
        /// 计算最终尺寸
        /// </summary>
        private string CalculateFinalDimensions(string filePath, double tetBleed, string cornerRadius, bool addPdfLayers)
        {
            using (var settingsForm = new SettingsForm())
            {
                settingsForm.RecognizePdfDimensions(filePath);
                return settingsForm.CalculateFinalDimensions(settingsForm.PdfWidth, settingsForm.PdfHeight, tetBleed, cornerRadius, addPdfLayers);
            }
        }

        /// <summary>
        /// 更新重命名结果
        /// </summary>
        private void UpdateRenameResults(FileInfo fileInfo, string newFileName, string destPath, string pattern,
            string orderNumber, string selectedMaterial, string quantity, string finalDimensions, 
            string fixedField, string serialNumber)
        {
            // 获取正则结果部分
            string regexPart = string.Empty;
            if (!string.IsNullOrEmpty(pattern))
            {
                var originalName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var regexMatch = Regex.Match(originalName, pattern);
                if (regexMatch.Success)
                {
                    regexPart = regexMatch.Groups.Count > 1 ? regexMatch.Groups[1].Value : regexMatch.Value;
                }
            }

            // 如果未提供序号则自动生成
            if (string.IsNullOrEmpty(serialNumber))
            {
                var bindingList = dgvFiles.DataSource as BindingList<FileRenameInfo>;
                int maxSerial = 0;
                if (bindingList != null && bindingList.Any())
                {
                    int.TryParse(bindingList.Max(item => item.SerialNumber), out maxSerial);
                }
                serialNumber = (maxSerial + 1).ToString("D2");
            }

            // 添加到数据网格以显示结果
            AddOrUpdateFileRow(fileInfo, newFileName, destPath, regexPart, orderNumber, 
                selectedMaterial, quantity, finalDimensions, fixedField, serialNumber);
        }

        #region 文件重命名

        // 公开属性，用于直接访问ExcelImportForm中的cmbRegex2控件
        public ExcelImportForm ExcelImportFormInstance { get; private set; }
        
        // 保存最后一个MaterialSelectFormModern实例，用于获取用户选择
        private MaterialSelectFormModern _lastMaterialSelectForm;
        
        /// <summary>
        /// 清理MaterialSelectForm实例
        /// </summary>
        private void CleanupMaterialSelectForm()
        {
            if (_lastMaterialSelectForm != null)
            {
                _lastMaterialSelectForm.Dispose();
                _lastMaterialSelectForm = null;

                // 清除SettingsForm的MaterialSelectFormModern引用
                SettingsForm.ClearMaterialSelectFormReference();
            }
        }

        private void BtnImportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                // 将业务逻辑完全委托给Presenter处理
                _presenter.HandleExcelImport();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Excel导入错误: {ex.Message}");
                MessageBox.Show($"导入Excel文件时出错: {ex.Message}", "导入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 清除Excel按钮点击事件处理程序
        /// </summary>
        private void BtnClearExcel_Click(object sender, EventArgs e)
        {
            try
            {
                // 将业务逻辑完全委托给Presenter处理
                _presenter.HandleClearExcelData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清除Excel数据错误: {ex.Message}");
                MessageBox.Show("清除Excel数据失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvExcelData_Resize(object sender, EventArgs e)
        {
            AdjustColumnWidths();
        }

        private void AdjustColumnWidths()
        {
            if (dgvExcelData.Columns.Count == 0) return;

            // 判断是否显示垂直滚动条
            bool hasVerticalScrollBar = dgvExcelData.DisplayRectangle.Height > dgvExcelData.ClientSize.Height;
            int scrollBarWidth = hasVerticalScrollBar ? SystemInformation.VerticalScrollBarWidth : 0;
            // 计算可用宽度（排除滚动条）
            int availableWidth = dgvExcelData.ClientSize.Width - scrollBarWidth;

            // 设置列自动填充模式
            dgvExcelData.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // 获取可见列
            var visibleColumns = dgvExcelData.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();

            // 根据可见列数量设置不同的权重比
            if (visibleColumns.Count == 3)
            {
                visibleColumns[0].FillWeight = 25;
                visibleColumns[1].FillWeight = 50;
                visibleColumns[2].FillWeight = 25;
            }
            else if (visibleColumns.Count == 2)
            {
                visibleColumns[0].FillWeight = 70;
                visibleColumns[1].FillWeight = 30;
            }
        }

        private void DgvFiles_Resize(object sender, EventArgs e)
        {
            AdjustFilesColumnWidths();
        }

        private void AdjustFilesColumnWidths()
        {
            if (dgvFiles.Columns.Count == 0) return;

            // 判断是否显示垂直滚动条
            bool hasVerticalScrollBar = dgvFiles.DisplayRectangle.Height > dgvFiles.ClientSize.Height;
            int scrollBarWidth = hasVerticalScrollBar ? SystemInformation.VerticalScrollBarWidth : 0;
            // 计算可用宽度（排除滚动条）
            int availableWidth = dgvFiles.ClientSize.Width - scrollBarWidth;

            // 设置列自动填充模式
            dgvFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // 平均分配可见列宽度
            var visibleColumns = dgvFiles.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();
            if (visibleColumns.Count > 0)
            {
                float fillWeight = 100f / visibleColumns.Count;
                visibleColumns.ForEach(col => col.FillWeight = fillWeight);
            }
        }

        private void BtnMonitor_Click(object sender, EventArgs e)
        {
            try
            {
                // 触发MonitorClick事件
                MonitorClick?.Invoke(this, e);
                
                // 将业务逻辑完全委托给Presenter处理
                _presenter.HandleMonitorClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理监控按钮点击时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 此方法已删除 - 导出路径管理功能已整合到SettingsForm中

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
            try
            {
                // 将业务逻辑完全委托给Presenter处理
                _presenter.HandleExportExcelClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("导出Excel文件时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRename_Click(object sender, EventArgs e)
        {
            try
            {
                // 将业务逻辑完全委托给Presenter处理
                _presenter.HandleRenameClick();
            }
            catch (Exception ex)
            {
                MessageBox.Show("重命名过程中发生异常: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnToggleMode_Click(object sender, EventArgs e)
        {
            try
            {
                // 将业务逻辑完全委托给Presenter处理
                _presenter.HandleToggleModeClick();
                
                // 触发接口事件
                ToggleModeClick?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show("切换模式时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        #region 设置持久化
        private void LoadSettings()
        {
            // 加载目录设置
            // 保存当前手动输入的路径（如果有效），以便加载后恢复
            string currentManualPath = null;
            if (!string.IsNullOrEmpty(txtInputDir.Text) && Directory.Exists(txtInputDir.Text) && 
                (txtInputDir.SelectedItem == null || txtInputDir.Text != txtInputDir.SelectedItem.ToString()))
            {
                currentManualPath = txtInputDir.Text;
            }
            
            // 加载最近5次选择的路径到下拉框
            txtInputDir.Items.Clear();
            if (!string.IsNullOrEmpty(AppSettings.LastInputDir))
            {
                string[] recentPaths = AppSettings.LastInputDir.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var path in recentPaths)
                {
                    txtInputDir.Items.Add(path);
                }
                
                // 如果有保存的手动输入路径，优先使用它
                if (!string.IsNullOrEmpty(currentManualPath))
                {
                    txtInputDir.Text = currentManualPath;
                    // 如果手动输入的路径不在下拉列表中，添加它
                    if (!txtInputDir.Items.Contains(currentManualPath))
                    {
                        txtInputDir.Items.Add(currentManualPath);
                    }
                }
                else if (txtInputDir.Items.Count > 0)
                {
                    txtInputDir.SelectedIndex = 0; // 默认选中最近的路径
                }
                                               // 加载导出路径到下拉框
                                               // 加载正则表达式设置
                if (AppSettings.RegexPatterns != null)
                {
                    regexPatterns = ParseRegexSettings(AppSettings.RegexPatterns);

                    // 保存上次选择的正则表达式名称
                    string lastSelectedRegex = AppSettings.LastSelectedRegex;
                    LogHelper.Debug($"LoadSettings: 正在加载LastSelectedRegex = '{lastSelectedRegex}'");

                    // 更新ComboBox，传入上次选择的项目
                    UpdateRegexComboBox(lastSelectedRegex);

                    // 如果UpdateRegexComboBox没有成功恢复选择，再次尝试
                    if (!string.IsNullOrEmpty(lastSelectedRegex) && cmbRegex.SelectedItem?.ToString() != lastSelectedRegex)
                    {
                        LogHelper.Debug($"LoadSettings: 第一次恢复失败，再次尝试恢复 '{lastSelectedRegex}'");
                        if (cmbRegex.Items.Contains(lastSelectedRegex))
                        {
                            cmbRegex.SelectedItem = lastSelectedRegex;
                            LogHelper.Debug($"LoadSettings: 成功恢复选择 '{lastSelectedRegex}'");
                        }
                        else
                        {
                            LogHelper.Debug($"LoadSettings: 恢复失败，'{lastSelectedRegex}' 不在ComboBox项目中");
                        }
                    }
                    else
                    {
                        LogHelper.Debug($"LoadSettings: 成功恢复选择，当前选中项 = '{cmbRegex.SelectedItem?.ToString()}'");
                    }
                }

                // 加载材料设置
                if (AppSettings.Materials != null)
                {
                    var materialStr = AppSettings.Materials;
                    if (!string.IsNullOrEmpty(materialStr))
                    {
                        materials = materialStr.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(m => m.Trim())
                            .Where(m => !string.IsNullOrEmpty(m))
                            .ToList();

                    }
                }

                // 加载单位设置
                // 单位设置已迁移至SettingsForm管理


            }
        }

        private void SaveSettings()
        {
            // 保存目录设置
            // 保存最近5次选择的路径
            List<string> recentPaths = new List<string>();
            // 读取现有路径列表
            if (!string.IsNullOrEmpty(AppSettings.LastInputDir))
            {
                recentPaths = AppSettings.LastInputDir.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            
            // 获取要保存的路径 - 优先使用Text属性，以支持手动输入
            string pathToSave = null;
            if (!string.IsNullOrEmpty(txtInputDir.Text) && Directory.Exists(txtInputDir.Text))
            {
                pathToSave = txtInputDir.Text;
            }
            else if (txtInputDir.SelectedItem != null)
            {
                pathToSave = txtInputDir.SelectedItem.ToString();
            }
            
            if (!string.IsNullOrEmpty(pathToSave))
            {
                // 总是添加当前路径到列表首位
                if (recentPaths.Contains(pathToSave))
                {
                    recentPaths.Remove(pathToSave);
                }
                recentPaths.Insert(0, pathToSave);
                // 只保留最近5条记录
                if (recentPaths.Count > 5)
                    recentPaths.RemoveRange(5, recentPaths.Count - 5);
                // 保存更新后的路径列表
                AppSettings.LastInputDir = string.Join("|", recentPaths);
                
                // 如果手动输入的路径不在下拉列表中，添加它
                if (!txtInputDir.Items.Contains(pathToSave))
                {
                    txtInputDir.Items.Add(pathToSave);
                }
            }
            
            AppSettings.Save();
            // 保存选中的导出路径
            // 单位设置已迁移至SettingsForm管理，此处不再保存


            if (cmbRegex.SelectedItem != null)
            {
                AppSettings.LastSelectedRegex = cmbRegex.SelectedItem.ToString();
            }

            AppSettings.Save();
        }
        
        // txtInputDir文本变化事件处理程序
        private void txtInputDir_TextChanged(object sender, EventArgs e)
        {
            // 当用户直接输入文本时，检查是否是有效的目录路径
            if (!string.IsNullOrEmpty(txtInputDir.Text) && Directory.Exists(txtInputDir.Text))
            {
                // 保存设置
                SaveSettings();
            }
        }

        private void SaveRegexSettings()
        {
            var regexStr = string.Join("|", regexPatterns.Select(kvp => kvp.Key + "=" + kvp.Value));
            AppSettings.RegexPatterns = regexStr;
            AppSettings.Save();
        }

        private void SaveMaterialSettings()
        {
            AppSettings.Materials = string.Join("|", materials);
            // 单位设置已迁移至SettingsForm管理，此处不再保存 // 保存数量单位
            AppSettings.Save();
        }

        private Dictionary<string, string> ParseRegexSettings(string regexStr)
        {
            var dict = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(regexStr)) return dict;

            var parts = regexStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var keyValue = part.Split(new[] { '=' }, 2);
                if (keyValue.Length == 2)
                {
                    dict[keyValue[0]] = keyValue[1];
                }
            }
            return dict;
        }
        #endregion


        #region 状态更新
        // 存储ExcelImportForm中选择的正则表达式
        public string ExcelFormRegexPattern { get; set; } = "未选择";

        public void UpdateStatusStrip()
        {
            // 使用滚动功能更新状态栏
            UpdateStatusStripWithScroll();
        }
        /// <summary>
        /// 获取事件分组和顺序状态信息
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <returns>分组和顺序状态字符串</returns>
        private string GetEventGroupAndOrderStatus(string separator)
        {
            try
            {
                // 优先使用新的分组配置系统
                var lastSelectedPreset = AppSettings.Get("LastSelectedEventPreset") as string;

                if (!string.IsNullOrEmpty(lastSelectedPreset))
                {
                    // 尝试从CustomSettings获取当前预设的配置
                    string presetConfigKey = $"EventItemsPreset_{lastSelectedPreset}";
                    var eventGroupConfigJson = AppSettings.Get(presetConfigKey) as string;

                    if (!string.IsNullOrEmpty(eventGroupConfigJson))
                    {
                        // 使用新的分组配置系统
                        var groupConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<WindowsFormsApp3.Models.EventGroupConfiguration>(eventGroupConfigJson);
                        if (groupConfig != null)
                        {
                            var result = GetGroupedEventStatus(groupConfig, separator);
                            // 确保结果总是包含前缀
                            if (!string.IsNullOrEmpty(result) && !result.StartsWith("规则:") && !result.StartsWith("顺序:"))
                            {
                                LogHelper.Warn($"GetEventGroupAndOrderStatus: 检测到缺少前缀的结果，自动添加前缀: '{result}'");
                                result = "规则: " + result;
                            }
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取分组配置失败，使用旧系统: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"获取分组配置失败，使用旧系统: {ex.Message}");
            }

            // 回退到旧的EventItems系统
            var legacyResult = GetLegacyEventStatus(separator);
            // 确保旧系统的结果也总是包含前缀
            if (!string.IsNullOrEmpty(legacyResult) && !legacyResult.StartsWith("规则:") && !legacyResult.StartsWith("顺序:"))
            {
                LogHelper.Warn($"GetEventGroupAndOrderStatus: 检测到旧系统结果缺少前缀，自动添加前缀: '{legacyResult}'");
                legacyResult = "顺序: " + legacyResult;
            }
            return legacyResult;
        }

        /// <summary>
        /// 获取分组事件状态信息
        /// </summary>
        /// <param name="groupConfig">分组配置</param>
        /// <param name="separator">分隔符</param>
        /// <returns>分组事件状态字符串</returns>
              private string GetGroupedEventStatus(WindowsFormsApp3.Models.EventGroupConfiguration groupConfig, string separator)
        {
            var allItems = groupConfig.Items;
            LogHelper.Debug($"GetGroupedEventStatus: 总共找到 {allItems?.Count ?? 0} 个项目");

            if (!allItems.Any())
            {
                LogHelper.Warn("GetGroupedEventStatus: 没有找到任何项目，尝试从分组名称生成规则显示");

                // 如果没有Items但有Groups，从Groups的Prefix生成规则显示
                if (groupConfig.Groups != null && groupConfig.Groups.Any())
                {
                    var enabledGroups = groupConfig.Groups.Where(g => g.IsEnabled).OrderBy(g => g.SortOrder).ToList();
                    if (enabledGroups.Any())
                    {
                        var groupRuleParts = new List<string>();
                        foreach (var group in enabledGroups)
                        {
                            if (!string.IsNullOrEmpty(group.Prefix))
                            {
                                groupRuleParts.Add($"{group.Prefix}{group.DisplayName}");
                            }
                            else
                            {
                                // 如果没有Prefix，使用DisplayName
                                groupRuleParts.Add(group.DisplayName);
                            }
                        }

                        // ✅ 修改：分组间不使用分隔符，直接相接
                        var ruleFromGroups = string.Concat(groupRuleParts);
                        LogHelper.Debug($"GetGroupedEventStatus: 从分组Prefix生成规则显示: '{ruleFromGroups}'");
                        return "规则: " + ruleFromGroups;
                    }
                }

                // 最后的备用方案：使用预设名称
                var lastSelectedPreset = AppSettings.Get("LastSelectedEventPreset") as string;
                if (!string.IsNullOrEmpty(lastSelectedPreset))
                {
                    LogHelper.Debug($"GetGroupedEventStatus: 使用预设名称作为规则显示: '{lastSelectedPreset}'");
                    return "规则: " + lastSelectedPreset;
                }

                LogHelper.Warn("GetGroupedEventStatus: 无法获取任何规则信息，返回'规则: 未设置'");
                return "规则: 未设置";
            }

            // 按分组和排序顺序组织显示 - 每个分组一个前缀，分组内多个项目用分隔符连接
            var ruleParts = new List<string>();
            var allGroups = groupConfig.Groups.OrderBy(g => g.SortOrder).ToList();

            LogHelper.Debug($"GetGroupedEventStatus: 开始构建规则，共有 {allGroups.Count} 个分组");

            foreach (var group in allGroups)
            {
                var groupItems = allItems.Where(i => i.GroupId == group.Id && i.IsEnabled).OrderBy(i => i.SortOrder).ToList();
                if (groupItems.Any())
                {
                    // ... existing code ...
                    LogHelper.Debug($"GetGroupedEventStatus: 处理分组 '{group.DisplayName}' (Id: {group.Id}, Prefix: '{group.Prefix}')，有 {groupItems.Count} 个启用项目");

                    // ✅ 改进：一个分组一个前缀，分组内多个项目用分隔符连接
                    var escapedPrefix = group.Prefix.Replace("&", "&&");
                    var itemNames = groupItems.Select(item => item.Name).ToList();
                    // ✅ 只在分组内有多个项目时才使用分隔符
                    var groupValue = itemNames.Count > 1 
                        ? string.Join(separator ?? "", itemNames)
                        : itemNames[0]; // 单个项目不使用分隔符
                    var rulePart = $"{escapedPrefix}{groupValue}";
                    ruleParts.Add(rulePart);
                    LogHelper.Debug($"GetGroupedEventStatus: 添加规则项: '{rulePart}' (Prefix: '{group.Prefix}', Items: {string.Join(",", itemNames)}, 项目数={itemNames.Count}, 使用分隔符={itemNames.Count > 1})");
                }
                else
                {
                    LogHelper.Debug($"GetGroupedEventStatus: 分组 '{group.DisplayName}' (Id: {group.Id}) 没有启用项目");
                }
            }

            // 检查是否有未分组的项目
            var ungroupedItems = allItems.Where(i => string.IsNullOrEmpty(i.GroupId)).OrderBy(i => i.SortOrder).ToList();
            if (ungroupedItems.Any())
            {
                var ungroupedGroup = groupConfig.Groups.FirstOrDefault(g => g.Id == "ungrouped");
                var ungroupedStatus = ungroupedGroup?.IsEnabled == false ? "[禁用]" : "";

                // 对于未分组项目，也使用前缀格式，如果没有前缀则使用默认前缀
                var ungroupedPrefix = ungroupedGroup?.Prefix ?? "&OTHER-";
                // 在WinForms中，单个&会被解释为快捷键，需要使用&&来显示单个&符号
                var escapedUngroupedPrefix = ungroupedPrefix.Replace("&", "&&");

                foreach (var item in ungroupedItems)
                {
                    if (item.IsEnabled)
                    {
                        ruleParts.Add($"{escapedUngroupedPrefix}{item.Name}{ungroupedStatus}");
                    }
                }
            }

            LogHelper.Debug($"GetGroupedEventStatus: 规则构建完成，共有 {ruleParts.Count} 个规则项");

            if (ruleParts.Any())
            {
                // ✅ 修改：分组间不使用分隔符，直接相接
                var finalRule = "规则: " + string.Concat(ruleParts);
                LogHelper.Debug($"GetGroupedEventStatus: 最终规则: '{finalRule}'");
                return finalRule;
            }
            else
            {
                LogHelper.Debug($"GetGroupedEventStatus: 没有启用的项目，返回'规则: 未启用项目'");
                return "规则: 未启用项目";
            }
        }

        /// <summary>
        /// 获取传统事件状态信息
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <returns>传统事件状态字符串</returns>
        private string GetLegacyEventStatus(string separator)
        {
            // 从设置中获取事件顺序配置
            var eventItems = AppSettings.EventItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var eventOrder = new List<string>();

            // 如果EventItems为空或无效，使用默认顺序规则
            if (eventItems.Length == 0)
            {
                // 默认顺序规则：正则结果|序号|订单号|材料|数量|工艺|尺寸
                eventOrder.AddRange(new[] { "正则结果", "序号", "订单号", "材料", "数量", "工艺", "尺寸" });
            }
            else
            {
                for (int i = 0; i < eventItems.Length; i += 2)
                {
                    if (i + 1 < eventItems.Length && bool.Parse(eventItems[i + 1].Trim()))
                    {
                        eventOrder.Add(eventItems[i].Trim());
                    }
                }
            }

            var eventStatus = eventOrder.Any() ? string.Join(separator, eventOrder) : "未设置";
            return "顺序: " + eventStatus;
        }

        #endregion

        private void LblSeparator_Click(object sender, EventArgs e)
        {

        }

        private void LabelOpacity_Click(object sender, EventArgs e)
        {

        }

        private void TxtRegexTestResult_TextChanged(object sender, EventArgs e)
        {

        }

        private void TxtRegexTestInput_TextChanged(object sender, EventArgs e)
        {

        }

        private void ChkLstEvents_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


        private void dgvFiles_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // 确保在批量模式下能够正常响应点击事件
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    LogDebugInfo($"CellContentClick: 行={e.RowIndex}, 列={e.ColumnIndex}, 模式={(isImmediateRenameActive ? "手动" : "批量")}");

                    // 在批量模式下，确保 DataGridView 能够正常处理点击
                    if (!isImmediateRenameActive)
                    {
                        // 可以在这里添加批量模式下的特殊处理逻辑
                        // 例如：开始编辑单元格
                        if (dgvFiles.Columns[e.ColumnIndex].Name != "colSerialNumber" &&
                            dgvFiles.Columns[e.ColumnIndex].Name != "colOriginalName")
                        {
                            dgvFiles.BeginEdit(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebugInfo($"CellContentClick 处理异常: {ex.Message}");
            }
        }


        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void toolStripDropDownButton1_Click(object sender, EventArgs e)
        {

        }

        #region IForm1View 接口成员实现

        // 事件实现
        public event EventHandler BatchProcessClick;
        public event EventHandler ImmediateRenameClick;
        public event EventHandler StopImmediateRenameClick;
        public event EventHandler ToggleModeClick;
        public event EventHandler MonitorClick;
        public event EventHandler ExportSettingsClick;
        public new event EventHandler<KeyEventArgs> KeyDown;
        public new event FormClosingEventHandler FormClosing;
        public event EventHandler FormLoad;
        public new event EventHandler Resize;
        public event DataGridViewCellEventHandler CellValueChanged;
        public event DataGridViewCellMouseEventHandler ColumnHeaderMouseClick;

        // 属性实现
        public bool IsImmediateRenameActive
        {
            get { return isImmediateRenameActive; }
            set { isImmediateRenameActive = value; }
        }

        public bool IsCopyMode
        {
            get { return isCopyMode; }
            set { isCopyMode = value; }
        }

        public bool IsMonitoring
        {
            get { return isMonitoring; }
            set 
            { 
                isMonitoring = value;
                // 同时更新按钮文本
                if (btnMonitor != null)
                {
                    btnMonitor.Text = isMonitoring ? "停止监控" : "开始监控";
                }
            }
        }

        public string CurrentConfigName
        {
            get { return _currentConfigName; }
            set { _currentConfigName = value; }
        }

        public NotifyIcon TrayIcon
        {
            get { return trayIcon; }
        }

        public string CurrentRegexPattern
        {
            get 
            { 
                // 添加调试信息
                System.Diagnostics.Debug.WriteLine($"获取CurrentRegexPattern值: '{currentRegexPattern}'");
                // 显示当前使用的重命名规则
                System.Diagnostics.Debug.WriteLine($"当前使用的重命名规则 (来自CurrentRegexPattern属性getter):");
                System.Diagnostics.Debug.WriteLine($"  - 正则表达式模式: '{currentRegexPattern}'");
                System.Diagnostics.Debug.WriteLine($"  - 模式来源: CurrentRegexPattern属性getter");
                return currentRegexPattern; 
            }
            set 
            { 
                currentRegexPattern = value;
                // 添加调试信息
                System.Diagnostics.Debug.WriteLine($"设置CurrentRegexPattern值为: '{currentRegexPattern}'");
                // 显示当前使用的重命名规则
                System.Diagnostics.Debug.WriteLine($"当前使用的重命名规则 (来自CurrentRegexPattern属性setter):");
                System.Diagnostics.Debug.WriteLine($"  - 正则表达式模式: '{currentRegexPattern}'");
                System.Diagnostics.Debug.WriteLine($"  - 模式来源: CurrentRegexPattern属性setter");
            }
        }

        public BindingList<FileRenameInfo> FileBindingList
        {
            get { return dgvFiles.DataSource as BindingList<FileRenameInfo>; }
            set { dgvFiles.DataSource = value; }
        }

        public int ExcelSearchColumnIndex
        {
            get { return _excelSearchColumnIndex; }
        }
        
        public int ExcelReturnColumnIndex
        {
            get { return _excelReturnColumnIndex; }
        }

        /// <summary>
        /// 获取导出路径
        /// </summary>
        /// <returns>导出路径</returns>
        public string GetExportPath()
        {
            string exportPath = string.Empty;
            using (var dialog = new Ookii.Dialogs.WinForms.VistaFolderBrowserDialog())
            {
                dialog.Description = "请选择导出目录";
                dialog.UseDescriptionForTitle = true;

                // 设置上次使用的路径
                if (!string.IsNullOrEmpty(AppSettings.LastExportPath) &&
                    Directory.Exists(AppSettings.LastExportPath))
                {
                    dialog.SelectedPath = AppSettings.LastExportPath;
                }

                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    // 用户取消选择
                    return string.Empty;
                }

                exportPath = dialog.SelectedPath;
                // 保存选择的路径
                AppSettings.LastExportPath = exportPath;
                AppSettings.Save();
            }
            return exportPath;
        }

        public DataTable ExcelImportedData
        {
            get { return _excelImportedData; }
            set { _excelImportedData = value; }
        }

        // 方法实现
        public void ShowMessage(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            MessageBox.Show(message, title, buttons, icon);
        }

        public DialogResult ShowMessageWithResult(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            return MessageBox.Show(message, title, buttons, icon);
        }

        public void ShowProgressForm()
        {
            // 创建并显示进度窗口
            using (var progressForm = new ProgressForm())
            {
                progressForm.ShowDialog();
            }
        }

        public void ShowProgressForm(Action<ProgressForm> progressAction)
        {
            using (var progressForm = new ProgressForm())
            {
                // 启动一个任务来执行回调函数
                Task.Run(() =>
                {
                    try
                    {
                        progressAction(progressForm);
                    }
                    catch (Exception ex)
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            MessageBox.Show("处理过程中发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            progressForm.Close();
                        }));
                    }
                });

                // 显示进度窗口
                progressForm.ShowDialog();
            }
        }

        public void ShowProgressFormWithCancellation(Func<ProgressForm, System.Threading.CancellationToken, Task> progressAction, System.Threading.CancellationTokenSource cancellationTokenSource)
        {
            using (var progressForm = new ProgressForm())
            {
                // 添加取消按钮到进度窗体
                progressForm.AddCancelButton(() =>
                {
                    cancellationTokenSource.Cancel();
                    progressForm.UpdateProgress(0, "正在取消操作...");
                });

                // 启动异步任务来执行回调函数
                var task = Task.Run(async () =>
                {
                    try
                    {
                        await progressAction(progressForm, cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // 操作被取消，这是预期的异常类型
                        this.Invoke((MethodInvoker)(() =>
                        {
                            progressForm.Close();
                        }));
                    }
                    catch (Exception ex)
                    {
                        this.Invoke((MethodInvoker)(() =>
                        {
                            MessageBox.Show("处理过程中发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            progressForm.Close();
                        }));
                    }
                });

                // 在窗体关闭时检查任务状态
                progressForm.FormClosing += (sender, e) =>
                {
                    if (!task.IsCompleted && !task.IsCanceled && !cancellationTokenSource.IsCancellationRequested)
                    {
                        cancellationTokenSource.Cancel();
                        try
                        {
                            task.Wait(1000); // 等待最多1秒让任务正常完成
                        }
                        catch (AggregateException)
                        {
                            // 忽略取消异常
                        }
                    }
                };

                // 显示进度窗口
                progressForm.ShowDialog();
            }
        }

        public void CloseProgressForm()
        {
            // 关闭进度窗口的逻辑
        }

        // 实现IForm1View接口的UpdateProgress方法
        public void UpdateProgress(int percentProgress, string statusText)
        {
            // 调用带double参数的重载方法
            UpdateProgress((double)percentProgress, statusText);
        }
        
        // 保留原有方法作为重载，保持向后兼容性
        public void UpdateProgress(double percentage, string message)
        {
            // 更新进度条的逻辑
        }

        // 实现IForm1View接口的ShowFolderBrowserDialog方法
        public string ShowFolderBrowserDialog(string description)
        {
            // 调用带默认路径参数的重载方法，默认路径为null
            return ShowFolderBrowserDialog(description, null);
        }
        
        // 保留原有方法作为重载，保持向后兼容性
        public string ShowFolderBrowserDialog(string description, string defaultPath = null)
        {
            using (var dialog = new Ookii.Dialogs.WinForms.VistaFolderBrowserDialog())
            {
                dialog.Description = description;
                if (!string.IsNullOrEmpty(defaultPath))
                {
                    dialog.SelectedPath = defaultPath;
                }
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }
            return null;
        }

        // 实现IForm1View接口的ShowOpenFileDialog方法
        public string[] ShowOpenFileDialog(string filter, string title)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = filter;
                dialog.Title = title;
                dialog.Multiselect = true;
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileNames;
                }
            }
            return new string[0];
        }

        // 实现IForm1View接口的ShowSaveFileDialog方法
        public string ShowSaveFileDialog(string filter, string title, string defaultFileName = null)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = filter;
                dialog.Title = title;
                dialog.AddExtension = true;
                dialog.DefaultExt = "xlsx";

                // 设置默认文件名
                if (!string.IsNullOrEmpty(defaultFileName))
                {
                    dialog.FileName = defaultFileName;
                }
                else
                {
                    // 如果没有提供默认文件名，使用当前时间作为文件名
                    dialog.FileName = $"数据导出_{DateTime.Now:yyyyMMdd_HHmmss}";
                }

                // 设置初始目录
                string initialDirectory = GetLastExportDirectory();
                if (!string.IsNullOrEmpty(initialDirectory) && Directory.Exists(initialDirectory))
                {
                    dialog.InitialDirectory = initialDirectory;
                }
                else
                {
                    // 如果上次导出目录不存在，使用文档目录
                    string defaultExportFolder = AppDataPathManager.ExcelExportDirectory;
                    dialog.InitialDirectory = defaultExportFolder;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // 保存导出目录以供下次使用
                    AppSettings.LastExportPath = Path.GetDirectoryName(dialog.FileName);
                    AppSettings.Save();

                    return dialog.FileName;
                }
            }
            return null;
        }

        private string GetLastExportDirectory()
        {
            // 首先尝试从Properties.Settings获取
            if (!string.IsNullOrEmpty(AppSettings.LastExportPath) &&
                Directory.Exists(AppSettings.LastExportPath))
            {
                return AppSettings.LastExportPath;
            }

            // 如果设置中没有或目录不存在，尝试使用当前输入目录
            if (!string.IsNullOrEmpty(txtInputDir.Text) && Directory.Exists(txtInputDir.Text))
            {
                return txtInputDir.Text;
            }

            // 最后返回文档目录下的默认导出文件夹
            string defaultFolder = AppDataPathManager.ExcelExportDirectory;
            return defaultFolder;
        }

        public bool AskForConfirmation(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
        
        // 实现IForm1View接口的GetMaterials方法
        public List<string> GetMaterials()
        {
            return materials;
        }
        
        // 实现IForm1View接口的ShowMaterialSelectionDialog方法
        public DialogResult ShowMaterialSelectionDialog(
            List<string> materials, 
            string fileName, 
            string regexResult, 
            double opacity, 
            string width, 
            string height, 
            DataTable excelData, 
            int searchColumnIndex, 
            int returnColumnIndex, 
            int serialColumnIndex, 
            string serialNumber)
        {
            // 释放之前的实例（如果存在）
            _lastMaterialSelectForm?.Dispose();
            
            _lastMaterialSelectForm = new MaterialSelectFormModern(materials, fileName, regexResult, opacity, width, height, excelData, searchColumnIndex, returnColumnIndex, serialColumnIndex, -1, serialNumber);

            // 设置Owner属性，以便MaterialSelectFormModern可以访问主窗体的dgvFiles
            _lastMaterialSelectForm.Owner = this;

            // 设置SettingsForm的MaterialSelectFormModern引用
            SettingsForm.SetMaterialSelectFormReference(_lastMaterialSelectForm);

            _lastMaterialSelectForm.LoadExcelData(excelData, searchColumnIndex, returnColumnIndex, serialColumnIndex, -1); // 传入-1表示没有newColumnIndex

            _lastMaterialSelectForm.TopMost = true;
            // ✨ 关键修复：不要强制设置StartPosition，让WindowPositionManager控制位置
            // _lastMaterialSelectForm.StartPosition = FormStartPosition.CenterScreen;
            return _lastMaterialSelectForm.ShowDialog();
        }
        
        // 实现IForm1View接口的GetSelectedMaterial方法
        public string GetSelectedMaterial()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的SelectedMaterial属性获取
            return string.Empty;
        }
        
        // 实现IForm1View接口的GetOrderNumber方法
        public string GetOrderNumber()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的OrderNumber属性获取
            return string.Empty;
        }
        
        // 实现IForm1View接口的GetSelectedExportPath方法
        public string GetSelectedExportPath()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的SelectedExportPath属性获取
            return string.Empty;
        }
        
        // 实现IForm1View接口的GetQuantities方法
        public List<string> GetQuantities()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的Quantities属性获取
            return new List<string>();
        }
        
        // 实现IForm1View接口的GetSerialNumbers方法
        public List<string> GetSerialNumbers()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的SerialNumber属性获取
            return new List<string>();
        }
        
        // 实现IForm1View接口的GetMatchedRows方法
        public List<DataRow> GetMatchedRows()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的MatchedRows属性获取
            return new List<DataRow>();
        }
        
        // 实现IForm1View接口的GetAdjustedDimensions方法
        public string GetAdjustedDimensions()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的AdjustedDimensions属性获取
            return string.Empty;
        }
        
        // 实现IForm1View接口的GetFixedField方法
        public string GetFixedField()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的FixedField属性获取
            return string.Empty;
        }
        
        // 实现IForm1View接口的GetSelectedTetBleed方法
        public double GetSelectedTetBleed()
        {
            // 这个方法将在Presenter中通过MaterialSelectForm的SelectedTetBleed属性获取
            return 0.0;
        }
        
        // 实现IForm1View接口的GetCornerRadius方法
        public string GetCornerRadius()
        {
            // 返回MaterialSelectForm中设置的圆角半径值
#pragma warning disable CS0618 // 禁用过时API警告
            if (ExcelImportFormInstance != null)
            {
                return ExcelImportFormInstance.CornerRadius ?? string.Empty;
            }
#pragma warning restore CS0618 // 恢复警告
            return string.Empty;
        }

        // 实现IForm1View接口的GetUsePdfLastPage方法
        public bool GetUsePdfLastPage()
        {
            // 返回MaterialSelectForm中设置的使用PDF最后一页选项
            if (ExcelImportFormInstance != null)
            {
                return ExcelImportFormInstance.UsePdfLastPage;
            }
            return false;
        }

        // 实现IForm1View接口的GetAddPdfLayers方法
        public bool GetAddPdfLayers()
        {
            // 返回MaterialSelectForm中设置的添加PDF图层选项
            if (ExcelImportFormInstance != null)
            {
                return ExcelImportFormInstance.AddPdfLayers;
            }
            return false;
        }
        
        // 实现IForm1View接口的GetAddIdentifierPage方法
        public bool GetAddIdentifierPage()
        {
            // 返回MaterialSelectForm中设置的添加标识页选项
            bool result = false;
            if (_lastMaterialSelectForm != null)
            {
                result = _lastMaterialSelectForm.AddIdentifierPage;
                LogHelper.Debug($"[标识页调试] Form1.GetAddIdentifierPage: _lastMaterialSelectForm != null, AddIdentifierPage={result}");
            }
            else
            {
                LogHelper.Debug($"[标识页调试] Form1.GetAddIdentifierPage: _lastMaterialSelectForm == null, 返回 false");
            }
            return result;
        }
        
        // 实现IForm1View接口的GetIdentifierPageContent方法
        public string GetIdentifierPageContent()
        {
            // 调用MaterialSelectForm的GenerateIdentifierPageContent方法生成内容
            string result = string.Empty;
            if (_lastMaterialSelectForm != null)
            {
                result = _lastMaterialSelectForm.GenerateIdentifierPageContent();
                LogHelper.Debug($"[标识页调试] Form1.GetIdentifierPageContent: _lastMaterialSelectForm != null, Content='{result}', Length={result?.Length ?? 0}");
            }
            else
            {
                LogHelper.Debug($"[标识页调试] Form1.GetIdentifierPageContent: _lastMaterialSelectForm == null, 返回空字符串");
            }
            return result;
        }

        // 实现IForm1View接口的GetLayoutMode方法
        public LayoutMode GetLayoutMode()
        {
            if (_lastMaterialSelectForm != null)
            {
                return _lastMaterialSelectForm.GetLayoutMode();
            }
            return LayoutMode.Continuous; // 默认连拼模式
        }

        // 实现IForm1View接口的GetLayoutQuantity方法
        public int GetLayoutQuantity()
        {
            if (_lastMaterialSelectForm != null)
            {
                return _lastMaterialSelectForm.GetLayoutQuantity();
            }
            return 0;
        }

        // 实现IForm1View接口的GetRotationAngle方法
        public int GetRotationAngle()
        {
            if (_lastMaterialSelectForm != null)
            {
                return _lastMaterialSelectForm.GetRotationAngle();
            }
            return 0; // 默认不旋转
        }

        // 更新MaterialSelectFormModern的正则结果
        public void UpdateMaterialSelectFormRegexResult(string regexResult)
        {
            if (_lastMaterialSelectForm != null)
            {
                _lastMaterialSelectForm.SetRegexResult(regexResult);
                LogHelper.Debug($"[标识页调试] Form1.UpdateMaterialSelectFormRegexResult: 已更新_lastMaterialSelectForm的正则结果为 '{regexResult}'");
            }
            else
            {
                LogHelper.Debug($"[标识页调试] Form1.UpdateMaterialSelectFormRegexResult: _lastMaterialSelectForm == null，无法更新正则结果");
            }
        }

        // 实现IForm1View接口的AddMatchedRows方法
        public void AddMatchedRows(List<DataRow> rows)
        {
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    if (!_matchedRows.Contains(row))
                    {
                        _matchedRows.Add(row);
                    }
                }
            }
        }

        // 实现IForm1View接口的GetCmbRegexSelectedItem方法
        public string GetCmbRegexSelectedItem()
        {
            if (cmbRegex.SelectedItem != null)
            {
                return cmbRegex.SelectedItem.ToString();
            }
            return string.Empty;
        }
        
        /// <summary>
        /// 获取用于数据匹配的Excel正则表达式模式
        /// </summary>
        /// <returns>Excel正则表达式模式（用于数据匹配）</returns>
        public string GetExcelRegexPattern()
        {
            // ✅ 修复：Excel正则检测只在导入了表格时才需要
            LogHelper.Debug("GetExcelRegexPattern: 开始获取Excel正则表达式... ExcelImportFormInstance=" + (ExcelImportFormInstance != null ? "not null" : "null"));
            
            // 首先检查是否导入了表格
            if (ExcelImportFormInstance == null)
            {
                LogHelper.Debug("GetExcelRegexPattern: ExcelImportFormInstance为null，未导入Excel数据");
                return string.Empty;
            }
            
            // 检查是否有有效的导入数据
            if (ExcelImportFormInstance.ImportedData == null || ExcelImportFormInstance.ImportedData.Rows.Count == 0)
            {
                LogHelper.Debug("GetExcelRegexPattern: 未导入表格数据（ImportedData为null或无行数据），无需检测Excel正则");
                return string.Empty;
            }
            
            // 导入了表格，开始检测Excel正则配置
            LogHelper.Debug("GetExcelRegexPattern: ExcelImportFormInstance已初始化且有导入数据，开始检测Excel正则");
            
            // 尝试通过ComboBox的SelectedItem获取
            if (ExcelImportFormInstance.RegexComboBox != null && ExcelImportFormInstance.RegexComboBox.SelectedItem != null)
            {
                string selectedPatternName = ExcelImportFormInstance.RegexComboBox.SelectedItem.ToString();
                LogHelper.Debug("GetExcelRegexPattern: Excel正则选择框SelectedItem='" + selectedPatternName + "'");
                
                if (regexPatterns.TryGetValue(selectedPatternName, out string selectedPattern))
                {
                    LogHelper.Debug("GetExcelRegexPattern: 成功获取Excel正则表达式 '" + selectedPatternName + "' -> '" + selectedPattern + "' (用于数据匹配)");
                    // 更新状态栏显示的Excel正则表达式
                    ExcelFormRegexPattern = selectedPatternName;
                    UpdateStatusStrip();
                    return selectedPattern;
                }
                else
                {
                    LogHelper.Debug("GetExcelRegexPattern: 警告 - '" + selectedPatternName + "'不在regexPatterns字典中");
                }
            }
            else
            {
                LogHelper.Debug("GetExcelRegexPattern: 警告 - RegexComboBox为null或SelectedItem为null");
                if (ExcelImportFormInstance.RegexComboBox == null)
                    LogHelper.Debug("GetExcelRegexPattern: RegexComboBox为null");
                else if (ExcelImportFormInstance.RegexComboBox.SelectedItem == null)
                    LogHelper.Debug("GetExcelRegexPattern: SelectedItem为null");
            }
            
            // 如果ComboBox的SelectedItem为null，尝试从SelectedRegexPattern属性获取
            // 这个属性在用户选择时由ExcelImportForm的cmbRegex2_SelectedIndexChanged事件设置
            if (!string.IsNullOrEmpty(ExcelImportFormInstance.SelectedRegexPattern))
            {
                LogHelper.Debug("GetExcelRegexPattern: 从ExcelImportFormInstance.SelectedRegexPattern获取正则表达式: '" + ExcelImportFormInstance.SelectedRegexPattern + "'");
                ExcelFormRegexPattern = "(已选择)";
                UpdateStatusStrip();
                return ExcelImportFormInstance.SelectedRegexPattern;
            }
            
            // 如果都没有找到，返回空字符串，用户可以通过状态栏查看选择状态
            LogHelper.Debug("GetExcelRegexPattern: 导入了Excel表格但未选择正则表达式，返回空字符串");
            ExcelFormRegexPattern = "未选择";
            UpdateStatusStrip();
            return string.Empty;
        }
        
        #region 状态栏滚动功能

        /// <summary>
        /// 初始化状态栏滚动Timer
        /// </summary>
        private void InitializeStatusScrollTimer()
        {
            _statusScrollTimer = new Timer();
            _statusScrollTimer.Interval = 200; // 滚动间隔（毫秒）
            _statusScrollTimer.Tick += StatusScrollTimer_Tick;
        }

        /// <summary>
        /// 更新状态栏文本（支持滚动）
        /// </summary>
        /// <param name="fullText">完整的状态文本</param>
        private void UpdateStatusTextWithScroll(string fullText)
        {
            if (string.IsNullOrEmpty(fullText))
            {
                this.toolStripStatusLabel.Text = "";
                return;
            }
            
            // 直接使用两行显示，不使用滚动
            var lines = SplitTextToTwoLines(fullText);
            this.toolStripStatusLabel.Text = string.Join(Environment.NewLine, lines);
        }

/// <summary>
        /// 将长文本智能分割为两行显示
        /// </summary>
        /// <param name="fullText">要分割的完整文本</param>
        /// <returns>包含两行文本的数组</returns>
        private string[] SplitTextToTwoLines(string fullText)
        {
            if (string.IsNullOrEmpty(fullText))
                return new string[] { "" };

            // 尝试在适当位置分割文本
            var parts = fullText.Split(new[] { " | " }, StringSplitOptions.None);
            if (parts.Length <= 4)
            {
                // 如果分割部分较少，放在第一行
                return new string[] { fullText, "" };
            }

            // 将文本分成两行，尽量保持平衡
            var midPoint = parts.Length / 2;
            var firstLine = string.Join(" | ", parts.Take(midPoint));
            var secondLine = string.Join(" | ", parts.Skip(midPoint));

            return new string[] { firstLine, secondLine };
        }

        /// <summary>
        /// 滚动Timer的Tick事件处理
        /// </summary>
        private void StatusScrollTimer_Tick(object sender, EventArgs e)
        {
            // 滚动功能已禁用，现在使用两行显示
            return;
        }

        /// <summary>
        /// 更新状态栏文本（支持滚动）
        /// </summary>
              private void UpdateStatusStripWithScroll()
        {
            var regexStatus = cmbRegex.SelectedItem != null ? cmbRegex.SelectedItem.ToString() : "未选择";
            var materialStatus = materials.Any() ? materials.Count + "种材料" : "未设置";
            var unitStatus = !string.IsNullOrEmpty(AppSettings.Unit?.Trim()) ? AppSettings.Unit.Trim() : "未设置";

            // 对于状态栏规则显示，如果分隔符为空，则使用空字符串，因为前缀本身已经有&符号
            var userSeparator = AppSettings.Separator ?? "";
            var separator = string.IsNullOrEmpty(userSeparator) ? "" : userSeparator;

            // 获取分组和顺序信息
            var eventStatus = GetEventGroupAndOrderStatus(separator);

            string fullStatusText = "状态: " + (isMonitoring ? "监控中" : "未监控") + " | 配置: " + (_currentConfigName) + " | " + (isCopyMode ? "复制模式" : "剪切模式") + " | 操作模式: " + (isImmediateRenameActive ? "手动模式" : "批量模式") + " | 主正则: " + regexStatus + " | Excel正则: " + ExcelFormRegexPattern + " | 材料: " + materialStatus + " | 单位: " + unitStatus + " | " + eventStatus + " | 待处理: " + GetDataRowCount(dgvFiles.DataSource as BindingList<FileRenameInfo>) + "个文件";

            // 使用滚动功能更新状态栏
            UpdateStatusTextWithScroll(fullStatusText);
        }

        #endregion

        /// <summary>
        /// 获取用于文件重命名的主正则表达式模式
        /// </summary>
        /// <returns>主正则表达式模式（用于文件重命名）</returns>
        public string GetSelectedRegexPattern()
        {
            // ... existing code ...
            LogHelper.Debug("GetSelectedRegexPattern: 未选择主正则表达式");
            return string.Empty;
        }

        /// <summary>
        /// 从返单文件名中提取正则结果
        /// 返单文件名格式包含保留格式前缀，如：&ID-123&MT-钢&DN-10&DP-工艺
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>提取的正则结果，如果不是返单文件则返回空字符串</returns>
        private string ExtractReturnOrderRegexResult(string fileName)
        {
            try
            {
                // 返单文件名应该包含保留格式的前缀，如 &ID-、&MT- 等
                // 检查文件名中是否包含这种格式
                if (string.IsNullOrEmpty(fileName))
                    return string.Empty;

                // 使用与 ExtractPreserveGroupData 相同的正则表达式来匹配保留格式前缀
                var pattern = @"&([A-Z]+)-([^&]+)";
                var matches = System.Text.RegularExpressions.Regex.Matches(fileName, pattern);

                if (matches.Count == 0)
                {
                    // 不是返单文件，不包含保留格式前缀
                    return string.Empty;
                }

                // ✅ 这是返单文件，需要提取正则结果
                // 根据配置的事件项顺序，从保留分组中查找第一个启用的分组作为正则结果
                // 通常是 "正则结果" 分组对应的前缀
                
                // 首先尝试查找 &正则结果 对应的前缀（如果有的话）
                // 如果没有，使用第一个找到的保留格式前缀对应的值
                
                foreach (var match in matches.Cast<System.Text.RegularExpressions.Match>())
                {
                    var prefix = $"&{match.Groups[1].Value}-";
                    var value = match.Groups[2].Value;
                    
                    // 检查这个前缀是否对应 "正则结果" 组件
                    // 优先返回 "正则结果" 对应的值
                    if (prefix == "&RE-")  // 假设 &RE- 是正则结果的前缀
                    {
                        LogHelper.Debug($"ExtractReturnOrderRegexResult: 从返单文件提取正则结果: '{value}'");
                        return value;
                    }
                }
                
                // 如果没有找到 "正则结果" 前缀，返回第一个保留格式的值（向后兼容）
                if (matches.Count > 0)
                {
                    var firstValue = matches[0].Groups[2].Value;
                    LogHelper.Debug($"ExtractReturnOrderRegexResult: 从返单文件提取第一个保留值作为正则结果: '{firstValue}'");
                    return firstValue;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"ExtractReturnOrderRegexResult: 提取返单文件正则结果时发生异常: {ex.Message}");
                return string.Empty;
            }
        }

        #endregion

        /// <summary>
        /// 释放Timer资源
        /// </summary>
        private void DisposeTimer()
        {
            if (_statusScrollTimer != null)
            {
                _statusScrollTimer.Stop();
                _statusScrollTimer.Dispose();
                _statusScrollTimer = null;
            }
        }
    }
}
