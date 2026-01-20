using System;
using System.Collections.Generic;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// PDF检查器信息模型
    /// 类似于Enfocus PitStop Pro的Inspector功能
    /// </summary>
    public class PdfInspectorInfo
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 当前页码（1-based）
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 当前页面的页面框信息
        /// </summary>
        public PageBoxInfo CurrentPageBoxes { get; set; }

        /// <summary>
        /// 所有页面的页面框信息
        /// </summary>
        public List<PageBoxInfo> AllPageBoxes { get; set; }

        /// <summary>
        /// 页面框异常列表
        /// </summary>
        public List<PageBoxIssue> Issues { get; set; }

        public PdfInspectorInfo()
        {
            AllPageBoxes = new List<PageBoxInfo>();
            Issues = new List<PageBoxIssue>();
        }
    }

    /// <summary>
    /// 页面框信息
    /// </summary>
    public class PageBoxInfo
    {
        /// <summary>
        /// 页码（1-based）
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 页面旋转角度
        /// </summary>
        public int Rotation { get; set; }

        /// <summary>
        /// MediaBox（媒体框）- PDF页面的物理尺寸
        /// </summary>
        public BoxDimension MediaBox { get; set; }

        /// <summary>
        /// CropBox（裁剪框）- 页面显示和打印的区域
        /// </summary>
        public BoxDimension CropBox { get; set; }

        /// <summary>
        /// TrimBox（裁切框）- 成品尺寸
        /// </summary>
        public BoxDimension TrimBox { get; set; }

        /// <summary>
        /// BleedBox（出血框）- 包含出血的区域
        /// </summary>
        public BoxDimension BleedBox { get; set; }

        /// <summary>
        /// ArtBox（艺术框）- 有意义内容的区域
        /// </summary>
        public BoxDimension ArtBox { get; set; }

        /// <summary>
        /// 是否有异常
        /// </summary>
        public bool HasIssues { get; set; }

        /// <summary>
        /// 异常描述
        /// </summary>
        public List<string> IssueDescriptions { get; set; }

        public PageBoxInfo()
        {
            IssueDescriptions = new List<string>();
        }
    }

    /// <summary>
    /// 页面框尺寸信息
    /// </summary>
    public class BoxDimension
    {
        /// <summary>
        /// 是否已定义
        /// </summary>
        public bool IsDefined { get; set; }

        /// <summary>
        /// 左下角X坐标（点）
        /// </summary>
        public double Left { get; set; }

        /// <summary>
        /// 左下角Y坐标（点）
        /// </summary>
        public double Bottom { get; set; }

        /// <summary>
        /// 右上角X坐标（点）
        /// </summary>
        public double Right { get; set; }

        /// <summary>
        /// 右上角Y坐标（点）
        /// </summary>
        public double Top { get; set; }

        /// <summary>
        /// 宽度（点）
        /// </summary>
        public double Width => Right - Left;

        /// <summary>
        /// 高度（点）
        /// </summary>
        public double Height => Top - Bottom;

        /// <summary>
        /// 宽度（毫米）
        /// </summary>
        public double WidthMm => Math.Round(Width / 72 * 25.4, 2);

        /// <summary>
        /// 高度（毫米）
        /// </summary>
        public double HeightMm => Math.Round(Height / 72 * 25.4, 2);

        /// <summary>
        /// 宽度（英寸）
        /// </summary>
        public double WidthInch => Math.Round(Width / 72, 3);

        /// <summary>
        /// 高度（英寸）
        /// </summary>
        public double HeightInch => Math.Round(Height / 72, 3);

        /// <summary>
        /// 获取格式化的尺寸字符串
        /// </summary>
        public string GetFormattedSize(MeasurementUnit unit)
        {
            switch (unit)
            {
                case MeasurementUnit.Millimeter:
                    return $"{WidthMm} × {HeightMm} mm";
                case MeasurementUnit.Inch:
                    return $"{WidthInch} × {HeightInch} in";
                case MeasurementUnit.Point:
                    return $"{Math.Round(Width, 2)} × {Math.Round(Height, 2)} pt";
                default:
                    return $"{WidthMm} × {HeightMm} mm";
            }
        }

        /// <summary>
        /// 获取格式化的位置字符串
        /// </summary>
        public string GetFormattedPosition(MeasurementUnit unit)
        {
            switch (unit)
            {
                case MeasurementUnit.Millimeter:
                    return $"({Math.Round(Left / 72 * 25.4, 2)}, {Math.Round(Bottom / 72 * 25.4, 2)}) mm";
                case MeasurementUnit.Inch:
                    return $"({Math.Round(Left / 72, 3)}, {Math.Round(Bottom / 72, 3)}) in";
                case MeasurementUnit.Point:
                    return $"({Math.Round(Left, 2)}, {Math.Round(Bottom, 2)}) pt";
                default:
                    return $"({Math.Round(Left / 72 * 25.4, 2)}, {Math.Round(Bottom / 72 * 25.4, 2)}) mm";
            }
        }
    }

    /// <summary>
    /// 页面框问题
    /// </summary>
    public class PageBoxIssue
    {
        /// <summary>
        /// 页码
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// 问题类型
        /// </summary>
        public IssueType Type { get; set; }

        /// <summary>
        /// 问题描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 严重程度
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// 涉及的页面框类型
        /// </summary>
        public string BoxType { get; set; }
    }

    /// <summary>
    /// 问题类型
    /// </summary>
    public enum IssueType
    {
        /// <summary>
        /// 页面框未定义
        /// </summary>
        UndefinedBox,

        /// <summary>
        /// 页面框尺寸无效（0或负数）
        /// </summary>
        InvalidSize,

        /// <summary>
        /// 页面框超出MediaBox范围
        /// </summary>
        OutOfBounds,

        /// <summary>
        /// 页面框顺序不正确
        /// </summary>
        IncorrectOrder,

        /// <summary>
        /// 不同页面尺寸不一致
        /// </summary>
        InconsistentSize,

        /// <summary>
        /// 页面方向不一致
        /// </summary>
        InconsistentOrientation
    }

    /// <summary>
    /// 问题严重程度
    /// </summary>
    public enum IssueSeverity
    {
        /// <summary>
        /// 信息
        /// </summary>
        Info,

        /// <summary>
        /// 警告
        /// </summary>
        Warning,

        /// <summary>
        /// 错误
        /// </summary>
        Error
    }

    /// <summary>
    /// 测量单位
    /// </summary>
    public enum MeasurementUnit
    {
        /// <summary>
        /// 毫米
        /// </summary>
        Millimeter,

        /// <summary>
        /// 英寸
        /// </summary>
        Inch,

        /// <summary>
        /// 点（PostScript点，1/72英寸）
        /// </summary>
        Point
    }
}
