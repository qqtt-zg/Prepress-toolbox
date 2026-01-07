using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.IO;
using System.ComponentModel;
using WindowsFormsApp3.Interfaces; // 修改为使用Interfaces命名空间的ILogger
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3
{
    /// <summary>
    /// DataGridView右键菜单管理器
    /// </summary>
    public class DgvContextMenu
    {
        private DataGridView _dgv;
        private ContextMenuStrip _contextMenu;
        private List<string> _materials; // 存储材料类别
        private string _currentColumnName; // 当前点击的列名
        private Interfaces.ILogger _logger; // 日志服务，明确指定使用Interfaces命名空间的ILogger
        private Func<bool> _isBatchModeFunc; // 用于检查是否为批量模式的函数

        // 构造函数
        public DgvContextMenu(DataGridView dgv, Interfaces.ILogger logger = null, Func<bool> isBatchModeFunc = null) // 明确指定使用Interfaces命名空间的ILogger
        {
            _dgv = dgv;
            _materials = new List<string>();
            _logger = logger;
            _isBatchModeFunc = isBatchModeFunc;

            // 记录DgvContextMenu初始化日志
            _logger?.LogInformation("DgvContextMenu初始化开始");
            _logger?.LogDebug($"Logger状态: {(_logger != null ? "已提供" : "未提供")}");

            InitializeContextMenu();
            // 监听单元格点击事件，用于更新右键菜单
            _dgv.CellClick += Dgv_CellClick;
            // 监听单元格鼠标按下事件，专门处理右键点击
            _dgv.CellMouseDown += Dgv_CellMouseDown;

            _logger?.LogInformation("DgvContextMenu初始化完成");
        }

        // 设置材料列表
        public void SetMaterials(List<string> materials)
        {
            _materials = materials ?? new List<string>();
            _logger?.LogInformation($"设置材料列表，共{_materials.Count}项");
        }

        // 单元格点击事件（左键点击处理）
        private void Dgv_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                // 确保点击的是有效单元格
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // 保存当前点击的列名
                    _currentColumnName = _dgv.Columns[e.ColumnIndex].Name;
                    _logger?.LogInformation($"单元格点击，列名: {_currentColumnName}, 行索引: {e.RowIndex}");
                    
                    // 根据列名创建相应的右键菜单
                    CreateContextMenuForColumn(_currentColumnName);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理单元格点击事件时发生错误");
                MessageBox.Show("处理点击事件时发生错误，请查看日志了解详情。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 单元格鼠标按下事件（专门处理右键点击）
        private void Dgv_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                // 确保点击的是右键且是有效单元格
                if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    // 保存当前点击的列名
                    _currentColumnName = _dgv.Columns[e.ColumnIndex].Name;
                    _logger?.LogInformation($"右键点击单元格，列名: {_currentColumnName}, 行索引: {e.RowIndex}, 列索引: {e.ColumnIndex}, ReadOnly: {_dgv.ReadOnly}, Enabled: {_dgv.Enabled}");

                    // 调试信息：输出DataGridView状态
                    _logger?.LogInformation($"DataGridView状态 - 行数: {_dgv.RowCount}, 列数: {_dgv.ColumnCount}, 选中行数: {_dgv.SelectedRows.Count}, 选中单元格数: {_dgv.SelectedCells.Count}");

                    // 根据列名创建相应的右键菜单
                    CreateContextMenuForColumn(_currentColumnName);

                    // 确保右键菜单显示
                    if (_contextMenu != null && _contextMenu.Items.Count > 0)
                    {
                        _logger?.LogInformation($"右键菜单已创建，包含 {_contextMenu.Items.Count} 个菜单项");
                    }
                    else
                    {
                        _logger?.LogWarning("右键菜单创建失败或为空");
                    }
                }
                else
                {
                    _logger?.LogDebug($"右键点击但条件不满足：Button={e.Button}, RowIndex={e.RowIndex}, ColumnIndex={e.ColumnIndex}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理单元格右键点击事件时发生错误");
                MessageBox.Show("处理右键点击事件时发生错误，请查看日志了解详情。", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 根据列名创建相应的右键菜单
        private void CreateContextMenuForColumn(string columnName)
        {
            try
            {
                _contextMenu.Items.Clear();

                // 根据列名选择创建相应的菜单
                switch (columnName)
                {
                    case "colMaterial":
                        CreateMaterialContextMenu();
                        break;
                    case "colQuantity":
                        CreateQuantityContextMenu();
                        break;
                    case "colOrderNumber":
                    case "colSerialNumber":
                        CreateOrderNumberContextMenu();
                        break;
                    case "colOriginalName":
                        CreateOriginalNameContextMenu();
                        break;
                    case "colDimensions":
                        CreateDimensionsContextMenu();
                        break;
                    default:
                        CreateDefaultContextMenu();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"为列 {columnName} 创建右键菜单时发生错误");
                // 出错时创建默认菜单
                CreateDefaultContextMenu();
            }
        }

        // 初始化右键菜单
        private void InitializeContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            CreateDefaultContextMenu();
            // 将右键菜单绑定到DataGridView
            _dgv.ContextMenuStrip = _contextMenu;
            _logger?.LogInformation("右键菜单初始化完成");
        }

        // 创建默认右键菜单
        private void CreateDefaultContextMenu()
        {
            try
            {
                _contextMenu.Items.Clear();

                // 添加菜单项
                ToolStripMenuItem copyItem = new ToolStripMenuItem("复制");
                copyItem.Click += CopyItem_Click;
                copyItem.ShortcutKeys = Keys.Control | Keys.C;
                _contextMenu.Items.Add(copyItem);

                ToolStripMenuItem cutItem = new ToolStripMenuItem("剪切");
                cutItem.Click += CutItem_Click;
                cutItem.ShortcutKeys = Keys.Control | Keys.X;
                _contextMenu.Items.Add(cutItem);

                ToolStripMenuItem pasteItem = new ToolStripMenuItem("粘贴");
                pasteItem.Click += PasteItem_Click;
                pasteItem.ShortcutKeys = Keys.Control | Keys.V;
                _contextMenu.Items.Add(pasteItem);

                ToolStripMenuItem deleteItem = new ToolStripMenuItem("删除");
                deleteItem.Click += DeleteItem_Click;
                _contextMenu.Items.Add(deleteItem);

                _contextMenu.Items.Add(new ToolStripSeparator());

                ToolStripMenuItem refreshItem = new ToolStripMenuItem("刷新");
                refreshItem.Click += RefreshItem_Click;
                _contextMenu.Items.Add(refreshItem);
                
                _logger?.LogInformation("默认右键菜单创建完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建默认右键菜单时发生错误");
                throw;
            }
        }

        // 创建材料列右键菜单
        private void CreateMaterialContextMenu()
        {
            try
            {
                _contextMenu.Items.Clear();

                // 添加材料类别菜单项
                if (_materials != null && _materials.Count > 0)
                {
                    foreach (string material in _materials)
                    {
                        ToolStripMenuItem materialItem = new ToolStripMenuItem(material);
                        materialItem.Click += (sender, e) =>
                        {
                            try
                            {
                                int updatedCount = 0;
                                // 填充所有选中的单元格
                                foreach (DataGridViewCell cell in _dgv.SelectedCells)
                                {
                                    if (cell.ColumnIndex == _dgv.Columns["colMaterial"].Index)
                                    {
                                        // 检查DataGridView是否绑定到BindingList
                                        if (_dgv.DataSource is BindingList<FileRenameInfo> bindingList)
                                        {
                                            // 通过BindingList更新数据，这样会自动同步到DataGridView
                                            if (cell.RowIndex >= 0 && cell.RowIndex < bindingList.Count)
                                            {
                                                var item = bindingList[cell.RowIndex];
                                                item.Material = material;
                                                updatedCount++;
                                            }
                                        }
                                        else
                                        {
                                            // 如果没有绑定到BindingList，直接设置单元格值
                                            cell.Value = material;
                                            updatedCount++;
                                        }
                                    }
                                }
                                _logger?.LogInformation($"设置材料为: {material}，更新了 {updatedCount} 个单元格");
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, $"设置材料 {material} 时发生错误");
                                MessageBox.Show($"设置材料时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        };
                        _contextMenu.Items.Add(materialItem);
                    }

                    _contextMenu.Items.Add(new ToolStripSeparator());
                }

                // 添加默认菜单项
                ToolStripMenuItem copyItem = new ToolStripMenuItem("复制");
                copyItem.Click += CopyItem_Click;
                _contextMenu.Items.Add(copyItem);

                ToolStripMenuItem refreshItem = new ToolStripMenuItem("刷新");
                refreshItem.Click += RefreshItem_Click;
                _contextMenu.Items.Add(refreshItem);
                
                _logger?.LogInformation("材料列右键菜单创建完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建材料列右键菜单时发生错误");
                // 出错时回退到默认菜单
                CreateDefaultContextMenu();
            }
        }

        // 创建数量列右键菜单
        private void CreateQuantityContextMenu()
        {
            try
            {
                _contextMenu.Items.Clear();

                // 添加手动输入菜单项
                ToolStripMenuItem manualInputItem = new ToolStripMenuItem("手动输入");
                manualInputItem.Click += (sender, e) =>
                {
                    try
                    {
                        ShowQuantityInputDialog();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "显示数量输入对话框时发生错误");
                        MessageBox.Show($"显示输入对话框时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                _contextMenu.Items.Add(manualInputItem);

                _contextMenu.Items.Add(new ToolStripSeparator());

                // 添加默认菜单项
                ToolStripMenuItem copyItem = new ToolStripMenuItem("复制");
                copyItem.Click += CopyItem_Click;
                _contextMenu.Items.Add(copyItem);

                ToolStripMenuItem refreshItem = new ToolStripMenuItem("刷新");
                refreshItem.Click += RefreshItem_Click;
                _contextMenu.Items.Add(refreshItem);
                
                _logger?.LogInformation("数量列右键菜单创建完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建数量列右键菜单时发生错误");
                // 出错时回退到默认菜单
                CreateDefaultContextMenu();
            }
        }

        // 显示数量输入对话框
        private void ShowQuantityInputDialog()
        {
            // 创建一个简单的输入对话框
            using (Form inputForm = new Form())
            {
                inputForm.Text = "手动输入增量值";
                inputForm.Width = 300;
                inputForm.Height = 150;
                inputForm.StartPosition = FormStartPosition.CenterScreen;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                // 添加标签
                Label label = new Label();
                label.Text = "请输入增量值(正数为增加，负数为减少):";
                label.Left = 10;
                label.Top = 10;
                label.Width = 260;
                inputForm.Controls.Add(label);

                // 添加文本框
                TextBox textBox = new TextBox();
                textBox.Left = 10;
                textBox.Top = 35;
                textBox.Width = 260;
                textBox.Text = "1";
                inputForm.Controls.Add(textBox);

                // 添加确认按钮
                Button okButton = new Button();
                okButton.Text = "确定";
                okButton.Left = 100;
                okButton.Top = 65;
                okButton.Click += (senderBtn, eBtn) =>
                {
                    try
                    {
                        // 尝试将输入的值转换为整数
                        if (int.TryParse(textBox.Text, out int delta))
                        {
                            // 批量更新选中的单元格的值
                            BatchUpdateQuantity(delta);
                            inputForm.Close();
                            _logger?.LogInformation($"批量更新数量，增量值: {delta}");
                        }
                        else
                        {
                            MessageBox.Show("请输入有效的整数!", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "处理数量输入时发生错误");
                        MessageBox.Show($"处理输入时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                inputForm.Controls.Add(okButton);

                // 显示对话框
                inputForm.ShowDialog();
            }
        }

        // 批量更新数量
        private void BatchUpdateQuantity(int delta)
        {
            try
            {
                int updatedCount = 0;
                foreach (DataGridViewCell cell in _dgv.SelectedCells)
                {
                    if (cell.ColumnIndex == _dgv.Columns["colQuantity"].Index)
                    {
                        try
                        {
                            int newValue;
                            // 尝试将单元格的值转换为整数
                            if (int.TryParse(cell.Value?.ToString() ?? "0", out int value))
                            {
                                // 更新值
                                newValue = value + delta;
                            }
                            else
                            {
                                // 如果转换失败，设置为delta
                                newValue = delta;
                            }

                            // 检查DataGridView是否绑定到BindingList
                            if (_dgv.DataSource is BindingList<FileRenameInfo> bindingList)
                            {
                                // 通过BindingList更新数据，这样会自动同步到DataGridView
                                if (cell.RowIndex >= 0 && cell.RowIndex < bindingList.Count)
                                {
                                    var item = bindingList[cell.RowIndex];
                                    item.Quantity = newValue.ToString();
                                    updatedCount++;
                                }
                            }
                            else
                            {
                                // 如果没有绑定到BindingList，直接设置单元格值
                                cell.Value = newValue.ToString();
                                updatedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, "更新单元格数量时发生错误");
                            // 继续处理其他单元格
                        }
                    }
                }
                _logger?.LogInformation($"批量更新数量完成，共更新{updatedCount}个单元格");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "批量更新数量时发生错误");
                MessageBox.Show($"批量更新数量时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 创建尺寸列右键菜单
        private void CreateDimensionsContextMenu()
        {
            try
            {
                _contextMenu.Items.Clear();

                // 添加设置出血值菜单项
                ToolStripMenuItem setBleedItem = new ToolStripMenuItem("设置出血值");
                setBleedItem.Click += (sender, e) =>
                {
                    try
                    {
                        _logger?.LogInformation("用户点击了设置出血值菜单项");
                        _logger?.LogInformation($"当前DataGridView状态 - 选中行数: {_dgv.SelectedRows.Count}, 选中单元格数: {_dgv.SelectedCells.Count}");

                        // 输出选中行的详细信息
                        foreach (DataGridViewRow row in _dgv.SelectedRows)
                        {
                            _logger?.LogInformation($"选中行 {row.Index}: {row.Cells["colOriginalName"]?.Value}");
                        }

                        ShowBleedInputDialog();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "显示出血值输入对话框时发生错误");
                        MessageBox.Show($"显示输入对话框时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                _contextMenu.Items.Add(setBleedItem);

                _contextMenu.Items.Add(new ToolStripSeparator());

                // 添加默认菜单项
                ToolStripMenuItem copyItem = new ToolStripMenuItem("复制");
                copyItem.Click += CopyItem_Click;
                _contextMenu.Items.Add(copyItem);

                ToolStripMenuItem refreshItem = new ToolStripMenuItem("刷新");
                refreshItem.Click += RefreshItem_Click;
                _contextMenu.Items.Add(refreshItem);
                
                _logger?.LogInformation("尺寸列右键菜单创建完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建尺寸列右键菜单时发生错误");
                // 出错时回退到默认菜单
                CreateDefaultContextMenu();
            }
        }

        // 显示出血值输入对话框
        private void ShowBleedInputDialog()
        {
            // 创建出血值输入对话框
            using (Form bleedForm = new Form())
            {
                bleedForm.Text = "设置出血值";
                bleedForm.Width = 300;
                bleedForm.Height = 180;
                bleedForm.StartPosition = FormStartPosition.CenterScreen;
                bleedForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                bleedForm.MaximizeBox = false;
                bleedForm.MinimizeBox = false;

                // 添加标签
                Label label = new Label();
                label.Text = "请输入出血值(mm):";
                label.Left = 10;
                label.Top = 10;
                label.Width = 260;
                bleedForm.Controls.Add(label);

                // 添加文本框
                TextBox textBox = new TextBox();
                textBox.Left = 10;
                textBox.Top = 40;
                textBox.Width = 260;
                // 批量模式下默认使用2，但允许修改
                if (_isBatchModeFunc != null && _isBatchModeFunc())
                {
                    textBox.Text = "2"; // 批量模式下默认出血值为2
                    _logger?.LogInformation("批量模式下出血值默认为2，允许用户修改");
                }
                else
                {
                    // 手动模式下优先使用临时设置，其次使用全局设置
                    string manualBleed = AppSettings.Get("ManualModeBleedValue")?.ToString();
                    if (!string.IsNullOrEmpty(manualBleed))
                    {
                        textBox.Text = manualBleed;
                        _logger?.LogInformation($"手动模式：加载临时出血值 {manualBleed}");
                    }
                    else
                    {
                        string currentBleed = AppSettings.Get("TetBleedValues")?.ToString() ?? "0";
                        textBox.Text = currentBleed.Split(',')[0]; // 取第一个出血值
                        _logger?.LogInformation($"手动模式：使用全局出血值 {textBox.Text}");
                    }
                }
                bleedForm.Controls.Add(textBox);

                // 添加确认按钮
                Button okButton = new Button();
                okButton.Text = "确定";
                okButton.Left = 60;
                okButton.Top = 75;
                okButton.Click += (senderBtn, eBtn) =>
                {
                    try
                    {
                        string bleedValue = textBox.Text.Trim();
                        if (string.IsNullOrEmpty(bleedValue))
                        {
                            MessageBox.Show("请输入出血值", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        if (!double.TryParse(bleedValue, out double bleed))
                        {
                            MessageBox.Show("请输入有效的数字", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // 区分手动模式和批量模式的出血值保存
                        if (_isBatchModeFunc != null && _isBatchModeFunc())
                        {
                            // 批量模式：保存到全局设置
                            AppSettings.Set("TetBleedValues", bleedValue);
                            AppSettings.Save();
                            _logger?.LogInformation($"批量模式：保存出血值 {bleed} 到全局设置");
                        }
                        else
                        {
                            // 手动模式：保存到临时设置，不影响全局TetBleedValues
                            AppSettings.Set("ManualModeBleedValue", bleedValue);
                            AppSettings.Save();
                            _logger?.LogInformation($"手动模式：保存出血值 {bleed} 到临时设置，不影响全局配置");
                        }

                        // 重新计算选中行的尺寸
                        RecalculateDimensionsForSelectedRows(bleed);

                        if (_isBatchModeFunc != null && _isBatchModeFunc())
                        {
                            _logger?.LogInformation($"批量模式：用户修改出血值为 {bleed}mm，已保存到设置");
                        }
                        else
                        {
                            _logger?.LogInformation($"手动模式：保存出血值 {bleed} 到设置");
                        }
                        bleedForm.Close();
                        _logger?.LogInformation($"设置出血值为: {bleed}mm (批量模式: {(_isBatchModeFunc != null && _isBatchModeFunc())})");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "处理出血值输入时发生错误");
                        MessageBox.Show($"处理输入时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                bleedForm.Controls.Add(okButton);

                // 添加取消按钮
                Button cancelButton = new Button();
                cancelButton.Text = "取消";
                cancelButton.Left = 150;
                cancelButton.Top = 75;
                cancelButton.Click += (senderBtn, eBtn) =>
                {
                    bleedForm.Close();
                };
                bleedForm.Controls.Add(cancelButton);

                // 显示对话框
                bleedForm.ShowDialog();
            }
        }

        // 重新计算选中行的尺寸
        private void RecalculateDimensionsForSelectedRows(double bleedValue)
        {
            try
            {
                _logger?.LogInformation($"开始重新计算尺寸，出血值: {bleedValue}mm");
                _logger?.LogInformation($"选中行数: {_dgv.SelectedRows.Count}, 选中单元格数: {_dgv.SelectedCells.Count}");

                // 获取需要处理的行索引集合（避免重复处理同一行）
                var rowIndexSet = new HashSet<int>();

                // 首先处理选中的行
                foreach (DataGridViewRow row in _dgv.SelectedRows)
                {
                    if (row.Index >= 0 && row.Index < _dgv.RowCount)
                    {
                        rowIndexSet.Add(row.Index);
                    }
                }

                // 然后处理选中的单元格（确保包含尺寸列的单元格）
                foreach (DataGridViewCell cell in _dgv.SelectedCells)
                {
                    if (cell.RowIndex >= 0 && cell.RowIndex < _dgv.RowCount)
                    {
                        rowIndexSet.Add(cell.RowIndex);
                    }
                }

                _logger?.LogInformation($"需要处理的行数: {rowIndexSet.Count}");
                if (rowIndexSet.Count == 0)
                {
                    MessageBox.Show("请先选择要更新的行或单元格。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int processedCount = 0;
                foreach (int rowIndex in rowIndexSet)
                {
                    DataGridViewRow row = _dgv.Rows[rowIndex];
                    _logger?.LogInformation($"正在处理行 {rowIndex}");

                    // 获取原文件名单元格值
                    DataGridViewCell originalNameCell = row.Cells["colOriginalName"];
                    if (originalNameCell == null || originalNameCell.Value == null)
                    {
                        _logger?.LogWarning($"行 {rowIndex} 没有原文件名信息");
                        continue;
                    }

                    string originalName = originalNameCell.Value.ToString();
                    if (string.IsNullOrEmpty(originalName))
                    {
                        _logger?.LogWarning($"行 {rowIndex} 原文件名为空");
                        continue;
                    }

                    try
                    {
                        // 获取完整文件路径
                        string fullPath = string.Empty;
                        FileRenameInfo fileInfo = null;

                        // 尝试从数据绑定获取完整路径和文件信息
                        if (_dgv.DataSource is BindingList<FileRenameInfo> dataSourceList)
                        {
                            if (rowIndex >= 0 && rowIndex < dataSourceList.Count)
                            {
                                fileInfo = dataSourceList[rowIndex];
                                fullPath = fileInfo.FullPath;
                            }
                        }

                        // 如果没有完整路径，尝试构造路径
                        if (string.IsNullOrEmpty(fullPath))
                        {
                            // 尝试从多个可能的源获取路径
                            if (fileInfo != null && !string.IsNullOrEmpty(fileInfo.OriginalName))
                            {
                                // 尝试当前目录
                                fullPath = Path.Combine(Directory.GetCurrentDirectory(), fileInfo.OriginalName);
                                if (!File.Exists(fullPath))
                                {
                                    // 尝试应用程序目录
                                    fullPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), fileInfo.OriginalName);
                                }
                            }
                            else
                            {
                                // 使用原文件名作为最后的选择
                                fullPath = Path.Combine(Directory.GetCurrentDirectory(), originalName);
                                if (!File.Exists(fullPath))
                                {
                                    fullPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), originalName);
                                }
                            }
                        }

                        // 验证文件是否存在
                        if (!File.Exists(fullPath))
                        {
                            _logger?.LogWarning($"文件不存在，无法重新计算尺寸: {fullPath}");
                            continue;
                        }

                        // 使用 DimensionCalculationService 替代 SettingsForm 直接实例化
                        string adjustedDimensions = string.Empty;
                        var dimensionService = ServiceLocator.Instance.GetDimensionCalculationService();
                        if (dimensionService.RecognizePdfDimensions(fullPath, out double pdfWidth, out double pdfHeight))
                        {
                            // 使用传入的出血值参数（批量模式下也使用用户设置或修改的值）
                            adjustedDimensions = dimensionService.CalculateFinalDimensions(pdfWidth, pdfHeight, bleedValue);
                            _logger?.LogInformation($"成功重新计算尺寸: {Path.GetFileName(fullPath)} -> {adjustedDimensions} (出血值: {bleedValue})");
                        }
                        else
                        {
                            _logger?.LogWarning($"无法获取PDF尺寸信息: {Path.GetFileName(fullPath)}");
                            continue;
                        }

                        // 更新尺寸列
                        DataGridViewCell dimensionsCell = row.Cells["colDimensions"];
                        if (dimensionsCell != null)
                        {
                            string oldValue = dimensionsCell.Value?.ToString() ?? "(空)";
                            _logger?.LogInformation($"正在更新行 {rowIndex} 的尺寸值: {oldValue} -> {adjustedDimensions}");

                            // 检查DataGridView是否绑定到BindingList
                            if (_dgv.DataSource is BindingList<FileRenameInfo> updateList)
                            {
                                // 通过BindingList更新数据，这样会自动同步到DataGridView
                                if (rowIndex >= 0 && rowIndex < updateList.Count)
                                {
                                    var item = updateList[rowIndex];
                                    item.Dimensions = adjustedDimensions;
                                    processedCount++;
                                    _logger?.LogInformation($"通过BindingList更新尺寸: 行{rowIndex}, {oldValue} -> {adjustedDimensions}");
                                }
                                else
                                {
                                    _logger?.LogWarning($"行索引超出范围: {rowIndex} >= {updateList.Count}");
                                }
                            }
                            else
                            {
                                // 如果没有绑定到BindingList，直接设置单元格值
                                dimensionsCell.Value = adjustedDimensions;
                                processedCount++;
                                _logger?.LogInformation($"直接设置尺寸值: 行{rowIndex}, {oldValue} -> {adjustedDimensions}");

                                // 立即提交编辑并刷新该行
                                _dgv.EndEdit();
                                _dgv.RefreshEdit();
                            }
                        }
                        else
                        {
                            _logger?.LogWarning($"无法找到尺寸列单元格，行 {rowIndex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"处理行 {rowIndex} 的尺寸计算时发生错误");
                        // 继续处理其他行
                    }
                }

                // 强制刷新DataGridView和绑定源
                _logger?.LogInformation("开始强制刷新DataGridView和绑定源");

                // 方法1：直接刷新DataGridView
                _dgv.Refresh();
                _dgv.Invalidate();
                _dgv.Update();

                if (_dgv.DataSource is BindingList<FileRenameInfo> bindingList)
                {
                    _logger?.LogInformation("触发BindingList更新通知");

                    // 方法2：重置绑定源
                    var dataSource = _dgv.DataSource;
                    _dgv.DataSource = null;
                    _dgv.DataSource = dataSource;

                    // 方法3：触发BindingList更新通知
                    var listChangedMethod = bindingList.GetType().GetMethod("OnListChanged",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (listChangedMethod != null)
                    {
                        listChangedMethod.Invoke(bindingList, new object[] {
                            new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.Reset, -1)
                        });
                        _logger?.LogInformation("BindingList更新通知已触发");
                    }
                    else
                    {
                        _logger?.LogWarning("无法找到BindingList的OnListChanged方法");
                    }

                    // 方法4：强制更新每个单元格
                    foreach (int rowIndex in rowIndexSet)
                    {
                        if (rowIndex < _dgv.RowCount && rowIndex < bindingList.Count)
                        {
                            var item = bindingList[rowIndex];
                            var dimensionsCell = _dgv.Rows[rowIndex].Cells["colDimensions"];
                            if (dimensionsCell != null)
                            {
                                dimensionsCell.Value = item.Dimensions;
                                _logger?.LogDebug($"强制更新单元格值: 行{rowIndex} = {item.Dimensions}");
                            }
                        }
                    }
                }
                else
                {
                    _logger?.LogInformation("DataGridView未绑定到BindingList");
                }

                // 验证更新结果
                _logger?.LogInformation("验证更新结果...");
                foreach (int rowIndex in rowIndexSet)
                {
                    if (rowIndex < _dgv.RowCount)
                    {
                        var dimensionsCell = _dgv.Rows[rowIndex].Cells["colDimensions"];
                        if (dimensionsCell != null)
                        {
                            _logger?.LogInformation($"行 {rowIndex} 尺寸值验证: {dimensionsCell.Value}");
                        }
                    }
                }

                _logger?.LogInformation($"重新计算尺寸完成，共处理{processedCount}行");
                // 移除弹框提示，操作结果通过日志记录
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "重新计算选中行尺寸时发生错误");
                MessageBox.Show($"重新计算尺寸时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 创建订单号列右键菜单
        private void CreateOrderNumberContextMenu()
        {
            try
            {
                _contextMenu.Items.Clear();

                // 添加设置订单号序列菜单项
                ToolStripMenuItem setSequenceItem = new ToolStripMenuItem("设置订单号序列");
                setSequenceItem.Click += (sender, e) =>
                {
                    try
                    {
                        ShowOrderNumberSequenceDialog();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "显示订单号序列对话框时发生错误");
                        MessageBox.Show($"显示对话框时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                _contextMenu.Items.Add(setSequenceItem);

                _contextMenu.Items.Add(new ToolStripSeparator());

                // 添加默认菜单项
                ToolStripMenuItem copyItem = new ToolStripMenuItem("复制");
                copyItem.Click += CopyItem_Click;
                _contextMenu.Items.Add(copyItem);

                ToolStripMenuItem refreshItem = new ToolStripMenuItem("刷新");
                refreshItem.Click += RefreshItem_Click;
                _contextMenu.Items.Add(refreshItem);
                
                _logger?.LogInformation("订单号列右键菜单创建完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建订单号列右键菜单时发生错误");
                // 出错时回退到默认菜单
                CreateDefaultContextMenu();
            }
        }

        // 显示订单号序列设置对话框
        private void ShowOrderNumberSequenceDialog()
        {
            // 创建订单号序列设置对话框
            using (Form sequenceForm = new Form())
            {
                sequenceForm.Text = "订单号序列设置";
                sequenceForm.Width = 350;
                sequenceForm.Height = 250;
                sequenceForm.StartPosition = FormStartPosition.CenterScreen;
                sequenceForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                sequenceForm.MaximizeBox = false;
                sequenceForm.MinimizeBox = false;

                // 前缀标签和文本框
                Label prefixLabel = new Label { Text = "前缀:", Left = 20, Top = 20, Width = 80 };
                TextBox prefixTextBox = new TextBox { Left = 110, Top = 20, Width = 200 };
                sequenceForm.Controls.Add(prefixLabel);
                sequenceForm.Controls.Add(prefixTextBox);

                // 后缀标签和文本框
                Label suffixLabel = new Label { Text = "后缀:", Left = 20, Top = 50, Width = 80 };
                TextBox suffixTextBox = new TextBox { Left = 110, Top = 50, Width = 200 };
                sequenceForm.Controls.Add(suffixLabel);
                sequenceForm.Controls.Add(suffixTextBox);

                // 开始值标签和文本框
                Label startLabel = new Label { Text = "开始值:", Left = 20, Top = 80, Width = 80 };
                TextBox startTextBox = new TextBox { Left = 110, Top = 80, Width = 200, Text = "1" };
                sequenceForm.Controls.Add(startLabel);
                sequenceForm.Controls.Add(startTextBox);

                // 增量标签和文本框
                Label incrementLabel = new Label { Text = "增量:", Left = 20, Top = 110, Width = 80 };
                TextBox incrementTextBox = new TextBox { Left = 110, Top = 110, Width = 200, Text = "1" };
                sequenceForm.Controls.Add(incrementLabel);
                sequenceForm.Controls.Add(incrementTextBox);

                // 确认按钮
                Button okButton = new Button
                {
                    Text = "确定",
                    Left = 100,
                    Top = 150,
                    Width = 75,
                    DialogResult = DialogResult.OK
                };
                sequenceForm.Controls.Add(okButton);

                // 取消按钮
                Button cancelButton = new Button
                {
                    Text = "取消",
                    Left = 185,
                    Top = 150,
                    Width = 75,
                    DialogResult = DialogResult.Cancel
                };
                sequenceForm.Controls.Add(cancelButton);

                // 设置默认按钮
                sequenceForm.AcceptButton = okButton;
                sequenceForm.CancelButton = cancelButton;

                // 显示对话框
                if (sequenceForm.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 获取输入值
                        string prefix = prefixTextBox.Text;
                        string suffix = suffixTextBox.Text;
                        if (!int.TryParse(startTextBox.Text, out int startValue))
                        {
                            MessageBox.Show("开始值必须是整数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        if (!int.TryParse(incrementTextBox.Text, out int increment))
                        {
                            MessageBox.Show("增量必须是整数", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // 应用订单号序列
                        ApplyOrderNumberSequence(prefix, suffix, startValue, increment);
                        _logger?.LogInformation($"应用订单号序列，前缀:{prefix}, 后缀:{suffix}, 开始值:{startValue}, 增量:{increment}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "处理订单号序列输入时发生错误");
                        MessageBox.Show($"处理输入时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // 为原文件名列创建右键菜单
        private void CreateOriginalNameContextMenu()
        {
            try
            {
                _contextMenu.Items.Clear();

                // 添加提取数字到数量列菜单项
                ToolStripMenuItem extractNumberItem = new ToolStripMenuItem("提取数字到数量列");
                extractNumberItem.Click += (sender, e) =>
                {
                    try
                    {
                        ShowExtractNumberDialog();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "显示提取数字对话框时发生错误");
                        MessageBox.Show($"显示对话框时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                _contextMenu.Items.Add(extractNumberItem);

                _contextMenu.Items.Add(new ToolStripSeparator());

                // 添加默认菜单项
                ToolStripMenuItem copyItem = new ToolStripMenuItem("复制");
                copyItem.Click += CopyItem_Click;
                _contextMenu.Items.Add(copyItem);

                ToolStripMenuItem refreshItem = new ToolStripMenuItem("刷新");
                refreshItem.Click += RefreshItem_Click;
                _contextMenu.Items.Add(refreshItem);
                
                _logger?.LogInformation("原文件名列右键菜单创建完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "创建原文件名列右键菜单时发生错误");
                // 出错时回退到默认菜单
                CreateDefaultContextMenu();
            }
        }

        // 显示提取数字对话框
        private void ShowExtractNumberDialog()
        {
            // 创建关键字输入对话框
            using (Form keywordForm = new Form())
            {
                keywordForm.Text = "提取数字到数量列"; 
                keywordForm.Width = 300;
                keywordForm.Height = 180;
                keywordForm.StartPosition = FormStartPosition.CenterScreen;
                keywordForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                keywordForm.MaximizeBox = false;
                keywordForm.MinimizeBox = false;

                // 添加标签
                Label label = new Label();
                label.Text = "请输入关键字(位于数字之后，可用逗号分隔多个):";
                label.Left = 10;
                label.Top = 10;
                label.Width = 260;
                label.AutoSize = true;
                keywordForm.Controls.Add(label);

                // 添加文本框
                TextBox textBox = new TextBox();
                textBox.Left = 10;
                textBox.Top = 40;
                textBox.Width = 260;
                // 加载上一次保存的关键字
                textBox.Text = AppSettings.ExtractNumberKeywords ?? "";
                keywordForm.Controls.Add(textBox);

                // 添加确认按钮
                Button okButton = new Button();
                okButton.Text = "确定";
                okButton.Left = 60;
                okButton.Top = 75;
                okButton.Click += (senderBtn, eBtn) =>
                {
                    try
                    {
                        string keywordsText = textBox.Text.Trim();
                        if (string.IsNullOrEmpty(keywordsText))
                        {
                            MessageBox.Show("请输入关键字", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        // 保存关键字到设置
                        AppSettings.ExtractNumberKeywords = keywordsText;
                        AppSettings.Save();

                        // 提取数字并填充到数量列
                        ExtractNumberToQuantityColumn(keywordsText);
                        keywordForm.Close();
                        _logger?.LogInformation($"提取数字关键字设置为: {keywordsText}");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "处理关键字输入时发生错误");
                        MessageBox.Show($"处理输入时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                keywordForm.Controls.Add(okButton);

                // 添加取消按钮
                Button cancelButton = new Button();
                cancelButton.Text = "取消";
                cancelButton.Left = 150;
                cancelButton.Top = 75;
                cancelButton.Click += (senderBtn, eBtn) =>
                {
                    keywordForm.Close();
                };
                keywordForm.Controls.Add(cancelButton);

                // 显示对话框
                keywordForm.ShowDialog();
            }
        }

        // 静态变量用于记住用户的选择
        private static bool _rememberChoice = false;
        private static string _lastSelectedOption = null;
        private static int _lastSelectedIndex = -1;

        // 从文件名提取数字到数量列（支持多个关键字）
        private void ExtractNumberToQuantityColumn(string keywordsText)
        {
            try
            {
                // 重置记忆选择，每次新操作都重新开始
                _rememberChoice = false;
                _lastSelectedOption = null;
                _lastSelectedIndex = -1;

                // 分割多个关键字
                string[] keywords = keywordsText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                               .Select(k => k.Trim())
                                               .Where(k => !string.IsNullOrEmpty(k))
                                               .ToArray();

                if (keywords.Length == 0)
                {
                    MessageBox.Show("请输入有效的关键字", "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 获取需要处理的行索引集合（兼容不同的选择模式）
                var rowIndexSet = new HashSet<int>();

                // 处理选中的行
                foreach (DataGridViewRow row in _dgv.SelectedRows)
                {
                    if (row.Index >= 0 && row.Index < _dgv.RowCount)
                    {
                        rowIndexSet.Add(row.Index);
                    }
                }

                // 处理选中的单元格
                foreach (DataGridViewCell cell in _dgv.SelectedCells)
                {
                    if (cell.RowIndex >= 0 && cell.RowIndex < _dgv.RowCount)
                    {
                        rowIndexSet.Add(cell.RowIndex);
                    }
                }

                _logger?.LogInformation($"数量提取：需要处理的行数: {rowIndexSet.Count}");
                if (rowIndexSet.Count == 0)
                {
                    MessageBox.Show("请先选择要处理的行或单元格。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int processedCount = 0;
                foreach (int rowIndex in rowIndexSet)
                {
                    DataGridViewRow row = _dgv.Rows[rowIndex];
                    if (row.Index < _dgv.RowCount)
                    {
                        // 获取原文件名单元格值
                        DataGridViewCell originalNameCell = row.Cells["colOriginalName"];
                        if (originalNameCell == null || originalNameCell.Value == null)
                        {
                            LogHelper.Debug("原文件名单元格为空或不存在");
                            continue;
                        }

                        string originalName = originalNameCell.Value.ToString();
                        if (string.IsNullOrEmpty(originalName))
                        {
                            LogHelper.Debug("原文件名为空");
                            continue;
                        }

                        List<string> numbers = new List<string>();
                        // 尝试每个关键字
                        foreach (string keyword in keywords)
                        {
                            string num = ExtractNumberBeforeKeyword(originalName, keyword);
                            if (!string.IsNullOrEmpty(num))
                            {
                                LogHelper.Debug("提取到数字: " + num + "，使用关键字: " + keyword);
                                // 如果返回的是多个数字（逗号分隔），则拆分成单个数字并添加
                                if (num.Contains(','))
                                {
                                    string[] multiNums = num.Split(',');
                                    foreach (string singleNum in multiNums)
                                    {
                                        if (!string.IsNullOrEmpty(singleNum.Trim()))
                                        {
                                            numbers.Add(singleNum.Trim());
                                        }
                                    }
                                }
                                else
                                {
                                    numbers.Add(num);
                                }
                            }
                        }

                        // 当提取出多个数值时，让用户选择保留哪一个
                        string finalNumber = null;
                        if (numbers.Count > 1 && !_rememberChoice)
                        {
                            // 创建选择对话框
                            using (Form selectForm = new Form())
                            {
                                selectForm.Text = "选择要保留的数量";
                                selectForm.Width = 300;
                                selectForm.Height = 250;
                                selectForm.StartPosition = FormStartPosition.CenterScreen;
                                selectForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                                selectForm.MaximizeBox = false;
                                selectForm.MinimizeBox = false;

                                // 添加标签
                                Label label = new Label();
                                label.Text = $"文件名: {originalName.Substring(0, Math.Min(originalName.Length, 30))}{(originalName.Length > 30 ? "..." : "")}\n\n提取到以下数量值，请选择要保留的:";
                                label.Left = 20;
                                label.Top = 20;
                                label.Width = selectForm.Width - 40;
                                label.Height = 80;
                                label.AutoSize = false;
                                label.TextAlign = System.Drawing.ContentAlignment.TopLeft;
                                selectForm.Controls.Add(label);

                                // 添加列表框
                                ListBox listBox = new ListBox();
                                listBox.Left = 20;
                                listBox.Top = 100;
                                listBox.Width = selectForm.Width - 40;
                                listBox.Height = 60;
                                foreach (string num in numbers)
                                {
                                    listBox.Items.Add(num);
                                }
                                listBox.Items.Add("全部保留"); // 添加全部保留选项
                                listBox.SelectedIndex = listBox.Items.Count - 1; // 默认选择全部保留
                                selectForm.Controls.Add(listBox);

                                // 添加"记住我的选择"复选框
                                CheckBox rememberCheckBox = new CheckBox();
                                rememberCheckBox.Text = "记住我的选择，应用到所有文件";
                                rememberCheckBox.Left = 20;
                                rememberCheckBox.Top = selectForm.Height - 90;
                                selectForm.Controls.Add(rememberCheckBox);

                                // 添加确定按钮
                                Button okButton = new Button();
                                okButton.Text = "确定";
                                okButton.Left = selectForm.Width - 100;
                                okButton.Top = selectForm.Height - 70;
                                okButton.Width = 70;
                                okButton.DialogResult = DialogResult.OK;
                                selectForm.Controls.Add(okButton);

                                // 设置默认按钮
                                selectForm.AcceptButton = okButton;

                                // 显示对话框
                                if (selectForm.ShowDialog() == DialogResult.OK)
                                {
                                    // 保存用户的选择偏好
                                    _rememberChoice = rememberCheckBox.Checked;
                                    if (_rememberChoice)
                                    {
                                        if (listBox.SelectedIndex == listBox.Items.Count - 1)
                                        {
                                            _lastSelectedOption = "all";
                                        }
                                        else
                                        {
                                            _lastSelectedIndex = listBox.SelectedIndex;
                                            _lastSelectedOption = listBox.SelectedItem.ToString();
                                        }
                                    }

                                    // 处理当前文件的选择
                                    if (listBox.SelectedIndex == listBox.Items.Count - 1)
                                    {
                                        // 用户选择全部保留
                                        finalNumber = string.Join(",", numbers);
                                    }
                                    else
                                    {
                                        // 用户选择了特定的数字
                                        finalNumber = listBox.SelectedItem.ToString();
                                    }
                                }
                            }
                        }
                        // 如果用户选择了"记住我的选择"，则直接应用上次的选择
                        else if (numbers.Count > 1 && _rememberChoice)
                        {
                            if (_lastSelectedOption == "all")
                            {
                                finalNumber = string.Join(",", numbers);
                            }
                            else if (_lastSelectedIndex >= 0 && _lastSelectedIndex < numbers.Count)
                            {
                                finalNumber = numbers[_lastSelectedIndex];
                            }
                        }
                        else if (numbers.Count == 1)
                        {
                            finalNumber = numbers[0];
                        }

                        if (!string.IsNullOrEmpty(finalNumber))
                        {
                            // 更新数量列
                            DataGridViewCell quantityCell = row.Cells["colQuantity"];
                            if (quantityCell != null)
                            {
                                LogHelper.Debug($"数量列单元格存在，ReadOnly: {quantityCell.ReadOnly}");

                                // 检查DataGridView是否绑定到BindingList
                                if (_dgv.DataSource is BindingList<FileRenameInfo> bindingList)
                                {
                                    // 通过BindingList更新数据，这样会自动同步到DataGridView
                                    if (row.Index >= 0 && row.Index < bindingList.Count)
                                    {
                                        var item = bindingList[row.Index];
                                        item.Quantity = finalNumber;
                                        _logger?.LogInformation($"数量提取：通过BindingList设置数量值: 行{row.Index}, {finalNumber}");
                                        processedCount++;
                                    }
                                    else
                                    {
                                        _logger?.LogWarning($"数量提取：行索引超出范围: {row.Index} >= {bindingList.Count}");
                                    }
                                }
                                else
                                {
                                    // 如果没有绑定到BindingList，直接设置单元格值
                                    if (!quantityCell.ReadOnly)
                                    {
                                        quantityCell.Value = finalNumber;
                                        _logger?.LogInformation($"数量提取：直接设置数量列值: 行{row.Index}, {finalNumber}");
                                        processedCount++;
                                    }
                                    else
                                    {
                                        _logger?.LogWarning($"数量提取：数量列单元格是只读的，无法设置值: 行{row.Index}");
                                    }
                                }
                            }
                            else
                            {
                                LogHelper.Debug("未找到数量列单元格");
                            }
                        }
                        else
                        {
                            LogHelper.Debug("未提取到数字或用户取消选择，文件名: " + originalName);
                        }
                    }
                }
                // 刷新整个DataGridView
                _dgv.Refresh();
                _logger?.LogInformation($"提取数字到数量列完成，共处理{processedCount}行");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "提取数字到数量列时发生错误");
                MessageBox.Show($"提取数字时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 提取关键字前的数字
        private string ExtractNumberBeforeKeyword(string text, string keyword)
        {
            try
            {
                List<string> numbers = new List<string>();
                int lastPosition = 0;

                // 循环查找所有关键字位置（不区分大小写）
                while (true)
                {
                    int keywordIndex = text.IndexOf(keyword, lastPosition, StringComparison.OrdinalIgnoreCase);
                    if (keywordIndex < 1) // 关键字不存在或在文本开头
                        break;

                    // 从关键字位置向前查找数字
                    int startIndex = keywordIndex - 1;
                    while (startIndex >= 0 && (char.IsDigit(text[startIndex]) || text[startIndex] == '.'))
                    {
                        startIndex--;
                    }
                    startIndex++;

                    // 提取数字
                    if (startIndex < keywordIndex)
                    {
                        string numberStr = text.Substring(startIndex, keywordIndex - startIndex);
                        // 移除可能的小数点后多余的数字（如果有）
                        if (numberStr.Contains('.'))
                        {
                            int dotIndex = numberStr.IndexOf('.');
                            if (dotIndex < numberStr.Length - 1)
                            {
                                numberStr = numberStr.Substring(0, dotIndex + 1) + 
                                    new string(numberStr.Substring(dotIndex + 1).Take(2).ToArray());
                            }
                        }
                        numbers.Add(numberStr);
                    }

                    // 移动到下一个关键字位置
                    lastPosition = keywordIndex + keyword.Length;
                }

                // 如果找到了数字，返回逗号分隔的字符串，否则返回null
                return numbers.Count > 0 ? string.Join(",", numbers) : null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"提取关键字 {keyword} 前的数字时发生错误");
                return null;
            }
        }

        // 应用序列到选中单元格（订单号或序号）
        private void ApplyOrderNumberSequence(string prefix, string suffix, int startValue, int increment)
        {
            try
            {
                // 找到选中的行的最小行索引
                int minRowIndex = int.MaxValue;
                foreach (DataGridViewCell cell in _dgv.SelectedCells)
                {
                    if (cell.RowIndex < minRowIndex)
                    {
                        minRowIndex = cell.RowIndex;
                    }
                }
                
                // 按照行索引的顺序应用序列
                int currentValue = startValue;
                int appliedCount = 0;
                for (int rowIndex = minRowIndex; rowIndex < _dgv.RowCount; rowIndex++)
                {
                    // 检查行是否被选中
                    bool isRowSelected = false;
                    foreach (DataGridViewCell cell in _dgv.Rows[rowIndex].Cells)
                    {
                        if (cell.Selected)
                        {
                            isRowSelected = true;
                            break;
                        }
                    }
                    
                    // 如果行被选中，应用序列
                    if (isRowSelected && rowIndex < _dgv.RowCount)
                    {
                        // 根据当前点击的列决定更新哪个列
                        string columnName = _currentColumnName == "colSerialNumber" ? "colSerialNumber" : "colOrderNumber";
                        DataGridViewCell cell = _dgv.Rows[rowIndex].Cells[columnName];
                        if (cell != null)
                        {
                            try
                            {
                                string newValue;
                                if (increment == 0)
                                {
                                    newValue = $"{prefix}{suffix}";
                                }
                                else
                                {
                                    newValue = $"{prefix}{currentValue}{suffix}";
                                    currentValue += increment;
                                }

                                // 检查DataGridView是否绑定到BindingList
                                if (_dgv.DataSource is BindingList<FileRenameInfo> bindingList)
                                {
                                    // 通过BindingList更新数据，这样会自动同步到DataGridView
                                    if (rowIndex >= 0 && rowIndex < bindingList.Count)
                                    {
                                        var item = bindingList[rowIndex];

                                        // 根据列名设置相应的属性
                                        if (columnName == "colSerialNumber")
                                        {
                                            item.SerialNumber = newValue;
                                        }
                                        else if (columnName == "colOrderNumber")
                                        {
                                            item.OrderNumber = newValue;
                                        }
                                        appliedCount++;
                                    }
                                }
                                else
                                {
                                    // 如果没有绑定到BindingList，直接设置单元格值
                                    cell.Value = newValue;
                                    appliedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogError(ex, $"应用序列到行 {rowIndex} 时发生错误");
                                // 继续处理其他行
                            }
                        }
                    }
                }
                _logger?.LogInformation($"应用订单号序列完成，共应用到{appliedCount}行");

                // 添加数据同步验证 - 确保UI和底层数据同步
                var updatedRows = new HashSet<int>();
                foreach (DataGridViewCell cell in _dgv.SelectedCells)
                {
                    if (cell.RowIndex >= 0)
                    {
                        updatedRows.Add(cell.RowIndex);
                    }
                }
                ValidateDataSync(updatedRows, "ApplyOrderNumberSequence");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "应用订单号序列时发生错误");
                MessageBox.Show($"应用序列时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 复制菜单项点击事件
        private void CopyItem_Click(object sender, EventArgs e)
        {
            try
            {
                CopySelectedCells();
                _logger?.LogInformation("执行复制操作");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "执行复制操作时发生错误");
                MessageBox.Show($"复制时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CutItem_Click(object sender, EventArgs e)
        {
            try
            {
                CopySelectedCells();
                ClearSelectedCells();
                _logger?.LogInformation("执行剪切操作");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "执行剪切操作时发生错误");
                MessageBox.Show($"剪切时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PasteItem_Click(object sender, EventArgs e)
        {
            try
            {
                PasteSelectedCells();
                _logger?.LogInformation("执行粘贴操作");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "执行粘贴操作时发生错误");
                MessageBox.Show($"粘贴时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Copy()
        {
            try
            {
                CopySelectedCells();
                _logger?.LogInformation("执行复制操作（通过公共方法）");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "执行复制操作（通过公共方法）时发生错误");
                MessageBox.Show($"复制时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CopySelectedCells()
        {
            try
            {
                // 获取选中的单元格范围
                var selectedCells = _dgv.SelectedCells.Cast<DataGridViewCell>()
                    .OrderBy(c => c.RowIndex).ThenBy(c => c.ColumnIndex).ToList();

                if (selectedCells.Count == 0)
                    return;

                // 确定选择区域的边界
                int minRow = selectedCells.Min(c => c.RowIndex);
                int maxRow = selectedCells.Max(c => c.RowIndex);
                int minCol = selectedCells.Min(c => c.ColumnIndex);
                int maxCol = selectedCells.Max(c => c.ColumnIndex);

                // 创建数据表格存储选中区域数据
                DataTable dataTable = new DataTable();
                for (int col = minCol; col <= maxCol; col++)
                {
                    dataTable.Columns.Add(_dgv.Columns[col].Name);
                }

                // 填充数据
                for (int row = minRow; row <= maxRow; row++)
                {
                    DataRow dataRow = dataTable.NewRow();
                    for (int col = minCol; col <= maxCol; col++)
                    {
                        var cell = selectedCells.FirstOrDefault(c => c.RowIndex == row && c.ColumnIndex == col);
                        dataRow[col - minCol] = cell?.Value?.ToString() ?? string.Empty;
                    }
                    dataTable.Rows.Add(dataRow);
                }

                // 将数据存入剪贴板（制表符分隔格式）
                StringBuilder sb = new StringBuilder();
                foreach (DataRow dr in dataTable.Rows)
                {
                    sb.AppendLine(string.Join("\t", dr.ItemArray));
                }
                Clipboard.SetText(sb.ToString().TrimEnd(Environment.NewLine.ToCharArray()));
                _logger?.LogInformation($"复制了{selectedCells.Count}个单元格到剪贴板");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "复制选中单元格时发生错误");
                throw;
            }
        }

        private void ClearSelectedCells()
        {
            try
            {
                int clearedCount = 0;
                foreach (DataGridViewCell cell in _dgv.SelectedCells)
                {
                    cell.Value = string.Empty;
                    clearedCount++;
                }
                _logger?.LogInformation($"清空了{clearedCount}个单元格");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "清空选中单元格时发生错误");
                throw;
            }
        }

        public void Paste()
        {
            try
            {
                PasteSelectedCells();
                _logger?.LogInformation("执行粘贴操作（通过公共方法）");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "执行粘贴操作（通过公共方法）时发生错误");
                MessageBox.Show($"粘贴时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void Cut()
        {
            try
            {
                CopySelectedCells();
                ClearSelectedCells();
                _logger?.LogInformation("执行剪切操作（通过公共方法）");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "执行剪切操作（通过公共方法）时发生错误");
                MessageBox.Show($"剪切时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PasteSelectedCells()
        {
            try
            {
                if (!Clipboard.ContainsText())
                    return;

                // 获取剪贴板文本
                string clipboardText = Clipboard.GetText();
                string[] rows = clipboardText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                // 获取当前选中单元格
                if (_dgv.SelectedCells.Count == 0)
                    return;

                int startRow = _dgv.SelectedCells[0].RowIndex;
                int startCol = _dgv.SelectedCells[0].ColumnIndex;
                int currentRow = startRow;
                int pastedCount = 0;

                // 粘贴数据到选中区域
                foreach (string row in rows)
                {
                    if (currentRow >= _dgv.RowCount)
                        break;

                    string[] cells = row.Split('\t');
                    int currentCol = startCol;

                    foreach (string cellValue in cells)
                    {
                        if (currentCol >= _dgv.ColumnCount)
                            break;

                        _dgv[currentCol, currentRow].Value = cellValue;
                        currentCol++;
                        pastedCount++;
                    }

                    currentRow++;
                }
                _logger?.LogInformation($"从剪贴板粘贴了{pastedCount}个单元格");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "粘贴选中单元格时发生错误");
                throw;
            }
        }

        // 删除菜单项点击事件
        private void DeleteItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (_dgv.CurrentRow != null && !_dgv.CurrentRow.IsNewRow)
                {
                    _dgv.Rows.Remove(_dgv.CurrentRow);
                    _logger?.LogInformation("删除了当前行");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "删除行时发生错误");
                MessageBox.Show($"删除行时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 刷新菜单项点击事件
        private void RefreshItem_Click(object sender, EventArgs e)
        {
            try
            {
                _dgv.Refresh();
                _logger?.LogInformation("刷新了DataGridView");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新DataGridView时发生错误");
                MessageBox.Show($"刷新时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 数据同步验证方法
        /// 确保DataGridView和BindingList之间的数据同步
        /// </summary>
        /// <param name="updatedRowIndices">已更新的行索引集合</param>
        /// <param name="operationName">操作名称，用于日志记录</param>
        private void ValidateDataSync(HashSet<int> updatedRowIndices = null, string operationName = "数据更新")
        {
            try
            {
                // 如果没有提供更新的行索引，尝试获取当前选中的行
                if (updatedRowIndices == null)
                {
                    updatedRowIndices = new HashSet<int>();
                    foreach (DataGridViewCell cell in _dgv.SelectedCells)
                    {
                        if (cell.RowIndex >= 0 && cell.RowIndex < _dgv.RowCount)
                        {
                            updatedRowIndices.Add(cell.RowIndex);
                        }
                    }
                }

                if (updatedRowIndices.Count == 0)
                {
                    _logger?.LogDebug($"{operationName}：没有需要验证的行");
                    return;
                }

                // 记录验证开始
                _logger?.LogInformation($"开始数据同步验证，操作: {operationName}，验证行数: {updatedRowIndices.Count}");

                // 验证DataGridView状态
                if (_dgv.InvokeRequired)
                {
                    _dgv.Invoke((MethodInvoker)delegate {
                        try
                        {
                            // 方法1：结束编辑状态
                            _dgv.EndEdit();
                            _dgv.RefreshEdit();

                            // 方法2：刷新DataGridView
                            _dgv.Refresh();
                            _dgv.Invalidate();
                            _dgv.Update();

                            _logger?.LogDebug($"{operationName}：DataGridView状态已刷新");
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogError(ex, $"{operationName}：刷新DataGridView时发生错误");
                        }
                    });
                }
                else
                {
                    // 如果不在UI线程，直接调用
                    try
                    {
                        _dgv.EndEdit();
                        _dgv.RefreshEdit();
                        _dgv.Refresh();
                        _dgv.Invalidate();
                        _dgv.Update();

                        _logger?.LogDebug($"{operationName}：DataGridView状态已刷新");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, $"{operationName}：刷新DataGridView时发生错误");
                    }
                }

                // 获取BindingList引用
                BindingList<FileRenameInfo> bindingList = null;

                // 如果有BindingList，验证数据同步
                if (_dgv.DataSource is BindingList<FileRenameInfo> currentBindingList)
                {
                    bindingList = currentBindingList;
                    _logger?.LogInformation($"开始验证BindingList数据同步，操作: {operationName}");

                    // 方法3：重置绑定源以确保通知机制触发
                    var dataSource = _dgv.DataSource;
                    _dgv.DataSource = null;
                    _dgv.DataSource = dataSource;

                    _logger?.LogDebug($"{operationName}：BindingList绑定源已重置");

                    // 方法4：触发BindingList更新通知
                    var listChangedMethod = bindingList.GetType().GetMethod("OnListChanged",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (listChangedMethod != null)
                    {
                        listChangedMethod.Invoke(bindingList, new object[] {
                            new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.Reset, -1)
                        });
                        _logger?.LogInformation($"{operationName}：BindingList更新通知已触发");
                    }
                    else
                    {
                        _logger?.LogWarning($"{operationName}：无法找到BindingList的OnListChanged方法");
                    }

                    // 方法5：验证特定行的数据一致性
                    foreach (int rowIndex in updatedRowIndices)
                    {
                        if (rowIndex < _dgv.RowCount && rowIndex < bindingList.Count)
                        {
                            var item = bindingList[rowIndex];
                            var quantityCell = _dgv.Rows[rowIndex].Cells["colQuantity"];
                            var orderNumberCell = _dgv.Rows[rowIndex].Cells["colOrderNumber"];
                            var serialNumberCell = _dgv.Rows[rowIndex].Cells["colSerialNumber"];

                            // 验证数量列
                            if (quantityCell != null)
                            {
                                string cellValue = quantityCell.Value?.ToString() ?? "";
                                string modelValue = item.Quantity;
                                if (!string.IsNullOrEmpty(modelValue) && !string.IsNullOrEmpty(cellValue))
                                {
                                    if (!cellValue.Equals(modelValue))
                                    {
                                        _logger?.LogWarning($"{operationName} - 行{rowIndex} 数量列数据不一致: DataGridView={cellValue}, BindingList={modelValue}");
                                    }
                                    else
                                    _logger?.LogDebug($"{operationName} - 行{rowIndex} 数量列数据同步正常: {modelValue}");
                                }
                            }

                            // 验证订单号列
                            if (orderNumberCell != null)
                            {
                                string cellValue = orderNumberCell.Value?.ToString() ?? "";
                                string modelValue = item.OrderNumber;
                                if (!string.IsNullOrEmpty(modelValue) && !string.IsNullOrEmpty(cellValue))
                                {
                                    if (!cellValue.Equals(modelValue))
                                    {
                                        _logger?.LogWarning($"{operationName} - 行{rowIndex} 订单号列数据不一致: DataGridView={cellValue}, BindingList={modelValue}");
                                    }
                                    else
                                    _logger?.LogDebug($"{operationName} - 行{rowIndex} 订单号列数据同步正常: {modelValue}");
                                }
                            }

                            // 验证序号列
                            if (serialNumberCell != null)
                            {
                                string cellValue = serialNumberCell.Value?.ToString() ?? "";
                                string modelValue = item.SerialNumber;
                                if (!string.IsNullOrEmpty(modelValue) && !string.IsNullOrEmpty(cellValue))
                                {
                                    if (!cellValue.Equals(modelValue))
                                    {
                                        _logger?.LogWarning($"{operationName} - 行{rowIndex} 序号列数据不一致: DataGridView={cellValue}, BindingList={modelValue}");
                                    }
                                    else
                                    {
                                        _logger?.LogDebug($"{operationName} - 行{rowIndex} 序号列数据同步正常: {modelValue}");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    _logger?.LogInformation($"{operationName}：DataGridView未绑定到BindingList，跳过BindingList验证");
                }

                // 验证更新后的结果
                int validatedCount = 0;
                foreach (int rowIndex in updatedRowIndices)
                {
                    if (rowIndex < _dgv.RowCount)
                    {
                        // 验证数量列
                        var quantityCell = _dgv.Rows[rowIndex].Cells["colQuantity"];
                        if (quantityCell != null && !string.IsNullOrEmpty(quantityCell.Value?.ToString()))
                        {
                            validatedCount++;
                            _logger?.LogDebug($"{operationName} - 行{rowIndex} 数量列验证通过: {quantityCell.Value}");
                        }

                        // 验证订单号列
                        var orderNumberCell = _dgv.Rows[rowIndex].Cells["colOrderNumber"];
                        if (orderNumberCell != null && !string.IsNullOrEmpty(orderNumberCell.Value?.ToString()))
                        {
                            validatedCount++;
                            _logger?.LogDebug($"{operationName} - 行{rowIndex} 订单号列验证通过: {orderNumberCell.Value}");
                        }

                        // 验证序号列
                        var serialNumberCell = _dgv.Rows[rowIndex].Cells["colSerialNumber"];
                        if (serialNumberCell != null && !string.IsNullOrEmpty(serialNumberCell.Value?.ToString()))
                        {
                            validatedCount++;
                            _logger?.LogDebug($"{operationName} - 行{rowIndex} 序号列验证通过: {serialNumberCell.Value}");
                        }
                    }
                }

                _logger?.LogInformation($"数据同步验证完成，操作: {operationName}，验证{validatedCount}个字段");

                // 如果有严重不一致，记录错误
                if (bindingList != null && bindingList.Count > 0 && updatedRowIndices.Count > 0)
                {
                    bool hasInconsistency = false;
                    foreach (int rowIndex in updatedRowIndices)
                    {
                        if (rowIndex < _dgv.RowCount && rowIndex < bindingList.Count)
                        {
                            var item = bindingList[rowIndex];
                            var quantityCell = _dgv.Rows[rowIndex].Cells["colQuantity"];
                            var orderNumberCell = _dgv.Rows[rowIndex].Cells["colOrderNumber"];
                            var serialNumberCell = _dgv.Rows[rowIndex].Cells["colSerialNumber"];

                            if ((quantityCell?.Value?.ToString() ?? "") != (item.Quantity ?? "") ||
                                (orderNumberCell?.Value?.ToString() ?? "") != (item.OrderNumber ?? "") ||
                                (serialNumberCell?.Value?.ToString() ?? "") != (item.SerialNumber ?? ""))
                            {
                                hasInconsistency = true;
                                break;
                            }
                        }
                    }

                    if (hasInconsistency)
                    {
                        _logger?.LogError($"{operationName}：检测到数据不一致问题，可能影响自动填充的稳定性");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, $"{operationName}：数据同步验证时发生异常");
                MessageBox.Show($"数据同步验证时发生错误: {ex.Message}", "同步验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 获取当前右键菜单实例
        /// </summary>
        public ContextMenuStrip ContextMenu => _contextMenu;

        /// <summary>
        /// 获取当前材料列表
        /// </summary>
        public List<string> Materials => new List<string>(_materials);
    }
}