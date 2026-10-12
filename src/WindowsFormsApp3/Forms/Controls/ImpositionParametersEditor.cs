using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Presenters;

namespace WindowsFormsApp3.Forms.Controls
{
    /// <summary>共享原生视图；保存目标、校验及计算由 Presenter 决定。</summary>
    public class ImpositionParametersEditor : UserControl
    {
        private static readonly Color Blue = Color.FromArgb(18, 100, 214);
        private static readonly Color Muted = Color.FromArgb(100, 116, 139);
        private readonly ImpositionEditorPresenter _presenter;

        private readonly System.Windows.Forms.Panel _parameters = new System.Windows.Forms.Panel();
        private readonly CardPanel _materialPanel = new CardPanel();
        private readonly System.Windows.Forms.Panel _segmentCapsule = new System.Windows.Forms.Panel();
        private readonly CardPanel _specs = new CardPanel();
        private readonly CardPanel _directionPanel = new CardPanel();
        private readonly CardPanel _strategy = new CardPanel();
        private readonly System.Windows.Forms.Panel _strategySubpanel = new System.Windows.Forms.Panel();
        private readonly System.Windows.Forms.Panel _preview = new System.Windows.Forms.Panel();
        private readonly LegendControl _legend = new LegendControl();
        private readonly PreviewFooterPanel _footerPanel = new PreviewFooterPanel();

        private readonly Dictionary<string, Field> _fields = new Dictionary<string, Field>();
        private readonly AntdUI.Label _title = Label("本次排版参数", true);
        private readonly AntdUI.Label _origin = Label("本次草稿 · 不修改默认参数");
        private readonly AntdUI.Label _specTitle = Label("卷装材料规格", true);
        private readonly AntdUI.Label _marginsTitle = Label("四周边距 (Margins)");
        private readonly AntdUI.Label _spacingTitle = Label("成品拼版间距 (Spacing)");
        private readonly AntdUI.Label _parameterSource = Label("");
        private readonly AntdUI.Label _lengthHelp = Label("");
        private readonly AntdUI.Label _source = Label("");
        private readonly AntdUI.Label _strategyHelp = Label("");
        private readonly AntdUI.Label _previewTitle = Label("排版预览 · 居中", true);

        // 保留 _summary 和 _status 用于兼容测试反射读取
        private readonly AntdUI.Label _summary = Label("");
        private readonly AntdUI.Label _offsets = Label("");
        private readonly AntdUI.Label _status = Label("");

        private readonly AntdUI.Label _draftState = Label("草稿 · 尚未应用");
        private readonly AntdUI.Checkbox _enabled = new AntdUI.Checkbox { Name = "impositionEnabled", Text = "启用本次排版" };
        private readonly AntdUI.Checkbox _autoRows = new AntdUI.Checkbox { Name = "automaticRows", Text = "自动" };
        private readonly AntdUI.Checkbox _autoColumns = new AntdUI.Checkbox { Name = "automaticColumns", Text = "自动" };
        private readonly AntdUI.Button _flat = Button("平张材料 (Sheet)", "materialFlat");
        private readonly AntdUI.Button _roll = Button("卷装材料 (Roll)", "materialRoll");
        private readonly AntdUI.Button[] _rotations = new AntdUI.Button[3];
        private readonly AntdUI.Button[] _strategies = new AntdUI.Button[2];
        private readonly AntdUI.Button[] _resets = new AntdUI.Button[6];
        private readonly AntdUI.Button _resetSpecs = Button("↺ 恢复推荐", "restoreSpecs");
        private readonly AntdUI.Button _zeroSpacing = Button("清零", "zeroSpacing");
        private readonly AntdUI.Button _resetAutoGrid = Button("一键归0", "resetAutoGrid");
        private readonly AntdUI.Select _direction = new AntdUI.Select { Name = "layoutConstraintDirection" };
        private readonly AntdUI.Button _importPaper = Button("从 PDF 拾取", "getPaperPdfSize");
        private readonly AntdUI.Button _apply = Button("应用本次参数", "applyParameters");
        private readonly AntdUI.Button _cancel = Button("取消修改", "cancelParameters");
        private readonly AntdUI.Button _restore = Button("恢复默认参数", "restoreGlobalParameters");
        private readonly AntdUI.Button _automatic = Button("恢复自动布局", "restoreAutomaticLayout");
        private readonly AntdUI.Button _fix = Button("改为自动择优", "fixAutomaticLayout");
        private readonly AntdUI.Button[] _viewButtons = new AntdUI.Button[4];
        private readonly AntdUI.Slider _position = new AntdUI.Slider { Name = "previewPosition", MinValue = 0, MaxValue = 100 };
        private readonly AntdUI.Slider _horizontalPosition = new AntdUI.Slider { Name = "previewPositionX", MinValue = 0, MaxValue = 100, Value = 50 };
        private readonly AntdUI.Label _positionCaption = Label("纵向");
        private readonly AntdUI.Label _horizontalCaption = Label("横向");
        private readonly ImpositionPreviewControl _canvas = new ImpositionPreviewControl();

        private List<ImpositionResult> _layouts = new List<ImpositionResult>();
        private TemporaryImpositionParameters _previewParameters;
        private ImpositionResult _selectedLayout;
        private CancellationTokenSource _previewCancellation;
        private CancellationTokenSource _fileCancellation;
        private bool _loading;
        private bool _calculating;
        private bool _readingFile;
        private string _fileError;

        public bool IsFlatSheet => _presenter.IsFlatSheet;
        public TemporaryImpositionParameters Parameters { get; private set; }
        public event EventHandler Committed;
        public event EventHandler CancelRequested;

        public ImpositionParametersEditor(ImpositionEditorPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScroll = false;
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F);
            BuildControls();
            BindDraft();
            Resize += (s, e) => Arrange();
            Load += (s, e) => { Arrange(); _ = RefreshPreviewAsync(); };
        }

        private static AntdUI.Label Label(string text, bool heading = false) => new AntdUI.Label
        {
            Text = text, ForeColor = heading ? Color.FromArgb(30, 41, 59) : Muted,
            Font = new Font("Microsoft YaHei UI", heading ? 9.5F : 8F, heading ? FontStyle.Bold : FontStyle.Regular)
        };

