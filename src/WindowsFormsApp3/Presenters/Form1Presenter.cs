using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Commands;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Forms.Main;
// 使用别名避免命名冲突
using LogHelper = WindowsFormsApp3.Utils.LogHelper;
using Ookii.Dialogs.WinForms;
using OfficeOpenXml;
using System.Collections.Specialized;

namespace WindowsFormsApp3.Presenters
{
    /// <summary>
    /// Form1演示器实现类，负责处理Form1的业务逻辑和数据处理
    /// </summary>
    public class Form1Presenter : IForm1Presenter
    {
        private readonly IForm1View _view;
        private readonly BatchProcessingService _batchProcessingService;
        private readonly FileRenameService _fileRenameService;
        private readonly IUndoRedoService _undoRedoService;
        private readonly IExcelImportService _excelImportService;
        private readonly ICompositeColumnService _compositeColumnService;
        private readonly IPdfDimensionService _pdfDimensionService;
        private bool _isSaving = false;
        private bool _allowClose = false;
        private FileSystemWatcher _watcher;
        private List<FileInfo> _pendingFiles;
        private Dictionary<string, string> _regexPatterns;
        private List<string> _materials;

        public Form1Presenter(IForm1View view)
        {
            _view = view;
            // 初始化服务
            _batchProcessingService = ServiceLocator.Instance.GetBatchProcessingService() as BatchProcessingService;
            _fileRenameService = ServiceLocator.Instance.GetFileRenameService() as FileRenameService;
            // 初始化Excel导入服务
            _excelImportService = ServiceLocator.Instance.GetExcelImportService();
            // 初始化列组合服务
            _compositeColumnService = ServiceLocator.Instance.GetCompositeColumnService();
            _undoRedoService = ServiceLocator.Instance.GetUndoRedoService();
            _pdfDimensionService = PdfDimensionServiceFactory.GetInstance();
            // 初始化变量
            _pendingFiles = new List<FileInfo>();
            _regexPatterns = new Dictionary<string, string>();
            _materials = new List<string>();
            // 注册视图事件
            RegisterViewEvents();
        }

        private void RegisterViewEvents()
        {
            // 添加调试信息 - 记录事件绑定过程
            LogDebugInfo($"RegisterViewEvents: 开始注册视图事件");

            _view.ImmediateRenameClick += (sender, e) => HandleImmediateRenameToggle();
            _view.StopImmediateRenameClick += (sender, e) => HandleImmediateRenameToggle();
            _view.ToggleModeClick += (sender, e) => HandleModeToggle();
            _view.MonitorClick += (sender, e) => HandleFileMonitoring();
            _view.ExportSettingsClick += (sender, e) => HandleConfigImportExport();
            _view.KeyDown += (sender, e) => HandleKeyDown(e);

            // 特别记录FormClosing事件绑定
            LogDebugInfo($"RegisterViewEvents: 即将绑定FormClosing事件");
            _view.FormClosing += (sender, e) =>
            {
                LogDebugInfo($"FormClosing事件被触发! sender={sender?.GetType().Name}, e.CloseReason={e.CloseReason}");
                HandleFormClosing(e);
            };
            LogDebugInfo($"RegisterViewEvents: FormClosing事件已绑定完成");

            _view.FormLoad += (sender, e) => HandleFormLoadComplete();
            _view.Resize += (sender, e) => HandleResize();
            _view.CellValueChanged += (sender, e) => HandleCellValueChanged(e.RowIndex, e.ColumnIndex, null, null);
            _view.ColumnHeaderMouseClick += (sender, e) => HandleColumnHeaderMouseClick(e.ColumnIndex);

            LogDebugInfo($"RegisterViewEvents: 所有视图事件注册完成");
        }

        public void Initialize()
        {
            // 初始化数据
            LoadSettings();
            // 初始化文件监控器
            InitializeFileWatcher();
            // 初始化数据网格视图
            _view.InitializeDataGridView();
            // 更新UI状态
            _view.UpdateStatusStrip();
            _view.UpdateDgvFilesEditMode();
            _view.UpdateTrayMenuItems();
        }

        private void InitializeFileWatcher()
        {
            _watcher = new FileSystemWatcher();
            _watcher.Created += Watcher_Created;
            _watcher.Renamed += Watcher_Renamed;
            _watcher.Error += Watcher_Error;
        }

        public void HandleImmediateRenameToggle()
        {
            // 不再直接切换状态，让Form1中的按钮点击事件完全控制状态切换
            // 只负责更新UI显示和其他相关操作
            _view.UpdateStatusStrip();
            _view.UpdateDgvFilesEditMode();
            _view.UpdateTrayMenuItems();
        }

        public void HandleModeToggle()
        {
            HandleToggleMode(); // 调用更完整的实现
        }

        public void HandleFileMonitoring()
        {
            // 监控的开始和停止逻辑完全由Form1处理，这里不再干预
            // 仅确保状态栏菜单项更新
            _view.UpdateTrayMenuItems();
        }

        public void HandleExcelImport()
        {
            try
            {
                // 重要：在启动导入前设置父视图引用，确保ExcelImportForm能正确更新状态栏
                if (_excelImportService is ExcelImportHelper excelImportHelper)
                {
                    // 将当前视图作为父视图传递
                    IExcelParentView parentView = _view as IExcelParentView;
                    if (parentView != null)
                    {
                        excelImportHelper.SetParentView(parentView);
                        LogHelper.Debug("Form1Presenter: 已设置ExcelImportHelper的父视图引用");
                    }
                }
                
                // 启动导入流程
                bool importSuccess = _excelImportService.StartImport();

                // 如果导入成功且有有效数据
                if (importSuccess && _excelImportService.HasValidData())
                {
                    // 获取导入的数据
                    var importedData = _excelImportService.ImportedData;

                    // 关键修复: 将Excel导入表单实例传递给视图，确保能获取到选择的正则表达式
                    var excelImportFormInstance = _excelImportService.ExcelImportFormInstance;
                    if (excelImportFormInstance != null)
                    {
                        _view.SetExcelImportFormInstance(excelImportFormInstance);
                        LogHelper.Debug("Excel导入成功，ExcelImportFormInstance已传递给视图");
                    }

                    // 显示导入的数据
                    _view.DisplayImportedExcelData(importedData);

                    // 更新视图数据
                    _view.UpdateExcelData();

                    // 强制刷新UI
                    _view.Refresh();
                    Application.DoEvents();

                    // 确保Excel数据显示控件刷新
                    var dataGridView = _view.GetDgvExcelData();
                    if (dataGridView != null)
                    {
                        dataGridView.Refresh();
                        Application.DoEvents();
                    }

                    // 更新状态栏信息
                    _view.UpdateStatusStrip();
                }
                else if (importSuccess)
                {
                    // 导入成功但没有有效数据
                    // 不显示弹框提示
                }
                else
                {
                    // 导入被取消或失败
                    // 不显示弹框提示
                }
            }
            catch (Exception ex)
            {
                // 记录详细错误信息到日志
                LogHelper.Error("Excel导入失败: " + ex, ex);
                // 不显示弹框提示
            }
        }

