namespace WindowsFormsApp3.Models
{
    /// <summary>保留原始输入，切换策略或材料时不丢弃尚未合法的草稿。</summary>
    public class ImpositionEditorDraft
    {
        public string Width { get; set; }
        public string Length { get; set; }
        public string Left { get; set; }
        public string Right { get; set; }
        public string Top { get; set; }
        public string Bottom { get; set; }
        public string SpacingX { get; set; }
        public string SpacingY { get; set; }
        public string Rows { get; set; }
        public string Columns { get; set; }
        public string Quantity { get; set; }
        public string Multiple { get; set; }
        public bool AutoRows { get; set; }
        public bool AutoColumns { get; set; }
        public ImpositionLayoutStrategy Strategy { get; set; }
        public ImpositionLayoutStrategy LastSpecifiedStrategy { get; set; } = ImpositionLayoutStrategy.FixedGrid;
        public bool QuantityInitialized { get; set; }
        public int QuantityRows { get; set; }
        public int QuantityColumns { get; set; }
        public int QuantityAngle { get; set; }
        public RollRotationMode Rotation { get; set; }
        public CopyMode Direction { get; set; }
    }

    public class ImpositionPreviewGeometry
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Left { get; set; }
        public float Right { get; set; }
        public float Top { get; set; }
        public float Bottom { get; set; }
        public float PrintableLeft { get; set; }
        public float PrintableTop { get; set; }
        public float PrintableWidth { get; set; }
        public float PrintableHeight { get; set; }
        public float SpacingX { get; set; }
        public float SpacingY { get; set; }
        public float GridWidth { get; set; }
        public float GridHeight { get; set; }
    }
}
