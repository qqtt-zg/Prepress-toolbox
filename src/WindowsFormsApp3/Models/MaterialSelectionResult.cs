using System.Collections.Generic;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// 材料选择对话框结果
    /// </summary>
    public class MaterialSelectionResult
    {
        /// <summary>
        /// 选中的材料
        /// </summary>
        public string SelectedMaterial { get; set; }

        /// <summary>
        /// 输入的数量
        /// </summary>
        public string SelectedQuantity { get; set; }

        /// <summary>
        /// 输入的序号
        /// </summary>
        public string SelectedSerialNumber { get; set; }

        /// <summary>
        /// 列值字典（列名 -> 选中的值）
        /// </summary>
        public Dictionary<string, string> ColumnValues { get; set; }

        /// <summary>
        /// 是否启用列组合模式
        /// </summary>
        public bool IsColumnCombineMode { get; set; }
        
        /// <summary>
        /// 导出路径
        /// </summary>
        public string ExportPath { get; set; }

        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// 尺寸（格式：宽x高）
        /// </summary>
        public string Dimensions { get; set; }

        /// <summary>
        /// 工艺（如：覆膜类型）
        /// </summary>
        public string Process { get; set; }

        /// <summary>
        /// 布局行数
        /// </summary>
        public string LayoutRows { get; set; }

        /// <summary>
        /// 布局列数
        /// </summary>
        public string LayoutColumns { get; set; }

        /// <summary>
        /// 选中的形状类型（0=直角，1=圆形，2=圆角，3=异形）
        /// </summary>
        public int SelectedShape { get; set; }

        /// <summary>
        /// 圆角半径（仅用于圆角矩形）
        /// </summary>
        public double RoundRadius { get; set; }

        /// <summary>
        /// 是否明确选择了形状
        /// </summary>
        public bool IsShapeSelected { get; set; }

        /// <summary>
        /// 兼容的圆角半径字符串（用于文件名和处理）
        /// </summary>
        public string CornerRadius { get; set; }

        /// <summary>
        /// 布局计算是否需要旋转
        /// </summary>
        public bool NeedsRotation { get; set; }

        /// <summary>
        /// 旋转角度（0, 90, 180, 270）
        /// </summary>
        public int RotationAngle { get; set; }

        /// <summary>
        /// 是否启用排版
        /// </summary>
        public bool EnableImposition { get; set; } = false;

        /// <summary>
        /// 排版模式（连拼/折手）
        /// </summary>
        public LayoutMode LayoutMode { get; set; } = LayoutMode.Continuous;

        /// <summary>
        /// 排版数量（每纸页数）
        /// </summary>
        public int LayoutQuantity { get; set; } = 0;

        /// <summary>
        /// 列组合数据
        /// </summary>
        public string CompositeColumn { get; set; }

        /// <summary>
        /// 是否添加标识页
        /// </summary>
        public bool AddIdentifierPage { get; set; } = false;

        /// <summary>
        /// 标识页文字内容
        /// </summary>
        public string IdentifierPageContent { get; set; } = "";

        /// <summary>
        /// 一式N联联数
        /// </summary>
        public int CopyCount { get; set; }

        /// <summary>
        /// 一式N联倍数方向
        /// </summary>
        public CopyMode CopyMode { get; set; }

	/// <summary>
	/// 一式类型（联/份）
	/// </summary>
	public CopyType CopyType { get; set; }

	/// <summary>
	/// 份数（一式几份）
	/// </summary>
	public int DuplicateCount { get; set; }

        /// <summary>
        /// 排版材料类型 (FlatSheet/RollMaterial)
        /// </summary>
        public string ImpositionMaterialType { get; set; }

        /// <summary>
        /// 更新后的正则结果（序号搜索反向更新时使用）
        /// </summary>
        public string UpdatedRegexResult { get; set; }

        public MaterialSelectionResult()
        {
            ColumnValues = new Dictionary<string, string>();
        }
    }
}
