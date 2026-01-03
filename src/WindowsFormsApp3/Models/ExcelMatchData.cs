namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// Excel 匹配数据
    /// </summary>
    public class ExcelMatchData
    {
        /// <summary>
        /// 匹配的行索引
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 数量值
        /// </summary>
        public string Quantity { get; set; }

        /// <summary>
        /// 序号值
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// 材料值
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// 是否有匹配结果
        /// </summary>
        public bool HasMatch => RowIndex >= 0;

        public ExcelMatchData()
        {
            RowIndex = -1;
            Quantity = "";
            SerialNumber = "";
            Material = "";
        }
    }
}
