using System;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// 印刷排版功能相关数据模型
    /// </summary>
    #region 枚举定义

    /// <summary>
    /// 材料类型枚举
    /// </summary>
    public enum MaterialType
    {
        /// <summary>
        /// 平张材料
        /// </summary>
        FlatSheet = 0,

        /// <summary>
        /// 卷装材料
        /// </summary>
        RollMaterial = 1
    }

    /// <summary>
    /// 排版模式枚举
    /// </summary>
    public enum LayoutMode
    {
        /// <summary>
        /// 连拼模式 - 不需要添加空白页，直接按布局数量拼接
        /// </summary>
        Continuous = 0,

        /// <summary>
        /// 折手模式 - 需要添加空白页，确保总页数为布局数量的倍数
        /// </summary>
        Folding = 1
    }

    /// <summary>
    /// 布局计算模式枚举
    /// </summary>
    public enum LayoutCalculationMode
    {
        /// <summary>
        /// 完全自动计算 - 行数和列数都自动计算
        /// </summary>
        FullyAuto = 0,

        /// <summary>
        /// 固定行数 - 行数指定，列数自动计算
        /// </summary>
        FixedRows = 1,

        /// <summary>
        /// 固定列数 - 列数指定，行数自动计算
        /// </summary>
        FixedColumns = 2,

        /// <summary>
        /// 手动指定 - 行数和列数都手动指定
        /// </summary>
        Manual = 3
    }

    /// <summary>
    /// 卷装旋转模式枚举
    /// </summary>
    public enum RollRotationMode
    {
        /// <summary>
        /// 自动（默认）
        /// </summary>
        Auto = 0,

        /// <summary>
        /// 强制0度（不旋转）
        /// </summary>
        Force0Degree = 1,

        /// <summary>
        /// 强制270度（旋转90度）
        /// </summary>
        Force270Degree = 2
    }

    #endregion

    #region 配置模型

    /// <summary>
    /// 平张模式配置
    /// </summary>
    public class FlatSheetConfiguration
    {
        /// <summary>
        /// 配置名称
        /// </summary>
        public string Name { get; set; } = "标准平张配置";

        /// <summary>
        /// 纸张宽度（毫米）
        /// </summary>
        public float PaperWidth { get; set; } = 210f;

        /// <summary>
        /// 纸张高度（毫米）
        /// </summary>
        public float PaperHeight { get; set; } = 297f;

        /// <summary>
        /// 可印刷宽度（毫米）= 纸张宽度 - 左右边距
        /// </summary>
        public float PrintableWidth => PaperWidth - MarginLeft - MarginRight;

        /// <summary>
        /// 可印刷高度（毫米）= 纸张高度 - 上下边距
        /// </summary>
        public float PrintableHeight => PaperHeight - MarginTop - MarginBottom;

        // 边距设置
        /// <summary>
        /// 上边距（毫米）
        /// </summary>
        public float MarginTop { get; set; } = 10f;

        /// <summary>
        /// 下边距（毫米）
        /// </summary>
        public float MarginBottom { get; set; } = 10f;

        /// <summary>
        /// 左边距（毫米）
        /// </summary>
        public float MarginLeft { get; set; } = 10f;

        /// <summary>
        /// 右边距（毫米）
        /// </summary>
        public float MarginRight { get; set; } = 10f;

        // 布局设置
        /// <summary>
        /// 行数设置（0=自动计算，或1-8行）
        /// </summary>
        public int Rows { get; set; } = 0;

        /// <summary>
        /// 列数设置（0=自动计算，或1-8列）
        /// </summary>
        public int Columns { get; set; } = 0;

        /// <summary>
        /// 布局计算模式
        /// </summary>
        public LayoutCalculationMode CalculationMode
        {
            get
            {
                if (Rows > 0 && Columns > 0)
                    return LayoutCalculationMode.Manual;
                if (Rows > 0)
                    return LayoutCalculationMode.FixedRows;
                if (Columns > 0)
                    return LayoutCalculationMode.FixedColumns;
                return LayoutCalculationMode.FullyAuto;
            }
        }

        /// <summary>
        /// 验证配置参数的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ImpositionValidationResult Validate()
        {
            var result = new ImpositionValidationResult { IsValid = true };

            if (PaperWidth <= 0 || PaperHeight <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "纸张尺寸必须大于0";
                return result;
            }

            if (MarginTop < 0 || MarginBottom < 0 || MarginLeft < 0 || MarginRight < 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "边距不能为负数";
                return result;
            }

            if (MarginTop + MarginBottom >= PaperHeight || MarginLeft + MarginRight >= PaperWidth)
            {
                result.IsValid = false;
                result.ErrorMessage = "边距总和不能大于或等于纸张尺寸";
                return result;
            }

            if ((Rows > 0 && Rows > 8) || (Columns > 0 && Columns > 8))
            {
                result.IsValid = false;
                result.ErrorMessage = "行数和列数不能超过8";
                return result;
            }

            return result;
        }
    }

    /// <summary>
    /// 卷装模式配置
    /// </summary>
    public class RollMaterialConfiguration
    {
        /// <summary>
        /// 配置名称
        /// </summary>
        public string Name { get; set; } = "标准卷装配置";

        /// <summary>
        /// 固定宽度（卷装材料宽度，毫米）
        /// </summary>
        public float FixedWidth { get; set; } = 210f;

        /// <summary>
        /// 最小长度（材料最小使用长度，毫米）
        /// </summary>
        public float MinLength { get; set; } = 297f;

        /// <summary>
        /// 可用宽度（毫米）= 固定宽度 - 左右边距
        /// </summary>
        public float UsableWidth => FixedWidth - MarginLeft - MarginRight;

        // 边距设置
        /// <summary>
        /// 上边距（毫米）
        /// </summary>
        public float MarginTop { get; set; } = 10f;

        /// <summary>
        /// 下边距（毫米）
        /// </summary>
        public float MarginBottom { get; set; } = 10f;

        /// <summary>
        /// 左边距（毫米）
        /// </summary>
        public float MarginLeft { get; set; } = 10f;

        /// <summary>
        /// 右边距（毫米）
        /// </summary>
        public float MarginRight { get; set; } = 10f;

        // 布局设置
        /// <summary>
        /// 行数设置（0=自动计算，或1-8行）
        /// </summary>
        public int Rows { get; set; } = 0;

        /// <summary>
        /// 列数设置（0=自动计算，或1-8列）
        /// </summary>
        public int Columns { get; set; } = 0;

        /// <summary>
        /// 布局计算模式
        /// </summary>
        public LayoutCalculationMode CalculationMode
        {
            get
            {
                if (Rows > 0 && Columns > 0)
                    return LayoutCalculationMode.Manual;
                if (Rows > 0)
                    return LayoutCalculationMode.FixedRows;
                if (Columns > 0)
                    return LayoutCalculationMode.FixedColumns;
                return LayoutCalculationMode.FullyAuto;
            }
        }

        /// <summary>
        /// 验证配置参数的有效性
        /// </summary>
        /// <returns>验证结果</returns>
        public ImpositionValidationResult Validate()
        {
            var result = new ImpositionValidationResult { IsValid = true };

            if (FixedWidth <= 0 || MinLength <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "材料尺寸必须大于0";
                return result;
            }

            if (MarginTop < 0 || MarginBottom < 0 || MarginLeft < 0 || MarginRight < 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "边距不能为负数";
                return result;
            }

            if (MarginLeft + MarginRight >= FixedWidth)
            {
                result.IsValid = false;
                result.ErrorMessage = "左右边距总和不能大于或等于固定宽度";
                return result;
            }

            if ((Rows > 0 && Rows > 8) || (Columns > 0 && Columns > 8))
            {
                result.IsValid = false;
                result.ErrorMessage = "行数和列数不能超过8";
                return result;
            }

            return result;
        }
    }

    #endregion

    #region PDF文件信息模型（扩展现有PdfFileInfo）

    /// <summary>
    /// 排版专用的PDF文件信息（扩展现有PdfFileInfo）
    /// </summary>
    public class ImpositionPdfInfo : PdfFileInfo
    {
        /// <summary>
        /// 内容区域宽度（毫米）- 基于TrimBox或CropBox
        /// </summary>
        public float CropBoxWidth { get; set; }

        /// <summary>
        /// 内容区域高度（毫米）- 基于TrimBox或CropBox
        /// </summary>
        public float CropBoxHeight { get; set; }

        /// <summary>
        /// 是否包含有效内容区域（非MediaBox）
        /// </summary>
        public bool HasCropBox { get; set; }

        /// <summary>
        /// PDF页面的原始旋转角度（0, 90, 180, 270）
        /// </summary>
        public int PageRotation { get; set; }

        /// <summary>
        /// 从现有的PdfFileInfo创建ImpositionPdfInfo
        /// </summary>
        /// <param name="baseInfo">现有的PDF文件信息</param>
        /// <param name="cropBoxWidth">内容区域宽度</param>
        /// <param name="cropBoxHeight">内容区域高度</param>
        /// <param name="hasCropBox">是否有有效内容区域</param>
        /// <param name="pageRotation">PDF页面的原始旋转角度</param>
        /// <returns>排版用PDF信息</returns>
        public static ImpositionPdfInfo FromPdfFileInfo(
            PdfFileInfo baseInfo,
            float cropBoxWidth,
            float cropBoxHeight,
            bool hasCropBox,
            int pageRotation = 0)
        {
            return new ImpositionPdfInfo
            {
                FilePath = baseInfo.FilePath,
                FileName = baseInfo.FileName,
                FileSize = baseInfo.FileSize,
                PageCount = baseInfo.PageCount,
                FirstPageSize = baseInfo.FirstPageSize,
                AllPageSizes = baseInfo.AllPageSizes,
                Errors = baseInfo.Errors,
                LastModified = baseInfo.LastModified,

                // 排版计算专用属性
                CropBoxWidth = cropBoxWidth,
                CropBoxHeight = cropBoxHeight,
                HasCropBox = hasCropBox,
                PageRotation = pageRotation
            };
        }

        /// <summary>
        /// 获取用于排版计算的页面宽度（优先使用裁剪框尺寸，已考虑旋转角度）
        /// </summary>
        /// <returns>页面宽度（毫米）</returns>
        public float GetEffectivePageWidth()
        {
            // 先获取原始宽高
            float rawWidth = HasCropBox ? CropBoxWidth : (float)(FirstPageSize?.Width ?? 0);
            float rawHeight = HasCropBox ? CropBoxHeight : (float)(FirstPageSize?.Height ?? 0);

            // 根据旋转角度调整宽高：90度战27090度旋转时，宽高互换
            if (PageRotation == 90 || PageRotation == 270)
            {
                return rawHeight; // 旋转90度战27090度后，原始高度变成宽度
            }
            return rawWidth;
        }

        /// <summary>
        /// 获取用于排版计算的页面高度（优先使用裁剪框尺寸，已考虑旋转角度）
        /// </summary>
        /// <returns>页面高度（毫米）</returns>
        public float GetEffectivePageHeight()
        {
            // 先获取原始宽高
            float rawWidth = HasCropBox ? CropBoxWidth : (float)(FirstPageSize?.Width ?? 0);
            float rawHeight = HasCropBox ? CropBoxHeight : (float)(FirstPageSize?.Height ?? 0);

            // 根据旋转角度调整宽高：90度战27090度旋转时，宽高互换
            if (PageRotation == 90 || PageRotation == 270)
            {
                return rawWidth; // 旋转90度战27090度后，原始宽度变成高度
            }
            return rawHeight;
        }
    }

    #endregion

    #region 结果模型

    /// <summary>
    /// 布局计算结果
    /// </summary>
    public class ImpositionResult
    {
        /// <summary>
        /// 最优布局数量（每页容纳的页面数）
        /// </summary>
        public int OptimalLayoutQuantity { get; set; }

        /// <summary>
        /// 列数
        /// </summary>
        public int Columns { get; set; }

        /// <summary>
        /// 行数
        /// </summary>
        public int Rows { get; set; }

        /// <summary>
        /// 单元格宽度（毫米）
        /// </summary>
        public float CellWidth { get; set; }

        /// <summary>
        /// 单元格高度（毫米）
        /// </summary>
        public float CellHeight { get; set; }

        /// <summary>
        /// 空间利用率（百分比）
        /// </summary>
        public float SpaceUtilization { get; set; }

        /// <summary>
        /// 卷装模式的单行利用率（仅用于卷装模式）
        /// </summary>
        public float? RowOneUtilization { get; set; }

        /// <summary>
        /// 是否适合印刷
        /// </summary>
        public bool IsPrintable { get; set; }

        /// <summary>
        /// 是否使用了旋转
        /// </summary>
        public bool UseRotation { get; set; }

        /// <summary>
        /// 旋转角度（0或270度）
        /// </summary>
        public int RotationAngle { get; set; }

        /// <summary>
        /// 计算说明
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 计算耗时（毫秒）
        /// </summary>
        public long CalculationTimeMs { get; set; }

        /// <summary>
        /// 材料类型
        /// </summary>
        public MaterialType MaterialType { get; set; }

        /// <summary>
        /// 排版模式
        /// </summary>
        public LayoutMode LayoutMode { get; set; }

        /// <summary>
        /// 是否成功计算
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 错误信息（如果计算失败）
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// 空白页补充结果
    /// </summary>
    public class BlankPageResult
    {
        /// <summary>
        /// 原始页数
        /// </summary>
        public int OriginalPageCount { get; set; }

        /// <summary>
        /// 布局数量
        /// </summary>
        public int LayoutQuantity { get; set; }

        /// <summary>
        /// 需要补充的空白页数量
        /// </summary>
        public int BlankPagesNeeded { get; set; }

        /// <summary>
        /// 补充后的总页数
        /// </summary>
        public int TotalPageCount { get; set; }

        /// <summary>
        /// 所需纸张数
        /// </summary>
        public int RequiredSheets { get; set; }

        /// <summary>
        /// 空白页建议插入位置列表
        /// </summary>
        public System.Collections.Generic.List<int> BlankPagePositions { get; set; } = new System.Collections.Generic.List<int>();

        /// <summary>
        /// 计算详情和说明
        /// </summary>
        public string CalculationDetails { get; set; }

        /// <summary>
        /// 排版模式
        /// </summary>
        public LayoutMode LayoutMode { get; set; }

        /// <summary>
        /// 是否成功计算
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// 错误信息（如果计算失败）
        /// </summary>
        public string ErrorMessage { get; set; }
    }

    #endregion

    #region 验证结果

    /// <summary>
    /// 排版配置验证结果
    /// </summary>
    public class ImpositionValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 警告信息列表
        /// </summary>
        public System.Collections.Generic.List<string> Warnings { get; set; } = new System.Collections.Generic.List<string>();

        /// <summary>
        /// 验证上下文
        /// </summary>
        public string ValidationContext { get; set; }
    }

    #endregion
}