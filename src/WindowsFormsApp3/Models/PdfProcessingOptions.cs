using System;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// PDF处理选项数据传输对象
    /// 用于封装PDF文件处理的各种配置选项
    /// </summary>
    public class PdfProcessingOptions
    {
        /// <summary>
        /// 是否为PDF文件
        /// </summary>
        public bool IsPdfFile { get; set; }

        /// <summary>
        /// 是否添加PDF图层
        /// </summary>
        public bool AddPdfLayers { get; set; }

        /// <summary>
        /// 是否使用PDF最后一页（兼容旧版本）
        /// </summary>
        [Obsolete("请使用ShapeType枚举判断，ShapeType.Special表示异形")]
        public bool UsePdfLastPage
        {
            get => ShapeType == global::WindowsFormsApp3.ShapeType.Special;
            set
            {
                if (value)
                {
                    ShapeType = global::WindowsFormsApp3.ShapeType.Special;
                }
                // 当设置为false时，不改变ShapeType，保持原有状态
            }
        }

        /// <summary>
        /// 形状类型（新的枚举方式）
        /// </summary>
        public ShapeType ShapeType { get; set; } = ShapeType.RightAngle;

        /// <summary>
        /// 圆角半径（仅用于圆角矩形）
        /// </summary>
        public double RoundRadius { get; set; } = 0;

        /// <summary>
        /// 圆角半径（兼容旧版本）
        /// </summary>
        [Obsolete("请使用ShapeType和RoundRadius属性")]
        public string CornerRadius
        {
            get => GetCompatibleCornerRadius();
            set => SetFromLegacyCornerRadius(value);
        }

        /// <summary>
        /// 根据新的形状属性生成兼容的CornerRadius字符串
        /// </summary>
        /// <returns>兼容旧版本的CornerRadius值</returns>
        private string GetCompatibleCornerRadius()
        {
            switch (ShapeType)
            {
                case global::WindowsFormsApp3.ShapeType.Circle:
                    return "R"; // 旧版本用"R"表示圆形
                case global::WindowsFormsApp3.ShapeType.Special:
                    return "Y"; // 旧版本用"Y"表示异形
                case global::WindowsFormsApp3.ShapeType.RoundRect:
                    return RoundRadius.ToString(); // 圆角矩形用数字
                case global::WindowsFormsApp3.ShapeType.RightAngle:
                default:
                    return "0"; // 直角用"0"
            }
        }

        /// <summary>
        /// 从旧版本的CornerRadius字符串设置新的形状属性
        /// </summary>
        /// <param name="legacyCornerRadius">旧版本的CornerRadius值</param>
        private void SetFromLegacyCornerRadius(string legacyCornerRadius)
        {
            if (string.IsNullOrEmpty(legacyCornerRadius))
            {
                ShapeType = global::WindowsFormsApp3.ShapeType.RightAngle;
                RoundRadius = 0;
                return;
            }

            switch (legacyCornerRadius.ToUpper())
            {
                case "R":
                    ShapeType = global::WindowsFormsApp3.ShapeType.Circle;
                    RoundRadius = 0;
                    break;
                case "Y":
                    ShapeType = global::WindowsFormsApp3.ShapeType.Special;
                    RoundRadius = 0;
                    break;
                default:
                    // 尝试解析为数字，作为圆角半径
                    if (double.TryParse(legacyCornerRadius, out double radius) && radius > 0)
                    {
                        ShapeType = global::WindowsFormsApp3.ShapeType.RoundRect;
                        RoundRadius = radius;
                    }
                    else
                    {
                        ShapeType = global::WindowsFormsApp3.ShapeType.RightAngle;
                        RoundRadius = 0;
                    }
                    break;
            }
        }

        /// <summary>
        /// 出血值
        /// </summary>
        public double TetBleed { get; set; }

        /// <summary>
        /// PDF原始宽度
        /// </summary>
        public double PdfWidth { get; set; }

        /// <summary>
        /// PDF原始高度
        /// </summary>
        public double PdfHeight { get; set; }

        /// <summary>
        /// 最终尺寸字符串（包含形状信息）
        /// </summary>
        public string FinalDimensions { get; set; }

        /// <summary>
        /// 缓存文件夹路径
        /// </summary>
        public string CacheFolder { get; set; }

        /// <summary>
        /// 临时文件路径
        /// </summary>
        public string TempFilePath { get; set; }

        /// <summary>
        /// 是否需要图层检查
        /// </summary>
        public bool RequireLayerCheck { get; set; }

        /// <summary>
        /// 目标图层名称列表
        /// </summary>
        public string[] TargetLayerNames { get; set; }

        /// <summary>
        /// 是否添加标识页
        /// </summary>
        public bool AddIdentifierPage { get; set; }

        /// <summary>
        /// 标识页文字内容
        /// </summary>
        public string IdentifierPageContent { get; set; }

        /// <summary>
        /// 排版模式（连拼/折手）
        /// </summary>
        public LayoutMode LayoutMode { get; set; }

        /// <summary>
        /// 排版数量（每纸页数）
        /// </summary>
        public int LayoutQuantity { get; set; }

        /// <summary>
        /// 页面旋转角度（布局计算结果，0°或270°）
        /// </summary>
        public int RotationAngle { get; set; }

        /// <summary>
        /// 联数（一式几联，0=不使用）
        /// </summary>
        public int CopyCount { get; set; }

        /// <summary>
        /// 一式类型（联/份）
        /// </summary>
        public CopyType CopyType { get; set; }

        /// <summary>
        /// 份数（一式几份，仅当CopyType为Duplicate时有效）
        /// </summary>
        public int DuplicateCount { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PdfProcessingOptions()
        {
            IsPdfFile = false;
            AddPdfLayers = true;
            ShapeType = global::WindowsFormsApp3.ShapeType.RightAngle;
            RoundRadius = 0;
            TetBleed = 0.0;
            PdfWidth = 0.0;
            PdfHeight = 0.0;
            FinalDimensions = string.Empty;
            CacheFolder = string.Empty;
            TempFilePath = string.Empty;
            RequireLayerCheck = true;
            TargetLayerNames = new[] { "Dots_AddCounter", "Dots_L_B_出血线" };
            AddIdentifierPage = false;
            IdentifierPageContent = string.Empty;
            LayoutMode = LayoutMode.Continuous;
            LayoutQuantity = 0;
            RotationAngle = 0;
            CopyCount = 0;
            CopyType = CopyType.Layout;
            DuplicateCount = 2;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public PdfProcessingOptions(bool isPdfFile, bool addPdfLayers, bool usePdfLastPage,
            string cornerRadius, double tetBleed)
        {
            IsPdfFile = isPdfFile;
            AddPdfLayers = addPdfLayers;

            // 转换旧参数为新属性
            if (usePdfLastPage)
            {
                ShapeType = global::WindowsFormsApp3.ShapeType.Special;
            }
            else
            {
                SetFromLegacyCornerRadius(cornerRadius ?? "0");
            }

            TetBleed = tetBleed;
            PdfWidth = 0.0;
            PdfHeight = 0.0;
            FinalDimensions = string.Empty;
            CacheFolder = string.Empty;
            TempFilePath = string.Empty;
            RequireLayerCheck = true;
            TargetLayerNames = new[] { "Dots_AddCounter", "Dots_L_B_出血线" };
            AddIdentifierPage = false;
            IdentifierPageContent = string.Empty;
            LayoutMode = LayoutMode.Continuous;
            LayoutQuantity = 0;
        }

        /// <summary>
        /// 验证PDF处理选项
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            if (TetBleed < 0)
            {
                return ValidationResult.Failure("出血值不能为负数", ValidationErrorType.InvalidParameters);
            }

            if (IsPdfFile && AddPdfLayers)
            {
                if (string.IsNullOrEmpty(CacheFolder))
                {
                    return ValidationResult.Failure("PDF处理需要指定缓存文件夹", ValidationErrorType.InvalidParameters);
                }

                if (string.IsNullOrEmpty(TempFilePath))
                {
                    return ValidationResult.Failure("PDF处理需要指定临时文件路径", ValidationErrorType.InvalidParameters);
                }
            }

            if (PdfWidth < 0 || PdfHeight < 0)
            {
                return ValidationResult.Failure("PDF尺寸不能为负数", ValidationErrorType.InvalidParameters);
            }

            return ValidationResult.Success("PDF处理选项验证通过");
        }

        /// <summary>
        /// 检查是否需要PDF处理
        /// </summary>
        /// <returns>是否需要处理</returns>
        public bool RequiresPdfProcessing()
        {
            return IsPdfFile && AddPdfLayers;
        }

        /// <summary>
        /// 生成缓存文件夹路径
        /// </summary>
        /// <returns>缓存文件夹路径</returns>
        public string GenerateCacheFolder()
        {
            if (string.IsNullOrEmpty(CacheFolder))
            {
                CacheFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PDFToolCache");
            }
            return CacheFolder;
        }

        /// <summary>
        /// 生成临时文件路径
        /// </summary>
        /// <param name="cacheFolder">缓存文件夹</param>
        /// <returns>临时文件路径</returns>
        public string GenerateTempFilePath(string cacheFolder = null)
        {
            if (string.IsNullOrEmpty(cacheFolder))
            {
                cacheFolder = GenerateCacheFolder();
            }

            if (string.IsNullOrEmpty(TempFilePath))
            {
                TempFilePath = System.IO.Path.Combine(cacheFolder, System.IO.Path.GetRandomFileName() + ".pdf");
            }
            return TempFilePath;
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息字符串</returns>
        public string GetDebugInfo()
        {
            return $"PdfOptions: IsPdf={IsPdfFile}, AddLayers={AddPdfLayers}, ShapeType={ShapeType}, " +
                   $"RoundRadius={RoundRadius}, Bleed={TetBleed}, Size={PdfWidth}x{PdfHeight}, " +
                   $"Dimensions='{FinalDimensions}', Cache='{CacheFolder}', Temp='{TempFilePath}'";
        }
    }
}