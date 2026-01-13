using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    public partial class SettingsFontTextControl : UserControl
    {
        public event EventHandler SettingsSaved;

        private Control selectedRow = null;
        private readonly string[] allTextItems = { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号", "列组合" };
        private string _selectedFont = "Microsoft YaHei";
        private List<FontFamily> _systemFonts = new List<FontFamily>();

        public SettingsFontTextControl()
        {
            InitializeComponent();

            // 仅在运行时加载设置
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                LoadSettings();
                
                // 在控件加载完成后刷新主题
                this.Load += (s, e) =>
                {
                    RefreshRowThemes();
                    ApplyThemeToControls();
                };

                // 在控件可见时重新应用主题（确保在 ThemeHelper 应用后执行）
                this.VisibleChanged += (s, e) =>
                {
                    if (this.Visible)
                    {
                        // 延迟应用，确保 ThemeHelper 已完成
                        System.Threading.Tasks.Task.Delay(50).ContinueWith(_ =>
                        {
                            if (this.InvokeRequired)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    ApplyThemeToControls();
                                    RefreshRowThemes();
                                }));
                            }
                            else
                            {
                                ApplyThemeToControls();
                                RefreshRowThemes();
                            }
                        });
                    }
                };
            }
        }

        /// <summary>
        /// 应用主题到控件
        /// </summary>
        private void ApplyThemeToControls()
        {
            try
            {
                Color textColor = GetTextColor();
                
                // 应用主题到字体预览输入框
                if (txtFontPreview != null && !txtFontPreview.IsDisposed)
                {
                    txtFontPreview.ForeColor = textColor;
                    // 强制刷新显示
                    txtFontPreview.Invalidate();
                }

                // 应用主题到组合预览输入框
                if (txtComboPreview != null && !txtComboPreview.IsDisposed)
                {
                    txtComboPreview.ForeColor = textColor;
                    // 强制刷新显示
                    txtComboPreview.Invalidate();
                }

                LogHelper.Debug($"应用主题到控件，文字颜色: {textColor}, 是否深色主题: {IsDarkTheme()}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"应用主题到控件失败: {ex.Message}", ex);
            }
        }

        private void LoadSettings()
        {
            LoadTextItems();
            LoadSystemFonts();
        }

        public void SaveSettings()
        {
            SaveTextItems();
            SaveFontSettings();
            AppSettings.Save();
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }

        #region 文字项管理逻辑

        private void LoadTextItems()
        {
            pnlTextItems.Controls.Clear();

            // 加载保存的文字项
            string savedItems = AppSettings.Get("TextItems") as string;
            var loadDict = new Dictionary<string, bool>();
            var displayOrder = new List<string>();

            if (!string.IsNullOrEmpty(savedItems))
            {
                string[] parts = savedItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length % 2 == 0)
                {
                    for (int i = 0; i < parts.Length; i += 2)
                    {
                        string text = parts[i];
                        if (Array.IndexOf(allTextItems, text) > -1)
                        {
                            bool isChecked = bool.Parse(parts[i + 1]);
                            loadDict[text] = isChecked;
                            displayOrder.Add(text);
                        }
                    }
                }
            }

            // 添加缺失的项
            foreach (var item in allTextItems)
            {
                if (!loadDict.ContainsKey(item))
                {
                    loadDict[item] = true;
                    displayOrder.Add(item);
                }
            }

            // 创建UI行
            foreach (var text in displayOrder)
            {
                CreateRow(text, loadDict[text]);
            }

            // 应用主题到所有行
            RefreshRowThemes();

            UpdateComboPreview();
        }

        /// <summary>
        /// 刷新所有行的主题颜色
        /// </summary>
        private void RefreshRowThemes()
        {
            foreach (Control row in pnlTextItems.Controls)
            {
                if (row is Panel p)
                {
                    // 查找拖拽图标
                    Label lblDrag = null;
                    foreach (Control ctrl in p.Controls)
                    {
                        if (ctrl is Label lbl && lbl.Tag?.ToString() == "dragIcon")
                        {
                            lblDrag = lbl;
                            break;
                        }
                    }

                    // 应用主题
                    if (lblDrag != null)
                    {
                        ApplyRowTheme(p, lblDrag, p == selectedRow);
                    }
                }
            }
        }

        private void CreateRow(string text, bool isChecked)
        {
            var row = new Panel
            {
                Size = new Size(pnlTextItems.Width - 25, 30),
                Margin = new Padding(0, 0, 0, 2),
                AllowDrop = true,
                Tag = "textItemRow"  // 标记为文字项行，用于主题应用
            };

            // 添加拖拽图标标签
            var lblDrag = new Label
            {
                Text = "☰",  // 拖拽图标
                Location = new Point(5, 5),
                Size = new Size(20, 20),
                Cursor = Cursors.SizeAll,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Tag = "dragIcon"  // 标记为拖拽图标
            };

            var chk = new AntdUI.Checkbox
            {
                Text = text,
                Checked = isChecked,
                Location = new Point(30, 5),
                Size = new Size(200, 20),
                AutoCheck = true
            };

            chk.CheckedChanged += (s, e) => UpdateComboPreview();

            // 应用主题颜色
            ApplyRowTheme(row, lblDrag, false);

            // 选择行（只在空白区域点击时选择）
            row.Click += (s, e) =>
            {
                var mousePos = row.PointToClient(Cursor.Position);
                // 如果点击在拖拽图标或 Checkbox 之外的区域
                if (mousePos.X > 230 || mousePos.Y < 0 || mousePos.Y > 30)
                {
                    SelectRow(row);
                }
            };

            // 拖拽事件 - 只绑定到拖拽图标
            lblDrag.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    draggedRow = row;
                    SelectRow(row);
                    // 添加视觉反馈
                    row.BackColor = GetDragColor();
                    row.DoDragDrop(row, DragDropEffects.Move);
                    // 恢复颜色
                    if (selectedRow == row)
                    {
                        row.BackColor = GetSelectedColor();
                    }
                    else
                    {
                        row.BackColor = GetNormalColor();
                    }
                }
            };

            lblDrag.MouseEnter += (s, e) => lblDrag.BackColor = GetHoverColor();
            lblDrag.MouseLeave += (s, e) => lblDrag.BackColor = Color.Transparent;

            // Panel 的拖拽事件
            row.DragEnter += Row_DragEnter;
            row.DragOver += Row_DragOver;
            row.DragDrop += Row_DragDrop;

            row.Controls.Add(lblDrag);
            row.Controls.Add(chk);
            pnlTextItems.Controls.Add(row);
        }

        /// <summary>
        /// 应用行主题
        /// </summary>
        private void ApplyRowTheme(Panel row, Label lblDrag, bool isSelected)
        {
            row.BackColor = isSelected ? GetSelectedColor() : GetNormalColor();
            lblDrag.ForeColor = GetTextColor();
        }

        /// <summary>
        /// 获取正常状态背景色
        /// </summary>
        private Color GetNormalColor()
        {
            // 检查是否为深色主题
            return IsDarkTheme() ? Color.FromArgb(45, 45, 48) : Color.White;
        }

        /// <summary>
        /// 获取选中状态背景色
        /// </summary>
        private Color GetSelectedColor()
        {
            return IsDarkTheme() ? Color.FromArgb(51, 153, 255) : Color.FromArgb(230, 247, 255);
        }

        /// <summary>
        /// 获取拖拽状态背景色
        /// </summary>
        private Color GetDragColor()
        {
            return IsDarkTheme() ? Color.FromArgb(0, 122, 204) : Color.FromArgb(200, 230, 255);
        }

        /// <summary>
        /// 获取悬停状态背景色
        /// </summary>
        private Color GetHoverColor()
        {
            return IsDarkTheme() ? Color.FromArgb(62, 62, 64) : Color.FromArgb(240, 240, 240);
        }

        /// <summary>
        /// 获取高亮状态背景色（拖拽目标）
        /// </summary>
        private Color GetHighlightColor()
        {
            return IsDarkTheme() ? Color.FromArgb(255, 200, 100) : Color.FromArgb(255, 255, 200);
        }

        /// <summary>
        /// 获取文字颜色
        /// </summary>
        private Color GetTextColor()
        {
            return IsDarkTheme() ? Color.FromArgb(241, 241, 241) : Color.Black;
        }

        /// <summary>
        /// 判断是否为深色主题
        /// </summary>
        private bool IsDarkTheme()
        {
            // 通过检查父容器的背景色来判断主题
            // 深色主题的背景色通常较暗（RGB值较小）
            if (this.ParentForm != null)
            {
                var bgColor = this.ParentForm.BackColor;
                // 计算亮度：(R * 299 + G * 587 + B * 114) / 1000
                int brightness = (bgColor.R * 299 + bgColor.G * 587 + bgColor.B * 114) / 1000;
                return brightness < 128;  // 亮度小于128认为是深色主题
            }
            return false;
        }

        // 拖拽相关变量
        private Panel draggedRow = null;

        private void Row_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Panel)))
            {
                e.Effect = DragDropEffects.Move;
                
                // 先清除所有行的高亮
                foreach (Control ctrl in pnlTextItems.Controls)
                {
                    if (ctrl is Panel p && p != draggedRow && p != selectedRow)
                    {
                        p.BackColor = GetNormalColor();
                    }
                }
                
                // 高亮当前目标行
                if (sender is Panel targetRow && targetRow != draggedRow)
                {
                    targetRow.BackColor = GetHighlightColor();
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void Row_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Panel)))
            {
                e.Effect = DragDropEffects.Move;
            }
        }

        private void Row_DragLeave(object sender, EventArgs e)
        {
            // 恢复目标行颜色
            if (sender is Panel targetRow && targetRow != draggedRow && targetRow != selectedRow)
            {
                targetRow.BackColor = GetNormalColor();
            }
        }

        private void Row_DragDrop(object sender, DragEventArgs e)
        {
            if (sender is Panel targetRow && draggedRow != null && draggedRow != targetRow)
            {
                try
                {
                    // 获取拖拽行和目标行的索引
                    int draggedIndex = pnlTextItems.Controls.IndexOf(draggedRow);
                    int targetIndex = pnlTextItems.Controls.IndexOf(targetRow);

                    if (draggedIndex != -1 && targetIndex != -1)
                    {
                        LogHelper.Debug($"拖拽排序: 从位置 {draggedIndex} 移动到 {targetIndex}");

                        // 暂停布局
                        pnlTextItems.SuspendLayout();

                        // 移除拖拽行
                        pnlTextItems.Controls.Remove(draggedRow);

                        // 重新计算目标索引（因为移除了一个控件）
                        if (draggedIndex < targetIndex)
                        {
                            targetIndex--;
                        }

                        // 在目标位置插入
                        pnlTextItems.Controls.Add(draggedRow);
                        pnlTextItems.Controls.SetChildIndex(draggedRow, targetIndex);

                        // 恢复布局
                        pnlTextItems.ResumeLayout();
                        pnlTextItems.PerformLayout();

                        // 更新预览
                        UpdateComboPreview();

                        LogHelper.Debug($"拖拽排序完成: 新位置 {pnlTextItems.Controls.IndexOf(draggedRow)}");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"拖拽排序失败: {ex.Message}", ex);
                }
                finally
                {
                    // 清除所有高亮状态，使用主题颜色
                    foreach (Control ctrl in pnlTextItems.Controls)
                    {
                        if (ctrl is Panel p)
                        {
                            if (p == selectedRow)
                            {
                                p.BackColor = GetSelectedColor();  // 选中状态
                            }
                            else
                            {
                                p.BackColor = GetNormalColor();  // 正常状态
                            }
                        }
                    }
                    
                    draggedRow = null;
                }
            }
        }

        private void SelectRow(Control row)
        {
            if (selectedRow != null)
            {
                selectedRow.BackColor = GetNormalColor();
            }
            selectedRow = row;
            selectedRow.BackColor = GetSelectedColor();
        }

        private void UpdateComboPreview()
        {
            var previewParts = new List<string>();
            foreach (Control row in pnlTextItems.Controls)
            {
                if (row is Panel p)
                {
                    // 查找 Checkbox 控件（现在是第二个控件，因为第一个是拖拽图标）
                    AntdUI.Checkbox chk = null;
                    foreach (Control ctrl in p.Controls)
                    {
                        if (ctrl is AntdUI.Checkbox checkbox)
                        {
                            chk = checkbox;
                            break;
                        }
                    }

                    if (chk != null && chk.Checked)
                    {
                        previewParts.Add(chk.Text);
                    }
                }
            }

            string sep = AppSettings.GetValue<string>("Separator") ?? "_";
            txtComboPreview.Text = string.Join(sep, previewParts);
        }

        private void SaveTextItems()
        {
            var sb = new System.Text.StringBuilder();
            foreach (Control row in pnlTextItems.Controls)
            {
                if (row is Panel p)
                {
                    // 查找 Checkbox 控件
                    AntdUI.Checkbox chk = null;
                    foreach (Control ctrl in p.Controls)
                    {
                        if (ctrl is AntdUI.Checkbox checkbox)
                        {
                            chk = checkbox;
                            break;
                        }
                    }

                    if (chk != null)
                    {
                        sb.Append($"{chk.Text}|{chk.Checked}|");
                    }
                }
            }

            AppSettings.Set("TextItems", sb.ToString().TrimEnd('|'));
        }

        #endregion

        #region 字体管理逻辑

        private void LoadSystemFonts()
        {
            try
            {
                cmbFontFamily.Items.Clear();
                _systemFonts.Clear();

                LogHelper.Info("开始加载系统字体...");

                // 显示加载提示
                lblPreviewHint.Text = "正在筛选PDF兼容的字体，请稍候...";
                lblPreviewHint.ForeColor = System.Drawing.Color.Blue;

                // 异步加载字体
                System.Threading.Tasks.Task.Run(() =>
                {
                    var allFonts = new List<string>();
                    int totalCount = 0;

                    try
                    {
                        // 加载系统所有已安装字体
                        using (InstalledFontCollection installedFonts = new InstalledFontCollection())
                        {
                            // 按字体名称排序，中文字体优先
                            var sortedFonts = installedFonts.Families
                                .OrderByDescending(f => IsCjkFont(f.Name))  // 中文字体优先
                                .ThenBy(f => f.Name)
                                .ToList();

                            totalCount = sortedFonts.Count;
                            LogHelper.Debug($"加载系统字体，共 {totalCount} 个");

                            int processedCount = 0;

                            // 添加PDF兼容的字体到列表
                            foreach (FontFamily font in sortedFonts)
                            {
                                processedCount++;
                                
                                try
                                {
                                    // 验证字体是否与 iTextSharp.LGPLv2 兼容
                                    if (IsPdfCompatible(font.Name))
                                    {
                                        allFonts.Add(font.Name);
                                        LogHelper.Debug($"加载PDF兼容字体: {font.Name}");
                                    }
                                    else
                                    {
                                        LogHelper.Debug($"字体 {font.Name} 不兼容PDF，已跳过");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogHelper.Debug($"字体 {font.Name} 验证失败: {ex.Message}");
                                }

                                // 更新进度
                                if (processedCount % 10 == 0 || processedCount == totalCount)
                                {
                                    UpdateLoadingProgress(processedCount, totalCount, allFonts.Count);
                                }
                            }

                            LogHelper.Info($"字体加载完成：共 {allFonts.Count} 个PDF兼容字体（从 {totalCount} 个系统字体中筛选）");
                        }

                        // 在 UI 线程更新字体列表
                        if (cmbFontFamily.InvokeRequired)
                        {
                            cmbFontFamily.Invoke(new Action(() =>
                            {
                                foreach (var fontName in allFonts)
                                {
                                    cmbFontFamily.Items.Add(fontName);
                                }

                                LogHelper.Debug($"已加载 {cmbFontFamily.Items.Count} 个PDF兼容字体");

                                // 加载保存的字体设置
                                string savedFont = AppSettings.GetValue<string>("IdentifierPageFont") ?? "Microsoft YaHei";
                                _selectedFont = savedFont;

                                // 选中保存的字体
                                SelectFont(savedFont);
                                UpdateFontPreview();

                                // 更新提示
                                lblPreviewHint.Text = $"已加载 {allFonts.Count} 个PDF兼容字体";
                                lblPreviewHint.ForeColor = System.Drawing.Color.Green;
                            }));
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"加载字体列表失败: {ex.Message}", ex);
                        
                        if (lblPreviewHint.InvokeRequired)
                        {
                            lblPreviewHint.Invoke(new Action(() =>
                            {
                                lblPreviewHint.Text = "加载PDF兼容字体列表失败，请重试";
                                lblPreviewHint.ForeColor = System.Drawing.Color.Red;
                            }));
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载系统字体失败: {ex.Message}", ex);
                lblPreviewHint.Text = "加载字体失败";
                lblPreviewHint.ForeColor = System.Drawing.Color.Red;
            }
        }

        /// <summary>
        /// 更新加载进度
        /// </summary>
        private void UpdateLoadingProgress(int processed, int total, int compatibleFontCount)
        {
            try
            {
                if (lblPreviewHint.InvokeRequired)
                {
                    lblPreviewHint.Invoke(new Action(() =>
                    {
                        lblPreviewHint.Text = $"正在筛选PDF兼容字体... ({processed}/{total}，已找到 {compatibleFontCount} 个)";
                        lblPreviewHint.ForeColor = System.Drawing.Color.Blue;
                    }));
                }
            }
            catch
            {
                // 忽略更新失败
            }
        }

        /// <summary>
        /// 检查字体是否与 iTextSharp.LGPLv2 兼容
        /// </summary>
        private bool IsPdfCompatible(string fontName)
        {
            try
            {
                // 使用 iTextSharp.LGPLv2.Core 测试字体兼容性
                return ITextSharpLGPLFontHelper.TestFontCompatibility(fontName);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 检查字体是否支持中文（已弃用，改用 IsPdfCompatible）
        /// </summary>
        [Obsolete("已弃用，请使用 IsPdfCompatible 方法")]
        private bool SupportsChinese(FontFamily fontFamily)
        {
            try
            {
                // 测试常用中文字符
                string testChars = "中文测试";
                
                using (var font = new Font(fontFamily, 12F, FontStyle.Regular))
                using (var bitmap = new Bitmap(1, 1))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // 测试每个中文字符是否有字形
                    foreach (char c in testChars)
                    {
                        // 使用 MeasureString 测试字符是否可以渲染
                        var size = graphics.MeasureString(c.ToString(), font);
                        
                        // 如果宽度为0或太小，说明字体不支持该字符
                        if (size.Width < 1)
                        {
                            return false;
                        }
                    }
                    
                    return true;
                }
            }
            catch
            {
                // 如果测试失败，认为不支持中文
                return false;
            }
        }

        /// <summary>
        /// 快速测试字体的兼容性（使用 iTextSharp.LGPLv2.Core）
        /// </summary>
        private bool TestFontCompatibilityQuick(string fontName)
        {
            try
            {
                // 使用 iTextSharp.LGPLv2.Core 测试字体兼容性
                return ITextSharpLGPLFontHelper.TestFontCompatibility(fontName);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否为中日韩字体（用于排序优先显示）
        /// </summary>
        private bool IsCjkFont(string fontName)
        {
            string[] cjkKeywords = { 
                "黑", "宋", "楷", "仿宋", "雅黑", "微软", "华文", "方正", "思源", 
                "SimHei", "SimSun", "KaiTi", "FangSong", "YaHei", "Microsoft", 
                "Noto Sans SC", "Noto Sans CJK", "Source Han", "PingFang",
                "Hiragino", "Meiryo", "Malgun", "Gulim"
            };
            return cjkKeywords.Any(k => fontName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void SelectFont(string fontName)
        {
            for (int i = 0; i < cmbFontFamily.Items.Count; i++)
            {
                if (string.Equals(cmbFontFamily.Items[i] as string, fontName, StringComparison.OrdinalIgnoreCase))
                {
                    cmbFontFamily.SelectedIndex = i;
                    return;
                }
            }

            // 如果找不到保存的字体，尝试选择微软雅黑
            for (int i = 0; i < cmbFontFamily.Items.Count; i++)
            {
                string name = cmbFontFamily.Items[i] as string;
                if (name != null && name.Contains("Microsoft YaHei"))
                {
                    cmbFontFamily.SelectedIndex = i;
                    _selectedFont = name;
                    return;
                }
            }

            // 最后选择第一个字体
            if (cmbFontFamily.Items.Count > 0)
            {
                cmbFontFamily.SelectedIndex = 0;
                _selectedFont = cmbFontFamily.Items[0] as string;
            }
        }

        private void CmbFontFamily_SelectedIndexChanged(object sender, AntdUI.IntEventArgs e)
        {
            if (e.Value >= 0 && e.Value < cmbFontFamily.Items.Count)
            {
                string selectedFontName = cmbFontFamily.Items[e.Value] as string;
                if (!string.IsNullOrEmpty(selectedFontName))
                {
                    _selectedFont = selectedFontName;
                    UpdateFontPreview();
                    LogHelper.Debug($"选择字体: {_selectedFont}");
                }
            }
        }

        private void UpdateFontPreview()
        {
            try
            {
                // 1. 更新文本框字体（界面显示）
                txtFontPreview.Font = new Font(_selectedFont, 14F);
                txtFontPreview.ForeColor = GetTextColor();  // 使用主题颜色

                // 2. 更新提示标签（验证中状态）
                lblPreviewHint.Text = $"字体预览（左侧输入测试文本，右侧显示实际PDF渲染效果） - 当前字体: {_selectedFont} - 验证中...";
                lblPreviewHint.ForeColor = System.Drawing.Color.Gray;

                // 3. 异步验证 PDFsharp 兼容性（使用实际PDF生成API）
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    bool pdfCompatible = false;
                    try
                    {
                        pdfCompatible = TestPdfSharpFontRendering(_selectedFont);
                    }
                    catch (Exception testEx)
                    {
                        // 捕获测试过程中的所有异常
                        LogHelper.Error($"字体测试异常 [{_selectedFont}]: {testEx.Message}", testEx);
                        pdfCompatible = false;
                    }
                    
                    // 在UI线程更新显示
                    try
                    {
                        if (lblPreviewHint != null && !lblPreviewHint.IsDisposed)
                        {
                            if (lblPreviewHint.InvokeRequired)
                            {
                                lblPreviewHint.Invoke(new Action(() =>
                                {
                                    UpdateCompatibilityStatus(pdfCompatible);
                                }));
                            }
                            else
                            {
                                UpdateCompatibilityStatus(pdfCompatible);
                            }
                        }
                    }
                    catch (Exception uiEx)
                    {
                        // UI 更新异常（可能控件已释放）
                        LogHelper.Debug($"UI 更新异常: {uiEx.Message}");
                    }
                });

                // 添加异常观察，防止未观察到的任务异常
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        LogHelper.Error($"字体测试任务异常: {t.Exception.GetBaseException().Message}", t.Exception.GetBaseException());
                    }
                }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);

                LogHelper.Debug($"字体预览已更新: {_selectedFont}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"更新字体预览失败: {ex.Message}", ex);
                try
                {
                    txtFontPreview.Font = new Font("Microsoft YaHei", 14F);
                    lblPreviewHint.Text = "字体预览（左侧输入测试文本，右侧显示实际PDF渲染效果） - 默认字体";
                    lblPreviewHint.ForeColor = System.Drawing.Color.Black;
                    ClearPdfPreview();
                }
                catch
                {
                    // 忽略回退操作的异常
                }
            }
        }

        /// <summary>
        /// 更新兼容性状态显示
        /// </summary>
        private void UpdateCompatibilityStatus(bool pdfCompatible)
        {
            try
            {
                if (pdfCompatible)
                {
                    lblPreviewHint.Text = $"字体预览（左侧输入测试文本，右侧显示实际PDF渲染效果） - 当前字体: {_selectedFont} ✓ PDF兼容";
                    lblPreviewHint.ForeColor = System.Drawing.Color.Green;
                    LogHelper.Debug($"字体 {_selectedFont} 验证通过：PDF兼容");
                }
                else
                {
                    lblPreviewHint.Text = $"字体预览（左侧输入测试文本，右侧显示实际PDF渲染效果） - 当前字体: {_selectedFont} ✗ PDF不兼容，请选择其他字体";
                    lblPreviewHint.ForeColor = System.Drawing.Color.Red;
                    LogHelper.Debug($"字体 {_selectedFont} 验证失败：PDF不兼容");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"更新兼容性状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 文本框内容改变事件（用户输入测试文本）
        /// </summary>
        private void TxtFontPreview_TextChanged(object sender, EventArgs e)
        {
            // 用户输入时保持当前字体
            try
            {
                if (!string.IsNullOrEmpty(_selectedFont))
                {
                    txtFontPreview.Font = new Font(_selectedFont, 14F);
                    
                    // 重新验证字体兼容性（使用新的测试文本）
                    lblPreviewHint.Text = $"字体预览（左侧输入测试文本，右侧显示实际PDF渲染效果） - 当前字体: {_selectedFont} - 验证中...";
                    lblPreviewHint.ForeColor = System.Drawing.Color.Gray;
                    
                    // 异步验证
                    var task = System.Threading.Tasks.Task.Run(() =>
                    {
                        bool pdfCompatible = false;
                        try
                        {
                            pdfCompatible = TestPdfSharpFontRendering(_selectedFont);
                        }
                        catch (Exception testEx)
                        {
                            LogHelper.Error($"字体测试异常 [{_selectedFont}]: {testEx.Message}", testEx);
                            pdfCompatible = false;
                        }
                        
                        try
                        {
                            if (lblPreviewHint != null && !lblPreviewHint.IsDisposed)
                            {
                                if (lblPreviewHint.InvokeRequired)
                                {
                                    lblPreviewHint.Invoke(new Action(() =>
                                    {
                                        UpdateCompatibilityStatus(pdfCompatible);
                                    }));
                                }
                                else
                                {
                                    UpdateCompatibilityStatus(pdfCompatible);
                                }
                            }
                        }
                        catch (Exception uiEx)
                        {
                            LogHelper.Debug($"UI 更新异常: {uiEx.Message}");
                        }
                    });

                    // 添加异常观察
                    task.ContinueWith(t =>
                    {
                        if (t.IsFaulted && t.Exception != null)
                        {
                            LogHelper.Error($"字体测试任务异常: {t.Exception.GetBaseException().Message}", t.Exception.GetBaseException());
                        }
                    }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"更新预览文本字体失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试字体在 PDFsharp 中的渲染效果（使用实际的PDF生成API）
        /// <summary>
        /// 测试字体渲染效果（使用 iTextSharp.LGPLv2.Core）
        /// </summary>
        private bool TestPdfSharpFontRendering(string fontName)
        {
            try
            {
                // 使用 iTextSharp.LGPLv2.Core 测试字体
                string testText = txtFontPreview.Text;
                if (string.IsNullOrEmpty(testText))
                {
                    testText = "中文测试ABC123";
                }

                // 创建测试 PDF
                byte[] pdfBytes = GenerateTestPdf(fontName, testText);
                
                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    ClearPdfPreview();
                    return false;
                }

                // 生成预览图像
                GeneratePdfPreviewImageFromBytes(pdfBytes);
                
                LogHelper.Debug($"字体 {fontName} 测试成功");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"iTextSharp.LGPLv2 字体测试失败 [{fontName}]: {ex.GetType().Name} - {ex.Message}");
                ClearPdfPreview();
                return false;
            }
        }

        /// <summary>
        /// 生成测试 PDF
        /// </summary>
        private byte[] GenerateTestPdf(string fontName, string testText)
        {
            try
            {
                using (var ms = new System.IO.MemoryStream())
                {
                    // 创建文档，尺寸匹配 picPdfPreview (342x173)
                    // 使用更大的尺寸以获得更好的渲染质量，保持相同的宽高比
                    float width = 684;  // 342 * 2
                    float height = 346; // 173 * 2
                    
                    iTextSharp.text.Document document = new iTextSharp.text.Document(
                        new iTextSharp.text.Rectangle(width, height));
                    iTextSharp.text.pdf.PdfWriter writer = iTextSharp.text.pdf.PdfWriter.GetInstance(document, ms);
                    document.Open();

                    // 创建字体
                    iTextSharp.text.pdf.BaseFont baseFont = ITextSharpLGPLFontHelper.CreateBaseFont(fontName);

                    // 绘制白色背景（使用 DirectContentUnder 确保在文字下方）
                    iTextSharp.text.pdf.PdfContentByte cbUnder = writer.DirectContentUnder;
                    cbUnder.SetColorFill(iTextSharp.text.BaseColor.White);
                    cbUnder.Rectangle(0, 0, width, height);
                    cbUnder.Fill();

                    // 使用 DirectContent 直接绘制文字（确保在最上层）
                    iTextSharp.text.pdf.PdfContentByte cb = writer.DirectContent;
                    
                    // 计算合适的字体大小，让文字填充大部分区域
                    // 目标是让文字宽度占据约80%的PDF宽度
                    float targetWidth = width * 0.8f;
                    float fontSize = 48; // 初始字体大小
                    float textWidth = baseFont.GetWidthPoint(testText, fontSize);
                    
                    // 调整字体大小以适应目标宽度
                    if (textWidth > targetWidth)
                    {
                        fontSize = fontSize * (targetWidth / textWidth);
                    }
                    else if (textWidth < targetWidth * 0.5f)
                    {
                        // 如果文字太短，增大字体
                        fontSize = fontSize * (targetWidth * 0.6f / textWidth);
                    }
                    
                    // 限制字体大小范围
                    fontSize = Math.Max(24, Math.Min(fontSize, 72));
                    
                    // 重新计算文字宽度
                    textWidth = baseFont.GetWidthPoint(testText, fontSize);
                    
                    // 计算居中位置
                    float x = (width - textWidth) / 2;
                    float y = (height - fontSize) / 2; // 垂直居中
                    
                    cb.BeginText();
                    cb.SetFontAndSize(baseFont, fontSize);
                    cb.SetColorFill(iTextSharp.text.BaseColor.Black);
                    cb.SetTextMatrix(x, y);
                    cb.ShowText(testText);
                    cb.EndText();
                    
                    document.Close();

                    LogHelper.Debug($"生成测试 PDF 成功: {fontName}, 文字: {testText}, 字体大小: {fontSize}");
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"生成测试 PDF 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从 PDF 字节数据生成预览图像
        /// </summary>
        private void GeneratePdfPreviewImageFromBytes(byte[] pdfBytes)
        {
            try
            {
                // 保存到临时文件
                string tempPdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), 
                    $"font_preview_{Guid.NewGuid()}.pdf");
                System.IO.File.WriteAllBytes(tempPdfPath, pdfBytes);

                // 使用 PdfiumViewer 渲染 PDF 为图像
                using (var pdfDocument = PdfiumViewer.PdfDocument.Load(tempPdfPath))
                {
                    if (pdfDocument.PageCount > 0)
                    {
                        // 渲染第一页为图像，使用 picPdfPreview 的实际尺寸
                        // 342x173 的控件尺寸，使用 2 倍 DPI 以获得更清晰的效果
                        int renderWidth = picPdfPreview.Width * 2;
                        int renderHeight = picPdfPreview.Height * 2;
                        
                        var image = pdfDocument.Render(0, renderWidth, renderHeight, 
                            PdfiumViewer.PdfRenderFlags.Annotations);

                        // 在 UI 线程更新图像
                        if (picPdfPreview.InvokeRequired)
                        {
                            picPdfPreview.Invoke(new Action(() =>
                            {
                                // 释放旧图像
                                if (picPdfPreview.Image != null)
                                {
                                    var oldImage = picPdfPreview.Image;
                                    picPdfPreview.Image = null;
                                    oldImage.Dispose();
                                }
                                picPdfPreview.Image = image;
                            }));
                        }
                        else
                        {
                            if (picPdfPreview.Image != null)
                            {
                                var oldImage = picPdfPreview.Image;
                                picPdfPreview.Image = null;
                                oldImage.Dispose();
                            }
                            picPdfPreview.Image = image;
                        }
                    }
                }

                // 删除临时文件
                try
                {
                    System.IO.File.Delete(tempPdfPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"生成 PDF 预览图像失败: {ex.Message}");
                ClearPdfPreview();
            }
        }

        /// <summary>
        /// 生成 PDF 预览图像（PDFsharp 版本 - 已弃用）
        /// </summary>
        [Obsolete("已弃用，使用 GeneratePdfPreviewImageFromBytes 代替")]
        private void GeneratePdfPreviewImage(PdfSharp.Pdf.PdfDocument doc, PdfSharp.Pdf.PdfPage page)
        {
            try
            {
                // 保存 PDF 到临时文件
                string tempPdfPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"font_preview_{Guid.NewGuid()}.pdf");
                doc.Save(tempPdfPath);
                
                // 使用 PdfiumViewer 渲染 PDF 为图像
                using (var pdfDocument = PdfiumViewer.PdfDocument.Load(tempPdfPath))
                {
                    if (pdfDocument.PageCount > 0)
                    {
                        // 渲染第一页为图像（高 DPI 以获得清晰效果）
                        var image = pdfDocument.Render(0, 290, 60, PdfiumViewer.PdfRenderFlags.Annotations);
                        
                        // 在 UI 线程更新图像
                        if (picPdfPreview.InvokeRequired)
                        {
                            picPdfPreview.Invoke(new Action(() =>
                            {
                                // 释放旧图像
                                if (picPdfPreview.Image != null)
                                {
                                    var oldImage = picPdfPreview.Image;
                                    picPdfPreview.Image = null;
                                    oldImage.Dispose();
                                }
                                picPdfPreview.Image = image;
                            }));
                        }
                        else
                        {
                            if (picPdfPreview.Image != null)
                            {
                                var oldImage = picPdfPreview.Image;
                                picPdfPreview.Image = null;
                                oldImage.Dispose();
                            }
                            picPdfPreview.Image = image;
                        }
                    }
                }
                
                // 删除临时文件
                try
                {
                    System.IO.File.Delete(tempPdfPath);
                }
                catch
                {
                    // 忽略删除失败
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"生成 PDF 预览图像失败: {ex.Message}");
                ClearPdfPreview();
            }
        }

        /// <summary>
        /// 清除 PDF 预览图像
        /// </summary>
        private void ClearPdfPreview()
        {
            try
            {
                if (picPdfPreview.InvokeRequired)
                {
                    picPdfPreview.Invoke(new Action(() =>
                    {
                        if (picPdfPreview.Image != null)
                        {
                            var oldImage = picPdfPreview.Image;
                            picPdfPreview.Image = null;
                            oldImage.Dispose();
                        }
                    }));
                }
                else
                {
                    if (picPdfPreview.Image != null)
                    {
                        var oldImage = picPdfPreview.Image;
                        picPdfPreview.Image = null;
                        oldImage.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"清除 PDF 预览失败: {ex.Message}");
            }
        }

        private void BtnReloadFonts_Click(object sender, EventArgs e)
        {
            try
            {
                LogHelper.Info("重新加载系统字体");
                LoadSystemFonts();

                MessageBox.Show($"字体重新加载成功！共加载 {_systemFonts.Count} 个字体", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"重新加载字体失败: {ex.Message}", ex);
                MessageBox.Show($"重新加载字体失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveFontSettings()
        {
            AppSettings.Set("IdentifierPageFont", _selectedFont);
            LogHelper.Debug($"保存字体设置: {_selectedFont}");
        }

        #endregion
    }
}
