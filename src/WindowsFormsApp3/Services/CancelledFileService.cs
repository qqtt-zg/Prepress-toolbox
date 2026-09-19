using System;
using System.IO;

namespace WindowsFormsApp3.Services
{
    public sealed class CancelledFileMoveResult
    {
        public bool Success { get; set; }
        public string SourcePath { get; set; } = "";
        public string DestinationPath { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    /// <summary>
    /// 将用户取消处理的文件移入监控目录下的“取消处理”文件夹。
    /// </summary>
    public sealed class CancelledFileService
    {
        public const string CancelledFolderName = "取消处理";

        public CancelledFileMoveResult MoveToCancelledFolder(string sourcePath, string monitorDirectory)
        {
            var result = new CancelledFileMoveResult { SourcePath = sourcePath ?? "" };
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(monitorDirectory))
                {
                    result.ErrorMessage = "文件路径或监控目录为空";
                    return result;
                }

                string resolvedMonitorDirectory = Path.GetFullPath(monitorDirectory)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string resolvedSourcePath = Path.GetFullPath(sourcePath);
                string sourceDirectory = Path.GetDirectoryName(resolvedSourcePath)?
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // 只允许移动监控目录的直接子文件，拒绝越界路径和子目录中的文件。
                if (string.IsNullOrEmpty(sourceDirectory) ||
                    !string.Equals(sourceDirectory, resolvedMonitorDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    result.ErrorMessage = "文件不在当前监控目录中";
                    return result;
                }

                if (!Directory.Exists(resolvedMonitorDirectory) || !File.Exists(resolvedSourcePath))
                {
                    result.ErrorMessage = "待移动文件或监控目录不存在";
                    return result;
                }

                if ((new DirectoryInfo(resolvedMonitorDirectory).Attributes & FileAttributes.ReparsePoint) != 0 ||
                    (File.GetAttributes(resolvedSourcePath) & FileAttributes.ReparsePoint) != 0)
                {
                    result.ErrorMessage = "不允许移动重解析点中的文件";
                    return result;
                }

                string cancelledDirectory = Path.Combine(resolvedMonitorDirectory, CancelledFolderName);
                Directory.CreateDirectory(cancelledDirectory);
                if ((new DirectoryInfo(cancelledDirectory).Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    result.ErrorMessage = "取消处理文件夹不能是重解析点";
                    return result;
                }
                string destinationPath = GetAvailableDestinationPath(cancelledDirectory, Path.GetFileName(resolvedSourcePath));

                File.Move(resolvedSourcePath, destinationPath);
                result.Success = true;
                result.DestinationPath = destinationPath;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private static string GetAvailableDestinationPath(string destinationDirectory, string fileName)
        {
            string candidate = Path.Combine(destinationDirectory, fileName);
            if (!File.Exists(candidate)) return candidate;

            string name = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            int suffix = 1;
            do
            {
                candidate = Path.Combine(destinationDirectory, $"{name} ({suffix}){extension}");
                suffix++;
            }
            while (File.Exists(candidate));

            return candidate;
        }
    }
}
