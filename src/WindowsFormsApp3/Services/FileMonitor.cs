using System;
using System.IO;
using System.Windows.Forms;
using WindowsFormsApp3.Interfaces;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 文件监控服务实现，负责监控指定目录中的文件变化
    /// </summary>
    public class FileMonitor : IFileMonitor
    {
        private FileSystemWatcher _watcher;
        private bool _isMonitoring;
        private WindowsFormsApp3.Interfaces.ILogger _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        public FileMonitor()
        {
            // 注意：此处不直接初始化logger，由ServiceLocator通过属性注入
            
            _watcher = new FileSystemWatcher();
            _watcher.Created += Watcher_Created;
            _watcher.Renamed += Watcher_Renamed;
            _watcher.Changed += Watcher_Changed;
            _watcher.Error += Watcher_Error;
            _watcher.IncludeSubdirectories = false;
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // 修复：仅监控 PDF 文件，与 Form1 的 watcher.Filter = "*.pdf" 保持一致
            _watcher.Filter = "*.pdf";
        }

        /// <summary>
        /// 设置日志服务（用于依赖注入）
        /// </summary>
        /// <param name="logger">日志服务实例</param>
        public void SetLogger(WindowsFormsApp3.Interfaces.ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 当文件变化时触发的事件（实现IFileMonitor接口）
        /// </summary>
        public event EventHandler<WindowsFormsApp3.Interfaces.FileChangedEventArgs> FileChanged;

        /// <summary>
        /// 当新文件创建时触发的事件（实现WindowsFormsApp3.Services.IFileMonitor接口）
        /// </summary>
        public event EventHandler<FileSystemEventArgs> FileCreated;

        /// <summary>
        /// 当文件重命名时触发的事件（实现WindowsFormsApp3.Services.IFileMonitor接口）
        /// </summary>
        public event EventHandler<RenamedEventArgs> FileRenamed;

        /// <summary>
        /// 当监控发生错误时触发的事件
        /// </summary>
        public event EventHandler<ErrorEventArgs> MonitorError;

        /// <summary>
        /// 获取监控状态
        /// </summary>
        public bool IsMonitoring => _isMonitoring;

        /// <summary>
        /// 开始监控指定目录
        /// </summary>
        /// <param name="path">要监控的目录路径</param>
        /// <param name="includeSubdirectories">是否包含子目录</param>
        public void StartMonitoring(string path, bool includeSubdirectories)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("监控目录不能为空", nameof(path));
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException("监控目录不存在: " + path);
            }

            try
            {
                StopMonitoring(); // 先停止之前的监控
                
                _watcher.Path = path;
                _watcher.IncludeSubdirectories = includeSubdirectories;
                _watcher.EnableRaisingEvents = true;
                _isMonitoring = true;
            }
            catch (Exception ex)
            {
                throw new Exception("启动文件监控失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 开始监控指定目录（兼容旧版本）
        /// </summary>
        /// <param name="directoryPath">要监控的目录路径</param>
        public void StartMonitoring(string directoryPath)
        {
            StartMonitoring(directoryPath, false);
        }

        /// <summary>
        /// 停止监控
        /// </summary>
        public void StopMonitoring()
        {
            try
            {
                _watcher.EnableRaisingEvents = false;
                _isMonitoring = false;
            }
            catch (Exception ex)
            {
                throw new Exception("停止文件监控失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 处理文件创建事件
        /// </summary>
        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                // 确保文件完全创建完成
            System.Threading.Thread.Sleep(500);
            var args = new WindowsFormsApp3.Interfaces.FileChangedEventArgs
            {
                FilePath = e.FullPath,
                ChangeType = WindowsFormsApp3.Interfaces.WatcherChangeTypes.Created
            };
            FileChanged?.Invoke(this, args);
            FileCreated?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError(ex, "处理文件创建事件错误");
                }
            }
        }

        /// <summary>
        /// 处理文件重命名事件
        /// </summary>
        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            try
            {
                // 确保文件重命名操作完成
            System.Threading.Thread.Sleep(500);
            var args = new WindowsFormsApp3.Interfaces.FileChangedEventArgs
            {
                FilePath = e.FullPath,
                ChangeType = WindowsFormsApp3.Interfaces.WatcherChangeTypes.Renamed
            };
            FileChanged?.Invoke(this, args);
            FileRenamed?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError(ex, "处理文件重命名事件错误");
                }
            }
        }

        /// <summary>
        /// 处理文件变化事件
        /// </summary>
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                // 确保文件操作完成
                System.Threading.Thread.Sleep(500);
                var args = new WindowsFormsApp3.Interfaces.FileChangedEventArgs
                {
                    FilePath = e.FullPath,
                    ChangeType = WindowsFormsApp3.Interfaces.WatcherChangeTypes.Changed
                };
                FileChanged?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                if (_logger != null)
                {
                    _logger.LogError(ex, "处理文件变化事件错误");
                }
            }
        }

        /// <summary>
        /// 处理监控错误事件
        /// </summary>
        private void Watcher_Error(object sender, ErrorEventArgs e)
        {
            if (e.GetException() != null && _logger != null)
            {
                _logger.LogError(e.GetException(), "文件监控系统错误");
            }
            MonitorError?.Invoke(this, e);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
            _watcher.Dispose();
        }
    }
}