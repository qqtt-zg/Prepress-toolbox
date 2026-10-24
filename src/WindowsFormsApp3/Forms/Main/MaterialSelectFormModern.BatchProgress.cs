using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp3
{
    public partial class MaterialSelectFormModern
    {
        // 按路径计数，同一 PDF 的多条 Excel 匹配行不重复累计进度。
        private readonly Dictionary<string, bool?> _batchMetadataProgress = new Dictionary<string, bool?>(StringComparer.OrdinalIgnoreCase);
        private Panel _batchProgressPanel;
        private AntdUI.Label _batchProgressLabel;
        private AntdUI.Progress _batchMetadataProgressBar;
        private bool _isBatchLoading;
        private string _batchLoadingText;

        private void InitializeBatchMetadataProgress()
        {
            _batchProgressPanel = new Panel
            {
                Name = "batchMetadataProgressPanel", Dock = DockStyle.Fill,
                Visible = false, BackColor = Color.FromArgb(245, 247, 250)
            };
            var content = new Panel { Size = new Size(280, 44), BackColor = Color.Transparent };
            _batchProgressPanel.Resize += (sender, e) =>
            {
                content.Width = Math.Max(1, Math.Min(280, _batchProgressPanel.ClientSize.Width - 24));
                content.Location = new Point(Math.Max(0, (_batchProgressPanel.ClientSize.Width - content.Width) / 2),
                    Math.Max(0, (_batchProgressPanel.ClientSize.Height - content.Height) / 2));
            };
            _batchProgressLabel = new AntdUI.Label
            {
                Name = "batchMetadataProgressLabel", Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 8F), TabStop = false
            };
            _batchMetadataProgressBar = new AntdUI.Progress
            {
                Name = "batchMetadataProgressBar", Dock = DockStyle.Bottom,
                Height = 8, Text = "", TabStop = false
            };
            content.Controls.Add(_batchProgressLabel);
            content.Controls.Add(_batchMetadataProgressBar);
            _batchProgressPanel.Controls.Add(content);
        }

        private void SetBatchProgressVisible(bool visible)
        {
            if (_batchProgressPanel == null || IsDisposed || Disposing) return;
            _batchProgressPanel.Visible = visible;
            if (visible) _batchProgressPanel.BringToFront();
            else pnlCardsContainer?.BringToFront();
        }

        private void BeginBatchMetadataProgress(IEnumerable<string> paths)
        {
            foreach (string path in paths) _batchMetadataProgress[path] = null;
            UpdateBatchMetadataProgress();
        }

        private void CompleteBatchMetadataProgress(string path, bool success)
        {
            _batchMetadataProgress[path] = success;
            UpdateBatchMetadataProgress();
        }

        private void UpdateBatchMetadataProgress()
        {
            if (_batchProgressPanel == null || IsDisposed || Disposing || _batchPageCountRetriesStopped) return;
            var files = _batchItems.Where(item => !string.IsNullOrWhiteSpace(item.FilePath) &&
                string.Equals(Path.GetExtension(item.FilePath), ".pdf", StringComparison.OrdinalIgnoreCase))
                .GroupBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
            var currentPaths = new HashSet<string>(files.Select(group => group.Key), StringComparer.OrdinalIgnoreCase);
            foreach (string removed in _batchMetadataProgress.Keys.Where(path => !currentPaths.Contains(path)).ToList())
                _batchMetadataProgress.Remove(removed);
            int total = files.Count;
            if (total == 0)
            {
                _batchMetadataProgressBar.Value = 0;
                _batchProgressLabel.Text = _batchLoadingText ?? "正在加载工单文件...";
                SetBatchProgressVisible(_isBatchLoading);
                return;
            }
            int completed = 0;
            int failed = 0;
            bool hasActiveMetadata = false;
            foreach (var group in files)
            {
                bool registered = _batchMetadataProgress.TryGetValue(group.Key, out var tracked);
                bool? state = registered
                    ? tracked : group.All(item => item.PageBoxSource.HasValue) ? (bool?)true : null;
                if (registered && !tracked.HasValue) hasActiveMetadata = true;
                if (state.HasValue) completed++;
                if (state == false) failed++;
            }
            _batchMetadataProgressBar.Value = (float)completed / total;
            _batchMetadataProgressBar.Fill = _activeTheme?.Primary ?? Color.FromArgb(22, 119, 255);
            _batchProgressPanel.BackColor = _activeTheme?.Background ?? Color.FromArgb(245, 247, 250);
            _batchProgressLabel.ForeColor = _activeTheme?.TextPrimary ?? Color.FromArgb(74, 85, 104);
            _batchProgressLabel.Text = completed < total
                ? $"正在解析 {completed} / {total} 个文件" + (failed > 0 ? $" · 失败 {failed}" : "")
                : $"解析结束：完成 {total - failed}，失败 {failed}";
            if (_isBatchLoading && completed == total) _batchProgressLabel.Text = _batchLoadingText;
            // 与原加载提示共用内容区：加载结束立即隐藏，不在标题栏保留结果。
            SetBatchProgressVisible(_isBatchLoading || hasActiveMetadata);
        }
    }
}