        public void HandleConfigImportExport()
        {
            try
            {
                // 配置管理功能已移除，所有配置现在通过ApplicationSettings管理
                _view.ShowMessage("配置管理功能已移除，请使用设置窗口进行配置。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // 配置更改后重新加载设置
                LoadSettings();
            }
            catch (Exception ex)
            {
                _view.ShowMessage("配置管理失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleUndo()
        {
            _undoRedoService.Undo();
        }

        public void HandleRedo()
        {
            _undoRedoService.Redo();
        }

        public void HandleCellValueChanged(int rowIndex, int columnIndex, object oldValue, object newValue)
        {
            if (rowIndex >= 0 && columnIndex >= 0 && _view.FileBindingList != null)
            {
                var item = _view.FileBindingList[rowIndex];
                // 从视图中获取列信息
                // 假设视图有方法获取列的DataPropertyName
                // 创建编辑命令
                var command = new EditCellCommand(_view.DataGrid, rowIndex, columnIndex, oldValue, newValue);
                _undoRedoService.ExecuteCommand(command);
            }
        }

        public void HandleColumnHeaderMouseClick(int columnIndex)
        {
            if (_view.FileBindingList == null || columnIndex < 0)
                return;

            // 从视图中获取列信息
            // 假设视图有方法获取列的DataPropertyName
            string propertyName = "";
            if (string.IsNullOrEmpty(propertyName))
                return;

            // 保存当前排序前的数据顺序
            List<FileRenameInfo> originalOrder = new List<FileRenameInfo>(_view.FileBindingList);
            List<int> originalIndices = Enumerable.Range(0, _view.FileBindingList.Count).ToList();

            // 确定排序方向
            ListSortDirection direction = ListSortDirection.Ascending;
            // 假设视图有方法获取当前排序方向

            // 创建排序命令
            var sortOrder = direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;
            var command = new SortCommand(_view.DataGrid, columnIndex, sortOrder);
            _undoRedoService.ExecuteCommand(command);

            // 执行排序
            command.Execute();
        }

        public void LoadFiles(IEnumerable<FileInfo> files)
        {
            try
            {
                // 实现加载文件的逻辑
                foreach (var fileInfo in files)
                {
                    // 添加文件到视图的文件列表
                    AddOrUpdateFileRow(fileInfo);
                }
                _view.UpdateStatusStrip();
            }
            catch (Exception ex)
            {
                _view.ShowMessage("加载文件失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddOrUpdateFileRow(FileInfo fileInfo, string newFileName = null, string destPath = null,
            string regexPart = null, string orderNumber = null, string selectedMaterial = null,
            string quantity = null, string finalDimensions = null, string fixedField = null, string serialNumber = null)
        {
            if (_view.FileBindingList == null)
                return;

            // 查找第一个空行
            int emptyRowIndex = FindFirstEmptyFileRow(_view.FileBindingList);
            if (emptyRowIndex != -1)
            {
                var row = _view.FileBindingList[emptyRowIndex];
                row.OriginalName = fileInfo.Name;
                row.NewName = newFileName ?? fileInfo.Name;
                row.FullPath = destPath ?? fileInfo.FullName;
                row.RegexResult = regexPart;
                row.OrderNumber = orderNumber;
                row.Material = selectedMaterial;
                row.Quantity = quantity;
                row.Dimensions = finalDimensions;
                row.Process = fixedField;
                row.Time = DateTime.Now.ToString("MM-dd");
                row.SerialNumber = serialNumber;
                // 如果是PDF文件，获取页数
                if (fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    row.PageCount = GetPdfPageCount(fileInfo.FullName);
                }
            }
            else
            {
                _view.FileBindingList.Add(new FileRenameInfo
                {
                    OriginalName = fileInfo.Name,
                    NewName = newFileName ?? fileInfo.Name,
                    FullPath = destPath ?? fileInfo.FullName,
                    RegexResult = regexPart,
                    OrderNumber = orderNumber,
                    Material = selectedMaterial,
                    Quantity = quantity,
                    Dimensions = finalDimensions,
                    Process = fixedField,
                    Time = DateTime.Now.ToString("MM-dd"),
                    SerialNumber = serialNumber,
                    PageCount = fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? GetPdfPageCount(fileInfo.FullName) : null
                });
            }
        }

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

        private int GetPdfPageCount(string filePath)
        {
            try
            {
                // 使用统一的PDF尺寸服务获取PDF页数
                int? pageCount = _pdfDimensionService.GetPageCount(filePath);
                return pageCount ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 处理Excel导入完成事件
        /// </summary>
        public void HandleExcelImportComplete(DataTable importedData)
        {
            try
            {
                // 存储导入的数据到服务中
                if (_excelImportService != null && importedData != null && importedData.Rows.Count > 0)
                {
                    // 将导入的数据存储到服务中
                    _excelImportService.ImportedData = importedData;

                    // 更新视图的Excel导入数据
                    _view.ExcelImportedData = importedData;

                    // 更新Excel数据显示
                    _view.UpdateExcelData();

                    // 显示导入成功消息
                    _view.ShowMessage("Excel数据导入完成！共导入" + importedData.Rows.Count + "条数据。", "导入成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _view.ShowMessage("未找到有效数据或导入服务不可用。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("处理Excel导入数据失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理清除Excel数据事件
        /// </summary>
        public void HandleClearExcelData()
        {
            try
            {
                if (_excelImportService != null)
                {
                    // 清除Excel导入服务中的数据
                    _excelImportService.ClearData();

                    // 清除视图中的Excel导入数据
                    _view.ExcelImportedData = null;

                    // 更新Excel数据显示
                    _view.UpdateExcelData();

                    // 显示清除成功消息
                    _view.ShowMessage("Excel导入数据已清除。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("清除Excel导入数据失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理模式切换事件
        /// </summary>
        public void HandleToggleMode()
        {
            try
            {
                // 不再直接切换状态，让Form1中的按钮点击事件完全控制状态切换
                // 只负责更新UI显示和其他相关操作
                _view.UpdateStatusStrip();
                _view.UpdateTrayMenuItems();
            }
            catch (Exception ex)
            {
                _view.ShowMessage("切换模式失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理快捷键按下事件
        /// </summary>
        public void HandleKeyDown(KeyEventArgs e)
        {
            try
            {
                // 处理撤销快捷键 (Ctrl+Z)
                if (e.Control && e.KeyCode == Keys.Z)
                {
                    HandleUndo();
                }
                // 处理重做快捷键 (Ctrl+Y)
                else if (e.Control && e.KeyCode == Keys.Y)
                {
                    HandleRedo();
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("快捷键处理失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void UpdateFileList()
        {
            // 实现更新文件列表的逻辑
            _view.UpdateStatusStrip();
        }

        public void SaveSettings()
        {
            try
            {
                _isSaving = true;
                
                // 调用视图的自动保存方法来保存JSON文件数据
                _view.PerformAutoSave();

                // 保存cmbRegex选择到Properties.Settings
                string selectedRegex = _view.GetCmbRegexSelectedItem();
                if (!string.IsNullOrEmpty(selectedRegex))
                {
                    AppSettings.LastSelectedRegex = selectedRegex;
                    AppSettings.Save();
                    LogHelper.Debug($"Presenter.SaveSettings: 已保存LastSelectedRegex = '{selectedRegex}'");
                }

                _isSaving = false;
            }
            catch (Exception ex)
            {
                _isSaving = false;
                _view.ShowMessage("保存设置失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadSettings()
        {
            try
            {
                // 从 AppSettings 加载设置
                _view.CurrentConfigName = "默认";

                // 从 AppSettings 加载正则表达式模式
                string regexPatternsStr = AppSettings.RegexPatterns;
                _regexPatterns = new Dictionary<string, string>();
                if (!string.IsNullOrEmpty(regexPatternsStr))
                {
                    string[] patterns = regexPatternsStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string pattern in patterns)
                    {
                        string[] parts = pattern.Split(new[] { '=' }, 2);
                        if (parts.Length == 2 && !_regexPatterns.ContainsKey(parts[0]))
                        {
                            _regexPatterns.Add(parts[0], parts[1]);
                        }
                    }
                }

                // 从 AppSettings 加载材料列表
                _materials = new List<string>();
                string materialsStr = AppSettings.Material;
                if (!string.IsNullOrEmpty(materialsStr))
                {
                    _materials.AddRange(materialsStr.Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("加载设置失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理表单加载完成事件
        /// </summary>
        public void HandleFormLoadComplete()
        {
            try
            {
                Initialize();
            }
            catch (Exception ex)
            {
                _view.ShowMessage("表单加载完成处理失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理表单关闭事件
        /// </summary>
        public void HandleFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // 调试输出 - 记录FormClosing事件开始
                LogHelper.Debug("HandleFormClosing: 事件开始 - CloseReason=" + e.CloseReason + ", _allowClose=" + _allowClose + ", Cancel=" + e.Cancel);
                LogDebugInfo($"HandleFormClosing: 事件开始 - CloseReason={e.CloseReason}, _allowClose={_allowClose}, Cancel={e.Cancel}");

                if (_isSaving)
                {
                    LogDebugInfo($"HandleFormClosing: 跳过，正在保存中");
                    return;
                }

                // 先执行自动保存逻辑（无论是否会关闭窗口）
                SaveSettings();

                // 如果是用户点击关闭按钮且不允许关闭，则最小化到托盘
                if (e.CloseReason == CloseReason.UserClosing && !_allowClose)
                {
                    // 调试输出
                    LogHelper.Debug("HandleFormClosing: 用户关闭窗口，执行最小化到托盘逻辑");
                    LogDebugInfo($"HandleFormClosing: 用户关闭窗口，执行最小化到托盘逻辑");

                    // 最小化到托盘逻辑
                    e.Cancel = true; // 取消窗口关闭
                    _view.WindowState = FormWindowState.Minimized;
                    _view.Hide();

                    // 确保托盘图标显示
                    _view.TrayIcon.Visible = true;

                    LogHelper.Debug("HandleFormClosing: 操作后 - WindowState=" + _view.WindowState + ", trayIcon.Visible=" + _view.TrayIcon.Visible + ", e.Cancel=" + e.Cancel);
                    LogDebugInfo($"HandleFormClosing: 操作后 - WindowState={_view.WindowState}, trayIcon.Visible={_view.TrayIcon.Visible}, e.Cancel={e.Cancel}");
                    return;
                }

                // 如果是真正的退出（如通过菜单退出），则允许程序退出
                LogHelper.Debug("HandleFormClosing: 允许程序退出 - CloseReason=" + e.CloseReason + ", _allowClose=" + _allowClose);
                LogDebugInfo($"HandleFormClosing: 允许程序退出 - CloseReason={e.CloseReason}, _allowClose={_allowClose}");
            }
            catch (Exception ex)
            {
                _view.ShowMessage("表单关闭处理失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                System.Diagnostics.Debug.WriteLine($"Presenter日志记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理表单调整大小事件
        /// </summary>
        public void HandleResize()
        {
            try
            {
                // 检查窗口是否被最小化到托盘
                if (_view.WindowState == FormWindowState.Minimized && !_allowClose)
                {
                    _view.Hide();
                }
            }
            catch (Exception)
            {
                // 这里不显示错误消息，因为调整大小是频繁操作
                // 可以记录日志
            }
        }


        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                // 处理文件创建事件
                _pendingFiles.Add(new FileInfo(e.FullPath));
                // 延迟处理以确保文件完全写入
                Task.Delay(1000).ContinueWith(t => ProcessPendingFiles());
            }
            catch
            {
                // 记录错误日志
            }
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            try
            {
                // 处理文件重命名事件
                _pendingFiles.Add(new FileInfo(e.FullPath));
                // 延迟处理以确保文件完全写入
                Task.Delay(1000).ContinueWith(t => ProcessPendingFiles());
            }
            catch
            {
                // 记录错误日志
            }
        }

        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            try
            {
                // 处理监控错误
                _view.ShowMessage("文件监控错误: " + e.GetException().Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
                // 忽略错误
            }
        }

        private void ProcessPendingFiles()
        {
            if (_pendingFiles.Count == 0)
                return;

            // 处理待处理的文件
            try
            {
                foreach (var fileInfo in _pendingFiles)
                {
                    // 处理文件
                    // 这里可以根据需要添加文件处理逻辑
                }
            }
            finally
            {
                _pendingFiles.Clear();
            }
        }

        /// <summary>
        /// 处理添加文件到网格事件
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
        public void HandleAddFileToGrid(FileInfo fileInfo, string material, string orderNumber, string quantity, string unit, string adjustedDimensions, string fixedField, string serialNumber, string compositeColumnValue = "")
        {
            try
            {
                // 添加调试信息
                LogHelper.Debug("=== 开始处理添加文件到网格 ===");
            LogHelper.Debug("文件名: " + fileInfo.Name);
            LogHelper.Debug("当前正则表达式模式: '" + _view.CurrentRegexPattern + "'");
            LogHelper.Debug("cmbRegex当前选中项: '" + _view.GetCmbRegexSelectedItem() + "'");
            LogHelper.Debug("获取的正则表达式模式: '" + _view.GetSelectedRegexPattern() + "'");
                
                // 显示当前使用的重命名规则
                LogHelper.Debug("当前使用的重命名规则:");
                LogHelper.Debug("  - 文件名: " + fileInfo.Name);
                LogHelper.Debug("  - CurrentRegexPattern: '" + _view.CurrentRegexPattern + "'");
                LogHelper.Debug("  - GetSelectedRegexPattern(): '" + _view.GetSelectedRegexPattern() + "'");
                LogHelper.Debug("  - cmbRegex选中项: '" + _view.GetCmbRegexSelectedItem() + "'");
                LogHelper.Debug("  - 模式来源: Presenter中的HandleAddFileToGrid方法");
                
                // 获取文件重命名服务
                var fileRenameService = ServiceLocator.Instance.GetFileRenameService() as WindowsFormsApp3.Interfaces.IFileRenameService;
                if (fileRenameService == null)
                {
                    _view.ShowMessage("无法获取文件重命名服务", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 判断序号是否来自Excel
                bool isSerialNumberFromExcel = false;
                if (_view.ExcelImportedData != null && _view.ExcelSearchColumnIndex >= 0)
                {
                    // 如果有Excel数据且有序号列，则认为序号来自Excel
                    isSerialNumberFromExcel = true;
                    LogHelper.Debug($"序号来自Excel: ExcelImportedData不为空，ExcelSearchColumnIndex={_view.ExcelSearchColumnIndex}");
                }
                else
                {
                    LogHelper.Debug($"序号非Excel来源: ExcelImportedData={_view.ExcelImportedData != null}, ExcelSearchColumnIndex={_view.ExcelSearchColumnIndex}");
                }

                // 验证输入参数
                var validationResult = fileRenameService.ValidateFileGridInput(fileInfo, material, orderNumber, quantity, serialNumber, isSerialNumberFromExcel);
                if (!validationResult.IsValid)
                {
                    _view.ShowMessage(validationResult.ErrorMessage, "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 检查必要条件
                if (string.IsNullOrEmpty(quantity) && string.IsNullOrEmpty(serialNumber))
                {
                    return;
                }

                // 获取网格绑定列表
                var gridBindingList = _view.FileBindingList;
                if (gridBindingList == null)
                {
                    _view.ShowMessage("无法获取文件列表", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 生成或验证序号
                // 注意：这里需要访问Form1中的_excelNewColumnIndex字段，我们可以通过视图属性来处理
                // bool isFromExcel = false; // 这个值需要从视图获取，暂时注释未使用的变量
                // 获取Excel相关的属性值
                int excelNewColumnIndex = _view.ExcelNewColumnIndex;
                int excelSearchColumnIndex = _view.ExcelSearchColumnIndex;
                int excelReturnColumnIndex = _view.ExcelReturnColumnIndex;

                // 处理正则表达式匹配 - 统一使用EventGroup配置系统
                string regexPart = string.Empty;
                var config = CreateFileNameComponentsConfigFromEventGroup();

                if (config.RegexResultEnabled)
                {
                    // 使用GetSelectedRegexPattern获取正确的正则表达式模式
                    string pattern = _view.GetSelectedRegexPattern();
                    
                    // 如果获取的模式为空，尝试从CurrentRegexPattern获取
                    if (string.IsNullOrEmpty(pattern) && !string.IsNullOrEmpty(_view.CurrentRegexPattern))
                    {
                        pattern = _view.CurrentRegexPattern;
                        LogHelper.Debug("手动模式下使用CurrentRegexPattern作为后备: '" + pattern + "'");
                    }
                    
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        LogHelper.Debug("处理正则表达式，当前模式: '" + pattern + "'");
                        regexPart = ProcessRegexForFileName(fileInfo.Name, pattern, config.RegexResultEnabled);
                        LogHelper.Debug("正则表达式处理结果: '" + regexPart + "'");
                        // 添加额外的调试信息
                        LogHelper.Debug("当前使用的重命名规则:");
                        LogHelper.Debug("  - 正则表达式模式: '" + pattern + "'");
                        LogHelper.Debug("  - 模式来源: GetSelectedRegexPattern (主正则cmbRegex)");
                        LogHelper.Debug("  - 正则结果: '" + regexPart + "'");
                        
                        // 检查是否正则匹配失败需要用户处理
                        if (regexPart != null && regexPart.StartsWith("REGEX_MATCH_FAILED:"))
                        {
                            string originalName = regexPart.Substring("REGEX_MATCH_FAILED:".Length);
                            DialogResult result = _view.ShowMessageWithResult(
                                "正则表达式匹配失败，是否使用完整原文件名替代正则部分？",
                                "匹配失败",
                                MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.OK)
                            {
                                regexPart = originalName;
                            }
                            else
                            {
                                // 用户取消，不执行重命名
                                return;
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Debug("警告: 正则表达式模式为空，无法进行正则处理");
                        // 使用原文件名作为后备
                        regexPart = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    }
                }

                // 计算显示尺寸（不受DimensionsEnabled影响，始终计算尺寸用于显示和PDF处理）
                string displayDimensions = string.Empty;
                // 使用SettingsForm的CalculateFinalDimensions方法来正确处理形状参数
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
                    // 使用修改后的CalculateFinalDimensions方法，传递cornerRadius和addPdfLayers参数
                    displayDimensions = settingsForm.CalculateFinalDimensions(settingsForm.PdfWidth, settingsForm.PdfHeight, tetBleed, "0", false);
                    
                    if (!string.IsNullOrEmpty(displayDimensions))
                    {
                        var dimensionsParts = displayDimensions.Split('x');
                        string finalDimensions = displayDimensions;
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
                                        finalDimensions = $"{maxDim}x{minDim}{shapePart}";
                                    }
                                }
                            }
                            else if (double.TryParse(dimensionsParts[0], out double dim1) &&
                                     double.TryParse(dimensionsParts[1], out double dim2))
                            {
                                // 确保大数在前
                                double maxDim = Math.Max(dim1, dim2);
                                double minDim = Math.Min(dim1, dim2);
                                finalDimensions = $"{maxDim}x{minDim}";
                            }
                        }
                        displayDimensions = finalDimensions;
                    }
                }

                // 获取导出路径
                // 在批量模式下不弹出路径选择框
                string exportPath = string.Empty;
                if (!_view.IsImmediateRenameActive)
                {
                    // 批量模式下使用默认导出路径或上次使用的路径
                    exportPath = AppSettings.LastExportPath ?? string.Empty;
                    // 如果没有设置导出路径，则使用文档目录
                    if (string.IsNullOrEmpty(exportPath))
                    {
                        exportPath = AppDataPathManager.ExcelExportDirectory;
                    }
                }
                else
                {
                    // 手动模式下仍然弹出路径选择框
                    exportPath = _view.GetExportPath();
                    // 如果用户取消选择，直接返回
                    if (string.IsNullOrEmpty(exportPath))
                    {
                        return;
                    }
                }

                // 创建文件名组件
                var components = CreateFileNameComponents(
                    fileInfo, material, orderNumber, quantity, unit,
                    displayDimensions, // 传递计算出的尺寸信息
                    fixedField, serialNumber, regexPart,
                    AppSettings.Separator ?? "", config, compositeColumnValue);

                // 构建新文件名
                string newFileName = BuildNewFileName(components);
                if (string.IsNullOrEmpty(newFileName))
                {
                    _view.ShowMessage("无法构建有效的文件名", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 解决文件名冲突
                string destPath = Path.Combine(exportPath, newFileName);
                destPath = HandleFileNameConflict(destPath);
                newFileName = Path.GetFileName(destPath);

                // 创建处理后的数据对象
                var processedData = new ProcessedFileData
                {
                    NewFileName = newFileName,
                    DestinationPath = fileInfo.FullName,
                    RegexResult = regexPart,
                    OrderNumber = orderNumber,
                    Material = material,
                    Quantity = quantity,
                    Dimensions = displayDimensions,
                    Process = fixedField,
                    SerialNumber = serialNumber,
                    CompositeColumn = compositeColumnValue
                };
                
                LogHelper.Debug($"HandleAddFileToGrid: 创建 ProcessedFileData, CompositeColumn = '{processedData.CompositeColumn}'");

                // 调用服务层方法添加文件到网格
                bool success = fileRenameService.AddFileToGrid(fileInfo, processedData, gridBindingList);
                if (!success)
                {
                    _view.ShowMessage("添加文件到网格失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    // 在手动模式下，添加数据后强制刷新界面以确保列组合值正确显示
                    if (_view.IsImmediateRenameActive)
                    {
                        LogHelper.Debug($"手动模式下强制刷新界面: compositeColumnValue = '{compositeColumnValue}'");
                        
                        // 检查数据是否正确添加
                        if (gridBindingList is BindingList<FileRenameInfo> bindingList && bindingList.Count > 0)
                        {
                            var lastItem = bindingList[bindingList.Count - 1];
                            LogHelper.Debug($"最后一行数据检查: OriginalName='{lastItem.OriginalName}', CompositeColumn='{lastItem.CompositeColumn}'");
                            
                            // 检查所有行的列组合值
                            for (int i = 0; i < bindingList.Count; i++)
                            {
                                var item = bindingList[i];
                                if (!string.IsNullOrEmpty(item.CompositeColumn))
                                {
                                    LogHelper.Debug($"行{i}: OriginalName='{item.OriginalName}', CompositeColumn='{item.CompositeColumn}'");
                                }
                            }
                        }
                        
                        // 强制刷新DataGridView以确保列组合值立即显示
                        if (gridBindingList is BindingList<FileRenameInfo> bindingList2)
                        {
                            // 触发绑定列表的重置事件，强制UI刷新
                            LogHelper.Debug("执行 ResetBindings()");
                            bindingList2.ResetBindings();
                        }
                        
                        // 强制刷新视图
                        LogHelper.Debug("执行 UpdateFileList()");
                        _view.UpdateFileList();
                        
                        // 新增：直接刷新DataGridView
                        LogHelper.Debug("执行 RefreshDgvFiles()");
                        _view.RefreshDgvFiles();
                        
                        // 检查DataGridView的数据源
                        LogHelper.Debug($"DataGridView数据源检查: 当前数据源类型 = {_view.FileBindingList?.GetType()?.Name}, 行数 = {_view.FileBindingList?.Count}");
                        
                        // 检查列组合列是否存在和可见
                        var dgv = _view.GetType().GetField("dgvFiles", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(_view) as DataGridView;
                        if (dgv != null)
                        {
                            var compositeCol = dgv.Columns["colCompositeColumn"];
                            if (compositeCol != null)
                            {
                                LogHelper.Debug($"列组合列检查: 存在={compositeCol != null}, 可见={compositeCol.Visible}, DataPropertyName='{compositeCol.DataPropertyName}'");
                            }
                            else
                            {
                                LogHelper.Debug("警告：列组合列(colCompositeColumn)不存在！");
                            }
                        }
                    }
                }

                // 更新状态栏
                _view.UpdateStatusStrip();
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"处理文件时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从EventGroup配置创建文件名组件配置（统一配置系统）
        /// </summary>
        /// <returns>文件名组件配置</returns>
        private FileNameComponentsConfig CreateFileNameComponentsConfigFromEventGroup()
        {
            var config = new FileNameComponentsConfig();

            try
            {
                LogHelper.Debug("[CreateFileNameComponentsConfigFromEventGroup] 开始从EventGroup创建配置");

                // 从EventGroup配置获取设置
                var eventGroupConfig = SettingsForm.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups != null && eventGroupConfig.Items != null)
                {
                    LogHelper.Debug("[CreateFileNameComponentsConfigFromEventGroup] 使用EventGroup配置");

                    // 遍历所有启用的项目，设置对应的配置
                    foreach (var item in eventGroupConfig.Items.Where(i => i.IsEnabled))
                    {
                        string componentType = MapEventItemToComponentType(item.Name);

                        switch (componentType)
                        {
                            case "正则结果":
                                config.RegexResultEnabled = true;
                                break;
                            case "订单号":
                                config.OrderNumberEnabled = true;
                                break;
                            case "材料":
                                config.MaterialEnabled = true;
                                break;
                            case "数量":
                                config.QuantityEnabled = true;
                                break;
                            case "尺寸":
                                config.DimensionsEnabled = true;
                                break;
                            case "工艺":
                                config.ProcessEnabled = true;
                                break;
                            case "序号":
                                config.SerialNumberEnabled = true;
                                break;
                            case "行数":
                                config.LayoutRowsEnabled = true;
                                break;
                            case "列数":
                                config.LayoutColumnsEnabled = true;
                                break;
                            case "列组合":
                                config.CompositeColumnEnabled = true;
                                break;
                            default:
                                LogHelper.Warn($"[CreateFileNameComponentsConfigFromEventGroup] 未知的组件类型: {componentType}");
                                break;
                        }

                        LogHelper.Debug($"[CreateFileNameComponentsConfigFromEventGroup] 启用组件: {componentType}");
                    }
                }
                else
                {
                    LogHelper.Warn("[CreateFileNameComponentsConfigFromEventGroup] EventGroup配置为空，使用默认配置");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[CreateFileNameComponentsConfigFromEventGroup] 创建配置时发生异常: {ex.Message}");
                // 发生异常时使用默认配置
            }

            LogHelper.Debug($"[CreateFileNameComponentsConfigFromEventGroup] 配置结果 - RegexResultEnabled:{config.RegexResultEnabled}, OrderNumberEnabled:{config.OrderNumberEnabled}, MaterialEnabled:{config.MaterialEnabled}, QuantityEnabled:{config.QuantityEnabled}, DimensionsEnabled:{config.DimensionsEnabled}, ProcessEnabled:{config.ProcessEnabled}, SerialNumberEnabled:{config.SerialNumberEnabled}, LayoutRowsEnabled:{config.LayoutRowsEnabled}, LayoutColumnsEnabled:{config.LayoutColumnsEnabled}, CompositeColumnEnabled:{config.CompositeColumnEnabled}");

            return config;
        }

        /// <summary>
        /// 解析文件名组件配置（保留用于向后兼容）
        /// </summary>
        /// <param name="eventItems">事件项字符串</param>
        /// <returns>文件名组件配置</returns>
        private FileNameComponentsConfig ParseFileNameConfig(string eventItems)
        {
            var config = new FileNameComponentsConfig();

            try
            {
                LogHelper.Debug($"[ParseFileNameConfig] 输入参数 eventItems='{eventItems}'");

                if (string.IsNullOrEmpty(eventItems))
                {
                    LogHelper.Debug("[ParseFileNameConfig] eventItems为空，返回默认配置（全部启用）");
                    return config; // 返回默认配置（全部启用）
                }

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
                                case "列组合":
                                    config.CompositeColumnEnabled = isEnabled;
                                    break;
                                case "行数":
                                    config.LayoutRowsEnabled = isEnabled;
                                    break;
                                case "列数":
                                    config.LayoutColumnsEnabled = isEnabled;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug("解析文件名配置时发生异常: " + ex.Message);
                // 异常情况下返回默认配置（全部启用）
            }

            LogHelper.Debug($"[ParseFileNameConfig] 解析结果 - LayoutRowsEnabled:{config.LayoutRowsEnabled}, LayoutColumnsEnabled:{config.LayoutColumnsEnabled}");
            return config;
        }

        /// <summary>
        /// 处理文件名中的正则表达式匹配
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="pattern">正则表达式模式</param>
        /// <param name="isRegexEnabled">是否启用正则结果</param>
        /// <returns>正则匹配结果，如果用户取消则返回null</returns>
        private string ProcessRegexForFileName(string fileName, string pattern, bool isRegexEnabled)
        {
            // 添加简洁的调试信息
            LogHelper.Debug("ProcessRegexForFileName: 文件名 '" + fileName + "'，模式 '" + pattern + "'，启用状态 " + isRegexEnabled);
            
            // 验证正则表达式模式是否有效
            if (string.IsNullOrEmpty(pattern))
            {
                LogHelper.Debug("ProcessRegexForFileName: 正则表达式模式为空，返回空字符串");
                return string.Empty;
            }
            
            if (!isRegexEnabled)
            {
                LogHelper.Debug("ProcessRegexForFileName: 正则表达式未启用，返回空字符串");
                return string.Empty;
            }

            var regexResult = ProcessRegexMatch(fileName, pattern);
            if (!regexResult.IsMatch)
            {
                // 添加调试信息
                LogHelper.Debug("正则表达式匹配失败，返回特殊值");
                // 在服务层中，我们不直接显示对话框，而是返回一个特殊值表示需要处理
                string failResult = "REGEX_MATCH_FAILED:" + Path.GetFileNameWithoutExtension(fileName);
                LogHelper.Debug("返回结果: " + failResult);
                return failResult;
            }

            LogHelper.Debug("正则表达式匹配成功，返回匹配文本: " + regexResult.MatchedText);
            return regexResult.MatchedText;
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
                // 添加简洁的调试信息
                LogHelper.Debug("ProcessRegexMatch: 文件名 '" + fileName + "'，模式 '" + pattern + "'，名称 '" + patternName + "'");
                if (string.IsNullOrEmpty(fileName))
                {
                    LogHelper.Debug("ProcessRegexMatch: 文件名为空，返回失败结果");
                    return RegexMatchResult.Failure("", "", "文件名不能为空", "ProcessRegexMatch");
                }

                if (string.IsNullOrEmpty(pattern))
                {
                    LogHelper.Debug("ProcessRegexMatch: 正则表达式模式为空，返回失败结果");
                    return RegexMatchResult.Failure(fileName, "", "正则表达式模式不能为空", "ProcessRegexMatch");
                }

                var originalName = Path.GetFileNameWithoutExtension(fileName);
                var regexMatch = Regex.Match(originalName, pattern);

                if (regexMatch.Success)
                {
                    LogHelper.Debug("ProcessRegexMatch: 匹配成功 - 文本: '" + regexMatch.Value + "'，捕获组数量: " + regexMatch.Groups.Count);
                    
                    // 输出所有捕获组的内容
                    if (regexMatch.Groups.Count > 1)  // 只有当有多个捕获组时才输出详细信息
                    {
                        string groupsInfo = string.Join(", ", Enumerable.Range(0, regexMatch.Groups.Count)
                            .Select(i => $"组{i}: '{regexMatch.Groups[i].Value}'"));
                        LogHelper.Debug("ProcessRegexMatch: 捕获组 - " + groupsInfo);
                    }
                    
                    // 针对特定模式增加特殊处理
                    if (pattern == "^(.*?)-\\d+\\+.*$")
                    {
                        // 确保只获取第一个捕获组
                        if (regexMatch.Groups.Count > 1)
                        {
                            LogHelper.Debug("ProcessRegexMatch: 使用特殊处理，获取第一个捕获组: '" + regexMatch.Groups[1].Value + "'");
                            var result = RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                            result.MatchedText = regexMatch.Groups[1].Value;
                            return result;
                        }
                        else
                        {
                            LogHelper.Debug("ProcessRegexMatch: 使用普通处理");
                            return RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                        }
                    }
                    else
                    {
                        LogHelper.Debug("ProcessRegexMatch: 使用普通处理");
                        return RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                    }
                }
                else
                {
                    LogHelper.Debug("ProcessRegexMatch: 匹配失败 - 模式='" + pattern + "', 文本='" + originalName + "'");
                    return RegexMatchResult.Failure(originalName, pattern, "正则表达式匹配失败: 模式='" + pattern + "', 文本='" + originalName + "'", "ProcessRegexMatch");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug("正则匹配过程中发生异常: " + ex.Message);
                return RegexMatchResult.Failure(fileName, pattern, "正则匹配过程中发生异常: " + ex.Message, "ProcessRegexMatch");
            }
        }

        /// <summary>
        /// 创建文件名组件对象
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="material">材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="unit">单位</param>
        /// <param name="dimensions">尺寸</param>
        /// <param name="notes">工艺</param>
        /// <param name="serialNumber">序号</param>
        /// <param name="regexPart">正则部分</param>
        /// <param name="separator">分隔符</param>
        /// <param name="enabledComponents">启用的组件配置</param>
        /// <param name="compositeColumnValue">组合列值</param>
        /// <returns>文件名组件对象</returns>
        private FileNameComponents CreateFileNameComponents(FileInfo fileInfo, string material, string orderNumber,
            string quantity, string unit, string dimensions, string notes, string serialNumber, string regexPart,
            string separator, FileNameComponentsConfig enabledComponents, string compositeColumnValue = "")
        {
            // 添加调试日志
            var layoutRows = SettingsForm.GetLayoutRowsForRenaming();
            var layoutColumns = SettingsForm.GetLayoutColumnsForRenaming();
            
            LogHelper.Debug($"[CreateFileNameComponents] 开始创建文件名组件");
            LogHelper.Debug($"[CreateFileNameComponents] LayoutRows='{layoutRows}', LayoutColumns='{layoutColumns}'");
            LogHelper.Debug($"[CreateFileNameComponents] 基础数据 - OrderNumber='{orderNumber}', Material='{material}', Quantity='{quantity}', Unit='{unit}', Dimensions='{dimensions}', Notes='{notes}', SerialNumber='{serialNumber}', RegexPart='{regexPart}'");
            LogHelper.Debug($"[CreateFileNameComponents] 组件配置 - RegexResultEnabled:{enabledComponents?.RegexResultEnabled}, OrderNumberEnabled:{enabledComponents?.OrderNumberEnabled}, MaterialEnabled:{enabledComponents?.MaterialEnabled}, QuantityEnabled:{enabledComponents?.QuantityEnabled}, DimensionsEnabled:{enabledComponents?.DimensionsEnabled}, ProcessEnabled:{enabledComponents?.ProcessEnabled}, SerialNumberEnabled:{enabledComponents?.SerialNumberEnabled}, LayoutRowsEnabled:{enabledComponents?.LayoutRowsEnabled}, LayoutColumnsEnabled:{enabledComponents?.LayoutColumnsEnabled}");
            
            // 获取EventGroup配置
            var eventGroupConfig = SettingsForm.GetEventGroupConfiguration();

            // 构建保留分组配置字典
            var preserveGroupConfig = new Dictionary<string, bool>();
            if (eventGroupConfig?.Groups != null)
            {
                foreach (var group in eventGroupConfig.Groups)
                {
                    preserveGroupConfig[group.Id] = group.IsPreserved;
                    LogHelper.Debug($"[CreateFileNameComponents] 保留分组配置: {group.DisplayName} ({group.Id}) -> {group.IsPreserved}");
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
                CompositeColumn = compositeColumnValue,
                LayoutRows = layoutRows,
                LayoutColumns = layoutColumns,
                FileExtension = fileInfo.Extension,
                Separator = separator,
                EnabledComponents = enabledComponents,
                // 从配置中获取重命名规则顺序
                ComponentOrder = GetComponentOrderFromSettings(),
                // 从EventGroup配置中获取前缀
                Prefixes = GetPrefixesFromEventGroupConfig(),
                // 设置保留分组配置
                PreserveGroupConfig = preserveGroupConfig,
                // 设置原始文件名用于保留分组检测
                OriginalFileName = Path.GetFileNameWithoutExtension(fileInfo.Name)
            };
            
            LogHelper.Debug($"[CreateFileNameComponents] 原始文件名: '{components.OriginalFileName}'");
            LogHelper.Debug($"[CreateFileNameComponents] ComponentOrder: [{string.Join(", ", components.ComponentOrder ?? new List<string>())}]");
            LogHelper.Debug($"[CreateFileNameComponents] Prefixes: {string.Join(", ", components.Prefixes?.Select(kvp => $"{kvp.Key}={kvp.Value}") ?? Enumerable.Empty<string>())}");

            // 确保分隔符为合法文件名字符（允许空字符串）
            if (!string.IsNullOrEmpty(components.Separator) && Path.GetInvalidFileNameChars().Contains(components.Separator[0]))
            {
                LogHelper.Debug($"[CreateFileNameComponents] 分隔符 '{components.Separator}' 包含非法字符，替换为 '_'");
                components.Separator = "_";
            }
            
            LogHelper.Debug($"[CreateFileNameComponents] 最终Separator='{components.Separator}', FileExtension='{components.FileExtension}'");

            return components;
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
                    LogHelper.Debug($"文件名组件验证失败: {validationResult.ErrorMessage}");
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
                LogHelper.Debug($"构建文件名时发生异常: {ex.Message}");
                return $"未命名{components.FileExtension}";
            }
        }

        /// <summary>
        /// 从设置中获取组件顺序
        /// </summary>
        /// <returns>组件顺序列表</returns>
        private List<string> GetComponentOrderFromSettings()
        {
            var componentOrder = new List<string>();
            
            try
            {
                // 首先尝试从EventGroup配置获取组件顺序
                var eventGroupConfig = SettingsForm.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups != null && eventGroupConfig.Items != null)
                {
                    LogHelper.Debug("GetComponentOrderFromSettings: 使用EventGroup配置设置组件顺序");
                    
                    // 按照组的SortOrder排序
                    var sortedGroups = eventGroupConfig.Groups
                        .Where(g => g.IsEnabled)
                        .OrderBy(g => g.SortOrder)
                        .ToList();
                    
                    LogHelper.Debug($"GetComponentOrderFromSettings: 启用的组数量: {sortedGroups.Count}");
                    
                    foreach (var group in sortedGroups)
                    {
                        // 获取该组下启用的项目
                        var groupItems = eventGroupConfig.Items
                            .Where(item => item.GroupId == group.Id && item.IsEnabled)
                            .OrderBy(item => item.SortOrder)
                            .ToList();
                        
                        LogHelper.Debug($"GetComponentOrderFromSettings: 组 '{group.DisplayName}'({group.Id}) 包含 {groupItems.Count} 个启用项目");
                        
                        foreach (var item in groupItems)
                        {
                            // 将EventGroup项目名称映射到FileNameComponents期望的组件类型
                            string componentType = MapEventItemToComponentType(item.Name);
                            if (!string.IsNullOrEmpty(componentType) && !componentOrder.Contains(componentType))
                            {
                                componentOrder.Add(componentType);
                                LogHelper.Debug($"GetComponentOrderFromSettings: 从EventGroup添加组件 '{componentType}' (原项目名: {item.Name}, 组: {group.DisplayName})");
                            }
                        }
                    }
                }
                else
                {
                    LogHelper.Warn("GetComponentOrderFromSettings: EventGroup配置为空，尝试使用旧的EventItems配置");
                    
                    // 回退到旧的EventItems配置
                    string eventItemsStr = AppSettings.EventItems ?? string.Empty;
                    
                    // 从设置中获取事件顺序配置
                    var eventItems = eventItemsStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    // 如果EventItems为空或无效，使用默认顺序规则
                    if (eventItems.Length == 0)
                    {
                        // 默认顺序规则：正则结果|序号|订单号|材料|数量|工艺|尺寸
                        componentOrder.AddRange(new[] { "正则结果", "序号", "订单号", "材料", "数量", "工艺", "尺寸" });
                        LogHelper.Debug("GetComponentOrderFromSettings: 使用默认组件顺序");
                    }
                    else
                    {
                        // 解析EventItems格式：偶数位置是组件名称，奇数位置是布尔值表示是否启用
                        for (int i = 0; i < eventItems.Length; i += 2)
                        {
                            if (i + 1 < eventItems.Length)
                            {
                                bool isEnabled;
                                if (bool.TryParse(eventItems[i + 1].Trim(), out isEnabled) && isEnabled)
                                {
                                    // 将旧的项目名称映射到正确的组件类型
                                    string componentType = MapEventItemToComponentType(eventItems[i].Trim());
                                    if (!string.IsNullOrEmpty(componentType))
                                    {
                                        componentOrder.Add(componentType);
                                        LogHelper.Debug($"GetComponentOrderFromSettings: 从EventItems添加组件 '{componentType}' (原项目名: {eventItems[i].Trim()})");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"GetComponentOrderFromSettings: 获取组件顺序时发生异常: {ex.Message}");
                // 使用默认顺序作为后备
                componentOrder.AddRange(new[] { "正则结果", "序号", "订单号", "材料", "数量", "工艺", "尺寸" });
            }
            
            if (componentOrder.Count > 0)
            {
                LogHelper.Info($"GetComponentOrderFromSettings: 最终组件顺序 = [{string.Join(", ", componentOrder)}]");
            }
            else
            {
                LogHelper.Warn("GetComponentOrderFromSettings: 没有设置组件顺序，将使用默认顺序");
                componentOrder.AddRange(new[] { "正则结果", "序号", "订单号", "材料", "数量", "工艺", "尺寸" });
            }
            
            return componentOrder;
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

            // 处理保留分组前缀 - 移除 [*] 或 [保留] 前缀
            string cleanItemName = itemName;
            if (itemName.StartsWith("[*] "))
            {
                cleanItemName = itemName.Substring(4).Trim(); // 移除 "[*] " 前缀
            }
            else if (itemName.StartsWith("[保留] "))
            {
                cleanItemName = itemName.Substring(5).Trim(); // 移除 "[保留] " 前缀
            }

            LogHelper.Debug($"MapEventItemToComponentType: 原项目名 '{itemName}' -> 清理后 '{cleanItemName}'");

            // 根据清理后的项目名称映射到对应的组件类型
            switch (cleanItemName)
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
                    LogHelper.Warn($"MapEventItemToComponentType: 未知的项目名称 '{itemName}' (清理后: '{cleanItemName}')");
                    return string.Empty;
            }
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
                    LogHelper.Debug("EventGroup配置为空，使用空前缀字典");
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

                        LogHelper.Debug($"[GetPrefixesFromEventGroupConfig] 正在处理: item.Name='{item.Name}', group.Id='{group.Id}', group.Prefix='{group.Prefix}'");

                        // 建立组件名到前缀的映射
                        if (!prefixes.ContainsKey(item.Name))
                        {
                            prefixes[item.Name] = group.Prefix;
                            LogHelper.Debug($"[GetPrefixesFromEventGroupConfig] 已添加映射: '{item.Name}' -> '{group.Prefix}'");
                        }
                        else
                        {
                            LogHelper.Debug($"[GetPrefixesFromEventGroupConfig] 键已存在，跳过: '{item.Name}'");
                        }
                    }
                }

                LogHelper.Debug($"获取到的前缀配置: {string.Join(", ", prefixes.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"获取前缀配置时发生异常: {ex.Message}");
            }

            return prefixes;
        }

        /// <summary>
        /// 检查文件名冲突
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>处理冲突后的文件路径</returns>
        private string HandleFileNameConflict(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int counter = 1;
            string newFilePath;

            // 循环直到找到不冲突的文件名
            do
            {
                newFilePath = Path.Combine(directory, $"{fileName}({counter}){extension}");
                counter++;
            } while (File.Exists(newFilePath));

            return newFilePath;
        }

        /// <summary>
        /// 处理立即重命名文件事件
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="selectedMaterial">选择的材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="unit">单位</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="tetBleed">TET出血</param>
        /// <summary>
        /// 处理立即重命名文件事件
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="selectedMaterial">选择的材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="unit">单位</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="tetBleed">TET出血</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="fixedField">固定字段</param>
        /// <param name="serialNumber">序号</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="usePdfLastPage">是否使用PDF最后一页</param>
        /// <param name="addPdfLayers">是否添加PDF图层</param>
        /// <param name="compositeColumnValue">列组合值</param>
        public void HandleRenameFileImmediately(FileInfo fileInfo, string selectedMaterial, string orderNumber, string quantity, string unit, string exportPath, double tetBleed, string width, string height, string fixedField, string serialNumber, string cornerRadius, bool usePdfLastPage, bool addPdfLayers, string compositeColumnValue = "")
        {
            try
            {
                // 添加简洁的调试信息：记录重命名操作开始
                string regexPattern = _view.GetSelectedRegexPattern();
                LogHelper.Debug("=== 开始处理文件重命名 === [配置: " + _view.CurrentConfigName + "]");
                LogHelper.Debug("文件名: " + fileInfo.Name);
                LogHelper.Debug("使用的正则表达式: '" + regexPattern + "'");
                LogHelper.Debug("正则表达式来源: " + _view.GetCmbRegexSelectedItem());

                
                // 获取文件重命名服务
                var fileRenameService = ServiceLocator.Instance.GetFileRenameService() as WindowsFormsApp3.Interfaces.IFileRenameService;
                if (fileRenameService == null)
                {
                    _view.ShowMessage("无法获取文件重命名服务", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 判断序号是否来自Excel
                bool isSerialNumberFromExcel = false;
                if (_view.ExcelImportedData != null && _view.ExcelSearchColumnIndex >= 0)
                {
                    // 如果有Excel数据且有序号列，则认为序号来自Excel
                    isSerialNumberFromExcel = true;
                    LogHelper.Debug($"序号来自Excel: ExcelImportedData不为空，ExcelSearchColumnIndex={_view.ExcelSearchColumnIndex}");
                }
                else
                {
                    LogHelper.Debug($"序号非Excel来源: ExcelImportedData={_view.ExcelImportedData != null}, ExcelSearchColumnIndex={_view.ExcelSearchColumnIndex}");
                }

                // 验证重命名操作
                var validationResult = fileRenameService.ValidateFileGridInput(fileInfo, selectedMaterial, orderNumber, quantity, serialNumber, isSerialNumberFromExcel);
                if (!validationResult.IsValid)
                {
                    _view.ShowMessage(validationResult.ErrorMessage, "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 构建新文件名组件 - 统一使用EventGroup配置系统
                var config = CreateFileNameComponentsConfigFromEventGroup();

                // 处理正则表达式匹配
                string regexPart = string.Empty;
                if (config.RegexResultEnabled)
                {
                    // 使用GetSelectedRegexPattern获取正确的正则表达式模式
                    string pattern = _view.GetSelectedRegexPattern();
                    
                    // 如果获取的模式为空，尝试从CurrentRegexPattern获取
                    if (string.IsNullOrEmpty(pattern) && !string.IsNullOrEmpty(_view.CurrentRegexPattern))
                    {
                        pattern = _view.CurrentRegexPattern;
                        LogHelper.Debug("手动模式下使用CurrentRegexPattern作为后备: '" + pattern + "'");
                    }
                    
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        LogHelper.Debug("处理正则表达式，当前模式: '" + pattern + "'");
                        regexPart = ProcessRegexForFileName(fileInfo.Name, pattern, config.RegexResultEnabled);
                        LogHelper.Debug("正则表达式处理结果: '" + regexPart + "'");
                        // 添加额外的调试信息
                        LogHelper.Debug("当前使用的重命名规则:");
                        LogHelper.Debug("  - 正则表达式模式: '" + pattern + "'");
                        LogHelper.Debug("  - 模式来源: GetSelectedRegexPattern (主正则cmbRegex)");
                        LogHelper.Debug("  - 正则结果: '" + regexPart + "'");
                        
                        // 检查是否正则匹配失败需要用户处理
                        if (regexPart != null && regexPart.StartsWith("REGEX_MATCH_FAILED:"))
                        {
                            string originalName = regexPart.Substring("REGEX_MATCH_FAILED:".Length);
                            DialogResult result = _view.ShowMessageWithResult(
                                "正则表达式匹配失败，是否使用完整原文件名替代正则部分？",
                                "匹配失败",
                                MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.OK)
                            {
                                regexPart = originalName;
                            }
                            else
                            {
                                // 用户取消，不执行重命名
                                return;
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Debug("警告: 正则表达式模式为空，无法进行正则处理");
                        // 使用原文件名作为后备
                        regexPart = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    }
                }

                // 正则处理完成后，立即更新MaterialSelectFormModern的正则结果
                if (!string.IsNullOrEmpty(regexPart))
                {
                    _view.UpdateMaterialSelectFormRegexResult(regexPart);
                }

                // 计算最终尺寸（不受DimensionsEnabled影响，始终计算尺寸用于显示和PDF处理）
                string finalDimensions = string.Empty;
                // 使用SettingsForm的CalculateFinalDimensions方法来正确处理形状参数
                using (var settingsForm = new SettingsForm())
                {
                    settingsForm.RecognizePdfDimensions(fileInfo.FullName);
                    // 使用修改后的CalculateFinalDimensions方法，传递cornerRadius和addPdfLayers参数
                    finalDimensions = settingsForm.CalculateFinalDimensions(settingsForm.PdfWidth, settingsForm.PdfHeight, tetBleed, cornerRadius, addPdfLayers);
                    
                    if (!string.IsNullOrEmpty(finalDimensions))
                    {
                        var dimensionsParts = finalDimensions.Split('x');
                        string displayDimensions = finalDimensions;
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
                                        displayDimensions = $"{maxDim}x{minDim}{shapePart}";
                                    }
                                }
                            }
                            else if (double.TryParse(dimensionsParts[0], out double dim1) &&
                                     double.TryParse(dimensionsParts[1], out double dim2))
                            {
                                // 确保大数在前
                                double maxDim = Math.Max(dim1, dim2);
                                double minDim = Math.Min(dim1, dim2);
                                displayDimensions = $"{maxDim}x{minDim}";
                            }
                        }
                        finalDimensions = displayDimensions;
                    }
                }

                // 创建文件名组件（在计算尺寸之后重新创建，确保包含正确的尺寸信息）
                var finalComponents = CreateFileNameComponents(
                    fileInfo, selectedMaterial, orderNumber, quantity, unit,
                    finalDimensions, // 使用计算出的最终尺寸
                    fixedField, serialNumber, regexPart,
                    AppSettings.Separator ?? "", config, compositeColumnValue);

                // ✅ 提取行列数信息（用于dgvFiles显示）
                string layoutRows = finalComponents?.LayoutRows ?? string.Empty;
                string layoutColumns = finalComponents?.LayoutColumns ?? string.Empty;
                LogHelper.Debug($"[行列数提取] 行数={layoutRows}, 列数={layoutColumns}");

                // 重新构建文件名（使用包含正确尺寸的组件）
                string newFileName = BuildNewFileName(finalComponents);
                if (string.IsNullOrEmpty(newFileName))
                {
                    _view.ShowMessage("无法构建有效的文件名", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 重新解决文件名冲突
                string destPath = Path.Combine(exportPath, newFileName);
                destPath = HandleFileNameConflict(destPath);
                newFileName = Path.GetFileName(destPath);

                // 准备PDF处理选项
#pragma warning disable CS0618 // 禁用过时API警告
                // 获取标识页设置并记录调试信息
                bool addIdentifierPage = _view.GetAddIdentifierPage();
                string identifierPageContent = _view.GetIdentifierPageContent();
                
                // 获取排版模式和布局数量
                var layoutMode = _view.GetLayoutMode();
                int layoutQuantity = _view.GetLayoutQuantity();
                
                // 获取页面旋转角度（从布局计算结果）
                int rotationAngle = _view.GetRotationAngle();
                
                LogHelper.Debug($"[标识页调试] 获取标识页设置: AddIdentifierPage={addIdentifierPage}");
                LogHelper.Debug($"[标识页调试] 标识页内容: '{identifierPageContent}'");
                LogHelper.Debug($"[标识页调试] 内容长度: {identifierPageContent?.Length ?? 0} 字符");
                LogHelper.Debug($"[排版模式调试] 排版模式: {layoutMode}, 布局数量: {layoutQuantity}, 旋转角度: {rotationAngle}°");
                
                var pdfOptions = new PdfProcessingOptions
                {
                    FinalDimensions = finalDimensions,
                    CornerRadius = cornerRadius,
                    UsePdfLastPage = usePdfLastPage,
                    AddPdfLayers = addPdfLayers,
                    TargetLayerNames = new[] { "Dots_AddCounter" },
                    AddIdentifierPage = addIdentifierPage,
                    IdentifierPageContent = identifierPageContent,
                    LayoutMode = layoutMode,
                    LayoutQuantity = layoutQuantity,
                    RotationAngle = rotationAngle  // 添加旋转角度
                };
                
                LogHelper.Debug($"[标识页调试] PdfProcessingOptions创建完成: AddIdentifierPage={pdfOptions.AddIdentifierPage}, Content='{pdfOptions.IdentifierPageContent}'");
#pragma warning restore CS0618 // 恢复警告

                // 记录从视图层传递的组合列值
                LogHelper.Debug("从视图层传递的组合列值: '" + compositeColumnValue + "'");

                // 创建FileRenameInfo对象
                var fileRenameInfo = new FileRenameInfo
                {
                    OriginalName = fileInfo.Name,
                    NewName = newFileName,
                    FullPath = fileInfo.FullName,
                    RegexResult = regexPart,
                    OrderNumber = orderNumber,
                    Material = selectedMaterial,
                    Quantity = quantity,
                    Dimensions = finalDimensions,
                    Process = fixedField,
                    SerialNumber = serialNumber,
                    Time = DateTime.Now.ToString("MM-dd"),
                    CompositeColumn = compositeColumnValue,
                    LayoutRows = layoutRows,        // ✅ 新增：设置行数
                    LayoutColumns = layoutColumns   // ✅ 新增：设置列数
                };

                // 执行文件重命名,根据addPdfLayers或addIdentifierPage选择合适的方法
                bool success;
                if (addPdfLayers || addIdentifierPage)
                {
                    LogHelper.Debug($"调用带PDF处理选项的重命名方法 (PDF图层: {addPdfLayers}, 标识页: {addIdentifierPage})");
                    // 调用新添加的重载方法,传递pdfOptions
                    success = fileRenameService.RenameFileImmediately(fileRenameInfo, exportPath, _view.IsCopyMode, pdfOptions);
                }
                else
                {
                    LogHelper.Debug("调用标准重命名方法");
                    // 使用原来的方法
                    success = fileRenameService.RenameFileImmediately(fileRenameInfo, exportPath, _view.IsCopyMode);
                }

                if (success)
                {
                    // 更新网格显示
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
                        CompositeColumn = compositeColumnValue,
                        LayoutRows = layoutRows,        // ✅ 新增：设置行数
                        LayoutColumns = layoutColumns   // ✅ 新增：设置列数
                    };
                    
                    LogHelper.Debug($"创建 ProcessedFileData 用于更新网格: CompositeColumn = '{compositeColumnValue}'");

                    // 修复：调用正确的服务方法更新网格数据
                    fileRenameService.AddFileToGrid(fileInfo, processedData, _view.FileBindingList);

                    // ✅ 关键步骤1：初始化保留数据
                    // 获取刚刚添加到grid中的FileRenameInfo对象
                    if (_view.FileBindingList != null && _view.FileBindingList.Count > 0)
                    {
                        var addedFileInfo = _view.FileBindingList.FirstOrDefault(f => f.OriginalName == fileRenameInfo.OriginalName);
                        if (addedFileInfo != null)
                        {
                            // 确保BackupData已初始化
                            if (addedFileInfo.BackupData == null)
                            {
                                addedFileInfo.BackupData = new Dictionary<string, string>();
                            }

                            // 获取批量处理服务来设置保留配置
                            var batchService = ServiceLocator.Instance.GetBatchProcessingService();
                            if (batchService != null)
                            {
                                LogHelper.Debug($"批量处理服务类型: {batchService.GetType().FullName}, 是否为BatchProcessingService: {batchService is BatchProcessingService}");
                                
                                // ✅ 步骤0：先从配置中获取保留分组信息并设置到服务
                                try
                                {
                                    var eventGroupConfig = SettingsForm.GetEventGroupConfiguration();
                                    if (eventGroupConfig?.Groups != null && eventGroupConfig.Groups.Any())
                                    {
                                        // 提取所有 IsPreserved = true 的分组，并从 Models.EventGroup 转换为 Forms.Dialogs.EventGroupConfig
                                        var preserveGroupConfigs = eventGroupConfig.Groups
                                            .Where(g => g != null && g.IsPreserved)
                                            .Select(g => new WindowsFormsApp3.Forms.Dialogs.EventGroupConfig
                                            {
                                                Group = ConvertEventGroupEnumFromModels(g.Id),
                                                DisplayName = g.DisplayName,
                                                Prefix = g.Prefix,
                                                IsEnabled = g.IsEnabled,
                                                SortOrder = g.SortOrder,
                                                IsPreserved = g.IsPreserved,
                                                Items = GetItemsForGroup(eventGroupConfig, g.Id)
                                            })
                                            .ToList();
                                        
                                        if (preserveGroupConfigs.Any())
                                        {
                                            var groupInfo = string.Join("; ", preserveGroupConfigs.Select(g => g.DisplayName));
                                            LogHelper.Debug($"从配置获取保留分组: {groupInfo}");
                                            batchService.SetPreserveGroupConfigs(preserveGroupConfigs);
                                            LogHelper.Debug("已设置保留分组配置到批量处理服务");
                                        }
                                        else
                                        {
                                            LogHelper.Debug("配置中没有启用任何保留分组");
                                        }
                                    }
                                    else
                                    {
                                        LogHelper.Debug("无法获取事件分组配置");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.Debug($"获取保留分组配置时出错: {ex.Message}");
                                }
                                
                                // ✅ 步骤1：从设置中获取保留分组配置并应用到当前文件
                                // 直接使用接口调用，不需要类型转换
                                batchService.ApplyPreserveModeToFileList(new List<FileRenameInfo> { addedFileInfo });
                                LogHelper.Debug("已应用保留模式配置到单文件");

                                // ✅ 步骤2：如果启用了保留模式，恢复保留数据到属性（用于显示在dgvFiles中）
                                // 这样dgvFiles会通过数据绑定自动刷新显示恢复后的保留数据
                                if (addedFileInfo.IsPreserveMode && addedFileInfo.BackupData != null && addedFileInfo.BackupData.Any())
                                {
                                    LogHelper.Debug($"[文件监控] 恢复前备份数据: {string.Join(", ", addedFileInfo.BackupData.Select(kv => kv.Key + "=" + kv.Value))}");
                                    LogHelper.Debug($"[文件监控] 恢复前属性: OrderNumber='{addedFileInfo.OrderNumber}', Material='{addedFileInfo.Material}', Process='{addedFileInfo.Process}', Dimensions='{addedFileInfo.Dimensions}'");
                                    
                                    batchService.RestorePreservedFields(new List<FileRenameInfo> { addedFileInfo });
                                    
                                    LogHelper.Debug($"[文件监控] 恢复后属性: OrderNumber='{addedFileInfo.OrderNumber}', Material='{addedFileInfo.Material}', Process='{addedFileInfo.Process}', Dimensions='{addedFileInfo.Dimensions}'");
                                    LogHelper.Debug("已恢复单文件保留字段数据到属性");
                                    
                                    // ⭐ 关键修复：强制刷新dgvFiles以显示恢复后的数据
                                    // PropertyChanged 事件虽然被触发，但 dgvFiles 可能没有及时刷新
                                    // 这个刷新确保用户能看到恢复后的保留字段值
                                    _view.RefreshDgvFiles();
                                    LogHelper.Debug("[文件监控] 已强制刷新dgvFiles以显示恢复后的保留数据");
                                }
                                else
                                {
                                    LogHelper.Debug($"保留模式检查: IsPreserveMode={addedFileInfo.IsPreserveMode}, BackupData count={(addedFileInfo.BackupData?.Count ?? 0)}");
                                }
                            }
                            else
                            {
                                LogHelper.Debug("错误: 批量处理服务为null");
                            }
                        }
                    }

                    // 更新状态栏，确保顺序规则正确显示
                    _view.UpdateStatusStrip();

                    // 添加配置名称的调试输出
                    LogHelper.Debug("=== 文件重命名成功 === [配置: " + _view.CurrentConfigName + "]");
                    LogHelper.Debug("文件名: " + fileInfo.Name + " -> " + newFileName);
                    LogHelper.Debug("使用的正则表达式: '" + _view.GetSelectedRegexPattern() + "'");

                    // 显示成功消息（根据用户偏好，手动模式下不显示成功弹框）
                    // _view.ShowMessage($"文件重命名成功: {fileInfo.Name} -> {newFileName}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // 显示失败消息
                    _view.ShowMessage($"文件重命名失败: {fileInfo.Name}", "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("重命名文件时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理重命名按钮点击事件
        /// </summary>
        public void HandleRenameClick()
        {
            try
            {
                // 获取导出路径
                string exportPath = _view.GetExportPath();
                if (string.IsNullOrEmpty(exportPath))
                {
                    // 用户取消选择
                    return;
                }

                // 获取文件绑定列表
                var bindingList = _view.FileBindingList;
                if (bindingList == null || bindingList.Count == 0)
                {
                    _view.ShowMessage("没有文件需要重命名", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 初始化计数器
                int successCount = 0;
                int failCount = 0;

                // 获取事件项配置
                var fileRenameService = ServiceLocator.Instance.GetFileRenameService();
                if (fileRenameService == null)
                {
                    _view.ShowMessage("无法获取文件重命名服务", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 处理每个文件
                foreach (var fileInfo in bindingList)
                {
                    try
                    {
                        // 检查是否为空文件信息
                        if (IsFileRenameInfoEmpty(fileInfo))
                        {
                            continue;
                        }

                        // 重新构建文件名
                        string newFileName = RebuildFileName(fileInfo);
                        if (string.IsNullOrEmpty(newFileName))
                        {
                            failCount++;
                            continue;
                        }

                        // 更新文件信息
                        fileInfo.NewName = newFileName;

                        // 处理文件名冲突
                        string destPath = Path.Combine(exportPath, fileInfo.NewName);
                        destPath = fileRenameService.HandleFileNameConflict(destPath, destPath);
                        fileInfo.NewName = Path.GetFileName(destPath);

                        // 执行文件操作
                        bool operationSuccess = fileRenameService.RenameFileImmediately(fileInfo, exportPath, _view.IsCopyMode);
                        if (operationSuccess)
                        {
                            // ✅ 恢复保留字段数据（确保dgvFiles显示正确）
                            if (fileInfo.IsPreserveMode && fileInfo.BackupData != null && fileInfo.BackupData.Any())
                            {
                                var batchService = ServiceLocator.Instance.GetBatchProcessingService();
                                if (batchService != null)
                                {
                                    batchService.RestorePreservedFields(new List<FileRenameInfo> { fileInfo });
                                    LogHelper.Debug($"已恢复单文件 {fileInfo.OriginalName} 的保留字段数据");
                                }
                            }
                            
                            successCount++;
                        }
                        else
                        {
                            failCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        // 更新状态栏错误信息
                        LogHelper.Debug($"处理文件 {fileInfo?.OriginalName} 时发生错误: {ex.Message}");
                    }
                }

                // 显示结果消息
                _view.ShowMessage($"重命名完成：成功 {successCount} 个, 失败 {failCount} 个", "重命名完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _view.ShowMessage($"重命名过程中发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 重新构建文件名
        /// </summary>
        private string RebuildFileName(FileRenameInfo fileInfo)
        {
            try
            {
                // 获取事件项配置
                string eventItemsStr = AppSettings.EventItems ?? string.Empty;
                var eventItems = eventItemsStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                var newNameParts = new List<string>();
                string unit = AppSettings.Unit ?? "";

                // 检查哪些项被勾选
                for (int i = 0; i < eventItems.Length; i += 2)
                {
                    if (i + 1 < eventItems.Length)
                    {
                        string itemText = eventItems[i];
                        bool isChecked = bool.Parse(eventItems[i + 1]);
                        if (isChecked)
                        {
                            switch (itemText)
                            {
                                case "正则结果":
                                    // 保留原正则结果
                                    if (!string.IsNullOrEmpty(fileInfo.RegexResult)) newNameParts.Add(fileInfo.RegexResult);
                                    break;
                                case "订单号":
                                    if (!string.IsNullOrEmpty(fileInfo.OrderNumber)) newNameParts.Add(fileInfo.OrderNumber);
                                    break;
                                case "材料":
                                    if (!string.IsNullOrEmpty(fileInfo.Material)) newNameParts.Add(fileInfo.Material);
                                    break;
                                case "数量":
                                    if (!string.IsNullOrEmpty(fileInfo.Quantity))
                                    {
                                        string quantityWithUnit = fileInfo.Quantity;
                                        if (!string.IsNullOrEmpty(unit))
                                            quantityWithUnit += unit;
                                        newNameParts.Add(quantityWithUnit);
                                    }
                                    break;
                                case "尺寸":
                                    if (!string.IsNullOrEmpty(fileInfo.Dimensions)) newNameParts.Add(fileInfo.Dimensions);
                                    break;
                                case "工艺":
                                    if (!string.IsNullOrEmpty(fileInfo.Process)) newNameParts.Add(fileInfo.Process);
                                    break;
                                case "序号":
                                    if (!string.IsNullOrEmpty(fileInfo.SerialNumber)) newNameParts.Add(fileInfo.SerialNumber);
                                    break;
                                case "列组合":
                                    if (!string.IsNullOrEmpty(fileInfo.CompositeColumn)) newNameParts.Add(fileInfo.CompositeColumn);
                                    break;
                            }
                        }
                    }
                }

                // 获取用户设置的间隔符号，支持空分隔符
                string separator = AppSettings.Separator ?? "";
                // 只在分隔符包含非法字符时才替换为默认值，允许空分隔符
                if (!string.IsNullOrEmpty(separator) && Path.GetInvalidFileNameChars().Contains(separator[0]))
                {
                    separator = "_";
                }

                // 构建新文件名
                string newFileName = string.Join(separator, newNameParts) + Path.GetExtension(fileInfo.FullPath);
                return newFileName;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"重新构建文件名时发生错误: {ex.Message}");
                return $"未命名{Path.GetExtension(fileInfo.FullPath)}";
            }
        }

        /// <summary>
        /// 检查文件重命名信息是否为空
        /// </summary>
        private bool IsFileRenameInfoEmpty(FileRenameInfo fileInfo)
        {
            return fileInfo == null ||
                   (string.IsNullOrEmpty(fileInfo.OriginalName) &&
                    string.IsNullOrEmpty(fileInfo.NewName) &&
                    string.IsNullOrEmpty(fileInfo.FullPath));
        }

        /// <summary>
        /// 处理选择输入目录按钮点击事件
        /// </summary>
        public void HandleSelectInputDirClick()
        {
            try
            {
                // 显示文件夹选择对话框
                string selectedPath = _view.ShowFolderBrowserDialog("请选择输入目录");

                // 如果用户选择了路径
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 通知视图更新输入目录显示
                    _view.UpdateInputDirDisplay(selectedPath);

                    // 保存设置
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("选择输入目录时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理管理正则表达式按钮点击事件
        /// </summary>
        public void HandleManageRegexClick()
        {
            try
            {
                // 显示设置表单，让用户管理正则表达式
                using (var settingsForm = new SettingsForm())
                {
                    // 导航到正则表达式管理页面
                    settingsForm.ShowRegexManagement();

                    // 显示设置表单
                    settingsForm.ShowDialog();

                    // 通知视图更新状态栏
                    _view.UpdateStatusStrip();
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("管理正则表达式时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理正则表达式测试按钮点击事件
        /// </summary>
        public void HandleRegexTestClick()
        {
            try
            {
                // 这个方法在Form1中主要是UI逻辑，暂时不需要处理
            }
            catch (Exception ex)
            {
                _view.ShowMessage("正则表达式测试时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理管理导出路径按钮点击事件
        /// </summary>
        public void HandleManageExportPathsClick()
        {
            try
            {
                // 获取当前导出路径设置
                var currentExportPaths = AppSettings.ExportPaths;
                string currentExportPathsStr = currentExportPaths != null ? string.Join("|", currentExportPaths.Cast<string>()) : "";

                // 显示导出路径管理对话框
                string newExportPathsStr = _view.ShowExportPathManagerDialog(currentExportPathsStr);

                // 如果用户点击了确定按钮
                if (newExportPathsStr != null)
                {
                    // 更新设置
                    var newExportPaths = new List<string>();
                    if (!string.IsNullOrEmpty(newExportPathsStr))
                    {
                        newExportPaths.AddRange(newExportPathsStr.Split('|'));
                    }
                    AppSettings.ExportPaths = newExportPaths;
                    AppSettings.Save();

                    // 通知视图更新UI状态
                    _view.UpdateStatusStrip();
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("管理导出路径时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理切换模式按钮点击事件
        /// </summary>
        public void HandleToggleModeClick()
        {
            try
            {
                // 切换复制模式状态
                _view.IsCopyMode = !_view.IsCopyMode;

                // 更新按钮文本
                string buttonText = _view.IsCopyMode ? "复制模式" : "剪切模式";
                _view.SetToggleModeButtonText(buttonText);

                // 通知视图更新UI状态
                _view.UpdateTrayMenuItems();
                _view.UpdateStatusStrip();
            }
            catch (Exception ex)
            {
                _view.ShowMessage("切换模式时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理监控按钮点击事件
        /// </summary>
        public void HandleMonitorClick()
        {
            try
            {
                // 检查当前是否正在监控
                if (!_view.IsMonitoring)
                {
                    // 获取输入目录路径
                    string inputDirPath = _view.GetInputDirPath();

                    // 检查目录是否有效
                    if (string.IsNullOrEmpty(inputDirPath) || !_view.DirectoryExists(inputDirPath))
                    {
                        _view.ShowMessage("请选择有效的监控目录");
                        return;
                    }

                    // 显示材料设置对话框
                    _view.ShowMaterialSettingsDialog();

                    // 更新状态栏显示
                    _view.UpdateStatusStrip();

                    // 设置文件系统监控器属性
                    _view.SetWatcherProperties(inputDirPath, "*.pdf", false, true);

                    // 更新监控状态
                    _view.IsMonitoring = true;

                    // 更新按钮文本
                    _view.SetMonitorButtonText("停止监控");
                }
                else
                {
                    // 停止监控
                    _view.SetWatcherProperties(string.Empty, "*.pdf", false, false);

                    // 更新监控状态
                    _view.IsMonitoring = false;

                    // 更新按钮文本
                    _view.SetMonitorButtonText("开始监控");
                }

                // 通知视图更新UI状态
                _view.UpdateStatusStrip();
                _view.UpdateTrayMenuItems();
            }
            catch (Exception ex)
            {
                _view.ShowMessage("处理监控按钮点击时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理导出Excel按钮点击事件
        /// </summary>
        public void HandleExportExcelClick()
        {
            try
            {
                // 获取文件绑定列表
                var data = _view.FileBindingList;

                // 检查是否有数据可导出
                if (data == null || data.Count == 0)
                {
                    _view.ShowMessage("没有可导出的数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 显示保存文件对话框
                string defaultFileName = $"数据导出_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string fileName = _view.ShowSaveFileDialog("Excel文件|*.xlsx|所有文件|*.*", "导出Excel文件", defaultFileName);

                if (!string.IsNullOrEmpty(fileName))
                {
                    // 设置EPPlus许可证上下文
                    ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                    using var package = new ExcelPackage(new FileInfo(fileName));

                    // 检查工作表是否已存在
                    var existingWorksheet = package.Workbook.Worksheets["数据导出"];
                    ExcelWorksheet worksheet;

                    // 如果工作表存在
                    if (existingWorksheet != null)
                    {
                        // 检查工作簿中工作表的数量
                        if (package.Workbook.Worksheets.Count > 1)
                        {
                            // 如果有多个工作表，则直接删除
                            package.Workbook.Worksheets.Delete(existingWorksheet);
                            worksheet = package.Workbook.Worksheets.Add("数据导出");
                        }
                        else
                        {
                            // 如果只有一个工作表，则清空内容而不是删除
                            worksheet = existingWorksheet;
                            // 清空所有内容但保留工作表
                            worksheet.Cells.Clear();
                        }
                    }
                    else
                    {
                        // 如果工作表不存在，则创建新工作表
                        worksheet = package.Workbook.Worksheets.Add("数据导出");
                    }

                    // 设置中文表头
                    worksheet.Cells["A1"].Value = "序号";
                    worksheet.Cells["B1"].Value = "原文件名";
                    worksheet.Cells["C1"].Value = "新文件名";
                    worksheet.Cells["D1"].Value = "正则结果";
                    worksheet.Cells["E1"].Value = "订单号";
                    worksheet.Cells["F1"].Value = "材料";
                    worksheet.Cells["G1"].Value = "数量";
                    worksheet.Cells["H1"].Value = "尺寸";
                    worksheet.Cells["I1"].Value = "工艺";
                    worksheet.Cells["J1"].Value = "日期";

                    // 从第二行开始加载数据，排除路径信息
                    var exportData = data.Select(item => new
                    {
                        item.SerialNumber,
                        item.OriginalName,
                        item.NewName,
                        item.RegexResult,
                        item.OrderNumber,
                        item.Material,
                        item.Quantity,
                        item.Dimensions,
                        item.Process,
                        item.Time
                    }).ToList();

                    worksheet.Cells["A2"].LoadFromCollection(exportData, false);

                    package.Save();
                    _view.ShowMessage("导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("导出Excel文件时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 处理显示材料选择对话框事件
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void HandleShowMaterialSelectionDialog(FileInfo fileInfo, string width, string height)
        {
            try
            {
                // 获取材料列表
                var materials = _view.GetMaterials();

                // 计算应用出血值后的尺寸
                string adjustedDimensions = string.Empty;
                double tetBleed = 0;

                // 获取出血值设置
                string bleedValues = AppSettings.Get("TetBleedValues")?.ToString() ?? "0";
                string[] bleedParts = bleedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (bleedParts.Length > 0 && double.TryParse(bleedParts[0].Trim(), out double bleedValue))
                {
                    tetBleed = bleedValue;
                }

                // 将width和height字符串转换为double
                if (!double.TryParse(width, out double widthValue))
                {
                    widthValue = 0;
                }

                if (!double.TryParse(height, out double heightValue))
                {
                    heightValue = 0;
                }

                // 使用SettingsForm的CalculateFinalDimensions方法来正确处理形状参数
                using (var settingsForm = new SettingsForm())
                {
                    // 获取形状参数（从视图中获取）
                    string cornerRadius = _view.GetCornerRadius();
                    bool addPdfLayers = _view.GetAddPdfLayers();

                    // 调用带形状参数的尺寸计算方法
                    adjustedDimensions = settingsForm.CalculateFinalDimensions(widthValue, heightValue, tetBleed, cornerRadius, addPdfLayers);
                }

                // 应用选中的正则表达式获取匹配结果
                string regexResult = string.Empty;
                string pattern = string.Empty;

                // 添加调试信息
                LogHelper.Debug($"=== Presenter中手动模式正则表达式处理开始 ===");
                LogHelper.Debug($"文件名: {fileInfo.Name}");
                LogHelper.Debug($"从_view获取的CurrentRegexPattern: '{_view.CurrentRegexPattern}'");
                LogHelper.Debug($"cmbRegex当前选中项: '{_view.GetCmbRegexSelectedItem()}'");

                // 获取当前正则表达式模式 - 使用新的方法确保获取正确的模式
                pattern = _view.GetSelectedRegexPattern();
                
                // 添加详细的调试信息
                LogHelper.Debug($"手动模式下从_view获取的正则表达式模式: '{pattern}'");
                LogHelper.Debug($"正则表达式模式是否为空: {string.IsNullOrEmpty(pattern)}");
                
                // 如果获取的模式为空，尝试从CurrentRegexPattern获取
                if (string.IsNullOrEmpty(pattern) && !string.IsNullOrEmpty(_view.CurrentRegexPattern))
                {
                    pattern = _view.CurrentRegexPattern;
                    LogHelper.Debug($"手动模式下使用CurrentRegexPattern作为后备: '{pattern}'");
                }
                
                // 显示当前使用的重命名规则
                LogHelper.Debug($"HandleShowMaterialSelectionDialog: 使用配置 '{_view.CurrentConfigName}' 的正则表达式 '{_view.GetCmbRegexSelectedItem()}' -> '{pattern}' 处理文件 '{fileInfo.Name}'");

                if (!string.IsNullOrEmpty(pattern))
                {
                    Match match = Regex.Match(fileInfo.Name, pattern);
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
                        LogHelper.Debug($"手动模式下匹配成功: '{regexResult}'");
                    }
                    else
                    {
                        // 调试信息：匹配失败
                        LogHelper.Debug($"手动模式下匹配失败: 文件名 '{fileInfo.Name}' 不匹配模式 '{pattern}'");
                    }
                }
                else
                {
                    // 调试信息：pattern为空
                    LogHelper.Debug("手动模式下错误: 正则表达式模式为空");
                }

                // 生成序号
                string serialNumber = string.Empty;
                var bindingList = _view.FileBindingList;
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

                // 获取Excel相关数据
                var excelImportedData = _view.ExcelImportedData;
                int searchColumnIndex = _view.ExcelSearchColumnIndex;
                int returnColumnIndex = _view.ExcelReturnColumnIndex;
                int newColumnIndex = _view.ExcelNewColumnIndex;

                // 显示材料选择对话框，传递完整路径以便获取真实的PDF旋转角度
                var dialogResult = _view.ShowMaterialSelectionDialog(
                    materials,
                    fileInfo.FullName,  // 传递完整路径而不是仅文件名
                    regexResult,
                    AppSettings.Opacity,
                    width,
                    height,
                    excelImportedData,
                    searchColumnIndex,
                    returnColumnIndex,
                    newColumnIndex,
                    serialNumber);

                if (dialogResult == DialogResult.OK)
                {
                    // 获取用户选择的结果
                    var selectedMaterial = _view.GetSelectedMaterial();
                    var orderNumber = _view.GetOrderNumber();
                    var exportPath = _view.GetSelectedExportPath();
                    var quantities = _view.GetQuantities();
                    var serialNumbers = _view.GetSerialNumbers();
                    var matchedRows = _view.GetMatchedRows();
                    var adjustedDims = _view.GetAdjustedDimensions();
                    var fixedField = _view.GetFixedField();
                    var selectedTetBleed = _view.GetSelectedTetBleed();
                    var cornerRadius = _view.GetCornerRadius();
                    var usePdfLastPage = _view.GetUsePdfLastPage();
                    var addPdfLayers = _view.GetAddPdfLayers();

                    // 收集匹配行并添加到高亮集合
                    if (matchedRows != null && matchedRows.Count > 0)
                    {
                        _view.AddMatchedRows(matchedRows);
                        _view.RefreshExcelDataDisplay(); // 刷新高亮显示
                    }

                    // 检查是否处于即时重命名模式
                    bool isImmediateRenameActive = _view.IsImmediateRenameActive;

                    if (isImmediateRenameActive)
                    {
                        // 使用正则表达式安全提取宽高维度
                        var input = adjustedDims ?? "";
                        var matches = System.Text.RegularExpressions.Regex.Matches(input, @"\d+\.?\d*");
                        var dimensionsParts = matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value).ToArray();
                        string parsedWidth = dimensionsParts.Length >= 1 ? dimensionsParts[0] : string.Empty;
                        string parsedHeight = dimensionsParts.Length >= 2 ? dimensionsParts[1] : string.Empty;

                        // 循环处理每个数量值
                        for (int i = 0; i < quantities.Count; i++)
                        {
                            var quantity = quantities[i];
                            string currentSerialNumber = string.Empty;
                            if (matchedRows != null && i < matchedRows.Count && newColumnIndex >= 0)
                            {
                                var excelRow = matchedRows[i];
                                if (newColumnIndex < excelRow.ItemArray.Length)
                                {
                                    if (int.TryParse(excelRow[newColumnIndex].ToString(), out int num))
                                        currentSerialNumber = num.ToString("D2");
                                    else
                                        currentSerialNumber = excelRow[newColumnIndex].ToString();
                                }
                            }
                            // 确保有足够的序号
                            string serialNum = i < serialNumbers.Count ? serialNumbers[i] : (i + 1).ToString("D2");

                            // 添加调试信息：记录即将调用的重命名操作
                            LogHelper.Debug("即将调用HandleRenameFileImmediately方法");
                            LogHelper.Debug("传递的参数 - 文件名: " + fileInfo.Name + ", 材料: " + selectedMaterial + ", 订单号: " + orderNumber + ", 数量: " + quantity);
                            LogHelper.Debug("传递的正则表达式模式: '" + _view.CurrentRegexPattern + "'");
                            
                            // 处理文件重命名
                            HandleRenameFileImmediately(
                                fileInfo,
                                selectedMaterial,
                                orderNumber,
                                quantity,
                                AppSettings.Unit ?? "",
                                exportPath,
                                selectedTetBleed,
                                parsedWidth,
                                parsedHeight,
                                fixedField,
                                serialNum,
                                cornerRadius,
                                usePdfLastPage,
                                addPdfLayers);
                        }
                    }
                    else
                    {
                        // 循环处理每个数量值，与匹配行一一对应
                        for (int i = 0; i < serialNumbers.Count; i++)
                        {
                            string currentSerialNumber = string.Empty;
                            var quantity = quantities[i];
                            if (matchedRows != null && i < matchedRows.Count && newColumnIndex >= 0)
                            {
                                var excelRow = matchedRows[i];
                                if (newColumnIndex < excelRow.ItemArray.Length)
                                {
                                    // 直接使用Excel中的序号值
                                    currentSerialNumber = excelRow[newColumnIndex]?.ToString() ?? string.Empty;
                                }
                            }
                            // 添加文件到网格
                            HandleAddFileToGrid(
                                fileInfo,
                                selectedMaterial,
                                orderNumber,
                                quantity,
                                AppSettings.Unit ?? "",
                                adjustedDims,
                                fixedField,
                                serialNumbers[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _view.ShowMessage("显示材料选择对话框时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 从 Models.EventGroup ID 转换为 Forms.Dialogs.EventGroup 枚举
        /// </summary>
        private WindowsFormsApp3.Forms.Dialogs.EventGroup ConvertEventGroupEnumFromModels(string groupId)
        {
            return groupId switch
            {
                "order" => WindowsFormsApp3.Forms.Dialogs.EventGroup.Order,
                "material" => WindowsFormsApp3.Forms.Dialogs.EventGroup.Material,
                "quantity" => WindowsFormsApp3.Forms.Dialogs.EventGroup.Quantity,
                "process" => WindowsFormsApp3.Forms.Dialogs.EventGroup.Process,
                "customer" => WindowsFormsApp3.Forms.Dialogs.EventGroup.Customer,
                "remark" => WindowsFormsApp3.Forms.Dialogs.EventGroup.Remark,
                "row" => WindowsFormsApp3.Forms.Dialogs.EventGroup.Row,
                "column" => WindowsFormsApp3.Forms.Dialogs.EventGroup.Column,
                _ => WindowsFormsApp3.Forms.Dialogs.EventGroup.Ungrouped
            };
        }

        /// <summary>
        /// 从 EventGroupConfiguration 中获取特定分组的所有项目
        /// </summary>
        private List<string> GetItemsForGroup(WindowsFormsApp3.Models.EventGroupConfiguration config, string groupId)
        {
            if (config?.Items == null)
                return new List<string>();

            return config.Items
                .Where(item => item.GroupId == groupId)
                .Select(item => item.Name)
                .ToList();
        }
    }
}