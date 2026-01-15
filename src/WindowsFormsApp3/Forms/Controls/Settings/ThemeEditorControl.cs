using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

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
        /// 加载主题颜色到颜色选择器
        /// </summary>
        private void LoadThemeColors()
        {
            if (_editingTheme == null) return;

            _isLoading = true;

            // 背景色组
            cpBackground.Value = _editingTheme.Background;
            cpSurface.Value = _editingTheme.Surface;
            cpSurfaceLight.Value = _editingTheme.SurfaceLight;

            // 文字色组
            cpTextPrimary.Value = _editingTheme.TextPrimary;
            cpTextSecondary.Value = _editingTheme.TextSecondary;

            // 边框色
            cpBorder.Value = _editingTheme.Border;

            // 强调色组
            cpPrimary.Value = _editingTheme.Primary;
            cpSuccess.Value = _editingTheme.Success;
            cpWarning.Value = _editingTheme.Warning;
            cpError.Value = _editingTheme.Error;

            // 交互色组
            cpBackActive.Value = _editingTheme.BackActive;
            cpBackHover.Value = _editingTheme.BackHover;

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
        /// 更新控件可编辑状态（内置主题不可编辑此颜色）
        /// </summary>
        private void UpdateEditability()
        {
            bool isBuiltIn = _editingTheme?.IsBuiltIn ?? true;
            bool canEdit = !isBuiltIn;

            // 如果是内置主题，可以基于它创建新主题，但不能直接修改颜色
            EnableColorPickers(canEdit);
            
            // 设置按钮启用状态
            btnSave.Enabled = canEdit;
            btnDelete.Enabled = canEdit;
            
            // 为禁用按钮设置更明显的样式（解决深色模式下不清晰的问题）
            if (!canEdit)
            {
                // 禁用状态：使用更亮的文字颜色确保在深色背景下清晰可见
                btnSave.BackColor = Color.FromArgb(100, 100, 100);
                btnSave.ForeColor = Color.FromArgb(240, 240, 240);
                btnDelete.BackColor = Color.FromArgb(100, 100, 100);
                btnDelete.ForeColor = Color.FromArgb(240, 240, 240);
            }
            else
            {
                // 启用状态：恢复原本的 Type 颜色
                // AntdUI 按钮会根据 Type 自动设置颜色
                btnSave.BackColor = Color.Empty;
                btnDelete.BackColor = Color.Empty;
            }
        }

        /// <summary>
        /// 启用/禁用所有颜色选择器
        /// </summary>
        private void EnableColorPickers(bool enabled)
        {
            cpBackground.Enabled = enabled;
            cpSurface.Enabled = enabled;
            cpSurfaceLight.Enabled = enabled;
            cpTextPrimary.Enabled = enabled;
            cpTextSecondary.Enabled = enabled;
            cpBorder.Enabled = enabled;
            cpPrimary.Enabled = enabled;
            cpSuccess.Enabled = enabled;
            cpWarning.Enabled = enabled;
            cpError.Enabled = enabled;
            cpBackActive.Enabled = enabled;
            cpBackHover.Enabled = enabled;
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
            _editingTheme.SurfaceLight = cpSurfaceLight.Value;
            _editingTheme.TextPrimary = cpTextPrimary.Value;
            _editingTheme.TextSecondary = cpTextSecondary.Value;
            _editingTheme.Border = cpBorder.Value;
            _editingTheme.Primary = cpPrimary.Value;
            _editingTheme.Success = cpSuccess.Value;
            _editingTheme.Warning = cpWarning.Value;
            _editingTheme.Error = cpError.Value;
            _editingTheme.BackActive = cpBackActive.Value;
            _editingTheme.BackHover = cpBackHover.Value;

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

            if (_themeManager.SaveCustomTheme(_editingTheme))
            {
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

            _themeManager.SetCurrentTheme(_editingTheme.Name);
            AppSettings.CurrentThemeName = _editingTheme.Name;
            AppSettings.Save();

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
            AddColorRow(lblSurface, cpSurface, "卡片:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblSurfaceLight, cpSurfaceLight, "输入框:", ref y, labelWidth, pickerWidth, rowHeight);

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
            AddColorRow(lblError, cpError, "Error:", ref y, labelWidth, pickerWidth, rowHeight);

            y += 10;

            // 交互色组
            AddGroupLabel(lblInteractionGroup, "交互色组", "ThunderboltOutlined", ref y);
            AddColorRow(lblBackActive, cpBackActive, "激活状态:", ref y, labelWidth, pickerWidth, rowHeight);
            AddColorRow(lblBackHover, cpBackHover, "悬停状态:", ref y, labelWidth, pickerWidth, rowHeight);
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
            picker.ValueChanged += new AntdUI.ColorEventHandler(this.ColorPicker_ValueChanged);
            this.pnlColorConfig.Controls.Add(picker);

            y += rowHeight;
        }
        
        #endregion
    }
}
