using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// 数据库面板 - 显示Excel导入的数据
    /// </summary>
    public partial class DatabasePanel : BasePanelControl
    {
        private ContextMenuStrip _contextMenu;
        private int _currentColumnIndex = -1;
        private int _currentRowIndex = -1;
        private IExcelImportService _excelImportService;

        public override string PanelKey => "database";
        public override string DisplayName => "数据库";
        public override string IconName => "DatabaseOutlined";

        public DatabasePanel()
        {
            InitializeComponent();
        }

        protected override void InitializePanel()
        {
            base.InitializePanel();

            try
            {
                ShowLoading("正在初始化数据库...");

                // 获取 Excel 导入服务
                _excelImportService = ServiceLocator.Instance.GetExcelImportService();

                // 配置 KryptonDataGridView（控件已在 Designer.cs 中创建）
                InitializeExcelTable();

                // 初始化右键菜单
                InitializeContextMenu();

                // 加载数据
                LoadExcelData();
            }
            catch (Exception ex)
            {
                ShowError($"加载失败: {ex.Message}");
                LogHelper.Error($"DatabasePanel初始化失败: {ex}");
            }
            finally
            {
                HideLoading();
            }
        }

        private void InitializeExcelTable()
        {
            // 禁用自动生成列
            _excelTable.AutoGenerateColumns = false;

            // 先获取 Excel 数据以确定列结构
            var excelData = _excelImportService?.ImportedData;

            if (excelData != null && excelData.Columns.Count > 0)
            {
                // 清除现有列
                _excelTable.Columns.Clear();

                // 根据实际数据动态创建列
                foreach (DataColumn col in excelData.Columns)
                {
                    var dgvCol = new DataGridViewTextBoxColumn
                    {
                        Name = col.ColumnName,
                        HeaderText = col.ColumnName,
                        DataPropertyName = col.ColumnName,
                        FillWeight = 100f / excelData.Columns.Count
                    };
                    _excelTable.Columns.Add(dgvCol);
                }

                // 设置数据源
                _excelTable.DataSource = excelData;
            }
            else
            {
                // 没有数据时显示默认列结构
                _excelTable.Columns.Clear();
                _excelTable.Columns.Add(new DataGridViewTextBoxColumn { Name = "SerialNumber", HeaderText = "序号", FillWeight = 5 });
                _excelTable.Columns.Add(new DataGridViewTextBoxColumn { Name = "OrderNumber", HeaderText = "订单号", FillWeight = 10 });
                _excelTable.Columns.Add(new DataGridViewTextBoxColumn { Name = "Material", HeaderText = "材料", FillWeight = 10 });
                _excelTable.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "数量", FillWeight = 8 });
                _excelTable.Columns.Add(new DataGridViewTextBoxColumn { Name = "Dimensions", HeaderText = "尺寸", FillWeight = 10 });
                _excelTable.Columns.Add(new DataGridViewTextBoxColumn { Name = "Process", HeaderText = "工艺", FillWeight = 10 });
                _excelTable.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "备注", FillWeight = 15 });

                // 设置空数据源
                _excelTable.DataSource = null;
            }

            // 绑定事件
            _excelTable.CellClick += ExcelTable_CellClick;
            _excelTable.CellMouseUp += ExcelTable_CellMouseUp;
            _excelTable.RowPostPaint += ExcelTable_RowPostPaint;
        }

        private void ExcelTable_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            _currentRowIndex = e.RowIndex;
            _currentColumnIndex = e.ColumnIndex;
        }

        private void ExcelTable_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                var rect = _excelTable.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                _contextMenu.Show(_excelTable, rect.Left, rect.Bottom);
            }
        }

        /// <summary>
        /// 在行头绘制序号
        /// </summary>
        private void ExcelTable_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            // 绘制行号 (从1开始)
            var rowNumber = (e.RowIndex + 1).ToString();
            
            // 使用浅灰色，不突兀
            using (var brush = new SolidBrush(Color.FromArgb(160, 160, 160)))
            {
                // 计算绘制位置（水平和垂直都居中）
                var bounds = e.RowBounds;
                var headerWidth = _excelTable.RowHeadersWidth;
                var size = e.Graphics.MeasureString(rowNumber, _excelTable.Font);
                var x = (headerWidth - size.Width) / 2;
                var y = bounds.Top + (bounds.Height - size.Height) / 2;
                
                e.Graphics.DrawString(rowNumber, _excelTable.Font, brush, x, y);
            }
        }

        private void InitializeContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            
            var copyItem = new ToolStripMenuItem("复制");
            copyItem.Click += (s, e) => CopySelectedCell();
            _contextMenu.Items.Add(copyItem);

            var deleteItem = new ToolStripMenuItem("删除行");
            deleteItem.Click += (s, e) => DeleteSelectedRow();
            _contextMenu.Items.Add(deleteItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            var refreshItem = new ToolStripMenuItem("刷新");
            refreshItem.Click += (s, e) => LoadExcelData();
            _contextMenu.Items.Add(refreshItem);
        }

        private void CopySelectedCell()
        {
            try
            {
                if (_currentRowIndex >= 0 && _currentColumnIndex >= 0 &&
                    _excelTable.DataSource is DataTable dt &&
                    _currentRowIndex < dt.Rows.Count)
                {
                    var value = dt.Rows[_currentRowIndex][_currentColumnIndex]?.ToString() ?? "";
                    Clipboard.SetText(value);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"复制失败: {ex.Message}");
            }
        }

        private void DeleteSelectedRow()
        {
            if (_currentRowIndex < 0) return;
            
            try
            {
                if (_excelTable.DataSource is DataTable dt &&
                    _currentRowIndex < dt.Rows.Count)
                {
                    dt.Rows.RemoveAt(_currentRowIndex);
                    _excelTable.Invalidate();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"删除行失败: {ex.Message}");
            }
        }

        private void LoadExcelData()
        {
            try
            {
                // 从服务获取最新的 Excel 数据
                var excelData = _excelImportService?.ImportedData;

                if (excelData != null && excelData.Rows.Count > 0)
                {
                    // 有数据：重新初始化表格以适应数据结构
                    InitializeExcelTable();
                    LogHelper.Debug($"DatabasePanel: 加载了 {excelData.Rows.Count} 行 Excel 数据");
                }
                else
                {
                    // 无数据：显示空表格
                    _excelTable.DataSource = null;
                    LogHelper.Debug("DatabasePanel: 没有 Excel 数据");
                }

                _excelTable.Invalidate();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载Excel数据失败: {ex.Message}");
            }
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            LoadExcelData();
        }

        /// <summary>
        /// 面板激活时自动刷新数据
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible)
            {
                LoadExcelData();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _contextMenu?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
