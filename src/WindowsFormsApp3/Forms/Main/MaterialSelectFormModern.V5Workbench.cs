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

        // 7 大白底细线框卡片容器
        private System.Windows.Forms.Panel pnlMaterialsCard;
        private System.Windows.Forms.Panel pnlOutputPathCard;
        private System.Windows.Forms.Panel pnlCraftMatrixCard;
        private System.Windows.Forms.Panel tlpOrderParametersCard;
        private System.Windows.Forms.Panel pnlImpositionDashboardCard;
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
        private BatchModeSwitch btnBatchModeSwitch;

        // 象限 4: 一式N联控件 (去“一式”文字，联/份改为微型切换按钮胶囊)
        private System.Windows.Forms.Panel pnlCopyCountWrap;
        private System.Windows.Forms.TextBox txtCopyCountInput;
        private MiniSegmentedControl segCopyType;
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
        /// 初始化 V5 右侧流水线工位架构（将旧控件整理收敛入 7 大白底细框卡片）
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

                // 构建 7 大卡片并加入流式面板
                BuildMaterialsCard();
                BuildOutputPathCard();
                BuildCraftMatrixCard();
                BuildOrderParametersCard();
                BuildImpositionDashboardCard();
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
        /// 卡片 4: 2×2 工单参数舱卡片 (订单号加宽至118px + 下拉加宽至62px · 序号限定5位数宽度48px + GDI+矢量放大镜按钮 · 58px 高)
        /// </summary>
        private void BuildOrderParametersCard()
        {
            tlpOrderParametersCard = CreateWhiteCardPanel("tlpOrderParametersCard", 58);
            tlpOrderParametersCard.Padding = new Padding(4, 3, 4, 3);
            tlpOrderParametersCard.Margin = new Padding(0, 0, 0, 4);

            // ----------------------------------------------------
            // 第一行：[订单组合 (加宽至210px)] + [序号组合 (紧凑5位数 118px)] (23px 高)
            // ----------------------------------------------------
            var pnlRow1 = new System.Windows.Forms.Panel
            {
                Location = new Point(4, 4),
                Size = new Size(332, 23),
                BackColor = Color.Transparent
            };

            // (1) 订单组合区 (占用左侧 210px，输入框 116px + 下拉框 60px，长单号与完整文字极度舒展)
            var pnlOrderGroup = new System.Windows.Forms.Panel
            {
                Location = new Point(0, 0),
                Size = new Size(210, 23),
                BackColor = Color.Transparent
            };
            var lblO = new System.Windows.Forms.Label
            {
                Text = "订单",
                Location = new Point(0, 0),
                Size = new Size(32, 23),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(92, 99, 112),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlOrderGroup.Controls.Add(lblO);

            if (orderNumberTextBox != null)
            {
                orderNumberTextBox.Location = new Point(32, 0);
                orderNumberTextBox.Size = new Size(104, 23);
                orderNumberTextBox.Font = new Font("Microsoft YaHei UI", 9F);
                orderNumberTextBox.Radius = 3;
                pnlOrderGroup.Controls.Add(orderNumberTextBox);
            }

            if (btnOrderNumberMode != null)
            {
                btnOrderNumberMode.Location = new Point(138, 0);
                btnOrderNumberMode.Size = new Size(72, 23);
                btnOrderNumberMode.Radius = 3;
                btnOrderNumberMode.Font = new Font("Microsoft YaHei UI", 9F);
                pnlOrderGroup.Controls.Add(btnOrderNumberMode);
            }
            pnlRow1.Controls.Add(pnlOrderGroup);

            // (2) 序号组合区 (占用右侧 118px，序号框仅保留容纳5位数宽度 46px，后接精美矢量放大镜反查按钮)
            var pnlSerialGroup = new System.Windows.Forms.Panel
            {
                Location = new Point(212, 0),
                Size = new Size(118, 23),
                BackColor = Color.Transparent
            };
            var lblS = new System.Windows.Forms.Label
            {
                Text = "序号",
                Location = new Point(0, 0),
                Size = new Size(32, 23),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(92, 99, 112),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlSerialGroup.Controls.Add(lblS);

            if (serialNumberTextBox != null)
            {
                serialNumberTextBox.Location = new Point(32, 0);
                serialNumberTextBox.Size = new Size(46, 23);
                serialNumberTextBox.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
                serialNumberTextBox.TextAlign = HorizontalAlignment.Left;
                serialNumberTextBox.Radius = 3;
                serialNumberTextBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                        PerformSerialLookup(true);
                    }
                };
                pnlSerialGroup.Controls.Add(serialNumberTextBox);
            }

            btnSearchSerialAction = new SerialSearchButton
            {
                Location = new Point(80, 0),
                Size = new Size(28, 23),
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
            pnlSerialGroup.Controls.Add(btnSearchSerialAction);
            pnlRow1.Controls.Add(pnlSerialGroup);

            tlpOrderParametersCard.Controls.Add(pnlRow1);

            // ----------------------------------------------------
            // 第二行：[数量组合 (138px)] + [一式N联组合 (188px)] (23px 高)
            // ----------------------------------------------------
            var pnlRow2 = new System.Windows.Forms.Panel
            {
                Location = new Point(4, 30),
                Size = new Size(332, 23),
                BackColor = Color.Transparent
            };

            // (1) 数量区
            var pnlQty = new System.Windows.Forms.Panel
            {
                Location = new Point(0, 0),
                Size = new Size(138, 23),
                BackColor = Color.Transparent
            };
            var lblQ = new System.Windows.Forms.Label
            {
                Text = "数量",
                Location = new Point(0, 0),
                Size = new Size(32, 23),
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(92, 99, 112),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlQty.Controls.Add(lblQ);

            if (quantityTextBox != null)
            {
                quantityTextBox.Location = new Point(32, 0);
                quantityTextBox.Size = new Size(76, 23);
                quantityTextBox.Font = new Font("Microsoft YaHei UI", 9F);
                quantityTextBox.Radius = 3;
                pnlQty.Controls.Add(quantityTextBox);
            }
            if (incrementTextBox != null)
            {
                incrementTextBox.Location = new Point(110, 0);
                incrementTextBox.Size = new Size(26, 23);
                incrementTextBox.Font = new Font("Microsoft YaHei UI", 9F);
                incrementTextBox.Radius = 3;
                pnlQty.Controls.Add(incrementTextBox);
            }
            pnlRow2.Controls.Add(pnlQty);

            // (2) 一式N联区 (去“一式”文字 · 联/份改为微型切换按钮胶囊 · 自适应独立下拉)
            var pnlQuadrant4 = new System.Windows.Forms.Panel
            {
                Location = new Point(142, 0),
                Size = new Size(190, 23),
                BackColor = Color.Transparent
            };

            // 纯复选框方块 [√] (清除“一式”文字，仅保留纯净勾选框，宽 18px)
            if (duplicateLayoutCheckbox != null)
            {
                duplicateLayoutCheckbox.Location = new Point(0, 1);
                duplicateLayoutCheckbox.Size = new Size(18, 22);
                duplicateLayoutCheckbox.Text = "";
                duplicateLayoutCheckbox.CheckedChanged += (s, e) => UpdateJoinGroupEnabledState(duplicateLayoutCheckbox.Checked);
                pnlQuadrant4.Controls.Add(duplicateLayoutCheckbox);
            }

            // 联数数字输入框 [ 2 ] (独立白底细框，3px圆角，宽 26px，高 21px，Consolas 8.5pt Bold 居中)
            pnlCopyCountWrap = new System.Windows.Forms.Panel
            {
                Location = new Point(22, 1),
                Size = new Size(26, 21),
                BackColor = Color.White
            };
            pnlCopyCountWrap.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnlCopyCountWrap.Width - 1, pnlCopyCountWrap.Height - 1), 3))
                using (var pen = new Pen(CardBorderColor, 1F))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            txtCopyCountInput = new System.Windows.Forms.TextBox
            {
                Location = new Point(1, 2),
                Size = new Size(24, 17),
                BorderStyle = BorderStyle.None,
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 36, 41),
                BackColor = Color.White,
                Text = _copyCount.ToString()
            };
            txtCopyCountInput.TextChanged += (s, e) =>
            {
                if (int.TryParse(txtCopyCountInput.Text, out int val) && val >= 2 && val <= 16)
                {
                    _copyCount = val;
                    if (copyCountNumericUpDown != null) copyCountNumericUpDown.Value = val;
                }
            };
            pnlCopyCountWrap.Controls.Add(txtCopyCountInput);
            pnlQuadrant4.Controls.Add(pnlCopyCountWrap);

            // 联/份微型分段切换胶囊 [ 联 | 份 ] (宽 46px，高 21px，点击直接切换，浅蓝底加粗高亮)
            segCopyType = new MiniSegmentedControl("联", "份")
            {
                Location = new Point(51, 1),
                Size = new Size(46, 21),
                SelectedIndex = (_copyType == CopyType.Duplicate) ? 1 : 0
            };
            segCopyType.SelectedIndexChanged += (s, e) =>
            {
                if (duplicateLayoutCheckbox?.Checked != true) return;
                _copyType = (segCopyType.SelectedIndex == 1) ? CopyType.Duplicate : CopyType.Layout;
                if (copyTypeComboBox != null) copyTypeComboBox.SelectedIndex = (int)_copyType;
                OnImpositionSettingsChanged(s, e);
            };
            pnlQuadrant4.Controls.Add(segCopyType);

            // 排版模式下拉 [ 自适应 ▾ ] (3px微圆角细框，宽 74px，舒展展开，文字完全不截断)
            var pnlModeWrap = new System.Windows.Forms.Panel
            {
                Location = new Point(100, 1),
                Size = new Size(74, 21),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            pnlModeWrap.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, pnlModeWrap.Width - 1, pnlModeWrap.Height - 1), 3))
                using (var pen = new Pen(CardBorderColor, 1F))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };
            lblCopyModePill = new System.Windows.Forms.Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = Color.FromArgb(75, 82, 93),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Text = GetCopyModePillText(_copyMode)
            };

            var cmsCopyMode = new System.Windows.Forms.ContextMenuStrip();
            cmsCopyMode.Items.Add("自适应", null, (s, e) =>
            {
                _copyMode = CopyMode.AutoByColumn;
                lblCopyModePill.Text = "自适应 ▾";
                if (copyModeComboBox != null) copyModeComboBox.SelectedIndex = 0;
                OnImpositionSettingsChanged(s, e);
            });
            cmsCopyMode.Items.Add("列优先", null, (s, e) =>
            {
                _copyMode = CopyMode.AutoByRow;
                lblCopyModePill.Text = "列优先 ▾";
                if (copyModeComboBox != null) copyModeComboBox.SelectedIndex = 1;
                OnImpositionSettingsChanged(s, e);
            });
            cmsCopyMode.Items.Add("不旋转", null, (s, e) =>
            {
                _copyMode = CopyMode.FixedNoRotationByColumn;
                lblCopyModePill.Text = "不旋转 ▾";
                if (copyModeComboBox != null) copyModeComboBox.SelectedIndex = 2;
                OnImpositionSettingsChanged(s, e);
            });

            pnlModeWrap.Click += (s, e) =>
            {
                if (duplicateLayoutCheckbox?.Checked != true) return;
                cmsCopyMode.Show(pnlModeWrap, new Point(0, pnlModeWrap.Height));
            };
            lblCopyModePill.Click += (s, e) =>
            {
                if (duplicateLayoutCheckbox?.Checked != true) return;
                cmsCopyMode.Show(pnlModeWrap, new Point(0, pnlModeWrap.Height));
            };
            pnlModeWrap.Controls.Add(lblCopyModePill);
            pnlQuadrant4.Controls.Add(pnlModeWrap);

            pnlRow2.Controls.Add(pnlQuadrant4);
            tlpOrderParametersCard.Controls.Add(pnlRow2);

            UpdateJoinGroupEnabledState(duplicateLayoutCheckbox?.Checked ?? false);

            pnlStationContent.Controls.Add(tlpOrderParametersCard);
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
                };
                folderTreeViewToolTip?.SetToolTip(btnImpositionToggle, "启用或停用自动排版计算");
                pnlImpoControlRow.Controls.Add(btnImpositionToggle);
            }

            // 1. 材料模式微型分段胶囊 (平张 / 卷装 · 100% 像素级还原 HTML 原型 .mini-segmented)
            segMaterialType = new MiniSegmentedControl("平张", "卷装")
            {
                Location = new Point(100, 1),
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
            };
            pnlImpoControlRow.Controls.Add(segMaterialType);

            // 2. 排版模式微型分段胶囊 (连拼 / 折手 · 100% 像素级还原 HTML 原型 .mini-segmented)
            segLayoutMode = new MiniSegmentedControl("连拼", "折手")
            {
                Location = new Point(178, 1),
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
            };
            pnlImpoControlRow.Controls.Add(segLayoutMode);

            if (chkIdentifierPage != null)
            {
                chkIdentifierPage.Location = new Point(258, 1);
                chkIdentifierPage.Size = new Size(74, 24);
                chkIdentifierPage.Text = "标识页";
                chkIdentifierPage.Font = new Font("Microsoft YaHei UI", 9F);
                pnlImpoControlRow.Controls.Add(chkIdentifierPage);
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

            // 上行分组标签页 (纯白底色，无灰底)
            if (tabs1 != null)
            {
                tabs1.Dock = DockStyle.Top;
                tabs1.Height = 22;
                tabs1.BackColor = Color.White;
                presetBrowserCard.Controls.Add(tabs1);
            }

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
                {
                    Size titleSize = TextRenderer.MeasureText(g, label, fontTop, Size.Empty, centeredTextFlags);
                    Size valueSize = TextRenderer.MeasureText(g, val, fontVal, Size.Empty, centeredTextFlags);
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
                        DrawDimensionPillValue(g, pnl, valueBounds, valueSize, contentTop, titleSize, val, fontVal, valueColor, centeredTextFlags);
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
            TextRenderer.DrawText(g, val, fontVal, Rectangle.Round(valueBounds), valueColor, centeredTextFlags);

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
                _v5RotationText = $"{_currentImpositionResult?.RotationAngle ?? (_forceRotationEnabled ? 90 : 0)}";

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
                pnlPillDimension?.Invalidate();
                pnlPillBleed?.Invalidate();
                pnlPillColor?.Invalidate();
                tlpMetrics?.Invalidate(true);
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
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 看板] 更新模式分段控件状态失败: {ex.Message}", ex);
            }
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

        private static string GetCopyModePillText(CopyMode mode)
        {
            switch (mode)
            {
                case CopyMode.AutoByColumn: return "自适应 ▾";
                case CopyMode.AutoByRow: return "列优先 ▾";
                case CopyMode.FixedNoRotationByColumn:
                case CopyMode.FixedNoRotationByRow: return "不旋转 ▾";
                default: return "自适应 ▾";
            }
        }

        private void CycleCopyMode()
        {
            switch (_copyMode)
            {
                case CopyMode.AutoByColumn:
                    _copyMode = CopyMode.AutoByRow;
                    break;
                case CopyMode.AutoByRow:
                    _copyMode = CopyMode.FixedNoRotationByColumn;
                    break;
                default:
                    _copyMode = CopyMode.AutoByColumn;
                    break;
            }
            if (lblCopyModePill != null)
            {
                lblCopyModePill.Text = GetCopyModePillText(_copyMode);
            }
            if (copyModeComboBox != null)
            {
                copyModeComboBox.SelectedIndex = (int)_copyMode;
            }
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
                    lblCopyModePill.ForeColor = enabled ? Color.FromArgb(75, 82, 93) : Color.FromArgb(170, 170, 170);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[V5 参数舱] 更新一式N联状态异常: {ex.Message}", ex);
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
    /// Uiverse 风格批量模式按钮：悬停时蓝色色块扩展并覆盖文字。
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
            float targetWidth = _isHovered ? 98F : 24F;
            _slideWidth += (targetWidth - _slideWidth) * 0.10F;
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
            using (var buttonPath = MaterialSelectFormModern.CreateRoundedRectanglePath(
                new Rectangle(0, 0, Width - 1, Height - 1), 6))
            using (var buttonBrush = new SolidBrush(Color.White))
            using (var borderPen = new Pen(Color.FromArgb(217, 217, 217), 1F))
            {
                g.FillPath(buttonBrush, buttonPath);
                g.DrawPath(borderPen, buttonPath);
            }

            // 先绘制文字，再绘制滑块，保证悬停展开时色块覆盖文字。
            string label = _isChecked ? "仅处理当前款" : "批量处理";
            TextRenderer.DrawText(g, label, Font, new Rectangle(24, 0, Width - 26, Height),
                Color.FromArgb(31, 36, 41),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            var slideRect = new RectangleF(2F, 3F, _slideWidth, 18F);
            using (var slidePath = MaterialSelectFormModern.CreateRoundedRectanglePath(
                Rectangle.Round(slideRect), 5))
            using (var slideBrush = new SolidBrush(Color.FromArgb(22, 119, 255)))
            {
                g.FillPath(slideBrush, slidePath);
            }

            float centerX = slideRect.Left + slideRect.Width / 2F;
            float centerY = slideRect.Top + slideRect.Height / 2F;
            DrawDirectionArrow(g, centerX, centerY, _isChecked);
        }

        private static void DrawDirectionArrow(Graphics g, float centerX, float centerY, bool pointsRight)
        {
            using (var pen = new Pen(Color.White, 1.6F))
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
    /// 带稳定彩色文件夹图标的目录切换按钮，避免彩色 Emoji 被字体替换为单色字形。
    /// </summary>
    public class ColoredFolderBrowseButton : Control
    {
        private bool _isHovered;
        private bool _isPressed;

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
            Color textColor = _isHovered || _isPressed
                ? Color.FromArgb(22, 119, 255)
                : Color.FromArgb(51, 61, 71);

            using (var backgroundPath = MaterialSelectFormModern.CreateRoundedRectanglePath(
                new Rectangle(0, 0, Width - 1, Height - 1), 3))
            using (var backgroundBrush = new SolidBrush(background))
            {
                g.FillPath(backgroundBrush, backgroundPath);
            }

            // 多色打开文件夹：黄色文件夹 + 蓝色文件页，稳定还原 HTML 中 📂 的色彩语义。
            using (var folderBackPath = new GraphicsPath())
            {
                folderBackPath.AddPolygon(new[]
                {
                    new Point(6, 7), new Point(11, 7), new Point(13, 9),
                    new Point(21, 9), new Point(21, 17), new Point(6, 17)
                });
                using (var folderBrush = new SolidBrush(Color.FromArgb(255, 241, 184)))
                using (var folderPen = new Pen(Color.FromArgb(255, 229, 143), 1F))
                {
                    g.FillPath(folderBrush, folderBackPath);
                    g.DrawPath(folderPen, folderBackPath);
                }
            }

            using (var paperBrush = new SolidBrush(Color.FromArgb(230, 244, 255)))
            using (var paperPen = new Pen(Color.FromArgb(145, 202, 255), 1F))
            {
                g.FillRectangle(paperBrush, 10, 9, 8, 6);
                g.DrawRectangle(paperPen, 10, 9, 8, 6);
            }

            using (var folderFrontPath = new GraphicsPath())
            {
                folderFrontPath.AddPolygon(new[]
                {
                    new Point(6, 11), new Point(21, 11),
                    new Point(19, 18), new Point(7, 18)
                });
                using (var frontBrush = new SolidBrush(Color.FromArgb(255, 214, 102)))
                using (var frontPen = new Pen(Color.FromArgb(255, 197, 61), 1F))
                {
                    g.FillPath(frontBrush, folderFrontPath);
                    g.DrawPath(frontPen, folderFrontPath);
                }
            }

            TextRenderer.DrawText(
                g,
                "切换目录 ▾",
                Font,
                new Rectangle(25, 0, Width - 28, Height),
                textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
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
    /// 序号反查矢量搜索按钮 (精美 GDI+ 矢量放大镜图标，支持激活/未激活双态切换与即时反查)
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
            Size = new Size(28, 23);
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

            int w = Width - 1;
            int h = Height - 1;

            Color bg = _isSearchActive ? Color.FromArgb(230, 244, 255) : Color.White;
            Color border = _isHovered
                ? Color.FromArgb(22, 119, 255)
                : (_isSearchActive ? Color.FromArgb(145, 202, 255) : Color.FromArgb(212, 219, 228));
            Color iconColor = _isSearchActive ? Color.FromArgb(22, 119, 255) : Color.FromArgb(140, 149, 163);

            // 1. 填充微圆角背景
            using (var brush = new SolidBrush(bg))
            {
                g.FillRectangle(brush, 0, 0, Width, Height);
            }

            // 2. 绘制边框
            using (var pen = new Pen(border, 1F))
            {
                g.DrawRectangle(pen, 0, 0, w, h);
            }

            // 3. 矢量绘制精美小放大镜
            float cx = Width * 0.40f;
            float cy = Height * 0.40f;
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
}
