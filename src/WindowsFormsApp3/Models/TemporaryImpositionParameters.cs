namespace WindowsFormsApp3.Models
{
    public enum ImpositionLayoutStrategy
    {
        Automatic,
        FixedGrid,
        FixedQuantity,
        Multiple
    }

    /// <summary>两种材料分别记住已应用的布局；旧字段保留为当前材料的兼容入口。</summary>
    public class ImpositionLayoutState
    {
        public ImpositionLayoutStrategy? Strategy { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int Quantity { get; set; }
        public bool MultipleEnabled { get; set; }
        public int Multiple { get; set; }
        public CopyMode? Direction { get; set; }
        public float SpacingX { get; set; }
        public float SpacingY { get; set; }
        public ImpositionLayoutState Clone() => (ImpositionLayoutState)MemberwiseClone();
    }

    /// <summary>
    /// 材料选择对话框的临时排版参数，独立保留上次值，不写入全局自动排版设置。
    /// </summary>
    public class TemporaryImpositionParameters
    {
        public bool Enabled { get; set; } = true;

        /// <summary>空值兼容旧快照；新编辑器显式使用互斥策略。</summary>
        public ImpositionLayoutStrategy? Strategy { get; set; }
        public ImpositionLayoutState FlatLayout { get; set; }
        public ImpositionLayoutState RollLayout { get; set; }
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

        /// <summary>排版水平列间距 (mm)。</summary>
        public float SpacingX { get; set; }

        /// <summary>排版垂直行间距 (mm)。</summary>
        public float SpacingY { get; set; }

        /// <summary>0 表示自动计算。</summary>
        public int RequestedRows { get; set; }

        /// <summary>0 表示自动计算。</summary>
        public int RequestedColumns { get; set; }

        /// <summary>0 表示按行列输入；正数表示每纸指定布局数量。</summary>
        public int RequestedLayoutQuantity { get; set; }

        /// <summary>本次材料选择是否启用独立的行列倍数约束。</summary>
        public bool IsLayoutMultipleEnabled { get; set; }

        /// <summary>行列约束倍数，与 PDF 页面复制次数无关。</summary>
        public int LayoutMultiple { get; set; } = 2;

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
            if (FlatLayout != null && !FlatLayout.Direction.HasValue) FlatLayout.Direction = LayoutConstraintDirection;
            if (RollLayout != null && !RollLayout.Direction.HasValue) RollLayout.Direction = LayoutConstraintDirection;
        }

        public TemporaryImpositionParameters Clone()
        {
            var value = (TemporaryImpositionParameters)MemberwiseClone();
            value.FlatLayout = FlatLayout?.Clone(); value.RollLayout = RollLayout?.Clone();
            return value;
        }

        private ImpositionLayoutState CaptureLayout() => new ImpositionLayoutState
        {
            Strategy = Strategy, Rows = RequestedRows, Columns = RequestedColumns, Quantity = RequestedLayoutQuantity,
            MultipleEnabled = IsLayoutMultipleEnabled, Multiple = LayoutMultiple, Direction = LayoutConstraintDirection,
            SpacingX = SpacingX, SpacingY = SpacingY
        };

        public void EnsureLayoutSnapshots()
        {
            FlatLayout ??= CaptureLayout(); RollLayout ??= CaptureLayout();
        }

        public void StoreLayout(bool flat)
        {
            if (flat) FlatLayout = CaptureLayout(); else RollLayout = CaptureLayout();
        }

        public TemporaryImpositionParameters ForMaterial(bool flat)
        {
            var value = Clone();
            var state = flat ? FlatLayout : RollLayout;
            if (state != null)
            {
                value.Strategy = state.Strategy; value.RequestedRows = state.Rows; value.RequestedColumns = state.Columns;
                value.RequestedLayoutQuantity = state.Quantity; value.IsLayoutMultipleEnabled = state.MultipleEnabled;
                value.LayoutMultiple = state.Multiple; value.LayoutConstraintDirection = state.Direction;
                value.SpacingX = state.SpacingX; value.SpacingY = state.SpacingY;
            }
            return value;
        }

        /// <summary>清除仅本次使用的布局操作，保留尺寸、边距和方向策略。</summary>
        public void ResetLayoutOverrides()
        {
            Strategy = null;
            FlatLayout = RollLayout = null;
            RequestedRows = 0;
            RequestedColumns = 0;
            RequestedLayoutQuantity = 0;
            IsLayoutMultipleEnabled = false;
            LayoutMultiple = 2;
        }

        /// <summary>只输出当前策略的有效参数，隐藏的缓存不参与生产计算。</summary>
        public TemporaryImpositionParameters EffectiveLayout(bool? flat = null)
        {
            var value = flat.HasValue ? ForMaterial(flat.Value) : Clone();
            if (!value.Strategy.HasValue) return value;
            value.IsLayoutMultipleEnabled = value.Strategy == ImpositionLayoutStrategy.Multiple;
            if (value.Strategy != ImpositionLayoutStrategy.FixedQuantity) value.RequestedLayoutQuantity = 0;
            if (value.Strategy == ImpositionLayoutStrategy.Automatic || value.Strategy == ImpositionLayoutStrategy.Multiple)
                value.RequestedRows = value.RequestedColumns = 0;
            return value;
        }

        public TemporaryImpositionParameters CloneWithoutLayoutOverrides()
        {
            var clone = Clone();
            clone.ResetLayoutOverrides();
            return clone;
        }
    }
}
