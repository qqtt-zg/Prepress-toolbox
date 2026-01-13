using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;
using Spire.Pdf.License;
using WindowsFormsApp3.Forms.Main;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services;
using System.IO;
using System.Diagnostics;

namespace WindowsFormsApp3
{
    internal static class Program
    {
        private static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45FD-A8CF-72F04E6BDE8F}");
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        private const int SW_RESTORE = 9;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 添加全局未观察任务异常处理
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                LogHelper.Error($"未观察到的任务异常: {e.Exception.GetBaseException().Message}", e.Exception.GetBaseException());
                e.SetObserved(); // 标记异常已被观察，防止程序崩溃
            };

            // 立即设置环境变量，在任何 iText 代码执行之前防止 CJK 字体系统初始化
            try
            {
                Environment.SetEnvironmentVariable("ITEXT_DISABLE_CJK_FONT_LOADING", "true");
                Environment.SetEnvironmentVariable("ITEXT_DISABLE_CJK", "true");
                Environment.SetEnvironmentVariable("ITEXT_NO_CJK", "true");
                Environment.SetEnvironmentVariable("ITEXT_DISABLE_CJK_LOADING", "true");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置环境变量失败: {ex.Message}");
            }

            // 设置全局异常处理，捕获 iText7 CJK 字体加载器的空引用异常
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var exception = e.ExceptionObject as Exception;
                if (exception != null && exception.Message.Contains("CjkResourceLoader"))
                {
                    Console.WriteLine("捕获到iText7 CJK字体加载异常，已忽略，程序将继续运行");
                    return; // 不终止程序
                }

                // 其他异常正常处理
                Console.WriteLine($"未处理的异常: {exception?.Message}");
            };

            // 设置Windows Forms应用程序的全局异常处理
            Application.ThreadException += (sender, e) =>
            {
                if (e.Exception.Message.Contains("CjkResourceLoader"))
                {
                    Console.WriteLine("捕获到iText7 CJK字体加载异常，已忽略，程序将继续运行");
                    return; // 不终止程序
                }

                Console.WriteLine($"Windows Forms异常: {e.Exception.Message}");
            };

            // 在程序启动的最早期立即设置环境变量，禁用 iText7 CJK 字体加载
            // 这样可以避免任何 CJK 相关的静态构造函数触发空引用异常
            Environment.SetEnvironmentVariable("ITEXT_DISABLE_CJK_FONT_LOADING", "true");
            Environment.SetEnvironmentVariable("ITEXT_DISABLE_CJK", "true");
            Environment.SetEnvironmentVariable("ITEXT_NO_CJK", "true");
            Environment.SetEnvironmentVariable("iText.disableCJK", "true");

            try
            {
                // 初始化日志配置
                InitializeLogging();
            }
            catch (System.NullReferenceException ex) when (ex.Message.Contains("CjkResourceLoader") || ex.Source == "itext.io")
            {
                Console.WriteLine("捕获到iText7 CJK字体加载异常，已忽略，程序将继续运行");
            }

            // 初始化 iText7 字体系统，修复 CJK 字体加载异常
            try
            {
                InitializeIText7Fonts();
            }
