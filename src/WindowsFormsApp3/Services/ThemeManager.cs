using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
        private List<ThemeDefinition> _allThemes;
        private ThemeDefinition _currentTheme;

        public ThemeManager(ILogger logger)
        {
            _logger = logger;
            _customThemesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Prepress-toolbox",
                "themes.json"
            );

            InitializeThemes();
        }

        /// <summary>
        /// 初始化主题列表（内置 + 自定义）
        /// </summary>
        private void InitializeThemes()
        {
            _allThemes = new List<ThemeDefinition>();

            // 添加内置预设主题
            _allThemes.Add(CreateLightTheme());
            _allThemes.Add(CreateDarkTheme());
            _allThemes.Add(CreateGreenTheme());
            _allThemes.Add(CreateClassicBlueTheme());

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
                AccentColor4 = Color.MediumPurple    // 旋转
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
                AccentColor4 = Color.MediumPurple
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
                AccentColor4 = Color.MediumPurple
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
                AccentColor4 = Color.MediumPurple
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
        /// 保存自定义主题
        /// </summary>
        public bool SaveCustomTheme(ThemeDefinition theme)
        {
            try
            {
                if (theme.IsBuiltIn)
                {
                    _logger?.LogWarning("不能修改内置主题");
                    return false;
                }

                // 检查是否已存在
                var existing = _allThemes.FirstOrDefault(t => t.Name == theme.Name);
                if (existing != null)
                {
                    // 更新现有主题
                    _allThemes.Remove(existing);
                }

                _allThemes.Add(theme);
                SaveCustomThemesToFile();

                _logger?.LogInformation($"自定义主题已保存: {theme.Name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError($"保存自定义主题失败: {ex.Message}");
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

        #endregion
    }
}
