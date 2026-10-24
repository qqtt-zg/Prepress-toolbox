using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3
{
    public partial class MaterialSelectFormModern
    {
        private readonly HashSet<string> _pendingBatchArrivals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _pendingBatchArrivalOrder = new List<string>();
        private Timer _batchArrivalTimer;

        internal void QueuePendingBatchFile(string path)
        {
            if (IsDisposed || Disposing || string.IsNullOrWhiteSpace(path)) return;
            if (_pendingBatchArrivals.Add(path)) _pendingBatchArrivalOrder.Add(path);
            if (_batchArrivalTimer == null)
            {
                components ??= new Container();
                _batchArrivalTimer = new Timer(components) { Interval = 350 };
                _batchArrivalTimer.Tick += (sender, e) => FlushPendingBatchFiles();
            }
            // 不反复重置计时器，持续进入文件时仍定期提交。
            if (!_batchArrivalTimer.Enabled) _batchArrivalTimer.Start();
        }

        internal void FlushPendingBatchFiles()
        {
            _batchArrivalTimer?.Stop();
            if (IsDisposed || Disposing || _pendingBatchArrivalOrder.Count == 0) return;
            var paths = _pendingBatchArrivalOrder.ToArray();
            _pendingBatchArrivalOrder.Clear();
            _pendingBatchArrivals.Clear();
            AppendPendingFiles(paths);
        }

        private bool TryRefreshExistingBatchGroups()
        {
            var grids = pnlCardsContainer.Controls.Cast<Control>()
                .SelectMany(card => card.Controls.Cast<Control>()).OfType<DataGridView>().ToList();
            if (_processGroups == null || grids.Count == 0 || grids.Count != _processGroups.Count) return false;
            for (int index = 0; index < grids.Count; index++)
            {
                var group = _processGroups[index];
                if (!ReferenceEquals(grids[index].Tag, group) ||
                    !(grids[index].DataSource is BindingList<BatchFileItem> rows) || rows.Count > group.Items.Count)
                    return false;
                for (int row = 0; row < rows.Count; row++)
                    if (!ReferenceEquals(rows[row], group.Items[row])) return false;
            }
            // 列表结构未改变或仅在末尾追加时，保留原表格、编辑控件和滚动位置。
            foreach (var grid in grids)
            {
                var group = (BatchProcessGroup)grid.Tag;
                var rows = (BindingList<BatchFileItem>)grid.DataSource;
                for (int row = rows.Count; row < group.Items.Count; row++) rows.Add(group.Items[row]);
                ConfigureBatchPageBoxRows(grid, group.Items);
                grid.Visible = !group.IsCollapsed;
                grid.Invalidate();
                foreach (var header in grid.Parent.Controls.OfType<BatchGroupHeaderCard>()) header.UpdateCardStyle();
            }
            RefreshGroupSummaryHeader();
            return true;
        }
    }
}
