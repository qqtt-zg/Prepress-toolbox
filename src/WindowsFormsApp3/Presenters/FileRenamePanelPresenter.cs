using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;  // 添加此using以支持DialogResult
using WindowsFormsApp3.Forms.Panels;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Newtonsoft.Json;
using WindowsFormsApp3.Services.Events;

namespace WindowsFormsApp3.Presenters
{
    /// <summary>
    /// 文件重命名面板Presenter实现
    /// 负责处理FileRenamePanel的业务逻辑
    /// </summary>
    public class FileRenamePanelPresenter : IFileRenamePanelPresenter
    {
        private readonly IFileRenamePanelView _view;
        private readonly Interfaces.IFileRenameService _fileRenameService;
        private readonly Services.IFileMonitor _fileMonitor;
        private readonly IExcelImportService _excelImportService;
        private readonly IPdfDimensionService _pdfDimensionService;
        private readonly Services.IPdfProcessingService _pdfProcessingService;
        private readonly Interfaces.ILogger _logger;
        private readonly string _savedGridsPath;
        private readonly IImpositionService _impositionService;

        private System.Windows.Forms.Timer _autoSaveTimer;
        private bool _isShutdownInProgress;

        private readonly object _fileQueueLock = new object();
        private readonly Queue<string> _pendingFiles = new Queue<string>();
        private readonly HashSet<string> _enqueuedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DateTime> _recentEnqueueTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private bool _isFileQueueWorkerRunning;

        // 数据存储
        private Dictionary<string, string> _regexPatterns;
        private List<string> _materials;
        private string _exportPath;
        private bool _isCopyMode = true; // 默认复制模式
        
        // PDF处理相关字段(用于手动模式)
        private bool _currentIsShapeSelected = false;
        private ShapeType _currentShapeType = ShapeType.RightAngle;
        private double _currentRoundRadius = 0;
        private string _currentDimensions = "";
        private bool _currentNeedsRotation = false;
        private int _currentRotationAngle = 0;
        private bool _currentEnableImposition = false;
        private LayoutMode _currentLayoutMode = LayoutMode.Continuous;
        private int _currentLayoutQuantity = 0;
        // ✅ 添加标识页相关字段
        private bool _currentAddIdentifierPage = false;
        private string _currentIdentifierPageContent = "";
        private string _currentImpositionMaterialType = "";
        // ✅ 添加一式N联联数字段
        private int _currentCopyCount = 0;
        // ✅ 添加一式类型（联/份）
        private CopyType _currentCopyType = CopyType.Layout;
        // ✅ 添加份数字段
        private int _currentDuplicateCount = 2;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="view">视图实例</param>
        public FileRenamePanelPresenter(IFileRenamePanelView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));

            // 获取logger
            _logger = LogHelper.Logger;

