using System;
using System.IO;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Services.Events;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using WindowsFormsApp3.Utils;
using System.ComponentModel;
using System.Threading;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 文件重命名服务实现，负责文件重命名的核心逻辑
    /// </summary>
    public class FileRenameService : WindowsFormsApp3.Interfaces.IFileRenameService
    {
        private IEventBus _eventBus;
        private readonly IPdfDimensionService _pdfDimensionService;
        
        /// <summary>
        /// 当文件重命名成功时触发的事件
        /// </summary>
        public event EventHandler<FileRenameEventArgs> FileRenamedSuccessfully;

        /// <summary>
        /// 当文件重命名失败时触发的事件
        /// </summary>
        public event EventHandler<FileRenameEventArgs> FileRenameFailed;

        /// <summary>
        /// 当批量重命名进度更新时触发的事件
        /// </summary>
        public event EventHandler<BatchRenameProgressEventArgs> BatchRenameProgressChanged;

        // 接口定义的事件
        public event EventHandler<FileRenamedEventArgs> FileRenamed;
        public event EventHandler<RenameBatchCompletedEventArgs> BatchRenameCompleted;
        public event EventHandler<RenameErrorEventArgs> RenameError;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventBus">事件总线实例</param>
        public FileRenameService(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _pdfDimensionService = PdfDimensionServiceFactory.GetInstance();
        }

        /// <summary>
        /// 供ServiceLocator使用的构造函数，用于兼容现有代码
        /// </summary>
        public FileRenameService()
        {
            // 这里不使用ServiceLocator，避免递归初始化
            _eventBus = ServiceLocator.Instance.GetEventBus();
            _pdfDimensionService = PdfDimensionServiceFactory.GetInstance();
        }
        


        /// <summary>
        /// 立即重命名文件（接口实现）
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="newName">新文件名</param>
        /// <returns>重命名是否成功</returns>
        public bool RenameFileImmediately(string sourcePath, string newName)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(newName))
            {
                RenameError?.Invoke(this, new RenameErrorEventArgs
                {
                    FilePath = sourcePath,
                    ErrorMessage = "源文件路径或新文件名不能为空"
                });
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(sourcePath);
                string newFilePath = Path.Combine(directory, newName);
                
                // 处理文件名冲突
                newFilePath = HandleFileNameConflict(newFilePath);



                // 执行文件移动操作
                File.Move(sourcePath, newFilePath);

                // 触发接口定义的事件
                FileRenamed?.Invoke(this, new FileRenamedEventArgs
                {
                    OldPath = sourcePath,
                    NewPath = newFilePath
                });

                // 发布文件重命名事件到事件总线
                _eventBus?.Publish(new FileRenamedEvent
                {
                    OriginalFileName = Path.GetFileName(sourcePath),
                    NewFileName = Path.GetFileName(newFilePath),
                    FilePath = newFilePath,
                    FileSize = new FileInfo(newFilePath).Length,
                    Timestamp = DateTime.Now
                });

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = "重命名文件失败: " + ex.Message;
                
                // 触发错误事件
                RenameError?.Invoke(this, new RenameErrorEventArgs
                {
                    FilePath = sourcePath,
                    ErrorMessage = errorMessage
                });
                return false;
            }
        }

        /// <summary>
        /// 异步重命名文件
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="newName">新文件名</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重命名是否成功</returns>
        public async Task<bool> RenameFileImmediatelyAsync(string sourcePath, string newName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(newName))
            {
                RenameError?.Invoke(this, new RenameErrorEventArgs
                {
                    FilePath = sourcePath,
                    ErrorMessage = "源文件路径或新文件名不能为空"
                });
                return false;
            }

            try
            {
                // 在后台线程执行文件操作
                return await Task.Run(() =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string directory = Path.GetDirectoryName(sourcePath);
                    string newFilePath = Path.Combine(directory, newName);

                    // 处理文件名冲突
                    newFilePath = HandleFileNameConflict(newFilePath);

                    // 执行文件移动操作
                    File.Move(sourcePath, newFilePath);

                    // 触发接口定义的事件
                    FileRenamed?.Invoke(this, new FileRenamedEventArgs
                    {
                        OldPath = sourcePath,
                        NewPath = newFilePath
                    });

                    // 发布文件重命名事件到事件总线
                    _eventBus?.Publish(new FileRenamedEvent
                    {
                        OriginalFileName = Path.GetFileName(sourcePath),
                        NewFileName = Path.GetFileName(newFilePath),
                        FilePath = newFilePath,
                        FileSize = new FileInfo(newFilePath).Length,
                        Timestamp = DateTime.Now
                    });

                    return true;
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 操作被取消，不触发错误事件
                return false;
            }
            catch (Exception ex)
            {
                string errorMessage = "异步重命名文件失败: " + ex.Message;

                // 触发错误事件
                RenameError?.Invoke(this, new RenameErrorEventArgs
                {
                    FilePath = sourcePath,
                    ErrorMessage = errorMessage
                });
                return false;
            }
        }

        /// <summary>
        /// 立即重命名文件（内部实现，兼容现有代码）
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <returns>重命名是否成功</returns>
        public bool RenameFileImmediately(FileRenameInfo fileInfo, string exportPath, bool isCopyMode)
        {
            // 添加调试信息
            LogHelper.Debug($"=== 开始执行RenameFileImmediately ===");
            LogHelper.Debug($"文件信息: {fileInfo?.OriginalName}");
            LogHelper.Debug($"导出路径: {exportPath}");
            LogHelper.Debug($"复制模式: {isCopyMode}");
            
            if (fileInfo == null || string.IsNullOrEmpty(exportPath))
            {
                LogHelper.Debug("文件信息或导出路径为空");
                RaiseFileRenameFailed(fileInfo, "文件信息或导出路径为空");
                return false;
            }

            try
            {
                // 确保导出目录存在
                IOHelper.EnsureDirectoryExists(exportPath);

                // 构建新文件路径
                string newFilePath = Path.Combine(exportPath, fileInfo.NewName);
                LogHelper.Debug($"构建新文件路径: {newFilePath}");
        
                // 处理文件名冲突
                newFilePath = HandleFileNameConflict(newFilePath);
                LogHelper.Debug($"处理冲突后路径: {newFilePath}");

                // 执行文件复制或移动操作
                if (isCopyMode)
                {
                    LogHelper.Debug($"执行文件复制: {fileInfo.FullPath} -> {newFilePath}");
                    File.Copy(fileInfo.FullPath, newFilePath, false);
                }
                else
                {
                    LogHelper.Debug($"执行文件移动: {fileInfo.FullPath} -> {newFilePath}");
                    File.Move(fileInfo.FullPath, newFilePath);
                }

                // 更新文件信息
                fileInfo.FullPath = newFilePath;
                fileInfo.Status = "已重命名";
                fileInfo.ErrorMessage = string.Empty;
                LogHelper.Debug($"文件重命名成功");

                // 触发成功事件
                RaiseFileRenamedSuccessfully(fileInfo);

                // 发布文件重命名事件到事件总线
                _eventBus?.Publish(new FileRenamedEvent
                {
                    OriginalFileName = Path.GetFileName(fileInfo.OriginalName),
                    NewFileName = fileInfo.NewName,
                    FilePath = newFilePath,
                    FileSize = new FileInfo(newFilePath).Length,
                    Timestamp = DateTime.Now
                });

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = "重命名文件失败: " + ex.Message;
                LogHelper.Debug($"文件重命名失败: {errorMessage}");
                LogHelper.Debug($"异常堆栈: {ex.StackTrace}");
        
                fileInfo.Status = "重命名失败";
                fileInfo.ErrorMessage = errorMessage;
        
                // 触发失败事件
                RaiseFileRenameFailed(fileInfo, errorMessage);
                return false;
            }
        }

        /// <summary>
        /// 异步重命名文件
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重命名是否成功</returns>
        public async Task<bool> RenameFileImmediatelyAsync(FileRenameInfo fileInfo, string exportPath, bool isCopyMode, CancellationToken cancellationToken = default)
        {
            LogHelper.Debug($"=== 开始执行RenameFileImmediatelyAsync ===");
            LogHelper.Debug($"文件信息: {fileInfo?.OriginalName}");
            LogHelper.Debug($"导出路径: {exportPath}");
            LogHelper.Debug($"复制模式: {isCopyMode}");

            if (fileInfo == null || string.IsNullOrEmpty(exportPath))
            {
                LogHelper.Debug("文件信息或导出路径为空");
                RaiseFileRenameFailed(fileInfo, "文件信息或导出路径为空");
                return false;
            }

            try
            {
                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();

                // 确保导出目录存在
                if (!Directory.Exists(exportPath))
                {
                    LogHelper.Debug($"创建导出目录: {exportPath}");
                    await IOHelper.EnsureDirectoryExistsAsync(exportPath, cancellationToken);
                }

                // 构建新文件路径
                string newFilePath = Path.Combine(exportPath, fileInfo.NewName);
                LogHelper.Debug($"构建新文件路径: {newFilePath}");

                // 处理文件名冲突
                newFilePath = await Task.Run(() => HandleFileNameConflict(newFilePath), cancellationToken);
                LogHelper.Debug($"处理冲突后路径: {newFilePath}");

                // 执行文件复制或移动操作
                await Task.Run(() =>
                {
                    if (isCopyMode)
                    {
                        LogHelper.Debug($"执行文件复制: {fileInfo.FullPath} -> {newFilePath}");
                        File.Copy(fileInfo.FullPath, newFilePath, false);
                    }
                    else
                    {
                        LogHelper.Debug($"执行文件移动: {fileInfo.FullPath} -> {newFilePath}");
                        File.Move(fileInfo.FullPath, newFilePath);
                    }
                }, cancellationToken);

                // 检查取消请求（在文件操作后）
                cancellationToken.ThrowIfCancellationRequested();

                // 更新文件信息
                fileInfo.FullPath = newFilePath;
                fileInfo.Status = "已重命名";
                fileInfo.ErrorMessage = string.Empty;
                LogHelper.Debug($"文件重命名成功");

                // 触发成功事件
                RaiseFileRenamedSuccessfully(fileInfo);

                // 发布文件重命名事件到事件总线
                _eventBus?.Publish(new FileRenamedEvent
                {
                    OriginalFileName = Path.GetFileName(fileInfo.OriginalName),
                    NewFileName = fileInfo.NewName,
                    FilePath = newFilePath,
                    FileSize = new FileInfo(newFilePath).Length,
                    Timestamp = DateTime.Now
                });

                return true;
            }
            catch (OperationCanceledException)
            {
                LogHelper.Debug($"文件重命名被取消: {fileInfo?.OriginalName}");
                fileInfo.Status = "已取消";
                fileInfo.ErrorMessage = "操作被取消";
                RaiseFileRenameFailed(fileInfo, "操作被取消");
                return false;
            }
            catch (Exception ex)
            {
                string errorMessage = "重命名文件失败: " + ex.Message;
                LogHelper.Debug($"文件重命名失败: {errorMessage}");
                LogHelper.Debug($"异常堆栈: {ex.StackTrace}");

                fileInfo.Status = "重命名失败";
                fileInfo.ErrorMessage = errorMessage;

                // 触发失败事件
                RaiseFileRenameFailed(fileInfo, errorMessage);
                return false;
            }
        }

        /// <summary>
        /// 异步重命名文件并应用PDF处理选项
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="pdfOptions">PDF处理选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>重命名和处理是否成功</returns>
        public async Task<bool> RenameFileImmediatelyAsync(FileRenameInfo fileInfo, string exportPath, bool isCopyMode, PdfProcessingOptions pdfOptions, CancellationToken cancellationToken = default)
        {
            LogHelper.Debug($"=== 开始执行RenameFileImmediatelyAsync (带PDF处理) ===");

            // 安全地记录PDF处理选项，避免null引用
            if (pdfOptions != null)
            {
                LogHelper.Debug($"PDF处理选项 - AddPdfLayers: {pdfOptions.AddPdfLayers}, ShapeType: {pdfOptions.ShapeType}, RoundRadius: {pdfOptions.RoundRadius}");
            }
            else
            {
                LogHelper.Debug("PDF处理选项为null，跳过PDF处理");
            }

            // 检查原文件是否存在
            if (!File.Exists(fileInfo.FullPath))
            {
                LogHelper.Debug($"原文件不存在: {fileInfo.FullPath}");
                fileInfo.Status = "文件不存在";
                fileInfo.ErrorMessage = "原文件不存在";
                RaiseFileRenameFailed(fileInfo, "原文件不存在");
                return false;
            }

            // 如果没有PDF处理选项，直接使用原有的简单重命名方法
            if (pdfOptions == null || (!pdfOptions.AddPdfLayers && !pdfOptions.AddIdentifierPage))
            {
                LogHelper.Debug("没有PDF处理需求，使用标准重命名方法");
                return await RenameFileImmediatelyAsync(fileInfo, exportPath, isCopyMode, cancellationToken);
            }

            // 优化：先在临时位置完成所有PDF处理，最后才创建目标文件
            // 这样可以避免在性能较差的电脑上处理过程中产生目标文件

            // 创建临时工作目录用于PDF处理
            string tempDir = Path.GetTempPath();
            string tempFile = Path.Combine(tempDir, $"temp_{Guid.NewGuid()}_{Path.GetFileName(fileInfo.OriginalName)}");

            try
            {
                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();

                LogHelper.Debug($"步骤1: 创建临时文件副本进行处理: {tempFile}");

                // 先复制原文件到临时位置进行处理
                await Task.Run(() => File.Copy(fileInfo.FullPath, tempFile, false), cancellationToken);

                LogHelper.Debug($"步骤2: 在临时文件上执行PDF处理");

                // 使用临时文件路径进行处理
                var tempFileInfo = new FileRenameInfo
                {
                    OriginalName = fileInfo.OriginalName,
                    NewName = fileInfo.NewName,
                    FullPath = tempFile,
                    Status = fileInfo.Status,
                    ErrorMessage = fileInfo.ErrorMessage
                };

                // 执行PDF处理（在临时文件上）
                bool pdfProcessSuccess = await ProcessPdfFileInternal(tempFileInfo, pdfOptions, cancellationToken);

                if (!pdfProcessSuccess)
                {
                    LogHelper.Debug($"PDF处理失败，取消文件重命名");
                    fileInfo.Status = "PDF处理失败";
                    fileInfo.ErrorMessage = tempFileInfo.ErrorMessage;
                    RaiseFileRenameFailed(fileInfo, "PDF处理失败");
                    return false;
                }

                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();

                LogHelper.Debug($"步骤3: PDF处理完成，现在创建目标文件");

                // PDF处理成功后，现在才创建目标文件
                bool renameSuccess = await CreateTargetFileAfterPdfProcessing(fileInfo, exportPath, isCopyMode, tempFile, cancellationToken);

                if (renameSuccess)
                {
                    LogHelper.Debug($"=== 文件重命名和PDF处理全部完成 ===");
                    return true;
                }
                else
                {
                    LogHelper.Debug($"目标文件创建失败");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.Debug($"文件重命名和PDF处理被取消");
                fileInfo.Status = "已取消";
                fileInfo.ErrorMessage = "操作被取消";
                RaiseFileRenameFailed(fileInfo, "操作被取消");
                return false;
            }
            catch (Exception ex)
            {
                string errorMessage = "重命名和PDF处理失败: " + ex.Message;
                LogHelper.Debug($"操作失败: {errorMessage}");
                LogHelper.Debug($"异常堆栈: {ex.StackTrace}");

                fileInfo.Status = "处理失败";
                fileInfo.ErrorMessage = errorMessage;
                RaiseFileRenameFailed(fileInfo, errorMessage);
                return false;
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                        LogHelper.Debug($"临时文件已清理: {tempFile}");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Debug($"清理临时文件失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 在PDF处理完成后创建目标文件
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="processedTempFile">已处理完成的临时文件</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>创建目标文件是否成功</returns>
        private async Task<bool> CreateTargetFileAfterPdfProcessing(FileRenameInfo fileInfo, string exportPath, bool isCopyMode, string processedTempFile, CancellationToken cancellationToken = default)
        {
            try
            {
                // 构建新文件路径
                string newFilePath = Path.Combine(exportPath, fileInfo.NewName);
                LogHelper.Debug($"构建最终目标文件路径: {newFilePath}");

                // 处理文件名冲突
                newFilePath = await Task.Run(() => HandleFileNameConflict(newFilePath), cancellationToken);
                LogHelper.Debug($"处理冲突后最终路径: {newFilePath}");

                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();

                // 将处理完成的临时文件复制或移动到目标位置
                await Task.Run(() =>
                {
                    if (isCopyMode)
                    {
                        LogHelper.Debug($"复制处理后的文件: {processedTempFile} -> {newFilePath}");
                        File.Copy(processedTempFile, newFilePath, false);
                    }
                    else
                    {
                        LogHelper.Debug($"移动处理后的文件: {processedTempFile} -> {newFilePath}");
                        File.Move(processedTempFile, newFilePath);
                    }
                }, cancellationToken);

                // 更新文件信息
                fileInfo.FullPath = newFilePath;
                fileInfo.Status = "已重命名";
                fileInfo.ErrorMessage = string.Empty;
                LogHelper.Debug($"目标文件创建成功: {newFilePath}");

                // 触发成功事件
                RaiseFileRenamedSuccessfully(fileInfo);

                // 发布文件重命名事件到事件总线
                _eventBus?.Publish(new FileRenamedEvent
                {
                    OriginalFileName = Path.GetFileName(fileInfo.OriginalName),
                    NewFileName = fileInfo.NewName,
                    FilePath = newFilePath,
                    FileSize = new FileInfo(newFilePath).Length,
                    Timestamp = DateTime.Now
                });

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = "创建目标文件失败: " + ex.Message;
                LogHelper.Debug($"创建目标文件失败: {errorMessage}");

                fileInfo.Status = "创建目标文件失败";
                fileInfo.ErrorMessage = errorMessage;
                RaiseFileRenameFailed(fileInfo, errorMessage);
                return false;
            }
        }

        /// <summary>
        /// 处理PDF文件的内部方法（在临时文件上执行）
        /// </summary>
        /// <param name="tempFileInfo">临时文件信息</param>
        /// <param name="pdfOptions">PDF处理选项</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>PDF处理是否成功</returns>
        private async Task<bool> ProcessPdfFileInternal(FileRenameInfo tempFileInfo, PdfProcessingOptions pdfOptions, CancellationToken cancellationToken = default)
        {
            try
            {
                // 检查文件是否为PDF
                if (!string.Equals(Path.GetExtension(tempFileInfo.FullPath), ".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    LogHelper.Debug($"跳过非PDF文件: {tempFileInfo.FullPath}");
                    return true; // 非PDF文件不需要处理，但不算失败
                }

                // === PDF处理顺序逻辑 ===
                // 完整处理流程：原始文件 → 页面重排 → 添加标识页 → 图层处理 → 独立页面旋转 → 最终文件
                // 1. 页面重排：统一所有页面的尺寸和坐标系统（第一步）
                // 2. 标识页插入：在重排后的文件第一页添加标识页（第二步）
                // 3. 图层处理：添加点号、计数器和出血线等图层（第三步）
                // 4. 独立页面旋转：始终执行，不受图层预检查影响（第四步）
                // 5. 生成最终文件：保存处理后的完整PDF（第五步）

                LogHelper.Debug($"开始在临时文件上处理PDF: {tempFileInfo.FullPath}");

                // 执行PDF处理（使用统一的同步处理逻辑）
                bool pdfProcessSuccess = await Task.Run(() => ProcessPdfFileInternalSync(tempFileInfo, pdfOptions), cancellationToken);

                if (!pdfProcessSuccess)
                {
                    LogHelper.Debug($"PDF处理失败，取消文件重命名");
                    tempFileInfo.Status = "PDF处理失败";
                    tempFileInfo.ErrorMessage = tempFileInfo.ErrorMessage;
                    RaiseFileRenameFailed(tempFileInfo, "PDF处理失败");
                    return false;
                }

                LogHelper.Debug($"PDF处理完成: {tempFileInfo.FullPath}");

                // 独立步骤：页面旋转（始终执行，不受图层预检查影响）
                if (pdfOptions.RotationAngle != 0)
                {
                    LogHelper.Debug($"=== 独立页面旋转步骤 ===");
                    LogHelper.Debug($"旋转角度: {pdfOptions.RotationAngle}°");

                    bool rotationSuccess = PdfTools.RotateAllPages(tempFileInfo.FullPath, pdfOptions.RotationAngle);

                    if (rotationSuccess)
                    {
                        LogHelper.Debug($"✓ 页面旋转成功: {tempFileInfo.FullPath}");
                    }
                    else
                    {
                        LogHelper.Debug($"✗ 页面旋转失败: {tempFileInfo.FullPath}");
                        tempFileInfo.ErrorMessage = "页面旋转失败";
                        return false;
                    }
                }
                else
                {
                    LogHelper.Debug("旋转角度为0°，跳过页面旋转");
                }

                // 如果没有其他处理需求，直接返回成功
                if (!pdfOptions.AddPdfLayers)
                {
                    return true;
                }

                // 步骤3：处理PDF形状（在所有PDF处理完成后执行）
                if (pdfOptions.AddPdfLayers)
                {
                    // 获取PDF处理服务
                    var pdfService = ServiceLocator.Instance.GetPdfProcessingService();
                    if (pdfService == null)
                    {
                        LogHelper.Debug("无法获取PDF处理服务");
                        tempFileInfo.ErrorMessage = "无法获取PDF处理服务";
                        return false;
                    }

                    // 检查PDF文件是否已经存在所需图层
                    bool layersExist = _pdfDimensionService.CheckPdfLayersExist(
                        tempFileInfo.FullPath,
                        pdfOptions.TargetLayerNames ?? new[] { "Dots_AddCounter", "Dots_L_B_出血线" });

                    if (!layersExist)
                    {
                        // 异步调用AddDotsAddCounterLayer方法处理PDF文件
#pragma warning disable CS0618 // 禁用过时API警告
                        bool pdfShapeProcessSuccess = await Task.Run(() => pdfService.AddDotsAddCounterLayer(
                            tempFileInfo.FullPath,
                            pdfOptions.FinalDimensions,
                            pdfOptions.CornerRadius,
                            pdfOptions.UsePdfLastPage), cancellationToken);
#pragma warning restore CS0618 // 恢复警告

                        if (pdfShapeProcessSuccess)
                        {
                            LogHelper.Debug($"PDF文件处理成功: {tempFileInfo.FullPath}");
                        }
                        else
                        {
                            LogHelper.Debug($"PDF文件处理失败: {tempFileInfo.FullPath}");
                            tempFileInfo.ErrorMessage = "PDF形状处理失败";
                            return false;
                        }
                    }
                    else
                    {
                        LogHelper.Debug($"PDF文件已存在所需图层，跳过处理: {tempFileInfo.FullPath}");
                    }
                }
                else
                {
                    LogHelper.Debug("未启用PDF图层处理");
                }

                LogHelper.Debug($"PDF处理完成: {tempFileInfo.FullPath}");
                return true;
            }
            catch (OperationCanceledException)
            {
                LogHelper.Debug($"PDF处理被取消: {tempFileInfo.FullPath}");
                tempFileInfo.Status = "已取消";
                tempFileInfo.ErrorMessage = "PDF处理被取消";
                return false;
            }
            catch (Exception ex)
            {
                string errorMessage = "PDF处理异常: " + ex.Message;
                LogHelper.Debug($"PDF处理异常: {errorMessage}");
                LogHelper.Debug($"异常堆栈: {ex.StackTrace}");

                tempFileInfo.Status = "PDF处理失败";
                tempFileInfo.ErrorMessage = errorMessage;
                return false;
            }
        }

        /// <summary>
        /// 立即重命名文件并应用PDF处理选项
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="pdfOptions">PDF处理选项</param>
        /// <returns>重命名和处理是否成功</returns>
        public bool RenameFileImmediately(FileRenameInfo fileInfo, string exportPath, bool isCopyMode, PdfProcessingOptions pdfOptions)
        {
            LogHelper.Debug($"=== 开始执行RenameFileImmediately (带PDF处理) ===");

            // 安全地记录PDF处理选项，避免null引用
            if (pdfOptions != null)
            {
                LogHelper.Debug($"PDF处理选项 - AddPdfLayers: {pdfOptions.AddPdfLayers}, ShapeType: {pdfOptions.ShapeType}, RoundRadius: {pdfOptions.RoundRadius}");
            }
            else
            {
                LogHelper.Debug("PDF处理选项为null，跳过PDF处理");
            }

            // 检查原文件是否存在
            if (!File.Exists(fileInfo.FullPath))
            {
                LogHelper.Debug($"原文件不存在: {fileInfo.FullPath}");
                fileInfo.Status = "文件不存在";
                fileInfo.ErrorMessage = "原文件不存在";
                RaiseFileRenameFailed(fileInfo, "原文件不存在");
                return false;
            }

            // 如果没有PDF处理选项，直接使用原有的简单重命名方法
            if (pdfOptions == null || (!pdfOptions.AddPdfLayers && !pdfOptions.AddIdentifierPage))
            {
                LogHelper.Debug("没有PDF处理需求，使用标准重命名方法");
                return RenameFileImmediately(fileInfo, exportPath, isCopyMode);
            }

            // 优化：先在临时位置完成所有PDF处理，最后才创建目标文件
            // 这样可以避免在性能较差的电脑上处理过程中产生目标文件

            // 创建临时工作目录用于PDF处理
            string tempDir = Path.GetTempPath();
            string tempFile = Path.Combine(tempDir, $"temp_{Guid.NewGuid()}_{Path.GetFileName(fileInfo.OriginalName)}");

            try
            {
                LogHelper.Debug($"步骤1: 创建临时文件副本进行处理: {tempFile}");

                // 先复制原文件到临时位置进行处理
                File.Copy(fileInfo.FullPath, tempFile, false);

                LogHelper.Debug($"步骤2: 在临时文件上执行PDF处理");

                // 使用临时文件路径进行处理
                var tempFileInfo = new FileRenameInfo
                {
                    OriginalName = fileInfo.OriginalName,
                    NewName = fileInfo.NewName,
                    FullPath = tempFile,
                    Status = fileInfo.Status,
                    ErrorMessage = fileInfo.ErrorMessage
                };

                // 执行PDF处理（在临时文件上）
                bool pdfProcessSuccess = ProcessPdfFileInternalSync(tempFileInfo, pdfOptions);

                if (!pdfProcessSuccess)
                {
                    LogHelper.Debug($"PDF处理失败，取消文件重命名");
                    fileInfo.Status = "PDF处理失败";
                    fileInfo.ErrorMessage = tempFileInfo.ErrorMessage;
                    RaiseFileRenameFailed(fileInfo, "PDF处理失败");
                    return false;
                }

                LogHelper.Debug($"步骤3: PDF处理完成，执行独立页面旋转");

                // 独立步骤：页面旋转（始终执行，不受图层预检查影响）
                if (pdfOptions.RotationAngle != 0)
                {
                    LogHelper.Debug($"=== 独立页面旋转步骤 ===");
                    LogHelper.Debug($"旋转角度: {pdfOptions.RotationAngle}°");

                    bool rotationSuccess = PdfTools.RotateAllPages(tempFileInfo.FullPath, pdfOptions.RotationAngle);

                    if (rotationSuccess)
                    {
                        LogHelper.Debug($"✓ 页面旋转成功: {tempFileInfo.FullPath}");
                    }
                    else
                    {
                        LogHelper.Debug($"✗ 页面旋转失败: {tempFileInfo.FullPath}");
                        fileInfo.Status = "页面旋转失败";
                        fileInfo.ErrorMessage = "页面旋转失败";
                        return false;
                    }
                }
                else
                {
                    LogHelper.Debug("旋转角度为0°，跳过页面旋转");
                }

                LogHelper.Debug($"步骤4: PDF处理和页面旋转完成，现在创建目标文件");

                // PDF处理和页面旋转成功后，现在才创建目标文件
                bool renameSuccess = CreateTargetFileAfterPdfProcessingSync(fileInfo, exportPath, isCopyMode, tempFile);

                if (renameSuccess)
                {
                    LogHelper.Debug($"=== 文件重命名和PDF处理全部完成 ===");
                    return true;
                }
                else
                {
                    LogHelper.Debug($"目标文件创建失败");
                    return false;
                }
            }
            catch (Exception ex)
            {
                string errorMessage = "重命名和PDF处理失败: " + ex.Message;
                LogHelper.Debug($"操作失败: {errorMessage}");
                LogHelper.Debug($"异常堆栈: {ex.StackTrace}");

                fileInfo.Status = "处理失败";
                fileInfo.ErrorMessage = errorMessage;
                RaiseFileRenameFailed(fileInfo, errorMessage);
                return false;
            }
            finally
            {
                // 清理临时文件
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                        LogHelper.Debug($"临时文件已清理: {tempFile}");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Debug($"清理临时文件失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 在PDF处理完成后创建目标文件（同步版本）
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="processedTempFile">已处理完成的临时文件</param>
        /// <returns>创建目标文件是否成功</returns>
        private bool CreateTargetFileAfterPdfProcessingSync(FileRenameInfo fileInfo, string exportPath, bool isCopyMode, string processedTempFile)
        {
            try
            {
                // 构建新文件路径
                string newFilePath = Path.Combine(exportPath, fileInfo.NewName);
                LogHelper.Debug($"构建最终目标文件路径: {newFilePath}");

                // 处理文件名冲突
                newFilePath = HandleFileNameConflict(newFilePath);
                LogHelper.Debug($"处理冲突后最终路径: {newFilePath}");

                // 将处理完成的临时文件复制或移动到目标位置
                if (isCopyMode)
                {
                    LogHelper.Debug($"复制处理后的文件: {processedTempFile} -> {newFilePath}");
                    File.Copy(processedTempFile, newFilePath, false);
                }
                else
                {
                    LogHelper.Debug($"移动处理后的文件: {processedTempFile} -> {newFilePath}");
                    File.Move(processedTempFile, newFilePath);
                }

                // 更新文件信息
                fileInfo.FullPath = newFilePath;
                fileInfo.Status = "已重命名";
                fileInfo.ErrorMessage = string.Empty;
                LogHelper.Debug($"目标文件创建成功: {newFilePath}");

                // 触发成功事件
                RaiseFileRenamedSuccessfully(fileInfo);

                // 发布文件重命名事件到事件总线
                _eventBus?.Publish(new FileRenamedEvent
                {
                    OriginalFileName = Path.GetFileName(fileInfo.OriginalName),
                    NewFileName = fileInfo.NewName,
                    FilePath = newFilePath,
                    FileSize = new FileInfo(newFilePath).Length,
                    Timestamp = DateTime.Now
                });

                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = "创建目标文件失败: " + ex.Message;
                LogHelper.Debug($"创建目标文件失败: {errorMessage}");

                fileInfo.Status = "创建目标文件失败";
                fileInfo.ErrorMessage = errorMessage;
                RaiseFileRenameFailed(fileInfo, errorMessage);
                return false;
            }
        }

        /// <summary>
        /// 处理PDF文件的内部方法（同步版本，在临时文件上执行）
        /// </summary>
        /// <param name="tempFileInfo">临时文件信息</param>
        /// <param name="pdfOptions">PDF处理选项</param>
        /// <returns>PDF处理是否成功</returns>
      /// <summary>
        /// 检查并处理PDF图层过滤逻辑（在页面重排之前执行）
        /// </summary>
        /// <param name="tempFileInfo">临时文件信息</param>
        /// <param name="pdfOptions">PDF处理选项</param>
        /// <returns>是否需要跳过页面重排</returns>
        private bool CheckAndFilterPdfLayers(FileRenameInfo tempFileInfo, PdfProcessingOptions pdfOptions)
        {
            try
            {
                // 如果未启用PDF图层处理，直接返回false（继续执行页面重排）
                if (!pdfOptions.AddPdfLayers)
                {
                    LogHelper.Debug("未启用PDF图层处理，将继续执行页面重排");
                    return false;
                }

                LogHelper.Debug($"=== 图层过滤检查（页面重排前执行） ===");
                LogHelper.Debug($"检查文件: {tempFileInfo.FullPath}");
                LogHelper.Debug($"目标图层: {string.Join(", ", pdfOptions.TargetLayerNames ?? new[] { "Dots_AddCounter", "Dots_L_B_出血线" })}");

                // 检查PDF文件是否已经存在所需图层
                bool layersExist = _pdfDimensionService.CheckPdfLayersExist(
                    tempFileInfo.FullPath,
                    pdfOptions.TargetLayerNames ?? new[] { "Dots_AddCounter", "Dots_L_B_出血线" });

                if (layersExist)
                {
                    LogHelper.Debug($"✓ PDF文件已存在所需图层，跳过所有PDF处理步骤: {tempFileInfo.FullPath}");
                    
                    // 如果图层已存在，则：
                    // 1. 跳过页面重排
                    // 2. 跳过标识页插入（如果存在的话）
                    // 3. 跳过图层处理
                    LogHelper.Debug("由于图层已存在，将跳过页面重排及后续所有PDF处理步骤");
                    return true; // 返回true表示跳过页面重排
                }
                else
                {
                    LogHelper.Debug($"○ PDF文件不存在所需图层，将继续执行完整的PDF处理流程: {tempFileInfo.FullPath}");
                    return false; // 返回false表示继续执行页面重排
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"图层过滤检查异常: {ex.Message}");
                // 异常情况下继续执行完整流程以确保处理成功
                return false;
            }
        }

        private bool ProcessPdfFileInternalSync(FileRenameInfo tempFileInfo, PdfProcessingOptions pdfOptions)
        {
            try
            {
                // === PDF处理顺序逻辑 ===
                // 完整处理流程：原始文件 → 图层过滤检查 → 页面重排 → 添加标识页 → 图层处理 → 最终文件
                // 0. 图层过滤检查：在页面重排之前检查图层是否已存在（第一步）
                // 1. 页面重排：统一所有页面的尺寸和坐标系统（第二步）
                // 2. 标识页插入：在重排后的文件第一页添加标识页（第三步）
                // 3. 图层处理：添加点号、计数器和出血线等图层（第四步）
                // 注意：页面旋转已分离为独立步骤，始终在PDF处理后执行

                // 检查文件是否为PDF
                if (!string.Equals(Path.GetExtension(tempFileInfo.FullPath), ".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    LogHelper.Debug($"跳过非PDF文件: {tempFileInfo.FullPath}");
                    return true; // 非PDF文件不需要处理，但不算失败
                }

                LogHelper.Debug($"开始在临时文件上处理PDF: {tempFileInfo.FullPath}");

                // 步骤0：图层过滤检查（在页面重排之前执行）
                // 处理流程：原始文件 → 图层过滤检查
                bool skipPageReorganization = CheckAndFilterPdfLayers(tempFileInfo, pdfOptions);
                
                if (skipPageReorganization)
                {
                    LogHelper.Debug($"✓ 图层已存在，跳过所有PDF处理步骤: {tempFileInfo.FullPath}");
                    LogHelper.Debug($"PDF处理完成（跳过模式）: {tempFileInfo.FullPath}");
                    return true;
                }

                // 步骤1：页面重排和坐标统一处理（在图层过滤检查完成后执行）
                // 处理流程：原始文件 → 页面重排
                LogHelper.Debug($"=== 步骤1：开始页面重排 ===");
                LogHelper.Debug($"页面重排前文件页数: {GetPdfPageCount(tempFileInfo.FullPath)}");

                Dictionary<int, PdfTools.PageTransformInfo> transformMap;
                bool setPageBoxesSuccess = PdfTools.AdvancedPageReorganizer.ExecuteAdvancedReorganization(tempFileInfo.FullPath, out transformMap);

                LogHelper.Debug($"页面重排后文件页数: {GetPdfPageCount(tempFileInfo.FullPath)}");

                if (setPageBoxesSuccess)
                {
                    LogHelper.Debug($"✓ 页面统一处理成功: {tempFileInfo.FullPath}");
                }
                else
                {
                    LogHelper.Debug($"✗ 页面统一处理失败: {tempFileInfo.FullPath}");
                    tempFileInfo.ErrorMessage = "页面统一处理失败";
                    return false;
                }

                //步骤2：标识页插入（在页面重排完成后执行）
                // 处理流程：页面重排 → 添加标识页
                if (pdfOptions.AddIdentifierPage)
                {
                    LogHelper.Debug($"=== 步骤2：开始插入标识页 ===");
                    LogHelper.Debug($"标识页插入前文件页数: {GetPdfPageCount(tempFileInfo.FullPath)}");
                    LogHelper.Debug($"标识页内容: {pdfOptions.IdentifierPageContent}");

                    bool identifierPageSuccess = PdfTools.InsertIdentifierPage(
                        tempFileInfo.FullPath,
                        pdfOptions.IdentifierPageContent,
                        12f);

                    LogHelper.Debug($"标识页插入后文件页数: {GetPdfPageCount(tempFileInfo.FullPath)}");

                    if (identifierPageSuccess)
                    {
                        LogHelper.Debug($"✓ 标识页插入成功: {tempFileInfo.FullPath}");
                    }
                    else
                    {
                        LogHelper.Debug($"✗ 标识页插入失败: {tempFileInfo.FullPath}");
                        tempFileInfo.ErrorMessage = "标识页插入失败";
                        return false;
                    }
                }

                // 步骤2.5：折手模式空白页插入（在标识页后或页面重排后执行）
                if (pdfOptions.LayoutMode == LayoutMode.Folding && pdfOptions.LayoutQuantity > 0)
                {
                    LogHelper.Debug($"=== 步骤2.5：折手模式空白页处理 ===");
                    int? currentPageCountNullable = GetPdfPageCount(tempFileInfo.FullPath);
                    int currentPageCount = currentPageCountNullable ?? 0;
                    LogHelper.Debug($"当前页数: {currentPageCount}, 布局数量: {pdfOptions.LayoutQuantity}");

                    // 获取排版服务
                    var impositionService = ServiceLocator.Instance.GetService<IImpositionService>();
                    if (impositionService == null)
                    {
                        impositionService = new ImpositionService();
                    }

                    // 计算需要的空白页数量
                    int blankPagesNeeded = impositionService.CalculateBlankPagesNeeded(currentPageCount, pdfOptions.LayoutQuantity);
                    LogHelper.Debug($"需要添加空白页数量: {blankPagesNeeded}");

                    if (blankPagesNeeded > 0)
                    {
                        // 在最后一页之后插入空白页（尺寸为首页尺寸）
                        bool blankPageSuccess = PdfTools.InsertIdentifierPage(
                            tempFileInfo.FullPath,
                            "",  // 空白页
                            12f,
                            -1,  // 最后一页之后
                            blankPagesNeeded);  // 插入多页

                        if (blankPageSuccess)
                        {
                            LogHelper.Debug($"✓ 成功在最后一页之后添加{blankPagesNeeded}个空白页，补足至布局数量的倍数");
                            LogHelper.Debug($"空白页插入后文件页数: {GetPdfPageCount(tempFileInfo.FullPath)}");
                        }
                        else
                        {
                            LogHelper.Debug($"✗ 空白页插入失败");
                            tempFileInfo.ErrorMessage = "空白页插入失败";
                            return false;
                        }
                    }
                    else
                    {
                        LogHelper.Debug($"页数已是布局数量的倍数，无需添加空白页");
                    }
                }

                // 步骤3：处理PDF形状（在标识页处理之后执行）
                if (pdfOptions.AddPdfLayers)
                {
                    // 获取PDF处理服务
                    var pdfService = ServiceLocator.Instance.GetPdfProcessingService();
                    if (pdfService == null)
                    {
                        LogHelper.Debug("无法获取PDF处理服务");
                        tempFileInfo.ErrorMessage = "无法获取PDF处理服务";
                        return false;
                    }

                    // 由于在步骤0已经检查过图层不存在，这里直接进行处理
                    // 直接调用PdfTools的AddDotsAddCounterLayer方法（页面已统一，不会重复处理）
                    bool pdfProcessSuccess = PdfTools.AddDotsAddCounterLayer(
                        tempFileInfo.FullPath,
                        pdfOptions.FinalDimensions,
                        pdfOptions.ShapeType,
                        pdfOptions.RoundRadius);

                    if (pdfProcessSuccess)
                    {
                        LogHelper.Debug($"PDF文件处理成功: {tempFileInfo.FullPath}");
                    }
                    else
                    {
                        LogHelper.Debug($"PDF文件处理失败: {tempFileInfo.FullPath}");
                        tempFileInfo.ErrorMessage = "PDF形状处理失败";
                        return false;
                    }
                }
                else
                {
                    LogHelper.Debug("未启用PDF图层处理");
                }

                LogHelper.Debug($"PDF处理完成: {tempFileInfo.FullPath}");
                return true;
            }
            catch (Exception ex)
            {
                string errorMessage = "PDF处理异常: " + ex.Message;
                LogHelper.Debug($"PDF处理异常: {errorMessage}");
                LogHelper.Debug($"异常堆栈: {ex.StackTrace}");

                tempFileInfo.Status = "PDF处理失败";
                tempFileInfo.ErrorMessage = errorMessage;
                return false;
            }
        }

        /// <summary>
        /// 批量重命名文件
        /// </summary>
        /// <param name="fileInfos">文件重命名信息列表</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="progressCallback">进度回调函数</param>
        /// <returns>成功重命名的文件数量</returns>
        public int BatchRenameFiles(List<FileRenameInfo> fileInfos, string exportPath, bool isCopyMode, Action<int, int> progressCallback = null)
        {
            if (fileInfos == null || fileInfos.Count == 0 || string.IsNullOrEmpty(exportPath))
            {
                return 0;
            }

            int successCount = 0;
            int totalCount = fileInfos.Count;

            try
            {
                // 确保导出目录存在
                IOHelper.EnsureDirectoryExists(exportPath);

                // 预处理：收集所有文件名，避免在循环中重复检查文件存在性
                List<string> existingFiles = new List<string>();
                foreach (var fileInfo in fileInfos)
                {
                    if (fileInfo != null && !string.IsNullOrEmpty(fileInfo.NewName))
                    {
                        string newFilePath = Path.Combine(exportPath, fileInfo.NewName);
                        existingFiles.Add(newFilePath);
                    }
                }

                // 发布批量处理开始事件
                _eventBus?.Publish(new BatchProcessingStartedEvent
                {
                    ProcessingType = isCopyMode ? "文件复制" : "文件重命名",
                    FileCount = totalCount,
                    TargetPath = exportPath
                });

                // 批量处理文件
                for (int i = 0; i < fileInfos.Count; i++)
                {
                    FileRenameInfo fileInfo = fileInfos[i];
                    if (fileInfo == null || string.IsNullOrEmpty(fileInfo.FullPath))
                    {
                        continue;
                    }

                    // 重命名单个文件
                    bool success = RenameFileImmediately(fileInfo, exportPath, isCopyMode);
                    if (success)
                    {
                        successCount++;
                    }

                    // 更新进度
                    int currentProgress = i + 1;
                    
                    // 触发进度事件
                    RaiseBatchRenameProgressChanged(currentProgress, totalCount, fileInfo);
                    
                    // 调用回调函数
                    progressCallback?.Invoke(currentProgress, totalCount);

                    // 为了防止UI卡死，每处理几个文件后短暂休眠
                    if (i % 10 == 0)
                    {
                        Application.DoEvents();
                        Task.Delay(10).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug("批量重命名过程中发生错误: " + ex.Message);
            }

            // 发布批量处理完成事件
            _eventBus?.Publish(new BatchProcessingCompletedEvent
            {
                ProcessingType = isCopyMode ? "文件复制" : "文件重命名",
                SuccessCount = successCount,
                FailedCount = totalCount - successCount,
                TotalTimeMs = 0, // 实际应用中应该计算实际耗时
                AverageTimePerFileMs = 0
            });

            return successCount;
        }

        /// <summary>
        /// 异步批量重命名文件
        /// </summary>
        /// <param name="fileInfos">文件重命名信息列表</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="progressCallback">进度回调函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>成功重命名的文件数量</returns>
        public async Task<int> BatchRenameFilesAsync(List<FileRenameInfo> fileInfos, string exportPath, bool isCopyMode, Action<int, int> progressCallback = null, CancellationToken cancellationToken = default)
        {
            if (fileInfos == null || fileInfos.Count == 0 || string.IsNullOrEmpty(exportPath))
            {
                return 0;
            }

            int successCount = 0;
            int totalCount = fileInfos.Count;

            try
            {
                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();

                // 确保导出目录存在
                if (!Directory.Exists(exportPath))
                {
                    await IOHelper.EnsureDirectoryExistsAsync(exportPath, cancellationToken);
                }

                // 预处理：收集所有文件名，避免在循环中重复检查文件存在性
                List<string> existingFiles = new List<string>();
                foreach (var fileInfo in fileInfos)
                {
                    if (fileInfo != null && !string.IsNullOrEmpty(fileInfo.NewName))
                    {
                        string newFilePath = Path.Combine(exportPath, fileInfo.NewName);
                        existingFiles.Add(newFilePath);
                    }
                }

                // 发布批量处理开始事件
                _eventBus?.Publish(new BatchProcessingStartedEvent
                {
                    ProcessingType = isCopyMode ? "文件复制" : "文件重命名",
                    FileCount = totalCount,
                    TargetPath = exportPath
                });

                // 批量处理文件
                for (int i = 0; i < fileInfos.Count; i++)
                {
                    // 检查取消请求
                    cancellationToken.ThrowIfCancellationRequested();

                    FileRenameInfo fileInfo = fileInfos[i];
                    if (fileInfo == null || string.IsNullOrEmpty(fileInfo.FullPath))
                    {
                        continue;
                    }

                    // 异步重命名单个文件
                    bool success = await RenameFileImmediatelyAsync(fileInfo, exportPath, isCopyMode, cancellationToken);
                    if (success)
                    {
                        successCount++;
                    }

                    // 更新进度
                    int currentProgress = i + 1;

                    // 触发进度事件
                    RaiseBatchRenameProgressChanged(currentProgress, totalCount, fileInfo);

                    // 调用回调函数
                    progressCallback?.Invoke(currentProgress, totalCount);

                    // 异步延迟，避免UI卡死
                    if (i % 10 == 0)
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.Debug("批量重命名操作被取消");
                return successCount;
            }
            catch (Exception ex)
            {
                LogHelper.Debug("批量重命名过程中发生错误: " + ex.Message);
            }

            // 发布批量处理完成事件
            _eventBus?.Publish(new BatchProcessingCompletedEvent
            {
                ProcessingType = isCopyMode ? "文件复制" : "文件重命名",
                SuccessCount = successCount,
                FailedCount = totalCount - successCount,
                TotalTimeMs = 0, // 实际应用中应该计算实际耗时
                AverageTimePerFileMs = 0
            });

            return successCount;
        }

        /// <summary>
        /// 异步批量重命名文件（带PDF处理）
        /// </summary>
        /// <param name="fileInfos">文件重命名信息列表</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="pdfOptions">PDF处理选项</param>
        /// <param name="progressCallback">进度回调函数</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>成功重命名的文件数量</returns>
        public async Task<int> BatchRenameFilesAsync(List<FileRenameInfo> fileInfos, string exportPath, bool isCopyMode, PdfProcessingOptions pdfOptions, Action<int, int> progressCallback = null, CancellationToken cancellationToken = default)
        {
            if (fileInfos == null || fileInfos.Count == 0 || string.IsNullOrEmpty(exportPath))
            {
                return 0;
            }

            int successCount = 0;
            int totalCount = fileInfos.Count;

            try
            {
                // 检查取消请求
                cancellationToken.ThrowIfCancellationRequested();

                // 确保导出目录存在
                if (!Directory.Exists(exportPath))
                {
                    await IOHelper.EnsureDirectoryExistsAsync(exportPath, cancellationToken);
                }

                // 预处理：收集所有文件名，避免在循环中重复检查文件存在性
                List<string> existingFiles = new List<string>();
                foreach (var fileInfo in fileInfos)
                {
                    if (fileInfo != null && !string.IsNullOrEmpty(fileInfo.NewName))
                    {
                        string newFilePath = Path.Combine(exportPath, fileInfo.NewName);
                        existingFiles.Add(newFilePath);
                    }
                }

                // 发布批量处理开始事件
                _eventBus?.Publish(new BatchProcessingStartedEvent
                {
                    ProcessingType = isCopyMode ? "文件复制" : "文件重命名",
                    FileCount = totalCount,
                    TargetPath = exportPath
                });

                // 批量处理文件
                for (int i = 0; i < fileInfos.Count; i++)
                {
                    // 检查取消请求
                    cancellationToken.ThrowIfCancellationRequested();

                    FileRenameInfo fileInfo = fileInfos[i];
                    if (fileInfo == null || string.IsNullOrEmpty(fileInfo.FullPath))
                    {
                        continue;
                    }

                    // 异步重命名单个文件（带PDF处理）
                    bool success = await RenameFileImmediatelyAsync(fileInfo, exportPath, isCopyMode, pdfOptions, cancellationToken);
                    if (success)
                    {
                        successCount++;
                    }

                    // 更新进度
                    int currentProgress = i + 1;

                    // 触发进度事件
                    RaiseBatchRenameProgressChanged(currentProgress, totalCount, fileInfo);

                    // 调用回调函数
                    progressCallback?.Invoke(currentProgress, totalCount);

                    // 异步延迟，避免UI卡死
                    if (i % 10 == 0)
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                LogHelper.Debug("批量重命名操作被取消");
                return successCount;
            }
            catch (Exception ex)
            {
                LogHelper.Debug("批量重命名过程中发生错误: " + ex.Message);
            }

            // 发布批量处理完成事件
            _eventBus?.Publish(new BatchProcessingCompletedEvent
            {
                ProcessingType = isCopyMode ? "文件复制" : "文件重命名",
                SuccessCount = successCount,
                FailedCount = totalCount - successCount,
                TotalTimeMs = 0, // 实际应用中应该计算实际耗时
                AverageTimePerFileMs = 0
            });

            return successCount;
        }

        /// <summary>
        /// 生成新文件名
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <param name="separator">分隔符</param>
        /// <returns>生成的新文件名</returns>
        public string GenerateNewFileName(FileRenameInfo fileInfo, string separator)
        {
            if (fileInfo == null)
                return string.Empty;

            try
            {
                // 获取文件扩展名
                string extension = Path.GetExtension(fileInfo.OriginalName);
                
                // 根据配置构建新文件名
                string newFileName = string.Empty;
                
                // 处理序号
                if (!string.IsNullOrEmpty(fileInfo.SerialNumber))
                {
                    newFileName += fileInfo.SerialNumber;
                }
                
                // 处理订单号
                if (!string.IsNullOrEmpty(fileInfo.OrderNumber))
                {
                    if (!string.IsNullOrEmpty(newFileName))
                        newFileName += separator;
                    newFileName += fileInfo.OrderNumber;
                }
                
                // 处理材料
                if (!string.IsNullOrEmpty(fileInfo.Material))
                {
                    if (!string.IsNullOrEmpty(newFileName))
                        newFileName += separator;
                    newFileName += fileInfo.Material;
                }
                
                // 处理数量
                if (!string.IsNullOrEmpty(fileInfo.Quantity))
                {
                    if (!string.IsNullOrEmpty(newFileName))
                        newFileName += separator;
                    newFileName += fileInfo.Quantity;
                }
                
                // 处理尺寸
                if (!string.IsNullOrEmpty(fileInfo.Dimensions))
                {
                    if (!string.IsNullOrEmpty(newFileName))
                        newFileName += separator;
                    newFileName += fileInfo.Dimensions;
                }
                
                // 处理列组合
                if (!string.IsNullOrEmpty(fileInfo.CompositeColumn))
                {
                    if (!string.IsNullOrEmpty(newFileName))
                        newFileName += separator;
                    newFileName += fileInfo.CompositeColumn;
                }
                
                // 添加扩展名
                newFileName += extension;
                
                return newFileName;
            }
            catch (Exception ex)
            {
                LogHelper.Debug("生成新文件名时出错: " + ex.Message);
                return fileInfo.OriginalName; // 出错时返回原文件名
            }
        }

        /// <summary>
        /// 检查文件名冲突
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>处理冲突后的文件路径</returns>
        public string HandleFileNameConflict(string filePath)
        {
            if (!File.Exists(filePath))
                return filePath;

            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);
            
            int counter = 1;
            string newFilePath;
            
            // 循环直到找到不冲突的文件名
            do
            {
                newFilePath = Path.Combine(directory, $"{fileName}({counter}){extension}");
                counter++;
            } while (File.Exists(newFilePath));
            
            return newFilePath;
        }

        /// <summary>
        /// 批量重命名文件（接口实现）
        /// </summary>
        /// <param name="renameInfos">文件重命名信息列表</param>
        /// <returns>异步任务结果</returns>
        public async Task<bool> BatchRenameFilesAsync(List<FileRenameInfo> renameInfos)
        {
            if (renameInfos == null || renameInfos.Count == 0)
            {
                return false;
            }

            int successCount = 0;
            int errorCount = 0;

            await Task.Run(() =>
            {
                for (int i = 0; i < renameInfos.Count; i++)
                {
                    FileRenameInfo fileInfo = renameInfos[i];
                    if (fileInfo == null || string.IsNullOrEmpty(fileInfo.FullPath))
                    {
                        errorCount++;
                        continue;
                    }

                    try
                    {
                        bool success = RenameFileImmediately(fileInfo.FullPath, fileInfo.NewName);
                        
                        if (success)
                        {
                            successCount++;
                            FileRenamed?.Invoke(this, new FileRenamedEventArgs
                            {
                                OldPath = fileInfo.FullPath,
                                NewPath = Path.Combine(Path.GetDirectoryName(fileInfo.FullPath), fileInfo.NewName)
                            });
                        }
                        else
                        {
                            errorCount++;
                            RenameError?.Invoke(this, new RenameErrorEventArgs
                            {
                                FilePath = fileInfo.FullPath,
                                ErrorMessage = "重命名文件失败"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        RenameError?.Invoke(this, new RenameErrorEventArgs
                        {
                            FilePath = fileInfo.FullPath,
                            ErrorMessage = ex.Message,
                            Exception = ex
                        });
                    }
                }

                // 触发批量重命名完成事件
                BatchRenameCompleted?.Invoke(this, new RenameBatchCompletedEventArgs
                {
                    TotalFiles = renameInfos.Count,
                    SuccessfulRenames = successCount,
                    FailedRenames = errorCount
                });
            });

            // 如果成功重命名的文件数量大于0，则返回true
            return successCount > 0;
        }

        /// <summary>
        /// 生成新文件名（接口实现）
        /// </summary>
        /// <param name="baseName">基础名称</param>
        /// <param name="extension">扩展名</param>
        /// <param name="pattern">重命名模式</param>
        /// <param name="sequenceNumber">序列号</param>
        /// <returns>生成的新文件名</returns>
        public string GenerateNewFileName(string baseName, string extension, string pattern, int sequenceNumber = 1)
        {
            try
            {
                // 根据模式生成新文件名
                string newFileName = pattern;
                newFileName = newFileName.Replace("{序号}", sequenceNumber.ToString());
                newFileName = newFileName.Replace("{原文件名}", baseName);
                newFileName = newFileName.Replace("{扩展名}", extension);
                newFileName = newFileName.Replace("{日期}", DateTime.Now.ToString("yyyyMMdd"));
                newFileName = newFileName.Replace("{时间}", DateTime.Now.ToString("HHmmss"));

                // 确保文件名有效（移除非法字符）
                string invalidChars = new string(Path.GetInvalidFileNameChars());
                foreach (char c in invalidChars)
                {
                    newFileName = newFileName.Replace(c.ToString(), "");
                }

                // 添加扩展名（如果模式中没有包含）
                if (!newFileName.Contains("." + extension) && !string.IsNullOrEmpty(extension))
                {
                    if (!newFileName.EndsWith("."))
                        newFileName += ".";
                    newFileName += extension;
                }

                return newFileName;
            }
            catch (Exception ex)
            {
                LogHelper.Debug("生成新文件名时出错: " + ex.Message);
                // 出错时返回基础名称+扩展名
                return baseName + (string.IsNullOrEmpty(extension) ? "" : ("." + extension));
            }
        }

        /// <summary>
        /// 验证重命名模式（接口实现）
        /// </summary>
        /// <param name="pattern">重命名模式</param>
        /// <returns>验证结果错误列表（为空表示验证通过）</returns>
        public List<string> ValidateRenamePattern(string pattern)
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrEmpty(pattern))
            {
                errors.Add("重命名模式不能为空");
                return errors;
            }

            // 检查模式中是否包含至少一个变量或固定文本
            bool hasVariable = pattern.Contains("{序号}") || 
                               pattern.Contains("{原文件名}") || 
                               pattern.Contains("{扩展名}") || 
                               pattern.Contains("{日期}") || 
                               pattern.Contains("{时间}");

            if (!hasVariable && pattern.Trim().Length == 0)
            {
                errors.Add("重命名模式必须包含至少一个变量或固定文本");
            }

            // 检查是否包含非法字符
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            foreach (char c in invalidChars)
            {
                if (pattern.Contains(c.ToString()))
                {
                    errors.Add("重命名模式包含非法字符: " + c);
                }
            }

            // 检查变量格式是否正确
            if (pattern.Contains("{序号}") || pattern.Contains("{原文件名}") || 
                pattern.Contains("{扩展名}") || pattern.Contains("{日期}") || pattern.Contains("{时间}"))
            {
                // 检查是否有未闭合的变量格式
                int openBraceCount = pattern.Split('{').Length - 1;
                int closeBraceCount = pattern.Split('}').Length - 1;
                if (openBraceCount != closeBraceCount)
                {
                    errors.Add("重命名模式中的变量格式不正确，存在未闭合的括号");
                }
            }

            return errors;
        }

        /// <summary>
        /// 触发文件重命名成功事件
        /// </summary>
        private void RaiseFileRenamedSuccessfully(FileRenameInfo fileInfo)
        {
            FileRenamedSuccessfully?.Invoke(this, new FileRenameEventArgs(fileInfo));
        }

        /// <summary>
        /// 触发文件重命名失败事件
        /// </summary>
        private void RaiseFileRenameFailed(FileRenameInfo fileInfo, string errorMessage)
        {
            FileRenameFailed?.Invoke(this, new FileRenameEventArgs(fileInfo) { ErrorMessage = errorMessage });
        }

        /// <summary>
        /// 触发批量重命名进度更新事件
        /// </summary>
        private void RaiseBatchRenameProgressChanged(int currentCount, int totalCount, FileRenameInfo currentFileInfo)
        {
            BatchRenameProgressChanged?.Invoke(this, new BatchRenameProgressEventArgs(currentCount, totalCount, currentFileInfo));
        }

        /// <summary>
        /// 查找第一个空文件行
        /// </summary>
        /// <param name="list">文件绑定列表</param>
        /// <returns>空行索引，如果未找到则返回-1</returns>
        private int FindFirstEmptyFileRow(BindingList<FileRenameInfo> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (string.IsNullOrEmpty(item.OriginalName) &&
                    string.IsNullOrEmpty(item.NewName) &&
                    string.IsNullOrEmpty(item.Status) &&
                    string.IsNullOrEmpty(item.ErrorMessage) &&
                    string.IsNullOrEmpty(item.FullPath) &&
                    string.IsNullOrEmpty(item.RegexResult) &&
                    string.IsNullOrEmpty(item.OrderNumber) &&
                    string.IsNullOrEmpty(item.Material) &&
                    string.IsNullOrEmpty(item.Quantity) &&
                    string.IsNullOrEmpty(item.Dimensions) &&
                    string.IsNullOrEmpty(item.Process) &&
                    string.IsNullOrEmpty(item.Time))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 更新现有的行数据
        /// </summary>
        /// <param name="row">网格行对象</param>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="processedData">处理后的数据</param>
        private void UpdateExistingRow(FileRenameInfo row, FileInfo fileInfo, ProcessedFileData processedData)
        {
            row.OriginalName = fileInfo.Name;
            row.NewName = processedData.NewFileName;
            row.FullPath = processedData.DestinationPath;
            row.RegexResult = processedData.RegexResult;
            row.OrderNumber = processedData.OrderNumber;
            row.Material = processedData.Material;
            row.Quantity = processedData.Quantity;
            row.Dimensions = processedData.Dimensions;
            row.Process = processedData.Process;
            row.Time = DateTime.Now.ToString("MM-dd");
            row.SerialNumber = processedData.SerialNumber;
            row.CompositeColumn = processedData.CompositeColumn;
            row.LayoutRows = processedData.LayoutRows;   // ✅ 新增：设置行数
            row.LayoutColumns = processedData.LayoutColumns;  // ✅ 新增：设置列数
            
            LogHelper.Debug($"服务层 UpdateExistingRow: 设置 CompositeColumn = '{processedData.CompositeColumn}'");
            
            // 处理PDF文件的特殊需求
            if (fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                row.PageCount = GetPdfPageCount(fileInfo.FullName);
            }
        }

        /// <summary>
        /// 创建新的文件重命名信息对象
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="processedData">处理后的数据</param>
        /// <returns>新的FileRenameInfo对象</returns>
        private FileRenameInfo CreateNewFileRenameInfo(FileInfo fileInfo, ProcessedFileData processedData)
        {
            var newRow = new FileRenameInfo
            {
                OriginalName = fileInfo.Name,
                NewName = processedData.NewFileName,
                FullPath = processedData.DestinationPath,
                RegexResult = processedData.RegexResult,
                OrderNumber = processedData.OrderNumber,
                Material = processedData.Material,
                Quantity = processedData.Quantity,
                Dimensions = processedData.Dimensions,
                Process = processedData.Process,
                Time = DateTime.Now.ToString("MM-dd"),
                SerialNumber = processedData.SerialNumber,
                LayoutRows = processedData.LayoutRows,    // ✅ 新增：设置行数
                LayoutColumns = processedData.LayoutColumns, // ✅ 新增：设置列数
                PageCount = fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) 
                    ? GetPdfPageCount(fileInfo.FullName) 
                    : null,
                CompositeColumn = processedData.CompositeColumn,
                // ✅ 初始化备份数据容器（用于保留模式）
                BackupData = new Dictionary<string, string>()
            };
            
            LogHelper.Debug($"服务层 CreateNewFileRenameInfo: 创建新行, CompositeColumn = '{processedData.CompositeColumn}'");
            return newRow;
        }

        /// <summary>
        /// 获取PDF页数
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <returns>PDF页数</returns>
        private int? GetPdfPageCount(string filePath)
        {
            try
            {
                // 获取PDF处理服务
                var pdfProcessingService = ServiceLocator.Instance.GetPdfProcessingService();
                return pdfProcessingService.GetPdfPageCount(filePath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 判断是否为临时文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否为临时文件</returns>
        private bool IsTemporaryFile(string fileName)
        {
            // 检查常见的临时文件扩展名
            string[] tempExtensions = { ".tmp", ".temp", "~" };
            foreach (string ext in tempExtensions)
            {
                if (fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // 检查是否以~开头（如~WRL0001.tmp）
            if (fileName.StartsWith("~"))
                return true;

            return false;
        }

        /// <summary>
        /// 处理正则表达式匹配
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="pattern">正则表达式模式</param>
        /// <param name="patternName">模式名称</param>
        /// <returns>正则匹配结果</returns>
        public RegexMatchResult ProcessRegexMatch(string fileName, string pattern, string patternName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                {
                    return RegexMatchResult.Failure("", "", "文件名不能为空", "ProcessRegexMatch");
                }

                if (string.IsNullOrEmpty(pattern))
                {
                    return RegexMatchResult.Failure(fileName, "", "正则表达式模式不能为空", "ProcessRegexMatch");
                }

                var originalName = Path.GetFileNameWithoutExtension(fileName);
                var regexMatch = Regex.Match(originalName, pattern);
                
                if (regexMatch.Success)
                {
                    // 针对特定模式增加特殊处理
                    if (pattern == @"^(.*?)-\\d+\\+.*$")
                    {
                        // 确保只获取第一个捕获组
                        if (regexMatch.Groups.Count > 1)
                        {
                            var result = RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                            result.MatchedText = regexMatch.Groups[1].Value;
                            return result;
                        }
                        else
                        {
                            return RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                        }
                    }
                    else
                    {
                        return RegexMatchResult.Success(originalName, pattern, regexMatch, patternName);
                    }
                }
                else
                {
                    return RegexMatchResult.Failure(originalName, pattern, $"正则表达式匹配失败: 模式='{pattern}', 文本='{originalName}'", "ProcessRegexMatch");
                }
            }
            catch (Exception ex)
            {
                return RegexMatchResult.Failure(fileName, pattern, $"正则匹配过程中发生异常: {ex.Message}", "ProcessRegexMatch");
            }
        }



        public string ProcessRegexForFileName(string pattern, string originalName)
        {
            var result = ProcessRegexMatch(originalName, pattern);
            if (result.IsMatch)
            {
                return result.MatchedText;
            }
            return string.Empty;
        }



        /// <summary>
        /// 构建新文件名
        /// </summary>
        /// <param name="components">文件名组件</param>
        /// <returns>构建的新文件名</returns>
        public string BuildNewFileName(FileNameComponents components)
        {
            try
            {
                // 首先验证组件
                var validationResult = components.Validate();
                if (!validationResult.IsValid)
                {
                    LogHelper.Debug($"BuildNewFileName: 文件名组件验证失败: {validationResult.ErrorMessage}");
                    return $"未命名{components.FileExtension}";
                }

                LogHelper.Debug($"BuildNewFileName: 开始构建文件名，组件顺序: [{(components.ComponentOrder != null && components.ComponentOrder.Count > 0 ? string.Join(", ", components.ComponentOrder) : "使用默认顺序")}]");

                // 使用FileNameComponents的内置方法构建文件名
                string newFileName = components.BuildFileName();
                
                LogHelper.Debug($"BuildNewFileName: 构建的文件名: '{newFileName}'");
                
                // 如果构建失败，使用备用方案
                if (string.IsNullOrEmpty(newFileName) || newFileName == components.FileExtension)
                {
                    LogHelper.Warn("BuildNewFileName: 构建的文件名为空，使用备用方案");
                    return $"未命名{components.FileExtension}";
                }

                LogHelper.Info($"BuildNewFileName: 最终文件名: '{newFileName}'");
                return newFileName;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"BuildNewFileName: 构建文件名时发生异常: {ex.Message}");
                LogHelper.Debug($"BuildNewFileName: 异常详情: {ex.StackTrace}");
                return $"未命名{components.FileExtension}";
            }
        }

        /// <summary>
        /// 生成或验证序号
        /// </summary>
        /// <param name="providedSerialNumber">提供的序号（可能为空）</param>
        /// <param name="bindingList">数据绑定列表</param>
        /// <param name="isFromExcel">是否来自Excel导入</param>
        /// <returns>最终使用的序号</returns>
        public string GenerateSerialNumber(string providedSerialNumber, BindingList<FileRenameInfo> bindingList, bool isFromExcel)
        {
            try
            {
                // 如果已提供序号且来自Excel，直接使用
                if (!string.IsNullOrEmpty(providedSerialNumber) && isFromExcel)
                {
                    return providedSerialNumber;
                }

                // 如果提供了序号但不是来自Excel，验证并使用
                if (!string.IsNullOrEmpty(providedSerialNumber))
                {
                    if (int.TryParse(providedSerialNumber, out int provided) && provided > 0)
                    {
                        return provided.ToString("D2");
                    }
                }

                // 需要自动生成序号
                int maxSerial = 0;
                if (bindingList?.Any() == true)
                {
                    // 遍历现有数据，找到最大序号
                    foreach (var item in bindingList)
                    {
                        if (int.TryParse(item.SerialNumber, out int currentSerial))
                        {
                            if (currentSerial > maxSerial)
                            {
                                maxSerial = currentSerial;
                            }
                        }
                    }
                }

                // 返回下一个序号，格式化为两位数
                return (maxSerial + 1).ToString("D2");
            }
            catch (Exception ex)
            {
                // 异常情况下返回默认序号
                LogHelper.Debug($"生成序号时发生异常: {ex.Message}");
                return "01";
            }
        }

        /// <summary>
        /// 计算和格式化显示尺寸
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="adjustedDimensions">调整后的尺寸字符串</param>
        /// <returns>格式化的尺寸字符串</returns>
        public string CalculateDisplayDimensions(FileInfo fileInfo, string adjustedDimensions)
        {
            try
            {
                string displayDimensions = "未知尺寸";

                // 尝试从adjustedDimensions提取尺寸
                if (!string.IsNullOrEmpty(adjustedDimensions))
                {
                    var matches = Regex.Matches(adjustedDimensions, @"\d+(?:\.\d+)?");
                    var dimensionsParts = matches.Cast<Match>().Select(m => m.Value).ToArray();

                    if (dimensionsParts.Length >= 2 &&
                        double.TryParse(dimensionsParts[0], out double width) &&
                        double.TryParse(dimensionsParts[1], out double height))
                    {
                        // 确保大数在前
                        double maxDim = Math.Max(width, height);
                        double minDim = Math.Min(width, height);
                        displayDimensions = $"{maxDim}x{minDim}";
                    }
                    else if (dimensionsParts.Length == 1)
                    {
                        displayDimensions = dimensionsParts[0];
                    }
                }

                // 如果adjustedDimensions为空或提取失败，直接从SettingsForm获取尺寸
                if (displayDimensions == "未知尺寸")
                {
                    displayDimensions = CalculatePdfDimensions(fileInfo.FullName);
                }

                return displayDimensions;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"计算显示尺寸时发生异常: {ex.Message}");
                return "未知尺寸";
            }
        }

        /// <summary>
        /// 从 PDF 文件计算尺寸
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>计算的尺寸字符串</returns>
        private string CalculatePdfDimensions(string filePath)
        {
            try
            {
                // 注意：这里需要一个SettingsForm的实例来计算PDF尺寸
                // 在实际应用中，可能需要重构这部分逻辑以避免依赖UI组件
                return "未知尺寸";
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"计算PDF尺寸时发生异常: {ex.Message}");
                return "未知尺寸";
            }
        }

        /// <summary>
        /// 创建处理后的数据对象
        /// </summary>
        /// <param name="newFileName">新文件名</param>
        /// <param name="destinationPath">目标路径</param>
        /// <param name="regexResult">正则结果</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="material">材料</param>
        /// <param name="quantity">数量</param>
        /// <param name="dimensions">尺寸</param>
        /// <param name="notes">工艺</param>
        /// <param name="serialNumber">序号</param>
        /// <param name="compositeColumn">列组合值</param>
        /// <returns>处理后的数据对象</returns>
        public ProcessedFileData CreateProcessedFileData(string newFileName, string destinationPath, string regexResult, 
            string orderNumber, string material, string quantity, string dimensions, string notes, string serialNumber, string compositeColumn = null)
        {
            return new ProcessedFileData
            {
                NewFileName = newFileName,
                DestinationPath = destinationPath,
                RegexResult = regexResult,
                OrderNumber = orderNumber,
                Material = material,
                Quantity = quantity,
                Dimensions = dimensions,
                Process = notes,
                SerialNumber = serialNumber,
                CompositeColumn = compositeColumn
            };
        }

        /// <summary>
        /// 创建文件名组件对象
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="material">材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="unit">单位</param>
        /// <param name="dimensions">尺寸</param>
        /// <param name="notes">工艺</param>
        /// <param name="serialNumber">序号</param>
        /// <param name="regexPart">正则部分</param>
        /// <param name="separator">分隔符</param>
        /// <param name="enabledComponents">启用的组件配置</param>
        /// <returns>文件名组件对象</returns>
        public FileNameComponents CreateFileNameComponents(FileInfo fileInfo, string material, string orderNumber, 
            string quantity, string unit, string dimensions, string notes, string serialNumber, string regexPart,
            string separator, FileNameComponentsConfig enabledComponents)
        {
            var components = new FileNameComponents
            {
                RegexResult = regexPart,
                OrderNumber = orderNumber,
                Material = material,
                Quantity = quantity,
                Unit = unit,
                Dimensions = dimensions,
                Process = notes,
                SerialNumber = serialNumber,
                FileExtension = fileInfo.Extension,
                Separator = separator,
                EnabledComponents = enabledComponents,
                // 从EventGroup配置中获取前缀
                Prefixes = GetPrefixesFromEventGroupConfig()
            };

            // 确保分隔符为合法文件名字符（允许空字符串）
            if (!string.IsNullOrEmpty(components.Separator) && Path.GetInvalidFileNameChars().Contains(components.Separator[0]))
            {
                components.Separator = "_";
            }

            return components;
        }

        /// <summary>
        /// 处理文件名中的正则表达式匹配
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="pattern">正则表达式模式</param>
        /// <param name="isRegexEnabled">是否启用正则结果</param>
        /// <returns>正则匹配结果，如果用户取消则返回null</returns>
        public string ProcessRegexForFileName(string fileName, string pattern, bool isRegexEnabled)
        {
            if (!isRegexEnabled)
            {
                return string.Empty;
            }

            var regexResult = ProcessRegexMatch(fileName, pattern);
            if (!regexResult.IsMatch)
            {
                // 在服务层中，我们不直接显示对话框，而是返回一个特殊值表示需要处理
                return "REGEX_MATCH_FAILED:" + Path.GetFileNameWithoutExtension(fileName);
            }
            
            return regexResult.MatchedText;
        }

        /// <summary>
        /// 解析文件名组件配置
        /// </summary>
        /// <param name="eventItems">事件项字符串</param>
        /// <returns>文件名组件配置</returns>
        public FileNameComponentsConfig ParseFileNameConfig(string eventItems)
        {
            var config = new FileNameComponentsConfig();
            
            try
            {
                if (string.IsNullOrEmpty(eventItems))
                    return config; // 返回默认配置（全部启用）

                var eventItemPairs = eventItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                
                for (int i = 0; i < eventItemPairs.Length; i += 2)
                {
                    if (i + 1 < eventItemPairs.Length)
                    {
                        string itemText = eventItemPairs[i];
                        if (bool.TryParse(eventItemPairs[i + 1], out bool isEnabled))
                        {
                            switch (itemText)
                            {
                                case "正则结果":
                                    config.RegexResultEnabled = isEnabled;
                                    break;
                                case "订单号":
                                    config.OrderNumberEnabled = isEnabled;
                                    break;
                                case "材料":
                                    config.MaterialEnabled = isEnabled;
                                    break;
                                case "数量":
                                    config.QuantityEnabled = isEnabled;
                                    break;
                                case "尺寸":
                                    config.DimensionsEnabled = isEnabled;
                                    break;
                                case "工艺":
                                    config.ProcessEnabled = isEnabled;
                                    break;
                                case "序号":
                                    config.SerialNumberEnabled = isEnabled;
                                    break;
                                case "列组合":
                                    config.CompositeColumnEnabled = isEnabled;
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"解析文件名配置时发生异常: {ex.Message}");
                // 异常情况下返回默认配置（全部启用）
            }
            
            return config;
        }

        /// <summary>
        /// 从EventGroup配置中获取前缀字典
        /// </summary>
        /// <returns>前缀字典，键为组件名称，值为前缀</returns>
        private Dictionary<string, string> GetPrefixesFromEventGroupConfig()
        {
            var prefixes = new Dictionary<string, string>();

            try
            {
                // 获取EventGroup配置
                var eventGroupConfig = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups == null)
                {
                    LogHelper.Debug("EventGroup配置为空，使用空前缀字典");
                    return prefixes;
                }

                // 遍历所有分组，建立组件名到前缀的映射
                foreach (var group in eventGroupConfig.Groups)
                {
                    // 获取该分组下的所有项目
                    var groupItems = eventGroupConfig.Items.Where(item => item.GroupId == group.Id);

                    foreach (var item in groupItems)
                    {
                        // 如果没有前缀则跳过
                        if (string.IsNullOrEmpty(group.Prefix))
                            continue;

                        LogHelper.Debug($"[GetPrefixesFromEventGroupConfig] 正在处理: item.Name='{item.Name}', group.Id='{group.Id}', group.Prefix='{group.Prefix}'");

                        // 建立组件名到前缀的映射
                        if (!prefixes.ContainsKey(item.Name))
                        {
                            prefixes[item.Name] = group.Prefix;
                            LogHelper.Debug($"[GetPrefixesFromEventGroupConfig] 已添加映射: '{item.Name}' -> '{group.Prefix}'");
                        }
                        else
                        {
                            LogHelper.Debug($"[GetPrefixesFromEventGroupConfig] 键已存在，跳过: '{item.Name}'");
                        }
                    }
                }

                LogHelper.Debug($"获取到的前缀配置: {string.Join(", ", prefixes.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"获取前缀配置时发生异常: {ex.Message}");
            }

            return prefixes;
        }

        /// <summary>
        /// 验证文件添加到网格的输入参数
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="material">材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="serialNumber">序号</param>
        /// <returns>验证结果</returns>
        public WindowsFormsApp3.Models.ValidationResult ValidateFileGridInput(FileInfo fileInfo, string material, string orderNumber, string quantity, string serialNumber, bool isSerialNumberFromExcel = false)
        {
            try
            {
                // 验证文件信息
                if (fileInfo == null)
                {
                    return WindowsFormsApp3.Models.ValidationResult.Failure("文件信息不能为空", ValidationErrorType.InvalidFileInfo, "ValidateFileGridInput");
                }

                if (!fileInfo.Exists)
                {
                    return WindowsFormsApp3.Models.ValidationResult.Failure($"文件不存在: {fileInfo.FullName}", ValidationErrorType.InvalidFileInfo, "ValidateFileGridInput");
                }

                // 验证数量和序号至少有一个不为空
                if (string.IsNullOrEmpty(quantity) && string.IsNullOrEmpty(serialNumber))
                {
                    return WindowsFormsApp3.Models.ValidationResult.Failure("数量和序号不能同时为空", ValidationErrorType.InvalidParameters, "ValidateFileGridInput");
                }

                // 验证临时文件
                if (IsTemporaryFile(fileInfo.Name))
                {
                    return WindowsFormsApp3.Models.ValidationResult.Failure("不能添加临时文件", ValidationErrorType.InvalidFileInfo, "ValidateFileGridInput");
                }

                // 验证序号格式（如果不为空）
                if (!string.IsNullOrEmpty(serialNumber))
                {
                    if (isSerialNumberFromExcel)
                    {
                        // Excel来源的序号：支持任意字符（字母、数字、符号等）
                        if (string.IsNullOrWhiteSpace(serialNumber))
                        {
                            return WindowsFormsApp3.Models.ValidationResult.Failure($"序号不能为空白字符", ValidationErrorType.InvalidSerialNumber, "ValidateFileGridInput");
                        }
                        
                        // 验证序号长度不超过合理限制
                        if (serialNumber.Length > 50)
                        {
                            return WindowsFormsApp3.Models.ValidationResult.Failure($"序号长度不能超过50个字符", ValidationErrorType.InvalidSerialNumber, "ValidateFileGridInput");
                        }
                    }
                    else
                    {
                        // 非Excel来源的序号：必须是正整数（用于自动递增）
                        if (!int.TryParse(serialNumber, out int serial) || serial <= 0)
                        {
                            return WindowsFormsApp3.Models.ValidationResult.Failure($"序号格式无效: {serialNumber}，非Excel来源的序号必须是正整数", ValidationErrorType.InvalidSerialNumber, "ValidateFileGridInput");
                        }
                    }
                }

                // 验证数量格式（如果不为空）
                if (!string.IsNullOrEmpty(quantity))
                {
                    if (!int.TryParse(quantity, out int qty) || qty <= 0)
                    {
                        return WindowsFormsApp3.Models.ValidationResult.Failure($"数量格式无效: {quantity}", ValidationErrorType.InvalidQuantity, "ValidateFileGridInput");
                    }
                }

                return WindowsFormsApp3.Models.ValidationResult.Success("ValidateFileGridInput");
            }
            catch (Exception ex)
            {
                return WindowsFormsApp3.Models.ValidationResult.Failure($"验证过程中发生异常: {ex.Message}", ValidationErrorType.General, "ValidateFileGridInput");
            }
        }

        /// <summary>
        /// 添加文件到网格（将Form1中的AddFileToGrid逻辑迁移到服务中）
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="processedData">处理后的数据</param>
        /// <param name="bindingList">文件绑定列表</param>
        /// <returns>是否添加成功</returns>
        bool WindowsFormsApp3.Interfaces.IFileRenameService.AddFileToGrid(FileInfo fileInfo, ProcessedFileData processedData, BindingList<FileRenameInfo> bindingList)
        {
            try
            {
                if (fileInfo == null || processedData == null || bindingList == null)
                {
                    return false;
                }

                int emptyRowIndex = FindFirstEmptyFileRow(bindingList);
                if (emptyRowIndex != -1)
                {
                    // 更新现有空行
                    UpdateExistingRow(bindingList[emptyRowIndex], fileInfo, processedData);
                }
                else
                {
                    // 添加新行
                    var newRow = CreateNewFileRenameInfo(fileInfo, processedData);
                    bindingList.Add(newRow);
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"添加文件到网格时发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 处理文件名冲突
        /// </summary>
        /// <param name="existingPath">已存在的文件路径</param>
        /// <param name="newPath">新的文件路径</param>
        /// <returns>解决冲突后的文件路径</returns>
        public string HandleFileNameConflict(string existingPath, string newPath)
        {
            try
            {
                if (string.IsNullOrEmpty(existingPath) || string.IsNullOrEmpty(newPath))
                {
                    return newPath;
                }

                string directory = Path.GetDirectoryName(newPath);
                string fileName = Path.GetFileNameWithoutExtension(newPath);
                string extension = Path.GetExtension(newPath);

                int counter = 1;
                string resolvedPath = newPath;

                while (File.Exists(resolvedPath))
                {
                    string newFileName = $"{fileName}({counter}){extension}";
                    resolvedPath = Path.Combine(directory, newFileName);
                    counter++;
                }

                return resolvedPath;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理文件名冲突时发生异常: {ex.Message}");
                return newPath;
            }
        }
    }
}