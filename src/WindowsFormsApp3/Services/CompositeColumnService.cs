using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Forms.Dialogs;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 列组合服务的具体实现
    /// </summary>
    public class CompositeColumnService : ICompositeColumnService
    {
        /// <summary>
        /// 获取列组合值
        /// </summary>
        public string GetCompositeColumnValue(DataRow row, List<string> selectedColumns, string separator)
        {
            if (row == null || selectedColumns == null || selectedColumns.Count == 0)
                return string.Empty;

            List<string> values = new List<string>();

            // 遍历选中的列，获取对应的值
            foreach (string columnName in selectedColumns)
            {
                // 在Excel数据表中查找对应的列
                for (int i = 0; i < row.Table.Columns.Count; i++)
                {
                    if (row.Table.Columns[i].ColumnName == columnName && row[i] != null)
                    {
                        string value = row[i].ToString();
                        if (!string.IsNullOrEmpty(value))
                        {
                            values.Add(value);
                        }
                        break;
                    }
                }
            }

            // 用分隔符连接所有值
            return string.Join(separator, values);
        }

        /// <summary>
        /// 为数据表格添加列组合列
        /// </summary>
        public DataTable AddCompositeColumnToDataTable(DataTable dataTable, List<string> selectedColumns, string separator)
        {
            if (dataTable == null)
                return null;

            // 创建新的数据表以包含组合列
            DataTable tableWithCompositeColumn = dataTable.Clone();

            // 添加列组合列
            tableWithCompositeColumn.Columns.Add("列组合", typeof(string));

            // 处理每一行数据
            foreach (DataRow row in dataTable.Rows)
            {
                DataRow newRow = tableWithCompositeColumn.NewRow();

                // 复制原始数据
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    newRow[i] = row[i];
                }

                // 计算组合列值
                string compositeValue = GetCompositeColumnValue(row, selectedColumns, separator);
                newRow["列组合"] = compositeValue;

                tableWithCompositeColumn.Rows.Add(newRow);
            }

            return tableWithCompositeColumn;
        }

        /// <summary>
        /// 保存列组合设置
        /// </summary>
        public void SaveCompositeColumnSettings(List<string> selectedColumns, string separator)
        {
            try
            {
                // 保存选中的列（使用分隔符连接）
                AppSettings.CompositeColumns = string.Join("|", selectedColumns ?? new List<string>());

                // 保存分隔符
                AppSettings.CompositeColumnSeparator = separator;

                // 持久化保存
                AppSettings.Save();
            }
            catch
            {
                // 错误处理，可以记录日志
            }
        }

        /// <summary>
        /// 加载列组合设置
        /// </summary>
        public (List<string> selectedColumns, string separator) LoadCompositeColumnSettings()
        {
            try
            {
                // 加载选中的列
                List<string> selectedColumns = new List<string>();
                if (!string.IsNullOrEmpty(AppSettings.CompositeColumns))
                {
                    selectedColumns.AddRange(AppSettings.CompositeColumns.Split('|'));
                }

                // 加载分隔符
                string separator = AppSettings.CompositeColumnSeparator ?? ",";
                
                return (selectedColumns, separator);
            }
            catch
            {
                // 错误处理，返回默认值
                return (new List<string>(), ",");
            }
        }
        
        /// <summary>
        /// 获取组合列设置
        /// </summary>
        public (List<string> selectedColumns, string separator) GetCompositeColumnSettings()
        {
            // 直接调用LoadCompositeColumnSettings方法获取设置
            return LoadCompositeColumnSettings();
        }
        
        /// <summary>
        /// 为指定行获取组合列值（使用服务中保存的设置）
        /// </summary>
        public string GetCompositeColumnValueForRow(DataRow row)
        {
            try
            {
                // 获取保存的列组合设置
                (List<string> selectedColumns, string separator) = LoadCompositeColumnSettings();
                
                LogHelper.Debug($"[GetCompositeColumnValueForRow] 加载的selectedColumns数量: {selectedColumns?.Count ?? 0}");
                if (selectedColumns != null && selectedColumns.Count > 0)
                {
                    LogHelper.Debug($"[GetCompositeColumnValueForRow] selectedColumns内容: {string.Join(", ", selectedColumns)}");
                }
                
                // 使用设置调用现有的GetCompositeColumnValue方法
                string result = GetCompositeColumnValue(row, selectedColumns, separator);
                LogHelper.Debug($"[GetCompositeColumnValueForRow] GetCompositeColumnValue返回: '{result}'");
                
                // 如果没有选中任何列，尝试从行列数提取
                if (string.IsNullOrEmpty(result) && (selectedColumns == null || selectedColumns.Count == 0))
                {
                    LogHelper.Debug($"[GetCompositeColumnValueForRow] 没有选中任何列，尝试从行列数提取");
                    // 尝试从DataRow中查找行数和列数列
                    string layoutRows = string.Empty;
                    string layoutColumns = string.Empty;
                    
                    // 查找行数列
                    if (row.Table.Columns.Contains("行数"))
                    {
                        layoutRows = row["行数"]?.ToString() ?? string.Empty;
                        LogHelper.Debug($"[GetCompositeColumnValueForRow] 找到行数列，值: '{layoutRows}'");
                    }
                    
                    // 查找列数列
                    if (row.Table.Columns.Contains("列数"))
                    {
                        layoutColumns = row["列数"]?.ToString() ?? string.Empty;
                        LogHelper.Debug($"[GetCompositeColumnValueForRow] 找到列数列，值: '{layoutColumns}'");
                    }
                    
                    // 如果有行数或列数，组合成列组合值
                    if (!string.IsNullOrEmpty(layoutRows) || !string.IsNullOrEmpty(layoutColumns))
                    {
                        result = $"{layoutRows}{separator}{layoutColumns}".Trim(new[] { separator[0] });
                        LogHelper.Debug($"[GetCompositeColumnValueForRow] 使用行列数生成的列组合值: '{result}'");
                    }
                    else
                    {
                        LogHelper.Debug($"[GetCompositeColumnValueForRow] 没有找到行数和列数列");
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                // 错误处理，返回空字符串
                LogHelper.Error($"[GetCompositeColumnValueForRow] 异常: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 检查列组合功能是否启用
        /// </summary>
        public bool IsCompositeColumnFeatureEnabled()
        {
            try
            {
                // 首先检查EventGroup配置中列组合项是否启用
                var eventGroupConfig = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (eventGroupConfig?.Items != null)
                {
                    var compositeColumnItem = eventGroupConfig.Items.FirstOrDefault(item => item.Name == "列组合");
                    if (compositeColumnItem != null)
                    {
                        LogHelper.Debug($"[IsCompositeColumnFeatureEnabled] 从EventGroup配置找到列组合项，启用状态: {compositeColumnItem.IsEnabled}");
                        return compositeColumnItem.IsEnabled;
                    }
                }
                
                // 如果EventGroup配置中没有找到，检查旧的EventItems配置（向后兼容）
                string savedItems = AppSettings.EventItems;
                if (!string.IsNullOrEmpty(savedItems))
                {
                    string[] items = savedItems.Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                    
                    // 确保数组长度为偶数（每项+状态）
                    if (items.Length % 2 == 0)
                    {
                        for (int i = 0; i < items.Length; i += 2)
                        {
                            string text = items[i];
                            // 使用TryParse处理可能无效的布尔值字符串
                            bool isChecked = false;
                            bool.TryParse(items[i + 1], out isChecked);
                            
                            if (text == "列组合")
                            {
                                LogHelper.Debug($"[IsCompositeColumnFeatureEnabled] 从EventItems配置找到列组合项，启用状态: {isChecked}");
                                return isChecked;
                            }
                        }
                    }
                }
                
                LogHelper.Debug("[IsCompositeColumnFeatureEnabled] 未找到列组合配置，返回false");
                return false;
            }
            catch (Exception ex)
            {
                // 错误处理，返回默认值
                LogHelper.Error($"[IsCompositeColumnFeatureEnabled] 异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 启用或禁用列组合功能
        /// </summary>
        public void SetCompositeColumnFeatureEnabled(bool enabled)
        {
            try
            {
                // 从应用设置加载保存的项
                string savedItems = AppSettings.EventItems;
                List<string> itemsList = new List<string>();
                bool found = false;
                
                if (!string.IsNullOrEmpty(savedItems))
                {
                    string[] items = savedItems.Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);
                    itemsList.AddRange(items);
                }
                
                // 确保数组长度为偶数
                if (itemsList.Count % 2 != 0)
                {
                    itemsList.Add("False");
                }
                
                // 查找并更新列组合功能的状态
                for (int i = 0; i < itemsList.Count; i += 2)
                {
                    if (itemsList[i] == "列组合")
                    {
                        itemsList[i + 1] = enabled.ToString();
                        found = true;
                        break;
                    }
                }
                
                // 如果没有找到列组合功能，添加它
                if (!found)
                {
                    itemsList.Add("列组合");
                    itemsList.Add(enabled.ToString());
                }
                
                // 保存更新后的设置
                AppSettings.EventItems = string.Join("|", itemsList);
                AppSettings.Save();
            }
            catch
            {
                // 错误处理，可以记录日志
            }
        }
    }
}