            // 初始化服务
            try
            {
                _fileRenameService = ServiceLocator.Instance.GetFileRenameService();
                _fileMonitor = ServiceLocator.Instance.GetFileMonitor();
                _excelImportService = ServiceLocator.Instance.GetExcelImportService();
                _pdfDimensionService = PdfDimensionServiceFactory.GetInstance();
                _pdfProcessingService = new Services.PdfProcessingService();
                _impositionService = new ImpositionService();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "初始化服务失败");
            }

            // 初始化数据存储
            _regexPatterns = new Dictionary<string, string>();
            _materials = new List<string>();

            // 初始化路径
            _savedGridsPath = Path.Combine(AppDataPathManager.AppRootDirectory, "SavedGrids");
            if (!Directory.Exists(_savedGridsPath))
            {
                Directory.CreateDirectory(_savedGridsPath);
            }

            // 订阅文件监控事件
            SubscribeToFileMonitorEvents();

            // 订阅配置保存事件，以便立即应用新设置
            var eventBus = ServiceLocator.Instance.GetEventBus();
            eventBus.Subscribe<ConfigSavedEvent>(e => ReloadSettings());

            // 初始化并启动自动保存定时器
            InitializeAutoSaveTimer();

            // 订阅程序退出事件，用于正常关闭时保存
            Application.ApplicationExit += OnApplicationExit;
        }

        #region 初始化与生命周期

        private string GetRecoveryJsonFilePath()
        {
            return Path.Combine(_savedGridsPath, $"{DateTime.Now:yyyy-MM-dd}.recovery.json");
        }

        private void InitializeAutoSaveTimer()
        {
            try
            {
                if (_autoSaveTimer != null)
                {
                    _autoSaveTimer.Stop();
                    _autoSaveTimer.Dispose();
                    _autoSaveTimer = null;
                }

                // 当关闭“当日JSON自动创建/加载”功能时，同时禁用定时自动保存
                if (!AppSettings.EnableDailyJson)
                {
                    return;
                }

                var intervalSeconds = AppSettings.AutoSaveIntervalSeconds;
                if (intervalSeconds < 10)
                {
                    intervalSeconds = 10;
                }

                _autoSaveTimer = new System.Windows.Forms.Timer();
                _autoSaveTimer.Interval = intervalSeconds * 1000;
                _autoSaveTimer.Tick += (s, e) =>
                {
                    if (_isShutdownInProgress)
                    {
                        return;
                    }

                    try
                    {
                        // 再次确认开关，避免运行时关闭后仍写入恢复文件
                        if (!AppSettings.EnableDailyJson)
                        {
                            _autoSaveTimer?.Stop();
                            return;
                        }

                        var recoveryPath = GetRecoveryJsonFilePath();
                        SaveToJsonFile(recoveryPath);
                        _logger?.LogInformation($"自动保存完成: {Path.GetFileName(recoveryPath)}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "自动保存失败");
                    }
                };

                _autoSaveTimer.Start();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "初始化自动保存定时器失败");
            }
        }

        public void ReloadSettings()
        {
            try
            {
                // 重新初始化自动保存定时器以应用最新频率
                InitializeAutoSaveTimer();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "重新加载设置失败");
            }
        }

        private void TryRecoverFromAutoSave()
        {
            try
            {
                var recoveryPath = GetRecoveryJsonFilePath();
                if (!File.Exists(recoveryPath))
                {
                    return;
                }

                if (!_view.ShowConfirm("检测到上次异常退出的自动保存数据，是否恢复？", "异常恢复"))
                {
                    return;
                }

                var dataList = LoadFromJsonFile(recoveryPath);
                if (dataList != null)
                {
                    _view.FileList = new BindingList<FileRenameInfo>(dataList);
                    _view.RefreshFileTable();
                    _view.ShowSuccess("已恢复上次未保存的数据");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "异常恢复失败");
            }
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                _isShutdownInProgress = true;
                _autoSaveTimer?.Stop();

                // 正常退出时，优先保存到当前选中的配置；如果没有则保存到当日JSON
                var configName = _view.CurrentConfigName;
                string targetPath;
                if (!string.IsNullOrEmpty(configName))
                {
                    targetPath = Path.Combine(_savedGridsPath, configName + ".json");
                }
                else
                {
                    targetPath = GetTodayJsonFilePath();
                }

                SaveToJsonFile(targetPath);

                // 正常退出后删除恢复文件
                var recoveryPath = GetRecoveryJsonFilePath();
                if (File.Exists(recoveryPath))
                {
                    File.Delete(recoveryPath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "程序退出保存失败");
            }
        }

        /// <summary>
        /// 获取材料类型字符串
        /// </summary>
        private string GetImpositionModeString()
        {
            // 只要有排版材料类型，就返回对应的中文描述，不再强校验 _currentEnableImposition
            // 因为有时用户可能没有勾选"启用排版"（那个复选框可能只控制是否进行PDF拼版），
            // 但仍然希望在文件名中体现是"平张"还是"卷装"
            if (!string.IsNullOrEmpty(_currentImpositionMaterialType))
            {
                if (_currentImpositionMaterialType == "FlatSheet")
                {
                    return "平张";
                }
                else if (_currentImpositionMaterialType == "RollMaterial")
                {
                    return "卷装";
                }
            }
            
            // 兼容旧逻辑
            if (_currentEnableImposition)
            {
                if (_currentImpositionMaterialType == "FlatSheet")
                {
                    return "平张";
                }
                else if (_currentImpositionMaterialType == "RollMaterial")
                {
                    return "卷装";
                }
            }
            return "";
        }

        /// <summary>
        /// 从缓存读取并更新两种排版模式的布局数量
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        private void UpdateBothLayoutCountsAsync(FileRenameInfo fileInfo)
        {
            // 使用INFO级别日志确保能看见
            _logger?.LogInformation($"[UpdateBothLayoutCountsAsync] 从缓存读取布局数 - AlwaysOutputBothLayoutCounts={AppSettings.AlwaysOutputBothLayoutCounts}");

            try
            {
                // 从 LayoutResultsCache 读取布局结果
                int flatSheetCount = LayoutResultsCache.FlatSheetLayoutCount;
                int flatSheetRows = LayoutResultsCache.FlatSheetRows;
                int flatSheetCols = LayoutResultsCache.FlatSheetColumns;

                int rollCount = LayoutResultsCache.RollMaterialLayoutCount;
                int rollRows = LayoutResultsCache.RollMaterialRows;
                int rollCols = LayoutResultsCache.RollMaterialColumns;

                _logger?.LogInformation($"[UpdateBothLayoutCountsAsync] 缓存读取: FlatSheet={flatSheetCount}({flatSheetRows}x{flatSheetCols}), Roll={rollCount}({rollRows}x{rollCols})");

                // 根据设置和用户选择的模式决定如何显示
                if (AppSettings.AlwaysOutputBothLayoutCounts)
                {
                    // 启用同时输出：两列都显示计算值
                    fileInfo.FlatSheetLayoutCount = flatSheetCount > 0 ? flatSheetCount.ToString() : "";
                    fileInfo.FlatSheetLayoutRows = flatSheetRows > 0 ? flatSheetRows.ToString() : "";
                    fileInfo.FlatSheetLayoutColumns = flatSheetCols > 0 ? flatSheetCols.ToString() : "";
                    fileInfo.RollMaterialLayoutCount = rollCount > 0 ? rollCount.ToString() : "";
                    fileInfo.RollMaterialLayoutRows = rollRows > 0 ? rollRows.ToString() : "";
                    fileInfo.RollMaterialLayoutColumns = rollCols > 0 ? rollCols.ToString() : "";

                    // 根据用户选择的模式设置 LayoutRows/LayoutColumns 用于重命名输出
                    if (_currentImpositionMaterialType == "FlatSheet")
                    {
                        fileInfo.LayoutRows = flatSheetRows > 0 ? flatSheetRows.ToString() : "";
                        fileInfo.LayoutColumns = flatSheetCols > 0 ? flatSheetCols.ToString() : "";
                    }
                    else if (_currentImpositionMaterialType == "RollMaterial")
                    {
                        fileInfo.LayoutRows = rollRows > 0 ? rollRows.ToString() : "";
                        fileInfo.LayoutColumns = rollCols > 0 ? rollCols.ToString() : "";
                    }
                    else
                    {
                        fileInfo.LayoutRows = "";
                        fileInfo.LayoutColumns = "";
                    }
                }
                else
                {
                    // 未启用同时输出：只在对应列显示布局数
                    if (_currentImpositionMaterialType == "FlatSheet")
                    {
                        fileInfo.FlatSheetLayoutCount = flatSheetCount > 0 ? flatSheetCount.ToString() : "";
                        fileInfo.FlatSheetLayoutRows = flatSheetRows > 0 ? flatSheetRows.ToString() : "";
                        fileInfo.FlatSheetLayoutColumns = flatSheetCols > 0 ? flatSheetCols.ToString() : "";
                        fileInfo.RollMaterialLayoutCount = ""; // 卷装列清空
                        fileInfo.RollMaterialLayoutRows = "";
                        fileInfo.RollMaterialLayoutColumns = "";
                        fileInfo.LayoutRows = flatSheetRows > 0 ? flatSheetRows.ToString() : "";
                        fileInfo.LayoutColumns = flatSheetCols > 0 ? flatSheetCols.ToString() : "";
                    }
                    else if (_currentImpositionMaterialType == "RollMaterial")
                    {
                        fileInfo.FlatSheetLayoutCount = ""; // 平张列清空
                        fileInfo.FlatSheetLayoutRows = "";
                        fileInfo.FlatSheetLayoutColumns = "";
                        fileInfo.RollMaterialLayoutCount = rollCount > 0 ? rollCount.ToString() : "";
                        fileInfo.RollMaterialLayoutRows = rollRows > 0 ? rollRows.ToString() : "";
                        fileInfo.RollMaterialLayoutColumns = rollCols > 0 ? rollCols.ToString() : "";
                        fileInfo.LayoutRows = rollRows > 0 ? rollRows.ToString() : "";
                        fileInfo.LayoutColumns = rollCols > 0 ? rollCols.ToString() : "";
                    }
                    else
                    {
                        fileInfo.FlatSheetLayoutCount = "";
                        fileInfo.FlatSheetLayoutRows = "";
                        fileInfo.FlatSheetLayoutColumns = "";
                        fileInfo.RollMaterialLayoutCount = "";
                        fileInfo.RollMaterialLayoutRows = "";
                        fileInfo.RollMaterialLayoutColumns = "";
                        fileInfo.LayoutRows = "";
                        fileInfo.LayoutColumns = "";
                    }
                }

                // 记录添加行时的是否启用高亮设置，之后不受设置变化影响
                fileInfo.HighlightApplied = AppSettings.AlwaysOutputBothLayoutCounts;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[UpdateBothLayoutCountsAsync] 更新布局数失败: {fileInfo.OriginalName}");
                fileInfo.FlatSheetLayoutCount = "";
                fileInfo.RollMaterialLayoutCount = "";
            }
        }

        /// <summary>
        /// 初始化Presenter
        /// </summary>
        public void Initialize()
        {
            LoadSettings();
            LoadRegexPatterns();
            LoadMaterials();

            // 启动时先尝试异常恢复（优先级最高）
            TryRecoverFromAutoSave();

            // 更新视图
            _view.UpdateRegexComboBox(_regexPatterns.Keys.ToList());
            _view.UpdateMaterialsContextMenu(_materials);
            UpdateJsonFilesList();

            // 异常恢复提示完成后，再执行当日配置自动加载/创建（可通过设置关闭）
            if (AppSettings.EnableDailyJson)
            {
                EnsureDailyJsonLoaded();
            }
        }

        /// <summary>
        /// 加载设置和配置
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                // 加载导出路径
                _exportPath = AppSettings.Instance.LastOutputDir;
                if (string.IsNullOrEmpty(_exportPath))
                {
                    _exportPath = Path.Combine(AppDataPathManager.AppRootDirectory, "Output");
                    if (!Directory.Exists(_exportPath))
                    {
                        Directory.CreateDirectory(_exportPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载设置失败");
                _view.ShowError($"加载设置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存设置和配置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                AppSettings.Instance.LastOutputDir = _exportPath;
                AppSettings.Save();

                SaveRegexSettings();
                SaveMaterialSettings();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存设置失败");
                _view.ShowError($"保存设置失败: {ex.Message}");
            }
        }

        #endregion

        #region 目录选择

        /// <summary>
        /// 处理选择输入目录
        /// 参考Form1Presenter.HandleSelectInputDirClick()的正确实现：
        /// 选择目录时只设置监控路径，不扫描文件。
        /// 文件只在监控启动时自动添加。
        /// </summary>
        public void HandleSelectInputDir()
        {
            try
            {
                var selectedPath = _view.ShowFolderBrowser("选择输入文件夹", _view.InputDirectory);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    // 更新输入目录
                    _view.InputDirectory = selectedPath;
                    AppSettings.Instance.LastInputDir = selectedPath;

                    // 添加到历史记录
                    AppSettings.Instance.AddInputDirToHistory(selectedPath);

                    _view.UpdateStatus($"已选择目录: {selectedPath}");

                    // 保存设置
                    SaveSettings();

                    // 注意：选择目录时不扫描文件，文件只在监控启动时自动添加
                    // 这是与Form1保持一致的行为
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "选择目录失败");
                _view.ShowError($"选择目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置输入目录（从下拉框选择时调用）
        /// </summary>
        public void SetInputDirectory(string dirPath)
        {
            if (!string.IsNullOrEmpty(dirPath))
            {
                _view.InputDirectory = dirPath;
                AppSettings.Instance.LastInputDir = dirPath;
                SaveSettings();
            }
        }

        /// <summary>
        /// 扫描目录中的文件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        private void ScanDirectoryForFiles(string directoryPath)
        {
            try
            {
                _view.ShowProgress("正在扫描文件...");

                var fileInfoList = new List<FileRenameInfo>();
                var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    if (ShouldProcessFile(file))
                    {
                        var fileInfo = CreateFileRenameInfo(file);
                        fileInfoList.Add(fileInfo);
                    }
                }

                _view.FileList = new BindingList<FileRenameInfo>(fileInfoList);
                _view.RefreshFileTable();
                _view.UpdateStatus($"扫描完成，找到 {fileInfoList.Count} 个文件");
                _view.HideProgress();
            }
            catch (Exception ex)
            {
                _view.HideProgress();
                _logger?.LogError(ex, "扫描目录失败");
                _view.ShowError($"扫描目录失败: {ex.Message}");
            }
        }

        #endregion

        #region 文件监控

        /// <summary>
        /// 订阅文件监控事件
        /// </summary>
        private void SubscribeToFileMonitorEvents()
        {
            if (_fileMonitor != null)
            {
                _fileMonitor.FileCreated += OnFileCreated;
                _fileMonitor.FileRenamed += OnFileRenamed;
                _fileMonitor.MonitorError += OnMonitorError;
            }
        }

        /// <summary>
        /// 处理切换文件监控状态
        /// </summary>
        public void HandleToggleMonitoring()
        {
            try
            {
                if (_fileMonitor.IsMonitoring)
                {
                    StopMonitoring();
                }
                else
                {
                    StartMonitoring();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "切换监控状态失败");
                _view.ShowError($"切换监控状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 启动文件监控
        /// 参考Form1Presenter.HandleMonitorClick()的实现：
        /// 启动监控前重新加载材料设置，确保材料列表是最新的。
        /// </summary>
        public void StartMonitoring()
        {
            try
            {
                if (string.IsNullOrEmpty(_view.InputDirectory) || !Directory.Exists(_view.InputDirectory))
                {
                    _view.ShowError("请先选择有效的输入目录");
                    return;
                }

                // 重新加载材料设置（与Form1的ShowMaterialSettingsDialog行为一致）
                LoadMaterials();
                _view.UpdateMaterialsContextMenu(_materials);

                _fileMonitor.StartMonitoring(_view.InputDirectory);
                _view.IsMonitoring = true;
                _view.UpdateMonitorButtonState(true);
                _view.UpdateStatus($"正在监控: {_view.InputDirectory}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "启动监控失败");
                _view.ShowError($"启动监控失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 停止文件监控
        /// </summary>
        public void StopMonitoring()
        {
            try
            {
                _fileMonitor.StopMonitoring();
                _view.IsMonitoring = false;
                _view.UpdateMonitorButtonState(false);
                _view.UpdateStatus("监控已停止");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "停止监控失败");
                _view.ShowError($"停止监控失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 文件创建事件处理
        /// </summary>
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            EnqueueMonitoredFile(e.FullPath);
        }

        /// <summary>
        /// 文件重命名事件处理
        /// </summary>
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            EnqueueMonitoredFile(e.FullPath);
        }

        private void EnqueueMonitoredFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            try
            {
                var now = DateTime.UtcNow;

                lock (_fileQueueLock)
                {
                    if (_recentEnqueueTimes.TryGetValue(filePath, out var lastTime))
                    {
                        if ((now - lastTime).TotalSeconds < 2)
                        {
                            return;
                        }
                    }

                    _recentEnqueueTimes[filePath] = now;

                    if (_enqueuedFiles.Contains(filePath))
                    {
                        return;
                    }

                    _pendingFiles.Enqueue(filePath);
                    _enqueuedFiles.Add(filePath);

                    if (!_isFileQueueWorkerRunning)
                    {
                        _isFileQueueWorkerRunning = true;
                        _ = Task.Run(ProcessFileQueueWorkerAsync);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[监控入队] 失败: {filePath}");
            }
        }

        private async Task ProcessFileQueueWorkerAsync()
        {
            while (true)
            {
                string filePath;

                lock (_fileQueueLock)
                {
                    if (_pendingFiles.Count == 0)
                    {
                        _isFileQueueWorkerRunning = false;
                        return;
                    }

                    filePath = _pendingFiles.Dequeue();
                }

                try
                {
                    await WaitForFileReadyAsync(filePath);
                    await ProcessNewFileAsync(filePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"[监控处理] 处理失败: {filePath}");
                }
                finally
                {
                    lock (_fileQueueLock)
                    {
                        _enqueuedFiles.Remove(filePath);
                    }
                }
            }
        }

        private async Task WaitForFileReadyAsync(string filePath)
        {
            var timeout = GetFileReadyTimeout(filePath);
            var deadline = DateTime.UtcNow + timeout;

            // Adobe 另存大文件时会出现“短暂停顿/结构未完整但可读”的阶段
            // 仅靠大小/mtime 稳定仍可能误判，因此这里增加 iText 可解析性探针
            const int pollDelayMs = 500;
            const int stabilityThresholdMs = 2000; // 文件大小/mtime 稳定窗口
            const int minWaitMs = 1200; // 最小等待，避免刚创建就立刻放行
            const int maxProbeFailuresBeforeLog = 3;

            var startTime = DateTime.UtcNow;

            long lastSize = -1;
            DateTime lastWriteTime = DateTime.MinValue;
            DateTime stableSince = DateTime.MinValue;
            int probeFailures = 0;

            _logger?.LogInformation($"[文件监控] 开始等待文件就绪: {Path.GetFileName(filePath)}, 超时: {timeout.TotalSeconds}s");

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        stableSince = DateTime.MinValue;
                        await Task.Delay(pollDelayMs);
                        continue;
                    }

                    // 先做最基础的可访问性检查（不要求独占）
                    try
                    {
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            if (!stream.CanRead)
                            {
                                throw new IOException("文件流不可读");
                            }
                        }
                    }
                    catch
                    {
                        stableSince = DateTime.MinValue;
                        await Task.Delay(pollDelayMs);
                        continue;
                    }

                    var fi = new FileInfo(filePath);
                    var currentSize = fi.Length;
                    var currentWriteTime = fi.LastWriteTimeUtc;

                    // 跳过极端情况：大小为0的临时文件阶段
                    if (currentSize <= 0)
                    {
                        stableSince = DateTime.MinValue;
                        await Task.Delay(pollDelayMs);
                        continue;
                    }

                    // 如果大小/mtime 有变化，重置稳定计时器
                    if (currentSize != lastSize || currentWriteTime != lastWriteTime)
                    {
                        lastSize = currentSize;
                        lastWriteTime = currentWriteTime;
                        stableSince = DateTime.MinValue;
                        await Task.Delay(pollDelayMs);
                        continue;
                    }

                    // 第一次进入稳定状态时记录时间
                    if (stableSince == DateTime.MinValue)
                    {
                        stableSince = DateTime.UtcNow;
                    }

                    // 最小等待：避免刚创建就误放行
                    if ((DateTime.UtcNow - startTime).TotalMilliseconds < minWaitMs)
                    {
                        await Task.Delay(pollDelayMs);
                        continue;
                    }

                    // 稳定窗口不够，继续等
                    if ((DateTime.UtcNow - stableSince).TotalMilliseconds < stabilityThresholdMs)
                    {
                        await Task.Delay(pollDelayMs);
                        continue;
                    }

                    // ✅ 关键：iText 可解析性探针
                    // 只有当 iText 能读到“页数>0 且第一页尺寸>0”才认为 PDF 结构完成
                    if (_pdfDimensionService != null &&
                        (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                    {
                        var pageCount = _pdfDimensionService.GetPageCount(filePath);
                        if (!pageCount.HasValue || pageCount.Value <= 0)
                        {
                            probeFailures++;
                            stableSince = DateTime.MinValue;

                            if (probeFailures == maxProbeFailuresBeforeLog)
                            {
                                _logger?.LogInformation($"[文件监控] iText探针：页数不可用，继续等待: {Path.GetFileName(filePath)}");
                            }

                            await Task.Delay(pollDelayMs);
                            continue;
                    }

                        if (!_pdfDimensionService.GetFirstPageSize(filePath, out double w, out double h) || w <= 0 || h <= 0)
                    {
                            probeFailures++;
                            stableSince = DateTime.MinValue;

                            if (probeFailures == maxProbeFailuresBeforeLog)
                            {
                                _logger?.LogInformation($"[文件监控] iText探针：尺寸不可用，继续等待: {Path.GetFileName(filePath)}");
                    }

                            await Task.Delay(pollDelayMs);
                            continue;
                        }
                    }

                    _logger?.LogInformation($"[文件监控] 文件已稳定且通过iText探针，确认就绪: {Path.GetFileName(filePath)}");
                    return;
                }
                catch (Exception ex)
                {
                    // 任何异常都重置稳定计时器，继续等待
                    stableSince = DateTime.MinValue;
                    _logger?.LogDebug($"[文件监控] 等待文件就绪异常，继续等待: {Path.GetFileName(filePath)} ({ex.Message})");
                    await Task.Delay(pollDelayMs);
                }
            }

            throw new TimeoutException($"等待文件写入完成超时: {filePath}, Timeout={timeout.TotalSeconds}s");
        }

        private TimeSpan GetFileReadyTimeout(string filePath)
        {
            var fallback = TimeSpan.FromSeconds(90);

            try
            {
                if (!AppSettings.EnableDynamicFileReadyTimeout)
                {
                    return fallback;
                }

                if (!File.Exists(filePath))
                {
                    return fallback;
                }

                var fi = new FileInfo(filePath);
                var sizeMb = fi.Length / 1024d / 1024d;

                if (sizeMb <= AppSettings.FileReadyTimeoutSmallThresholdMb)
                {
                    return TimeSpan.FromSeconds(AppSettings.FileReadyTimeoutSmallSeconds);
                }

                if (sizeMb >= AppSettings.FileReadyTimeoutLargeThresholdMb)
                {
                    return TimeSpan.FromSeconds(AppSettings.FileReadyTimeoutLargeSeconds);
                }

                return TimeSpan.FromSeconds(AppSettings.FileReadyTimeoutMediumSeconds);
            }
            catch
            {
                return fallback;
            }
        }

        /// <summary>
        /// 监控错误事件处理
        /// </summary>
        private void OnMonitorError(object sender, ErrorEventArgs e)
        {
            _view.ShowError($"监控错误: {e.GetException().Message}");
            _logger?.LogError(e.GetException(), "文件监控错误");
        }

        /// <summary>
        /// 处理新文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private async Task ProcessNewFileAsync(string filePath)
        {
            try
            {
                _logger?.LogInformation($"[ProcessNewFileAsync] 开始处理文件: {filePath}");
                
                if (!ShouldProcessFile(filePath))
                {
                    _logger?.LogInformation($"[ProcessNewFileAsync] 文件被过滤，跳过: {filePath}");
                    return;
                }

                // 创建文件信息（已包含保留分组数据提取逻辑）
                var fileInfo = CreateFileRenameInfo(filePath);
                
                _logger?.LogInformation($"[ProcessNewFileAsync] IsImmediateMode={_view.IsImmediateMode}, FileName={fileInfo.OriginalName}, RegexResult={fileInfo.RegexResult}");

                if (_view.IsImmediateMode)  // 手动模式
                {
                    _logger?.LogInformation($"[ProcessNewFileAsync] 手动模式 - 准备显示材料选择对话框");
                    
                    // 记录传递的参数
                    _logger?.LogInformation($"[参数验证] 材料列表数量: {_materials?.Count ?? 0}");
                    if (_materials != null && _materials.Count > 0)
                    {
                        _logger?.LogInformation($"[参数验证] 材料列表: {string.Join(", ", _materials.Take(5))}...");
                    }
                    _logger?.LogInformation($"[参数验证] 文件名: {fileInfo.OriginalName}");
                    _logger?.LogInformation($"[参数验证] 正则结果: {fileInfo.RegexResult ?? "(空)"}");
                    _logger?.LogInformation($"[参数验证] PDF尺寸: {fileInfo.Width ?? "(空)"} x {fileInfo.Height ?? "(空)"}");
                    _logger?.LogInformation($"[参数验证] Excel数据: {(_view.ExcelData != null ? $"{_view.ExcelData.Rows.Count}行" : "无")}");
                    
                    // ✅ 修复跨线程访问问题：在UI线程上显示对话框
                    MaterialSelectionResult selectionResult = null;
                    DialogResult dialogResult = DialogResult.Cancel;
                    
                    try
                    {
                        // 检查_view是否是Control类型
                        if (_view is System.Windows.Forms.Control viewControl)
                        {
                            _logger?.LogInformation($"[跨线程修复] 使用Invoke在UI线程上显示对话框");
                            
                            // 在UI线程上调用对话框
                            viewControl.Invoke((Action)(() =>
                            {
                                // ✅ 修复：使用匹配正则结果（cmbRegex2）传递给对话框，用于Excel数据匹配
                                string matchingRegexResult = GetRegexResultForMatching(fileInfo);
                                dialogResult = _view.ShowMaterialSelectionDialog(
                                    materials: _materials,
                                    fileName: fileInfo.FullPath,  // ✅ 修复：传递完整路径用于PDF预览
                                    regexResult: matchingRegexResult ?? "",
                                    width: fileInfo.Width ?? "",
                                    height: fileInfo.Height ?? "",
                                    tetBleed: fileInfo.TetBleed ?? "",
                                    isColumnCombineMode: AppSettings.EnableColumnCombine,
                                    columnNames: GetExcelColumnNames(),
                                    columnItemsMap: GetExcelColumnItemsMap(),
                                    initialSerialNumber: GetNextSerialNumber(),
                                    enableSerialSearchResultToRegex: _excelImportService.EnableSerialSearchResultToRegex,
                                    serialSearchResultColumnIndex: _excelImportService.SerialSearchResultColumnIndex,
                                    out selectionResult
                                );
                            }));
                        }
                        else
                        {
                            _logger?.LogError($"[跨线程修复] 无法转换_view为Control类型，直接调用");
                            // 如果无法转换，直接调用（可能失败）
                            // ✅ 修复：使用匹配正则结果(cmbRegex2)传递给对话框，用于Excel数据匹配
                            string matchingRegexResult = GetRegexResultForMatching(fileInfo);
                            dialogResult = _view.ShowMaterialSelectionDialog(
                                materials: _materials,
                                fileName: fileInfo.FullPath,  // ✅ 修复：传递完整路径
                                regexResult: matchingRegexResult ?? "",
                                width: fileInfo.Width ?? "",
                                height: fileInfo.Height ?? "",
                                tetBleed: fileInfo.TetBleed ?? "",
                                isColumnCombineMode: AppSettings.EnableColumnCombine,
                                columnNames: GetExcelColumnNames(),
                                columnItemsMap: GetExcelColumnItemsMap(),
                                initialSerialNumber: GetNextSerialNumber(),
                                enableSerialSearchResultToRegex: _excelImportService.EnableSerialSearchResultToRegex,
                                serialSearchResultColumnIndex: _excelImportService.SerialSearchResultColumnIndex,
                                out selectionResult
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "[跨线程修复] 显示对话框失败");
                        _view.ShowError($"显示材料选择对话框失败: {ex.Message}");
                        return;
                    }

                    _logger?.LogInformation($"[ProcessNewFileAsync] 材料选择对话框返回: {dialogResult}");

                    if (dialogResult == System.Windows.Forms.DialogResult.OK && selectionResult != null)
                    {
                        _logger?.LogInformation($"[ProcessNewFileAsync] 用户选择材料: {selectionResult.SelectedMaterial}");
                        
                        // ✅ 新增：解析逗号分隔的多个数量和序号值
                        var quantities = (selectionResult.SelectedQuantity ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(q => q.Trim()).ToArray();
                        var serialNumbers = (selectionResult.SelectedSerialNumber ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim()).ToArray();
                        
                        // 如果没有有效值，使用默认值
                        if (quantities.Length == 0) quantities = new[] { "" };
                        if (serialNumbers.Length == 0) serialNumbers = new[] { GetNextSerialNumber().ToString() };

                        // ✅ 序号搜索反向更新正则结果
                        if (!string.IsNullOrEmpty(selectionResult.UpdatedRegexResult))
                        {
                            fileInfo.RegexResult = selectionResult.UpdatedRegexResult;
                            _logger?.LogInformation($"[ProcessNewFileAsync] 序号搜索更新正则结果: {selectionResult.UpdatedRegexResult}");
                        }

                        // 确定输出文件数量（取最大值）
                        int fileCount = Math.Max(quantities.Length, serialNumbers.Length);
                        _logger?.LogInformation($"[ProcessNewFileAsync] 多值输出: 数量数={quantities.Length}, 序号数={serialNumbers.Length}, 总文件数={fileCount}");
                        
                        // ✅ 保存形状处理信息（所有文件共用）
                        _currentIsShapeSelected = selectionResult.IsShapeSelected;
                        _currentShapeType = (ShapeType)selectionResult.SelectedShape;
                        _currentRoundRadius = selectionResult.RoundRadius;
                        _currentDimensions = selectionResult.Dimensions ?? "";
                        _currentNeedsRotation = selectionResult.NeedsRotation;
                        _currentRotationAngle = selectionResult.RotationAngle;
                        
                        // ✅ 保存排版信息(用于折手模式空白页功能)
                        _currentEnableImposition = selectionResult.EnableImposition;
                        _currentLayoutMode = selectionResult.LayoutMode;
                        _currentLayoutQuantity = selectionResult.LayoutQuantity;
                        // ✅ 保存一式N联联数
                        _currentCopyCount = selectionResult.CopyCount;
                        // ✅ 保存一式类型（联/份）
                        _currentCopyType = selectionResult.CopyType;
                        // ✅ 保存份数
                        _currentDuplicateCount = selectionResult.DuplicateCount;
                        // ✅ 保存排版材料类型（平张/卷装）
                        _currentImpositionMaterialType = selectionResult.ImpositionMaterialType;

                        _logger?.LogInformation($"[ProcessNewFileAsync] 排版信息: EnableImposition={_currentEnableImposition}, LayoutMode={_currentLayoutMode}, LayoutQuantity={_currentLayoutQuantity}, MaterialType={_currentImpositionMaterialType}");
                        
                        // ✅ 保存标识页信息
                        _currentAddIdentifierPage = selectionResult.AddIdentifierPage;
                        _currentIdentifierPageContent = selectionResult.IdentifierPageContent ?? "";
                        _currentImpositionMaterialType = selectionResult.ImpositionMaterialType ?? "";
                        _logger?.LogInformation($"[ProcessNewFileAsync] 标识页信息: AddIdentifierPage={_currentAddIdentifierPage}, Content='{_currentIdentifierPageContent}', MaterialType={_currentImpositionMaterialType}");
                        
                        // ✅ 获取导出路径
                        string exportPath = selectionResult.ExportPath;
                        if (string.IsNullOrEmpty(exportPath))
                        {
                            exportPath = AppDataPathManager.ExcelExportDirectory;
                        }
                        _exportPath = exportPath;
                        
                        int successCount = 0;
                        
                        // ✅ 预先匹配Excel数据（移出循环以支持多对多匹配）
                        List<ExcelMatchData> matchDataList = null;
                        if (_view.ExcelData != null)
                        {
                            // 使用第一个文件的信息进行匹配（假设所有分裂文件基于同一个原始正则结果）
                            string regexForMatching = GetRegexResultForMatching(fileInfo);
                            matchDataList = MatchExcelData(regexForMatching);
                        }

                        // ✅ 序号搜索更新正则后，重新匹配Excel数据
                        if (_view.ExcelData != null && !string.IsNullOrEmpty(selectionResult?.UpdatedRegexResult))
                        {
                            matchDataList = MatchExcelData(selectionResult.UpdatedRegexResult);
                            _logger?.LogInformation($"[ProcessNewFileAsync] 序号搜索更新后重新匹配Excel数据，匹配到 {matchDataList?.Count ?? 0} 条记录");
                        }

                        // ✅ 序号搜索更新后，如果序号搜索返回了列组合值，则优先使用（来自序号搜索匹配到的那一行）
                        bool useDirectCompositeColumn = _view.ExcelData != null &&
                            !string.IsNullOrEmpty(selectionResult?.UpdatedRegexResult) &&
                            !string.IsNullOrEmpty(selectionResult?.CompositeColumn);

                        // ✅ 为每个值组合生成独立文件
                        for (int i = 0; i < fileCount; i++)
                        {
                            // 创建新的 FileRenameInfo（第一个使用原 fileInfo，后续创建副本）
                            var currentFileInfo = i == 0 ? fileInfo : new FileRenameInfo
                            {
                                OriginalName = fileInfo.OriginalName,
                                FullPath = fileInfo.FullPath,
                                FileExtension = fileInfo.FileExtension,
                                RegexResult = fileInfo.RegexResult,
                                Width = fileInfo.Width,
                                Height = fileInfo.Height,
                                TetBleed = fileInfo.TetBleed
                            };
                            
                            // 应用用户选择（共用参数）
                            currentFileInfo.Material = selectionResult.SelectedMaterial;
                            currentFileInfo.OrderNumber = selectionResult.OrderNumber ?? "";
                            // 解析尺寸和形状
                            var (dim, shape) = ParseDimensionsAndShape(selectionResult.Dimensions ?? "");
                            currentFileInfo.Dimensions = dim;
                            currentFileInfo.Shape = shape;
                            currentFileInfo.Process = selectionResult.Process ?? "";
                            currentFileInfo.LayoutRows = selectionResult.LayoutRows ?? "";
                            currentFileInfo.LayoutColumns = selectionResult.LayoutColumns ?? "";
                            // ✅ 设置排版模式
                            currentFileInfo.ImpositionMode = GetImpositionModeString();

                            // ✅ 计算平张布局数量（仅输出到平张布局数列）
                            UpdateBothLayoutCountsAsync(currentFileInfo);

                            // 应用当前索引对应的数量和序号
                            currentFileInfo.Quantity = i < quantities.Length ? quantities[i] : quantities[quantities.Length - 1];
                            currentFileInfo.SerialNumber = i < serialNumbers.Length ? serialNumbers[i] : serialNumbers[serialNumbers.Length - 1];
                            
                            // ✅ 从Excel匹配列组合数据（与批量模式保持一致）
                            if (matchDataList != null && matchDataList.Count > 0)
                            {
                                // 使用当前索引对应的匹配数据，如果不够则复用最后一个
                                // 这确保了当手动输入多个数量/序号时，能够对应匹配到Excel中的多行数据
                                var matchData = matchDataList[Math.Min(i, matchDataList.Count - 1)];

                                if (matchData != null)
                                {
                                    // 应用材料匹配结果（如果用户没有手动选择）
                                    if (string.IsNullOrEmpty(currentFileInfo.Material) && !string.IsNullOrEmpty(matchData.Material))
                                    {
                                        currentFileInfo.Material = matchData.Material;
                                    }

                                    // 应用列组合匹配结果
                                    if (!string.IsNullOrEmpty(matchData.CompositeColumn))
                                    {
                                        currentFileInfo.CompositeColumn = matchData.CompositeColumn;
                                        _logger?.LogDebug($"✅ 手动模式设置列组合数据({i+1}): 文件='{currentFileInfo.OriginalName}', 列组合='{matchData.CompositeColumn}'");
                                    }
                                }
                            }

                            // ✅ 如果序号搜索更新后重新匹配失败，直接使用序号搜索返回的列组合
                            if (useDirectCompositeColumn)
                            {
                                currentFileInfo.CompositeColumn = selectionResult.CompositeColumn;
                                _logger?.LogInformation($"[ProcessNewFileAsync] 序号搜索直接返回列组合: 列组合='{selectionResult.CompositeColumn}'");
                            }
                            
                            // 生成新文件名
                            currentFileInfo.NewName = GenerateNewFileName(currentFileInfo);
                            
                            _logger?.LogInformation($"[ProcessNewFileAsync] 文件 {i + 1}/{fileCount}: 数量={currentFileInfo.Quantity}, 序号={currentFileInfo.SerialNumber}, 新名称={currentFileInfo.NewName}");
                            
                            // 在UI线程上添加到列表（使用 AddFileToTable 优先填入空行）
                            System.Windows.Forms.Application.OpenForms[0].Invoke((Action)(() =>
                            {
                                AddFileToTable(currentFileInfo);
                            }));
                            
                            // 执行文件操作（第一个移动/复制原文件，后续复制原文件）
                            bool success;
                            if (i == 0)
                            {
                                // 第一个文件：使用原文件
                                success = await RenameFileAsync(currentFileInfo);
                            }
                            else
                            {
                                // 后续文件：复制原文件到目标位置
                                success = await CopyFileToDestinationAsync(filePath, currentFileInfo);
                            }
                            
                            if (success) successCount++;
                        }
                        
                        if (successCount > 0 && AppSettings.ShowRenameCompleteNotification)
                        {
                            _view.ShowSuccess($"{fileInfo.OriginalName} → 已生成 {successCount}/{fileCount} 个文件");
                        }
                    }
                    else
                    {
                        _logger?.LogInformation($"[ProcessNewFileAsync] 用户取消或对话框返回Cancel");
                    }
                }
                else  // 批量模式
                {
                    _logger?.LogInformation($"[ProcessNewFileAsync] 批量模式 - 自动处理");
                    
                    // 匹配 Excel 数据（使用辅助方法支持保留分组）
                    string regexForMatching = GetRegexResultForMatching(fileInfo);
                    var excelDataList = MatchExcelData(regexForMatching);

                    if (excelDataList != null && excelDataList.Count > 0)
                    {
                        // ✅ 支持多重匹配（文件分裂）
                        for (int i = 0; i < excelDataList.Count; i++)
                        {
                            var excelData = excelDataList[i];
                            FileRenameInfo currentFileInfo;

                            if (i == 0)
                            {
                                // 第一个匹配项：使用原对象
                                currentFileInfo = fileInfo;
                            }
                            else
                            {
                                // 后续匹配项：克隆新对象（分裂）
                                currentFileInfo = fileInfo.Clone();
                            }

                            // 重新解析尺寸和形状（因为Clone或原对象可能包含未分离的尺寸字符串）
                            var (dim, shape) = ParseDimensionsAndShape(currentFileInfo.Dimensions ?? "");
                            currentFileInfo.Dimensions = dim;
                            currentFileInfo.Shape = shape;
                            
                            _logger?.LogDebug($"[批量模式] Excel匹配结果({i + 1}/{excelDataList.Count}) - Quantity: '{excelData.Quantity}', SerialNumber: '{excelData.SerialNumber}', Material: '{excelData.Material}', CompositeColumn: '{excelData.CompositeColumn}'");

                            // 应用 Excel 匹配结果
                            currentFileInfo.Quantity = excelData.Quantity;
                            currentFileInfo.SerialNumber = excelData.SerialNumber;
                            
                            // 应用材料匹配结果
                            if (!string.IsNullOrEmpty(excelData.Material))
                            {
                                currentFileInfo.Material = excelData.Material;
                            }
                            
                            // 应用列组合匹配结果
                            if (!string.IsNullOrEmpty(excelData.CompositeColumn))
                            {
                                currentFileInfo.CompositeColumn = excelData.CompositeColumn;
                                _logger?.LogDebug($"✅ 批量模式设置列组合数据: 文件='{currentFileInfo.OriginalName}', 列组合='{excelData.CompositeColumn}'");
                            }
                            else
                            {
                                _logger?.LogWarning($"⚠️ 批量模式：Excel匹配成功但列组合数据为空，文件='{currentFileInfo.OriginalName}'");
                            }

                            // 标记为已匹配
                            currentFileInfo.Status = "已匹配";

                            // ✅ 设置排版模式
                            currentFileInfo.ImpositionMode = GetImpositionModeString();

                            // ✅ 计算平张布局数量（仅输出到平张布局数列）
                            UpdateBothLayoutCountsAsync(currentFileInfo);

                            // 生成新文件名
                            currentFileInfo.NewName = GenerateNewFileName(currentFileInfo);

                            // 在UI线程上添加到列表（使用 AddFileToTable 优先填入空行）
                            System.Windows.Forms.Application.OpenForms[0].Invoke((Action)(() =>
                            {
                                AddFileToTable(currentFileInfo);
                            }));
                        }
                        
                        _view.UpdateStatus($"已匹配 {excelDataList.Count} 条 Excel 数据");
                    }
                    else
                    {
                        // 无匹配，按默认逻辑处理
                        _logger?.LogInformation($"[ProcessNewFileAsync] 无匹配结果，使用默认逻辑");
                        
                        // 自动生成序号
                        fileInfo.SerialNumber = GetNextSerialNumber().ToString();

                        // 解析尺寸和形状
                        var (dim, shape) = ParseDimensionsAndShape(fileInfo.Dimensions ?? "");
                        fileInfo.Dimensions = dim;
                        fileInfo.Shape = shape;
                        
                        // ✅ 设置排版模式
                        fileInfo.ImpositionMode = GetImpositionModeString();

                        // ✅ 计算平张布局数量（仅输出到平张布局数列）
                        UpdateBothLayoutCountsAsync(fileInfo);

                        // 生成新文件名
                        fileInfo.NewName = GenerateNewFileName(fileInfo);

                        // 在UI线程上添加到列表
                        System.Windows.Forms.Application.OpenForms[0].Invoke((Action)(() =>
                        {
                            AddFileToTable(fileInfo);
                            _view.UpdateStatus($"已添加文件: {fileInfo.OriginalName}");
                        }));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"处理新文件失败: {filePath}");
            }
        }

        #endregion

        #region 模式切换

        /// <summary>
        /// 处理切换复制/剪切模式
        /// </summary>
        public void HandleModeToggle()
        {
            _isCopyMode = !_isCopyMode;
            _view.UpdateModeButtonState(_isCopyMode);
            _view.UpdateStatus(_isCopyMode ? "已切换到复制模式" : "已切换到剪切模式");

            // 保存设置
            SaveSettings();
        }

        /// <summary>
        /// 处理切换手动/批量重命名模式
        /// </summary>
        public void HandleImmediateModeToggle()
        {
            var isImmediateMode = !_view.IsImmediateMode;
            _view.IsImmediateMode = isImmediateMode;
            _view.UpdateImmediateModeButtonState(isImmediateMode);
            _view.UpdateStatus(isImmediateMode ? "已切换到手动模式" : "已切换到批量模式");
        }

        /// <summary>
        /// 启动手动模式（立即弹出材料选择对话框）
        /// </summary>
        public void StartImmediateMode()
        {
            _view.IsImmediateMode = true;
            _view.UpdateImmediateModeButtonState(true);
            _view.UpdateStatus("已启动手动模式");
        }

        /// <summary>
        /// 停止手动模式（切换到批量模式）
        /// </summary>
        public void StopImmediateMode()
        {
            _view.IsImmediateMode = false;
            _view.UpdateImmediateModeButtonState(false);
            _view.UpdateStatus("已启动批量模式");
        }

        #endregion

        #region 文件重命名

        /// <summary>
        /// 处理批量重命名
        /// </summary>
        public async Task HandleRenameAsync()
        {
            try
            {
                var fileList = _view.FileList;
                if (fileList == null || fileList.Count == 0)
                {
                    _view.ShowWarning("没有需要重命名的文件");
                    return;
                }

                if (string.IsNullOrEmpty(_exportPath) || !Directory.Exists(_exportPath))
                {
                    _view.ShowError("请先设置有效的导出路径");
                    return;
                }

                // 确认操作
                var modeText = _isCopyMode ? "复制" : "移动";
                if (!_view.ShowConfirm($"确定要{modeText}重命名 {fileList.Count} 个文件吗？", "确认操作"))
                {
                    return;
                }

                _view.ShowProgress("正在重命名文件...");

                int successCount = 0;
                int failCount = 0;

                foreach (var fileInfo in fileList.Where(f => !string.IsNullOrEmpty(f.NewName)))
                {
                    _view.UpdateProgress(successCount + failCount + 1, fileList.Count, $"正在处理: {fileInfo.OriginalName}");

                    if (await RenameFileAsync(fileInfo))
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                    }
                }

                _view.HideProgress();
                _view.UpdateStatus($"重命名完成: 成功 {successCount}, 失败 {failCount}");

                // 检查是否显示通知
                if (successCount > 0 && AppSettings.ShowRenameCompleteNotification)
                {
                    // 获取第一个成功文件的原文件名作为通知的一部分
                    var firstFile = fileList.FirstOrDefault(f => !string.IsNullOrEmpty(f.NewName));
                    string originalName = firstFile?.OriginalName ?? "";
                    if (successCount == 1 && !string.IsNullOrEmpty(originalName))
                    {
                        _view.ShowSuccess($"{originalName} → 重命名成功");
                    }
                    else
                    {
                        _view.ShowSuccess($"成功重命名 {successCount} 个文件");
                    }
                }

                if (failCount > 0)
                {
                    _view.ShowWarning($"{failCount} 个文件重命名失败");
                }
            }
            catch (Exception ex)
            {
                _view.HideProgress();
                _logger?.LogError(ex, "批量重命名失败");
                _view.ShowError($"批量重命名失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理立即重命名单个文件
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <returns>重命名是否成功</returns>
        public async Task<bool> RenameFileAsync(FileRenameInfo fileInfo)
        {
            try
            {
                _logger?.LogInformation($"[RenameFileAsync] 开始重命名: {fileInfo?.OriginalName}");
                
                if (fileInfo == null || string.IsNullOrEmpty(fileInfo.FullPath))
                {
                    _logger?.LogWarning($"[RenameFileAsync] fileInfo为空或FullPath为空");
                    return false;
                }

                if (!File.Exists(fileInfo.FullPath))
                {
                    _logger?.LogError($"[RenameFileAsync] 文件不存在: {fileInfo.FullPath}");
                    _view.ShowError($"文件不存在: {fileInfo.OriginalName}");
                    return false;
                }

                if (string.IsNullOrEmpty(fileInfo.NewName))
                {
                    _logger?.LogWarning($"[RenameFileAsync] 文件 {fileInfo.OriginalName} 没有设置新文件名");
                    _view.ShowWarning($"文件 {fileInfo.OriginalName} 没有设置新文件名");
                    return false;
                }

                _logger?.LogInformation($"[RenameFileAsync] 导出路径: {_exportPath}");
                _logger?.LogInformation($"[RenameFileAsync] 新文件名: {fileInfo.NewName}");

                // 判断是否为PDF文件以及是否需要PDF处理
                bool isPdfFile = fileInfo.FullPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
                bool needsPdfProcessing = isPdfFile && (_currentIsShapeSelected || _currentNeedsRotation || 
                    (_currentEnableImposition && _currentLayoutMode == LayoutMode.Folding && _currentLayoutQuantity > 0));

                _logger?.LogInformation($"[RenameFileAsync] isPdfFile={isPdfFile}, needsPdfProcessing={needsPdfProcessing}");
                _logger?.LogInformation($"[RenameFileAsync] 排版: EnableImposition={_currentEnableImposition}, LayoutMode={_currentLayoutMode}, LayoutQuantity={_currentLayoutQuantity}");

                if (needsPdfProcessing)
                {
                    // ✅ 修复：使用 FileRenameService 的带 PdfProcessingOptions 的方法
                    // 这会触发完整的 PDF 处理流程，包括折手模式空白页插入
                    var pdfOptions = new PdfProcessingOptions
                    {
                        IsPdfFile = true,
                        AddPdfLayers = _currentIsShapeSelected,
                        ShapeType = _currentShapeType,
                        RoundRadius = _currentRoundRadius,
                        FinalDimensions = _currentDimensions,
                        RotationAngle = _currentRotationAngle,
                        // ✅ 关键修复:传递排版信息以触发折手模式空白页逻辑
                        LayoutMode = _currentLayoutMode,
                        LayoutQuantity = _currentLayoutQuantity,
                        // ✅ 一式N联联数
                        CopyCount = _currentCopyCount,
                        // ✅ 一式类型（联/份）
                        CopyType = _currentCopyType,
                        // ✅ 份数
                        DuplicateCount = _currentDuplicateCount,
                        // ✅ 关键修复:传递标识页信息
                        AddIdentifierPage = _currentAddIdentifierPage,
                        IdentifierPageContent = _currentIdentifierPageContent
                    };

                    _logger?.LogInformation($"[RenameFileAsync] 使用PdfProcessingOptions: LayoutMode={pdfOptions.LayoutMode}, LayoutQuantity={pdfOptions.LayoutQuantity}");

                    // 使用 FileRenameService 的带 PdfProcessingOptions 的方法
                    bool result = _fileRenameService.RenameFileImmediately(fileInfo, _exportPath, _isCopyMode, pdfOptions);

                    if (result)
                    {
                        _logger?.LogInformation($"[RenameFileAsync] 成功重命名并处理PDF: {fileInfo.OriginalName} -> {fileInfo.NewName}");
                    }
                    else
                    {
                        _logger?.LogError($"[RenameFileAsync] 重命名或PDF处理失败: {fileInfo.OriginalName}");
                    }

                    return result;
                }
                else
                {
                    // 非PDF或不需要PDF处理时，使用原有的简单处理方式
                    var destPath = Path.Combine(_exportPath, fileInfo.NewName);
                    _logger?.LogInformation($"[RenameFileAsync] 目标路径: {destPath}");

                    // 处理文件名冲突
                    destPath = HandleFileNameConflict(destPath);
                    _logger?.LogInformation($"[RenameFileAsync] 冲突处理后路径: {destPath}");

                    // 执行复制或移动
                    _logger?.LogInformation($"[RenameFileAsync] 复制模式: {_isCopyMode}");
                    var result = ProcessFileByMode(fileInfo.FullPath, destPath, _isCopyMode);

                    if (result)
                    {
                        _logger?.LogInformation($"[RenameFileAsync] 成功重命名: {fileInfo.OriginalName} -> {fileInfo.NewName}");
                    }
                    else
                    {
                        _logger?.LogError($"[RenameFileAsync] 重命名失败: {fileInfo.OriginalName}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"重命名文件 {fileInfo?.OriginalName} 失败");
                _view.ShowError($"重命名文件 {fileInfo?.OriginalName} 失败: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// 立即重命名文件（同步版本）
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="newName">新文件名</param>
        /// <returns>重命名是否成功</returns>
        public bool RenameFileImmediately(string sourcePath, string newName)
        {
            try
            {
                return _fileRenameService.RenameFileImmediately(sourcePath, newName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "立即重命名失败");
                _view.ShowError($"立即重命名失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 复制文件到目标位置（用于多值输出多文件场景的后续文件）
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <returns>复制是否成功</returns>
        private async Task<bool> CopyFileToDestinationAsync(string sourcePath, FileRenameInfo fileInfo)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || !System.IO.File.Exists(sourcePath))
                {
                    _logger?.LogWarning($"[CopyFileToDestinationAsync] 源文件不存在: {sourcePath}");
                    return false;
                }

                // 构建目标路径
                string destPath = System.IO.Path.Combine(_exportPath ?? AppDataPathManager.ExcelExportDirectory, fileInfo.NewName);
                
                // 处理文件名冲突
                destPath = HandleFileNameConflict(destPath);
                
                _logger?.LogInformation($"[CopyFileToDestinationAsync] 复制文件: {sourcePath} -> {destPath}");
                
                // 异步复制文件
                await Task.Run(() => System.IO.File.Copy(sourcePath, destPath, overwrite: false));
                
                // 应用 PDF 形状处理（如果需要）
                if (_currentIsShapeSelected && destPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) && _pdfProcessingService != null)
                {
                    // 使用新版方法 AddDotsAddCounterLayer(string, string, ShapeType, double)
                    // 通过强制转换调用具体类型的明确方法实现，避免接口方法歧义
                    bool shapeResult = ((Services.PdfProcessingService)_pdfProcessingService).AddDotsAddCounterLayer(destPath, _currentDimensions, _currentShapeType, _currentRoundRadius);
                    _logger?.LogInformation($"[CopyFileToDestinationAsync] PDF形状处理结果: {shapeResult}");
                }
                
                // 应用旋转处理（如果需要）
                if (_currentNeedsRotation && _currentRotationAngle != 0 && destPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        WindowsFormsApp3.Utils.PdfTools.RotateAllPages(destPath, _currentRotationAngle);
                        _logger?.LogInformation($"[CopyFileToDestinationAsync] 已旋转 PDF {_currentRotationAngle}°");
                    }
                    catch (Exception rotateEx)
                    {
                        _logger?.LogWarning($"[CopyFileToDestinationAsync] 旋转失败: {rotateEx.Message}");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[CopyFileToDestinationAsync] 复制文件失败: {sourcePath}");
                return false;
            }
        }

        /// <summary>
        /// 根据模式处理文件（复制或剪切）
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <returns>处理是否成功</returns>
        public bool ProcessFileByMode(string sourcePath, string destPath, bool isCopyMode)
        {
            try
            {
                if (isCopyMode)
                {
                    // 复制模式
                    File.Copy(sourcePath, destPath, overwrite: true);
                }
                else
                {
                    // 剪切模式
                    File.Move(sourcePath, destPath);
                }
                return true;
            }
            catch (Exception ex)
            {
                var modeText = isCopyMode ? "复制" : "移动";
                _logger?.LogError(ex, $"{modeText}文件失败: {sourcePath} -> {destPath}");
                return false;
            }
        }

        /// <summary>
        /// 处理文件名冲突
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>处理后的文件路径</returns>
        private string HandleFileNameConflict(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return filePath;
            }

            var directory = Path.GetDirectoryName(filePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var counter = 1;

            string newPath;
            do
            {
                newPath = Path.Combine(directory, $"{fileNameWithoutExtension} ({counter}){extension}");
                counter++;
            } while (File.Exists(newPath));

            return newPath;
        }

        #endregion

        #region Excel导入导出

        /// <summary>
        /// 处理导入Excel - 弹出ExcelImportForm对话框
        /// </summary>
        public async Task HandleImportExcelAsync()
        {
            try
            {
                // 设置父视图引用，让 ExcelImportForm 可以更新状态
                if (_excelImportService is ExcelImportHelper excelImportHelper)
                {
                    IExcelParentView parentView = _view as IExcelParentView;
                    if (parentView != null)
                    {
                        excelImportHelper.SetParentView(parentView);
                    }
                }

                // 启动导入流程（弹出 ExcelImportForm 对话框）
                bool importSuccess = _excelImportService.StartImport();

                // 如果导入成功且有有效数据
                if (importSuccess && _excelImportService.HasValidData())
                {
                    _view.ExcelData = _excelImportService.ImportedData;
                    // ✅ 同步列索引到视图（用于 MaterialSelectFormModern 自动填充）
                    _view.ExcelSearchColumnIndex = _excelImportService.SearchColumnIndex;
                    _view.ExcelReturnColumnIndex = _excelImportService.ReturnColumnIndex;
                    _view.ExcelSerialColumnIndex = _excelImportService.SerialColumnIndex;
                    _logger?.LogDebug($"同步列索引: Search={_excelImportService.SearchColumnIndex}, Return={_excelImportService.ReturnColumnIndex}, Serial={_excelImportService.SerialColumnIndex}");
                    _view.UpdateStatus($"成功导入 {_excelImportService.ImportedData.Rows.Count} 行 Excel 数据");
                    
                    // ✅ 自动触发匹配逻辑
                    MatchExcelData();
                    
                    _view.ShowSuccess($"已导入 {_excelImportService.ImportedData.Rows.Count} 行数据并自动匹配");
                }
                else
                {
                    _logger?.LogDebug("用户取消导入或导入失败");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入Excel失败");
                _view.ShowError($"导入Excel失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理清除Excel数据
        /// </summary>
        public void HandleClearExcel()
        {
            try
            {
                _view.ExcelData = null;
                _excelImportService.ClearData();
                _view.UpdateStatus("已清除Excel数据");
                _view.ShowInfo("Excel数据已清除");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "清除Excel数据失败");
                _view.ShowError($"清除Excel数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理导出Excel - 使用EPPlus导出文件列表
        /// </summary>
        public void HandleExportExcel()
        {
            try
            {
                var fileList = _view.FileList;
                if (fileList == null || fileList.Count == 0)
                {
                    _view.ShowWarning("没有可导出的数据");
                    return;
                }

                var defaultFileName = $"导出数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var filePath = _view.ShowSaveFileDialog("Excel文件 (*.xlsx)|*.xlsx|所有文件 (*.*)|*.*", defaultFileName);

                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                _view.ShowProgress("正在导出Excel...");

                // 使用 EPPlus 导出 Excel
                OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using (var package = new OfficeOpenXml.ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets.Add("文件重命名数据");

                    // 添加表头
                    var headers = new string[]
                    {
                        "序号", "原文件名", "新文件名", "完整路径", "正则结果",
                        "工单号", "材料", "数量", "尺寸", "形状", "工艺", "行数", "列数",
                        "时间", "状态", "错误信息", "页数", "列组合", "宽度", "高度", "出血值", "扩展名"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = headers[i];
                    }

                    // 添加数据行
                    int row = 2;
                    foreach (var item in fileList)
                    {
                        worksheet.Cells[row, 1].Value = item.SerialNumber ?? "";
                        worksheet.Cells[row, 2].Value = item.OriginalName ?? "";
                        worksheet.Cells[row, 3].Value = item.NewName ?? "";
                        worksheet.Cells[row, 4].Value = item.FullPath ?? "";
                        worksheet.Cells[row, 5].Value = item.RegexResult ?? "";
                        worksheet.Cells[row, 6].Value = item.OrderNumber ?? "";
                        worksheet.Cells[row, 7].Value = item.Material ?? "";
                        worksheet.Cells[row, 8].Value = item.Quantity ?? "";
                        worksheet.Cells[row, 9].Value = item.Dimensions ?? "";
                        worksheet.Cells[row, 10].Value = item.Shape ?? "";
                        worksheet.Cells[row, 11].Value = item.Process ?? "";
                        worksheet.Cells[row, 12].Value = item.LayoutRows ?? "";
                        worksheet.Cells[row, 13].Value = item.LayoutColumns ?? "";
                        worksheet.Cells[row, 14].Value = item.Time ?? "";
                        worksheet.Cells[row, 15].Value = item.Status ?? "";
                        worksheet.Cells[row, 16].Value = item.ErrorMessage ?? "";
                        worksheet.Cells[row, 17].Value = item.PageCount?.ToString() ?? "";
                        worksheet.Cells[row, 18].Value = item.CompositeColumn ?? "";
                        worksheet.Cells[row, 19].Value = item.Width ?? "";
                        worksheet.Cells[row, 20].Value = item.Height ?? "";
                        worksheet.Cells[row, 21].Value = item.TetBleed ?? "";
                        worksheet.Cells[row, 22].Value = item.FileExtension ?? "";
                        row++;
                    }

                    // 自动调整列宽
                    worksheet.Cells.AutoFitColumns();

                    package.Save();
                }

                _view.HideProgress();
                _view.UpdateStatus($"导出成功: {filePath}");
                _view.ShowSuccess("导出成功");
            }
            catch (Exception ex)
            {
                _view.HideProgress();
                _logger?.LogError(ex, "导出Excel失败");
                _view.ShowError($"导出Excel失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示导入的Excel数据
        /// </summary>
        /// <param name="data">Excel数据表</param>
        public void DisplayImportedExcelData(DataTable data)
        {
            // 数据已通过 _view.ExcelData 属性共享，DatabasePanel 会自动显示
        }

        /// <summary>
        /// 匹配Excel数据与文件列表
        /// 根据 regexResult 在 Excel 数据中查找匹配的行，并提取数量和序号
        /// </summary>
        public void MatchExcelData()
        {
            try
            {
                var excelData = _view.ExcelData;
                var fileList = _view.FileList;

                if (excelData == null || fileList == null)
                {
                    _logger?.LogDebug("MatchExcelData: Excel数据或文件列表为空，跳过匹配");
                    return;
                }

                // 获取列索引
                int searchColumnIndex = _excelImportService.SearchColumnIndex;
                int returnColumnIndex = _excelImportService.ReturnColumnIndex;
                int serialColumnIndex = _excelImportService.SerialColumnIndex;

                if (searchColumnIndex < 0 || returnColumnIndex < 0)
                {
                    _logger?.LogDebug($"MatchExcelData: 列索引无效 - 搜索列:{searchColumnIndex}, 返回列:{returnColumnIndex}");
                    return;
                }

                if (searchColumnIndex >= excelData.Columns.Count ||
                    returnColumnIndex >= excelData.Columns.Count)
                {
                    _logger?.LogDebug($"MatchExcelData: 列索引超出范围 - Excel列数:{excelData.Columns.Count}");
                    return;
                }

                _logger?.LogDebug($"MatchExcelData: 开始匹配 - 搜索列:{searchColumnIndex}, 返回列:{returnColumnIndex}, 序号列:{serialColumnIndex}");

                int matchedCount = 0;
                var newFileList = new BindingList<FileRenameInfo>();

                foreach (var fileInfo in fileList)
                {
                    // 如果文件名为空（可能是占位行），直接保留
                    if (string.IsNullOrEmpty(fileInfo.OriginalName))
                    {
                        newFileList.Add(fileInfo);
                        continue;
                    }

                    string regexResultForMatching = null;
                    
                    // ✅ 修复：先检查是否为保留分组文件
                    bool hasPreserveGroupPrefix = false;
                    // ... (保留现有逻辑: regexResultForMatching 提取部分) ...
                    if (!string.IsNullOrEmpty(fileInfo.OriginalName))
                    {
                        string originalNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.OriginalName);
                        
                        // 检测是否包含保留分组前缀模式 (如 &ID-, &MT- 等)
                        hasPreserveGroupPrefix = System.Text.RegularExpressions.Regex.IsMatch(originalNameWithoutExt, @"&[A-Z]+-");
                        
                        if (hasPreserveGroupPrefix)
                        {
                            // ... (保留现有逻辑) ...
                            var preserveGroupConfig = GetPreserveGroupConfig();
                            if (preserveGroupConfig != null && preserveGroupConfig.Count > 0)
                            {
                                var currentPrefixes = GetCurrentPrefixes();
                                var tempComponents = new Models.FileNameComponents
                                {
                                    OriginalFileName = originalNameWithoutExt,
                                    PreserveGroupConfig = preserveGroupConfig,
                                    Prefixes = currentPrefixes
                                };
                                var preserveData = tempComponents.ExtractPreserveGroupData(originalNameWithoutExt);
                                if (preserveData != null && preserveData.ContainsKey("正则结果"))
                                {
                                    string preservedRegexResult = preserveData["正则结果"];
                                    if (!string.IsNullOrEmpty(preservedRegexResult))
                                    {
                                        regexResultForMatching = preservedRegexResult;
                                        _logger?.LogInformation($"✅ 使用保留的正则结果: '{regexResultForMatching}'");
                                    }
                                }
                                else hasPreserveGroupPrefix = false;
                            }
                            else hasPreserveGroupPrefix = false;
                        }
                    }
                    
                    if (!hasPreserveGroupPrefix || string.IsNullOrEmpty(regexResultForMatching))
                    {
                        regexResultForMatching = ExtractRegexResultForMatching(fileInfo.OriginalName ?? "");
                        if (string.IsNullOrEmpty(regexResultForMatching))
                        {
                            // 无正则结果，保持原样加入
                            newFileList.Add(fileInfo);
                            continue;
                        }
                    }

                    // 获取所有匹配结果
                    var matches = MatchExcelData(regexResultForMatching);
                    
                    _logger?.LogDebug($"文件 '{fileInfo.OriginalName}' (正则: '{regexResultForMatching}') 在Excel中找到 {matches?.Count ?? 0} 个匹配项");

                    if (matches != null && matches.Count > 0)
                    {
                        // ✅ 支持多重匹配（文件分裂）
                        for (int i = 0; i < matches.Count; i++)
                        {
                            var matchData = matches[i];
                            FileRenameInfo currentFile;

                            if (i == 0)
                            {
                                // 第一个匹配项：使用原对象（更新）
                                currentFile = fileInfo;
                            }
                            else
                            {
                                // 后续匹配项：克隆新对象（分裂）
                                currentFile = fileInfo.Clone();
                            }

                            // 解析尺寸和形状
                            var (dim, shape) = ParseDimensionsAndShape(currentFile.Dimensions ?? "");
                            currentFile.Dimensions = dim;
                            currentFile.Shape = shape;

                            // 更新文件信息
                            currentFile.Quantity = matchData.Quantity;
                            currentFile.SerialNumber = matchData.SerialNumber;
                            if (!string.IsNullOrEmpty(matchData.Material)) currentFile.Material = matchData.Material;
                            if (!string.IsNullOrEmpty(matchData.CompositeColumn)) currentFile.CompositeColumn = matchData.CompositeColumn;

                            // 标记为已匹配
                            currentFile.Status = "已匹配";
                            
                            newFileList.Add(currentFile);
                            matchedCount++;
                            _logger?.LogDebug($"匹配成功({i + 1}/{matches.Count}): '{regexResultForMatching}' -> 序号='{matchData.SerialNumber}'");
                        }
                    }
                    else
                    {
                        // 无匹配，保持原样加入
                        newFileList.Add(fileInfo);
                    }
                }

                // ✅ 更新视图的文件列表
                _view.FileList = newFileList;
                _view.RefreshFileTable();
                _view.UpdateStatus($"匹配完成，成功匹配 {matchedCount} 条数据");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "匹配Excel数据失败");
                _view.ShowError($"匹配Excel数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据 regexResult 在 Excel 数据中查找匹配的行
        /// 返回匹配的数量、序号和材料
        /// </summary>
        /// <param name="regexResult">正则表达式匹配结果</param>
        /// <returns>匹配的 Excel 数据列表，如果没有匹配则返回空列表</returns>
        public List<Models.ExcelMatchData> MatchExcelData(string regexResult)
        {
            var results = new List<Models.ExcelMatchData>();
            
            try
            {
                var excelData = _excelImportService.ImportedData;
                if (excelData == null || string.IsNullOrEmpty(regexResult))
                {
                    return results;
                }

                int searchColumnIndex = _excelImportService.SearchColumnIndex;
                int returnColumnIndex = _excelImportService.ReturnColumnIndex;
                int serialColumnIndex = _excelImportService.SerialColumnIndex;
                int materialColumnIndex = _excelImportService.MaterialColumnIndex;

                // 验证列索引有效性
                if (searchColumnIndex < 0 || returnColumnIndex < 0 ||
                    searchColumnIndex >= excelData.Columns.Count ||
                    returnColumnIndex >= excelData.Columns.Count)
                {
                    return results;
                }

                // 在 Excel 中查找所有精确匹配
                foreach (DataRow row in excelData.Rows)
                {
                    if (row[searchColumnIndex] != null)
                    {
                        string tableValue = row[searchColumnIndex].ToString().Trim();

                        // 百分百精确匹配 (Trim后)
                        bool isExactMatch = string.Equals(tableValue, regexResult, StringComparison.Ordinal);

                        if (isExactMatch)
                        {
                            // 匹配成功，提取数据
                            string quantity = returnColumnIndex >= 0 && returnColumnIndex < excelData.Columns.Count
                                ? row[returnColumnIndex]?.ToString() ?? string.Empty
                                : string.Empty;
                            string serialNumber = serialColumnIndex >= 0 && serialColumnIndex < excelData.Columns.Count
                                ? row[serialColumnIndex]?.ToString() ?? string.Empty
                                : string.Empty;
                            // 提取材料字段
                            string material = materialColumnIndex >= 0 && materialColumnIndex < excelData.Columns.Count
                                ? row[materialColumnIndex]?.ToString() ?? string.Empty
                                : string.Empty;

                            // 提取列组合字段
                            string compositeColumn = string.Empty;
                            
                            if (excelData.Columns.Contains("列组合"))
                            {
                                int compositeColumnIndex = excelData.Columns.IndexOf("列组合");
                                compositeColumn = row[compositeColumnIndex]?.ToString() ?? string.Empty;
                            }

                            results.Add(new Models.ExcelMatchData
                            {
                                RowIndex = excelData.Rows.IndexOf(row),
                                Quantity = quantity,
                                SerialNumber = serialNumber,
                                Material = material,
                                CompositeColumn = compositeColumn
                            });
                        }
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"匹配 Excel 数据失败: regexResult='{regexResult}'");
                return results;
            }
        }

        #endregion

        #region JSON数据管理

        /// <summary>
        /// 获取当日JSON文件路径
        /// </summary>
        private string GetTodayJsonFilePath()
        {
            return Path.Combine(_savedGridsPath, $"{DateTime.Now:yyyy-MM-dd}.json");
        }

        /// <summary>
        /// 启动时确保当日JSON文件存在并自动加载
        /// </summary>
        private void EnsureDailyJsonLoaded()
        {
            try
            {
                if (!Directory.Exists(_savedGridsPath))
                {
                    Directory.CreateDirectory(_savedGridsPath);
                }

                var todayFilePath = GetTodayJsonFilePath();
                var todayConfigName = Path.GetFileNameWithoutExtension(todayFilePath);

                // 优先级最高：当日文件存在则加载，否则创建并保存当前表格数据（即使为空也创建文件）
                if (!File.Exists(todayFilePath))
                {
                    SaveToJsonFile(todayFilePath);
                    _logger?.LogInformation($"已自动创建当日配置: {todayConfigName}");
                }

                // 更新下拉列表并选中当日文件
                _view.CurrentConfigName = todayConfigName;
                UpdateJsonFilesList();

                // 加载到表格
                HandleLoadJson(todayConfigName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "当日JSON自动加载失败");
            }
        }

        /// <summary>
        /// 处理保存JSON
        /// </summary>
        public void HandleSaveJson()
        {
            try
            {
                var defaultFileName = $"保存数据_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = _view.ShowSaveFileDialog("JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*", defaultFileName);

                if (string.IsNullOrEmpty(filePath))
                {
                    return;
                }

                SaveToJsonFile(filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存JSON失败");
                _view.ShowError($"保存JSON失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理加载选定的JSON文件
        /// </summary>
        /// <param name="jsonFileName">JSON文件名</param>
        public void HandleLoadJson(string jsonFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonFileName))
                {
                    return;
                }

                var filePath = Path.Combine(_savedGridsPath, jsonFileName + ".json");

                if (!File.Exists(filePath))
                {
                    _view.ShowError($"JSON文件不存在: {jsonFileName}");
                    return;
                }

                _view.ShowProgress("正在加载JSON...");

                var dataList = LoadFromJsonFile(filePath);

                if (dataList != null)
                {
                    // 补齐到999行，保持Excel表格效果
                    while (dataList.Count < 999)
                    {
                        dataList.Add(new FileRenameInfo());
                    }

                    _view.FileList = new BindingList<FileRenameInfo>(dataList);
                    _view.RefreshFileTable();
                    _view.UpdateStatus($"加载成功，共 {dataList.Count} 条记录");
                    _view.ShowSuccess($"加载成功，共 {dataList.Count} 条记录");
                }
                else
                {
                    _view.ShowWarning("JSON文件为空或加载失败");
                }

                _view.HideProgress();
            }
            catch (Exception ex)
            {
                _view.HideProgress();
                _logger?.LogError(ex, "加载JSON失败");
                _view.ShowError($"加载JSON失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行自动保存
        /// </summary>
        public void PerformAutoSave()
        {
            try
            {
                var configName = _view.CurrentConfigName;
                if (string.IsNullOrEmpty(configName))
                {
                    configName = $"AutoSave_{DateTime.Now:yyyyMMdd_HHmmss}";
                }

                var filePath = Path.Combine(_savedGridsPath, configName + ".json");
                SaveToJsonFile(filePath);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "自动保存失败");
            }
        }

        /// <summary>
        /// 保存数据到JSON文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        public void SaveToJsonFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    _view.ShowError("保存失败: 文件路径为空");
                    return;
                }

                // 确保目录存在
                var dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var dataList = _view.FileList?.ToList();

                if (dataList == null)
                {
                    dataList = new List<FileRenameInfo>();
                }

                var json = JsonConvert.SerializeObject(dataList, Formatting.Indented);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                _view.UpdateStatus($"保存成功: {Path.GetFileName(filePath)}");
                UpdateJsonFilesList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存JSON失败");
                _view.ShowError($"保存JSON失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从JSON文件加载数据
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns>文件重命名信息列表</returns>
        public List<FileRenameInfo> LoadFromJsonFile(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                var dataList = JsonConvert.DeserializeObject<List<FileRenameInfo>>(json);
                return dataList;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载JSON失败");
                _view.ShowError($"加载JSON失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取已保存的配置列表
        /// </summary>
        /// <returns>配置名称列表</returns>
        public List<string> GetSavedConfigs()
        {
            try
            {
                if (!Directory.Exists(_savedGridsPath))
                {
                    return new List<string>();
                }

                return Directory.GetFiles(_savedGridsPath, "*.json")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .OrderByDescending(f => f)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取已保存配置列表失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 更新JSON文件列表
        /// </summary>
        private void UpdateJsonFilesList()
        {
            var jsonFiles = GetSavedConfigs();
            _view.UpdateJsonFilesDropdown(jsonFiles);
        }

        #endregion

        #region 正则表达式管理

        /// <summary>
        /// 加载正则表达式模式
        /// </summary>
        public void LoadRegexPatterns()
        {
            try
            {
                _regexPatterns.Clear();

                // 从AppSettings加载
                var savedPatterns = AppSettings.Instance.RegexPatterns;
                if (!string.IsNullOrEmpty(savedPatterns))
                {
                    var patterns = savedPatterns.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pattern in patterns)
                    {
                        var parts = pattern.Split(new[] { '=' }, 2);
                        if (parts.Length == 2)
                        {
                            _regexPatterns[parts[0]] = parts[1];
                        }
                    }
                }

                // 如果为空，添加默认模式
                if (_regexPatterns.Count == 0)
                {
                    _regexPatterns["订单号(4位数字)"] = @"(\d{4})";
                    _regexPatterns["订单号(5位数字)"] = @"(\d{5})";
                    _regexPatterns["订单号(6位数字)"] = @"(\d{6})";
                }

                _logger?.LogInformation($"加载了 {_regexPatterns.Count} 个正则表达式模式");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载正则表达式失败");
            }
        }

        /// <summary>
        /// 保存正则表达式设置
        /// </summary>
        public void SaveRegexSettings()
        {
            try
            {
                var patterns = _regexPatterns
                    .Select(p => $"{p.Key}={p.Value}")
                    .Aggregate((a, b) => a + "|" + b);

                AppSettings.Instance.RegexPatterns = patterns;
                AppSettings.Save();

                _logger?.LogInformation("正则表达式设置已保存");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存正则表达式设置失败");
            }
        }

        /// <summary>
        /// 获取所有正则表达式模式
        /// </summary>
        /// <returns>正则表达式模式字典（显示名称 -> 正则表达式）</returns>
        public Dictionary<string, string> GetRegexPatterns()
        {
            return new Dictionary<string, string>(_regexPatterns);
        }

        /// <summary>
        /// 添加正则表达式模式
        /// </summary>
        /// <param name="name">模式名称</param>
        /// <param name="pattern">正则表达式</param>
        public void AddRegexPattern(string name, string pattern)
        {
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(pattern))
            {
                _regexPatterns[name] = pattern;
                _view.UpdateRegexComboBox(_regexPatterns.Keys.ToList());
                SaveRegexSettings();
            }
        }

        /// <summary>
        /// 移除正则表达式模式
        /// </summary>
        /// <param name="name">模式名称</param>
        public void RemoveRegexPattern(string name)
        {
            if (_regexPatterns.Remove(name))
            {
                _view.UpdateRegexComboBox(_regexPatterns.Keys.ToList());
                SaveRegexSettings();
            }
        }

        #endregion

        #region 材料管理

        /// <summary>
        /// 加载材料列表
        /// </summary>
        public void LoadMaterials()
        {
            try
            {
                _logger?.LogInformation("[LoadMaterials] 开始加载材料列表");
                _materials.Clear();

                var savedMaterials = AppSettings.Instance.Materials;
                if (!string.IsNullOrEmpty(savedMaterials))
                {
                    var materials = savedMaterials.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var material in materials)
                    {
                        _materials.Add(material.Trim());
                    }
                }

                // 如果为空，添加默认材料
                if (_materials.Count == 0)
                {
                    _materials.AddRange(new[] { "铜版纸", "胶版纸", "特种纸", "不干胶", "PET", "PVC" });
                }

                _view.UpdateMaterialsContextMenu(_materials);
                _logger?.LogInformation($"加载了 {_materials.Count} 个材料");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载材料列表失败");
            }
        }

        /// <summary>
        /// 保存材料设置
        /// </summary>
        public void SaveMaterialSettings()
        {
            try
            {
                var materials = string.Join(",", _materials);
                AppSettings.Instance.Materials = materials;
                AppSettings.Save();

                _logger?.LogInformation("材料设置已保存");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存材料设置失败");
            }
        }

        /// <summary>
        /// 获取所有材料
        /// </summary>
        /// <returns>材料列表</returns>
        public List<string> GetMaterials()
        {
            return new List<string>(_materials);
        }

        /// <summary>
        /// 添加材料
        /// </summary>
        /// <param name="material">材料名称</param>
        public void AddMaterial(string material)
        {
            if (!string.IsNullOrEmpty(material) && !_materials.Contains(material))
            {
                _materials.Add(material);
                _view.UpdateMaterialsContextMenu(_materials);
                SaveMaterialSettings();
            }
        }

        /// <summary>
        /// 移除材料
        /// </summary>
        /// <param name="material">材料名称</param>
        public void RemoveMaterial(string material)
        {
            if (_materials.Remove(material))
            {
                _view.UpdateMaterialsContextMenu(_materials);
                SaveMaterialSettings();
            }
        }

        #endregion

        #region 数据表格操作

        /// <summary>
        /// 添加文件到表格
        /// 优先使用已存在的空行，如果没有空行才添加新行
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        public void AddFileToTable(FileRenameInfo fileInfo)
        {
            if (_view.FileList == null)
            {
                _view.FileList = new BindingList<FileRenameInfo>();
            }

            // 查找第一个空行（OriginalName 为空或 FullPath 为空表示空行）
            int emptyRowIndex = -1;
            for (int i = 0; i < _view.FileList.Count; i++)
            {
                var row = _view.FileList[i];
                if (string.IsNullOrEmpty(row.OriginalName) && string.IsNullOrEmpty(row.FullPath))
                {
                    emptyRowIndex = i;
                    break;
                }
            }

            if (emptyRowIndex >= 0)
            {
                // 使用空行：复制所有属性到空行
                var emptyRow = _view.FileList[emptyRowIndex];
                CopyFileRenameInfo(fileInfo, emptyRow);
                _logger?.LogDebug($"填入空行 {emptyRowIndex + 1}: {fileInfo.OriginalName}");
            }
            else
            {
                // 没有空行，添加新行
                _view.FileList.Add(fileInfo);
                _logger?.LogDebug($"添加新行: {fileInfo.OriginalName}");
            }
            
            _view.RefreshFileTable();
        }

        /// <summary>
        /// 添加空行到表格
        /// </summary>
        public void AddEmptyRowToTable()
        {
            if (_view.FileList == null)
            {
                _view.FileList = new BindingList<FileRenameInfo>();
            }

            _view.FileList.Add(new FileRenameInfo());
        }

        /// <summary>
        /// 复制 FileRenameInfo 的所有属性
        /// </summary>
        private void CopyFileRenameInfo(FileRenameInfo source, FileRenameInfo target)
        {
            target.OriginalName = source.OriginalName;
            target.NewName = source.NewName;
            target.Material = source.Material;
            target.Quantity = source.Quantity;
            target.Dimensions = source.Dimensions;
            target.Process = source.Process;
            target.Status = source.Status;
            target.FullPath = source.FullPath;
            target.Width = source.Width;
            target.Height = source.Height;
            target.TetBleed = source.TetBleed;
            target.PageCount = source.PageCount;
            target.RegexResult = source.RegexResult;
            target.OrderNumber = source.OrderNumber;
            target.LayoutRows = source.LayoutRows;
            target.LayoutColumns = source.LayoutColumns;
            target.Time = source.Time;
            target.ErrorMessage = source.ErrorMessage;
            target.CompositeColumn = source.CompositeColumn;
            target.FileExtension = source.FileExtension;
            target.SerialNumber = source.SerialNumber;
            target.Shape = source.Shape;
            target.ImpositionMode = source.ImpositionMode; // Also verify this one while I am at it
            target.FlatSheetLayoutCount = source.FlatSheetLayoutCount;
            target.RollMaterialLayoutCount = source.RollMaterialLayoutCount;
            target.FlatSheetLayoutRows = source.FlatSheetLayoutRows;
            target.FlatSheetLayoutColumns = source.FlatSheetLayoutColumns;
            target.RollMaterialLayoutRows = source.RollMaterialLayoutRows;
            target.RollMaterialLayoutColumns = source.RollMaterialLayoutColumns;
            target.HighlightApplied = source.HighlightApplied;
        }

        /// <summary>
        /// 从表格中删除文件行
        /// </summary>
        /// <param name="rowIndex">行索引</param>
        public void RemoveFileFromTable(int rowIndex)
        {
            if (_view.FileList != null && rowIndex >= 0 && rowIndex < _view.FileList.Count)
            {
                _view.FileList.RemoveAt(rowIndex);
                _view.RefreshFileTable();
            }
        }

        /// <summary>
        /// 刷新文件列表
        /// </summary>
        public void RefreshFileList()
        {
            _view.RefreshFileTable();
        }

        /// <summary>
        /// 清空文件列表
        /// </summary>
        public void ClearFileList()
        {
            _view.FileList = new BindingList<FileRenameInfo>();
            _view.RefreshFileTable();
        }

        #endregion

        #region 单元格编辑

        /// <summary>
        /// 处理单元格值变化
        /// </summary>
        /// <param name="rowIndex">行索引</param>
        /// <param name="columnIndex">列索引</param>
        /// <param name="newValue">新值</param>
        /// <param name="oldValue">旧值</param>
        public void HandleCellValueChanged(int rowIndex, int columnIndex, object newValue, object oldValue)
        {
            try
            {
                if (_view.FileList != null && rowIndex >= 0 && rowIndex < _view.FileList.Count)
                {
                    var fileInfo = _view.FileList[rowIndex];

                    // 根据列索引更新对应属性
                    // TODO: 实现具体的属性更新逻辑

                    _view.RefreshFileTable();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理单元格值变化失败");
            }
        }

        #endregion

        #region 排版材料类型

        /// <summary>
        /// 获取当前排版材料类型
        /// </summary>
        public string CurrentImpositionMaterialType => _currentImpositionMaterialType;

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取导出路径
        /// </summary>
        /// <returns>导出路径</returns>
        public string GetExportPath()
        {
            return _exportPath;
        }

        /// <summary>
        /// 设置导出路径
        /// </summary>
        /// <param name="path">导出路径</param>
        public void SetExportPath(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                _exportPath = path;
                AppSettings.Instance.LastOutputDir = path;
                AppSettings.Save();
            }
        }

        /// <summary>
        /// 从文件名中提取正则匹配结果
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>正则匹配结果</returns>
        public string ExtractRegexResult(string fileName)
        {
            try
            {
                var pattern = _view.SelectedRegexValue;
                _logger?.LogInformation($"[ExtractRegexResult] 开始提取正则结果 - 文件名: {fileName}, 正则: {pattern}");
                
                if (string.IsNullOrEmpty(pattern))
                {
                    _logger?.LogWarning($"[ExtractRegexResult] 正则表达式为空");
                    return null;
                }

                var match = Regex.Match(fileName, pattern);
                if (match.Success)
                {
                    // ✅ 修复：优先使用第一个捕获组的值，如果没有捕获组则使用整个匹配
                    string result;
                    if (match.Groups.Count > 1 && match.Groups[1].Success)
                    {
                        result = match.Groups[1].Value;
                        _logger?.LogInformation($"[ExtractRegexResult] 提取捕获组结果: '{result}'");
                    }
                    else
                    {
                        result = match.Value;
                        _logger?.LogInformation($"[ExtractRegexResult] 使用完整匹配结果: '{result}'");
                    }
                    return result;
                }

                _logger?.LogWarning($"[ExtractRegexResult] 正则匹配失败 - 文件名: {fileName}, 正则: {pattern}");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"提取正则结果失败: {fileName}");
                return null;
            }
        }

        /// <summary>
        /// 从文件名中提取正则匹配结果（用于数据匹配）
        /// 使用 ExcelImportService 中保存的正则表达式
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>正则匹配结果</returns>
        private string ExtractRegexResultForMatching(string fileName)
        {
            try
            {
                // ✅ 修复：使用 ExcelImportService 的正则表达式
                var pattern = _excelImportService?.SelectedRegexPattern;
                _logger?.LogInformation($"[ExtractRegexResultForMatching] 开始提取正则结果 - 文件名: {fileName}, 正则: {pattern}");
                
                if (string.IsNullOrEmpty(pattern))
                {
                    _logger?.LogWarning($"[ExtractRegexResultForMatching] ExcelImportService 的正则表达式为空");
                    return null;
                }

                var match = Regex.Match(fileName, pattern);
                if (match.Success)
                {
                    // 优先使用第一个捕获组的值，如果没有捕获组则使用整个匹配
                    string result;
                    if (match.Groups.Count > 1 && match.Groups[1].Success)
                    {
                        result = match.Groups[1].Value;
                        _logger?.LogInformation($"[ExtractRegexResultForMatching] 提取捕获组结果: '{result}'");
                    }
                    else
                    {
                        result = match.Value;
                        _logger?.LogInformation($"[ExtractRegexResultForMatching] 使用完整匹配结果: '{result}'");
                    }
                    return result;
                }

                _logger?.LogWarning($"[ExtractRegexResultForMatching] 正则匹配失败 - 文件名: {fileName}, 正则: {pattern}");
                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[ExtractRegexResultForMatching] 提取正则结果失败: {fileName}");
                return null;
            }
        }

        /// <summary>
        /// 根据文件名是否包含分组前缀来选择合适的正则提取方法
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>正则匹配结果</returns>
        private string ExtractRegexResultBasedOnPrefix(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            try
            {
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                
                // 检查是否包含分组前缀 (如 &ID-, &MT- 等)
                bool hasGroupPrefix = Regex.IsMatch(fileNameWithoutExt, @"&[A-Z]+-");
                
                if (hasGroupPrefix)
                {
                    // 有分组前缀 - 使用 FileRenamePanel 的正则 (用于显示)
                    _logger?.LogDebug($"[ExtractRegexResultBasedOnPrefix] 检测到分组前缀,使用 FileRenamePanel 正则: '{fileName}'");
                    return ExtractRegexResult(fileName);
                }
                else
                {
                    // 无分组前缀 - 使用 ExcelImportForm 的正则 (用于数据匹配)
                    _logger?.LogDebug($"[ExtractRegexResultBasedOnPrefix] 无分组前缀,使用 ExcelImportForm 正则: '{fileName}'");
                    return ExtractRegexResultForMatching(fileName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[ExtractRegexResultBasedOnPrefix] 提取正则结果失败: {fileName}");
                return null;
            }
        }


        /// <summary>
        /// 处理正则表达式变化事件
        /// </summary>
        public async Task HandleRegexPatternChangedAsync(string newPattern)
        {
            try
            {
                // 获取当前文件列表
                var fileList = _view.FileList;
                if (fileList == null || fileList.Count == 0)
                {
                    _view.UpdateStatus("文件列表为空，无需重新处理");
                    return;
                }

                _view.UpdateStatus($"正在重新处理 {fileList.Count} 个文件...");

                // 重新处理所有文件
                int processedCount = 0;
                foreach (var file in fileList)
                {
                    // 更新正则匹配结果
                    // ✅ 修复：RegexResult 始终使用 FileRenamePanel 的正则表达式 (_cmbRegex)
                    file.RegexResult = ExtractRegexResult(file.OriginalName ?? "");

                    // 如果启用了自动刷新，也更新新文件名
                    if (AppSettings.AutoRefreshFileNameOnRegexChange)
                    {
                        // 重新匹配 Excel 数据（使用辅助方法支持保留分组）
                        // 重新匹配 Excel 数据（使用辅助方法支持保留分组）
                        string regexForMatching = GetRegexResultForMatching(file);
                        var excelDataList = MatchExcelData(regexForMatching);
                        if (excelDataList != null && excelDataList.Count > 0)
                        {
                            var excelData = excelDataList[0];
                            file.Quantity = excelData.Quantity;
                            file.SerialNumber = excelData.SerialNumber;
                        }

                        // 重新生成文件名
                        file.NewName = GenerateNewFileName(file);
                    }

                    processedCount++;
                }

                // 刷新表格显示
                _view.RefreshFileTable();
                _view.UpdateStatus($"已重新处理 {processedCount} 个文件");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "重新处理文件列表失败");
                _view.ShowError($"重新处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证文件是否需要处理
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否需要处理</returns>
        public bool ShouldProcessFile(string fileName)
        {
            try
            {
                var extension = Path.GetExtension(fileName).ToLower();
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tiff", ".tif" };
                return allowedExtensions.Contains(extension);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 创建文件重命名信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件重命名信息</returns>
        private FileRenameInfo CreateFileRenameInfo(string filePath)
        {
            var fileInfo = new FileRenameInfo
            {
                FullPath = filePath,
                OriginalName = Path.GetFileName(filePath),
                // ✅ 修复：RegexResult 始终使用 FileRenamePanel 的正则表达式 (_cmbRegex)
                // 数据匹配会单独使用 GetRegexResultForMatching 方法来判断使用哪个正则
                RegexResult = ExtractRegexResult(Path.GetFileName(filePath))
            };

            // ✅ 新增：检查是否为返单文件，如果是则从保留分组中提取数据
            try
            {
                string originalNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.OriginalName);
                
                // 检测是否包含保留分组前缀模式 (如 &ID-, &MT- 等)
                if (System.Text.RegularExpressions.Regex.IsMatch(originalNameWithoutExt, @"&[A-Z]+-"))
                {
                    _logger?.LogDebug($"[CreateFileRenameInfo] 检测到保留分组前缀，提取保留数据: '{originalNameWithoutExt}'");
                    
                    // 获取保留分组配置
                    var preserveGroupConfig = GetPreserveGroupConfig();
                    
                    if (preserveGroupConfig != null && preserveGroupConfig.Count > 0)
                    {
                        // 获取当前的前缀配置
                        var currentPrefixes = GetCurrentPrefixes();
                        
                        // 创建临时的 FileNameComponents 对象用于提取保留数据
                        var tempComponents = new Models.FileNameComponents
                        {
                            OriginalFileName = originalNameWithoutExt,
                            PreserveGroupConfig = preserveGroupConfig,
                            Prefixes = currentPrefixes
                        };
                        
                        // 提取保留的分组数据
                        var preserveData = tempComponents.ExtractPreserveGroupData(originalNameWithoutExt);
                        
                        if (preserveData != null && preserveData.Count > 0)
                        {
                            _logger?.LogInformation($"✅ [CreateFileRenameInfo] 提取到 {preserveData.Count} 个保留分组数据");
                            
                            // 应用保留的数据到 fileInfo
                            if (preserveData.ContainsKey("正则结果"))
                            {
                                fileInfo.RegexResult = preserveData["正则结果"];
                                _logger?.LogInformation($"   - 正则结果: '{fileInfo.RegexResult}'");
                            }
                            if (preserveData.ContainsKey("订单号"))
                            {
                                fileInfo.OrderNumber = preserveData["订单号"];
                                _logger?.LogDebug($"   - 订单号: '{fileInfo.OrderNumber}'");
                            }
                            if (preserveData.ContainsKey("材料"))
                            {
                                fileInfo.Material = preserveData["材料"];
                                _logger?.LogDebug($"   - 材料: '{fileInfo.Material}'");
                            }
                            if (preserveData.ContainsKey("数量"))
                            {
                                fileInfo.Quantity = preserveData["数量"];
                                _logger?.LogDebug($"   - 数量: '{fileInfo.Quantity}'");
                            }
                            if (preserveData.ContainsKey("工艺"))
                            {
                                fileInfo.Process = preserveData["工艺"];
                                _logger?.LogDebug($"   - 工艺: '{fileInfo.Process}'");
                            }
                            if (preserveData.ContainsKey("序号"))
                            {
                                fileInfo.SerialNumber = preserveData["序号"];
                                _logger?.LogDebug($"   - 序号: '{fileInfo.SerialNumber}'");
                            }
                            if (preserveData.ContainsKey("尺寸"))
                            {
                                fileInfo.Dimensions = preserveData["尺寸"];
                                _logger?.LogDebug($"   - 尺寸: '{fileInfo.Dimensions}'");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[CreateFileRenameInfo] 提取保留分组数据时发生异常");
            }

            // 解析 PDF 尺寸和页数
            try
            {
                if (_pdfDimensionService != null && 
                    (filePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) ||
                     filePath.EndsWith(".PDF", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger?.LogInformation($"[CreateFileRenameInfo] 开始解析PDF尺寸: {filePath}");
                    
                    // 使用 GetFirstPageSize 方法获取PDF尺寸
                    if (_pdfDimensionService.GetFirstPageSize(filePath, out double width, out double height))
                    {
                        // 转换为字符串（保持1位小数，如果是整数则不显示）
                        fileInfo.Width = width.ToString("0.#");
                        fileInfo.Height = height.ToString("0.#");
                        // ✅ 自动填充 Dimensions 属性，确保表格“尺寸”列显示数据
                        fileInfo.Dimensions = $"{fileInfo.Width}x{fileInfo.Height}";
                        _logger?.LogInformation($"[CreateFileRenameInfo] PDF尺寸解析成功: {fileInfo.Width}x{fileInfo.Height}mm");
                        
                        // 设置默认出血位（从 AppSettings 获取）
                        var tetBleedValues = AppSettings.Instance.TetBleedValues;
                        if (!string.IsNullOrEmpty(tetBleedValues))
                        {
                            var bleedValues = tetBleedValues.Split(',');
                            if (bleedValues.Length > 0)
                            {
                                fileInfo.TetBleed = bleedValues[0].Trim();
                                _logger?.LogInformation($"[CreateFileRenameInfo] 设置默认出血位: {fileInfo.TetBleed}mm");
                            }
                        }
                    }
                    else
                    {
                        _logger?.LogWarning($"[CreateFileRenameInfo] PDF尺寸解析失败: {filePath}");
                    }
                    
                    // ✅ 获取 PDF 页数
                    try
                    {
                        int? pageCount = _pdfDimensionService.GetPageCount(filePath);
                        if (pageCount.HasValue && pageCount.Value > 0)
                        {
                            fileInfo.PageCount = pageCount.Value;
                            _logger?.LogInformation($"[CreateFileRenameInfo] PDF页数: {pageCount.Value}");
                        }
                    }
                    catch (Exception pageEx)
                    {
                        _logger?.LogWarning($"[CreateFileRenameInfo] 获取PDF页数失败: {pageEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[CreateFileRenameInfo] 解析PDF尺寸时发生错误: {filePath}");
            }

            return fileInfo;
        }

        /// <summary>
        /// 获取下一个序号
        /// </summary>
        private int GetNextSerialNumber()
        {
            // ✅ 修复：基于现有数据的最大序号递增
            int maxSerialNumber = 0;
            
            if (_view.FileList != null && _view.FileList.Count > 0)
            {
                foreach (var item in _view.FileList)
                {
                    // 只统计有数据的行（OriginalName 不为空）
                    if (!string.IsNullOrEmpty(item.OriginalName) && 
                        !string.IsNullOrEmpty(item.SerialNumber))
                    {
                        if (int.TryParse(item.SerialNumber, out int serial))
                        {
                            if (serial > maxSerialNumber)
                            {
                                maxSerialNumber = serial;
                            }
                        }
                    }
                }
            }
            
            return maxSerialNumber + 1;
        }

        /// <summary>
        /// 获取 Excel 列名列表
        /// </summary>
        private List<string> GetExcelColumnNames()
        {
            var columnNames = new List<string>();
            try
            {
                if (_excelImportService != null && _excelImportService.HasValidData())
                {
                    var data = _excelImportService.ImportedData;
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        columnNames.Add(data.Columns[i].ColumnName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取 Excel 列名失败");
            }
            return columnNames;
        }

        /// <summary>
        /// 获取 Excel 列项映射
        /// </summary>
        private Dictionary<string, List<string>> GetExcelColumnItemsMap()
        {
            var columnItemsMap = new Dictionary<string, List<string>>();
            try
            {
                if (_excelImportService != null && _excelImportService.HasValidData())
                {
                    var data = _excelImportService.ImportedData;
                    foreach (System.Data.DataColumn col in data.Columns)
                    {
                        var items = new List<string>();
                        foreach (System.Data.DataRow row in data.Rows)
                        {
                            var value = row[col]?.ToString();
                            if (!string.IsNullOrEmpty(value) && !items.Contains(value))
                            {
                                items.Add(value);
                            }
                        }
                        columnItemsMap[col.ColumnName] = items;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取 Excel 列项映射失败");
            }
            return columnItemsMap;
        }

        #region 事件分组配置辅助方法

        /// <summary>
        /// 从EventGroup配置创建文件名组件配置
        /// </summary>
        private FileNameComponentsConfig CreateFileNameComponentsConfigFromEventGroup()
        {
            var config = new FileNameComponentsConfig();

            try
            {
                _logger?.LogInformation("[CreateFileNameComponentsConfigFromEventGroup] 开始从EventGroup创建配置");

                var eventGroupConfig = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups != null && eventGroupConfig.Items != null)
                {
                    _logger?.LogInformation("[CreateFileNameComponentsConfigFromEventGroup] 使用EventGroup配置");

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
                            case "排版模式":
                            case "材料类型":
                                config.ImpositionModeEnabled = true;
                                break;
                        }

                        _logger?.LogInformation($"[CreateFileNameComponentsConfigFromEventGroup] 启用组件: {componentType}");
                    }
                }
                else
                {
                    _logger?.LogWarning("[CreateFileNameComponentsConfigFromEventGroup] EventGroup配置为空，使用默认配置");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[CreateFileNameComponentsConfigFromEventGroup] 创建配置时发生异常");
            }

            // ✅ 修复：如果启用了列组合功能，强制启用CompositeColumnEnabled
            if (AppSettings.EnableColumnCombine)
            {
                config.CompositeColumnEnabled = true;
                _logger?.LogInformation("[CreateFileNameComponentsConfigFromEventGroup] 检测到启用列组合功能，强制启用CompositeColumnEnabled");
            }

            // ✅ 调试：记录最终配置状态
            _logger?.LogInformation($"[CreateFileNameComponentsConfigFromEventGroup] 最终配置 - CompositeColumnEnabled: {config.CompositeColumnEnabled}");

            return config;
        }

        /// <summary>
        /// 从设置中获取组件顺序
        /// </summary>
        private List<string> GetComponentOrderFromSettings()
        {
            var componentOrder = new List<string>();

            try
            {
                var eventGroupConfig = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups != null && eventGroupConfig.Items != null)
                {
                    _logger?.LogInformation("GetComponentOrderFromSettings: 使用EventGroup配置设置组件顺序");

                    var sortedGroups = eventGroupConfig.Groups
                        .Where(g => g.IsEnabled)
                        .OrderBy(g => g.SortOrder)
                        .ToList();

                    // ✅ 修复：检查是否存在"未分组"组（Id="" 或 "ungrouped"），如果不存在且有未分组项目，则虚拟添加一个
                    // 这防止了旧的预设配置（缺少未分组定义）导致未分组项目被忽略
                    var hasUngroupedDefinition = sortedGroups.Any(g => string.IsNullOrEmpty(g.Id) || g.Id == "ungrouped");
                    if (!hasUngroupedDefinition)
                    {
                        var ungroupedItemsExist = eventGroupConfig.Items.Any(i => (string.IsNullOrEmpty(i.GroupId) || i.GroupId == "ungrouped") && i.IsEnabled);
                        if (ungroupedItemsExist)
                        {
                            _logger?.LogInformation("GetComponentOrderFromSettings: 检测到未分组项目但缺少分组定义，虚拟添加未分组");
                            // 添加到最后
                            sortedGroups.Add(new EventGroup { Id = "", DisplayName = "未分组", IsEnabled = true, SortOrder = 999 });
                        }
                    }

                    foreach (var group in sortedGroups)
                    {
                        // ✅ 修复：处理 GroupId 为 "" 或 "ungrouped" 的兼容性
                        var groupItems = eventGroupConfig.Items
                            .Where(item => 
                            {
                                if (!item.IsEnabled) return false;
                                // 精确匹配
                                if (item.GroupId == group.Id) return true;
                                // 兼容性匹配：空字符串和"ungrouped"视为等同
                                if ((string.IsNullOrEmpty(group.Id) || group.Id == "ungrouped") && 
                                    (string.IsNullOrEmpty(item.GroupId) || item.GroupId == "ungrouped"))
                                    return true;
                                return false;
                            })
                            .OrderBy(item => item.SortOrder)
                            .ToList();

                        foreach (var item in groupItems)
                        {
                            string componentType = MapEventItemToComponentType(item.Name);
                            if (!string.IsNullOrEmpty(componentType) && !componentOrder.Contains(componentType))
                            {
                                componentOrder.Add(componentType);
                                _logger?.LogInformation($"GetComponentOrderFromSettings: 从EventGroup添加组件 '{componentType}'");
                            }
                        }
                    }
                }
                else
                {
                    _logger?.LogWarning("GetComponentOrderFromSettings: EventGroup配置为空，使用默认顺序");
                    componentOrder.AddRange(new[] { "正则结果", "序号", "订单号", "材料", "数量", "工艺", "尺寸", "列组合", "行数", "列数", "材料类型" });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "GetComponentOrderFromSettings: 获取组件顺序时发生异常");
                componentOrder.AddRange(new[] { "正则结果", "序号", "订单号", "材料", "数量", "工艺", "尺寸", "列组合", "行数", "列数", "材料类型" });
            }

            if (componentOrder.Count == 0)
            {
                componentOrder.AddRange(new[] { "正则结果", "序号", "订单号", "材料", "数量", "工艺", "尺寸", "列组合", "行数", "列数", "材料类型" });
            }

            _logger?.LogInformation($"GetComponentOrderFromSettings: 最终组件顺序 = [{string.Join(", ", componentOrder)}]");
            return componentOrder;
        }

        /// <summary>
        /// 从EventGroup配置中获取前缀字典
        /// </summary>
        private Dictionary<string, string> GetPrefixesFromEventGroupConfig()
        {
            var prefixes = new Dictionary<string, string>();

            try
            {
                var eventGroupConfig = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups == null)
                {
                    _logger?.LogInformation("EventGroup配置为空，使用空前缀字典");
                    return prefixes;
                }

                foreach (var group in eventGroupConfig.Groups)
                {
                    var groupItems = eventGroupConfig.Items.Where(item => item.GroupId == group.Id);

                    foreach (var item in groupItems)
                    {
                        // ✅ Fix: Do not skip groups with empty prefixes. We need to track them to maintain order.
                        // if (string.IsNullOrEmpty(group.Prefix))
                        //    continue;
                        var prefix = group.Prefix ?? string.Empty;

                        if (!prefixes.ContainsKey(item.Name))
                        {
                            prefixes[item.Name] = prefix;
                            _logger?.LogInformation($"[GetPrefixesFromEventGroupConfig] 已添加映射: '{item.Name}' -> '{prefix}'");
                        }
                    }
                }

                _logger?.LogInformation($"获取到的前缀配置: {string.Join(", ", prefixes.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取前缀配置时发生异常");
            }

            return prefixes;
        }

        /// <summary>
        /// 获取用于Excel数据匹配的正则结果
        /// 如果文件包含保留分组前缀，则从保留数据中提取正则结果
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <returns>用于匹配的正则结果</returns>
        private string GetRegexResultForMatching(FileRenameInfo fileInfo)
        {
            if (fileInfo == null)
                return string.Empty;

            try
            {
                // 检查是否为返单文件(包含保留分组前缀)
                if (!string.IsNullOrEmpty(fileInfo.OriginalName))
                {
                    string originalNameWithoutExt = Path.GetFileNameWithoutExtension(fileInfo.OriginalName);

                    // 检测是否包含保留分组前缀模式 (如 &ID-, &MT- 等)
                    if (System.Text.RegularExpressions.Regex.IsMatch(originalNameWithoutExt, @"&[A-Z]+-"))
                    {
                        _logger?.LogDebug($"[GetRegexResultForMatching] 检测到保留分组前缀: '{originalNameWithoutExt}'");

                        // 获取保留分组配置
                        var preserveGroupConfig = GetPreserveGroupConfig();

                        if (preserveGroupConfig != null && preserveGroupConfig.Count > 0)
                        {
                            // 获取当前的前缀配置
                            var currentPrefixes = GetCurrentPrefixes();

                            // 创建临时的 FileNameComponents 对象用于提取保留数据
                            var tempComponents = new Models.FileNameComponents
                            {
                                OriginalFileName = originalNameWithoutExt,
                                PreserveGroupConfig = preserveGroupConfig,
                                Prefixes = currentPrefixes
                            };

                            // 提取保留的分组数据
                            var preserveData = tempComponents.ExtractPreserveGroupData(originalNameWithoutExt);

                            // 如果保留数据中包含正则结果，则使用保留的值
                            if (preserveData != null && preserveData.ContainsKey("正则结果"))
                            {
                                string preservedRegexResult = preserveData["正则结果"];
                                if (!string.IsNullOrEmpty(preservedRegexResult))
                                {
                                    _logger?.LogInformation($"✅ [GetRegexResultForMatching] 使用保留的正则结果: '{preservedRegexResult}'");
                                    return preservedRegexResult;
                                }
                            }
                        }
                    }
                    else
                    {
                        // ✅ 修复：无分组前缀 - 使用 cmbRegex2 (ExcelImportForm) 进行数据匹配
                        _logger?.LogDebug($"[GetRegexResultForMatching] 无分组前缀，使用 cmbRegex2: '{fileInfo.OriginalName}'");
                        string matchingResult = ExtractRegexResultForMatching(fileInfo.OriginalName);
                        if (!string.IsNullOrEmpty(matchingResult))
                        {
                            _logger?.LogInformation($"✅ [GetRegexResultForMatching] 使用 cmbRegex2 匹配结果: '{matchingResult}'");
                            return matchingResult;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"[GetRegexResultForMatching] 获取匹配正则结果时发生异常");
            }

            // 降级：使用 RegexResult (来自 _cmbRegex)
            _logger?.LogDebug($"[GetRegexResultForMatching] 降级使用 RegexResult: '{fileInfo.RegexResult}'");
            return fileInfo.RegexResult ?? string.Empty;
        }

        /// <summary>
        /// 获取当前事件分组的前缀配置
        /// </summary>
        /// <returns>前缀字典，key为组件名称（可能带[*]标记），value为前缀值</returns>
        private Dictionary<string, string> GetCurrentPrefixes()
        {
            var prefixes = new Dictionary<string, string>();

            try
            {
                var eventGroupConfig = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups == null)
                {
                    _logger?.LogDebug("[GetCurrentPrefixes] EventGroup配置为空，返回空前缀字典");
                    return prefixes;
                }

                // 遍历所有分组，构建前缀字典
                foreach (var group in eventGroupConfig.Groups)
                {
                    if (string.IsNullOrEmpty(group.Prefix))
                        continue;

                    // 找出该分组下的所有项目
                    var groupItems = eventGroupConfig.Items.Where(item => item.GroupId == group.Id);

                    foreach (var item in groupItems)
                    {
                        // 如果分组标记为保留，则组件名称需要带[*]标记
                        string componentKey = group.IsPreserved 
                            ? $"[*] {item.Name}" 
                            : item.Name;

                        prefixes[componentKey] = group.Prefix;
                        _logger?.LogDebug($"[GetCurrentPrefixes] 添加前缀映射: '{componentKey}' -> '{group.Prefix}'");
                    }
                }

                _logger?.LogDebug($"[GetCurrentPrefixes] 获取到 {prefixes.Count} 个前缀配置");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[GetCurrentPrefixes] 获取当前前缀配置时发生异常");
            }

            return prefixes;
        }

        /// <summary>
        /// 从EventGroup配置中获取保留分组配置
        /// </summary>
        /// <returns>保留分组配置字典，key为组件名称/分组ID/前缀，value为是否保留</returns>
        private Dictionary<string, bool> GetPreserveGroupConfig()
        {
            var preserveConfig = new Dictionary<string, bool>();

            try
            {
                var eventGroupConfig = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups == null)
                {
                    _logger?.LogDebug("EventGroup配置为空，使用空保留配置");
                    return preserveConfig;
                }

                // 遍历所有分组，找出标记为保留的分组
                foreach (var group in eventGroupConfig.Groups.Where(g => g.IsPreserved == true))
                {
                    // ✅ 添加分组ID到保留配置（用于ExtractPreserveGroupData的groupId检查）
                    preserveConfig[group.Id] = true;
                    _logger?.LogDebug($"[GetPreserveGroupConfig] 标记保留分组ID: '{group.Id}' (分组: {group.DisplayName})");

                    // ✅ 添加分组前缀到保留配置（用于ExtractPreserveGroupData的prefix检查）
                    if (!string.IsNullOrEmpty(group.Prefix))
                    {
                        preserveConfig[group.Prefix] = true;
                        _logger?.LogDebug($"[GetPreserveGroupConfig] 标记保留前缀: '{group.Prefix}' (分组: {group.DisplayName})");
                    }

                    // 找出该分组下的所有项目
                    var groupItems = eventGroupConfig.Items.Where(item => item.GroupId == group.Id);

                    foreach (var item in groupItems)
                    {
                        // ✅ 添加项目名称到保留配置（用于ExtractPreserveGroupData的componentType检查）
                        preserveConfig[item.Name] = true;
                        _logger?.LogDebug($"[GetPreserveGroupConfig] 标记保留组件: '{item.Name}' (分组: {group.DisplayName})");
                    }
                }

                _logger?.LogInformation($"获取到的保留分组配置: {preserveConfig.Count} 个条目 - [{string.Join(", ", preserveConfig.Keys)}]");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取保留分组配置时发生异常");
            }

            return preserveConfig;
        }

        /// <summary>
        /// 将EventGroup项目名称映射到FileNameComponents期望的组件类型
        /// </summary>
        private string MapEventItemToComponentType(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return string.Empty;

            // 处理保留分组前缀 - 移除 [*] 或 [保留] 前缀
            string cleanItemName = itemName;
            if (itemName.StartsWith("[*] "))
            {
                cleanItemName = itemName.Substring(4).Trim();
            }
            else if (itemName.StartsWith("[保留] "))
            {
                cleanItemName = itemName.Substring(5).Trim();
            }

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
                case "排版模式": // 兼容旧配置
                case "材料类型":
                    return "材料类型";
                case "正则结果":
                    return "正则结果";
                default:
                    _logger?.LogWarning($"MapEventItemToComponentType: 未知的项目名称 '{itemName}'");
                    return string.Empty;
            }
        }

        #endregion

        /// <summary>
        /// 生成新文件名
        /// </summary>
        private string GenerateNewFileName(FileRenameInfo fileInfo)
        {
            try
            {
                _logger?.LogInformation($"[GenerateNewFileName] 开始生成新文件名");
                
                var fileName = fileInfo.OriginalName ?? "";
                var regexResult = fileInfo.RegexResult ?? "";
                var serialNumber = fileInfo.SerialNumber ?? "";
                var material = fileInfo.Material ?? "";
                var quantity = fileInfo.Quantity ?? "";
                var orderNumber = fileInfo.OrderNumber ?? "";
                var dimensions = fileInfo.Dimensions ?? "";
                // 如果存在形状代号，将其追加回尺寸字符串，以便生成文件名时保持原有格式 (例如 50x50R5)
                if (!string.IsNullOrEmpty(fileInfo.Shape))
                {
                    dimensions += fileInfo.Shape;
                }
                var process = fileInfo.Process ?? "";

                _logger?.LogInformation($"[GenerateNewFileName] 原文件名: {fileName}");
                _logger?.LogInformation($"[GenerateNewFileName] 正则结果: {regexResult}");
                _logger?.LogInformation($"[GenerateNewFileName] 序号: {serialNumber}");
                _logger?.LogInformation($"[GenerateNewFileName] 材料: {material}");
                _logger?.LogInformation($"[GenerateNewFileName] 数量: {quantity}");
                _logger?.LogInformation($"[GenerateNewFileName] 列组合: {fileInfo.CompositeColumn ?? "(空)"}");

                // 确定排版模式文本
                string impositionModeValue = fileInfo.ImpositionMode ?? "";
                // 如果 fileInfo 中没有，尝试使用当前状态计算（兼容旧逻辑）
                if (string.IsNullOrEmpty(impositionModeValue))
                {
                    impositionModeValue = GetImpositionModeString();
                }

                _logger?.LogInformation($"[GenerateNewFileName] 排版模式值: '{impositionModeValue}' (启用: {_currentEnableImposition}, 模式: {_currentImpositionMaterialType})");

                // ✅ 调试：如果没有Excel数据但启用了列组合功能，提供测试数据
                if (string.IsNullOrEmpty(fileInfo.CompositeColumn) && AppSettings.EnableColumnCombine)
                {
                    _logger?.LogInformation("[GenerateNewFileName] 检测到启用列组合但无数据，建议用户导入Excel数据或检查列组合配置");
                }

                // ✅ 使用与 Form1Presenter 一致的完整实现
                var enabledConfig = CreateFileNameComponentsConfigFromEventGroup();
                var componentOrder = GetComponentOrderFromSettings();
                var prefixes = GetPrefixesFromEventGroupConfig();
                var preserveGroupConfig = GetPreserveGroupConfig();  // ✅ 新增：获取保留分组配置

                var components = new WindowsFormsApp3.Models.FileNameComponents
                {
                    FileExtension = Path.GetExtension(fileName),
                    Separator = AppSettings.Separator ?? "",
                    RegexResult = regexResult,
                    SerialNumber = serialNumber,
                    OrderNumber = orderNumber,
                    Material = material,
                    Quantity = quantity,
                    Unit = AppSettings.Unit ?? "",
                    Process = process,
                    Dimensions = dimensions,
                    // ✅ 修复：添加行数和列数
                    LayoutRows = fileInfo.LayoutRows ?? "",
                    LayoutColumns = fileInfo.LayoutColumns ?? "",
                    // ✅ 修复：添加列组合数据
                    CompositeColumn = fileInfo.CompositeColumn ?? "",
                    // ✅ 修复：添加排版模式数据
                    ImpositionMode = impositionModeValue,
                    EnabledComponents = enabledConfig,
                    ComponentOrder = componentOrder,
                    Prefixes = prefixes,
                    PreserveGroupConfig = preserveGroupConfig,  // ✅ 新增：设置保留分组配置
                    OriginalFileName = Path.GetFileNameWithoutExtension(fileName)
                };

                // 确保分隔符为合法文件名字符
                if (!string.IsNullOrEmpty(components.Separator) && Path.GetInvalidFileNameChars().Contains(components.Separator[0]))
                {
                    _logger?.LogInformation($"[GenerateNewFileName] 分隔符 '{components.Separator}' 包含非法字符，替换为 '_'");
                    components.Separator = "_";
                }
                
                // 构建文件名
                string newFileName = components.BuildFileName();
                _logger?.LogInformation($"[GenerateNewFileName] 生成的新文件名: {newFileName}");
                
                if (string.IsNullOrEmpty(newFileName) || newFileName == components.FileExtension)
                {
                    return $"未命名{components.FileExtension}";
                }
                
                return newFileName;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成新文件名失败");
                return fileInfo.OriginalName ?? "";
            }
        }

        /// <summary>
        /// 解析尺寸字符串，分离尺寸和形状代号
        /// </summary>
        /// <param name="fullDimensions">完整尺寸字符串 (例如 50x50R5)</param>
        /// <returns>分离后的元组 (Dimensions, Shape)</returns>
        private (string Dimension, string Shape) ParseDimensionsAndShape(string fullDimensions)
        {
            if (string.IsNullOrEmpty(fullDimensions)) return (fullDimensions, "");

            // 匹配标准格式: WxH[ShapeCode]
            // 例如: 54x85, 54x85R5, 54x85Z, 54x85C
            // 正则: ^(\d+(\.\d+)?)x(\d+(\.\d+)?)(.*)$
            var match = System.Text.RegularExpressions.Regex.Match(fullDimensions, @"^([\d\.]+[xX][\d\.]+)(.*)$");
            if (match.Success)
            {
                return (match.Groups[1].Value, match.Groups[2].Value);
            }
            return (fullDimensions, "");
        }

        #endregion
    }
}
