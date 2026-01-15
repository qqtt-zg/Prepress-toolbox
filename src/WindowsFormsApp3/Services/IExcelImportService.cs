using System.Data;
using System.Windows.Forms;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// Excel导入服务接口，负责Excel文件数据的导入、显示和处理
    /// 提供数据导入、UI显示和数据操作等功能
    /// </summary>
    public interface IExcelImportService
    {
        /// <summary>
        /// 获取或设置导入的Excel数据表格
        /// </summary>
        DataTable ImportedData { get; set; }

        /// <summary>
        /// 获取或设置用于搜索的列索引
        /// </summary>
        int SearchColumnIndex { get; set; }

        /// <summary>
        /// 获取或设置数量列的索引
        /// 用于存储和显示数量数据，以便后续操作中明确区分数量信息
        /// </summary>
        int ReturnColumnIndex { get; set; }

        /// <summary>
        /// 获取或设置序号列的索引
        /// </summary>
        int SerialColumnIndex { get; set; }

        /// <summary>
        /// 获取或设置材料列的索引
        /// 用于存储和匹配材料数据
        /// </summary>
        int MaterialColumnIndex { get; set; }

        /// <summary>
        /// 获取或设置选中的正则表达式模式
        /// 用于在数据匹配时提取文件名中的正则结果
        /// </summary>
        string SelectedRegexPattern { get; set; }

        /// <summary>
        /// 获取Excel导入表单实例
        /// </summary>
        ExcelImportForm ExcelImportFormInstance { get; }

        /// <summary>
        /// 启动Excel导入流程
        /// 显示文件选择对话框并导入所选Excel文件的数据
        /// </summary>
        /// <returns>导入操作是否成功完成</returns>
        bool StartImport();

        /// <summary>
        /// 将导入的Excel数据显示到DataGridView控件
        /// </summary>
        /// <param name="dataGridView">目标DataGridView控件</param>
        /// <exception cref="ArgumentNullException">当dataGridView为null时抛出</exception>
        /// <exception cref="InvalidOperationException">当没有有效的导入数据时抛出</exception>
        void DisplayImportedData(DataGridView dataGridView);

        /// <summary>
        /// 自动调整DataGridView控件的列宽以适应内容
        /// </summary>
        /// <param name="dataGridView">目标DataGridView控件</param>
        /// <exception cref="ArgumentNullException">当dataGridView为null时抛出</exception>
        void AdjustColumnWidths(DataGridView dataGridView);

        /// <summary>
        /// 获取指定列的标题，如果原标题为空则使用默认标题
        /// </summary>
        /// <param name="dataGridView">DataGridView控件</param>
        /// <param name="columnIndex">列索引</param>
        /// <returns>列标题字符串</returns>
        /// <exception cref="ArgumentNullException">当dataGridView为null时抛出</exception>
        /// <exception cref="ArgumentOutOfRangeException">当columnIndex超出有效范围时抛出</exception>
        string GetColumnHeader(DataGridView dataGridView, int columnIndex);

        /// <summary>
        /// 清空当前导入的数据
        /// 重置所有相关状态和属性
        /// </summary>
        void ClearData();

        /// <summary>
        /// 检查是否存在有效的导入数据
        /// </summary>
        /// <returns>如果存在有效数据则返回true，否则返回false</returns>
        bool HasValidData();

        /// <summary>
        /// 获取导入数据的信息摘要
        /// 包含行数、列数等基本信息
        /// </summary>
        /// <returns>数据摘要信息字符串</returns>
        string GetDataSummary();
    }
}