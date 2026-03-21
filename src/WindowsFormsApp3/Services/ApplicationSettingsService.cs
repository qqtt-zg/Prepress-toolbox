using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 应用程序设置服务 - 替代Properties.Settings
    /// 统一管理所有应用程序配置，存储到application_settings.json中
    /// </summary>
    public class ApplicationSettingsService
    {
        private readonly ILogger _logger;
        private readonly string _settingsFilePath;
        private ApplicationSettings _settings;
        private readonly object _lock = new object();
        private bool _hasPendingChanges = false;
        private readonly object _pendingLock = new object();
        private readonly System.Timers.Timer _autoSaveTimer;

        /// <summary>
        /// 应用程序设置模型
        /// </summary>
        public class ApplicationSettings
        {
            // 基础配置
            public string LastInputDir { get; set; } = "";
            public List<string> InputDirHistory { get; set; } = new List<string>();
            public string LastOutputDir { get; set; } = "";
            public string RegexPatterns { get; set; } = "";
            public string Materials { get; set; } = "";
            public string Unit { get; set; } = "";
            public string Material { get; set; } = "";
            public double Opacity { get; set; } = 1.0;
            public string FixedFieldPresets { get; set; } = "";
            public string FixedField { get; set; } = "";
            public List<string> ExportPaths { get; set; } = new List<string>();

            // 界面状态
            public string LastSelectedRegex { get; set; } = "";
            public bool RegexResultChecked { get; set; } = false;
            public bool DisableRegexChecked { get; set; } = false;
            public string RenameRulesOrder { get; set; } = "";
            public string Separator { get; set; } = "_";
            public string EventItems { get; set; } = "";
            public string TetBleedValues { get; set; } = "2,1.5,0";
            public string ToggleMinimizeHotkey { get; set; } = "";
            public string LastExportPath { get; set; } = "";
            public string LastUsedConfigName { get; set; } = "默认配置";

            // Excel配置
            public string ExcelSerialColumnParams { get; set; } = "序号,编号";
            public string ExcelSearchColumnParams { get; set; } = "名称,物料名";
            public string ExcelReturnColumnParams { get; set; } = "数量,Qty";

            // 序号管理
            public bool AutoIncrementOrderNumber1 { get; set; } = false;
            public bool AutoIncrementOrderNumber2 { get; set; } = false;
            public string LastOrderNumber1 { get; set; } = "";
            public string LastOrderNumber2 { get; set; } = "0";
            public string LastSelectedMaterial { get; set; } = "";
            public string LastIncrementValue { get; set; } = "0";

            // PDF处理
            public string LastSelectedRegex2 { get; set; } = "";
            public string ExtractNumberKeywords { get; set; } = "";
            public string LastCornerRadius { get; set; } = "0";
            public bool UsePdfLastPage { get; set; } = false;
            public string LastSelectedTetBleed { get; set; } = "0";

            // 列组合
            public string CompositeColumns { get; set; } = "";
            public string CompositeColumnSeparator { get; set; } = ",";

            // 材料选择
            public string LastColorMode { get; set; } = "彩色";
            public string LastFilmType { get; set; } = "光膜";
            public string LastRoundedRadiusValue { get; set; } = "";

            // 排版功能控件状态持久化
            public bool EnableImpositionChecked { get; set; } = false;
            public string LastMaterialType { get; set; } = "平张";
            public string LastLayoutMode { get; set; } = "连拼模式";
            public string RollRotationMode { get; set; } = "Auto";

            // ... existing code ...
            // 是否隐藏半径数值
            public bool HideRadiusValue { get; set; } = false;

            // EventItems预设配置
            public EventItemsPresets EventItemsPresets { get; set; } = new EventItemsPresets();

            // 窗口位置配置（MaterialSelectFormModern专用）
            public int MaterialFormX { get; set; } = -1;
            public int MaterialFormY { get; set; } = -1;
            public int MaterialFormWidth { get; set; } = -1;
            public int MaterialFormHeight { get; set; } = -1;
            public bool MaterialFormMaximized { get; set; } = false;
            public FormWindowState MaterialFormWindowState { get; set; } = FormWindowState.Normal;
            public bool MaterialFormPreviewExpanded { get; set; } = false; // PDF预览状态

            // 材料选择预设
            public List<Models.MaterialSelectionPreset> MaterialPresets { get; set; } = new List<Models.MaterialSelectionPreset>();
            public string LastUsedMaterialPreset { get; set; } = "";

            // 正则表达式变化自动刷新配置
            public bool AutoRefreshFileNameOnRegexChange { get; set; } = false;

            // 动态设置（不在Properties.Settings中定义的）
            public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
        }

        public ApplicationSettingsService(ILogger logger)
        {
            _logger = logger;
            _settingsFilePath = Path.Combine(AppDataPathManager.AppRootDirectory, "application_settings.json");
            EnsureSettingsDirectory();
            LoadSettingsInternal();

            // 初始化自动保存定时器（5秒后自动保存未提交的更改）
            _autoSaveTimer = new System.Timers.Timer(5000);
            _autoSaveTimer.Elapsed += AutoSaveTimer_Elapsed;
            _autoSaveTimer.AutoReset = false;
        }

        /// <summary>
        /// 确保设置目录存在
        /// </summary>
        private void EnsureSettingsDirectory()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建设置目录失败");
                throw;
            }
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        /// <summary>
        /// 加载设置
        /// </summary>
        public static ApplicationSettings LoadSettings()
        {
            // 使用默认的日志实现
            var logger = new FileLogger();
            var service = new ApplicationSettingsService(logger);
            return service.LoadSettingsInternal();
        }

        internal ApplicationSettings LoadSettingsInternal()
        {
            try
            {
                lock (_lock)
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        var json = File.ReadAllText(_settingsFilePath);
                        _settings = JsonConvert.DeserializeObject<ApplicationSettings>(json) ?? new ApplicationSettings();
                        _logger.LogDebug($"[LoadSettingsInternal] 从文件加载: Material='{_settings.Material}', ToggleMinimizeHotkey='{_settings.ToggleMinimizeHotkey}'");
                    }
                    else
                    {
                        _settings = new ApplicationSettings();
                        _logger.LogDebug("[LoadSettingsInternal] 文件不存在，创建新设置");
                        SaveSettingsInternal(_settings);
                    }

                    // 初始化EventItems预设（如果为空）
                    if (_settings.EventItemsPresets == null)
                    {
                        _settings.EventItemsPresets = new EventItemsPresets();
                    }
                    _settings.EventItemsPresets.InitializeDefaultPresets();
                }
                _logger.LogInformation("应用程序设置加载成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载应用程序设置失败");
                _settings = new ApplicationSettings();
            }
            return _settings;
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        /// <summary>
        /// 保存设置
        /// </summary>
        public static void SaveSettings(ApplicationSettings settings)
        {
            // 使用默认的日志实现
            var logger = new FileLogger();
            var service = new ApplicationSettingsService(logger);
            service.SaveSettingsInternal(settings);
        }

        /// <summary>
        /// 保存当前实例的设置
        /// </summary>
        public void Save()
        {
            SaveSettingsInternal(_settings);
        }

        internal void SaveSettingsInternal(ApplicationSettings settings)
        {
            try
            {
                lock (_lock)
                {
                    _settings = settings;
                    _logger.LogDebug($"[SaveSettingsInternal] Material='{_settings.Material}', Separator='{_settings.Separator}', ToggleMinimizeHotkey='{_settings.ToggleMinimizeHotkey}'");
                    var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                    File.WriteAllText(_settingsFilePath, json);
                    _logger.LogDebug($"[SaveSettingsInternal] 已保存到文件: {_settingsFilePath}");
                }
                _logger.LogDebug("应用程序设置保存成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存应用程序设置失败");
                throw;
            }
        }

    
        /// <summary>
        /// 标记设置已更改并启动自动保存定时器
        /// </summary>
        private void MarkAsChanged()
        {
            lock (_pendingLock)
            {
                _hasPendingChanges = true;
                // 重置自动保存定时器
                ResetAutoSaveTimer();
            }
        }

        #region 属性访问器

        public string LastInputDir
        {
            get => _settings.LastInputDir;
            set { _settings.LastInputDir = value; MarkAsChanged(); }
        }

        /// <summary>
        /// 输入目录历史记录（最近5次）
        /// </summary>
        public List<string> InputDirHistory
        {
            get => _settings.InputDirHistory;
            set { _settings.InputDirHistory = value; MarkAsChanged(); }
        }

        /// <summary>
        /// 添加输入目录到历史记录（保留最近5次）
        /// </summary>
        public void AddInputDirToHistory(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath)) return;

            // 移除重复项
            _settings.InputDirHistory.Remove(dirPath);

            // 添加到开头
            _settings.InputDirHistory.Insert(0, dirPath);

            // 保留最近5次
            if (_settings.InputDirHistory.Count > 5)
            {
                _settings.InputDirHistory = _settings.InputDirHistory.Take(5).ToList();
            }

            MarkAsChanged();
        }

        /// <summary>
        /// 正则表达式变化时自动刷新文件名
        /// </summary>
        public bool AutoRefreshFileNameOnRegexChange
        {
            get => _settings.AutoRefreshFileNameOnRegexChange;
            set { _settings.AutoRefreshFileNameOnRegexChange = value; MarkAsChanged(); }
        }

        public string LastOutputDir
        {
            get => _settings.LastOutputDir;
            set { _settings.LastOutputDir = value; MarkAsChanged(); }
        }

        public string RegexPatterns
        {
            get => _settings.RegexPatterns;
            set { _settings.RegexPatterns = value; MarkAsChanged(); }
        }

        public string Materials
        {
            get => _settings.Materials;
            set { _settings.Materials = value; MarkAsChanged(); }
        }

        public string Unit
        {
            get => _settings.Unit;
            set { _settings.Unit = value; MarkAsChanged(); }
        }

        public string Material
        {
            get => _settings.Material;
            set { _settings.Material = value; MarkAsChanged(); }
        }

        public double Opacity
        {
            get => _settings.Opacity;
            set { _settings.Opacity = value; MarkAsChanged(); }
        }

        public string Separator
        {
            get => _settings.Separator;
            set { _settings.Separator = value; MarkAsChanged(); }
        }

        public string EventItems
        {
            get => _settings.EventItems;
            set { _settings.EventItems = value; MarkAsChanged(); }
        }

        public string TetBleedValues
        {
            get => _settings.TetBleedValues;
            set { _settings.TetBleedValues = value; MarkAsChanged(); }
        }

        public string ToggleMinimizeHotkey
        {
            get => _settings.ToggleMinimizeHotkey;
            set { _settings.ToggleMinimizeHotkey = value; MarkAsChanged(); }
        }

        // 窗口位置配置（MaterialSelectFormModern专用）
        public int MaterialFormX
        {
            get => _settings.MaterialFormX;
            set { _settings.MaterialFormX = value; MarkAsChanged(); }
        }

        public int MaterialFormY
        {
            get => _settings.MaterialFormY;
            set { _settings.MaterialFormY = value; MarkAsChanged(); }
        }

        public int MaterialFormWidth
        {
            get => _settings.MaterialFormWidth;
            set { _settings.MaterialFormWidth = value; MarkAsChanged(); }
        }

        public int MaterialFormHeight
        {
            get => _settings.MaterialFormHeight;
            set { _settings.MaterialFormHeight = value; MarkAsChanged(); }
        }

        public bool MaterialFormMaximized
        {
            get => _settings.MaterialFormMaximized;
            set { _settings.MaterialFormMaximized = value; MarkAsChanged(); }
        }

        public bool MaterialFormPreviewExpanded
        {
            get => _settings.MaterialFormPreviewExpanded;
            set { _settings.MaterialFormPreviewExpanded = value; MarkAsChanged(); }
        }

        public List<Models.MaterialSelectionPreset> MaterialPresets
        {
            get => _settings.MaterialPresets;
            set { _settings.MaterialPresets = value; MarkAsChanged(); }
        }

        public string LastUsedMaterialPreset
        {
            get => _settings.LastUsedMaterialPreset;
            set { _settings.LastUsedMaterialPreset = value; MarkAsChanged(); }
        }

        public string LastExportPath
        {
            get => _settings.LastExportPath;
            set { _settings.LastExportPath = value; MarkAsChanged(); }
        }

        public string LastUsedConfigName
        {
            get => _settings.LastUsedConfigName;
            set { _settings.LastUsedConfigName = value; MarkAsChanged(); }
        }

    
        public string LastColorMode
        {
            get => _settings.LastColorMode;
            set { _settings.LastColorMode = value; MarkAsChanged(); }
        }

        public string LastFilmType
        {
            get => _settings.LastFilmType;
            set { _settings.LastFilmType = value; MarkAsChanged(); }
        }

        public string LastSelectedRegex
        {
            get => _settings.LastSelectedRegex;
            set { _settings.LastSelectedRegex = value; MarkAsChanged(); }
        }

        public string ExtractNumberKeywords
        {
            get => _settings.ExtractNumberKeywords;
            set { _settings.ExtractNumberKeywords = value; MarkAsChanged(); }
        }

        public List<string> ExportPaths
        {
            get => _settings.ExportPaths;
            set { _settings.ExportPaths = value; MarkAsChanged(); }
        }

        // 排版功能控件状态持久化
        public bool EnableImpositionChecked
        {
            get => _settings.EnableImpositionChecked;
            set { _settings.EnableImpositionChecked = value; MarkAsChanged(); }
        }

        public string LastMaterialType
        {
            get => _settings.LastMaterialType;
            set { _settings.LastMaterialType = value; MarkAsChanged(); }
        }

        public string LastLayoutMode
        {
            get => _settings.LastLayoutMode;
            set { _settings.LastLayoutMode = value; MarkAsChanged(); }
        }

        public string RollRotationMode
        {
            get => _settings.RollRotationMode;
            set { _settings.RollRotationMode = value; MarkAsChanged(); }
        }

        // ... existing code ...
        /// <summary>
        /// 是否隐藏半径数值
        /// </summary>
        public bool HideRadiusValue
        {
            get => _settings.HideRadiusValue;
            set { _settings.HideRadiusValue = value; MarkAsChanged(); }
        }
        public string ExcelSerialColumnParams
        {
            get => _settings.ExcelSerialColumnParams;
            set { _settings.ExcelSerialColumnParams = value; MarkAsChanged(); }
        }

        public string ExcelSearchColumnParams
        {
            get => _settings.ExcelSearchColumnParams;
            set { _settings.ExcelSearchColumnParams = value; MarkAsChanged(); }
        }

        public string ExcelReturnColumnParams
        {
            get => _settings.ExcelReturnColumnParams;
            set { _settings.ExcelReturnColumnParams = value; MarkAsChanged(); }
        }

        // 通用设置访问器
        public object this[string key]
        {
            get
            {
                if (_settings.CustomSettings.TryGetValue(key, out var value))
                {
                    _logger.LogDebug($"[AppSettings.Indexer] 从CustomSettings获取 {key}: {value}");
                    return value;
                }

                // 尝试从已定义的属性中获取
                var property = typeof(ApplicationSettings).GetProperty(key);
                var propertyValue = property?.GetValue(_settings);
                _logger.LogDebug($"[AppSettings.Indexer] 从属性获取 {key}: {propertyValue}");
                return propertyValue;
            }
            set
            {
                var property = typeof(ApplicationSettings).GetProperty(key);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(_settings, value);
                }
                else
                {
                    _settings.CustomSettings[key] = value;
                }
                MarkAsChanged();
            }
        }

        /// <summary>
        /// 获取设置值
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                var value = this[key];
                if (value == null) return defaultValue;
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 设置值
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            this[key] = value;
        }

        #endregion

        /// <summary>
        /// 备份设置
        /// </summary>
        public void BackupSettings(string backupName = null)
        {
            try
            {
                var backupFileName = backupName ?? $"settings_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var backupPath = Path.Combine(
                    Path.GetDirectoryName(_settingsFilePath),
                    "backups",
                    backupFileName);

                Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                File.Copy(_settingsFilePath, backupPath, true);

                _logger.LogInformation($"设置已备份到: {backupPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "备份设置失败");
                throw;
            }
        }

        /// <summary>
        /// 恢复设置
        /// </summary>
        public void RestoreSettings(string backupFileName)
        {
            try
            {
                var backupPath = Path.Combine(
                    Path.GetDirectoryName(_settingsFilePath),
                    "backups",
                    backupFileName);

                if (!File.Exists(backupPath))
                {
                    throw new FileNotFoundException($"备份文件不存在: {backupPath}");
                }

                // 先备份当前设置
                BackupSettings("before_restore");

                // 恢复设置
                File.Copy(backupPath, _settingsFilePath, true);
                LoadSettingsInternal();

                _logger.LogInformation($"设置已从备份恢复: {backupPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复设置失败");
                throw;
            }
        }

        /// <summary>
        /// 自动保存定时器事件处理
        /// </summary>
        private void AutoSaveTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (_hasPendingChanges)
                {
                    _logger.LogDebug("自动保存定时器触发，保存待处理的更改");
                    CommitChanges();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "自动保存失败");
            }
        }

        /// <summary>
        /// 开始批量编辑（延迟保存）
        /// </summary>
        public void BeginBatchEdit()
        {
            lock (_pendingLock)
            {
                _hasPendingChanges = true;
                // 停止自动保存定时器
                _autoSaveTimer.Stop();
            }
        }

        /// <summary>
        /// 提交所有待处理的更改
        /// </summary>
        public void CommitChanges()
        {
            lock (_pendingLock)
            {
                if (_hasPendingChanges)
                {
                    SaveSettingsInternal(_settings);
                    _hasPendingChanges = false;
                    _logger.LogDebug("已提交所有待处理的配置更改");
                }
                // 停止自动保存定时器
                _autoSaveTimer.Stop();
            }
        }

        /// <summary>
        /// 取消所有待处理的更改
        /// </summary>
        public void RollbackChanges()
        {
            lock (_pendingLock)
            {
                if (_hasPendingChanges)
                {
                    LoadSettingsInternal();
                    _hasPendingChanges = false;
                    _logger.LogDebug("已回滚所有待处理的配置更改");
                }
                // 停止自动保存定时器
                _autoSaveTimer.Stop();
            }
        }

        /// <summary>
        /// 重置自动保存定时器
        /// </summary>
        private void ResetAutoSaveTimer()
        {
            lock (_pendingLock)
            {
                if (_hasPendingChanges)
                {
                    _autoSaveTimer.Stop();
                    _autoSaveTimer.Start();
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _autoSaveTimer?.Stop();
            _autoSaveTimer?.Dispose();
            // 如果有待处理的更改，立即保存
            if (_hasPendingChanges)
            {
                CommitChanges();
            }
        }

        #region EventItems预设管理

        /// <summary>
        /// 加载EventItems预设
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <returns>是否加载成功</returns>
        public bool LoadEventItemsPreset(string presetName)
        {
            try
            {
                var presetValue = _settings.EventItemsPresets.GetPresetEventItems(presetName);
                if (presetValue != null)
                {
                    // 检查是否为JSON格式（EventGroupConfiguration）
                    if (presetValue.TrimStart().StartsWith("{"))
                    {
                        // 这是EventGroupConfiguration格式，保存到CustomSettings
                        this["EventGroupConfiguration"] = presetValue;
                        _logger.LogDebug($"已加载EventGroupConfiguration预设: {presetName}");
                    }
                    else
                    {
                        // 这是旧的EventItems格式
                        _settings.EventItems = presetValue;
                        _logger.LogDebug($"已加载EventItems预设: {presetName}");
                    }

                    _settings.EventItemsPresets.LastUsedPreset = presetName;
                    MarkAsChanged();
                    return true;
                }
                _logger.LogWarning($"预设不存在: {presetName}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加载EventItems预设失败: {presetName}");
                return false;
            }
        }

        /// <summary>
        /// 保存当前EventItems为新预设
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <returns>是否保存成功</returns>
        public bool SaveEventItemsAsPreset(string presetName, string eventGroupConfiguration = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(presetName))
                {
                    _logger.LogWarning("预设名称不能为空");
                    return false;
                }

                // 优先使用传入的EventGroupConfiguration，如果没有则使用旧的EventItems
                if (!string.IsNullOrWhiteSpace(eventGroupConfiguration))
                {
                    _settings.EventItemsPresets.Presets[presetName] = eventGroupConfiguration;
                    _logger.LogDebug($"已保存EventGroupConfiguration预设: {presetName}");
                }
                else
                {
                    _settings.EventItemsPresets.Presets[presetName] = _settings.EventItems;
                    _logger.LogDebug($"已保存EventItems预设: {presetName}");
                }

                _settings.EventItemsPresets.LastUsedPreset = presetName;
                MarkAsChanged();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"保存EventItems预设失败: {presetName}");
                return false;
            }
        }

        /// <summary>
        /// 删除EventItems预设
        /// </summary>
        /// <param name="presetName">预设名称</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteEventItemsPreset(string presetName)
        {
            try
            {
                // 不允许删除内置预设
                if (_settings.EventItemsPresets.IsBuiltInPreset(presetName))
                {
                    _logger.LogWarning($"内置预设不能删除: {presetName}");
                    return false;
                }

                if (_settings.EventItemsPresets.Presets.Remove(presetName))
                {
                    // 如果删除的是当前使用的预设，切换到默认预设
                    if (_settings.EventItemsPresets.LastUsedPreset == presetName)
                    {
                        LoadEventItemsPreset("默认方案");
                    }
                    MarkAsChanged();
                    _logger.LogDebug($"已删除EventItems预设: {presetName}");
                    return true;
                }
                _logger.LogWarning($"预设不存在: {presetName}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"删除EventItems预设失败: {presetName}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有EventItems预设名称
        /// </summary>
        /// <returns>预设名称列表</returns>
        public List<string> GetEventItemsPresetNames()
        {
            return new List<string>(_settings.EventItemsPresets.Presets.Keys);
        }

        /// <summary>
        /// 获取当前使用的预设名称
        /// </summary>
        /// <returns>预设名称</returns>
        public string GetCurrentPresetName()
        {
            return _settings.EventItemsPresets.LastUsedPreset;
        }

        /// <summary>
        /// 清理旧的预设配置，只保留默认配置
        /// </summary>
        /// <returns>是否清理成功</returns>
        public bool CleanupOldPresets()
        {
            try
            {
                var presetsToRemove = new List<string>();

                // 识别需要删除的旧预设
                foreach (var presetName in _settings.EventItemsPresets.Presets.Keys)
                {
                    if (presetName != "全功能配置")
                    {
                        presetsToRemove.Add(presetName);
                    }
                }

                // 删除旧预设
                foreach (var presetName in presetsToRemove)
                {
                    _settings.EventItemsPresets.Presets.Remove(presetName);
                    _logger.LogDebug($"已删除旧预设: {presetName}");
                }

                // 确保默认配置存在
                if (!_settings.EventItemsPresets.Presets.ContainsKey("默认配置"))
                {
                    // 如果没有默认配置，创建一个
                    var defaultConfig = CreateDefaultEventGroupConfiguration();
                    _settings.EventItemsPresets.Presets["默认配置"] = defaultConfig;
                    _logger.LogDebug("已创建默认配置");
                }

                // 设置当前使用的预设为默认配置
                _settings.EventItemsPresets.LastUsedPreset = "默认配置";

                if (presetsToRemove.Count > 0)
                {
                    MarkAsChanged();
                    _logger.LogInformation($"已清理 {presetsToRemove.Count} 个旧预设配置，只保留默认配置");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理旧预设配置失败");
                return false;
            }
        }

        /// <summary>
        /// 创建默认的EventGroupConfiguration
        /// </summary>
        /// <returns>默认配置的JSON字符串</returns>
        private string CreateDefaultEventGroupConfiguration()
        {
            var defaultConfig = new Models.EventGroupConfiguration
            {
                Groups = new List<Models.EventGroup>
                {
                    new Models.EventGroup { Id = "order", DisplayName = "订单组", Prefix = "&ID-", IsEnabled = true, SortOrder = 1 },
                    new Models.EventGroup { Id = "material", DisplayName = "材料组", Prefix = "&MT-", IsEnabled = true, SortOrder = 2 },
                    new Models.EventGroup { Id = "quantity", DisplayName = "数量组", Prefix = "&DN-", IsEnabled = true, SortOrder = 3 },
                    new Models.EventGroup { Id = "process", DisplayName = "工艺组", Prefix = "&DP-", IsEnabled = true, SortOrder = 4 },
                    new Models.EventGroup { Id = "customer", DisplayName = "客户组", Prefix = "&CU-", IsEnabled = true, SortOrder = 5 },
                    new Models.EventGroup { Id = "remark", DisplayName = "备注组", Prefix = "&MK-", IsEnabled = true, SortOrder = 6 },
                    new Models.EventGroup { Id = "row", DisplayName = "行数组", Prefix = "&Row-", IsEnabled = true, SortOrder = 7 },
                    new Models.EventGroup { Id = "column", DisplayName = "列数组", Prefix = "&Col-", IsEnabled = true, SortOrder = 8 }
                },
                Items = new List<Models.EventItem>
                {
                    new Models.EventItem { Name = "正则结果", GroupId = "", IsEnabled = true, SortOrder = 1 },
                    new Models.EventItem { Name = "订单号", GroupId = "order", IsEnabled = true, SortOrder = 1 },
                    new Models.EventItem { Name = "材料", GroupId = "material", IsEnabled = true, SortOrder = 1 },
                    new Models.EventItem { Name = "数量", GroupId = "quantity", IsEnabled = true, SortOrder = 1 },
                    new Models.EventItem { Name = "工艺", GroupId = "process", IsEnabled = true, SortOrder = 1 },
                    new Models.EventItem { Name = "尺寸", GroupId = "", IsEnabled = true, SortOrder = 2 },
                    new Models.EventItem { Name = "序号", GroupId = "", IsEnabled = false, SortOrder = 3 },
                    new Models.EventItem { Name = "列组合", GroupId = "", IsEnabled = true, SortOrder = 4 },
                    new Models.EventItem { Name = "行数", GroupId = "row", IsEnabled = true, SortOrder = 1 },
                    new Models.EventItem { Name = "列数", GroupId = "column", IsEnabled = true, SortOrder = 1 }
                },
                Version = "1.0"
            };

            return JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
        }

        /// <summary>
        /// 强制重新加载CustomSettings部分
        /// </summary>
        public void ReloadCustomSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settingsFromDisk = JsonConvert.DeserializeObject<ApplicationSettings>(json);

                    if (settingsFromDisk != null)
                    {
                        // 同时重新加载基本属性，包括 Separator
                        _settings.Separator = settingsFromDisk.Separator;
                        _settings.Unit = settingsFromDisk.Unit;
                        _settings.Material = settingsFromDisk.Material;
                        _settings.TetBleedValues = settingsFromDisk.TetBleedValues;
                        _settings.ToggleMinimizeHotkey = settingsFromDisk.ToggleMinimizeHotkey;
                        // ... existing code ...
                        // ✅ 修复：也重新加载HideRadiusValue
                        _settings.HideRadiusValue = settingsFromDisk.HideRadiusValue;

                        _logger.LogDebug($"[ReloadCustomSettings] 重新加载基本属性: Separator='{_settings.Separator}', Unit='{_settings.Unit}', Material='{_settings.Material}', HideRadiusValue={_settings.HideRadiusValue}");
                    }

                    if (settingsFromDisk?.CustomSettings != null)
                    {
                        _settings.CustomSettings.Clear();
                        foreach (var kvp in settingsFromDisk.CustomSettings)
                        {
                            // ... existing code ...
                            // ✅ 修复：跳过旧的HideRadiusValue（现在使用根级属性）
                            if (kvp.Key == "HideRadiusValue")
                                continue;
                            
                            _settings.CustomSettings[kvp.Key] = kvp.Value;
                        }

                        _logger.LogDebug("[ReloadCustomSettings] 已从文件重新加载CustomSettings");

                        // 打印重新加载的形状代号
                        var zeroShapeCode = _settings.CustomSettings.TryGetValue("ZeroShapeCode", out var zsc) ? zsc.ToString() : "未找到";
                        var roundShapeCode = _settings.CustomSettings.TryGetValue("RoundShapeCode", out var rsc) ? rsc.ToString() : "未找到";
                        var ellipseShapeCode = _settings.CustomSettings.TryGetValue("EllipseShapeCode", out var esc) ? esc.ToString() : "未找到";
                        var circleShapeCode = _settings.CustomSettings.TryGetValue("CircleShapeCode", out var csc) ? csc.ToString() : "未找到";

                        _logger.LogDebug($"[ReloadCustomSettings] 重新加载后的形状代号: ZeroShapeCode='{zeroShapeCode}', RoundShapeCode='{roundShapeCode}', EllipseShapeCode='{ellipseShapeCode}', CircleShapeCode='{circleShapeCode}'");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReloadCustomSettings] 重新加载CustomSettings失败");
            }
        }

        /// <summary>
        /// 重新加载所有设置（包括根级属性和CustomSettings）
        /// </summary>
        public void ReloadAllSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settingsFromDisk = JsonConvert.DeserializeObject<ApplicationSettings>(json);

                    if (settingsFromDisk != null)
                    {
                        // 重新加载根级属性（直接操作字段避免触发自动保存）
                        _logger.LogDebug($"[ReloadAllSettings] 从文件加载的Separator: '{settingsFromDisk.Separator}'");
                        
                        // 完全按照用户设置为准，不使用默认值替代
                        _settings.Separator = settingsFromDisk.Separator;
                        _logger.LogDebug($"[ReloadAllSettings] 设置后的Separator: '{_settings.Separator}'");
                        
                        _settings.Unit = settingsFromDisk.Unit ?? "";
                        _settings.Material = settingsFromDisk.Material ?? "";
                        _settings.EventItems = settingsFromDisk.EventItems ?? "";
                        _settings.TetBleedValues = settingsFromDisk.TetBleedValues ?? "2,1.5,0";
                        // ... existing code ...
                        // ✅ 修复：也重新加载HideRadiusValue
                        _settings.HideRadiusValue = settingsFromDisk.HideRadiusValue;

                        // 🔧 关键修复：重新加载窗口位置相关属性
                        _settings.MaterialFormX = settingsFromDisk.MaterialFormX;
                        _settings.MaterialFormY = settingsFromDisk.MaterialFormY;
                        _settings.MaterialFormWidth = settingsFromDisk.MaterialFormWidth;
                        _settings.MaterialFormHeight = settingsFromDisk.MaterialFormHeight;
                        _settings.MaterialFormMaximized = settingsFromDisk.MaterialFormMaximized;
                        _settings.MaterialFormWindowState = settingsFromDisk.MaterialFormWindowState;
                        _settings.MaterialFormPreviewExpanded = settingsFromDisk.MaterialFormPreviewExpanded;
                        _logger.LogDebug($"[ReloadAllSettings] 已重新加载窗口位置: X={_settings.MaterialFormX}, Y={_settings.MaterialFormY}, Width={_settings.MaterialFormWidth}, Height={_settings.MaterialFormHeight}, Maximized={_settings.MaterialFormMaximized}, PreviewExpanded={_settings.MaterialFormPreviewExpanded}");

                        // 重新加载CustomSettings
                        if (settingsFromDisk.CustomSettings != null)
                        {
                            _settings.CustomSettings.Clear();
                            foreach (var kvp in settingsFromDisk.CustomSettings)
                            {
                                // ... existing code ...
                                // ✅ 修复：跳过旧的HideRadiusValue（现在使用根级属性）
                                if (kvp.Key == "HideRadiusValue")
                                    continue;
                                
                                _settings.CustomSettings[kvp.Key] = kvp.Value;
                            }
                        }

                        _logger.LogDebug("[ReloadAllSettings] 已从文件重新加载所有设置");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ReloadAllSettings] 重新加载所有设置失败");
            }
        }

        #endregion
    }
}