using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Forms.Main
{
    /// <summary>材料选择入口的宿主，只提交当前材料，不写入默认设置。</summary>
    public class TemporaryImpositionParametersForm : Form
    {
        private Form _layerOwner;
        private bool _restoringWindowLayer;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr window);
        public ImpositionParametersEditor Editor { get; }
        public TemporaryImpositionParameters Parameters { get; private set; }
        public bool IsFlatSheet => Editor.IsFlatSheet;

        public TemporaryImpositionParametersForm(bool isFlatSheet, TemporaryImpositionParameters parameters, bool focusRotation = false,
            ImpositionPdfInfo pdfInfo = null, IImpositionService impositionService = null,
            bool focusLayoutQuantity = false, bool focusLayoutGrid = false)
        {
            Parameters = (parameters ?? throw new ArgumentNullException(nameof(parameters))).Clone();
            Text = "本次排版参数";
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            KeyPreview = true;
            var area = Screen.FromControl(this).WorkingArea;
            ClientSize = new Size(Math.Min(1160, area.Width - 80), Math.Min(720, area.Height - 100));
            MinimumSize = new Size(Math.Min(840, area.Width - 40), Math.Min(560, area.Height - 40));
            Editor = new ImpositionParametersEditor(new ImpositionEditorPresenter(impositionService ?? new ImpositionService(),
                false, isFlatSheet, Parameters, pdfInfo)) { Dock = DockStyle.Fill };
            Controls.Add(Editor);
            Editor.Committed += (s, e) => { Parameters = Editor.Parameters.Clone(); DialogResult = DialogResult.OK; };
            Editor.CancelRequested += (s, e) => DialogResult = DialogResult.Cancel;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Escape) return;
                DialogResult = DialogResult.Cancel;
                e.Handled = e.SuppressKeyPress = true;
            };
            if (focusLayoutQuantity || focusLayoutGrid) Editor.FocusMode(false, focusLayoutQuantity, focusLayoutGrid);
            Shown += (s, e) =>
            {
                // DPI 自动缩放后再限制到实际工作区，避免窗口底部落在任务栏之外。
                var work = Screen.FromControl(this).WorkingArea;
                if (StartPosition != FormStartPosition.Manual)
                {
                    int maxWidth = work.Width - 24, maxHeight = work.Height - 24;
                    MinimumSize = new Size(Math.Min(MinimumSize.Width, maxWidth), Math.Min(MinimumSize.Height, maxHeight));
                    Size = new Size(Math.Min(Width, maxWidth), Math.Min(Height, maxHeight));
                    Location = new Point(Math.Max(work.Left, Math.Min(Left, work.Right - Width)), Math.Max(work.Top, Math.Min(Top, work.Bottom - Height)));
                }
                Editor.FocusMode(focusRotation, focusLayoutQuantity, focusLayoutGrid);
            };
        }

        private Task RefreshPreviewAsync() => Editor.RefreshPreviewAsync();
        private void ApplyButton_Click(object sender, EventArgs e) => Editor.TryApply(out _);

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!Modal || Owner == null) return;
            _layerOwner = Owner;
            _layerOwner.HandleCreated += OwnerLayerChanged;
            _layerOwner.Activated += OwnerLayerChanged;
            RestoreOwnerWindowLayer();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            QueueWindowLayerRestore();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            RestoreOwnerWindowLayer();
        }

        private void OwnerLayerChanged(object sender, EventArgs e) => QueueWindowLayerRestore();

        private void QueueWindowLayerRestore()
        {
            if (IsDisposed || Disposing || !IsHandleCreated || !Visible || !Modal) return;
            // 等待父窗完成句柄重建或激活逻辑，再恢复模态窗口前后顺序。
            BeginInvoke(new Action(RestoreOwnerWindowLayer));
        }

        internal void RestoreOwnerWindowLayer()
        {
            var owner = _layerOwner ?? Owner;
            if (_restoringWindowLayer || IsDisposed || Disposing || !Visible || !Modal ||
                !IsHandleCreated || !IsWindowEnabled(Handle) || owner == null || owner.IsDisposed || !owner.IsHandleCreated) return;
            _restoringWindowLayer = true;
            try
            {
                TopMost = owner.TopMost;
                // 不激活其他窗口、不移动父窗，只把当前模态窗口恢复到对应层级前端。
                const uint keepBoundsAndOwnerWithoutActivation = 0x0001 | 0x0002 | 0x0010 | 0x0200;
                if (!SetWindowPos(Handle, TopMost ? new IntPtr(-1) : IntPtr.Zero, 0, 0, 0, 0, keepBoundsAndOwnerWithoutActivation))
                    WindowsFormsApp3.Utils.LogHelper.Warn("恢复临时排版窗口层级失败，Win32错误码：" + Marshal.GetLastWin32Error());
            }
            finally { _restoringWindowLayer = false; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _layerOwner != null)
            {
                _layerOwner.HandleCreated -= OwnerLayerChanged;
                _layerOwner.Activated -= OwnerLayerChanged;
                _layerOwner = null;
            }
            base.Dispose(disposing);
        }
    }
}
