using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3
{
    /// <summary>
    /// V5 流水线一体化工作台（部分类）
    /// 负责实现右侧 360px 流水线工位的白底细线框卡片化布局、尺寸契约与交互呈现
    /// 唯一设计基准：docs/材料选择框终版一体化工作台.html
    /// </summary>
    public partial class MaterialSelectFormModern
    {
        // 右侧工位主容器与卡片部件
        private System.Windows.Forms.Panel pnlRightStation;
        private System.Windows.Forms.FlowLayoutPanel pnlStationContent;

        // 7 个常驻卡片和 1 个页框存在差异时显示的选择卡片
        private System.Windows.Forms.Panel pnlMaterialsCard;
        private System.Windows.Forms.Panel pnlOutputPathCard;
        private System.Windows.Forms.Panel pnlCraftMatrixCard;
        private System.Windows.Forms.Panel tlpOrderParametersCard;
        private System.Windows.Forms.Panel pnlImpositionDashboardCard;
        private System.Windows.Forms.Panel pnlPageBoxChoiceCard;
        private System.Windows.Forms.Label lblPageBoxChoiceTitle;
        private AntdUI.Button btnMediaBoxChoice;
        private AntdUI.Button btnCropBoxChoice;
        private AntdUI.Button btnPageBoxDetails;
        private ToolStripDropDown _pageBoxDetailsDropDown;
        private PageBoxInfo _pageBoxChoicePage;
        private string _pageBoxChoiceReason;
        private System.Windows.Forms.Panel presetBrowserCard;
        private System.Windows.Forms.Panel pnlActionBar;

        private HierarchicalPathDisplay outputPathTextBox;
        private ColoredFolderBrowseButton btnBrowseOutputFolder;
        private System.Windows.Forms.Label lblOutputPathTitle;
        private ToolStripDropDown outputFolderDropDown;
        private ToolStripControlHost outputFolderDropDownHost;
        private SerialSearchButton btnSearchSerialAction;

        // 排版控制行微型分段胶囊控件 (100% 像素级还原 HTML 原型 .mini-segmented)
        private MiniSegmentedControl segMaterialType;
        private MiniSegmentedControl segLayoutMode;
        private ImpositionToggleButton btnImpositionToggle;
        private IdentifierPageToggleButton btnIdentifierPageToggle;
        private BatchModeSwitch btnBatchModeSwitch;
        private CustomSizeCapsule _customSizeCapsule;

        // 象限 4: 一式N联控件 (去“一式”文字，联/份改为微型切换按钮胶囊)
        private System.Windows.Forms.Panel pnlCopyCountWrap;
        private AntdUI.Input txtCopyCountInput;
        private bool _isEditingCopyCount;
        private MiniSegmentedControl segCopyType;
        private System.Windows.Forms.Label lblCopyCountPill;
        private System.Windows.Forms.Label lblCopyModePill;

        // 模切形状第 4 项：圆角原地微型输入组件 (100% 还原 HTML 原型 .round-inplace-wrap)
        private System.Windows.Forms.Panel pnlRoundRectSlot;
        private System.Windows.Forms.Panel pnlInplaceRoundInput;
        private System.Windows.Forms.Panel pnlRadiusInputWrap;
        private System.Windows.Forms.TextBox txtInplaceRoundRadius;
        private System.Windows.Forms.Label lblRoundPrefix;
        private System.Windows.Forms.Label lblRoundSuffix;

        // 折叠条左右双侧显示标签 (左侧页码，右侧展开提示，100%还原HTML原型)
        private System.Windows.Forms.Panel pnlCollapseBar;
        private System.Windows.Forms.Label lblCollapsePageNumber;
        private System.Windows.Forms.Label lblCollapseArrow;

        // V5 纯净 6 宫格看板指标数据持有
        private string _v5GridText = "5 × 3";
        private string _v5CountText = "15";
        private string _v5RotationText = "0";
        private string _v5DimensionText = "80×45";
        private string _v5BleedText = "2.0mm";
        private string _v5ColorText = "彩色";
        private Color _v5ColorTextColor = Color.FromArgb(19, 194, 194);

        private System.Windows.Forms.Panel pnlPillGrid;
        private System.Windows.Forms.Panel pnlPillCount;
        private System.Windows.Forms.Panel pnlPillRotation;
        private System.Windows.Forms.Panel pnlPillDimension;
        private System.Windows.Forms.Panel pnlPillBleed;
        private System.Windows.Forms.Panel pnlPillColor;
        private System.Windows.Forms.TableLayoutPanel tlpMetrics;
        private Bitmap _v5MetricsGlowBitmap;
        private string _v5MetricsGlowCacheKey;
        private ContextMenuStrip bleedOptionsMenu;

        private static readonly Color CardBorderColor = Color.FromArgb(212, 219, 228); // #d4dbe4
        private static readonly Color CardBgColor = Color.White;

        /// <summary>
        /// 初始化 V5 右侧流水线工位架构（7 个常驻卡片及页框异常选择区）
        /// </summary>
        private void InitializeV5RightStation()
        {
            if (pnlRightStation != null) return;

            try
            {
                this.SuspendLayout();

                // 1. 创建右侧工位主容器 (固定宽 360px，取消 Bottom Anchor，防止任何自动二次缩放)
                pnlRightStation = new System.Windows.Forms.Panel
                {
                    Name = "pnlRightStation",
                    Size = new Size(STATION_WIDTH, this.ClientSize.Height),
                    Location = new Point(_isBatchPanelExpanded ? BATCH_PANEL_WIDTH : 0, 0),
                    BackColor = Color.White,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left
                };

                // 2. 标题栏 (26px)
                if (titleBarPanel != null)
                {
                    titleBarPanel.Dock = DockStyle.Top;
                    titleBarPanel.Height = TITLE_BAR_HEIGHT;
                    titleBarPanel.BackColor = Color.FromArgb(226, 232, 240);
                    titleBarPanel.Padding = new Padding(0, 0, 3, 0);
                    titleBarPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    pnlRightStation.Controls.Add(titleBarPanel);
                }

                // 3. 工位核心内容容器 (容纳 7 张卡片，流式向下堆叠，绝不挤压)
                pnlStationContent = new System.Windows.Forms.FlowLayoutPanel
                {
                    Name = "pnlStationContent",
                    Location = new Point(0, TITLE_BAR_HEIGHT),
                    Size = new Size(STATION_WIDTH, BASE_CLIENT_HEIGHT - TITLE_BAR_HEIGHT - COLLAPSE_BAR_HEIGHT), // 508 - 26 - 16 = 466px
                    BackColor = Color.White,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoScroll = false,
                    Padding = new Padding(10, 4, 10, 2),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left
                };

                // 构建卡片并加入流式面板
                BuildMaterialsCard();
                BuildOutputPathCard();
                BuildCraftMatrixCard();
                BuildOrderParametersCard();
                BuildImpositionDashboardCard();
                BuildPageBoxChoiceCard();
                BuildPresetBrowserCard();
                BuildActionBar();

                pnlRightStation.Controls.Add(pnlStationContent);

                // 4. 初始化折叠条并加入右侧工位主容器 (固定坐标，Anchor 仅限 Top|Left)
                SetupCollapseBar();
                if (pnlCollapseBar != null)
                {
                    pnlRightStation.Controls.Add(pnlCollapseBar);
                }
                if (previewCollapseButton != null)
                {
                    previewCollapseButton.Visible = false;
                }

                if (pdfPreviewPanel != null)
                {
                    pdfPreviewPanel.Location = new Point(0, BASE_CLIENT_HEIGHT);
                    pdfPreviewPanel.Size = new Size(STATION_WIDTH, _isPreviewExpanded ? MAX_PREVIEW_HEIGHT : 0);
                    pdfPreviewPanel.Visible = _isPreviewExpanded;
                    pdfPreviewPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                    pnlRightStation.Controls.Add(pdfPreviewPanel);
                }

                // 将右侧工位加入窗体并隐藏旧版散落的占位控件
                HideLegacyControls();
                this.Controls.Add(pnlRightStation);
                pnlRightStation.BringToFront();

                // 监听底层排版单选框变动，自动同步按钮高亮
                if (flatSheetRadioButton != null) flatSheetRadioButton.CheckedChanged += (s, e) => UpdateImpoModeButtonTypes();
                if (rollMaterialRadioButton != null) rollMaterialRadioButton.CheckedChanged += (s, e) => UpdateImpoModeButtonTypes();
                if (continuousLayoutRadioButton != null) continuousLayoutRadioButton.CheckedChanged += (s, e) => UpdateImpoModeButtonTypes();
                if (foldingLayoutRadioButton != null) foldingLayoutRadioButton.CheckedChanged += (s, e) => UpdateImpoModeButtonTypes();

                // 刷新看板初值
                UpdateV5ImpositionDashboard();
                UpdateImpoModeButtonTypes();
                UpdateV5InplaceRoundRadiusUI(SelectedShape == ShapeType.RoundRect);

                // 确保窗体与工位尺寸严格按当前展开状态对齐
                UpdateFormSizeBasedOnPreviewState();

                this.ResumeLayout(true);
                LogHelper.Debug("[MaterialSelectFormModern] V5 右侧流水线工位架构初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 初始化 V5 右侧工位失败: {ex.Message}", ex);
            }
        }

        /// <summary>C3 双尺寸：以一行展示页框差异和统一基准，精确参数放在详情浮层。</summary>
        private void BuildPageBoxChoiceCard()
        {
            pnlPageBoxChoiceCard = CreateWhiteCardPanel("pnlPageBoxChoiceCard", 26);
            pnlPageBoxChoiceCard.Visible = false;

            lblPageBoxChoiceTitle = new System.Windows.Forms.Label
            {
                Name = "lblPageBoxChoiceTitle",
                Text = "⚠ 框不同",
                Location = new Point(4, 3),
                Size = new Size(55, 20),
                Font = new Font("Microsoft YaHei UI", 7.5F),
                ForeColor = Color.FromArgb(191, 93, 18),
                TextAlign = ContentAlignment.MiddleLeft
            };

            btnMediaBoxChoice = CreatePageBoxChoiceButton("btnMediaBoxChoice", 60);
            btnCropBoxChoice = CreatePageBoxChoiceButton("btnCropBoxChoice", 189);
            btnMediaBoxChoice.Click += (s, e) => SelectPageBoxSource(PdfPageBoxSource.MediaBox);
            btnCropBoxChoice.Click += (s, e) => SelectPageBoxSource(PdfPageBoxSource.CropBox);
            btnPageBoxDetails = new AntdUI.Button
            {
                Name = "btnPageBoxDetails",
                Text = "ⓘ",
                AccessibleName = "查看页面框精确参数",
                Location = new Point(318, 3),
                Size = new Size(18, 20),
                Font = new Font("Segoe UI", 9F),
                DefaultBack = Color.Transparent,
                ForeColor = Color.FromArgb(130, 143, 161),
                Cursor = Cursors.Hand,
                WaveSize = 0
            };
            btnPageBoxDetails.Click += (s, e) => ShowPageBoxDetails();
            folderTreeViewToolTip?.SetToolTip(btnPageBoxDetails, "查看精确尺寸、坐标和统一基准说明（单位 mm）");
            Disposed += (s, e) => _pageBoxDetailsDropDown?.Dispose();

            pnlPageBoxChoiceCard.Controls.Add(lblPageBoxChoiceTitle);
            pnlPageBoxChoiceCard.Controls.Add(btnMediaBoxChoice);
            pnlPageBoxChoiceCard.Controls.Add(btnCropBoxChoice);
            pnlPageBoxChoiceCard.Controls.Add(btnPageBoxDetails);
            pnlStationContent.Controls.Add(pnlPageBoxChoiceCard);
        }

        private static AntdUI.Button CreatePageBoxChoiceButton(string name, int left)
        {
            return new AntdUI.Button
            {
                Name = name,
                Location = new Point(left, 3),
                Size = new Size(126, 20),
                Font = new Font("Microsoft YaHei UI", 7.5F),
                Radius = 3,
                BorderWidth = 1F,
                WaveSize = 0,
                Cursor = Cursors.Hand,
                Type = AntdUI.TTypeMini.Default
            };
        }

        internal void UpdatePageBoxChoiceFromInspection(PdfInspectorInfo inspection)
        {
            if (pnlPageBoxChoiceCard == null || IsDisposed) return;
            _pageBoxDetailsDropDown?.Close();

            var issue = inspection?.Issues?.FirstOrDefault(item =>
                item.Type == IssueType.OutOfBounds && string.Equals(item.BoxType, "CropBox", StringComparison.OrdinalIgnoreCase))
                ?? inspection?.Issues?.FirstOrDefault(item => item.Type == IssueType.DifferentPageBoxes &&
                    string.Equals(item.BoxType, "CropBox", StringComparison.OrdinalIgnoreCase));
            var page = issue == null ? null : inspection.AllPageBoxes
                .FirstOrDefault(item => item.PageNumber == issue.PageNumber);

            if (page?.MediaBox == null || page.CropBox == null)
            {
                bool selectionChanged = SelectedPageBoxSource != PdfPageBoxSource.Automatic;
                pnlPageBoxChoiceCard.Visible = false;
                _pageBoxChoicePage = null;
                SelectedPageBoxSource = PdfPageBoxSource.Automatic;
                SelectedPageBoxReferencePage = 1;
                if (selectionChanged)
                {
                    _pageBoxSelectionRevision++;
                    RefreshPageBoxLayout();
                }
            }
            else
            {
                SelectedPageBoxReferencePage = page.PageNumber;
                _pageBoxChoicePage = page;
                _pageBoxChoiceReason = issue.Type == IssueType.OutOfBounds
                    ? "CropBox 超出 MediaBox" : "媒体框与裁剪框不一致";
                btnMediaBoxChoice.Text = $"媒体 {FormatCompactPageBoxSize(page.MediaBox)}";
                btnCropBoxChoice.Text = $"裁剪 {FormatCompactPageBoxSize(page.CropBox)}";
                btnMediaBoxChoice.AccessibleName = $"按媒体框 {FormatPageBoxSize(page.MediaBox)} 统一全部页面框";
                btnCropBoxChoice.AccessibleName = $"按裁剪框 {FormatPageBoxSize(page.CropBox)} 统一全部页面框";
                bool positionDiffers = PdfTools.GetPageDimensionForDisplay(page.MediaBox.Left * 25.4 / 72) !=
                    PdfTools.GetPageDimensionForDisplay(page.CropBox.Left * 25.4 / 72) ||
                    PdfTools.GetPageDimensionForDisplay(page.MediaBox.Bottom * 25.4 / 72) !=
                    PdfTools.GetPageDimensionForDisplay(page.CropBox.Bottom * 25.4 / 72);
                lblPageBoxChoiceTitle.Text = issue.Type == IssueType.OutOfBounds ? "⚠ 超界" :
                    positionDiffers ? "⚠ 位置差" : "⚠ 框不同";
                folderTreeViewToolTip?.SetToolTip(btnMediaBoxChoice,
                    $"第{page.PageNumber}页 MediaBox：{FormatPageBoxSize(page.MediaBox)}；左下角 ({page.MediaBox.Left:0.####}, {page.MediaBox.Bottom:0.####}) pt。点击后统一所有页面框，框外内容将不可见。");
                folderTreeViewToolTip?.SetToolTip(btnCropBoxChoice,
                    $"第{page.PageNumber}页 CropBox：{FormatPageBoxSize(page.CropBox)}；左下角 ({page.CropBox.Left:0.####}, {page.CropBox.Bottom:0.####}) pt。点击后按此尺寸与位置统一所有页面框。");
                pnlPageBoxChoiceCard.Visible = true;
                SelectPageBoxSource(issue.Type == IssueType.OutOfBounds
                    ? PdfPageBoxSource.MediaBox : PdfPageBoxSource.CropBox);
            }

            pnlStationContent?.PerformLayout();
            UpdateFormSizeBasedOnPreviewState();
        }

        private static string FormatPageBoxSize(BoxDimension box)
        {
            return $"{box.Width * 25.4 / 72:0.####} × {box.Height * 25.4 / 72:0.####} mm";
        }

        private static string FormatCompactPageBoxSize(BoxDimension box)
        {
            return $"{PdfTools.GetPageDimensionForDisplay(box.Width * 25.4 / 72):0.#}×{PdfTools.GetPageDimensionForDisplay(box.Height * 25.4 / 72):0.#}";
        }

        private void ShowPageBoxDetails()
        {
            if (_pageBoxChoicePage == null) return;
            if (_pageBoxDetailsDropDown?.Visible == true)
            {
                _pageBoxDetailsDropDown.Close();
                return;
            }
            _pageBoxDetailsDropDown?.Dispose();
            var content = new Panel { Size = new Size(338, 167), BackColor = Color.White };
            string selected = SelectedPageBoxSource == PdfPageBoxSource.MediaBox ? "媒体框" : "裁剪框";
            AddPageBoxDetailLabel(content, "lblPageBoxDetailsTitle",
                $"第{SelectedPageBoxReferencePage}页 · {_pageBoxChoiceReason}", 8, 25, true);
            AddPageBoxDetailLabel(content, "lblMediaBoxDetails", FormatPageBoxDetails("媒体框 MediaBox", _pageBoxChoicePage.MediaBox), 38, 39);
            AddPageBoxDetailLabel(content, "lblCropBoxDetails", FormatPageBoxDetails("裁剪框 CropBox", _pageBoxChoicePage.CropBox), 82, 39);
            AddPageBoxDetailLabel(content, "lblPageBoxDetailsEffect",
                $"已选{selected}：按其尺寸与位置统一全部页面框。\n框外内容将不可见；点击另一尺寸可切换。", 128, 36);
            _pageBoxDetailsDropDown = new ToolStripDropDown
            {
                AutoSize = false, Size = new Size(340, 169),
                Padding = Padding.Empty, Margin = Padding.Empty, BackColor = Color.White
            };
            _pageBoxDetailsDropDown.Items.Add(new ToolStripControlHost(content)
            {
                AutoSize = false, Size = content.Size, Padding = Padding.Empty, Margin = Padding.Empty
            });
            _pageBoxDetailsDropDown.Show(pnlPageBoxChoiceCard, new Point(0, pnlPageBoxChoiceCard.Height + 2));
        }

        private static string FormatPageBoxDetails(string name, BoxDimension box)
        {
            return $"{name}    {FormatPageBoxSize(box)}\n左下角 ({box.Left * 25.4 / 72:0.####}, {box.Bottom * 25.4 / 72:0.####}) mm";
        }

        private static void AddPageBoxDetailLabel(Panel panel, string name, string text, int top, int height, bool bold = false)
        {
            panel.Controls.Add(new AntdUI.Label
            {
                Name = name, Text = text, Location = new Point(10, top), Size = new Size(318, height),
                Font = new Font("Microsoft YaHei UI", 8F, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.FromArgb(74, 85, 104)
            });
        }

        internal void SelectPageBoxSource(PdfPageBoxSource source)
        {
            if (source != PdfPageBoxSource.MediaBox && source != PdfPageBoxSource.CropBox) return;
            SelectedPageBoxSource = source;
            _pageBoxSelectionRevision++;
            if (_pageBoxChoicePage != null)
            {
                var selectedBox = source == PdfPageBoxSource.MediaBox ? _pageBoxChoicePage.MediaBox : _pageBoxChoicePage.CropBox;
                _initialWidth = selectedBox.Width * 25.4 / 72;
                _initialHeight = selectedBox.Height * 25.4 / 72;
                _originalWidth = _initialWidth.ToString();
                _originalHeight = _initialHeight.ToString();
                UpdateDimensionsWithBleed();
            }
            UpdatePageBoxChoiceStyle(btnMediaBoxChoice, source == PdfPageBoxSource.MediaBox);
            UpdatePageBoxChoiceStyle(btnCropBoxChoice, source == PdfPageBoxSource.CropBox);
            _pageBoxDetailsDropDown?.Close();
            folderTreeViewToolTip?.SetToolTip(lblPageBoxChoiceTitle,
                $"第{SelectedPageBoxReferencePage}页 {_pageBoxChoiceReason} · 已选{(source == PdfPageBoxSource.MediaBox ? "媒体框" : "裁剪框")}；点击尺寸统一全部页面框。");
            RefreshPageBoxLayout();
        }

        private static void UpdatePageBoxChoiceStyle(AntdUI.Button button, bool selected)
        {
            if (button == null) return;
            button.Type = AntdUI.TTypeMini.Default;
            button.DefaultBack = selected ? Color.FromArgb(238, 246, 255) : Color.White;
            button.DefaultBorderColor = selected ? Color.FromArgb(169, 204, 246) : Color.FromArgb(227, 232, 239);
            button.ForeColor = selected ? Color.FromArgb(22, 119, 255) : Color.FromArgb(74, 85, 104);
            button.Invalidate();
        }

        private void RefreshPageBoxLayout()
        {
            if (IsHandleCreated && !IsDisposed && !Disposing)
            {
                ClearLayoutDisplay();
                if (enableImpositionCheckbox?.Checked == true)
                    _ = _isDuplicateLayoutEnabled && _copyType == CopyType.Layout
                        ? CalculateDuplicateLayout() : CalculateAndUpdateLayout();
            }
        }

        /// <summary>
        /// 卡片 1: 常用材料卡片 (独立白底细框 · 4×4 纯净按钮网格 · 26px 高按钮居中对齐 · 127px 高)
        /// </summary>
        private void BuildMaterialsCard()
        {
            pnlMaterialsCard = CreateWhiteCardPanel("pnlMaterialsCard", 127);

            var tlp = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 4,
                BackColor = Color.Transparent,
                Padding = new Padding(2)
            };

            for (int i = 0; i < 4; i++)
            {
                tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
                tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }

            var matButtons = new[]
            {
                materialButton1, materialButton2, materialButton3, materialButton4,
                materialButton5, materialButton6, materialButton7, materialButton8,
                materialButton9, materialButton10, materialButton11, materialButton12,
                materialButton13, materialButton14, materialButton15
            };

            for (int idx = 0; idx < matButtons.Length; idx++)
            {
                var btn = matButtons[idx];
                if (btn != null)
                {
                    btn.Dock = DockStyle.Fill;
                    btn.Margin = new Padding(1);
                    btn.Radius = 3;
                    btn.Font = new Font("Microsoft YaHei UI", 9F);
                    btn.BorderWidth = 1F;
                    int row = idx / 4;
                    int col = idx % 4;
                    tlp.Controls.Add(btn, col, row);
                }
            }

            // 第 16 格放置更多材料下拉
            if (dropdown16 != null)
            {
                dropdown16.Dock = DockStyle.Fill;
                dropdown16.Margin = new Padding(1);
                dropdown16.Font = new Font("Microsoft YaHei UI", 9F);
                dropdown16.Radius = 3;
                tlp.Controls.Add(dropdown16, 3, 3);
            }

            pnlMaterialsCard.Controls.Add(tlp);
            pnlStationContent.Controls.Add(pnlMaterialsCard);
        }

        /// <summary>
        /// 卡片 2: 独立全宽输出路径卡片 (白底细框 · 垂直居中对齐 · 25px 高)
        /// </summary>
        private void BuildOutputPathCard()
        {
            pnlOutputPathCard = CreateWhiteCardPanel("pnlOutputPathCard", 25);
            pnlOutputPathCard.Padding = new Padding(6, 1, 4, 1);

            lblOutputPathTitle = new System.Windows.Forms.Label
            {
                Text = "输出:",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(92, 99, 112),
                Width = 36,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnBrowseOutputFolder = new ColoredFolderBrowseButton
            {
                Size = new Size(100, 22),
                Dock = DockStyle.Right,
                Margin = new Padding(0)
            };
            btnBrowseOutputFolder.Click += (s, e) => ToggleOutputFolderDropdown();

            outputPathTextBox = new HierarchicalPathDisplay
            {
                Name = "outputPathTextBox",
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 9F),
                BackColor = Color.White
            };
            outputPathTextBox.SetPath(GetOutputPathDisplayText(), null);
            folderTreeViewToolTip?.SetToolTip(outputPathTextBox, outputPathTextBox.Text);

            pnlOutputPathCard.Controls.Add(outputPathTextBox);
            pnlOutputPathCard.Controls.Add(btnBrowseOutputFolder);
            pnlOutputPathCard.Controls.Add(lblOutputPathTitle);
            pnlStationContent.Controls.Add(pnlOutputPathCard);
        }

        /// <summary>
        /// 卡片 3: 工艺开关矩阵卡片 (覆膜分段 + 模切形状分段 · 22px精巧高度 · 居中规范短语 · 55px 高)
        /// </summary>
        private void BuildCraftMatrixCard()
        {
            pnlCraftMatrixCard = CreateWhiteCardPanel("pnlCraftMatrixCard", 55);
            pnlCraftMatrixCard.Padding = new Padding(4, 4, 4, 4);

            // 1. 覆膜行 (22px 高，文字水平垂直居中)
            var pnlFilmRow = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.Transparent
            };
            var lblFilm = new System.Windows.Forms.Label
            {
                Text = "覆膜",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(92, 99, 112),
                Width = 32,
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var flpFilm = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            flpFilm.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 4; i++) flpFilm.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var filmBtns = new[] { filmTypeLightButton, filmTypeMatteButton, filmTypeRedButton, filmTypeNoneButton };
            var filmTexts = new[] { "光膜", "哑膜", "红膜", "不过膜" };
            for (int i = 0; i < filmBtns.Length; i++)
            {
                if (filmBtns[i] != null)
                {
                    filmBtns[i].Dock = DockStyle.Fill;
                    filmBtns[i].Margin = new Padding(1, 0, 1, 0);
                    filmBtns[i].Radius = 2;
                    filmBtns[i].Font = new Font("Microsoft YaHei UI", 9F);
                    filmBtns[i].Text = filmTexts[i];
                    filmBtns[i].BorderWidth = 1F;
                    flpFilm.Controls.Add(filmBtns[i], i, 0);
                }
            }
            pnlFilmRow.Controls.Add(flpFilm);
            pnlFilmRow.Controls.Add(lblFilm);

            // 2. 形状行 (22px 高，文案统一为2字规范短语：直角 / 圆形 / 异形 / 圆角)
            var pnlShapeRow = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                BackColor = Color.Transparent
            };
            var lblShape = new System.Windows.Forms.Label
            {
                Text = "形状",
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(92, 99, 112),
                Size = new Size(32, 22),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var flpShape = new System.Windows.Forms.TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0)
            };
            flpShape.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 4; i++) flpShape.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var shapeBtns = new[] { shapeRightAngleButton, shapeCircleButton, shapeSpecialButton };
            var shapeTexts = new[] { "直角", "圆形", "异形" };
            for (int i = 0; i < shapeBtns.Length; i++)
            {
                if (shapeBtns[i] != null)
                {
                    shapeBtns[i].Dock = DockStyle.Fill;
                    shapeBtns[i].Margin = new Padding(1, 0, 1, 0);
                    shapeBtns[i].Radius = 2;
                    shapeBtns[i].Font = new Font("Microsoft YaHei UI", 9F);
                    shapeBtns[i].Text = shapeTexts[i];
                    shapeBtns[i].BorderWidth = 1F;
                    flpShape.Controls.Add(shapeBtns[i], i, 0);
                }
            }

            // 第 4 列：圆角槽位 (未激活时呈现“圆角”按钮；激活后原地无缝展开微型输入框 R [ 3 ] mm)
            pnlRoundRectSlot = new System.Windows.Forms.Panel
            {
                Name = "pnlRoundRectSlot",
                Dock = DockStyle.Fill,
                Margin = new Padding(1, 0, 1, 0),
                BackColor = Color.Transparent
            };

            if (shapeRoundRectButton != null)
            {
                shapeRoundRectButton.Dock = DockStyle.Fill;
                shapeRoundRectButton.Margin = new Padding(0);
                shapeRoundRectButton.Radius = 2;
                shapeRoundRectButton.Font = new Font("Microsoft YaHei UI", 9F);
                shapeRoundRectButton.Text = "圆角";
                shapeRoundRectButton.BorderWidth = 1F;
                pnlRoundRectSlot.Controls.Add(shapeRoundRectButton);
            }

            BuildInplaceRoundRadiusComponent();
            if (pnlInplaceRoundInput != null)
            {
                pnlRoundRectSlot.Controls.Add(pnlInplaceRoundInput);
            }

            flpShape.Controls.Add(pnlRoundRectSlot, 3, 0);
            pnlShapeRow.Controls.Add(flpShape);
            pnlShapeRow.Controls.Add(lblShape);

            pnlCraftMatrixCard.Controls.Add(pnlShapeRow);
            pnlCraftMatrixCard.Controls.Add(pnlFilmRow);
            pnlStationContent.Controls.Add(pnlCraftMatrixCard);
        }

        /// <summary>
        /// 卡片 4: 2×2 工单参数舱卡片（两行共用 175/151px 分栏，订单模式完整显示四字选项）
        /// </summary>
        private void BuildOrderParametersCard()
        {
            tlpOrderParametersCard = CreateWhiteCardPanel("tlpOrderParametersCard", 58);
            tlpOrderParametersCard.Padding = new Padding(4, 3, 4, 3);
            tlpOrderParametersCard.Margin = new Padding(0, 0, 0, 4);

            // ----------------------------------------------------
            // 两行共用 175/151px 分栏，给订单模式留出空间，同时保持四格分界线对齐。
            // ----------------------------------------------------
            var pnlRow1 = new System.Windows.Forms.Panel
            {
                Location = new Point(4, 3),
                Size = new Size(332, 24),
                BackColor = Color.Transparent
            };

            // (1) 订单组合框 (InputGroupBox: 左侧浅灰标签 [订单] + 订单号无缝输入 + 模式无缝下拉)
            var pnlBoxOrder = new InputGroupBox("订单")
            {
                Location = new Point(0, 0),
                Size = new Size(175, 24)
            };

            if (orderNumberTextBox != null)
            {
                orderNumberTextBox.Location = new Point(36, 1);
                orderNumberTextBox.Size = new Size(67, 22);
                orderNumberTextBox.Font = new Font("Microsoft YaHei UI", 9F);
                orderNumberTextBox.BorderWidth = 0F;
                orderNumberTextBox.WaveSize = 0;
                orderNumberTextBox.PlaceholderText = "订单号";
                folderTreeViewToolTip?.SetToolTip(orderNumberTextBox, "订单编号/工程单号");
                pnlBoxOrder.Controls.Add(orderNumberTextBox);
            }

            if (btnOrderNumberMode != null)
            {
                btnOrderNumberMode.Location = new Point(103, 1);
                btnOrderNumberMode.Size = new Size(70, 22);
                btnOrderNumberMode.Ghost = true;
                btnOrderNumberMode.BorderWidth = 0F;
                btnOrderNumberMode.WaveSize = 0;
                btnOrderNumberMode.Font = new Font("Microsoft YaHei UI", 8F);
                btnOrderNumberMode.ForeColor = Color.FromArgb(71, 85, 105);
                pnlBoxOrder.Controls.Add(btnOrderNumberMode);
            }
            pnlRow1.Controls.Add(pnlBoxOrder);

            // (2) 序号组合框 (InputGroupBox: 左侧浅灰标签 [序号] + 序号无缝输入 + 放大镜无缝后缀)
            var pnlBoxSerial = new InputGroupBox("序号")
            {
                Location = new Point(181, 0),
                Size = new Size(151, 24)
            };

            if (serialNumberTextBox != null)
            {
                serialNumberTextBox.Location = new Point(36, 1);
                serialNumberTextBox.Size = new Size(86, 22);
                serialNumberTextBox.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
                serialNumberTextBox.TextAlign = HorizontalAlignment.Left;
                serialNumberTextBox.BorderWidth = 0F;
                serialNumberTextBox.WaveSize = 0;
                serialNumberTextBox.PlaceholderText = "序号";
                folderTreeViewToolTip?.SetToolTip(serialNumberTextBox, "图档序号/行序号，按回车或点击右侧放大镜可反查");
                serialNumberTextBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        PerformSerialLookup(true);
                    }
                };
                pnlBoxSerial.Controls.Add(serialNumberTextBox);
            }

            btnSearchSerialAction = new SerialSearchButton
            {
                Location = new Point(124, 1),
                Size = new Size(25, 22),
                IsSearchActive = AppSettings.MaterialEnableSerialSearchCheckbox
            };
            btnSearchSerialAction.Click += (s, e) =>
            {
                bool newState = !btnSearchSerialAction.IsSearchActive;
                btnSearchSerialAction.IsSearchActive = newState;
                AppSettings.MaterialEnableSerialSearchCheckbox = newState;
                if (enableSerialSearchCheckbox != null)
                {
                    enableSerialSearchCheckbox.Checked = newState;
                }
                if (newState && serialNumberTextBox != null && !string.IsNullOrWhiteSpace(serialNumberTextBox.Text))
                {
                    PerformSerialLookup(true);
                }
            };
            folderTreeViewToolTip?.SetToolTip(btnSearchSerialAction, "序号反查开关（启用后输入序号自动反查表格并更新）");
            pnlBoxSerial.Controls.Add(btnSearchSerialAction);
            pnlRow1.Controls.Add(pnlBoxSerial);

            tlpOrderParametersCard.Controls.Add(pnlRow1);

            // ----------------------------------------------------
            // 第二行沿用 175/151px 分栏，视觉分界线与订单、序号完全一致。
            // ----------------------------------------------------
            var pnlRow2 = new System.Windows.Forms.Panel
            {
                Location = new Point(4, 30),
                Size = new Size(332, 24),
                BackColor = Color.Transparent
            };

            // (3) 数量组合框 (InputGroupBox: 左侧浅灰标签 [数量] + 数量无缝输入 + 增量步长)
            var pnlBoxQty = new InputGroupBox("数量")
            {
                Location = new Point(0, 0),
                Size = new Size(175, 24)
            };

            if (quantityTextBox != null)
            {
                quantityTextBox.Location = new Point(36, 1);
                quantityTextBox.Size = new Size(100, 22);
                quantityTextBox.Font = new Font("Microsoft YaHei UI", 9F);
                quantityTextBox.BorderWidth = 0F;
                quantityTextBox.WaveSize = 0;
                quantityTextBox.PlaceholderText = "数量";
                folderTreeViewToolTip?.SetToolTip(quantityTextBox, "印刷制作数量");
                pnlBoxQty.Controls.Add(quantityTextBox);
            }
            if (incrementTextBox != null)
            {
                incrementTextBox.Location = new Point(138, 1);
                incrementTextBox.Size = new Size(35, 22);
                incrementTextBox.Font = new Font("Microsoft YaHei UI", 8.5F);
                incrementTextBox.BorderWidth = 0F;
                incrementTextBox.WaveSize = 0;
                incrementTextBox.PlaceholderText = "+1";
                incrementTextBox.ForeColor = Color.FromArgb(148, 163, 184);
                folderTreeViewToolTip?.SetToolTip(incrementTextBox, "序号/数量自动递增步长（如 1）");
                pnlBoxQty.Controls.Add(incrementTextBox);
            }
            pnlRow2.Controls.Add(pnlBoxQty);

            // PDF 页序复制：左侧次数，右侧逐页/整套方式。
            var pnlBoxImpo = new InputGroupBox("页序")
            {
                Location = new Point(181, 0),
                Size = new Size(151, 24)
            };

            // 保持原底层复选框与文本框在后台同步，保证业务逻辑与事件 100% 畅通
            if (duplicateLayoutCheckbox != null)
            {
                duplicateLayoutCheckbox.Visible = false;
                duplicateLayoutCheckbox.CheckedChanged += (s, e) =>
                {
                    UpdateJoinGroupEnabledState(duplicateLayoutCheckbox.Checked);
                    UpdateCopyCountPillDisplay();
                };
                pnlBoxImpo.Controls.Add(duplicateLayoutCheckbox);
            }

            pnlCopyCountWrap = new System.Windows.Forms.Panel { Visible = false };
            txtCopyCountInput = new AntdUI.Input
            {
                Name = "txtCopyCountInput",
                Location = new Point(38, 1),
                Size = new Size(49, 22),
                Text = _copyCount.ToString(),
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                BorderWidth = 0F,
                WaveSize = 0,
                Visible = false
            };
            txtCopyCountInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    if (e.KeyCode == Keys.Enter) CommitCopyCountInput();
                    else CancelCopyCountEdit();
                }
            };
            txtCopyCountInput.Leave += (s, e) =>
            {
                if (_isEditingCopyCount) CommitCopyCountInput();
            };
            pnlBoxImpo.Controls.Add(txtCopyCountInput);
            segCopyType = new MiniSegmentedControl("联", "份") { Visible = false, SelectedIndex = (_copyType == CopyType.Duplicate) ? 1 : 0 };
            segCopyType.SelectedIndexChanged += (s, e) =>
            {
                if (duplicateLayoutCheckbox?.Checked != true) return;
                _copyType = (segCopyType.SelectedIndex == 1) ? CopyType.Duplicate : CopyType.Layout;
                if (copyTypeComboBox != null) copyTypeComboBox.SelectedIndex = (int)_copyType;
                UpdateCopyCountPillDisplay();
                OnImpositionSettingsChanged(s, e);
            };

            // 重复次数药丸。
            lblCopyCountPill = new System.Windows.Forms.Label
            {
                Location = new Point(38, 1),
                Size = new Size(49, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            UpdateCopyCountPillDisplay();

            lblCopyCountPill.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                if (duplicateLayoutCheckbox == null) return;
                if (!duplicateLayoutCheckbox.Checked)
                {
                    duplicateLayoutCheckbox.Checked = true;
                }
                BeginCopyCountEdit();
            };
            lblCopyCountPill.MouseUp += (s, e) =>
            {
                if (e.Button != MouseButtons.Right || duplicateLayoutCheckbox?.Checked != true) return;
                {
                    // 已启用时提供页序说明、重复次数和自动排版方向。
                    var cms = new System.Windows.Forms.ContextMenuStrip();
                    cms.Items.Add(_copyType == CopyType.Layout
                        ? "逐页重复：1122"
                        : "整套循环：123123").Enabled = false;
                    cms.Items.Add(new ToolStripSeparator());
                    var repeatCountMenu = new ToolStripMenuItem("重复次数");
                    for (int count = 2; count <= 16; count++)
                    {
                        int c = count;
                        var item = repeatCountMenu.DropDownItems.Add($"{c} 次", null, (s2, e2) =>
                        {
                            _copyCount = c;
                            if (copyCountNumericUpDown != null) copyCountNumericUpDown.Value = c;
                            if (txtCopyCountInput != null) txtCopyCountInput.Text = c.ToString();
                            UpdateCopyCountPillDisplay();
                            OnImpositionSettingsChanged(s2, e2);
                        });
                        if (_copyCount == c) item.Font = new Font(item.Font, FontStyle.Bold);
                    }
                    cms.Items.Add(repeatCountMenu);

                    cms.Items.Add("关闭PDF页序复制", null, (s2, e2) =>
                    {
                        duplicateLayoutCheckbox.Checked = false;
                    });
                    cms.Show(lblCopyCountPill, new Point(0, lblCopyCountPill.Height + 2));
                }
            };
            folderTreeViewToolTip?.SetToolTip(lblCopyCountPill, "点击输入重复次数（2–16）；右键打开快捷选项；不改变制作数量");
            folderTreeViewToolTip?.SetToolTip(txtCopyCountInput, "输入2–16的整数；回车或离开时确认，Esc取消");
            pnlBoxImpo.Controls.Add(lblCopyCountPill);

            // 当前 PDF 页序方式。
            lblCopyModePill = new System.Windows.Forms.Label
            {
                Location = new Point(88, 1),
                Size = new Size(61, 22),
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                ForeColor = Color.FromArgb(71, 85, 105),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Text = GetPageOrderPillText(_copyType)
            };

            var cmsPageOrder = new System.Windows.Forms.ContextMenuStrip();
            cmsPageOrder.Items.Add("逐页重复（1122）", null, (s, e) =>
            {
                SelectPageRepeatType(CopyType.Layout);
            });
            cmsPageOrder.Items.Add("整套循环（123123）", null, (s, e) =>
            {
                SelectPageRepeatType(CopyType.Duplicate);
            });
            cmsPageOrder.Items.Add(new ToolStripSeparator());
            cmsPageOrder.Items.Add("关闭PDF页序复制", null, (s, e) => duplicateLayoutCheckbox.Checked = false);

            lblCopyModePill.Click += (s, e) =>
            {
                if (duplicateLayoutCheckbox?.Checked != true) return;
                cmsPageOrder.Show(lblCopyModePill, new Point(lblCopyModePill.Width - cmsPageOrder.Width, lblCopyModePill.Height + 2));
            };
            folderTreeViewToolTip?.SetToolTip(lblCopyModePill, "选择PDF页序方式：逐页重复或整套循环");
            pnlBoxImpo.Controls.Add(lblCopyModePill);

            pnlRow2.Controls.Add(pnlBoxImpo);
            tlpOrderParametersCard.Controls.Add(pnlRow2);

            UpdateJoinGroupEnabledState(duplicateLayoutCheckbox?.Checked ?? false);

            pnlStationContent.Controls.Add(tlpOrderParametersCard);
        }

        private void BeginCopyCountEdit()
        {
            if (duplicateLayoutCheckbox?.Checked != true || txtCopyCountInput == null) return;
            txtCopyCountInput.Text = _copyCount.ToString();
            _isEditingCopyCount = true;
            lblCopyCountPill.Visible = false;
            txtCopyCountInput.Visible = true;
            txtCopyCountInput.BringToFront();
            txtCopyCountInput.Focus();
            txtCopyCountInput.SelectAll();
        }

        private void CancelCopyCountEdit()
        {
            // 隐藏输入框会先触发 Leave；先结束编辑，避免 Esc 意外提交尚未确认的值。
            _isEditingCopyCount = false;
            txtCopyCountInput.Visible = false;
            lblCopyCountPill.Visible = true;
            UpdateCopyCountPillDisplay();
        }

        private void CommitCopyCountInput()
        {
            if (!_isEditingCopyCount) return;
            bool accepted = TryApplyPageRepeatCount(txtCopyCountInput.Text);
            CancelCopyCountEdit();
            if (!accepted)
            {
                AntdUI.Message.warn(this, "重复次数请输入2–16的整数，已保留原值", autoClose: 3);
            }
        }

        internal bool TryApplyPageRepeatCount(string text)
        {
            int minimum = (int)(copyCountNumericUpDown?.Minimum ?? 2M);
            int maximum = (int)(copyCountNumericUpDown?.Maximum ?? 16M);
            if (!int.TryParse(text, out int count) || count < minimum || count > maximum) return false;

            _copyCount = count;
            if (copyCountNumericUpDown != null) copyCountNumericUpDown.Value = count;
            UpdateCopyCountPillDisplay();
            return true;
        }

        private void UpdateCopyCountPillDisplay()
        {
            if (lblCopyCountPill == null) return;
            if (_isDuplicateLayoutEnabled || duplicateLayoutCheckbox?.Checked == true)
            {
                lblCopyCountPill.Text = $"{_copyCount}次";
                lblCopyCountPill.ForeColor = Color.FromArgb(22, 119, 255);
                lblCopyCountPill.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            }
            else
            {
                _isEditingCopyCount = false;
                if (txtCopyCountInput != null) txtCopyCountInput.Visible = false;
                lblCopyCountPill.Visible = true;
                lblCopyCountPill.Text = "未启用";
                lblCopyCountPill.ForeColor = Color.FromArgb(148, 163, 184);
                lblCopyCountPill.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular);
            }
            if (txtCopyCountInput != null && !_isEditingCopyCount) txtCopyCountInput.Text = _copyCount.ToString();
            if (lblCopyModePill != null) lblCopyModePill.Text = GetPageOrderPillText(_copyType);
        }

        /// <summary>
        /// 卡片 5: 纯净 6 宫格排版看板卡片 (图标开关 + 等宽指标列 · 82px 高)
        /// </summary>
        private void BuildImpositionDashboardCard()
        {
            pnlImpositionDashboardCard = CreateWhiteCardPanel("pnlImpositionDashboardCard", 82);
            pnlImpositionDashboardCard.Padding = new Padding(4, 4, 4, 4);

            // 顶部排版模式控制行
            var pnlImpoControlRow = new System.Windows.Forms.Panel
            {
                Location = new Point(4, 4),
                Size = new Size(332, 26),
                BackColor = Color.Transparent
            };
            if (enableImpositionCheckbox != null)
            {
                enableImpositionCheckbox.Visible = false;
                btnImpositionToggle = new ImpositionToggleButton
                {
                    Location = new Point(0, 1),
                    Size = new Size(28, 24),
                    IsActive = enableImpositionCheckbox.Checked
                };
                btnImpositionToggle.Click += (s, e) => enableImpositionCheckbox.Checked = !enableImpositionCheckbox.Checked;
                enableImpositionCheckbox.CheckedChanged += (s, e) =>
                {
                    if (btnImpositionToggle != null)
                    {
                        btnImpositionToggle.IsActive = enableImpositionCheckbox.Checked;
                    }
                    UpdateCustomImpositionCapsule();
                };
                folderTreeViewToolTip?.SetToolTip(btnImpositionToggle, "启用或停用自动排版计算");
                pnlImpoControlRow.Controls.Add(btnImpositionToggle);
            }

            // 专属/自定义大版尺寸微型指示胶囊 (仅当与系统全局设置不一致时浮现，点击直达大版参数微调)
            _customSizeCapsule = new CustomSizeCapsule
            {
                Name = "_customSizeCapsule",
                Location = new Point(31, 1),
                Size = new Size(74, 24),
                Visible = false
            };
            _customSizeCapsule.Click += (s, e) => OpenTemporaryImpositionParameters();
            pnlImpoControlRow.Controls.Add(_customSizeCapsule);

            // 1. 材料模式微型分段胶囊 (平张 / 卷装) - 靠右排列，与连拼/折手保持一致间距 6px
            segMaterialType = new MiniSegmentedControl("平张", "卷装")
            {
                Location = new Point(150, 1),
                Size = new Size(72, 24),
                SelectedIndex = (flatSheetRadioButton?.Checked ?? true) ? 0 : 1
            };
            segMaterialType.SelectedIndexChanged += (s, e) =>
            {
                if (flatSheetRadioButton != null && rollMaterialRadioButton != null)
                {
                    if (segMaterialType.SelectedIndex == 0) flatSheetRadioButton.Checked = true;
                    else rollMaterialRadioButton.Checked = true;
                }
                UpdateCustomImpositionCapsule();
            };
            pnlImpoControlRow.Controls.Add(segMaterialType);

            // 2. 排版模式微型分段胶囊 (连拼 / 折手) - 靠右排列，与两端间距严格一致 (6px)
            segLayoutMode = new MiniSegmentedControl("连拼", "折手")
            {
                Location = new Point(228, 1),
                Size = new Size(72, 24),
                SelectedIndex = (continuousLayoutRadioButton?.Checked ?? true) ? 0 : 1
            };
            segLayoutMode.SelectedIndexChanged += (s, e) =>
            {
                if (continuousLayoutRadioButton != null && foldingLayoutRadioButton != null)
                {
                    if (segLayoutMode.SelectedIndex == 0) continuousLayoutRadioButton.Checked = true;
                    else foldingLayoutRadioButton.Checked = true;
                }
                UpdateCustomImpositionCapsule();
            };
            pnlImpoControlRow.Controls.Add(segLayoutMode);

            if (chkIdentifierPage != null)
            {
                chkIdentifierPage.Visible = false;
                btnIdentifierPageToggle = new IdentifierPageToggleButton
                {
                    Name = "btnIdentifierPageToggle",
                    Location = new Point(306, 1),
                    Size = new Size(26, 24),
                    IsActive = chkIdentifierPage.Checked,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right
                };
                btnIdentifierPageToggle.Click += (s, e) => chkIdentifierPage.Checked = !chkIdentifierPage.Checked;
                chkIdentifierPage.CheckedChanged += (s, e) =>
                {
                    if (btnIdentifierPageToggle != null)
                    {
                        btnIdentifierPageToggle.IsActive = chkIdentifierPage.Checked;
                    }
                    UpdateIdentifierPageTooltip();
                };
                UpdateIdentifierPageTooltip();
                pnlImpoControlRow.Controls.Add(chkIdentifierPage);
                pnlImpoControlRow.Controls.Add(btnIdentifierPageToggle);
            }
            pnlImpositionDashboardCard.Controls.Add(pnlImpoControlRow);

            // 6 宫格看板容器：六列严格等宽，标题与数值使用统一基线和间距。
            tlpMetrics = new System.Windows.Forms.TableLayoutPanel
            {
                Name = "tlpMetrics",
                Location = new Point(4, 34),
                Size = new Size(332, 44),
                ColumnCount = 6,
                RowCount = 1,
                BackColor = Color.White,
                Margin = new Padding(0),
                // 六宫格必须无间隙铺满整块面板：一旦留出内边距或列间距，父容器自绘的
                // 彩环与药丸之间就会露出一圈纯白，黑白模式下会变成明显的白色描边。
                Padding = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            tlpMetrics.Paint += PaintV5MetricsInsetGlow;
            this.Disposed += (s, e) =>
            {
                _v5MetricsGlowBitmap?.Dispose();
                _v5MetricsGlowBitmap = null;
            };
            tlpMetrics.RowStyles.Clear();
            tlpMetrics.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 6; i++)
            {
                tlpMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 6F));
            }

            // 1. 网格 (只读)
            pnlPillGrid = CreateMetricPill("网格", () => _v5GridText, () => Color.FromArgb(22, 119, 255), null, null);
            tlpMetrics.Controls.Add(pnlPillGrid, 0, 0);

            // 2. 布局数 (只读)
            pnlPillCount = CreateMetricPill("布局数", () => _v5CountText, () => Color.FromArgb(250, 140, 22), null, null);
            tlpMetrics.Controls.Add(pnlPillCount, 1, 0);

            // 3. 角度 (标题与数值均可点击切换)
            pnlPillRotation = CreateMetricPill("角度", () => _v5RotationText, () => Color.FromArgb(114, 46, 209),
                (s, e) => RotationDisplayLabel_Click(s, e),
                (s, e) => RotationDisplayLabel_Click(s, e));
            tlpMetrics.Controls.Add(pnlPillRotation, 2, 0);

            // 4. 尺寸 (纯白无框，PDF解析自动获得，只读不可修改，光标default)
            pnlPillDimension = CreateMetricPill("尺寸", () => _v5DimensionText, () => Color.FromArgb(9, 88, 217), null, null);
            tlpMetrics.Controls.Add(pnlPillDimension, 3, 0);

            // 5. 出血：点击标题显示下拉选项，点击数值直接循环切换。
            pnlPillBleed = CreateMetricPill("出血", () => _v5BleedText, () => Color.FromArgb(235, 47, 150),
                (s, e) => ShowBleedOptionsMenu(),
                (s, e) => CycleBleedPreset());
            tlpMetrics.Controls.Add(pnlPillBleed, 4, 0);

            // 6. 颜色模式：标题与数值均可点击，并立即刷新 V5 显示状态。
            pnlPillColor = CreateMetricPill("颜色", () => _v5ColorText, () => _v5ColorTextColor,
                (s, e) => ToggleV5ColorMode(),
                (s, e) => ToggleV5ColorMode());
            tlpMetrics.Controls.Add(pnlPillColor, 5, 0);

            pnlImpositionDashboardCard.Controls.Add(tlpMetrics);
            pnlStationContent.Controls.Add(pnlImpositionDashboardCard);
        }

        /// <summary>
        /// 卡片 6: 快捷预设方案卡片 (纯白底无灰底 · Tabs.StyleLine + 22px 微圆角白底预设按钮 · 自适应多行高度)
        /// </summary>
        private void BuildPresetBrowserCard()
        {
            presetBrowserCard = CreateWhiteCardPanel("presetBrowserCard", 54);
            presetBrowserCard.BackColor = Color.White;
            presetBrowserCard.Padding = new Padding(4, 2, 4, 2);

            // 上行导航行容器：左侧分组标签页，右侧常驻“管理”入口 (22px 高)
            var pnlPresetNavRow = new System.Windows.Forms.Panel
            {
                Name = "pnlPresetNavRow",
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.White
            };

            var btnManagePresets = new AntdUI.Button
            {
                Name = "btnManagePresets",
                Text = "",
                IconSvg = "SettingOutlined",
                Dock = DockStyle.Right,
                Width = 24,
                Height = 20,
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                BorderWidth = 0F,
                DefaultBack = Color.Transparent,
                DefaultBorderColor = Color.Transparent,
                ForeColor = Color.FromArgb(140, 149, 163),
                Radius = 4,
                Cursor = Cursors.Hand,
                TabStop = false,
                WaveSize = 0,
                Margin = new Padding(0)
            };
            btnManagePresets.MouseEnter += (s, e) => btnManagePresets.ForeColor = Color.FromArgb(22, 119, 255);
            btnManagePresets.MouseLeave += (s, e) =>
            {
                btnManagePresets.ForeColor = Color.FromArgb(140, 149, 163);
                if (this.ActiveControl == btnManagePresets) this.ActiveControl = null;
            };
            btnManagePresets.MouseUp += (s, e) =>
            {
                if (this.IsDisposed) return;
                this.ActiveControl = null;
            };
            btnManagePresets.Click += (s, e) =>
            {
                OpenPresetManagement();
                this.BeginInvoke(new Action(() =>
                {
                    if (this.IsDisposed) return;
                    this.ActiveControl = null;
                    SetOrderTextBoxFocus();
                }));
            };
            folderTreeViewToolTip?.SetToolTip(btnManagePresets, "打开预设管理工作台：分组归类、批量移动与属性配置");

            var btnSaveCurrentPreset = new AntdUI.Button
            {
                Name = "btnSaveCurrentPreset",
                Text = "",
                IconSvg = "PlusOutlined",
                Dock = DockStyle.Right,
                Width = 24,
                Height = 20,
                Type = AntdUI.TTypeMini.Default,
                Ghost = true,
                BorderWidth = 0F,
                DefaultBack = Color.Transparent,
                DefaultBorderColor = Color.Transparent,
                ForeColor = Color.FromArgb(140, 149, 163),
                Radius = 4,
                Cursor = Cursors.Hand,
                TabStop = false,
                WaveSize = 0,
                Margin = new Padding(0)
            };
            btnSaveCurrentPreset.MouseEnter += (s, e) => btnSaveCurrentPreset.ForeColor = Color.FromArgb(22, 119, 255);
            btnSaveCurrentPreset.MouseLeave += (s, e) =>
            {
                btnSaveCurrentPreset.ForeColor = Color.FromArgb(140, 149, 163);
                if (this.ActiveControl == btnSaveCurrentPreset) this.ActiveControl = null;
            };
            btnSaveCurrentPreset.MouseUp += (s, e) =>
            {
                if (this.IsDisposed) return;
                this.ActiveControl = null;
            };
            btnSaveCurrentPreset.Click += (s, e) =>
            {
                this.ActiveControl = null;
                SaveCurrentAsPreset();
                this.BeginInvoke(new Action(() =>
                {
                    if (this.IsDisposed) return;
                    this.ActiveControl = null;
                    SetOrderTextBoxFocus();
                }));
            };
            folderTreeViewToolTip?.SetToolTip(btnSaveCurrentPreset, "保存当前配置为新预设");

            // 注意：WinForms 控件 DockStyle.Right 的布局规则是先添加的靠右，后添加的靠其左侧。
            // 保持 [➕ 存为预设] 在左，[⚙️ 管理] 在最右侧的标准视觉习惯。
            pnlPresetNavRow.Controls.Add(btnSaveCurrentPreset);
            pnlPresetNavRow.Controls.Add(btnManagePresets);

            if (tabs1 != null)
            {
                tabs1.Dock = DockStyle.Fill;
                tabs1.Height = 22;
                tabs1.BackColor = Color.White;
                pnlPresetNavRow.Controls.Add(tabs1);
            }

            presetBrowserCard.Controls.Add(pnlPresetNavRow);

            // 下行预设按钮放置面板 (纯白底色，自动多行包裹)
            if (presetButtonsPanel != null)
            {
                presetButtonsPanel.Dock = DockStyle.Fill;
                presetButtonsPanel.Padding = new Padding(0);
                presetButtonsPanel.BackColor = Color.White;
                presetButtonsPanel.AutoScroll = false;
                presetBrowserCard.Controls.Add(presetButtonsPanel);
            }

            pnlStationContent.Controls.Add(presetBrowserCard);
        }

        /// <summary>
        /// 卡片 7: 底部沉底操作栏 (位于预设正下方、折叠条正上方 · 统一 24px 居中对齐 · 28px 高 · 无顶部分割线)
        /// </summary>
        private void BuildActionBar()
        {
            pnlActionBar = new System.Windows.Forms.Panel
            {
                Name = "pnlActionBar",
                Size = new Size(340, 28),
                BackColor = Color.White,
                Padding = new Padding(2, 2, 2, 2),
                Margin = new Padding(0, 0, 0, 0)
            };

            // 左侧：仅处理当前款 / 批量处理 幽灵按钮
            if (btnApplyToAll != null)
            {
                btnApplyToAll.Visible = false;
                btnBatchModeSwitch = new BatchModeSwitch
                {
                    Name = "btnBatchModeSwitch",
                    Dock = DockStyle.Left,
                    Size = new Size(104, 24),
                    IsChecked = _isBatchPanelExpanded
                };
                btnBatchModeSwitch.Click += BtnApplyToAll_Click;
                UpdateBatchScopeButtonAppearance();
                pnlActionBar.Controls.Add(btnBatchModeSwitch);
            }

            // 右侧紧凑操作组：[取消] + [确认并继续]
            var pnlRightActions = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Right,
                Width = 168,
                BackColor = Color.Transparent
            };

            if (confirmButton != null)
            {
                confirmButton.Dock = DockStyle.Right;
                confirmButton.Size = new Size(110, 24);
                confirmButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
                confirmButton.Radius = 3;
                confirmButton.Type = AntdUI.TTypeMini.Primary;
                confirmButton.Text = "确认并继续";
                pnlRightActions.Controls.Add(confirmButton);
            }

            if (cancelButton != null)
            {
                cancelButton.Dock = DockStyle.Left;
                cancelButton.Size = new Size(50, 24);
                cancelButton.Font = new Font("Microsoft YaHei UI", 9F);
                cancelButton.Radius = 3;
                cancelButton.Text = "取消";
                pnlRightActions.Controls.Add(cancelButton);
            }

            pnlActionBar.Controls.Add(pnlRightActions);
            pnlStationContent.Controls.Add(pnlActionBar);
        }

        private void UpdateBatchScopeButtonAppearance()
        {
            if (btnBatchModeSwitch != null)
            {
                btnBatchModeSwitch.IsChecked = _isBatchPanelExpanded;
            }
        }

        // ==========================================
        // 模切形状：圆角 R 半径原地微型输入组件 (100% 还原 HTML 原型 .round-inplace-wrap)
        // ==========================================

        private void BuildInplaceRoundRadiusComponent()
        {
            pnlInplaceRoundInput = new System.Windows.Forms.Panel
            {
                Name = "pnlInplaceRoundInput",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Color.FromArgb(230, 244, 255),
                Visible = false,
                Cursor = Cursors.Default
            };

            pnlInplaceRoundInput.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnlInplaceRoundInput.Width - 1, pnlInplaceRoundInput.Height - 1), 2))
                using (var brush = new SolidBrush(Color.FromArgb(230, 244, 255)))
                using (var pen = new Pen(Color.FromArgb(22, 119, 255), 1F))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            pnlInplaceRoundInput.Click += (s, e) => SelectShape(ShapeType.RoundRect);

            lblRoundPrefix = new System.Windows.Forms.Label
            {
                Name = "lblRoundPrefix",
                Text = "R",
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(9, 88, 217),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Cursor = Cursors.Hand
            };
            lblRoundPrefix.Click += (s, e) => SelectShape(ShapeType.RoundRect);

            pnlRadiusInputWrap = new System.Windows.Forms.Panel
            {
                Name = "pnlRadiusInputWrap",
                BackColor = Color.White
            };
            pnlRadiusInputWrap.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnlRadiusInputWrap.Width - 1, pnlRadiusInputWrap.Height - 1), 2))
                using (var pen = new Pen(Color.FromArgb(145, 202, 255), 1F))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            double initRadius = RoundRadius > 0
                ? RoundRadius
                : (double.TryParse(AppSettings.GetValue<string>("LastRoundRadius"), out double r) ? r : 3);

            txtInplaceRoundRadius = new System.Windows.Forms.TextBox
            {
                Name = "txtInplaceRoundRadius",
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(9, 88, 217),
                Font = new Font("Consolas", 9F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                MaxLength = 5,
                Text = initRadius.ToString("0.##")
            };

            txtInplaceRoundRadius.TextChanged += (s, e) =>
            {
                if (double.TryParse(txtInplaceRoundRadius.Text, out double val) && val >= 0)
                {
                    if (Math.Abs(RoundRadius - val) > 0.001)
                    {
                        RoundRadius = val;
                        AppSettings.Set("LastCornerRadius", GetCompatibleCornerRadius());
                        AppSettings.Set("LastRoundRadius", RoundRadius.ToString());
                        AppSettings.Save();
                        if (radiusTextBox != null && radiusTextBox.Text != txtInplaceRoundRadius.Text)
                        {
                            radiusTextBox.Text = txtInplaceRoundRadius.Text;
                        }
                        UpdateDimensionsWithBleed();
                        SyncCurrentSelectionsToActiveGroup();
                    }
                }
            };

            txtInplaceRoundRadius.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    CommitInplaceRadiusInput();
                    this.ActiveControl = null;
                }
            };

            txtInplaceRoundRadius.LostFocus += (s, e) => CommitInplaceRadiusInput();

            pnlRadiusInputWrap.Controls.Add(txtInplaceRoundRadius);

            lblRoundSuffix = new System.Windows.Forms.Label
            {
                Name = "lblRoundSuffix",
                Text = "mm",
                Font = new Font("Microsoft YaHei UI", 7.5F),
                ForeColor = Color.FromArgb(92, 99, 112),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                Cursor = Cursors.Hand
            };
            lblRoundSuffix.Click += (s, e) =>
            {
                txtInplaceRoundRadius?.Focus();
                txtInplaceRoundRadius?.SelectAll();
            };

            pnlInplaceRoundInput.Controls.Add(lblRoundPrefix);
            pnlInplaceRoundInput.Controls.Add(pnlRadiusInputWrap);
            pnlInplaceRoundInput.Controls.Add(lblRoundSuffix);

            pnlInplaceRoundInput.Resize += (s, e) => LayoutInplaceRoundInputControls();
            LayoutInplaceRoundInputControls();
        }

        private void CommitInplaceRadiusInput()
        {
            if (txtInplaceRoundRadius == null) return;
            if (!double.TryParse(txtInplaceRoundRadius.Text, out double r) || r < 0)
            {
                double fallback = RoundRadius > 0 ? RoundRadius : (double.TryParse(AppSettings.GetValue<string>("LastRoundRadius"), out double dr) ? dr : 3);
                txtInplaceRoundRadius.Text = fallback.ToString("0.##");
            }
        }

        private void LayoutInplaceRoundInputControls()
        {
            if (pnlInplaceRoundInput == null || lblRoundPrefix == null || pnlRadiusInputWrap == null || lblRoundSuffix == null) return;
            int totalW = pnlInplaceRoundInput.ClientSize.Width;
            int totalH = pnlInplaceRoundInput.ClientSize.Height;
            if (totalW <= 0 || totalH <= 0) return;

            int prefixW = 12;
            int suffixW = 26;
            int inputW = Math.Max(20, Math.Min(24, totalW - prefixW - suffixW - 5));
            int inputH = Math.Min(16, totalH - 4);
            int inputTop = (totalH - inputH) / 2;

            lblRoundPrefix.SetBounds(1, 0, prefixW, totalH);
            pnlRadiusInputWrap.SetBounds(prefixW + 2, inputTop, inputW, inputH);
            if (txtInplaceRoundRadius != null)
            {
                txtInplaceRoundRadius.SetBounds(1, 0, Math.Max(10, inputW - 2), Math.Max(10, inputH));
            }
            lblRoundSuffix.SetBounds(prefixW + 2 + inputW + 2, 0, suffixW, totalH);
        }

        public void UpdateV5InplaceRoundRadiusUI(bool isRoundRectSelected)
        {
            if (IsDisposed || Disposing || pnlInplaceRoundInput == null || shapeRoundRectButton == null) return;

            if (isRoundRectSelected)
            {
                shapeRoundRectButton.Visible = false;
                pnlInplaceRoundInput.Visible = true;
                pnlInplaceRoundInput.BringToFront();

                double currentR = RoundRadius > 0
                    ? RoundRadius
                    : (double.TryParse(AppSettings.GetValue<string>("LastRoundRadius"), out double r) ? r : 3);
                string rText = currentR.ToString("0.##");
                if (txtInplaceRoundRadius != null && txtInplaceRoundRadius.Text != rText)
                {
                    txtInplaceRoundRadius.Text = rText;
                }

                this.BeginInvoke(new Action(() =>
                {
                    if (txtInplaceRoundRadius != null && !txtInplaceRoundRadius.IsDisposed && txtInplaceRoundRadius.Visible && txtInplaceRoundRadius.CanFocus)
                    {
                        txtInplaceRoundRadius.Focus();
                        txtInplaceRoundRadius.SelectAll();
                    }
                }));
            }
            else
            {
                pnlInplaceRoundInput.Visible = false;
                shapeRoundRectButton.Visible = true;
                shapeRoundRectButton.BringToFront();
            }
        }

        // ==========================================
        // 辅助组件生成方法
        // ==========================================

        private static System.Windows.Forms.Panel CreateWhiteCardPanel(string name, int height)
        {
            var pnl = new System.Windows.Forms.Panel
            {
                Name = name,
                Size = new Size(340, height),
                BackColor = CardBgColor,
                Margin = new Padding(0, 0, 0, 4)
            };
            pnl.Paint += PaintCardBorder;
            return pnl;
        }

        public static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void PaintCardBorder(object sender, PaintEventArgs e)
        {
            if (sender is Control ctrl)
            {
                using (var pen = new Pen(CardBorderColor, 1F))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
                }
            }
        }

        /// <summary>
        /// 排参框药丸专用面板：开启双缓冲与整帧绘制。
        /// 药丸要「刷底 → 贴彩环切片 → 画文字」三步，不双缓冲时白底会先被看见，
        /// 切换颜色模式时表现为闪烁。
        /// </summary>
        private sealed class V5MetricPillPanel : System.Windows.Forms.Panel
        {
            public V5MetricPillPanel()
            {
                SetStyle(
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint,
                    true);
                UpdateStyles();
            }
        }

        /// <summary>
        /// 创建 6 宫格纯净指标药丸 (GDI+ OwnerDraw 自绘 · 标题与大字绝对水平垂直居中)
        /// 药丸改为不透明并自绘所属彩环切片，六格之间不再留间隙。
        /// </summary>
        private System.Windows.Forms.Panel CreateMetricPill(
            string label,
            Func<string> valueProvider,
            Func<Color> valColorProvider,
            EventHandler titleClickHandler,
            EventHandler valueClickHandler)
        {
            bool clickable = titleClickHandler != null || valueClickHandler != null;
            var pnl = new V5MetricPillPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Cursor = clickable ? Cursors.Hand : Cursors.Default,
                Margin = new Padding(0)
            };

            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

                // 每格自绘自己那一片彩环底：此前依赖「父容器自绘 + 透明药丸」透出彩环，
                // 而父容器自身的内边距与列间隙不会走子控件的透明重绘路径，
                // 于是留下一条条纯白竖线（黑白模式下彩环内层压暗后尤其明显）。
                PaintV5MetricsGlowSlice(g, pnl);

                const int rowGap = 7;
                var centeredTextFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix |
                    TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;

                string val = valueProvider?.Invoke() ?? "—";
                const string valueFontFamily = "Microsoft YaHei UI";

                using (var fontTop = new Font("Microsoft YaHei UI", 7.5F))
                using (var fontVal = new Font(valueFontFamily, 9.5F, FontStyle.Bold))
                using (var dimensionFont = label == "尺寸"
                    ? new Font(valueFontFamily, GetDimensionFontSize(g, val, pnl.ClientSize.Width - 4), FontStyle.Bold)
                    : null)
                {
                    Font valueFont = dimensionFont ?? fontVal;
                    Size titleSize = TextRenderer.MeasureText(g, label, fontTop, Size.Empty, centeredTextFlags);
                    Size valueSize = TextRenderer.MeasureText(g, val, valueFont, Size.Empty, centeredTextFlags);
                    int valueRowHeight = valueSize.Height;
                    int contentHeight = titleSize.Height + rowGap + valueRowHeight;
                    int remainingHeight = Math.Max(0, pnl.ClientSize.Height - contentHeight);
                    // 保持垂直间距 7px 下的像素级绝对居中：
                    // GDI 字体度量包含顶部 internal leading，直接取中会导致实际墨迹顶部留白偏大 4px（上 11px / 下 7px 造成下沉感）；
                    // 施加 -2px 墨迹对称修正，使标题小字与粗体数值在 rowGap=7 时上下墨迹留白达到严格的 9px:9px 完美对称。
                    int contentTop = Math.Max(1, ((remainingHeight + 1) / 2) - 2);

                    TextRenderer.DrawText(g, label, fontTop,
                        new Rectangle(0, contentTop, pnl.Width, titleSize.Height),
                        Color.FromArgb(92, 99, 112), centeredTextFlags);

                    var valueBounds = new RectangleF(
                        0,
                        contentTop + titleSize.Height + rowGap,
                        pnl.Width,
                        valueRowHeight);
                    Color valueColor = valColorProvider?.Invoke() ?? Color.FromArgb(31, 36, 41);
                    if (label == "颜色")
                    {
                        DrawColorModeValue(g, valueBounds, val, fontVal, valueColor);
                    }
                    else if (label == "尺寸")
                    {
                        DrawDimensionPillValue(g, pnl, valueBounds, valueSize, contentTop, titleSize, val, valueFont, valueColor, centeredTextFlags);
                    }
                    else if (label == "角度")
                    {
                        TextRenderer.DrawText(g, val, fontVal, Rectangle.Round(valueBounds), valueColor, centeredTextFlags);
                        DrawRotationPolicyBadge(g, pnl.Width, (int)valueBounds.Y);
                    }
                    else
                    {
                        TextRenderer.DrawText(g, val, fontVal,
                            Rectangle.Round(valueBounds), valueColor, centeredTextFlags);
                    }
                }
            };

            if (clickable)
            {
                pnl.MouseClick += (s, e) =>
                {
                    if (e.Y < pnl.Height / 2)
                    {
                        titleClickHandler?.Invoke(s, e);
                    }
                    else
                    {
                        valueClickHandler?.Invoke(s, e);
                    }
                };
            }

            return pnl;
        }

        private static void DrawColorModeValue(
            Graphics graphics,
            RectangleF bounds,
            string value,
            Font font,
            Color textColor)
        {
            var textFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix |
                TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;
            TextRenderer.DrawText(graphics, value, font, Rectangle.Round(bounds), textColor, textFlags);
        }

        private void DrawRotationPolicyBadge(Graphics graphics, int width, int valueTop)
        {
            var mode = GetCurrentRotationMode();
            string text = mode == RollRotationMode.Auto ? "自动" : mode == RollRotationMode.Force0Degree ? "禁转" : "固定";
            using (var font = new Font("Microsoft YaHei UI", 6F))
            {
                int badgeWidth = TextRenderer.MeasureText(graphics, text, font, Size.Empty, TextFormatFlags.NoPadding).Width + 4;
                var bounds = new Rectangle(width - badgeWidth - 1, valueTop - 12, badgeWidth, 10);
                bool locked = mode == RollRotationMode.Force0Degree;
                using (var path = CreateRoundedRectanglePath(bounds, 3))
                using (var fill = new SolidBrush(locked ? Color.FromArgb(255, 247, 230) : Color.FromArgb(249, 240, 255)))
                using (var border = new Pen(locked ? Color.FromArgb(255, 213, 145) : Color.FromArgb(211, 173, 247)))
                {
                    graphics.FillPath(fill, path);
                    graphics.DrawPath(border, path);
                    TextRenderer.DrawText(graphics, text, font, bounds,
                        locked ? Color.FromArgb(173, 104, 0) : Color.FromArgb(114, 46, 209),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoClipping);
                }
            }
        }

        /// <summary>
        /// 尺寸数字超过指标列宽时逐级缩小字体；极长数字在最小字号下由绘制阶段省略显示。
        /// </summary>
        internal static float GetDimensionFontSize(Graphics graphics, string value, int availableWidth)
        {
            const float defaultSize = 9.5F;
            const float minimumSize = 5.5F;
            const float step = 0.5F;
            var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

            for (float size = defaultSize; size >= minimumSize; size -= step)
            {
                using (var font = new Font("Microsoft YaHei UI", size, FontStyle.Bold))
                {
                    if (TextRenderer.MeasureText(graphics, value, font, Size.Empty, flags).Width <= availableWidth)
                    {
                        return size;
                    }
                }
            }

            return minimumSize;
        }

        /// <summary>
        /// 最小字号仍无法放下时才省略末尾，避免普通尺寸受到裁剪标志影响。
        /// </summary>
        internal static string GetDimensionDisplayText(Graphics graphics, string value, Font font, int availableWidth)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;

            var flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            if (TextRenderer.MeasureText(graphics, value, font, Size.Empty, flags).Width <= availableWidth)
            {
                return value;
            }

            const string ellipsis = "…";
            for (int length = value.Length - 1; length >= 0; length--)
            {
                string candidate = value.Substring(0, length) + ellipsis;
                if (TextRenderer.MeasureText(graphics, candidate, font, Size.Empty, flags).Width <= availableWidth)
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// 绘制尺寸指标项：主数值（如 80×45）严格按自身宽度绝对居中绘制；
        /// 形状代号（如 R8/Z/C/Y）以微型胶囊形式精准附着在 80×45 右上肩，绝对不影响主数值的居中计算，
        /// 且严格上浮于主数值顶边缘之上，绝不压迫或遮挡主数值任何笔画。
        /// </summary>
        private void DrawDimensionPillValue(
            Graphics g,
            Control pnl,
            RectangleF valueBounds,
            Size valueSize,
            int contentTop,
            Size titleSize,
            string val,
            Font fontVal,
            Color valueColor,
            TextFormatFlags centeredTextFlags)
        {
            // 1. 主数值绝对严格居中绘制 (矩形为完整面板宽度，以纯净 80×45 自身计算绝对居中)
            string displayValue = GetDimensionDisplayText(g, val, fontVal, pnl.ClientSize.Width - 4);
            TextRenderer.DrawText(g, displayValue, fontVal, Rectangle.Round(valueBounds), valueColor, centeredTextFlags);

            // 2. 检查是否有形状代号需要以胶囊形式呈现在 80×45 右上角
            string shapeCode = GetCurrentShapeBadgeCode();
            if (!string.IsNullOrEmpty(shapeCode))
            {
                using (var fontBadge = new Font("Consolas", 6.5F, FontStyle.Bold))
                {
                    Size badgeTextSize = TextRenderer.MeasureText(g, shapeCode, fontBadge, Size.Empty, TextFormatFlags.NoPadding);
                    int badgeW = Math.Max(12, badgeTextSize.Width + 4);
                    int badgeH = 9;

                    int valTop = (int)valueBounds.Y;

                    // 右上角精准定位：
                    // X: 靠右侧对齐 (pnl.Width - badgeW - 1)
                    // Y: 严格浮在主数值顶边缘上方 (valTop - badgeH - 2)，完全在 7px 行间距的右上部空间，与 80×45 笔画零重叠
                    int badgeX = pnl.Width - badgeW - 1;
                    int badgeY = valTop - badgeH - 2;

                    Rectangle badgeRect = new Rectangle(badgeX, badgeY, badgeW, badgeH);
                    using (var badgePath = CreateRoundedRectanglePath(badgeRect, 3))
                    {
                        Color badgeBg;
                        Color badgeBorder;
                        Color badgeTextClr;

                        if (SelectedShape == ShapeType.Special)
                        {
                            badgeBg = Color.FromArgb(249, 240, 255);
                            badgeBorder = Color.FromArgb(211, 173, 247);
                            badgeTextClr = Color.FromArgb(114, 46, 209);
                        }
                        else
                        {
                            badgeBg = Color.FromArgb(230, 244, 255);
                            badgeBorder = Color.FromArgb(145, 202, 255);
                            badgeTextClr = Color.FromArgb(9, 88, 217);
                        }

                        using (var brush = new SolidBrush(badgeBg))
                        using (var pen = new Pen(badgeBorder, 1F))
                        {
                            g.FillPath(brush, badgePath);
                            g.DrawPath(pen, badgePath);
                        }

                        TextRenderer.DrawText(g, shapeCode, fontBadge, badgeRect, badgeTextClr, centeredTextFlags);
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前形状的微型角标代号：
        /// 只要用户选择了形状按钮（含直角），就显示对应设置代号（圆角显示 R8/R5，直角显示 Z，圆形显示 C，异形显示 Y）；
        /// 只有当用户未选择任何形状按钮（_isShapeExplicitlySelected == false）时才不显示胶囊。
        /// </summary>
        private string GetCurrentShapeBadgeCode()
        {
            if (!_isShapeExplicitlySelected) return null;
            switch (SelectedShape)
            {
                case ShapeType.RoundRect:
                    var roundCode = AppSettings.Get("RoundShapeCode") as string ?? "R";
                    var hideRadius = AppSettings.Get("HideRadiusValue") as bool? ?? false;
                    double r = RoundRadius > 0
                        ? RoundRadius
                        : (double.TryParse(AppSettings.GetValue<string>("LastRoundRadius"), out double dr) ? dr : 3);
                    return hideRadius ? roundCode : $"{roundCode}{r:0.##}";

                case ShapeType.Circle:
                    return AppSettings.Get("CircleShapeCode") as string ?? "C";

                case ShapeType.Special:
                    return AppSettings.Get("EllipseShapeCode") as string ?? "Y";

                case ShapeType.RightAngle:
                default:
                    return AppSettings.Get("ZeroShapeCode") as string ?? "Z";
            }
        }

        /// <summary>
        /// 把排参框彩环位图中属于该药丸的那一片铺到药丸底色上。
        /// 六宫格无间隙平铺，因此每格只画自己那一片，合起来正好是一块连续的彩环，
        /// 不会再出现列与列之间的白色竖线。
        /// </summary>
        private void PaintV5MetricsGlowSlice(Graphics graphics, System.Windows.Forms.Panel pill)
        {
            Bitmap glow = GetV5MetricsGlow();
            if (glow == null || pill.Width <= 0 || pill.Height <= 0)
            {
                return;
            }

            int sourceX = Math.Max(0, Math.Min(pill.Left, glow.Width - 1));
            int sourceY = Math.Max(0, Math.Min(pill.Top, glow.Height - 1));
            if (sourceX >= glow.Width || sourceY >= glow.Height)
            {
                return;
            }

            // glow 位图已通过精确的 SDF 距离场算法完成了亚像素同心圆角与抗锯齿羽化。
            // 严禁在此叠加 GDI+ 的 SetClip 二值化裁剪：GDI+ 剪裁掩码仅有 1-bit 精度，
            // 会粗暴切掉圆角弧线外侧的抗锯齿彩环半透明像素，在角部留下明显的白斑断口（缺口）。
            // 直接绘制预生成的切片即可实现连续、无断口的完美圆角闭环。
            graphics.DrawImageUnscaled(glow, -sourceX, -sourceY);
        }

        /// <summary>
        /// 按当前主题与色彩模式取得排参框彩环位图（尺寸与 tlpMetrics 客户区一致）。
        /// </summary>
        private Bitmap GetV5MetricsGlow()
        {
            if (tlpMetrics == null || tlpMetrics.Width <= 2 || tlpMetrics.Height <= 2)
            {
                return null;
            }

            bool isBlackAndWhite = _v5ColorText.Contains("黑白");
            return GetV5MetricsGlowBitmap(
                tlpMetrics.Width,
                tlpMetrics.Height,
                _activeTheme?.Surface ?? Color.White,
                BuildV5MetricsPalette(isBlackAndWhite),
                isBlackAndWhite,
                _activeTheme != null && IsThemeDark(_activeTheme));
        }

        /// <summary>
        /// 在触发重绘之前把彩环位图重建好，把锥形渐变与高斯模糊从 Paint 里挪出来。
        /// 尺寸与配色未变时 <see cref="GetV5MetricsGlowBitmap"/> 会直接命中缓存，代价可以忽略。
        /// </summary>
        private void RefreshV5MetricsGlowCache()
        {
            try
            {
                GetV5MetricsGlow();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 看板] 预生成排参框彩环失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 排参框彩环调色板：彩色与黑白只差一组停靠点。
        /// 黑白模式重色精准分布在两端(左/右)和正中间(顶中/底中)，
        /// 两侧爆发纯白耀眼的“镜面亮银高光（Bright Specular Peak）”，
        /// 配合高光聚焦收束停靠点，使金属反光极具穿透力，光泽感显著拉满。
        /// </summary>
        private static Color[] BuildV5MetricsPalette(bool isBlackAndWhite)
        {
            if (isBlackAndWhite)
            {
                var darkSilver = Color.FromArgb(138, 146, 158);     // 重色：钛冷银金属阴影 (两端与中间)
                var midSilver = Color.FromArgb(195, 202, 212);      // 中过渡银
                var brightSpecular = Color.FromArgb(255, 255, 255); // 纯白高光耀斑 (高光极亮，突破平淡)

                return new[]
                {
                    darkSilver,     // 0: 顶中 (正中间重色)
                    midSilver,
                    brightSpecular, // 1: 东北 (纯白镜面高光)
                    midSilver,
                    darkSilver,     // 2: 右端 (最右端重色)
                    midSilver,
                    brightSpecular, // 3: 东南 (纯白镜面高光)
                    midSilver,
                    darkSilver,     // 4: 底中 (正中间重色)
                    midSilver,
                    brightSpecular, // 5: 西南 (纯白镜面高光)
                    midSilver,
                    darkSilver,     // 6: 左端 (最左端重色)
                    midSilver,
                    brightSpecular, // 7: 西北 (纯白镜面高光)
                    midSilver
                };
            }

            return new[]
            {
                Color.FromArgb(255, 154, 154),
                Color.FromArgb(141, 226, 220),
                Color.FromArgb(131, 206, 225),
                Color.FromArgb(189, 228, 210),
                Color.FromArgb(255, 224, 138),
                Color.FromArgb(255, 200, 242)
            };
        }

        /// <summary>
        /// 绘制 HTML 第三套“排参框内陷光”：大范围锥形渐变、模糊扩散、
        /// 半透明内层和轻微 inset 阴影共同形成向内收敛的边框状态。
        /// </summary>
        private void PaintV5MetricsInsetGlow(object sender, PaintEventArgs e)
        {
            if (!(sender is Control control) || control.Width <= 2 || control.Height <= 2)
            {
                return;
            }

            Color surfaceColor = _activeTheme?.Surface ?? Color.White;
            bool isBlackAndWhite = _v5ColorText.Contains("黑白");
            bool isDarkTheme = _activeTheme != null && IsThemeDark(_activeTheme);
            Color[] palette = BuildV5MetricsPalette(isBlackAndWhite);

            Graphics graphics = e.Graphics;
            GraphicsState state = graphics.Save();
            try
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                Bitmap glowBitmap = GetV5MetricsGlowBitmap(
                    control.Width,
                    control.Height,
                    surfaceColor,
                    palette,
                    isBlackAndWhite,
                    isDarkTheme);
                // glowBitmap 自身已由 SDF 距离场算法实现亚像素级的同心圆角与抗锯齿羽化，
                // 不再叠加 GDI+ 粗暴的 1-bit SetClip 裁剪（SetClip 会破坏边缘亚像素并产生断口切角）
                graphics.DrawImageUnscaled(glowBitmap, 0, 0);

                // 裁剪由外层 Save/Restore 还原即可，不要在这里 ResetClip：
                // ResetClip 会把裁剪区置为无限大，泄漏到本次绘制的后续步骤。
                // HTML 的 .effect-border-inset 把 border-color 设为 transparent，四边轮廓
                // 只由模糊渐变环自己构成，不再叠加任何额外的中性描边，否则会冲淡或加深
                // 某一侧的环色，导致四边不一致。
            }
            finally
            {
                graphics.Restore(state);
            }
        }

        private Bitmap GetV5MetricsGlowBitmap(
            int width,
            int height,
            Color surfaceColor,
            Color[] palette,
            bool isBlackAndWhite,
            bool isDarkTheme)
        {
            string cacheKey = string.Join("|",
                width,
                height,
                surfaceColor.ToArgb(),
                isBlackAndWhite,
                isDarkTheme);
            if (_v5MetricsGlowBitmap != null && _v5MetricsGlowCacheKey == cacheKey)
            {
                return _v5MetricsGlowBitmap;
            }

            _v5MetricsGlowBitmap?.Dispose();
            _v5MetricsGlowBitmap = new Bitmap(width, height);
            _v5MetricsGlowCacheKey = cacheKey;

            // 彩色与黑白共用锥形渐变与模糊管线，并在内表面通透度与阴影上进行差异化微调
            BuildV5MetricsConicGlow(_v5MetricsGlowBitmap, palette, surfaceColor, isBlackAndWhite);

            return _v5MetricsGlowBitmap;
        }


        /// <summary>
        /// 复刻 HTML .metrics-pills-pure.effect-border-inset 的 ::before：大范围锥形渐变
        /// 叠加 blur(10px) 与 0.72 透明度，再按 ::after 的 inset:2px 白色内层收口。
        /// 彩色与黑白共用此管线，针对黑白浅色模式进行内表面提纯与淡雅微阴影优化。
        /// </summary>
        private static void BuildV5MetricsConicGlow(Bitmap bitmap, Color[] palette, Color surfaceColor, bool isBlackAndWhite)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            const int horizontalBleed = 30;
            const int verticalBleed = 190;
            const int blurRadius = 30;
            const double blurSigma = 10D;
            // 黑白模式提升到 0.85D，强化亮银高光的穿透力与金属镜面反光锐度；彩色模式保持 0.72D
            double gradientOpacity = isBlackAndWhite ? 0.85D : 0.72D;
            // 黑白模式内表面调浅至 0.90D，配合极浅冷银白底色(#F8FAFC)，既保持凹槽容器感，又极其通透轻盈
            double innerSurfaceOpacity = isBlackAndWhite ? 0.90D : 0.80D;

            int sourceWidth = width + horizontalBleed * 2 + blurRadius * 2;
            int sourceHeight = height + verticalBleed * 2 + blurRadius * 2;
            var source = new int[sourceWidth * sourceHeight];
            var horizontalBlur = new int[source.Length];
            var blurred = new int[source.Length];
            double centerX = blurRadius + horizontalBleed + (width - 1) / 2D;
            double centerY = blurRadius + verticalBleed + (height - 1) / 2D;

            for (int y = 0; y < sourceHeight; y++)
            {
                int rowOffset = y * sourceWidth;
                double dy = y - centerY;
                for (int x = 0; x < sourceWidth; x++)
                {
                    double dx = x - centerX;
                    // CSS conic-gradient 以正上方为 0 度并沿顺时针方向递增。
                    double clockwiseAngle = Math.Atan2(dx, -dy);
                    double position = clockwiseAngle / (Math.PI * 2D);
                    if (position < 0D) position += 1D;
                    source[rowOffset + x] = PackV5Rgb(InterpolatePaletteColor(palette, position));
                }
            }

            double[] weights = CreateV5GaussianWeights(blurRadius, blurSigma);
            BlurV5RgbHorizontal(source, horizontalBlur, sourceWidth, sourceHeight, weights, blurRadius);
            BlurV5RgbVertical(horizontalBlur, blurred, sourceWidth, sourceHeight, weights, blurRadius);

            const double rOut = 5.0D;
            const double thickness = 2.0D;

            for (int y = 0; y < height; y++)
            {
                int sourceY = y + blurRadius + verticalBleed;
                double py = y + 0.5D;
                for (int x = 0; x < width; x++)
                {
                    int sourceX = x + blurRadius + horizontalBleed;
                    double px = x + 0.5D;

                    // 同心圆角 SDF 欧氏距离：解决直角矩形 Math.Min 在角部产生的 90° 尖锐死角与彩环厚度畸变
                    double distToEdge;
                    if (px < rOut && py < rOut)
                    {
                        double dx = px - rOut;
                        double dy = py - rOut;
                        distToEdge = rOut - Math.Sqrt(dx * dx + dy * dy);
                    }
                    else if (px > width - rOut && py < rOut)
                    {
                        double dx = px - (width - rOut);
                        double dy = py - rOut;
                        distToEdge = rOut - Math.Sqrt(dx * dx + dy * dy);
                    }
                    else if (px < rOut && py > height - rOut)
                    {
                        double dx = px - rOut;
                        double dy = py - (height - rOut);
                        distToEdge = rOut - Math.Sqrt(dx * dx + dy * dy);
                    }
                    else if (px > width - rOut && py > height - rOut)
                    {
                        double dx = px - (width - rOut);
                        double dy = py - (height - rOut);
                        distToEdge = rOut - Math.Sqrt(dx * dx + dy * dy);
                    }
                    else
                    {
                        distToEdge = Math.Min(Math.Min(px, (double)width - px), Math.Min(py, (double)height - py));
                    }

                    // 1. 外圆角亚像素平滑抗锯齿覆盖率 (0.0 ~ 1.0)
                    double outerCoverage = Math.Max(0.0D, Math.Min(1.0D, distToEdge + 0.5D));

                    // 2. 内表面同心圆弧平滑过渡率 (0.0 ~ 1.0)
                    double distToInner = distToEdge - thickness;
                    double innerCoverage = Math.Max(0.0D, Math.Min(1.0D, distToInner + 0.5D));

                    Color glowColor = UnpackV5Rgb(blurred[sourceY * sourceWidth + sourceX]);
                    Color blended = BlendV5GlowColor(surfaceColor, glowColor, gradientOpacity);

                    if (innerCoverage > 0.001D)
                    {
                        // 黑白模式内表面使用调浅后的极淡冷银白底色(#F8FAFC)，既保持凹槽容器感，又极其通透轻盈
                        Color innerTargetColor = isBlackAndWhite
                            ? Color.FromArgb(248, 250, 252)
                            : Color.White;
                        blended = BlendV5GlowColor(blended, innerTargetColor, innerSurfaceOpacity * innerCoverage);
                        if (distToInner > 0.0D)
                        {
                            if (isBlackAndWhite)
                            {
                                // 0.5px 精细金属倒角微阴影
                                double softShadow = 0.012D * Math.Exp(-distToInner / 2.8D) * innerCoverage;
                                blended = BlendV5GlowColor(blended, Color.FromArgb(130, 140, 155), softShadow);
                            }
                            else
                            {
                                double darkShadow = 0.03D * Math.Exp(-distToInner / 4.0D) * innerCoverage;
                                blended = BlendV5GlowColor(blended, Color.FromArgb(22, 31, 42), darkShadow);
                            }
                        }
                    }

                    // 3. 外圆角与背景底色自然抗锯齿融合
                    Color finalPixel = BlendV5GlowColor(surfaceColor, blended, outerCoverage);
                    bitmap.SetPixel(x, y, finalPixel);
                }
            }
        }

        private static double[] CreateV5GaussianWeights(int radius, double sigma)
        {
            var weights = new double[radius * 2 + 1];
            double total = 0D;
            for (int offset = -radius; offset <= radius; offset++)
            {
                double weight = Math.Exp(-(offset * offset) / (2D * sigma * sigma));
                weights[offset + radius] = weight;
                total += weight;
            }
            for (int i = 0; i < weights.Length; i++) weights[i] /= total;
            return weights;
        }

        private static void BlurV5RgbHorizontal(int[] source, int[] destination, int width, int height, double[] weights, int radius)
        {
            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    double red = 0D, green = 0D, blue = 0D;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int sampleX = Math.Max(0, Math.Min(width - 1, x + offset));
                        int pixel = source[rowOffset + sampleX];
                        double weight = weights[offset + radius];
                        red += ((pixel >> 16) & 255) * weight;
                        green += ((pixel >> 8) & 255) * weight;
                        blue += (pixel & 255) * weight;
                    }
                    destination[rowOffset + x] = PackV5Rgb(red, green, blue);
                }
            }
        }

        private static void BlurV5RgbVertical(int[] source, int[] destination, int width, int height, double[] weights, int radius)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double red = 0D, green = 0D, blue = 0D;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int sampleY = Math.Max(0, Math.Min(height - 1, y + offset));
                        int pixel = source[sampleY * width + x];
                        double weight = weights[offset + radius];
                        red += ((pixel >> 16) & 255) * weight;
                        green += ((pixel >> 8) & 255) * weight;
                        blue += (pixel & 255) * weight;
                    }
                    destination[y * width + x] = PackV5Rgb(red, green, blue);
                }
            }
        }

        private static int PackV5Rgb(Color color) => (color.R << 16) | (color.G << 8) | color.B;

        private static int PackV5Rgb(double red, double green, double blue)
        {
            int r = Math.Max(0, Math.Min(255, (int)Math.Round(red)));
            int g = Math.Max(0, Math.Min(255, (int)Math.Round(green)));
            int b = Math.Max(0, Math.Min(255, (int)Math.Round(blue)));
            return (r << 16) | (g << 8) | b;
        }

        private static Color UnpackV5Rgb(int pixel)
        {
            return Color.FromArgb((pixel >> 16) & 255, (pixel >> 8) & 255, pixel & 255);
        }

        private static Color InterpolatePaletteColor(Color[] palette, double position)
        {
            if (palette == null || palette.Length == 0)
            {
                return Color.Transparent;
            }
            if (palette.Length == 1)
            {
                return palette[0];
            }

            double wrapped = position - Math.Floor(position);
            double scaled = wrapped * palette.Length;
            int firstIndex = Math.Min(palette.Length - 1, (int)Math.Floor(scaled));
            int secondIndex = (firstIndex + 1) % palette.Length;
            double amount = scaled - Math.Floor(scaled);
            Color first = palette[firstIndex];
            Color second = palette[secondIndex];
            return Color.FromArgb(
                (int)Math.Round(first.R + (second.R - first.R) * amount),
                (int)Math.Round(first.G + (second.G - first.G) * amount),
                (int)Math.Round(first.B + (second.B - first.B) * amount));
        }

        private static Color BlendV5GlowColor(Color surface, Color glow, double opacity)
        {
            double amount = Math.Max(0D, Math.Min(1D, opacity));
            return Color.FromArgb(
                (int)Math.Round(surface.R + (glow.R - surface.R) * amount),
                (int)Math.Round(surface.G + (glow.G - surface.G) * amount),
                (int)Math.Round(surface.B + (glow.B - surface.B) * amount));
        }

        /// <summary>
        /// 同步更新 V5 纯净 6 宫格看板指标数据 (触发药丸平滑重绘)
        /// </summary>
        public void UpdateV5ImpositionDashboard()
        {
            try
            {
                int r = _currentImpositionResult?.Rows ?? 5;
                int c = _currentImpositionResult?.Columns ?? 3;
                _v5GridText = $"{r}×{c}";

                _v5CountText = $"{_currentImpositionResult?.OptimalLayoutQuantity ?? 15}";
                _v5RotationText = $"{_currentImpositionResult?.RotationAngle ?? (GetCurrentRotationMode() == RollRotationMode.Force270Degree ? 270 : 0)}";

                if (!string.IsNullOrWhiteSpace(AdjustedDimensions))
                {
                    // 仅提取纯净长宽尺寸（如 "80x45R8" -> "80×45"），使尺寸主数值永远纯净居中，形状代号由右上角胶囊独立呈现
                    var match = System.Text.RegularExpressions.Regex.Match(AdjustedDimensions, @"^[\d\.]+[x×*][\d\.]+");
                    if (match.Success)
                    {
                        _v5DimensionText = match.Value.Replace("x", "×").Replace("*", "×");
                    }
                    else
                    {
                        _v5DimensionText = AdjustedDimensions.Replace("x", "×");
                    }
                }
                else if (_initialWidth > 0 && _initialHeight > 0)
                {
                    _v5DimensionText = $"{FormatDimension(_initialWidth)}×{FormatDimension(_initialHeight)}";
                }
                else if (_pdfOriginalWidth > 0 && _pdfOriginalHeight > 0)
                {
                    _v5DimensionText = $"{FormatDimension(_pdfOriginalWidth)}×{FormatDimension(_pdfOriginalHeight)}";
                }

                _v5BleedText = double.TryParse(bleedDropdown?.Text, out double bleedValue)
                    ? bleedValue.ToString("0.##")
                    : "2";
                string colorModeText = !string.IsNullOrWhiteSpace(colorModeButton?.Text)
                    ? colorModeButton.Text
                    : "彩色";
                bool isBlackAndWhite = colorModeText.Contains("黑白");
                _v5ColorText = isBlackAndWhite ? "黑白" : "彩色";
                _v5ColorTextColor = isBlackAndWhite
                    ? Color.FromArgb(75, 82, 93)
                    : Color.FromArgb(19, 194, 194);

                UpdateImpoModeButtonTypes();

                // 彩环位图必须在这里先重建好再触发重绘：它要跑一遍 452×484 的锥形渐变
                // 加两趟 61 抽头高斯模糊，留在 Paint 里做会让第一个重绘的药丸卡住数百毫秒，
                // 期间其余药丸仍是旧配色，看上去就是切换颜色模式时明显闪烁。
                RefreshV5MetricsGlowCache();

                pnlPillGrid?.Invalidate();
                pnlPillCount?.Invalidate();
                pnlPillRotation?.Invalidate();
                if (pnlPillRotation != null)
                    folderTreeViewToolTip?.SetToolTip(pnlPillRotation, "点击打开排版参数，选择自动择优、禁止旋转或固定270°");
                pnlPillDimension?.Invalidate();
                if (pnlPillDimension != null)
                {
                    folderTreeViewToolTip?.SetToolTip(pnlPillDimension, _v5DimensionText);
                }
                pnlPillBleed?.Invalidate();
                pnlPillColor?.Invalidate();
                tlpMetrics?.Invalidate(true);

                UpdateCustomImpositionCapsule();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 看板] 更新指标失败: {ex.Message}", ex);
            }
        }

        private void UpdateImpoModeButtonTypes()
        {
            try
            {
                if (segMaterialType != null)
                {
                    segMaterialType.SelectedIndex = (flatSheetRadioButton?.Checked ?? true) ? 0 : 1;
                }
                if (segLayoutMode != null)
                {
                    segLayoutMode.SelectedIndex = (continuousLayoutRadioButton?.Checked ?? true) ? 0 : 1;
                }
                UpdateCustomImpositionCapsule();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 看板] 更新模式分段控件状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查当前大版纸张尺寸/卷装幅宽是否与系统全局设置不一致；
        /// 仅当尺寸与设置不一致时浮现胶囊指示器，点击可直接调出大版开料参数；与设置一致时自动隐藏保持极简。
        /// </summary>
        public void UpdateCustomImpositionCapsule()
        {
            if (pnlImpositionDashboardCard == null || _customSizeCapsule == null || IsDisposed) return;

            try
            {
                EnsureTemporaryImpositionParameters();
                bool isFlat = (segMaterialType != null ? segMaterialType.SelectedIndex == 0 : (flatSheetRadioButton?.Checked ?? true));

                if (isFlat)
                {
                    float defaultW = GetFloatValue(AppSettings.Get("Imposition_PaperWidth") as string, 320);
                    float defaultH = GetFloatValue(AppSettings.Get("Imposition_PaperHeight") as string, 464);

                    float currentW = _temporaryImpositionParameters.PaperWidth > 0 ? _temporaryImpositionParameters.PaperWidth : defaultW;
                    float currentH = _temporaryImpositionParameters.PaperHeight > 0 ? _temporaryImpositionParameters.PaperHeight : defaultH;

                    bool isCustom = (_temporaryImpositionParameters.IsPaperSizeUnlocked || _temporaryImpositionParameters.PaperWidth > 0 || _temporaryImpositionParameters.PaperHeight > 0) &&
                                    (Math.Abs(currentW - defaultW) > 0.1f || Math.Abs(currentH - defaultH) > 0.1f);

                    if (isCustom)
                    {
                        _customSizeCapsule.IsRoll = false;
                        _customSizeCapsule.CapsuleText = $"{currentW:0.#}×{currentH:0.#}";
                        _customSizeCapsule.Visible = true;
                        _customSizeCapsule.BringToFront();
                        folderTreeViewToolTip?.SetToolTip(_customSizeCapsule, $"当前平张纸张为非默认大版尺寸: {currentW:0.#}×{currentH:0.#}mm (系统默认: {defaultW:0.#}×{defaultH:0.#}mm)\n点击可直接查看或微调大版开料参数");
                    }
                    else
                    {
                        _customSizeCapsule.Visible = false;
                    }
                }
                else
                {
                    float defaultW = GetFloatValue(AppSettings.Get("Imposition_FixedWidth") as string, 320);
                    float defaultH = GetFloatValue(AppSettings.Get("Imposition_MinLength") as string, 100);

                    float currentW = _temporaryImpositionParameters.FixedWidth > 0 ? _temporaryImpositionParameters.FixedWidth : defaultW;
                    float currentH = _temporaryImpositionParameters.MinLength > 0 ? _temporaryImpositionParameters.MinLength : defaultH;

                    bool isWidthCustom = Math.Abs(currentW - defaultW) > 0.1f;
                    bool isHeightCustom = Math.Abs(currentH - defaultH) > 0.1f;

                    bool isCustom = (_temporaryImpositionParameters.IsRollSizeUnlocked || _temporaryImpositionParameters.FixedWidth > 0 || _temporaryImpositionParameters.MinLength > 0) &&
                                    (isWidthCustom || isHeightCustom);

                    if (isCustom)
                    {
                        _customSizeCapsule.IsRoll = true;
                        if (isWidthCustom && isHeightCustom)
                        {
                            _customSizeCapsule.CapsuleText = $"{currentW:0.#}×{currentH:0.#}";
                        }
                        else if (isHeightCustom)
                        {
                            _customSizeCapsule.CapsuleText = $"高 {currentH:0.#}";
                        }
                        else
                        {
                            _customSizeCapsule.CapsuleText = $"{currentW:0.#}幅宽";
                        }
                        _customSizeCapsule.Visible = true;
                        _customSizeCapsule.BringToFront();
                        folderTreeViewToolTip?.SetToolTip(_customSizeCapsule, $"当前卷装材料使用非默认尺寸: 高度 {currentH:0.#}mm (系统默认: {defaultH:0.#}mm), 幅宽 {currentW:0.#}mm (系统默认: {defaultW:0.#}mm)\n点击可直接查看或微调大版开料参数");
                    }
                    else
                    {
                        _customSizeCapsule.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"[UpdateCustomImpositionCapsule] 更新特殊尺寸胶囊失败: {ex.Message}");
                if (_customSizeCapsule != null) _customSizeCapsule.Visible = false;
            }
        }

        private void UpdateIdentifierPageTooltip()
        {
            if (btnIdentifierPageToggle == null || chkIdentifierPage == null) return;
            string status = chkIdentifierPage.Checked ? "已开启" : "已停用";
            folderTreeViewToolTip?.SetToolTip(btnIdentifierPageToggle, $"大版首尾插入标识页 ({status})\n用于记录订单信息、材料参数与拼版规格");
        }

        private void CycleBleedPreset()
        {
            var values = GetAvailableBleedValues();
            if (values.Count == 0) return;

            double current = double.TryParse(bleedDropdown?.Text, out double parsedCurrent)
                ? parsedCurrent
                : values[0];
            int currentIndex = values.FindIndex(value => Math.Abs(value - current) < 0.001);
            int nextIndex = (currentIndex + 1 + values.Count) % values.Count;
            ApplyV5BleedValue(values[nextIndex]);
        }

        private void ShowBleedOptionsMenu()
        {
            var values = GetAvailableBleedValues();
            if (values.Count == 0 || pnlPillBleed == null) return;

            if (bleedOptionsMenu == null || bleedOptionsMenu.IsDisposed)
            {
                bleedOptionsMenu = new ContextMenuStrip
                {
                    ShowImageMargin = false,
                    Font = new Font("Microsoft YaHei UI", 9F)
                };
                this.Disposed += (s, e) => bleedOptionsMenu?.Dispose();
            }

            bleedOptionsMenu.Items.Clear();
            foreach (double value in values)
            {
                double selectedValue = value;
                var item = new ToolStripMenuItem($"{value:0.##} mm")
                {
                    Checked = double.TryParse(bleedDropdown?.Text, out double current) && Math.Abs(current - value) < 0.001
                };
                item.Click += (s, e) => ApplyV5BleedValue(selectedValue);
                bleedOptionsMenu.Items.Add(item);
            }
            bleedOptionsMenu.Show(pnlPillBleed, new Point(0, pnlPillBleed.Height));
        }

        private List<double> GetAvailableBleedValues()
        {
            var values = new List<double>();
            if (bleedDropdown != null)
            {
                foreach (object item in bleedDropdown.Items)
                {
                    if (double.TryParse(item?.ToString(), out double value) && !values.Contains(value))
                    {
                        values.Add(value);
                    }
                }
            }

            if (values.Count == 0)
            {
                values.AddRange(new[] { 0D, 1.5D, 2D, 3D });
            }

            return values;
        }

        private void ApplyV5BleedValue(double value)
        {
            if (bleedDropdown == null) return;

            int matchedIndex = -1;
            for (int i = 0; i < bleedDropdown.Items.Count; i++)
            {
                if (double.TryParse(bleedDropdown.Items[i]?.ToString(), out double option) && Math.Abs(option - value) < 0.001)
                {
                    matchedIndex = i;
                    break;
                }
            }

            if (matchedIndex >= 0)
            {
                bleedDropdown.SelectedIndex = matchedIndex;
                bleedDropdown.Text = bleedDropdown.Items[matchedIndex].ToString();
            }
            else
            {
                bleedDropdown.Text = value.ToString("0.##");
                BleedDropdown_SelectedIndexChanged(bleedDropdown, EventArgs.Empty);
            }
            UpdateV5ImpositionDashboard();
        }

        private void ToggleV5ColorMode()
        {
            ColorModeButton_Click(colorModeButton, EventArgs.Empty);
            SyncCurrentSelectionsToActiveGroup();
            UpdateV5ImpositionDashboard();
        }

        private void ToggleOutputFolderDropdown()
        {
            if (folderTreeView == null || btnBrowseOutputFolder == null)
            {
                return;
            }

            EnsureOutputFolderDropDown();

            if (outputFolderDropDown.Visible)
            {
                outputFolderDropDown.Close(ToolStripDropDownCloseReason.CloseCalled);
                return;
            }

            SyncOutputPathDisplay();
            folderTreeView.BackColor = Color.White;
            outputFolderDropDown.BackColor = Color.White;
            ResizeOutputFolderDropDownToContent();
            outputFolderDropDown.Show(btnBrowseOutputFolder, new Point(btnBrowseOutputFolder.Width - outputFolderDropDown.Width, btnBrowseOutputFolder.Height + 1));
            folderTreeView.Focus();
        }

        /// <summary>
        /// 创建贴附在“切换目录”按钮下方的目录选择浮层，避免旧 TreeView 被 V5 工位遮挡。
        /// </summary>
        private void EnsureOutputFolderDropDown()
        {
            if (outputFolderDropDown != null) return;

            // 直接承载既有 TreeView，不复制节点、不重新定义绘制样式。
            folderTreeView.Dock = DockStyle.None;
            folderTreeView.Visible = true;
            folderTreeView.Scrollable = false;
            folderTreeView.BackColor = Color.White;
            folderTreeView.AfterExpand += (s, e) => ResizeOutputFolderDropDownToContent();
            folderTreeView.AfterCollapse += (s, e) => ResizeOutputFolderDropDownToContent();
            folderTreeView.NodeMouseDoubleClick += FolderTreeView_NodeMouseDoubleClickV5;

            outputFolderDropDownHost = new ToolStripControlHost(folderTreeView)
            {
                AutoSize = false,
                Size = new Size(198, 120),
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            outputFolderDropDown = new ToolStripDropDown
            {
                AutoSize = false,
                Size = new Size(200, 122),
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                DropShadowEnabled = false,
                BackColor = Color.White,
                Opacity = 1.0D
            };
            outputFolderDropDown.Items.Add(outputFolderDropDownHost);
            ResizeOutputFolderDropDownToContent();
        }

        /// <summary>
        /// 根据当前展开节点数调整浮层高度，以完整显示节点且不出现滚动条。
        /// </summary>
        private void ResizeOutputFolderDropDownToContent()
        {
            if (folderTreeView == null || outputFolderDropDownHost == null || outputFolderDropDown == null) return;

            int visibleNodeCount = CountVisibleFolderNodes(folderTreeView.Nodes);
            int treeHeight = Math.Max(folderTreeView.ItemHeight * Math.Max(visibleNodeCount, 2) + 4, 44);

            folderTreeView.Size = new Size(198, treeHeight);
            outputFolderDropDownHost.Size = folderTreeView.Size;
            outputFolderDropDown.Size = new Size(200, treeHeight + 2);
        }

        /// <summary>
        /// 双击任意目录时确认当前路径并关闭附着下拉框。
        /// </summary>
        private void FolderTreeView_NodeMouseDoubleClickV5(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag == null) return;

            SelectedExportPath = e.Node.Tag.ToString();
            folderTreeView.SelectedNode = e.Node;
            SaveSelectedExportPath();
            SyncOutputPathDisplay();
            SyncCurrentSelectionsToActiveGroup();
            outputFolderDropDown?.Close(ToolStripDropDownCloseReason.ItemClicked);
        }

        private static int CountVisibleFolderNodes(TreeNodeCollection nodes)
        {
            int count = 0;
            foreach (TreeNode node in nodes)
            {
                count++;
                if (node.IsExpanded && node.Nodes.Count > 0)
                {
                    count += CountVisibleFolderNodes(node.Nodes);
                }
            }
            return count;
        }

        /// <summary>
        /// 将路径栏与当前真实输出目录保持一致，并用提示完整展示长路径。
        /// </summary>
        private void SyncOutputPathDisplay()
        {
            if (outputPathTextBox == null) return;

            string displayPath = GetOutputPathDisplayText();
            var segments = displayPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var segmentColors = new List<Color>();
            if (selectedTreeNode != null && segments.Length > 0)
            {
                int selectedLevel = GetNodeLevel(selectedTreeNode);
                int firstVisibleLevel = Math.Max(0, selectedLevel - segments.Length + 1);
                for (int i = 0; i < segments.Length; i++)
                {
                    segmentColors.Add(GetLevelColor(firstVisibleLevel + i));
                }
            }
            else
            {
                for (int i = 0; i < segments.Length; i++)
                {
                    segmentColors.Add(Color.FromArgb(31, 36, 41));
                }
            }
            outputPathTextBox.SetPath(displayPath, segmentColors);
            folderTreeViewToolTip?.SetToolTip(outputPathTextBox, SelectedExportPath ?? displayPath);
        }

        private string GetOutputPathDisplayText()
        {
            string selectedPath = SelectedExportPath;
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                selectedPath = AppSettings.GetValue<string>("LastSelectedExportPath");
            }

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return "未选择输出目录";
            }

            // 路径栏只显示相对已配置根目录的部分，完整路径保留在悬停提示中。
            string normalizedSelectedPath = selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var matchedRoot = GetConfiguredExportFolders()
                .Select(folder => folder.Path?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .Where(root => !string.IsNullOrWhiteSpace(root) &&
                    (normalizedSelectedPath.Equals(root, StringComparison.OrdinalIgnoreCase) ||
                     normalizedSelectedPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                     normalizedSelectedPath.StartsWith(root + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(root => root.Length)
                .FirstOrDefault();

            if (matchedRoot == null)
            {
                return selectedPath;
            }

            string relativePath = normalizedSelectedPath.Substring(matchedRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.IsNullOrWhiteSpace(relativePath)
                ? Path.GetFileName(matchedRoot)
                : relativePath;
        }

        private void PerformSerialLookup(bool notifyFeedback = false)
        {
            try
            {
                if (enableSerialSearchCheckbox != null)
                {
                    enableSerialSearchCheckbox.Checked = true;
                }
                if (btnSearchSerialAction != null)
                {
                    btnSearchSerialAction.IsSearchActive = true;
                }
                string serial = serialNumberTextBox?.Text?.Trim();
                if (!string.IsNullOrEmpty(serial))
                {
                    SearchQuantityBySerialNumber(serial);
                }
                else if (notifyFeedback)
                {
                    AntdUI.Message.info(this, "请输入序号后再进行反查", autoClose: 2);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 工位] 序号反查异常: {ex.Message}", ex);
            }
        }

        private static string GetPageOrderPillText(CopyType type)
        {
            return type == CopyType.Duplicate ? "整套 ▾" : "逐页 ▾";
        }

        private void SelectPageRepeatType(CopyType type)
        {
            if (_copyType == type) return;
            _copyType = type;
            if (copyTypeComboBox != null)
            {
                copyTypeComboBox.SelectedIndex = (int)type;
            }
            UpdateCopyCountPillDisplay();
        }

        private void UpdateJoinGroupEnabledState(bool enabled)
        {
            try
            {
                if (txtCopyCountInput != null)
                {
                    txtCopyCountInput.Enabled = enabled;
                    txtCopyCountInput.ForeColor = enabled ? Color.FromArgb(31, 36, 41) : Color.FromArgb(170, 170, 170);
                }
                if (segCopyType != null)
                {
                    segCopyType.Enabled = enabled;
                }
                if (lblCopyModePill != null)
                {
                    lblCopyModePill.Enabled = enabled;
                    lblCopyModePill.ForeColor = enabled ? Color.FromArgb(71, 85, 105) : Color.FromArgb(170, 170, 170);
                }
                UpdateCopyCountPillDisplay();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 参数舱] 更新PDF页序复制状态异常: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 动态计算 V5 工位实际内容高度（多行预设自适应加高，紧凑消除所有空白）
        /// </summary>
        public int CalculateStationContentHeight()
        {
            if (presetButtonsPanel != null)
            {
                presetButtonsPanel.Width = 330;
                presetButtonsPanel.PerformLayout();
                int maxBtnBottom = 0;
                foreach (Control c in presetButtonsPanel.Controls)
                {
                    if (c.Visible)
                    {
                        maxBtnBottom = Math.Max(maxBtnBottom, c.Bottom + c.Margin.Bottom);
                    }
                }
                int tabsH = tabs1?.Height ?? 22;
                int innerH = Math.Max(26, maxBtnBottom);
                int hPreset = tabsH + innerH + 8;
                if (presetBrowserCard != null && presetBrowserCard.Height != hPreset)
                {
                    presetBrowserCard.Height = hPreset;
                }
            }

            int contentHeight = 0;
            if (pnlStationContent != null)
            {
                pnlStationContent.PerformLayout();
                foreach (Control c in pnlStationContent.Controls)
                {
                    if (c.Visible)
                    {
                        contentHeight = Math.Max(contentHeight, c.Bottom + c.Margin.Bottom);
                    }
                }
                contentHeight += pnlStationContent.Padding.Bottom + 4;
            }
            if (contentHeight < 400) contentHeight = 466;

            return contentHeight;
        }

        /// <summary>
        /// 调整预设卡片与整体工位高度，确保多行预设完全展示且操作栏/折叠条永远紧随其后
        /// </summary>
        public void AdjustV5StationLayout()
        {
            try
            {
                if (presetButtonsPanel == null || pnlStationContent == null) return;

                presetButtonsPanel.Width = 330;
                presetButtonsPanel.PerformLayout();
                int maxBtnBottom = 0;
                foreach (Control c in presetButtonsPanel.Controls)
                {
                    if (c.Visible)
                    {
                        maxBtnBottom = Math.Max(maxBtnBottom, c.Bottom + c.Margin.Bottom);
                    }
                }

                int tabsH = tabs1?.Height ?? 22;
                int innerH = Math.Max(26, maxBtnBottom);
                int hPreset = tabsH + innerH + 8;

                if (presetBrowserCard != null)
                {
                    presetBrowserCard.Height = hPreset;
                }

                UpdateFormSizeBasedOnPreviewState();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 布局] 调整工位自适应高度失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 初始化折叠条：100% 像素级还原 HTML 原型中左侧页码、右侧折叠提示双侧排布 (Panel + 双 Label，无任何遮挡)
        /// </summary>
        private void SetupCollapseBar()
        {
            if (pnlCollapseBar == null)
            {
                pnlCollapseBar = new System.Windows.Forms.Panel
                {
                    Name = "pnlCollapseBar",
                    Size = new Size(STATION_WIDTH, COLLAPSE_BAR_HEIGHT),
                    BackColor = Color.FromArgb(240, 242, 245),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left
                };

                pnlCollapseBar.Paint += (s, e) =>
                {
                    using (var pen = new Pen(Color.FromArgb(230, 235, 241), 1F))
                    {
                        e.Graphics.DrawLine(pen, 0, 0, pnlCollapseBar.Width, 0);
                    }
                };

                pnlCollapseBar.MouseEnter += (s, e) => pnlCollapseBar.BackColor = Color.FromArgb(230, 244, 255);
                pnlCollapseBar.MouseLeave += (s, e) => pnlCollapseBar.BackColor = Color.FromArgb(240, 242, 245);
                pnlCollapseBar.Click += (s, e) => TogglePreviewState();

                // 左侧页码标签
                lblCollapsePageNumber = new System.Windows.Forms.Label
                {
                    Text = string.IsNullOrEmpty(label1?.Text) ? "页码: —" : label1.Text,
                    Font = new Font("Microsoft YaHei UI", 9F),
                    ForeColor = Color.FromArgb(140, 149, 163),
                    Dock = DockStyle.Left,
                    Width = 100,
                    Padding = new Padding(10, 0, 0, 0),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                lblCollapsePageNumber.Click += (s, e) => TogglePreviewState();
                lblCollapsePageNumber.MouseEnter += (s, e) => pnlCollapseBar.BackColor = Color.FromArgb(230, 244, 255);
                lblCollapsePageNumber.MouseLeave += (s, e) => pnlCollapseBar.BackColor = Color.FromArgb(240, 242, 245);

                // 右侧折叠展开提示 (彻底去除 +245px 字样)
                lblCollapseArrow = new System.Windows.Forms.Label
                {
                    Text = _isPreviewExpanded ? "▲ 点击折叠预览" : "▼ 点击展开预览",
                    Font = new Font("Microsoft YaHei UI", 9F),
                    ForeColor = Color.FromArgb(140, 149, 163),
                    Dock = DockStyle.Right,
                    Width = 140,
                    Padding = new Padding(0, 0, 10, 0),
                    TextAlign = ContentAlignment.MiddleRight,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent
                };
                lblCollapseArrow.Click += (s, e) => TogglePreviewState();
                lblCollapseArrow.MouseEnter += (s, e) => pnlCollapseBar.BackColor = Color.FromArgb(230, 244, 255);
                lblCollapseArrow.MouseLeave += (s, e) => pnlCollapseBar.BackColor = Color.FromArgb(240, 242, 245);

                pnlCollapseBar.Controls.Add(lblCollapseArrow);
                pnlCollapseBar.Controls.Add(lblCollapsePageNumber);
            }
        }

        /// <summary>
        /// 隐藏旧版设计器中散落在根窗体上的冗余占位与旧标签控件
        /// </summary>
        private void HideLegacyControls()
        {
            try
            {
                var legacy = new Control[]
                {
                    materialTypeGroupBox, layoutModeGroupBox, orderNumberLabel, quantityLabel,
                    serialNumberLabel, dimensionsLabel, dimensionsTextBox, fileNameSeparator,
                    rowsDisplayLabel, columnsDisplayLabel, layoutCountDisplayLabel,
                    rotationDisplayLabel, pdfSizeDisplayLabel, label1, _rowsInput, _columnsInput,
                    enableSerialSearchCheckbox
                };

                foreach (var ctrl in legacy)
                {
                    if (ctrl != null)
                    {
                        ctrl.Visible = false;
                        ctrl.Location = new Point(-9999, -9999);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 工位] 隐藏旧版控件异常: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Uiverse 风格批量模式按钮：悬停时低饱和柔和浅蓝胶囊向右平滑展开，遮盖下方文字并使蓝色箭头居中。
    /// </summary>
    public class BatchModeSwitch : Control
    {
        private bool _isChecked;
        private bool _isHovered;
        private readonly System.Windows.Forms.Timer _animationTimer;
        private float _slideWidth = 24F;

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                AccessibleName = value ? "仅处理当前款" : "批量处理";
                AccessibleDefaultActionDescription = value ? "关闭批量处理" : "开启批量处理";
                Invalidate();
            }
        }

        public BatchModeSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.StandardClick, true);
            Size = new Size(104, 24);
            Font = new Font("Microsoft YaHei UI", 9F);
            Cursor = Cursors.Hand;
            AccessibleName = "批量处理";
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleDefaultActionDescription = "开启批量处理";
            _animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _animationTimer.Tick += AnimateSlide;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            _animationTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _animationTimer.Start();
        }

        private void AnimateSlide(object sender, EventArgs e)
        {
            float targetWidth = _isHovered ? (Width - 4F) : 24F;
            _slideWidth += (targetWidth - _slideWidth) * 0.12F;
            if (Math.Abs(targetWidth - _slideWidth) < 0.5F)
            {
                _slideWidth = targetWidth;
                _animationTimer.Stop();
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color borderColor = _isHovered ? Color.FromArgb(145, 202, 255) : Color.FromArgb(217, 217, 217);

            using (var buttonPath = MaterialSelectFormModern.CreateRoundedRectanglePath(
                new Rectangle(0, 0, Width - 1, Height - 1), 5))
            using (var buttonBrush = new SolidBrush(Color.White))
            using (var borderPen = new Pen(borderColor, 1F))
            {
                g.FillPath(buttonBrush, buttonPath);
                g.DrawPath(borderPen, buttonPath);
            }

            // 1. 先绘制文字：常态下文字清晰可见，当浅蓝滑块向右推开时被实心浅蓝滑块平滑覆盖遮蔽
            string label = _isChecked ? "仅处理当前款" : "批量处理";
            TextRenderer.DrawText(g, label, Font, new Rectangle(24, 0, Width - 26, Height),
                Color.FromArgb(51, 65, 85),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            // 2. 绘制低饱和浅蓝滑块：实心不透明浅蓝（#e6f4ff），平滑向右伸展覆盖整张按钮文字
            var slideRect = new RectangleF(2F, 3F, _slideWidth, Height - 6F);
            Color slideColor = _isHovered ? Color.FromArgb(215, 237, 255) : Color.FromArgb(230, 244, 255);
            using (var slidePath = MaterialSelectFormModern.CreateRoundedRectanglePath(
                Rectangle.Round(slideRect), 4))
            using (var slideBrush = new SolidBrush(slideColor))
            {
                g.FillPath(slideBrush, slidePath);
            }

            // 3. 绘制品牌蓝箭头（#1677ff）：随着滑块向右展开，箭头平滑移向滑块正中央
            float centerX = slideRect.Left + slideRect.Width / 2F;
            float centerY = slideRect.Top + slideRect.Height / 2F;
            DrawDirectionArrow(g, centerX, centerY, _isChecked);
        }

        private static void DrawDirectionArrow(Graphics g, float centerX, float centerY, bool pointsRight)
        {
            using (var pen = new Pen(Color.FromArgb(22, 119, 255), 1.6F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float left = centerX - 5F;
                float right = centerX + 5F;
                g.DrawLine(pen, left, centerY, right, centerY);
                if (pointsRight)
                {
                    g.DrawLine(pen, right, centerY, right - 4F, centerY - 4F);
                    g.DrawLine(pen, right, centerY, right - 4F, centerY + 4F);
                }
                else
                {
                    g.DrawLine(pen, left, centerY, left + 4F, centerY - 4F);
                    g.DrawLine(pen, left, centerY, left + 4F, centerY + 4F);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 按目录层级逐段着色的只读路径显示控件。
    /// </summary>
    public class HierarchicalPathDisplay : Control
    {
        private string[] _segments = Array.Empty<string>();
        private readonly List<Color> _segmentColors = new List<Color>();
        private static readonly Color SeparatorColor = Color.FromArgb(140, 149, 163);

        public HierarchicalPathDisplay()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.White;
            ForeColor = Color.FromArgb(31, 36, 41);
            AccessibleRole = AccessibleRole.StaticText;
        }

        public void SetPath(string path, IList<Color> segmentColors)
        {
            path = path ?? string.Empty;
            Text = path;
            _segments = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            _segmentColors.Clear();
            for (int i = 0; i < _segments.Length; i++)
            {
                _segmentColors.Add(segmentColors != null && i < segmentColors.Count
                    ? segmentColors[i]
                    : ForeColor);
            }
            AccessibleName = string.IsNullOrWhiteSpace(path) ? "未选择输出目录" : path;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_segments.Length == 0) return;

            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
            int availableWidth = Math.Max(0, Width - 8);
            int separatorWidth = TextRenderer.MeasureText(g, "\\", Font, Size.Empty, flags).Width;
            var segmentWidths = new int[_segments.Length];
            int totalWidth = 0;
            for (int i = 0; i < _segments.Length; i++)
            {
                segmentWidths[i] = TextRenderer.MeasureText(g, _segments[i], Font, Size.Empty, flags).Width;
                totalWidth += segmentWidths[i] + (i > 0 ? separatorWidth : 0);
            }

            int startIndex = 0;
            int ellipsisWidth = TextRenderer.MeasureText(g, "…\\", Font, Size.Empty, flags).Width;
            while (startIndex < _segments.Length - 1 &&
                totalWidth + (startIndex > 0 ? ellipsisWidth : 0) > availableWidth)
            {
                totalWidth -= segmentWidths[startIndex];
                if (startIndex + 1 < _segments.Length) totalWidth -= separatorWidth;
                startIndex++;
            }

            int x = 4;
            if (startIndex > 0)
            {
                TextRenderer.DrawText(g, "…\\", Font, new Rectangle(x, 0, ellipsisWidth, Height),
                    SeparatorColor, flags);
                x += ellipsisWidth;
            }

            for (int i = startIndex; i < _segments.Length && x < Width - 4; i++)
            {
                if (i > startIndex)
                {
                    TextRenderer.DrawText(g, "\\", Font, new Rectangle(x, 0, separatorWidth, Height),
                        SeparatorColor, flags);
                    x += separatorWidth;
                }

                int remainingWidth = Width - 4 - x;
                var segmentFlags = flags;
                if (segmentWidths[i] > remainingWidth)
                {
                    segmentFlags |= TextFormatFlags.EndEllipsis;
                }
                TextRenderer.DrawText(g, _segments[i], Font,
                    new Rectangle(x, 0, remainingWidth, Height), _segmentColors[i], segmentFlags);
                x += Math.Min(segmentWidths[i], remainingWidth);
            }
        }
    }

    /// <summary>
    /// Ant Design 风格线性展开文件夹目录切换按钮 (FolderOpenOutlined)，统一全局单色线性图标语言。
    /// </summary>
    public class ColoredFolderBrowseButton : Control
    {
        private bool _isHovered;
        private bool _isPressed;
        private Bitmap _cachedFolderBitmap;
        private Color _cachedFolderColor = Color.Empty;

        private const string FolderOpenSvgTemplate = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""64 64 896 896"">
<path fill=""{0}"" d=""M928 444H820V330.4c0-17.7-14.3-32-32-32H473L389 186.2c-7.9-10.6-20.4-16.8-33.8-16.8H128c-17.7 0-32 14.3-32 32v576c0 17.7 14.3 32 32 32h768c17.7 0 32-14.3 32-32L928 444zm-768-208h177.3l84 112.2c7.9 10.6 20.4 16.8 33.8 16.8H756v106.6H160V236zm696 544H160l60.8-272h680.4l-65.2 272z""/>
</svg>";

        public ColoredFolderBrowseButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.StandardClick, true);
            Size = new Size(100, 22);
            Font = new Font("Microsoft YaHei UI", 8.5F);
            Cursor = Cursors.Hand;
            AccessibleName = "切换输出目录";
            AccessibleRole = AccessibleRole.PushButton;
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
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color background = _isPressed
                ? Color.FromArgb(230, 244, 255)
                : (_isHovered ? Color.FromArgb(240, 247, 255) : Color.White);
            Color border = _isHovered || _isPressed
                ? Color.FromArgb(145, 202, 255)
                : Color.FromArgb(217, 217, 217);
            Color textColor = _isHovered || _isPressed
                ? Color.FromArgb(22, 119, 255)
                : Color.FromArgb(51, 65, 85);

            using (var backgroundPath = MaterialSelectFormModern.CreateRoundedRectanglePath(
                new Rectangle(0, 0, Width - 1, Height - 1), 3))
            using (var backgroundBrush = new SolidBrush(background))
            using (var borderPen = new Pen(border, 1F))
            {
                g.FillPath(backgroundBrush, backgroundPath);
                g.DrawPath(borderPen, backgroundPath);
            }

            // 绘制 Ant Design 线性展开文件夹矢量图标 (FolderOpenOutlined)
            int iconSize = 13;
            int iconX = 6;
            int iconY = (Height - iconSize) / 2;
            var iconBounds = new Rectangle(iconX, iconY, iconSize, iconSize);

            DrawFolderIcon(g, iconBounds, textColor);

            TextRenderer.DrawText(
                g,
                "切换目录 ▾",
                Font,
                new Rectangle(23, 0, Width - 24, Height),
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }

        private void DrawFolderIcon(Graphics g, Rectangle bounds, Color color)
        {
            try
            {
                if (_cachedFolderBitmap == null || _cachedFolderColor != color)
                {
                    _cachedFolderBitmap?.Dispose();
                    _cachedFolderColor = color;

                    string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                    string svgXml = string.Format(FolderOpenSvgTemplate, hexColor);

                    var svgDoc = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(svgXml);
                    svgDoc.Width = bounds.Width;
                    svgDoc.Height = bounds.Height;
                    _cachedFolderBitmap = svgDoc.Draw();
                }

                if (_cachedFolderBitmap != null)
                {
                    g.DrawImage(_cachedFolderBitmap, bounds);
                    return;
                }
            }
            catch { }

            // GDI+ 线性展开文件夹兜底绘制
            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width;
            float h = bounds.Height;

            using (var pen = new Pen(color, 1.15f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                g.DrawLine(pen, x, y + 2, x + w * 0.4f, y + 2);
                g.DrawLine(pen, x + w * 0.4f, y + 2, x + w * 0.55f, y + 4);
                g.DrawLine(pen, x + w * 0.55f, y + 4, x + w - 2, y + 4);
                g.DrawLine(pen, x + w - 2, y + 4, x + w - 2, y + h - 1);
                g.DrawLine(pen, x, y + 2, x, y + h - 1);

                using (var path = new GraphicsPath())
                {
                    path.AddPolygon(new[]
                    {
                        new PointF(x, y + h - 1),
                        new PointF(x + 2, y + 5),
                        new PointF(x + w, y + 5),
                        new PointF(x + w - 2, y + h - 1)
                    });
                    g.DrawPath(pen, path);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cachedFolderBitmap?.Dispose();
                _cachedFolderBitmap = null;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 自动排版图标开关：3×3 网格直接表达拼版语义，避免长复选框文字占用控制行。
    /// </summary>
    public class ImpositionToggleButton : Control
    {
        private bool _isActive;
        private bool _isHovered;

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) return;
                _isActive = value;
                AccessibleDefaultActionDescription = value ? "停用自动排版" : "启用自动排版";
                Invalidate();
            }
        }

        public ImpositionToggleButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(28, 24);
            Cursor = Cursors.Hand;
            AccessibleName = "自动排版";
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleDefaultActionDescription = "启用自动排版";
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
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Color background = _isActive
                ? Color.FromArgb(22, 119, 255)
                : (_isHovered ? Color.FromArgb(240, 247, 255) : Color.White);
            Color border = _isActive || _isHovered
                ? Color.FromArgb(22, 119, 255)
                : Color.FromArgb(211, 217, 226);
            Color gridColor = _isActive ? Color.White : Color.FromArgb(92, 99, 112);

            using (var backgroundBrush = new SolidBrush(background))
            using (var borderPen = new Pen(border, 1F))
            {
                g.FillRectangle(backgroundBrush, 0, 0, Width, Height);
                g.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
            }

            const int cellSize = 3;
            const int gap = 2;
            int gridSize = cellSize * 3 + gap * 2;
            int startX = (Width - gridSize) / 2;
            int startY = (Height - gridSize) / 2;
            using (var gridBrush = new SolidBrush(gridColor))
            {
                for (int row = 0; row < 3; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        g.FillRectangle(
                            gridBrush,
                            startX + column * (cellSize + gap),
                            startY + row * (cellSize + gap),
                            cellSize,
                            cellSize);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 标识页图标开关：采用折角文档线性矢量图标，首尾插入大版标识页。
    /// 与自动排版图标按钮形成左右对称与视觉闭环。
    /// </summary>
    public class IdentifierPageToggleButton : Control
    {
        private bool _isActive;
        private bool _isHovered;
        private bool _isPressed;
        private Bitmap _cachedIconBitmap;
        private Color _cachedIconColor = Color.Empty;

        private const string IdentifierSvgTemplate = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""64 64 896 896"">
<path fill=""{0}"" d=""M854.6 288.6L639.4 73.4c-6-6-14.1-9.4-22.6-9.4H192c-17.7 0-32 14.3-32 32v832c0 17.7 14.3 32 32 32h640c17.7 0 32-14.3 32-32V311.3c0-8.6-3.4-16.7-9.4-22.7zM790.2 326H602V137.8L790.2 326zM224 896V128h320v224c0 17.7 14.3 32 32 32h224v512H224z""/>
<path fill=""{0}"" d=""M320 448h384v64H320zm0 144h384v64H320zm0 144h256v64H320z""/>
</svg>";

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive == value) return;
                _isActive = value;
                AccessibleDefaultActionDescription = value ? "停用大版标识页" : "启用大版标识页";
                Invalidate();
            }
        }

        public IdentifierPageToggleButton()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            Size = new Size(26, 24);
            Cursor = Cursors.Hand;
            AccessibleName = "标识页";
            AccessibleRole = AccessibleRole.CheckButton;
            AccessibleDefaultActionDescription = "启用大版标识页";
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
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 无边框设计：仅在激活或交互时呈现轻盈圆角背景底色，不绘制任何边框线
            Color background = _isActive
                ? (_isPressed ? Color.FromArgb(215, 237, 255) : (_isHovered ? Color.FromArgb(230, 244, 255) : Color.FromArgb(240, 247, 255)))
                : (_isPressed ? Color.FromArgb(235, 238, 242) : (_isHovered ? Color.FromArgb(245, 247, 250) : Color.Transparent));

            // 图标颜色：激活状态为高亮主色蓝，关闭时为中性静默灰
            Color iconColor = _isActive
                ? Color.FromArgb(22, 119, 255)
                : Color.FromArgb(145, 150, 160);

            if (background != Color.Transparent)
            {
                using (var path = MaterialSelectFormModern.CreateRoundedRectanglePath(rect, 4))
                using (var brush = new SolidBrush(background))
                {
                    g.FillPath(brush, path);
                }
            }

            // 图标尺寸稍微放大至 16px（原为 13px），辨识度更高
            int iconSize = 16;
            int iconX = (Width - iconSize) / 2;
            int iconY = (Height - iconSize) / 2;
            var iconBounds = new Rectangle(iconX, iconY, iconSize, iconSize);

            DrawIdentifierIcon(g, iconBounds, iconColor);
        }

        private void DrawIdentifierIcon(Graphics g, Rectangle bounds, Color color)
        {
            try
            {
                if (_cachedIconBitmap == null || _cachedIconColor != color)
                {
                    _cachedIconBitmap?.Dispose();
                    _cachedIconColor = color;

                    string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                    string svgXml = string.Format(IdentifierSvgTemplate, hexColor);

                    var svgDoc = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(svgXml);
                    svgDoc.Width = bounds.Width;
                    svgDoc.Height = bounds.Height;
                    _cachedIconBitmap = svgDoc.Draw();
                }

                if (_cachedIconBitmap != null)
                {
                    g.DrawImage(_cachedIconBitmap, bounds);
                    return;
                }
            }
            catch
            {
                // Fallback GDI+
            }

            // GDI+ 矢量兜底绘制
            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width;
            float h = bounds.Height;

            using (var pen = new Pen(color, 1.15f))
            {
                pen.StartCap = LineCap.Flat;
                pen.EndCap = LineCap.Flat;
                pen.LineJoin = LineJoin.Round;

                float fold = w * 0.28f;
                using (var path = new GraphicsPath())
                {
                    path.AddLine(x + 1, y, x + w - fold, y);
                    path.AddLine(x + w - fold, y, x + w - 1, y + fold);
                    path.AddLine(x + w - 1, y + fold, x + w - 1, y + h - 1);
                    path.AddLine(x + w - 1, y + h - 1, x + 1, y + h - 1);
                    path.CloseFigure();
                    g.DrawPath(pen, path);
                }

                g.DrawLine(pen, x + w - fold, y, x + w - fold, y + fold);
                g.DrawLine(pen, x + w - fold, y + fold, x + w - 1, y + fold);

                g.DrawLine(pen, x + 3, y + h * 0.45f, x + w - 3, y + h * 0.45f);
                g.DrawLine(pen, x + 3, y + h * 0.65f, x + w - 3, y + h * 0.65f);
                g.DrawLine(pen, x + 3, y + h * 0.85f, x + w * 0.7f, y + h * 0.85f);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cachedIconBitmap?.Dispose();
                _cachedIconBitmap = null;
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// 组合输入容器 (InputGroup)：左侧浅灰语义标签 + 右侧无缝内嵌控件
    /// 100% 像素级实现 HTML 原型 .order-group-box
    /// </summary>
    public class InputGroupBox : Control
    {
        private string _prefixText = "";
        private const int PrefixWidth = 34;

        public string PrefixText
        {
            get => _prefixText;
            set
            {
                if (_prefixText != value)
                {
                    _prefixText = value ?? "";
                    Invalidate();
                }
            }
        }

        public InputGroupBox(string prefixText)
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            _prefixText = prefixText ?? "";
            Size = new Size(163, 25);
            BackColor = Color.White;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color borderColor = Color.FromArgb(211, 217, 226);
            Color tagBg = Color.FromArgb(241, 245, 249);
            Color dividerColor = Color.FromArgb(226, 232, 240);
            Color textColor = Color.FromArgb(71, 85, 105);

            // 1. 整体圆角背景与外边框
            using (var path = MaterialSelectFormModern.CreateRoundedRectanglePath(rect, 4))
            using (var brush = new SolidBrush(Color.White))
            using (var pen = new Pen(borderColor, 1F))
            {
                g.FillPath(brush, path);

                // 2. 左侧前缀标签背景 (裁切左侧区域保持左半圆角)
                g.SetClip(new Rectangle(0, 0, PrefixWidth, Height));
                using (var tagBrush = new SolidBrush(tagBg))
                {
                    g.FillPath(tagBrush, path);
                }
                g.ResetClip();

                // 3. 垂直分割线
                using (var divPen = new Pen(dividerColor, 1F))
                {
                    g.DrawLine(divPen, PrefixWidth, 0, PrefixWidth, Height - 1);
                }

                // 4. 外边框
                g.DrawPath(pen, path);
            }

            // 5. 前缀标签文本
            using (var font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    g,
                    _prefixText,
                    font,
                    new Rectangle(0, 0, PrefixWidth, Height),
                    textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            }
        }
    }

    /// <summary>
    /// 序号反查矢量搜索按钮 (精美 GDI+ 矢量放大镜图标，内嵌式无缝后缀按钮)
    /// </summary>
    public class SerialSearchButton : Control
    {
        private bool _isSearchActive;
        private bool _isHovered;

        public bool IsSearchActive
        {
            get => _isSearchActive;
            set
            {
                if (_isSearchActive != value)
                {
                    _isSearchActive = value;
                    Invalidate();
                }
            }
        }

        public SerialSearchButton()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(24, 23);
            BackColor = Color.White;
            Cursor = Cursors.Hand;
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
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 1. 无外边框内嵌：常态背景透明，激活或悬停时呈现轻柔圆角背景
            Color bg = _isHovered
                ? (_isSearchActive ? Color.FromArgb(215, 237, 255) : Color.FromArgb(241, 245, 249))
                : (_isSearchActive ? Color.FromArgb(230, 244, 255) : Color.Transparent);

            Color iconColor = _isSearchActive ? Color.FromArgb(22, 119, 255) : Color.FromArgb(140, 149, 163);

            if (bg != Color.Transparent)
            {
                using (var path = MaterialSelectFormModern.CreateRoundedRectanglePath(new Rectangle(1, 1, Width - 3, Height - 3), 3))
                using (var brush = new SolidBrush(bg))
                {
                    g.FillPath(brush, path);
                }
            }

            // 2. 矢量绘制精美小放大镜
            float cx = Width * 0.44f;
            float cy = Height * 0.44f;
            float r = 4.2f;

            using (var penIcon = new Pen(iconColor, 1.8F))
            {
                g.DrawEllipse(penIcon, cx - r, cy - r, r * 2, r * 2);

                float handleStartX = cx + (float)(r * Math.Cos(Math.PI / 4));
                float handleStartY = cy + (float)(r * Math.Sin(Math.PI / 4));
                float handleEndX = handleStartX + 4.5f;
                float handleEndY = handleStartY + 4.5f;
                penIcon.StartCap = LineCap.Round;
                penIcon.EndCap = LineCap.Round;
                g.DrawLine(penIcon, handleStartX, handleStartY, handleEndX, handleEndY);
            }
        }
    }

    /// <summary>
    /// 100% 像素级还原 HTML 原型 .mini-segmented 的微型分段胶囊控件
    /// 高度 20px，细灰框 3px圆角，选中项为浅蓝底(#e6f4ff) + 蓝字加粗(#1677ff)，未选中项为白底灰字，中间带浅灰分割线
    /// </summary>
    public class MiniSegmentedControl : Control
    {
        private int _selectedIndex = 0;
        public string Item1 { get; set; } = "";
        public string Item2 { get; set; } = "";

        public event EventHandler SelectedIndexChanged;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = Math.Max(0, Math.Min(1, value));
                if (_selectedIndex != clamped)
                {
                    _selectedIndex = clamped;
                    Invalidate();
                    SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public MiniSegmentedControl(string item1, string item2)
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Item1 = item1;
            Item2 = item2;
            Size = new Size(56, 20);
            BackColor = Color.White;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int half = Width / 2;
            SelectedIndex = (e.X < half) ? 0 : 1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int w = Width - 1;
            int h = Height - 1;
            int half = Width / 2;

            // 1. 填充白底背景
            using (var bgBrush = new SolidBrush(Color.White))
            {
                g.FillRectangle(bgBrush, 0, 0, Width, Height);
            }

            // 2. 绘制选中项高亮底色 (浅蓝底 #e6f4ff)
            var activeRect = (_selectedIndex == 0)
                ? new Rectangle(1, 1, half - 1, Height - 2)
                : new Rectangle(half + 1, 1, Width - half - 2, Height - 2);

            using (var activeBrush = new SolidBrush(Color.FromArgb(230, 244, 255)))
            {
                g.FillRectangle(activeBrush, activeRect);
            }

            // 3. 中间细竖分割线 (#e6ebf1)
            using (var sepPen = new Pen(Color.FromArgb(230, 235, 241), 1F))
            {
                g.DrawLine(sepPen, half, 2, half, h - 1);
            }

            // 4. 外边框 (细灰框 #d3d9e2)
            using (var borderPen = new Pen(Color.FromArgb(211, 217, 226), 1F))
            {
                g.DrawRectangle(borderPen, 0, 0, w, h);
            }

            // 5. 绘制两项文字 (选中项 #1677ff 蓝字加粗，未选中项 #5c6370 正常灰字)
            var rect1 = new RectangleF(0, 0, half, Height);
            var rect2 = new RectangleF(half, 0, Width - half, Height);

            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                bool isItem1Active = (_selectedIndex == 0);
                using (var font1 = new Font("Microsoft YaHei UI", 9F, isItem1Active ? FontStyle.Bold : FontStyle.Regular))
                using (var brush1 = new SolidBrush(isItem1Active ? Color.FromArgb(22, 119, 255) : Color.FromArgb(92, 99, 112)))
                {
                    g.DrawString(Item1, font1, brush1, rect1, sf);
                }

                bool isItem2Active = (_selectedIndex == 1);
                using (var font2 = new Font("Microsoft YaHei UI", 9F, isItem2Active ? FontStyle.Bold : FontStyle.Regular))
                using (var brush2 = new SolidBrush(isItem2Active ? Color.FromArgb(22, 119, 255) : Color.FromArgb(92, 99, 112)))
                {
                    g.DrawString(Item2, font2, brush2, rect2, sf);
                }
            }
        }
    }

    /// <summary>
    /// 当平张纸张尺寸或卷装材料幅宽与系统设置不一致时浮现的微型胶囊指示器，点击可直接调出大版开料参数
    /// </summary>
    /// <summary>
    /// 当平张纸张尺寸或卷装材料高度/幅宽与系统设置不一致时浮现的微型胶囊指示器，点击可直接调出大版开料参数
    /// </summary>
    public class CustomSizeCapsule : Control
    {
        private bool _isHovered;
        private bool _isPressed;
        private string _capsuleText = "";
        private bool _isRoll = false;
        private Bitmap _cachedRollIconBitmap;
        private Color _cachedRollColor = Color.Empty;

        private const string RollSvgTemplate = @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""2.19 2.59 981 997"">
<path fill=""{0}"" d=""M983.004 314.501c-25.735-60.845-62.541-115.407-109.422-162.287C826.7 105.333 772.04 68.526 711.294 42.79 648.254 16.16 581.324 2.593 512.399 2.593S376.544 16.06 313.604 42.692c-60.846 25.734-115.407 62.54-162.288 109.422-46.881 46.88-83.688 101.542-109.422 162.288-26.633 63.04-40.198 129.97-40.198 198.895s13.466 135.855 40.098 198.795c17.156 40.298 39 77.902 65.533 112.315 10.773 14.064 32.318 12.867 41.495-2.294 5.287-8.778 4.788-20.05-1.496-28.229-59.948-77.802-95.557-175.155-95.557-280.687-0.1-255.551 206.276-461.329 461.927-460.63C766.753 53.265 972.43 258.943 973.129 512c0.698 254.653-203.583 460.73-458.236 462.127h-0.5v-0.599H27.132c-13.765 0-24.937 11.172-24.937 24.937s11.172 24.937 24.937 24.937h459.633c8.578 0.399 17.057 0.598 25.735 0.598 68.925 0 135.855-13.466 198.795-40.098 60.845-25.735 115.407-62.541 162.288-109.422 46.88-46.881 83.687-101.543 109.422-162.288 26.632-62.94 40.098-129.87 40.098-198.796 0-69.024-13.466-135.954-40.098-198.895z""/>
<path fill=""{0}"" d=""M512.399 727.752c-57.255 0-111.118-22.343-151.615-62.84-40.497-40.497-62.84-94.36-62.84-151.615 0-57.255 22.343-111.118 62.84-151.615 40.497-40.498 94.36-62.84 151.615-62.84 57.255 0 111.118 22.342 151.615 62.84 40.497 40.497 62.84 94.36 62.84 151.615 0 57.254-22.343 111.118-62.84 151.615-40.497 40.497-94.36 62.84-151.615 62.84z m0-379.037c-90.77 0-164.582 73.812-164.582 164.582s73.812 164.582 164.582 164.582 164.582-73.813 164.582-164.582S603.17 348.715 512.4 348.715z""/>
</svg>";

        public string CapsuleText
        {
            get => _capsuleText;
            set
            {
                if (_capsuleText != value)
                {
                    _capsuleText = value ?? "";
                    AdjustSize();
                    Invalidate();
                }
            }
        }

        public bool IsRoll
        {
            get => _isRoll;
            set
            {
                if (_isRoll != value)
                {
                    _isRoll = value;
                    AdjustSize();
                    Invalidate();
                }
            }
        }

        public CustomSizeCapsule()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            Size = new Size(74, 24);
            Font = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Bold);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        private void AdjustSize()
        {
            if (string.IsNullOrWhiteSpace(_capsuleText)) return;
            try
            {
                int textW = TextRenderer.MeasureText(_capsuleText, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
                int iconSize = 13;
                int gap = 3;
                int padding = 12;
                int targetW = Math.Max(52, Math.Min(74, textW + iconSize + gap + padding));
                if (Width != targetW)
                {
                    Width = targetW;
                }
            }
            catch { }
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
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_capsuleText)) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 平张使用主色蓝系，卷装使用琥珀橙系
            Color bg;
            Color border;
            Color text;

            if (_isRoll)
            {
                bg = _isPressed ? Color.FromArgb(253, 230, 138)
                    : (_isHovered ? Color.FromArgb(254, 235, 175) : Color.FromArgb(254, 243, 199));
                border = Color.FromArgb(252, 211, 77);
                text = Color.FromArgb(180, 83, 9);
            }
            else
            {
                bg = _isPressed ? Color.FromArgb(186, 231, 255)
                    : (_isHovered ? Color.FromArgb(215, 237, 255) : Color.FromArgb(230, 244, 255));
                border = Color.FromArgb(145, 202, 255);
                text = Color.FromArgb(2, 132, 199);
            }

            using (var path = MaterialSelectFormModern.CreateRoundedRectanglePath(rect, 3))
            using (var brush = new SolidBrush(bg))
            using (var pen = new Pen(border, 1F))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // 计算整体内容（图标 + 间距 + 文本）在胶囊内部的绝对水平光学居中
            int iconSize = 13;
            int gap = 3;
            Size textSize = TextRenderer.MeasureText(g, _capsuleText, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            int totalContentW = iconSize + gap + textSize.Width;
            int startX = Math.Max(2, (Width - totalContentW) / 2);
            int iconY = (Height - iconSize) / 2;

            var iconBounds = new Rectangle(startX, iconY, iconSize, iconSize);

            if (_isRoll)
            {
                DrawRollIcon(g, iconBounds, text);
            }
            else
            {
                DrawFlatSheetIcon(g, iconBounds, text);
            }

            // 绘制文本 (紧随图标之后，垂直严格居中)
            int textX = startX + iconSize + gap;
            var textRect = new Rectangle(textX, 0, Width - textX - 1, Height);
            TextRenderer.DrawText(
                g,
                _capsuleText,
                Font,
                textRect,
                text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
        }

        private void DrawFlatSheetIcon(Graphics g, Rectangle bounds, Color color)
        {
            float x = bounds.X;
            float y = bounds.Y;
            float w = bounds.Width;
            float h = bounds.Height;

            using (var pen = new Pen(color, 1.15f))
            {
                pen.StartCap = LineCap.Flat;
                pen.EndCap = LineCap.Flat;
                pen.LineJoin = LineJoin.Round;

                // 顶部宽标尺 |---| (居中对齐)
                float rulerY = y + h * 0.14f;
                float rulerLeft = x + w * 0.32f;
                float rulerRight = x + w * 0.95f;
                float tickH = h * 0.10f;
                g.DrawLine(pen, rulerLeft, rulerY, rulerRight, rulerY);
                g.DrawLine(pen, rulerLeft, rulerY - tickH, rulerLeft, rulerY + tickH);
                g.DrawLine(pen, rulerRight, rulerY - tickH, rulerRight, rulerY + tickH);

                // 左侧高标尺 |--|
                float rulerX = x + w * 0.14f;
                float rulerTop = y + h * 0.32f;
                float rulerBottom = y + h * 0.95f;
                float tickW = w * 0.10f;
                g.DrawLine(pen, rulerX, rulerTop, rulerX, rulerBottom);
                g.DrawLine(pen, rulerX - tickW, rulerTop, rulerX + tickW, rulerTop);
                g.DrawLine(pen, rulerX - tickW, rulerBottom, rulerX + tickW, rulerBottom);

                // 折角印张本体
                float paperLeft = x + w * 0.32f;
                float paperTop = y + h * 0.32f;
                float paperRight = x + w * 0.95f;
                float paperBottom = y + h * 0.95f;
                float foldSize = w * 0.25f;

                using (var path = new GraphicsPath())
                {
                    path.AddLine(paperLeft, paperTop, paperRight - foldSize, paperTop);
                    path.AddLine(paperRight - foldSize, paperTop, paperRight, paperTop + foldSize);
                    path.AddLine(paperRight, paperTop + foldSize, paperRight, paperBottom);
                    path.AddLine(paperRight, paperBottom, paperLeft, paperBottom);
                    path.CloseFigure();
                    g.DrawPath(pen, path);
                }

                // 折角内部线条
                g.DrawLine(pen, paperRight - foldSize, paperTop, paperRight - foldSize, paperTop + foldSize);
                g.DrawLine(pen, paperRight - foldSize, paperTop + foldSize, paperRight, paperTop + foldSize);
            }
        }

        private void DrawRollIcon(Graphics g, Rectangle bounds, Color color)
        {
            try
            {
                if (_cachedRollIconBitmap == null || _cachedRollColor != color)
                {
                    _cachedRollIconBitmap?.Dispose();
                    _cachedRollColor = color;

                    string hexColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                    string svgXml = string.Format(RollSvgTemplate, hexColor);

                    var svgDoc = Svg.SvgDocument.FromSvg<Svg.SvgDocument>(svgXml);
                    svgDoc.Width = bounds.Width;
                    svgDoc.Height = bounds.Height;
                    _cachedRollIconBitmap = svgDoc.Draw();
                }

                if (_cachedRollIconBitmap != null)
                {
                    g.DrawImage(_cachedRollIconBitmap, bounds);
                    return;
                }
            }
            catch
            {
                // Fallback GDI+
            }

            // GDI+ Fallback 兜底绘制
            using (var brush = new SolidBrush(color))
            using (var pen = new Pen(color, 1.2f))
            {
                g.DrawEllipse(pen, bounds.X + 2, bounds.Y + 1, bounds.Width - 4, bounds.Height - 4);
                g.DrawLine(pen, bounds.X + bounds.Width / 2, bounds.Bottom - 2, bounds.X + 1, bounds.Bottom - 2);
                g.FillEllipse(brush, bounds.X + bounds.Width / 2 - 2, bounds.Y + bounds.Height / 2 - 2, 4, 4);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cachedRollIconBitmap?.Dispose();
                _cachedRollIconBitmap = null;
            }
            base.Dispose(disposing);
        }
    }
}
