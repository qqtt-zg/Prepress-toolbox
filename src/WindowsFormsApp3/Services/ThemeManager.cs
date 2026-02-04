using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 主题管理服务
    /// </summary>
    public class ThemeManager
    {
        private readonly ILogger _logger;
        private readonly string _customThemesPath;
        private readonly string _builtInThemesOverridesPath;
        private List<ThemeDefinition> _allThemes;
        private ThemeDefinition _currentTheme;
        private List<ThemeDefinition> _originalBuiltInThemes; // 保存原始内置主题定义

        public ThemeManager(ILogger logger)
        {
            _logger = logger;
            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Prepress-toolbox"
            );
            _customThemesPath = Path.Combine(appDataDir, "themes.json");
            _builtInThemesOverridesPath = Path.Combine(appDataDir, "builtin-themes-overrides.json");

            InitializeThemes();
        }

        /// <summary>
        /// 初始化主题列表（内置 + 自定义）
        /// </summary>
        private void InitializeThemes()
        {
            _allThemes = new List<ThemeDefinition>();
            _originalBuiltInThemes = new List<ThemeDefinition>();

            // 创建并保存原始内置主题定义
            var lightTheme = CreateLightTheme();
            var darkTheme = CreateDarkTheme();
            var greenTheme = CreateGreenTheme();
            var classicBlueTheme = CreateClassicBlueTheme();

            // 保存原始定义（克隆）
            _originalBuiltInThemes.Add(lightTheme.Clone());
            _originalBuiltInThemes.Add(darkTheme.Clone());
            _originalBuiltInThemes.Add(greenTheme.Clone());
            _originalBuiltInThemes.Add(classicBlueTheme.Clone());

            // 添加到主题列表
            _allThemes.Add(lightTheme);
            _allThemes.Add(darkTheme);
            _allThemes.Add(greenTheme);
            _allThemes.Add(classicBlueTheme);

            // 加载内置主题的覆盖值（必须在加载自定义主题之前，因为覆盖值会修改内置主题）
            LoadBuiltInThemesOverrides();

            // 加载用户自定义主题
            LoadCustomThemes();

            // 设置默认主题
            _currentTheme = _allThemes.FirstOrDefault(t => t.Name == "浅色");
        }

        #region 预设主题定义

        /// <summary>
        /// 浅色主题
        /// </summary>
        private ThemeDefinition CreateLightTheme()
        {
            return new ThemeDefinition
            {
                Name = "浅色",
                IsBuiltIn = true,
                Background = Color.FromArgb(248, 249, 250),
                Surface = Color.White,
                SurfaceLight = Color.White,
                InputBackground = Color.White,
                TextPrimary = Color.FromArgb(33, 37, 41),
                TextSecondary = Color.FromArgb(108, 117, 125),
                Border = Color.FromArgb(222, 226, 230),
                Primary = Color.FromArgb(13, 110, 253),
                Success = Color.FromArgb(25, 135, 84),
                Warning = Color.FromArgb(255, 193, 7),
                Error = Color.FromArgb(220, 53, 69),
                BackActive = Color.FromArgb(230, 240, 255),
                BackHover = Color.FromArgb(245, 247, 250),
                UseScrollBarDarkMode = false, // 浅色主题使用浅色滚动条
                AccentColor1 = Color.DodgerBlue,     // 行数
                AccentColor2 = Color.ForestGreen,    // 列数
                AccentColor3 = Color.Orange,         // 数量
                AccentColor4 = Color.MediumPurple,   // 旋转
                FloatingDropZoneDefaultWidth = 180,
                FloatingDropZoneDefaultHeight = 80,
                FloatingDropZoneBackColor = Color.FromArgb(70, 130, 180),
                FloatingDropZoneBackColorDrag = Color.FromArgb(50, 100, 150),
                FloatingDropZoneOpacity = 0.92,
                FloatingDropZonePopcatEnabled = false
            };
        }

        /// <summary>
        /// 深色主题（当前默认）
        /// </summary>
        private ThemeDefinition CreateDarkTheme()
        {
            return new ThemeDefinition
            {
                Name = "深色",
                IsBuiltIn = true,
                Background = Color.FromArgb(40, 42, 46),
                Surface = Color.FromArgb(48, 50, 54),
                SurfaceLight = Color.FromArgb(55, 57, 61),
                InputBackground = Color.FromArgb(55, 57, 61), // 默认同 SurfaceLight，用户可调
                TextPrimary = Color.FromArgb(230, 230, 235),
                TextSecondary = Color.FromArgb(160, 165, 170),
                Border = Color.FromArgb(65, 67, 71),
                Primary = Color.FromArgb(48, 100, 160),
                Success = Color.FromArgb(56, 120, 80),
                Warning = Color.FromArgb(160, 100, 50),
                Error = Color.FromArgb(140, 60, 60),
                BackActive = Color.FromArgb(45, 55, 65),
                BackHover = Color.FromArgb(55, 57, 61),
                UseScrollBarDarkMode = true,  // 深色主题使用深色滚动条
                AccentColor1 = Color.DodgerBlue,
                AccentColor2 = Color.ForestGreen,
                AccentColor3 = Color.Orange,
                AccentColor4 = Color.MediumPurple,
                FloatingDropZoneDefaultWidth = 180,
                FloatingDropZoneDefaultHeight = 80,
                FloatingDropZoneBackColor = Color.FromArgb(70, 130, 180),
                FloatingDropZoneBackColorDrag = Color.FromArgb(50, 100, 150),
                FloatingDropZoneOpacity = 0.92,
                FloatingDropZonePopcatEnabled = false
            };
        }

        /// <summary>
        /// 护眼绿色主题
        /// </summary>
        private ThemeDefinition CreateGreenTheme()
        {
            return new ThemeDefinition
            {
                Name = "护眼绿",
                IsBuiltIn = true,
                Background = Color.FromArgb(199, 237, 204),
                Surface = Color.FromArgb(212, 245, 217),
                SurfaceLight = Color.FromArgb(220, 248, 224),
                InputBackground = Color.FromArgb(220, 248, 224),
                TextPrimary = Color.FromArgb(25, 50, 30),
                TextSecondary = Color.FromArgb(60, 90, 70),
                Border = Color.FromArgb(180, 225, 185),
                Primary = Color.FromArgb(40, 167, 69),
                Success = Color.FromArgb(25, 135, 84),
                Warning = Color.FromArgb(255, 193, 7),
                Error = Color.FromArgb(220, 53, 69),
                BackActive = Color.FromArgb(185, 230, 195),
                BackHover = Color.FromArgb(205, 240, 210),
                UseScrollBarDarkMode = false, // 护眼绿使用浅色滚动条
                AccentColor1 = Color.DodgerBlue,
                AccentColor2 = Color.ForestGreen,
                AccentColor3 = Color.Orange,
                AccentColor4 = Color.MediumPurple,
                FloatingDropZoneDefaultWidth = 180,
                FloatingDropZoneDefaultHeight = 80,
                FloatingDropZoneBackColor = Color.FromArgb(70, 130, 180),
                FloatingDropZoneBackColorDrag = Color.FromArgb(50, 100, 150),
                FloatingDropZoneOpacity = 0.92,
                FloatingDropZonePopcatEnabled = false
            };
        }

        /// <summary>
        /// 经典蓝色主题
        /// </summary>
        private ThemeDefinition CreateClassicBlueTheme()
        {
            return new ThemeDefinition
            {
                Name = "经典蓝",
                IsBuiltIn = true,
                Background = Color.FromArgb(225, 235, 245),
                Surface = Color.FromArgb(235, 243, 250),
                SurfaceLight = Color.White,
                InputBackground = Color.White,
                TextPrimary = Color.FromArgb(30, 50, 80),
                TextSecondary = Color.FromArgb(90, 110, 140),
                Border = Color.FromArgb(200, 215, 230),
                Primary = Color.FromArgb(0, 123, 255),
                Success = Color.FromArgb(40, 167, 69),
                Warning = Color.FromArgb(255, 193, 7),
                Error = Color.FromArgb(220, 53, 69),
                BackActive = Color.FromArgb(210, 225, 240),
                BackHover = Color.FromArgb(220, 232, 243),
                UseScrollBarDarkMode = false, // 经典蓝使用浅色滚动条
                AccentColor1 = Color.DodgerBlue,
                AccentColor2 = Color.ForestGreen,
                AccentColor3 = Color.Orange,
                AccentColor4 = Color.MediumPurple,
                FloatingDropZoneDefaultWidth = 180,
                FloatingDropZoneDefaultHeight = 80,
                FloatingDropZoneBackColor = Color.FromArgb(70, 130, 180),
                FloatingDropZoneBackColorDrag = Color.FromArgb(50, 100, 150),
                FloatingDropZoneOpacity = 0.92,
                FloatingDropZonePopcatEnabled = false
            };
        }

        #endregion

        #region 主题管理

        /// <summary>
        /// 获取所有可用主题
        /// </summary>
        public List<ThemeDefinition> GetAllThemes()
        {
            return new List<ThemeDefinition>(_allThemes);
        }

        /// <summary>
        /// 获取当前应用的主题
        /// </summary>
        public ThemeDefinition GetCurrentTheme()
        {
            return _currentTheme;
        }

        /// <summary>
        /// 根据名称获取主题
        /// </summary>
        public ThemeDefinition GetThemeByName(string name)
        {
            return _allThemes.FirstOrDefault(t => t.Name == name);
        }

        /// <summary>
        /// 获取原始内置主题定义（用于重置）
        /// </summary>
        public ThemeDefinition GetOriginalBuiltInTheme(string name)
        {
            return _originalBuiltInThemes?.FirstOrDefault(t => t.Name == name)?.Clone();
        }

        /// <summary>
        /// 设置当前主题
        /// </summary>
        public void SetCurrentTheme(string themeName)
        {
            var theme = GetThemeByName(themeName);
            if (theme != null)
            {
                _currentTheme = theme;
                _logger?.LogInformation($"主题已切换到: {themeName}");
            }
            else
            {
                _logger?.LogWarning($"未找到主题: {themeName}");
            }
        }

        /// <summary>
        /// 保存自定义主题（现在也支持保存内置主题的修改）
        /// </summary>
        public bool SaveCustomTheme(ThemeDefinition theme)
        {
            try
            {
                // 检查是否已存在
                var existing = _allThemes.FirstOrDefault(t => t.Name == theme.Name);
                if (existing != null)
                {
                    // 更新现有主题
                    _allThemes.Remove(existing);
                }

                _allThemes.Add(theme);
                
                // 如果是内置主题，保存到覆盖文件；否则保存到自定义主题文件
                if (theme.IsBuiltIn)
                {
                    SaveBuiltInThemesOverrides();
                }
                else
                {
                    SaveCustomThemesToFile();
                }

                // 如果保存的主题是当前应用的主题，更新当前主题引用
                if (_currentTheme != null && _currentTheme.Name == theme.Name)
                {
                    _currentTheme = theme;
                }

                if (theme.IsBuiltIn)
                {
                    _logger?.LogInformation($"内置主题已更新: {theme.Name}");
                }
                else
                {
                    _logger?.LogInformation($"自定义主题已保存: {theme.Name}");
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"保存主题失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除自定义主题
        /// </summary>
        public bool DeleteCustomTheme(string themeName)
        {
            try
            {
                var theme = GetThemeByName(themeName);
                if (theme == null)
                {
                    _logger?.LogWarning($"未找到主题: {themeName}");
                    return false;
                }

                if (theme.IsBuiltIn)
                {
                    _logger?.LogWarning("不能删除内置主题");
                    return false;
                }

                _allThemes.Remove(theme);
                SaveCustomThemesToFile();

                _logger?.LogInformation($"自定义主题已删除: {themeName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"删除自定义主题失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 导入/导出

        /// <summary>
        /// 导出主题为 JSON 文件
        /// </summary>
        public bool ExportTheme(ThemeDefinition theme, string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(theme, Formatting.Indented);
                File.WriteAllText(filePath, json);
                _logger?.LogInformation($"主题已导出: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"导出主题失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从 JSON 文件导入主题
        /// </summary>
        public ThemeDefinition ImportTheme(string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var theme = JsonConvert.DeserializeObject<ThemeDefinition>(json);
                theme.IsBuiltIn = false; // 导入的主题标记为自定义
                _logger?.LogInformation($"主题已导入: {theme.Name}");
                return theme;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"导入主题失败: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 持久化

        /// <summary>
        /// 加载自定义主题
        /// </summary>
        private void LoadCustomThemes()
        {
            try
            {
                if (!File.Exists(_customThemesPath))
                    return;

                var json = File.ReadAllText(_customThemesPath);
                var customThemes = JsonConvert.DeserializeObject<List<ThemeDefinition>>(json);

                if (customThemes != null && customThemes.Count > 0)
                {
                    _allThemes.AddRange(customThemes);
                    _logger?.LogInformation($"已加载 {customThemes.Count} 个自定义主题");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"加载自定义主题失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存自定义主题到文件
        /// </summary>
        private void SaveCustomThemesToFile()
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(_customThemesPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 只保存自定义主题
                var customThemes = _allThemes.Where(t => !t.IsBuiltIn).ToList();
                var json = JsonConvert.SerializeObject(customThemes, Formatting.Indented);
                File.WriteAllText(_customThemesPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"保存自定义主题文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载内置主题的覆盖值
        /// </summary>
        private void LoadBuiltInThemesOverrides()
        {
            try
            {
                if (!File.Exists(_builtInThemesOverridesPath))
                    return;

                var json = File.ReadAllText(_builtInThemesOverridesPath);
                var overrides = JsonConvert.DeserializeObject<Dictionary<string, ThemeDefinition>>(json);

                if (overrides != null && overrides.Count > 0)
                {
                    foreach (var kvp in overrides)
                    {
                        var themeName = kvp.Key;
                        var overrideTheme = kvp.Value;

                        // 查找对应的内置主题
                        var builtInTheme = _allThemes.FirstOrDefault(t => t.IsBuiltIn && t.Name == themeName);
                        if (builtInTheme != null && overrideTheme != null)
                        {
                            // 应用覆盖值（复制所有属性，但保持 IsBuiltIn = true）
                            builtInTheme.Background = overrideTheme.Background;
                            builtInTheme.Surface = overrideTheme.Surface;
                            builtInTheme.SurfaceLight = overrideTheme.SurfaceLight;
                            builtInTheme.InputBackground = overrideTheme.InputBackground;
                            builtInTheme.TextPrimary = overrideTheme.TextPrimary;
                            builtInTheme.TextSecondary = overrideTheme.TextSecondary;
                            builtInTheme.Border = overrideTheme.Border;
                            builtInTheme.Primary = overrideTheme.Primary;
                            builtInTheme.Success = overrideTheme.Success;
                            builtInTheme.Warning = overrideTheme.Warning;
                            builtInTheme.Error = overrideTheme.Error;
                            builtInTheme.AccentColor1 = overrideTheme.AccentColor1;
                            builtInTheme.AccentColor2 = overrideTheme.AccentColor2;
                            builtInTheme.AccentColor3 = overrideTheme.AccentColor3;
                            builtInTheme.AccentColor4 = overrideTheme.AccentColor4;
                            builtInTheme.BackActive = overrideTheme.BackActive;
                            builtInTheme.BackHover = overrideTheme.BackHover;
                            builtInTheme.UseScrollBarDarkMode = overrideTheme.UseScrollBarDarkMode;
                            builtInTheme.FloatingDropZoneDefaultWidth = overrideTheme.FloatingDropZoneDefaultWidth;
                            builtInTheme.FloatingDropZoneDefaultHeight = overrideTheme.FloatingDropZoneDefaultHeight;
                            builtInTheme.FloatingDropZoneBackColor = overrideTheme.FloatingDropZoneBackColor;
                            builtInTheme.FloatingDropZoneBackColorDrag = overrideTheme.FloatingDropZoneBackColorDrag;
                            builtInTheme.FloatingDropZoneOpacity = overrideTheme.FloatingDropZoneOpacity;
                            builtInTheme.FloatingDropZonePopcatEnabled = overrideTheme.FloatingDropZonePopcatEnabled;

                            _logger?.LogInformation($"已加载内置主题覆盖值: {themeName}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"加载内置主题覆盖值失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存内置主题的覆盖值
        /// </summary>
        private void SaveBuiltInThemesOverrides()
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(_builtInThemesOverridesPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 收集所有已修改的内置主题（与原始定义不同的）
                var overrides = new Dictionary<string, ThemeDefinition>();
                foreach (var builtInTheme in _allThemes.Where(t => t.IsBuiltIn))
                {
                    var originalTheme = _originalBuiltInThemes?.FirstOrDefault(t => t.Name == builtInTheme.Name);
                    if (originalTheme != null && !AreThemesEqual(builtInTheme, originalTheme))
                    {
                        // 主题已被修改，保存覆盖值
                        overrides[builtInTheme.Name] = builtInTheme.Clone();
                    }
                }

                // 保存覆盖值到文件
                var json = JsonConvert.SerializeObject(overrides, Formatting.Indented);
                File.WriteAllText(_builtInThemesOverridesPath, json);
                
                _logger?.LogInformation($"已保存 {overrides.Count} 个内置主题覆盖值");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"保存内置主题覆盖值失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 比较两个主题是否相等
        /// </summary>
        private bool AreThemesEqual(ThemeDefinition theme1, ThemeDefinition theme2)
        {
            if (theme1 == null || theme2 == null)
                return theme1 == theme2;

            return theme1.Background == theme2.Background &&
                   theme1.Surface == theme2.Surface &&
                   theme1.SurfaceLight == theme2.SurfaceLight &&
                   theme1.InputBackground == theme2.InputBackground &&
                   theme1.TextPrimary == theme2.TextPrimary &&
                   theme1.TextSecondary == theme2.TextSecondary &&
                   theme1.Border == theme2.Border &&
                   theme1.Primary == theme2.Primary &&
                   theme1.Success == theme2.Success &&
                   theme1.Warning == theme2.Warning &&
                   theme1.Error == theme2.Error &&
                   theme1.AccentColor1 == theme2.AccentColor1 &&
                   theme1.AccentColor2 == theme2.AccentColor2 &&
                   theme1.AccentColor3 == theme2.AccentColor3 &&
                   theme1.AccentColor4 == theme2.AccentColor4 &&
                   theme1.BackActive == theme2.BackActive &&
                   theme1.BackHover == theme2.BackHover &&
                   theme1.UseScrollBarDarkMode == theme2.UseScrollBarDarkMode &&
                   theme1.FloatingDropZoneDefaultWidth == theme2.FloatingDropZoneDefaultWidth &&
                   theme1.FloatingDropZoneDefaultHeight == theme2.FloatingDropZoneDefaultHeight &&
                   theme1.FloatingDropZoneBackColor == theme2.FloatingDropZoneBackColor &&
                   theme1.FloatingDropZoneBackColorDrag == theme2.FloatingDropZoneBackColorDrag &&
                   Math.Abs(theme1.FloatingDropZoneOpacity - theme2.FloatingDropZoneOpacity) < 0.001 &&
                   theme1.FloatingDropZonePopcatEnabled == theme2.FloatingDropZonePopcatEnabled;
        }

        #endregion
    }
}
