using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Properties;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Services.Events;
using WindowsFormsApp3.Forms.Dialogs;


namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 批量处理服务，提供高效的文件批量处理功能
    /// 实现IBatchProcessingService接口，支持文件的批量重命名、复制等操作
    /// </summary>
    public class BatchProcessingService : IBatchProcessingService
    {
        private readonly Interfaces.IFileRenameService _fileRenameService;
        private readonly WindowsFormsApp3.Services.IPdfProcessingService _pdfProcessingService;
        private readonly WindowsFormsApp3.Interfaces.ILogger _logger;
        private readonly WindowsFormsApp3.Services.IEventBus _eventBus;
        private string _currentRegexPattern;
        private List<EventGroupConfig> _preserveGroupConfigs;

        /// <summary>
        /// 设置当前正则表达式模式
        /// </summary>
        public string CurrentRegexPattern
        {
            get { return _currentRegexPattern; }
            set { _currentRegexPattern = value; }
        }

        /// <summary>
        /// 进度变更事件
        /// </summary>
        public event EventHandler<WindowsFormsApp3.Models.BatchProgressEventArgs> ProgressChanged;

        /// <summary>
        /// 批量处理完成事件
        /// </summary>
        public event EventHandler<BatchCompleteEventArgs> ProcessingComplete;

        /// <summary>
        /// 当前正在处理的文件数
        /// </summary>
        private int _currentProgress;

        /// <summary>
        /// 总文件数
        /// </summary>
        private int _totalFiles;

        /// <summary>
        /// 是否取消处理
        /// </summary>
        private bool _cancellationRequested;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fileRenameService">文件重命名服务</param>
        /// <param name="pdfProcessingService">PDF处理服务</param>
        /// <param name="logger">日志服务</param>
        /// <param name="eventBus">事件总线</param>
        public BatchProcessingService(Interfaces.IFileRenameService fileRenameService,
                                     WindowsFormsApp3.Services.IPdfProcessingService pdfProcessingService,
                                     WindowsFormsApp3.Interfaces.ILogger logger,
                                     WindowsFormsApp3.Services.IEventBus eventBus)
        {
            _fileRenameService = fileRenameService ?? throw new ArgumentNullException(nameof(fileRenameService));
            _pdfProcessingService = pdfProcessingService ?? throw new ArgumentNullException(nameof(pdfProcessingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// 供ServiceLocator使用的构造函数，用于兼容现有代码
        /// </summary>
        public BatchProcessingService()
        {
            _fileRenameService = ServiceLocator.Instance.GetFileRenameService();
            _pdfProcessingService = ServiceLocator.Instance.GetPdfProcessingService();
            _logger = ServiceLocator.Instance.GetLogger();
            _eventBus = ServiceLocator.Instance.GetEventBus();
        }

        /// <summary>
        /// 设置保留分组配置
        /// </summary>
        /// <param name="preserveGroupConfigs">保留的分组配置列表</param>
        public void SetPreserveGroupConfigs(List<EventGroupConfig> preserveGroupConfigs)
        {
            _preserveGroupConfigs = preserveGroupConfigs?.Where(g => g.IsPreserved).ToList() ?? new List<EventGroupConfig>();
            _logger?.LogInformation($"已设置保留分组配置，共{_preserveGroupConfigs.Count}个保留分组");
        }

        /// <summary>
        /// 应用保留模式到文件列表
        /// </summary>
        /// <param name="fileInfos">文件信息列表</param>
        public void ApplyPreserveModeToFileList(List<FileRenameInfo> fileInfos)
        {
            if (_preserveGroupConfigs == null || !_preserveGroupConfigs.Any() || fileInfos == null)
            {
                _logger?.LogInformation("无法应用保留模式：保留配置或文件列表为空");
                return;
            }

            var preserveItemNames = new List<string>();
            foreach (var groupConfig in _preserveGroupConfigs)
            {
                preserveItemNames.AddRange(groupConfig.Items);
            }

            _logger?.LogInformation($"ApplyPreserveModeToFileList: 准备为 {fileInfos.Count} 个文件备份 {preserveItemNames.Count} 个字段: {string.Join(", ", preserveItemNames)}");

            // 🔑 关键修复：获取当前配置的前缀映射
            // 不同配置预设可能使用不同的前缀，需要动态获取
            var fieldPrefixMapping = GetFieldPrefixMappingFromCurrentConfig();
            _logger?.LogInformation($"当前配置的前缀映射: {string.Join(", ", fieldPrefixMapping.Select(kvp => kvp.Key + "=" + kvp.Value))}");

            foreach (var fileInfo in fileInfos)
            {
                fileInfo.IsPreserveMode = true;
                
                // 🔑 关键修复：为每个文件设置前缀映射
                // 这样 BackupFieldFromOriginalName 调用时才能使用正确的前缀
                fileInfo.FieldPrefixMapping = new Dictionary<string, string>(fieldPrefixMapping);
                _logger?.LogDebug($"为文件 {fileInfo.OriginalName} 设置前缀映射（共{fieldPrefixMapping.Count}个映射）");

                // 备份所有保留字段
                foreach (var itemName in preserveItemNames)
                {
                    // ✅ 关键修复：从配置中的项目名称提取纯净的字段名
                    // 配置中可能是"[*] 订单号"，需要提取为"订单号"
                    string cleanItemName = ExtractCleanFieldName(itemName);
                    string actualPrefix = fieldPrefixMapping.ContainsKey(cleanItemName) ? fieldPrefixMapping[cleanItemName] : "[未在映射中]";
                    _logger?.LogDebug($"备份字段: '{itemName}' -> 清理后: '{cleanItemName}' -> 使用前缀: {actualPrefix}");
                    
                    // ⭐ 新增诊断：备份前检查原文件名
                    _logger?.LogDebug($"  [备份前] OriginalName='{fileInfo.OriginalName}'");
                    
                    fileInfo.BackupFieldFromOriginalName(cleanItemName);
                    
                    // ⭐ 新增诊断：备份后检查 BackupData 和属性值
                    string backupedValue = fileInfo.BackupData.ContainsKey(cleanItemName) ? fileInfo.BackupData[cleanItemName] : "[未备份]"; 
                    _logger?.LogDebug($"  [备份后] BackupData['{cleanItemName}']='{backupedValue}'");
                }

                _logger?.LogDebug($"文件 {fileInfo.OriginalName} 备份完成，备份数据数: {fileInfo.BackupData.Count}，内容: {string.Join(", ", fileInfo.BackupData.Select(kv => kv.Key + "=" + kv.Value))}");
            }

            _logger?.LogInformation($"已为{fileInfos.Count}个文件应用保留模式，保留字段数: {preserveItemNames.Count}");
        }

        /// <summary>
        /// Extract clean field name from config item name
        /// Config may have "[*] OrderNumber", needs to extract as "OrderNumber"
        /// </summary>
        private string ExtractCleanFieldName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return "";

            var cleaned = itemName.Trim();
            
            if (cleaned.StartsWith("[*]"))
            {
                cleaned = cleaned.Substring(3).Trim();
            }
            
            return cleaned;
        }

        /// <summary>
        /// Get field-to-prefix mapping from current config
        /// Different presets may have different prefix mappings, need dynamic lookup
        /// </summary>
        /// <returns>Dictionary of field name to prefix mapping</returns>
        private Dictionary<string, string> GetFieldPrefixMappingFromCurrentConfig()
        {
            var fieldPrefixMapping = new Dictionary<string, string>();
            
            try
            {
                // Get current EventGroupConfiguration
                var eventGroupConfig = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (eventGroupConfig?.Groups == null || eventGroupConfig.Items == null)
                {
                    _logger?.LogWarning("Unable to get EventGroupConfiguration, using empty mapping");
                    return fieldPrefixMapping;
                }
                
                // Traverse all groups and build field-to-prefix mapping
                foreach (var group in eventGroupConfig.Groups)
                {
                    // Skip if no prefix
                    if (string.IsNullOrEmpty(group.Prefix))
                        continue;
                    
                    // Get all items in this group
                    var groupItems = eventGroupConfig.Items.Where(item => item.GroupId == group.Id);
                    
                    foreach (var item in groupItems)
                    {
                        // Clean up item name (remove "[*] " prefix if exists)
                        string cleanedItemName = ExtractCleanFieldName(item.Name);
                        
                        if (!fieldPrefixMapping.ContainsKey(cleanedItemName))
                        {
                            fieldPrefixMapping[cleanedItemName] = group.Prefix;
                            _logger?.LogDebug($"[GetFieldPrefixMappingFromCurrentConfig] Mapping: {cleanedItemName} -> {group.Prefix}");
                        }
                    }
                }
                
                _logger?.LogInformation($"[GetFieldPrefixMappingFromCurrentConfig] Got {fieldPrefixMapping.Count} field mappings");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error getting field prefix mapping: {ex.Message}");
            }
            
            return fieldPrefixMapping;
        }

        /// <summary>
        /// 恢复保留字段的原始数据
        /// </summary>
        /// <param name="fileInfos">文件信息列表</param>
        public void RestorePreservedFields(List<FileRenameInfo> fileInfos)
        {
            if (fileInfos == null)
                return;

            var restoreCount = 0;
            _logger?.LogInformation($"RestorePreservedFields: 开始恢复 {fileInfos.Count} 个文件的保留字段");

            foreach (var fileInfo in fileInfos)
            {
                _logger?.LogDebug($"文件 {fileInfo.OriginalName}: IsPreserveMode={fileInfo.IsPreserveMode}, BackupData.Count={fileInfo.BackupData?.Count ?? 0}");

                if (fileInfo.IsPreserveMode && fileInfo.BackupData != null && fileInfo.BackupData.Any())
                {
                    RestoreBackupDataForSingleFile(fileInfo);
                    restoreCount++;
                }
                else
                {
                    _logger?.LogDebug($"文件 {fileInfo.OriginalName}: 跳过恢复（IsPreserveMode={fileInfo.IsPreserveMode}, BackupData为空或无数据）");
                }
            }

            _logger?.LogInformation($"已恢复{restoreCount}个文件的保留字段数据");
        }

        /// <summary>
        /// 恢复单个文件的备份数据
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        private void RestoreBackupDataForSingleFile(FileRenameInfo fileInfo)
        {
            if (fileInfo.BackupData == null || !fileInfo.BackupData.Any())
                return;

            int successCount = 0;
            int failureCount = 0;

            foreach (var backup in fileInfo.BackupData)
            {
                var fieldName = backup.Key;        // "订单号"、"工艺" 等
                var backupValue = backup.Value;    // 备份的值

                // ✅ 修改：现在只有文件名中实际存在的字段才会被备份
                // 所以这里可以直接恢复备份的值，不需要检查是否为空
                // 如果为空，说明文件名中虽然存在该字段的前缀，但值是空的，应该恢复为空
                // 这是正确的行为

                // ✅ 使用 PreserveFieldMapper 获取对应的属性名
                string propertyName = PreserveFieldMapper.GetPropertyName(fieldName);
                
                if (string.IsNullOrEmpty(propertyName))
                {
                    _logger?.LogWarning($"未找到字段 '{fieldName}' 的属性映射");
                    failureCount++;
                    continue;
                }

                try
                {
                    // ✅ 直接调用属性的setter（会触发 OnPropertyChanged）
                    // 使用属性名的初字母大写来匹配C#命名规范
                    bool setSuccess = false;
                    
                    switch (propertyName)
                    {
                        case "OrderNumber":
                            fileInfo.OrderNumber = backupValue;
                            setSuccess = true;
                            break;
                        case "Material":
                            fileInfo.Material = backupValue;
                            setSuccess = true;
                            break;
                        case "Quantity":
                            fileInfo.Quantity = backupValue;
                            setSuccess = true;
                            break;
                        case "Process":
                            fileInfo.Process = backupValue;
                            setSuccess = true;
                            break;
                        case "Dimensions":
                            fileInfo.Dimensions = backupValue;
                            setSuccess = true;
                            break;
                        case "RegexResult":
                            fileInfo.RegexResult = backupValue;
                            setSuccess = true;
                            break;
                        case "CompositeColumn":
                            fileInfo.CompositeColumn = backupValue;
                            setSuccess = true;
                            break;
                        case "LayoutRows":
                            fileInfo.LayoutRows = backupValue;
                            setSuccess = true;
                            break;
                        case "LayoutColumns":
                            fileInfo.LayoutColumns = backupValue;
                            setSuccess = true;
                            break;
                        case "SerialNumber":
                            fileInfo.SerialNumber = backupValue;
                            setSuccess = true;
                            break;
                        case "Time":
                            fileInfo.Time = backupValue;
                            setSuccess = true;
                            break;
                        case "Status":
                            fileInfo.Status = backupValue;
                            setSuccess = true;
                            break;
                        case "ErrorMessage":
                            fileInfo.ErrorMessage = backupValue;
                            setSuccess = true;
                            break;
                        default:
                            _logger?.LogWarning($"未知的属性名 '{propertyName}'，无法恢复");
                            failureCount++;
                            break;
                    }

                    if (setSuccess)
                    {
                        _logger?.LogInformation($"✓ 已恢复字段 '{fieldName}' → 属性 '{propertyName}' = '{backupValue}'（已通过setter触发PropertyChanged事件）");
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning($"恢复字段 '{fieldName}' → '{propertyName}' 时出错: {ex.Message}");
                    failureCount++;
                }
            }

            _logger?.LogInformation($"已恢复 {fileInfo.OriginalName} 的保留字段: 成功={successCount}, 失败={failureCount}, 总计={fileInfo.BackupData.Count}");
        }

        /// <summary>
        /// 开始批量处理文件
        /// </summary>
        /// <param name="fileInfos">文件信息列表</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="batchSize">批处理大小</param>
        /// <param name="maxDegreeOfParallelism">最大并行度</param>
        public async Task StartBatchProcessingAsync(List<FileRenameInfo> fileInfos,
                                                   string exportPath,
                                                   bool isCopyMode = false,
                                                   int batchSize = 10,
                                                   int maxDegreeOfParallelism = 4)
        {
            if (fileInfos == null)
                fileInfos = new List<FileRenameInfo>();

            // 即使是空列表，也应该触发完成事件
            if (fileInfos.Count == 0)
            {
                _logger.LogInformation("处理空文件列表");
                var completeArgs = new WindowsFormsApp3.Models.BatchCompleteEventArgs(
                    successCount: 0,
                    failedCount: 0,
                    failedFiles: new List<WindowsFormsApp3.FileRenameInfo>(),
                    isCanceled: false
                );
                OnProcessingComplete(completeArgs);
                return;
            }

            _cancellationRequested = false;
            _currentProgress = 0;
            _totalFiles = fileInfos.Count;
            int successCount = 0;
            int failedCount = 0;
            var failedFiles = new ConcurrentBag<FailedFileInfo>();

            try
            {
                _logger.LogInformation($"开始批量处理，共{_totalFiles}个文件，批处理大小: {batchSize}，最大并行度: {maxDegreeOfParallelism}");

                // 创建导出目录（如果不存在）
                IOHelper.EnsureDirectoryExists(exportPath);

                // 分批处理文件
                var batches = SplitIntoBatches(fileInfos, batchSize);
                int batchIndex = 0;

                // 创建并行处理的选项
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, maxDegreeOfParallelism),
                    CancellationToken = new System.Threading.CancellationToken()
                };

                // 处理每个批次
                foreach (var batch in batches)
                {
                    if (_cancellationRequested)
                        break;

                    batchIndex++;
                    _logger.LogDebug($"开始处理批次 {batchIndex}/{batches.Count()}，文件数量: {batch.Count()}");

                    // 处理当前批次
                    var batchResult = await ProcessBatchAsync(batch.ToList(), exportPath, isCopyMode, parallelOptions);

                    // 更新统计信息
                    successCount += batchResult.SuccessCount;
                    failedCount += batchResult.FailedCount;

                    // 添加失败的文件信息
                    foreach (var failedFile in batchResult.FailedFiles)
                    {
                        failedFiles.Add(failedFile);
                    }

                    _logger.LogDebug($"批次 {batchIndex} 处理完成，成功: {batchResult.SuccessCount}，失败: {batchResult.FailedCount}");
                }

                // 触发完成事件
                var failedFileRenameInfos = failedFiles.Select(f => new WindowsFormsApp3.FileRenameInfo { FullPath = f.FilePath, ErrorMessage = f.ErrorMessage }).ToList();
                OnProcessingComplete(new WindowsFormsApp3.Models.BatchCompleteEventArgs(
                    successCount: successCount,
                    failedCount: failedCount,
                    failedFiles: failedFileRenameInfos,
                    isCanceled: _cancellationRequested
                ));

                _logger.LogInformation($"批量处理完成，成功: {successCount}，失败: {failedCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量处理过程中发生异常");

                // 触发完成事件，标记为失败
                int unprocessedCount = _totalFiles - successCount - failedCount;
                int totalFailedCount = failedCount + unprocessedCount;
                var failedFileRenameInfos = failedFiles.Select(f => new WindowsFormsApp3.FileRenameInfo { FullPath = f.FilePath, ErrorMessage = f.ErrorMessage }).ToList();

                // 添加未处理的文件作为失败文件
                if (unprocessedCount > 0)
                {
                    // 这里简化处理，不单独列出未处理的文件
                }

                OnProcessingComplete(new WindowsFormsApp3.Models.BatchCompleteEventArgs(
                    successCount: successCount,
                    failedCount: totalFailedCount,
                    failedFiles: failedFileRenameInfos,
                    isCanceled: _cancellationRequested
                ));
            }
        }

        /// <summary>
        /// 异步批量处理文件（支持取消令牌）
        /// </summary>
        /// <param name="fileInfos">文件信息列表</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="batchSize">批处理大小</param>
        /// <param name="maxDegreeOfParallelism">最大并行度</param>
        /// <param name="progress">进度报告</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量处理结果</returns>
        public async Task<WindowsFormsApp3.Models.BatchProcessResult> ProcessFilesAsync(List<FileRenameInfo> fileInfos,
                                                               string exportPath,
                                                               bool isCopyMode = false,
                                                               int batchSize = 10,
                                                               int maxDegreeOfParallelism = -1,
                                                               IProgress<WindowsFormsApp3.Models.BatchProgress> progress = null,
                                                               CancellationToken cancellationToken = default)
        {
            if (fileInfos == null)
                throw new ArgumentNullException(nameof(fileInfos));

            if (string.IsNullOrEmpty(exportPath))
                throw new ArgumentException("导出路径不能为空", nameof(exportPath));

            var result = new WindowsFormsApp3.Models.BatchProcessResult();
            var fileList = fileInfos.Where(f => f != null).ToList();
            var processedCount = 0;

            // 设置默认并行度
            if (maxDegreeOfParallelism <= 0)
                maxDegreeOfParallelism = Environment.ProcessorCount;

            try
            {
                _logger.LogInformation($"开始异步批量处理 {fileList.Count} 个文件，并行度: {maxDegreeOfParallelism}");

                // 应用保留模式到文件列表
                if (_preserveGroupConfigs != null && _preserveGroupConfigs.Any())
                {
                    ApplyPreserveModeToFileList(fileList);
                    _logger.LogInformation("已应用保留模式到文件列表");
                }

                // 确保导出目录存在
                await IOHelper.EnsureDirectoryExistsAsync(exportPath, cancellationToken);

                // 发布批量处理开始事件
                _eventBus?.Publish(new BatchProcessingStartedEvent
                {
                    ProcessingType = isCopyMode ? "文件复制" : "文件重命名",
                    FileCount = fileList.Count,
                    TargetPath = exportPath
                });

                // 分批处理文件以优化性能
                var batches = SplitIntoBatches(fileList, batchSize);

                foreach (var batch in batches)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 并行处理当前批次
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(batch, new ParallelOptions
                        {
                            MaxDegreeOfParallelism = maxDegreeOfParallelism,
                            CancellationToken = cancellationToken
                        }, async fileInfo =>
                        {
                            try
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                // 异步处理单个文件
                                bool success = await ProcessSingleFileAsync(fileInfo, exportPath, isCopyMode, cancellationToken);

                                if (success)
                                {
                                    result.SuccessCount++;
                                }
                                else
                                {
                                    result.AddError(fileInfo.FullPath, "文件处理失败");
                                }

                                // 更新进度
                                int current = Interlocked.Increment(ref processedCount);
                                progress?.Report(new WindowsFormsApp3.Models.BatchProgress
                                {
                                    ProcessedCount = current,
                                    TotalCount = fileList.Count,
                                    CurrentFile = fileInfo.OriginalName
                                });

                                // 触发进度事件
                                OnProgressChanged(new BatchProgressEventArgs(current, fileList.Count));
                            }
                            catch (OperationCanceledException)
                            {
                                result.AddError(fileInfo.FullPath, "操作被取消");
                                throw;
                            }
                            catch (Exception ex)
                            {
                                result.AddError(fileInfo.FullPath, ex.Message);
                                _logger.LogError(ex, $"处理文件 {fileInfo.OriginalName} 时出错");
                            }
                        });
                    }, cancellationToken);
                }

                // 发布批量处理完成事件
                _eventBus?.Publish(new BatchProcessingCompletedEvent
                {
                    ProcessingType = isCopyMode ? "文件复制" : "文件重命名",
                    SuccessCount = result.SuccessCount,
                    FailedCount = result.Errors.Count,
                    TotalTimeMs = result.ElapsedTimeMs,
                    AverageTimePerFileMs = result.ElapsedTimeMs > 0 ? result.ElapsedTimeMs / fileList.Count : 0
                });

                _logger.LogInformation($"异步批量处理完成，成功: {result.SuccessCount}，失败: {result.Errors.Count}");
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("异步批量处理被取消");
                result.IsCanceled = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "异步批量处理过程中发生异常");
                throw;
            }
        }

        /// <summary>
        /// 异步处理单个文件
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>处理是否成功</returns>
        private async Task<bool> ProcessSingleFileAsync(FileRenameInfo fileInfo, string exportPath, bool isCopyMode, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(fileInfo.FullPath))
            {
                _logger.LogWarning($"文件不存在: {fileInfo.FullPath}");
                return false;
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // 处理正则表达式匹配
                string regexPart = string.Empty;
                if (!string.IsNullOrEmpty(_currentRegexPattern))
                {
                    var regexMatch = Regex.Match(Path.GetFileNameWithoutExtension(fileInfo.OriginalName), _currentRegexPattern);
                    if (regexMatch.Success)
                    {
                        regexPart = regexMatch.Groups.Count > 1 ? regexMatch.Groups[1].Value : regexMatch.Value;
                        fileInfo.RegexResult = regexPart;
                    }
                }

                // 重新构建文件名
                string newFileName = RebuildFileName(fileInfo, regexPart);
                fileInfo.NewName = newFileName;

                // 恢复保留字段（如果有）
                if (fileInfo.IsPreserveMode)
                {
                    RestoreBackupDataForSingleFile(fileInfo);
                }

                // 使用异步重命名方法
                return await _fileRenameService.RenameFileImmediatelyAsync(fileInfo, exportPath, isCopyMode, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"文件处理被取消: {fileInfo.OriginalName}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"处理文件 {fileInfo.OriginalName} 时出错");
                return false;
            }
        }

        /// <summary>
        /// 取消批量处理
        /// </summary>
        public void CancelProcessing()
        {
            _cancellationRequested = true;
            _logger.LogInformation("批量处理已取消");
        }

        /// <summary>
        /// 处理单个批次的文件
        /// </summary>
        /// <param name="batchFiles">批次文件列表</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <param name="parallelOptions">并行选项</param>
        /// <returns>批次处理结果</returns>
        private async Task<BatchResult> ProcessBatchAsync(List<FileRenameInfo> batchFiles, 
                                                         string exportPath, 
                                                         bool isCopyMode, 
                                                         ParallelOptions parallelOptions)
        {
            int successCount = 0;
            int failedCount = 0;
            var failedFiles = new ConcurrentBag<FailedFileInfo>();

            try
            {
                // 使用并行处理处理批次内的文件
                await Task.Run(() =>
                {
                    Parallel.ForEach(batchFiles, parallelOptions, (fileInfo, loopState) =>
                    {
                        if (_cancellationRequested)
                        {
                            loopState.Stop();
                            return;
                        }

                        try
                        {
                            // 处理单个文件
                            bool success = ProcessSingleFile(fileInfo, exportPath, isCopyMode);
                            
                            if (success)
                            {
                                Interlocked.Increment(ref successCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref failedCount);
                                failedFiles.Add(new FailedFileInfo
                                {
                                    FilePath = fileInfo.FullPath,
                                    ErrorMessage = "文件处理失败"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref failedCount);
                            failedFiles.Add(new FailedFileInfo
                            {
                                FilePath = fileInfo.FullPath,
                                ErrorMessage = ex.Message
                            });
                            _logger.LogError(ex, $"处理文件 {fileInfo.OriginalName} 时出错");
                        }
                        finally
                        {
                            // 更新进度
                            int current = Interlocked.Increment(ref _currentProgress);
                            double progressPercentage = (double)current / _totalFiles * 100;
                            
                            // 定期触发进度事件，避免过多事件触发
                            if (current % Math.Max(1, _totalFiles / 100) == 0 || current == _totalFiles)
                            {
                                // 创建Models命名空间中的BatchProgressEventArgs实例，使用构造函数
                                OnProgressChanged(new BatchProgressEventArgs(current, _totalFiles));
                            }
                        }
                    });
                });

                return new BatchResult
                {
                    SuccessCount = successCount,
                    FailedCount = failedCount,
                    FailedFiles = failedFiles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理批次时发生异常");
                
                return new BatchResult
                {
                    SuccessCount = successCount,
                    FailedCount = failedCount + (batchFiles.Count - successCount - failedCount),
                    FailedFiles = failedFiles
                };
            }
        }

        /// <summary>
        /// 处理单个文件
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <returns>处理是否成功</returns>
        private bool ProcessSingleFile(FileRenameInfo fileInfo, string exportPath, bool isCopyMode)
        {
            if (!File.Exists(fileInfo.FullPath))
            {
                _logger.LogWarning($"文件不存在: {fileInfo.FullPath}");
                return false;
            }

            try
            {
                // 检查是否需要添加图层
                bool isPdfFile = Path.GetExtension(fileInfo.FullPath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
                bool needAddLayer = isPdfFile && !string.IsNullOrEmpty(fileInfo.Material); // 使用Material作为图层判断条件

                string tempFilePath = null;
                string finalFilePath = null;

                try
                {
                    // 如果是PDF且需要添加图层，先处理图层
                    if (needAddLayer)
                    {
                        // 创建临时文件路径
                        tempFilePath = Path.Combine(Path.GetTempPath(), $"temp_{Guid.NewGuid()}.pdf");
                        
                        // 复制原始文件到临时文件
                        File.Copy(fileInfo.FullPath, tempFilePath, true);
                        
                        // 创建图层信息
                        var layerInfo = new PdfLayerInfo
                        {
                            LayerName = "BatchProcess",
                            Content = fileInfo.Material, // 使用Material作为图层内容
                            X = 100, // 默认位置
                            Y = 100, // 默认位置
                            FontSize = 12
                        };
                        
                        // 添加图层
                        bool layerAdded = _pdfProcessingService.AddLayerToPdf(tempFilePath, tempFilePath, layerInfo);
                        
                        if (!layerAdded)
                        {
                            _logger.LogWarning($"添加图层失败: {fileInfo.OriginalName}");
                            return false;
                        }
                        
                        // 使用处理后的临时文件进行重命名
                        fileInfo.FullPath = tempFilePath;
                    }

                    // 处理正则表达式匹配（如果启用）
                    string regexPart = string.Empty;
                    if (!string.IsNullOrEmpty(_currentRegexPattern))
                    {
                        // 添加调试信息
                        LogHelper.Debug("批量处理中使用的正则表达式模式: '" + _currentRegexPattern + "'");
                        LogHelper.Debug("批量处理中处理的文件名: '" + fileInfo.OriginalName + "'");
                        
                        var regexMatch = Regex.Match(Path.GetFileNameWithoutExtension(fileInfo.OriginalName), _currentRegexPattern);
                        if (regexMatch.Success)
                        {
                            // 获取匹配结果
                            if (regexMatch.Groups.Count > 1)
                            {
                                regexPart = regexMatch.Groups[1].Value; // 使用第一个捕获组
                            }
                            else
                            {
                                regexPart = regexMatch.Value; // 使用完整匹配
                            }
                            
                            // 更新FileRenameInfo中的正则结果
                            fileInfo.RegexResult = regexPart;
                            
                            // 添加调试信息
                            LogHelper.Debug("批量处理中正则匹配成功: '" + regexPart + "'");
                        }
                        else
                        {
                            // 添加调试信息
                        LogHelper.Debug("批量处理中正则匹配失败");
                        }
                    }
                    else
                    {
                        // 添加调试信息
                        LogHelper.Debug("批量处理中正则表达式模式为空");
                    }

                    // 处理形状参数和尺寸计算
                    string finalDimensions = string.Empty;
                    string cornerRadius = "0";
                    bool addPdfLayers = false;
                    // 批量模式下使用设置中的出血值，如果没有设置则默认为2
                    double tetBleed = 2; // 默认出血值
                    try
                    {
                        string bleedValues = AppSettings.Get("TetBleedValues")?.ToString() ?? "2";
                        string[] bleedParts = bleedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (bleedParts.Length > 0 && double.TryParse(bleedParts[0].Trim(), out double bleedValue))
                        {
                            tetBleed = bleedValue;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning($"加载出血值设置失败: {ex.Message}，使用默认值2");
                    }
                    
                    // 从文件信息中获取形状参数（如果存在）
                    if (!string.IsNullOrEmpty(fileInfo.Dimensions))
                    {
                        // 解析尺寸信息以提取形状参数
                        finalDimensions = fileInfo.Dimensions;
                        
                        // 尝试从尺寸字符串中提取形状信息
                        if (finalDimensions.Contains("R") || finalDimensions.Contains("Y") || finalDimensions.Contains("Z"))
                        {
                            addPdfLayers = true;
                            // 提取圆角半径信息
                            var match = System.Text.RegularExpressions.Regex.Match(finalDimensions, @"(\d+)x(\d+(?:\.\d+)?)R(\d+)");
                            if (match.Success)
                            {
                                cornerRadius = match.Groups[3].Value;
                            }
                            else
                            {
                                // 检查是否包含R或Y
                                if (finalDimensions.Contains("R"))
                                {
                                    cornerRadius = "R";
                                }
                                else if (finalDimensions.Contains("Y"))
                                {
                                    cornerRadius = "Y";
                                }
                                else if (finalDimensions.Contains("Z"))
                                {
                                    cornerRadius = "0";
                                }
                            }
                        }
                    }

                    // 重新构建文件名，包含正则表达式结果和形状信息
                    string newFileName = RebuildFileNameWithShapes(fileInfo, regexPart, cornerRadius, addPdfLayers, tetBleed);
                    fileInfo.NewName = newFileName;

                    // 确保正则结果也更新到FileRenameInfo对象中
                    if (!string.IsNullOrEmpty(fileInfo.RegexResult))
                    {
                        regexPart = fileInfo.RegexResult;
                    }

                    // 恢复保留字段（如果有）
                    if (fileInfo.IsPreserveMode)
                    {
                        RestoreBackupDataForSingleFile(fileInfo);
                    }

                    // 添加调试信息
                    LogHelper.Debug("批量处理中构建的新文件名: '" + newFileName + "'");

                    bool renameSuccess = _fileRenameService.RenameFileImmediately(fileInfo, exportPath, false);
                    
                    // 确保在重命名成功后，FileRenameInfo对象的状态正确更新
                    if (renameSuccess)
                    {
                        finalFilePath = Path.Combine(exportPath, newFileName);
                        _logger.LogInformation($"文件处理成功: {Path.GetFileName(finalFilePath)}");
                        // 确保正则结果在重命名后仍然保持
                        fileInfo.RegexResult = regexPart;
                    }
                    else
                    {
                        _logger.LogWarning($"文件重命名失败: {fileInfo.OriginalName}");
                    }

                    return renameSuccess;
                }
                finally
                {
                    // 清理临时文件
                    if (!string.IsNullOrEmpty(tempFilePath) && File.Exists(tempFilePath))
                    {
                        try
                        {
                            // 只有当临时文件不是最终文件路径时才删除
                            if (string.IsNullOrEmpty(finalFilePath) || !string.Equals(tempFilePath, finalFilePath, StringComparison.OrdinalIgnoreCase))
                            {
                                File.Delete(tempFilePath);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"删除临时文件失败: {tempFilePath}");
                        }
                    }
                }
            }
            catch
            {
                // 对于重命名操作的异常，我们让它传播出去，这样ProcessBatchAsync可以捕获并使用异常消息作为ErrorMessage
                // 注意：不在这里记录日志，因为ProcessBatchAsync会捕获并记录
                throw;
            }
        }

        /// <summary>
        /// 重新构建文件名（包含形状参数）
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="regexPart">正则表达式匹配结果</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="addPdfLayers">是否添加PDF图层</param>
        /// <param name="tetBleed">出血值</param>
        /// <returns>构建的新文件名</returns>
        private string RebuildFileNameWithShapes(FileRenameInfo fileInfo, string regexPart, string cornerRadius, bool addPdfLayers, double tetBleed)
        {
            try
            {
                var newNameParts = new List<string>();
                string unit = AppSettings.Unit ?? "";

                // 优先使用新的分组配置系统
                var lastSelectedPreset = AppSettings.Get("LastSelectedEventPreset") as string;

                if (!string.IsNullOrEmpty(lastSelectedPreset))
                {
                    // 尝试从CustomSettings获取当前预设的配置
                    string presetConfigKey = $"EventItemsPreset_{lastSelectedPreset}";
                    var eventGroupConfigJson = AppSettings.Get(presetConfigKey) as string;

                    if (!string.IsNullOrEmpty(eventGroupConfigJson))
                    {
                        // 使用新的分组配置系统
                        LogHelper.Debug($"使用分组配置系统构建文件名，预设: {lastSelectedPreset}");

                        try
                        {
                            var groupConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<WindowsFormsApp3.Models.EventGroupConfiguration>(eventGroupConfigJson);
                            if (groupConfig != null)
                            {
                                var enabledItems = groupConfig.GetAllEnabledItems();
                                LogHelper.Debug($"获取到 {enabledItems.Count} 个启用的事件项");

                                foreach (var item in enabledItems)
                                {
                                    string prefix = groupConfig.GetPrefixForItem(item.Name);
                                    string value = GetItemValue(fileInfo, item.Name, regexPart, cornerRadius, addPdfLayers, tetBleed, unit);

                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        // 添加分组前缀（如果有）
                                        if (!string.IsNullOrEmpty(prefix))
                                        {
                                            newNameParts.Add(prefix + value);
                                            LogHelper.Debug($"添加分组项到文件名: '{prefix}{value}' (项目: {item.Name})");
                                        }
                                        else
                                        {
                                            newNameParts.Add(value);
                                            LogHelper.Debug($"添加未分组项到文件名: '{value}' (项目: {item.Name})");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Debug($"解析分组配置失败，回退到旧系统: {ex.Message}");
                            // 回退到旧的EventItems系统
                            BuildFileNameFromOldEventItems(fileInfo, regexPart, cornerRadius, addPdfLayers, tetBleed, unit, newNameParts);
                        }
                    }
                    else
                    {
                        LogHelper.Debug($"未找到预设 {lastSelectedPreset} 的配置，回退到旧系统");
                        // 回退到旧的EventItems系统
                        BuildFileNameFromOldEventItems(fileInfo, regexPart, cornerRadius, addPdfLayers, tetBleed, unit, newNameParts);
                    }
                }
                else
                {
                    // 使用旧的EventItems系统
                    LogHelper.Debug("使用旧EventItems系统构建文件名");
                    BuildFileNameFromOldEventItems(fileInfo, regexPart, cornerRadius, addPdfLayers, tetBleed, unit, newNameParts);
                }

                // 获取用户设置的间隔符号，支持空分隔符
                string separator = AppSettings.Separator ?? "";
                // 只在分隔符包含非法字符时才替换为默认值，允许空分隔符
                if (!string.IsNullOrEmpty(separator) && Path.GetInvalidFileNameChars().Contains(separator[0]))
                {
                    separator = "_";
                }

                // 构建新文件名
                string newFileName = string.Join(separator, newNameParts) + Path.GetExtension(fileInfo.FullPath);
                // 添加调试信息
                LogHelper.Debug($"构建的新文件名: '{newFileName}'");
                return newFileName;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"重新构建文件名时发生错误: {ex.Message}");
                return $"未命名{Path.GetExtension(fileInfo.FullPath)}";
            }
        }

        /// <summary>
        /// 获取事件项的值
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="itemName">事件项名称</param>
        /// <param name="regexPart">正则表达式结果</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="addPdfLayers">是否添加PDF图层</param>
        /// <param name="tetBleed">出血值</param>
        /// <param name="unit">单位</param>
        /// <returns>事件项的值</returns>
        private string GetItemValue(FileRenameInfo fileInfo, string itemName, string regexPart, string cornerRadius, bool addPdfLayers, double tetBleed, string unit)
        {
            switch (itemName)
            {
                case "正则结果":
                    return !string.IsNullOrEmpty(regexPart) ? regexPart : string.Empty;

                case "订单号":
                    return !string.IsNullOrEmpty(fileInfo.OrderNumber) ? fileInfo.OrderNumber : string.Empty;

                case "材料":
                    return !string.IsNullOrEmpty(fileInfo.Material) ? fileInfo.Material : string.Empty;

                case "数量":
                    if (!string.IsNullOrEmpty(fileInfo.Quantity))
                    {
                        string quantityWithUnit = fileInfo.Quantity;
                        if (!string.IsNullOrEmpty(unit))
                            quantityWithUnit += unit;
                        return quantityWithUnit;
                    }
                    return string.Empty;

                case "尺寸":
                    return ProcessDimensions(fileInfo.Dimensions, cornerRadius, addPdfLayers, tetBleed);

                case "工艺":
                    return !string.IsNullOrEmpty(fileInfo.Process) ? fileInfo.Process : string.Empty;

                case "序号":
                    return !string.IsNullOrEmpty(fileInfo.SerialNumber) ? fileInfo.SerialNumber : string.Empty;

                case "行数":
                    // 暂时返回空字符串，因为FileRenameInfo没有RowCount属性
                    return string.Empty;

                case "列数":
                    // 暂时返回空字符串，因为FileRenameInfo没有ColumnCount属性
                    return string.Empty;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 处理尺寸信息，支持形状参数
        /// </summary>
        /// <param name="dimensions">原始尺寸</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="addPdfLayers">是否添加PDF图层</param>
        /// <param name="tetBleed">出血值</param>
        /// <returns>处理后的尺寸</returns>
        private string ProcessDimensions(string dimensions, string cornerRadius, bool addPdfLayers, double tetBleed)
        {
            if (string.IsNullOrEmpty(dimensions))
                return string.Empty;

            // 如果启用了添加PDF图层，确保尺寸包含形状信息
            if (addPdfLayers && !string.IsNullOrEmpty(cornerRadius))
            {
                // 使用PdfTools计算最终尺寸
                if (dimensions.Contains("x"))
                {
                    var parts = dimensions.Split('x');
                    if (parts.Length >= 2)
                    {
                        // 提取宽度和高度
                        string widthStr = parts[0];
                        string heightStr = parts[1];

                        // 移除可能的形状代号
                        heightStr = System.Text.RegularExpressions.Regex.Replace(heightStr, @"[A-Za-z].*$", "");

                        // 转换为double
                        if (double.TryParse(widthStr, out double width) &&
                            double.TryParse(heightStr, out double height))
                        {
                            // 使用PdfTools计算最终尺寸
                            return PdfTools.CalculateFinalDimensions(width, height, tetBleed, cornerRadius, addPdfLayers);
                        }
                    }
                }
            }

            return dimensions;
        }

        /// <summary>
        /// 使用旧的EventItems系统构建文件名
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="regexPart">正则表达式结果</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="addPdfLayers">是否添加PDF图层</param>
        /// <param name="tetBleed">出血值</param>
        /// <param name="unit">单位</param>
        /// <param name="newNameParts">文件名部分列表</param>
        private void BuildFileNameFromOldEventItems(FileRenameInfo fileInfo, string regexPart, string cornerRadius, bool addPdfLayers, double tetBleed, string unit, List<string> newNameParts)
        {
            // 获取事件项配置
            string eventItemsStr = AppSettings.EventItems ?? string.Empty;
            var eventItems = eventItemsStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 检查哪些项被勾选
            for (int i = 0; i < eventItems.Length; i += 2)
            {
                if (i + 1 < eventItems.Length)
                {
                    string itemText = eventItems[i];
                    bool isChecked = bool.Parse(eventItems[i + 1]);
                    if (isChecked)
                    {
                        string value = GetItemValue(fileInfo, itemText, regexPart, cornerRadius, addPdfLayers, tetBleed, unit);
                        if (!string.IsNullOrEmpty(value))
                        {
                            newNameParts.Add(value);
                            LogHelper.Debug($"添加旧系统事件项到文件名: '{value}' (项目: {itemText})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取简单重命名的事件项值
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="itemName">事件项名称</param>
        /// <param name="regexPart">正则表达式结果</param>
        /// <param name="unit">单位</param>
        /// <returns>事件项的值</returns>
        private string GetItemValueForSimpleRename(FileRenameInfo fileInfo, string itemName, string regexPart, string unit)
        {
            switch (itemName)
            {
                case "正则结果":
                    return !string.IsNullOrEmpty(regexPart) ? regexPart : string.Empty;

                case "订单号":
                    return !string.IsNullOrEmpty(fileInfo.OrderNumber) ? fileInfo.OrderNumber : string.Empty;

                case "材料":
                    return !string.IsNullOrEmpty(fileInfo.Material) ? fileInfo.Material : string.Empty;

                case "数量":
                    if (!string.IsNullOrEmpty(fileInfo.Quantity))
                    {
                        string quantityWithUnit = fileInfo.Quantity;
                        if (!string.IsNullOrEmpty(unit))
                            quantityWithUnit += unit;
                        return quantityWithUnit;
                    }
                    return string.Empty;

                case "尺寸":
                    return !string.IsNullOrEmpty(fileInfo.Dimensions) ? fileInfo.Dimensions : string.Empty;

                case "工艺":
                    return !string.IsNullOrEmpty(fileInfo.Process) ? fileInfo.Process : string.Empty;

                case "序号":
                    return !string.IsNullOrEmpty(fileInfo.SerialNumber) ? fileInfo.SerialNumber : string.Empty;

                case "行数":
                    // 暂时返回空字符串，因为FileRenameInfo没有RowCount属性
                    return string.Empty;

                case "列数":
                    // 暂时返回空字符串，因为FileRenameInfo没有ColumnCount属性
                    return string.Empty;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 使用旧的EventItems系统构建简单文件名
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="regexPart">正则表达式结果</param>
        /// <param name="unit">单位</param>
        /// <param name="newNameParts">文件名部分列表</param>
        private void BuildFileNameFromOldEventItemsSimple(FileRenameInfo fileInfo, string regexPart, string unit, List<string> newNameParts)
        {
            // 获取事件项配置
            string eventItemsStr = AppSettings.EventItems ?? string.Empty;
            var eventItems = eventItemsStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            // 检查哪些项被勾选
            for (int i = 0; i < eventItems.Length; i += 2)
            {
                if (i + 1 < eventItems.Length)
                {
                    string itemText = eventItems[i];
                    bool isChecked = bool.Parse(eventItems[i + 1]);
                    if (isChecked)
                    {
                        string value = GetItemValueForSimpleRename(fileInfo, itemText, regexPart, unit);
                        if (!string.IsNullOrEmpty(value))
                        {
                            newNameParts.Add(value);
                            LogHelper.Debug($"添加旧系统事件项到简单文件名: '{value}' (项目: {itemText})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 将列表拆分为多个批次
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <param name="items">要拆分的列表</param>
        /// <param name="batchSize">批次大小</param>
        /// <returns>批次列表</returns>
        private IEnumerable<IEnumerable<T>> SplitIntoBatches<T>(IEnumerable<T> items, int batchSize)
        {
            var batch = new List<T>(batchSize);
            foreach (var item in items)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }
            
            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        /// <summary>
        /// 重新构建文件名
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="regexPart">正则表达式匹配结果</param>
        /// <returns>构建的新文件名</returns>
        private string RebuildFileName(FileRenameInfo fileInfo, string regexPart)
        {
            try
            {
                var newNameParts = new List<string>();
                string unit = AppSettings.Unit ?? "";

                // 优先使用新的分组配置系统
                var lastSelectedPreset = AppSettings.Get("LastSelectedEventPreset") as string;

                if (!string.IsNullOrEmpty(lastSelectedPreset))
                {
                    // 尝试从CustomSettings获取当前预设的配置
                    string presetConfigKey = $"EventItemsPreset_{lastSelectedPreset}";
                    var eventGroupConfigJson = AppSettings.Get(presetConfigKey) as string;

                    if (!string.IsNullOrEmpty(eventGroupConfigJson))
                    {
                        // 使用新的分组配置系统
                        LogHelper.Debug($"使用分组配置系统构建简单文件名，预设: {lastSelectedPreset}");

                        try
                        {
                            var groupConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<WindowsFormsApp3.Models.EventGroupConfiguration>(eventGroupConfigJson);
                            if (groupConfig != null)
                            {
                                var enabledItems = groupConfig.GetAllEnabledItems();
                                LogHelper.Debug($"获取到 {enabledItems.Count} 个启用的事件项");

                                foreach (var item in enabledItems)
                                {
                                    string prefix = groupConfig.GetPrefixForItem(item.Name);
                                    string value = GetItemValueForSimpleRename(fileInfo, item.Name, regexPart, unit);

                                    if (!string.IsNullOrEmpty(value))
                                    {
                                        // 添加分组前缀（如果有）
                                        if (!string.IsNullOrEmpty(prefix))
                                        {
                                            newNameParts.Add(prefix + value);
                                            LogHelper.Debug($"添加分组项到简单文件名: '{prefix}{value}' (项目: {item.Name})");
                                        }
                                        else
                                        {
                                            newNameParts.Add(value);
                                            LogHelper.Debug($"添加未分组项到简单文件名: '{value}' (项目: {item.Name})");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Debug($"解析分组配置失败，回退到旧系统: {ex.Message}");
                            // 回退到旧的EventItems系统
                            BuildFileNameFromOldEventItemsSimple(fileInfo, regexPart, unit, newNameParts);
                        }
                    }
                    else
                    {
                        LogHelper.Debug($"未找到预设 {lastSelectedPreset} 的配置，回退到旧系统");
                        // 回退到旧的EventItems系统
                        BuildFileNameFromOldEventItemsSimple(fileInfo, regexPart, unit, newNameParts);
                    }
                }
                else
                {
                    // 使用旧的EventItems系统
                    LogHelper.Debug("使用旧EventItems系统构建简单文件名");
                    BuildFileNameFromOldEventItemsSimple(fileInfo, regexPart, unit, newNameParts);
                }

                // 获取用户设置的间隔符号，支持空分隔符
                string separator = AppSettings.Separator ?? "";
                // 只在分隔符包含非法字符时才替换为默认值，允许空分隔符
                if (!string.IsNullOrEmpty(separator) && Path.GetInvalidFileNameChars().Contains(separator[0]))
                {
                    separator = "_";
                }

                // 构建新文件名
                string newFileName = string.Join(separator, newNameParts) + Path.GetExtension(fileInfo.FullPath);
                // 添加调试信息
                LogHelper.Debug($"构建的新文件名: '{newFileName}'");
                return newFileName;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"重新构建文件名时发生错误: {ex.Message}");
                return $"未命名{Path.GetExtension(fileInfo.FullPath)}";
            }
        }

        /// <summary>
        /// 触发进度变更事件
        /// </summary>
        /// <param name="e">进度事件参数</param>
        protected virtual void OnProgressChanged(WindowsFormsApp3.Models.BatchProgressEventArgs e)
        {
            ProgressChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 触发处理完成事件
        /// </summary>
        /// <param name="e">完成事件参数</param>
        protected virtual void OnProcessingComplete(BatchCompleteEventArgs e)
        {
            ProcessingComplete?.Invoke(this, e);
        }
    }

    // 使用Models命名空间中的BatchProgressEventArgs类

    /// <summary>
    /// 批次处理结果
    /// </summary>
    internal class BatchResult
    {
        /// <summary>
        /// 成功处理的文件数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 处理失败的文件数
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 失败的文件列表
        /// </summary>
        public ConcurrentBag<FailedFileInfo> FailedFiles { get; set; }
    }

    /// <summary>
    /// 失败文件信息
    /// </summary>
    internal class FailedFileInfo
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }
    }
}