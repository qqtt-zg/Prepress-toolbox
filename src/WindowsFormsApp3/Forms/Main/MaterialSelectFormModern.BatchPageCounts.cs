using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3
{
    public partial class MaterialSelectFormModern
    {
        private const int MaxBatchPageCountRetries = 3;
        private readonly Dictionary<BatchFileItem, int> _batchPageCountRetryAttempts = new Dictionary<BatchFileItem, int>();
        private Timer _batchPageCountRetryTimer;
        private bool _isBatchPageCountRetrying;
        private bool _batchPageCountRetriesStopped;

        private List<BatchFileItem> GetMissingBatchPageCountItems()
        {
            return _batchItems.Where(item => item.PageCount.GetValueOrDefault() <= 0 &&
                !string.IsNullOrWhiteSpace(item.FilePath) &&
                string.Equals(Path.GetExtension(item.FilePath), ".pdf", StringComparison.OrdinalIgnoreCase) &&
                (!_batchPageCountRetryAttempts.TryGetValue(item, out int attempts) || attempts < MaxBatchPageCountRetries)).ToList();
        }

        private void ScheduleMissingBatchPageCounts()
        {
            if (IsDisposed || Disposing || _batchPageCountRetriesStopped || !IsHandleCreated) return;
            foreach (var removed in _batchPageCountRetryAttempts.Keys.Where(item => !_batchItems.Contains(item)).ToList())
                _batchPageCountRetryAttempts.Remove(removed);

            if (GetMissingBatchPageCountItems().Count == 0)
            {
                _batchPageCountRetryTimer?.Stop();
                return;
            }
            if (_batchPageCountRetryTimer == null)
            {
                // 由窗体组件容器释放计时器；每次后台读取结束后再安排下一轮，避免并发重入。
                components ??= new System.ComponentModel.Container();
                _batchPageCountRetryTimer = new Timer(components) { Interval = 1000 };
                _batchPageCountRetryTimer.Tick += async (sender, e) => await RetryMissingBatchPageCountsAsync();
            }
            if (!_isBatchPageCountRetrying) _batchPageCountRetryTimer.Start();
        }

        internal async Task RetryMissingBatchPageCountsAsync(Func<string, int?> readPageCount = null)
        {
            if (IsDisposed || Disposing || _batchPageCountRetriesStopped || _isBatchPageCountRetrying) return;
            _batchPageCountRetryTimer?.Stop();
            _isBatchPageCountRetrying = true;
            try
            {
                var groups = GetMissingBatchPageCountItems()
                    .GroupBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
                foreach (var group in groups)
                {
                    foreach (var item in group)
                    {
                        _batchPageCountRetryAttempts.TryGetValue(item, out int attempts);
                        _batchPageCountRetryAttempts[item] = attempts + 1;
                    }
                }

                var reader = readPageCount ?? ResolveActualPageCount;
                var results = await Task.Run(() => groups.Select(group =>
                {
                    try
                    {
                        return reader(group.Key);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"后台补读 PDF 页数失败: {group.Key}", ex);
                        return (int?)null;
                    }
                }).ToList());
                if (IsDisposed || Disposing || _batchPageCountRetriesStopped) return;
                for (int index = 0; index < groups.Count; index++)
                {
                    if (results[index].GetValueOrDefault() <= 0) continue;
                    foreach (var item in groups[index]) ApplyMissingBatchPageCount(item, results[index].Value);
                }
                // 文件由未就绪变为可读后，同步补齐尺寸和页面框，不能仅恢复页数。
                var readyItems = groups.SelectMany(group => group).Where(item => _batchItems.Contains(item) &&
                    item.PageCount.GetValueOrDefault() > 0 && !item.PageBoxSource.HasValue).ToList();
                if (readyItems.Count > 0) await LoadBatchPageBoxChoicesAsync(readyItems);
            }
            finally
            {
                _isBatchPageCountRetrying = false;
                ScheduleMissingBatchPageCounts();
            }
        }

        internal void CompleteMissingBatchPageCounts(string filePath, int pageCount)
        {
            if (IsDisposed || Disposing || _batchPageCountRetriesStopped || pageCount <= 0) return;
            foreach (var item in _batchItems.Where(item =>
                string.Equals(item.FilePath, filePath, StringComparison.OrdinalIgnoreCase)).ToList())
                ApplyMissingBatchPageCount(item, pageCount);
        }

        private void ApplyMissingBatchPageCount(BatchFileItem item, int pageCount)
        {
            // 已移除的行和已由预览补全的行不再回写，避免异步结果覆盖较新的有效值。
            if (!_batchItems.Contains(item) || item.PageCount.GetValueOrDefault() > 0) return;
            item.PageCount = pageCount;
            _batchPageCountRetryAttempts.Remove(item);
            var grids = new List<DataGridView>();
            if (dgvBatchFiles != null) grids.Add(dgvBatchFiles);
            if (pnlCardsContainer != null)
                grids.AddRange(pnlCardsContainer.Controls.Cast<Control>()
                    .SelectMany(card => card.Controls.Cast<Control>()).OfType<DataGridView>());
            foreach (var grid in grids.Where(grid => !grid.IsDisposed))
            {
                var column = grid.Columns.Cast<DataGridViewColumn>().FirstOrDefault(candidate =>
                    candidate.DataPropertyName == nameof(BatchFileItem.PageCount));
                if (column == null) continue;
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (ReferenceEquals(row.DataBoundItem, item)) grid.InvalidateCell(column.Index, row.Index);
                }
            }
        }
    }
}
