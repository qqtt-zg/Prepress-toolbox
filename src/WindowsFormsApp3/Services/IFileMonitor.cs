using System;
using System.IO;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 文件监控服务接口，定义文件监控相关的功能
    /// </summary>
    public interface IFileMonitor
    {
        /// <summary>
        /// 开始监控指定目录
        /// </summary>
        /// <param name="directoryPath">要监控的目录路径</param>
        void StartMonitoring(string directoryPath);

        /// <summary>
        /// 停止监控
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// 获取监控状态
        /// </summary>
        /// <returns>是否正在监控</returns>
        bool IsMonitoring { get; }

        /// <summary>
        /// 当新文件创建时触发的事件
        /// </summary>
        event EventHandler<FileSystemEventArgs> FileCreated;

        /// <summary>
        /// 当文件重命名时触发的事件
        /// </summary>
        event EventHandler<RenamedEventArgs> FileRenamed;

        /// <summary>文件内容更新；同名覆盖可能只触发此事件。</summary>
        event EventHandler<WindowsFormsApp3.Interfaces.FileChangedEventArgs> FileChanged;

        /// <summary>
        /// 当监控发生错误时触发的事件
        /// </summary>
        event EventHandler<ErrorEventArgs> MonitorError;
    }
}
