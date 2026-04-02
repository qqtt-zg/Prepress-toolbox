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
        /// 联数（一式几联）
        /// </summary>
        public int CopyCount { get; set; } = 2;

        /// <summary>
        /// 联数倍数方向
        /// </summary>
        public CopyMode CopyMode { get; set; } = CopyMode.AutoByColumn;

        /// <summary>
        /// 导出路径
        /// </summary>
        public string ExportPath { get; set; } = "";

        /// <summary>
        /// 圆角半径（仅用于圆角矩形）
        /// </summary>
        public double RoundRadius { get; set; } = 0;

        /// <summary>
        /// 材料类型（FlatSheet-平张/RollMaterial-卷装）
        /// </summary>
        public string MaterialType { get; set; } = "FlatSheet";

        /// <summary>
        /// 排版模式（Continuous-连拼/Folding-折手）
        /// </summary>
        public string LayoutMode { get; set; } = "Continuous";

        /// <summary>
        /// 是否启用排版
        /// </summary>
        public bool EnableImposition { get; set; } = false;

        /// <summary>
        /// 预设加载时禁用的参数（使用 PresetIgnoreOptions 标志）
        /// </summary>
        public PresetIgnoreOptions DisabledOptions { get; set; } = PresetIgnoreOptions.None;

        /// <summary>
        /// 是否显示在预设按钮区域（默认不显示）
        /// </summary>
        public bool ShowInPresetButtons { get; set; } = false;

        /// <summary>
        /// 创建当前预设的深度副本
        /// </summary>
        public MaterialSelectionPreset Clone()
        {
            return new MaterialSelectionPreset
            {
                Name = this.Name,
                SelectedMaterial = this.SelectedMaterial,
                TetBleed = this.TetBleed,
                ColorMode = this.ColorMode,
                FilmType = this.FilmType,
                AddIdentifierPage = this.AddIdentifierPage,
                ShapeState = this.ShapeState,
                IsDualCopy = this.IsDualCopy,
                CopyCount = this.CopyCount,
                CopyMode = this.CopyMode,
                ExportPath = this.ExportPath,
                RoundRadius = this.RoundRadius,
                MaterialType = this.MaterialType,
                LayoutMode = this.LayoutMode,
                EnableImposition = this.EnableImposition,
                DisabledOptions = this.DisabledOptions,
                ShowInPresetButtons = this.ShowInPresetButtons
            };
        }

        /// <summary>
        /// 从另一个预设中复制所有属性
        /// </summary>
        public void CopyFrom(MaterialSelectionPreset other)
        {
            if (other == null) return;

            this.Name = other.Name;
            this.SelectedMaterial = other.SelectedMaterial;
            this.TetBleed = other.TetBleed;
            this.ColorMode = other.ColorMode;
            this.FilmType = other.FilmType;
            this.AddIdentifierPage = other.AddIdentifierPage;
            this.ShapeState = other.ShapeState;
            this.IsDualCopy = other.IsDualCopy;
            this.CopyCount = other.CopyCount;
            this.CopyMode = other.CopyMode;
            this.ExportPath = other.ExportPath;
            this.RoundRadius = other.RoundRadius;
            this.MaterialType = other.MaterialType;
            this.LayoutMode = other.LayoutMode;
            this.EnableImposition = other.EnableImposition;
            this.DisabledOptions = other.DisabledOptions;
            this.ShowInPresetButtons = other.ShowInPresetButtons;
        }
    }

    /// <summary>
    /// 预设忽略选项标志枚举
    /// </summary>
    [Flags]
    public enum PresetIgnoreOptions
    {
        None = 0,
        Material = 1,
        TetBleed = 2,
        ColorMode = 4,
        FilmType = 8,
        IdentifierPage = 16,
        Shape = 32,
        IsDualCopy = 64,
        CopyCount = 8192,
        CopyMode = 16384,
        ExportPath = 128,
        RoundRadius = 256,
        MaterialType = 512,
        LayoutMode = 1024,
        EnableImposition = 2048,
        ShowInPresetButtons = 4096
    }
}
