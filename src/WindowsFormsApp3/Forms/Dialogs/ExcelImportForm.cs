using System;
using System.Data;
using System.Windows.Forms;
using OfficeOpenXml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApp3.Properties;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Forms.Main;

namespace WindowsFormsApp3
{

    /// <summary>
    /// Excel 导入表单
    /// 支持独立对话框模式和嵌入面板模式
    /// </summary>
    public partial class ExcelImportForm : Form, IExcelImportForm
    {
        public DataTable ImportedData { get; private set; }
        /// <summary>
        /// 是否嵌入在面板中运行
        /// </summary>
        public bool IsEmbedded { get; set; } = false;

        /// <summary>
        /// 数据导入完成事件（嵌入模式下使用）
        /// </summary>
        public event EventHandler<DataTable> OnDataImported;

        public int SearchColumnIndex { get; private set; }
        /// <summary>
        /// 获取或设置数量列的索引
        /// 用于存储和显示数量数据，以便后续操作中明确区分数量信息
        /// </summary>
        public int ReturnColumnIndex { get; private set; }
        public int NewColumnIndex { get; private set; } // 序号列索引
        public string SelectedRegexPattern { get; private set; } // 选中的正则表达式模式

        
        // ...
        

        private Dictionary<string, string> regexPatterns = new Dictionary<string, string>(); // 正则表达式模式集合
        private IExcelParentView parentView; // 父视图接口（解耦 Form1 依赖）

        // 添加缺失的属性
        public string CornerRadius { get; set; }
        public bool UsePdfLastPage { get; set; }

        public bool AddPdfLayers { get; set; }

        // 列组合相关属性
        public List<string> SelectedCompositeColumns { get; set; }
        public string CompositeColumnSeparator { get; set; }
        private ICompositeColumnService _compositeColumnService;

        // 公开属性，用于直接访问cmbRegex2控件
        public ComboBox RegexComboBox
        {
            get { return cmbRegex2; }
        }

        public ExcelImportForm(IExcelParentView parent = null)
        {
            parentView = parent;
            InitializeComponent();
            ImportedData = new DataTable();
            
            // 获取列组合服务
            _compositeColumnService = ServiceLocator.Instance.GetCompositeColumnService();
            
            // 初始化列组合相关属性
            SelectedCompositeColumns = new List<string>();
            CompositeColumnSeparator = ",";
            LoadUserSettings();
            
            // 设置快捷键
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void LoadUserSettings()
        {
            txtNewColumnParams.Text = AppSettings.ExcelSerialColumnParams;
            txtSearchColumnParams.Text = AppSettings.ExcelSearchColumnParams;
            txtReturnColumnParams.Text = AppSettings.ExcelReturnColumnParams;
            
            // 使用列组合服务加载设置
            (List<string> columns, string separator) = _compositeColumnService.LoadCompositeColumnSettings();
            if (columns != null)
            {
                SelectedCompositeColumns = columns;
            }
            if (!string.IsNullOrEmpty(separator))
            {
                CompositeColumnSeparator = separator;
                txtCompositeSeparator.Text = CompositeColumnSeparator;
            }
            
            LoadRegexPatterns();
            UpdateRegexComboBox();
        }

        public ExcelImportForm(string filePath, IExcelParentView parent = null) : this(parent)
        {
            // 只有在提供了有效的文件路径时才加载数据
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                LoadExcelData(filePath);
            }
        }

        private void LoadExcelData(string filePath)
        {
            try
            {
                // 设置EPPlus许可证上下文
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        MessageBox.Show("Excel文件中没有工作表", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    // 安全地获取第一个工作表，解决"Worksheet position out of range"错误
                    var worksheet = package.Workbook.Worksheets[0];
                    ImportedData = new DataTable();

                    // 读取列头
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        var columnName = worksheet.Cells[1, col].Text;
                        ImportedData.Columns.Add(columnName);
                    }

                    // 读取数据行
                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var dataRow = ImportedData.NewRow();
                        for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                        {
                            dataRow[col - 1] = worksheet.Cells[row, col].Text;
                        }
                        ImportedData.Rows.Add(dataRow);
                    }

                    dgvPreview.DataSource = ImportedData;

                // 移除列选择复选框，改为全量显示所有列
            foreach (DataGridViewColumn column in dgvPreview.Columns) {
                column.Visible = true;
            }

                    LoadColumnComboBoxes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载Excel文件失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadRegexPatterns()
        {
            // 从配置加载正则表达式
            string savedPatterns = AppSettings.RegexPatterns;
            regexPatterns.Clear();
            if (!string.IsNullOrEmpty(savedPatterns))
            {
                foreach (var item in savedPatterns.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = item.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        regexPatterns[parts[0]] = parts[1];
                    }
                }
            }
        }

