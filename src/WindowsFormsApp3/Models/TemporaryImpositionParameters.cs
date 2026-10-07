namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// 材料选择对话框的临时排版参数，独立保留上次值，不写入全局自动排版设置。
    /// </summary>
    public class TemporaryImpositionParameters
    {
        /// <summary>平张纸张尺寸是否已解锁自定义。</summary>
        public bool IsPaperSizeUnlocked { get; set; }

        /// <summary>卷装材料规格是否已解锁自定义。</summary>
        public bool IsRollSizeUnlocked { get; set; }

        public float PaperWidth { get; set; }
        public float PaperHeight { get; set; }
        public float FixedWidth { get; set; }
        public float MinLength { get; set; }

        public float MarginTop { get; set; }
        public float MarginBottom { get; set; }
        public float MarginLeft { get; set; }
        public float MarginRight { get; set; }

        public float RollMarginTop { get; set; }
        public float RollMarginBottom { get; set; }
        public float RollMarginLeft { get; set; }
        public float RollMarginRight { get; set; }

        /// <summary>0 表示自动计算。</summary>
        public int RequestedRows { get; set; }

        /// <summary>0 表示自动计算。</summary>
        public int RequestedColumns { get; set; }

        /// <summary>卷装旋转标签是否处于强制切换状态。</summary>
        public bool IsForceRotationEnabled { get; set; }

        /// <summary>空值表示旧配置尚未迁移；显式 Auto 优先于旧版不旋转模式。</summary>
        public RollRotationMode? FlatSheetRotationMode { get; set; }
        public RollRotationMode? RollMaterialRotationMode { get; set; }
        /// <summary>仅存储行／列约束方向，旋转由独立策略决定。</summary>
        public CopyMode? LayoutConstraintDirection { get; set; }

        public void MigrateRotationModes(CopyMode legacyMode, int legacyAngle = 0)
        {
            bool noRotation = legacyMode == CopyMode.FixedNoRotationByColumn || legacyMode == CopyMode.FixedNoRotationByRow;
            LayoutConstraintDirection ??= legacyMode == CopyMode.AutoByRow || legacyMode == CopyMode.FixedNoRotationByRow
                ? CopyMode.AutoByRow : CopyMode.AutoByColumn;
            FlatSheetRotationMode ??= noRotation ? RollRotationMode.Force0Degree : RollRotationMode.Auto;
            RollMaterialRotationMode ??= IsForceRotationEnabled
                ? (legacyAngle == 270 ? RollRotationMode.Force270Degree : RollRotationMode.Force0Degree)
                : (noRotation ? RollRotationMode.Force0Degree : RollRotationMode.Auto);
            IsForceRotationEnabled = false;
        }

        public TemporaryImpositionParameters Clone()
        {
            return (TemporaryImpositionParameters)MemberwiseClone();
        }
    }
}
