using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3
{
    public partial class MaterialSelectFormModern
    {
        private sealed class BatchPdfMetadata
        {
            public PdfInspectorInfo Inspection;
            public double Width;
            public double Height;
        }

        private readonly SemaphoreSlim _batchInspectionSlots = new SemaphoreSlim(2);
        private readonly Dictionary<string, Task<BatchPdfMetadata>> _batchPageBoxInspections =
            new Dictionary<string, Task<BatchPdfMetadata>>(StringComparer.OrdinalIgnoreCase);
        private BatchFileItem _activeBatchPageBoxItem;
        // 仅由独立预览入口设置，正式监控默认不增加任何延迟。
        internal int PreviewMetadataDelayMilliseconds { get; set; }

        private async Task<BatchPdfMetadata> ReadBatchPdfMetadataAsync(string path)
        {
            await _batchInspectionSlots.WaitAsync();
            try
            {
                if (IsDisposed || Disposing || _batchPageCountRetriesStopped) return null;
                if (PreviewMetadataDelayMilliseconds > 0)
                {
                    await Task.Delay(PreviewMetadataDelayMilliseconds);
                    if (IsDisposed || Disposing || _batchPageCountRetriesStopped) return null;
                }
                return await Task.Run(() =>
                {
                    var metadata = new BatchPdfMetadata { Inspection = new PdfInspectorService().InspectPdf(path) };
                    IText7PdfTools.GetFirstPageSize(path, out metadata.Width, out metadata.Height);
                    return metadata;
                });
            }
            finally
            {
                _batchInspectionSlots.Release();
            }
        }

        /// <summary>在后台逐文件检查页面框，同一路径复用读取任务，避免阻塞列表交互。</summary>
        internal async Task LoadBatchPageBoxChoicesAsync(IEnumerable<BatchFileItem> items, bool refresh = false)
        {
            if (IsDisposed || Disposing) return;
            var groups = (items ?? Enumerable.Empty<BatchFileItem>())
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.FilePath) &&
                    (refresh || !item.PageBoxSource.HasValue))
                .GroupBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
            if (groups.Count == 0) return;
            BeginBatchMetadataProgress(groups.Select(group => group.Key));
            try
            {
                int completed = 0;
                foreach (var group in groups)
                {
                    string path = group.Key;
                    if (!string.Equals(System.IO.Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var item in group) item.PageBoxSource = PdfPageBoxSource.Automatic;
                        continue;
                    }
                    if (refresh) _batchPageBoxInspections.Remove(path);
                    if (!_batchPageBoxInspections.TryGetValue(path, out var task))
                    {
                        task = ReadBatchPdfMetadataAsync(path);
                        _batchPageBoxInspections[path] = task;
                    }
                    BatchPdfMetadata metadata;
                    try
                    {
                        metadata = await task;
                    }
                    catch (Exception ex)
                    {
                        if (_batchPageBoxInspections.TryGetValue(path, out var failedTask) && ReferenceEquals(task, failedTask))
                        {
                            _batchPageBoxInspections.Remove(path);
                            CompleteBatchMetadataProgress(path, false);
                        }
                        LogHelper.Error($"读取批量文件信息失败: {path}", ex);
                        continue;
                    }
                    if (IsDisposed || Disposing || _batchPageCountRetriesStopped) return;
                    if (!_batchPageBoxInspections.TryGetValue(path, out var latest) || !ReferenceEquals(task, latest)) continue;
                    if (metadata?.Inspection == null || metadata.Inspection.TotalPages <= 0)
                    {
                        _batchPageBoxInspections.Remove(path);
                        CompleteBatchMetadataProgress(path, false);
                        continue;
                    }
                    var inspection = metadata.Inspection;
                    foreach (var item in group.Where(item => _batchItems.Contains(item)))
                    {
                        if (metadata.Width > 0 && metadata.Height > 0 && !item.IsPreserveJob)
                        {
                            item.RawPdfWidth = metadata.Width;
                            item.RawPdfHeight = metadata.Height;
                            if (AppSettings.SwapWidthHeightForDisplay && item.RawPdfWidth < item.RawPdfHeight)
                            {
                                item.RawPdfWidth = metadata.Height;
                                item.RawPdfHeight = metadata.Width;
                            }
                            item.Dimensions = ServiceLocator.Instance.GetDimensionCalculationService().CalculateFinalDimensions(
                                item.RawPdfWidth, item.RawPdfHeight, SelectedTetBleed, GetCompatibleCornerRadius(), GetIsShapeSelected());
                        }
                        CompleteMissingBatchPageCounts(item.FilePath, inspection.TotalPages);
                        BatchPageBoxSelectionService.ApplyInspection(item, inspection);
                        if (item.PageBoxChoicePage != null) UpdateBatchPageBoxDimensions(item);
                    }
                    var active = _activeBatchPageBoxItem ?? _batchItems.FirstOrDefault(item =>
                        string.Equals(item.FilePath, CurrentFileName, StringComparison.OrdinalIgnoreCase));
                    if (active != null && group.Contains(active))
                    {
                        ActivateBatchPageBoxItem(active);
                    }
                    // 逐个显示已完成结果，但不更换正在编辑的表格。
                    if (++completed == 1 || completed % 16 == 0) RenderGroupCards();
                    if (group.Any(item => _batchItems.Contains(item))) CompleteBatchMetadataProgress(path, true);
                }
                if (IsDisposed || Disposing) return;
                RenderGroupCards();
                UpdateBatchLayoutColumns();
            }
            catch (Exception ex)
            {
                LogHelper.Error("批量列表页面框检查失败", ex);
                foreach (var group in groups)
                {
                    if (_batchMetadataProgress.TryGetValue(group.Key, out var state) && !state.HasValue)
                        CompleteBatchMetadataProgress(group.Key, false);
                }
            }
        }

        private void UpdateBatchPageBoxDimensions(BatchFileItem item)
        {
            BatchPageBoxSelectionService.UpdateDimensions(item, ServiceLocator.Instance.GetDimensionCalculationService(),
                SelectedTetBleed, GetCompatibleCornerRadius(), GetIsShapeSelected(), AppSettings.SwapWidthHeightForDisplay);
        }

        /// <summary>列表每项独立切换，不改写其他项或分组的工艺参数。</summary>
        internal void SelectBatchPageBoxSource(BatchFileItem item, PdfPageBoxSource source)
        {
            if (item?.PageBoxChoicePage == null) return;
            if (item.PageBoxSource == source && ReferenceEquals(_activeBatchPageBoxItem, item)) return;
            bool restoreFocus = pnlCardsContainer?.ContainsFocus == true;
            try
            {
                BatchPageBoxSelectionService.SelectSource(item, source);
                UpdateBatchPageBoxDimensions(item);
                ActivateBatchPageBoxItem(item);
                QueueBatchFilePreview(item.FilePath);
                RefreshAllCardsData();
                UpdateBatchLayoutColumns();
                if (restoreFocus && pnlCardsContainer != null)
                {
                    foreach (var grid in pnlCardsContainer.Controls.Cast<Control>()
                        .SelectMany(card => card.Controls.Cast<Control>()).OfType<DataGridView>())
                    {
                        var row = grid.Rows.Cast<DataGridViewRow>().FirstOrDefault(candidate => ReferenceEquals(candidate.DataBoundItem, item));
                        var column = grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(candidate =>
                            candidate.DataPropertyName == nameof(BatchFileItem.Dimensions));
                        if (row == null || column == null) continue;
                        grid.CurrentCell = row.Cells[column.Index];
                        grid.Focus();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"选择文件页面框失败: {item.FilePath}", ex);
                AntdUI.Message.error(this, "页面框选择失败：" + ex.Message);
            }
        }

        private void ActivateBatchPageBoxItem(BatchFileItem item)
        {
            if (item == null) return;
            _activeBatchPageBoxItem = item;
            if (!item.PageBoxSource.HasValue)
            {
                // 用户选中的未解析文件使用另一读取槽位优先加载，无需等待整批完成。
                _ = LoadBatchPageBoxChoicesAsync(new[] { item });
                return;
            }
            var inspection = new PdfInspectorInfo();
            if (item.PageBoxChoicePage != null)
            {
                inspection.AllPageBoxes.Add(item.PageBoxChoicePage);
                inspection.Issues.Add(new PageBoxIssue
                {
                    PageNumber = item.PageBoxReferencePage,
                    Type = item.PageBoxIssueType ?? IssueType.DifferentPageBoxes,
                    BoxType = "CropBox"
                });
            }
            // 仅同步当前行到右侧，避免批量尺寸重算重建表格，打断数量编辑和单元格选区。
            UpdatePageBoxChoiceFromInspection(inspection, syncBatchItems: false);
            if (item.PageBoxChoicePage == null && item.RawPdfWidth > 0 && item.RawPdfHeight > 0)
            {
                _initialWidth = item.RawPdfWidth;
                _initialHeight = item.RawPdfHeight;
                UpdateDimensionsWithBleed(false);
                RefreshPageBoxLayout();
            }
            UpdateFormTitleWithFileName(System.IO.Path.GetFileNameWithoutExtension(item.FileName));
        }

        private void RestoreSingleFilePageBoxSelection()
        {
            var original = _batchItems.FirstOrDefault(item =>
                string.Equals(item.FilePath, CurrentFileName, StringComparison.OrdinalIgnoreCase));
            if (original != null) ActivateBatchPageBoxItem(original);
        }

        private void ConfigureBatchPageBoxRows(DataGridView grid, IList<BatchFileItem> items)
        {
            for (int index = 0; index < grid.Rows.Count && index < items.Count; index++)
            {
                var item = items[index];
                grid.Rows[index].Height = item.PageBoxChoicePage == null ? 26 : 46;
                if (item.PageBoxChoicePage == null) continue;
                string reason = item.PageBoxIssueType == IssueType.OutOfBounds ? "裁剪框超出媒体框" : "媒体框与裁剪框不一致";
                string details = $"第{item.PageBoxReferencePage}页 · {reason}\n" +
                    FormatPageBoxDetails("媒体框", item.PageBoxChoicePage.MediaBox) + "\n" +
                    FormatPageBoxDetails("裁剪框", item.PageBoxChoicePage.CropBox) +
                    "\n行内显示扣除当前出血后的成品尺寸；点击选用，空格键切换基准。";
                foreach (DataGridViewColumn column in grid.Columns)
                {
                    if (column.DataPropertyName == nameof(BatchFileItem.Dimensions) ||
                        column.DataPropertyName == nameof(BatchFileItem.FileName))
                        grid.Rows[index].Cells[column.Index].ToolTipText = details;
                }
            }
            grid.Height = grid.ColumnHeadersHeight + grid.Rows.Cast<DataGridViewRow>().Sum(row => row.Height) + 2;
            if (grid.Parent is Panel card)
            {
                var header = card.Controls.OfType<Forms.Controls.BatchGroupHeaderCard>().FirstOrDefault();
                if (header != null)
                {
                    // Visible 会受整个窗口或左侧面板隐藏影响，卡片高度应仅由该组折叠状态决定。
                    bool collapsed = (grid.Tag as BatchProcessGroup)?.IsCollapsed == true;
                    card.Height = header.Height + (collapsed ? 0 : grid.Height);
                    RelayoutCards();
                }
            }
        }
    }
}