        private void UpdateRegexComboBox()
        {
            // 清空并填充正则表达式下拉菜单
            cmbRegex2.Items.Clear();
            foreach (var name in regexPatterns.Keys)
            {
                cmbRegex2.Items.Add(name);
            }

            // 尝试选择最后使用的正则表达式
            string lastUsedRegex = AppSettings.Get("LastSelectedRegex2") as string;
            if (!string.IsNullOrEmpty(lastUsedRegex) && cmbRegex2.Items.Contains(lastUsedRegex))
            {
                cmbRegex2.SelectedItem = lastUsedRegex;
                SelectedRegexPattern = regexPatterns[lastUsedRegex];
            }
            else if (cmbRegex2.Items.Count > 0)
            {
                cmbRegex2.SelectedIndex = 0;
                SelectedRegexPattern = regexPatterns[cmbRegex2.SelectedItem.ToString()];
                // 保存首次选择的正则表达式
                AppSettings.Set("LastSelectedRegex2", cmbRegex2.SelectedItem.ToString());
                AppSettings.Save();
            }

            // 绑定选择变化事件
            cmbRegex2.SelectedIndexChanged -= cmbRegex2_SelectedIndexChanged;
            cmbRegex2.SelectedIndexChanged += cmbRegex2_SelectedIndexChanged;
        }

