using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Models;
using Newtonsoft.Json;

namespace WindowsFormsApp3
{
    /// <summary>
    /// PDF形状类型枚举
    /// </summary>
    public enum ShapeType
    {
        /// <summary>
        /// 直角矩形
        /// </summary>
        RightAngle,
        /// <summary>
        /// 圆形
        /// </summary>
        Circle,
        /// <summary>
        /// 异形
        /// </summary>
        Special,
        /// <summary>
        /// 圆角矩形
        /// </summary>
        RoundRect
    }

    public partial class MaterialSelectFormModern : Form
    {

        // Win32 API - 用于设置窗口透明度
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_COLORKEY = 0x1;
        private const int LWA_ALPHA = 0x2;

        // 窗体拖动相关字段
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // 公共属性 - 用于返回选择的数据
        public string SelectedMaterial { get; private set; }
        public string OrderNumber { get; private set; }
        public string Quantity { get; private set; }
        public string FixedField { get; private set; }
        public string SelectedExportPath { get; private set; }
        public string AdjustedDimensions { get; private set; }
        public string CurrentFileName { get; private set; }
        public double SelectedTetBleed { get; private set; }
        public string SerialNumber { get; set; }
        public List<string> Quantities { get; set; } = new List<string>();
        public List<DataRow> MatchedRows { get; private set; } = new List<DataRow>();

        // 新增属性 - 工艺参数
        public string ColorMode { get; private set; } = "彩色";
        public string FilmType { get; private set; } = "光膜";
        public int Increment { get; private set; } = 0;
        
        // 标识页功能
        public bool AddIdentifierPage { get; private set; } = false;

        // 排版材料类型
        public string ImpositionMaterialType { get; private set; }

        // Excel数据相关字段
        private DataTable _excelData;
        private int _searchColumnIndex;
        private int _returnColumnIndex;
        private int _serialColumnIndex;
        private int _newColumnIndex;
        private string _regexResult;

        // 透明度相关
        private double _opacityValue;

        // 尺寸相关字段
        private string _originalWidth;
        private string _originalHeight;
        private double _initialWidth;  // 已统一输出格式（大数在前）
        private double _initialHeight; // 已统一输出格式（小数在后）

        // PDF原始宽高（未经处理，用于显示和验证布局计算）
        private double _pdfOriginalWidth;  // PDF原始宽度
        private double _pdfOriginalHeight; // PDF原始高度

        // 形状处理字段 - 新的枚举方式
        public ShapeType SelectedShape { get; private set; } = ShapeType.RightAngle;
        public double RoundRadius { get; private set; } = 0; // 仅用于圆角矩形

        // 标记是否明确选择了形状（用于区分默认状态和用户主动选择）
        private bool _isShapeExplicitlySelected = false;

        // PDF 预览相关字段
        private bool _isPreviewExpanded = false;  // 预览是否展开
        private const int BASE_FORM_HEIGHT = 859; // 匹配设计器中的ClientSize设置 (400, 859) // 调整基础高度使运行时窗口高度匹配设计器896px (896 - 276系统边框) // 调整基础高度使运行时窗口高度匹配设计器896px (638 - 53) // 窗体基础高度（不含预览面板，包括折叠按钮）(661-23)
        private const int MAX_PREVIEW_HEIGHT = 245; // 预览最大高度（匹配设计器设置） // 预览最大高度（调整填满底部）
        private string _cachedPdfPath; // 缓存的 PDF 路径（用于检查是否为新文件）
        private string _pendingPdfToLoad; // 待加载的PDF文件路径（用于窗体加载完成后）
        private const string PREVIEW_STATE_KEY = "PdfPreviewExpanded"; // 注册表键名

        // 延迟初始化相关字段
        private WindowsFormsApp3.Controls.PdfPreviewControl _realPdfPreviewControl; // 真实的PDF预览控件
        private bool _pdfControlInitialized = false; // PDF控件是否已初始化

        // 保留原有属性以兼容现有代码（后续版本可移除）
        [Obsolete("请使用SelectedShape代替")]
        public bool IsShapeSelected => _isShapeExplicitlySelected;
        [Obsolete("请使用SelectedShape和RoundRadius代替")]
        public string CornerRadius => GetCompatibleCornerRadius();
        [Obsolete("请使用SelectedShape代替")]
        public bool UsePdfLastPage => SelectedShape == ShapeType.Special;

        /// <summary>
        /// 根据新的形状属性生成兼容的CornerRadius字符串
        /// </summary>
        /// <returns>兼容旧版本的CornerRadius值</returns>
        /// <summary>
        /// 根据新的形状属性生成兼容的CornerRadius字符串
        /// </summary>
        /// <returns>兼容旧版本的CornerRadius值</returns>
        /// <summary>
        /// 根据新的形状属性生成兼容的CornerRadius字符串
        /// </summary>
        /// <returns>兼容旧版本的CornerRadius值</returns>
        public string GetCompatibleCornerRadius()
        {
            switch (SelectedShape)
            {
                case ShapeType.Circle:
                    return "R"; // 旧版本用"R"表示圆形
                case ShapeType.Special:
                    return "Y"; // 旧版本用"Y"表示异形
                case ShapeType.RoundRect:
                    return RoundRadius.ToString(); // 圆角矩形用数字
                case ShapeType.RightAngle:
                default:
                    return "0"; // 直角用"0"
            }
        }

        /// <summary>
        /// 检查是否有形状被选中（兼容旧版本IsShapeSelected逻辑）
        /// </summary>
        /// <returns>是否选中了形状</returns>
        /// <summary>
        /// 检查是否有形状被选中（兼容旧版本IsShapeSelected逻辑）
        /// </summary>
        /// <returns>是否选中了形状</returns>
        public bool GetIsShapeSelected()
        {
            return _isShapeExplicitlySelected;
        }

        /// <summary>
        /// 检查是否使用PDF最后一页（兼容旧版本UsePdfLastPage逻辑）
        /// </summary>
        /// <returns>是否使用PDF最后一页</returns>
        /// <summary>
        /// 检查是否使用PDF最后一页（兼容旧版本UsePdfLastPage逻辑）
        /// </summary>
        /// <returns>是否使用PDF最后一页</returns>
        public bool GetUsePdfLastPage()
        {
            return SelectedShape == ShapeType.Special;
        }

        // 工具支持
        private ToolTip _toolTip;

        // 排版服务
        private readonly IImpositionService _impositionService;

        // 当前排版计算结果
        private ImpositionResult _currentImpositionResult;

        // 一式两联复选框状态
        private bool _isDuplicateLayoutEnabled = false;

        // 预设右键菜单
        private System.Windows.Forms.ContextMenuStrip _presetContextMenu;
        private string _currentPresetName = "";

        // 配置数据结构
        public class ExportFolderConfig
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public bool Enabled { get; set; } = true;
            public string Icon { get; set; } = "folder"; // Changed default to "folder" key

            // 预设参数绑定（通过导出路径快速选择预设）
            public string PresetMaterial { get; set; } = "";      // 预设材料
            public string PresetBleed { get; set; } = "";        // 预设出血值
            public string PresetColorMode { get; set; } = "";    // 预设颜色模式
            public string PresetFilmType { get; set; } = "";     // 预设膜类型
            public bool PresetAddIdentifierPage { get; set; } = false; // 预设标识页
            public string PresetShape { get; set; } = "";        // 预设形状
            public bool PresetDualCopy { get; set; } = false;   // 预设一式两联
        }

        // 路径项数据结构
        public class PathItem
        {
            public string FullPath { get; set; }
            public string FolderName { get; set; }
            
            public override string ToString()
            {
                return FolderName;
            }
        }

        // 当前选中的菜单项
        private TreeNode selectedTreeNode = null;

        // 材料列表 - 固定15个常用材料按钮
        private string[] _materials = new[]
        {
            "PET", "PP", "PVC", "PET环保", "PET透明", "PET哑光", "PET镭射",
            "PET磨砂", "PET金色", "PET银色", "PET白色", "PET红色", "PET蓝色",
            "PET绿色"
        };

        // 下拉框材料选项 - 超过15个的额外材料
        private readonly string[] _dropdownMaterials = new[]
        {
            "PP白色", "PP银色", "PP金色", "PP红色", "PP蓝色", "PP绿色",
            "PVC环保", "PVC白色", "PVC透明", "PVC银色", "PVC金色", "PVC红色", "PVC蓝色",
            "AL6061-T6", "AL7075-T6", "ST304", "ST316", "BRASS_C360",
            "TITANIUM_GR2", "MAGNESIUM_AZ31D", "CARBON_STEEL_1045"
        };

