using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.UI;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Main
{
    /// <summary>
    /// 预设管理现代化工作台 (Preset Manager Pro)
    /// 1:1 像素级复刻极简属性检查器架构设计原型 (docs/预设管理全属性交互设计原型.html)
    /// 三栏架构：左侧分组导轨 + 中间预设卡片矩阵与批量操作 + 右侧极简双列工位舱属性检查器
    /// 100% 原生矢量矢量图标引擎，全量支持多选批量移动分组、全属性跟随控制与大版参数联动
    /// </summary>
    public class PresetManagementForm : Form
    {
        // -------------------------------------------------------------
        // UI 主体容器
        // -------------------------------------------------------------
        private Panel _pnlHeader;
        private Panel _pnlBody;
        private Panel _pnlFooter;

        // 顶栏组件
        private TextBox _txtSearch;
        private Panel _pnlSearchBox;

        // 栏 1: 分组导轨 (180px)
        private Panel _pnlGroupSidebar;
        private FlowLayoutPanel _flpGroupList;
        private string _activeGroupName = AppSettings.LastSelectedPresetGroup ?? "未命名";
        private List<string> _groups = new List<string> { "未命名" };

        // 栏 2: 预设列表矩阵 (360px)
        private Panel _pnlPresetDeck;
        private Label _lblDeckCurrentGroup;
        private CheckBox _chkSelectAll;
        private FlowLayoutPanel _flpPresetScroll;
        private Panel _pnlBatchFloatingBar;
        private Label _lblBatchCount;
        private AntdUI.Button _btnMoveGroup;
        private Panel _pnlBatchGroupPopup; // 原生内嵌浮层，彻底告别 WinForms 菜单闪退与错位

        // 栏 3: 右侧极简属性检查器 (~500px)
        private Panel _pnlInspector;
        private FlowLayoutPanel _pnlInspectorStack;

        // 顶层轻量身份栏
        private AntdUI.Input _txtPresetName;
        private AntdUI.Select _cmbGroupName;
        private CheckBox _chkShowInButtons;

        // 舱 1：材料与工艺舱 (Material & Finish)
        private AntdUI.Select _selMaterial;
        private AntdUI.Select _selColorMode;
        private AntdUI.Select _selFilmType;
        private AntdUI.Select _selShapeState;
        private AntdUI.InputNumber _numRoundRadius;
        private CheckBox _chkAddIdentifierPage;

        // 舱 2：订单与编号规则舱 (Order Number Mode)
        private AntdUI.Select _selOrderNumberMode;
        private AntdUI.Select _selOrderRegexName;

        // 舱 3：排版开料与倍数舱 (Imposition & Duplicate)
        private AntdUI.Select _selEnableImposition;
        private AntdUI.InputNumber _numTetBleed;
        private AntdUI.Select _selMaterialType;
        private AntdUI.Select _selLayoutMode;
        private CheckBox _chkIsDualCopy;
        private Panel _pnlDualCopySub;
        private AntdUI.InputNumber _numCopyCount;
        private AntdUI.Select _selCopyType;
        private AntdUI.Select _selCopyMode;

        // 嵌入式大版排版参数工位 (平张与卷装两种设置分开独立启动)
        private Panel _pnlEmbeddedImpo;
        private CheckBox _chkEnableFlatImpo;
        private Label _lblFlatImpoHint;
        private CheckBox _chkEnableRollImpo;
        private Label _lblRollImpoHint;

        // 平张大版参数控件
        private AntdUI.InputNumber _numFlatPaperWidth;
        private AntdUI.InputNumber _numFlatPaperHeight;
        private AntdUI.InputNumber _numFlatMarginTop;
        private AntdUI.InputNumber _numFlatMarginBottom;
        private AntdUI.InputNumber _numFlatMarginLeft;
        private AntdUI.InputNumber _numFlatMarginRight;

        // 卷装大版参数控件
        private AntdUI.InputNumber _numRollFixedWidth;
        private AntdUI.InputNumber _numRollMinLength;
        private AntdUI.InputNumber _numRollMarginTop;
        private AntdUI.InputNumber _numRollMarginBottom;
        private AntdUI.InputNumber _numRollMarginLeft;
        private AntdUI.InputNumber _numRollMarginRight;

        // 舱 4：目标导出路径舱 (Export Path)
        private AntdUI.Input _txtExportPath;
        private AntdUI.Button _btnBrowseExportPath;

        // 跟随状态锁存储字典 (参数标志 -> 锁标控件)
        private readonly Dictionary<PresetIgnoreOptions, MicroFollowLatch> _latchMap = new Dictionary<PresetIgnoreOptions, MicroFollowLatch>();

        // 数据状态
        private List<MaterialSelectionPreset> _presets;
        private MaterialSelectionPreset _activePreset;
        private readonly HashSet<MaterialSelectionPreset> _checkedPresets = new HashSet<MaterialSelectionPreset>();
        private bool _isUpdatingUi = false;

        // 设计规范调色板 (严格对齐 HTML 原型 CSS 变量)
        private static readonly Color BgColor = Color.FromArgb(242, 245, 248);           // var(--bg) #f2f5f8
        private static readonly Color SurfaceColor = Color.White;                         // var(--surface) #ffffff
        private static readonly Color BorderColor = Color.FromArgb(212, 219, 228);        // var(--border) #d4dbe4
        private static readonly Color BorderLightColor = Color.FromArgb(230, 235, 242);   // var(--border-light) #e6ebf2
        private static readonly Color TextMainColor = Color.FromArgb(31, 36, 41);          // var(--text-main) #1f2429
        private static readonly Color TextSecondaryColor = Color.FromArgb(75, 82, 93);     // var(--text-secondary) #4b525d
        private static readonly Color TextMutedColor = Color.FromArgb(140, 149, 160);      // var(--text-muted) #8c95a0
        private static readonly Color PrimaryColor = Color.FromArgb(22, 119, 255);         // var(--primary) #1677ff
        private static readonly Color PrimaryLightColor = Color.FromArgb(230, 244, 255);    // var(--primary-light) #e6f4ff
        private static readonly Color PrimaryBorderColor = Color.FromArgb(145, 202, 255);  // var(--primary-border) #91caff

        public PresetManagementForm()
        {
            InitializeComponent();
            LoadPresets();
        }

        private void InitializeComponent()
        {
            this.Text = "材料预设管理与配置中心";
            this.Size = new Size(1060, 620);
            this.MinimumSize = new Size(1000, 580);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BgColor;
            this.Font = new Font("Microsoft YaHei UI", 9F);

            // 1. 顶栏 (44px)
            BuildHeader();

            // 2. 底栏 (48px)
            BuildFooter();

            // 3. 主体三栏容器
            _pnlBody = new Panel
            {
                Name = "_pnlBody",
                Dock = DockStyle.Fill,
                BackColor = BgColor
            };

            // 栏 3: 右侧极简属性检查器 (~500px)
            BuildInspectorPane();

            // 栏 2: 中间预设卡片矩阵 (360px)
            BuildPresetDeck();

            // 栏 1: 左侧分组导轨 (180px)
            BuildGroupSidebar();

            _pnlBody.Controls.Add(_pnlInspector);
            _pnlBody.Controls.Add(_pnlPresetDeck);
            _pnlBody.Controls.Add(_pnlGroupSidebar);

            this.Controls.Add(_pnlBody);
            this.Controls.Add(_pnlHeader);
            this.Controls.Add(_pnlFooter);
        }

        // =============================================================
        // 1. 顶栏 (44px) —— 严格对齐 HTML .win-header
        // =============================================================

        private void BuildHeader()
        {
            _pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = SurfaceColor,
                Padding = new Padding(16, 0, 16, 0)
            };
            _pnlHeader.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor, 1F))
                    e.Graphics.DrawLine(pen, 0, _pnlHeader.Height - 1, _pnlHeader.Width, _pnlHeader.Height - 1);
            };

            // 标题与图标
            var pnlTitleGroup = new Panel
            {
                Dock = DockStyle.Left,
                Width = 420,
                BackColor = Color.Transparent
            };
            pnlTitleGroup.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawVectorIcon(e.Graphics, "gear", new Rectangle(0, 13, 18, 18), PrimaryColor);
                using (var font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(e.Graphics, "材料预设管理与配置中心 (极简检查器架构)", font, new Point(24, 12), TextMainColor);
                }
            };

            // 搜索胶囊框右侧容器 (260 × 44px)
            var pnlSearchWrap = new Panel
            {
                Dock = DockStyle.Right,
                Width = 260,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 8, 8, 8)
            };

            _pnlSearchBox = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgColor
            };
            _pnlSearchBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, _pnlSearchBox.Width - 1, _pnlSearchBox.Height - 1), 14))
                {
                    using (var brush = new SolidBrush(BgColor))
                        e.Graphics.FillPath(brush, path);
                    using (var pen = new Pen(BorderColor, 1F))
                        e.Graphics.DrawPath(pen, path);
                }
                DrawVectorIcon(e.Graphics, "search", new Rectangle(8, 6, 14, 14), TextMutedColor);
                if (string.IsNullOrEmpty(_txtSearch.Text))
                {
                    using (var font = new Font("Microsoft YaHei UI", 8.5F))
                    {
                        TextRenderer.DrawText(e.Graphics, "按名称、材料、工艺搜索预设...", font, new Point(28, 5), TextMutedColor);
                    }
                }
            };

            _txtSearch = new TextBox
            {
                Location = new Point(28, 5),
                Size = new Size(204, 18),
                BorderStyle = BorderStyle.None,
                BackColor = BgColor,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = TextMainColor
            };
            _txtSearch.TextChanged += (s, e) =>
            {
                _pnlSearchBox.Invalidate();
                FilterPresetCards(_txtSearch.Text);
            };
            _pnlSearchBox.Controls.Add(_txtSearch);
            pnlSearchWrap.Controls.Add(_pnlSearchBox);

            _pnlHeader.Controls.Add(pnlSearchWrap);
            _pnlHeader.Controls.Add(pnlTitleGroup);
        }

        // =============================================================
        // 2. 底栏 (48px) —— 严格对齐 HTML .win-footer
        // =============================================================

        private void BuildFooter()
        {
            _pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                BackColor = SurfaceColor,
                Padding = new Padding(16, 9, 16, 9)
            };
            _pnlFooter.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor, 1F))
                    e.Graphics.DrawLine(pen, 0, 0, _pnlFooter.Width, 0);
            };

            var lblHint = new Label
            {
                Text = "💡 规则极简感知：每项右上角的 [⚡跟随] 表示应用预设时覆盖工单；[🔒保持] 表示保留工单既有值不变。",
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = TextMutedColor,
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pnlBtns = new Panel
            {
                Dock = DockStyle.Right,
                Width = 210,
                BackColor = Color.Transparent
            };

            var btnCancel = new AntdUI.Button
            {
                Text = "关闭",
                Size = new Size(80, 30),
                Dock = DockStyle.Right,
                Radius = 4,
                Type = AntdUI.TTypeMini.Default,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            btnCancel.Click += (s, e) => this.Close();

            var btnApply = new AntdUI.Button
            {
                Text = "保存并应用",
                Size = new Size(110, 30),
                Dock = DockStyle.Left,
                Radius = 4,
                Type = AntdUI.TTypeMini.Primary,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
            btnApply.Click += (s, e) =>
            {
                CommitActivePresetChanges();
                SaveAllPresetsToStorage();
                AntdUI.Message.success(this, "预设与分组变更已全部成功同步！");
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            pnlBtns.Controls.Add(btnCancel);
            pnlBtns.Controls.Add(btnApply);

            _pnlFooter.Controls.Add(lblHint);
            _pnlFooter.Controls.Add(pnlBtns);
        }

        // =============================================================
        // 栏 1: 左侧分组导轨 (180px) —— 严格对齐 HTML .group-sidebar
        // =============================================================

        private void BuildGroupSidebar()
        {
            _pnlGroupSidebar = new Panel
            {
                Name = "_pnlGroupSidebar",
                Dock = DockStyle.Left,
                Width = 180,
                BackColor = SurfaceColor,
                Padding = new Padding(0)
            };
            _pnlGroupSidebar.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor, 1F))
                    e.Graphics.DrawLine(pen, _pnlGroupSidebar.Width - 1, 0, _pnlGroupSidebar.Width - 1, _pnlGroupSidebar.Height);
            };

            // 分组头部 (38px)
            var pnlGroupHead = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = SurfaceColor,
                Padding = new Padding(12, 6, 8, 6)
            };
            pnlGroupHead.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderLightColor, 1F))
                    e.Graphics.DrawLine(pen, 0, pnlGroupHead.Height - 1, pnlGroupHead.Width, pnlGroupHead.Height - 1);
            };

            var lblHead = new Label
            {
                Text = "预设分组",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = TextSecondaryColor,
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var btnAddGroup = new AntdUI.Button
            {
                Text = "＋ 新建组",
                Dock = DockStyle.Right,
                Width = 66,
                Height = 22,
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold)
            };
            btnAddGroup.Click += (s, e) => PromptCreateNewGroup();

            pnlGroupHead.Controls.Add(lblHead);
            pnlGroupHead.Controls.Add(btnAddGroup);

            // 分组列表
            _flpGroupList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = SurfaceColor,
                Padding = new Padding(8, 8, 8, 8)
            };

            // 底部业务提示卡片
            var pnlTip = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = SurfaceColor,
                Padding = new Padding(10, 8, 10, 8)
            };
            pnlTip.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderLightColor, 1F))
                    e.Graphics.DrawLine(pen, 0, 0, pnlTip.Width, 0);
            };
            var lblTipText = new Label
            {
                Text = "💡 提示：分组直接映射到 V5 主界面上方预设 Tab 的分类标签与排序。",
                Font = new Font("Microsoft YaHei UI", 7.5F),
                ForeColor = TextMutedColor,
                Dock = DockStyle.Fill
            };
            pnlTip.Controls.Add(lblTipText);

            _pnlGroupSidebar.Controls.Add(_flpGroupList);
            _pnlGroupSidebar.Controls.Add(pnlTip);
            _pnlGroupSidebar.Controls.Add(pnlGroupHead);
        }

        // =============================================================
        // 栏 2: 中间预设列表矩阵 (360px) —— 严格对齐 HTML .preset-center-deck
        // =============================================================

        private void BuildPresetDeck()
        {
            _pnlPresetDeck = new Panel
            {
                Name = "_pnlPresetDeck",
                Dock = DockStyle.Left,
                Width = 360,
                BackColor = BgColor,
                Padding = new Padding(0)
            };
            _pnlPresetDeck.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderColor, 1F))
                    e.Graphics.DrawLine(pen, _pnlPresetDeck.Width - 1, 0, _pnlPresetDeck.Width - 1, _pnlPresetDeck.Height);
            };

            // 顶部操作头 (38px)
            var pnlDeckHead = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = SurfaceColor,
                Padding = new Padding(12, 6, 12, 6)
            };
            pnlDeckHead.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderLightColor, 1F))
                    e.Graphics.DrawLine(pen, 0, pnlDeckHead.Height - 1, pnlDeckHead.Width, pnlDeckHead.Height - 1);
            };

            _chkSelectAll = new CheckBox
            {
                Location = new Point(12, 11),
                Size = new Size(16, 16),
                Cursor = Cursors.Hand
            };
            _chkSelectAll.CheckedChanged += (s, e) => ToggleSelectAllCards(_chkSelectAll.Checked);

            _lblDeckCurrentGroup = new Label
            {
                Name = "_lblDeckCurrentGroup",
                Location = new Point(34, 9),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = TextMainColor,
                Text = "全选 · 未命名"
            };

            var btnMoveUp = new AntdUI.Button
            {
                Text = "↑",
                Location = new Point(148, 7),
                Size = new Size(24, 24),
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold)
            };
            btnMoveUp.Click += (s, e) => MovePresetRelative(_activePreset, true);

            var btnMoveDown = new AntdUI.Button
            {
                Text = "↓",
                Location = new Point(176, 7),
                Size = new Size(24, 24),
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold)
            };
            btnMoveDown.Click += (s, e) => MovePresetRelative(_activePreset, false);

            var btnSort = new AntdUI.Button
            {
                Text = "排序 ▾",
                Location = new Point(204, 7),
                Size = new Size(60, 24),
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 8F)
            };
            btnSort.Click += (s, e) => ShowSortContextMenu(btnSort);

            var btnNewPreset = new AntdUI.Button
            {
                Text = "＋ 新建预设",
                Location = new Point(268, 7),
                Size = new Size(78, 24),
                Type = AntdUI.TTypeMini.Primary,
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold)
            };
            btnNewPreset.Click += (s, e) => PromptCreateNewPreset();

            pnlDeckHead.Controls.Add(_chkSelectAll);
            pnlDeckHead.Controls.Add(_lblDeckCurrentGroup);
            pnlDeckHead.Controls.Add(btnMoveUp);
            pnlDeckHead.Controls.Add(btnMoveDown);
            pnlDeckHead.Controls.Add(btnSort);
            pnlDeckHead.Controls.Add(btnNewPreset);

            // 预设卡片滚动面板
            _flpPresetScroll = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = BgColor,
                Padding = new Padding(10, 8, 10, 8)
            };

            // 原生批量操作底栏 (始终显示，统一浅色 Ant Design 风格)
            BuildBatchFloatingBar();

            // 原生内嵌批量移动分组弹层 (彻底告别 ContextMenuStrip 错位)
            BuildBatchGroupPopup();

            _pnlPresetDeck.Controls.Add(_flpPresetScroll);
            _pnlPresetDeck.Controls.Add(_pnlBatchFloatingBar);
            _pnlPresetDeck.Controls.Add(pnlDeckHead);
            _pnlPresetDeck.Controls.Add(_pnlBatchGroupPopup);

            _pnlPresetDeck.Resize += (s, e) =>
            {
                if (_pnlBatchFloatingBar != null && _pnlBatchGroupPopup != null && _pnlBatchGroupPopup.Visible)
                {
                    _pnlBatchGroupPopup.Top = _pnlBatchFloatingBar.Top - _pnlBatchGroupPopup.Height - 4;
                }
            };
        }

        private void BuildBatchFloatingBar()
        {
            _pnlBatchFloatingBar = new Panel
            {
                Name = "_pnlBatchFloatingBar",
                Dock = DockStyle.Bottom,
                Height = 42,
                BackColor = SurfaceColor,
                Visible = true
            };
            _pnlBatchFloatingBar.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderLightColor, 1F))
                    e.Graphics.DrawLine(pen, 0, 0, _pnlBatchFloatingBar.Width, 0);
            };

            _lblBatchCount = new Label
            {
                Location = new Point(8, 12),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = TextMutedColor,
                Text = "已选 0 项"
            };

            _btnMoveGroup = new AntdUI.Button
            {
                IconSvg = "FolderOutlined",
                Text = " 批量移动分组 ▾",
                Location = new Point(68, 7),
                Size = new Size(130, 28),
                Radius = 4,
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                Cursor = Cursors.Hand
            };
            _btnMoveGroup.Click += (s, e) => ToggleBatchGroupPopup();

            var btnBatchPin = new AntdUI.Button
            {
                IconSvg = "PushpinOutlined",
                Text = " 批量上屏",
                Location = new Point(204, 7),
                Size = new Size(82, 28),
                Radius = 4,
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                Cursor = Cursors.Hand
            };
            btnBatchPin.Click += (s, e) => ExecuteBatchTogglePin(true);

            var btnBatchDelete = new AntdUI.Button
            {
                IconSvg = "DeleteOutlined",
                Text = " 删除",
                Location = new Point(292, 7),
                Size = new Size(60, 28),
                Radius = 4,
                Type = AntdUI.TTypeMini.Error,
                Ghost = true,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                Cursor = Cursors.Hand
            };
            btnBatchDelete.Click += (s, e) => ExecuteBatchDelete();

            _pnlBatchFloatingBar.Controls.Add(_lblBatchCount);
            _pnlBatchFloatingBar.Controls.Add(_btnMoveGroup);
            _pnlBatchFloatingBar.Controls.Add(btnBatchPin);
            _pnlBatchFloatingBar.Controls.Add(btnBatchDelete);
        }

        private void BuildBatchGroupPopup()
        {
            _pnlBatchGroupPopup = new Panel
            {
                Size = new Size(190, 185),
                Location = new Point(52, 240),
                BackColor = SurfaceColor,
                Visible = false
            };
            _pnlBatchGroupPopup.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, _pnlBatchGroupPopup.Width - 1, _pnlBatchGroupPopup.Height - 1), 6))
                {
                    using (var brush = new SolidBrush(SurfaceColor))
                        e.Graphics.FillPath(brush, path);
                    using (var pen = new Pen(BorderColor, 1F))
                        e.Graphics.DrawPath(pen, path);
                }
            };
        }

        private void ToggleBatchGroupPopup()
        {
            if (_pnlBatchGroupPopup.Visible)
            {
                _pnlBatchGroupPopup.Visible = false;
                return;
            }

            // 动态填充所有可用分组并精确渲染
            _pnlBatchGroupPopup.Controls.Clear();

            int itemH = 28;
            int totalH = 28 + (_groups.Count + 1) * itemH + 6;
            _pnlBatchGroupPopup.Width = 190;
            _pnlBatchGroupPopup.Height = Math.Min(260, totalH);
            _pnlBatchGroupPopup.Top = _pnlBatchFloatingBar.Top - _pnlBatchGroupPopup.Height - 4;
            _pnlBatchGroupPopup.Left = 52;

            var lblHeader = new Label
            {
                Text = "移动到目标分组:",
                Location = new Point(8, 6),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8F),
                ForeColor = TextMutedColor
            };
            _pnlBatchGroupPopup.Controls.Add(lblHeader);

            int curY = 24;
            foreach (var g in _groups)
            {
                string target = g;
                var pnlBtn = new Panel
                {
                    Location = new Point(4, curY),
                    Size = new Size(_pnlBatchGroupPopup.Width - 8, itemH),
                    Cursor = Cursors.Hand,
                    BackColor = SurfaceColor
                };

                pnlBtn.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    DrawVectorIcon(e.Graphics, "folder", new Rectangle(6, 7, 14, 14), PrimaryColor);
                    using (var font = new Font("Microsoft YaHei UI", 8.5F))
                    {
                        TextRenderer.DrawText(e.Graphics, target, font, new Point(24, 5), TextMainColor);
                    }
                };

                pnlBtn.MouseEnter += (s, e) => pnlBtn.BackColor = PrimaryLightColor;
                pnlBtn.MouseLeave += (s, e) => pnlBtn.BackColor = SurfaceColor;
                pnlBtn.Click += (s, e) =>
                {
                    _pnlBatchGroupPopup.Visible = false;
                    ExecuteBatchMoveGroup(target);
                };

                _pnlBatchGroupPopup.Controls.Add(pnlBtn);
                curY += itemH;
            }

            // 新建分组并移动项
            var pnlNew = new Panel
            {
                Location = new Point(4, curY),
                Size = new Size(_pnlBatchGroupPopup.Width - 8, itemH),
                Cursor = Cursors.Hand,
                BackColor = SurfaceColor
            };
            pnlNew.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(e.Graphics, "＋ 新建分组并移动...", font, new Point(8, 5), PrimaryColor);
                }
            };
            pnlNew.MouseEnter += (s, e) => pnlNew.BackColor = PrimaryLightColor;
            pnlNew.MouseLeave += (s, e) => pnlNew.BackColor = SurfaceColor;
            pnlNew.Click += (s, e) =>
            {
                _pnlBatchGroupPopup.Visible = false;
                string newGroupName = PromptInput("新建分组并批量移动", "请输入新分组名称:");
                if (!string.IsNullOrWhiteSpace(newGroupName))
                {
                    ExecuteBatchMoveGroup(newGroupName.Trim());
                }
            };
            _pnlBatchGroupPopup.Controls.Add(pnlNew);

            _pnlBatchGroupPopup.Visible = true;
            _pnlBatchGroupPopup.BringToFront();
        }

        // =============================================================
        // 栏 3: 右侧全属性极简检查器 —— 严格对齐 HTML .inspector-pane (两列 Grid)
        // =============================================================

        private void BuildInspectorPane()
        {
            _pnlInspector = new Panel
            {
                Name = "_pnlInspector",
                Dock = DockStyle.Fill,
                BackColor = SurfaceColor,
                AutoScroll = true,
                Padding = new Padding(14, 10, 14, 10)
            };

            _pnlInspectorStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = false,
                BackColor = SurfaceColor,
                Padding = new Padding(0)
            };

            // 1. 顶层轻量身份栏 (.compact-header-card)
            var pnlIdCard = BuildHeaderCard();

            // 2. 舱 1：材料与表面工艺 (Material & Finish Pod) - 双列 Grid!
            var podCraft = BuildCraftPod();

            // 3. 舱 2：订单与编号规则 (Order Number Mode Pod)
            var podOrder = BuildOrderModePod();

            // 4. 舱 3：排版开料与倍数 (Imposition & Duplicate Pod) - 双列 Grid!
            var podImpo = BuildImpositionPod();

            // 5. 舱 4：目标导出路径 (Export Path Pod)
            var podExport = BuildExportPathPod();

            // 6. 底部快捷保存组
            var pnlSaveBar = BuildSaveBar();

            _pnlInspectorStack.Controls.Add(pnlIdCard);
            _pnlInspectorStack.Controls.Add(podCraft);
            _pnlInspectorStack.Controls.Add(podOrder);
            _pnlInspectorStack.Controls.Add(podImpo);
            _pnlInspectorStack.Controls.Add(podExport);
            _pnlInspectorStack.Controls.Add(pnlSaveBar);

            _pnlInspector.Controls.Add(_pnlInspectorStack);
        }

        private Panel BuildHeaderCard()
        {
            var pnlIdCard = new Panel
            {
                Size = new Size(464, 76),
                BackColor = BgColor,
                Padding = new Padding(12, 8, 12, 8),
                Margin = new Padding(0, 0, 0, 10)
            };
            pnlIdCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnlIdCard.Width - 1, pnlIdCard.Height - 1), 8))
                using (var pen = new Pen(BorderColor, 1F))
                    e.Graphics.DrawPath(pen, path);
                DrawVectorIcon(e.Graphics, "tag", new Rectangle(10, 12, 16, 16), PrimaryColor);
            };

            _txtPresetName = new AntdUI.Input
            {
                Location = new Point(32, 6),
                Size = new Size(328, 28),
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                PlaceholderText = "预设名称..."
            };
            _txtPresetName.TextChanged += (s, e) =>
            {
                if (_isUpdatingUi || _activePreset == null) return;
                _activePreset.Name = _txtPresetName.Text.Trim();
                RefreshActivePresetCardTitle();
            };

            var btnDeletePreset = new AntdUI.Button
            {
                Text = "删除预设",
                Location = new Point(366, 6),
                Size = new Size(88, 28),
                Type = AntdUI.TTypeMini.Error,
                Ghost = true,
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 8F)
            };
            btnDeletePreset.Click += (s, e) => DeleteActivePreset();

            // 底行: 所属分组 + 快捷药丸开关
            var lblGroup = new Label { Text = "所属分组:", Location = new Point(10, 44), AutoSize = true, Font = new Font("Microsoft YaHei UI", 8.5F), ForeColor = TextMutedColor };

            _cmbGroupName = new AntdUI.Select
            {
                Location = new Point(68, 40),
                Size = new Size(130, 26),
                Radius = 3,
                Font = new Font("Microsoft YaHei UI", 8.5F)
            };
            _cmbGroupName.SelectedIndexChanged += (s, e) =>
            {
                if (_isUpdatingUi || _activePreset == null) return;
                string newGroup = _cmbGroupName.Text;
                if (newGroup == "＋新建组...")
                {
                    PromptCreateNewGroup();
                    return;
                }
                if (!string.IsNullOrEmpty(newGroup))
                {
                    if (_checkedPresets.Count > 1 && _checkedPresets.Contains(_activePreset))
                    {
                        foreach (var p in _checkedPresets)
                        {
                            p.GroupName = newGroup;
                        }
                    }
                    else
                    {
                        _activePreset.GroupName = newGroup;
                    }
                    SaveAllPresetsToStorage();
                    RefreshGroupSidebar();
                    RefreshAllPresetDecks();
                }
            };

            _chkShowInButtons = new CheckBox
            {
                Text = "在 V5 快捷药丸显示",
                Location = new Point(208, 44),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = TextSecondaryColor,
                Cursor = Cursors.Hand
            };
            _chkShowInButtons.CheckedChanged += (s, e) =>
            {
                if (_isUpdatingUi || _activePreset == null) return;
                _activePreset.ShowInPresetButtons = _chkShowInButtons.Checked;
                SaveAllPresetsToStorage();
                RefreshActivePresetPinIcon();
            };

            pnlIdCard.Controls.Add(_txtPresetName);
            pnlIdCard.Controls.Add(btnDeletePreset);
            pnlIdCard.Controls.Add(lblGroup);
            pnlIdCard.Controls.Add(_cmbGroupName);
            pnlIdCard.Controls.Add(_chkShowInButtons);

            return pnlIdCard;
        }

        private Panel BuildCraftPod()
        {
            var pod = CreateModulePod("材料与表面工艺 (Material & Finish)", "palette", 186, () => TogglePodRule(new[]
            {
                PresetIgnoreOptions.Material, PresetIgnoreOptions.ColorMode, PresetIgnoreOptions.FilmType, PresetIgnoreOptions.Shape, PresetIgnoreOptions.IdentifierPage
            }));

            // 双列网格 (每列宽度 216px, 间距 14px)
            // 列 1: 指定材料
            _selMaterial = new AntdUI.Select { Radius = 4, Font = new Font("Microsoft YaHei UI", 9F) };
            var uMaterial = CreateFieldUnit("指定材料", _selMaterial, PresetIgnoreOptions.Material, 216, 52);
            uMaterial.Location = new Point(12, 34);

            // 列 2: 颜色模式
            _selColorMode = new AntdUI.Select { Radius = 4, Font = new Font("Microsoft YaHei UI", 9F) };
            _selColorMode.Items.AddRange(new object[] { "彩色", "黑白" });
            var uColor = CreateFieldUnit("颜色模式", _selColorMode, PresetIgnoreOptions.ColorMode, 216, 52);
            uColor.Location = new Point(238, 34);

            // 列 1: 膜类型
            _selFilmType = new AntdUI.Select { Radius = 4, Font = new Font("Microsoft YaHei UI", 9F) };
            _selFilmType.Items.AddRange(new object[] { "光膜", "哑膜", "红膜", "不过膜" });
            var uFilm = CreateFieldUnit("膜类型", _selFilmType, PresetIgnoreOptions.FilmType, 216, 52);
            uFilm.Location = new Point(12, 94);

            // 列 2: 模切形状与圆角半径 R (两项合一！)
            var pnlShapeWrap = new Panel { Size = new Size(216, 28) };
            _selShapeState = new AntdUI.Select { Location = new Point(0, 0), Size = new Size(142, 28), Radius = 4, Font = new Font("Microsoft YaHei UI", 8.5F) };
            _selShapeState.Items.AddRange(new object[] { "▢ 直角", "▢ 圆角 R", "○ 圆形", "◇ 异形", "无" });

            _numRoundRadius = new AntdUI.InputNumber
            {
                Location = new Point(148, 0),
                Size = new Size(68, 28),
                Radius = 4,
                Value = 5M,
                Minimum = 0M,
                Maximum = 50M,
                DecimalPlaces = 1,
                Visible = false
            };

            _selShapeState.SelectedIndexChanged += (s, e) =>
            {
                bool isRound = _selShapeState.Text.Contains("圆角");
                _numRoundRadius.Visible = isRound;
            };

            pnlShapeWrap.Controls.Add(_selShapeState);
            pnlShapeWrap.Controls.Add(_numRoundRadius);

            var uShape = CreateFieldUnit("模切形状 / 半径", pnlShapeWrap, PresetIgnoreOptions.Shape, 216, 52);
            uShape.Location = new Point(238, 94);

            // 整行全宽: 大版首尾插入标识页
            var pnlIdPageRow = new Panel { Location = new Point(12, 154), Size = new Size(440, 24) };
            _chkAddIdentifierPage = new CheckBox
            {
                Text = "大版首尾插入标识页",
                Location = new Point(0, 2),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = TextSecondaryColor,
                Cursor = Cursors.Hand
            };
            var lIdPage = CreateFollowLatch(PresetIgnoreOptions.IdentifierPage, (on) =>
            {
                _chkAddIdentifierPage.Enabled = on;
                _chkAddIdentifierPage.ForeColor = on ? TextSecondaryColor : TextMutedColor;
            });
            lIdPage.Location = new Point(384, 2);
            pnlIdPageRow.Controls.Add(_chkAddIdentifierPage);
            pnlIdPageRow.Controls.Add(lIdPage);

            pod.Controls.Add(uMaterial);
            pod.Controls.Add(uColor);
            pod.Controls.Add(uFilm);
            pod.Controls.Add(uShape);
            pod.Controls.Add(pnlIdPageRow);

            return pod;
        }

        private Panel BuildOrderModePod()
        {
            var pod = CreateModulePod("订单与编号规则 (Order Number Mode)", "gear", 92, () => TogglePodRule(new[]
            {
                PresetIgnoreOptions.OrderNumberMode
            }));

            // 列 1: 订单号生成模式
            _selOrderNumberMode = new AntdUI.Select { Radius = 4, Font = new Font("Microsoft YaHei UI", 9F) };
            _selOrderNumberMode.Items.AddRange(new object[] { "无", "自动递增", "正则提取" });
            _selOrderNumberMode.SelectedIndexChanged += (s, e) =>
            {
                bool isRegex = _selOrderNumberMode.Text == "正则提取";
                if (_selOrderRegexName != null)
                    _selOrderRegexName.Visible = isRegex;
            };

            var uOrderMode = CreateFieldUnit("订单号模式", _selOrderNumberMode, PresetIgnoreOptions.OrderNumberMode, 216, 52);
            uOrderMode.Location = new Point(12, 34);

            // 列 2: 正则提取规则
            _selOrderRegexName = new AntdUI.Select { Radius = 4, Font = new Font("Microsoft YaHei UI", 9F), Visible = false };
            LoadOrderRegexRules();

            var uRegexRule = new Panel { Size = new Size(216, 52), Location = new Point(238, 34), BackColor = Color.Transparent };
            var lblRegex = new Label
            {
                Text = "提取正则规则",
                Location = new Point(0, 1),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
                ForeColor = TextSecondaryColor
            };
            _selOrderRegexName.Location = new Point(0, 22);
            _selOrderRegexName.Size = new Size(216, 28);
            uRegexRule.Controls.Add(lblRegex);
            uRegexRule.Controls.Add(_selOrderRegexName);

            pod.Controls.Add(uOrderMode);
            pod.Controls.Add(uRegexRule);

            return pod;
        }

        private void LoadOrderRegexRules()
        {
            _selOrderRegexName.Items.Clear();
            string rawPatterns = AppSettings.RegexPatterns;
            if (!string.IsNullOrEmpty(rawPatterns))
            {
                string[] patterns = rawPatterns.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in patterns)
                {
                    var parts = p.Split('=');
                    if (parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[0]))
                    {
                        string ruleName = parts[0].Trim();
                        if (!_selOrderRegexName.Items.Contains(ruleName))
                            _selOrderRegexName.Items.Add(ruleName);
                    }
                }
            }
            if (_selOrderRegexName.Items.Count == 0)
            {
                _selOrderRegexName.Items.AddRange(new object[] { "第一组数", "威立德", "保留9位", "保留12位" });
            }
        }

        private Panel BuildImpositionPod()
        {
            var pod = CreateModulePod("排版开料与倍数 (Imposition & Duplicate)", "ruler", 420, () => TogglePodRule(new[]
            {
                PresetIgnoreOptions.EnableImposition, PresetIgnoreOptions.TetBleed, PresetIgnoreOptions.MaterialType, PresetIgnoreOptions.LayoutMode, PresetIgnoreOptions.IsDualCopy
            }));

            // 列 1: 启用自动排版
            _selEnableImposition = new AntdUI.Select { Radius = 4, Font = new Font("Microsoft YaHei UI", 9F) };
            _selEnableImposition.Items.AddRange(new object[] { "启用计算", "停用计算" });
            var uEnable = CreateFieldUnit("启用自动排版", _selEnableImposition, PresetIgnoreOptions.EnableImposition, 216, 52);
            uEnable.Location = new Point(12, 34);

            // 列 2: 单边出血位 (mm)
            _numTetBleed = new AntdUI.InputNumber
            {
                Radius = 4,
                Value = 2.0M,
                Minimum = 0M,
                Maximum = 50M,
                DecimalPlaces = 1
            };
            var uBleed = CreateFieldUnit("出血位 (mm)", _numTetBleed, PresetIgnoreOptions.TetBleed, 216, 52);
            uBleed.Location = new Point(238, 34);

            // 列 1: 材料形态 (平张 / 卷装)
            _selMaterialType = new AntdUI.Select { Radius = 4, Font = new Font("Microsoft YaHei UI", 9F) };
            _selMaterialType.Items.AddRange(new object[] { "平张", "卷装" });
            var uMatType = CreateFieldUnit("材料形态", _selMaterialType, PresetIgnoreOptions.MaterialType, 216, 52);
            uMatType.Location = new Point(12, 94);

            // 列 2: 排版模式 (连拼 / 折手)
            _selLayoutMode = new AntdUI.Select { Radius = 4, Font = new Font("Microsoft YaHei UI", 9F) };
            _selLayoutMode.Items.AddRange(new object[] { "连拼", "折手" });
            var uLayout = CreateFieldUnit("排版模式", _selLayoutMode, PresetIgnoreOptions.LayoutMode, 216, 52);
            uLayout.Location = new Point(238, 94);

            // 整行全宽: 一式N联拼版与参数子面板 (常态呈现完整功能，消灭空白区域)
            var pnlDualWrap = new Panel { Location = new Point(12, 154), Size = new Size(440, 78) };
            var pnlDualTop = new Panel { Dock = DockStyle.Top, Height = 22, BackColor = Color.Transparent };
            _chkIsDualCopy = new CheckBox
            {
                Text = "开启一式N联拼版",
                Location = new Point(0, 0),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
                ForeColor = TextSecondaryColor,
                Cursor = Cursors.Hand
            };
            _chkIsDualCopy.CheckedChanged += (s, e) =>
            {
                bool enabled = _chkIsDualCopy.Checked;
                if (_numCopyCount != null) _numCopyCount.Enabled = enabled;
                if (_selCopyType != null) _selCopyType.Enabled = enabled;
                if (_selCopyMode != null) _selCopyMode.Enabled = enabled;
            };

            var lDual = CreateFollowLatch(PresetIgnoreOptions.IsDualCopy, (on) =>
            {
                _chkIsDualCopy.Enabled = on;
                bool subEnabled = on && _chkIsDualCopy.Checked;
                if (_numCopyCount != null) _numCopyCount.Enabled = subEnabled;
                if (_selCopyType != null) _selCopyType.Enabled = subEnabled;
                if (_selCopyMode != null) _selCopyMode.Enabled = subEnabled;
            });
            lDual.Location = new Point(384, 0);
            pnlDualTop.Controls.Add(_chkIsDualCopy);
            pnlDualTop.Controls.Add(lDual);

            // 一式N联参数面板 (3列) - 常态呈现，禁用时置灰，消除空白断层
            _pnlDualCopySub = new Panel
            {
                Location = new Point(0, 24),
                Size = new Size(440, 52),
                BackColor = BgColor
            };
            _pnlDualCopySub.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, _pnlDualCopySub.Width - 1, _pnlDualCopySub.Height - 1), 6))
                using (var pen = new Pen(BorderLightColor, 1F))
                    e.Graphics.DrawPath(pen, path);
            };

            // 联数
            var lblC1 = new Label { Text = "联数 (份数):", Location = new Point(8, 6), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numCopyCount = new AntdUI.InputNumber { Location = new Point(8, 22), Size = new Size(134, 24), Radius = 3, Value = 2M, Minimum = 2M, Maximum = 16M, Enabled = false };

            // 类型
            var lblC2 = new Label { Text = "一式类型:", Location = new Point(152, 6), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _selCopyType = new AntdUI.Select { Location = new Point(152, 22), Size = new Size(134, 24), Radius = 3, Font = new Font("Microsoft YaHei UI", 8F), Enabled = false };
            _selCopyType.Items.AddRange(new object[] { "联", "份" });

            // 方向
            var lblC3 = new Label { Text = "倍数方向:", Location = new Point(296, 6), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _selCopyMode = new AntdUI.Select { Location = new Point(296, 22), Size = new Size(136, 24), Radius = 3, Font = new Font("Microsoft YaHei UI", 8F), Enabled = false };
            _selCopyMode.Items.AddRange(new object[] { "自适应列", "自适应行", "不旋转列", "不旋转行" });

            _pnlDualCopySub.Controls.Add(lblC1);
            _pnlDualCopySub.Controls.Add(_numCopyCount);
            _pnlDualCopySub.Controls.Add(lblC2);
            _pnlDualCopySub.Controls.Add(_selCopyType);
            _pnlDualCopySub.Controls.Add(lblC3);
            _pnlDualCopySub.Controls.Add(_selCopyMode);

            pnlDualWrap.Controls.Add(_pnlDualCopySub);
            pnlDualWrap.Controls.Add(pnlDualTop);

            // 内嵌大版开料与边距参数工位卡片 (平张与卷装两种设置兼备)
            _pnlEmbeddedImpo = BuildEmbeddedImpositionPanel();
            _pnlEmbeddedImpo.Location = new Point(12, 236);

            pod.Controls.Add(uEnable);
            pod.Controls.Add(uBleed);
            pod.Controls.Add(uMatType);
            pod.Controls.Add(uLayout);
            pod.Controls.Add(pnlDualWrap);
            pod.Controls.Add(_pnlEmbeddedImpo);

            return pod;
        }

        private Panel BuildEmbeddedImpositionPanel()
        {
            var pnl = new Panel
            {
                Size = new Size(440, 172),
                BackColor = Color.Transparent
            };

            // ================= 1. 平张大版参数卡片 (独立启动) =================
            var pnlBoxFlat = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(440, 82),
                BackColor = BgColor
            };
            pnlBoxFlat.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnlBoxFlat.Width - 1, pnlBoxFlat.Height - 1), 6))
                using (var pen = new Pen(_chkEnableFlatImpo != null && _chkEnableFlatImpo.Checked ? PrimaryBorderColor : BorderLightColor, 1F))
                    e.Graphics.DrawPath(pen, path);
            };

            // 平张 Row 0: 独立勾选开关 + 状态提示
            _chkEnableFlatImpo = new CheckBox
            {
                Text = "📄 启用平张专属纸张与边距",
                Location = new Point(8, 4),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
                ForeColor = PrimaryColor,
                Cursor = Cursors.Hand
            };
            _lblFlatImpoHint = new Label
            {
                Text = "未激活：遵循全局设置",
                Location = new Point(275, 6),
                Size = new Size(158, 16),
                Font = new Font("Microsoft YaHei UI", 7.5F),
                ForeColor = TextMutedColor,
                TextAlign = ContentAlignment.MiddleRight
            };

            _chkEnableFlatImpo.CheckedChanged += (s, e) =>
            {
                if (_isUpdatingUi) return;
                bool activated = _chkEnableFlatImpo.Checked;
                SetFlatImpositionInputsEnabled(activated);
                _lblFlatImpoHint.Text = activated ? "已激活：预设专属平张" : "未激活：遵循全局设置";
                _lblFlatImpoHint.ForeColor = activated ? PrimaryColor : TextMutedColor;
                pnlBoxFlat.Invalidate();
                if (!activated)
                {
                    ReloadGlobalFlatImpositionParameters();
                }
            };

            // 平张 Row 1: 纸张尺寸
            var lblFlatDim = new Label { Text = "纸张尺寸:", Location = new Point(8, 28), AutoSize = true, Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold), ForeColor = TextSecondaryColor };
            _numFlatPaperWidth = new AntdUI.InputNumber { Location = new Point(68, 25), Size = new Size(72, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 5000M };
            var lblFlatX = new Label { Text = "×", Location = new Point(144, 28), AutoSize = true, Font = new Font("Microsoft YaHei UI", 8F), ForeColor = TextMutedColor };
            _numFlatPaperHeight = new AntdUI.InputNumber { Location = new Point(160, 25), Size = new Size(72, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 5000M };
            var lblFlatDimUnit = new Label { Text = "mm", Location = new Point(236, 28), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };

            // 平张 Row 2: 4向边距
            var lblFlatMargins = new Label { Text = "平张边距:", Location = new Point(8, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 8F), ForeColor = TextSecondaryColor };
            var lblFT = new Label { Text = "上:", Location = new Point(68, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numFlatMarginTop = new AntdUI.InputNumber { Location = new Point(86, 51), Size = new Size(50, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 200M };
            var lblFB = new Label { Text = "下:", Location = new Point(140, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numFlatMarginBottom = new AntdUI.InputNumber { Location = new Point(158, 51), Size = new Size(50, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 200M };
            var lblFL = new Label { Text = "左:", Location = new Point(212, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numFlatMarginLeft = new AntdUI.InputNumber { Location = new Point(230, 51), Size = new Size(50, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 200M };
            var lblFR = new Label { Text = "右:", Location = new Point(284, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numFlatMarginRight = new AntdUI.InputNumber { Location = new Point(302, 51), Size = new Size(50, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 200M };
            var lblFlatMarginUnit = new Label { Text = "mm", Location = new Point(356, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };

            pnlBoxFlat.Controls.Add(_chkEnableFlatImpo);
            pnlBoxFlat.Controls.Add(_lblFlatImpoHint);
            pnlBoxFlat.Controls.Add(lblFlatDim);
            pnlBoxFlat.Controls.Add(_numFlatPaperWidth);
            pnlBoxFlat.Controls.Add(lblFlatX);
            pnlBoxFlat.Controls.Add(_numFlatPaperHeight);
            pnlBoxFlat.Controls.Add(lblFlatDimUnit);
            pnlBoxFlat.Controls.Add(lblFlatMargins);
            pnlBoxFlat.Controls.Add(lblFT);
            pnlBoxFlat.Controls.Add(_numFlatMarginTop);
            pnlBoxFlat.Controls.Add(lblFB);
            pnlBoxFlat.Controls.Add(_numFlatMarginBottom);
            pnlBoxFlat.Controls.Add(lblFL);
            pnlBoxFlat.Controls.Add(_numFlatMarginLeft);
            pnlBoxFlat.Controls.Add(lblFR);
            pnlBoxFlat.Controls.Add(_numFlatMarginRight);
            pnlBoxFlat.Controls.Add(lblFlatMarginUnit);

            // ================= 2. 卷装大版参数卡片 (独立启动) =================
            var pnlBoxRoll = new Panel
            {
                Location = new Point(0, 88),
                Size = new Size(440, 82),
                BackColor = BgColor
            };
            pnlBoxRoll.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnlBoxRoll.Width - 1, pnlBoxRoll.Height - 1), 6))
                using (var pen = new Pen(_chkEnableRollImpo != null && _chkEnableRollImpo.Checked ? Color.FromArgb(245, 158, 11) : BorderLightColor, 1F))
                    e.Graphics.DrawPath(pen, path);
            };

            // 卷装 Row 0: 独立勾选开关 + 状态提示
            _chkEnableRollImpo = new CheckBox
            {
                Text = "🌀 启用卷装专属规格与边距",
                Location = new Point(8, 4),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 119, 6),
                Cursor = Cursors.Hand
            };
            _lblRollImpoHint = new Label
            {
                Text = "未激活：遵循全局设置",
                Location = new Point(275, 6),
                Size = new Size(158, 16),
                Font = new Font("Microsoft YaHei UI", 7.5F),
                ForeColor = TextMutedColor,
                TextAlign = ContentAlignment.MiddleRight
            };

            _chkEnableRollImpo.CheckedChanged += (s, e) =>
            {
                if (_isUpdatingUi) return;
                bool activated = _chkEnableRollImpo.Checked;
                SetRollImpositionInputsEnabled(activated);
                _lblRollImpoHint.Text = activated ? "已激活：预设专属卷装" : "未激活：遵循全局设置";
                _lblRollImpoHint.ForeColor = activated ? Color.FromArgb(217, 119, 6) : TextMutedColor;
                pnlBoxRoll.Invalidate();
                if (!activated)
                {
                    ReloadGlobalRollImpositionParameters();
                }
            };

            // 卷装 Row 1: 固定宽/最小长
            var lblRollDim = new Label { Text = "卷装规格:", Location = new Point(8, 28), AutoSize = true, Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold), ForeColor = TextSecondaryColor };
            var lblRW = new Label { Text = "固定宽:", Location = new Point(68, 28), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numRollFixedWidth = new AntdUI.InputNumber { Location = new Point(112, 25), Size = new Size(68, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 5000M };
            var lblRWUnit = new Label { Text = "mm", Location = new Point(182, 28), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };

            var lblRL = new Label { Text = "最小长:", Location = new Point(206, 28), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numRollMinLength = new AntdUI.InputNumber { Location = new Point(250, 25), Size = new Size(68, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 5000M };
            var lblRLUnit = new Label { Text = "mm", Location = new Point(320, 28), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };

            // 卷装 Row 2: 4向边距
            var lblRollMargins = new Label { Text = "卷装边距:", Location = new Point(8, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 8F), ForeColor = TextSecondaryColor };
            var lblRT = new Label { Text = "上:", Location = new Point(68, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numRollMarginTop = new AntdUI.InputNumber { Location = new Point(86, 51), Size = new Size(50, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 200M };
            var lblRB = new Label { Text = "下:", Location = new Point(140, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numRollMarginBottom = new AntdUI.InputNumber { Location = new Point(158, 51), Size = new Size(50, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 200M };
            var lblRLm = new Label { Text = "左:", Location = new Point(212, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numRollMarginLeft = new AntdUI.InputNumber { Location = new Point(230, 51), Size = new Size(50, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 200M };
            var lblRRm = new Label { Text = "右:", Location = new Point(284, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };
            _numRollMarginRight = new AntdUI.InputNumber { Location = new Point(302, 51), Size = new Size(50, 24), Radius = 3, DecimalPlaces = 1, Minimum = 0M, Maximum = 200M };
            var lblRollMarginUnit = new Label { Text = "mm", Location = new Point(356, 54), AutoSize = true, Font = new Font("Microsoft YaHei UI", 7.5F), ForeColor = TextMutedColor };

            pnlBoxRoll.Controls.Add(_chkEnableRollImpo);
            pnlBoxRoll.Controls.Add(_lblRollImpoHint);
            pnlBoxRoll.Controls.Add(lblRollDim);
            pnlBoxRoll.Controls.Add(lblRW);
            pnlBoxRoll.Controls.Add(_numRollFixedWidth);
            pnlBoxRoll.Controls.Add(lblRWUnit);
            pnlBoxRoll.Controls.Add(lblRL);
            pnlBoxRoll.Controls.Add(_numRollMinLength);
            pnlBoxRoll.Controls.Add(lblRLUnit);
            pnlBoxRoll.Controls.Add(lblRollMargins);
            pnlBoxRoll.Controls.Add(lblRT);
            pnlBoxRoll.Controls.Add(_numRollMarginTop);
            pnlBoxRoll.Controls.Add(lblRB);
            pnlBoxRoll.Controls.Add(_numRollMarginBottom);
            pnlBoxRoll.Controls.Add(lblRLm);
            pnlBoxRoll.Controls.Add(_numRollMarginLeft);
            pnlBoxRoll.Controls.Add(lblRRm);
            pnlBoxRoll.Controls.Add(_numRollMarginRight);
            pnlBoxRoll.Controls.Add(lblRollMarginUnit);

            pnl.Controls.Add(pnlBoxRoll);
            pnl.Controls.Add(pnlBoxFlat);

            return pnl;
        }

        private void SetFlatImpositionInputsEnabled(bool enabled)
        {
            if (_numFlatPaperWidth != null) _numFlatPaperWidth.Enabled = enabled;
            if (_numFlatPaperHeight != null) _numFlatPaperHeight.Enabled = enabled;
            if (_numFlatMarginTop != null) _numFlatMarginTop.Enabled = enabled;
            if (_numFlatMarginBottom != null) _numFlatMarginBottom.Enabled = enabled;
            if (_numFlatMarginLeft != null) _numFlatMarginLeft.Enabled = enabled;
            if (_numFlatMarginRight != null) _numFlatMarginRight.Enabled = enabled;
        }

        private void SetRollImpositionInputsEnabled(bool enabled)
        {
            if (_numRollFixedWidth != null) _numRollFixedWidth.Enabled = enabled;
            if (_numRollMinLength != null) _numRollMinLength.Enabled = enabled;
            if (_numRollMarginTop != null) _numRollMarginTop.Enabled = enabled;
            if (_numRollMarginBottom != null) _numRollMarginBottom.Enabled = enabled;
            if (_numRollMarginLeft != null) _numRollMarginLeft.Enabled = enabled;
            if (_numRollMarginRight != null) _numRollMarginRight.Enabled = enabled;
        }

        private void ReloadGlobalFlatImpositionParameters()
        {
            if (_numFlatPaperWidth == null) return;
            _numFlatPaperWidth.Value = (decimal)GetAppSettingsFloat("Imposition_PaperWidth", 320);
            _numFlatPaperHeight.Value = (decimal)GetAppSettingsFloat("Imposition_PaperHeight", 464);
            _numFlatMarginTop.Value = (decimal)GetAppSettingsFloat("Imposition_MarginTop", 19);
            _numFlatMarginBottom.Value = (decimal)GetAppSettingsFloat("Imposition_MarginBottom", 19);
            _numFlatMarginLeft.Value = (decimal)GetAppSettingsFloat("Imposition_MarginLeft", 7);
            _numFlatMarginRight.Value = (decimal)GetAppSettingsFloat("Imposition_MarginRight", 7);
        }

        private void ReloadGlobalRollImpositionParameters()
        {
            if (_numRollFixedWidth == null) return;
            _numRollFixedWidth.Value = (decimal)GetAppSettingsFloat("Imposition_FixedWidth", 320);
            _numRollMinLength.Value = (decimal)GetAppSettingsFloat("Imposition_MinLength", 150);
            _numRollMarginTop.Value = (decimal)GetAppSettingsFloat("Imposition_RollMarginTop", 0);
            _numRollMarginBottom.Value = (decimal)GetAppSettingsFloat("Imposition_RollMarginBottom", 0);
            _numRollMarginLeft.Value = (decimal)GetAppSettingsFloat("Imposition_RollMarginLeft", 8);
            _numRollMarginRight.Value = (decimal)GetAppSettingsFloat("Imposition_RollMarginRight", 2);
        }

        private void LoadPresetFlatImpositionParameters(TemporaryImpositionParameters p)
        {
            if (_numFlatPaperWidth == null || p == null) return;
            _numFlatPaperWidth.Value = (decimal)(p.PaperWidth > 0 ? p.PaperWidth : GetAppSettingsFloat("Imposition_PaperWidth", 320));
            _numFlatPaperHeight.Value = (decimal)(p.PaperHeight > 0 ? p.PaperHeight : GetAppSettingsFloat("Imposition_PaperHeight", 464));
            _numFlatMarginTop.Value = (decimal)p.MarginTop;
            _numFlatMarginBottom.Value = (decimal)p.MarginBottom;
            _numFlatMarginLeft.Value = (decimal)p.MarginLeft;
            _numFlatMarginRight.Value = (decimal)p.MarginRight;
        }

        private void LoadPresetRollImpositionParameters(TemporaryImpositionParameters p)
        {
            if (_numRollFixedWidth == null || p == null) return;
            _numRollFixedWidth.Value = (decimal)(p.FixedWidth > 0 ? p.FixedWidth : GetAppSettingsFloat("Imposition_FixedWidth", 320));
            _numRollMinLength.Value = (decimal)(p.MinLength > 0 ? p.MinLength : GetAppSettingsFloat("Imposition_MinLength", 150));
            _numRollMarginTop.Value = (decimal)p.RollMarginTop;
            _numRollMarginBottom.Value = (decimal)p.RollMarginBottom;
            _numRollMarginLeft.Value = (decimal)p.RollMarginLeft;
            _numRollMarginRight.Value = (decimal)p.RollMarginRight;
        }

        private static float GetAppSettingsFloat(string key, float defaultVal)
        {
            try
            {
                var val = AppSettings.Get(key);
                if (val != null && float.TryParse(val.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                {
                    return result;
                }
            }
            catch { }
            return defaultVal;
        }

        private Panel BuildExportPathPod()
        {
            var pod = CreateModulePod("目标导出路径 (Export Path)", "path", 80);

            var pnlTop = new Panel { Location = new Point(12, 28), Size = new Size(440, 18) };
            var lblTitle = new Label { Text = "目标导出路径", Location = new Point(0, 1), AutoSize = true, Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold), ForeColor = TextSecondaryColor };
            var lPath = CreateFollowLatch(PresetIgnoreOptions.ExportPath, (on) =>
            {
                _txtExportPath.Enabled = on;
                _btnBrowseExportPath.Enabled = on;
            });
            lPath.Location = new Point(384, 0);
            pnlTop.Controls.Add(lblTitle);
            pnlTop.Controls.Add(lPath);

            var pnlBottom = new Panel { Location = new Point(12, 48), Size = new Size(440, 28) };
            _txtExportPath = new AntdUI.Input
            {
                Location = new Point(0, 0),
                Size = new Size(364, 28),
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                PlaceholderText = "例如 F:\\点源自动\\威立德..."
            };
            _btnBrowseExportPath = new AntdUI.Button
            {
                Text = "浏览...",
                Location = new Point(372, 0),
                Size = new Size(68, 28),
                Radius = 4,
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                Font = new Font("Microsoft YaHei UI", 8.5F)
            };
            _btnBrowseExportPath.Click += (s, e) =>
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "选择预设默认导出目录";
                    if (Directory.Exists(_txtExportPath.Text)) dialog.SelectedPath = _txtExportPath.Text;
                    if (dialog.ShowDialog(this) == DialogResult.OK) _txtExportPath.Text = dialog.SelectedPath;
                }
            };
            pnlBottom.Controls.Add(_txtExportPath);
            pnlBottom.Controls.Add(_btnBrowseExportPath);

            pod.Controls.Add(pnlTop);
            pod.Controls.Add(pnlBottom);

            return pod;
        }

        private Panel BuildSaveBar()
        {
            var pnlSaveBar = new Panel
            {
                Size = new Size(464, 36),
                BackColor = SurfaceColor,
                Padding = new Padding(0, 2, 0, 0),
                Margin = new Padding(0, 4, 0, 12)
            };
            var btnSaveCurrent = new AntdUI.Button
            {
                Text = "💾 保存预设参数",
                Size = new Size(326, 34),
                Dock = DockStyle.Left,
                Type = AntdUI.TTypeMini.Primary,
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold)
            };
            btnSaveCurrent.Click += (s, e) =>
            {
                CommitActivePresetChanges();
                SaveAllPresetsToStorage();
                AntdUI.Message.success(this, $"预设 [{_activePreset?.Name}] 已即时保存！");
            };

            var btnCloneCurrent = new AntdUI.Button
            {
                Text = "另存副本",
                Size = new Size(130, 34),
                Dock = DockStyle.Right,
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                Radius = 4,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            btnCloneCurrent.Click += (s, e) => CloneActivePreset();

            pnlSaveBar.Controls.Add(btnSaveCurrent);
            pnlSaveBar.Controls.Add(btnCloneCurrent);

            return pnlSaveBar;
        }

        // =============================================================
        // 工位舱辅助生成器与矢量图标
        // =============================================================

        private Panel CreateModulePod(string title, string iconType, int height, Action onToggleRule = null)
        {
            var pnl = new Panel
            {
                Size = new Size(464, height),
                BackColor = SurfaceColor,
                Margin = new Padding(0, 0, 0, 10)
            };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 8))
                using (var pen = new Pen(BorderColor, 1F))
                    e.Graphics.DrawPath(pen, path);

                using (var pen = new Pen(BorderLightColor, 1F))
                    e.Graphics.DrawLine(pen, 1, 26, pnl.Width - 2, 26);

                // 绘制舱头矢量图标
                DrawVectorIcon(e.Graphics, iconType, new Rectangle(10, 6, 16, 16), PrimaryColor);

                using (var font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(e.Graphics, title, font, new Point(30, 5), PrimaryColor, TextFormatFlags.NoPrefix);
                }
            };

            if (onToggleRule != null)
            {
                var lblQuick = new Label
                {
                    Text = "一键全部跟随 / 保持",
                    Location = new Point(pnl.Width - 136, 6),
                    AutoSize = true,
                    Font = new Font("Microsoft YaHei UI", 8F),
                    ForeColor = TextMutedColor,
                    Cursor = Cursors.Hand
                };
                lblQuick.MouseEnter += (s, e) => lblQuick.ForeColor = PrimaryColor;
                lblQuick.MouseLeave += (s, e) => lblQuick.ForeColor = TextMutedColor;
                lblQuick.Click += (s, e) => onToggleRule();
                pnl.Controls.Add(lblQuick);
            }

            return pnl;
        }

        private Panel CreateFieldUnit(string label, Control inputControl, PresetIgnoreOptions option, int width, int height)
        {
            var pnl = new Panel { Size = new Size(width, height), BackColor = Color.Transparent };

            var pnlBar = new Panel { Location = new Point(0, 0), Size = new Size(width, 18), BackColor = Color.Transparent };
            var lbl = new Label
            {
                Text = label,
                Location = new Point(0, 1),
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
                ForeColor = TextSecondaryColor
            };

            var latch = CreateFollowLatch(option, (follow) =>
            {
                inputControl.Enabled = follow;
                lbl.ForeColor = follow ? TextSecondaryColor : TextMutedColor;
            });
            latch.Location = new Point(width - 56, 0);

            pnlBar.Controls.Add(lbl);
            pnlBar.Controls.Add(latch);

            inputControl.Location = new Point(0, 22);
            inputControl.Size = new Size(width, 28);

            pnl.Controls.Add(pnlBar);
            pnl.Controls.Add(inputControl);

            return pnl;
        }

        private MicroFollowLatch CreateFollowLatch(PresetIgnoreOptions option, Action<bool> onStateChanged)
        {
            var latch = new MicroFollowLatch();
            latch.StateChanged += (s, isFollow) =>
            {
                onStateChanged?.Invoke(isFollow);
                if (_isUpdatingUi || _activePreset == null) return;

                if (isFollow)
                    _activePreset.DisabledOptions &= ~option;
                else
                    _activePreset.DisabledOptions |= option;

                RefreshActivePresetChips();
            };
            _latchMap[option] = latch;
            return latch;
        }

        private void TogglePodRule(IEnumerable<PresetIgnoreOptions> options)
        {
            if (_activePreset == null) return;
            bool anyFollow = options.Any(opt => !_activePreset.DisabledOptions.HasFlag(opt));
            bool targetFollow = !anyFollow;

            foreach (var opt in options)
            {
                if (targetFollow) _activePreset.DisabledOptions &= ~opt;
                else _activePreset.DisabledOptions |= opt;

                if (_latchMap.TryGetValue(opt, out var latch))
                {
                    latch.IsFollowing = targetFollow;
                }
            }

            SelectPresetForInspection(_activePreset);
            RefreshActivePresetChips();
        }

        // =============================================================
        // 数据绑定与渲染 (卡片胶囊 Chips 体系)
        // =============================================================

        private void LoadPresets()
        {
            _presets = AppSettings.MaterialPresets ?? new List<MaterialSelectionPreset>();
            foreach (var p in _presets)
            {
                if (string.IsNullOrWhiteSpace(p.GroupName))
                    p.GroupName = "未命名";
            }

            // 初始化真实用户分组列表（默认只有“未命名”）
            _groups = new List<string> { "未命名" };

            // 从配置中加载已保存的用户自定义分组（严格剔除 HTML 示例假分组）
            var savedGroups = AppSettings.MaterialPresetGroups ?? new List<string>();
            foreach (var g in savedGroups)
            {
                if (string.IsNullOrWhiteSpace(g)) continue;
                string trim = g.Trim();
                if (IsMockGroup(trim)) continue;
                if (!_groups.Contains(trim, StringComparer.OrdinalIgnoreCase))
                    _groups.Add(trim);
            }

            // 从已有预设中自动汇总分组（同样清除假数据归入未命名）
            foreach (var p in _presets)
            {
                if (IsMockGroup(p.GroupName))
                    p.GroupName = "未命名";

                if (!_groups.Contains(p.GroupName, StringComparer.OrdinalIgnoreCase))
                    _groups.Add(p.GroupName);
            }

            string lastGroup = AppSettings.LastSelectedPresetGroup ?? "未命名";
            _activeGroupName = _groups.Contains(lastGroup, StringComparer.OrdinalIgnoreCase) ? lastGroup : "未命名";

            AppSettings.MaterialPresetGroups = _groups;
            AppSettings.Save();

            LoadMaterialsToInspector();
            RefreshGroupSidebar();
            RefreshAllPresetDecks();
        }

        private static bool IsMockGroup(string groupName)
        {
            return groupName == "常用标签" || groupName == "卷装专区" || groupName == "特种模切";
        }

        private void LoadMaterialsToInspector()
        {
            _selMaterial.Items.Clear();
            var mats = (AppSettings.Material ?? "").Split(new[] { ',', '|', '，' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var m in mats)
            {
                string trim = m.Trim();
                if (!string.IsNullOrEmpty(trim) && !_selMaterial.Items.Contains(trim))
                    _selMaterial.Items.Add(trim);
            }
            if (_selMaterial.Items.Count == 0)
            {
                _selMaterial.Items.AddRange(new object[] { "PET", "PP", "PVC", "PET透明", "PET哑光", "PP环保" });
            }
        }

        private void RefreshGroupSidebar()
        {
            _flpGroupList.SuspendLayout();
            _flpGroupList.Controls.Clear();

            _cmbGroupName.Items.Clear();
            foreach (var g in _groups) _cmbGroupName.Items.Add(g);
            _cmbGroupName.Items.Add("＋新建组...");

            foreach (var groupName in _groups)
            {
                int count = _presets.Count(p => (string.IsNullOrWhiteSpace(p.GroupName) ? "未命名" : p.GroupName.Trim()) == groupName);

                var pnlItem = new Panel
                {
                    Size = new Size(164, 34),
                    Margin = new Padding(0, 0, 0, 4),
                    BackColor = (groupName == _activeGroupName) ? PrimaryLightColor : Color.Transparent,
                    Cursor = Cursors.Hand
                };

                pnlItem.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    if (groupName == _activeGroupName)
                    {
                        using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnlItem.Width - 1, pnlItem.Height - 1), 6))
                        using (var brush = new SolidBrush(PrimaryLightColor))
                        {
                            e.Graphics.FillPath(brush, path);
                        }
                    }

                    // 统一使用纯粹现代的文件夹矢量图标
                    DrawVectorIcon(e.Graphics, "folder", new Rectangle(8, 9, 16, 16), (groupName == _activeGroupName) ? PrimaryColor : TextSecondaryColor);

                    using (var fontText = new Font("Microsoft YaHei UI", 9F, (groupName == _activeGroupName) ? FontStyle.Bold : FontStyle.Regular))
                    {
                        Color textClr = (groupName == _activeGroupName) ? PrimaryColor : TextSecondaryColor;
                        TextRenderer.DrawText(e.Graphics, groupName, fontText, new Point(28, 8), textClr);
                    }

                    // 绘制计数药丸徽标
                    var badgeRect = new Rectangle(pnlItem.Width - 32, 8, 24, 18);
                    Color badgeBg = (groupName == _activeGroupName) ? PrimaryColor : Color.FromArgb(234, 237, 241);
                    Color badgeTxt = (groupName == _activeGroupName) ? Color.White : TextMutedColor;

                    using (var path = CreateRoundedRectanglePath(badgeRect, 9))
                    using (var brush = new SolidBrush(badgeBg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (var fontBadge = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Bold))
                    {
                        TextRenderer.DrawText(e.Graphics, count.ToString(), fontBadge, badgeRect, badgeTxt,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                };

                pnlItem.Click += (s, e) => SwitchActiveGroup(groupName);

                // 右键菜单支持修改与删除
                pnlItem.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        ShowGroupContextMenu(groupName, pnlItem, e.Location);
                    }
                };

                // 拖拽放入支持：将卡片拖拽到分组上即刻转移
                pnlItem.AllowDrop = true;
                pnlItem.DragEnter += (s, e) =>
                {
                    if (e.Data.GetDataPresent(typeof(MaterialSelectionPreset)))
                        e.Effect = DragDropEffects.Move;
                };
                pnlItem.DragDrop += (s, e) =>
                {
                    if (e.Data.GetDataPresent(typeof(MaterialSelectionPreset)))
                    {
                        var dragged = e.Data.GetData(typeof(MaterialSelectionPreset)) as MaterialSelectionPreset;
                        if (dragged != null)
                        {
                            var targets = (_checkedPresets.Count > 0 && _checkedPresets.Contains(dragged))
                                ? _checkedPresets.ToList()
                                : new List<MaterialSelectionPreset> { dragged };

                            foreach (var p in targets)
                            {
                                p.GroupName = groupName;
                            }
                            _checkedPresets.Clear();
                            _chkSelectAll.Checked = false;
                            UpdateBatchFloatingBar();
                            SaveAllPresetsToStorage();
                            RefreshGroupSidebar();
                            SwitchActiveGroup(groupName);
                            AntdUI.Message.success(this, $"已将 {targets.Count} 个预设转移至【{groupName}】！");
                        }
                    }
                };

                _flpGroupList.Controls.Add(pnlItem);
            }

            _flpGroupList.ResumeLayout(true);
        }

        private void ShowGroupContextMenu(string groupName, Control anchor, Point pt)
        {
            var cms = new ContextMenuStrip();
            if (groupName == "未命名")
            {
                var item = cms.Items.Add("默认基础分组不可修改或删除");
                item.Enabled = false;
                cms.Show(anchor, pt);
                return;
            }

            cms.Items.Add("✏️ 重命名分组", null, (s, e) =>
            {
                string newName = PromptInput("重命名分组", "请输入新分组名称:", groupName);
                if (string.IsNullOrWhiteSpace(newName)) return;
                newName = newName.Trim();
                if (newName == groupName) return;

                if (_groups.Any(g => g.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                {
                    AntdUI.Message.error(this, $"已存在名为【{newName}】的分组！");
                    return;
                }

                int idx = _groups.IndexOf(groupName);
                if (idx >= 0) _groups[idx] = newName;

                foreach (var p in _presets)
                {
                    if (p.GroupName == groupName) p.GroupName = newName;
                }

                if (_activeGroupName == groupName) _activeGroupName = newName;

                AppSettings.MaterialPresetGroups = _groups;
                SaveAllPresetsToStorage();

                RefreshGroupSidebar();
                RefreshAllPresetDecks();
                AntdUI.Message.success(this, $"分组已成功重命名为【{newName}】！");
            });

            cms.Items.Add("🗑️ 删除分组", null, (s, e) =>
            {
                int count = _presets.Count(p => p.GroupName == groupName);
                string confirmMsg = count > 0
                    ? $"确定要删除分组【{groupName}】吗？\n该组下的 {count} 个预设将自动转移到【未命名】分组。"
                    : $"确定要删除空分组【{groupName}】吗？";

                if (MessageBox.Show(this, confirmMsg, "删除分组确认", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _groups.Remove(groupName);
                    foreach (var p in _presets)
                    {
                        if (p.GroupName == groupName) p.GroupName = "未命名";
                    }

                    if (_activeGroupName == groupName) _activeGroupName = "未命名";

                    AppSettings.MaterialPresetGroups = _groups;
                    SaveAllPresetsToStorage();

                    RefreshGroupSidebar();
                    SwitchActiveGroup(_activeGroupName);
                    AntdUI.Message.success(this, $"分组【{groupName}】已删除！");
                }
            });

            cms.Show(anchor, pt);
        }

        private void SwitchActiveGroup(string groupName)
        {
            _activeGroupName = groupName;
            _lblDeckCurrentGroup.Text = $"全选 · {groupName}";
            _checkedPresets.Clear();
            _chkSelectAll.Checked = false;
            UpdateBatchFloatingBar();

            AppSettings.LastSelectedPresetGroup = groupName;
            AppSettings.Save();

            RefreshGroupSidebar();
            RefreshAllPresetDecks();
        }

        private void RefreshAllPresetDecks()
        {
            if (_lblDeckCurrentGroup != null)
                _lblDeckCurrentGroup.Text = $"全选 · {_activeGroupName}";

            _flpPresetScroll.SuspendLayout();
            _flpPresetScroll.Controls.Clear();

            var currentPresets = _presets.Where(p => (string.IsNullOrWhiteSpace(p.GroupName) ? "未命名" : p.GroupName.Trim()) == _activeGroupName).ToList();

            foreach (var preset in currentPresets)
            {
                var card = CreatePresetRowCard(preset);
                _flpPresetScroll.Controls.Add(card);
            }

            _flpPresetScroll.ResumeLayout(true);

            if (currentPresets.Count > 0 && (_activePreset == null || !currentPresets.Contains(_activePreset)))
            {
                SelectPresetForInspection(currentPresets[0]);
            }
            else if (currentPresets.Count == 0)
            {
                _activePreset = null;
                ClearInspector();
            }
        }

        private Panel CreatePresetRowCard(MaterialSelectionPreset preset)
        {
            bool isActive = preset == _activePreset;
            bool isChecked = _checkedPresets.Contains(preset);
            int dropIndicator = 0; // -1 = top line, 1 = bottom line, 0 = none

            var card = new Panel
            {
                Size = new Size(338, 66),
                Margin = new Padding(0, 0, 0, 8),
                BackColor = isChecked ? Color.FromArgb(240, 247, 255) : SurfaceColor,
                Cursor = Cursors.Hand,
                Tag = preset,
                AllowDrop = true
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                Color bClr = isActive ? PrimaryColor : (isChecked ? PrimaryBorderColor : BorderColor);

                using (var path = CreateRoundedRectanglePath(rect, 6))
                using (var pen = new Pen(bClr, isActive ? 1.5F : 1F))
                {
                    e.Graphics.DrawPath(pen, path);
                }

                if (isActive)
                {
                    using (var b = new SolidBrush(PrimaryColor))
                        e.Graphics.FillRectangle(b, 0, 2, 3, card.Height - 4);
                }

                // 拖拽插入蓝色指示线
                if (dropIndicator == -1)
                {
                    using (var linePen = new Pen(PrimaryColor, 3F))
                    {
                        e.Graphics.DrawLine(linePen, 4, 1, card.Width - 5, 1);
                    }
                }
                else if (dropIndicator == 1)
                {
                    using (var linePen = new Pen(PrimaryColor, 3F))
                    {
                        e.Graphics.DrawLine(linePen, 4, card.Height - 2, card.Width - 5, card.Height - 2);
                    }
                }

                // 拖拽手柄 ⋮⋮
                using (var font = new Font("Microsoft YaHei UI", 9F))
                {
                    TextRenderer.DrawText(e.Graphics, "⋮⋮", font, new Point(6, 22), TextMutedColor);
                }

                // 预设名称与分组药丸 (Row 1)
                using (var fontTitle = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(e.Graphics, preset.Name ?? "", fontTitle, new Point(48, 8), TextMainColor);
                    var sizeTitle = TextRenderer.MeasureText(e.Graphics, preset.Name ?? "", fontTitle);

                    string gName = string.IsNullOrWhiteSpace(preset.GroupName) ? "未命名" : preset.GroupName;
                    var tagRect = new Rectangle(48 + sizeTitle.Width + 6, 9, 46, 16);
                    using (var pathTag = CreateRoundedRectanglePath(tagRect, 3))
                    using (var brushTag = new SolidBrush(Color.FromArgb(240, 242, 245)))
                    {
                        e.Graphics.FillPath(brushTag, pathTag);
                    }
                    using (var fontTag = new Font("Microsoft YaHei UI", 7.5F))
                    {
                        TextRenderer.DrawText(e.Graphics, gName, fontTag, tagRect, TextMutedColor,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                }

                // 彩色 Chips 药丸流 (Row 2, y=34)
                int curX = 48;
                int curY = 34;

                if (preset.DisabledOptions.HasFlag(PresetIgnoreOptions.Material))
                {
                    DrawChip(e.Graphics, ref curX, curY, "材料保持", Color.Transparent, BorderColor, TextMutedColor, true);
                }
                else if (!string.IsNullOrEmpty(preset.SelectedMaterial))
                {
                    DrawChip(e.Graphics, ref curX, curY, preset.SelectedMaterial, PrimaryLightColor, PrimaryBorderColor, PrimaryColor, false, true);
                }

                if (!preset.DisabledOptions.HasFlag(PresetIgnoreOptions.FilmType) && !string.IsNullOrEmpty(preset.FilmType))
                {
                    DrawChip(e.Graphics, ref curX, curY, preset.FilmType, Color.FromArgb(245, 247, 250), BorderLightColor, TextSecondaryColor);
                }

                if (preset.DisabledOptions.HasFlag(PresetIgnoreOptions.OrderNumberMode))
                {
                    DrawChip(e.Graphics, ref curX, curY, "单号保持", Color.Transparent, BorderColor, TextMutedColor, true);
                }
                else if (preset.OrderNumberMode != OrderNumberMode.None)
                {
                    string orderChipText = preset.OrderNumberMode == OrderNumberMode.AutoIncrement
                        ? "自增单号"
                        : (!string.IsNullOrEmpty(preset.OrderRegexName) ? preset.OrderRegexName : "正则单号");
                    DrawChip(e.Graphics, ref curX, curY, orderChipText, Color.FromArgb(249, 240, 255), Color.FromArgb(211, 173, 247), Color.FromArgb(114, 46, 209), false, true);
                }

                if (!preset.DisabledOptions.HasFlag(PresetIgnoreOptions.EnableImposition) && preset.EnableImposition)
                {
                    string impoText = preset.MaterialType == "RollMaterial"
                        ? (preset.LayoutMode == "Folding" ? "卷装·折手" : "卷装·连拼")
                        : (preset.LayoutMode == "Folding" ? "折手" : "连拼");
                    DrawChip(e.Graphics, ref curX, curY, impoText, Color.FromArgb(255, 247, 230), Color.FromArgb(255, 213, 145), Color.FromArgb(250, 140, 22), false, true);
                }

                if (!preset.DisabledOptions.HasFlag(PresetIgnoreOptions.TetBleed) && preset.TetBleed > 0)
                {
                    DrawChip(e.Graphics, ref curX, curY, $"出血 {preset.TetBleed:0.#}", Color.FromArgb(245, 247, 250), BorderLightColor, TextSecondaryColor);
                }

                if (preset.TemporaryImpositionParameters != null)
                {
                    var tip = preset.TemporaryImpositionParameters;
                    bool hasFlat = (tip.IsPaperSizeUnlocked || tip.PaperWidth > 0) && (tip.PaperWidth > 0 || tip.PaperHeight > 0);
                    bool hasRoll = (tip.IsRollSizeUnlocked || tip.FixedWidth > 0) && (tip.FixedWidth > 0 || tip.MinLength > 0);
                    if (hasFlat && hasRoll)
                    {
                        DrawChip(e.Graphics, ref curX, curY, "平+卷大版", Color.FromArgb(230, 247, 255), PrimaryBorderColor, PrimaryColor, false, true);
                    }
                    else if (hasFlat)
                    {
                        DrawChip(e.Graphics, ref curX, curY, "平张大版", Color.FromArgb(230, 247, 255), PrimaryBorderColor, PrimaryColor, false, true);
                    }
                    else if (hasRoll)
                    {
                        DrawChip(e.Graphics, ref curX, curY, "卷装大版", Color.FromArgb(254, 243, 199), Color.FromArgb(252, 211, 77), Color.FromArgb(217, 119, 6), false, true);
                    }
                }

                if (preset.DisabledOptions.HasFlag(PresetIgnoreOptions.ExportPath))
                {
                    DrawChip(e.Graphics, ref curX, curY, "路径保持", Color.Transparent, BorderColor, TextMutedColor, true);
                }
            };

            // 拖拽排序逻辑与事件
            card.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(typeof(MaterialSelectionPreset)))
                {
                    var dragged = e.Data.GetData(typeof(MaterialSelectionPreset)) as MaterialSelectionPreset;
                    if (dragged != null && dragged != preset)
                    {
                        e.Effect = DragDropEffects.Move;
                    }
                }
            };

            card.DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent(typeof(MaterialSelectionPreset)))
                {
                    var dragged = e.Data.GetData(typeof(MaterialSelectionPreset)) as MaterialSelectionPreset;
                    if (dragged != null && dragged != preset)
                    {
                        e.Effect = DragDropEffects.Move;
                        var pt = card.PointToClient(new Point(e.X, e.Y));
                        int newInd = (pt.Y < card.Height / 2) ? -1 : 1;
                        if (dropIndicator != newInd)
                        {
                            dropIndicator = newInd;
                            card.Invalidate();
                        }
                    }
                }
            };

            card.DragLeave += (s, e) =>
            {
                if (dropIndicator != 0)
                {
                    dropIndicator = 0;
                    card.Invalidate();
                }
            };

            card.DragDrop += (s, e) =>
            {
                if (dropIndicator != 0)
                {
                    int finalInd = dropIndicator;
                    dropIndicator = 0;
                    card.Invalidate();
                    if (e.Data.GetDataPresent(typeof(MaterialSelectionPreset)))
                    {
                        var dragged = e.Data.GetData(typeof(MaterialSelectionPreset)) as MaterialSelectionPreset;
                        if (dragged != null && dragged != preset)
                        {
                            ReorderPreset(dragged, preset, finalInd == -1);
                        }
                    }
                }
            };

            // 多选复选框
            var chk = new CheckBox
            {
                Location = new Point(24, 25),
                Size = new Size(16, 16),
                Checked = isChecked,
                Cursor = Cursors.Hand
            };
            chk.CheckedChanged += (s, e) =>
            {
                if (_isUpdatingUi) return;
                if (chk.Checked) _checkedPresets.Add(preset);
                else _checkedPresets.Remove(preset);
                card.Invalidate();
                UpdateBatchFloatingBar();
            };

            // 拖拽手柄及右键快捷操作支持
            card.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left && (e.X < 35 || e.X > 280))
                {
                    card.DoDragDrop(preset, DragDropEffects.Move);
                }
                else if (e.Button == MouseButtons.Right)
                {
                    ShowCardContextMenu(preset, card, e.Location);
                }
            };

            // 图钉按钮 (📌 28 × 28px 方圆角)
            var btnPin = new PinCardButton
            {
                Location = new Point(302, 19),
                Size = new Size(28, 28),
                IsPinned = preset.ShowInPresetButtons
            };
            btnPin.Click += (s, e) =>
            {
                preset.ShowInPresetButtons = !preset.ShowInPresetButtons;
                btnPin.IsPinned = preset.ShowInPresetButtons;
                SaveAllPresetsToStorage();
                if (preset == _activePreset) _chkShowInButtons.Checked = preset.ShowInPresetButtons;
            };

            Action selectCard = () =>
            {
                SelectPresetForInspection(preset);
                foreach (Control c in _flpPresetScroll.Controls) c.Invalidate();
            };

            card.Click += (s, e) => selectCard();

            card.Controls.Add(chk);
            card.Controls.Add(btnPin);

            return card;
        }

        private static void DrawChip(Graphics g, ref int curX, int curY, string text, Color fill, Color border, Color textClr, bool isIgnored = false, bool isBold = false)
        {
            using (var font = new Font("Microsoft YaHei UI", 7.5F, isBold ? FontStyle.Bold : FontStyle.Regular))
            {
                var size = TextRenderer.MeasureText(g, text, font, Size.Empty, TextFormatFlags.NoPadding);
                int w = size.Width + 10;
                int h = 18;
                var rect = new Rectangle(curX, curY, w, h);

                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(rect, 3))
                {
                    if (fill != Color.Transparent)
                    {
                        using (var b = new SolidBrush(fill))
                            g.FillPath(b, path);
                    }
                    using (var pen = new Pen(border, 1F))
                    {
                        if (isIgnored) pen.DashStyle = DashStyle.Dash;
                        g.DrawPath(pen, path);
                    }
                }

                TextRenderer.DrawText(g, text, font, rect, textClr,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                if (isIgnored)
                {
                    using (var linePen = new Pen(textClr, 1F))
                    {
                        int lineY = curY + h / 2;
                        g.DrawLine(linePen, curX + 4, lineY, curX + w - 4, lineY);
                    }
                }

                curX += w + 4;
            }
        }

        private void SelectPresetForInspection(MaterialSelectionPreset preset)
        {
            _activePreset = preset;
            if (preset == null) return;

            _isUpdatingUi = true;
            try
            {
                _txtPresetName.Text = preset.Name ?? "";
                _cmbGroupName.Text = string.IsNullOrWhiteSpace(preset.GroupName) ? "未命名" : preset.GroupName;
                _chkShowInButtons.Checked = preset.ShowInPresetButtons;

                // 舱 1：材料与工艺
                _selMaterial.Text = preset.SelectedMaterial ?? "";
                _selColorMode.Text = preset.ColorMode ?? "彩色";
                _selFilmType.Text = preset.FilmType ?? "光膜";
                _chkAddIdentifierPage.Checked = preset.AddIdentifierPage;

                string shapeDesc = preset.ShapeState switch
                {
                    "RightAngle" => "▢ 直角",
                    "Circle" => "○ 圆形",
                    "Special" => "◇ 异形",
                    "RoundRect" => "▢ 圆角 R",
                    _ => "无"
                };
                _selShapeState.Text = shapeDesc;
                _numRoundRadius.Value = (decimal)(preset.RoundRadius > 0 ? preset.RoundRadius : 5);
                _numRoundRadius.Visible = preset.ShapeState == "RoundRect";

                // 舱 2：订单与编号规则
                string orderModeStr = preset.OrderNumberMode switch
                {
                    OrderNumberMode.AutoIncrement => "自动递增",
                    OrderNumberMode.RegexExtraction => "正则提取",
                    _ => "无"
                };
                _selOrderNumberMode.Text = orderModeStr;
                _selOrderRegexName.Visible = preset.OrderNumberMode == OrderNumberMode.RegexExtraction;
                _selOrderRegexName.Text = preset.OrderRegexName ?? "";

                // 舱 3：排版几何
                _selEnableImposition.Text = preset.EnableImposition ? "启用计算" : "停用计算";
                _numTetBleed.Value = (decimal)(preset.TetBleed >= 0 ? preset.TetBleed : 2.0);
                _selMaterialType.Text = preset.MaterialType == "RollMaterial" ? "卷装" : "平张";
                _selLayoutMode.Text = preset.LayoutMode == "Folding" ? "折手" : "连拼";
                _chkIsDualCopy.Checked = preset.IsDualCopy;
                _numCopyCount.Value = Math.Max(2, Math.Min(16, preset.CopyCount));
                _selCopyType.Text = preset.CopyType == CopyType.Duplicate ? "份" : "联";
                _selCopyMode.Text = preset.CopyMode switch
                {
                    CopyMode.AutoByRow => "自适应行",
                    CopyMode.FixedNoRotationByColumn => "不旋转列",
                    CopyMode.FixedNoRotationByRow => "不旋转行",
                    _ => "自适应列"
                };
                bool dualEnabled = !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.IsDualCopy) && preset.IsDualCopy;
                _numCopyCount.Enabled = dualEnabled;
                _selCopyType.Enabled = dualEnabled;
                _selCopyMode.Enabled = dualEnabled;

                // 嵌入式大版排版参数 (平张与卷装分开独立启动)
                var p = preset.TemporaryImpositionParameters;
                bool hasFlat = p != null && p.IsPaperSizeUnlocked && (p.PaperWidth > 0 || p.PaperHeight > 0);
                bool hasRoll = p != null && p.IsRollSizeUnlocked && (p.FixedWidth > 0 || p.MinLength > 0);

                // 兼容旧预设数据（若旧数据中有值但未细分解锁标记）
                if (p != null)
                {
                    if (!hasFlat && (p.PaperWidth > 0 && p.PaperHeight > 0)) hasFlat = true;
                    if (!hasRoll && (p.FixedWidth > 0 && p.MinLength > 0)) hasRoll = true;
                }

                _chkEnableFlatImpo.Checked = hasFlat;
                _lblFlatImpoHint.Text = hasFlat ? "已激活：预设专属平张" : "未激活：遵循全局设置";
                _lblFlatImpoHint.ForeColor = hasFlat ? PrimaryColor : TextMutedColor;
                SetFlatImpositionInputsEnabled(hasFlat);
                if (hasFlat)
                {
                    LoadPresetFlatImpositionParameters(p);
                }
                else
                {
                    ReloadGlobalFlatImpositionParameters();
                }

                _chkEnableRollImpo.Checked = hasRoll;
                _lblRollImpoHint.Text = hasRoll ? "已激活：预设专属卷装" : "未激活：遵循全局设置";
                _lblRollImpoHint.ForeColor = hasRoll ? Color.FromArgb(217, 119, 6) : TextMutedColor;
                SetRollImpositionInputsEnabled(hasRoll);
                if (hasRoll)
                {
                    LoadPresetRollImpositionParameters(p);
                }
                else
                {
                    ReloadGlobalRollImpositionParameters();
                }

                // 舱 4：导出路径
                _txtExportPath.Text = preset.ExportPath ?? "";

                // 同步所有跟随状态微型锁标
                UpdateLatchState(PresetIgnoreOptions.Material, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.Material), _selMaterial);
                UpdateLatchState(PresetIgnoreOptions.ColorMode, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.ColorMode), _selColorMode);
                UpdateLatchState(PresetIgnoreOptions.FilmType, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.FilmType), _selFilmType);
                UpdateLatchState(PresetIgnoreOptions.Shape, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.Shape), _selShapeState);
                UpdateLatchState(PresetIgnoreOptions.IdentifierPage, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.IdentifierPage), _chkAddIdentifierPage);
                UpdateLatchState(PresetIgnoreOptions.OrderNumberMode, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.OrderNumberMode), _selOrderNumberMode);
                UpdateLatchState(PresetIgnoreOptions.EnableImposition, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.EnableImposition), _selEnableImposition);
                UpdateLatchState(PresetIgnoreOptions.TetBleed, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.TetBleed), _numTetBleed);
                UpdateLatchState(PresetIgnoreOptions.MaterialType, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.MaterialType), _selMaterialType);
                UpdateLatchState(PresetIgnoreOptions.LayoutMode, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.LayoutMode), _selLayoutMode);
                UpdateLatchState(PresetIgnoreOptions.IsDualCopy, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.IsDualCopy), _chkIsDualCopy);
                UpdateLatchState(PresetIgnoreOptions.ExportPath, !preset.DisabledOptions.HasFlag(PresetIgnoreOptions.ExportPath), _txtExportPath);
            }
            finally
            {
                _isUpdatingUi = false;
            }
        }

        private void UpdateLatchState(PresetIgnoreOptions opt, bool isFollow, Control targetControl)
        {
            if (_latchMap.TryGetValue(opt, out var latch))
            {
                latch.IsFollowing = isFollow;
                if (targetControl != null) targetControl.Enabled = isFollow;
            }
        }

        private void ClearInspector()
        {
            _isUpdatingUi = true;
            _txtPresetName.Text = "";
            _txtExportPath.Text = "";
            _isUpdatingUi = false;
        }

        private void CommitActivePresetChanges()
        {
            if (_activePreset == null) return;
            _activePreset.Name = _txtPresetName.Text.Trim();
            _activePreset.GroupName = string.IsNullOrWhiteSpace(_cmbGroupName.Text) ? "未命名" : _cmbGroupName.Text.Trim();
            _activePreset.ShowInPresetButtons = _chkShowInButtons.Checked;

            _activePreset.SelectedMaterial = _selMaterial.Text;
            _activePreset.ColorMode = _selColorMode.Text;
            _activePreset.FilmType = _selFilmType.Text;
            _activePreset.AddIdentifierPage = _chkAddIdentifierPage.Checked;

            _activePreset.ShapeState = _selShapeState.Text switch
            {
                var s when s.Contains("直角") => "RightAngle",
                var s when s.Contains("圆形") => "Circle",
                var s when s.Contains("异形") => "Special",
                var s when s.Contains("圆角") => "RoundRect",
                _ => ""
            };
            _activePreset.RoundRadius = (double)_numRoundRadius.Value;

            _activePreset.OrderNumberMode = _selOrderNumberMode.Text switch
            {
                "自动递增" => OrderNumberMode.AutoIncrement,
                "正则提取" => OrderNumberMode.RegexExtraction,
                _ => OrderNumberMode.None
            };
            _activePreset.OrderRegexName = _selOrderRegexName.Text.Trim();

            _activePreset.EnableImposition = _selEnableImposition.Text == "启用计算";
            _activePreset.TetBleed = (double)_numTetBleed.Value;
            _activePreset.MaterialType = _selMaterialType.Text == "卷装" ? "RollMaterial" : "FlatSheet";
            _activePreset.LayoutMode = _selLayoutMode.Text == "折手" ? "Folding" : "Continuous";

            _activePreset.IsDualCopy = _chkIsDualCopy.Checked;
            _activePreset.CopyCount = (int)_numCopyCount.Value;
            _activePreset.CopyType = _selCopyType.Text == "份" ? CopyType.Duplicate : CopyType.Layout;
            _activePreset.CopyMode = _selCopyMode.Text switch
            {
                "自适应行" => CopyMode.AutoByRow,
                "不旋转列" => CopyMode.FixedNoRotationByColumn,
                "不旋转行" => CopyMode.FixedNoRotationByRow,
                _ => CopyMode.AutoByColumn
            };

            // 嵌入式大版参数保存（平张与卷装分开独立保存）
            bool flatActive = _chkEnableFlatImpo != null && _chkEnableFlatImpo.Checked;
            bool rollActive = _chkEnableRollImpo != null && _chkEnableRollImpo.Checked;

            if (flatActive || rollActive)
            {
                var p = _activePreset.TemporaryImpositionParameters ?? new TemporaryImpositionParameters();

                // 平张独立保存
                if (flatActive)
                {
                    p.IsPaperSizeUnlocked = true;
                    p.PaperWidth = (float)_numFlatPaperWidth.Value;
                    p.PaperHeight = (float)_numFlatPaperHeight.Value;
                    p.MarginTop = (float)_numFlatMarginTop.Value;
                    p.MarginBottom = (float)_numFlatMarginBottom.Value;
                    p.MarginLeft = (float)_numFlatMarginLeft.Value;
                    p.MarginRight = (float)_numFlatMarginRight.Value;
                }
                else
                {
                    p.IsPaperSizeUnlocked = false;
                    p.PaperWidth = 0;
                    p.PaperHeight = 0;
                    p.MarginTop = 0;
                    p.MarginBottom = 0;
                    p.MarginLeft = 0;
                    p.MarginRight = 0;
                }

                // 卷装独立保存
                if (rollActive)
                {
                    p.IsRollSizeUnlocked = true;
                    p.FixedWidth = (float)_numRollFixedWidth.Value;
                    p.MinLength = (float)_numRollMinLength.Value;
                    p.RollMarginTop = (float)_numRollMarginTop.Value;
                    p.RollMarginBottom = (float)_numRollMarginBottom.Value;
                    p.RollMarginLeft = (float)_numRollMarginLeft.Value;
                    p.RollMarginRight = (float)_numRollMarginRight.Value;
                }
                else
                {
                    p.IsRollSizeUnlocked = false;
                    p.FixedWidth = 0;
                    p.MinLength = 0;
                    p.RollMarginTop = 0;
                    p.RollMarginBottom = 0;
                    p.RollMarginLeft = 0;
                    p.RollMarginRight = 0;
                }

                _activePreset.TemporaryImpositionParameters = p;
            }
            else
            {
                _activePreset.TemporaryImpositionParameters = null;
            }

            _activePreset.ExportPath = _txtExportPath.Text.Trim();
        }

        private void SaveAllPresetsToStorage()
        {
            AppSettings.MaterialPresets = _presets;
            AppSettings.Save();
        }

        // =============================================================
        // 组内顺序调整引擎 (拖拽排序 / 微调按钮 / 置顶置底 / 字母排序)
        // =============================================================

        private void ReorderPreset(MaterialSelectionPreset source, MaterialSelectionPreset target, bool insertBefore)
        {
            if (source == null || target == null || source == target) return;

            if (source.GroupName != target.GroupName)
            {
                source.GroupName = target.GroupName;
            }

            _presets.Remove(source);
            int targetIdx = _presets.IndexOf(target);
            if (targetIdx < 0) targetIdx = 0;

            int insertIdx = insertBefore ? targetIdx : targetIdx + 1;
            if (insertIdx > _presets.Count) insertIdx = _presets.Count;
            if (insertIdx < 0) insertIdx = 0;

            _presets.Insert(insertIdx, source);

            SaveAllPresetsToStorage();
            RefreshGroupSidebar();
            RefreshAllPresetDecks();
            SelectPresetForInspection(source);
            AntdUI.Message.success(this, $"预设【{source.Name}】顺序已调整！");
        }

        private void MovePresetRelative(MaterialSelectionPreset preset, bool moveUp)
        {
            if (preset == null) return;
            var currentGroupPresets = _presets.Where(p => (string.IsNullOrWhiteSpace(p.GroupName) ? "未命名" : p.GroupName.Trim()) == _activeGroupName).ToList();
            int groupIdx = currentGroupPresets.IndexOf(preset);
            if (groupIdx < 0) return;

            int targetGroupIdx = moveUp ? groupIdx - 1 : groupIdx + 1;
            if (targetGroupIdx < 0 || targetGroupIdx >= currentGroupPresets.Count) return;

            var target = currentGroupPresets[targetGroupIdx];
            ReorderPreset(preset, target, moveUp);
        }

        private void MovePresetToExtremum(MaterialSelectionPreset preset, bool toTop)
        {
            if (preset == null) return;
            var currentGroupPresets = _presets.Where(p => (string.IsNullOrWhiteSpace(p.GroupName) ? "未命名" : p.GroupName.Trim()) == _activeGroupName).ToList();
            if (currentGroupPresets.Count <= 1) return;

            var target = toTop ? currentGroupPresets.First() : currentGroupPresets.Last();
            if (target == preset) return;

            ReorderPreset(preset, target, toTop);
        }

        private void SortCurrentGroupPresets(bool ascending)
        {
            var otherPresets = _presets.Where(p => (string.IsNullOrWhiteSpace(p.GroupName) ? "未命名" : p.GroupName.Trim()) != _activeGroupName).ToList();
            var currentPresets = _presets.Where(p => (string.IsNullOrWhiteSpace(p.GroupName) ? "未命名" : p.GroupName.Trim()) == _activeGroupName).ToList();

            currentPresets = ascending
                ? currentPresets.OrderBy(p => p.Name).ToList()
                : currentPresets.OrderByDescending(p => p.Name).ToList();

            var newPresets = new List<MaterialSelectionPreset>();
            bool inserted = false;
            foreach (var p in _presets)
            {
                string grp = string.IsNullOrWhiteSpace(p.GroupName) ? "未命名" : p.GroupName.Trim();
                if (grp == _activeGroupName)
                {
                    if (!inserted)
                    {
                        newPresets.AddRange(currentPresets);
                        inserted = true;
                    }
                }
                else
                {
                    newPresets.Add(p);
                }
            }
            if (!inserted) newPresets.AddRange(currentPresets);

            _presets = newPresets;
            SaveAllPresetsToStorage();
            RefreshAllPresetDecks();
            AntdUI.Message.success(this, $"当前组预设已按名称{(ascending ? "升序" : "降序")}排序");
        }

        private void ShowSortContextMenu(Control anchor)
        {
            var cms = new ContextMenuStrip();
            cms.Items.Add("↑ 上移当前预设", null, (s, e) => MovePresetRelative(_activePreset, true));
            cms.Items.Add("↓ 下移当前预设", null, (s, e) => MovePresetRelative(_activePreset, false));
            cms.Items.Add("🔝 置顶当前预设", null, (s, e) => MovePresetToExtremum(_activePreset, true));
            cms.Items.Add("🔚 置底当前预设", null, (s, e) => MovePresetToExtremum(_activePreset, false));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("🔤 按名称升序 (A-Z)", null, (s, e) => SortCurrentGroupPresets(true));
            cms.Items.Add("🔤 按名称降序 (Z-A)", null, (s, e) => SortCurrentGroupPresets(false));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("💾 导出全部预设备份 (.json)...", null, (s, e) => ExportPresetsToFile());
            cms.Items.Add("📂 从备份文件导入预设 (.json)...", null, (s, e) => ImportPresetsFromFile());
            cms.Items.Add("🕒 打开系统自动备份目录...", null, (s, e) => OpenBackupsFolder());
            cms.Show(anchor, new Point(0, anchor.Height + 2));
        }

        private void ExportPresetsToFile()
        {
            try
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Title = "导出全部预设备份";
                    sfd.Filter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                    sfd.FileName = $"材料预设备份_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        var backupData = new
                        {
                            ExportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            Version = "2.7.0",
                            PresetGroups = _groups,
                            Presets = _presets
                        };
                        var json = JsonConvert.SerializeObject(backupData, Formatting.Indented);
                        File.WriteAllText(sfd.FileName, json);
                        AntdUI.Message.success(this, $"成功导出 {_presets.Count} 个预设及 {_groups.Count} 个分组到备份文件！");
                    }
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, "导出预设备份失败: " + ex.Message);
            }
        }

        private void ImportPresetsFromFile()
        {
            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "从备份文件导入预设";
                    ofd.Filter = "JSON 配置文件 (*.json)|*.json|所有文件 (*.*)|*.*";
                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        var json = File.ReadAllText(ofd.FileName);
                        List<MaterialSelectionPreset> importedPresets = null;
                        List<string> importedGroups = null;

                        try
                        {
                            var token = Newtonsoft.Json.Linq.JToken.Parse(json);
                            if (token is Newtonsoft.Json.Linq.JObject obj)
                            {
                                if (obj["Presets"] != null)
                                {
                                    importedPresets = obj["Presets"].ToObject<List<MaterialSelectionPreset>>();
                                }
                                else if (obj["MaterialPresets"] != null)
                                {
                                    importedPresets = obj["MaterialPresets"].ToObject<List<MaterialSelectionPreset>>();
                                }
                                if (obj["PresetGroups"] != null)
                                {
                                    importedGroups = obj["PresetGroups"].ToObject<List<string>>();
                                }
                                else if (obj["MaterialPresetGroups"] != null)
                                {
                                    importedGroups = obj["MaterialPresetGroups"].ToObject<List<string>>();
                                }
                            }
                            else if (token is Newtonsoft.Json.Linq.JArray arr)
                            {
                                importedPresets = arr.ToObject<List<MaterialSelectionPreset>>();
                            }
                        }
                        catch
                        {
                        }

                        if (importedPresets == null || importedPresets.Count == 0)
                        {
                            AntdUI.Message.error(this, "无法从所选文件中解析出有效预设数据！");
                            return;
                        }

                        var result = MessageBox.Show(this,
                            $"发现 {importedPresets.Count} 个预设。\n\n点击【是】：清空现有预设，完全以导入文件为准（覆盖）\n点击【否】：保留现有预设，仅将导入的预设追加合并进来\n点击【取消】：放弃导入",
                            "选择导入方式",
                            MessageBoxButtons.YesNoCancel,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Cancel) return;

                        if (result == DialogResult.Yes)
                        {
                            _presets.Clear();
                            _presets.AddRange(importedPresets);
                            if (importedGroups != null && importedGroups.Count > 0)
                            {
                                _groups = importedGroups;
                            }
                        }
                        else
                        {
                            foreach (var imp in importedPresets)
                            {
                                var existing = _presets.FirstOrDefault(p => p.Name.Equals(imp.Name, StringComparison.OrdinalIgnoreCase));
                                if (existing != null)
                                {
                                    imp.Name = imp.Name + " (导入)";
                                }
                                _presets.Add(imp);
                            }
                            if (importedGroups != null)
                            {
                                foreach (var g in importedGroups)
                                {
                                    if (!_groups.Contains(g, StringComparer.OrdinalIgnoreCase))
                                        _groups.Add(g);
                                }
                            }
                        }

                        if (!_groups.Contains("未命名", StringComparer.OrdinalIgnoreCase))
                            _groups.Insert(0, "未命名");

                        AppSettings.MaterialPresetGroups = _groups;
                        SaveAllPresetsToStorage();
                        RefreshGroupSidebar();
                        RefreshAllPresetDecks();

                        AntdUI.Message.success(this, $"成功导入 {importedPresets.Count} 个预设！");
                    }
                }
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, "导入预设备份失败: " + ex.Message);
            }
        }

        private void OpenBackupsFolder()
        {
            try
            {
                var dir = AppDataPathManager.BackupsDirectory;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
            catch (Exception ex)
            {
                AntdUI.Message.error(this, "打开备份文件夹失败: " + ex.Message);
            }
        }

        private void ShowCardContextMenu(MaterialSelectionPreset p, Control anchor, Point pt)
        {
            SelectPresetForInspection(p);
            var cms = new ContextMenuStrip();
            cms.Items.Add("↑ 上移", null, (s, e) => MovePresetRelative(p, true));
            cms.Items.Add("↓ 下移", null, (s, e) => MovePresetRelative(p, false));
            cms.Items.Add("🔝 置顶", null, (s, e) => MovePresetToExtremum(p, true));
            cms.Items.Add("🔚 置底", null, (s, e) => MovePresetToExtremum(p, false));
            cms.Items.Add(new ToolStripSeparator());
            cms.Items.Add("📋 另存副本", null, (s, e) => CloneActivePreset());
            cms.Items.Add("🗑️ 删除预设", null, (s, e) => DeleteActivePreset());
            cms.Show(anchor, pt);
        }

        // =============================================================
        // 批量多选、一键移动分组与搜索
        // =============================================================

        private void ToggleSelectAllCards(bool selectAll)
        {
            _isUpdatingUi = true;
            try
            {
                var visiblePresets = _presets.Where(p => (string.IsNullOrWhiteSpace(p.GroupName) ? "未命名" : p.GroupName.Trim()) == _activeGroupName).ToList();
                if (selectAll)
                {
                    foreach (var p in visiblePresets) _checkedPresets.Add(p);
                }
                else
                {
                    _checkedPresets.Clear();
                }

                foreach (Control card in _flpPresetScroll.Controls)
                {
                    if (card is Panel pnl)
                    {
                        var chk = pnl.Controls.OfType<CheckBox>().FirstOrDefault();
                        if (chk != null) chk.Checked = selectAll;
                        pnl.Invalidate();
                    }
                }
            }
            finally
            {
                _isUpdatingUi = false;
            }
            UpdateBatchFloatingBar();
        }

        private void UpdateBatchFloatingBar()
        {
            int count = _checkedPresets.Count;
            if (count > 0)
            {
                _lblBatchCount.Text = $"已选 {count} 项";
                _lblBatchCount.ForeColor = PrimaryColor;
                _lblBatchCount.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
                if (_btnMoveGroup != null)
                {
                    _btnMoveGroup.Type = AntdUI.TTypeMini.Primary;
                    _btnMoveGroup.Ghost = false;
                }
            }
            else
            {
                _lblBatchCount.Text = "已选 0 项";
                _lblBatchCount.ForeColor = TextMutedColor;
                _lblBatchCount.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular);
                if (_btnMoveGroup != null)
                {
                    _btnMoveGroup.Type = AntdUI.TTypeMini.Default;
                    _btnMoveGroup.Ghost = true;
                }
            }
            _pnlBatchFloatingBar.Visible = true;
            _pnlBatchFloatingBar.BringToFront();
        }

        private void ExecuteBatchMoveGroup(string targetGroup)
        {
            string grp = string.IsNullOrWhiteSpace(targetGroup) ? "未命名" : targetGroup.Trim();
            if (!_groups.Contains(grp, StringComparer.OrdinalIgnoreCase))
            {
                _groups.Add(grp);
                AppSettings.MaterialPresetGroups = _groups;
            }

            if (_checkedPresets.Count > 0)
            {
                foreach (var p in _checkedPresets)
                {
                    p.GroupName = grp;
                }
                int movedCount = _checkedPresets.Count;
                _checkedPresets.Clear();
                _chkSelectAll.Checked = false;
                UpdateBatchFloatingBar();

                SaveAllPresetsToStorage();
                RefreshGroupSidebar();
                SwitchActiveGroup(grp);

                AntdUI.Message.success(this, $"已成功将 {movedCount} 个预设批量移动至【{grp}】！");
            }
            else if (_activePreset != null)
            {
                _activePreset.GroupName = grp;
                SaveAllPresetsToStorage();
                RefreshGroupSidebar();
                SwitchActiveGroup(grp);

                AntdUI.Message.success(this, $"已将预设 [{_activePreset.Name}] 移动至【{grp}】！");
            }
            else
            {
                AntdUI.Message.info(this, "请先在上方勾选或选择需要移动的预设卡片！");
            }
        }

        private void ExecuteBatchTogglePin(bool pin)
        {
            if (_checkedPresets.Count > 0)
            {
                foreach (var p in _checkedPresets)
                {
                    p.ShowInPresetButtons = pin;
                }
                int count = _checkedPresets.Count;
                SaveAllPresetsToStorage();
                RefreshAllPresetDecks();
                AntdUI.Message.success(this, $"已批量{(pin ? "开启" : "取消")} {count} 个预设的快捷药丸上屏！");
            }
            else if (_activePreset != null)
            {
                _activePreset.ShowInPresetButtons = pin;
                SaveAllPresetsToStorage();
                RefreshAllPresetDecks();
                AntdUI.Message.success(this, $"已{(pin ? "开启" : "取消")}预设 [{_activePreset.Name}] 的快捷药丸上屏！");
            }
            else
            {
                AntdUI.Message.info(this, "请先在上方勾选或选择预设卡片！");
            }
        }

        private void ExecuteBatchDelete()
        {
            if (_checkedPresets.Count > 0)
            {
                if (MessageBox.Show(this, $"确定要永久删除已选中的 {_checkedPresets.Count} 个预设吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    _presets.RemoveAll(p => _checkedPresets.Contains(p));
                    _checkedPresets.Clear();
                    _chkSelectAll.Checked = false;
                    UpdateBatchFloatingBar();

                    SaveAllPresetsToStorage();
                    RefreshGroupSidebar();
                    RefreshAllPresetDecks();
                    AntdUI.Message.success(this, "批量预设删除成功！");
                }
            }
            else if (_activePreset != null)
            {
                DeleteActivePreset();
            }
            else
            {
                AntdUI.Message.info(this, "请先在上方勾选或选择要删除的预设卡片！");
            }
        }

        private void FilterPresetCards(string keyword)
        {
            string kw = (keyword ?? "").Trim().ToLower();
            _flpPresetScroll.SuspendLayout();
            foreach (Control c in _flpPresetScroll.Controls)
            {
                if (c is Panel card)
                {
                    var preset = card.Tag as MaterialSelectionPreset;
                    if (preset == null) continue;

                    bool match = string.IsNullOrEmpty(kw) ||
                                 (preset.Name ?? "").ToLower().Contains(kw) ||
                                 (preset.SelectedMaterial ?? "").ToLower().Contains(kw) ||
                                 (preset.FilmType ?? "").ToLower().Contains(kw);

                    card.Visible = match;
                }
            }
            _flpPresetScroll.ResumeLayout(true);
        }

        // =============================================================
        // 增删与副本操作
        // =============================================================

        private void PromptCreateNewGroup()
        {
            string name = PromptInput("新建预设分组", "请输入分组名称:");
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();

            if (name.Equals("未命名", StringComparison.OrdinalIgnoreCase))
            {
                AntdUI.Message.info(this, "【未命名】为默认基础分组，已存在。");
                SwitchActiveGroup("未命名");
                return;
            }

            if (_groups.Any(g => g.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                AntdUI.Message.info(this, $"分组【{name}】已存在，已为您切换到该分组。");
                SwitchActiveGroup(name);
                return;
            }

            _groups.Add(name);
            AppSettings.MaterialPresetGroups = _groups;
            AppSettings.Save();

            RefreshGroupSidebar();
            SwitchActiveGroup(name);
            AntdUI.Message.success(this, $"预设分组【{name}】创建成功！");
        }

        private void PromptCreateNewPreset()
        {
            string name = PromptInput("新建材料预设", "请输入新预设名称:");
            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();

            if (_presets.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                AntdUI.Message.error(this, $"已存在名为 [{name}] 的预设！");
                return;
            }

            var newPreset = new MaterialSelectionPreset
            {
                Name = name,
                GroupName = _activeGroupName,
                SelectedMaterial = "PET透明",
                TetBleed = 2.0,
                ColorMode = "彩色",
                FilmType = "光膜",
                EnableImposition = true,
                MaterialType = "FlatSheet",
                LayoutMode = "Continuous",
                ShowInPresetButtons = true,
                DisabledOptions = PresetIgnoreOptions.None
            };
            _presets.Add(newPreset);
            SaveAllPresetsToStorage();
            RefreshGroupSidebar();
            RefreshAllPresetDecks();
            SelectPresetForInspection(newPreset);
            AntdUI.Message.success(this, $"预设 [{name}] 创建成功！");
        }

        private void CloneActivePreset()
        {
            if (_activePreset == null) return;
            CommitActivePresetChanges();
            string newName = PromptInput("另存为新预设副本", "请输入副本名称:", _activePreset.Name + "_副本");
            if (string.IsNullOrWhiteSpace(newName)) return;

            var clone = _activePreset.Clone();
            clone.Name = newName.Trim();
            _presets.Add(clone);
            SaveAllPresetsToStorage();
            RefreshGroupSidebar();
            RefreshAllPresetDecks();
            SelectPresetForInspection(clone);
            AntdUI.Message.success(this, $"已成功复制新预设 [{newName}]！");
        }

        private void DeleteActivePreset()
        {
            if (_activePreset == null) return;
            if (MessageBox.Show(this, $"确定要删除预设 [{_activePreset.Name}] 吗？", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _presets.Remove(_activePreset);
                SaveAllPresetsToStorage();
                RefreshGroupSidebar();
                RefreshAllPresetDecks();
                AntdUI.Message.success(this, "预设已成功删除！");
            }
        }

        private void OpenTemporaryImpositionDialog()
        {
            if (_activePreset == null) return;
            bool isFlat = _activePreset.MaterialType != "RollMaterial";
            var currentParams = _activePreset.TemporaryImpositionParameters ?? new TemporaryImpositionParameters();
            using (var dlg = new TemporaryImpositionParametersForm(isFlat, currentParams))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _activePreset.TemporaryImpositionParameters = dlg.Parameters;
                    AntdUI.Message.success(this, "临时大版开料纸张与边距参数已成功绑定至该预设！");
                }
            }
        }

        private void RefreshActivePresetCardTitle()
        {
            foreach (Control c in _flpPresetScroll.Controls)
            {
                if (c is Panel card) card.Invalidate();
            }
        }

        private void RefreshActivePresetChips()
        {
            foreach (Control c in _flpPresetScroll.Controls)
            {
                if (c is Panel card) card.Invalidate();
            }
        }

        private void RefreshActivePresetPinIcon()
        {
            foreach (Control c in _flpPresetScroll.Controls)
            {
                if (c is Panel card)
                {
                    var btnPin = card.Controls.OfType<PinCardButton>().FirstOrDefault();
                    if (btnPin != null && _activePreset != null)
                        btnPin.IsPinned = _activePreset.ShowInPresetButtons;
                }
            }
        }

        private string PromptInput(string title, string prompt, string defaultVal = "")
        {
            using (var form = new Form())
            using (var lbl = new Label())
            using (var txt = new AntdUI.Input())
            using (var btnOk = new AntdUI.Button())
            using (var btnCancel = new AntdUI.Button())
            {
                form.Text = title;
                form.ClientSize = new Size(320, 130);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.BackColor = SurfaceColor;
                form.Font = new Font("Microsoft YaHei UI", 9F);

                form.KeyPreview = true;
                form.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        form.DialogResult = DialogResult.OK;
                        form.Close();
                    }
                    else if (e.KeyCode == Keys.Escape)
                    {
                        e.Handled = true;
                        form.DialogResult = DialogResult.Cancel;
                        form.Close();
                    }
                };

                lbl.Text = prompt;
                lbl.Location = new Point(16, 12);
                lbl.AutoSize = true;

                txt.Location = new Point(16, 36);
                txt.Size = new Size(288, 32);
                txt.Text = defaultVal;
                txt.Radius = 4;
                txt.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        form.DialogResult = DialogResult.OK;
                        form.Close();
                    }
                };

                btnOk.Text = "确定";
                btnOk.Location = new Point(136, 80);
                btnOk.Size = new Size(80, 30);
                btnOk.Type = AntdUI.TTypeMini.Primary;
                btnOk.Radius = 4;
                btnOk.Click += (s, e) => { form.DialogResult = DialogResult.OK; form.Close(); };

                btnCancel.Text = "取消";
                btnCancel.Location = new Point(224, 80);
                btnCancel.Size = new Size(80, 30);
                btnCancel.Type = AntdUI.TTypeMini.Default;
                btnCancel.Radius = 4;
                btnCancel.Click += (s, e) => { form.DialogResult = DialogResult.Cancel; form.Close(); };

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });

                return form.ShowDialog(this) == DialogResult.OK ? txt.Text : null;
            }
        }

        // =============================================================
        // 高性能矢量微图标引擎 (彻底告别字体缺失和乱码)
        // =============================================================

        public static void DrawVectorIcon(Graphics g, string iconName, Rectangle rect, Color color)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var pen = new Pen(color, 1.4F))
            using (var brush = new SolidBrush(color))
            {
                int x = rect.X;
                int y = rect.Y;
                int w = rect.Width;
                int h = rect.Height;

                switch (iconName)
                {
                    case "folder":
                        // 文件夹矢量轮廓
                        using (var path = new GraphicsPath())
                        {
                            path.AddLine(x + 2, y + 4, x + (int)(w * 0.45F), y + 4);
                            path.AddLine(x + (int)(w * 0.55F), y + 6, x + w - 2, y + 6);
                            path.AddLine(x + w - 2, y + h - 2, x + 2, y + h - 2);
                            path.CloseFigure();
                            g.DrawPath(pen, path);
                            using (var fillB = new SolidBrush(Color.FromArgb(40, color)))
                                g.FillPath(fillB, path);
                        }
                        break;

                    case "tag":
                        // 标签矢量图形
                        using (var path = new GraphicsPath())
                        {
                            path.AddLine(x + 5, y + 2, x + w - 2, y + 2);
                            path.AddLine(x + w - 2, y + h - 3, x + 5, y + h - 3);
                            path.AddLine(x + 1, y + h / 2, x + 5, y + 2);
                            path.CloseFigure();
                            g.DrawPath(pen, path);
                            g.FillEllipse(brush, x + 4, y + h / 2 - 2, 3, 3);
                        }
                        break;

                    case "roll":
                        // 卷轴圆形轮廓与连续纸带
                        g.DrawEllipse(pen, x + 2, y + 2, w - 4, h - 4);
                        g.DrawEllipse(pen, x + w / 2 - 2, y + h / 2 - 2, 4, 4);
                        g.DrawLine(pen, x + w - 2, y + h / 2, x + w, y + h / 2);
                        break;

                    case "sparkle":
                        // 四角闪光星芒
                        using (var path = new GraphicsPath())
                        {
                            int cx = x + w / 2;
                            int cy = y + h / 2;
                            path.AddCurve(new Point[] {
                                new Point(cx, y + 1),
                                new Point(cx + 2, cy - 2),
                                new Point(x + w - 1, cy),
                                new Point(cx + 2, cy + 2),
                                new Point(cx, y + h - 1),
                                new Point(cx - 2, cy + 2),
                                new Point(x + 1, cy),
                                new Point(cx - 2, cy - 2),
                                new Point(cx, y + 1)
                            });
                            g.FillPath(brush, path);
                        }
                        break;

                    case "gear":
                        // 齿轮设置图标
                        g.DrawEllipse(pen, x + 3, y + 3, w - 6, h - 6);
                        g.DrawEllipse(pen, x + w / 2 - 2, y + h / 2 - 2, 4, 4);
                        int gcx = x + w / 2;
                        int gcy = y + h / 2;
                        g.DrawLine(pen, gcx, y + 1, gcx, y + 3);
                        g.DrawLine(pen, gcx, y + h - 3, gcx, y + h - 1);
                        g.DrawLine(pen, x + 1, gcy, x + 3, gcy);
                        g.DrawLine(pen, x + w - 3, gcy, x + w - 1, gcy);
                        break;

                    case "palette":
                        // 调色盘工艺图标 (现代艺术调色盘与彩色色斑)
                        using (var path = new GraphicsPath())
                        {
                            path.AddEllipse(x + 1, y + 1, w - 3, h - 3);
                            g.DrawPath(pen, path);
                            using (var b1 = new SolidBrush(Color.FromArgb(245, 34, 45))) g.FillEllipse(b1, x + 4, y + 5, 3, 3);
                            using (var b2 = new SolidBrush(Color.FromArgb(250, 173, 20))) g.FillEllipse(b2, x + w - 8, y + 5, 3, 3);
                            using (var b3 = new SolidBrush(Color.FromArgb(82, 196, 26))) g.FillEllipse(b3, x + 6, y + h - 8, 3, 3);
                        }
                        break;

                    case "ruler":
                        // 绘图三角板图标
                        using (var path = new GraphicsPath())
                        {
                            path.AddPolygon(new Point[]
                            {
                                new Point(x + 2, y + h - 2),
                                new Point(x + w - 2, y + h - 2),
                                new Point(x + 2, y + 2)
                            });
                            g.DrawPath(pen, path);
                            g.DrawPolygon(pen, new Point[]
                            {
                                new Point(x + 5, y + h - 5),
                                new Point(x + w - 7, y + h - 5),
                                new Point(x + 5, y + 7)
                            });
                        }
                        break;

                    case "path":
                        // 路径导出图标
                        g.DrawRectangle(pen, x + 2, y + 3, w - 4, h - 6);
                        g.DrawLine(pen, x + 5, y + 3, x + 9, y + 3);
                        g.DrawLine(pen, x + 5, y + h / 2, x + w - 5, y + h / 2);
                        break;

                    case "search":
                        // 放大镜图标
                        g.DrawEllipse(pen, x + 2, y + 2, w - 7, h - 7);
                        g.DrawLine(pen, x + w - 5, y + h - 5, x + w - 1, y + h - 1);
                        break;

                    case "pin":
                        // 现代立体倾角图钉
                        int pcx = x + w / 2;
                        int pcy = y + h / 2;
                        using (var needlePen = new Pen(color, 1.5F))
                        {
                            g.DrawLine(needlePen, pcx, pcy + 2, pcx - 4, pcy + 8);
                        }
                        Point[] head = new Point[] {
                            new Point(pcx - 3, pcy - 6),
                            new Point(pcx + 5, pcy - 6),
                            new Point(pcx + 3, pcy - 2),
                            new Point(pcx + 4, pcy + 1),
                            new Point(pcx - 1, pcy + 2),
                            new Point(pcx - 3, pcy - 1)
                        };
                        g.FillPolygon(brush, head);
                        break;

                    default:
                        g.DrawRectangle(pen, x + 2, y + 2, w - 4, h - 4);
                        break;
                }
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// 极简微型跟随锁标 (MicroFollowLatch)
    /// 1:1 对应 HTML 原型 .micro-follow-latch
    /// 浅蓝药丸 [⚡跟随] ⇄ 极简中性灰锁 [🔒保持]
    /// </summary>
    public class MicroFollowLatch : Control
    {
        private bool _isFollowing = true;
        private bool _isHovered = false;

        public event EventHandler<bool> StateChanged;

        public bool IsFollowing
        {
            get => _isFollowing;
            set
            {
                if (_isFollowing != value)
                {
                    _isFollowing = value;
                    Invalidate();
                }
            }
        }

        public MicroFollowLatch()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.SupportsTransparentBackColor, true);
            this.Size = new Size(56, 18);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            IsFollowing = !IsFollowing;
            StateChanged?.Invoke(this, IsFollowing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            Color fillClr = _isFollowing
                ? (_isHovered ? Color.FromArgb(215, 235, 255) : Color.FromArgb(230, 244, 255))
                : (_isHovered ? Color.FromArgb(230, 235, 242) : Color.FromArgb(241, 245, 249));

            Color borderClr = _isFollowing ? Color.FromArgb(145, 202, 255) : Color.FromArgb(203, 213, 225);
            Color textClr = _isFollowing ? Color.FromArgb(22, 119, 255) : Color.FromArgb(140, 149, 160);
            string text = _isFollowing ? "⚡ 跟随" : "🔒 保持";

            using (var path = CreateRoundedRectanglePath(rect, 3))
            using (var brush = new SolidBrush(fillClr))
            using (var pen = new Pen(borderClr, 1F))
            {
                e.Graphics.FillPath(brush, path);
                e.Graphics.DrawPath(pen, path);
            }

            using (var font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Bold))
            {
                TextRenderer.DrawText(e.Graphics, text, font, rect, textClr,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>
    /// 卡片专属方圆角图钉按钮 (PinCardButton)
    /// 1:1 对应 HTML 原型 .btn-pin
    /// </summary>
    public class PinCardButton : Control
    {
        private bool _isPinned = false;
        private bool _isHovered = false;

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    Invalidate();
                }
            }
        }

        public PinCardButton()
        {
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.SupportsTransparentBackColor, true);
            this.Size = new Size(26, 26);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            Color fillClr = _isPinned
                ? (_isHovered ? Color.FromArgb(215, 235, 255) : Color.FromArgb(230, 244, 255))
                : (_isHovered ? Color.FromArgb(240, 242, 245) : Color.Transparent);

            Color borderClr = _isPinned ? Color.FromArgb(145, 202, 255) : Color.Transparent;
            Color pinColor = _isPinned ? Color.FromArgb(22, 119, 255) : Color.FromArgb(140, 149, 160);

            using (var path = CreateRoundedRectanglePath(rect, 4))
            {
                if (fillClr != Color.Transparent)
                {
                    using (var brush = new SolidBrush(fillClr))
                        e.Graphics.FillPath(brush, path);
                }
                if (borderClr != Color.Transparent)
                {
                    using (var pen = new Pen(borderClr, 1F))
                        e.Graphics.DrawPath(pen, path);
                }
            }

            // 绘制原生矢量图钉 (锐利抗锯齿)
            PresetManagementForm.DrawVectorIcon(e.Graphics, "pin", rect, pinColor);
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
