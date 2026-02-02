using System;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 回收站图标切换服务：支持在默认图标和 Popcat 猫猫图标之间切换
    /// </summary>
    public class RecycleBinIconChanger
    {
        private const string RegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CLSID\{645FF040-5081-101B-9F08-00AA002F954E}\DefaultIcon";
        private const string IconDir = @"C:\小猫图标\";
        private const string CatFullIcon = @"C:\小猫图标\cat_full.dll,0";
        private const string CatEmptyIcon = @"C:\小猫图标\cat_empty.dll,0";
        private const string DefaultFullIcon = @"%SystemRoot%\System32\imageres.dll,-54";
        private const string DefaultEmptyIcon = @"%SystemRoot%\System32\imageres.dll,-55";

        private bool _isCatIcon = false;  // 当前是否为猫猫图标

        /// <summary>
        /// 构造函数：初始化并复制资源文件
        /// </summary>
        public RecycleBinIconChanger()
        {
            try
            {
                // 创建图标目录
                if (!Directory.Exists(IconDir))
                {
                    Directory.CreateDirectory(IconDir);
                    LogHelper.Info($"创建图标目录: {IconDir}");
                }

                // 复制图标资源文件
                CopyIconResources();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"初始化 RecycleBinIconChanger 失败: {ex.Message}, 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 从嵌入资源复制图标文件到目标目录
        /// </summary>
        private void CopyIconResources()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceNames = new[] { "cat_empty.dll", "cat_full.dll" };

                foreach (var resourceName in resourceNames)
                {
                    // 尝试不同的资源路径格式
                    var resourcePaths = new[]
                    {
                        $"WindowsFormsApp3.Resources.{resourceName}",
                        $"Resources.{resourceName}",
                        resourceName
                    };

                    string resourcePath = null;
                    foreach (var path in resourcePaths)
                    {
                        var stream = assembly.GetManifestResourceStream(path);
                        if (stream != null)
                        {
                            resourcePath = path;
                            stream.Dispose();
                            break;
                        }
                    }

                    if (resourcePath == null)
                    {
                        LogHelper.Warn($"未找到嵌入资源: {resourceName}");
                        continue;
                    }

                    string destPath = Path.Combine(IconDir, resourceName);

                    // 如果文件已存在且大小相同，跳过复制
                    if (File.Exists(destPath))
                    {
                        using (var resourceStream = assembly.GetManifestResourceStream(resourcePath))
                        {
                            if (resourceStream != null && resourceStream.Length == new FileInfo(destPath).Length)
                            {
                                LogHelper.Info($"图标文件已存在且大小相同，跳过复制: {destPath}");
                                continue;
                            }
                        }
                    }

                    // 复制资源文件
                    using (var resourceStream = assembly.GetManifestResourceStream(resourcePath))
                    {
                        if (resourceStream != null)
                        {
                            using (var fileStream = new FileStream(destPath, FileMode.Create, FileAccess.Write))
                            {
                                resourceStream.CopyTo(fileStream);
                                LogHelper.Info($"成功复制图标资源: {resourceName} -> {destPath}");
                            }
                        }
                        else
                        {
                            LogHelper.Warn($"无法打开资源流: {resourcePath}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"复制图标资源失败: {ex.Message}, 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 往返切换图标（默认 ↔ 猫猫）
        /// </summary>
        public void ToggleIcon()
        {
            try
            {
                _isCatIcon = !_isCatIcon;

                if (_isCatIcon)
                {
                    SetCatIcon();
                }
                else
                {
                    SetDefaultIcon();
                }

                // 刷新桌面使图标更改生效
                RefreshDesktop();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"切换回收站图标失败: {ex.Message}, 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 设置为猫猫图标
        /// </summary>
        public void SetCatIcon()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("full", CatFullIcon);
                        key.SetValue("empty", CatEmptyIcon);
                        _isCatIcon = true;
                        LogHelper.Info($"已设置回收站图标为猫猫图标");
                    }
                    else
                    {
                        LogHelper.Warn($"无法打开注册表路径: {RegistryPath}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.Error($"设置猫猫图标失败：权限不足。可能需要管理员权限。错误: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"设置猫猫图标失败: {ex.Message}, 堆栈: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 设置为默认图标
        /// </summary>
        public void SetDefaultIcon()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPath, true))
                {
                    if (key != null)
                    {
                        key.SetValue("full", DefaultFullIcon);
                        key.SetValue("empty", DefaultEmptyIcon);
                        _isCatIcon = false;
                        LogHelper.Info($"已设置回收站图标为默认图标");
                    }
                    else
                    {
                        LogHelper.Warn($"无法打开注册表路径: {RegistryPath}");
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                LogHelper.Error($"设置默认图标失败：权限不足。可能需要管理员权限。错误: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"设置默认图标失败: {ex.Message}, 堆栈: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 获取当前是否为猫猫图标
        /// </summary>
        public bool IsCatIcon => _isCatIcon;

        /// <summary>
        /// 刷新桌面使图标更改生效
        /// </summary>
        private void RefreshDesktop()
        {
            try
            {
                IntPtr hWnd = FindWindow("Progman", "Program Manager");
                if (hWnd != IntPtr.Zero)
                {
                    PostMessage(hWnd, WM_COMMAND, (IntPtr)F5_REFRESH, IntPtr.Zero);
                    LogHelper.Info("已发送刷新桌面消息");
                }
                else
                {
                    LogHelper.Warn("未找到桌面窗口句柄");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"刷新桌面失败: {ex.Message}");
            }
        }

        #region Windows API 声明

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_COMMAND = 0x0111;
        private const uint F5_REFRESH = 0x0002; // F5 刷新

        #endregion
    }
}
