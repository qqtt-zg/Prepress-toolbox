using System;
using System.IO;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// 应用数据路径管理器
    /// 统一管理所有应用配置和数据文件的存储路径
    /// 所有路径统一存放在 %AppData%\Roaming\大诚重命名工具\ 下
    /// </summary>
    public static class AppDataPathManager
    {
        private static string _autoTestSandboxDir = null;

        /// <summary>
        /// 应用根目录：%AppData%\Roaming\大诚重命名工具\
        /// 当检测到处于单元测试环境（xUnit, VSTest, TestHost 等）或环境变量 CODEX_APPDATA_ROOT 时，
        /// 物理重定向至独立沙箱，坚决杜绝测试运行触碰或污染用户的真实生产配置文件。
        /// </summary>
        public static string AppRootDirectory
        {
            get
            {
                var envRoot = Environment.GetEnvironmentVariable("CODEX_APPDATA_ROOT");
                if (!string.IsNullOrEmpty(envRoot))
                {
                    EnsureDirectoryExists(envRoot);
                    return envRoot;
                }

                if (IsRunningInTestRunner())
                {
                    if (string.IsNullOrEmpty(_autoTestSandboxDir))
                    {
                        _autoTestSandboxDir = Path.Combine(Path.GetTempPath(), "CodexTests_AutoSandbox_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                    }
                    EnsureDirectoryExists(_autoTestSandboxDir);
                    return _autoTestSandboxDir;
                }

                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "大诚重命名工具");
                EnsureDirectoryExists(path);
                return path;
            }
        }

        /// <summary>
        /// 检查当前进程是否处于单元测试执行环境（xUnit, VSTest, TestHost 等）
        /// </summary>
        public static bool IsRunningInTestRunner()
        {
            try
            {
                var friendlyName = AppDomain.CurrentDomain.FriendlyName ?? "";
                if (friendlyName.IndexOf("testhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    friendlyName.IndexOf("vstest", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    friendlyName.IndexOf("xunit", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    var name = assemblies[i].FullName;
                    if (name != null && (name.StartsWith("xunit.", StringComparison.OrdinalIgnoreCase) ||
                                         name.StartsWith("Microsoft.VisualStudio.TestPlatform", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        /// <summary>
        /// 配置文件路径：%AppData%\Roaming\大诚重命名工具\configs.json
        /// </summary>
        public static string ConfigFilePath => Path.Combine(AppRootDirectory, "configs.json");

        /// <summary>
        /// 材料选择配置文件路径：%AppData%\Roaming\大诚重命名工具\MaterialSelectSettings.json
        /// </summary>
        public static string MaterialSelectSettingsPath => Path.Combine(AppRootDirectory, "MaterialSelectSettings.json");

        /// <summary>
        /// 日志配置文件路径：%AppData%\Roaming\大诚重命名工具\LogConfig.json
        /// </summary>
        public static string LogConfigPath => Path.Combine(AppRootDirectory, "LogConfig.json");

        /// <summary>
        /// 保存的网格数据目录：%AppData%\Roaming\大诚重命名工具\SavedGrids\
        /// </summary>
        public static string SavedGridsDirectory
        {
            get
            {
                var path = Path.Combine(AppRootDirectory, "SavedGrids");
                EnsureDirectoryExists(path);
                return path;
            }
        }

        /// <summary>
        /// 错误报告目录：%AppData%\Roaming\大诚重命名工具\ErrorReports\
        /// </summary>
        public static string ErrorReportsDirectory
        {
            get
            {
                var path = Path.Combine(AppRootDirectory, "ErrorReports");
                EnsureDirectoryExists(path);
                return path;
            }
        }

        /// <summary>
        /// 命令历史目录：%AppData%\Roaming\大诚重命名工具\History\
        /// </summary>
        public static string HistoryDirectory
        {
            get
            {
                var path = Path.Combine(AppRootDirectory, "History");
                EnsureDirectoryExists(path);
                return path;
            }
        }

        /// <summary>
        /// 命令历史文件路径：%AppData%\Roaming\大诚重命名工具\History\command_history.json
        /// </summary>
        public static string CommandHistoryPath => Path.Combine(HistoryDirectory, "command_history.json");

        /// <summary>
        /// 日志目录：%AppData%\Roaming\大诚重命名工具\Logs\
        /// </summary>
        public static string LogsDirectory
        {
            get
            {
                var path = Path.Combine(AppRootDirectory, "Logs");
                EnsureDirectoryExists(path);
                return path;
            }
        }

        /// <summary>
        /// 配置备份目录：%AppData%\Roaming\大诚重命名工具\Backups\
        /// </summary>
        public static string BackupsDirectory
        {
            get
            {
                var path = Path.Combine(AppRootDirectory, "Backups");
                EnsureDirectoryExists(path);
                return path;
            }
        }

        /// <summary>
        /// Excel导出目录：%AppData%\Roaming\大诚重命名工具\Excel导出\
        /// </summary>
        public static string ExcelExportDirectory
        {
            get
            {
                var path = Path.Combine(AppRootDirectory, "Excel导出");
                EnsureDirectoryExists(path);
                return path;
            }
        }

        /// <summary>
        /// 确保目录存在，如果不存在则创建
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        private static void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        /// <summary>
        /// 从旧路径迁移数据到新路径
        /// </summary>
        public static void MigrateFromOldPaths()
        {
            try
            {
                // 迁移configs.json
                MigrateConfigFile();

                // 迁移MaterialSelectSettings.json
                MigrateMaterialSettings();

                // 迁移SavedGrids
                MigrateSavedGrids();

                // 迁移ErrorReports
                MigrateErrorReports();

                // 迁移CommandHistory
                MigrateCommandHistory();

                // 迁移LogConfig.json
                MigrateLogConfig();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"数据迁移失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 迁移configs.json文件
        /// </summary>
        private static void MigrateConfigFile()
        {
            try
            {
                // 旧路径：Application.UserAppDataPath
                var oldPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WindowsFormsApp3", "WindowsFormsApp3");

                // 查找所有可能的版本目录
                if (Directory.Exists(oldPath))
                {
                    var versionDirs = Directory.GetDirectories(oldPath);
                    foreach (var versionDir in versionDirs)
                    {
                        var oldConfigFile = Path.Combine(versionDir, "configs.json");
                        if (File.Exists(oldConfigFile) && !File.Exists(ConfigFilePath))
                        {
                            File.Copy(oldConfigFile, ConfigFilePath, false);
                            LogHelper.Info($"已迁移配置文件: {oldConfigFile} -> {ConfigFilePath}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"迁移configs.json失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 迁移材料选择配置文件
        /// </summary>
        private static void MigrateMaterialSettings()
        {
            try
            {
                // 旧路径：%AppData%\Roaming\大诚工具箱\MaterialSelectSettings.json
                var oldPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "大诚工具箱", "MaterialSelectSettings.json");

                if (File.Exists(oldPath) && !File.Exists(MaterialSelectSettingsPath))
                {
                    File.Copy(oldPath, MaterialSelectSettingsPath, false);
                    LogHelper.Info($"已迁移材料选择配置: {oldPath} -> {MaterialSelectSettingsPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"迁移MaterialSelectSettings.json失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 迁移SavedGrids目录
        /// </summary>
        private static void MigrateSavedGrids()
        {
            try
            {
                // 旧路径：%UserProfile%\Documents\大诚工具箱\SavedGrids\
                var oldPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "大诚工具箱", "SavedGrids");

                if (Directory.Exists(oldPath))
                {
                    var files = Directory.GetFiles(oldPath, "*.json");
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var newPath = Path.Combine(SavedGridsDirectory, fileName);
                        if (!File.Exists(newPath))
                        {
                            File.Copy(file, newPath, false);
                        }
                    }
                    if (files.Length > 0)
                    {
                        LogHelper.Info($"已迁移 {files.Length} 个网格数据文件");
                    }
                }

                // 也检查程序目录下的SavedGrids
                var appDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SavedGrids");
                if (Directory.Exists(appDirPath))
                {
                    var files = Directory.GetFiles(appDirPath, "*.json");
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var newPath = Path.Combine(SavedGridsDirectory, fileName);
                        if (!File.Exists(newPath))
                        {
                            File.Copy(file, newPath, false);
                        }
                    }
                    if (files.Length > 0)
                    {
                        LogHelper.Info($"已从程序目录迁移 {files.Length} 个网格数据文件");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"迁移SavedGrids失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 迁移错误报告目录
        /// </summary>
        private static void MigrateErrorReports()
        {
            try
            {
                // 旧路径可能在LocalApplicationData
                var oldPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "大诚重命名工具", "ErrorReports");

                if (Directory.Exists(oldPath))
                {
                    var files = Directory.GetFiles(oldPath, "*.json");
                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);
                        var newPath = Path.Combine(ErrorReportsDirectory, fileName);
                        if (!File.Exists(newPath))
                        {
                            File.Copy(file, newPath, false);
                        }
                    }
                    if (files.Length > 0)
                    {
                        LogHelper.Info($"已迁移 {files.Length} 个错误报告文件");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"迁移ErrorReports失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 迁移命令历史文件
        /// </summary>
        private static void MigrateCommandHistory()
        {
            try
            {
                // 旧路径：%AppData%\Roaming\WindowsFormsApp3\History\command_history.json
                var oldPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WindowsFormsApp3", "History", "command_history.json");

                if (File.Exists(oldPath) && !File.Exists(CommandHistoryPath))
                {
                    File.Copy(oldPath, CommandHistoryPath, false);
                    LogHelper.Info($"已迁移命令历史: {oldPath} -> {CommandHistoryPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"迁移command_history.json失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 迁移日志配置文件
        /// </summary>
        private static void MigrateLogConfig()
        {
            try
            {
                // 旧路径：程序根目录\LogConfig.json
                var oldPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogConfig.json");

                if (File.Exists(oldPath) && !File.Exists(LogConfigPath))
                {
                    File.Copy(oldPath, LogConfigPath, false);
                    LogHelper.Info($"已迁移日志配置: {oldPath} -> {LogConfigPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"迁移LogConfig.json失败: {ex.Message}");
            }
        }
    }
}