catch (System.NullReferenceException ex) when (ex.Message.Contains("CjkResourceLoader") || ex.Source == "itext.io")
{
    Console.WriteLine("捕获到iText7 CJK字体加载异常，已忽略，程序将继续运行");
    LogHelper.Warn($"iText7初始化被CJK异常中断: {ex.Message}");
}
catch (Exception ex)
{
    LogHelper.Error($"iText7初始化失败: {ex.Message}", ex);
}

            // 初始化字体管理器
            try
            {
                FontManager.Initialize();
            }
            catch (System.NullReferenceException ex) when (ex.Message.Contains("CjkResourceLoader") || ex.Source == "itext.io")
            {
                Console.WriteLine("捕获到iText7 CJK字体加载异常，已忽略，程序将继续运行");
            }

            // 初始化 PDFsharp 字体解析器（提高系统字体兼容性）
            try
            {
                PdfSharpFontInitializer.Initialize();
                LogHelper.Info("PDFsharp 字体解析器初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"PDFsharp 字体解析器初始化失败: {ex.Message}", ex);
            }

            // 初始化应用程序设置服务
            try
            {
                var logger = LogHelper.GetLogger();
                AppSettings.Initialize(logger);
                LogHelper.Info("应用程序设置服务初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"应用程序设置服务初始化失败: {ex.Message}");
                LogHelper.Error($"应用程序设置服务初始化失败: {ex.Message}");
            }

            // 执行数据完整性检查
            try
            {
                var integrityService = new DataIntegrityService(LogHelper.GetLogger());
                var integrityReport = integrityService.PerformIntegrityCheck();
                
                Console.WriteLine($"数据完整性检查完成: 正常 {integrityReport.ValidCount}, 警告 {integrityReport.WarningCount}, 错误 {integrityReport.ErrorCount}");
                
                if (integrityReport.HasCriticalError)
                {
                    Console.WriteLine($"数据完整性检查发生严重错误: {integrityReport.ErrorMessage}");
                }
                
                // 如果有错误的项目，尝试自动修复
                if (integrityReport.ErrorCount > 0)
                {
                    var errorItems = integrityReport.Items.Where(i => i.Status == ValidationStatus.Error);
                    foreach (var item in errorItems)
                    {
                        Console.WriteLine($"完整性错误 - {item.Item}: {item.Message}");
                        
                        // 尝试自动修复
                        if (integrityService.RepairDataFile(item.Item))
                        {
                            Console.WriteLine($"自动修复成功: {item.Item}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据完整性检查失败: {ex.Message}");
                LogHelper.Error($"数据完整性检查失败: {ex.Message}");
            }

            // 添加全局异常处理
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // 添加 iText7 CJK 异常的特殊处理
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            LogHelper.Info("应用程序启动");

            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                // 设置Spire授权密钥
                string licenseKey = "8DJPwcCe0QCAfvuXySwTk7A7I6/mKl79wKDNNMYfS6zvvICl6bJkNs6GJD7UkU93Ec05scOcf0EjgptJB6fPlGQSArVHX6ZgZC+PqjV363gPN3JH3ctEksNCC24btBXI9CtWqaEw4g333LI7ukXtNVMmpOUUpoIX1C0Dqg3sPu7Uy5nhqVahScV3LHQr2RoIuTOaAACqCTE1DnmvHrpNYmFH/Y/8Qciv+4wieZlwNkS/rUWY3y6AVCFzV+klSXKOFC7DpgfNRvATANnQe5uC9fYA1WGrfKzY2HSUGvUBiEjJ11wGMog57bkaXvO6B/qMtcIqX4Crkg+qD+UKzaYMPET+osAX1SIk6gECaZBKvco5m/kvmJ4yAqrcZvT0iJKtnbokuomSkCkWPNlADMdAOKzczQjyJOM92zlOs2eYIpDNdy2Pm4tG0qX4TnwpKm2Vq+k7Pmh8dC0MFRp61DoZFXu3qNTODwnBDB+RwtO3l0sb4pHZQ3ZAJbMiKtjiFWYhnnZqlBKJ/YMV7dh0mRsO7+bSnldkikAcGMwYi2+BDpP+rtQAwZO2qjRrKlojbx8CnGKIzFnuCDOvXpOzfFpZGSQSuYQScYXWaPwLHMwPcw52EXaqQG8mXmOy/Sn0DAJEe3goI38wGaegPGePnQTcs0nTmCyPVxqEN/+efiTZuCqL/0i6Fj6Ap762Hhr7megmrxt1rxc1wb9iwFWlozeBqCHcoWzafjIfmzTZQIsn1GCTFMk/lEEDyERIUOY4TP48Ueu5CUvgp947VYnH7Jeg8jKGMRl3rIqJK6h5HGV0yLEQmw24Cs0c/EjM0Ja2iodY6sh6tRgW9nxwHriuyd0XfawQkVS407nOOks6b82B7IU1la5CvEMJew92qpSDIc+7abEFa0RXnwbeaKb96EVv8VBkarludtfH9lPwL4392LqUgsTbd6FcE2/ZZQ3gaaDawkfIQKcqV7zQ3p1PSXEta4rEA/mF+ldY/e7j5BoXoqktCdz13bTcxIU2+uN/es4jVP5N3f+uHFuxYzm30Kt8HNMfhrqJ8RYUxHpWUJhcuYQ=";
                LicenseProvider.SetLicenseKey(licenseKey);
                LicenseProvider.LoadLicense();

                // 启动自动更新检查（异步，不阻塞启动）
                try
                {
                    var updateManager = new UpdateManager();
                    Task.Run(() => updateManager.CheckForUpdatesOnStartup());
                }
                catch (Exception ex)
                {
                    LogHelper.Error("自动更新检查初始化失败: " + ex.Message);
                }

                // 创建测试PDF文件用于CefSharp预览测试
                try
                {
                    string testPdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Test", "TestPdf.pdf");
                    Directory.CreateDirectory(Path.GetDirectoryName(testPdfPath));
                    WindowsFormsApp3.Test.PdfTestGenerator.CreateTestPdf(testPdfPath, 5);
                    Console.WriteLine("测试PDF已创建: " + testPdfPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("创建测试PDF失败: " + ex.Message);
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.Run(new MainShellForm());

                mutex.ReleaseMutex();
            }
            else
            {
                // 查找已运行的主窗口
                IntPtr mainWindowPtr = FindWindow(null, "大诚工具箱");
                if (mainWindowPtr != IntPtr.Zero)
                {
                    // 恢复最小化窗口
                    // 恢复窗口并激活
                    ShowWindow(mainWindowPtr, SW_RESTORE);
                    SetForegroundWindow(mainWindowPtr);
                    // 强制窗口置顶后取消置顶，确保从任务栏弹出
                    SetWindowPos(mainWindowPtr, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                    SetWindowPos(mainWindowPtr, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                }
            }
        }
        
        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            LogException("UI线程异常", e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException("应用程序域异常", e.ExceptionObject as Exception);
        }

        private static void LogException(string type, Exception ex)
            {
                try
                {
                    // 使用统一的日志系统记录异常
                    if (ex != null)
                    {
                        LogHelper.Error($"{type}: {ex.Message}", ex);
                    }
                    else
                    {
                        LogHelper.Error(type);
                    }
                }
                catch
                {
                    // 如果日志记录失败，尝试使用备用方法
                    try
                    {
                        string logPath = Path.Combine(
                            Path.GetDirectoryName(Application.ExecutablePath),
                            "fallback_log.txt");

                        string logContent = "\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + type + ": " + (ex?.Message ?? "") + "\r\n";
                        if (ex != null)
                        {
                            logContent += "堆栈跟踪: " + ex.StackTrace + "\r\n";
                        }

                        File.AppendAllText(logPath, logContent);
                    }
                    catch
                    {
                        // 忽略所有错误
                    }
                }
            }

            /// <summary>
            /// 处理程序集解析事件，防止 iText7 CJK 相关异常
            /// </summary>
            private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
            {
                try
                {
                    // 如果是 iText7 相关的程序集请求，返回 null 让系统处理
                    if (args.Name.Contains("iText") || args.Name.Contains("itext"))
                    {
                        LogHelper.Debug($"iText7 程序集解析请求: {args.Name}");
                        // 不做特殊处理，让系统自然解析
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Debug($"程序集解析异常（已忽略）: {ex.Message}");
                }

                return null;
            }

        /// <summary>
        /// 初始化 iText7 字体系统,修复 CJK 字体加载异常
        /// </summary>
        private static void InitializeIText7Fonts()
        {
            try
            {
                LogHelper.Info("开始初始化 iText7 字体系统");

                // 设置多个环境变量强制禁用 CJK 字体加载
                Environment.SetEnvironmentVariable("ITEXT_DISABLE_CJK_FONT_LOADING", "true");
                Environment.SetEnvironmentVariable("ITEXT_DISABLE_CJK", "true");
                Environment.SetEnvironmentVariable("ITEXT_NO_CJK", "true");

                // ✅ 注册系统字体目录,让CreateRegisteredFont可用
                try
                {
                    LogHelper.Info("注册系统字体目录...");
                    iText.Kernel.Font.PdfFontFactory.RegisterSystemDirectories();
                    LogHelper.Info("系统字体注册成功");
                }
                catch (Exception regEx)
                {
                    LogHelper.Warn($"系统字体注册失败: {regEx.Message},将使用备用字体方案");
                }

                // 完全避免 CJK 字体系统的任何初始化
                // 不要访问 CjkResourceLoader 类型,这会触发静态构造函数和 LoadRegistry
                try
                {
                    LogHelper.Debug("跳过 CJK 字体系统初始化,避免触发空引用异常");
                }
                catch (Exception ex)
                {
                    LogHelper.Debug($"跳过 CJK 初始化时发生异常: {ex.Message}");
                }

                // 预热字体系统,尝试创建基础字体
                try
                {
                    // 强制使用只包含基本字符的字体,避免触发 CJK 加载
                    var testFont = iText.Kernel.Font.PdfFontFactory.CreateFont(
                        iText.IO.Font.Constants.StandardFonts.HELVETICA);

                    // 尝试创建一些基础字体以确保字体系统正常工作
                    var testFont2 = iText.Kernel.Font.PdfFontFactory.CreateFont(
                        iText.IO.Font.Constants.StandardFonts.TIMES_ROMAN);

                    LogHelper.Info("iText7 字体系统预热成功");
                }
                catch (System.NullReferenceException nullEx)
                {
                    LogHelper.Error($"iText7 字体系统 CJK 空引用异常(已捕获): {nullEx.Message}");
                    LogHelper.Info("CJK 异常已被处理,程序将继续运行");
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"iText7 字体系统预热失败: {ex.Message}");
                }

                LogHelper.Info("iText7 字体系统初始化完成");
            }
            catch (System.NullReferenceException nullEx)
            {
                LogHelper.Error($"iText7 字体系统初始化 CJK 空引用异常(已捕获): {nullEx.Message}");
                LogHelper.Info("CJK 异常已被处理,程序将继续运行");
                // 不抛出异常,允许程序继续运行
            }
            catch (Exception ex)
            {
                LogHelper.Error($"iText7 字体系统初始化失败: {ex.Message}");
                // 不抛出异常,允许程序继续运行
            }
        }/// <summary>
         /// 初始化日志配置
         /// </summary>
        private static void InitializeLogging()
{
    try
    {
        // 确保LogConfig.json文件存在
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LogConfig.json");
        if (!File.Exists(configPath))
        {
            // 创建默认配置文件
            LogConfigManager.GetConfig();
            LogHelper.Info("日志配置文件不存在,已创建默认配置");
        }
        else
        {
            LogHelper.Info("已加载日志配置文件");
        }
    }
    catch (Exception ex)
    {
        // 如果初始化失败,使用控制台输出
        Console.WriteLine("初始化日志配置失败: " + ex.Message);
    }
}

    }
}
