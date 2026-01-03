using System;
using System.Data;
using System.Windows.Forms;
using System.ComponentModel;
using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Interfaces;
using System.Threading.Tasks;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// Excel导入助手类，封装Excel导入相关的功能
    /// </summary>
    public class ExcelImportHelper : IExcelImportService
    {
        private IExcelParentView _parentView;
        private DataTable _importedData;
        private int _searchColumnIndex = -1;
        private int _returnColumnIndex = -1;
        private int _serialColumnIndex = -1;
        private int _materialColumnIndex = -1;
        private ExcelImportForm _excelImportFormInstance;
        private Interfaces.ILogger _logger;
        
        /// <summary>
        /// 默认构造函数，用于ServiceLocator初始化
        /// </summary>
        public ExcelImportHelper()
        {
            // 空构造函数，供服务定位器使用
            _parentView = null;
            // 注意：此处不直接初始化logger，由ServiceLocator通过属性注入
        }

        /// <summary>
        /// 设置日志服务（用于依赖注入）
        /// </summary>
        /// <param name="logger">日志服务实例</param>
        public void SetLogger(Interfaces.ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 设置父视图引用
        /// </summary>
        /// <param name="parentView">父视图实例</param>
        public void SetParentView(IExcelParentView parentView)
        {
            _parentView = parentView;
        }
        
        /// <summary>
        /// 获取或设置导入的数据表
        /// </summary>
        public DataTable ImportedData
        {
            get { return _importedData; }
            set { _importedData = value; }
        }

        /// <summary>
        /// 导入Excel数据
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="sheetName">工作表名称（可选）</param>
        /// <returns>包含Excel数据的字典列表</returns>
        public List<Dictionary<string, object>> ImportExcelData(string filePath, string sheetName = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("文件路径不能为空", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("指定的Excel文件不存在", filePath);
                }

                var result = new List<Dictionary<string, object>>();
                FileInfo fileInfo = new FileInfo(filePath);

                using (ExcelPackage package = new ExcelPackage(fileInfo))
                {
                    ExcelWorksheet worksheet;
                    if (string.IsNullOrEmpty(sheetName))
                    {
                        // 默认使用第一个工作表
                        if (package.Workbook.Worksheets.Count == 0)
                        {
                            throw new InvalidOperationException("Excel文件中没有工作表");
                        }
                        worksheet = package.Workbook.Worksheets[0];
                    }
                    else
                    {
                        worksheet = package.Workbook.Worksheets[sheetName];
                        if (worksheet == null)
                        {
                            throw new ArgumentException($"指定的工作表 '{sheetName}' 不存在");
                        }
                    }

                    if (worksheet.Dimension == null)
                    {
                        _logger?.LogWarning($"Excel工作表 '{worksheet.Name}' 为空");
                        return result;
                    }

                    // 获取最大行数和列数
                    int rowCount = worksheet.Dimension.Rows;
                    int colCount = worksheet.Dimension.Columns;

                    // 记录工作表信息
                    _logger?.LogInformation($"读取Excel工作表 '{worksheet.Name}'，{rowCount}行 × {colCount}列");

                    // 获取表头行（第一行）
                    var headers = new string[colCount];
                    for (int col = 1; col <= colCount; col++)
                    {
                        headers[col - 1] = worksheet.Cells[1, col].Text ?? $"列{col}";
                    }

                    // 读取数据行
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var rowData = new Dictionary<string, object>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value;
                            rowData[headers[col - 1]] = cellValue;
                        }
                        result.Add(rowData);
                    }
                }

                // 记录成功日志
                _logger?.LogInformation($"成功导入Excel文件: {filePath}，共{result.Count}行数据");

                return result;
            }
            catch (Exception ex)
            {
                // 记录错误日志
                _logger?.LogError(ex, $"导入Excel文件失败: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// 导入Excel数据（包装方法，保持与之前版本的兼容性）
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <param name="sheetName">工作表名称（可选）</param>
        /// <returns>包含Excel数据的字典列表</returns>
        public List<Dictionary<string, object>> ImportExcelDataWrapper(string filePath, string sheetName = null)
        {
            // 直接调用同步方法，因为EPPlus库的操作都是同步的
            return ImportExcelData(filePath, sheetName);
        }

        /// <summary>
        /// 获取或设置搜索列索引
        /// </summary>
        public int SearchColumnIndex
        {
            get { return _searchColumnIndex; }
            set { _searchColumnIndex = value; }
        }

        /// <summary>
        /// 获取或设置数量列的索引
        /// 用于存储和显示数量数据，以便后续操作中明确区分数量信息
        /// </summary>
        public int ReturnColumnIndex
        {
            get { return _returnColumnIndex; }
            set { _returnColumnIndex = value; }
        }

        /// <summary>
        /// 获取或设置序号列索引
        /// </summary>
        public int SerialColumnIndex
        {
            get { return _serialColumnIndex; }
            set { _serialColumnIndex = value; }
        }

        /// <summary>
        /// 获取或设置材料列索引
        /// </summary>
        public int MaterialColumnIndex
        {
            get { return _materialColumnIndex; }
            set { _materialColumnIndex = value; }
        }

        /// <summary>
        /// 获取Excel导入配置窗体实例
        /// </summary>
        public ExcelImportForm ExcelImportFormInstance
        {
            get { return _excelImportFormInstance; }
            internal set { _excelImportFormInstance = value; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="parentView">父视图引用</param>
        public ExcelImportHelper(IExcelParentView parentView)
        {
            _parentView = parentView;
            // Logger将通过SetLogger方法注入
            _logger = null;
        }

        /// <summary>
        /// 启动Excel导入流程
        /// </summary>
        /// <returns>是否成功导入</returns>
        public bool StartImport()
        {
            try
            {
                _logger?.LogInformation("开始Excel导入流程");
                
                // 选择Excel文件
                string filePath = SelectExcelFile();
                if (string.IsNullOrEmpty(filePath))
                {
                    _logger?.LogInformation("用户取消了文件选择");
                    return false;
                }

                // 显示导入配置窗体
                bool result = ShowImportForm(filePath);
                _logger?.LogInformation($"Excel导入流程完成，结果: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导入Excel文件时发生错误");
                return false;
            }
        }

        /// <summary>
        /// 选择Excel文件
        /// </summary>
        /// <returns>选择的文件路径</returns>
        private string SelectExcelFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Excel files (*.xlsx)|*.xlsx|Excel 97-2003 files (*.xls)|*.xls|All files (*.*)|*.*";
                openFileDialog.Title = "选择Excel文件";
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    _logger?.LogInformation($"选择了Excel文件: {openFileDialog.FileName}");
                    return openFileDialog.FileName;
                }
            }
            return null;
        }

        /// <summary>
        /// 显示导入配置窗体
        /// </summary>
        /// <param name="filePath">Excel文件路径</param>
        /// <returns>是否成功导入</returns>
        private bool ShowImportForm(string filePath)
        {
            try
            {
                _excelImportFormInstance = new ExcelImportForm(filePath, _parentView);
                DialogResult result = _excelImportFormInstance.ShowDialog();
                
                if (result == DialogResult.OK)
                {
                    _logger?.LogInformation("用户确认导入配置");
                    
                    // 保存导入结果
                    _importedData = _excelImportFormInstance.ImportedData;
                    _searchColumnIndex = _excelImportFormInstance.SearchColumnIndex;
                    _returnColumnIndex = _excelImportFormInstance.ReturnColumnIndex;
                    _serialColumnIndex = _excelImportFormInstance.NewColumnIndex;

                    // 验证数据和索引
                    if (!ValidateImportResult())
                    {
                        _logger?.LogWarning("导入结果验证失败");
                        return false;
                    }

                    _logger?.LogInformation("Excel导入配置完成");
                    return true;
                }
                
                _logger?.LogInformation("用户取消导入配置");
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "显示导入表单时出错");
                MessageBox.Show($"显示Excel导入配置窗口时出错: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 验证导入结果的有效性
        /// </summary>
        /// <returns>验证是否通过</returns>
        private bool ValidateImportResult()
        {
            if (_importedData == null || _importedData.Rows.Count == 0)
            {
                _logger?.LogWarning("导入的Excel文件中没有数据");
                return false;
            }

            _logger?.LogDebug($"导入数据验证: 搜索列索引: {_searchColumnIndex}, 返回列索引: {_returnColumnIndex}, 序号列索引: {_serialColumnIndex}, 总列数: {_importedData.Columns.Count}");

            // 自动调整无效的列索引
            AutoAdjustColumnIndexes();

            return true;
        }

        /// <summary>
        /// 自动调整无效的列索引
        /// </summary>
        private void AutoAdjustColumnIndexes()
        {
            if (_importedData == null || _importedData.Columns.Count == 0)
            {
                _logger?.LogDebug("数据表为空，跳过列索引调整");
                return;
            }

            int totalColumns = _importedData.Columns.Count;
            bool indexesAdjusted = false;

            // 确保搜索列索引有效
            if (_searchColumnIndex < 0 || _searchColumnIndex >= totalColumns)
            {
                _searchColumnIndex = 0;
                indexesAdjusted = true;
                _logger?.LogDebug("自动调整搜索列索引为: 0");
            }

            // 确保数量列索引有效
            if (_returnColumnIndex < 0 || _returnColumnIndex >= totalColumns)
            {
                _returnColumnIndex = totalColumns > 1 ? 1 : 0;
                indexesAdjusted = true;
                _logger?.LogDebug($"自动调整数量列索引为: {_returnColumnIndex}");
            }

            // 确保数量列索引不与搜索列相同（除非只有一列）
            if (totalColumns > 1 && _returnColumnIndex == _searchColumnIndex)
            {
                _returnColumnIndex = (_searchColumnIndex + 1) % totalColumns;
                indexesAdjusted = true;
                _logger?.LogDebug($"自动调整数量列索引以避免与搜索列重复: {_returnColumnIndex}");
            }

            // 如果只有一列，允许数量列与搜索列相同
            if (totalColumns == 1)
            {
                _returnColumnIndex = 0;
                indexesAdjusted = true;
                _logger?.LogDebug("只有一列，数量列索引设为0");
            }

            // 调整序号列索引
            if (_serialColumnIndex != -1 && (_serialColumnIndex < 0 || _serialColumnIndex >= totalColumns))
            {
                _serialColumnIndex = -1; // 设为无效值表示不使用序号列
                indexesAdjusted = true;
                _logger?.LogDebug("序号列索引无效，设为-1");
            }

            // 如果有调整，显示调试信息
            if (indexesAdjusted)
            {
                _logger?.LogDebug($"调整后的索引: 搜索列={_searchColumnIndex}, 返回列={_returnColumnIndex}, 序号列={_serialColumnIndex}");
            }
        }

        /// <summary>
        /// 将导入的数据显示到DataGridView
        /// </summary>
        /// <param name="dataGridView">目标DataGridView控件</param>
        public void DisplayImportedData(DataGridView dataGridView)
        {
            if (_importedData == null || dataGridView == null)
            {
                _logger?.LogDebug("DisplayImportedData: 数据源或DataGridView为空");
                return;
            }

            try
            {
                _logger?.LogInformation($"开始显示导入数据，行数={_importedData.Rows.Count}, 列数={_importedData.Columns.Count}");

                // 检查导入数据的实际结构
                _logger?.LogDebug($"导入数据表结构: 行数={_importedData.Rows.Count}, 列数={_importedData.Columns.Count}");
                for (int i = 0; i < _importedData.Columns.Count; i++)
                {
                    _logger?.LogDebug($"列 {i}: {_importedData.Columns[i].ColumnName}, 数据类型: {_importedData.Columns[i].DataType}");
                }

                // 确保列索引有效
                AutoAdjustColumnIndexes();

                // 先清除现有数据绑定
                dataGridView.DataSource = null;
                dataGridView.Columns.Clear();
                
                _logger?.LogDebug($"清除后: 列数={dataGridView.Columns.Count}, 行数={dataGridView.Rows.Count}");

                // 设置自动生成功能
                dataGridView.AutoGenerateColumns = true;
                
                // 直接绑定数据源
                dataGridView.DataSource = _importedData;

                // 强制刷新DataGridView以确保数据绑定完成
                dataGridView.Refresh();
                Application.DoEvents();
                
                // 确保数据源已正确绑定
                if (dataGridView.DataSource != _importedData)
                {
                    dataGridView.DataSource = _importedData;
                }
                
                _logger?.LogDebug($"绑定后: 列数={dataGridView.Columns.Count}, 行数={dataGridView.Rows.Count}");

                // 再次验证并调整列索引，确保与实际DataGridView的列数匹配
                if (dataGridView.Columns.Count > 0)
                {
                    int totalColumns = dataGridView.Columns.Count;
                    
                    // 确保搜索列索引有效
                    if (_searchColumnIndex < 0 || _searchColumnIndex >= totalColumns)
                    {
                        _searchColumnIndex = 0;
                        _logger?.LogDebug("再次调整搜索列索引为: 0");
                    }

                    // 确保数量列索引有效
                    if (_returnColumnIndex < 0 || _returnColumnIndex >= totalColumns)
                    {
                        _returnColumnIndex = totalColumns > 1 ? 1 : 0;
                        _logger?.LogDebug($"再次调整数量列索引为: {_returnColumnIndex}");
                    }
                    
                    // 确保序号列索引有效
                    if (_serialColumnIndex >= totalColumns)
                    {
                        _serialColumnIndex = -1; // 设为无效值
                        _logger?.LogDebug($"调整序号列索引为: {_serialColumnIndex}");
                    }
                }

                // 配置DataGridView的列显示
                ConfigureDataGridViewColumns(dataGridView);

                // 调整列宽
                AdjustColumnWidths(dataGridView);

                // 添加调整事件
                dataGridView.Resize -= DataGridView_Resize;
                dataGridView.Resize += DataGridView_Resize;
                
                // 强制刷新DataGridView以确保所有更改生效
                dataGridView.Refresh();
                Application.DoEvents();
                
                // 检查最终可见列数和数据行数
                int finalVisibleColumns = dataGridView.Columns.Cast<DataGridViewColumn>().Count(c => c.Visible);
                int rowCount = dataGridView.Rows.Count;
                _logger?.LogInformation($"DisplayImportedData完成: 可见列数={finalVisibleColumns}, 数据行数={rowCount}");
                
                // 最终刷新确保数据显示
                dataGridView.Refresh();
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "显示导入数据时发生错误");
                MessageBox.Show("显示导入数据时发生错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // 尝试备选方案显示数据
                try
                {
                    _logger?.LogInformation("尝试备选方案显示导入数据");
                    
                    // 清除现有绑定
                    dataGridView.DataSource = null;
                    dataGridView.Columns.Clear();
                    
                    // 重新绑定数据
                    dataGridView.AutoGenerateColumns = true;
                    dataGridView.DataSource = _importedData;
                    
                    // 显示所有列
                    foreach (DataGridViewColumn column in dataGridView.Columns)
                    {
                        column.Visible = true;
                    }
                    
                    // 设置默认列宽
                    dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                    
                    // 最终刷新
                    dataGridView.Refresh();
                    Application.DoEvents();
                    
                    _logger?.LogInformation("备选方案显示导入数据完成");
                }
                catch (Exception fallbackEx)
                {
                    _logger?.LogError(fallbackEx, "备选方案显示导入数据时发生错误");
                    MessageBox.Show("无法显示导入的数据，请检查Excel文件格式。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 配置DataGridView的列显示
        /// </summary>
        /// <param name="dataGridView">目标DataGridView控件</param>
        private void ConfigureDataGridViewColumns(DataGridView dataGridView)
        {
            if (dataGridView == null || dataGridView.Columns.Count == 0)
            {
                _logger?.LogDebug("ConfigureDataGridViewColumns: DataGridView为空或没有列");
                return;
            }

            _logger?.LogDebug($"ConfigureDataGridViewColumns: 总列数={dataGridView.Columns.Count}, 搜索列索引={_searchColumnIndex}, 返回列索引={_returnColumnIndex}, 序号列索引={_serialColumnIndex}");

            // 保存之前的可见列数用于验证
            int visibleColumnsBefore = dataGridView.Columns.Cast<DataGridViewColumn>().Count(c => c.Visible);
            _logger?.LogDebug($"配置前可见列数: {visibleColumnsBefore}");

            // 隐藏所有列
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.Visible = false;
            }

            // 检查隐藏后可见列数
            int visibleColumnsAfterHide = dataGridView.Columns.Cast<DataGridViewColumn>().Count(c => c.Visible);
            _logger?.LogDebug($"隐藏所有列后可见列数: {visibleColumnsAfterHide}");

            // 显示搜索列
            if (_searchColumnIndex >= 0 && _searchColumnIndex < dataGridView.Columns.Count)
            {
                dataGridView.Columns[_searchColumnIndex].Visible = true;
                _logger?.LogDebug($"显示搜索列: 索引={_searchColumnIndex}, 列名={dataGridView.Columns[_searchColumnIndex].HeaderText}, 数据类型={dataGridView.Columns[_searchColumnIndex].ValueType}");
            }
            else
            {
                _logger?.LogDebug($"无效的搜索列索引: {_searchColumnIndex}");
            }

            // 显示数量列
            if (_returnColumnIndex >= 0 && _returnColumnIndex < dataGridView.Columns.Count)
            {
                dataGridView.Columns[_returnColumnIndex].Visible = true;
                _logger?.LogDebug($"显示数量列: 索引={_returnColumnIndex}, 列名={dataGridView.Columns[_returnColumnIndex].HeaderText}, 数据类型={dataGridView.Columns[_returnColumnIndex].ValueType}");
            }
            else
            {
                _logger?.LogDebug($"无效的数量列索引: {_returnColumnIndex}");
            }

            // 显示序号列
            if (_serialColumnIndex >= 0 && _serialColumnIndex < dataGridView.Columns.Count)
            {
                dataGridView.Columns[_serialColumnIndex].Visible = true;
                _logger?.LogDebug($"显示序号列: 索引={_serialColumnIndex}, 列名={dataGridView.Columns[_serialColumnIndex].HeaderText}, 数据类型={dataGridView.Columns[_serialColumnIndex].ValueType}");
            }
            else if (_serialColumnIndex != -1)
            {
                _logger?.LogDebug($"无效的序号列索引: {_serialColumnIndex}");
            }

            // 检查设置后可见列数
            int visibleColumnsAfterShow = dataGridView.Columns.Cast<DataGridViewColumn>().Count(c => c.Visible);
            _logger?.LogDebug($"设置后可见列数: {visibleColumnsAfterShow}");

            // 如果设置后没有可见列，显示所有列作为备选方案
            if (visibleColumnsAfterShow == 0)
            {
                _logger?.LogWarning("警告：没有可见列，将显示所有列作为备选方案");
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    column.Visible = true;
                }
                
                // 再次检查可见列数
                visibleColumnsAfterShow = dataGridView.Columns.Cast<DataGridViewColumn>().Count(c => c.Visible);
                _logger?.LogDebug($"备选方案后可见列数: {visibleColumnsAfterShow}");
            }
        }

        /// <summary>
        /// 调整DataGridView的列宽
        /// </summary>
        /// <param name="dataGridView">目标DataGridView控件</param>
        public void AdjustColumnWidths(DataGridView dataGridView)
        {
            if (dataGridView == null || dataGridView.Columns.Count == 0)
            {
                _logger?.LogDebug("AdjustColumnWidths: DataGridView为空或没有列");
                return;
            }

            try
            {
                _logger?.LogDebug("开始调整列宽");
                
                // 判断是否显示垂直滚动条
                bool hasVerticalScrollBar = dataGridView.DisplayRectangle.Height > dataGridView.ClientSize.Height;
                int scrollBarWidth = hasVerticalScrollBar ? SystemInformation.VerticalScrollBarWidth : 0;
                
                // 设置列自动填充模式
                dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // 获取可见列
                var visibleColumns = dataGridView.Columns.Cast<DataGridViewColumn>().Where(c => c.Visible).ToList();

                // 根据可见列数量设置不同的权重比
                if (visibleColumns.Count >= 3)
                {
                    // 对于3列或更多列，使用25-50-25的权重分布
                    visibleColumns[0].FillWeight = 25;
                    visibleColumns[1].FillWeight = 50;
                    visibleColumns[2].FillWeight = 25;
                    
                    // 对于更多列，平均分配剩余列的权重
                    if (visibleColumns.Count > 3)
                    {
                        float remainingWeight = 25f / (visibleColumns.Count - 3);
                        for (int i = 3; i < visibleColumns.Count; i++)
                        {
                            visibleColumns[i].FillWeight = remainingWeight;
                        }
                    }
                }
                else if (visibleColumns.Count == 2)
                {
                    // 对于2列，使用70-30的权重分布
                    visibleColumns[0].FillWeight = 70;
                    visibleColumns[1].FillWeight = 30;
                }
                else if (visibleColumns.Count == 1)
                {
                    // 对于1列，使用100%的权重
                    visibleColumns[0].FillWeight = 100;
                }
                
                _logger?.LogDebug($"列宽调整完成，可见列数: {visibleColumns.Count}");
                
                // 强制刷新DataGridView以确保列宽调整生效
                dataGridView.Refresh();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "调整列宽时发生错误");
            }
        }

        /// <summary>
        /// DataGridView调整大小时的事件处理
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void DataGridView_Resize(object sender, EventArgs e)
        {
            if (sender is DataGridView dataGridView)
            {
                _logger?.LogDebug("DataGridView大小调整事件触发");
                AdjustColumnWidths(dataGridView);
            }
        }

        /// <summary>
        /// 获取列标题，如果为空则使用默认标题
        /// </summary>
        /// <param name="dataGridView">DataGridView控件</param>
        /// <param name="columnIndex">列索引</param>
        /// <returns>列标题</returns>
        public string GetColumnHeader(DataGridView dataGridView, int columnIndex)
        {
            if (dataGridView == null || columnIndex < 0 || columnIndex >= dataGridView.Columns.Count)
            {
                _logger?.LogDebug($"无效的列索引: {columnIndex}");
                return "未命名列";
            }

            string headerText = dataGridView.Columns[columnIndex].HeaderText;
            string result = string.IsNullOrEmpty(headerText) ? "未命名列" : headerText;
            _logger?.LogDebug($"获取列标题，索引: {columnIndex}, 标题: {result}");
            return result;
        }

        /// <summary>
        /// 清空导入数据
        /// </summary>
        public void ClearData()
        {
            _importedData = null;
            _searchColumnIndex = -1;
            _returnColumnIndex = -1;
            _serialColumnIndex = -1;
            _materialColumnIndex = -1;
            _logger?.LogInformation("清空导入数据");
        }

        /// <summary>
        /// 检查是否有有效的导入数据
        /// </summary>
        /// <returns>是否有有效数据</returns>
        public bool HasValidData()
        {
            bool result = _importedData != null && _importedData.Rows.Count > 0;
            _logger?.LogDebug($"检查是否有有效数据: {result}");
            return result;
        }

        /// <summary>
        /// 获取导入数据的信息摘要
        /// </summary>
        /// <returns>数据摘要信息，如果没有数据则返回空字符串</returns>
        public string GetDataSummary()
        {
            _logger?.LogDebug("获取数据摘要信息");
            
            if (!HasValidData())
            {
                _logger?.LogDebug("没有有效数据");
                return string.Empty;
            }

            string summary = $"导入数据: {_importedData.Rows.Count}行 × {_importedData.Columns.Count}列";
            
            // 记录数据摘要信息到日志
            _logger?.LogInformation($"数据摘要: {summary}");
            
            return summary;
        }
        
        /// <summary>
        /// 获取导入的数据行数
        /// </summary>
        /// <returns>数据行数</returns>
        public int GetDataRowCount()
        {
            int count = _importedData?.Rows.Count ?? 0;
            _logger?.LogDebug($"获取数据行数: {count}");
            return count;
        }
        
        /// <summary>
        /// 获取导入的数据列数
        /// </summary>
        /// <returns>数据列数</returns>
        public int GetDataColumnCount()
        {
            int count = _importedData?.Columns.Count ?? 0;
            _logger?.LogDebug($"获取数据列数: {count}");
            return count;
        }
        
        /// <summary>
        /// 获取指定行和列的单元格值
        /// </summary>
        /// <param name="rowIndex">行索引</param>
        /// <param name="columnIndex">列索引</param>
        /// <returns>单元格值</returns>
        public object GetCellValue(int rowIndex, int columnIndex)
        {
            if (_importedData == null || rowIndex < 0 || rowIndex >= _importedData.Rows.Count || 
                columnIndex < 0 || columnIndex >= _importedData.Columns.Count)
            {
                _logger?.LogDebug($"无效的单元格索引，行: {rowIndex}, 列: {columnIndex}");
                return null;
            }

            object value = _importedData.Rows[rowIndex][columnIndex];
            _logger?.LogDebug($"获取单元格值，行: {rowIndex}, 列: {columnIndex}, 值: {value}");
            return value;
        }
    }
}