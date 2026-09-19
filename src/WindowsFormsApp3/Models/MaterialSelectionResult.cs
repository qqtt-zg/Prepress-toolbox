using System.Collections.Generic;

namespace WindowsFormsApp3.Models
{
    public enum OrderNumberMode
    {
        None = 0,
        AutoIncrement = 1,
        RegexExtraction = 2
    }

    public class BatchFileItem
    {
        public int Index { get; set; }
        public bool IsSelected { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        public string GroupId { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public int? PageCount { get; set; }
        public string OrderNumber { get; set; }
        public string Quantity { get; set; }
        public string SerialNumber { get; set; }
        public bool IsExcelMatched { get; set; }
        public int ExcelRowIndex { get; set; } = -1;
        public string RegexResult { get; set; }
        public string Dimensions { get; set; }
        public string Shape { get; set; }
       public string Material { get; set; } = "";
       public string Process { get; set; } = "";
        public string ColorMode { get; set; } = "";
        public string FilmType { get; set; } = "";
        public string MaterialType { get; set; } = "";
        public string LayoutPattern { get; set; } = "";
        public string RoundRadius { get; set; } = "";
        public string ImpositionMode { get; set; } = "";
        public string LayoutInfo { get; set; } = "";
        public bool IsPreserveJob { get; set; } = false;
        public string PreservePrefix { get; set; } = "";
        public double RawPdfWidth { get; set; } = 0;
        public double RawPdfHeight { get; set; } = 0;
        public string ExportPath { get; set; } = "";
    }

    /// <summary>
    /// 智能工艺分组实体（按相同材料、覆膜工艺、切刀形状等参数聚合）
    /// </summary>
    public class BatchProcessGroup
    {
        public string GroupId { get; set; } = System.Guid.NewGuid().ToString("N");
        public string GroupName { get; set; } = "【新单工艺组 1】";
        public bool IsPreserveGroup { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        public bool IsCollapsed { get; set; } = false;
        public string Material { get; set; } = "";
       public string Process { get; set; } = "";
        public string ColorMode { get; set; } = "";
        public string FilmType { get; set; } = "";
        public string MaterialType { get; set; } = "";
        public string LayoutPattern { get; set; } = "";
       public string Shape { get; set; } = "";
       public string RoundRadius { get; set; } = "";
        public string ImpositionMode { get; set; } = "";
        public string ExportPath { get; set; } = "";
        public List<BatchFileItem> Items { get; set; } = new List<BatchFileItem>();
    }

    public class MaterialSelectionResult
    {
        public string SelectedMaterial { get; set; }
        public string SelectedQuantity { get; set; }
        public string SelectedSerialNumber { get; set; }
        public Dictionary<string, string> ColumnValues { get; set; }
        public bool IsColumnCombineMode { get; set; }
        public string ExportPath { get; set; }
        public string OrderNumber { get; set; }
        public string Dimensions { get; set; }
        public double SelectedTetBleed { get; set; } = 0;
        public string Process { get; set; }
        public string LayoutRows { get; set; }
        public string LayoutColumns { get; set; }
        public int SelectedShape { get; set; }
        public double RoundRadius { get; set; }
        public bool IsShapeSelected { get; set; }
        public string CornerRadius { get; set; }
        public bool NeedsRotation { get; set; }
        public int RotationAngle { get; set; }
        public bool IsForceRotationEnabled { get; set; } = false;
        public bool EnableImposition { get; set; } = false;
        public LayoutMode LayoutMode { get; set; } = LayoutMode.Continuous;
        public int LayoutQuantity { get; set; } = 0;
        public string CompositeColumn { get; set; }
        public bool AddIdentifierPage { get; set; } = false;
        public string IdentifierPageContent { get; set; } = "";
        public int CopyCount { get; set; }
        public CopyMode CopyMode { get; set; }
        public CopyType CopyType { get; set; }
        public int DuplicateCount { get; set; }
        public string ImpositionMaterialType { get; set; }
        public string UpdatedRegexResult { get; set; }
        public TemporaryImpositionParameters TemporaryImpositionParameters { get; set; }

        public bool IsApplyToAll { get; set; } = false;
        public OrderNumberMode OrderNumberMode { get; set; } = OrderNumberMode.None;
        public string SelectedOrderRegexName { get; set; } = "";
        public string SelectedOrderRegexPattern { get; set; } = "";
        public List<BatchFileItem> BatchItems { get; set; } = new List<BatchFileItem>();
        public List<BatchProcessGroup> ProcessGroups { get; set; } = new List<BatchProcessGroup>();

        public MaterialSelectionResult()
        {
            ColumnValues = new Dictionary<string, string>();
            BatchItems = new List<BatchFileItem>();
            ProcessGroups = new List<BatchProcessGroup>();
        }
    }
}
