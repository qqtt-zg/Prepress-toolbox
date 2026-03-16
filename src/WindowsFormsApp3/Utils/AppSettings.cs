using System;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Interfaces;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// 应用程序设置的静态访问器
    /// 提供类似Properties.Settings的访问方式，但使用configs.json存储
    /// </summary>
    public static class AppSettings
    {
        private static ApplicationSettingsService _settingsService;
        private static readonly object _lock = new object();

        /// <summary>
        /// 初始化设置服务
        /// </summary>
        public static void Initialize(ILogger logger)
        {
            lock (_lock)
            {
                if (_settingsService == null)
                {
                    _settingsService = new ApplicationSettingsService(logger);
                }
            }
        }

        /// <summary>
        /// 获取设置服务实例
        /// </summary>
        public static ApplicationSettingsService Instance
        {
            get
            {
                if (_settingsService == null)
                {
                    throw new InvalidOperationException("AppSettings未初始化，请先调用Initialize方法");
                }
                return _settingsService;
            }
        }

        /// <summary>
        /// 保存所有设置
        /// </summary>
        public static void Save()
        {
            // 保存当前Instance中的设置，而不是重新加载
            Instance.Save();
        }

        /// <summary>
        /// 立即提交所有待处理的设置更改到文件
        /// </summary>
        public static void CommitChanges()
        {
            Instance.CommitChanges();
        }

        #region 常用设置快捷访问

        public static string LastInputDir
        {
            get => Instance.LastInputDir;
            set => Instance.LastInputDir = value;
        }

        public static string LastOutputDir
        {
            get => Instance.LastOutputDir;
            set => Instance.LastOutputDir = value;
        }

        public static string RegexPatterns
        {
            get => Instance.RegexPatterns;
            set => Instance.RegexPatterns = value;
        }

        public static string Materials
        {
            get => Instance.Materials;
            set => Instance.Materials = value;
        }

        public static string Unit
        {
            get => Instance.Unit;
            set => Instance.Unit = value;
        }

        public static string Material
        {
            get => Instance.Material;
            set => Instance.Material = value;
        }

        public static double Opacity
        {
            get => Instance.Opacity;
            set => Instance.Opacity = value;
        }

        public static string Separator
        {
            get => Instance.Separator;
            set => Instance.Separator = value;
        }

        public static string EventItems
        {
            get => Instance.EventItems;
            set => Instance.EventItems = value;
        }

        public static string TetBleedValues
        {
            get => Instance.TetBleedValues;
            set => Instance.TetBleedValues = value;
        }

        public static string ToggleMinimizeHotkey
        {
            get => Instance.ToggleMinimizeHotkey;
            set => Instance.ToggleMinimizeHotkey = value;
        }

        // 窗口位置配置（MaterialSelectFormModern专用）
        public static int MaterialFormX
        {
            get => Instance.MaterialFormX;
            set => Instance.MaterialFormX = value;
        }

        public static int MaterialFormY
        {
            get => Instance.MaterialFormY;
            set => Instance.MaterialFormY = value;
        }

        public static int MaterialFormWidth
        {
            get => Instance.MaterialFormWidth;
            set => Instance.MaterialFormWidth = value;
        }

        public static int MaterialFormHeight
        {
            get => Instance.MaterialFormHeight;
            set => Instance.MaterialFormHeight = value;
        }

        public static bool MaterialFormMaximized
        {
            get => Instance.MaterialFormMaximized;
            set => Instance.MaterialFormMaximized = value;
        }

        public static bool MaterialFormPreviewExpanded
        {
            get => Instance.MaterialFormPreviewExpanded;
            set => Instance.MaterialFormPreviewExpanded = value;
        }

        public static System.Collections.Generic.List<Models.MaterialSelectionPreset> MaterialPresets
        {
            get => Instance.MaterialPresets;
            set => Instance.MaterialPresets = value;
        }

        public static string LastUsedMaterialPreset
        {
            get => Instance.LastUsedMaterialPreset;
            set => Instance.LastUsedMaterialPreset = value;
        }

        public static string LastExportPath
        {
            get => Instance.LastExportPath;
            set => Instance.LastExportPath = value;
        }

        public static string LastUsedConfigName
        {
            get => Instance.LastUsedConfigName;
            set => Instance.LastUsedConfigName = value;
        }

        public static string CompositeColumns
        {
            get => GetValue<string>("CompositeColumns", "");
            set => SetValue("CompositeColumns", value);
        }

        public static string CompositeColumnSeparator
        {
            get => GetValue<string>("CompositeColumnSeparator", ",");
            set => SetValue("CompositeColumnSeparator", value);
        }

      
        public static System.Collections.Generic.List<string> ExportPaths
        {
            get => Instance.ExportPaths;
            set => Instance.ExportPaths = value;
        }

        public static string LastColorMode
        {
            get => Instance.LastColorMode;
            set => Instance.LastColorMode = value;
        }

        public static string LastFilmType
        {
            get => Instance.LastFilmType;
            set => Instance.LastFilmType = value;
        }

        public static string LastSelectedRegex
        {
            get => Instance.LastSelectedRegex;
            set => Instance.LastSelectedRegex = value;
        }

        public static string ExtractNumberKeywords
        {
            get => Instance.ExtractNumberKeywords;
            set => Instance.ExtractNumberKeywords = value;
        }

        public static string LeftClickFilm
        {
            get => GetValue<string>("LeftClickFilm", "");
            set => SetValue("LeftClickFilm", value);
        }

        public static string RightClickFilm
        {
            get => GetValue<string>("RightClickFilm", "");
            set => SetValue("RightClickFilm", value);
        }

        // Excel相关
        public static string ExcelSerialColumnParams
        {
            get => Instance.ExcelSerialColumnParams;
            set => Instance.ExcelSerialColumnParams = value;
        }

        public static string ExcelSearchColumnParams
        {
            get => Instance.ExcelSearchColumnParams;
            set => Instance.ExcelSearchColumnParams = value;
        }

        public static string ExcelReturnColumnParams
        {
            get => Instance.ExcelReturnColumnParams;
            set => Instance.ExcelReturnColumnParams = value;
        }

        // 主题设置
        public static string CurrentThemeName
        {
            get => GetValue<string>("CurrentThemeName", "浅色");
            set => SetValue("CurrentThemeName", value);
        }

        /// <summary>
        /// 向后兼容：ThemeMode 属性（已弃用，请使用 CurrentThemeName）
        /// </summary>
        [Obsolete("请使用 CurrentThemeName 代替")]
        public static string ThemeMode
        {
            get => CurrentThemeName == "深色" ? "Dark" : "Light";
            set => CurrentThemeName = value == "Dark" ? "深色" : "浅色";
        }


        // 排版功能控件状态持久化
        public static bool EnableImpositionChecked
        {
            get => Instance.EnableImpositionChecked;
            set => Instance.EnableImpositionChecked = value;
        }

        public static string LastMaterialType
        {
            get => Instance.LastMaterialType;
            set => Instance.LastMaterialType = value;
        }

        public static string LastLayoutMode
        {
            get => Instance.LastLayoutMode;
            set => Instance.LastLayoutMode = value;
        }

        // ... existing code ...
        /// <summary>
        /// 是否隐藏半径数值
        /// </summary>
        public static bool HideRadiusValue
        {
            get => Instance.HideRadiusValue;
            set => Instance.HideRadiusValue = value;
        }

        /// <summary>
        /// 正则表达式变化时自动刷新文件名
        /// </summary>
        public static bool AutoRefreshFileNameOnRegexChange
        {
            get => Instance.AutoRefreshFileNameOnRegexChange;
            set => Instance.AutoRefreshFileNameOnRegexChange = value;
        }

        /// <summary>
        /// 是否启用列组合
        /// </summary>
        public static bool EnableColumnCombine
        {
            get => GetValue<bool>("EnableColumnCombine", false);
            set => SetValue("EnableColumnCombine", value);
        }

        /// <summary>
        /// 是否自动生成序号
        /// </summary>
        public static bool AutoGenerateSerial
        {
            get => GetValue<bool>("AutoGenerateSerial", false);
            set => SetValue("AutoGenerateSerial", value);
        }

        /// <summary>
        /// 默认材料
        /// </summary>
        public static string DefaultMaterial
        {
            get => GetValue<string>("DefaultMaterial", "");
            set => SetValue("DefaultMaterial", value);
        }

        /// <summary>
        /// 默认数量
        /// </summary>
        public static string DefaultQuantity
        {
            get => GetValue<string>("DefaultQuantity", "");
            set => SetValue("DefaultQuantity", value);
        }

        /// <summary>
        /// PDF预览是否展开
        /// </summary>
        public static bool PreviewExpanded
        {
            get => GetValue<bool>("PreviewExpanded", false);
            set => SetValue("PreviewExpanded", value);
        }

        /// <summary>
        /// 重命名完成后显示通知
        /// </summary>
        public static bool ShowRenameCompleteNotification
        {
            get => GetValue<bool>("ShowRenameCompleteNotification", true);
            set => SetValue("ShowRenameCompleteNotification", value);
        }

        /// <summary>
        /// 自动保存频率（秒）
        /// </summary>
        public static int AutoSaveIntervalSeconds
        {
            get => GetValue<int>("AutoSaveIntervalSeconds", 60);
            set => SetValue("AutoSaveIntervalSeconds", value);
        }

        /// <summary>
        /// 启用每日JSON自动加载/创建功能
        /// </summary>
        public static bool EnableDailyJson
        {
            get => GetValue<bool>("EnableDailyJson", true);
            set => SetValue("EnableDailyJson", value);
        }

        /// <summary>
        /// 同时输出排版模式布局数
        /// </summary>
        public static bool AlwaysOutputBothLayoutCounts
        {
            get => GetValue<bool>("AlwaysOutputBothLayoutCounts", false);
            set => SetValue("AlwaysOutputBothLayoutCounts", value);
        }

        public static bool EnableDynamicFileReadyTimeout
        {
            get => GetValue<bool>("EnableDynamicFileReadyTimeout", true);
            set => SetValue("EnableDynamicFileReadyTimeout", value);
        }

        public static int FileReadyTimeoutSmallSeconds
        {
            get => GetValue<int>("FileReadyTimeoutSmallSeconds", 45);
            set => SetValue("FileReadyTimeoutSmallSeconds", value);
        }

        public static int FileReadyTimeoutMediumSeconds
        {
            get => GetValue<int>("FileReadyTimeoutMediumSeconds", 90);
            set => SetValue("FileReadyTimeoutMediumSeconds", value);
        }

        public static int FileReadyTimeoutLargeSeconds
        {
            get => GetValue<int>("FileReadyTimeoutLargeSeconds", 150);
            set => SetValue("FileReadyTimeoutLargeSeconds", value);
        }

        public static int FileReadyTimeoutSmallThresholdMb
        {
            get => GetValue<int>("FileReadyTimeoutSmallThresholdMb", 50);
            set => SetValue("FileReadyTimeoutSmallThresholdMb", value);
        }

        public static int FileReadyTimeoutLargeThresholdMb
        {
            get => GetValue<int>("FileReadyTimeoutLargeThresholdMb", 300);
            set => SetValue("FileReadyTimeoutLargeThresholdMb", value);
        }

        /// <summary>
        /// 通用设置访问器，兼容Properties.Settings["key"]的访问方式
        /// </summary>
        public static object Get(string key)
        {
            return Instance[key];
        }

        /// <summary>
        /// 通用设置设置器
        /// </summary>
        public static void Set(string key, object value)
        {
            Instance[key] = value;
        }

        /// <summary>
        /// 获取类型化的设置值
        /// </summary>
        public static T GetValue<T>(string key, T defaultValue = default)
        {
            return Instance.GetValue<T>(key, defaultValue);
        }

        /// <summary>
        /// 设置类型化的设置值
        /// </summary>
        public static void SetValue<T>(string key, T value)
        {
            Instance.SetValue(key, value);
        }

        #endregion

        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public static bool IsInitialized => _settingsService != null;

        /// <summary>
        /// 重新加载设置
        /// </summary>
        public static void Reload()
        {
            // 这里需要重新创建实例来重新加载设置
            lock (_lock)
            {
                var logger = LogHelper.GetLogger();
                _settingsService = new ApplicationSettingsService(logger);
            }
        }
    }
}