        private static AntdUI.Button Button(string text, string name) => new AntdUI.Button
        {
            Text = text, Name = name, Radius = 4, WaveSize = 0, Font = new Font("Microsoft YaHei UI", 8.5F),
            Tag = "custom_button"
        };

        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible)
            {
                BindDraft();
            }
        }

        private class CardPanel : System.Windows.Forms.Panel
        {
            public CardPanel()
            {
                DoubleBuffered = true;
                BackColor = Color.White;
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
        }

        private class Field : System.Windows.Forms.Panel
        {
            public readonly AntdUI.Label Caption;
            public readonly AntdUI.Input Input;
            public bool Vertical { get; set; }

            public Field(string title, string name, string unit, bool vertical = false)
            {
                Vertical = vertical;
                Caption = Label(title);
                Caption.Font = new Font("Microsoft YaHei UI", 8.5F);
                Caption.ForeColor = Color.FromArgb(71, 85, 105);
                Input = new AntdUI.Input
                {
                    Name = name, SuffixText = unit,
                    Font = new Font("Consolas", 10F), AccessibleName = title,
                    Radius = 4
                };
                Controls.Add(Caption); Controls.Add(Input);
            }

            protected override void OnLayout(LayoutEventArgs e)
            {
                base.OnLayout(e);
                float dpi = DeviceDpi / 96F;
                if (Vertical || Height >= 38 * dpi)
                {
                    int captionHeight = (int)(16 * dpi);
                    Caption.SetBounds(0, 0, Width, captionHeight);
                    Input.SetBounds(0, captionHeight + 2, Width, Math.Max(1, Height - captionHeight - 2));
                    return;
                }
                int label = Math.Min((int)((Caption.Text.Length > 2 ? 56 : 22) * dpi), Math.Max((int)(20 * dpi), Width / 3));
                Caption.SetBounds(0, 0, label, Height);
                Input.SetBounds(label, 0, Math.Max(1, Width - label), Height);
            }
        }

        private class LegendControl : Control
        {
            public bool IsGlobal { get; set; }
            public LegendControl()
            {
                DoubleBuffered = true;
                BackColor = Color.White;
                Font = new Font("Microsoft YaHei UI", 8F);
            }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                int x = Width;
                if (!IsGlobal)
                {
                    var text = "真实比例成品";
                    var size = TextRenderer.MeasureText(text, Font);
                    x -= size.Width + 14;
                    using (var b = new SolidBrush(Color.FromArgb(230, 241, 255)))
                    using (var p = new Pen(Color.FromArgb(22, 119, 255)))
                    {
                        g.FillRectangle(b, x, 4, 8, 8);
                        g.DrawRectangle(p, x, 4, 8, 8);
                    }
                    TextRenderer.DrawText(g, text, Font, new Point(x + 11, 2), Color.FromArgb(100, 116, 139));
                    x -= 10;
                }
                {
                    var text = "净印刷安全区";
                    var size = TextRenderer.MeasureText(text, Font);
                    x -= size.Width + 14;
                    using (var b = new SolidBrush(Color.FromArgb(237, 245, 255)))
                    using (var p = new Pen(Color.FromArgb(18, 100, 214)) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    {
                        g.FillRectangle(b, x, 4, 8, 8);
                        g.DrawRectangle(p, x, 4, 8, 8);
                    }
                    TextRenderer.DrawText(g, text, Font, new Point(x + 11, 2), Color.FromArgb(100, 116, 139));
                    x -= 10;
                }
                {
                    var text = "纸张外框";
                    var size = TextRenderer.MeasureText(text, Font);
                    x -= size.Width + 14;
                    using (var p = new Pen(Color.FromArgb(71, 85, 105)))
                    {
                        g.FillRectangle(Brushes.White, x, 4, 8, 8);
                        g.DrawRectangle(p, x, 4, 8, 8);
                    }
                    TextRenderer.DrawText(g, text, Font, new Point(x + 11, 2), Color.FromArgb(100, 116, 139));
                }
            }
        }

        private class PreviewFooterPanel : System.Windows.Forms.Panel
        {
            public bool IsGlobal { get; set; }
            public bool Valid { get; set; }
            public bool EnabledMode { get; set; }
            public int Quantity { get; set; }
            public float ProductWidth { get; set; }
            public float ProductHeight { get; set; }
            public int Rows { get; set; }
            public int Columns { get; set; }
            public float SpacingX { get; set; }
            public float SpacingY { get; set; }
            public float GridWidth { get; set; }
            public float GridHeight { get; set; }
            public float PrintableWidth { get; set; }
            public float PrintableHeight { get; set; }
            public float PaperWidth { get; set; }
            public float PaperHeight { get; set; }
            public float MarginLeft { get; set; }
            public float MarginRight { get; set; }
            public float MarginTop { get; set; }
            public float MarginBottom { get; set; }
            public string StatusText { get; set; }
            public bool IsError { get; set; }

            public PreviewFooterPanel()
            {
                DoubleBuffered = true;
                BackColor = Color.White;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(226, 232, 240), 1))
                    g.DrawLine(pen, 0, 0, Width, 0);

                if (IsGlobal)
                {
                    using (var numFont = new Font("Consolas", 15F, FontStyle.Bold))
                    using (var unitFont = new Font("Microsoft YaHei UI", 8.5F))
                    using (var descFont = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold))
                    {
                        string numStr = $"{PrintableWidth:0.##} × {PrintableHeight:0.##}";
                        var numSize = TextRenderer.MeasureText(numStr, numFont);
                        TextRenderer.DrawText(g, numStr, numFont, new Point(4, 4), Color.FromArgb(18, 100, 214));
                        int curX = 4 + numSize.Width - 4;
                        TextRenderer.DrawText(g, "mm", unitFont, new Point(curX, 10), Color.FromArgb(100, 116, 139));
                        curX += 26;
                        TextRenderer.DrawText(g, "大版有效印刷安全区 (已扣除留白)", descFont, new Point(curX, 8), Color.FromArgb(30, 41, 59));
                    }

                    string status = StatusText ?? "大版规格就绪";
                    using (var badgeFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold))
                    {
                        var badgeSize = TextRenderer.MeasureText(status, badgeFont);
                        int badgeW = badgeSize.Width + 14, badgeH = 20;
                        int badgeX = Width - badgeW - 6, badgeY = 6;
                        var badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);
                        Color backCol = IsError ? Color.FromArgb(254, 226, 226) : Color.FromArgb(204, 251, 241);
                        Color textCol = IsError ? Color.FromArgb(153, 27, 27) : Color.FromArgb(15, 118, 110);
                        using (var brush = new SolidBrush(backCol))
                            g.FillRectangle(brush, badgeRect);
                        TextRenderer.DrawText(g, status, badgeFont, badgeRect, textCol, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }

                    using (var secFont = new Font("Microsoft YaHei UI", 8.5F))
                    {
                        string secStr = $"物理: {PaperWidth:0.##}×{PaperHeight:0.##}mm  |  边距: 左{MarginLeft:0.##} 右{MarginRight:0.##} 上{MarginTop:0.##} 下{MarginBottom:0.##}mm  |  间距: 水平{SpacingX:0.##}mm · 垂直{SpacingY:0.##}mm";
                        TextRenderer.DrawText(g, secStr, secFont, new Point(4, 27), Color.FromArgb(71, 85, 105));
                    }
                }
                else
                {
                    if (Valid && EnabledMode)
                    {
                        using (var numFont = new Font("Consolas", 15F, FontStyle.Bold))
                        using (var unitFont = new Font("Microsoft YaHei UI", 8.5F))
                        using (var pillFont = new Font("Consolas", 9F, FontStyle.Bold))
                        using (var descFont = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold))
                        {
                            string numStr = $"{Quantity}";
                            var numSize = TextRenderer.MeasureText(numStr, numFont);
                            TextRenderer.DrawText(g, numStr, numFont, new Point(4, 4), Color.FromArgb(18, 100, 214));
                            int curX = 4 + numSize.Width - 4;
                            TextRenderer.DrawText(g, "个/纸", unitFont, new Point(curX, 10), Color.FromArgb(100, 116, 139));
                            curX += 34;

                            string pillStr = $"📄 成品: {ProductWidth:0.##} × {ProductHeight:0.##} mm";
                            var pillSize = TextRenderer.MeasureText(pillStr, pillFont);
                            var pillRect = new Rectangle(curX, 5, pillSize.Width + 10, 20);
                            using (var pillBrush = new SolidBrush(Color.FromArgb(237, 245, 255)))
                            using (var pillPen = new Pen(Color.FromArgb(185, 213, 252)))
                            {
                                g.FillRectangle(pillBrush, pillRect);
                                g.DrawRectangle(pillPen, pillRect);
                            }
                            TextRenderer.DrawText(g, pillStr, pillFont, pillRect, Color.FromArgb(18, 100, 214), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                            curX += pillRect.Width + 10;

                            string desc = $"{Rows} 行 × {Columns} 列 · 居中真实对齐";
                            TextRenderer.DrawText(g, desc, descFont, new Point(curX, 8), Color.FromArgb(30, 41, 59));
                        }

                        using (var badgeFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold))
                        {
                            string status = StatusText ?? "排版已就绪";
                            var badgeSize = TextRenderer.MeasureText(status, badgeFont);
                            int badgeW = badgeSize.Width + 14, badgeH = 20;
                            int badgeX = Width - badgeW - 6, badgeY = 6;
                            var badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);
                            Color backCol = IsError ? Color.FromArgb(254, 226, 226) : Color.FromArgb(204, 251, 241);
                            Color textCol = IsError ? Color.FromArgb(153, 27, 27) : Color.FromArgb(15, 118, 110);
                            using (var brush = new SolidBrush(backCol))
                                g.FillRectangle(brush, badgeRect);
                            TextRenderer.DrawText(g, status, badgeFont, badgeRect, textCol, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }

                        using (var secFont = new Font("Microsoft YaHei UI", 8.5F))
                        {
                            string secStr = $"成品间距: 水平{SpacingX:0.##}mm · 垂直{SpacingY:0.##}mm  |  网格: {GridWidth:0.##}×{GridHeight:0.##}mm  |  安全区: {PrintableWidth:0.##}×{PrintableHeight:0.##}mm";
                            TextRenderer.DrawText(g, secStr, secFont, new Point(4, 27), Color.FromArgb(71, 85, 105));
                        }
                    }
                    else
                    {
                        using (var errFont = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold))
                        {
                            string errStr = StatusText ?? "暂无可用布局";
                            TextRenderer.DrawText(g, errStr, errFont, new Point(4, 12), Color.Firebrick);
                        }
                    }
                }
            }
        }

        private void AddField(Control parent, string key, string caption, string name, string unit = "mm", bool vertical = false)
        {
            var field = new Field(caption, name, unit, vertical);
            _fields.Add(key, field); parent.Controls.Add(field);
            field.Input.TextChanged += (s, e) => Changed();
        }

        private void BuildControls()
        {
            Controls.AddRange(new Control[] { _title, _origin, _enabled, _parameters, _preview,
                _apply, _cancel, _restore, _automatic, _draftState });

            _parameters.BackColor = Color.FromArgb(250, 251, 252);
            _parameters.Controls.AddRange(new Control[] { _materialPanel, _specs, _directionPanel, _strategy });

            // 1. 材料切换卡片 (胶囊背景 + 双切换按钮)
            _segmentCapsule.BackColor = Color.FromArgb(226, 232, 240);
            _segmentCapsule.Controls.AddRange(new Control[] { _flat, _roll });
            _materialPanel.Controls.Add(_segmentCapsule);

            // 2. 大版规格卡片
            _parameterSource.Name = "parameterSource";
            _resetSpecs.BorderWidth = 0;
            _resetSpecs.ForeColor = Blue;
            _resetSpecs.BackColor = Color.Transparent;
            _resetSpecs.Click += (s, e) =>
            {
                CaptureDraft();
                _presenter.Restore(field: "width");
                _presenter.Restore(field: "length");
                _presenter.Restore(field: "margins");
                _presenter.Restore(field: "spacing");
                BindDraft(); _ = RefreshPreviewAsync();
            };

            _zeroSpacing.BorderWidth = 0;
            _zeroSpacing.ForeColor = Blue;
            _zeroSpacing.BackColor = Color.Transparent;
            _zeroSpacing.Click += (s, e) =>
            {
                if (_fields.ContainsKey("spacingX")) _fields["spacingX"].Input.Text = "0";
                if (_fields.ContainsKey("spacingY")) _fields["spacingY"].Input.Text = "0";
                Changed();
            };

            _importPaper.BorderWidth = 0;
            _importPaper.ForeColor = Blue;
            _importPaper.BackColor = Color.Transparent;

            _specs.Controls.AddRange(new Control[] { _specTitle, _parameterSource, _resetSpecs, _lengthHelp, _importPaper, _marginsTitle, _spacingTitle, _zeroSpacing });
            AddField(_specs, "width", "固定幅宽", "materialWidth", "mm", true);
            AddField(_specs, "length", "最小长度", "materialLength", "mm", true);
            foreach (var field in new[] { new[] { "left", "左" }, new[] { "right", "右" }, new[] { "top", "上" }, new[] { "bottom", "下" } })
                AddField(_specs, field[0], field[1], "margin" + field[0], "mm", false);
            AddField(_specs, "spacingX", "水平列距", "spacingX", "mm", false);
            AddField(_specs, "spacingY", "垂直行距", "spacingY", "mm", false);

            string[] resetNames = { "width", "length", "margins", "spacing", "rotation", "rows" };
            string[] resetTexts = { "恢复宽度", "长度", "四边距", "间距", "方向", "行列" };
            for (int i = 0; i < _resets.Length; i++)
            {
                string key = resetNames[i];
                _resets[i] = Button(resetTexts[i], "restore" + key);
                _resets[i].Visible = false; // 内部保留，视觉收敛为单个 ↺ 恢复推荐
                _resets[i].Click += (s, e) => { CaptureDraft(); _presenter.Restore(field: key); BindDraft(); _ = RefreshPreviewAsync(); };
                _specs.Controls.Add(_resets[i]);
            }

            // 3. 页面方向偏好卡片
            var directionTitle = Label("页面方向偏好", true); directionTitle.Name = "rotationTitle";
            _directionPanel.Controls.Add(directionTitle);
            string[] rotations = { "自动方向", "禁止旋转 (0°)", "固定 270°" };
            for (int i = 0; i < 3; i++)
            {
                int index = i; _rotations[i] = Button(rotations[i], "rotationPolicy" + i);
                _rotations[i].Click += (s, e) => { CaptureDraft(); _presenter.Current.Rotation = (RollRotationMode)index; BindDraft(); _ = RefreshPreviewAsync(); };
                _directionPanel.Controls.Add(_rotations[i]);
            }

            // 4. 排版方式卡片
            var strategyTitle = Label("排版方式", true); strategyTitle.Name = "strategyTitle";
            _strategySubpanel.BackColor = Color.FromArgb(248, 250, 252);
            _strategy.Controls.AddRange(new Control[] { strategyTitle, _strategySubpanel });

            string[] strategies = { "网格布局 (可手动/0自动)", "倍数约束布局" };
            for (int i = 0; i < 2; i++)
            {
                int index = i; _strategies[i] = Button(strategies[i], new[] { "strategyGrid", "strategyMultiple" }[i]);
                _strategies[i].Click += (s, e) => SelectStrategy(index == 0 ? ImpositionLayoutStrategy.FixedGrid : ImpositionLayoutStrategy.Multiple);
                _strategy.Controls.Add(_strategies[i]);
            }

            _resetAutoGrid.BorderWidth = 0;
            _resetAutoGrid.ForeColor = Blue;
            _resetAutoGrid.BackColor = Color.Transparent;
            _resetAutoGrid.Click += (s, e) =>
            {
                if (_fields.ContainsKey("rows")) _fields["rows"].Input.Text = "0";
                if (_fields.ContainsKey("columns")) _fields["columns"].Input.Text = "0";
                _autoRows.Checked = _autoColumns.Checked = true;
                SelectStrategy(ImpositionLayoutStrategy.Automatic);
            };

            _strategySubpanel.Controls.AddRange(new Control[] { _direction, _strategyHelp, _resetAutoGrid });
            AddField(_strategySubpanel, "rows", "手动行数 (0 为自动)", "requestedRows", "行", true);
            AddField(_strategySubpanel, "columns", "手动列数 (0 为自动)", "requestedColumns", "列", true);
            AddField(_strategySubpanel, "multiple", "倍数 N", "layoutMultiple", "倍", true);

            _direction.Items.AddRange(new object[] { "按列数倍数", "按行数倍数" });
            _direction.SelectedIndexChanged += (s, e) => Changed();
            _autoRows.CheckedChanged += (s, e) => AutoAxisChanged("rows", _autoRows.Checked, _selectedLayout?.Rows ?? 0);
            _autoColumns.CheckedChanged += (s, e) => AutoAxisChanged("columns", _autoColumns.Checked, _selectedLayout?.Columns ?? 0);
            _enabled.CheckedChanged += (s, e) => Changed();

            _flat.Click += (s, e) => SwitchMaterial(true);
            _roll.Click += (s, e) => SwitchMaterial(false);
            _restore.Click += (s, e) => { CaptureDraft(); _presenter.Restore(); BindDraft(); _ = RefreshPreviewAsync(); };
            _automatic.Click += (s, e) => SelectStrategy(ImpositionLayoutStrategy.Automatic);
            _fix.Click += (s, e) => SelectStrategy(ImpositionLayoutStrategy.Automatic);
            _importPaper.Click += async (s, e) => await ReadPdfAsync(true);
            _apply.Type = TTypeMini.Primary;

            // 右侧预览区
            _preview.BackColor = Color.White;
            _status.Name = "layoutValidationStatus";
            _summary.Name = "layoutResultSummary";
            _status.Visible = _summary.Visible = _offsets.Visible = false; // 内部保留用于测试

            _preview.Controls.AddRange(new Control[] { _previewTitle, _legend, _canvas, _footerPanel, _summary, _offsets, _status, _fix,
                _position, _horizontalPosition, _positionCaption, _horizontalCaption });

            string[] viewTexts = { "−", "+", "适应窗口", "局部查看" };
            for (int i = 0; i < 4; i++)
            {
                int index = i; _viewButtons[i] = Button(viewTexts[i], "previewView" + i);
                _preview.Controls.Add(_viewButtons[i]);
            }
            _viewButtons[0].Click += (s, e) => { _canvas.Zoom = Math.Max(1, _canvas.Zoom - 0.25f); _canvas.Invalidate(); Arrange(); };
            _viewButtons[1].Click += (s, e) => { _canvas.Zoom = Math.Min(4, _canvas.Zoom + 0.25f); _canvas.Invalidate(); Arrange(); };
            _viewButtons[2].Click += (s, e) => { _canvas.Detail = false; _canvas.Zoom = 1; _canvas.Invalidate(); Arrange(); };
            _viewButtons[3].Click += (s, e) => { _canvas.Detail = true; _canvas.Invalidate(); Arrange(); };

            _position.AccessibleName = "预览纵向位置";
            _position.ValueChanged += (s, e) => { _canvas.Position = _position.Value; _canvas.Invalidate(); };
            _horizontalPosition.AccessibleName = "预览横向位置";
            _horizontalPosition.ValueChanged += (s, e) => { _canvas.HorizontalPosition = _horizontalPosition.Value; _canvas.Invalidate(); };

            _flat.TabIndex = 0; _roll.TabIndex = 1; _enabled.TabIndex = 2; _parameters.TabIndex = 3; _preview.TabIndex = 4;
            _restore.TabIndex = 5; _automatic.TabIndex = 6; _cancel.TabIndex = 7; _apply.TabIndex = 8;
            int order = 0;
            foreach (string key in new[] { "width", "length", "left", "right", "top", "bottom", "spacingX", "spacingY" })
                if (_fields.ContainsKey(key)) _fields[key].TabIndex = order++;
            _importPaper.TabIndex = order++;
            for (int i = 0; i < 3; i++) _rotations[i].TabIndex = i;
            for (int i = 0; i < 2; i++) _strategies[i].TabIndex = i;
            _fields["rows"].TabIndex = 0; _autoRows.TabIndex = 1;
            _fields["columns"].TabIndex = 2; _autoColumns.TabIndex = 3;
            _fields["multiple"].TabIndex = 4; _direction.TabIndex = 5;

            _apply.Click += (s, e) => TryApply(out _);
            _cancel.Click += (s, e) =>
            {
                if (_presenter.IsGlobal) { _presenter.Cancel(); BindDraft(); _ = RefreshPreviewAsync(); }
                else CancelRequested?.Invoke(this, EventArgs.Empty);
            };
        }

        private string TextOf(string key) => _fields.ContainsKey(key) ? _fields[key].Input.Text : "";

        private void AutoAxisChanged(string key, bool automatic, int calculated)
        {
            if (_loading) return;
            if (!automatic && (string.IsNullOrWhiteSpace(TextOf(key)) || TextOf(key) == "0"))
            {
                _loading = true; _fields[key].Input.Text = calculated.ToString(); _loading = false;
            }
            Changed();
        }

        private void CaptureDraft()
        {
            if (_loading) return;
            var d = _presenter.Current;
            d.Width = TextOf("width"); d.Length = TextOf("length"); d.Left = TextOf("left"); d.Right = TextOf("right");
            d.Top = TextOf("top"); d.Bottom = TextOf("bottom");
            d.SpacingX = TextOf("spacingX"); d.SpacingY = TextOf("spacingY");
            d.Rows = TextOf("rows"); d.Columns = TextOf("columns");
            d.Multiple = TextOf("multiple");
            d.AutoRows = string.IsNullOrWhiteSpace(d.Rows) || d.Rows == "0";
            d.AutoColumns = string.IsNullOrWhiteSpace(d.Columns) || d.Columns == "0";
            _autoRows.Checked = d.AutoRows; _autoColumns.Checked = d.AutoColumns;
            d.Direction = _direction.SelectedIndex == 1 ? CopyMode.AutoByRow : CopyMode.AutoByColumn;
            _presenter.Enabled = _enabled.Checked;
            _presenter.SyncCommonLayout();
        }

        private void Changed()
        {
            if (_loading) return;
            _fileError = null;
            CaptureDraft();
            _fields["rows"].Input.Enabled = true;
            _fields["columns"].Input.Enabled = true;
            _draftState.Text = "草稿 · 尚未" + (_presenter.IsGlobal ? "保存" : "应用");
            _ = RefreshPreviewAsync();
        }

        private static string Format(float value) => value > 0 ? value.ToString("0.###", CultureInfo.CurrentCulture) : "";

        private void BindDraft()
        {
            _loading = true;
            var d = _presenter.Current;
            var values = new Dictionary<string, string> {
                ["width"] = d.Width, ["length"] = d.Length, ["left"] = d.Left,
                ["right"] = d.Right, ["top"] = d.Top, ["bottom"] = d.Bottom,
                ["spacingX"] = d.SpacingX, ["spacingY"] = d.SpacingY,
                ["rows"] = d.Rows, ["columns"] = d.Columns, ["multiple"] = d.Multiple
            };
            foreach (var pair in values)
                if (_fields.ContainsKey(pair.Key)) _fields[pair.Key].Input.Text = pair.Value;

            _enabled.Checked = _presenter.Enabled;
            _autoRows.Checked = d.AutoRows; _autoColumns.Checked = d.AutoColumns;
            _direction.SelectedIndex = d.Direction == CopyMode.AutoByRow ? 1 : 0;
            _title.Text = _presenter.IsGlobal ? "自动排版设置 · 默认参数" : "本次排版参数 · 当前任务";
            _origin.Text = _presenter.IsGlobal ? "默认草稿 · 保存供后续任务使用" : "本次草稿 · 不修改默认参数，页序复制独立";
            _enabled.Text = _presenter.IsGlobal ? "默认启用排版" : "启用本次排版";
            _apply.Text = _presenter.IsGlobal ? "保存默认设置" : "应用本次参数";
            _restore.Text = _presenter.IsGlobal ? "撤销当前材料修改" : "恢复默认参数";
            _specTitle.Text = IsFlatSheet ? "平张大版规格" : "卷装材料规格";
            _fields["width"].Caption.Text = IsFlatSheet ? "纸张宽度" : "固定幅宽";
            _fields["length"].Caption.Text = IsFlatSheet ? "纸张高度" : "最小长度";
            _fields["width"].Input.AccessibleName = _fields["width"].Caption.Text;
            _fields["length"].Input.AccessibleName = _fields["length"].Caption.Text;
            _lengthHelp.Text = IsFlatSheet ? "宽高固定 · 边距以内为有效印刷区域" : "横向限幅宽 · 长度自动延展，下限留白";
            _source.Text = _presenter.Source;
            _importPaper.Visible = IsFlatSheet;

            MarkSegment(_flat, IsFlatSheet); MarkSegment(_roll, !IsFlatSheet);
            for (int i = 0; i < 3; i++) Mark(_rotations[i], i == (int)d.Rotation);
            Mark(_strategies[0], d.Strategy == ImpositionLayoutStrategy.Automatic || d.Strategy == ImpositionLayoutStrategy.FixedGrid);
            Mark(_strategies[1], d.Strategy == ImpositionLayoutStrategy.Multiple);
            _strategies[1].Enabled = !_presenter.IsGlobal;

            _fields["rows"].Input.Enabled = true;
            _fields["columns"].Input.Enabled = true;
            _loading = false;
            Arrange();
        }

        private static void MarkSegment(AntdUI.Button button, bool selected)
        {
            button.Type = TTypeMini.Default;
            button.DefaultBack = selected ? Color.White : Color.FromArgb(226, 232, 240);
            button.DefaultBorderColor = Color.Transparent;
            button.BorderWidth = 0;
            button.ForeColor = selected ? Blue : Color.FromArgb(100, 116, 139);
            button.Font = new Font("Microsoft YaHei UI", 8.5F, selected ? FontStyle.Bold : FontStyle.Regular);
            button.AccessibleDescription = selected ? "已选择" : "未选择";
            button.Invalidate();
        }

        private static void Mark(AntdUI.Button button, bool selected)
        {
            button.Type = TTypeMini.Default;
            button.DefaultBack = selected ? Color.FromArgb(237, 245, 255) : Color.White;
            button.DefaultBorderColor = selected ? Color.FromArgb(18, 100, 214) : Color.FromArgb(226, 232, 240);
            button.ForeColor = selected ? Blue : Color.FromArgb(71, 85, 105);
            button.BorderWidth = selected ? 1.5f : 1;
            button.Font = new Font("Microsoft YaHei UI", 8.5F, selected ? FontStyle.Bold : FontStyle.Regular);
            button.AccessibleDescription = selected ? "已选择" : "未选择";
            button.Invalidate();
        }

        private void SelectStrategy(ImpositionLayoutStrategy strategy, bool capture = true)
        {
            if (_presenter.IsGlobal && strategy == ImpositionLayoutStrategy.Multiple) return;
            if (capture) CaptureDraft();
            if (strategy == ImpositionLayoutStrategy.Automatic)
            {
                _presenter.Current.Rows = "0"; _presenter.Current.Columns = "0";
                _presenter.Current.AutoRows = _presenter.Current.AutoColumns = true;
                _presenter.Current.Strategy = ImpositionLayoutStrategy.Automatic;
            }
            else
            {
                _presenter.Current.Strategy = strategy;
            }
            _presenter.SyncCommonLayout(); BindDraft(); _ = RefreshPreviewAsync();
        }

        private void SwitchMaterial(bool flat)
        {
            CaptureDraft(); _presenter.IsFlatSheet = flat;
            _canvas.Detail = false; _canvas.Zoom = 1; _position.Value = 0; _horizontalPosition.Value = 50;
            _selectedLayout = null;
            BindDraft(); _ = RefreshPreviewAsync();
        }

        public void FocusMode(bool rotation, bool quantity, bool grid)
        {
            if (grid || quantity) SelectStrategy(ImpositionLayoutStrategy.FixedGrid);
            if (rotation) _rotations[(int)_presenter.Current.Rotation].Focus();
            else if (grid || quantity) (_autoRows.Checked ? _fields["columns"].Input : _fields["rows"].Input).Focus();
        }

        public async Task RefreshPreviewAsync()
        {
            if (IsDisposed || _loading) return;
            CaptureDraft();
            _parameterSource.Text = _presenter.MaterialOrigin;
            _previewCancellation?.Cancel(); _previewCancellation?.Dispose();
            var cancellation = _previewCancellation = new CancellationTokenSource();
            var previous = _selectedLayout;
            _selectedLayout = null; _canvas.LayoutResult = null; _canvas.Geometry = null;
            _fix.Visible = false;
            foreach (var button in _viewButtons) button.Enabled = false;
            _canvas.EmptyText = "正在更新预览"; _canvas.Invalidate();
            _calculating = true; _apply.Enabled = false;
            if (!_presenter.TryGetParameters(IsFlatSheet, out _previewParameters, out string error))
            {
                _calculating = false;
                _status.Text = error; _status.ForeColor = Color.Firebrick;
                _footerPanel.StatusText = error; _footerPanel.IsError = true; _footerPanel.Valid = false;
                _canvas.EmptyText = "请检查参数"; _summary.Text = "— × —"; _offsets.Text = ""; Arrange(); return;
            }
            _status.Text = "计算中…";
            try
            {
                var layouts = await _presenter.PreviewAsync(_previewParameters, cancellation.Token);
                if (IsDisposed || cancellation.IsCancellationRequested) return;
                _layouts = layouts.Where(r => r.Success && r.IsPrintable).ToList();
                _selectedLayout = _layouts.FirstOrDefault(r => previous != null && r.Rows == previous.Rows && r.Columns == previous.Columns && r.RotationAngle == previous.RotationAngle)
                    ?? layouts.FirstOrDefault();
                _calculating = false; UpdateResult();
            }
            catch (OperationCanceledException) { }
        }

        private void UpdateResult()
        {
            bool valid = _selectedLayout?.Success == true && _selectedLayout.IsPrintable;
            string validation = _presenter.IsGlobal ? _presenter.ValidateDefaults() : null;
            _apply.Enabled = !_readingFile && !_calculating && (_presenter.IsGlobal ? validation == null : !_presenter.Enabled || valid);
            _source.Text = _presenter.Source;

            _previewTitle.Text = _presenter.IsGlobal
                ? "大版规格与有效印刷区图示"
                : ("排版生产预览 · " + (_presenter.Current.Strategy == ImpositionLayoutStrategy.Multiple ? "倍数约束" : "真实比例"));

            _legend.IsGlobal = _presenter.IsGlobal;

            // 关键：在设置模式下，纯净展示大版安全区图示，不传 layoutResult，画布绝不画 1..8 假格子！
            _canvas.LayoutResult = (!_presenter.IsGlobal && _presenter.Enabled && valid) ? _selectedLayout : null;
            _canvas.Geometry = _previewParameters != null ? ImpositionEditorPresenter.Geometry(_previewParameters, IsFlatSheet, _presenter.IsGlobal ? null : _selectedLayout) : null;
            _canvas.EmptyText = !_presenter.Enabled ? "排版已停用 · 草稿保留" : "暂无可用布局";

            var geometry = _canvas.Geometry;
            if (geometry != null)
            {
                _footerPanel.IsGlobal = _presenter.IsGlobal;
                _footerPanel.Valid = valid;
                _footerPanel.EnabledMode = _presenter.Enabled;
                _footerPanel.PaperWidth = geometry.Width;
                _footerPanel.PaperHeight = geometry.Height;
                _footerPanel.PrintableWidth = geometry.PrintableWidth;
                _footerPanel.PrintableHeight = geometry.PrintableHeight;
                _footerPanel.MarginLeft = _previewParameters.MarginLeft;
                _footerPanel.MarginRight = _previewParameters.MarginRight;
                _footerPanel.MarginTop = _previewParameters.MarginTop;
                _footerPanel.MarginBottom = _previewParameters.MarginBottom;
                _footerPanel.SpacingX = _previewParameters.SpacingX;
                _footerPanel.SpacingY = _previewParameters.SpacingY;

                if (_presenter.IsGlobal)
                {
                    _footerPanel.StatusText = validation ?? "大版规格就绪";
                    _footerPanel.IsError = validation != null;
                    _status.Text = validation ?? "大版规格就绪";
                    _summary.Text = $"大版有效印刷安全区: {geometry.PrintableWidth:0.##} × {geometry.PrintableHeight:0.##} mm";
                }
                else if (valid && _presenter.Enabled)
                {
                    float taskW = _presenter.PdfInfo?.GetEffectivePageWidth() ?? 0;
                    float taskH = _presenter.PdfInfo?.GetEffectivePageHeight() ?? 0;
                    _footerPanel.Quantity = _selectedLayout.Rows * _selectedLayout.Columns;
                    _footerPanel.ProductWidth = taskW;
                    _footerPanel.ProductHeight = taskH;
                    _footerPanel.Rows = _selectedLayout.Rows;
                    _footerPanel.Columns = _selectedLayout.Columns;
                    _footerPanel.GridWidth = geometry.GridWidth;
                    _footerPanel.GridHeight = geometry.GridHeight;
                    _footerPanel.StatusText = "排版已就绪";
                    _footerPanel.IsError = false;

                    _status.Text = "排版已就绪";
                    _summary.Text = $"{_footerPanel.Quantity} 个/纸   📄 成品: {taskW:0.##} × {taskH:0.##} mm   {_selectedLayout.Rows} 行 × {_selectedLayout.Columns} 列 · 居中真实对齐";
                    _offsets.Text = $"成品间距: 水平 {_previewParameters.SpacingX:0.##}mm · 垂直 {_previewParameters.SpacingY:0.##}mm | 网格尺寸: {geometry.GridWidth:0.##} × {geometry.GridHeight:0.##} mm | 安全区: {geometry.PrintableWidth:0.##} × {geometry.PrintableHeight:0.##} mm";
                }
                else
                {
                    _footerPanel.StatusText = !_presenter.Enabled ? "排版停用" : (_selectedLayout?.ErrorMessage ?? "未取得成品尺寸，不能应用排版。请选择文件后重试。");
                    _footerPanel.IsError = true;
                    _status.Text = _footerPanel.StatusText;
                    _summary.Text = "— × —";
                }
            }

            _footerPanel.Invalidate();
            _legend.Invalidate();
            _fix.Visible = !valid && _presenter.Enabled && !_presenter.IsGlobal && _presenter.Current.Strategy != ImpositionLayoutStrategy.Automatic;
            foreach (var button in _viewButtons) button.Enabled = valid && _presenter.Enabled;
            Arrange(); _canvas.Invalidate();
        }

        public bool TryApply(out string error)
        {
            CaptureDraft();
            if (_readingFile || (!_presenter.IsGlobal && _calculating)) { error = "正在更新参数，请稍候。"; return false; }
            if (!_presenter.TrySubmit(_selectedLayout, out var value, out error))
            {
                _status.Text = error; _status.ForeColor = Color.Firebrick;
                _footerPanel.StatusText = error; _footerPanel.IsError = true; _footerPanel.Invalidate();
                return false;
            }
            Parameters = value;
            _draftState.Text = _presenter.IsGlobal ? "默认参数已保存" : "已应用本次参数";
            Committed?.Invoke(this, EventArgs.Empty); return true;
        }

        private async Task ReadPdfAsync(bool paper)
        {
            using (var dialog = new OpenFileDialog { Filter = "PDF 文件 (*.pdf)|*.pdf", Title = paper ? "选择纸张尺寸 PDF" : "选择预览参考 PDF" })
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                _fileCancellation?.Cancel(); _fileCancellation?.Dispose();
                var cancellation = _fileCancellation = new CancellationTokenSource();
                _readingFile = true; _apply.Enabled = false;
                _fileError = null; _parameters.Enabled = false;
                _flat.Enabled = _roll.Enabled = _importPaper.Enabled = false;
                _status.Text = "正在读取 PDF 尺寸…";
                try
                {
                    string error = await _presenter.ReadPdfAsync(dialog.FileName, paper, cancellation.Token);
                    if (IsDisposed || cancellation.IsCancellationRequested) return;
                    if (error != null) _fileError = error + " 原尺寸已保留。";
                    else { BindDraft(); await RefreshPreviewAsync(); }
                }
                catch (OperationCanceledException) { }
                finally
                {
                    if (!IsDisposed)
                    {
                        _readingFile = false;
                        _parameters.Enabled = true;
                        _flat.Enabled = _roll.Enabled = _importPaper.Enabled = true;
                        UpdateResult();
                    }
                }
            }
        }

        private void Arrange()
        {
            if (_fields.Count == 0) return;
            float dpi = DeviceDpi / 96F;
            int w = (int)(ClientSize.Width / dpi), h = (int)(ClientSize.Height / dpi);
            void Bounds(Control c, int x, int y, int width, int height) => c.SetBounds((int)(x * dpi), (int)(y * dpi), Math.Max(1, (int)(width * dpi)), Math.Max(1, (int)(height * dpi)));

            int inner = Math.Max(300, w - 20), bodyY = 38, footer = h - 36;
            int bodyH = Math.Max(250, footer - bodyY - 4), leftW = (int)(inner * 0.45) - 4;
            int previewW = inner - leftW - 12;

            // 顶栏标题与状态
            Bounds(_title, 10, 2, inner, 18);
            Bounds(_enabled, 10, 19, 115, 18);
            Bounds(_origin, 130, 19, inner - 135, 18);

            Bounds(_parameters, 10, bodyY, leftW, bodyH);
            Bounds(_preview, leftW + 22, bodyY, previewW, bodyH);

            // 1. 材料切换卡片 (精准置于左侧参数区顶部，按钮高 26px)
            Bounds(_materialPanel, 0, 0, leftW, 34);
            Bounds(_segmentCapsule, 3, 2, leftW - 6, 28);
            Bounds(_flat, 2, 2, (leftW - 10) / 2, 24);
            Bounds(_roll, (leftW - 10) / 2 + 2, 2, (leftW - 10) / 2, 24);

            // 2. 大版规格与边距、间距卡片 (所有输入框高度达到舒展饱满的 31px！)
            int specY = 38;
            int specH = 202;
            Bounds(_specs, 0, specY, leftW, specH);
            Bounds(_specTitle, 8, 5, leftW - 84, 16);
            Bounds(_resetSpecs, leftW - 76, 5, 70, 16);

            int half = (leftW - 20) / 2;
            // 纸张宽/高两行布局：上标签15px，下输入框饱满整整 31px！
            Bounds(_fields["width"], 8, 23, half, 48);
            Bounds(_fields["length"], half + 12, 23, half, 48);

            Bounds(_marginsTitle, 8, 73, leftW - 16, 14);
            for (int i = 0; i < 4; i++)
            {
                string key = new[] { "left", "right", "top", "bottom" }[i];
                // 四周边距输入框：每个饱满 31px 高！
                Bounds(_fields[key], 8 + i * (leftW - 16) / 4, 89, (leftW - 28) / 4, 31);
            }

            Bounds(_spacingTitle, 8, 122, leftW - 64, 14);
            Bounds(_zeroSpacing, leftW - 54, 122, 46, 14);
            // 拼版间距输入框：每个饱满 31px 高！
            Bounds(_fields["spacingX"], 8, 138, half, 31);
            Bounds(_fields["spacingY"], half + 12, 138, half, 31);

            if (IsFlatSheet)
            {
                Bounds(_lengthHelp, 8, 175, leftW - 86, 20);
                Bounds(_importPaper, leftW - 78, 175, 70, 20);
            }
            else
            {
                Bounds(_lengthHelp, 8, 175, leftW - 16, 20);
            }

            // 3. 页面方向偏好卡片 (按钮高度 28px)
            int dirY = specY + specH + 4;
            int dirH = 52;
            Bounds(_directionPanel, 0, dirY, leftW, dirH);
            Bounds(_directionPanel.Controls.Find("rotationTitle", false)[0], 8, 3, leftW - 16, 15);
            for (int i = 0; i < 3; i++)
                Bounds(_rotations[i], 8 + i * (leftW - 16) / 3, 20, (leftW - 24) / 3, 28);

            // 4. 排版方式卡片
            int stratY = dirY + dirH + 4;
            int stratH = Math.Max(1, bodyH - stratY);
            Bounds(_strategy, 0, stratY, leftW, stratH);
            Bounds(_strategy.Controls.Find("strategyTitle", false)[0], 8, 3, leftW - 16, 15);

            Bounds(_strategies[0], 8, 20, (leftW - 20) / 2, 28);
            Bounds(_strategies[1], half + 12, 20, (leftW - 20) / 2, 28);

            var mode = _presenter.Current.Strategy;
            bool multiple = mode == ImpositionLayoutStrategy.Multiple;

            _fields["rows"].Visible = _fields["columns"].Visible = !multiple;
            _autoRows.Visible = _autoColumns.Visible = false;
            _fields["multiple"].Visible = _direction.Visible = multiple;
            _resetAutoGrid.Visible = !multiple;

            int subpanelY = 52;
            int subpanelH = Math.Max(1, stratH - subpanelY - 4);
            Bounds(_strategySubpanel, 6, subpanelY, leftW - 12, subpanelH);

            int subHalf = (leftW - 28) / 2;
            if (!multiple)
            {
                if (subpanelH >= 48)
                {
                    // 行数、列数两行布局：上标签15px，下输入框整整 31px 饱满大方！
                    _fields["rows"].Visible = _fields["columns"].Visible = true;
                    _fields["rows"].Vertical = _fields["columns"].Vertical = true;
                    Bounds(_fields["rows"], 6, 2, subHalf, 48);
                    Bounds(_fields["columns"], subHalf + 8, 2, subHalf, 48);
                }
                else if (subpanelH >= 24)
                {
                    _fields["rows"].Visible = _fields["columns"].Visible = true;
                    _fields["rows"].Vertical = _fields["columns"].Vertical = false;
                    Bounds(_fields["rows"], 6, 2, subHalf, 24);
                    Bounds(_fields["columns"], subHalf + 8, 2, subHalf, 24);
                }
                else
                {
                    _fields["rows"].Visible = _fields["columns"].Visible = false;
                }

                string rText = TextOf("rows"), cText = TextOf("columns");
                bool isAllAuto = (string.IsNullOrEmpty(rText) || rText == "0") &&
                                 (string.IsNullOrEmpty(cText) || cText == "0");
                _strategyHelp.Text = isAllAuto ? "💡 当前全自动择优：行列为0，自动计算最高利用率组合。" : "💡 当前指定行列：输入 0 可切换为自适应。";

                if (subpanelH >= 70)
                {
                    _strategyHelp.Visible = true;
                    _resetAutoGrid.Visible = true;
                    Bounds(_strategyHelp, 6, 52, leftW - 80, 16);
                    Bounds(_resetAutoGrid, leftW - 12 - 62, 52, 56, 16);
                }
                else
                {
                    _strategyHelp.Visible = false;
                    _resetAutoGrid.Visible = false;
                }
            }
            else
            {
                if (subpanelH >= 48)
                {
                    // 倍数两行布局：输入框高 31px！方向下拉框高 28px！
                    _fields["multiple"].Visible = _direction.Visible = true;
                    _fields["multiple"].Vertical = true;
                    Bounds(_fields["multiple"], 6, 2, subHalf, 48);
                    Bounds(_direction, subHalf + 8, 20, subHalf, 28);
                }
                else if (subpanelH >= 24)
                {
                    _fields["multiple"].Visible = _direction.Visible = true;
                    _fields["multiple"].Vertical = false;
                    Bounds(_fields["multiple"], 6, 2, subHalf, 24);
                    Bounds(_direction, subHalf + 8, 2, subHalf, 24);
                }
                else
                {
                    _fields["multiple"].Visible = _direction.Visible = false;
                }

                _strategyHelp.Text = "💡 倍数约束：排版时严格保证当前方向数量为 N 的整倍数。";
                if (subpanelH >= 70)
                {
                    _strategyHelp.Visible = true;
                    Bounds(_strategyHelp, 6, 52, leftW - 20, 16);
                }
                else
                {
                    _strategyHelp.Visible = false;
                }
            }

            // 右侧预览与图示区
            int legendW = _presenter.IsGlobal ? 150 : 235;
            Bounds(_previewTitle, 8, 2, previewW - legendW - 14, 20);
            Bounds(_legend, previewW - legendW - 6, 2, legendW, 20);

            Bounds(_viewButtons[0], 8, 24, 26, 22);
            Bounds(_viewButtons[1], 36, 24, 26, 22);
            Bounds(_viewButtons[2], previewW - 150, 24, 68, 22);
            Bounds(_viewButtons[3], previewW - 78, 24, 70, 22);

            bool pan = _canvas.Detail || _canvas.Zoom > 1;
            _position.Visible = pan;
            bool horizontalPan = _canvas.Zoom > 1;
            _horizontalPosition.Visible = _horizontalCaption.Visible = horizontalPan;
            _positionCaption.Visible = pan;
            Mark(_viewButtons[2], !_canvas.Detail && _canvas.Zoom == 1);
            Mark(_viewButtons[3], _canvas.Detail);

            int footerH = 46;
            int footerY = bodyH - footerH;

            Bounds(_canvas, 8, 50, previewW - 16, Math.Max(60, footerY - 54 - (pan ? horizontalPan ? 50 : 26 : 0)));
            Bounds(_positionCaption, 8, footerY - 26, 34, 22);
            Bounds(_position, 44, footerY - 26, previewW - 52, 22);
            Bounds(_horizontalCaption, 8, footerY - 50, 34, 22);
            Bounds(_horizontalPosition, 44, footerY - 50, previewW - 52, 22);

            Bounds(_footerPanel, 8, footerY, previewW - 16, footerH);

            Bounds(_restore, 10, footer, 130, 28);
            Bounds(_automatic, 146, footer, 110, 28);
            Bounds(_draftState, w - 380, footer, 150, 28);
            Bounds(_cancel, w - 215, footer, 85, 28);
            Bounds(_apply, w - 122, footer, 112, 28);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _previewCancellation?.Cancel(); _previewCancellation?.Dispose();
                _fileCancellation?.Cancel(); _fileCancellation?.Dispose();
                _previewCancellation = null; _fileCancellation = null;
            }
            base.Dispose(disposing);
        }
    }
}
