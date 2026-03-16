using System;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// 材料选择预设配置
    /// 用于保存和加载材料选择对话框的常用设置组合
    /// </summary>
    public class MaterialSelectionPreset
    {
        /// <summary>
        /// 预设名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 选中的材料名称
        /// </summary>
        public string SelectedMaterial { get; set; } = "";

        /// <summary>
        /// 出血值
        /// </summary>
        public double TetBleed { get; set; } = 0;

        /// <summary>
        /// 颜色模式（彩色/黑白）
        /// </summary>
        public string ColorMode { get; set; } = "彩色";

        /// <summary>
        /// 膜类型（光膜/哑膜）
        /// </summary>
        public string FilmType { get; set; } = "光膜";

        /// <summary>
        /// 是否添加标识页
        /// </summary>
        public bool AddIdentifierPage { get; set; } = false;

        /// <summary>
        /// 形状类型（RightAngle/Circle/Special/RoundRect）
        /// </summary>
        public string ShapeState { get; set; } = "RightAngle";

        /// <summary>
        /// 是否启用一式两联
        /// </summary>
        public bool IsDualCopy { get; set; } = false;

        /// <summary>
        /// 导出路径
        /// </summary>
        public string ExportPath { get; set; } = "";

        /// <summary>
        /// 圆角半径（仅用于圆角矩形）
        /// </summary>
        public double RoundRadius { get; set; } = 0;
    }
}
