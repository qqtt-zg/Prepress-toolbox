using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Utils
{
    /// <summary>
    /// DWM API 封装类，用于实现 Acrylic 效果和透明窗口
    /// 支持 Windows 7 及更高版本
    /// </summary>
    internal static class FloatingDropZoneDwm
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }

        // DWM API 常量
        public const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        public const int DWMSBT_MAINWINDOW = 2; // Acrylic
        public const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic for transient windows
        public const int DWMSBT_TABBEDWINDOW = 4; // Tabbed window backdrop

        // DWM API P/Invoke 声明
        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern int DwmSetWindowAttribute(IntPtr hWnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = false)]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        /// <summary>
        /// 检测是否为 Windows 11 或更高版本
        /// </summary>
        public static bool IsWindows11OrLater()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                // Windows 11 的版本号是 10.0.22000 或更高
                return version.Major > 10 || (version.Major == 10 && version.Build >= 22000);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检测是否为 Windows 10 1903 或更高版本
        /// </summary>
        public static bool IsWindows10_1903OrLater()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                // Windows 10 1903 的版本号是 10.0.18362
                return version.Major == 10 && version.Build >= 18362;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检测是否为 Windows 7
        /// </summary>
        public static bool IsWindows7()
        {
            try
            {
                var version = Environment.OSVersion.Version;
                return version.Major == 6 && version.Minor == 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检测是否支持 Acrylic API (DWMWA_SYSTEMBACKDROP_TYPE)
        /// 需要 Windows 10 1903 或更高版本
        /// </summary>
        public static bool SupportsAcrylicAPI()
        {
            return IsWindows10_1903OrLater();
        }

        /// <summary>
        /// 检测是否支持 DWM
        /// Windows 7 及更高版本都支持 DWM
        /// </summary>
        public static bool SupportsDWM()
        {
            try
            {
                bool enabled = false;
                int result = DwmIsCompositionEnabled(out enabled);
                return result == 0 && enabled;
            }
            catch
            {
                // 如果 DWM API 不可用，返回 false
                return false;
            }
        }

        /// <summary>
        /// 启用 Acrylic 效果（Windows 11/10 1903+）
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否成功</returns>
        public static bool EnableAcrylic(IntPtr hWnd)
        {
            try
            {
                if (!SupportsAcrylicAPI())
                {
                    LogHelper.Warn("当前 Windows 版本不支持 Acrylic API");
                    return false;
                }

                if (!SupportsDWM())
                {
                    LogHelper.Warn("DWM 未启用，无法使用 Acrylic 效果");
                    return false;
                }

                // 扩展框架到客户区域（全窗口效果）
                MARGINS margins = new MARGINS
                {
                    cxLeftWidth = -1,
                    cxRightWidth = -1,
                    cyTopHeight = -1,
                    cyBottomHeight = -1
                };

                int result = DwmExtendFrameIntoClientArea(hWnd, ref margins);
                if (result != 0)
                {
                    LogHelper.Warn($"DwmExtendFrameIntoClientArea 失败，错误代码: {result}");
                    return false;
                }

                // 设置 Acrylic 背景类型
                int backdropType = DWMSBT_MAINWINDOW;
                result = DwmSetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                if (result != 0)
                {
                    LogHelper.Warn($"DwmSetWindowAttribute 失败，错误代码: {result}");
                    return false;
                }

                LogHelper.Info("Acrylic 效果已启用");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"启用 Acrylic 效果失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启用 DWM 透明效果（Windows 7/8/8.1/10 旧版）
        /// 使用 DwmExtendFrameIntoClientArea 实现基本透明效果
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>是否成功</returns>
        public static bool EnableDwmTransparency(IntPtr hWnd)
        {
            try
            {
                if (!SupportsDWM())
                {
                    LogHelper.Warn("DWM 未启用，无法使用透明效果");
                    return false;
                }

                // 扩展框架到客户区域（全窗口效果）
                MARGINS margins = new MARGINS
                {
                    cxLeftWidth = -1,
                    cxRightWidth = -1,
                    cyTopHeight = -1,
                    cyBottomHeight = -1
                };

                int result = DwmExtendFrameIntoClientArea(hWnd, ref margins);
                if (result != 0)
                {
                    LogHelper.Warn($"DwmExtendFrameIntoClientArea 失败，错误代码: {result}");
                    return false;
                }

                LogHelper.Info("DWM 透明效果已启用");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"启用 DWM 透明效果失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 禁用 DWM 效果
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        public static void DisableDwmEffect(IntPtr hWnd)
        {
            try
            {
                // 将边距设置为 0，禁用 DWM 效果
                MARGINS margins = new MARGINS
                {
                    cxLeftWidth = 0,
                    cxRightWidth = 0,
                    cyTopHeight = 0,
                    cyBottomHeight = 0
                };

                DwmExtendFrameIntoClientArea(hWnd, ref margins);
                LogHelper.Info("DWM 效果已禁用");
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"禁用 DWM 效果失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据 Windows 版本自动选择最佳的透明效果方案
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <returns>使用的方案类型</returns>
        public static TransparencyMode EnableTransparency(IntPtr hWnd)
        {
            // 优先尝试 Acrylic API（Windows 11/10 1903+）
            if (SupportsAcrylicAPI() && EnableAcrylic(hWnd))
            {
                return TransparencyMode.Acrylic;
            }

            // 回退到 DWM 透明效果（Windows 7+）
            if (EnableDwmTransparency(hWnd))
            {
                return TransparencyMode.Dwm;
            }

            // 如果都失败，返回 None
            return TransparencyMode.None;
        }
    }

    /// <summary>
    /// 透明效果模式
    /// </summary>
    public enum TransparencyMode
    {
        /// <summary>
        /// 无透明效果
        /// </summary>
        None,

        /// <summary>
        /// DWM 基本透明效果（Windows 7+）
        /// </summary>
        Dwm,

        /// <summary>
        /// Acrylic 效果（Windows 11/10 1903+）
        /// </summary>
        Acrylic
    }
}