        private void cmbRegex2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbRegex2.SelectedItem != null)
            {
                string selectedPatternName = cmbRegex2.SelectedItem.ToString();
                if (regexPatterns.TryGetValue(selectedPatternName, out string pattern))
                {
                    SelectedRegexPattern = pattern;
                    // 保存最后选择的正则表达式
                AppSettings.Set("LastSelectedRegex2", selectedPatternName);
                AppSettings.Save();
                }
            }
        }

        private void LoadColumnComboBoxes()
        {
            cmbSearchColumn.Items.Clear();
            cmbReturnColumn.Items.Clear();
            cmbNewColumn.Items.Clear();
            clbCompositeColumns.Items.Clear(); // 清空组合列选择框
            foreach (DataColumn column in ImportedData.Columns)
            {
                cmbSearchColumn.Items.Add(column.ColumnName);
                cmbReturnColumn.Items.Add(column.ColumnName);
                cmbNewColumn.Items.Add(column.ColumnName);
                clbCompositeColumns.Items.Add(column.ColumnName); // 填充组合列选择框
            }

            // 自动选择序号列
            AutoSelectColumn(cmbNewColumn, txtNewColumnParams.Text);
            // 自动选择搜索列
            AutoSelectColumn(cmbSearchColumn, txtSearchColumnParams.Text);
            // 自动选择返回列
            AutoSelectColumn(cmbReturnColumn, txtReturnColumnParams.Text);
        }

        private void AutoSelectColumn(ComboBox comboBox, string paramsText)
        {
            if (string.IsNullOrWhiteSpace(paramsText) || comboBox.Items.Count == 0)
                return;

            string[] keywords = paramsText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string keyword in keywords)
            {
                string trimmedKeyword = keyword.Trim();
                foreach (var item in comboBox.Items)
                {
                    string columnName = item.ToString();
                    if (columnName.IndexOf(trimmedKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        comboBox.SelectedItem = item;
                        return;
                    }
                }
            }

            // 如果没有匹配项，默认选择第一项
            comboBox.SelectedIndex = 0;
        }

        // 全选按钮点击事件
        private void btnSelectAllColumns_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbCompositeColumns.Items.Count; i++)
            {
                clbCompositeColumns.SetItemChecked(i, true);
            }
        }

        // 取消全选按钮点击事件
        private void btnClearAllColumns_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbCompositeColumns.Items.Count; i++)
            {
                clbCompositeColumns.SetItemChecked(i, false);
            }
        }

        // 已移除clbColumns控件，此方法不再需要
        // private void ClbColumns_ItemCheck(object sender, ItemCheckEventArgs e)
        // {
        // }

        // 已移除btnSelectAll按钮，此方法不再需要
        // private void BtnSelectAll_Click(object sender, EventArgs e)
        // {
        // }

        // 已移除btnInvertSelection按钮，此方法不再需要
        // private void BtnInvertSelection_Click(object sender, EventArgs e)
        // {
        // }

        // 已移除相关按钮，此方法不再需要
        // private void UpdateSelectAllButtonState()
        // {
        // }

        // 已移除clbColumns和btnSelectAll控件，以下方法不再需要
        // private void BtnSelectAll_Click(object sender, EventArgs e)
        // {
        // }
        
        // private void BtnInvertSelection_Click(object sender, EventArgs e)
        // {
        // }
        
        // private void UpdateSelectAllButtonState()
        // {
        // }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            if (ImportedData == null || ImportedData.Rows.Count == 0)
            {
                MessageBox.Show("没有可导入的数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 初始化全量列集合
            var checkedColumns = ImportedData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            // 序号列索引默认为-1（不导入）
            NewColumnIndex = -1;

            // 如果需要导入序号列
            if (chkImportSerialColumn.Checked)
            {
                if (cmbNewColumn.SelectedItem != null)
                {
                    string selectedColumnName = cmbNewColumn.SelectedItem.ToString();
                    NewColumnIndex = checkedColumns.IndexOf(selectedColumnName);
                }
                else
                {
                    NewColumnIndex = -1; // 未选择序号列
                }
            }

            if (checkedColumns.Count == 0)
            {
                MessageBox.Show("请至少选择一列进行导入", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 收集组合列选择
            SelectedCompositeColumns.Clear();
            foreach (object item in clbCompositeColumns.CheckedItems)
            {
                SelectedCompositeColumns.Add(item.ToString());
            }

            // 获取分隔符
            CompositeColumnSeparator = txtCompositeSeparator.Text;

            // 创建只包含选中列的新数据表
            DataTable selectedData = new DataTable();
            foreach (string colName in checkedColumns)
            {
                selectedData.Columns.Add(colName, ImportedData.Columns[colName].DataType);
            }

            foreach (DataRow row in ImportedData.Rows)
            {
                DataRow newRow = selectedData.NewRow();
                foreach (string colName in checkedColumns)
                {
                    newRow[colName] = row[colName];
                }
                selectedData.Rows.Add(newRow);
            }

            ImportedData = selectedData;

            // 保存当前选中的列名
            string selectedSearchColumn = cmbSearchColumn.SelectedItem?.ToString();
            string selectedReturnColumn = cmbReturnColumn.SelectedItem?.ToString();

            // 重新填充下拉框，只包含选中的列
            cmbSearchColumn.Items.Clear();
            cmbReturnColumn.Items.Clear();
            foreach (string colName in checkedColumns)
            {
                cmbSearchColumn.Items.Add(colName);
                cmbReturnColumn.Items.Add(colName);
            }

            // 尝试恢复之前的选择，如果不存在则选择第一个
            if (cmbSearchColumn.Items.Contains(selectedSearchColumn))
                cmbSearchColumn.SelectedItem = selectedSearchColumn;
            else if (cmbSearchColumn.Items.Count > 0)
                cmbSearchColumn.SelectedIndex = 0;

            if (cmbReturnColumn.Items.Contains(selectedReturnColumn))
                cmbReturnColumn.SelectedItem = selectedReturnColumn;
            else if (cmbReturnColumn.Items.Count > 0)
                cmbReturnColumn.SelectedIndex = 0;

            // 设置选中的搜索列和返回列索引
            SearchColumnIndex = cmbSearchColumn.SelectedIndex;
            ReturnColumnIndex = cmbReturnColumn.SelectedIndex;

            // 确保SelectedRegexPattern已设置
            if (cmbRegex2.SelectedItem != null)
            {
                string selectedPatternName = cmbRegex2.SelectedItem.ToString();
                if (regexPatterns.TryGetValue(selectedPatternName, out string pattern))
                {
                    SelectedRegexPattern = pattern;

                    // 通知父视图（解耦：通过接口而非直接依赖 Form1）
                    if (parentView != null)
                    {
                        parentView.UpdateStatus($"已选择正则表达式: {selectedPatternName}");
                    }
                }
            }

            // 保存用户预设参数
            AppSettings.ExcelSerialColumnParams = txtNewColumnParams.Text;
            AppSettings.ExcelSearchColumnParams = txtSearchColumnParams.Text;
            AppSettings.ExcelReturnColumnParams = txtReturnColumnParams.Text;
            
            // 使用列组合服务保存设置
            _compositeColumnService.SaveCompositeColumnSettings(SelectedCompositeColumns, CompositeColumnSeparator);
            
            AppSettings.Save();

            // 如果是嵌入模式，触发事件而不关闭
            if (IsEmbedded)
            {
                OnDataImported?.Invoke(this, ImportedData);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// 应用正则表达式到给定文本
        /// </summary>
        /// <param name="input">输入文本</param>
        /// <returns>匹配结果</returns>
        public string ApplyRegex(string input)
        {
            if (string.IsNullOrEmpty(SelectedRegexPattern) || string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            try
            {
                Match match = Regex.Match(input, SelectedRegexPattern);
                if (match.Success)
                {
                    if (match.Groups.Count > 1)
                    {
                        // 提取第一个捕获组的内容
                        return match.Groups[1].Value.Trim();
                    }
                    else
                    {
                        // 如果没有捕获组，使用整个匹配结果
                        return match.Value.Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                // 移除弹框提示，只记录日志
                // MessageBox.Show("正则表达式应用错误: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("正则表达式应用错误: " + ex.Message);
            }

            return string.Empty;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void ExcelImportForm_Load(object sender, EventArgs e)
        {
            // 如果已有数据，则重新加载列并应用预设参数
            if (ImportedData != null && ImportedData.Columns.Count > 0)
            {
                LoadColumnComboBoxes();
            }
        }
    }
}