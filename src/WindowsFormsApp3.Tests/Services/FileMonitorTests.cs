using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Moq;
using WindowsFormsApp3.Services;
using Logger = WindowsFormsApp3.Interfaces.ILogger;

namespace WindowsFormsApp3.Tests.Services
{
    public class FileMonitorTests : IDisposable
    {
        private readonly WindowsFormsApp3.Services.IFileMonitor _fileMonitor;
        private readonly Mock<Logger> _loggerMock;
        private readonly string _testDirectory;
        private bool _fileCreatedEventRaised;
        private bool _fileRenamedEventRaised;

        public FileMonitorTests()
        {
            // 创建测试目录
            _testDirectory = Path.Combine(Path.GetTempPath(), "FileMonitorTests_", DateTime.Now.Ticks.ToString());
            Directory.CreateDirectory(_testDirectory);

            // 创建模拟日志服务
            _loggerMock = new Mock<Logger>();

            // 创建FileMonitor实例
            _fileMonitor = new FileMonitor();
            if (_fileMonitor is FileMonitor fileMonitorImpl)
            {
                fileMonitorImpl.SetLogger(_loggerMock.Object);
            }

            // 订阅事件
            _fileMonitor.FileCreated += (sender, e) => _fileCreatedEventRaised = true;
            _fileMonitor.FileRenamed += (sender, e) => _fileRenamedEventRaised = true;
        }

        [Fact]
        public void StartMonitoring_Should_Start_Monitoring_Successfully()
        {
            // 执行开始监控
            _fileMonitor.StartMonitoring(_testDirectory);

            // 验证监控状态
            Assert.True(_fileMonitor.IsMonitoring);
        }

        [Fact]
        public void StartMonitoring_Should_Throw_Exception_When_Directory_Is_Null()
        {
            // 验证异常
            Assert.Throws<ArgumentException>(() => _fileMonitor.StartMonitoring(null));
            Assert.Throws<ArgumentException>(() => _fileMonitor.StartMonitoring(string.Empty));
        }

        [Fact]
        public void StartMonitoring_Should_Throw_Exception_When_Directory_Does_Not_Exist()
        {
            // 验证异常
            Assert.Throws<DirectoryNotFoundException>(() => 
                _fileMonitor.StartMonitoring(Path.Combine(_testDirectory, "non_existent_directory"))
            );
        }

        [Fact]
        public void StopMonitoring_Should_Stop_Monitoring_Successfully()
        {
            // 先开始监控
            _fileMonitor.StartMonitoring(_testDirectory);
            Assert.True(_fileMonitor.IsMonitoring);

            // 执行停止监控
            _fileMonitor.StopMonitoring();

            // 验证监控状态
            Assert.False(_fileMonitor.IsMonitoring);
        }

        [Fact]
        public async Task FileChangedEvent_ShouldReachMonitorContract_WhenPdfIsOverwritten()
        {
            string destination = Path.Combine(_testDirectory, "同名工单.pdf");
            string source = Path.Combine(_testDirectory, "复制源.tmp");
            File.WriteAllText(destination, "first version");
            File.WriteAllText(source, "second version with different length");
            var changed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<WindowsFormsApp3.Interfaces.FileChangedEventArgs> handler = (sender, args) =>
            {
                if (args.FilePath == destination && args.ChangeType == WindowsFormsApp3.Interfaces.WatcherChangeTypes.Changed)
                    changed.TrySetResult(true);
            };
            _fileMonitor.FileChanged += handler;
            _fileMonitor.StartMonitoring(_testDirectory);
            File.Copy(source, destination, overwrite: true);
            Assert.Same(changed.Task, await Task.WhenAny(changed.Task, Task.Delay(5000)));
        }

        [Fact]
        public async Task FileCreatedEvent_Should_Be_Raised_When_File_Is_Created()
        {
            // 先开始监控
            _fileMonitor.StartMonitoring(_testDirectory);

            // 创建测试文件
            string testFilePath = Path.Combine(_testDirectory, "test_file.pdf");
            File.WriteAllBytes(testFilePath, new byte[] { 0x25, 0x50, 0x44, 0x46 });

            // 等待事件触发
            await Task.Delay(1000);

            // 验证事件是否触发
            Assert.True(_fileCreatedEventRaised);
        }

        [Fact]
        public async Task FileRenamedEvent_Should_Be_Raised_When_File_Is_Renamed()
        {
            // 创建测试文件
            string originalFilePath = Path.Combine(_testDirectory, "original_file.pdf");
            string newFilePath = Path.Combine(_testDirectory, "renamed_file.pdf");
            File.WriteAllBytes(originalFilePath, new byte[] { 0x25, 0x50, 0x44, 0x46 });

            // 开始监控
            _fileMonitor.StartMonitoring(_testDirectory);

            // 重命名文件
            File.Move(originalFilePath, newFilePath);

            // 等待事件触发
            await Task.Delay(1000);

            // 验证事件是否触发
            Assert.True(_fileRenamedEventRaised);
        }

        [Fact]
        public void StartMonitoring_Should_Stop_Previous_Monitoring()
        {
            // 创建第二个测试目录
            string secondTestDirectory = Path.Combine(Path.GetTempPath(), "FileMonitorTests_Second_", DateTime.Now.Ticks.ToString());
            Directory.CreateDirectory(secondTestDirectory);

            try
            {
                // 先监控第一个目录
                _fileMonitor.StartMonitoring(_testDirectory);
                Assert.True(_fileMonitor.IsMonitoring);

                // 再监控第二个目录，这应该会停止对第一个目录的监控
                _fileMonitor.StartMonitoring(secondTestDirectory);
                Assert.True(_fileMonitor.IsMonitoring);
            }
            finally
            {
                // 清理第二个测试目录
                if (Directory.Exists(secondTestDirectory))
                {
                    Directory.Delete(secondTestDirectory, true);
                }
            }
        }

        // 清理测试资源
        public void Dispose()
        {
            // 停止监控
            _fileMonitor.StopMonitoring();

            // 清理测试目录
            if (Directory.Exists(_testDirectory))
            {
                try
                {
                    Directory.Delete(_testDirectory, true);
                }
                catch (IOException)
                {
                    // 如果文件被锁定，忽略异常
                }
            }
        }
    }
}
