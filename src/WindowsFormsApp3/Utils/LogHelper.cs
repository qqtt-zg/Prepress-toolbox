using WindowsFormsApp3.Interfaces;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// 日志帮助类，提供统一的日志记录接口
    /// </summary>
    public static class LogHelper
    {
        private static Interfaces.ILogger _logger;
        private static bool _isInitialized = false;
        private static readonly object _syncLock = new object();
        
        // 日志文件路径和配置
        private static string _logDirectory = AppDataPathManager.LogsDirectory;
        private static string _logFileNameFormat = "log_{0:yyyyMMdd}.txt";
        private const int _bufferSize = 50; // 默认缓冲大小
        private const int _maxLogAgeDays = 7; // 日志保留天数

        /// <summary>
        /// 获取日志记录器实例
        /// </summary>
        public static Interfaces.ILogger Logger
        {
            get
            {
                if (!_isInitialized)
                {
                    lock (_syncLock)
                    {
                        if (!_isInitialized)
                        {
                            InitializeLogger();
                            CleanupOldLogs();
                        }
                    }
                }
                return _logger;
            }
        }

        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        private static void CleanupOldLogs()
        {
            try
            {
                if (!Directory.Exists(_logDirectory)) return;

                var directory = new DirectoryInfo(_logDirectory);
                var files = directory.GetFiles("log_*.txt");
                var cutoffDate = DateTime.Now.AddDays(-_maxLogAgeDays);

                foreach (var file in files)
                {
                    if (file.LastWriteTime < cutoffDate)
                    {
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception ex)
                        {
                            // 无法删除则忽略，可能文件正在被使用
                            Trace.WriteLine($"无法删除旧日志文件 {file.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"清理旧日志失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取日志记录器实例的静态方法
        /// </summary>
        public static Interfaces.ILogger GetLogger()
        {
            return Logger;
        }

        /// <summary>
        /// 初始化日志记录器
        /// </summary>
        private static void InitializeLogger()
        {
            try
            {
                // 优先使用ServiceLocator中的日志服务
                if (ServiceLocator.Instance != null)
                {
                    _logger = ServiceLocator.Instance.GetLogger();
                }
                
                // 如果ServiceLocator不可用或获取日志服务失败，使用BufferedFileLogger
                if (_logger == null)
                {
                    // 确保日志目录存在
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }
                    
                    _logger = new BufferedFileLogger(_logDirectory, _logFileNameFormat, _bufferSize);
                }
            }
            catch (Exception ex)
            {
                // 如果获取失败，记录到控制台输出
                Console.WriteLine("Failed to initialize logger: " + ex.Message);
                
                // 创建一个简单的控制台日志实现作为后备
                _logger = new ConsoleLogger();
            }
            finally
            {
                _isInitialized = true;
            }
        }

        /// <summary>
        /// 处理日志消息，进行敏感信息隐藏
        /// </summary>
        /// <param name="message">原始日志消息</param>
        /// <returns>处理后的日志消息</returns>
        private static string ProcessLogMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            string processedMessage = message;
            
            // 使用简单的字符串替换，避免复杂的正则表达式转义问题
            if (processedMessage.Contains("密码") || processedMessage.Contains("token") || 
                processedMessage.Contains("secret") || processedMessage.Contains("连接字符串"))
            {
                // 简单替换敏感信息关键字后的内容
                string[] sensitiveKeywords = { "密码", "token", "secret", "连接字符串" };
                foreach (string keyword in sensitiveKeywords)
                {
                    int pos = processedMessage.IndexOf(keyword);
                    if (pos >= 0)
                    {
                        // 找到关键字后的位置，进行简单替换
                        int endPos = FindEndOfSensitiveData(processedMessage, pos + keyword.Length);
                        if (endPos > pos + keyword.Length)
                        {
                            processedMessage = processedMessage.Substring(0, pos + keyword.Length) + 
                                              " [敏感信息已隐藏]" + 
                                              processedMessage.Substring(endPos);
                        }
                    }
                }
            }

            return processedMessage;
        }

        /// <summary>
        /// 查找敏感数据的结束位置
        /// </summary>
        private static int FindEndOfSensitiveData(string message, int startPos)
        {
            // 从开始位置寻找下一个逗号、分号、换行符或空格
            for (int i = startPos; i < message.Length; i++)
            {
                if (message[i] == ',' || message[i] == '，' || message[i] == ';' || 
                    message[i] == '\n' || message[i] == '\r' || message[i] == '\t' || 
                    message[i] == ' ' || message[i] == '"' || message[i] == '\'')
                {
                    return i;
                }
            }
            return message.Length;
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Debug(string message)
        {
            Logger.LogDebug(ProcessLogMessage(message));
        }

        /// <summary>
        /// 异步记录调试信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static Task DebugAsync(string message)
        {
            return Task.Run(() => Logger.LogDebug(ProcessLogMessage(message)));
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Info(string message)
        {
            Logger.LogInformation(ProcessLogMessage(message));
        }

        /// <summary>
        /// 异步记录信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static Task InfoAsync(string message)
        {
            return Task.Run(() => Logger.LogInformation(ProcessLogMessage(message)));
        }

        /// <summary>
        /// 记录警告信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Warn(string message)
        {
            Logger.LogWarning(ProcessLogMessage(message));
        }

        /// <summary>
        /// 异步记录警告信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static Task WarnAsync(string message)
        {
            return Task.Run(() => Logger.LogWarning(ProcessLogMessage(message)));
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Error(string message)
        {
            Logger.LogError(ProcessLogMessage(message));
        }

        /// <summary>
        /// 异步记录错误信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static Task ErrorAsync(string message)
        {
            return Task.Run(() => Logger.LogError(ProcessLogMessage(message)));
        }

        /// <summary>
        /// 记录错误信息和异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常对象</param>
        public static void Error(string message, Exception ex)
        {
            Logger.LogError(ex, ProcessLogMessage(message));
        }

        /// <summary>
        /// 异步记录错误信息和异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常对象</param>
        public static Task ErrorAsync(string message, Exception ex)
        {
            return Task.Run(() => Logger.LogError(ex, ProcessLogMessage(message)));
        }

        /// <summary>
        /// 记录严重错误信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static void Critical(string message)
        {
            Logger.LogCritical(ProcessLogMessage(message));
        }

        /// <summary>
        /// 异步记录严重错误信息
        /// </summary>
        /// <param name="message">日志消息</param>
        public static Task CriticalAsync(string message)
        {
            return Task.Run(() => Logger.LogCritical(ProcessLogMessage(message)));
        }

        /// <summary>
        /// 记录严重错误信息和异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常对象</param>
        public static void Critical(string message, Exception ex)
        {
            Logger.LogError(ex, ProcessLogMessage(message));
        }

        /// <summary>
        /// 异步记录严重错误信息和异常
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="ex">异常对象</param>
        public static Task CriticalAsync(string message, Exception ex)
        {
            return Task.Run(() => Logger.LogError(ex, ProcessLogMessage(message)));
        }
    }

    /// <summary>
    /// 缓冲文件日志记录器
    /// </summary>
    internal class BufferedFileLogger : Interfaces.ILogger
    {
        private readonly string _logDirectory;
        private readonly string _logFileNameFormat;
        private readonly int _bufferSize;
        private readonly object _fileLock = new object();
        private readonly StringBuilder _buffer;
        private int _bufferCount;

        public BufferedFileLogger(string logDirectory, string logFileNameFormat, int bufferSize)
        {
            _logDirectory = logDirectory;
            _logFileNameFormat = logFileNameFormat;
            _bufferSize = bufferSize;
            _buffer = new StringBuilder();
            _bufferCount = 0;
        }

        public void Log(WindowsFormsApp3.Interfaces.LogLevel level, string message)
        {
            WriteToLog(level, message);
        }

        public void LogInformation(string message)
        {
            WriteToLog(WindowsFormsApp3.Interfaces.LogLevel.Information, message);
        }

        public void LogWarning(string message)
        {
            WriteToLog(WindowsFormsApp3.Interfaces.LogLevel.Warning, message);
        }

        public void LogError(string message)
        {
            WriteToLog(WindowsFormsApp3.Interfaces.LogLevel.Error, message);
        }

        public void LogError(Exception ex, string message)
        {
            string fullMessage = message + "\n" + ex.ToString() + "\n" + ex.StackTrace;
            WriteToLog(WindowsFormsApp3.Interfaces.LogLevel.Error, fullMessage);
            // 错误日志立即刷新缓冲区
            FlushBuffer();
        }

        public void LogDebug(string message)
        {
            WriteToLog(WindowsFormsApp3.Interfaces.LogLevel.Debug, message);
        }

        public void LogCritical(string message)
        {
            WriteToLog(WindowsFormsApp3.Interfaces.LogLevel.Critical, message);
            // 严重错误日志立即刷新缓冲区
            FlushBuffer();
        }

        public void LogCritical(Exception ex, string message)
        {
            string fullMessage = message + "\n" + ex.ToString() + "\n" + ex.StackTrace;
            WriteToLog(WindowsFormsApp3.Interfaces.LogLevel.Critical, fullMessage);
            // 严重错误日志立即刷新缓冲区
            FlushBuffer();
        }

        private void WriteToLog(WindowsFormsApp3.Interfaces.LogLevel level, string message)
        {
            try
            {
                string logEntry = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + level.ToString().ToUpper() + "] " + message + Environment.NewLine;
                
                lock (_fileLock)
                {
                    _buffer.Append(logEntry);
                    _bufferCount++;
                    
                    // 当缓冲区达到设定大小时，刷新到文件
                    if (_bufferCount >= _bufferSize)
                    {
                        FlushBuffer();
                    }
                }
            }
            catch (Exception)
            {
                // 如果写入缓冲区失败，静默忽略
            }
        }

        /// <summary>
        /// 强制刷新缓冲区，将所有缓冲的日志写入文件
        /// </summary>
        public void Flush()
        {
            FlushBuffer();
        }

        private void FlushBuffer()
        {
            try
            {
                string logFileName = string.Format(_logFileNameFormat, DateTime.Now);
                string logFilePath = Path.Combine(_logDirectory, logFileName);
                
                // 确保文件存在
                using (StreamWriter writer = new StreamWriter(logFilePath, true, Encoding.UTF8))
                {
                    writer.Write(_buffer.ToString());
                }
                
                // 清空缓冲区
                _buffer.Clear();
                _bufferCount = 0;
            }
            catch (Exception)
            {
                // 如果刷新失败，静默忽略
            }
        }
    }

    /// <summary>
    /// 控制台日志记录器实现
    /// </summary>
    internal class ConsoleLogger : Interfaces.ILogger
    {
        public void Log(WindowsFormsApp3.Interfaces.LogLevel level, string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");
        }

        public void LogInformation(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Information] {message}");
        }

        public void LogWarning(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Warning] {message}");
        }

        public void LogError(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Error] {message}");
        }

        public void LogError(Exception ex, string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Error] {message}\n{ex.Message}\n{ex.StackTrace}");
        }

        public void LogDebug(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Debug] {message}");
        }

        public void LogCritical(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Critical] {message}");
        }

        public void LogCritical(Exception ex, string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Critical] {message}\n{ex.Message}\n{ex.StackTrace}");
        }
    }
}