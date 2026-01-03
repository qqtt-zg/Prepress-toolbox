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

                // 配置AntdUI Table（控件已在Designer.cs中创建）
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
            // 先获取 Excel 数据以确定列结构
            var excelData = _excelImportService?.ImportedData;

            if (excelData != null && excelData.Columns.Count > 0)
            {
                // 根据实际数据动态创建列
                var columns = new AntdUI.ColumnCollection();
                foreach (DataColumn col in excelData.Columns)
                {
                    columns.Add(new AntdUI.Column(col.ColumnName, col.ColumnName, AntdUI.ColumnAlign.Center)
                    {
                        Width = $"{100 / excelData.Columns.Count}%"
                    });
                }
                _excelTable.Columns = columns;

                // 设置数据源
                _excelTable.DataSource = excelData;
            }
            else
            {
                // 没有数据时显示默认列结构
                _excelTable.Columns = new AntdUI.ColumnCollection
                {
                    new AntdUI.Column("SerialNumber", "序号", AntdUI.ColumnAlign.Center) { Width = "50px" },
                    new AntdUI.Column("OrderNumber", "订单号", AntdUI.ColumnAlign.Center) { Width = "10%" },
                    new AntdUI.Column("Material", "材料", AntdUI.ColumnAlign.Center) { Width = "10%" },
                    new AntdUI.Column("Quantity", "数量", AntdUI.ColumnAlign.Center) { Width = "8%" },
                    new AntdUI.Column("Dimensions", "尺寸", AntdUI.ColumnAlign.Center) { Width = "10%" },
                    new AntdUI.Column("Process", "工艺", AntdUI.ColumnAlign.Center) { Width = "10%" },
                    new AntdUI.Column("Notes", "备注", AntdUI.ColumnAlign.Left) { Width = "15%" }
                };

                // 设置空数据源以确保列头显示
                var emptyData = new DataTable();
                emptyData.Columns.Add("SerialNumber");
                emptyData.Columns.Add("OrderNumber");
                emptyData.Columns.Add("Material");
                emptyData.Columns.Add("Quantity");
                emptyData.Columns.Add("Dimensions");
                emptyData.Columns.Add("Process");
                emptyData.Columns.Add("Notes");
                _excelTable.DataSource = emptyData;
            }

            // 绑定事件
            _excelTable.CellClick += ExcelTable_CellClick;
            _excelTable.MouseUp += ExcelTable_MouseUp;
        }

        private void ExcelTable_CellClick(object sender, AntdUI.TableClickEventArgs e)
        {
            _currentRowIndex = e.RowIndex;
            _currentColumnIndex = e.ColumnIndex;
        }

        private void ExcelTable_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && _currentRowIndex >= 0)
            {
                _contextMenu.Show(_excelTable, e.Location);
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
                    _excelTable.DataSource is System.Data.DataTable dt &&
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
                if (_excelTable.DataSource is System.Data.DataTable dt &&
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
