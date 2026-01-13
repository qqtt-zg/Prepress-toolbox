using System;
using PdfSharp.Fonts;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// PDFsharp 字体初始化器
    /// 在应用程序启动时设置自定义字体解析器
    /// </summary>
    public static class PdfSharpFontInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        /// <summary>
        /// 初始化 PDFsharp 字体解析器
        /// 必须在创建任何 XFont 对象之前调用
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_initialized)
                {
                    LogHelper.Debug("PDFsharp 字体解析器已初始化，跳过");
                    return;
                }

                try
                {
                    LogHelper.Info("开始初始化 PDFsharp 字体解析器");

                    // 1. 启用 Windows 字体支持（作为回退）
                    try
                    {
                        GlobalFontSettings.UseWindowsFontsUnderWindows = true;
                        LogHelper.Debug("✓ 已启用 Windows 字体支持");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Warn($"启用 Windows 字体支持失败: {ex.Message}");
                    }

                    // 2. 创建自定义字体解析器
                    var fontResolver = new SystemFontResolver();

                    // 3. 设置为回退字体解析器（而不是主解析器）
                    // 这样 TTC 字体会使用平台解析器，TTF 字体使用自定义解析器
                    GlobalFontSettings.FallbackFontResolver = fontResolver;

                    _initialized = true;
                    LogHelper.Info("✓ PDFsharp 字体解析器初始化成功");
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"初始化 PDFsharp 字体解析器失败: {ex.Message}", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// 检查是否已初始化
        /// </summary>
        public static bool IsInitialized => _initialized;

        /// <summary>
        /// 重置字体管理（仅用于测试）
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                try
                {
                    LogHelper.Debug("重置 PDFsharp 字体管理");
                    GlobalFontSettings.ResetFontManagement();
                    _initialized = false;
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"重置字体管理失败: {ex.Message}", ex);
                }
            }
        }
    }
}
