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
            
            // 使用 lambda 绑定事件，避免委托类型匹配问题
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

            // 清除现有控件
            pnlPreview.Controls.Clear();
            pnlPreview.AutoScroll = false; // 模拟界面通常不需要滚动
            
            // 1. 设置主容器背景 (模拟窗体)
            pnlPreview.BackColor = _editingTheme.Background;
            
            // 2. 创建侧边栏
            var sideBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 60,
                BackColor = _editingTheme.Background, // 用户要求侧边栏与主背景一致
                Padding = new Padding(0, 10, 0, 0)
            };
            
            // 侧边栏菜单项 (使用 Panel 模拟)
            // 选中项 (Active)
            var menuActive = new Panel
            {
                Height = 36,
                Dock = DockStyle.Top,
                BackColor = _editingTheme.BackActive,
                Padding = new Padding(15, 8, 0, 0)
            };
            var iconActive = new Label // 模拟图标
            {
                Text = "📂",
                ForeColor = _editingTheme.Primary,
                AutoSize = true,
                Font = new Font("Segoe UI Emoji", 10),
                Location = new Point(18, 8),
                BackColor = Color.Transparent
            };
            menuActive.Controls.Add(iconActive);
            
            // 普通项 (Normal)
            var menuNormal = new Panel
            {
                Height = 36,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent,
                Padding = new Padding(15, 8, 0, 0)
            };
            var iconNormal = new Label
            {
                Text = "⚙️",
                ForeColor = _editingTheme.TextSecondary,
                AutoSize = true,
                Font = new Font("Segoe UI Emoji", 10),
                Location = new Point(18, 8),
                BackColor = Color.Transparent
            };
            menuNormal.Controls.Add(iconNormal);
            
            // 悬停项 (Hover)
            var menuHover = new Panel
            {
                Height = 36,
                Dock = DockStyle.Top,
                BackColor = _editingTheme.BackHover,
                Padding = new Padding(15, 8, 0, 0)
            };
            var iconHover = new Label
            {
                Text = "🗂️",
                ForeColor = _editingTheme.TextPrimary,
                AutoSize = true,
                Font = new Font("Segoe UI Emoji", 10),
                Location = new Point(18, 8),
                BackColor = Color.Transparent
            };
            menuHover.Controls.Add(iconHover);
            
            sideBar.Controls.Add(menuHover);
            sideBar.Controls.Add(menuNormal);
            sideBar.Controls.Add(menuActive);
            
            // 3. 内容区域
            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _editingTheme.Background, // 保持与主背景一致
                Padding = new Padding(15)
            };
            
            // 模拟顶部栏 (Header)
            var header = new Panel
            {
                Height = 40,
                Dock = DockStyle.Top,
                BackColor = _editingTheme.Surface, // Header 通常需要一点区分，或者是白色/深灰
            };
            
            var lblHeader = new Label
            {
                Text = "应用预览",
                ForeColor = _editingTheme.TextPrimary,
                Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            header.Controls.Add(lblHeader);
            
            // 4. 内容卡片
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _editingTheme.Surface,
                Padding = new Padding(15)
            };
            
            // 如果有边框色，加个边框 (Panel 比较难直接加边框，这里忽略或用 Padding+BackColor 模拟)
            // 这里简单处理：
            
            // 卡片内的元素
            int y = 15;
            
            // 标题
            var cardTitle = new Label
            {
                Text = "设置选项",
                ForeColor = _editingTheme.TextPrimary,
                Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold),
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(cardTitle);
            y += 35;
            
            // 说明文本
            var cardDesc = new Label
            {
                Text = "这是一个模拟的设置面板，用于展示主题颜色的实际应用效果。",
                ForeColor = _editingTheme.TextSecondary,
                Font = new Font("Microsoft YaHei UI", 9),
                Location = new Point(15, y),
                Size = new Size(200, 40),
                BackColor = Color.Transparent
            };
            card.Controls.Add(cardDesc);
            y += 50;
            
            // 输入框模拟
            var lblInput = new Label
            {
                Text = "输入框示例:",
                ForeColor = _editingTheme.TextPrimary,
                Location = new Point(15, y),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblInput);
            y += 25;
            
            var inputBox = new Panel
            {
                Size = new Size(200, 30),
                Location = new Point(15, y),
                BackColor = _editingTheme.SurfaceLight
            };
            
            // 绘制边框
            inputBox.Paint += (s, e) =>
            {
                using (var pen = new Pen(_editingTheme.Border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, inputBox.Width - 1, inputBox.Height - 1);
                }
            };
            
            var inputText = new Label
            {
                Text = "示例文本...",
                ForeColor = _editingTheme.TextSecondary,
                Location = new Point(5, 5),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            inputBox.Controls.Add(inputText);
            card.Controls.Add(inputBox);
            y += 45;
            
            // 按钮组 - 使用 Label 模拟按钮以展示即时颜色
            var btnPrimary = CreateSimulatedButton("Primary", _editingTheme.Primary, Color.White, new Point(15, y));
            card.Controls.Add(btnPrimary);
            
            var btnSuccess = CreateSimulatedButton("Success", _editingTheme.Success, Color.White, new Point(105, y));
            card.Controls.Add(btnSuccess);
            y += 40; // Next row
            
            var btnWarning = CreateSimulatedButton("Warning", _editingTheme.Warning, Color.White, new Point(15, y));
            card.Controls.Add(btnWarning);
            
            var btnError = CreateSimulatedButton("Error", _editingTheme.Error, Color.White, new Point(105, y));
            card.Controls.Add(btnError);
            
            // 组装界面: SideBar | Content (Header + Card)
            var mainRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(10) // 间距
            };
            
            // Header 放在 mainRight 顶部
            header.Dock = DockStyle.Top;
            
            // Card 放在 mainRight 剩余部分
            card.Dock = DockStyle.Fill;
            card.BringToFront();
            
            var cardContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0), // Header 和 Card 的间距
                BackColor = Color.Transparent
            };
            cardContainer.Controls.Add(card);
            
            mainRight.Controls.Add(cardContainer);
            mainRight.Controls.Add(header);
            
            pnlPreview.Controls.Add(mainRight);
            pnlPreview.Controls.Add(sideBar);
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
            btnSave.Enabled = canEdit;
            btnDelete.Enabled = canEdit;
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
    }
}
