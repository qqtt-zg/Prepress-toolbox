using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Utils;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Panels
{
    public partial class FileRenamePanel
    {
        private void ToggleFloatingDropZone()
        {
            try
            {
                if (_floatingDropZone == null || _floatingDropZone.IsDisposed)
                {
                    _floatingDropZone = new FloatingDropZoneForm(
                        HandleDroppedPdfToMonitorFolder,
                        () => _dropOperationMode,
                        InputDirectory);
                    _floatingDropZone.Location = new Point(200, 200);
                }

                // 更新目标目录和操作模式
                _floatingDropZone.SetTargetDirectory(InputDirectory);
                _floatingDropZone.SetDropOperationMode(_dropOperationMode);
                
                if (_floatingDropZone.Visible)
                {
                    _floatingDropZone.Hide();
                    if (_btnDropZone != null)
                    {
                        _btnDropZone.Type = AntdUI.TTypeMini.Default;
                    }
                }
                else
                {
                    _floatingDropZone.Show();
                    if (_btnDropZone != null)
                    {
                        _btnDropZone.Type = AntdUI.TTypeMini.Primary;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"悬浮拖拽区切换失败: {ex.Message}");
            }
        }

        private void HandleDroppedPdfToMonitorFolder(string sourceFile, DragDropEffects operation)
        {
            try
            {
                if (string.IsNullOrEmpty(InputDirectory) || !Directory.Exists(InputDirectory))
                {
                    ShowError("请先选择有效的监控目录");
                    return;
                }

                if (string.IsNullOrEmpty(sourceFile) || !File.Exists(sourceFile))
                {
                    ShowError("拖入的文件不存在");
                    return;
                }

                if (!string.Equals(Path.GetExtension(sourceFile), ".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    ShowError("仅支持拖入PDF文件");
                    return;
                }

                var destFile = Path.Combine(InputDirectory, Path.GetFileName(sourceFile));

                // 检查文件是否已存在，询问用户是否覆盖
                if (File.Exists(destFile))
                {
                    var fileName = Path.GetFileName(destFile);
                    var result = MessageBox.Show(
                        $"文件 \"{fileName}\" 已存在，是否覆盖？",
                        "文件覆盖确认",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.No)
                    {
                        // 用户选择不覆盖，取消操作
                        UpdateStatus("操作已取消：文件已存在");
                        return;
                    }
                    // 用户选择覆盖，继续执行（使用原文件名）
                }

                // 根据操作类型执行复制或移动
                string operationText = "导入";
                try
                {
                    if (operation == DragDropEffects.Move)
                    {
                        // 如果目标文件已存在（用户已确认覆盖），先删除目标文件
                        if (File.Exists(destFile))
                        {
                            File.Delete(destFile);
                            LogHelper.Info($"已删除目标文件: {destFile}");
                        }
                        File.Move(sourceFile, destFile);
                        operationText = "移动";
                        LogHelper.Info($"已移动文件: {sourceFile} -> {destFile}");
                    }
                    else
                    {
                        File.Copy(sourceFile, destFile, overwrite: true);
                        operationText = "复制";
                        LogHelper.Info($"已复制文件: {sourceFile} -> {destFile}");
                    }
                }
                catch (Exception ex)
                {
                    // 如果移动失败（如跨分区），回退到复制
                    if (operation == DragDropEffects.Move)
                    {
                        try
                        {
                            LogHelper.Warn($"移动失败，尝试回退到复制: {ex.Message}");
                            File.Copy(sourceFile, destFile, overwrite: true);
                            operationText = "复制（移动失败）";
                            LogHelper.Info($"已回退到复制文件: {sourceFile} -> {destFile}");
                        }
                        catch (Exception copyEx)
                        {
                            throw new Exception($"移动和复制都失败: 移动错误={ex.Message}, 复制错误={copyEx.Message}", copyEx);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                // 依赖现有 FileSystemWatcher 触发后续流程（弹材料选择框）
                UpdateStatus($"已{operationText}到监控目录: {Path.GetFileName(destFile)}");
            }
            catch (Exception ex)
            {
                ShowError($"导入PDF失败: {ex.Message}");
                LogHelper.Error($"导入PDF失败: {ex.Message}, 堆栈: {ex.StackTrace}");
            }
        }

        private void DisposeFloatingDropZone()
        {
            try
            {
                if (_floatingDropZone != null)
                {
                    _floatingDropZone.Dispose();
                    _floatingDropZone = null;
                }
            }
            catch
            {
                // ignore
            }
        }

        /// <summary>
        /// 更新悬浮拖拽窗口的主题设置
        /// </summary>
        public void UpdateFloatingDropZoneTheme()
        {
            try
            {
                if (_floatingDropZone != null && !_floatingDropZone.IsDisposed)
                {
                    _floatingDropZone.UpdateThemeSettings();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"更新悬浮拖拽窗口主题失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查悬浮拖拽窗口是否正在显示
        /// </summary>
        public bool IsFloatingDropZoneVisible()
        {
            return _floatingDropZone != null && !_floatingDropZone.IsDisposed && _floatingDropZone.Visible;
        }
    }
}
