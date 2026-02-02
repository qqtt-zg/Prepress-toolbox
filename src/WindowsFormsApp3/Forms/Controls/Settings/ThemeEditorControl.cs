using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Services.Events;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Forms.Main;
using WindowsFormsApp3.Forms.Panels;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    /// <summary>
    /// 主题编辑器控件
    /// </summary>
    public partial class ThemeEditorControl : UserControl
    {
        private ThemeManager _themeManager;
        private ThemeDefinition _editingTheme;
        private bool _isLoading;

        // 滚动条模式控件引用（用于禁用/启用）
        private AntdUI.Label _scrollBarLeftLabel;
        private AntdUI.Label _scrollBarRightLabel;

        public event EventHandler ThemeChanged;

        public ThemeEditorControl()
        {
            InitializeComponent();
            
            // 配置颜色面板布局
            ConfigureColorPanel();

            // 确保在控件加载时更新预览
            this.Load += (s, e) => 
            {
                if (_editingTheme != null)
                {
                    UpdatePreview();
                }
            };
            
            // 使用 lambda 绑定事件,避免委托类型匹配问题
            // ObjectNEventHandler: (object sender, object value)
            cmbThemes.SelectedValueChanged += (s, value) =>
            {
                if (!_isLoading && _themeManager != null)
                {
                    // 检查悬浮拖拽窗口是否显示
                    if (IsFloatingDropZoneVisible())
                    {
                        MessageBox.Show("悬浮拖拽窗口正在显示，请先关闭窗口后再进行主题操作，以避免UI显示异常。", 
                            "操作被阻止", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        // 恢复之前的选择
                        _isLoading = true;
                        if (_editingTheme != null)
                        {
                            cmbThemes.Text = _editingTheme.Name;
                        }
                        _isLoading = false;
                        return;
                    }

                    var themeName = cmbThemes.Text;
                    var theme = _themeManager.GetThemeByName(themeName);
                    if (theme != null)
                    {
                        _editingTheme = theme.Clone();
                        LoadThemeColors();
                        UpdatePreview();
                        UpdateEditability();
                    }
                }
            };
        }

        /// <summary>
        /// 初始化主题管理器
        /// </summary>
        public void Initialize(ThemeManager themeManager)
        {
            _themeManager = themeManager;
            LoadThemes();
            LoadCurrentTheme();
        }

        /// <summary>
        /// 加载所有可用主题到下拉列表
        /// </summary>
        private void LoadThemes()
        {
            if (_themeManager == null) return;

            _isLoading = true;
            cmbThemes.Items.Clear();

            var themes = _themeManager.GetAllThemes();
            foreach (var theme in themes)
            {
                cmbThemes.Items.Add(theme.Name);
            }

            _isLoading = false;
        }

        /// <summary>
        /// 加载当前主题
        /// </summary>
        private void LoadCurrentTheme()
        {
            if (_themeManager == null) return;

            var currentTheme = _themeManager.GetCurrentTheme();
            if (currentTheme != null)
            {
                _isLoading = true;
                cmbThemes.Text = currentTheme.Name;
                _editingTheme = currentTheme.Clone();
                LoadThemeColors();
                UpdatePreview();
                UpdateEditability(); // 启动时也检查禁用状态
                _isLoading = false;
            }
        }

        /// <summary>
        /// 检查悬浮拖拽窗口是否正在显示
        /// </summary>
        private bool IsFloatingDropZoneVisible()
        {
            try
            {
                // 通过主窗口获取 FileRenamePanel
                var mainForm = this.FindForm() as MainShellForm;
                if (mainForm == null) return false;

                // 通过 MainShellForm 的 panelCache 查找 FileRenamePanel
                // 使用反射访问私有字段 panelCache
                var panelCacheField = typeof(MainShellForm).GetField("panelCache", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (panelCacheField == null) return false;

                var panelCache = panelCacheField.GetValue(mainForm) as System.Collections.Generic.Dictionary<string, UserControl>;
                if (panelCache == null) return false;

                // 直接通过 "rename" 键获取 FileRenamePanel
                if (panelCache.TryGetValue("rename", out var control) && control is FileRenamePanel renamePanel)
                {
                    return renamePanel.IsFloatingDropZoneVisible();
                }

                return false;
            }
            catch
            {
                // 出错时允许操作，避免阻塞用户
                return false;
            }
        }

        /// <summary>
        /// 加载主题颜色到颜色选择器
        /// </summary>
        private void LoadThemeColors()
        {
            if (_editingTheme == null) return;

            _isLoading = true;

            // 背景色组
            cpBackground.Value = _editingTheme.Background;
            cpSurface.Value = _editingTheme.Surface;
            cpSurface.Value = _editingTheme.Surface;
            cpSurfaceLight.Value = _editingTheme.SurfaceLight;
            cpInputBackground.Value = _editingTheme.InputBackground;

            // 文字色组
            cpTextPrimary.Value = _editingTheme.TextPrimary;
            cpTextSecondary.Value = _editingTheme.TextSecondary;

            // 边框色
            cpBorder.Value = _editingTheme.Border;

            // 强调色组
            cpPrimary.Value = _editingTheme.Primary;
            cpSuccess.Value = _editingTheme.Success;
            cpWarning.Value = _editingTheme.Warning;
            cpWarning.Value = _editingTheme.Warning;
            cpError.Value = _editingTheme.Error;

            // 强调色组 (新)
            cpAccentColor4.Value = _editingTheme.AccentColor4;

            // 交互色组
            cpBackActive.Value = _editingTheme.BackActive;
            cpBackHover.Value = _editingTheme.BackHover;

            // 滚动条色组
            swScrollBarMode.Checked = _editingTheme.UseScrollBarDarkMode;

            // 悬浮拖拽窗口组
            numFloatingDropZoneWidth.Value = Math.Max(numFloatingDropZoneWidth.Minimum, Math.Min(numFloatingDropZoneWidth.Maximum, _editingTheme.FloatingDropZoneDefaultWidth));
            numFloatingDropZoneHeight.Value = Math.Max(numFloatingDropZoneHeight.Minimum, Math.Min(numFloatingDropZoneHeight.Maximum, _editingTheme.FloatingDropZoneDefaultHeight));
            cpFloatingDropZoneBackColor.Value = _editingTheme.FloatingDropZoneBackColor;
            cpFloatingDropZoneBackColorDrag.Value = _editingTheme.FloatingDropZoneBackColorDrag;
            sliderFloatingDropZoneOpacity.Value = (int)(_editingTheme.FloatingDropZoneOpacity * 100);
            if (lblFloatingDropZoneOpacityValue != null)
            {
                lblFloatingDropZoneOpacityValue.Text = $"{(int)(_editingTheme.FloatingDropZoneOpacity * 100)}%";
            }
            swFloatingDropZonePopcatEnabled.Checked = _editingTheme.FloatingDropZonePopcatEnabled;
            swFloatingDropZonePopcatEnabled.CheckedChanged += (s, e) =>
            {
                if (!_isLoading)
                {
                    UpdateIconToggleControlsEnabled();
                    _editingTheme.FloatingDropZonePopcatEnabled = swFloatingDropZonePopcatEnabled.Checked;
                    UpdateIconThumbnailsVisibility();
                    UpdatePreview();
                }
            };
            
            UpdateIconToggleControlsEnabled();

            _isLoading = false;
        }

        /// <summary>
        /// 更新预览面板 - 构建模拟应用程序界面
        /// </summary>
        private void UpdatePreview()
        {
            if (_editingTheme == null) return;

            // 清除现有控件并重置布局属性
            pnlPreview.Controls.Clear();
            pnlPreview.AutoScroll = false;
            pnlPreview.Padding = new Padding(0);  // 移除内边距，确保内容从边缘开始

            // 设置主容器背景
            pnlPreview.BackColor = _editingTheme.Background;

                // ===== 1. 顶部工具栏 (模拟 FileRenamePanel 的顶部控制面板) =====
                var toolbar = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 40,
                    BackColor = _editingTheme.Surface,
                    Padding = new Padding(5, 5, 5, 5)
                };

                // 输入框模拟
                var inputBox = new Panel
                {
                    Location = new Point(5, 8),
                    Size = new Size(120, 24),
                    BackColor = _editingTheme.SurfaceLight
                };
                inputBox.Paint += (s, e) =>
                {
                    using (var pen = new Pen(_editingTheme.Border))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, inputBox.Width - 1, inputBox.Height - 1);
                    }
                };
                var inputText = new Label
                {
                    Text = "选择文件夹...",
                    ForeColor = _editingTheme.TextSecondary,
                    Font = new Font("Microsoft YaHei UI", 7F),
                    Location = new Point(3, 5),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                inputBox.Controls.Add(inputText);
                toolbar.Controls.Add(inputBox);

                // 按钮模拟 - 展示所有四种强调色
                var btn1 = CreateMiniButton("选择", _editingTheme.Primary, new Point(130, 8));
                var btn2 = CreateMiniButton("导入", _editingTheme.Success, new Point(175, 8));
                var btn3 = CreateMiniButton("监控", _editingTheme.Warning, new Point(220, 8));
                var btn4 = CreateMiniButton("删除", _editingTheme.Error, new Point(265, 8));
                toolbar.Controls.Add(btn1);
                toolbar.Controls.Add(btn2);
                toolbar.Controls.Add(btn3);
                toolbar.Controls.Add(btn4);

                // ===== 2. 中间表格区域 (模拟 DataGridView) =====
                // 使用 TableLayoutPanel 精确控制表头和数据区域的布局
                var mainTablePanel = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    BackColor = _editingTheme.Surface,
                    Padding = new Padding(0),
                    Margin = new Padding(0),
                    ColumnCount = 1,
                    RowCount = 2,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.None
                };
                mainTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
                mainTablePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));  // 表头固定 28px
                mainTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // 数据区域填充剩余空间
                // 列定义 - 动态计算列宽以填充整个可用宽度
                int availableWidth = pnlPreview.ClientSize.Width - 2; // 减去边框
                var headers = new[] 
                { 
                    ("序号", (int)(availableWidth * 0.10)),      // 10%
                    ("原文件名", (int)(availableWidth * 0.25)),  // 25%
                    ("新文件名", (int)(availableWidth * 0.25)),  // 25%
                    ("订单号", (int)(availableWidth * 0.20)),    // 20%
                    ("材料", (int)(availableWidth * 0.20))       // 20%
                };
                
                // ===== 表头 (独立面板，使用 Dock.Top) =====
                var tableHeader = new Panel
                {
                    Height = 28,
                    Dock = DockStyle.Top,
                    BackColor = _editingTheme.SurfaceLight,
                };
                // 绘制表头边框线
                tableHeader.Paint += (s, e) =>
                {
                    using (var pen = new Pen(_editingTheme.Border))
                    {
                        e.Graphics.DrawLine(pen, 0, tableHeader.Height - 1, tableHeader.Width, tableHeader.Height - 1);
                        int x = 0;
                        foreach (var (_, width) in headers)
                        {
                            x += width;
                            e.Graphics.DrawLine(pen, x, 0, x, tableHeader.Height);
                        }
                    }
                };
                // 表头文字
                int headerX = 0;
                foreach (var (text, width) in headers)
                {
                    var headerLabel = new Label
                    {
                        Text = text,
                        Location = new Point(headerX + 2, 5),
                        Size = new Size(width - 4, 16),
                        ForeColor = _editingTheme.TextPrimary,
                        Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold),
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.Transparent
                    };
                    tableHeader.Controls.Add(headerLabel);
                    headerX += width;
                }

                // ===== 数据行区域 (可滚动) =====
                // 数据滚动区域 (Dock.Fill 到 mainTablePanel)
                var scrollableDataPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = _editingTheme.Surface,
                    AutoScroll = true
                };
                // 表格行数据
                var rowData = new[]
                {
                    new[] { "1", "文件001.pdf", "订单A_..", "A001", "铜版纸" },
                    new[] { "2", "文件002.pdf", "订单B_..", "B002", "哑粉纸" },
                    new[] { "3", "文件003.pdf", "订单C_..", "C003", "白卡纸" },
                    new[] { "4", "文件004.pdf", "订单D_..", "D004", "铜版纸" },
                    new[] { "5", "文件005.pdf", "订单E_..", "E005", "哑粉纸" },
                    new[] { "6", "文件006.pdf", "订单F_..", "F006", "白卡纸" },
                    new[] { "7", "文件007.pdf", "订单G_..", "G007", "铜版纸" },
                    new[] { "8", "文件008.pdf", "订单H_..", "H008", "哑粉纸" },
                    new[] { "9", "文件009.pdf", "订单I_..", "I009", "白卡纸" },
                    new[] { "10", "文件010.pdf", "订单J_..", "J010", "铜版纸" },
                    new[] { "11", "文件011.pdf", "订单K_..", "K011", "哑粉纸" },
                    new[] { "12", "文件012.pdf", "订单L_..", "L012", "白卡纸" },
                    new[] { "13", "文件013.pdf", "订单M_..", "M013", "铜版纸" },
                    new[] { "14", "文件014.pdf", "订单N_..", "N014", "哑粉纸" },
                    new[] { "15", "文件015.pdf", "订单O_..", "O015", "白卡纸" },
                    new[] { "16", "文件016.pdf", "订单P_..", "P016", "铜版纸" },
                    new[] { "17", "文件017.pdf", "订单Q_..", "Q017", "哑粉纸" },
                    new[] { "18", "文件018.pdf", "订单R_..", "R018", "白卡纸" },
                    new[] { "19", "文件019.pdf", "订单S_..", "S019", "铜版纸" },
                    new[] { "20", "文件020.pdf", "订单T_..", "T020", "哑粉纸" }
                };

                // 数据行 (从后往前添加,因为 Dock.Top 后添加的在上面)
                for (int i = rowData.Length - 1; i >= 0; i--)
                {
                    var row = new Panel
                    {
                        Height = 20,
                        Dock = DockStyle.Top,
                        BackColor = _editingTheme.Surface,
                    };
                    // 捕获当前索引用于 Paint 事件
                    int rowIndex = i;
                    // 绘制行网格线
                    row.Paint += (s, e) =>
                    {
                        using (var pen = new Pen(_editingTheme.Border))
                        {
                            // 底部边框线
                            e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
                            // 纵向列分隔线
                            int lineX = 0;
                            foreach (var (_, width) in headers)
                            {
                                lineX += width;
                                e.Graphics.DrawLine(pen, lineX, 0, lineX, row.Height);
                            }
                        }
                    };

                    // 单元格内容
                    int cellX = 0;
                    for (int j = 0; j < rowData[i].Length; j++)
                    {
                        var cellLabel = new Label
                        {
                            Text = rowData[i][j],
                            Location = new Point(cellX + 2, 2),
                            Size = new Size(headers[j].Item2 - 4, 16),
                            ForeColor = _editingTheme.TextPrimary,
                            Font = new Font("Microsoft YaHei UI", 7F),
                            TextAlign = j == 0 ? ContentAlignment.MiddleCenter : ContentAlignment.MiddleLeft,
                            BackColor = Color.Transparent
                        };
                        row.Controls.Add(cellLabel);
                        cellX += headers[j].Item2;
                    }
                    scrollableDataPanel.Controls.Add(row);
                }

                // ===== 3. 底部状态栏 =====
                var statusBar = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 20,
                    BackColor = _editingTheme.Surface,
                    Padding = new Padding(5, 2, 5, 2)
                };

                var statusText = new Label
                {
                    Text = "状态: 未监控 │ 模式: 复制 │ 组合: 订单号_材料...",
                    ForeColor = _editingTheme.TextSecondary,
                    Font = new Font("Microsoft YaHei UI", 7F),
                    Location = new Point(5, 2),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                statusBar.Controls.Add(statusText);


                // 组装内部表格：使用 TableLayoutPanel 单元格位置
                tableHeader.Dock = DockStyle.Fill;         // 填满单元格
                scrollableDataPanel.Dock = DockStyle.Fill; // 填满单元格
                mainTablePanel.Controls.Add(tableHeader, 0, 0);         // 第0行：表头
                mainTablePanel.Controls.Add(scrollableDataPanel, 0, 1); // 第1行：数据区域

                // ===== 组装预览界面 =====
                // WinForms Dock 规则: 后添加的控件先处理 Dock
                // 所以添加顺序应该是: Fill -> Bottom -> Top
                pnlPreview.Controls.Add(mainTablePanel); // Fill (最后处理，填充剩余空间)
                pnlPreview.Controls.Add(statusBar);      // Bottom (倒数第二处理)
                pnlPreview.Controls.Add(toolbar);        // Top (最先处理，占据顶部40px)
        }
        
        /// <summary>
        /// 创建迷你按钮用于预览
        /// </summary>
        private Label CreateMiniButton(string text, Color backColor, Point location)
        {
            return new Label
            {
                Text = text,
                BackColor = backColor,
                ForeColor = Color.White,
                Location = location,
                Size = new Size(40, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 7F),
                Cursor = Cursors.Hand
            };
        }

        /// <summary>
        /// 创建模拟按钮 (使用 Label)
        /// </summary>
        private Label CreateSimulatedButton(string text, Color backColor, Color foreColor, Point location)
        {
            return new Label
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                Location = location,
                Size = new Size(80, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 9),
                Cursor = Cursors.Hand
            };
        }

        /// <summary>
        /// 更新控件可编辑状态（现在内置主题也可以编辑）
        /// </summary>
        private void UpdateEditability()
        {
            bool isBuiltIn = _editingTheme?.IsBuiltIn ?? true;

            // 现在所有主题都可以编辑
            EnableColorPickers(true);
            
            // 设置按钮启用状态
            btnSave.Enabled = true; // 内置主题也可以保存
            btnDelete.Enabled = !isBuiltIn; // 内置主题不能删除
            btnReset.Enabled = isBuiltIn; // 重置按钮仅在内置主题时启用
            
            // 设置按钮样式
            var currentAppTheme = _themeManager?.GetCurrentTheme();
            if (currentAppTheme != null)
            {
                // 保存按钮始终启用
                btnSave.BackColor = currentAppTheme.Success;
                btnSave.ForeColor = Color.White;
                
                // 删除按钮：仅非内置主题时启用
                if (btnDelete.Enabled)
                {
                    btnDelete.BackColor = currentAppTheme.Error;
                    btnDelete.ForeColor = Color.White;
                }
                else
                {
                    btnDelete.BackColor = Color.FromArgb(100, 100, 100);
                    btnDelete.ForeColor = Color.FromArgb(240, 240, 240);
                }
                
                // 重置按钮：仅内置主题时启用
                if (btnReset.Enabled)
                {
                    btnReset.BackColor = currentAppTheme.Warning;
                    btnReset.ForeColor = Color.White;
                }
                else
                {
                    btnReset.BackColor = Color.FromArgb(100, 100, 100);
                    btnReset.ForeColor = Color.FromArgb(240, 240, 240);
                }
                
                // Apply 按钮颜色
                btnApply.BackColor = currentAppTheme.Primary;
                btnApply.ForeColor = Color.White;
            }
            else
            {
                // Fallback mechanism if theme manager is somehow null
                btnSave.BackColor = Color.Empty;
                btnDelete.BackColor = Color.Empty;
                btnReset.BackColor = Color.Empty;
            }
        }

        /// <summary>
        /// 启用/禁用所有颜色选择器
        /// </summary>
        private void EnableColorPickers(bool enabled)
        {
            cpBackground.Enabled = enabled;
            cpSurface.Enabled = enabled;
            cpSurface.Enabled = enabled;
            cpSurfaceLight.Enabled = enabled;
            cpInputBackground.Enabled = enabled;
            cpTextPrimary.Enabled = enabled;
            cpTextSecondary.Enabled = enabled;
            cpBorder.Enabled = enabled;
            cpPrimary.Enabled = enabled;
            cpSuccess.Enabled = enabled;
            cpWarning.Enabled = enabled;
            cpWarning.Enabled = enabled;
            cpError.Enabled = enabled;
            cpAccentColor4.Enabled = enabled;
            cpBackActive.Enabled = enabled;
            cpBackHover.Enabled = enabled;
            swScrollBarMode.Enabled = enabled;
            numFloatingDropZoneWidth.Enabled = enabled;
            numFloatingDropZoneHeight.Enabled = enabled;
            cpFloatingDropZoneBackColor.Enabled = enabled;
            cpFloatingDropZoneBackColorDrag.Enabled = enabled;
            lblFloatingDropZoneWidth.Enabled = enabled;
            lblFloatingDropZoneHeight.Enabled = enabled;
            lblFloatingDropZoneBackColor.Enabled = enabled;
            lblFloatingDropZoneBackColorDrag.Enabled = enabled;
            sliderFloatingDropZoneOpacity.Enabled = enabled;
            lblFloatingDropZoneOpacity.Enabled = enabled;
            if (lblFloatingDropZoneOpacityValue != null)
                lblFloatingDropZoneOpacityValue.Enabled = enabled;
            swFloatingDropZonePopcatEnabled.Enabled = enabled;
            lblFloatingDropZonePopcatEnabled.Enabled = enabled;
            UpdateIconToggleControlsEnabled();
            
            
            // 同时禁用/启用滚动条模式的左右标签
            if (_scrollBarLeftLabel != null)
                _scrollBarLeftLabel.Enabled = enabled;
            if (_scrollBarRightLabel != null)
                _scrollBarRightLabel.Enabled = enabled;
        }

        /// <summary>
        /// 数值输入框值改变事件
        /// </summary>
        private void NumericValueChanged()
        {
            if (_isLoading || _editingTheme == null) return;

            _editingTheme.FloatingDropZoneDefaultWidth = (int)numFloatingDropZoneWidth.Value;
            _editingTheme.FloatingDropZoneDefaultHeight = (int)numFloatingDropZoneHeight.Value;

            // 更新预览
            UpdatePreview();
        }

        /// <summary>
        /// 滑块值改变事件
        /// </summary>
        private void SliderValueChanged()
        {
            if (_isLoading || _editingTheme == null) return;

            _editingTheme.FloatingDropZoneOpacity = sliderFloatingDropZoneOpacity.Value / 100.0;

            // 更新预览
            UpdatePreview();
        }


        /// <summary>
        /// 选择图片文件
        /// </summary>
        private void SelectImageFile(TextBox textBox)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*";
                dialog.Title = "选择背景图片";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    textBox.Text = dialog.FileName;
                }
            }
        }

        /// <summary>
        /// 获取 ImageLayout 的索引
        /// </summary>
        private int GetImageLayoutIndex(ImageLayout layout)
        {
            switch (layout)
            {
                case ImageLayout.None: return 0;
                case ImageLayout.Tile: return 1;
                case ImageLayout.Center: return 2;
                case ImageLayout.Stretch: return 3;
                case ImageLayout.Zoom: return 4;
                default: return 3; // 默认 Stretch
            }
        }

        /// <summary>
        /// 从索引获取 ImageLayout
        /// </summary>
        private ImageLayout GetImageLayoutFromIndex(int index)
        {
            switch (index)
            {
                case 0: return ImageLayout.None;
                case 1: return ImageLayout.Tile;
                case 2: return ImageLayout.Center;
                case 3: return ImageLayout.Stretch;
                case 4: return ImageLayout.Zoom;
                default: return ImageLayout.Stretch;
            }
        }

        /// <summary>
        /// 更新图标切换功能相关控件的启用状态
        /// 当图标切换功能开启时，禁用背景色、拖拽背景色配置，但保持透明度设置可用
        /// </summary>
        private void UpdateIconToggleControlsEnabled()
        {
            bool iconToggleEnabled = swFloatingDropZonePopcatEnabled.Checked;
            
            // 当图标切换开启时，禁用背景色配置（因为使用透明背景）
            bool backgroundControlsEnabled = !iconToggleEnabled;
            
            cpFloatingDropZoneBackColor.Enabled = backgroundControlsEnabled;
            lblFloatingDropZoneBackColor.Enabled = backgroundControlsEnabled;
            cpFloatingDropZoneBackColorDrag.Enabled = backgroundControlsEnabled;
            lblFloatingDropZoneBackColorDrag.Enabled = backgroundControlsEnabled;
            
            
            // 透明度设置在图标切换启用时仍然可用（因为现在支持透明度设置）
            // 透明度设置始终启用，不受图标切换功能影响
            // sliderFloatingDropZoneOpacity.Enabled 由 EnableColorPickers() 控制，这里不需要修改
        }

        /// <summary>
        /// 颜色选择器值改变事件
        /// </summary>
        private void ColorPicker_ValueChanged(object sender, AntdUI.ColorEventArgs color)
        {
            if (_isLoading || _editingTheme == null) return;

            // 更新编辑中的主题颜色
            _editingTheme.Background = cpBackground.Value;
            _editingTheme.Surface = cpSurface.Value;
            _editingTheme.Surface = cpSurface.Value;
            _editingTheme.SurfaceLight = cpSurfaceLight.Value;
            _editingTheme.InputBackground = cpInputBackground.Value;
            _editingTheme.TextPrimary = cpTextPrimary.Value;
            _editingTheme.TextSecondary = cpTextSecondary.Value;
            _editingTheme.Border = cpBorder.Value;
            _editingTheme.Primary = cpPrimary.Value;
            _editingTheme.Success = cpSuccess.Value;
            _editingTheme.Warning = cpWarning.Value;
            _editingTheme.Warning = cpWarning.Value;
            _editingTheme.Error = cpError.Value;
            _editingTheme.AccentColor4 = cpAccentColor4.Value;
            _editingTheme.BackActive = cpBackActive.Value;
            _editingTheme.BackHover = cpBackHover.Value;
            _editingTheme.UseScrollBarDarkMode = swScrollBarMode.Checked;
            _editingTheme.FloatingDropZoneDefaultWidth = (int)numFloatingDropZoneWidth.Value;
            _editingTheme.FloatingDropZoneDefaultHeight = (int)numFloatingDropZoneHeight.Value;
            _editingTheme.FloatingDropZoneBackColor = cpFloatingDropZoneBackColor.Value;
            _editingTheme.FloatingDropZoneBackColorDrag = cpFloatingDropZoneBackColorDrag.Value;
            _editingTheme.FloatingDropZoneOpacity = sliderFloatingDropZoneOpacity.Value / 100.0;
            _editingTheme.FloatingDropZonePopcatEnabled = swFloatingDropZonePopcatEnabled.Checked;

            // 更新预览
            UpdatePreview();
        }

        /// <summary>
        /// 新建自定义主题
        /// </summary>
        private void btnNewTheme_Click(object sender, EventArgs e)
        {
            using (var inputForm = CreateInputDialog("新建主题", "请输入主题名称:"))
            {
                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    var themeName = inputForm.Tag as string;
                    if (!string.IsNullOrWhiteSpace(themeName))
                    {
                        // 基于当前主题创建新主题
                        var newTheme = _editingTheme?.Clone() ?? new ThemeDefinition();
                        newTheme.Name = themeName;
                        newTheme.IsBuiltIn = false;

                        _editingTheme = newTheme;
                        LoadThemeColors();
                        UpdateEditability();
                        EnableColorPickers(true);
                    }
                }
            }
        }

        /// <summary>
        /// 保存当前主题
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (_editingTheme == null || _themeManager == null) return;

            // 检查悬浮拖拽窗口是否显示
            if (IsFloatingDropZoneVisible())
            {
                MessageBox.Show("悬浮拖拽窗口正在显示，请先关闭窗口后再进行主题操作，以避免UI显示异常。", 
                    "操作被阻止", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_themeManager.SaveCustomTheme(_editingTheme))
            {
                // 如果保存的主题是当前应用的主题，确保 _currentTheme 引用已更新
                var currentTheme = _themeManager.GetCurrentTheme();
                if (currentTheme != null && currentTheme.Name == _editingTheme.Name)
                {
                    // 重新设置当前主题，确保引用更新到最新保存的版本
                    _themeManager.SetCurrentTheme(_editingTheme.Name);
                    
                    // 发布配置保存事件以更新窗口
                    try
                    {
                        var eventBus = Services.ServiceLocator.Instance.GetEventBus();
                        eventBus?.Publish(new ConfigSavedEvent
                        {
                            ConfigKey = "Theme",
                            SavedItemsCount = 0,
                            ConfigFilePath = ""
                        });
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Warn($"发布配置保存事件失败: {ex.Message}");
                    }
                }

                MessageBox.Show($"主题 \"{_editingTheme.Name}\" 已保存成功！", "保存成功",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadThemes();
                cmbThemes.Text = _editingTheme.Name;
            }
            else
            {
                MessageBox.Show("保存主题失败！", "保存失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 应用当前主题
        /// </summary>
        private void btnApply_Click(object sender, EventArgs e)
        {
            if (_editingTheme == null || _themeManager == null) return;

            // 检查悬浮拖拽窗口是否显示
            if (IsFloatingDropZoneVisible())
            {
                MessageBox.Show("悬浮拖拽窗口正在显示，请先关闭窗口后再进行主题操作，以避免UI显示异常。", 
                    "操作被阻止", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _themeManager.SetCurrentTheme(_editingTheme.Name);
            AppSettings.CurrentThemeName = _editingTheme.Name;
            AppSettings.Save();

            // 发布配置保存事件，通知悬浮拖拽窗口更新
            try
            {
                var eventBus = Services.ServiceLocator.Instance.GetEventBus();
                eventBus?.Publish(new ConfigSavedEvent
                {
                    ConfigKey = "Theme",
                    SavedItemsCount = 0,
                    ConfigFilePath = ""
                });
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"发布配置保存事件失败: {ex.Message}");
            }

            ThemeChanged?.Invoke(this, EventArgs.Empty);

            MessageBox.Show($"主题 \"{_editingTheme.Name}\" 已应用！", "应用成功",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 删除自定义主题
        /// </summary>
        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_editingTheme == null || _themeManager == null) return;

            if (_editingTheme.IsBuiltIn)
            {
                MessageBox.Show("不能删除内置主题！", "操作失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要删除主题 \"{_editingTheme.Name}\" 吗？",
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                if (_themeManager.DeleteCustomTheme(_editingTheme.Name))
                {
                    MessageBox.Show("主题已删除！", "删除成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    LoadThemes();
                    LoadCurrentTheme();
                }
                else
                {
                    MessageBox.Show("删除主题失败！", "删除失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 重置内置主题到默认设置
        /// </summary>
        private void btnReset_Click(object sender, EventArgs e)
        {
            if (_editingTheme == null || _themeManager == null) return;

            if (!_editingTheme.IsBuiltIn)
            {
                MessageBox.Show("只能重置内置主题！", "操作失败",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"确定要将主题 \"{_editingTheme.Name}\" 重置为默认设置吗？",
                "确认重置", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 从 ThemeManager 获取原始内置主题定义
                var originalTheme = _themeManager.GetOriginalBuiltInTheme(_editingTheme.Name);
                
                if (originalTheme != null)
                {
                    // 复制所有属性（除了 Name 和 IsBuiltIn）
                    _editingTheme.Background = originalTheme.Background;
                    _editingTheme.Surface = originalTheme.Surface;
                    _editingTheme.SurfaceLight = originalTheme.SurfaceLight;
                    _editingTheme.InputBackground = originalTheme.InputBackground;
                    _editingTheme.TextPrimary = originalTheme.TextPrimary;
                    _editingTheme.TextSecondary = originalTheme.TextSecondary;
                    _editingTheme.Border = originalTheme.Border;
                    _editingTheme.Primary = originalTheme.Primary;
                    _editingTheme.Success = originalTheme.Success;
                    _editingTheme.Warning = originalTheme.Warning;
                    _editingTheme.Error = originalTheme.Error;
                    _editingTheme.AccentColor1 = originalTheme.AccentColor1;
                    _editingTheme.AccentColor2 = originalTheme.AccentColor2;
                    _editingTheme.AccentColor3 = originalTheme.AccentColor3;
                    _editingTheme.AccentColor4 = originalTheme.AccentColor4;
                    _editingTheme.BackActive = originalTheme.BackActive;
                    _editingTheme.BackHover = originalTheme.BackHover;
                    _editingTheme.UseScrollBarDarkMode = originalTheme.UseScrollBarDarkMode;
                    _editingTheme.FloatingDropZoneDefaultWidth = originalTheme.FloatingDropZoneDefaultWidth;
                    _editingTheme.FloatingDropZoneDefaultHeight = originalTheme.FloatingDropZoneDefaultHeight;
                    _editingTheme.FloatingDropZoneBackColor = originalTheme.FloatingDropZoneBackColor;
                    _editingTheme.FloatingDropZoneBackColorDrag = originalTheme.FloatingDropZoneBackColorDrag;
                    _editingTheme.FloatingDropZoneOpacity = originalTheme.FloatingDropZoneOpacity;
                    _editingTheme.FloatingDropZonePopcatEnabled = originalTheme.FloatingDropZonePopcatEnabled;

                    // 重新加载颜色到UI
                    LoadThemeColors();
                    UpdatePreview();

                    MessageBox.Show($"主题 \"{_editingTheme.Name}\" 已重置为默认设置！", "重置成功",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("无法找到原始主题定义！", "重置失败",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 导出主题
        /// </summary>
        private void btnExport_Click(object sender, EventArgs e)
        {
            if (_editingTheme == null || _themeManager == null) return;

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "JSON 文件|*.json";
                dialog.FileName = _editingTheme.Name + ".json";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (_themeManager.ExportTheme(_editingTheme, dialog.FileName))
                    {
                        MessageBox.Show("主题已导出成功！", "导出成功",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("导出主题失败！", "导出失败",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 导入主题
        /// </summary>
        private void btnImport_Click(object sender, EventArgs e)
        {
            if (_themeManager == null) return;

            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "JSON 文件|*.json";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var theme = _themeManager.ImportTheme(dialog.FileName);
                    if (theme != null)
                    {
                        _editingTheme = theme;
                        LoadThemeColors();
                        UpdatePreview();
                        UpdateEditability();

                        MessageBox.Show("主题已导入成功！请保存以添加到主题列表。", "导入成功",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("导入主题失败！", "导入失败",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// 创建输入对话框
        /// </summary>
        private Form CreateInputDialog(string title, string prompt)
        {
            var form = new Form
            {
                Text = title,
                Size = new Size(400, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var label = new Label
            {
                Text = prompt,
                Location = new Point(20, 20),
                Size = new Size(350, 20)
            };

            var textBox = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(350, 25)
            };

            var btnOk = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(200, 85),
                Size = new Size(80, 30)
            };

            var btnCancel = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(290, 85),
                Size = new Size(80, 30)
            };

            form.Controls.AddRange(new Control[] { label, textBox, btnOk, btnCancel });
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            form.FormClosing += (s, e) =>
            {
                if (form.DialogResult == DialogResult.OK)
                {
                    form.Tag = textBox.Text;
                }
            };

            return form;
        }
        
        #region Designer Helper Methods
        
        private void ConfigureColorPanel()
        {
            int y = 10;
            int labelWidth = 120;
            int pickerWidth = 200;
            int rowHeight = 50;

            // 背景色组
            AddGroupLabel(lblBackgroundGroup, "背景色组", "BgColorsOutlined", ref y);
            AddColorRow(lblBackground, cpBackground, "主背景:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblSurface, cpSurface, "卡片(容器):", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblSurfaceLight, cpSurfaceLight, "面板(浅):", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblInputBackground, cpInputBackground, "输入框:", ref y, labelWidth, pickerWidth, rowHeight);

            y += 10;

            // 文字色组
            AddGroupLabel(lblTextGroup, "文字色组", "FontColorsOutlined", ref y);
            AddColorRow(lblTextPrimary, cpTextPrimary, "主文字:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblTextSecondary, cpTextSecondary, "次要文字:", ref y, labelWidth, pickerWidth, rowHeight);

            y += 10;

            // 边框色
            AddGroupLabel(lblBorderGroup, "边框色", "BorderOutlined", ref y);
            AddColorRow(lblBorder, cpBorder, "边框:", ref y, labelWidth, pickerWidth, rowHeight);

            y += 10;

            // 强调色组
            AddGroupLabel(lblAccentGroup, "强调色组", "BgColorsOutlined", ref y);
            AddColorRow(lblPrimary, cpPrimary, "Primary:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblSuccess, cpSuccess, "Success:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblWarning, cpWarning, "Warning:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblWarning, cpWarning, "Warning:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblError, cpError, "Error:", ref y, labelWidth, pickerWidth, rowHeight);
            
            // 新增强调色1-4
            AddColorRow(lblAccentColor4, cpAccentColor4, "Accent 4 (旋转):", ref y, labelWidth, pickerWidth, rowHeight);


            y += 10;

            // 交互色组
            AddGroupLabel(lblInteractionGroup, "交互色组", "ThunderboltOutlined", ref y);
            AddColorRow(lblBackActive, cpBackActive, "激活状态:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblBackHover, cpBackHover, "悬停状态:", ref y, labelWidth, pickerWidth, rowHeight);

            y += 10;

            // 滚动条色组
            AddGroupLabel(lblScrollBarGroup, "滚动条模式", "BarsOutlined", ref y);
            AddScrollBarModeRow(lblScrollBarMode, swScrollBarMode, "模式:", ref y, labelWidth, rowHeight);

            y += 10;

            // 悬浮拖拽窗口组
            AddGroupLabel(lblFloatingDropZoneGroup, "悬浮拖拽窗口", "AppstoreOutlined", ref y);
            AddNumericRow(lblFloatingDropZoneWidth, numFloatingDropZoneWidth, "默认宽度:", ref y, labelWidth, pickerWidth, rowHeight, 1, 600);
            AddNumericRow(lblFloatingDropZoneHeight, numFloatingDropZoneHeight, "默认高度:", ref y, labelWidth, pickerWidth, rowHeight, 1, 300);
            AddColorRow(lblFloatingDropZoneBackColor, cpFloatingDropZoneBackColor, "默认背景色:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblFloatingDropZoneBackColorDrag, cpFloatingDropZoneBackColorDrag, "拖拽时背景色:", ref y, labelWidth, pickerWidth, rowHeight);
            AddSliderRow(lblFloatingDropZoneOpacity, sliderFloatingDropZoneOpacity, lblFloatingDropZoneOpacityValue, "透明度:", ref y, labelWidth, pickerWidth, rowHeight, 0, 100);
            AddSimpleSwitchRow(lblFloatingDropZonePopcatEnabled, swFloatingDropZonePopcatEnabled, "启用图标切换:", ref y, labelWidth, rowHeight);
            AddIconThumbnailsRow(ref y, labelWidth, rowHeight);
        }

        private void AddGroupLabel(AntdUI.Label label, string text, string iconSvg, ref int y)
        {
            // 创建一个小的图标按钮
            var iconBtn = new AntdUI.Button
            {
                IconSvg = iconSvg,
                Location = new System.Drawing.Point(10, y + 2),
                Size = new System.Drawing.Size(26, 26),
                Ghost = true, // 透明背景
                Type = AntdUI.TTypeMini.Primary, // 使用主色，更醒目
                IconRatio = 0.8f,
                TabStop = false,
                Cursor = System.Windows.Forms.Cursors.Default // 保持默认光标，表明不可点击
            };
            this.pnlColorConfig.Controls.Add(iconBtn);
            
            // 设置标签
            label.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            label.Location = new System.Drawing.Point(40, y); // 留出图标的空间
            label.Size = new System.Drawing.Size(360, 30);
            label.Text = text;
            this.pnlColorConfig.Controls.Add(label);
            y += 35;
        }

        private void AddColorRow(AntdUI.Label label, AntdUI.ColorPicker picker, string text, ref int y, int labelWidth, int pickerWidth, int rowHeight)
        {
            label.Location = new System.Drawing.Point(20, y + 5);
            label.Size = new System.Drawing.Size(labelWidth, 30);
            label.Text = text;
            this.pnlColorConfig.Controls.Add(label);

            picker.Location = new System.Drawing.Point(labelWidth + 30, y);
            picker.Size = new System.Drawing.Size(pickerWidth, 40);
            picker.ValueChanged += ColorPicker_ValueChanged;
            this.pnlColorConfig.Controls.Add(picker);

            y += rowHeight;
        }

        private void AddScrollBarModeRow(AntdUI.Label label, AntdUI.Switch switchControl, string text, ref int y, int labelWidth, int rowHeight)
        {
            label.Location = new System.Drawing.Point(20, y);
            label.Size = new System.Drawing.Size(labelWidth, 40);
            label.Text = text;
            this.pnlColorConfig.Controls.Add(label);

            // 左侧标签："浅色"
            _scrollBarLeftLabel = new AntdUI.Label
            {
                Location = new System.Drawing.Point(labelWidth + 30, y),
                Size = new System.Drawing.Size(50, 40),
                Text = "浅色",
                TextAlign = System.Drawing.ContentAlignment.MiddleRight
            };
            this.pnlColorConfig.Controls.Add(_scrollBarLeftLabel);

            // 配置 Switch 控件（深色/浅色开关）
            switchControl.Location = new System.Drawing.Point(labelWidth + 85, y + 8);
            switchControl.Size = new System.Drawing.Size(50, 24);
            
            // 右侧标签："深色"
            _scrollBarRightLabel = new AntdUI.Label
            {
                Location = new System.Drawing.Point(labelWidth + 140, y),
                Size = new System.Drawing.Size(50, 40),
                Text = "深色",
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };
            this.pnlColorConfig.Controls.Add(_scrollBarRightLabel);
            
            switchControl.CheckedChanged += (s, e) =>
            {
                if (!_isLoading)
                {
                    ColorPicker_ValueChanged(null, null); // 触发保存逻辑
                }
            };
            this.pnlColorConfig.Controls.Add(switchControl);

            y += rowHeight;
        }

        private void AddSimpleSwitchRow(AntdUI.Label label, AntdUI.Switch switchControl, string text, ref int y, int labelWidth, int rowHeight)
        {
            label.Location = new System.Drawing.Point(20, y);
            label.Size = new System.Drawing.Size(labelWidth, 40);
            label.Text = text;
            this.pnlColorConfig.Controls.Add(label);

            // 配置 Switch 控件
            switchControl.Location = new System.Drawing.Point(labelWidth + 30, y + 8);
            switchControl.Size = new System.Drawing.Size(50, 24);
            
            // 注意：此方法用于通用开关，具体的事件处理在调用处设置
            this.pnlColorConfig.Controls.Add(switchControl);

            y += rowHeight;
        }

        /// <summary>
        /// 添加图标缩略图显示行
        /// </summary>
        private void AddIconThumbnailsRow(ref int y, int labelWidth, int rowHeight)
        {
            // 创建容器Panel
            pnlIconThumbnails = new Panel
            {
                Location = new Point(labelWidth + 30, y),
                Size = new Size(200, 80),
                BackColor = Color.Transparent
            };

            // 创建第一个图标缩略图（cat_full.ico）
            picIconFull = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            // 创建第二个图标缩略图（cat_empty.ico）
            picIconEmpty = new PictureBox
            {
                Location = new Point(80, 0),
                Size = new Size(64, 64),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };

            // 加载图标
            LoadIconThumbnails();

            // 添加到容器
            pnlIconThumbnails.Controls.Add(picIconFull);
            pnlIconThumbnails.Controls.Add(picIconEmpty);

            // 添加到配置面板
            this.pnlColorConfig.Controls.Add(pnlIconThumbnails);

            // 更新Y坐标
            y += 90; // 80像素高度 + 10像素间距
        }

        /// <summary>
        /// 加载图标缩略图
        /// </summary>
        private void LoadIconThumbnails()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                
                // 加载 cat_full.ico
                LoadIconToPictureBox(picIconFull, "cat_full.ico", assembly);
                
                // 加载 cat_empty.ico
                LoadIconToPictureBox(picIconEmpty, "cat_empty.ico", assembly);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载图标缩略图失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 将图标加载到PictureBox
        /// </summary>
        private void LoadIconToPictureBox(PictureBox pictureBox, string iconFileName, System.Reflection.Assembly assembly)
        {
            try
            {
                var resourcePaths = new[]
                {
                    $"WindowsFormsApp3.Resources.PopcatIcons.{iconFileName}",
                    $"Resources.PopcatIcons.{iconFileName}",
                    $"PopcatIcons.{iconFileName}",
                    $"WindowsFormsApp3.Resources.{iconFileName}",
                    $"Resources.{iconFileName}",
                    iconFileName
                };

                string resourcePath = null;
                foreach (var path in resourcePaths)
                {
                    var stream = assembly.GetManifestResourceStream(path);
                    if (stream != null)
                    {
                        resourcePath = path;
                        stream.Dispose();
                        break;
                    }
                }

                if (resourcePath != null)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourcePath))
                    {
                        if (stream != null)
                        {
                            // 尝试作为图像加载
                            try
                            {
                                stream.Position = 0;
                                Image loadedImage = Image.FromStream(stream);
                                pictureBox.Image = new Bitmap(loadedImage);
                                loadedImage.Dispose();
                            }
                            catch
                            {
                                // 如果直接加载失败，尝试作为图标加载
                                stream.Position = 0;
                                using (Icon icon = new Icon(stream, 64, 64))
                                {
                                    pictureBox.Image = icon.ToBitmap();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"加载图标 {iconFileName} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新图标缩略图显示/隐藏状态
        /// </summary>
        private void UpdateIconThumbnailsVisibility()
        {
            if (pnlIconThumbnails != null)
            {
                // 根据图标切换功能是否启用来显示/隐藏缩略图
                // 当前设计：始终显示缩略图用于预览
                pnlIconThumbnails.Visible = true;
            }
        }

        private void AddNumericRow(AntdUI.Label label, NumericUpDown numericControl, string text, ref int y, int labelWidth, int pickerWidth, int rowHeight, int minValue, int maxValue)
        {
            label.Location = new System.Drawing.Point(20, y + 5);
            label.Size = new System.Drawing.Size(labelWidth, 30);
            label.Text = text;
            this.pnlColorConfig.Controls.Add(label);

            numericControl.Location = new System.Drawing.Point(labelWidth + 30, y);
            numericControl.Size = new System.Drawing.Size(120, 40);
            numericControl.Minimum = minValue;
            numericControl.Maximum = maxValue;
            numericControl.ValueChanged += (s, e) =>
            {
                if (!_isLoading)
                {
                    NumericValueChanged();
                }
            };
            this.pnlColorConfig.Controls.Add(numericControl);

            y += rowHeight;
        }

        private void AddSliderRow(AntdUI.Label label, AntdUI.Slider slider, AntdUI.Label valueLabel, string text, ref int y, int labelWidth, int pickerWidth, int rowHeight, int minValue, int maxValue)
        {
            label.Location = new System.Drawing.Point(20, y);
            label.Size = new System.Drawing.Size(labelWidth, 40);
            label.Text = text;
            this.pnlColorConfig.Controls.Add(label);

            slider.Location = new System.Drawing.Point(labelWidth + 30, y + 8);
            slider.Size = new System.Drawing.Size(pickerWidth - 80, 24);
            slider.Value = Math.Max(minValue, Math.Min(maxValue, slider.Value));
            slider.ValueChanged += (s, e) =>
            {
                if (!_isLoading)
                {
                    if (valueLabel != null)
                    {
                        valueLabel.Text = $"{slider.Value}%";
                    }
                    SliderValueChanged();
                }
            };
            this.pnlColorConfig.Controls.Add(slider);

            if (valueLabel != null)
            {
                valueLabel.Location = new System.Drawing.Point(labelWidth + pickerWidth - 50, y);
                valueLabel.Size = new System.Drawing.Size(50, 40);
                valueLabel.Text = $"{slider.Value}%";
                valueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                this.pnlColorConfig.Controls.Add(valueLabel);
            }

            y += rowHeight;
        }

        private void AddFilePickerRow(AntdUI.Label label, TextBox textBox, AntdUI.Button button, string text, ref int y, int labelWidth, int pickerWidth, int rowHeight)
        {
            label.Location = new System.Drawing.Point(20, y + 5);
            label.Size = new System.Drawing.Size(labelWidth, 30);
            label.Text = text;
            this.pnlColorConfig.Controls.Add(label);

            textBox.Location = new System.Drawing.Point(labelWidth + 30, y + 5);
            textBox.Size = new System.Drawing.Size(pickerWidth - 100, 30);
            textBox.ReadOnly = true;
            // 文件路径改变事件在调用处处理
            this.pnlColorConfig.Controls.Add(textBox);

            button.Location = new System.Drawing.Point(labelWidth + pickerWidth - 60, y + 5);
            button.Size = new System.Drawing.Size(60, 30);
            button.Text = "选择";
            button.Type = AntdUI.TTypeMini.Default;
            button.Click += (s, e) => SelectImageFile(textBox);
            this.pnlColorConfig.Controls.Add(button);

            y += rowHeight;
        }

        private void AddComboBoxRow(AntdUI.Label label, AntdUI.Select comboBox, string text, ref int y, int labelWidth, int pickerWidth, int rowHeight, string[] items)
        {
            label.Location = new System.Drawing.Point(20, y + 5);
            label.Size = new System.Drawing.Size(labelWidth, 30);
            label.Text = text;
            this.pnlColorConfig.Controls.Add(label);

            comboBox.Location = new System.Drawing.Point(labelWidth + 30, y);
            comboBox.Size = new System.Drawing.Size(pickerWidth, 40);
            comboBox.Items.Clear();
            foreach (var item in items)
            {
                comboBox.Items.Add(item);
            }
            // 下拉框值改变事件在调用处处理
            this.pnlColorConfig.Controls.Add(comboBox);

            y += rowHeight;
        }


        // 移除旧的 UpdateScrollBarModeText 方法（不再需要）
        
        #endregion
    }
}
