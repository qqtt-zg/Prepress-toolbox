using System;
using System.IO;
using WindowsFormsApp3.Services;
using Xunit;

namespace WindowsFormsApp3.Tests.Services
{
    public class CancelledFileServiceTests
    {
        [Fact]
        public void MoveToCancelledFolder_ShouldCreateFolderAndMoveFile()
        {
            string monitorDirectory = Path.Combine(Path.GetTempPath(), $"cancelled-file-{Guid.NewGuid():N}");
            Directory.CreateDirectory(monitorDirectory);
            string sourcePath = Path.Combine(monitorDirectory, "待处理.pdf");
            File.WriteAllText(sourcePath, "test");

            try
            {
                var service = new CancelledFileService();

                CancelledFileMoveResult result = service.MoveToCancelledFolder(sourcePath, monitorDirectory);

                Assert.True(result.Success, result.ErrorMessage);
                Assert.False(File.Exists(sourcePath));
                Assert.True(File.Exists(Path.Combine(monitorDirectory, "取消处理", "待处理.pdf")));
            }
            finally
            {
                Directory.Delete(monitorDirectory, true);
            }
        }

        [Fact]
        public void MoveToCancelledFolder_ShouldRejectFileOutsideMonitorDirectory()
        {
            string root = Path.Combine(Path.GetTempPath(), $"cancelled-file-root-{Guid.NewGuid():N}");
            string monitorDirectory = Path.Combine(root, "monitor");
            string outsideDirectory = Path.Combine(root, "outside");
            Directory.CreateDirectory(monitorDirectory);
            Directory.CreateDirectory(outsideDirectory);
            string sourcePath = Path.Combine(outsideDirectory, "outside.pdf");
            File.WriteAllText(sourcePath, "test");

            try
            {
                var service = new CancelledFileService();

                CancelledFileMoveResult result = service.MoveToCancelledFolder(sourcePath, monitorDirectory);

                Assert.False(result.Success);
                Assert.True(File.Exists(sourcePath));
                Assert.False(Directory.Exists(Path.Combine(monitorDirectory, "取消处理")));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Fact]
        public void MoveToCancelledFolder_WhenNameExists_ShouldPreserveBothFiles()
        {
            string monitorDirectory = Path.Combine(Path.GetTempPath(), $"cancelled-file-duplicate-{Guid.NewGuid():N}");
            string cancelledDirectory = Path.Combine(monitorDirectory, "取消处理");
            Directory.CreateDirectory(cancelledDirectory);
            string sourcePath = Path.Combine(monitorDirectory, "重复.pdf");
            File.WriteAllText(sourcePath, "new");
            File.WriteAllText(Path.Combine(cancelledDirectory, "重复.pdf"), "old");

            try
            {
                var service = new CancelledFileService();

                CancelledFileMoveResult result = service.MoveToCancelledFolder(sourcePath, monitorDirectory);

                Assert.True(result.Success, result.ErrorMessage);
                Assert.Equal("重复 (1).pdf", Path.GetFileName(result.DestinationPath));
                Assert.Equal("old", File.ReadAllText(Path.Combine(cancelledDirectory, "重复.pdf")));
                Assert.Equal("new", File.ReadAllText(result.DestinationPath));
            }
            finally
            {
                Directory.Delete(monitorDirectory, true);
            }
        }
    }
}