        public MaterialSelectFormModern()
        {
            InitializeComponent();

            // 始终在最前端显示
            TopMost = true;

            // 设置窗口图标
            string iconPath = GetIconPath();
            if (!string.IsNullOrEmpty(iconPath) && System.IO.File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }

            // 🔧 关键优化：在构造函数中直接设置窗口位置，完全避免视觉跳跃
            PrePositionWindow();

            // 初始化显示标签字段引用
            InitializeDisplayLabels();

            // 初始化排版服务
            _impositionService = new ImpositionService();

            // 使用设计器中的窗口尺寸，不再动态调整大小
            LoadMaterials();

            // 初始化预设右键菜单
            InitializePresetContextMenu();

            // 绑定窗口位置管理事件
            this.FormClosing += MaterialSelectFormModern_FormClosing;

            // 延迟初始化PDF预览控件（在设计器模式下不会执行）
            if (!IsDesignMode())
            {
                // 在后台线程中初始化，避免阻塞UI
                Task.Run(() =>
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            InitializePdfPreviewControl();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error($"[PDF 预览] 后台初始化失败: {ex.Message}");
                        }
                    }));
                });
            }
        }

        /// <summary>
        /// 预定位窗口位置，在窗体显示前直接设置目标位置，完全避免视觉跳跃
        /// </summary>
        private void PrePositionWindow()
        {
            try
            {
                LogHelper.Debug("[MaterialSelectFormModern] ========== 开始预定位窗口 ==========");

                // 读取保存的位置设置
                int savedX = AppSettings.MaterialFormX;
                int savedY = AppSettings.MaterialFormY;
                int savedWidth = AppSettings.MaterialFormWidth;
                int savedHeight = AppSettings.MaterialFormHeight;
                bool savedMaximized = AppSettings.MaterialFormMaximized;

                LogHelper.Debug($"[MaterialSelectFormModern] 预定位读取设置: X={savedX}, Y={savedY}, Width={savedWidth}, Height={savedHeight}, Maximized={savedMaximized}");

                // 如果有保存的位置信息
                if (savedX >= 0 && savedY >= 0)
                {
                    // 强制设置为Manual模式，确保Location设置生效
                    this.StartPosition = FormStartPosition.Manual;
                    LogHelper.Debug("[MaterialSelectFormModern] 预定位设置StartPosition为Manual");

                    // 计算安全的窗口位置（确保在屏幕范围内）
                    var workingArea = Screen.PrimaryScreen.WorkingArea;
                    int targetX = Math.Max(workingArea.Left, Math.Min(savedX, workingArea.Right - this.MinimumSize.Width));
                    int targetY = Math.Max(workingArea.Top, Math.Min(savedY, workingArea.Bottom - this.MinimumSize.Height));

                    // 直接设置窗体位置
                    this.Location = new Point(targetX, targetY);
                    LogHelper.Debug($"[MaterialSelectFormModern] 预定位设置Location: ({targetX}, {targetY})");

                    // 恢复窗口大小（如果有效）
                    if (savedWidth > 0 && savedHeight > 0)
                    {
                        int width = Math.Max(this.MinimumSize.Width, savedWidth);
                        int height = Math.Max(this.MinimumSize.Height, savedHeight);

                        // 确保窗口大小不超过工作区域
                        width = Math.Min(width, workingArea.Width);
                        height = Math.Min(height, workingArea.Height);

                        this.Size = new Size(width, height);
                        LogHelper.Debug($"[MaterialSelectFormModern] 预定位设置Size: ({width}, {height})");
                    }

                    // 恢复窗口最大化状态
                    if (savedMaximized)
                    {
                        this.WindowState = FormWindowState.Maximized;
                        LogHelper.Debug("[MaterialSelectFormModern] 预定位设置WindowState为Maximized");
                    }
                    else
                    {
                        this.WindowState = FormWindowState.Normal;
                        LogHelper.Debug("[MaterialSelectFormModern] 预定位设置WindowState为Normal");
                    }

                    LogHelper.Debug("[MaterialSelectFormModern] ========== 预定位完成，窗口将直接在目标位置显示 ==========");
                }
                else
                {
                    // 首次运行，保持居中显示
                    this.StartPosition = FormStartPosition.CenterScreen;
                    LogHelper.Debug("[MaterialSelectFormModern] 首次运行或无保存位置，保持居中显示");
                }

                // 🔧 关键新增：在预定位阶段同时恢复预览状态
                RestorePreviewStateInPrePosition();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 预定位失败: {ex.Message}", ex);
                // 异常时确保窗口至少可见
                try
                {
                    this.StartPosition = FormStartPosition.CenterScreen;
                    LogHelper.Debug("[MaterialSelectFormModern] 预定位异常处理：设置StartPosition为CenterScreen");
                }
                catch (Exception fallbackEx)
                {
                    LogHelper.Error($"[MaterialSelectFormModern] 预定位异常处理也失败了: {fallbackEx.Message}", fallbackEx);
                }
            }
        }

        /// <summary>
        /// 预览状态预恢复方法，在预定位阶段安全地恢复预览状态
        /// </summary>
        private void RestorePreviewStateInPrePosition()
        {
            try
            {
                // 读取保存的预览状态
                bool shouldExpand = WindowPositionManager.ShouldExpandPreview();

                // 安全检查：确保控件已初始化
                if (pdfPreviewPanel == null || previewCollapseButton == null)
                {
                    LogHelper.Debug("[预定位] 预览控件未初始化，跳过状态恢复");
                    return;
                }

                // 在窗口显示前设置预览面板高度和按钮状态
                if (shouldExpand)
                {
                    _isPreviewExpanded = true;
                    pdfPreviewPanel.Height = MAX_PREVIEW_HEIGHT;
                    this.ClientSize = new Size(400, 886);  // 🔧 添加：设置展开状态窗体大小
                    previewCollapseButton.Text = "▲";
                    LogHelper.Debug("[预定位] 恢复预览展开状态和窗体大小");
                }
                else
                {
                    _isPreviewExpanded = false;
                    pdfPreviewPanel.Height = 0;
                    this.ClientSize = new Size(400, 644);  // 🔧 添加：设置折叠状态窗体大小（包含文件名区域）
                    previewCollapseButton.Text = "▼";
                    LogHelper.Debug("[预定位] 恢复预览折叠状态和窗体大小");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[预定位] 恢复预览状态失败: {ex.Message}");
                // 失败时保持默认状态（折叠）
                _isPreviewExpanded = false;
            }
        }

      /// <summary>
        /// 确保预览状态一致性 - 在Form_Load中调用，验证状态是否正确恢复
        /// </summary>
        private void EnsurePreviewStateConsistency()
        {
            try
            {
                // 读取保存的预览状态
                bool shouldExpand = WindowPositionManager.ShouldExpandPreview();
                
                LogHelper.Debug($"[状态检查] 保存的预览状态: {shouldExpand}, 当前状态: {_isPreviewExpanded}");

                // 确保控件完全加载后，状态与保存的设置一致
                if (shouldExpand != _isPreviewExpanded)
                {
                    LogHelper.Debug($"[状态检查] 检测到状态不一致，调用LoadPreviewStateFromSettings修正");
                    LoadPreviewStateFromSettings();
                }
                else
                {
                    LogHelper.Debug("[状态检查] 预览状态一致，无需修正");
                    
                    // 即使状态一致，也要确保UI控件状态正确
                    if (pdfPreviewPanel != null && previewCollapseButton != null)
                    {
                        if (shouldExpand)
                        {
                            pdfPreviewPanel.Height = MAX_PREVIEW_HEIGHT;
                            this.ClientSize = new Size(400, 886);  // 🔧 添加：设置展开状态窗体大小
                            previewCollapseButton.Text = "▲";

                            // ✅ 如果有待加载的PDF且预览已展开，现在加载它
                            if (!string.IsNullOrEmpty(_pendingPdfToLoad))
                            {
                                // 延迟一小段时间确保UI完全渲染
                                this.BeginInvoke(new Action(async () =>
                                {
                                    await Task.Delay(100); // 减少延迟时间
                                    await TryLoadPendingPdf();
                                    LogHelper.Debug("[PDF 预览] 状态检查后调用TryLoadPendingPdf");
                                    
                                    // 🔧 关键修复：加载 PDF 后强制应用正确的缩放
                                    await Task.Delay(200); // 额外延迟确保 PDF 加载完成
                                    if (PdfPreview != null && PdfPreview.PageCount > 0)
                                    {
                                        LogHelper.Debug("[PDF 预览] 状态检查后应用缩放");
                                        PdfPreview.ApplyBestFitZoomPublic();
                                    }
                                }));
                            }

                            LogHelper.Debug("[状态检查] 确认展开状态UI和窗体大小");
                        }
                        else
                        {
                            pdfPreviewPanel.Height = 0;
                            this.ClientSize = new Size(400, 644);  // 🔧 添加：设置折叠状态窗体大小（包含文件名区域）
                            previewCollapseButton.Text = "▼";
                            LogHelper.Debug("[状态检查] 确认折叠状态UI和窗体大小");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[状态检查] 确保预览状态一致性失败: {ex.Message}", ex);
                // 异常时调用完整恢复方法作为后备
                try
                {
                    LoadPreviewStateFromSettings();
                }
                catch (Exception fallbackEx)
                {
                    LogHelper.Error($"[状态检查] 后备恢复也失败: {fallbackEx.Message}", fallbackEx);
                }
            }
        }

        /// <summary>
        /// 格式化尺寸显示，只有当小数部分不为0时才显示小数
        /// </summary>
        /// <param name="value">尺寸值</param>
        /// <returns>格式化后的尺寸字符串</returns>
        private string FormatDimension(double value)
        {
            // 检查小数部分是否为0
            if (Math.Abs(value - Math.Round(value)) < 0.001)
            {
                // 如果小数部分为0，只显示整数
                return $"{Math.Round(value):F0}";
            }
            else
            {
                // 如果小数部分不为0，显示一位小数
                return $"{value:F1}";
            }
        }

        /// <summary>
        /// 格式化PDF尺寸显示
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>格式化后的PDF尺寸字符串</returns>
        private string FormatPdfSize(double width, double height)
        {
            var formattedWidth = FormatDimension(width);
            var formattedHeight = FormatDimension(height);
            return $"PDF尺寸: {formattedWidth}×{formattedHeight}mm";
        }

        /// <summary>
        /// 自动调整标签字体大小以适应文本
        /// </summary>
        /// <param name="label">要调整的标签控件</param>
        /// <param name="minFontSize">最小字体大小</param>
        /// <param name="maxFontSize">最大字体大小</param>
        private void AutoResizeLabelFont(AntdUI.Label label, float minFontSize = 6f, float maxFontSize = 9f)
        {
            if (label == null || string.IsNullOrEmpty(label.Text)) return;
            
            try
            {
                // 计算可用宽度(控件宽度 - 左右内边距)
                int availableWidth = label.Width - label.Padding.Left - label.Padding.Right;
                
                if (availableWidth <= 0) return;
                
                // 从最大字体开始尝试
                float fontSize = maxFontSize;
                Font testFont = null;
                SizeF textSize = SizeF.Empty;
                
                try
                {
                    using (Graphics g = label.CreateGraphics())
                    {
                        testFont = new Font(label.Font.FontFamily, fontSize, label.Font.Style);
                        textSize = g.MeasureString(label.Text, testFont);
                        
                        // 如果文本超出宽度,逐步减小字体
                        while (textSize.Width > availableWidth && fontSize > minFontSize)
                        {
                            fontSize -= 0.5f;
                            testFont.Dispose();
                            testFont = new Font(label.Font.FontFamily, fontSize, label.Font.Style);
                            textSize = g.MeasureString(label.Text, testFont);
                        }
                    }
                    
                    // 应用新字体
                    if (Math.Abs(label.Font.Size - fontSize) > 0.1f)
                    {
                        var oldFont = label.Font;
                        label.Font = new Font(label.Font.FontFamily, fontSize, label.Font.Style);
                        oldFont.Dispose();
                    }
                }
                finally
                {
                    testFont?.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"自动调整标签字体大小失败: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// 初始化显示标签字段引用
        /// </summary>
        private void InitializeDisplayLabels()
        {
            // 从控件集合中查找并初始化显示标签字段
            rowsDisplayLabel = Controls.Find("rowsDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
            columnsDisplayLabel = Controls.Find("columnsDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
            layoutCountDisplayLabel = Controls.Find("layoutCountDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
            rotationDisplayLabel = Controls.Find("rotationDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
            pdfSizeDisplayLabel = Controls.Find("pdfSizeDisplayLabel", true).FirstOrDefault() as AntdUI.Label;

            // 初始化一式两联复选框引用
            duplicateLayoutCheckbox = Controls.Find("duplicateLayoutCheckbox", true).FirstOrDefault() as AntdUI.Checkbox;

            LogHelper.Debug($"[MaterialSelectFormModern] 显示标签初始化完成: rowsDisplayLabel={(rowsDisplayLabel != null)}, columnsDisplayLabel={(columnsDisplayLabel != null)}, layoutCountDisplayLabel={(layoutCountDisplayLabel != null)}, rotationDisplayLabel={(rotationDisplayLabel != null)}, pdfSizeDisplayLabel={(pdfSizeDisplayLabel != null)}, duplicateLayoutCheckbox={(duplicateLayoutCheckbox != null)}");

            // 初始化一式两联复选框状态
            if (duplicateLayoutCheckbox != null)
            {
                duplicateLayoutCheckbox.Checked = _isDuplicateLayoutEnabled;
                // 一式两联复选框事件
                duplicateLayoutCheckbox.CheckedChanged += DuplicateLayoutCheckbox_CheckedChanged;
            }

            // 文件名标签拖动事件
            if (fileNameLabel != null)
            {
                fileNameLabel.MouseDown += FileNameLabel_MouseDown;
                fileNameLabel.MouseMove += FileNameLabel_MouseMove;
                fileNameLabel.MouseUp += FileNameLabel_MouseUp;
                // 添加 SizeChanged 事件处理,当控件大小变化时自动调整字体
                fileNameLabel.SizeChanged += (s, e) => AutoResizeLabelFont(fileNameLabel);
            }
        }

        // 兼容Form1.cs调用的构造函数
        public MaterialSelectFormModern(
            List<string> materials,
            string fileName,
            string regexResult,
            double opacity,
            string width,
            string height,
            DataTable excelData,
            int searchColumnIndex,
            int returnColumnIndex,
            int serialColumnIndex,
            int newColumnIndex,  // 添加缺失的newColumnIndex参数
            string serialNumber,
            List<DataRow> matchedRows = null)  // 添加matchedRows参数
        {
            InitializeComponent();

            // 始终在最前端显示
            TopMost = true;

            // 🔧 关键修复：添加预定位调用，确保无视觉跳跃且位置记忆正常
            PrePositionWindow();

            // 初始化显示标签字段引用
            InitializeDisplayLabels();

            // 初始化排版服务
            _impositionService = new ImpositionService();

            // 初始化Excel相关数据
            _excelData = excelData;
            _searchColumnIndex = searchColumnIndex;
            _returnColumnIndex = returnColumnIndex;
            _serialColumnIndex = serialColumnIndex;
            _newColumnIndex = newColumnIndex;
            _regexResult = regexResult;
            this.SerialNumber = serialNumber;
            
            // 如果传递了matchedRows，直接使用它
            if (matchedRows != null && matchedRows.Count > 0)
            {
                this.MatchedRows = matchedRows;
                LogHelper.Debug($"从构造函数接收到 {matchedRows.Count} 行匹配数据");
            }

            // 初始化透明度值
            _opacityValue = opacity;

            // 初始化尺寸数据
            if (!string.IsNullOrEmpty(width) && !string.IsNullOrEmpty(height))
            {
                SetDimensions(width, height);
            }

            // 更新材料列表
            if (materials != null && materials.Count > 0)
            {
                _materials = materials.ToArray();
            }

            // 早期初始化ToolTip以提高响应速度
            InitializeToolTip();

            // 添加窗体事件处理
            this.Shown += MaterialSelectFormModern_Shown;
            this.Load += MaterialSelectFormModern_Load;
            this.FormClosing += MaterialSelectFormModern_FormClosing;

            // 窗体显示时自动递增订单号
            this.VisibleChanged += (sender, e) =>
            {
                if (this.Visible && AppSettings.GetValue<bool>("AutoIncrementOrderNumber1"))
                {
                    IncrementLastNumberInOrderText();
                }
            };

            // 设置当前文件名并更新页面头部显示
            SetCurrentFileName(fileName);

            LoadMaterials();

            // 初始化预设右键菜单
            InitializePresetContextMenu();

            InitializeEventHandlers();
            InitializeFolderTree(); // 初始化文件夹菜单

            // 初始化排版控件
            InitializeImpositionControls();

            // 初始化 PDF 预览控件
            InitializePdfPreview();

            // 设置确认按钮为窗体的默认按钮，确保Enter键有效
            this.AcceptButton = confirmButton;

            // ⚠️ 注意：此处不再调用LoadLastSelectedMaterial，因为它会在Load事件中调用
            // 这样可以避免在材料按钮还未完全加载时尝试恢复选择状态
            // LoadLastSelectedMaterial(); // 已移除，改为在Load事件中调用

            // 自动填充数量
            AutoFillQuantity();
        }

        private void LoadMaterials()
        {
            // 设置材料按钮的事件处理
            materialButton1.Click += MaterialButton_Click;
            materialButton2.Click += MaterialButton_Click;
            materialButton3.Click += MaterialButton_Click;
            materialButton4.Click += MaterialButton_Click;
            materialButton5.Click += MaterialButton_Click;
            materialButton6.Click += MaterialButton_Click;
            materialButton7.Click += MaterialButton_Click;
            materialButton8.Click += MaterialButton_Click;
            materialButton9.Click += MaterialButton_Click;
            materialButton10.Click += MaterialButton_Click;
            materialButton11.Click += MaterialButton_Click;
            materialButton12.Click += MaterialButton_Click;
            materialButton13.Click += MaterialButton_Click;
            materialButton14.Click += MaterialButton_Click;
            materialButton15.Click += MaterialButton_Click;

            // 设置下拉框的事件处理和选项
            dropdown16.Items.Clear();
            // 添加取消选项（第一个选项）
            dropdown16.Items.Add("取消选择");
            foreach (var material in _dropdownMaterials)
            {
                dropdown16.Items.Add(material);
            }
            dropdown16.SelectedIndexChanged += Dropdown16_SelectedIndexChanged;

            // 添加双击事件用于快速取消选中
            dropdown16.MouseDoubleClick += Dropdown16_MouseDoubleClick;

            LogHelper.Debug($"已设置 {_materials.Length} 个材料按钮和 {_dropdownMaterials.Length} 个下拉框选项的事件处理");
        }

        /// <summary>
        /// 获取图标路径
        /// </summary>
        private string GetIconPath()
        {
            string[] paths = new[]
            {
                System.IO.Path.Combine(Application.StartupPath, "dc.ico"),
                System.IO.Path.Combine(Application.StartupPath, "Resources", "dc.ico")
            };
            
            foreach (var path in paths)
            {
                if (System.IO.File.Exists(path))
                {
                    return path;
                }
            }
            
            return null;
        }

        private void InitializeEventHandlers()
        {
            // 颜色模式按钮事件处理
            if (colorModeButton != null)
            {
                colorModeButton.Click += ColorModeButton_Click;
            }

            // 膜类型按钮事件处理
            if (filmTypeLightButton != null)
            {
                filmTypeLightButton.Click += FilmTypeButton_Click;
            }
            
            if (filmTypeMatteButton != null)
            {
                filmTypeMatteButton.Click += FilmTypeButton_Click;
            }
            
            if (filmTypeNoneButton != null)
            {
                filmTypeNoneButton.Click += FilmTypeButton_Click;
            }
            
            if (filmTypeRedButton != null)
            {
                filmTypeRedButton.Click += FilmTypeButton_Click;
            }

            // 确认按钮事件
            if (confirmButton != null)
            {
                confirmButton.Click += ConfirmButton_Click;
            }

            // 取消按钮事件
            if (cancelButton != null)
            {
                cancelButton.Click += CancelButton_Click;
            }
            
            // 标识页复选框事件
            if (chkIdentifierPage != null)
            {
                chkIdentifierPage.CheckedChanged += ChkIdentifierPage_CheckedChanged;
            }

            // 出血位下拉框事件
            if (bleedDropdown != null)
            {
                bleedDropdown.SelectedIndexChanged += BleedDropdown_SelectedIndexChanged;
            }

            // 增量文本框事件
            if (incrementTextBox != null)
            {
                incrementTextBox.LostFocus += IncrementTextBox_LostFocus;
            }

            // 数量文本框事件
            if (quantityTextBox != null)
            {
                quantityTextBox.LostFocus += QuantityTextBox_LostFocus;
            }

            // 订单号文本框事件
            if (orderNumberTextBox != null)
            {
                orderNumberTextBox.LostFocus += OrderNumberTextBox_LostFocus;
            }

            // 形状按钮事件处理
            if (shapeRightAngleButton != null)
            {
                shapeRightAngleButton.Click += (sender, e) => SelectShape(ShapeType.RightAngle);
            }

            if (shapeCircleButton != null)
            {
                shapeCircleButton.Click += (sender, e) => SelectShape(ShapeType.Circle);
            }

            if (shapeSpecialButton != null)
            {
                shapeSpecialButton.Click += (sender, e) => SelectShape(ShapeType.Special);
            }

            if (shapeRoundRectButton != null)
            {
                shapeRoundRectButton.Click += (sender, e) => SelectShape(ShapeType.RoundRect);
            }
            
            // 圆角输入框事件处理
            if (radiusTextBox != null)
            {
                radiusTextBox.TextChanged += RadiusTextBox_TextChanged;
                radiusTextBox.LostFocus += RadiusTextBox_LostFocus;
            }
            
            // TreeView选择事件处理
            if (folderTreeView != null)
            {
                folderTreeView.AfterSelect += FolderTreeView_AfterSelect;
                // BeforeExpand 和 AfterExpand 事件在 Designer 文件中绑定
            }

            // 初始化控件默认值
            InitializeControlValues();

            // 初始化形状控件状态
            InitializeShapeControls();

            // 初始化排版控件事件处理
            SetupImpositionEventHandlers();

        }

      /// <summary>
        /// 选择膜类型
        /// </summary>
        /// <param name="filmType">膜类型名称</param>
        /// <param name="buttonIndex">按钮索引</param>
/// <summary>
        /// 膜类型按钮点击事件处理
        /// </summary>
        private void FilmTypeButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is AntdUI.Button clickedButton)
                {
                    // 获取按钮对应的膜类型和索引
                    string filmType = "";
                    int buttonIndex = -1;
                    
                    if (clickedButton == filmTypeLightButton) { filmType = "光膜"; buttonIndex = 0; }
                    else if (clickedButton == filmTypeMatteButton) { filmType = "哑膜"; buttonIndex = 1; }
                    else if (clickedButton == filmTypeNoneButton) { filmType = "不过膜"; buttonIndex = 2; }
                    else if (clickedButton == filmTypeRedButton) { filmType = "红膜"; buttonIndex = 3; }
                    
                    if (!string.IsNullOrEmpty(filmType))
                    {
                        // 检查是否点击了已选中的按钮（用于取消选择）
                        bool isCurrentlySelected = FilmType == filmType;
                        
                        if (isCurrentlySelected)
                        {
                            // 取消选择
                            SelectFilmType("", -1);
                            LogHelper.Debug($"取消选择膜类型: {filmType}");
                        }
                        else
                        {
                            // 选择新的膜类型
                            SelectFilmType(filmType, buttonIndex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"膜类型按钮点击事件处理失败: {ex.Message}", ex);
                MessageBox.Show($"膜类型选择失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 选择膜类型
        /// </summary>
        /// <param name="filmType">膜类型名称</param>
        /// <param name="buttonIndex">按钮索引，-1表示取消选择</param>

        private void SelectFilmType(string filmType, int buttonIndex)
        {
            try
            {
                // 更新所有膜类型按钮的状态
                UpdateFilmTypeButtonStates(buttonIndex);

                // 设置膜类型
                FilmType = filmType;
                _lastSelectedFilmType = filmType;

                // 保存到设置
                if (string.IsNullOrEmpty(filmType))
                {
                    // 取消选择时保存空值
                    AppSettings.Set("LastFilmType", "");
                }
                else
                {
                    AppSettings.Set("LastFilmType", FilmType);
                }
                AppSettings.Save();

                // 更新显示
                UpdateFixedField();

                if (string.IsNullOrEmpty(filmType))
                {
                    LogHelper.Debug("已取消膜类型选择");
                }
                else
                {
                    LogHelper.Debug($"膜类型切换为: {FilmType}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"膜类型选择失败: {ex.Message}", ex);
                MessageBox.Show($"膜类型选择失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 更新膜类型按钮状态
        /// </summary>
        /// <param name="selectedIndex">选中的按钮索引</param>
        private void UpdateFilmTypeButtonStates(int selectedIndex)
        {
            // 获取当前主题（通过服务容器获取ThemeManager实例）
            var themeManager = ServiceLocator.Instance.GetThemeManager();
            ThemeDefinition theme = null;
            bool isDark = false;

            if (themeManager != null)
            {
                theme = themeManager.GetCurrentTheme();
                if (theme != null)
                {
                    isDark = IsThemeDark(theme);
                }
            }

            // 重置所有按钮状态
            if (filmTypeLightButton != null)
            {
                filmTypeLightButton.Type = AntdUI.TTypeMini.Default;
                if (theme != null) ApplyThemeToMaterialButton(filmTypeLightButton, theme, isDark);
            }
            if (filmTypeMatteButton != null)
            {
                filmTypeMatteButton.Type = AntdUI.TTypeMini.Default;
                if (theme != null) ApplyThemeToMaterialButton(filmTypeMatteButton, theme, isDark);
            }
            if (filmTypeNoneButton != null)
            {
                filmTypeNoneButton.Type = AntdUI.TTypeMini.Default;
                if (theme != null) ApplyThemeToMaterialButton(filmTypeNoneButton, theme, isDark);
            }
            if (filmTypeRedButton != null)
            {
                filmTypeRedButton.Type = AntdUI.TTypeMini.Default;
                if (theme != null) ApplyThemeToMaterialButton(filmTypeRedButton, theme, isDark);
            }
            
            // 设置选中按钮状态
            switch (selectedIndex)
            {
                case 0:
                    if (filmTypeLightButton != null)
                    {
                        filmTypeLightButton.Type = AntdUI.TTypeMini.Primary;
                        if (theme != null) ApplyThemeToMaterialButton(filmTypeLightButton, theme, isDark);
                    }
                    break;
                case 1:
                    if (filmTypeMatteButton != null)
                    {
                        filmTypeMatteButton.Type = AntdUI.TTypeMini.Primary;
                        if (theme != null) ApplyThemeToMaterialButton(filmTypeMatteButton, theme, isDark);
                    }
                    break;
                case 2:
                    if (filmTypeNoneButton != null)
                    {
                        filmTypeNoneButton.Type = AntdUI.TTypeMini.Primary;
                        if (theme != null) ApplyThemeToMaterialButton(filmTypeNoneButton, theme, isDark);
                    }
                    break;
                case 3:
                    if (filmTypeRedButton != null)
                    {
                        filmTypeRedButton.Type = AntdUI.TTypeMini.Primary;
                        if (theme != null) ApplyThemeToMaterialButton(filmTypeRedButton, theme, isDark);
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 选择形状（新的枚举版本）
        /// </summary>
        /// <param name="shapeType">形状类型</param>
        private void SelectShape(ShapeType shapeType)
        {
            try
            {
                // 检查是否点击了已选中的项（用于取消选择）
                if (_isShapeExplicitlySelected && SelectedShape == shapeType)
                {
                    // 取消选择 - 清空所有形状相关设置
                    SelectedShape = ShapeType.RightAngle; // 重置为默认值
                    RoundRadius = 0;
                    _isShapeExplicitlySelected = false;

  
                    AppSettings.Set("LastSelectedShape", "NONE"); // 使用特殊标记表示用户明确取消选择
                    AppSettings.Set("LastCornerRadius", "0");
                    AppSettings.Set("LastRoundRadius", "0");
                    AppSettings.Save();

                    // 隐藏圆角输入框
                    if (radiusTextBox != null) radiusTextBox.Visible = false;

                    // 重置选中状态
                    _lastSelectedShapeIndex = -1;

                    // 重置所有形状按钮状态
                    UpdateShapeButtonStates(-1);

                    LogHelper.Debug("形状选择已取消");

                    // 更新尺寸显示（形状变化可能影响最终尺寸）
                    UpdateDimensionsWithBleed();
                    return;
                }

                // 更新按钮状态
                UpdateShapeButtonStates((int)shapeType);

                // 根据选择的形状设置相应属性
                switch (shapeType)
                {
                    case ShapeType.RightAngle: // 直角
                        SelectedShape = ShapeType.RightAngle;
                        RoundRadius = 0; // 直角的圆角半径始终为0
                        _isShapeExplicitlySelected = true; // 标记为用户明确选择
                        // 隐藏圆角输入框
                        if (radiusTextBox != null) radiusTextBox.Visible = false;
                        break;
                    case ShapeType.Circle: // 圆形
                        SelectedShape = ShapeType.Circle;
                        RoundRadius = 0; // 圆形的圆角半径始终为0
                        _isShapeExplicitlySelected = true; // 标记为用户明确选择
                        // 隐藏圆角输入框
                        if (radiusTextBox != null) radiusTextBox.Visible = false;
                        break;
                    case ShapeType.Special: // 异形
                        SelectedShape = ShapeType.Special;
                        RoundRadius = 0; // 异形的圆角半径始终为0
                        _isShapeExplicitlySelected = true; // 标记为用户明确选择
                        // 隐藏圆角输入框
                        if (radiusTextBox != null) radiusTextBox.Visible = false;
                        break;
                    case ShapeType.RoundRect: // 圆角矩形
                        SelectedShape = ShapeType.RoundRect;
                        _isShapeExplicitlySelected = true; // 标记为用户明确选择
                        // 显示圆角输入框
                        if (radiusTextBox != null)
                        {
                            radiusTextBox.Visible = true;

                            // 总是恢复上次的圆角值，确保切换形状时能正确保存
                            string savedRadius = AppSettings.GetValue<string>("LastRoundRadius") ?? "5";
                            radiusTextBox.Text = savedRadius;
                            if (double.TryParse(savedRadius, out double parsedRadius))
                            {
                                RoundRadius = parsedRadius;
                            }

                            // 在设置文本后再设置焦点和选中内容
                            this.BeginInvoke(new Action(() => {
                                radiusTextBox.Focus();
                                radiusTextBox.SelectAll();
                            }));
                        }
                        break;
                }

                // 保存形状选择到设置
                AppSettings.Set("LastSelectedShape", shapeType.ToString());
                AppSettings.Set("LastCornerRadius", GetCompatibleCornerRadius());

                // 只有选择圆角矩形时才保存LastRoundRadius，避免其他形状覆盖用户的圆角设置
                if (shapeType == ShapeType.RoundRect)
                {
                    AppSettings.Set("LastRoundRadius", RoundRadius.ToString());
                }
                AppSettings.Save();

                LogHelper.Debug($"选择形状: {shapeType}, 圆角: {RoundRadius}");

                // 更新尺寸显示（形状变化可能影响最终尺寸）
                UpdateDimensionsWithBleed();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"形状选择失败: {ex.Message}", ex);
                MessageBox.Show($"形状选择失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 选择形状（兼容旧版本）
        /// </summary>
        /// <param name="shapeIndex">形状索引</param>
        [Obsolete("请使用SelectShape(ShapeType)方法")]
        private void SelectShape(int shapeIndex)
        {
            // 记录索引状态（用于兼容现有逻辑）
            _lastSelectedShapeIndex = shapeIndex;

            // 将索引转换为枚举类型，调用新方法
            if (shapeIndex >= 0 && shapeIndex < 4)
            {
                ShapeType shapeType = (ShapeType)shapeIndex;
                SelectShape(shapeType);
            }
        }
        
        /// <summary>
        /// 更新形状按钮状态
        /// </summary>
        /// <param name="selectedIndex">选中的按钮索引，-1表示取消选择</param>
 private void UpdateShapeButtonStates(int selectedIndex)
        {
            // 获取当前主题
            var themeManager = ServiceLocator.Instance.GetThemeManager();
            ThemeDefinition theme = null;
            bool isDark = false;

            if (themeManager != null)
            {
                theme = themeManager.GetCurrentTheme();
                if (theme != null)
                {
                    isDark = IsThemeDark(theme);
                }
            }

            // 重置所有按钮状态
            if (shapeRightAngleButton != null)
            {
                shapeRightAngleButton.Type = AntdUI.TTypeMini.Default;
                if (theme != null) ApplyThemeToMaterialButton(shapeRightAngleButton, theme, isDark);
            }
            if (shapeCircleButton != null)
            {
                shapeCircleButton.Type = AntdUI.TTypeMini.Default;
                if (theme != null) ApplyThemeToMaterialButton(shapeCircleButton, theme, isDark);
            }
            if (shapeSpecialButton != null)
            {
                shapeSpecialButton.Type = AntdUI.TTypeMini.Default;
                if (theme != null) ApplyThemeToMaterialButton(shapeSpecialButton, theme, isDark);
            }
            if (shapeRoundRectButton != null)
            {
                shapeRoundRectButton.Type = AntdUI.TTypeMini.Default;
                if (theme != null) ApplyThemeToMaterialButton(shapeRoundRectButton, theme, isDark);
            }
            
            // 设置选中按钮状态
            if (selectedIndex >= 0)
            {
                switch (selectedIndex)
                {
                    case 0:
                        if (shapeRightAngleButton != null)
                        {
                            shapeRightAngleButton.Type = AntdUI.TTypeMini.Primary;
                            if (theme != null) ApplyThemeToMaterialButton(shapeRightAngleButton, theme, isDark);
                        }
                        break;
                    case 1:
                        if (shapeCircleButton != null)
                        {
                            shapeCircleButton.Type = AntdUI.TTypeMini.Primary;
                            if (theme != null) ApplyThemeToMaterialButton(shapeCircleButton, theme, isDark);
                        }
                        break;
                    case 2:
                        if (shapeSpecialButton != null)
                        {
                            shapeSpecialButton.Type = AntdUI.TTypeMini.Primary;
                            if (theme != null) ApplyThemeToMaterialButton(shapeSpecialButton, theme, isDark);
                        }
                        break;
                    case 3:
                        if (shapeRoundRectButton != null)
                        {
                            shapeRoundRectButton.Type = AntdUI.TTypeMini.Primary;
                            if (theme != null) ApplyThemeToMaterialButton(shapeRoundRectButton, theme, isDark);
                        }
                        break;
                }
            }
        }

    /// <summary>
        /// 颜色模式按钮点击事件处理
        /// </summary>
        private void ColorModeButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (colorModeButton != null)
                {
                    // 切换颜色模式
                    ColorMode = (ColorMode == "彩色") ? "黑白" : "彩色";
                    SetColorModeButtonWithIcon(ColorMode);
                    
                    // 保存到设置
                    AppSettings.Set("LastColorMode", ColorMode);
                    AppSettings.Save();
                    
                    // 更新FixedField（颜色模式 + 膜类型）
                    UpdateFixedField();
                    
                    LogHelper.Debug($"颜色模式切换为: {ColorMode}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"颜色模式切换失败: {ex.Message}", ex);
                MessageBox.Show($"颜色模式切换失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeFolderTree()
        {
            try
            {
                if (folderTreeView == null) return;

                // 初始化ImageList
                var imageList = new ImageList();
                imageList.ImageSize = new Size(14, 14); // 缩小图标尺寸
                imageList.ColorDepth = ColorDepth.Depth32Bit;
                
                // 使用 GDI+ 绘制文件夹图标
                // 采用单色(Monochrome)灰色，类似 FolderOpenOutlined 的线条风格
                // 颜色稍微调浅一点 (100 -> 150)
                var folderBitmap = CreateFolderIcon(14, Color.FromArgb(150, 150, 150)); 
                imageList.Images.Add("folder", folderBitmap);
                
                folderTreeView.ImageList = imageList;

                // 清空现有节点
                folderTreeView.Nodes.Clear();

                // 获取配置的文件夹列表
                var folders = GetConfiguredExportFolders();

                // 创建树形节点
                foreach (var folder in folders)
                {
                    // 不再在文本中添加表情符号图标
                    var parentNode = folderTreeView.Nodes.Add(folder.Name);
                    parentNode.Tag = folder.Path;
                    parentNode.ImageKey = "folder";
                    parentNode.SelectedImageKey = "folder";

                    // 加载子文件夹（增加递归深度以支持更多层级）
                    LoadSubFolders(parentNode, folder.Path, 10); // 支持最多10层深度
                }

                // 展开第一个节点
                if (folderTreeView.Nodes.Count > 0)
                {
                    folderTreeView.Nodes[0].Expand();
                }

                // 恢复上次选择的导出路径
                RestoreLastSelectedExportPath();

                LogHelper.Debug($"文件夹树形菜单初始化完成，共加载 {folders.Count} 个主菜单");
            }
            catch (Exception ex)
            {
                LogHelper.Error("初始化文件夹树形菜单失败: " + ex.Message, ex);
                MessageBox.Show($"初始化文件夹树形菜单失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 创建文件夹图标 Bitmap (Monochrome Outline Style)
        /// </summary>
        private Bitmap CreateFolderIcon(int size, Color color)
        {
            var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                using (var pen = new Pen(color, 1.4f)) // 线条粗细 1.4
                {
                    // 坐标基于 14x14
                    // 1. 绘制后板轮廓 (Back Tab)
                    PointF[] backPoints = new PointF[]
                    {
                        new PointF(1, 3),    // Tab Top-Left
                        new PointF(5, 3),    // Tab Top-Right
                        new PointF(7, 5),    // Slope Down
                        new PointF(12, 5),   // Body Top-Right
                        new PointF(12, 11),  // Bottom-Right
                        new PointF(1, 11)    // Bottom-Left
                    };
                    g.DrawPolygon(pen, backPoints);
                    
                    // 2. 绘制前盖轮廓 (Front Flap - Open)
                    PointF[] frontPoints = new PointF[]
                    {
                        new PointF(1, 11),    // Bottom-Left
                        new PointF(2, 6),     // Top-Left (Skewed)
                        new PointF(13, 6),    // Top-Right
                        new PointF(12, 11)    // Bottom-Right
                    };
                    
                    // 遮挡后板线条
                    using(var brush = new SolidBrush(Color.White))
                    {
                         g.FillPolygon(brush, frontPoints); 
                    }
                    
                    g.DrawPolygon(pen, frontPoints);
                }
            }
            return bmp;
        }

        private void LoadSubFolders(TreeNode parentNode, string parentPath, int maxDepth)
        {
            if (maxDepth <= 0) return;

            try
            {
                // 获取复选框设置 - 修复JSON反序列化问题
                var checkboxSettingsObj = AppSettings.Get("ExportPathCheckboxSettings");
                Dictionary<string, bool> checkboxSettings = null;

                // 尝试多种方式反序列化
                if (checkboxSettingsObj != null)
                {
                    try
                    {
                        // 尝试直接转换
                        checkboxSettings = checkboxSettingsObj as Dictionary<string, bool>;

                        if (checkboxSettings == null)
                        {
                            // 尝试JSON反序列化
                            var json = checkboxSettingsObj.ToString();
                            checkboxSettings = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"[LoadSubFolders] 反序列化复选框设置失败: {ex.Message}");
                        checkboxSettings = new Dictionary<string, bool>();
                    }
                }
                else
                {
                    checkboxSettings = new Dictionary<string, bool>();
                }

                // 检查父路径或任何父级路径是否在复选框设置中且被勾选
                bool shouldIncludeSubFolders = ShouldIncludeSubFolders(parentPath, checkboxSettings);

                if (!shouldIncludeSubFolders)
                {
                    // 如果所有父路径都未勾选读取子文件夹，直接返回不加载子文件夹
                    return;
                }

                var directories = Directory.GetDirectories(parentPath);
                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    // 不再使用 Emoji 图标，直接添加名称
                    var childNode = parentNode.Nodes.Add(dirInfo.Name);
                    childNode.Tag = dirInfo.FullName;
                    childNode.ImageKey = "folder";
                    childNode.SelectedImageKey = "folder";

                    // 递归加载子文件夹，增加深度限制以支持更多层级
                    LoadSubFolders(childNode, dirInfo.FullName, maxDepth - 1);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载子文件夹失败: {parentPath}, {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否应该包含子文件夹（检查父路径链）
        /// </summary>
        private bool ShouldIncludeSubFolders(string currentPath, Dictionary<string, bool> checkboxSettings)
        {
            // 默认包含子文件夹
            if (checkboxSettings == null || checkboxSettings.Count == 0)
                return true;

            // 检查当前路径是否在设置中
            if (checkboxSettings.ContainsKey(currentPath))
            {
                return checkboxSettings[currentPath];
            }

            // 检查所有父路径，看是否有被勾选的
            string parentPath = Path.GetDirectoryName(currentPath);
            while (!string.IsNullOrEmpty(parentPath))
            {
                if (checkboxSettings.ContainsKey(parentPath) && checkboxSettings[parentPath])
                {
                    return true; // 如果父路径被勾选，则包含子文件夹
                }
                parentPath = Path.GetDirectoryName(parentPath);
            }

            // 检查主导出路径是否被勾选
            var exportPaths = AppSettings.ExportPaths ?? new List<string>();
            foreach (var exportPath in exportPaths)
            {
                if (currentPath.StartsWith(exportPath, StringComparison.OrdinalIgnoreCase))
                {
                    // 如果是导出路径的子路径，检查该导出路径是否被勾选
                    if (checkboxSettings.ContainsKey(exportPath) && checkboxSettings[exportPath])
                    {
                        return true;
                    }
                }
            }

            return false; // 默认不包含
        }

        private List<ExportFolderConfig> GetConfiguredExportFolders()
        {
            var folders = new List<ExportFolderConfig>();

            try
            {
                // 从AppSettings.ExportPaths加载导出路径
                var exportPaths = AppSettings.ExportPaths ?? new List<string>();

                LogHelper.Debug($"从AppSettings.ExportPaths加载了 {exportPaths.Count} 个导出路径");

                // 加载预设配置
                var presetSettingsObj = AppSettings.Get("ExportPathPresetSettings");
                Dictionary<string, Dictionary<string, object>> presetSettings = null;
                if (presetSettingsObj != null)
                {
                    try
                    {
                        var json = presetSettingsObj.ToString();
                        presetSettings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
                    }
                    catch
                    {
                        presetSettings = new Dictionary<string, Dictionary<string, object>>();
                    }
                }
                else
                {
                    presetSettings = new Dictionary<string, Dictionary<string, object>>();
                }

                foreach (var path in exportPaths)
                {
                    if (string.IsNullOrEmpty(path)) continue;

                    try
                    {
                        // 获取预设配置
                        var preset = presetSettings.ContainsKey(path) ? presetSettings[path] : null;

                        if (Directory.Exists(path))
                        {
                            var folderName = Path.GetFileName(path);
                            if (string.IsNullOrEmpty(folderName))
                            {
                                folderName = path; // 如果是根目录，使用完整路径
                            }

                            var folderConfig = new ExportFolderConfig
                            {
                                Name = folderName,
                                Path = path,
                                Icon = "📁",
                                Enabled = true
                            };

                            // 应用预设配置
                            if (preset != null)
                            {
                                if (preset.ContainsKey("PresetMaterial")) folderConfig.PresetMaterial = preset["PresetMaterial"]?.ToString() ?? "";
                                if (preset.ContainsKey("PresetBleed")) folderConfig.PresetBleed = preset["PresetBleed"]?.ToString() ?? "";
                                if (preset.ContainsKey("PresetColorMode")) folderConfig.PresetColorMode = preset["PresetColorMode"]?.ToString() ?? "";
                                if (preset.ContainsKey("PresetFilmType")) folderConfig.PresetFilmType = preset["PresetFilmType"]?.ToString() ?? "";
                                if (preset.ContainsKey("PresetAddIdentifierPage")) folderConfig.PresetAddIdentifierPage = preset["PresetAddIdentifierPage"]?.ToString() == "True";
                                if (preset.ContainsKey("PresetShape")) folderConfig.PresetShape = preset["PresetShape"]?.ToString() ?? "";
                                if (preset.ContainsKey("PresetDualCopy")) folderConfig.PresetDualCopy = preset["PresetDualCopy"]?.ToString() == "True";
                            }

                            folders.Add(folderConfig);

                            LogHelper.Debug($"添加导出路径: {path} -> {folderName}");
                        }
                        else
                        {
                            // 路径不存在，但仍然添加到列表中（用不同图标标识）
                            var folderName = Path.GetFileName(path);
                            if (string.IsNullOrEmpty(folderName))
                            {
                                folderName = path;
                            }

                            folders.Add(new ExportFolderConfig
                            {
                                Name = folderName + " (不存在)",
                                Path = path,
                                Icon = "⚠️",
                                Enabled = false
                            });

                            LogHelper.Warn($"导出路径不存在: {path}");
                        }
                    }
                    catch (Exception pathEx)
                    {
                        LogHelper.Error($"处理导出路径失败: {path}, 错误: {pathEx.Message}");
                    }
                }

                // 加载预设配置中的子文件夹（不在ExportPaths中的路径）
                if (presetSettings != null)
                {
                    foreach (var presetPath in presetSettings.Keys)
                    {
                        // 跳过根路径（已在上面处理）
                        if (exportPaths.Contains(presetPath)) continue;

                        // 跳过不存在的路径
                        if (!Directory.Exists(presetPath)) continue;

                        try
                        {
                            var folderName = Path.GetFileName(presetPath);
                            if (string.IsNullOrEmpty(folderName))
                            {
                                folderName = presetPath;
                            }

                            var folderConfig = new ExportFolderConfig
                            {
                                Name = folderName,
                                Path = presetPath,
                                Icon = "📂",
                                Enabled = true
                            };

                            // 应用预设配置
                            var preset = presetSettings[presetPath];
                            if (preset.ContainsKey("PresetMaterial")) folderConfig.PresetMaterial = preset["PresetMaterial"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetBleed")) folderConfig.PresetBleed = preset["PresetBleed"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetColorMode")) folderConfig.PresetColorMode = preset["PresetColorMode"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetFilmType")) folderConfig.PresetFilmType = preset["PresetFilmType"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetAddIdentifierPage")) folderConfig.PresetAddIdentifierPage = preset["PresetAddIdentifierPage"]?.ToString() == "True";
                            if (preset.ContainsKey("PresetShape")) folderConfig.PresetShape = preset["PresetShape"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetDualCopy")) folderConfig.PresetDualCopy = preset["PresetDualCopy"]?.ToString() == "True";

                            folders.Add(folderConfig);

                            LogHelper.Debug($"添加子文件夹: {presetPath} -> {folderName}");
                        }
                        catch (Exception subFolderEx)
                        {
                            LogHelper.Error($"处理子文件夹失败: {presetPath}, 错误: {subFolderEx.Message}");
                        }
                    }
                }

                // 如果没有配置任何路径，添加默认的点源文件夹路径
                if (folders.Count == 0)
                {
                    var defaultPath = @"F:\点源文件夹";

                    if (Directory.Exists(defaultPath))
                    {
                        folders.Add(new ExportFolderConfig
                        {
                            Name = "点源文件夹",
                            Path = defaultPath,
                            Icon = "📁",
                            Enabled = true
                        });
                        LogHelper.Debug($"使用默认导出路径: {defaultPath}");
                    }
                    else
                    {
                        // 尝试创建默认路径
                        try
                        {
                            Directory.CreateDirectory(defaultPath);
                            folders.Add(new ExportFolderConfig
                            {
                                Name = "点源文件夹",
                                Path = defaultPath,
                                Icon = "📁",
                                Enabled = true
                            });
                            LogHelper.Debug($"创建并使用默认导出路径: {defaultPath}");
                        }
                        catch (Exception createEx)
                        {
                            LogHelper.Error($"创建默认导出路径失败: {defaultPath}, 错误: {createEx.Message}");
                            MessageBox.Show($"默认导出路径不存在且无法创建:\n{defaultPath}\n\n请在设置中配置有效的导出路径。\n\n错误详情: {createEx.Message}",
                                "导出路径配置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }

                LogHelper.Debug($"总共加载了 {folders.Count} 个导出文件夹配置，其中 {folders.Count(f => f.Enabled)} 个有效");
            }
            catch (Exception ex)
            {
                LogHelper.Error("读取导出文件夹配置失败: " + ex.Message, ex);
            }

            return folders;
        }

        // TreeView选择事件处理
        private void FolderTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                if (e.Node != null && e.Node.Tag != null)
                {
                    // 设置当前选中
                    selectedTreeNode = e.Node;

                    // 设置导出路径
                    SelectedExportPath = e.Node.Tag.ToString();

                    // 保存选择的导出路径到设置
                    SaveSelectedExportPath();

                    // 应用路径绑定的预设参数
                    ApplyPathBoundPreset();

                    // 更新页面头部显示当前选择的路径（已改为显示文件名）
                    // UpdatePageHeaderWithSelectedPath(); // 改为显示文件名，不再显示导出路径

                    LogHelper.Debug($"选择导出路径: {SelectedExportPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("树形菜单选择处理失败: " + ex.Message, ex);
                MessageBox.Show($"选择路径失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // TreeView展开前事件处理 - 实现单根文件夹展开
            // 添加标志防止递归调用
        private bool _isExpanding = false;

        private void FolderTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                if (e.Node != null)
                {
                    if (e.Node.Level == 0)
                    {
                        // 如果要展开的是根节点，则折叠其他所有根节点
                        foreach (TreeNode rootNode in folderTreeView.Nodes)
                        {
                            if (rootNode != e.Node && rootNode.IsExpanded)
                            {
                                rootNode.Collapse();
                            }
                        }
                    }
                    else if (e.Node.Level == 1)
                    {
                        // 如果要展开的是二级目录，则折叠其他所有已展开的二级目录
                        foreach (TreeNode rootNode in folderTreeView.Nodes)
                        {
                            if (rootNode.IsExpanded)
                            {
                                foreach (TreeNode secondLevelNode in rootNode.Nodes)
                                {
                                    if (secondLevelNode != e.Node && secondLevelNode.IsExpanded)
                                    {
                                        secondLevelNode.Collapse();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"TreeView展开事件处理错误: {ex.Message}");
            }
        }

        /// <summary>
        /// TreeView展开后事件处理 - 自动展开二级目录的所有子目录
        /// </summary>
        private void FolderTreeView_AfterExpand(object sender, TreeViewEventArgs e)
        {
            try
            {
                // 防止递归调用
                if (_isExpanding) return;

                if (e.Node != null && e.Node.Level == 1)
                {
                    _isExpanding = true;

                    // 展开当前二级目录的所有子目录
                    ExpandAllChildNodesSafe(e.Node);
                    LogHelper.Debug($"自动展开二级目录的所有子目录: {GetFullFolderPath(e.Node)}");

                    _isExpanding = false;
                }
            }
            catch (Exception ex)
            {
                _isExpanding = false;
                LogHelper.Debug($"TreeView展开后事件处理错误: {ex.Message}");
            }
        }

        private void UpdatePageHeaderWithSelectedPath()
        {
            try
            {
                if (!string.IsNullOrEmpty(SelectedExportPath))
                {
                    // 显示文件夹名称而不是完整路径，在窗口标题中显示
                    var folderName = System.IO.Path.GetFileName(SelectedExportPath);
                    string exportTitle = $"导出到: {folderName}";
                    if (fileNameLabel != null)
                    {
                        fileNameLabel.Text = exportTitle;
                    }

                                    }
            }
            catch (Exception ex)
            {
                LogHelper.Error("更新窗口标题失败: " + ex.Message, ex);
            }
        }

        private void InitializeControlValues()
        {
            // 初始化出血位下拉框
            InitializeBleedDropdown();

            // 添加工具提示
            InitializeToolTips();

            // ✅ 修复：初始化序号输入框（如果有传入的序号值）
            if (serialNumberTextBox != null && !string.IsNullOrEmpty(this.SerialNumber))
            {
                serialNumberTextBox.Text = this.SerialNumber;
                LogHelper.Debug($"[InitializeControlValues] 设置序号输入框: {this.SerialNumber}");
            }

            // 从设置中恢复上次的选择
            LoadLastSettings();
        }

        /// <summary>
        /// 初始化工具提示
        /// </summary>
        private void InitializeToolTips()
        {
            if (_toolTip == null) return;

            try
            {
                // 为数量输入框添加提示
                if (quantityTextBox != null)
                {
                    _toolTip.SetToolTip(quantityTextBox, "输入多个数量用逗号分隔，例如: 100,99,98");
                }

                // 为增量输入框添加提示
                if (incrementTextBox != null)
                {
                    _toolTip.SetToolTip(incrementTextBox, "输入增量值，将在确认后自动加到每个数量上");
                }

                LogHelper.Debug("工具提示初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"初始化工具提示失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 初始化出血位下拉框
        /// </summary>
        private void InitializeBleedDropdown()
        {
            if (bleedDropdown == null) return;

            try
            {
                // 清空现有选项
                bleedDropdown.Items.Clear();

                // 加载出血位值设置
                string tetBleedValues = AppSettings.Get("TetBleedValues")?.ToString() ?? "3,5,10";
                foreach (var value in tetBleedValues.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (double.TryParse(value.Trim(), out double bleed))
                    {
                        bleedDropdown.Items.Add(bleed);
                    }
                }

                if (bleedDropdown.Items.Count > 0)
                {
                    // 尝试恢复上次选择的出血位值
                    string lastSelectedTetBleed = AppSettings.Get("LastSelectedTetBleed")?.ToString();
                    if (!string.IsNullOrEmpty(lastSelectedTetBleed) && double.TryParse(lastSelectedTetBleed, out double lastBleed))
                    {
                        // 设置Text为上次选择的值
                        bleedDropdown.Text = lastSelectedTetBleed;
                        SelectedTetBleed = lastBleed;
                        LogHelper.Debug($"恢复上次选择的出血位: {lastBleed}");
                    }
                    else
                    {
                        // 如果没有保存的值或解析失败，使用第一个选项
                        bleedDropdown.Text = bleedDropdown.Items[0].ToString();
                        SelectedTetBleed = double.TryParse(bleedDropdown.Items[0].ToString(), out double defaultBleed) ? defaultBleed : 3.0;
                        LogHelper.Debug($"使用默认出血位: {SelectedTetBleed}");
                    }

                    // 初始化时设置尺寸参数，但不立即更新显示
                    // 等待形状状态恢复后再统一更新尺寸显示
                    if (double.TryParse(_originalWidth, out double w)) _initialWidth = w;
                    if (double.TryParse(_originalHeight, out double h)) _initialHeight = h;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"初始化出血位下拉框失败: {ex.Message}", ex);
                MessageBox.Show($"初始化出血位选项失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLastSettings()
        {
            try
            {
                // 恢复订单号
                string lastOrderNumber = AppSettings.Get("LastOrderNumber1")?.ToString();
                if (!string.IsNullOrEmpty(lastOrderNumber))
                {
                    if (orderNumberTextBox != null)
                    {
                        orderNumberTextBox.Text = lastOrderNumber;
                        OrderNumber = lastOrderNumber;
                    }
                }

                // 恢复自动递增状态 - 使用与现有逻辑相同的设置键
                bool autoIncrementState = AppSettings.GetValue<bool>("AutoIncrementOrderNumber1");
                if (autoIncrementCheckbox != null)
                {
                    autoIncrementCheckbox.Checked = autoIncrementState;
                    LogHelper.Debug($"恢复自动递增状态: {autoIncrementState}");
                }

                // 恢复颜色模式
                string lastColorMode = AppSettings.Get("LastColorMode")?.ToString();
                if (!string.IsNullOrEmpty(lastColorMode))
                {
                    ColorMode = lastColorMode;
                    if (colorModeButton != null)
                    {
                        SetColorModeButtonWithIcon(lastColorMode);
                        // 按钮显示对应的状态文本
                    }
                }

                // 恢复膜类型
                string lastFilmType = AppSettings.Get("LastFilmType")?.ToString();
                if (!string.IsNullOrEmpty(lastFilmType))
                {
                    FilmType = lastFilmType;
                    _lastSelectedFilmType = lastFilmType;
                    
                    // 设置膜类型按钮状态
                    string[] filmTypes = { "光膜", "哑膜", "不过膜", "红膜" };
                    int index = Array.IndexOf(filmTypes, lastFilmType);
                    
                    if (index >= 0)
                    {
                        // 正常恢复膜类型选中状态，包括"不过膜"
                        UpdateFilmTypeButtonStates(index);
                    }
                    else
                    {
                        FilmType = ""; // 如果没有找到匹配项，设为空
                        _lastSelectedFilmType = "";
                        LogHelper.Debug($"未知的膜类型: {lastFilmType}，已清空选择");
                        UpdateFilmTypeButtonStates(-1);
                    }
                }
                else
                {
                    FilmType = ""; // 没有上次选择，设为空
                    _lastSelectedFilmType = "";
                    LogHelper.Debug("没有上次膜类型选择，初始化为空");
                    UpdateFilmTypeButtonStates(-1);
                }

                // 恢复形状选择
                string lastSelectedShape = AppSettings.Get("LastSelectedShape")?.ToString();
                if (!string.IsNullOrEmpty(lastSelectedShape) && lastSelectedShape != "NONE")
                {
                    // 支持中文形状名称和英文枚举值的映射
                    string[] shapeNames = { "直角", "圆形", "异形", "圆角矩形" };
                    string[] shapeEnums = { "RightAngle", "Circle", "Special", "RoundRect" };

                    int index = Array.IndexOf(shapeNames, lastSelectedShape);
                    if (index < 0) // 如果在中文名称中没找到，尝试在英文枚举值中查找
                    {
                        index = Array.IndexOf(shapeEnums, lastSelectedShape);
                    }
                    if (index >= 0)
                    {
                        _lastSelectedShapeIndex = index;
                        UpdateShapeButtonStates(index);
                        
                        // 根据形状设置相关属性（使用新的枚举系统）
                        switch (index)
                        {
                            case 0: // 直角
                                SelectedShape = ShapeType.RightAngle;
                                _isShapeExplicitlySelected = true; // 用户明确选择了直角
                                if (radiusTextBox != null) radiusTextBox.Visible = false;
                                break;
                            case 1: // 圆形
                                SelectedShape = ShapeType.Circle;
                                _isShapeExplicitlySelected = true;
                                if (radiusTextBox != null) radiusTextBox.Visible = false;
                                break;
                            case 2: // 异形
                                SelectedShape = ShapeType.Special;
                                _isShapeExplicitlySelected = true;
                                if (radiusTextBox != null) radiusTextBox.Visible = false;
                                break;
                            case 3: // 圆角矩形
                                SelectedShape = ShapeType.RoundRect;
                                _isShapeExplicitlySelected = true;
                                if (radiusTextBox != null)
                                {
                                    radiusTextBox.Visible = true;
                                    string savedRadius = AppSettings.GetValue<string>("LastRoundRadius") ?? "5";
                                    radiusTextBox.Text = savedRadius;
                                    if (double.TryParse(savedRadius, out double radius))
                                    {
                                        RoundRadius = radius;
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        LogHelper.Debug($"未知的形状: {lastSelectedShape}转为默认选择");
                        _lastSelectedShapeIndex = -1;
                        UpdateShapeButtonStates(-1);
                    }
                }
                else
                {
                    _lastSelectedShapeIndex = -1;
                    SelectedShape = ShapeType.RightAngle; // 重置为默认值
                    _isShapeExplicitlySelected = false; // 用户取消了选择或从未选择
                    RoundRadius = 0;
                    UpdateShapeButtonStates(-1); // 不选中任何按钮
                    if (radiusTextBox != null) radiusTextBox.Visible = false;

                    if (lastSelectedShape == "NONE")
                    {
                        LogHelper.Debug("检测到用户明确取消形状选择，保持未选择状态");
                    }
                    else
                    {
                        LogHelper.Debug("用户从未选择任何形状，保持未选择状态");
                    }
                }

                // 在恢复形状选择状态后，更新尺寸显示以包含正确的形状代号
                if (_initialWidth > 0 && _initialHeight > 0)
                {
                    UpdateDimensionsWithBleed();
                    LogHelper.Debug($"形状状态恢复后更新尺寸显示: {AdjustedDimensions}");
                }

                // 恢复增量值
                string lastIncrement = AppSettings.Get("LastIncrementValue")?.ToString();
                if (!string.IsNullOrEmpty(lastIncrement) && int.TryParse(lastIncrement, out int increment))
                {
                    Increment = increment;
                    if (incrementTextBox != null)
                    {
                        incrementTextBox.Text = lastIncrement;
                    }
                }

                // 恢复标识页状态
                bool markPageEnabled = AppSettings.GetValue<bool>("MarkPageEnabled");
                if (chkIdentifierPage != null)
                {
                    chkIdentifierPage.Checked = markPageEnabled;
                    AddIdentifierPage = markPageEnabled;
                    LogHelper.Debug($"恢复标识页状态: {markPageEnabled}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("加载上次设置失败: " + ex.Message, ex);
            }
        }

        
        private void MaterialButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 取消dropdown16的选择状态
                ClearDropdown16Selection();

                // 获取当前主题
                var themeManager = ServiceLocator.Instance.GetThemeManager();
                ThemeDefinition theme = themeManager?.GetCurrentTheme();
                bool isDark = theme != null && IsThemeDark(theme);

                if (sender is AntdUI.Button clickedButton)
                {
                    // 检查是否点击了已选中的按钮
                    bool isCurrentlySelected = SelectedMaterial == clickedButton.Text;

                    if (isCurrentlySelected)
                    {
                        // 取消选择
                        SelectedMaterial = null;
                        // 重置所有Tab页中所有按钮状态为默认
                        var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                        if (tabControl != null)
                        {
                            foreach (var tabPage in tabControl.Pages)
                            {
                                foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                                {
                                    btn.Type = AntdUI.TTypeMini.Default; // 重置为默认样式
                                    // 重新应用主题样式以更新ForeColor
                                    if (theme != null) ApplyThemeToMaterialButton(btn, theme, isDark);
                                }
                            }
                        }

                        // 保存取消选择的状态到AppSettings
                        AppSettings.Set("LastSelectedMaterial", "");
                        AppSettings.Save();

                        LogHelper.Debug($"取消选择材料: {clickedButton.Text}");
                    }
                else
                {
                    // 重置所有Tab页中所有按钮的状态
                    var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                    if (tabControl != null)
                    {
                        foreach (var tabPage in tabControl.Pages)
                        {
                            foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                            {
                                btn.Type = AntdUI.TTypeMini.Default; // 重置为默认样式
                                // 重新应用主题样式以更新ForeColor
                                if (theme != null) ApplyThemeToMaterialButton(btn, theme, isDark);
                            }
                        }
                    }

                    // 设置选中按钮状态
                    SelectedMaterial = clickedButton.Text;
                    clickedButton.Type = AntdUI.TTypeMini.Primary; // 高亮显示选中状态
                    // 重新应用主题样式以更新选中按钮的ForeColor
                    if (theme != null) ApplyThemeToMaterialButton(clickedButton, theme, isDark);

                    // 保存选择的材料
                    AppSettings.Set("LastSelectedMaterial", SelectedMaterial);
                    AppSettings.Save();

                    LogHelper.Debug($"选择材料: {SelectedMaterial}");
                }
                }
                else
                {
                    LogHelper.Debug("未知的按钮点击事件");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"材料按钮点击事件处理失败: {ex.Message}", ex);
                MessageBox.Show($"材料选择失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dropdown16_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // 获取当前主题
                var themeManager = ServiceLocator.Instance.GetThemeManager();
                ThemeDefinition theme = themeManager?.GetCurrentTheme();
                bool isDark = theme != null && IsThemeDark(theme);

                // 获取选择的值
                if (!string.IsNullOrEmpty(dropdown16.Text) && dropdown16.Text != "更多材料")
                {
                    // 检查是否选择了"取消选择"选项
                    if (dropdown16.Text == "取消选择")
                    {
                        // 取消选择
                        SelectedMaterial = null;
                        dropdown16.Text = "更多材料";

                        // 重置所有Tab页中所有按钮的状态
                        var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                        if (tabControl != null)
                        {
                            foreach (var tabPage in tabControl.Pages)
                            {
                                foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                                {
                                    btn.Type = AntdUI.TTypeMini.Default;
                                    // 重新应用主题样式以更新ForeColor
                                    if (theme != null) ApplyThemeToMaterialButton(btn, theme, isDark);
                                }
                            }
                        }

                        // 重置下拉框为默认状态
                        ResetDropdown16Style();

                        // 保存取消选择的状态到AppSettings
                        AppSettings.Set("LastSelectedMaterial", "");
                        AppSettings.Save();

                        LogHelper.Debug("通过'取消选择'选项清空材料选择");
                        return;
                    }

                    string selectedMaterial = dropdown16.Text;

                    // 检查是否点击了已选中的材料（用于取消选择）
                    bool isCurrentlySelected = SelectedMaterial == selectedMaterial;

                    if (isCurrentlySelected)
                    {
                        // 取消选择
                        SelectedMaterial = null;
                        dropdown16.Text = "更多材料";

                        // 重置所有Tab页中所有按钮的状态
                        var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                        if (tabControl != null)
                        {
                            foreach (var tabPage in tabControl.Pages)
                            {
                                foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                                {
                                    btn.Type = AntdUI.TTypeMini.Default;
                                    // 重新应用主题样式以更新ForeColor
                                    if (theme != null) ApplyThemeToMaterialButton(btn, theme, isDark);
                                }
                            }
                        }

                        // 重置下拉框为默认状态
                        ResetDropdown16Style();

                        // 保存取消选择的状态到AppSettings
                        AppSettings.Set("LastSelectedMaterial", "");
                        AppSettings.Save();

                        LogHelper.Debug($"取消选择材料: {selectedMaterial}");
                    }
                    else
                    {
                        // 重置所有Tab页中所有按钮的状态
                        var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                        if (tabControl != null)
                        {
                            foreach (var tabPage in tabControl.Pages)
                            {
                                foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                                {
                                    btn.Type = AntdUI.TTypeMini.Default;
                                    // 重新应用主题样式以更新ForeColor
                                    if (theme != null) ApplyThemeToMaterialButton(btn, theme, isDark);
                                }
                            }
                        }

                        // 设置选择的材料
                        SelectedMaterial = selectedMaterial;
                        
                        // ✅ 设置下拉框为激活状态（与材料按钮一致）
                        SetDropdown16ActiveStyle();

                        // 保存选择的材料
                        AppSettings.Set("LastSelectedMaterial", SelectedMaterial);
                        AppSettings.Save();

                        LogHelper.Debug($"从下拉框选择材料: {SelectedMaterial}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"下拉框选择材料失败: {ex.Message}", ex);
            }
        }

      
        // 颜色模式和膜类型的CheckedChanged事件已使用lambda表达式在InitializeEventHandlers方法中处理

        private void ConfirmButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (ValidateForm())
                {
                    // 保存所有数据到属性
                    SaveFormData();

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (ArgumentException argEx)
            {
                MessageBox.Show($"参数错误: {argEx.Message}\n\n请检查输入数据是否正确", "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"确认操作失败: {ex.Message}\n\n错误类型: {ex.GetType().Name}", "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            try
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"取消操作失败: {ex.Message}", ex);
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
        
        /// <summary>
        /// 标识页复选框状态变化事件
        /// </summary>
        private void ChkIdentifierPage_CheckedChanged(object sender, AntdUI.BoolEventArgs e)
        {
            try
            {
                AddIdentifierPage = e.Value;

                // 保存标识页状态到设置
                AppSettings.Set("MarkPageEnabled", AddIdentifierPage);
                AppSettings.Save();

                LogHelper.Debug($"标识页设置: {AddIdentifierPage} (已保存)");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"标识页设置失败: {ex.Message}", ex);
            }
        }

        // 添加Excel数据（如果有）
        public void AddExcelData(DataTable excelData, int searchColumnIndex, int returnColumnIndex, int serialColumnIndex, int newColumnIndex)
        {
            LoadExcelData(excelData, searchColumnIndex, returnColumnIndex, serialColumnIndex, newColumnIndex);
        }

        // 简化的Excel数据加载方法，支持灵活的列索引
        public void LoadExcelData(DataTable excelData, int searchColumnIndex, int returnColumnIndex, int serialColumnIndex, int newColumnIndex = -1)
        {
            _excelData = excelData;
            _searchColumnIndex = searchColumnIndex;
            _returnColumnIndex = returnColumnIndex;
            _serialColumnIndex = serialColumnIndex;
            _newColumnIndex = newColumnIndex;
        }

        private void UpdateFixedField()
        {
            // 生成FixedField：颜色模式 + 膜类型
            // 当膜类不选择时，FixedField为空，这样重命名就不会包含工艺信息
            if (string.IsNullOrEmpty(FilmType))
            {
                FixedField = "";
            }
            else
            {
                FixedField = ColorMode + FilmType;
            }
        }

        private void SaveFormData()
        {
            // 获取基本数据
            if (orderNumberTextBox != null)
            {
                OrderNumber = orderNumberTextBox.Text;
            }

            if (serialNumberTextBox != null)
            {
                SerialNumber = serialNumberTextBox.Text;
            }

            // 获取数量数据
            if (quantityTextBox != null)
            {
                string quantityText = quantityTextBox.Text;
                if (!string.IsNullOrEmpty(quantityText))
                {
                    try
                    {
                        // 解析多个数量值
                        Quantities = quantityText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(q => q.Trim())
                            .Where(q => !string.IsNullOrEmpty(q))
                            .ToList();
                    }
                    catch (ArgumentException splitEx)
                    {
                        MessageBox.Show($"数量解析错误: {splitEx.Message}", "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        quantityTextBox.Focus();
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"数量处理发生未知错误: {ex.Message}", "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        quantityTextBox.Focus();
                        return;
                    }

                    // 应用增量
                    if (incrementTextBox != null && int.TryParse(incrementTextBox.Text, out int increment))
                    {
                        Increment = increment;
                        if (Quantities != null)
                        {
                            for (int i = 0; i < Quantities.Count; i++)
                            {
                                try
                                {
                                    if (int.TryParse(Quantities[i], out int quantity))
                                    {
                                        Quantities[i] = (quantity + increment).ToString();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"数量增量计算错误 (索引 {i}): {ex.Message}", "计算错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                            }
                        }
                    }

                    if (Quantities != null && Quantities.All(q => q != null))
                    {
                        Quantity = string.Join(",", Quantities);
                    }
                    else
                    {
                        Quantity = "1"; // 默认值
                    }
                }
                else
                {
                    // 默认数量为1
                    Quantities = new List<string> { "1" };
                    Quantity = "1";
                }
            }

            // 获取尺寸信息
            if (dimensionsTextBox != null)
            {
                AdjustedDimensions = dimensionsTextBox.Text;
            }

            // 获取出血位信息
            if (bleedDropdown != null && !string.IsNullOrEmpty(bleedDropdown.Text))
            {
                if (double.TryParse(bleedDropdown.Text, out double bleed))
                {
                    SelectedTetBleed = bleed;
                }
            }

            // 保存增量设置
            if (incrementTextBox != null && int.TryParse(incrementTextBox.Text, out int incrementValue))
            {
                Increment = incrementValue;
                try
                {
                    AppSettings.Set("LastIncrementValue", Increment.ToString());
                    AppSettings.Save();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存增量设置失败: {ex.Message}", "保存错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            // 更新FixedField
            UpdateFixedField();
            
            // 保存标识页设置
            if (chkIdentifierPage != null)
            {
                AddIdentifierPage = chkIdentifierPage.Checked;
            }

            // 保存排版控件状态
            SaveImpositionControlStates();

            // 保存排版材料类型
            // 默认情况下如果排版复选框未选中，不需要更新 ImpositionMaterialType
            // 但如果选中了，必须从RadioButton更新
            if (enableImpositionCheckbox != null && enableImpositionCheckbox.Checked)
            {
                if (flatSheetRadioButton != null)
                {
                    ImpositionMaterialType = flatSheetRadioButton.Checked ? "FlatSheet" : "RollMaterial";
                }
            }
            else
            {
                // 如果没有启用排版，可以清空或者保持上次的值，取决于业务需求
                // 这里我们保持空，或者设为默认值
                ImpositionMaterialType = "";
            }
        }

        private System.Windows.Forms.FolderBrowserDialog FolderBrowserDialog()
        {
            return new System.Windows.Forms.FolderBrowserDialog();
        }

        // 更新窗口标题显示文件名
        public void UpdatePageHeaderSubText(string fileName)
        {
            try
            {
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(fileName);
                UpdateFormTitleWithFileName(fileNameWithoutExtension);
            }
            catch (Exception ex)
            {
                LogHelper.Error("更新窗口标题文件名失败: " + ex.Message, ex);
            }
        }

        // 更新窗体标题显示文件名
        private void UpdateFormTitleWithFileName(string fileName)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(UpdateFormTitleWithFileName), fileName);
                return;
            }

            try
            {
                if (fileNameLabel == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(fileName))
                {
                    fileNameLabel.Text = "材料选择";
                    AutoResizeLabelFont(fileNameLabel);
                    return;
                }

                // 直接显示文件名
                fileNameLabel.Text = fileName;
                AutoResizeLabelFont(fileNameLabel);
            }
            catch (Exception ex)
            {
                LogHelper.Error("更新文件名标签失败: " + ex.Message, ex);
                if (fileNameLabel != null)
                {
                    fileNameLabel.Text = "材料选择";
                    AutoResizeLabelFont(fileNameLabel);
                }
            }
        }



        // 重置窗口标题为默认状态
        public void ResetPageHeaderSubText()
        {
            if (fileNameLabel != null)
            {
                fileNameLabel.Text = "材料选择";
                AutoResizeLabelFont(fileNameLabel);
            }
        }

        // 窗体拖动事件处理
        private void FileNameLabel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
            }
        }

        private void FileNameLabel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                Point currentScreenPos = PointToScreen(e.Location);
                Point newFormPos = new Point(
                    currentScreenPos.X - _dragStartPoint.X,
                    currentScreenPos.Y - _dragStartPoint.Y
                );
                this.Location = newFormPos;
            }
        }

        private void FileNameLabel_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
            }
        }

        // 设置当前文件名并更新页面头部显示
        public void SetCurrentFileName(string fileName)
        {
            CurrentFileName = fileName;
            UpdatePageHeaderWithFileName();
        }

        // 更新窗口标题显示文件名
        private void UpdatePageHeaderWithFileName()
        {
            try
            {
                if (!string.IsNullOrEmpty(CurrentFileName))
                {
                    // 移除文件扩展名，只显示文件名主体
                    string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(CurrentFileName);

                    // 使用新的窗口标题更新方法
                    UpdateFormTitleWithFileName(fileNameWithoutExtension);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("更新窗口标题文件名失败: " + ex.Message, ex);
            }
        }

        // 获取表单数据
        public Dictionary<string, object> GetFormData()
        {
            var formData = new Dictionary<string, object>
            {
                ["Material"] = SelectedMaterial ?? "",
                ["OrderNumber"] = orderNumberTextBox?.Text ?? "",
                ["Quantity"] = quantityTextBox?.Text ?? "",
                ["Increment"] = incrementTextBox?.Text ?? "0",
                ["SerialNumber"] = serialNumberTextBox?.Text ?? "",
                ["ExportPath"] = SelectedExportPath ?? "",
                ["ColorMode"] = ColorMode,
                ["FilmType"] = FilmType,
                ["FixedField"] = FixedField,
                ["Dimensions"] = dimensionsTextBox?.Text ?? "",
                ["Bleed"] = bleedDropdown?.Text ?? "",
                ["AutoIncrement"] = autoIncrementCheckbox?.Checked ?? false
            };

            return formData;
        }

        // 表单验证
        public bool ValidateForm()
        {
            // 材料选择现在是可选的
            // 移除了材料选择的必填验证

            // 验证导出路径
            if (string.IsNullOrEmpty(SelectedExportPath))
            {
                MessageBox.Show("请从左侧菜单选择导出路径", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 验证路径是否存在
            if (!Directory.Exists(SelectedExportPath))
            {
                var result = MessageBox.Show($"选择的路径不存在:\n{SelectedExportPath}\n\n是否创建该文件夹？",
                    "路径验证", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(SelectedExportPath);
                        LogHelper.Debug($"创建导出文件夹: {SelectedExportPath}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"创建文件夹失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // 订单号现在是可选的
            // 移除了订单号的必填验证

            // 验证数量格式
            string quantityText = quantityTextBox?.Text ?? "";
            if (!string.IsNullOrEmpty(quantityText))
            {
                try
                {
                    // 检查数量格式：允许多个数字用逗号分隔
                    if (!System.Text.RegularExpressions.Regex.IsMatch(quantityText, @"^(\d+\s*,\s*)*\d+$"))
                    {
                        MessageBox.Show("数量输入格式错误，请使用逗号分隔多个数量，例如: 100,99,98", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        quantityTextBox.Focus();
                        return false;
                    }
                }
                catch (ArgumentException regexEx)
                {
                    MessageBox.Show($"数量格式验证正则表达式错误: {regexEx.Message}", "系统错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    quantityTextBox.Focus();
                    return false;
                }
            }

            // 验证增量格式
            string incrementText = incrementTextBox?.Text ?? "";
            if (!string.IsNullOrEmpty(incrementText))
            {
                if (!int.TryParse(incrementText, out int increment) || increment < 0)
                {
                    MessageBox.Show("增量必须是正整数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    incrementTextBox.Focus();
                    return false;
                }
            }

            // 验证出血位选择
            if (bleedDropdown == null || string.IsNullOrEmpty(bleedDropdown.Text))
            {
                MessageBox.Show("请选择出血位", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // 验证出血位值是否有效
            if (!double.TryParse(bleedDropdown.Text, out double bleedValue) || bleedValue < 0)
            {
                MessageBox.Show("出血位必须是正数", "验证失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        // 键盘快捷键支持
        protected override bool ProcessCmdKey(ref System.Windows.Forms.Message msg, Keys keyData)
        {
            // 处理 Enter 键 - 强制触发确认按钮，无论焦点在哪个控件上
            if (keyData == Keys.Enter && ModifierKeys == Keys.None)
            {
                // 确保 confirmButton 可用且可见
                if (confirmButton != null && confirmButton.Enabled && confirmButton.Visible)
                {
                    // 直接调用确认按钮的处理逻辑
                    ConfirmButton_Click(confirmButton, EventArgs.Empty);
                    return true;
                }
            }
            
            if (keyData == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
                return true;
            }

            // 保留原有的 Ctrl+Enter 功能
            if (keyData == Keys.Enter && ModifierKeys == Keys.Control)
            {
                if (ValidateForm())
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                return true;
            }

            if (keyData == Keys.F1)
            {
                MessageBox.Show(
                    "材料选择窗体使用说明:\n\n" +
                    "1. 在'材料选择'页面中选择合适的材料\n" +
                    "2. 填写订单信息和工程名称\n" +
                    "3. 从左侧菜单选择导出路径\n" +
                    "4. 在'高级设置'页面中配置工艺参数\n" +
                    "5. 按Ctrl+Enter确认选择\n\n" +
                    "快捷键:\n" +
                    "Esc - 关闭窗口\n" +
                    "Ctrl+Enter - 确认选择",
                    "使用说明",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void quantityTextBox_Click(object sender, EventArgs e)
        {

        }

        
        private void serialNumberLabel_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 检查是否有有效的Excel数据
        /// </summary>
        /// <returns>true表示有有效的Excel数据，false表示无数据或数据无效</returns>
        private bool HasValidExcelData()
        {
            try
            {
                // 检查DataTable是否为null
                if (_excelData == null)
                {
                    LogHelper.Debug("Excel数据检查: DataTable为null");
                    return false;
                }

                // 检查是否有行
                if (_excelData.Rows.Count == 0)
                {
                    LogHelper.Debug("Excel数据检查: 没有行数据");
                    return false;
                }

                // 检查是否有列
                if (_excelData.Columns.Count == 0)
                {
                    LogHelper.Debug("Excel数据检查: 没有列数据");
                    return false;
                }

                // 检查是否所有行都是空的
                bool hasAnyNonEmptyRow = false;
                foreach (DataRow row in _excelData.Rows)
                {
                    bool rowHasData = false;
                    foreach (var item in row.ItemArray)
                    {
                        if (item != null && !string.IsNullOrEmpty(item.ToString()?.Trim()))
                        {
                            rowHasData = true;
                            break;
                        }
                    }
                    if (rowHasData)
                    {
                        hasAnyNonEmptyRow = true;
                        break;
                    }
                }

                if (!hasAnyNonEmptyRow)
                {
                    LogHelper.Debug("Excel数据检查: 所有行都是空数据");
                    return false;
                }

                LogHelper.Debug($"Excel数据检查: 有效数据 ({_excelData.Rows.Count}行, {_excelData.Columns.Count}列)");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"检查Excel数据有效性时出错: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 自动获取并填充序号到serialNumberTextBox
        /// </summary>
        /// <param name="forceFill">是否强制填充（忽略文本框已有内容的检查）</param>
        private void AutoFillSerialNumber(bool forceFill = false)
        {
            LogHelper.Debug($"AutoFillSerialNumber方法被调用，forceFill={forceFill}");
            try
            {
                // 检查是否真的有有效的Excel数据
                bool hasValidExcelData = HasValidExcelData();
                LogHelper.Debug($"Excel数据有效性检查结果: {hasValidExcelData}");

                // 如果有有效的Excel数据且包含序号列，直接使用Excel中的序号
                if (hasValidExcelData && _serialColumnIndex >= 0 && _excelData?.Columns.Count > _serialColumnIndex)
                {
                    LogHelper.Debug("检测到有效的Excel数据且有序号列，尝试使用Excel序号");
                    
                    // 尝试从当前匹配的行中获取序号
                    if (MatchedRows != null && MatchedRows.Count > 0)
                    {
                        List<string> excelSerialNumbers = new List<string>();
                        foreach (DataRow row in MatchedRows)
                        {
                            if (row[_serialColumnIndex] != DBNull.Value)
                            {
                                string serialValue = row[_serialColumnIndex].ToString().Trim();
                                if (!string.IsNullOrEmpty(serialValue))
                                {
                                    excelSerialNumbers.Add(serialValue);
                                }
                            }
                        }
                        
                        if (excelSerialNumbers.Count > 0)
                        {
                            // 使用Excel中的序号（支持任意字符）
                            string excelSerialNumbersStr = string.Join(",", excelSerialNumbers);
                            if (serialNumberTextBox != null)
                            {
                                serialNumberTextBox.Text = excelSerialNumbersStr;
                                this.SerialNumber = excelSerialNumbersStr;
                                LogHelper.Debug($"使用Excel序号: {excelSerialNumbersStr}");
                            }
                            return;
                        }
                    }
                    
                    // 如果没有匹配的行但有Excel数据，尝试获取Excel中的序号列数据
                    if (string.IsNullOrEmpty(this.SerialNumber) || forceFill)
                    {
                        List<string> allSerialNumbers = new List<string>();
                        foreach (DataRow row in _excelData.Rows)
                        {
                            if (row[_serialColumnIndex] != DBNull.Value)
                            {
                                string serialValue = row[_serialColumnIndex].ToString().Trim();
                                if (!string.IsNullOrEmpty(serialValue))
                                {
                                    allSerialNumbers.Add(serialValue);
                                }
                            }
                        }
                        
                        if (allSerialNumbers.Count > 0)
                        {
                            string excelSerialNumbersStr = string.Join(",", allSerialNumbers);
                            if (serialNumberTextBox != null)
                            {
                                serialNumberTextBox.Text = excelSerialNumbersStr;
                                this.SerialNumber = excelSerialNumbersStr;
                                LogHelper.Debug($"使用Excel所有序号: {excelSerialNumbersStr}");
                            }
                            return;
                        }
                    }
                    
                    LogHelper.Debug("Excel数据中未找到有效的序号值");
                }

                // 只有在没有有效的Excel表格数据时才自动获取dgvFiles的最后一行序号
                if (!hasValidExcelData)
                {
                    // 如果不是强制填充且文本框已经有内容，不自动覆盖
                    if (!forceFill && !string.IsNullOrEmpty(serialNumberTextBox?.Text))
                    {
                        LogHelper.Debug("序号文本框已有内容，不自动填充");
                        return;
                    }

                    // 尝试获取主窗体的dgvFiles数据
                    var mainForm = this.Owner;
                    if (mainForm != null)
                    {
                        // 通过反射或公共接口获取Form1的dgvFiles数据源
                        var dataGridProperty = mainForm.GetType().GetProperty("DataGrid");
                        if (dataGridProperty != null)
                        {
                            var dgvFiles = dataGridProperty.GetValue(mainForm) as DataGridView;
                            if (dgvFiles?.DataSource is System.ComponentModel.BindingList<FileRenameInfo> bindingList && bindingList.Any())
                            {
                                // 查找最后一行有数据行的序号值
                                int maxSerial = 0;
                                foreach (var item in bindingList)
                                {
                                    if (int.TryParse(item.SerialNumber, out int currentSerial))
                                    {
                                        if (currentSerial > maxSerial)
                                        {
                                            maxSerial = currentSerial;
                                        }
                                    }
                                }

                                // 递增1并格式化为两位数
                                string nextSerialNumber = (maxSerial + 1).ToString("D2");

                                // 更新序号文本框
                                if (serialNumberTextBox != null)
                                {
                                    serialNumberTextBox.Text = nextSerialNumber;
                                    this.SerialNumber = nextSerialNumber;
                                    LogHelper.Debug($"自动获取下一序号: {nextSerialNumber} (当前最大序号: {maxSerial})");
                                }
                            }
                            else
                            {
                                // 如果没有数据，默认为01
                                if (serialNumberTextBox != null)
                                {
                                    serialNumberTextBox.Text = "01";
                                    this.SerialNumber = "01";
                                    LogHelper.Debug("dgvFiles无数据，设置默认序号: 01");
                                }
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Debug("无法获取主窗体引用，无法自动获取序号");
                    }
                }
                else
                {
                    LogHelper.Debug("已有Excel导入数据，不自动获取dgvFiles序号");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"自动获取序号时出错: {ex.Message}", ex);
            }
        }

        private void serialNumberTextBox_Click(object sender, EventArgs e)
        {
            AutoFillSerialNumber();
        }

        private void serialNumberTextBox_Enter(object sender, EventArgs e)
        {
            // 当用户进入序号文本框时，如果文本框为空且没有Excel数据，自动填充序号
            AutoFillSerialNumber();
        }

        private void autoIncrementCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (autoIncrementCheckbox != null)
                {
                    // 使用与现有自动递增逻辑相同的设置键
                    AppSettings.Set("AutoIncrementOrderNumber1", autoIncrementCheckbox.Checked);
                    AppSettings.Save();
                    LogHelper.Debug($"保存自动递增状态: {autoIncrementCheckbox.Checked}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存自动递增状态失败: {ex.Message}", ex);
            }
        }

        #region 阶段1：核心数据迁移方法

        /// <summary>
        /// 初始化工具提示
        /// </summary>
        private void InitializeToolTip()
        {
            // 创建并优化ToolTip性能
            _toolTip = new ToolTip
            {
                // 减少首次显示提示的延迟（毫秒）
                InitialDelay = 100,
                // 减少移动到其他控件时重新显示提示的延迟
                ReshowDelay = 50,
                // 增加提示保持可见的时间
                AutoPopDelay = 5000,
                // 设置为true以确保提示立即显示
                ShowAlways = true
            };
        }

        /// <summary>
        /// 设置订单号输入框焦点
        /// </summary>
        private void SetOrderTextBoxFocus()
        {
            var foundOrderTextBox = Controls.Find("orderNumberTextBox", true).FirstOrDefault() as AntdUI.Input;
            if (foundOrderTextBox != null && !foundOrderTextBox.IsDisposed)
            {
                foundOrderTextBox.Focus();
                foundOrderTextBox.SelectAll();
            }
        }

        /// <summary>
        /// 窗体加载事件处理
        /// </summary>
        private void MaterialSelectFormModern_Load(object sender, EventArgs e)
        {
            LogHelper.Debug("MaterialSelectFormModern_Load事件被触发");
            
            // 延迟显示窗体：先设置透明，等PDF准备好后再恢复显示
            this.Opacity = 0;

            // 使用API设置窗口透明度，确保文字不透明
            if (_opacityValue < 1.0)
            {
                // 设置窗口为分层窗口
                SetWindowLong(this.Handle, GWL_EXSTYLE, GetWindowLong(this.Handle, GWL_EXSTYLE) | WS_EX_LAYERED);
                // 设置透明度 (0-255)
                byte alpha = (byte)(_opacityValue * 255);
                SetLayeredWindowAttributes(this.Handle, 0, alpha, LWA_ALPHA);
            }
            
            // 加载材料设置
            LoadMaterialsFromSettings();

            // ✅ 关键修复：在材料按钮加载完成后，立即恢复上次的材料选择状态
            LoadLastSelectedMaterial();

            // 恢复排版控件状态
            LoadImpositionControlStates();

            // ✅ 初始化预览面板为折叠状态（设计器中为180px，需要运行时重置）
            if (pdfPreviewPanel != null)
            {
                pdfPreviewPanel.Height = 0;
                _isPreviewExpanded = false;
                previewCollapseButton.Text = "▼"; // 折叠时显示下箭头
            }
            
            // ✅ 重置预览面板为折叠状态
            pdfPreviewPanel.Height = 0;
            this.ClientSize = new System.Drawing.Size(400, 644); // 初始设置为折叠状态（包含文件名区域）

            // ✅ 保存当前需要加载的PDF文件路径
            if (!string.IsNullOrEmpty(CurrentFileName) && File.Exists(CurrentFileName))
            {
                _pendingPdfToLoad = CurrentFileName;
                LogHelper.Debug($"[PDF 预览] Load 事件中标记待加载 PDF: {CurrentFileName}");
            }

            // 🔧 优化：预览状态已在PrePositionWindow中恢复，这里只做一致性检查
            EnsurePreviewStateConsistency();

            // ✅ 订阅 PDF 预览控件的事件，更新页码显示
            var pdfControl = PdfPreview;
            if (pdfControl != null)
            {
                pdfControl.PageLoaded += PdfPreviewControl_PageLoaded;
                // 🔧 新增：监听翻页事件，实时更新页码
                pdfControl.PageChanged += PdfPreviewControl_PageChanged;
            }

            // 检查Excel数据的有效性
            bool hasValidExcelData = HasValidExcelData();
            LogHelper.Debug($"Excel数据有效性: {hasValidExcelData}");

            // 在没有有效的导入表格时，自动根据dgvFiles的最后一行序号填充serialNumberTextBox
            if (!hasValidExcelData)
            {
                LogHelper.Debug("开始调用AutoFillSerialNumber进行自动序号填充");
                AutoFillSerialNumber(true); // 强制填充，忽略已有内容检查
                LogHelper.Debug("AutoFillSerialNumber调用完成");
            }
            else
            {
                LogHelper.Debug("跳过自动序号填充：存在有效的Excel数据");
            }

            // 🔧 构造函数预定位优化：窗口位置已在构造函数中设置，无需再次移动
            LogHelper.Debug("[MaterialSelectFormModern] 构造函数预定位已生效，跳过动画恢复");

            // 🔧 应用当前主题
            ApplyCurrentTheme();
        }

        /// <summary>
        /// 从AppSettings.Material动态加载材料到按钮和下拉框
        /// </summary>
        private void LoadMaterialsFromSettings()
        {
            try
            {
                // 从AppSettings.Material获取材料字符串
                string materialStr = AppSettings.Material ?? string.Empty;

                // 如果材料字符串为空，使用默认材料
                if (string.IsNullOrEmpty(materialStr))
                {
                    LogHelper.Debug("AppSettings.Material为空，使用默认材料列表");
                    LoadDefaultMaterials();
                    return;
                }

                // 解析材料字符串（支持逗号和竖线分隔）
                var materials = materialStr.Split(new[] { ',', '|', '，', '、' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim())
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();

                if (materials.Count == 0)
                {
                    LogHelper.Debug("解析材料列表为空，使用默认材料列表");
                    LoadDefaultMaterials();
                    return;
                }

                LogHelper.Debug($"从AppSettings.Material加载了{materials.Count}个材料: {string.Join(", ", materials)}");

                // 更新内部材料数组
                _materials = materials.ToArray();

                // 动态加载材料到按钮和下拉框
                LoadMaterialsToControls(materials);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"从AppSettings.Material加载材料失败: {ex.Message}");
                LoadDefaultMaterials();
            }
        }

        /// <summary>
        /// 加载默认材料列表
        /// </summary>
        private void LoadDefaultMaterials()
        {
            var defaultMaterials = new List<string>
            {
                "PET", "PP", "PVC", "PET环保", "PET透明", "PET哑光", "PET镭射", "PET磨砂",
                "PET金色", "PET银色", "PET白色", "PET红色", "PET蓝色", "PET绿色", "PP环保"
            };

            _materials = defaultMaterials.ToArray();
            LoadMaterialsToControls(defaultMaterials);
        }

        /// <summary>
        /// 将材料列表加载到控件中
        /// </summary>
        /// <param name="materials">材料列表</param>
        private void LoadMaterialsToControls(List<string> materials)
        {
            try
            {
                // 获取所有材料按钮控件
                var materialButtons = new List<AntdUI.Button>
                {
                    materialButton1, materialButton2, materialButton3, materialButton4, materialButton5,
                    materialButton6, materialButton7, materialButton8, materialButton9, materialButton10,
                    materialButton11, materialButton12, materialButton13, materialButton14, materialButton15
                };

                // 清空dropdown16的项目
                dropdown16.Items.Clear();
                dropdown16.Text = "更多材料";

                // 前15个材料显示在按钮上
                for (int i = 0; i < 15 && i < materials.Count; i++)
                {
                    if (i < materialButtons.Count)
                    {
                        materialButtons[i].Text = materials[i];
                        materialButtons[i].Visible = true;
                    }
                }

                // 隐藏多余的按钮
                for (int i = materials.Count; i < 15 && i < materialButtons.Count; i++)
                {
                    materialButtons[i].Visible = false;
                }

                // 超过15个的材料添加到dropdown16
                if (materials.Count > 15)
                {
                    var extraMaterials = materials.Skip(15).ToList();
                    foreach (var material in extraMaterials)
                    {
                        dropdown16.Items.Add(material);
                    }
                    dropdown16.Visible = true;
                }
                else
                {
                    dropdown16.Visible = false;
                }

                LogHelper.Debug($"材料加载完成: 按钮显示{Math.Min(15, materials.Count)}个，下拉框显示{Math.Max(0, materials.Count - 15)}个");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载材料到控件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 窗体显示事件处理
        /// </summary>
        private void MaterialSelectFormModern_Shown(object sender, EventArgs e)
        {
            // 确保窗体始终在最前端显示
            TopMost = true;
            this.Activate();

            // 在窗体显示后设置焦点，确保句柄已创建
            this.BeginInvoke(new Action(SetOrderTextBoxFocus));

            // 添加PDF预览检查，确保PDF能够自动加载
            this.BeginInvoke(new Action(async () =>
            {
                await Task.Delay(300); // 等待窗体完全渲染
                await TryLoadPendingPdf();
                LogHelper.Debug("[PDF 预览] Shown事件中检查PDF加载");
                
                // 额外刷新PDF预览控件
                await Task.Delay(200);
                if (_isPreviewExpanded && PdfPreview != null)
                {
                    PdfPreview.ApplyBestFitZoomPublic();
                    LogHelper.Debug("[PDF 预览] Shown事件中额外刷新PDF预览");
                }
                
                // 🔧 最终确认：再次延迟后强制应用缩放（确保万无一失）
                await Task.Delay(300);
                if (_isPreviewExpanded && PdfPreview != null && PdfPreview.PageCount > 0)
                {
                    LogHelper.Debug("[PDF 预览] Shown事件最终确认应用缩放");
                    PdfPreview.ApplyBestFitZoomPublic();
                }
                
                // 窗体内容准备好后，恢复显示
                if (this.Opacity == 0)
                {
                    this.Opacity = _opacityValue > 0 ? _opacityValue : 1.0;
                    LogHelper.Debug("[PDF 预览] 窗体内容准备完成，恢复显示");
                }
            }));
        }

        /// <summary>
        /// 窗体关闭事件处理 - 保存窗口位置和状态
        /// </summary>
        private void MaterialSelectFormModern_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // ✅ 关闭PDF预览，释放文件句柄
                PdfPreview?.ClosePdf();
                LogHelper.Debug("[MaterialSelectFormModern] 已关闭PDF预览，释放文件句柄");
                
                // 保存窗口位置和状态
                WindowPositionManager.SaveWindowPosition(this, _isPreviewExpanded);
                LogHelper.Debug($"[MaterialSelectFormModern] 保存窗口位置: Location={this.Location}, Size={this.Size}, PreviewExpanded={_isPreviewExpanded}");

                // 🔧 关键修复：立即提交所有待处理的设置更改，确保窗口位置被保存到文件
                // 不能只依赖5秒自动保存定时器，因为窗口关闭的速度可能更快
                AppSettings.CommitChanges();
                LogHelper.Debug("[MaterialSelectFormModern] 已立即提交窗口位置设置到文件");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 保存窗口位置失败: {ex.Message}", ex);
            }

            LogHelper.Debug("[MaterialSelectFormModern] 窗体关闭完成");
        }

        /// <summary>
        /// 自动递增订单号中的数字
        /// </summary>
        private void IncrementLastNumberInOrderText()
        {
            var orderTextBox = Controls.Find("orderNumberTextBox", true).FirstOrDefault() as AntdUI.Input;
            if (orderTextBox == null) return;

            string currentText = orderTextBox.Text;
            if (string.IsNullOrEmpty(currentText)) return;

            // 使用正则表达式找到最后一组连续的数字
            Match match = Regex.Match(currentText, @"(.*?)(\d+)$");
            if (match.Success)
            {
                string prefix = match.Groups[1].Value;
                string lastNumber = match.Groups[2].Value;

                if (int.TryParse(lastNumber, out int number))
                {
                    int newNumber = number + 1;
                    string newText = prefix + newNumber.ToString().PadLeft(lastNumber.Length, '0');

                    orderTextBox.Text = newText;
                    AppSettings.Set("LastOrderNumber1", newText);
                    AppSettings.Save();
                }
            }
        }

        /// <summary>
        /// 设置尺寸数据
        /// </summary>
        private void SetDimensions(string width, string height)
        {
            // 清理输入，移除非数字和小数点字符
            _originalWidth = Regex.Replace(width, @"[^\d.]", "");
            _originalHeight = Regex.Replace(height, @"[^\d.]", "");

            // 确保大数在前（统一输出格式，避免54x84和84x54两种结果）
            if (double.TryParse(_originalWidth, out double w) && double.TryParse(_originalHeight, out double h))
            {
                if (w < h)
                {
                    // 交换宽高
                    var temp = _originalWidth;
                    _originalWidth = _originalHeight;
                    _originalHeight = temp;
                }

                // 初始化计算用的尺寸数据
                _initialWidth = double.TryParse(_originalWidth, out double parsedWidth) ? parsedWidth : 0;
                _initialHeight = double.TryParse(_originalHeight, out double parsedHeight) ? parsedHeight : 0;
            }

            UpdateDimensionsWithBleed();
        }

        /// <summary>
        /// 根据出血位更新尺寸显示
        /// </summary>
        private void UpdateDimensionsWithBleed()
        {
            double bleed = SelectedTetBleed;
            // 使用处理后的原始尺寸计算（已确保大数在前）
            double width = _initialWidth;
            double height = _initialHeight;

            // 使用 DimensionCalculationService 替代 SettingsForm 直接实例化
            var dimensionService = ServiceLocator.Instance.GetDimensionCalculationService();
            string cornerRadius = AppSettings.GetValue<string>("LastCornerRadius") ?? "0";
            // 根据新的形状选择逻辑决定是否启用形状处理
            bool enableShapeProcessing = GetIsShapeSelected();
            string finalDimensions = dimensionService.CalculateFinalDimensions(width, height, bleed, cornerRadius, enableShapeProcessing);

            // 直接使用CalculateFinalDimensions返回的完整结果，包含形状代号
            var dimensionsTextBox = Controls.Find("dimensionsTextBox", true).FirstOrDefault() as AntdUI.Input;
            if (dimensionsTextBox != null)
            {
                dimensionsTextBox.Text = finalDimensions;
            }

            AdjustedDimensions = finalDimensions;
        }

        /// <summary>
        /// 自动填充数量和序号
        /// </summary>
        private void AutoFillQuantity()
        {
            LogHelper.Debug($"AutoFillQuantity 开始执行:");
            LogHelper.Debug($"  - Excel数据行数: {_excelData?.Rows.Count ?? 0}");
            LogHelper.Debug($"  - 搜索列索引: {_searchColumnIndex}");
            LogHelper.Debug($"  - 返回列索引: {_returnColumnIndex}");
            LogHelper.Debug($"  - 序号列索引: {_serialColumnIndex}");
            LogHelper.Debug($"  - 正则结果: '{_regexResult}'");
            LogHelper.Debug($"  - 初始序号: '{this.SerialNumber}'");
            LogHelper.Debug($"  - 已匹配行数: {this.MatchedRows?.Count ?? 0}");

            if (_excelData == null || _excelData.Rows.Count == 0 || _searchColumnIndex < 0 || _returnColumnIndex < 0)
            {
                LogHelper.Debug("AutoFillQuantity 提前退出：不满足基本条件");
                return;
            }

            try
            {
                List<string> quantities = new List<string>();
                List<string> serialNumbers = new List<string>();

                // 优先使用已经传递进来的matchedRows
                if (this.MatchedRows != null && this.MatchedRows.Count > 0)
                {
                    LogHelper.Debug($"使用已传递的matchedRows，行数: {this.MatchedRows.Count}");
                    
                    foreach (DataRow row in this.MatchedRows)
                    {
                        // 收集数量
                        if (row[_returnColumnIndex] != DBNull.Value)
                        {
                            quantities.Add(row[_returnColumnIndex].ToString());
                        }
                        
                        // 收集序号
                        if (_serialColumnIndex >= 0 && _serialColumnIndex < _excelData.Columns.Count && row[_serialColumnIndex] != DBNull.Value)
                        {
                            serialNumbers.Add(row[_serialColumnIndex].ToString());
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(_regexResult))
                {
                    // 如果没有传递matchedRows，则重新匹配
                    LogHelper.Debug($"没有传递matchedRows，重新匹配数据");
                    
                    // 使用正则处理结果在搜索列中查找匹配内容
                    foreach (DataRow row in _excelData.Rows)
                    {
                        if (row[_searchColumnIndex] != DBNull.Value)
                        {
                            string cellValue = row[_searchColumnIndex].ToString().Trim();
                            string searchValue = _regexResult.Trim();
                            // 忽略前后空格并进行包含匹配
                            if (cellValue.IndexOf(searchValue, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                // 找到匹配行，收集数量
                                if (row[_returnColumnIndex] != DBNull.Value)
                                {
                                    quantities.Add(row[_returnColumnIndex].ToString());
                                    this.MatchedRows.Add(row);
                                    // 收集序列号列值
                                    if (_excelData.Columns.Count > _serialColumnIndex && _serialColumnIndex >= 0 && row[_serialColumnIndex] != DBNull.Value)
                                    {
                                        serialNumbers.Add(row[_serialColumnIndex].ToString());
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    LogHelper.Debug("无法填充：没有matchedRows也没有regexResult");
                    return;
                }

                // 设置序号值（支持多个值）
                LogHelper.Debug($"收集到的序号数量: {serialNumbers.Count}");
                if (serialNumbers.Count > 0)
                {
                    this.SerialNumber = string.Join(",", serialNumbers); // 连接所有序号值
                    LogHelper.Debug($"从Excel设置序号: '{this.SerialNumber}'");
                    // 更新序号文本框显示
                    var serialNumberTextBox = Controls.Find("serialNumberTextBox", true).FirstOrDefault() as AntdUI.Input;
                    if (serialNumberTextBox != null)
                    {
                        serialNumberTextBox.Text = this.SerialNumber;
                        LogHelper.Debug($"成功更新序号文本框: '{this.SerialNumber}'");
                    }
                    else
                    {
                        LogHelper.Debug("未找到序号文本框控件");
                    }
                }
                else
                {
                    // 如果没有从Excel获取到序号，使用传入的序号
                    LogHelper.Debug($"没有从Excel获取到序号，使用传入序号: '{this.SerialNumber}'");
                    var serialNumberTextBox = Controls.Find("serialNumberTextBox", true).FirstOrDefault() as AntdUI.Input;
                    if (serialNumberTextBox != null && !string.IsNullOrEmpty(this.SerialNumber))
                    {
                        serialNumberTextBox.Text = this.SerialNumber;
                        LogHelper.Debug($"使用传入序号成功更新文本框: '{this.SerialNumber}'");
                    }
                    else
                    {
                        LogHelper.Debug($"序号文本框为空或传入序号为空: TextBox={serialNumberTextBox != null}, SerialNumber='{this.SerialNumber}'");
                    }
                }

                // 填充所有匹配的数量，用逗号分隔
                var quantityTextBox = Controls.Find("quantityTextBox", true).FirstOrDefault() as AntdUI.Input;
                if (quantityTextBox != null && quantities.Count > 0)
                {
                    quantityTextBox.Text = string.Join(",", quantities);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("自动填充数量失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 加载上次选择的材料
        /// </summary>
        private void LoadLastSelectedMaterial()
        {
            try
            {
                // 加载上次选择的材料
                string lastMaterial = AppSettings.GetValue<string>("LastSelectedMaterial");
                
                // 如果配置中保存的是空字符串或null，表示上次没有选择材料
                if (string.IsNullOrEmpty(lastMaterial))
                {
                    SelectedMaterial = null;
                    // 确保所有按钮都是默认状态
                    ResetAllMaterialButtonsToDefault();
                    // 重置下拉框为默认状态
                    if (dropdown16 != null)
                    {
                        dropdown16.Text = "更多材料";
                        // ✅ 重置下拉框为默认状态
                        ResetDropdown16Style();
                    }
                    LogHelper.Debug("没有上次选择的材料记录，已清空所有选择状态");
                    return;
                }

                bool foundInButtons = false;

                // 先查找TabControl中的材料按钮
                var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                if (tabControl != null)
                {
                    // 遍历所有TabPage
                    foreach (var tabPage in tabControl.Pages)
                    {
                        // 查找TabPage中的材料按钮
                        foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                        {
                            if (btn.Text == lastMaterial)
                            {
                                SelectedMaterial = btn.Text;
                                btn.Type = AntdUI.TTypeMini.Primary;
                                foundInButtons = true;
                                LogHelper.Debug($"从按钮恢复材料选择: {SelectedMaterial}");
                                break;
                            }
                        }
                        if (foundInButtons) break;
                    }
                }

                // 如果在按钮中没有找到，检查dropdown16
                if (!foundInButtons && dropdown16 != null)
                {
                    for (int i = 0; i < dropdown16.Items.Count; i++)
                    {
                        if (dropdown16.Items[i].ToString() == lastMaterial)
                        {
                            SelectedMaterial = lastMaterial;
                            dropdown16.Text = lastMaterial;
                            
                            // ✅ 设置下拉框为激活状态
                            SetDropdown16ActiveStyle();
                            
                            LogHelper.Debug($"从下拉框恢复材料选择: {SelectedMaterial}");
                            foundInButtons = true; // 标记为已找到
                            break;
                        }
                    }
                }

                // 如果没有找到任何匹配的材料，清除选择并重置UI
                if (!foundInButtons)
                {
                    SelectedMaterial = null;
                    ResetAllMaterialButtonsToDefault();
                    if (dropdown16 != null)
                    {
                        dropdown16.Text = "更多材料";
                        // ✅ 重置下拉框为默认状态
                        ResetDropdown16Style();
                    }
                    LogHelper.Debug($"未找到上次选择的材料: {lastMaterial}，已清空选择状态");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载上次选择的材料失败: {ex.Message}", ex);
                SelectedMaterial = null;
            }
        }

        /// <summary>
        /// 重置所有材料按钮为默认状态
        /// </summary>
        private void ResetAllMaterialButtonsToDefault()
        {
            try
            {
                var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                if (tabControl != null)
                {
                    foreach (var tabPage in tabControl.Pages)
                    {
                        foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                        {
                            btn.Type = AntdUI.TTypeMini.Default;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"重置材料按钮状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 设置下拉框为激活状态（与材料按钮一致）
        /// </summary>
        private void SetDropdown16ActiveStyle()
        {
            try
            {
                if (dropdown16 != null)
                {
                    // 使用边框颜色设置激活状态（主Color.FromArgb(24, 144, 255)）
                    dropdown16.BorderColor = Color.FromArgb(24, 144, 255);
                    dropdown16.BorderWidth = 2F;
                    LogHelper.Debug("下拉框设置为激活状态");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"设置下拉框激活状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 重置下拉框为默认状态
        /// </summary>
        private void ResetDropdown16Style()
        {
            try
            {
                if (dropdown16 != null)
                {
                    // 重置为默认边框颜色（灰色 Color.FromArgb(217, 217, 217)）
                    dropdown16.BorderColor = Color.FromArgb(217, 217, 217);
                    dropdown16.BorderWidth = 2F;
                    LogHelper.Debug("下拉框重置为默认状态");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"重置下拉框状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 下拉框双击事件处理 - 快速取消选择
        /// </summary>
        private void Dropdown16_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (SelectedMaterial != null)
                {
                    // 取消选择
                    SelectedMaterial = null;
                    dropdown16.Text = "更多材料";

                    // 重置所有Tab页中所有按钮的状态
                    var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                    if (tabControl != null)
                    {
                        foreach (var tabPage in tabControl.Pages)
                        {
                            foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                            {
                                btn.Type = AntdUI.TTypeMini.Default;
                            }
                        }
                    }

                    // 重置下拉框为默认状态
                    ResetDropdown16Style();

                    // 保存取消选择的状态到AppSettings
                    AppSettings.Set("LastSelectedMaterial", "");
                    AppSettings.Save();

                    LogHelper.Debug("通过双击取消选择材料");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"双击取消选择失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 通用的取消dropdown16选择状态方法
        /// </summary>
        private void ClearDropdown16Selection()
        {
            try
            {
                if (SelectedMaterial != null)
                {
                    SelectedMaterial = null;
                    dropdown16.Text = "更多材料";

                    // 重置所有Tab页中所有按钮的状态
                    var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                    if (tabControl != null)
                    {
                        foreach (var tabPage in tabControl.Pages)
                        {
                            foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                            {
                                btn.Type = AntdUI.TTypeMini.Default;
                            }
                        }
                    }

                    // 重置下拉框为默认状态
                    ResetDropdown16Style();

                    // 保存取消选择的状态到AppSettings
                    AppSettings.Set("LastSelectedMaterial", "");
                    AppSettings.Save();

                    LogHelper.Debug("通过按钮操作取消选择材料");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"取消dropdown16选择失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 出血位下拉框选择变更事件处理
        /// </summary>
        private void BleedDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (bleedDropdown != null && !string.IsNullOrEmpty(bleedDropdown.Text))
                {
                    if (double.TryParse(bleedDropdown.Text, out double selectedBleed))
                    {
                        SelectedTetBleed = selectedBleed;
                        UpdateDimensionsWithBleed(); // 更新尺寸显示

                        // 触发布局重新计算
                        if (enableImpositionCheckbox?.Checked == true)
                        {
                            LogHelper.Debug($"[MaterialSelectFormModern] 出血值变更为 {selectedBleed}，触发布局重新计算");
                            Task.Run(() => CalculateAndUpdateLayout());
                        }

                        // 保存用户选择的出血位值
                        AppSettings.Set("LastSelectedTetBleed", SelectedTetBleed.ToString());
                        AppSettings.Save();

                        LogHelper.Debug($"选择出血位: {SelectedTetBleed} (已保存到设置)");
                    }
                    else
                    {
                        LogHelper.Debug($"无法解析出血位值: '{bleedDropdown.Text}'");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"出血位选择事件处理失败: {ex.Message}", ex);
                MessageBox.Show($"出血位选择失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 增量文本框失去焦点事件处理
        /// </summary>
        private void IncrementTextBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (incrementTextBox != null)
                {
                    AppSettings.Set("LastIncrementValue", incrementTextBox.Text);
                    AppSettings.Save();
                    LogHelper.Debug($"保存增量值: {incrementTextBox.Text}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存增量值失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 数量文本框失去焦点事件处理
        /// </summary>
        private void QuantityTextBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (quantityTextBox != null)
                {
                    // 验证数量格式
                    string quantityText = quantityTextBox.Text;
                    if (!string.IsNullOrEmpty(quantityText))
                    {
                        // 检查数量格式：允许多个数字用逗号分隔
                        if (!Regex.IsMatch(quantityText, @"^(\d+\s*,\s*)*\d+$"))
                        {
                            MessageBox.Show("数量输入格式错误，请使用逗号分隔多个数量，例如: 100,99,98", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            quantityTextBox.Focus();
                            return;
                        }
                    }
                    LogHelper.Debug($"数量文本框失去焦点: {quantityText}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"数量验证失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 订单号文本框失去焦点事件处理
        /// </summary>
        private void OrderNumberTextBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (orderNumberTextBox != null)
                {
                    AppSettings.Set("LastOrderNumber1", orderNumberTextBox.Text);
                    AppSettings.Save();
                    LogHelper.Debug($"保存订单号: {orderNumberTextBox.Text}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存订单号失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 阶段3：形状处理功能

        /// <summary>
        /// 初始化形状控件状态
        /// </summary>
        private void InitializeShapeControls()
        {
            try
            {
                // 恢复上次选择的形状
                string lastShape = AppSettings.GetValue<string>("LastSelectedShape") ?? "";

                bool showRadiusInput = false;

                if (!string.IsNullOrEmpty(lastShape))
                {
                    // 检查是否是用户明确取消选择的标记
                    if (lastShape == "NONE")
                    {
                        // 用户明确取消了选择，保持未选择状态
                        SelectedShape = ShapeType.RightAngle; // 默认值
                        _isShapeExplicitlySelected = false; // 用户明确取消选择
                        RoundRadius = 0;
                        showRadiusInput = false;
                        LogHelper.Debug("检测到用户明确取消形状选择，保持未选择状态");
                    }
                    // 尝试解析为枚举值
                    else if (Enum.TryParse<ShapeType>(lastShape, out ShapeType shapeType))
                    {
                        SelectedShape = shapeType;
                        _isShapeExplicitlySelected = true; // 从设置恢复的形状也是明确选择

                        switch (shapeType)
                        {
                            case ShapeType.RightAngle:
                                RoundRadius = 0;
                                showRadiusInput = false;
                                // ✅ 修复：恢复上次选择的直角时也应该保持明确选择状态
                                break;
                            case ShapeType.Circle:
                                RoundRadius = 0;
                                showRadiusInput = false;
                                break;
                            case ShapeType.Special:
                                RoundRadius = 0;
                                showRadiusInput = false;
                                break;
                            case ShapeType.RoundRect:
                                showRadiusInput = true;
                                // 恢复圆角值
                                string savedRadius = AppSettings.GetValue<string>("LastRoundRadius") ?? "5";
                                if (double.TryParse(savedRadius, out double parsedRadius))
                                    {
                                        RoundRadius = parsedRadius;
                                    }
                                break;
                        }
                    }
                    else
                    {
                        // 兼容旧的中文名称格式
                        switch (lastShape)
                        {
                            case "直角":
                                SelectedShape = ShapeType.RightAngle;
                                RoundRadius = 0;
                                _isShapeExplicitlySelected = true; // ✅ 修复：恢复上次选择的直角时也应该保持明确选择状态
                                break;
                            case "圆形":
                                SelectedShape = ShapeType.Circle;
                                RoundRadius = 0;
                                _isShapeExplicitlySelected = true; // 从设置恢复的形状也是明确选择
                                break;
                            case "异形":
                                SelectedShape = ShapeType.Special;
                                RoundRadius = 0;
                                _isShapeExplicitlySelected = true; // 从设置恢复的形状也是明确选择
                                break;
                            case "圆角矩形":
                                SelectedShape = ShapeType.RoundRect;
                                _isShapeExplicitlySelected = true; // 从设置恢复的形状也是明确选择
                                showRadiusInput = true;
                                // 恢复圆角值
                                string savedRadius = AppSettings.GetValue<string>("LastRoundRadius") ?? "5";
                                if (double.TryParse(savedRadius, out double parsedRadius))
                                    {
                                        RoundRadius = parsedRadius;
                                    }
                                break;
                            default:
                                // 未知的形状，使用默认值
                                SelectedShape = ShapeType.RightAngle;
                                RoundRadius = 0;
                                LogHelper.Debug($"未知的形状类型: {lastShape}，使用默认值");
                                break;
                        }
                    }
                }
                else
                {
                    // 没有上次选择或用户取消了选择，保持默认状态
                    SelectedShape = ShapeType.RightAngle;
                    RoundRadius = 0;
                    _isShapeExplicitlySelected = false; // 重要：标记为用户明确取消选择
                    LogHelper.Debug("没有上次形状选择或用户取消了选择，使用默认值");
                }

                // 显示/隐藏圆角输入框
                if (radiusTextBox != null)
                {
                    radiusTextBox.Visible = showRadiusInput;
                    if (showRadiusInput)
                    {
                        // 确保radiusTextBox显示正确的值
                        radiusTextBox.Text = RoundRadius.ToString();
                        LogHelper.Debug($"初始化圆角输入框值: {RoundRadius}");
                    }
                    else
                    {
                        // 确保非圆角矩形时清空输入框
                        radiusTextBox.Text = "";
                        LogHelper.Debug("清空圆角输入框（非圆角矩形）");
                    }
                }

                // 更新按钮状态 - 只有用户明确选择时才选中按钮
                if (_isShapeExplicitlySelected)
                {
                    UpdateShapeButtonStates((int)SelectedShape);
                }
                else
                {
                    // 用户没有明确选择任何形状，不选中任何按钮
                    UpdateShapeButtonStates(-1);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"初始化形状控件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 形状分段控件选择变化事件处理
        /// </summary>
        // 添加缺失的变量声明
        private string _lastSelectedFilmType = "";
        private int _lastSelectedShapeIndex = -1;

        

        /// <summary>
        /// 圆角输入框文本变化事件处理
        /// </summary>
        private void RadiusTextBox_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (radiusTextBox != null)
                {
                    // 处理空值情况
                    if (string.IsNullOrWhiteSpace(radiusTextBox.Text))
                    {
                        // 空值情况，保持当前RoundRadius不变，但保存空值状态
                        AppSettings.Set("LastRoundRadius", "");
                        AppSettings.Save();
                        LogHelper.Debug("圆角输入框为空，保存空值状态");
                        return;
                    }

                    // 尝试解析为数值，更新RoundRadius
                    if (double.TryParse(radiusTextBox.Text, out double radius) && radius >= 0)
                    {
                        // 只有当值真正改变时才保存
                        if (Math.Abs(RoundRadius - radius) > 0.001) // 避免浮点数精度问题
                        {
                            RoundRadius = radius;

                            // 实时保存圆角值
                            AppSettings.Set("LastCornerRadius", GetCompatibleCornerRadius());
                            AppSettings.Set("LastRoundRadius", RoundRadius.ToString());
                            AppSettings.Save();

                            LogHelper.Debug($"圆角值已更新并保存: {RoundRadius}");

                            // 更新尺寸显示
                            UpdateDimensionsWithBleed();
                        }
                        else
                        {
                            LogHelper.Debug($"圆角值未变化: {RoundRadius}，跳过保存");
                        }
                    }
                    else
                    {
                        // 无效输入，恢复上一个有效值
                        radiusTextBox.Text = RoundRadius.ToString();
                        LogHelper.Debug($"无效圆角值，恢复为: {RoundRadius}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"圆角输入框文本变化事件处理失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 圆角输入框失去焦点事件处理
        /// </summary>
        private void RadiusTextBox_LostFocus(object sender, EventArgs e)
        {
            try
            {
                if (radiusTextBox != null)
                {
                    // 验证圆角值格式
                    string radiusText = radiusTextBox.Text;
                    if (!string.IsNullOrEmpty(radiusText))
                    {
                        // 验证是否为有效的数字
                        if (!double.TryParse(radiusText, out double radius) || radius < 0)
                        {
                            MessageBox.Show("圆角值必须为非负数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            radiusTextBox.Focus();
                            radiusTextBox.SelectAll();
                            return;
                        }
                    }
  
                    // 保存圆角值
                    if (double.TryParse(radiusText, out double parsedRadius) && parsedRadius >= 0)
                    {
                        RoundRadius = parsedRadius;
                    }
                    AppSettings.Set("LastCornerRadius", GetCompatibleCornerRadius());
                    AppSettings.Set("LastRoundRadius", RoundRadius.ToString());
                    AppSettings.Save();
  
                    LogHelper.Debug($"保存圆角值: {GetCompatibleCornerRadius()} (RoundRadius: {RoundRadius})");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"圆角输入框失去焦点事件处理失败: {ex.Message}", ex);
            }
        }



        #endregion

        #region 阶段3：增强导出路径管理

        /// <summary>
        /// 保存导出路径选择到设置
        /// </summary>
        private void SaveSelectedExportPath()
        {
            try
            {
                if (selectedTreeNode != null && selectedTreeNode.Tag != null)
                {
                    string selectedPath = selectedTreeNode.Tag.ToString();
                    AppSettings.Set("LastSelectedExportPath", selectedPath);
                    AppSettings.Save();
                    LogHelper.Debug($"保存导出路径: {selectedPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存导出路径失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 应用导出路径绑定的预设参数
        /// </summary>
        private void ApplyPathBoundPreset()
        {
            try
            {
                if (selectedTreeNode?.Tag == null) return;

                string selectedPath = selectedTreeNode.Tag.ToString();
                var folders = GetConfiguredExportFolders();

                // 优先精确匹配当前路径的预设配置
                var folderConfig = folders.FirstOrDefault(f => f.Path == selectedPath);

                // 如果没有精确匹配，尝试查找父路径的预设配置（用于子路径继承父路径预设）
                if (folderConfig == null || !HasPresetConfiguration(folderConfig))
                {
                    // 查找父路径
                    var parentPath = FindParentConfiguredPath(selectedPath, folders);
                    if (parentPath != null)
                    {
                        folderConfig = parentPath;
                        LogHelper.Debug($"[路径预设] 子路径 {selectedPath} 继承父路径预设: {folderConfig.Name}");
                    }
                }

                if (folderConfig == null || !HasPresetConfiguration(folderConfig)) return;

                // 检查是否有绑定预设参数
                bool hasPreset = HasPresetConfiguration(folderConfig);

                if (!hasPreset) return;

                LogHelper.Debug($"[路径预设] 应用绑定预设: {folderConfig.Name}");

                // 应用材料
                if (!string.IsNullOrEmpty(folderConfig.PresetMaterial))
                {
                    // 尝试在材料按钮中找到匹配的材料并点击
                    var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                    if (tabControl != null)
                    {
                        foreach (var tabPage in tabControl.Pages)
                        {
                            foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                            {
                                if (btn.Text == folderConfig.PresetMaterial)
                                {
                                    btn.PerformClick();
                                    break;
                                }
                            }
                        }
                    }
                    // 如果按钮中没有找到，直接设置属性
                    if (SelectedMaterial != folderConfig.PresetMaterial)
                    {
                        SelectedMaterial = folderConfig.PresetMaterial;
                    }
                }

                // 应用出血值
                if (!string.IsNullOrEmpty(folderConfig.PresetBleed) && bleedDropdown != null)
                {
                    for (int i = 0; i < bleedDropdown.Items.Count; i++)
                    {
                        if (bleedDropdown.Items[i].ToString() == folderConfig.PresetBleed)
                        {
                            bleedDropdown.SelectedIndex = i;
                            SelectedTetBleed = double.TryParse(folderConfig.PresetBleed, out double bleed) ? bleed : 0;
                            break;
                        }
                    }
                }

                // 应用颜色模式
                if (!string.IsNullOrEmpty(folderConfig.PresetColorMode))
                {
                    ColorMode = folderConfig.PresetColorMode;
                    SetColorModeButtonWithIcon(ColorMode);
                    UpdateFixedField();
                }

                // 应用膜类型
                if (!string.IsNullOrEmpty(folderConfig.PresetFilmType))
                {
                    int buttonIndex = -1;
                    if (folderConfig.PresetFilmType == "光膜") buttonIndex = 0;
                    else if (folderConfig.PresetFilmType == "哑膜") buttonIndex = 1;
                    else if (folderConfig.PresetFilmType == "不过膜") buttonIndex = 2;
                    else if (folderConfig.PresetFilmType == "红膜") buttonIndex = 3;

                    if (buttonIndex >= 0)
                    {
                        SelectFilmType(folderConfig.PresetFilmType, buttonIndex);
                    }
                }

                // 应用标识页
                if (folderConfig.PresetAddIdentifierPage != AddIdentifierPage)
                {
                    AddIdentifierPage = folderConfig.PresetAddIdentifierPage;
                    if (chkIdentifierPage != null)
                    {
                        chkIdentifierPage.Checked = AddIdentifierPage;
                    }
                }

                // 应用形状
                if (!string.IsNullOrEmpty(folderConfig.PresetShape))
                {
                    if (Enum.TryParse<ShapeType>(folderConfig.PresetShape, out var shapeType))
                    {
                        SelectShape(shapeType);
                    }
                }

                // 应用一式两联
                if (folderConfig.PresetDualCopy != _isDuplicateLayoutEnabled)
                {
                    _isDuplicateLayoutEnabled = folderConfig.PresetDualCopy;
                    if (duplicateLayoutCheckbox != null)
                    {
                        duplicateLayoutCheckbox.Checked = _isDuplicateLayoutEnabled;
                    }
                }

                LogHelper.Info($"[路径预设] 已应用预设: {folderConfig.Name}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[路径预设] 应用预设失败: {ex.Message}", ex);
            }
        }

        #region 预设功能

        /// <summary>
        /// 检查是否有绑定预设参数
        /// </summary>
        private bool HasPresetConfiguration(ExportFolderConfig config)
        {
            if (config == null) return false;

            return !string.IsNullOrEmpty(config.PresetMaterial) ||
                   !string.IsNullOrEmpty(config.PresetBleed) ||
                   !string.IsNullOrEmpty(config.PresetColorMode) ||
                   !string.IsNullOrEmpty(config.PresetFilmType) ||
                   config.PresetAddIdentifierPage ||
                   !string.IsNullOrEmpty(config.PresetShape) ||
                   config.PresetDualCopy;
        }

        /// <summary>
        /// 查找父路径的预设配置（用于子路径继承）
        /// </summary>
        private ExportFolderConfig FindParentConfiguredPath(string childPath, List<ExportFolderConfig> folders)
        {
            if (string.IsNullOrEmpty(childPath) || folders == null) return null;

            // 遍历所有配置的根路径，检查 childPath 是否是其子路径
            foreach (var folder in folders)
            {
                if (string.IsNullOrEmpty(folder.Path)) continue;

                // 检查 childPath 是否以 folder.Path 开头
                if (childPath.StartsWith(folder.Path + Path.DirectorySeparatorChar) ||
                    childPath.StartsWith(folder.Path + "\\"))
                {
                    // 找到父路径，检查父路径是否有预设配置
                    if (HasPresetConfiguration(folder))
                    {
                        return folder;
                    }
                }
            }

            return null;
        }

        // 右键菜单消息常量
        private const int WM_CONTEXTMENU = 0x007B;

        /// <summary>
        /// 重写窗口消息处理，捕获右键菜单
        /// </summary>
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == WM_CONTEXTMENU && _presetContextMenu != null)
            {
                // 获取鼠标位置
                int x = m.LParam.ToInt32() & 0xFFFF;
                int y = m.LParam.ToInt32() >> 16;

                // 如果是 -1,-1（由键盘触发），则显示在窗体中心
                if (x == 0xFFFF && y == 0xFFFF)
                {
                    x = this.Width / 2;
                    y = this.Height / 2;
                }

                // 转换屏幕坐标到客户端坐标
                Point clientPoint = this.PointToClient(new Point(x, y));

                // 显示右键菜单
                UpdatePresetMenu();
                _presetContextMenu.Show(this, clientPoint);
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// 初始化预设右键菜单
        /// </summary>
        private void InitializePresetContextMenu()
        {
            _presetContextMenu = new System.Windows.Forms.ContextMenuStrip();
            _presetContextMenu.Items.Add("保存为预设...", null, (s, e) => SaveCurrentAsPreset());

            // 添加分隔线
            _presetContextMenu.Items.Add(new ToolStripSeparator());

            // 添加分隔线（为预设列表预留位置）
            _presetContextMenu.Items.Add(new ToolStripSeparator());

            // 预留位置给预设列表（动态插入）
            _presetContextMenu.Items.Add(new ToolStripMenuItem(""));

            // 添加分隔线
            _presetContextMenu.Items.Add(new ToolStripSeparator());

            // 删除预设菜单项
            var deleteItem = new ToolStripMenuItem("删除当前预设", null, (s, e) => DeleteCurrentPreset());
            deleteItem.Enabled = !string.IsNullOrEmpty(_currentPresetName);
            _presetContextMenu.Items.Add(deleteItem);

            // 加载预设列表
            UpdatePresetMenu();
        }

        /// <summary>
        /// 更新预设菜单项
        /// </summary>
        private void UpdatePresetMenu()
        {
            if (_presetContextMenu == null) return;

            // 索引2处是预设列表分隔线，索引3处是预设列表内容
            // 获取预设列表
            var presets = AppSettings.MaterialPresets;

            // 移除旧的预设列表项（从索引3开始移除，直到遇到下一个分隔线）
            while (_presetContextMenu.Items.Count > 4)
            {
                _presetContextMenu.Items.RemoveAt(3);
            }

            if (presets != null && presets.Count > 0)
            {
                foreach (var preset in presets)
                {
                    var menuItem = new ToolStripMenuItem(preset.Name, null, (s, e) => LoadPreset(preset.Name));
                    if (preset.Name == _currentPresetName)
                    {
                        menuItem.Checked = true;
                    }
                    _presetContextMenu.Items.Insert(3, menuItem);
                }
            }
            else
            {
                var noPresetItem = new ToolStripMenuItem("(无预设)");
                noPresetItem.Enabled = false;
                _presetContextMenu.Items.Insert(3, noPresetItem);
            }
        }

        /// <summary>
        /// 保存当前设置为预设
        /// </summary>
        private void SaveCurrentAsPreset()
        {
            // 弹出输入对话框让用户输入预设名称
            using (var inputDialog = new Form())
            {
                inputDialog.Width = 300;
                inputDialog.Height = 130;
                inputDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputDialog.Text = "保存预设";
                inputDialog.StartPosition = FormStartPosition.CenterParent;
                inputDialog.MaximizeBox = false;
                inputDialog.MinimizeBox = false;

                var label = new System.Windows.Forms.Label() { Left = 20, Top = 20, Text = "请输入预设名称:" };
                var textBox = new System.Windows.Forms.TextBox() { Left = 20, Top = 45, Width = 250 };
                var okButton = new System.Windows.Forms.Button() { Text = "确定", Left = 110, Width = 80, Top = 70, DialogResult = DialogResult.OK };
                var cancelButton = new System.Windows.Forms.Button() { Text = "取消", Left = 195, Width = 80, Top = 70, DialogResult = DialogResult.Cancel };

                inputDialog.Controls.Add(label);
                inputDialog.Controls.Add(textBox);
                inputDialog.Controls.Add(okButton);
                inputDialog.Controls.Add(cancelButton);
                inputDialog.AcceptButton = okButton;
                inputDialog.CancelButton = cancelButton;

                if (inputDialog.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    string presetName = textBox.Text.Trim();

                    // 创建预设
                    var preset = new MaterialSelectionPreset
                    {
                        Name = presetName,
                        SelectedMaterial = SelectedMaterial,
                        TetBleed = SelectedTetBleed,
                        ColorMode = ColorMode,
                        FilmType = FilmType,
                        AddIdentifierPage = AddIdentifierPage,
                        ShapeState = SelectedShape.ToString(),
                        IsDualCopy = _isDuplicateLayoutEnabled,
                        ExportPath = SelectedExportPath ?? "",
                        RoundRadius = RoundRadius
                    };

                    // 获取现有预设列表
                    var presets = AppSettings.MaterialPresets ?? new List<MaterialSelectionPreset>();

                    // 检查是否已存在同名预设
                    var existingIndex = presets.FindIndex(p => p.Name == presetName);
                    if (existingIndex >= 0)
                    {
                        // 询问是否覆盖
                        var result = MessageBox.Show($"预设\"{presetName}\"已存在，是否覆盖？", "确认覆盖",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result != DialogResult.Yes)
                            return;
                        presets[existingIndex] = preset;
                    }
                    else
                    {
                        presets.Add(preset);
                    }

                    // 保存到设置
                    AppSettings.MaterialPresets = presets;
                    AppSettings.LastUsedMaterialPreset = presetName;
                    AppSettings.Save();

                    // 更新当前预设名称
                    _currentPresetName = presetName;

                    // 更新菜单
                    UpdatePresetMenu();

                    LogHelper.Info($"[预设] 已保存预设: {presetName}");
                    MessageBox.Show($"预设\"{presetName}\"保存成功！", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        /// <summary>
        /// 加载预设
        /// </summary>
        private void LoadPreset(string presetName)
        {
            var presets = AppSettings.MaterialPresets;
            if (presets == null) return;

            var preset = presets.FirstOrDefault(p => p.Name == presetName);
            if (preset == null) return;

            try
            {
                // 加载材料
                if (!string.IsNullOrEmpty(preset.SelectedMaterial))
                {
                    // 尝试在材料按钮中找到匹配的材料
                    var tabControl = Controls.OfType<AntdUI.Tabs>().FirstOrDefault();
                    if (tabControl != null)
                    {
                        foreach (var tabPage in tabControl.Pages)
                        {
                            foreach (var btn in tabPage.Controls.OfType<AntdUI.Button>())
                            {
                                if (btn.Text == preset.SelectedMaterial)
                                {
                                    // 模拟点击按钮来选中材料
                                    btn.PerformClick();
                                    break;
                                }
                            }
                        }
                    }

                    // 如果按钮中没有找到，直接设置属性
                    if (SelectedMaterial != preset.SelectedMaterial)
                    {
                        SelectedMaterial = preset.SelectedMaterial;
                    }
                }

                // 加载出血值
                if (preset.TetBleed >= 0 && bleedDropdown != null)
                {
                    // 尝试在出血值列表中找到匹配的值
                    string bleedText = preset.TetBleed.ToString();
                    bool found = false;
                    for (int i = 0; i < bleedDropdown.Items.Count; i++)
                    {
                        if (bleedDropdown.Items[i].ToString() == bleedText)
                        {
                            bleedDropdown.SelectedIndex = i;
                            SelectedTetBleed = preset.TetBleed;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        // 如果没找到匹配的，直接设置文本
                        bleedDropdown.Text = bleedText;
                        SelectedTetBleed = preset.TetBleed;
                    }
                }

                // 加载颜色模式
                if (!string.IsNullOrEmpty(preset.ColorMode))
                {
                    ColorMode = preset.ColorMode;
                    SetColorModeButtonWithIcon(ColorMode);
                    UpdateFixedField();
                }

                // 加载膜类型
                if (!string.IsNullOrEmpty(preset.FilmType))
                {
                    // 根据膜类型选择对应的按钮索引
                    int buttonIndex = -1;
                    if (preset.FilmType == "光膜") buttonIndex = 0;
                    else if (preset.FilmType == "哑膜") buttonIndex = 1;
                    else if (preset.FilmType == "不过膜") buttonIndex = 2;
                    else if (preset.FilmType == "红膜") buttonIndex = 3;

                    if (buttonIndex >= 0)
                    {
                        SelectFilmType(preset.FilmType, buttonIndex);
                    }
                }

                // 加载标识页
                if (AddIdentifierPage != preset.AddIdentifierPage)
                {
                    AddIdentifierPage = preset.AddIdentifierPage;
                    if (chkIdentifierPage != null)
                    {
                        chkIdentifierPage.Checked = AddIdentifierPage;
                    }
                }

                // 加载形状
                if (Enum.TryParse<ShapeType>(preset.ShapeState, out var shapeType))
                {
                    SelectShape(shapeType);
                }

                // 加载一式两联
                if (preset.IsDualCopy != _isDuplicateLayoutEnabled)
                {
                    _isDuplicateLayoutEnabled = preset.IsDualCopy;
                    if (duplicateLayoutCheckbox != null)
                    {
                        duplicateLayoutCheckbox.Checked = _isDuplicateLayoutEnabled;
                    }
                }

                // 加载导出路径
                if (!string.IsNullOrEmpty(preset.ExportPath) && Directory.Exists(preset.ExportPath))
                {
                    SelectedExportPath = preset.ExportPath;
                }

                // 加载圆角半径
                RoundRadius = preset.RoundRadius;
                if (radiusTextBox != null)
                {
                    radiusTextBox.Text = preset.RoundRadius > 0 ? preset.RoundRadius.ToString() : "";
                }

                // 保存当前预设名称
                _currentPresetName = presetName;
                AppSettings.LastUsedMaterialPreset = presetName;

                // 更新菜单
                UpdatePresetMenu();

                LogHelper.Info($"[预设] 已加载预设: {presetName}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[预设] 加载预设失败: {ex.Message}", ex);
                MessageBox.Show($"加载预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 删除当前预设
        /// </summary>
        private void DeleteCurrentPreset()
        {
            if (string.IsNullOrEmpty(_currentPresetName))
            {
                MessageBox.Show("请先选择一个预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show($"确定要删除预设\"{_currentPresetName}\"吗？", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                var presets = AppSettings.MaterialPresets;
                if (presets != null)
                {
                    presets.RemoveAll(p => p.Name == _currentPresetName);
                    AppSettings.MaterialPresets = presets;
                    AppSettings.Save();

                    _currentPresetName = "";
                    UpdatePresetMenu();

                    LogHelper.Info($"[预设] 已删除预设");
                }
            }
        }

        #endregion

        /// <summary>
        /// 恢复上次选择的导出路径
        /// </summary>
        private void RestoreLastSelectedExportPath()
        {
            try
            {
                string lastPath = AppSettings.GetValue<string>("LastSelectedExportPath");
                if (!string.IsNullOrEmpty(lastPath))
                {
                    // 在TreeView中查找并选中上次选择的路径
                    SelectNodeByPath(folderTreeView, lastPath);
                    SelectedExportPath = lastPath;
                    LogHelper.Debug($"恢复导出路径: {lastPath}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"恢复导出路径失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据路径选择TreeNode节点
        /// </summary>
        private void SelectNodeByPath(TreeView treeView, string targetPath)
        {
            try
            {
                if (treeView == null || string.IsNullOrEmpty(targetPath)) return;

                foreach (TreeNode node in treeView.Nodes)
                {
                    if (node.Tag?.ToString() == targetPath)
                    {
                        treeView.SelectedNode = node;
                        selectedTreeNode = node;
                        return;
                    }

                    // 递归查找子节点
                    TreeNode foundNode = FindNodeByPath(node, targetPath);
                    if (foundNode != null)
                    {
                        treeView.SelectedNode = foundNode;
                        selectedTreeNode = foundNode;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"根据路径选择节点失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 递归查找指定路径的TreeNode
        /// </summary>
        private TreeNode FindNodeByPath(TreeNode parentNode, string targetPath)
        {
            try
            {
                foreach (TreeNode node in parentNode.Nodes)
                {
                    if (node.Tag?.ToString() == targetPath)
                    {
                        return node;
                    }

                    TreeNode foundNode = FindNodeByPath(node, targetPath);
                    if (foundNode != null)
                    {
                        return foundNode;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"递归查找节点失败: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 获取导出路径列表（兼容原版PathItem格式）
        /// </summary>
        public List<PathItem> GetExportPathItems()
        {
            var pathItems = new List<PathItem>();
            try
            {
                var folders = GetConfiguredExportFolders();
                foreach (var folder in folders)
                {
                    if (Directory.Exists(folder.Path))
                    {
                        pathItems.Add(new PathItem
                        {
                            FullPath = folder.Path,
                            FolderName = folder.Name
                        });
                        
                        // 添加子文件夹
                        AddSubFoldersToPathItems(folder.Path, pathItems, folder.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取导出路径列表失败: {ex.Message}", ex);
            }
            return pathItems;
        }

        /// <summary>
        /// 递归添加子文件夹到PathItem列表
        /// </summary>
        private void AddSubFoldersToPathItems(string parentPath, List<PathItem> pathItems, string parentName)
        {
            try
            {
                var subDirectories = Directory.GetDirectories(parentPath);
                foreach (var subDir in subDirectories)
                {
                    var dirInfo = new DirectoryInfo(subDir);
                    pathItems.Add(new PathItem
                    {
                        FullPath = dirInfo.FullName,
                        FolderName = $"{parentName}/{dirInfo.Name}"
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"添加子文件夹到PathItem列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 刷新导出路径树
        /// </summary>
        public void RefreshExportPaths()
        {
            try
            {
                InitializeFolderTree();
                RestoreLastSelectedExportPath();
                LogHelper.Debug("刷新导出路径树完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"刷新导出路径失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取当前选中的导出路径
        /// </summary>
        public string GetCurrentExportPath()
        {
            return SelectedExportPath ?? "";
        }

        /// <summary>
        /// TreeView鼠标按下事件，确保点击事件正确传递
        /// </summary>
        private void FolderTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                // 获取点击位置的节点
                TreeNode clickedNode = folderTreeView.GetNodeAt(e.X, e.Y);
                if (clickedNode != null)
                {
                    // 确保节点被选中
                    folderTreeView.SelectedNode = clickedNode;
                    LogHelper.Debug($"鼠标点击节点: {GetFullFolderPath(clickedNode)}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"TreeView鼠标按下事件处理失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// TreeView双击事件处理 - 展开二级目录的所有子目录
        /// </summary>
        private void FolderTreeView_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                // 获取当前选中的节点
                TreeNode selectedNode = folderTreeView.SelectedNode;
                if (selectedNode != null && selectedNode.Level == 1)
                {
                    // 双击二级目录时，展开所有剩余级别的子目录
                    ExpandAllChildNodes(selectedNode);
                    LogHelper.Debug($"双击展开二级目录的所有子目录: {GetFullFolderPath(selectedNode)}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"TreeView双击事件处理失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 递归展开指定节点下的所有子节点
        /// </summary>
        /// <param name="node">要展开的节点</param>
        private void ExpandAllChildNodes(TreeNode node)
        {
            try
            {
                if (node == null) return;

                // 展开当前节点
                node.Expand();

                // 递归展开所有子节点
                foreach (TreeNode childNode in node.Nodes)
                {
                    ExpandAllChildNodes(childNode);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"展开子节点失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 安全的递归展开指定节点下的所有子节点（不会触发BeforeExpand事件）
        /// </summary>
        /// <param name="node">要展开的节点</param>
        private void ExpandAllChildNodesSafe(TreeNode node)
        {
            try
            {
                if (node == null) return;

                // 直接展开所有子节点，不调用Expand()方法避免触发事件
                foreach (TreeNode childNode in node.Nodes)
                {
                    // 确保子节点已加载（如果需要的话）
                    if (childNode.Nodes.Count == 0 && !childNode.IsExpanded)
                    {
                        childNode.Expand(); // 这里可能会触发BeforeExpand，但因为我们已经设置了_isExpanding标志，所以是安全的
                    }

                    // 递归展开所有子节点
                    ExpandAllChildNodesSafe(childNode);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"安全展开子节点失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// TreeView鼠标移动事件（已禁用提示标签功能）
        /// </summary>
        private void FolderTreeView_MouseMove(object sender, MouseEventArgs e)
        {
            // 注释掉提示标签功能，不再显示鼠标悬停提示
            // 如果需要重新启用，可以取消下面的注释

            /*
            try
            {
                TreeNode node = folderTreeView.GetNodeAt(e.X, e.Y);
                if (node != null)
                {
                    string folderPath = GetFullFolderPath(node);
                    if (!string.IsNullOrEmpty(folderPath))
                    {
                        folderTreeViewToolTip.SetToolTip(folderTreeView, folderPath);
                    }
                }
                else
                {
                    folderTreeView.ToolTip.SetToolTip(folderTreeView, "");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"TreeView鼠标移动事件处理失败: {ex.Message}", ex);
            }
            */
        }

        /// <summary>
        /// 设置颜色模式按钮的图标和文本
        /// </summary>
        private void SetColorModeButtonWithIcon(string colorMode)
        {
            try
            {
                if (colorModeButton == null) return;

                // 直接设置文字和颜色，不使用SVG图标
                SetColorModeButtonWithEmoji(colorMode);

                LogHelper.Debug($"已设置颜色模式按钮，颜色模式: {colorMode}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"设置颜色模式按钮失败: {ex.Message}", ex);
                // 出错时只显示纯文本
                if (colorModeButton != null)
                {
                    colorModeButton.Text = colorMode;
                    colorModeButton.ForeColor = Color.Black;
                }
            }
        }

        /// <summary>
        /// 设置颜色模式按钮的文本和样式
        /// </summary>
        private void SetColorModeButtonWithEmoji(string colorMode)
        {
            try
            {
                if (colorModeButton == null) return;

                // 获取当前主题的文字颜色
                var themeManager = ServiceLocator.Instance.GetThemeManager();
                var theme = themeManager?.GetCurrentTheme();
                Color textColor = theme?.TextPrimary ?? Color.Black;

                // 设置按钮类型为默认，避免默认的主题颜色干扰
                colorModeButton.Type = AntdUI.TTypeMini.Default;
                colorModeButton.Ghost = true; // 启用ghost模式，透明背景
                
                if (colorMode == "彩色")
                {
                    colorModeButton.Text = "彩色"; // 只显示中文文字
                    colorModeButton.ForeColor = textColor; // 使用主题文字颜色
                    colorModeButton.DefaultBorderColor = Color.Red; // 彩色模式显示红色边框
                }
                else
                {
                    colorModeButton.Text = "黑白"; // 只显示中文文字
                    colorModeButton.ForeColor = textColor; // 使用主题文字颜色
                    colorModeButton.DefaultBorderColor = Color.Green; // 黑白模式显示绿色边框
                }

                LogHelper.Debug($"已设置颜色模式按钮: {colorModeButton.Text}, 颜色模式: {colorMode}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"设置颜色模式按钮失败: {ex.Message}", ex);
                if (colorModeButton != null)
                {
                    colorModeButton.Text = colorMode;
                    colorModeButton.ForeColor = Color.Black; // 出错时使用默认黑色
                    colorModeButton.DefaultBorderColor = Color.Black; // 默认黑色边框
                }
            }
        }

        /// <summary>
        /// 获取节点的完整文件夹路径
        /// </summary>
        private string GetFullFolderPath(TreeNode node)
        {
            try
            {
                if (node == null) return "";

                // 从节点文本中提取文件夹名称（去掉图标部分）
                string folderName = node.Text;
                if (folderName.Contains(" "))
                {
                    int spaceIndex = folderName.IndexOf(' ');
                    folderName = folderName.Substring(spaceIndex + 1).Trim();
                }

                // 递归构建完整路径
                if (node.Parent == null)
                {
                    return folderName;
                }
                else
                {
                    string parentPath = GetFullFolderPath(node.Parent);
                    return string.IsNullOrEmpty(parentPath) ? folderName : Path.Combine(parentPath, folderName);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取文件夹路径失败: {ex.Message}", ex);
                return node.Text;
            }
        }

        /// <summary>
        /// TreeView节点自定义绘制事件，为不同层级显示不同颜色
        /// </summary>
        private void FolderTreeView_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            try
            {
                // 获取节点层级
                int level = GetNodeLevel(e.Node);

                // 根据层级设置颜色
                Color textColor = GetLevelColor(level);
                Color backgroundColor = Color.Transparent;

                // 如果是选中状态，使用高亮背景色
                if ((e.State & TreeNodeStates.Selected) != 0)
                {
                    backgroundColor = Color.FromArgb(24, 144, 255); // 主色调背景
                    textColor = Color.White;
                }
                else if ((e.State & TreeNodeStates.Hot) != 0)
                {
                    backgroundColor = Color.FromArgb(240, 240, 240); // 鼠标悬停背景色
                }

                // 绘制完整背景 - 覆盖整个节点区域，使用平滑的绘制方式
                if (backgroundColor != Color.Transparent)
                {
                    // 扩展背景区域到TreeView的完整宽度，包括图标区域
                    // 确保从TreeView的最左边开始到最右边结束
                    Rectangle fullBackgroundRect = new Rectangle(
                        0, // 从TreeView的最左边开始
                        e.Bounds.Y,
                        folderTreeView.Width, // 扩展到TreeView的最右边
                        e.Bounds.Height);

                    // 使用抗锯齿的平滑绘制
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    e.Graphics.FillRectangle(new SolidBrush(backgroundColor), fullBackgroundRect);
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

                    // 🔧 修复：当绘制背景时，图标会被覆盖，因此需要手动重绘图标
                    // 图标位置计算：文本区域左侧 - 图标宽度 - 间距
                    if (folderTreeView.ImageList != null && folderTreeView.ImageList.Images.ContainsKey("folder"))
                    {
                        var image = folderTreeView.ImageList.Images["folder"];
                        // 通常 TreeView 图标在文本左侧，ImageSize=14
                        // 偏移量估算：e.Bounds.X 是文本起始X，System通常留出3-4px间距
                        int imageX = e.Bounds.X - 14 - 3;
                        int imageY = e.Bounds.Y + (e.Bounds.Height - image.Height) / 2;
                        
                        // 确保绘制位置有效
                        if (imageX >= 0)
                        {
                            e.Graphics.DrawImage(image, imageX, imageY, image.Width, image.Height);
                        }
                    }

                    // 3. 绘制展开/折叠图标 (如果有子节点) - 修复高亮时加减号不可见问题
                    if (e.Node.Nodes.Count > 0)
                    {
                        // 估算位置：在文件夹图标更左侧
                        // 文件夹图标 approx at: e.Bounds.X - 17
                        // 加减号 size 9x9, gap approx 12-15
                        int glyphSize = 9;
                        int glyphX = e.Bounds.X - 14 - 3 - 14; 
                        int glyphY = e.Bounds.Y + (e.Bounds.Height - glyphSize) / 2;

                        // 仅当位置在可视范围内时绘制
                        if (glyphX >= 0)
                        {
                            using (var pen = new Pen(textColor)) // 使用当前文本色（选中时为白色）
                            {
                                // 绘制边框
                                e.Graphics.DrawRectangle(pen, glyphX, glyphY, glyphSize, glyphSize);
                                
                                // 绘制横线 (-)
                                int midY = glyphY + glyphSize / 2;
                                e.Graphics.DrawLine(pen, glyphX + 2, midY, glyphX + glyphSize - 2, midY);
                                
                                // 绘制竖线 (|) - 仅在折叠时
                                if (!e.Node.IsExpanded)
                                {
                                    int midX = glyphX + glyphSize / 2;
                                    e.Graphics.DrawLine(pen, midX, glyphY + 2, midX, glyphY + glyphSize - 2);
                                }
                            }
                        }
                    }
                }

                // 设置文本格式 - 使用更好的文本格式选项避免截断
                TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                       TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix;

                // 启用文本渲染的抗锯齿效果，使文字更加平滑
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // 2. 绘制文本
                // 修复：移除额外的 textOffset，e.Bounds.X 已经是正确的文本起始位置
                // 添加微小偏移量微调
                int textOffset = 2; 
                Rectangle textRect = new Rectangle(
                    e.Bounds.X + textOffset, 
                    e.Bounds.Y, 
                    Math.Max(e.Bounds.Width - textOffset, 100), 
                    e.Bounds.Height);

                // 如果文本太长，自动扩展TreeView宽度
                Size textSize = TextRenderer.MeasureText(e.Graphics, e.Node.Text, e.Node.NodeFont, textRect.Size, flags);

                if (textSize.Width > textRect.Width && folderTreeView.Width < textSize.Width + e.Bounds.X + textOffset + 20)
                {
                    int newWidth = Math.Min(textSize.Width + e.Bounds.X + textOffset + 20, 800);
                    if (newWidth > folderTreeView.Width)
                    {
                        folderTreeView.Width = newWidth;
                    }
                }

                TextRenderer.DrawText(e.Graphics, e.Node.Text, e.Node.NodeFont,
                    textRect, textColor, backgroundColor, flags);

                // 移除焦点矩形绘制，避免切换时的闪烁效果
                // 只在需要时进行自定义绘制，保持原生点击行为
                e.DrawDefault = false;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"TreeView节点绘制失败: {ex.Message}", ex);
                e.DrawDefault = true; // 出错时使用默认绘制
            }
        }

        /// <summary>
        /// 获取节点层级深度
        /// </summary>
        private int GetNodeLevel(TreeNode node)
        {
            int level = 0;
            TreeNode current = node;
            while (current.Parent != null)
            {
                level++;
                current = current.Parent;
            }
            return level;
        }

        /// <summary>
        /// 根据层级获取对应的颜色
        /// </summary>
        private Color GetLevelColor(int level)
        {
            // 定义层级颜色方案（从深到浅的渐变色）
            switch (level)
            {
                case 0: // 根节点 - 深蓝色
                    return Color.FromArgb(0, 120, 215);
                case 1: // 一级子节点 - 蓝色
                    return Color.FromArgb(0, 150, 136);
                case 2: // 二级子节点 - 青色
                    return Color.FromArgb(0, 180, 216);
                case 3: // 三级子节点 - 浅青色
                    return Color.FromArgb(102, 187, 106);
                case 4: // 四级子节点 - 绿色
                    return Color.FromArgb(139, 195, 74);
                case 5: // 五级子节点 - 黄绿色
                    return Color.FromArgb(175, 180, 43);
                default: // 更深层级 - 灰色
                    return Color.FromArgb(150, 150, 150);
            }
        }

        /// <summary>
        /// 根据层级获取对应的图标
        /// </summary>
        private string GetLevelIcon(int level)
        {
            // 定义层级图标方案，使用不同的emoji图标来区分层级
            switch (level)
            {
                case 0: // 根节点
                    return "🏠";
                case 1: // 一级子节点
                    return "📁";
                case 2: // 二级子节点
                    return "📂";
                case 3: // 三级子节点
                    return "🗂️";
                case 4: // 四级子节点
                    return "🗄️";
                case 5: // 五级子节点
                    return "📋";
                default: // 更深层级
                    return "📄";
            }
        }

        /// <summary>
        /// 设置导出路径
        /// </summary>
        public bool SetExportPath(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    SelectNodeByPath(folderTreeView, path);
                    SelectedExportPath = path;
                    SaveSelectedExportPath();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"设置导出路径失败: {ex.Message}", ex);
                return false;
            }
        }

        #endregion
        
        #region 标识页功能
        
        /// <summary>
        /// 生成标识页文字内容，根据SettingsForm中chkLstTextItems的规则
        /// </summary>
        /// <returns>标识页文字内容</returns>
        public string GenerateIdentifierPageContent()
        {
            try
            {
                LogHelper.Debug($"[标识页调试] 开始生成标识页内容");
                LogHelper.Debug($"[标识页调试] _regexResult = '{_regexResult}'");
                LogHelper.Debug($"[标识页调试] OrderNumber = '{OrderNumber}'");
                LogHelper.Debug($"[标识页调试] SelectedMaterial = '{SelectedMaterial}'");
                LogHelper.Debug($"[标识页调试] Quantity = {Quantity}");
                LogHelper.Debug($"[标识页调试] FilmType = '{FilmType}'");
                LogHelper.Debug($"[标识页调试] AdjustedDimensions = '{AdjustedDimensions}'");
                LogHelper.Debug($"[标识页调试] SerialNumber = '{SerialNumber}'");

                List<string> selectedTextItems = GetSelectedTextItemsFromSettings();
                LogHelper.Debug($"[标识页调试] 选中的文字项数量: {selectedTextItems?.Count ?? 0}");
                
                if (selectedTextItems != null)
                {
                    foreach (string item in selectedTextItems)
                    {
                        LogHelper.Debug($"[标识页调试] 选中的文字项: '{item}'");
                    }
                }

                if (selectedTextItems == null || selectedTextItems.Count == 0)
                {
                    LogHelper.Debug("没有选中的文字项，生成空白标识页");
                    return "";
                }

                List<string> contentParts = new List<string>();
                string separator = AppSettings.Separator ?? "";
                LogHelper.Debug($"[标识页调试] 使用分隔符: '{separator}'{(string.IsNullOrEmpty(separator) ? " (空分隔符)" : "")}");

                foreach (string item in selectedTextItems)
                {
                    string part = GetTextContentByItemName(item);
                    LogHelper.Debug($"[标识页调试] 文字项 '{item}' -> 内容: '{part}'");
                    if (!string.IsNullOrEmpty(part))
                    {
                        contentParts.Add(part);
                    }
                }

                string content = string.Join(separator, contentParts);
                LogHelper.Debug($"生成标识页内容: '{content}' (长度: {content?.Length ?? 0})");
                return content;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"生成标识页内容失败: {ex.Message}", ex);
                return "";
            }
        }

/// <summary>
        /// 设置正则结果（用于标识页内容生成）
        /// </summary>
        /// <param name="regexResult">正则表达式处理结果</param>
        public void SetRegexResult(string regexResult)
        {
            _regexResult = regexResult;
            LogHelper.Debug($"[标识页调试] 更新_regexResult为: '{_regexResult}'");
        }

        /// <summary>
        /// 从SettingsForm获取选中的文字项
        /// </summary>
        private List<string> GetSelectedTextItemsFromSettings()
        {
            try
            {
                // 从应用设置加载保存的文字项
                string savedItems = AppSettings.Get("TextItems") as string;
                if (string.IsNullOrEmpty(savedItems))
                {
                    // 返回默认文字项
                    return new List<string> { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号" };
                }

                List<string> selectedItems = new List<string>();
                string[] items = savedItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                // 确保数组长度为偶数（每项+状态）
                if (items.Length % 2 == 0)
                {
                    for (int i = 0; i < items.Length; i += 2)
                    {
                        string itemText = items[i];
                        bool isChecked = items[i + 1] == "True";
                        if (isChecked)
                        {
                            selectedItems.Add(itemText);
                        }
                    }
                }

                return selectedItems;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取选中文字项失败: {ex.Message}", ex);
                return new List<string> { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号" };
            }
        }

        /// <summary>
        /// 根据项目名称获取对应的文字内容
        /// </summary>
        private string GetTextContentByItemName(string itemName)
        {
            try
            {
                switch (itemName)
                {
                    case "正则结果":
                        // 在正则结果中查找并替换材料名称为带空格的格式
                        string regexResult = _regexResult ?? "";
                        if (!string.IsNullOrEmpty(regexResult))
                        {
                            // 替换常见的材料名称为带空格的格式
                            regexResult = regexResult.Replace("PVC材料", "PVC 材料")
                                                          .Replace("PET材料", "PET 材料")
                                                          .Replace("PP材料", "PP 材料")
                                                          .Replace("PE材料", "PE 材料")
                                                          .Replace("ABS材料", "ABS 材料")
                                                          .Replace("PC材料", "PC 材料")
                                                          .Replace("亚克力材料", "亚克力 材料")
                                                          .Replace("铝板材料", "铝板 材料")
                                                          .Replace("不锈钢材料", "不锈钢 材料");
                        }
                        return regexResult;
                    case "订单号":
                        return OrderNumber ?? "";
                    case "材料":
                        // 返回材料的显示文本而不是代码
                        return GetMaterialDisplayText(SelectedMaterial) ?? "";
                    case "数量":
                        return Quantity?.ToString() ?? "";
                    case "工艺":
                        // 工艺字段应该包含颜色模式和膜类型
                        string colorMode = ColorMode ?? "";
                        string filmType = FilmType ?? "";

                        if (!string.IsNullOrEmpty(colorMode) && !string.IsNullOrEmpty(filmType))
                        {
                            return colorMode + filmType;
                        }
                        else if (!string.IsNullOrEmpty(filmType))
                        {
                            return filmType;
                        }
                        else
                        {
                            return colorMode;
                        }
                    case "尺寸":
                        return AdjustedDimensions ?? "";
                    case "序号":
                        return SerialNumber ?? "";
                    case "列组合":
                        // 检查是否有列组合数据，如果没有就返回空字符串跳过显示
                        if (MatchedRows != null && MatchedRows.Count > 0)
                        {
                            // 有列组合数据时，返回序号
                            return SerialNumber ?? "";
                        }
                        else
                        {
                            // 没有列组合数据时，返回空字符串，这样在标识页内容生成时会被跳过
                            return "";
                        }
                    default:
                        return "";
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取文字内容失败 [{itemName}]: {ex.Message}", ex);
                return "";
            }
        }

/// <summary>
        /// 获取材料的显示文本（带空格的格式化名称）
        /// </summary>
        /// <param name="materialCode">材料代码</param>
        /// <returns>格式化的材料显示文本</returns>
        private string GetMaterialDisplayText(string materialCode)
        {
            if (string.IsNullOrEmpty(materialCode))
                return "";

            // 根据材料代码返回对应的显示文本
            switch (materialCode.ToLower())
            {
                case "ulyy":
                    return "氯氧铝";
                case "50#yy":
                    return "50# 亚银";
                case "100#yy":
                    return "100# 亚银";
                case "b":
                    return "哑光白";
                case "kyb":
                    return "抗油白";
                case "hc":
                    return "透明";
                case "kyhc":
                    return "抗油透明";
                case "tm":
                    return "透明膜";
                case "kytm":
                    return "抗油透明膜";
                case "gy":
                    return "光银";
                case "ls":
                    return "镭射";
                case "ltb":
                    return "离型白";
                case "alb":
                    return "亚力白";
                case "ltyy":
                    return "离型亚银";
                case "zdb":
                    return "哑光白底";
                case "np":
                    return "耐品";
                case "pet":
                    return "PET 材料";
                case "lthc":
                    return "铝箔";
                case "dxjhc":
                    return "电解铝箔";
                default:
                    // 如果代码对应到按钮，尝试获取按钮的显示文本
                    return GetMaterialButtonText(materialCode);
            }
        }

        /// <summary>
        /// 根据材料代码获取对应按钮的显示文本
        /// </summary>
        /// <param name="materialCode">材料代码</param>
        /// <returns>按钮显示文本</returns>
        private string GetMaterialButtonText(string materialCode)
        {
            try
            {
                // 检查材料按钮文本，这里使用我们之前修复的按钮文本
                switch (materialCode.ToLower())
                {
                    case "ulyy":
                        return materialButton1?.Text?.Replace("材料", "").Trim() ?? "氯氧铝";
                    case "kyb":
                        return materialButton2?.Text?.Replace("材料", "").Trim() ?? "抗油白";
                    case "hc":
                        return materialButton3?.Text?.Replace("材料", "").Trim() ?? "透明";
                    case "kyhc":
                        return materialButton4?.Text?.Replace("材料", "").Trim() ?? "抗油透明";
                    case "tm":
                        return materialButton5?.Text?.Replace("材料", "").Trim() ?? "透明膜";
                    case "gy":
                        return materialButton6?.Text?.Replace("材料", "").Trim() ?? "光银";
                    case "ls":
                        return materialButton7?.Text?.Replace("材料", "").Trim() ?? "镭射";
                    case "np":
                        return materialButton8?.Text?.Replace("材料", "").Trim() ?? "耐品";
                    case "pet":
                        return materialButton9?.Text?.Replace("材料", "").Trim() ?? "PET 材料";
                    default:
                        return materialCode; // 返回原始代码
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取材料按钮文本失败 [{materialCode}]: {ex.Message}", ex);
                return materialCode;
            }
        }
        
        /// <summary>
        /// 根据项目类型生成对应内容
        /// </summary>
        private string GenerateContentPart(string itemType)
        {
            // 简化版本：暂时返回空字符串
            // 等空白页功能正常后再重新启用文字生成功能
            return "";
        }
        
        #endregion



        
        private void tree1_SelectChanged(object sender, TreeSelectEventArgs e)
        {

        }

        private void radiusTextBox_TextChanged_1(object sender, EventArgs e)
        {

        }

                private void cyberSwitch1_Load(object sender, EventArgs e)
        {

        }

        private void confirmButton_Click_1(object sender, EventArgs e)
        {

        }

        private void cancelButton_Click_1(object sender, EventArgs e)
        {

        }

        #region 排版功能

        /// <summary>
        /// 初始化排版控件
        /// </summary>
        private void InitializeImpositionControls()
        {
            try
            {
                // 设置默认值
                if (enableImpositionCheckbox != null)
                {
                    enableImpositionCheckbox.Checked = false; // 默认不启用
                }

                if (flatSheetRadioButton != null && rollMaterialRadioButton != null)
                {
                    flatSheetRadioButton.Checked = true; // 默认平张材料
                    rollMaterialRadioButton.Checked = false;
                }

                if (continuousLayoutRadioButton != null && foldingLayoutRadioButton != null)
                {
                    continuousLayoutRadioButton.Checked = true; // 默认连拼模式
                    foldingLayoutRadioButton.Checked = false;
                }

                // 清空布局显示
                ClearLayoutDisplay();

                LogHelper.Debug("[MaterialSelectFormModern] 排版控件初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 初始化排版控件时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置排版控件事件处理器
        /// </summary>
        private void SetupImpositionEventHandlers()
        {
            try
            {
                // 启用排版复选框事件
                if (enableImpositionCheckbox != null)
                {
                    enableImpositionCheckbox.CheckedChanged += OnImpositionSettingsChanged;
                }

                // 材料类型单选按钮事件
                if (flatSheetRadioButton != null)
                {
                    flatSheetRadioButton.CheckedChanged += OnImpositionSettingsChanged;
                }

                if (rollMaterialRadioButton != null)
                {
                    rollMaterialRadioButton.CheckedChanged += OnImpositionSettingsChanged;
                }

                // 排版模式单选按钮事件
                if (continuousLayoutRadioButton != null)
                {
                    continuousLayoutRadioButton.CheckedChanged += OnImpositionSettingsChanged;
                }

                if (foldingLayoutRadioButton != null)
                {
                    foldingLayoutRadioButton.CheckedChanged += OnImpositionSettingsChanged;
                }

                LogHelper.Debug("[MaterialSelectFormModern] 排版控件事件处理器设置完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 设置排版控件事件处理器时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 排版设置变更事件处理
        /// </summary>
        private void OnImpositionSettingsChanged(object sender, EventArgs e)
        {
            try
            {
                // 当任何排版设置改变时，自动重新计算并更新显示
                if (enableImpositionCheckbox != null && enableImpositionCheckbox.Checked)
                {
                    Task.Run(() => CalculateAndUpdateLayout());
                }
                else
                {
                    // 如果禁用排版，清空显示
                    ClearLayoutDisplay();
                }

                LogHelper.Debug("[MaterialSelectFormModern] 排版设置已更新");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 处理排版设置变更时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步计算并更新布局显示
        /// </summary>
        private async Task CalculateAndUpdateLayout()
        {
            try
            {
                // 防抖处理 - 等待一段时间避免频繁计算
                await Task.Delay(200);

                if (InvokeRequired)
                {
                    Invoke((Action)(async () => await CalculateAndUpdateLayout()));
                    return;
                }

                // 获取当前配置
                var config = GetCurrentImpositionConfiguration();
                if (config == null)
                {
                    LogHelper.Debug("[MaterialSelectFormModern] 无法获取当前排版配置");
                    return;
                }

                // 更新显示为计算中状态
                UpdateLayoutDisplay("计算中...");

                // 方案A：使用 ImpositionService.AnalyzePdfFileAsync 获取真实 PDF 信息（包括页面旋转角度）
                ImpositionPdfInfo pdfInfo = null;
                
                // 尝试获取PDF文件路径
                string pdfFilePath = null;
                if (!string.IsNullOrEmpty(CurrentFileName))
                {
                    // 使用当前文件名作为文件路径
                    pdfFilePath = CurrentFileName;
                    LogHelper.Debug($"[MaterialSelectFormModern] 使用当前文件路径: {pdfFilePath}");
                }

                if (!string.IsNullOrEmpty(pdfFilePath) && System.IO.File.Exists(pdfFilePath))
                {
                    try
                    {
                        // 方案A：调用 AnalyzePdfFileAsync 获取真实PDF信息
                        pdfInfo = await _impositionService.AnalyzePdfFileAsync(pdfFilePath);
                        
                        // 保存PDF的有效尺寸（根据PageRotation调整后的显示尺寸）
                        float rawWidth = 0;
                        float rawHeight = 0;
                        
                        // 优先使用裁剪框，其次MediaBox
                        if (pdfInfo.HasCropBox && pdfInfo.CropBoxWidth > 0 && pdfInfo.CropBoxHeight > 0)
                        {
                            rawWidth = pdfInfo.CropBoxWidth;
                            rawHeight = pdfInfo.CropBoxHeight;
                        }
                        else if (pdfInfo.FirstPageSize != null)
                        {
                            rawWidth = (float)pdfInfo.FirstPageSize.Width;
                            rawHeight = (float)pdfInfo.FirstPageSize.Height;
                        }
                        
                        // 根据PageRotation调整宽高（90°或270°时互换宽高）
                        if (pdfInfo.PageRotation == 90 || pdfInfo.PageRotation == 270)
                        {
                            // 旋转90°或270°时，宽高互换
                            _pdfOriginalWidth = (double)rawHeight;
                            _pdfOriginalHeight = (double)rawWidth;
                        }
                        else
                        {
                            // 0°或180°时，宽高不变
                            _pdfOriginalWidth = (double)rawWidth;
                            _pdfOriginalHeight = (double)rawHeight;
                        }
                        
                        LogHelper.Debug($"[排版分析] 成功获取PDF信息: 页面旋转={pdfInfo.PageRotation}°, 原始尺寸={rawWidth:F1}x{rawHeight:F1}mm(来源:{(pdfInfo.HasCropBox ? "裁剪框" : "MediaBox")}), 旋转后有效尺寸={_pdfOriginalWidth:F1}x{_pdfOriginalHeight:F1}mm");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"[排版分析] 获取PDF信息失败: {ex.Message}", ex);
                        // 如果分析失败，使用备选方案（但记录警告）
                        LogHelper.Warn("[排版分析] 将使用备选方案（基于传入的PDF尺寸创建模拟PDF信息）");
                    }
                }
                else
                {
                    LogHelper.Warn($"[MaterialSelectFormModern] PDF文件路径无效或文件不存在: {pdfFilePath ?? "(null)"}");
                }

                // 如果未能获取真实 PDF 信息，使用备选方案（基于传入的PDF尺寸创建mock数据）
                if (pdfInfo == null)
                {
                    LogHelper.Debug($"[排版分析] 使用备选方案：基于传入尺寸 {_initialWidth}x{_initialHeight}mm 创建模拟 PDF 信息（无旋转角度信息）");
                    pdfInfo = new ImpositionPdfInfo
                    {
                        FilePath = "Demo.pdf",
                        FileName = "Demo.pdf",
                        PageCount = 1,
                        FirstPageSize = new Utils.PageSize { Width = (float)_initialWidth, Height = (float)_initialHeight },
                        CropBoxWidth = (float)_initialWidth,
                        CropBoxHeight = (float)_initialHeight,
                        HasCropBox = true,
                        PageRotation = 0, // 备选方案下缺少旋转角度信息
                        Errors = new List<Utils.PageBoxError>()
                    };
                }

                // 使用实际的ImpositionService进行计算
                ImpositionResult result = null;

                // 根据一式两联状态选择计算方法
                if (_isDuplicateLayoutEnabled)
                {
                    // 一式两联模式：使用偶数列计算
                    if (config is FlatSheetConfiguration flatSheetConfig)
                    {
                        result = await _impositionService.CalculateOptimalEvenColumnsLayoutAsync(flatSheetConfig, pdfInfo);
                    }
                    else if (config is RollMaterialConfiguration rollMaterialConfig)
                    {
                        result = await _impositionService.CalculateOptimalEvenColumnsLayoutAsync(rollMaterialConfig, pdfInfo);
                    }
                }
                else
                {
                    // 标准模式：使用普通布局计算
                    if (config is FlatSheetConfiguration flatSheetConfig)
                    {
                        result = await _impositionService.CalculateFlatSheetLayoutAsync(flatSheetConfig, pdfInfo);
                    }
                    else if (config is RollMaterialConfiguration rollMaterialConfig)
                    {
                        result = await _impositionService.CalculateRollMaterialLayoutAsync(rollMaterialConfig, pdfInfo);
                    }
                }

                if (result != null)
                {
                    // 保存当前排版计算结果
                    _currentImpositionResult = result;

                    // 更新布局计算结果缓存，用于重命名功能（根据用户选择的类型）
                    if (config is FlatSheetConfiguration)
                    {
                        LayoutResultsCache.UpdateFlatSheetLayoutResults(result.Rows, result.Columns);
                    }
                    else if (config is RollMaterialConfiguration)
                    {
                        LayoutResultsCache.UpdateRollMaterialLayoutResults(result.Rows, result.Columns);
                    }

                    // 更新显示实际计算结果
                    UpdateLayoutDisplay(result);
                    LogHelper.Debug($"[MaterialSelectFormModern] 布局计算完成: {result.Rows}x{result.Columns}, 利用率: {result.SpaceUtilization:F1}%, 旋转角度: {result.RotationAngle}°");

                    // 计算两种布局数并缓存（用于平张列和卷装列同时显示）
                    await CalculateBothLayoutTypesAsync(pdfInfo);
                }
                else
                {
                    UpdateLayoutDisplay("计算失败");
                    LogHelper.Warn("[MaterialSelectFormModern] 布局计算返回空结果");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 计算布局时发生错误: {ex.Message}");
                if (InvokeRequired)
                {
                    Invoke((Action)(() =>
                    {
                        UpdateLayoutDisplay("计算错误");
                    }));
                }
                else
                {
                    UpdateLayoutDisplay("计算错误");
                }
            }
        }

        /// <summary>
        /// 计算平张和卷装两种布局类型并缓存结果
        /// 用于平张列和卷装列同时显示
        /// </summary>
        private async Task CalculateBothLayoutTypesAsync(ImpositionPdfInfo pdfInfo)
        {
            try
            {
                if (pdfInfo == null)
                    return;

                // 获取平张配置
                var flatSheetConfig = GetFlatSheetConfiguration();
                // 获取卷装配置
                var rollConfig = GetRollMaterialConfiguration();

                ImpositionResult flatSheetResult = null;
                ImpositionResult rollResult = null;

                // 并行计算两种布局
                if (flatSheetConfig != null)
                {
                    flatSheetResult = await _impositionService.CalculateFlatSheetLayoutAsync(flatSheetConfig, pdfInfo);
                }

                if (rollConfig != null)
                {
                    rollResult = await _impositionService.CalculateRollMaterialLayoutAsync(rollConfig, pdfInfo);
                }

                // 更新缓存
                if (flatSheetResult != null && flatSheetResult.OptimalLayoutQuantity > 0)
                {
                    LayoutResultsCache.UpdateFlatSheetLayoutResults(flatSheetResult.Rows, flatSheetResult.Columns);
                    LogHelper.Debug($"[MaterialSelectFormModern] 平张布局计算完成: {flatSheetResult.Rows}x{flatSheetResult.Columns}={flatSheetResult.OptimalLayoutQuantity}");
                }
                else
                {
                    LayoutResultsCache.UpdateFlatSheetLayoutResults(0, 0);
                }

                if (rollResult != null && rollResult.OptimalLayoutQuantity > 0)
                {
                    LayoutResultsCache.UpdateRollMaterialLayoutResults(rollResult.Rows, rollResult.Columns);
                    LogHelper.Debug($"[MaterialSelectFormModern] 卷装布局计算完成: {rollResult.Rows}x{rollResult.Columns}={rollResult.OptimalLayoutQuantity}");
                }
                else
                {
                    LayoutResultsCache.UpdateRollMaterialLayoutResults(0, 0);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 计算两种布局类型时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取平张配置（从设置中加载）
        /// </summary>
        private FlatSheetConfiguration GetFlatSheetConfiguration()
        {
            try
            {
                var paperWidthStr = AppSettings.Get("Imposition_PaperWidth") as string;
                var paperHeightStr = AppSettings.Get("Imposition_PaperHeight") as string;

                if (string.IsNullOrEmpty(paperWidthStr) || string.IsNullOrEmpty(paperHeightStr))
                    return null;

                var paperWidth = GetFloatValue(paperWidthStr, 0);
                var paperHeight = GetFloatValue(paperHeightStr, 0);

                if (paperWidth <= 0 || paperHeight <= 0)
                    return null;

                var marginTopStr = AppSettings.Get("Imposition_MarginTop") as string;
                var marginBottomStr = AppSettings.Get("Imposition_MarginBottom") as string;
                var marginLeftStr = AppSettings.Get("Imposition_MarginLeft") as string;
                var marginRightStr = AppSettings.Get("Imposition_MarginRight") as string;
                var rowsStr = AppSettings.Get("Imposition_Rows") as string;
                var columnsStr = AppSettings.Get("Imposition_Columns") as string;

                return new FlatSheetConfiguration
                {
                    PaperWidth = paperWidth,
                    PaperHeight = paperHeight,
                    MarginTop = GetFloatValue(marginTopStr, 0),
                    MarginBottom = GetFloatValue(marginBottomStr, 0),
                    MarginLeft = GetFloatValue(marginLeftStr, 0),
                    MarginRight = GetFloatValue(marginRightStr, 0),
                    Rows = GetIntValue(rowsStr, 0),
                    Columns = GetIntValue(columnsStr, 0)
                };
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"[MaterialSelectFormModern] 获取平张配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取卷装配置（从设置中加载）
        /// </summary>
        private RollMaterialConfiguration GetRollMaterialConfiguration()
        {
            try
            {
                var rollWidthStr = AppSettings.Get("Imposition_FixedWidth") as string;
                var minLengthStr = AppSettings.Get("Imposition_MinLength") as string;

                if (string.IsNullOrEmpty(rollWidthStr) || string.IsNullOrEmpty(minLengthStr))
                    return null;

                var rollWidth = GetFloatValue(rollWidthStr, 0);
                var minLength = GetFloatValue(minLengthStr, 0);

                if (rollWidth <= 0 || minLength <= 0)
                    return null;

                var marginTopStr = AppSettings.Get("Imposition_RollMarginTop") as string;
                var marginBottomStr = AppSettings.Get("Imposition_RollMarginBottom") as string;
                var marginLeftStr = AppSettings.Get("Imposition_RollMarginLeft") as string;
                var marginRightStr = AppSettings.Get("Imposition_RollMarginRight") as string;

                return new RollMaterialConfiguration
                {
                    FixedWidth = rollWidth,
                    MinLength = minLength,
                    MarginTop = GetFloatValue(marginTopStr, 0),
                    MarginBottom = GetFloatValue(marginBottomStr, 0),
                    MarginLeft = GetFloatValue(marginLeftStr, 0),
                    MarginRight = GetFloatValue(marginRightStr, 0)
                };
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"[MaterialSelectFormModern] 获取卷装配置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取当前排版配置
        /// </summary>
        private object GetCurrentImpositionConfiguration()
        {
            try
            {
                if (!enableImpositionCheckbox?.Checked == true)
                {
                    return null;
                }

                var materialType = flatSheetRadioButton?.Checked == true ? "FlatSheet" : "RollMaterial";
                var layoutMode = continuousLayoutRadioButton?.Checked == true ? LayoutMode.Continuous : LayoutMode.Folding;

                LogHelper.Debug($"[MaterialSelectFormModern] 获取排版配置: {materialType}, {layoutMode}, PDF原始尺寸: {_initialWidth}x{_initialHeight}");

                if (materialType == "FlatSheet")
                {
                    // 印刷行业要求：必须从设置中明确加载平张纸张尺寸，不允许使用备选值
                    var paperWidthStr = AppSettings.Get("Imposition_PaperWidth") as string;
                    var paperHeightStr = AppSettings.Get("Imposition_PaperHeight") as string;

                    if (string.IsNullOrEmpty(paperWidthStr))
                    {
                        throw new InvalidOperationException("平张纸张宽度未设置！请在设置中明确配置纸张宽度。");
                    }
                    if (string.IsNullOrEmpty(paperHeightStr))
                    {
                        throw new InvalidOperationException("平张纸张高度未设置！请在设置中明确配置纸张高度。");
                    }

                    var paperWidth = GetFloatValue(paperWidthStr, 0);
                    var paperHeight = GetFloatValue(paperHeightStr, 0);

                    // 验证设置值的有效性
                    if (paperWidth <= 0)
                    {
                        throw new InvalidOperationException($"平张纸张宽度设置无效：{paperWidth}mm。请设置有效的纸张宽度。");
                    }
                    if (paperHeight <= 0)
                    {
                        throw new InvalidOperationException($"平张纸张高度设置无效：{paperHeight}mm。请设置有效的纸张高度。");
                    }

                    // 从设置中加载边距和行列配置
                    var marginTopStr = AppSettings.Get("Imposition_MarginTop") as string;
                    var marginBottomStr = AppSettings.Get("Imposition_MarginBottom") as string;
                    var marginLeftStr = AppSettings.Get("Imposition_MarginLeft") as string;
                    var marginRightStr = AppSettings.Get("Imposition_MarginRight") as string;
                    var rowsStr = AppSettings.Get("Imposition_Rows") as string;
                    var columnsStr = AppSettings.Get("Imposition_Columns") as string;

                    // 印刷行业要求：边距也必须明确设置，不允许使用默认值
                    if (string.IsNullOrEmpty(marginTopStr))
                        throw new InvalidOperationException("平张上边距未设置！请在设置中明确配置上边距。");
                    if (string.IsNullOrEmpty(marginBottomStr))
                        throw new InvalidOperationException("平张下边距未设置！请在设置中明确配置下边距。");
                    if (string.IsNullOrEmpty(marginLeftStr))
                        throw new InvalidOperationException("平张左边距未设置！请在设置中明确配置左边距。");
                    if (string.IsNullOrEmpty(marginRightStr))
                        throw new InvalidOperationException("平张右边距未设置！请在设置中明确配置右边距。");

                    return new FlatSheetConfiguration
                    {
                        PaperWidth = paperWidth,
                        PaperHeight = paperHeight,
                        MarginTop = GetFloatValue(marginTopStr, 0),
                        MarginBottom = GetFloatValue(marginBottomStr, 0),
                        MarginLeft = GetFloatValue(marginLeftStr, 0),
                        MarginRight = GetFloatValue(marginRightStr, 0),
                        Rows = GetIntValue(rowsStr, 0),
                        Columns = GetIntValue(columnsStr, 0)
                    };
                }
                else
                {
                    // 从设置中加载卷装材料页面尺寸配置
                    var rollWidthStr = AppSettings.Get("Imposition_FixedWidth") as string;
                    var minLengthStr = AppSettings.Get("Imposition_MinLength") as string;

                    // 印刷行业要求：必须明确设置卷装材料尺寸，不允许使用备选值
                    if (string.IsNullOrEmpty(rollWidthStr))
                    {
                        throw new InvalidOperationException("卷装材料宽度未设置！请在设置中明确配置卷装宽度。");
                    }
                    if (string.IsNullOrEmpty(minLengthStr))
                    {
                        throw new InvalidOperationException("卷装材料最小长度未设置！请在设置中明确配置最小长度。");
                    }

                    var fixedWidth = GetFloatValue(rollWidthStr, 0);
                    var minLength = GetFloatValue(minLengthStr, 0);

                    // 验证设置值的有效性
                    if (fixedWidth <= 0)
                    {
                        throw new InvalidOperationException($"卷装材料宽度设置无效：{fixedWidth}mm。请设置有效的卷装宽度。");
                    }
                    if (minLength <= 0)
                    {
                        throw new InvalidOperationException($"卷装材料最小长度设置无效：{minLength}mm。请设置有效的最小长度。");
                    }

                    // 从设置中加载卷装专用边距和行列配置
                    var marginTopStr = AppSettings.Get("Imposition_RollMarginTop") as string;
                    var marginBottomStr = AppSettings.Get("Imposition_RollMarginBottom") as string;
                    var marginLeftStr = AppSettings.Get("Imposition_RollMarginLeft") as string;
                    var marginRightStr = AppSettings.Get("Imposition_RollMarginRight") as string;
                    var rowsStr = AppSettings.Get("Imposition_Rows") as string;
                    var columnsStr = AppSettings.Get("Imposition_Columns") as string;

                    return new RollMaterialConfiguration
                    {
                        FixedWidth = fixedWidth,
                        MinLength = minLength,
                        MarginTop = GetFloatValue(marginTopStr, 10f),
                        MarginBottom = GetFloatValue(marginBottomStr, 10f),
                        MarginLeft = GetFloatValue(marginLeftStr, 10f),
                        MarginRight = GetFloatValue(marginRightStr, 10f),
                        Rows = GetIntValue(rowsStr, 0),
                        Columns = GetIntValue(columnsStr, 0)
                    };
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 获取排版配置时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取浮点数值
        /// </summary>
        private float GetFloatValue(string value, float defaultValue)
        {
            if (float.TryParse(value, out float result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 获取整数值
        /// </summary>
        private int GetIntValue(string value, int defaultValue)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 获取材料宽度
        /// </summary>
        private float GetMaterialWidth()
        {
            try
            {
                // 印刷行业要求：必须从设置中明确加载卷装材料宽度，不允许使用备选值
                var rollWidthStr = AppSettings.Get("Imposition_FixedWidth") as string;

                if (string.IsNullOrEmpty(rollWidthStr))
                {
                    throw new InvalidOperationException("卷装材料宽度未设置！请在设置中明确配置卷装宽度。");
                }

                if (!float.TryParse(rollWidthStr, out float savedWidth))
                {
                    throw new InvalidOperationException($"卷装材料宽度设置格式错误：'{rollWidthStr}'。请输入有效的数字。");
                }

                if (savedWidth <= 0)
                {
                    throw new InvalidOperationException($"卷装材料宽度设置无效：{savedWidth}mm。请设置大于0的有效宽度。");
                }

                LogHelper.Debug($"[MaterialSelectFormModern] 从设置加载卷装宽度: {savedWidth}mm");
                return savedWidth;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 获取卷装材料宽度失败: {ex.Message}");
                throw; // 印刷行业要求：直接抛出异常，不允许使用备选值
            }
        }

        /// <summary>
        /// 获取材料高度
        /// </summary>
        private float GetMaterialHeight()
        {
            try
            {
                // 尝试从当前PDF文件尺寸获取基础高度，如果没有则使用默认值
                if (_initialHeight > 0)
                {
                    // 基于当前文件高度加上出血和边距计算材料高度
                    float baseHeight = (float)_initialHeight + (float)SelectedTetBleed * 2;
                    return Math.Max(baseHeight, 450f); // 最小450mm
                }

                // 使用默认材料高度
                return 450f;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"[MaterialSelectFormModern] 获取材料高度失败，使用默认值: {ex.Message}");
                return 450f;
            }
        }

        /// <summary>
        /// 获取材料长度（用于卷装材料）
        /// </summary>
        private float GetMaterialLength()
        {
            try
            {
                // 印刷行业要求：必须从设置中明确加载卷装材料最小长度，不允许使用备选值
                var minLengthStr = AppSettings.Get("Imposition_MinLength") as string;

                if (string.IsNullOrEmpty(minLengthStr))
                {
                    throw new InvalidOperationException("卷装材料最小长度未设置！请在设置中明确配置最小长度。");
                }

                if (!float.TryParse(minLengthStr, out float savedLength))
                {
                    throw new InvalidOperationException($"卷装材料最小长度设置格式错误：'{minLengthStr}'。请输入有效的数字。");
                }

                if (savedLength <= 0)
                {
                    throw new InvalidOperationException($"卷装材料最小长度设置无效：{savedLength}mm。请设置大于0的有效长度。");
                }

                LogHelper.Debug($"[MaterialSelectFormModern] 从设置加载卷装最小长度: {savedLength}mm");
                return savedLength;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 获取卷装材料最小长度失败: {ex.Message}");
                throw; // 印刷行业要求：直接抛出异常，不允许使用备选值
            }
        }

        /// <summary>
        /// 获取当前出血值
        /// </summary>
        private float GetCurrentBleedValue()
        {
            try
            {
                return (float)SelectedTetBleed;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"[MaterialSelectFormModern] 获取出血值失败，使用默认值: {ex.Message}");
                return 2f;
            }
        }

        /// <summary>
        /// 获取当前间距值
        /// </summary>
        private float GetCurrentSpacingValue()
        {
            try
            {
                // 默认间距为0，可以根据需要添加界面控件来设置
                return 0f;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"[MaterialSelectFormModern] 获取间距值失败，使用默认值: {ex.Message}");
                return 0f;
            }
        }

        /// <summary>
        /// 更新布局显示
        /// </summary>
        private void UpdateLayoutDisplay(string status)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke((Action)(() => UpdateLayoutDisplay(status)));
                    return;
                }

                // 更新行数显示
                if (rowsDisplayLabel != null)
                {
                    rowsDisplayLabel.Text = status == "计算中..." ? "计算中..." : $"行数: {status}";
                }

                // 更新列数显示
                if (columnsDisplayLabel != null)
                {
                    columnsDisplayLabel.Text = status == "计算中..." ? "计算中..." : $"列数: {status}";
                }

                // 更新布局数量显示
                if (layoutCountDisplayLabel != null)
                {
                    layoutCountDisplayLabel.Text = status == "计算中..." ? "计算中..." : $"布局数量: {status}";
                }

                // 更新旋转角度显示（只在计算中时更新，其他情况保持原有值）
                if (rotationDisplayLabel != null)
                {
                    if (status == "计算中...")
                    {
                        rotationDisplayLabel.Text = "计算中...";
                    }
                    // 注意：其他情况不更新旋转角度，避免覆盖UpdateLayoutDisplay(ImpositionResult)设置的值
                }

                // 更新PDF页面尺寸显示
                var pdfSizeDisplayLabel = Controls.Find("pdfSizeDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (pdfSizeDisplayLabel != null)
                {
                    pdfSizeDisplayLabel.Text = status == "计算中..." ? "计算中..." : FormatPdfSize(_initialWidth, _initialHeight);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 更新布局显示时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新布局显示（完整版本）
        /// </summary>
        private void UpdateLayoutDisplay(string rows, string columns, string layoutCount)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke((Action)(() => UpdateLayoutDisplay(rows, columns, layoutCount)));
                    return;
                }

                // 更新行数显示
                var rowsDisplayLabel = Controls.Find("rowsDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (rowsDisplayLabel != null)
                {
                    rowsDisplayLabel.Text = $"行数: {rows}";
                }

                // 更新列数显示
                var columnsDisplayLabel = Controls.Find("columnsDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (columnsDisplayLabel != null)
                {
                    columnsDisplayLabel.Text = $"列数: {columns}";
                }

                // 更新布局数量显示
                var layoutCountDisplayLabel = Controls.Find("layoutCountDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (layoutCountDisplayLabel != null)
                {
                    layoutCountDisplayLabel.Text = $"布局数量: {layoutCount}";
                }

                // 更新PDF页面尺寸显示（显示PDF的原始宽×高，用于验证布局计算是否正确）
                var pdfSizeDisplayLabel = Controls.Find("pdfSizeDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (pdfSizeDisplayLabel != null)
                {
                    // 使用PDF的原始宽高（未经"大数在前"处理）
                    if (_pdfOriginalWidth > 0 && _pdfOriginalHeight > 0)
                    {
                        pdfSizeDisplayLabel.Text = FormatPdfSize(_pdfOriginalWidth, _pdfOriginalHeight);
                    }
                    else
                    {
                        // 备选方案：如果没有获取到原始宽高，使用_initialWidth/_initialHeight
                        pdfSizeDisplayLabel.Text = FormatPdfSize(_initialWidth, _initialHeight);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 更新布局显示时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新布局显示 - 使用ImpositionResult对象
        /// </summary>
        private void UpdateLayoutDisplay(ImpositionResult result)
        {
            try
            {
                LogHelper.Debug($"[MaterialSelectFormModern] UpdateLayoutDisplay(ImpositionResult) 被调用: Success={result.Success}, RotationAngle={result.RotationAngle}, Rows={result.Rows}, Columns={result.Columns}, InvokeRequired={InvokeRequired}, ThreadId={System.Threading.Thread.CurrentThread.ManagedThreadId}");

                if (InvokeRequired)
                {
                    LogHelper.Debug("[MaterialSelectFormModern] 需要Invoke到UI线程执行UpdateLayoutDisplay");
                    Invoke((Action)(() => UpdateLayoutDisplay(result)));
                    return;
                }

                // 保存当前排版计算结果
                _currentImpositionResult = result;

                // 更新行数显示
                if (rowsDisplayLabel != null)
                {
                    rowsDisplayLabel.Text = $"行数: {result.Rows}";
                }

                // 更新列数显示
                if (columnsDisplayLabel != null)
                {
                    columnsDisplayLabel.Text = $"列数: {result.Columns}";
                }

                // 更新布局数量显示
                if (layoutCountDisplayLabel != null)
                {
                    layoutCountDisplayLabel.Text = $"布局数量: {result.OptimalLayoutQuantity}";
                }

                // 更新旋转角度显示
                if (rotationDisplayLabel != null)
                {
                    LogHelper.Debug($"[MaterialSelectFormModern] 更新旋转角度显示: RotationAngle={result.RotationAngle}, UseRotation={result.UseRotation}, 在UI线程执行: {!InvokeRequired}");
                    var newText = $"旋转角度: {result.RotationAngle}°";
                    LogHelper.Debug($"[MaterialSelectFormModern] 设置rotationDisplayLabel.Text = '{newText}'");
                    rotationDisplayLabel.Text = newText;
                    LogHelper.Debug($"[MaterialSelectFormModern] rotationDisplayLabel.Text设置完成: '{rotationDisplayLabel.Text}'");
                }
                else
                {
                    LogHelper.Warn("[MaterialSelectFormModern] rotationDisplayLabel字段为null");
                }

                // 更新PDF页面尺寸显示（显示PDF的原始宽×高，用于验证布局计算是否正确）
                var pdfSizeDisplayLabel = Controls.Find("pdfSizeDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (pdfSizeDisplayLabel != null)
                {
                    // 使用PDF的原始宽高（未经"大数在前"处理）
                    if (_pdfOriginalWidth > 0 && _pdfOriginalHeight > 0)
                    {
                        pdfSizeDisplayLabel.Text = FormatPdfSize(_pdfOriginalWidth, _pdfOriginalHeight);
                    }
                    else
                    {
                        // 备选方案：如果没有获取到原始宽高，使用_initialWidth/_initialHeight
                        pdfSizeDisplayLabel.Text = FormatPdfSize(_initialWidth, _initialHeight);
                    }
                }

                // 显示计算结果验证和提示
                DisplayCalculationValidationAndTips(result);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 更新布局显示时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示计算结果验证和提示信息
        /// </summary>
        private void DisplayCalculationValidationAndTips(ImpositionResult result)
        {
            try
            {
                // 记录布局计算描述信息
                if (!string.IsNullOrEmpty(result.Description))
                {
                    LogHelper.Debug($"[MaterialSelectFormModern] 布局计算描述: {result.Description}");
                }

                var tips = new List<string>();
                var validationMessages = new List<string>();

                // 基本成功性验证
                if (!result.Success)
                {
                    validationMessages.Add($"❌ 计算失败: {result.ErrorMessage}");
                }
                else
                {
                    validationMessages.Add("✅ 计算成功");
                }

                // 材料利用率分析
                if (result.SpaceUtilization < 50)
                {
                    tips.Add("⚠️ 材料利用率较低 (< 50%)，建议调整材料尺寸或布局参数");
                }
                else if (result.SpaceUtilization > 90)
                {
                    tips.Add("✅ 材料利用率优秀 (> 90%)");
                }
                else if (result.SpaceUtilization > 75)
                {
                    tips.Add("✅ 材料利用率良好 (> 75%)");
                }

                // 布局数量建议
                if (result.OptimalLayoutQuantity == 1)
                {
                    tips.Add("💡 当前布局每页只能放置1个页面，考虑使用更小的材料尺寸");
                }
                else if (result.OptimalLayoutQuantity >= 8)
                {
                    tips.Add("✅ 布局效率高，每页可放置多个页面");
                }

                // 行列配置建议
                if (result.Rows * result.Columns != result.OptimalLayoutQuantity)
                {
                    tips.Add($"📐 当前布局: {result.Rows}行 × {result.Columns}列 = {result.Rows * result.Columns}个位置");
                    tips.Add($"📊 实际使用: {result.OptimalLayoutQuantity}个页面");
                }

                // 材料类型相关提示
                var config = GetCurrentImpositionConfiguration();
                if (config is FlatSheetConfiguration)
                {
                    tips.Add("📄 平张模式: 适合标准尺寸的单页印刷");
                }
                else if (config is RollMaterialConfiguration)
                {
                    tips.Add("🔄 卷装模式: 适合大批量连续印刷");
                }

                // 显示所有提示信息
                if (validationMessages.Count > 0 || tips.Count > 0)
                {
                    var allMessages = new List<string>();
                    allMessages.AddRange(validationMessages);
                    allMessages.AddRange(tips);

                    LogHelper.Debug($"[MaterialSelectFormModern] 计算结果验证: {string.Join("; ", allMessages)}");

                    // 可以在这里添加UI显示逻辑，例如在状态栏或工具提示中显示这些信息
                    // 暂时通过日志输出，后续可以根据需要添加专门的显示控件
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 显示计算结果验证时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空布局显示
        /// </summary>
        private void ClearLayoutDisplay()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke((Action)(ClearLayoutDisplay));
                    return;
                }

                // 清空当前排版计算结果缓存，确保GetRows和GetColumns返回0
                _currentImpositionResult = null;

                // 更新行数显示
                var rowsDisplayLabel = Controls.Find("rowsDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (rowsDisplayLabel != null)
                {
                    rowsDisplayLabel.Text = "行数: —";
                }

                // 更新列数显示
                var columnsDisplayLabel = Controls.Find("columnsDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (columnsDisplayLabel != null)
                {
                    columnsDisplayLabel.Text = "列数: —";
                }

                // 更新布局数量显示
                var layoutCountDisplayLabel = Controls.Find("layoutCountDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (layoutCountDisplayLabel != null)
                {
                    layoutCountDisplayLabel.Text = "布局数量: —";
                }

                // 清空旋转角度显示
                var rotationDisplayLabel = Controls.Find("rotationDisplayLabel", true).FirstOrDefault() as AntdUI.Label;
                if (rotationDisplayLabel != null)
                {
                    rotationDisplayLabel.Text = "旋转角度: —";
                }

                // 清除布局结果缓存，防止它们在重命名结果中再次出现
                LayoutResultsCache.ClearLayoutResults();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 清空布局显示时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 重新加载排版配置并更新显示
        /// </summary>
        public void ReloadImpositionConfiguration()
        {
            try
            {
                LogHelper.Debug("[MaterialSelectFormModern] 重新加载排版配置");

                // 如果启用了排版功能，触发布局重新计算
                if (enableImpositionCheckbox?.Checked == true)
                {
                    _ = CalculateAndUpdateLayout();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 重新加载排版配置时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取排版模式
        /// </summary>
        /// <returns>排版模式</returns>
        public LayoutMode GetLayoutMode()
        {
            if (continuousLayoutRadioButton != null && continuousLayoutRadioButton.Checked)
            {
                return LayoutMode.Continuous;
            }
            else if (foldingLayoutRadioButton != null && foldingLayoutRadioButton.Checked)
            {
                return LayoutMode.Folding;
            }
            return LayoutMode.Continuous; // 默认连拼模式
        }

        /// <summary>
        /// 获取布局数量（每纸页数）
        /// <summary>
        /// 获取布局数量（每纸页数）
        /// </summary>
        /// <returns>布局数量</returns>
        public int GetLayoutQuantity()
        {
            // 从排版计算结果中获取布局数量
            if (_currentImpositionResult != null)
            {
                return _currentImpositionResult.OptimalLayoutQuantity;
            }
            return 0;
        }

        /// <summary>
        /// 获取排版功能是否启用
        /// </summary>
        /// <returns>是否启用排版</returns>
        public bool GetIsImpositionEnabled()
        {
            return enableImpositionCheckbox?.Checked == true;
        }

/// <summary>
        /// 获取布局计算的行数
        /// </summary>
        /// <returns>行数，如果未计算则返回0</returns>
        public int GetRows()
        {
            // 从排版本计算结果中获取行数
            if (_currentImpositionResult != null)
            {
                return _currentImpositionResult.Rows;
            }
            return 0;
        }

        /// <summary>
        /// 获取布局计算的列数
        /// </summary>
        /// <returns>列数，如果未计算则返回0</returns>
        public int GetColumns()
        {
            // 从排版本计算结果中获取列数
            if (_currentImpositionResult != null)
            {
                return _currentImpositionResult.Columns;
            }
            return 0;
        }

        /// <summary>
        /// 获取页面旋转角度（从布局计算结果中）
        /// </summary>
        /// <returns>旋转角度（0°或270°）</returns>
        public int GetRotationAngle()
        {
            // 从排版计算结果中获取旋转角度
            if (_currentImpositionResult != null)
            {
                return _currentImpositionResult.RotationAngle;
            }
            return 0; // 默认不旋转
        }

        /// <summary>
        /// 保存排版控件状态到AppSettings
        /// </summary>
        private void SaveImpositionControlStates()
        {
            try
            {
                // 保存启用排版复选框状态
                if (enableImpositionCheckbox != null)
                {
                    AppSettings.EnableImpositionChecked = enableImpositionCheckbox.Checked;
                    LogHelper.Debug($"[MaterialSelectFormModern] 保存启用排版状态: {enableImpositionCheckbox.Checked}");
                }

                // 保存材料类型选择状态
                string materialType = "平张"; // 默认值
                if (flatSheetRadioButton != null && flatSheetRadioButton.Checked)
                {
                    materialType = "平张";
                }
                else if (rollMaterialRadioButton != null && rollMaterialRadioButton.Checked)
                {
                    materialType = "卷装";
                }
                AppSettings.LastMaterialType = materialType;
                LogHelper.Debug($"[MaterialSelectFormModern] 保存材料类型: {materialType}");

                // 保存排版模式选择状态
                string layoutMode = "连拼模式"; // 默认值
                if (continuousLayoutRadioButton != null && continuousLayoutRadioButton.Checked)
                {
                    layoutMode = "连拼模式";
                }
                else if (foldingLayoutRadioButton != null && foldingLayoutRadioButton.Checked)
                {
                    layoutMode = "折手模式";
                }
                AppSettings.LastLayoutMode = layoutMode;
                LogHelper.Debug($"[MaterialSelectFormModern] 保存排版模式: {layoutMode}");

                // 保存一式两联按钮状态
                AppSettings.Set("LastDuplicateLayoutEnabled", _isDuplicateLayoutEnabled);
                LogHelper.Debug($"[MaterialSelectFormModern] 保存一式两联状态: {_isDuplicateLayoutEnabled}");

                // 保存设置到文件
                AppSettings.Save();
                LogHelper.Debug("[MaterialSelectFormModern] 排版控件状态已保存到AppSettings");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 保存排版控件状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从AppSettings加载排版控件状态
        /// </summary>
        private void LoadImpositionControlStates()
        {
            try
            {
                // 恢复启用排版复选框状态
                if (enableImpositionCheckbox != null)
                {
                    bool enableImposition = AppSettings.EnableImpositionChecked;
                    enableImpositionCheckbox.Checked = enableImposition;
                    LogHelper.Debug($"[MaterialSelectFormModern] 恢复启用排版状态: {enableImposition}");
                }

                // 恢复材料类型选择状态
                string materialType = AppSettings.LastMaterialType ?? "平张";
                if (flatSheetRadioButton != null && rollMaterialRadioButton != null)
                {
                    flatSheetRadioButton.Checked = (materialType == "平张");
                    rollMaterialRadioButton.Checked = (materialType == "卷装");
                    LogHelper.Debug($"[MaterialSelectFormModern] 恢复材料类型: {materialType}");
                }

                // 恢复排版模式选择状态
                string layoutMode = AppSettings.LastLayoutMode ?? "连拼模式";
                if (continuousLayoutRadioButton != null && foldingLayoutRadioButton != null)
                {
                    continuousLayoutRadioButton.Checked = (layoutMode == "连拼模式");
                    foldingLayoutRadioButton.Checked = (layoutMode == "折手模式");
                    LogHelper.Debug($"[MaterialSelectFormModern] 恢复排版模式: {layoutMode}");
                }

                // 恢复一式两联复选框状态
                var duplicateEnabledSetting = AppSettings.Get("LastDuplicateLayoutEnabled") as bool?;
                if (duplicateEnabledSetting.HasValue)
                {
                    _isDuplicateLayoutEnabled = duplicateEnabledSetting.Value;
                    LogHelper.Debug($"[MaterialSelectFormModern] 恢复一式两联状态: {_isDuplicateLayoutEnabled}");

                    // 更新复选框状态
                    if (duplicateLayoutCheckbox != null)
                    {
                        duplicateLayoutCheckbox.Checked = _isDuplicateLayoutEnabled;
                    }
                }

                LogHelper.Debug("[MaterialSelectFormModern] 排版控件状态已从AppSettings恢复");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 恢复排版控件状态失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 一式两联复选框选中状态改变事件处理
        /// </summary>
        private void DuplicateLayoutCheckbox_CheckedChanged(object sender, BoolEventArgs e)
        {
            try
            {
                // 从事件参数获取状态
                _isDuplicateLayoutEnabled = e.Value;

                if (_isDuplicateLayoutEnabled)
                {
                    LogHelper.Info("[MaterialSelectFormModern] 一式两联模式已激活，开始重新计算布局（偶数列）");

                    // 检查当前是否启用排版
                    if (enableImpositionCheckbox?.Checked != true)
                    {
                        MessageBox.Show("请先启用排版功能", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        if (duplicateLayoutCheckbox != null)
                        {
                            duplicateLayoutCheckbox.Checked = false;
                        }
                        _isDuplicateLayoutEnabled = false;
                        return;
                    }

                    // 获取当前配置
                    var config = GetCurrentImpositionConfiguration();
                    if (config == null)
                    {
                        MessageBox.Show("无法获取当前排版配置", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (duplicateLayoutCheckbox != null)
                        {
                            duplicateLayoutCheckbox.Checked = false;
                        }
                        _isDuplicateLayoutEnabled = false;
                        return;
                    }

                    // 异步计算一式两联布局
                    Task.Run(async () => await CalculateDuplicateLayout());
                }
                else
                {
                    LogHelper.Info("[MaterialSelectFormModern] 一式两联模式已取消，恢复标准布局计算");

                    // 恢复标准布局计算
                    if (enableImpositionCheckbox?.Checked == true)
                    {
                        Task.Run(async () => await CalculateAndUpdateLayout());
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 一式两联复选框状态改变事件处理错误: {ex.Message}", ex);
                MessageBox.Show($"一式两联功能发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // 出错时重置状态
                if (duplicateLayoutCheckbox != null)
                {
                    duplicateLayoutCheckbox.Checked = false;
                }
                _isDuplicateLayoutEnabled = false;
            }
        }

        
        /// <summary>
        /// 计算一式两联布局（确保列数为最优偶数）
        /// </summary>
        private async Task CalculateDuplicateLayout()
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke((Action)(async () => await CalculateDuplicateLayout()));
                    return;
                }

                // 更新显示为计算中状态
                UpdateLayoutDisplay("计算中...");

                // 获取当前配置
                var config = GetCurrentImpositionConfiguration();
                if (config == null)
                {
                    UpdateLayoutDisplay("配置错误");
                    return;
                }

                // 获取PDF信息
                ImpositionPdfInfo pdfInfo = null;
                string pdfFilePath = CurrentFileName;

                if (!string.IsNullOrEmpty(pdfFilePath) && System.IO.File.Exists(pdfFilePath))
                {
                    try
                    {
                        pdfInfo = await _impositionService.AnalyzePdfFileAsync(pdfFilePath);

                        // 保存PDF的有效尺寸
                        float rawWidth = 0;
                        float rawHeight = 0;

                        if (pdfInfo.HasCropBox && pdfInfo.CropBoxWidth > 0 && pdfInfo.CropBoxHeight > 0)
                        {
                            rawWidth = pdfInfo.CropBoxWidth;
                            rawHeight = pdfInfo.CropBoxHeight;
                        }
                        else if (pdfInfo.FirstPageSize != null)
                        {
                            rawWidth = (float)pdfInfo.FirstPageSize.Width;
                            rawHeight = (float)pdfInfo.FirstPageSize.Height;
                        }

                        // 根据PageRotation调整宽高
                        if (pdfInfo.PageRotation == 90 || pdfInfo.PageRotation == 270)
                        {
                            _pdfOriginalWidth = (double)rawHeight;
                            _pdfOriginalHeight = (double)rawWidth;
                        }
                        else
                        {
                            _pdfOriginalWidth = (double)rawWidth;
                            _pdfOriginalHeight = (double)rawHeight;
                        }

                        LogHelper.Debug($"[一式两联] 获取PDF信息: 旋转={pdfInfo.PageRotation}°, 尺寸={_pdfOriginalWidth:F1}x{_pdfOriginalHeight:F1}mm");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"[一式两联] 获取PDF信息失败: {ex.Message}", ex);
                    }
                }

                // 如果未能获取PDF信息，使用备选方案
                if (pdfInfo == null)
                {
                    pdfInfo = new ImpositionPdfInfo
                    {
                        FilePath = "Demo.pdf",
                        FileName = "Demo.pdf",
                        PageCount = 1,
                        FirstPageSize = new Utils.PageSize { Width = (float)_initialWidth, Height = (float)_initialHeight },
                        HasCropBox = false,
                        CropBoxWidth = (float)_initialWidth,
                        CropBoxHeight = (float)_initialHeight,
                        PageRotation = 0
                    };
                    _pdfOriginalWidth = _initialWidth;
                    _pdfOriginalHeight = _initialHeight;
                }

                // 计算布局
                ImpositionResult result = null;
                var materialType = flatSheetRadioButton?.Checked == true ? "FlatSheet" : "RollMaterial";

                if (materialType == "FlatSheet")
                {
                    // 对于平张材料，需要获取配置对象
                    dynamic flatSheetConfig = config;
                    result = await _impositionService.CalculateOptimalEvenColumnsLayoutAsync(flatSheetConfig, pdfInfo);
                }
                else
                {
                    dynamic rollMaterialConfig = config;
                    result = await _impositionService.CalculateOptimalEvenColumnsLayoutAsync(rollMaterialConfig, pdfInfo);
                }

                if (result != null)
                {
                    // 保存计算结果
                    _currentImpositionResult = result;

                    // 更新布局计算结果缓存（根据用户选择的类型）
                    if (materialType == "FlatSheet")
                    {
                        LayoutResultsCache.UpdateFlatSheetLayoutResults(result.Rows, result.Columns);
                    }
                    else
                    {
                        LayoutResultsCache.UpdateRollMaterialLayoutResults(result.Rows, result.Columns);
                    }

                    // 更新显示
                    UpdateLayoutDisplay(result);
                    LogHelper.Info($"[一式两联] 布局计算完成: {result.Rows}x{result.Columns}, 利用率: {result.SpaceUtilization:F1}%, 列数为最优偶数");
                }
                else
                {
                    UpdateLayoutDisplay("计算失败");
                    LogHelper.Warn("[一式两联] 布局计算返回空结果");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[一式两联] 计算布局时发生错误: {ex.Message}", ex);
                if (InvokeRequired)
                {
                    Invoke((Action)(() =>
                    {
                        UpdateLayoutDisplay("计算错误");
                    }));
                }
                else
                {
                    UpdateLayoutDisplay("计算错误");
                }
            }
        }

        #endregion

        #region PDF 预览功能

        /// <summary>
        /// 初始化 PDF 预览控件
        /// </summary>
        private void InitializePdfPreview()
        {
            try
            {
                // ✅ 移除了动画定时器（笠折叠定改为立即显示/隐藏）
                LogHelper.Debug("[PDF 预览] 控件初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 初始化失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 折叠按钮点击事件 - 控制 PDF 预览可见性（立即显示/隐藏，无动画）
        /// </summary>
        private void PreviewCollapseButton_Click(object sender, EventArgs e)
        {
            try
            {
                _isPreviewExpanded = !_isPreviewExpanded;

                if (_isPreviewExpanded)
                {
                    // ✅ 展开预览（立即显示，无动画）
                    pdfPreviewPanel.Height = MAX_PREVIEW_HEIGHT;
                    this.ClientSize = new System.Drawing.Size(400, 886); // 恢复到设计器大小（包含文件名区域）
                    previewCollapseButton.Text = "▲"; // 上箭头

                    // ✅ 如果有待加载的PDF，现在加载它
                    if (!string.IsNullOrEmpty(_pendingPdfToLoad))
                    {
                        _ = TryLoadPendingPdf();
                        LogHelper.Debug("[PDF 预览] 展开时调用TryLoadPendingPdf");
                    }

                    // ✅ 展开后触发最优适应缩放（延迟执行确保控件尺寸稳定）
                    this.BeginInvoke(new Action(async () =>
                    {
                        await Task.Delay(100); // 等待布局完成
                        if ((PdfPreview?.PageCount ?? 0) > 0)
                        {
                            PdfPreview.GoToPage(1); // 导航到首页
                            PdfPreview?.ApplyBestFitZoomPublic();
                            LogHelper.Debug("[PDF 预览] 展开后应用最优适应缩放并导航到首页");
                        }
                    }));
                }
                else
                {
                    // ✅ 折叠预览（立即隐藏，无动画）
                    pdfPreviewPanel.Height = 0;
                    this.ClientSize = new System.Drawing.Size(400, 644); // 折叠到按钮位置（包含文件名区域+30px）
                    previewCollapseButton.Text = "▼"; // 下箭头
                }

                // 保存预览状态到设置
                SavePreviewStateToSettings(_isPreviewExpanded);

                // 同步窗口位置和预览状态到WindowPositionManager
                WindowPositionManager.SaveWindowPosition(this, _isPreviewExpanded);
                
                // 🔧 关键修复：直接提交预览状态更改，不依赖5秒自动保存
                AppSettings.CommitChanges();
                LogHelper.Debug("[PDF 预览] 已立即提交预览状态设置到文件");

                LogHelper.Debug($"[PDF 预览] 折叠变换: {(_isPreviewExpanded ? "展开" : "折叠")} (无动画)");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 折叠失败: {ex.Message}");
            }
        }



        /// <summary>
        /// PDF 预览控件页面加载完成事件
        /// 更新页码显示并应用适应宽度的缩放
        /// </summary>
        private void PdfPreviewControl_PageLoaded(object sender, Controls.PageLoadedEventArgs e)
        {
            try
            {
                // ✅ 应用默认的最优适应缩放
                PdfPreview?.ApplyBestFit();
                LogHelper.Debug($"[PDF 预览] 页面加载完成，应用默认最优适应缩放 (页码: {e.PageIndex + 1} / {e.PageCount})");
                
                // 🔧 新增：更新页码标签显示
                UpdatePdfPageInfo(e.PageIndex + 1, e.PageCount);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 页面加载事件异常: {ex.Message}");
            }
        }

        /// <summary>
        /// PDF 预览控件页面改变事件（翻页时触发）
        /// 实时更新页码显示
        /// </summary>
        private void PdfPreviewControl_PageChanged(object sender, EventArgs e)
        {
            try
            {
                var pdfControl = PdfPreview;
                if (pdfControl != null)
                {
                    int currentPage = pdfControl.CurrentPageIndex + 1; // 转换为从1开始
                    int totalPages = pdfControl.PageCount;
                    
                    LogHelper.Debug($"[PDF 预览] 页面改变: {currentPage} / {totalPages}");
                    
                    // 更新页码标签显示
                    UpdatePdfPageInfo(currentPage, totalPages);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 页面改变事件异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载 PDF 文件到预览控件
        /// </summary>
        public async Task LoadPdfPreviewAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    LogHelper.Warn($"[PDF 预览] PDF 文件不存在: {filePath}");
                    return;
                }

                // 检查是否是新文件
                if (_cachedPdfPath != filePath)
                {
                    _cachedPdfPath = filePath;
                    LogHelper.Debug($"[PDF 预览] 加载新文件: {filePath}");
                }
                else
                {
                    LogHelper.Debug($"[PDF 预览] 文件未改变, 无需重新加载");
                    return;
                }

                // 加载到预览控件
                bool success = await PdfPreview.LoadPdfAsync(filePath);
                if (success && !_isPreviewExpanded)
                {
                    // 加载成功后，好为用户自动展开预览
                    // PreviewCollapseButton_Click(null, null);
                    LogHelper.Debug("[PDF 预览] 控件加载成功");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 加载失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存 PDF 预览状态到设置
        /// </summary>
        /// <param name="isExpanded">是否展开</param>
        private void SavePreviewStateToSettings(bool isExpanded)
        {
            try
            {
                AppSettings.MaterialFormPreviewExpanded = isExpanded; // 🔧 修复：使用正确的属性
                AppSettings.CommitChanges(); // 使用CommitChanges确保立即保存
                LogHelper.Debug($"[PDF 预览] 状态存储到MaterialFormPreviewExpanded: {isExpanded}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 保存状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试加载待处理的PDF文件
        /// 统一的PDF加载检查方法，确保PDF能够自动显示
        /// </summary>
        private async Task TryLoadPendingPdf()
        {
            try
            {
                if (_isPreviewExpanded && !string.IsNullOrEmpty(_pendingPdfToLoad))
                {
                    LogHelper.Debug($"[PDF 预览] 尝试加载待处理的PDF: {_pendingPdfToLoad}");

                    // 确保文件仍然存在
                    if (File.Exists(_pendingPdfToLoad))
                    {
                        await LoadPdfPreviewAsync(_pendingPdfToLoad);
                        _pendingPdfToLoad = null;
                        LogHelper.Debug("[PDF 预览] PDF加载成功");
                    }
                    else
                    {
                        LogHelper.Warn($"[PDF 预览] PDF文件不存在: {_pendingPdfToLoad}");
                        _pendingPdfToLoad = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 加载PDF失败: {ex.Message}", ex);
                // 清除待加载的PDF，避免重复尝试
                _pendingPdfToLoad = null;
            }
        }

        /// <summary>
        /// 从设置中加载 PDF 预览状态
        /// </summary>
        private void LoadPreviewStateFromSettings()
        {
            try
            {
                // 🔧 统一使用WindowPositionManager的数据源，避免重复逻辑
                bool shouldExpand = WindowPositionManager.ShouldExpandPreview();
                
                // 🔧 关键修复：直接设置预览状态，避免调用PreviewCollapseButton_Click干扰WindowPositionManager
                if (shouldExpand && !_isPreviewExpanded)
                {
                    // 直接设置展开状态，避免调用PreviewCollapseButton_Click
                    _isPreviewExpanded = true;
                    pdfPreviewPanel.Height = MAX_PREVIEW_HEIGHT;
                    this.ClientSize = new Size(400, 886);  // 🔧 添加：设置展开状态窗体大小
                    previewCollapseButton.Text = "▲";

                    // 展开后触发最优适应缩放
                    if ((PdfPreview?.PageCount ?? 0) > 0)
                    {
                        PdfPreview?.ApplyBestFit();
                        LogHelper.Debug("[PDF 预览] 从设置恢复展开状态，应用最优适应缩放");
                    }

                    LogHelper.Debug("[PDF 预览] 从设置恢复展开状态（直接设置，避免位置干扰）");
                }
                else if (!shouldExpand && _isPreviewExpanded)
                {
                    // 直接设置折叠状态，避免调用PreviewCollapseButton_Click
                    _isPreviewExpanded = false;
                    pdfPreviewPanel.Height = 0;
                    this.ClientSize = new Size(400, 644);  // 🔧 添加：设置折叠状态窗体大小（包含文件名区域）
                    previewCollapseButton.Text = "▼";

                    LogHelper.Debug("[PDF 预览] 从设置恢复折叠状态（直接设置，避免位置干扰）");
                }
                else
                {
                    LogHelper.Debug("[PDF 预览] 按钮的状态与想要状态一致");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 加载状态失败: {ex.Message}");
            }
        }

        #endregion

        #region PDF预览控件延迟初始化

        /// <summary>
        /// 检测是否处于设计模式
        /// </summary>
        private bool IsDesignMode()
        {
            return DesignMode ||
                   LicenseManager.UsageMode == LicenseUsageMode.Designtime ||
                   System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv";
        }

        /// <summary>
        /// 初始化PDF预览控件（延迟初始化）
        /// </summary>
        private void InitializePdfPreviewControl()
        {
            if (_pdfControlInitialized)
            {
                LogHelper.Debug("[PDF 预览] 控件已初始化，跳过重复初始化");
                return;
            }

            if (IsDesignMode())
            {
                LogHelper.Debug("[PDF 预览] 设计模式下，保持占位符");
                return;
            }

            try
            {
                LogHelper.Debug("[PDF 预览] 开始初始化真实PDF预览控件");

                // 运行时创建真实的PDF预览控件
                _realPdfPreviewControl = new WindowsFormsApp3.Controls.PdfPreviewControl();
                _realPdfPreviewControl.Dock = DockStyle.Fill;
                _realPdfPreviewControl.Name = "realPdfPreviewControl";

                // 设置事件绑定
                _realPdfPreviewControl.PageLoaded += RealPdfPreviewControl_PageLoaded;
                _realPdfPreviewControl.LoadError += RealPdfPreviewControl_LoadError;

                // 替换占位符
                if (pdfPreviewControl != null && pdfPreviewPanel != null)
                {
                    pdfPreviewPanel.Controls.Remove(pdfPreviewControl);
                    pdfPreviewPanel.Controls.Add(_realPdfPreviewControl);

                    LogHelper.Debug("[PDF 预览] 真实PDF控件已添加到预览面板");
                }

                _pdfControlInitialized = true;
                LogHelper.Info("[PDF 预览] PDF预览控件初始化完成");

                // 🔧 应用当前主题到新创建的PDF控件
                try
                {
                    var themeManager = ServiceLocator.Instance.GetThemeManager();
                    var theme = themeManager.GetThemeByName(AppSettings.CurrentThemeName);
                    if (theme != null)
                    {
                        bool isDark = IsThemeDark(theme);
                        _realPdfPreviewControl.BackColor = theme.SurfaceLight;
                        _realPdfPreviewControl.SetPreviewBackgroundColor(theme.SurfaceLight); // 🔧 设置预览背景色
                        _realPdfPreviewControl.SetScrollBarTheme(isDark);
                        LogHelper.Debug($"[PDF 预览] 已应用主题: {theme.Name}, isDark={isDark}");
                    }
                }
                catch (Exception themeEx)
                {
                    LogHelper.Warn($"[PDF 预览] 应用主题失败: {themeEx.Message}");
                }

                // 如果有待加载的PDF，现在加载它
                if (!string.IsNullOrEmpty(_pendingPdfToLoad))
                {
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(100); // 短暂等待确保UI完全准备好
                        await LoadPdfPreviewAsync(_pendingPdfToLoad);
                        _pendingPdfToLoad = null;
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 初始化失败: {ex.Message}", ex);

                // 创建错误占位符
                CreateErrorPlaceholder(ex.Message);
            }
        }

        /// <summary>
        /// 创建错误占位符
        /// </summary>
        private void CreateErrorPlaceholder(string errorMessage)
        {
            try
            {
                if (pdfPreviewPanel != null && pdfPreviewPanel.InvokeRequired)
                {
                    pdfPreviewPanel.Invoke(new Action(() => CreateErrorPlaceholder(errorMessage)));
                    return;
                }

                var errorLabel = new System.Windows.Forms.Label
                {
                    Text = $"PDF预览组件初始化失败\n{errorMessage}",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.Red,
                    BackColor = Color.LightGray,
                    Font = new System.Drawing.Font("Microsoft YaHei", 9f)
                };

                if (pdfPreviewPanel != null)
                {
                    pdfPreviewPanel.Controls.Clear();
                    pdfPreviewPanel.Controls.Add(errorLabel);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 创建错误占位符失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取真实的PDF预览控件（兼容现有代码）
        /// </summary>
        private WindowsFormsApp3.Controls.PdfPreviewControl GetRealPdfPreviewControl()
        {
            if (!_pdfControlInitialized && !IsDesignMode())
            {
                InitializePdfPreviewControl();
            }
            return _realPdfPreviewControl;
        }

        /// <summary>
        /// PDF预览控件的兼容属性（自动延迟初始化）
        /// </summary>
        private WindowsFormsApp3.Controls.PdfPreviewControl PdfPreview
        {
            get
            {
                var realControl = GetRealPdfPreviewControl();
                if (realControl == null && !IsDesignMode())
                {
                    // 如果真实控件为空且不是设计模式，创建一个默认的
                    LogHelper.Warn("[PDF 预览] 真实PDF控件为空，返回默认控件");
                    realControl = new WindowsFormsApp3.Controls.PdfPreviewControl();
                }
                return realControl;
            }
        }

        /// <summary>
        /// 真实PDF控件的页面加载事件
        /// </summary>
        private void RealPdfPreviewControl_PageLoaded(object sender, EventArgs e)
        {
            try
            {
                LogHelper.Debug("[PDF 预览] 真实PDF控件页面加载完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 处理页面加载事件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 真实PDF控件的加载错误事件
        /// </summary>
        private void RealPdfPreviewControl_LoadError(object sender, EventArgs e)
        {
            try
            {
                LogHelper.Warn("[PDF 预览] 真实PDF控件加载出错");
                // 可以在这里显示用户友好的错误信息
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 预览] 处理加载错误事件失败: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 更新 PDF 页码显示
        /// </summary>
        /// <param name="currentPage">当前页码（从1开始）</param>
        /// <param name="totalPages">总页数</param>
        private void UpdatePdfPageInfo(int currentPage, int totalPages)
        {
            try
            {
                string displayText;
                if (totalPages > 0)
                {
                    displayText = $"页码: 第{currentPage}页 / 共{totalPages}页";
                    LogHelper.Debug($"[PDF 页码] 更新显示: {displayText}");
                }
                else
                {
                    displayText = "页码: —";
                }

                // 更新页码标签
                if (label1 != null)
                {
                    label1.Text = displayText;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PDF 页码] 更新页码显示失败: {ex.Message}");
            }
        }

        private void orderNumberLabel_Click(object sender, EventArgs e)
        {

        }

        private void orderNumberTextBox_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        #region 主题支持

        /// <summary>
        /// 应用主题到窗体及所有控件
        /// </summary>
        /// <param name="theme">主题定义</param>
        public void ApplyTheme(ThemeDefinition theme)
        {
            if (theme == null) return;

            try
            {
                LogHelper.Debug($"[MaterialSelectFormModern] 开始应用主题: {theme.Name}");

                // 判断是否为深色主题
                bool isDark = IsThemeDark(theme);

                // 1. 应用窗体背景
                this.BackColor = theme.Background;

                // 2. 应用分隔线
                if (fileNameSeparator != null)
                    fileNameSeparator.BackColor = theme.Border;

                // 3. 应用标签页控件
                ApplyThemeToTabs(theme, isDark);

                // 4. 应用所有 AntdUI 按钮
                ApplyThemeToButtons(theme, isDark);

                // 5. 应用所有 AntdUI 输入框
                ApplyThemeToInputs(theme);

                // 6. 应用所有 AntdUI 下拉框
                ApplyThemeToSelects(theme, isDark);

                // 7. 应用所有 AntdUI 标签
                ApplyThemeToLabels(theme);

                // 8. 应用所有 AntdUI 复选框和单选框
                ApplyThemeToCheckboxesAndRadios(theme);

                // 9. 应用 GroupBox
                ApplyThemeToGroupBoxes(theme);

                // 10. 应用 TreeView
                ApplyThemeToTreeView(theme, isDark);

                // 11. 应用 PDF 预览面板
                ApplyThemeToPdfPreview(theme, isDark);

                // 12. 应用滚动条主题
                ThemeHelper.ApplyScrollBarThemeRecursive(this, isDark);

                LogHelper.Debug($"[MaterialSelectFormModern] 主题应用完成: {theme.Name}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 应用主题失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 判断主题是否为深色主题
        /// </summary>
        private bool IsThemeDark(ThemeDefinition theme)
        {
            // 通过背景色亮度判断
            var brightness = (theme.Background.R * 299 + theme.Background.G * 587 + theme.Background.B * 114) / 1000;
            return brightness < 128;
        }

        /// <summary>
        /// 应用主题到标签页控件
        /// </summary>
        private void ApplyThemeToTabs(ThemeDefinition theme, bool isDark)
        {
            if (tabs1 != null)
            {
                tabs1.BackColor = theme.Background;
                tabs1.ForeColor = theme.TextPrimary;

                // 设置标签页
                foreach (Control tabPage in tabs1.Controls)
                {
                    if (tabPage is AntdUI.TabPage antdTabPage)
                    {
                        antdTabPage.BackColor = theme.Background;
                        antdTabPage.ForeColor = theme.TextPrimary;
                    }
                }
            }
        }

        /// <summary>
        /// 应用主题到所有按钮
        /// </summary>
        private void ApplyThemeToButtons(ThemeDefinition theme, bool isDark)
        {
            // 材料按钮列表
            var materialButtons = new[] {
                materialButton1, materialButton2, materialButton3, materialButton4, materialButton5,
                materialButton6, materialButton7, materialButton8, materialButton9, materialButton10,
                materialButton11, materialButton12, materialButton13, materialButton14, materialButton15
            };

            foreach (var btn in materialButtons)
            {
                if (btn != null)
                {
                    ApplyThemeToMaterialButton(btn, theme, isDark);
                }
            }

            // 膜类型按钮
            var filmButtons = new[] { filmTypeLightButton, filmTypeMatteButton, filmTypeRedButton, filmTypeNoneButton };
            foreach (var btn in filmButtons)
            {
                if (btn != null)
                {
                    ApplyThemeToMaterialButton(btn, theme, isDark);
                }
            }

            // 形状按钮
            var shapeButtons = new[] { shapeRightAngleButton, shapeCircleButton, shapeSpecialButton, shapeRoundRectButton };
            foreach (var btn in shapeButtons)
            {
                if (btn != null)
                {
                    ApplyThemeToMaterialButton(btn, theme, isDark);
                }
            }

            // 颜色模式按钮 - 需要特殊处理以保持状态指示边框颜色
            if (colorModeButton != null)
            {
                ApplyThemeToMaterialButton(colorModeButton, theme, isDark);
                // 重新应用特定的边框颜色(红色/绿色)，覆盖主题边框色
                SetColorModeButtonWithIcon(ColorMode);
            }

            // 确认/取消按钮 - 保持原有样式（Primary/Default）
            if (confirmButton != null)
            {
                // Primary 按钮保持主题色
                confirmButton.BackColor = theme.Primary;
                confirmButton.DefaultBack = theme.Primary; // 🔧 显式设置默认背景色，确保非Primary类型也能生效
                confirmButton.ForeColor = Color.White;
            }

            if (cancelButton != null)
            {
                cancelButton.DefaultBack = theme.Surface;
                cancelButton.ForeColor = theme.TextPrimary;
                cancelButton.DefaultBorderColor = theme.Border;
            }

            // 预览折叠按钮
            if (previewCollapseButton != null)
            {
                previewCollapseButton.DefaultBack = theme.Surface;
                previewCollapseButton.ForeColor = theme.TextPrimary;
                previewCollapseButton.DefaultBorderColor = theme.Border;
            }
        }

        /// <summary>
        /// 应用主题到材料/选项按钮（非选中状态）
        /// </summary>
        private void ApplyThemeToMaterialButton(AntdUI.Button btn, ThemeDefinition theme, bool isDark)
        {
            // 检查按钮是否处于选中状态（通过 Type 判断，而非 BorderWidth，因为 Designer 可能默认设置了 BorderWidth）
            bool isSelected = btn.Type == AntdUI.TTypeMini.Primary;

            if (isSelected)
            {
                // 选中状态：保持边框高亮，但调整背景
                btn.DefaultBack = theme.BackActive;
                btn.ForeColor = theme.Primary;
            }
            else
            {
                // 非选中状态
                btn.DefaultBack = theme.Surface;
                btn.ForeColor = theme.TextPrimary;
                btn.DefaultBorderColor = theme.Border;
            }
        }

        /// <summary>
        /// 应用主题到所有输入框
        /// </summary>
        private void ApplyThemeToInputs(ThemeDefinition theme)
        {
            var inputs = new[] {
                orderNumberTextBox, quantityTextBox, incrementTextBox,
                serialNumberTextBox, dimensionsTextBox, radiusTextBox
            };

            foreach (var input in inputs)
            {
                if (input != null)
                {
                    input.BackColor = theme.SurfaceLight;
                    input.ForeColor = theme.TextPrimary;
                    input.BorderColor = theme.Border;
                }
            }
        }

        /// <summary>
        /// 应用主题到所有下拉框
        /// </summary>
        private void ApplyThemeToSelects(ThemeDefinition theme, bool isDark)
        {
            var selects = new AntdUI.Select[] { bleedDropdown, dropdown16 };

            foreach (var select in selects)
            {
                if (select != null)
                {
                    select.BackColor = theme.SurfaceLight;
                    select.ForeColor = theme.TextPrimary;
                    select.BorderColor = theme.Border;
                    // 设置下拉列表的主题模式
                    select.ColorScheme = isDark ? AntdUI.TAMode.Dark : AntdUI.TAMode.Light;
                }
            }
        }

        /// <summary>
        /// 应用主题到所有标签
        /// </summary>
        private void ApplyThemeToLabels(ThemeDefinition theme)
        {
            // 普通标签：使用主文本色
            var labels = new AntdUI.Label[] {
                fileNameLabel, orderNumberLabel, quantityLabel, serialNumberLabel,
                dimensionsLabel, pdfSizeDisplayLabel, label1
            };

            foreach (var label in labels)
            {
                if (label != null)
                {
                    label.ForeColor = theme.TextPrimary;
                }
            }

            // 🔧 特殊处理：使用强调色组的四个标签
            // Rows (Accent 1) -> Primary (主色 - 蓝)
            if (rowsDisplayLabel != null)
            {
                rowsDisplayLabel.ForeColor = theme.Primary;
            }

            // Columns (Accent 2) -> Success (成功色 - 绿)
            if (columnsDisplayLabel != null)
            {
                columnsDisplayLabel.ForeColor = theme.Success;
            }

            // LayoutCount (Accent 3) -> Warning (警告色 - 橙)
            if (layoutCountDisplayLabel != null)
            {
                layoutCountDisplayLabel.ForeColor = theme.Warning;
            }

            // Rotation (Accent 4) -> Keep independent (Purple)
            if (rotationDisplayLabel != null)
            {
                rotationDisplayLabel.ForeColor = IsColorValid(theme.AccentColor4) ? theme.AccentColor4 : Color.MediumPurple;
            }
        }

        private bool IsColorValid(Color color)
        {
            return color != Color.Empty && color != Color.Transparent && !(color.R == 0 && color.G == 0 && color.B == 0 && color.A == 0);
        }

        /// <summary>
        /// 应用主题到复选框和单选框
        /// </summary>
        private void ApplyThemeToCheckboxesAndRadios(ThemeDefinition theme)
        {
            // 复选框
            var checkboxes = new AntdUI.Checkbox[] {
                autoIncrementCheckbox, chkIdentifierPage, enableImpositionCheckbox, duplicateLayoutCheckbox
            };

            foreach (var checkbox in checkboxes)
            {
                if (checkbox != null)
                {
                    checkbox.ForeColor = theme.TextPrimary;
                }
            }

            // 单选框
            var radios = new AntdUI.Radio[] {
                flatSheetRadioButton, rollMaterialRadioButton,
                continuousLayoutRadioButton, foldingLayoutRadioButton
            };

            foreach (var radio in radios)
            {
                if (radio != null)
                {
                    radio.ForeColor = theme.TextPrimary;
                }
            }
        }

        /// <summary>
        /// 应用主题到 GroupBox
        /// </summary>
        private void ApplyThemeToGroupBoxes(ThemeDefinition theme)
        {
            var groupBoxes = new[] { materialTypeGroupBox, layoutModeGroupBox };

            foreach (var groupBox in groupBoxes)
            {
                if (groupBox != null)
                {
                    groupBox.BackColor = theme.Background;
                    groupBox.ForeColor = theme.TextPrimary;
                }
            }
        }

        /// <summary>
        /// 应用主题到 TreeView
        /// </summary>
        private void ApplyThemeToTreeView(ThemeDefinition theme, bool isDark)
        {
            if (folderTreeView != null)
            {
                folderTreeView.BackColor = theme.Background;
                folderTreeView.ForeColor = theme.TextPrimary;
                folderTreeView.LineColor = theme.Border;
            }
        }

        /// <summary>
        /// 应用主题到 PDF 预览面板
        /// </summary>
        private void ApplyThemeToPdfPreview(ThemeDefinition theme, bool isDark)
        {
            // PDF 预览面板背景
            if (pdfPreviewPanel != null)
            {
                pdfPreviewPanel.BackColor = theme.Surface;
            }

            // PDF 预览控件占位符
            if (pdfPreviewControl != null)
            {
                pdfPreviewControl.BackColor = theme.SurfaceLight;
            }

            // 真实的 PDF 预览控件
            if (_realPdfPreviewControl != null)
            {
                _realPdfPreviewControl.BackColor = theme.SurfaceLight;
                _realPdfPreviewControl.SetPreviewBackgroundColor(theme.SurfaceLight); // 🔧 设置预览背景色
                _realPdfPreviewControl.SetScrollBarTheme(isDark);
            }
        }

        /// <summary>
        /// 使用当前主题设置应用主题
        /// </summary>
        public void ApplyCurrentTheme()
        {
            try
            {
                var themeManager = ServiceLocator.Instance.GetThemeManager();
                var themeName = AppSettings.CurrentThemeName;
                var theme = themeManager.GetThemeByName(themeName);

                if (theme != null)
                {
                    ApplyTheme(theme);
                }
                else
                {
                    LogHelper.Warn($"[MaterialSelectFormModern] 未找到主题: {themeName}，使用默认主题");
                    // 使用默认浅色主题
                    var defaultTheme = themeManager.GetThemeByName("浅色");
                    if (defaultTheme != null)
                    {
                        ApplyTheme(defaultTheme);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[MaterialSelectFormModern] 应用当前主题失败: {ex.Message}", ex);
            }
        }

        #endregion
    }
}