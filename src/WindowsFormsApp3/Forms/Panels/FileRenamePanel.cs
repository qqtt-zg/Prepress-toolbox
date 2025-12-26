using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using WindowsFormsApp3;
using WindowsFormsApp3.Forms.Main;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// 文件重命名面板 - 使用AntdUI Table替代嵌入的Form1
    /// </summary>
    public partial class FileRenamePanel : BasePanelControl
    {
        private Form1 _embeddedForm; // 保留用于业务逻辑
        private ContextMenuStrip _contextMenu;
        private int _currentColumnIndex = -1;
        private int _currentRowIndex = -1;

        public override string PanelKey => "rename";
        public override string DisplayName => "首页";
        public override string IconName => "HomeOutlined";

        public FileRenamePanel()
        {
            InitializeComponent();
            
            // 在构造函数中初始化列，确保设计器可见
            // InitializeFileTable() 会判断列是否已存在，避免重复添加 (需添加相应逻辑)
            InitializeFileTable();
        }

        protected override void InitializePanel()
        {
            base.InitializePanel();
            
            try 
            {
                ShowLoading("正在初始化首页...");

                // 1. 初始化 Form1 用于业务逻辑（隐藏）
                _embeddedForm = new Form1();
                _embeddedForm.TopLevel = false;
                _embeddedForm.FormBorderStyle = FormBorderStyle.None;
                _embeddedForm.Visible = false; // 隐藏，仅用于逻辑
                _embeddedForm.ShowInTaskbar = false;
                this.Controls.Add(_embeddedForm);

                // 2. 绑定控件事件
                InitializeControlEvents();

                // 3. 创建AntdUI Table列定义
                // 3. 创建AntdUI Table列定义 (已在构造函数中执行)
               // InitializeFileTable();

                // 4. 初始化右键菜单
                InitializeContextMenu();

                // 5. 同步初始状态
                SyncInitialState();
                
                // 6. 订阅数据变化事件
                SubscribeToDataChanges();
            }
            catch (Exception ex)
            {
                ShowError($"加载失败: {ex.Message}");
                LogHelper.Error($"FileRenamePanel初始化失败: {ex}");
            }
            finally
            {
                HideLoading();
            }
        }

        /// <summary>
        /// 绑定控件事件（控件已在Designer.cs中创建）
        /// </summary>
        private void InitializeControlEvents()
        {
            // 正则选择事件
            _cmbRegex.SelectedValueChanged += (s, val) =>
            {
                if (val is AntdUI.ObjectNEventArgs args && args.Value is string pattern)
                {
                    _embeddedForm.SelectRegexPattern(pattern);
                }
            };

            // 选择文件夹按钮
            _btnSelectDir.Click += (s, e) =>
            {
                _embeddedForm.PerformSelectInputDir();
                _txtInputDir.Text = _embeddedForm.GetInputDirectory();
            };

            // 监控按钮
            _btnMonitor.Click += (s, e) =>
            {
                _embeddedForm.PerformMonitor();
                if (_btnMonitor.Text == "开始监控")
                {
                    _btnMonitor.Text = "停止监控";
                    _btnMonitor.Type = AntdUI.TTypeMini.Error;
                }
                else
                {
                    _btnMonitor.Text = "开始监控";
                    _btnMonitor.Type = AntdUI.TTypeMini.Primary;
                }
            };

            // Excel操作按钮
            _btnImportExcel.Click += (s, e) => _embeddedForm.PerformImportExcel();
            _btnClearExcel.Click += (s, e) => _embeddedForm.PerformClearExcel();
            _btnExportExcel.Click += (s, e) => _embeddedForm.PerformExportExcel();

            // 模式切换按钮
            _btnToggleMode.Click += (s, e) =>
            {
                _embeddedForm.PerformToggleMode();
                _btnToggleMode.Text = _btnToggleMode.Text == "复制模式" ? "剪切模式" : "复制模式";
            };

            // 其他操作按钮
            _btnManualMode.Click += (s, e) => _embeddedForm.PerformImmediateRename();
            _btnBatchMode.Click += (s, e) => _embeddedForm.PerformStopImmediateRename();
            _btnRename.Click += (s, e) => _embeddedForm.PerformRename();

            // JSON管理控件事件
            _cmbJsonFiles.SelectedValueChanged += CmbJsonFiles_SelectedValueChanged;
            _btnSaveJson.Click += BtnSaveJson_Click;
            
            // 初始化JSON文件列表
            PopulateJsonFilesDropdown();
        }

        /// <summary>
        /// 填充JSON文件下拉列表
        /// </summary>
        private void PopulateJsonFilesDropdown()
        {
            try
            {
                var jsonDir = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "SavedGrids");
                if (!System.IO.Directory.Exists(jsonDir))
                {
                    System.IO.Directory.CreateDirectory(jsonDir);
                }

                var jsonFiles = System.IO.Directory.GetFiles(jsonDir, "*.json")
                    .Select(f => System.IO.Path.GetFileNameWithoutExtension(f))
                    .ToArray();

                _cmbJsonFiles.Items.Clear();
                foreach (var file in jsonFiles)
                {
                    _cmbJsonFiles.Items.Add(file);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载JSON文件列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// JSON文件选择变化事件
        /// </summary>
        private void CmbJsonFiles_SelectedValueChanged(object sender, AntdUI.ObjectNEventArgs e)
        {
            if (e.Value is string fileName && !string.IsNullOrEmpty(fileName))
            {
                try
                {
                    var jsonDir = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "SavedGrids");
                    var filePath = System.IO.Path.Combine(jsonDir, fileName + ".json");
                    
                    if (System.IO.File.Exists(filePath))
                    {
                        var jsonContent = System.IO.File.ReadAllText(filePath);
                        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<System.ComponentModel.BindingList<FileRenameInfo>>(jsonContent);
                        
                        if (data != null)
                        {
                            _fileTable.DataSource = data;
                            UpdateStatusLabel($"已加载: {fileName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"加载JSON文件失败: {ex.Message}");
                    UpdateStatusLabel($"加载失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 保存JSON按钮点击事件
        /// </summary>
        private void BtnSaveJson_Click(object sender, EventArgs e)
        {
            try
            {
                var jsonDir = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "SavedGrids");
                if (!System.IO.Directory.Exists(jsonDir))
                {
                    System.IO.Directory.CreateDirectory(jsonDir);
                }

                // 生成带时间戳的文件名
                var fileName = $"Grid_{DateTime.Now:yyyyMMdd_HHmmss}";
                var filePath = System.IO.Path.Combine(jsonDir, fileName + ".json");

                if (_fileTable.DataSource is System.ComponentModel.BindingList<FileRenameInfo> bindingList)
                {
                    // 过滤掉空行
                    var dataToSave = bindingList.Where(item => !string.IsNullOrEmpty(item.OriginalName)).ToList();
                    var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(dataToSave, Newtonsoft.Json.Formatting.Indented);
                    System.IO.File.WriteAllText(filePath, jsonContent);

                    // 刷新下拉列表
                    PopulateJsonFilesDropdown();
                    UpdateStatusLabel($"已保存: {fileName}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存JSON文件失败: {ex.Message}");
                UpdateStatusLabel($"保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新状态栏文本
        /// </summary>
        public void UpdateStatusLabel(string text)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = text;
            }
        }

        private void InitializeFileTable()
        {
            // _fileTable 已在 Designer.cs 中创建，这里只配置列和事件
            
            // 定义列 (恢复运行时定义以修复设计器崩溃问题)
            if (_fileTable.Columns.Count > 0) return; // 防止重复添加
            _fileTable.Columns = new AntdUI.ColumnCollection
            {
                new AntdUI.Column("SerialNumber", "序号", AntdUI.ColumnAlign.Center) { Width = "50px" },
                new AntdUI.Column("OriginalName", "原文件名", AntdUI.ColumnAlign.Left) { Width = "16%" },
                new AntdUI.Column("NewName", "新文件名", AntdUI.ColumnAlign.Left) { Width = "12%" },
                new AntdUI.Column("RegexResult", "正则结果", AntdUI.ColumnAlign.Center) { Width = "8%" },
                new AntdUI.Column("OrderNumber", "订单号", AntdUI.ColumnAlign.Center) { Width = "7%" },
                new AntdUI.Column("Material", "材料", AntdUI.ColumnAlign.Center) { Width = "7%" },
                new AntdUI.Column("Quantity", "数量", AntdUI.ColumnAlign.Center) { Width = "5%" },
                new AntdUI.Column("Dimensions", "尺寸", AntdUI.ColumnAlign.Center) { Width = "7%" },
                new AntdUI.Column("CompositeColumn", "列组合", AntdUI.ColumnAlign.Center) { Width = "7%" },
                new AntdUI.Column("LayoutRows", "行数", AntdUI.ColumnAlign.Center) { Width = "4%" },
                new AntdUI.Column("LayoutColumns", "列数", AntdUI.ColumnAlign.Center) { Width = "4%" },
                new AntdUI.Column("Process", "工艺", AntdUI.ColumnAlign.Center) { Width = "6%" },
                new AntdUI.Column("PageCount", "页数", AntdUI.ColumnAlign.Center) { Width = "4%" },
                new AntdUI.Column("Time", "时间", AntdUI.ColumnAlign.Center) { Width = "6%" }
            };

            // 初始化999行空数据（与Form1保持一致）
            var fileBindingList = new BindingList<FileRenameInfo>();
            for (int i = 0; i < 999; i++)
            {
                fileBindingList.Add(new FileRenameInfo());
            }
            _fileTable.DataSource = fileBindingList;

            // 绑定右键菜单事件
            _fileTable.CellClick += FileTable_CellClick;
            _fileTable.MouseUp += FileTable_MouseUp;

            // 视觉优化：完全仿官方示例风格（白底+淡蓝悬浮）
            _fileTable.RowHoverBg = Color.FromArgb(230, 247, 255); // Ant Design Blue-1
            // 移除斑马纹，保持纯净白底
            
            // 初始化列头右键菜单
            InitializeColumnHeaderContextMenu();
        }
        
        // 列头右键菜单
        private System.Windows.Forms.ContextMenuStrip _columnHeaderMenu;
        
        /// <summary>
        /// 初始化列头右键菜单
        /// </summary>
        private void InitializeColumnHeaderContextMenu()
        {
            _columnHeaderMenu = new System.Windows.Forms.ContextMenuStrip();
            _columnHeaderMenu.Font = new Font("微软雅黑", 9F);
            
            // 添加"列显示设置"标题
            var titleItem = new ToolStripMenuItem("列显示设置") { Enabled = false };
            _columnHeaderMenu.Items.Add(titleItem);
            _columnHeaderMenu.Items.Add(new ToolStripSeparator());
            
            // 为每一列添加显示/隐藏选项
            foreach (var column in _fileTable.Columns)
            {
                var item = new ToolStripMenuItem(column.Title)
                {
                    Checked = column.Visible,
                    CheckOnClick = true,
                    Tag = column.Key
                };
                item.CheckedChanged += ColumnMenuItem_CheckedChanged;
                _columnHeaderMenu.Items.Add(item);
            }
            
            // 绑定表格列头右键事件
            _fileTable.MouseDown += FileTable_MouseDown_Header;
        }
        
        // 标记是否正在显示列头菜单，用于阻止单元格菜单
        private bool _isShowingHeaderMenu = false;
        
        /// <summary>
        /// 表格鼠标按下事件 - 检测列头右键
        /// </summary>
        private void FileTable_MouseDown_Header(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // 检测是否点击在列头区域
                if (e.Y <= _fileTable.RowHeightHeader)
                {
                    _isShowingHeaderMenu = true;
                    
                    // 更新菜单项的选中状态
                    foreach (var item in _columnHeaderMenu.Items.OfType<ToolStripMenuItem>())
                    {
                        if (item.Tag is string columnKey)
                        {
                            var column = _fileTable.Columns.FirstOrDefault(c => c.Key == columnKey);
                            if (column != null)
                            {
                                item.Checked = column.Visible;
                            }
                        }
                    }
                    // 显示列头右键菜单
                    _columnHeaderMenu.Show(_fileTable, e.Location);
                }
                else
                {
                    _isShowingHeaderMenu = false;
                }
            }
        }
        
        /// <summary>
        /// 列菜单项选中状态变化事件
        /// </summary>
        private void ColumnMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is string columnKey)
            {
                var column = _fileTable.Columns.FirstOrDefault(c => c.Key == columnKey);
                if (column != null)
                {
                    // 更新列的可见性
                    column.Visible = menuItem.Checked;
                    
                    // 刷新表格显示
                    _fileTable.Invalidate();
                }
            }
        }

        private void FileTable_CellClick(object sender, AntdUI.TableClickEventArgs e)
        {
            _currentRowIndex = e.RowIndex;
            _currentColumnIndex = e.ColumnIndex;
        }

        private void FileTable_MouseUp(object sender, MouseEventArgs e)
        {
            // 如果刚显示了列头菜单，跳过单元格菜单
            if (_isShowingHeaderMenu)
            {
                _isShowingHeaderMenu = false;
                return;
            }
            
            // 确保不在列头区域，并且有有效的行索引
            if (e.Button == MouseButtons.Right && _currentRowIndex >= 0 && e.Y > _fileTable.RowHeightHeader)
            {
                UpdateContextMenuForColumn();
                _contextMenu.Show(_fileTable, e.Location);
            }
        }



        private void InitializeContextMenu()
        {
            _contextMenu = new ContextMenuStrip();
            CreateDefaultContextMenu();
        }

        private void UpdateContextMenuForColumn()
        {
            _contextMenu.Items.Clear();
            
            if (_currentColumnIndex < 0 || _fileTable.Columns == null) 
            {
                CreateDefaultContextMenu();
                return;
            }

            string columnKey = _fileTable.Columns[_currentColumnIndex].Key;
            
            switch (columnKey)
            {
                case "Material":
                    CreateMaterialContextMenu();
                    break;
                case "Quantity":
                    CreateQuantityContextMenu();
                    break;
                case "Dimensions":
                    CreateDimensionsContextMenu();
                    break;
                default:
                    CreateDefaultContextMenu();
                    break;
            }
        }

        private void CreateDefaultContextMenu()
        {
            _contextMenu.Items.Clear();
            
            var copyItem = new ToolStripMenuItem("复制");
            copyItem.Click += (s, e) => CopySelectedCell();
            _contextMenu.Items.Add(copyItem);

            var deleteItem = new ToolStripMenuItem("删除行");
            deleteItem.Click += (s, e) => DeleteSelectedRow();
            _contextMenu.Items.Add(deleteItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            var refreshItem = new ToolStripMenuItem("刷新");
            refreshItem.Click += (s, e) => RefreshData();
            _contextMenu.Items.Add(refreshItem);
        }

        private void CreateMaterialContextMenu()
        {
            _contextMenu.Items.Clear();
            
            // 获取材料列表
            var materials = MaterialManager.Instance.GetMaterials();
            if (materials != null && materials.Count > 0)
            {
                foreach (var material in materials)
                {
                    var item = new ToolStripMenuItem(material);
                    item.Click += (s, e) => SetCellValue("Material", material);
                    _contextMenu.Items.Add(item);
                }
                _contextMenu.Items.Add(new ToolStripSeparator());
            }
            
            CreateDefaultContextMenu();
        }

        private void CreateQuantityContextMenu()
        {
            _contextMenu.Items.Clear();
            
            var inputItem = new ToolStripMenuItem("手动输入");
            inputItem.Click += (s, e) => ShowQuantityInputDialog();
            _contextMenu.Items.Add(inputItem);
            
            _contextMenu.Items.Add(new ToolStripSeparator());
            CreateDefaultContextMenu();
        }

        private void CreateDimensionsContextMenu()
        {
            _contextMenu.Items.Clear();
            
            var bleedItem = new ToolStripMenuItem("设置出血值");
            bleedItem.Click += (s, e) => ShowBleedInputDialog();
            _contextMenu.Items.Add(bleedItem);
            
            _contextMenu.Items.Add(new ToolStripSeparator());
            CreateDefaultContextMenu();
        }

        private void SetCellValue(string propertyName, string value)
        {
            if (_currentRowIndex < 0) return;
            
            try
            {
                if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList && 
                    _currentRowIndex < bindingList.Count)
                {
                    var item = bindingList[_currentRowIndex];
                    var prop = typeof(FileRenameInfo).GetProperty(propertyName);
                    prop?.SetValue(item, value);
                    _fileTable.Invalidate();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"设置单元格值失败: {ex.Message}");
            }
        }

        private void CopySelectedCell()
        {
            try
            {
                if (_currentRowIndex >= 0 && _currentColumnIndex >= 0 &&
                    _fileTable.DataSource is BindingList<FileRenameInfo> bindingList &&
                    _currentRowIndex < bindingList.Count)
                {
                    var item = bindingList[_currentRowIndex];
                    var columnKey = _fileTable.Columns[_currentColumnIndex].Key;
                    var prop = typeof(FileRenameInfo).GetProperty(columnKey);
                    var value = prop?.GetValue(item)?.ToString() ?? "";
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
                if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList &&
                    _currentRowIndex < bindingList.Count)
                {
                    bindingList.RemoveAt(_currentRowIndex);
                    _fileTable.Invalidate();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"删除行失败: {ex.Message}");
            }
        }

        private void ShowQuantityInputDialog()
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = "手动输入增量值";
                inputForm.Width = 300;
                inputForm.Height = 150;
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;

                var label = new Label { Text = "请输入增量值:", Left = 10, Top = 10, Width = 260 };
                var textBox = new TextBox { Left = 10, Top = 35, Width = 260, Text = "1" };
                var okButton = new Button { Text = "确定", Left = 100, Top = 70, DialogResult = DialogResult.OK };
                
                inputForm.Controls.AddRange(new Control[] { label, textBox, okButton });
                inputForm.AcceptButton = okButton;

                if (inputForm.ShowDialog() == DialogResult.OK && int.TryParse(textBox.Text, out int delta))
                {
                    BatchUpdateQuantity(delta);
                }
            }
        }

        private void BatchUpdateQuantity(int delta)
        {
            if (_currentRowIndex < 0) return;
            
            try
            {
                if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList &&
                    _currentRowIndex < bindingList.Count)
                {
                    var item = bindingList[_currentRowIndex];
                    int.TryParse(item.Quantity ?? "0", out int current);
                    item.Quantity = (current + delta).ToString();
                    _fileTable.Invalidate();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"更新数量失败: {ex.Message}");
            }
        }

        private void ShowBleedInputDialog()
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = "设置出血值";
                inputForm.Width = 300;
                inputForm.Height = 150;
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;

                var label = new Label { Text = "请输入出血值(mm):", Left = 10, Top = 10, Width = 260 };
                var textBox = new TextBox { Left = 10, Top = 35, Width = 260, Text = "2" };
                var okButton = new Button { Text = "确定", Left = 100, Top = 70, DialogResult = DialogResult.OK };
                
                inputForm.Controls.AddRange(new Control[] { label, textBox, okButton });
                inputForm.AcceptButton = okButton;

                if (inputForm.ShowDialog() == DialogResult.OK && double.TryParse(textBox.Text, out double bleed))
                {
                    AppSettings.Set("TetBleedValues", bleed.ToString());
                    AppSettings.Save();
                    LogHelper.Info($"设置出血值为: {bleed}mm");
                }
            }
        }

        private void RefreshData()
        {
            _fileTable.Invalidate();
        }

        private void SyncInitialState()
        {
            if (_embeddedForm == null) return;

            _txtInputDir.Text = _embeddedForm.GetInputDirectory();

            _cmbRegex.Items.Clear();
            var patterns = _embeddedForm.GetRegexPatterns();
            foreach (var p in patterns)
            {
                _cmbRegex.Items.Add(p);
            }
        }

        private void SubscribeToDataChanges()
        {
            // 暂时使用空实现，后续可通过事件机制同步数据
            // TODO: 实现从Form1获取数据源并绑定到_fileTable
            // 当前Table将在用户操作时通过Form1的事件更新
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            SubscribeToDataChanges();
            _fileTable?.Invalidate();
        }

        public void UpdateExcelData()
        {
            if (_embeddedForm != null && !_embeddedForm.IsDisposed)
            {
                _embeddedForm.UpdateExcelData();
                SubscribeToDataChanges();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_embeddedForm != null && !_embeddedForm.IsDisposed)
                {
                    _embeddedForm.Close();
                    _embeddedForm.Dispose();
                }
                _contextMenu?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
