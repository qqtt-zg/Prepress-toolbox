using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using WindowsFormsApp3;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Services;
using System.Text.RegularExpressions;
using WindowsFormsApp3.Forms.Utils;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// 拖拽操作模式
    /// </summary>
    public enum DropOperationMode
    {
        Default,      // 默认：智能判断（同分区→移动，跨分区→复制）
        ForceCopy,   // 强制复制
        ForceMove    // 强制移动
    }

    /// <summary>
    /// 文件重命名面板 - 使用AntdUI Table，采用MVP模式
    /// 阶段5：完全移除Form1依赖，使用Presenter处理业务逻辑
    /// </summary>
    public partial class FileRenamePanel : BasePanelControl, IFileRenamePanelView, IExcelParentView
    {
        // Presenter 实例（在InitializePanel中初始化）
        private IFileRenamePanelPresenter _presenter;

        private ContextMenuStrip _contextMenu;
        private int _currentColumnIndex = -1;
        private int _currentRowIndex = -1;

        // 悬浮拖拽区
        private FloatingDropZoneForm _floatingDropZone;
        
        // 拖拽操作模式
        private DropOperationMode _dropOperationMode = DropOperationMode.Default;

        // 列顺序保存Timer（用于防抖）
        private System.Windows.Forms.Timer _columnSaveTimer;

        public override string PanelKey => "rename";
        public override string DisplayName => "首页";
        public override string IconName => "HomeOutlined";

        public FileRenamePanel()
        {
            InitializeComponent();
            
            // 在构造函数中初始化列，确保设计器可见
            // InitializeFileTable() 会判断列是否已存在，避免重复添加 (需添加相应逻辑)
            InitializeFileTable();

            // 设置下拉框列表宽度自适应内容
            _cmbJsonFiles.ListAutoWidth = true;

            // 初始化列顺序保存Timer（用于防抖）
            _columnSaveTimer = new System.Windows.Forms.Timer();
            _columnSaveTimer.Interval = 1000; // 1秒延迟，避免频繁保存
            _columnSaveTimer.Tick += (s, args) => {
                _columnSaveTimer.Stop();
                SaveColumnSettings();
            };
        }

        protected override void InitializePanel()
        {
            base.InitializePanel();

            try
            {
                ShowLoading("正在初始化首页...");

                // 阶段5：创建 Presenter 实例
                _presenter = new FileRenamePanelPresenter(this);
                _presenter.Initialize();

                // 绑定控件事件
                InitializeControlEvents();

                // 创建AntdUI Table列定义 (已在构造函数中执行)
                // InitializeFileTable();

                // 初始化右键菜单
                InitializeContextMenu();

                // 同步初始状态（阶段5：从Presenter同步）
                SyncInitialState();

                // 订阅数据变化事件
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
            // 正则选择事件（UI已自动更新SelectedValue属性）
            _cmbRegex.SelectedValueChanged += (s, val) =>
            {
                if (val is AntdUI.ObjectNEventArgs args && args.Value is string pattern)
                {
                    // 保存选中的正则表达式到设置
                    AppSettings.LastSelectedRegex = pattern;
                    AppSettings.Save();

                    // 禁用自动触发更新，仅更新设置
                    // RegexPatternChanged?.Invoke(this, pattern);
                }
            };

            // 选择文件夹按钮（阶段5：迁移到Presenter）
            _btnSelectDir.Click += (s, e) =>
            {
                _presenter.HandleSelectInputDir();
                // 更新下拉框选项和选中值
                UpdateInputDirSelect();
            };

            // 输入目录下拉框选择变化事件
            _cmbInputDir.SelectedValueChanged += (s, args) =>
            {
                if (args.Value is string selectedPath && !string.IsNullOrEmpty(selectedPath))
                {
                    // 保存用户最后选择的目录
                    AppSettings.LastInputDir = selectedPath;
                    AppSettings.Save();

                    // 更新 Presenter 的输入目录
                    _presenter.SetInputDirectory(selectedPath);
                }
            };

            // 监控按钮（阶段2：迁移到Presenter）
            _btnMonitor.Click += (s, e) =>
            {
                _presenter.HandleToggleMonitoring();
                UpdateMonitorButtonState();
            };

            // 悬浮拖拽按钮：显示/隐藏悬浮拖拽区
            if (_btnDropZone != null)
            {
                _btnDropZone.Click += (s, e) => ToggleFloatingDropZone();
            }

            // Excel操作按钮（阶段5：迁移到Presenter）
            _btnImportExcel.Click += async (s, e) => await _presenter.HandleImportExcelAsync();
            _btnMatchExcel.Click += (s, e) => _presenter.MatchExcelData();
            _btnClearExcel.Click += (s, e) => {
                if (ShowConfirm("确定要清除所有文件列表数据吗？此操作不可撤销。", "确认清除"))
                {
                    _presenter.ClearFileList();
                    // 清除后添加999行空数据
                    for (int i = 0; i < 999; i++)
                    {
                        _presenter.AddEmptyRowToTable();
                    }
                }
            };
            _btnExportExcel.Click += (s, e) => _presenter.HandleExportExcel();

            // 模式切换按钮（阶段5：迁移到Presenter）
            _btnToggleMode.Click += (s, e) =>
            {
                _presenter.HandleModeToggle();
                UpdateModeButtonState();
            };

            // 其他操作按钮（阶段5：手动/批量模式切换）
            _btnManualMode.Click += (s, e) =>
            {
                _presenter.StartImmediateMode();
                UpdateImmediateModeButtonState();
            };
            _btnBatchMode.Click += (s, e) =>
            {
                _presenter.StopImmediateMode();
                UpdateImmediateModeButtonState();
            };

            // 重命名按钮（阶段3：迁移到Presenter）
            _btnRename.Click += async (s, e) => await _presenter.HandleRenameAsync();

            // JSON管理控件事件
            _cmbJsonFiles.SelectedValueChanged += CmbJsonFiles_SelectedValueChanged;
            _btnSaveJson.Click += BtnSaveJson_Click;

            // 初始化JSON文件列表
            PopulateJsonFilesDropdown();
            
            // 初始化拖拽操作模式下拉框
            if (_cmbDropOperationMode != null)
            {
                _cmbDropOperationMode.Items.Clear();
                _cmbDropOperationMode.Items.Add("默认");
                _cmbDropOperationMode.Items.Add("强制复制");
                _cmbDropOperationMode.Items.Add("强制移动");
                _cmbDropOperationMode.SelectedIndex = 0; // 默认选择"默认"
                
                // 添加选择变化事件处理
                _cmbDropOperationMode.SelectedValueChanged += (s, e) => UpdateDropOperationMode();
            }
            
            // 初始化拖拽操作模式
            UpdateDropOperationMode();
        }
        
        /// <summary>
        /// 更新拖拽操作模式
        /// </summary>
        private void UpdateDropOperationMode()
        {
            if (_cmbDropOperationMode == null) return;
            
            switch (_cmbDropOperationMode.Text)
            {
                case "强制复制":
                    _dropOperationMode = DropOperationMode.ForceCopy;
                    break;
                case "强制移动":
                    _dropOperationMode = DropOperationMode.ForceMove;
                    break;
                case "默认":
                default:
                    _dropOperationMode = DropOperationMode.Default;
                    break;
            }
        }

        /// <summary>
        /// 填充JSON文件下拉列表
        /// </summary>
        private void PopulateJsonFilesDropdown()
        {
            try
            {
                var jsonDir = AppDataPathManager.SavedGridsDirectory;

                var jsonFiles = System.IO.Directory.GetFiles(jsonDir, "*.json")
                    .Select(f => System.IO.Path.GetFileNameWithoutExtension(f))
                    .OrderByDescending(f => f)
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
        /// JSON文件选择改变事件（阶段4：迁移到Presenter）
        /// </summary>
        private void CmbJsonFiles_SelectedValueChanged(object sender, AntdUI.ObjectNEventArgs e)
        {
            if (e.Value is string fileName && !string.IsNullOrEmpty(fileName))
            {
                try
                {
                    var jsonDir = AppDataPathManager.SavedGridsDirectory;
                    var filePath = System.IO.Path.Combine(jsonDir, fileName + ".json");

                    // 同步当前配置名称，用于退出时自动保存到当前选中项
                    CurrentConfigName = fileName;

                    // 使用 Presenter 加载并设置到 FileList
                    var dataList = _presenter.LoadFromJsonFile(filePath);
                    if (dataList != null)
                    {
                        FileList = new BindingList<FileRenameInfo>(dataList);
                        RefreshFileTable();
                    }

                    UpdateStatusLabel($"已加载: {fileName}");
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"加载JSON文件失败: {ex.Message}");
                    UpdateStatusLabel($"加载失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 保存JSON按钮点击事件（阶段4：迁移到Presenter）
        /// </summary>
        private void BtnSaveJson_Click(object sender, EventArgs e)
        {
            try
            {
                var jsonDir = AppDataPathManager.SavedGridsDirectory;

                string fileName = null;

                // 1. 优先使用用户输入的文本（_cmbJsonFiles可输入）
                if (!string.IsNullOrEmpty(_cmbJsonFiles.Text))
                {
                    fileName = _cmbJsonFiles.Text.Trim();
                }

                // 2. 如果没有输入，尝试使用选中项
                if (string.IsNullOrEmpty(fileName) && _cmbJsonFiles.SelectedIndex >= 0)
                {
                    fileName = _cmbJsonFiles.SelectedValue?.ToString();
                }

                // 3. 如果都没有，提示用户输入
                if (string.IsNullOrEmpty(fileName))
                {
                   string defaultName = $"Grid_{DateTime.Now:yyyyMMdd_HHmmss}";
                   if (InputBox("保存配置", "请输入配置名称:", ref defaultName) == DialogResult.OK)
                   {
                       fileName = defaultName;
                   }
                   else
                   {
                       return; // 用户取消
                   }
                }

                if (string.IsNullOrEmpty(fileName)) return;

                // 自动添加 .json 后缀
                if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    fileName += ".json";
                }

                var filePath = System.IO.Path.Combine(jsonDir, fileName);

                // 使用 Presenter 保存
                _presenter.SaveToJsonFile(filePath);

                // 刷新下拉列表
                PopulateJsonFilesDropdown();

                // 尝试选中刚保存的文件
                var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                if (string.IsNullOrEmpty(InputDirectory)) // 只有在没有设置 InputDir 触发 setter 逻辑干扰时才手动选
                {
                     // 简单设置 Text 或遍历选中
                     for(int i=0; i<_cmbJsonFiles.Items.Count; i++) {
                         if (_cmbJsonFiles.Items[i]?.ToString() == nameWithoutExt) {
                             _cmbJsonFiles.SelectedIndex = i;
                             break;
                         }
                     }
                }

                UpdateStatusLabel($"已保存: {nameWithoutExt}");
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
            // 列已在 Designer.cs 中定义，这里只配置数据和事件
            
            // 禁用自动生成列，只显示设计器定义的列
            _fileTable.AutoGenerateColumns = false;

            // 隐藏行头箭头 (通过Padding将内容挤出可视区域，保留自定义绘制的序号)
            _fileTable.RowHeadersDefaultCellStyle.Padding = new Padding(_fileTable.RowHeadersWidth, 0, 0, 0);

            // 初始化999行空数据（与Form1保持一致）
            var fileBindingList = new BindingList<FileRenameInfo>();
            for (int i = 0; i < 999; i++)
            {
                fileBindingList.Add(new FileRenameInfo());
            }
            _fileTable.DataSource = fileBindingList;

            // 绑定右键菜单事件
            _fileTable.CellClick += FileTable_CellClick;
            _fileTable.CellMouseUp += FileTable_CellMouseUp;

            // 绑定行头序号绘制事件
            _fileTable.RowPostPaint += FileTable_RowPostPaint;

            // ✅ 绑定行预绘制事件，用于高亮”已匹配”的行
            _fileTable.RowPrePaint += FileTable_RowPrePaint;

            // ✅ 绑定单元格格式化事件，用于高亮平张/卷装列
            _fileTable.CellFormatting += FileTable_CellFormatting;

            // 初始化列头右键菜单
            InitializeColumnHeaderContextMenu();
            
            // 加载保存的列配置
            LoadColumnSettings();

            // 监听列顺序变化，自动保存
            _fileTable.ColumnDisplayIndexChanged += FileTable_ColumnDisplayIndexChanged;
        }
        
        /// <summary>
        /// 在行头绘制序号
        /// </summary>
        private void FileTable_RowPostPaint(object sender, System.Windows.Forms.DataGridViewRowPostPaintEventArgs e)
        {
            // 绘制行号 (从1开始)
            var rowNumber = (e.RowIndex + 1).ToString();
            
            // 使用浅灰色，不突兀
            using (var brush = new SolidBrush(Color.FromArgb(160, 160, 160)))
            {
                // 计算绘制位置（水平和垂直都居中）
                var bounds = e.RowBounds;
                var headerWidth = _fileTable.RowHeadersWidth;
                var size = e.Graphics.MeasureString(rowNumber, _fileTable.Font);
                var x = (headerWidth - size.Width) / 2;
                var y = bounds.Top + (bounds.Height - size.Height) / 2;
                
                e.Graphics.DrawString(rowNumber, _fileTable.Font, brush, x, y);
            }
        }

        /// <summary>
        /// 行预绘制 - 用于高亮状态
        /// </summary>
        private void FileTable_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            if (e.RowIndex >= 0 && _fileTable.Rows[e.RowIndex].DataBoundItem is FileRenameInfo info)
            {
                // 如果状态为"已匹配"，则背景色设为浅绿色
                if (info.Status == "已匹配")
                {
                    _fileTable.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240); // Honeydew
                    _fileTable.Rows[e.RowIndex].DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 240, 200);
                }
            }
        }

        /// <summary>
        /// 单元格格式化事件处理 - 高亮平张/卷装列
        /// </summary>
        private void FileTable_CellFormatting(object sender, System.Windows.Forms.DataGridViewCellFormattingEventArgs e)
        {
            // 只处理平张列和卷装列
            string columnName = _fileTable.Columns[e.ColumnIndex].Name;
            if (columnName != "LayoutCount" && columnName != "RollMaterialLayoutCount")
                return;

            // 获取当前行的数据
            if (e.RowIndex < 0 || e.RowIndex >= _fileTable.Rows.Count)
                return;

            var row = _fileTable.Rows[e.RowIndex];
            if (row.DataBoundItem is not FileRenameInfo fileInfo)
                return;

            // 获取该行的材料类型（ImpositionMode），而不是全局的
            string rowMaterialType = fileInfo.ImpositionMode ?? "";

            // 只有该行添加时启用"同时输出排版模式布局数"设置时才高亮（记录在行数据中）
            if (!fileInfo.HighlightApplied)
                return;

            // 获取单元格值
            var cellValue = e.Value?.ToString() ?? "";

            // 如果单元格有值，才进行高亮
            if (!string.IsNullOrEmpty(cellValue))
            {
                // 橙色高亮
                Color highlightColor = Color.FromArgb(255, 240, 200); // 浅橙色

                // 根据该行的 ImpositionMode 决定是否高亮
                if (columnName == "LayoutCount" && rowMaterialType == "平张")
                {
                    e.CellStyle.BackColor = highlightColor;
                }
                else if (columnName == "RollMaterialLayoutCount" && rowMaterialType == "卷装")
                {
                    e.CellStyle.BackColor = highlightColor;
                }
            }
        }

        // ... Existing code ...

        private void LoadColumnSettings()
        {
            try
            {
                var path = GetConfigPath();
                if (!System.IO.File.Exists(path)) return;

                var json = System.IO.File.ReadAllText(path);
                var configs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ColumnConfig>>(json);
                
                if (configs == null || configs.Count == 0) return;

                // 按保存的配置应用列属性
                foreach (var config in configs)
                {
                    if (_fileTable.Columns.Contains(config.Key))
                    {
                        var col = _fileTable.Columns[config.Key];
                        col.Visible = config.Visible;
                        col.DisplayIndex = config.Index;
                        // 使用 FillWeight 而不是 Width，因为 AutoSizeColumnsMode = Fill
                        if (config.FillWeight > 0)
                        {
                            col.FillWeight = config.FillWeight;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载列配置失败: {ex.Message}");
            }
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
            
            // 1. "隐藏列" 子菜单
            var hideColumnsItem = new ToolStripMenuItem("隐藏列");
            
            // 防止点击子菜单项时关闭菜单
            hideColumnsItem.DropDown.Closing += (s, e) =>
            {
                if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                {
                    e.Cancel = true;
                }
            };
            
            // 为每一列添加显示/隐藏选项到子菜单
            foreach (System.Windows.Forms.DataGridViewColumn column in _fileTable.Columns)
            {
                var item = new ToolStripMenuItem(column.HeaderText)
                {
                    Checked = column.Visible,
                    CheckOnClick = true,
                    Tag = column.Name
                };
                item.CheckedChanged += ColumnMenuItem_CheckedChanged;
                hideColumnsItem.DropDownItems.Add(item);
            }
            _columnHeaderMenu.Items.Add(hideColumnsItem);

            _columnHeaderMenu.Items.Add(new ToolStripSeparator());

            // 2. 保存设置
            var saveItem = new ToolStripMenuItem("保存配置");
            saveItem.Click += (s, e) => SaveColumnSettings();
            _columnHeaderMenu.Items.Add(saveItem);

            // 3. 恢复原始
            var restoreItem = new ToolStripMenuItem("恢复原始");
            restoreItem.Click += (s, e) => RestoreColumnDefaults();
            _columnHeaderMenu.Items.Add(restoreItem);
            
            // 绑定列头右键菜单
            _fileTable.ColumnHeaderMouseClick += FileTable_ColumnHeaderMouseClick;
        }
        
        // 标记是否正在显示列头菜单，用于阻止单元格菜单
        private bool _isShowingHeaderMenu = false;
        
        /// <summary>
        /// 列头右键点击事件
        /// </summary>
        private void FileTable_ColumnHeaderMouseClick(object sender, System.Windows.Forms.DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _isShowingHeaderMenu = true;
                
                // 更新菜单项的选中状态 (在"隐藏列"子菜单中)
                if (_columnHeaderMenu.Items.Count > 0 && _columnHeaderMenu.Items[0] is ToolStripMenuItem hideMenu)
                {
                    foreach (var item in hideMenu.DropDownItems.OfType<ToolStripMenuItem>())
                    {
                        if (item.Tag is string columnName)
                        {
                            var column = _fileTable.Columns[columnName];
                            if (column != null)
                            {
                                item.Checked = column.Visible;
                            }
                        }
                    }
                }
                // 显示列头右键菜单（居中于点击的列）
                var rect = _fileTable.GetCellDisplayRectangle(e.ColumnIndex, -1, true);
                var menuX = rect.Left + (rect.Width - _columnHeaderMenu.Width) / 2;
                _columnHeaderMenu.Show(_fileTable, menuX, rect.Bottom);
            }
        }
        
        /// <summary>
        /// 列菜单项选中状态变化事件
        /// </summary>
        private void ColumnMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is string columnName)
            {
                var column = _fileTable.Columns[columnName];
                if (column != null)
                {
                    // 更新列的可见性
                    column.Visible = menuItem.Checked;
                }
            }
        }

        private void FileTable_CellClick(object sender, System.Windows.Forms.DataGridViewCellEventArgs e)
        {
            _currentRowIndex = e.RowIndex;
            _currentColumnIndex = e.ColumnIndex;
        }

        private void FileTable_CellMouseUp(object sender, System.Windows.Forms.DataGridViewCellMouseEventArgs e)
        {
            // 如果刚显示了列头菜单，跳过单元格菜单
            if (_isShowingHeaderMenu)
            {
                _isShowingHeaderMenu = false;
                return;
            }
            
            // 确保不在列头区域，并且有有效的行索引
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                UpdateContextMenuForColumn();
                var rect = _fileTable.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
                _contextMenu.Show(_fileTable, rect.Left, rect.Bottom);
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
            
            if (_currentColumnIndex < 0 || _fileTable.Columns.Count == 0) 
            {
                CreateDefaultContextMenu();
                return;
            }

            string columnName = _fileTable.Columns[_currentColumnIndex].Name;
            
            // ✅ DEBUG: 输出当前点击的列名，帮助调试菜单不显示的问题
            LogHelper.Debug($"[UpdateContextMenuForColumn] Index={_currentColumnIndex}, Name='{columnName}', Header='{_fileTable.Columns[_currentColumnIndex].HeaderText}'");

            switch (columnName)
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
                case "OrderNumber":
                    CreateOrderNumberContextMenu();
                    break;
                case "OriginalName":
                    CreateOriginalNameContextMenu();
                    break;
                default:
                    CreateDefaultContextMenu();
                    break;
            }
        }

        private void CreateDefaultContextMenu()
        {
            // ❌ 移除 Clear，防止覆盖前面添加的自定义菜单项
            // _contextMenu.Items.Clear();
            
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

        private void CreateOriginalNameContextMenu()
        {
            // ❌ 移除 redundant Clear
            // _contextMenu.Items.Clear();

            var extractItem = new ToolStripMenuItem("提取数量（从文件名）");
            extractItem.Click += (s, e) => ShowExtractQuantityInputDialog();
            _contextMenu.Items.Add(extractItem);

            _contextMenu.Items.Add(new ToolStripSeparator());
            CreateDefaultContextMenu();
        }

        private void ShowExtractQuantityInputDialog()
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = "从文件名提取数量";
                inputForm.Width = 350;
                inputForm.Height = 180;
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var label = new Label { Text = "请输入单位字符 (如 '张', '个', 'F'):\n程序将提取该字符前的一组数字。", Left = 15, Top = 15, Width = 300, Height = 40 };
                var textBox = new TextBox { Left = 15, Top = 60, Width = 300, Text = "张" }; // 默认值
                var okButton = new Button { Text = "确定", Left = 60, Top = 95, Width = 100, DialogResult = DialogResult.OK };
                var cancelButton = new Button { Text = "取消", Left = 180, Top = 95, Width = 100, DialogResult = DialogResult.Cancel };

                inputForm.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    string unit = textBox.Text.Trim();
                    if (!string.IsNullOrEmpty(unit))
                    {
                        BatchExtractQuantityFromOriginalName(unit);
                    }
                    else
                    {
                        MessageBox.Show("单位字符不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void BatchExtractQuantityFromOriginalName(string unit)
        {
            var rowIndices = GetSelectedRowIndices();
            if (rowIndices.Count == 0) return;

            // 构建正则：寻找单位前的数字
            // pattern explanation: (\d+)\s*unit  -> 捕获数字，允许中间有些许空格
            string pattern = $@"(\d+)\s*{Regex.Escape(unit)}";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList)
            {
                int successCount = 0;
                foreach (int rowIndex in rowIndices)
                {
                    if (rowIndex >= 0 && rowIndex < bindingList.Count)
                    {
                        var item = bindingList[rowIndex];
                        if (!string.IsNullOrEmpty(item.OriginalName))
                        {
                            var match = regex.Match(item.OriginalName);
                            if (match.Success)
                            {
                                item.Quantity = match.Groups[1].Value;
                                successCount++;
                            }
                        }
                    }
                }

                if (successCount > 0)
                {
                    _fileTable.Invalidate();
                    LogHelper.Info($"从 {successCount} 个文件中提取了数量 (单位: {unit})");
                }
                else
                {
                    MessageBox.Show($"未在选中的文件中找到匹配单位 '{unit}' 的数字。", "提取结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void CreateOrderNumberContextMenu()
        {
            // ❌ 移除 redundant Clear
            // _contextMenu.Items.Clear();

            var generateItem = new ToolStripMenuItem("批量生产订单号");
            generateItem.Click += (s, e) => ShowOrderNumberInputDialog();
            _contextMenu.Items.Add(generateItem);

            _contextMenu.Items.Add(new ToolStripSeparator());
            CreateDefaultContextMenu();
        }

        private void ShowOrderNumberInputDialog()
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = "批量生成订单号";
                inputForm.Width = 350;
                inputForm.Height = 280;
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                int top = 15;
                int labelWidth = 60;
                int inputWidth = 240;
                int spacing = 35;

                // 前缀
                var lblPrefix = new Label { Text = "前缀:", Left = 15, Top = top + 3, Width = labelWidth };
                var txtPrefix = new TextBox { Left = 80, Top = top, Width = inputWidth };
                
                // 起始号
                top += spacing;
                var lblStart = new Label { Text = "起始号:", Left = 15, Top = top + 3, Width = labelWidth };
                var numStart = new NumericUpDown { Left = 80, Top = top, Width = inputWidth, Minimum = 0, Maximum = 99999999, Value = 1 };

                // 增量
                top += spacing;
                var lblStep = new Label { Text = "增量:", Left = 15, Top = top + 3, Width = labelWidth };
                var numStep = new NumericUpDown { Left = 80, Top = top, Width = inputWidth, Minimum = 1, Maximum = 1000, Value = 1 };

                // 位数
                top += spacing;
                var lblDigits = new Label { Text = "位数:", Left = 15, Top = top + 3, Width = labelWidth };
                var numDigits = new NumericUpDown { Left = 80, Top = top, Width = inputWidth, Minimum = 1, Maximum = 10, Value = 3 };

                // 后缀
                top += spacing;
                var lblSuffix = new Label { Text = "后缀:", Left = 15, Top = top + 3, Width = labelWidth };
                var txtSuffix = new TextBox { Left = 80, Top = top, Width = inputWidth };

                // 按钮
                top += spacing + 10;
                var okButton = new Button { Text = "确定", Left = 80, Top = top, Width = 100, DialogResult = DialogResult.OK };
                var cancelButton = new Button { Text = "取消", Left = 200, Top = top, Width = 100, DialogResult = DialogResult.Cancel };

                inputForm.Controls.AddRange(new Control[] { 
                    lblPrefix, txtPrefix, 
                    lblStart, numStart, 
                    lblStep, numStep, 
                    lblDigits, numDigits,
                    lblSuffix, txtSuffix, 
                    okButton, cancelButton 
                });
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    BatchGenerateOrderNumbers(
                        txtPrefix.Text, 
                        (int)numStart.Value, 
                        (int)numStep.Value, 
                        (int)numDigits.Value, 
                        txtSuffix.Text
                    );
                }
            }
        }

        private void BatchGenerateOrderNumbers(string prefix, int start, int step, int digits, string suffix)
        {
            var rowIndices = GetSelectedRowIndices();
            if (rowIndices.Count == 0) return;

            // 确保按行号排序，以便序号递增
            var sortedIndices = rowIndices.OrderBy(i => i).ToList();

            if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList)
            {
                int count = 0;
                for (int i = 0; i < sortedIndices.Count; i++)
                {
                    int rowIndex = sortedIndices[i];
                    if (rowIndex >= 0 && rowIndex < bindingList.Count)
                    {
                        var item = bindingList[rowIndex];
                        // 计算当前序号: 起始值 + (索引 * 步长)
                        int currentNum = start + (i * step);
                        string numberPart = currentNum.ToString("D" + digits);
                        
                        item.OrderNumber = $"{prefix}{numberPart}{suffix}";
                        count++;
                    }
                }

                if (count > 0)
                {
                    _fileTable.Invalidate();
                    LogHelper.Info($"批量生成了 {count} 个订单号 (前缀:{prefix}, 起始:{start}, 增量:{step})");
                }
            }
        }



        private void CreateQuantityContextMenu()
        {
            // ❌ 移除 redundant Clear
            // _contextMenu.Items.Clear();
            
            // ✅ 新增：批量设置数量（覆盖）
            var setItem = new ToolStripMenuItem("批量设置数量");
            setItem.Click += (s, e) => ShowQuantityInputDialog(isIncremental: false);
            _contextMenu.Items.Add(setItem);

            // ✅ 原有：批量增减数量（计算）
            var adjustItem = new ToolStripMenuItem("批量增减数量");
            adjustItem.Click += (s, e) => ShowQuantityInputDialog(isIncremental: true);
            _contextMenu.Items.Add(adjustItem);
            
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

        // ✅ 新增：获取所有涉及的行索引（合并 SelectedRows 和 SelectedCells）
        private HashSet<int> GetSelectedRowIndices()
        {
            var rowIndices = new HashSet<int>();

            // 1. 添加选中行的索引
            foreach (DataGridViewRow row in _fileTable.SelectedRows)
            {
                if (row.Index >= 0) rowIndices.Add(row.Index);
            }

            // 2. 添加选中单元格所在的行索引
            foreach (DataGridViewCell cell in _fileTable.SelectedCells)
            {
                if (cell.RowIndex >= 0) rowIndices.Add(cell.RowIndex);
            }

            // 3. 如果没有选中任何行或单元格，但有当前行（光标所在行），则作为 fallback
            if (rowIndices.Count == 0 && _currentRowIndex >= 0)
            {
                rowIndices.Add(_currentRowIndex);
            }

            return rowIndices;
        }

        // ✅ 新增：批量设置单元格值
        private void BatchSetCellValue(string propertyName, string value)
        {
            var rowIndices = GetSelectedRowIndices();
            if (rowIndices.Count == 0) return;

            try
            {
                if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList)
                {
                    int successCount = 0;
                    foreach (int rowIndex in rowIndices)
                    {
                        if (rowIndex >= 0 && rowIndex < bindingList.Count)
                        {
                            var item = bindingList[rowIndex];
                            var prop = typeof(FileRenameInfo).GetProperty(propertyName);
                            if (prop != null)
                            {
                                prop.SetValue(item, value);
                                successCount++;
                            }
                        }
                    }
                    
                    if (successCount > 0)
                    {
                        _fileTable.Invalidate(); // 刷新表格显示
                        LogHelper.Info($"批量更新了 {successCount} 行的 {propertyName} 为 '{value}'");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"批量设置单元格值失败: {ex.Message}");
            }
        }

        // 旧的单行设置方法（保留作为备用）
        private void SetCellValueSingleRow(string propertyName, string value)
        {
             // 逻辑已合并到 BatchSetCellValue，保留此为空方法或直接删除调用
             // 为了保持签名兼容暂时保留，但重定向到 BatchSetCellValue
             BatchSetCellValue(propertyName, value);
        }
        
        // 旧的 SetCellValue 方法已重命名并被 BatchSetCellValue 替代
        private void SetCellValue(string propertyName, string value) => BatchSetCellValue(propertyName, value);

        private void CopySelectedCell()
        {
            try
            {
                if (_currentRowIndex >= 0 && _currentColumnIndex >= 0 &&
                    _fileTable.DataSource is BindingList<FileRenameInfo> bindingList &&
                    _currentRowIndex < bindingList.Count)
                {
                    var item = bindingList[_currentRowIndex];
                    var columnName = _fileTable.Columns[_currentColumnIndex].Name;
                    var prop = typeof(FileRenameInfo).GetProperty(columnName);
                    var value = prop?.GetValue(item)?.ToString() ?? "";
                    Clipboard.SetText(value);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"复制失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 列显示顺序变化时自动保存（防抖处理）
        /// </summary>
        private void FileTable_ColumnDisplayIndexChanged(object sender, DataGridViewColumnEventArgs e)
        {
            _columnSaveTimer?.Stop();
            _columnSaveTimer?.Start();
        }

        private void DeleteSelectedRow()
        {
            var rowIndices = GetSelectedRowIndices().OrderByDescending(i => i).ToList();
            if (rowIndices.Count == 0) return;

            // 批量删除
            if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList)
            {
                foreach (int index in rowIndices)
                {
                    if (index >= 0 && index < bindingList.Count)
                    {
                        bindingList.RemoveAt(index);
                    }
                }
            }
        }

        // ✅ 修改：支持两种模式（增量/覆盖）
        private void ShowQuantityInputDialog(bool isIncremental = false)
        {
            using (var inputForm = new Form())
            {
                inputForm.Text = isIncremental ? "批量增减数量" : "批量设置数量";
                inputForm.Width = 300;
                inputForm.Height = 150;
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;

                var labelText = isIncremental ? "请输入增量值 (例如 1 或 -1):" : "请输入新的数量值:";
                var defaultText = isIncremental ? "1" : "500";

                var label = new Label { Text = labelText, Left = 10, Top = 10, Width = 260 };
                var textBox = new TextBox { Left = 10, Top = 35, Width = 260, Text = defaultText };
                var okButton = new Button { Text = "确定", Left = 100, Top = 70, DialogResult = DialogResult.OK };
                
                inputForm.Controls.AddRange(new Control[] { label, textBox, okButton });
                inputForm.AcceptButton = okButton;

                if (inputForm.ShowDialog() == DialogResult.OK && int.TryParse(textBox.Text, out int value))
                {
                    if (isIncremental)
                    {
                        BatchUpdateQuantity(value); // 增量模式
                    }
                    else
                    {
                        BatchSetQuantity(value); // 覆盖模式
                    }
                }
            }
        }

        // 保持兼容旧签名（默认为增量模式，供其他潜在调用者使用）
        private void ShowQuantityInputDialog() => ShowQuantityInputDialog(true);

        // ✅ 原有：批量增减（逻辑升级为支持多选）
        private void CreateMaterialContextMenu()
        {
            // ❌ 移除 redundant Clear
            // _contextMenu.Items.Clear();
            
            // 获取材料列表
            var materials = MaterialManager.Instance.GetMaterials();
            
            // ✅ 修复：如果列表为空，尝试从 AppSettings 加载
            if ((materials == null || materials.Count == 0) && !string.IsNullOrEmpty(AppSettings.Material))
            {
                var list = AppSettings.Material.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                MaterialManager.Instance.SetMaterials(list);
                materials = MaterialManager.Instance.GetMaterials();
            }

            if (materials != null && materials.Count > 0)
            {
                foreach (var material in materials)
                {
                    var item = new ToolStripMenuItem(material);
                    // ✅ 改为调用批量设置方法
                    item.Click += (s, e) => BatchSetCellValue("Material", material);
                    _contextMenu.Items.Add(item);
                }
                _contextMenu.Items.Add(new ToolStripSeparator());
            }
            
            CreateDefaultContextMenu();
        }

        // ✅ 原有：批量增减（逻辑升级为支持多选）
        private void BatchUpdateQuantity(int delta)
        {
            var rowIndices = GetSelectedRowIndices();
            if (rowIndices.Count == 0) return;
            
            // 批量处理
             if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList)
             {
                 foreach (int index in rowIndices)
                 {
                     UpdateQuantitySingleRow(index, delta);
                 }
                 _fileTable.Invalidate();
             }
        }
        
        // 辅助方法：单行更新数量
        private void UpdateQuantitySingleRow(int rowIndex, int delta)
        {
             if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList && rowIndex >= 0 && rowIndex < bindingList.Count)
             {
                 var item = bindingList[rowIndex];
                 int.TryParse(item.Quantity ?? "0", out int current);
                 item.Quantity = (current + delta).ToString();
             }
        }

        // ✅ 新增：批量设置数量（覆盖）
        private void BatchSetQuantity(int newValue)
        {
            var rowIndices = GetSelectedRowIndices();
            if (rowIndices.Count == 0) return;

             if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList)
             {
                 foreach (int index in rowIndices)
                 {
                     if (index >= 0 && index < bindingList.Count)
                     {
                         bindingList[index].Quantity = newValue.ToString();
                     }
                 }
                 _fileTable.Invalidate();
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
                    // 1. 保存全局设置
                    AppSettings.Set("TetBleedValues", bleed.ToString());
                    AppSettings.Save();
                    LogHelper.Info($"设置全局出血值为: {bleed}mm");
                    
                    // 2. ✅ 更新当前选中的文件对象
                    BatchUpdateBleed(bleed);
                }
            }
        }
        
        // ✅ 辅助方法：批量更新出血值
        private void BatchUpdateBleed(double bleed)
        {
            var rowIndices = GetSelectedRowIndices();
            if (rowIndices.Count == 0) return;

            string bleedStr = bleed.ToString();
            
            // 批量处理
            if (_fileTable.DataSource is BindingList<FileRenameInfo> bindingList)
            {
                 int count = 0;
                 foreach (int index in rowIndices)
                 {
                     if (index >= 0 && index < bindingList.Count)
                     {
                         var item = bindingList[index];
                         item.TetBleed = bleedStr;
                         
                         // ✅ 关键修复：更新出血值后，重新计算尺寸字符串用于显示
                         // 假设 Dimensions 格式为 "宽x高+出血"
                         RecalculateDimensionsString(item);
                         
                         count++;
                     }
                 }
                 if (count > 0)
                 {
                     _fileTable.Invalidate();
                     LogHelper.Info($"批量更新了 {count} 行的出血值为 {bleedStr}");
                 }
            }
        }

        /// <summary>
        /// 根据W/H和出血值重新格式化Dimensions字符串
        /// </summary>
        private void RecalculateDimensionsString(FileRenameInfo item)
        {
            string w = item.Width;
            string h = item.Height;

            // Fallback: 如果 Width/Height 属性为空，尝试从现有的 Dimensions 字符串解析
            if (string.IsNullOrEmpty(w) || string.IsNullOrEmpty(h))
            {
                var match = Regex.Match(item.Dimensions ?? "", @"^(\d+\.?\d*)x(\d+\.?\d*)");
                if (match.Success)
                {
                    w = match.Groups[1].Value;
                    h = match.Groups[2].Value;
                    // 回填属性以便下次使用
                    item.Width = w;
                    item.Height = h;
                }
            }

            if (!string.IsNullOrEmpty(w) && !string.IsNullOrEmpty(h))
            {
                 string dim = $"{w}x{h}";
                 if (double.TryParse(item.TetBleed, out double b) && b > 0)
                 {
                     // 如果有出血值，追加 "+N"
                     dim += $"+{item.TetBleed}";
                 }
                 item.Dimensions = dim;
            }
        }

        private void RefreshData()
        {
            _fileTable.Invalidate();
        }

        /// <summary>
        /// 阶段5：同步初始状态（从Presenter获取）
        /// </summary>
        private void SyncInitialState()
        {
            // 从Presenter获取输入目录并更新下拉框
            UpdateInputDirSelect();

            // 正则表达式已在Presenter初始化时更新
            // 模式按钮状态
            UpdateModeButtonState();

            // 监控按钮状态
            UpdateMonitorButtonState();

            // 手动/批量模式按钮状态
            UpdateImmediateModeButtonState(IsImmediateMode);

            // 更新状态栏显示（事件分组预览）
            UpdateEventGroupPreview();
        }

        /// <summary>
        /// 更新输入目录下拉框选项和选中值
        /// </summary>
        private void UpdateInputDirSelect()
        {
            var history = AppSettings.Instance.InputDirHistory;
            var currentDir = InputDirectory;
            var lastSelectedDir = AppSettings.LastInputDir;

            _cmbInputDir.Items.Clear();
            foreach (var dir in history)
            {
                _cmbInputDir.Items.Add(dir);
            }

            // 如果当前目录不在历史中，添加它
            if (!string.IsNullOrEmpty(currentDir) && !history.Contains(currentDir))
            {
                _cmbInputDir.Items.Insert(0, currentDir);
            }

            // 如果上次选择的目录不在历史中，添加它
            if (!string.IsNullOrEmpty(lastSelectedDir) && !history.Contains(lastSelectedDir) && lastSelectedDir != currentDir)
            {
                // 检查是否已经在列表中
                bool exists = false;
                for (int i = 0; i < _cmbInputDir.Items.Count; i++)
                {
                    if (_cmbInputDir.Items[i]?.ToString() == lastSelectedDir)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    _cmbInputDir.Items.Insert(0, lastSelectedDir);
                }
            }

            // 设置选中值（优先级：当前目录 > 上次选择 > 第一项）
            // 🔧 修复：优先使用当前 InputDirectory，因为它已经被 Presenter 更新了
            string targetDir = currentDir;
            if (string.IsNullOrEmpty(targetDir))
            {
                targetDir = lastSelectedDir;
            }

            if (!string.IsNullOrEmpty(targetDir))
            {
                // 查找匹配的项并设置选中
                for (int i = 0; i < _cmbInputDir.Items.Count; i++)
                {
                    if (_cmbInputDir.Items[i]?.ToString() == targetDir)
                    {
                        _cmbInputDir.SelectedIndex = i;
                        return;
                    }
                }
            }

            // 如果没有找到匹配项，选择第一项（如果存在）
            if (_cmbInputDir.Items.Count > 0)
            {
                _cmbInputDir.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 阶段2：根据监控状态更新监控按钮显示
        /// </summary>
        private void UpdateMonitorButtonState()
        {
            if (IsMonitoring)
            {
                _btnMonitor.Text = "停止监控";
                _btnMonitor.Type = AntdUI.TTypeMini.Error;
            }
            else
            {
                _btnMonitor.Text = "开始监控";
                _btnMonitor.Type = AntdUI.TTypeMini.Primary;
            }
            UpdateModeStatusDisplay();
        }

        /// <summary>
        /// 阶段5：根据模式状态更新模式按钮显示
        /// </summary>
        private void UpdateModeButtonState()
        {
            _btnToggleMode.Text = IsCopyMode ? "剪切模式" : "复制模式";
            UpdateModeStatusDisplay();
        }

        /// <summary>
        /// 阶段5：根据手动/批量模式状态更新按钮显示
        /// </summary>
        private void UpdateImmediateModeButtonState()
        {
            if (IsImmediateMode)
            {
                _btnManualMode.Text = "已启动手动模式";
                _btnBatchMode.Text = "批量模式";
            }
            else
            {
                _btnManualMode.Text = "手动模式";
                _btnBatchMode.Text = "已启动批量模式";
            }
            UpdateModeStatusDisplay();
        }

        /// <summary>
        /// 更新状态栏中部的模式状态显示
        /// </summary>
        private void UpdateModeStatusDisplay()
        {
            if (_modeStatusLabel == null) return;

            var modes = new List<string>
            {
                IsCopyMode ? "复制" : "剪切",
                IsImmediateMode ? "手动" : "批量",
                IsMonitoring ? "监控中" : "未监控"
            };
            _modeStatusLabel.Text = string.Join(" │ ", modes);
        }

        /// <summary>
        /// 更新状态栏右侧的事件分组组合预览
        /// </summary>
        public void UpdateEventGroupPreview()
        {
            if (_eventPreviewLabel == null) return;

            try
            {
                _eventPreviewLabel.Text = "组合：" + GenerateEventGroupPreview();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"更新事件分组预览失败: {ex.Message}");
                _eventPreviewLabel.Text = "组合：加载失败";
            }
        }

        /// <summary>
        /// 生成事件分组组合预览字符串
        /// </summary>
        private string GenerateEventGroupPreview()
        {
            try
            {
                // 加载事件分组配置
                var config = EventGroupConfigurationService.GetEventGroupConfiguration();
                if (config == null || config.Items == null || config.Items.Count == 0)
                {
                    return "无配置";
                }

                var previewParts = new List<string>();

                // 获取启用的分组项目（按排序）
                var groupedItems = config.GetEnabledGroupedItems();
                foreach (var (group, item) in groupedItems)
                {
                    // 组合格式：前缀+项目名
                    var prefix = !string.IsNullOrEmpty(group.Prefix) ? group.Prefix : "";
                    previewParts.Add($"{prefix}{item.Name}");
                }

                // 获取未分组项目
                var ungroupedItems = config.GetUngroupedItems();
                foreach (var item in ungroupedItems)
                {
                    previewParts.Add(item.Name);
                }

                if (previewParts.Count == 0)
                {
                    return "无启用项";
                }

                // 用下划线连接
                var preview = string.Join("_", previewParts);

                return preview;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"生成事件分组预览失败: {ex.Message}");
                return "加载失败";
            }
        }

        private void SubscribeToDataChanges()
        {
            // 暂时使用空实现，后续可通过事件机制同步数据
            // TODO: 实现从Form1获取数据源并绑定到_fileTable
            // 当前Table将在用户操作时通过Form1的事件更新

            // 订阅配置保存事件，以便主题切换时更新悬浮拖拽窗口
            try
            {
                var eventBus = Services.ServiceLocator.Instance.GetEventBus();
                if (eventBus != null)
                {
                    eventBus.Subscribe<Services.Events.ConfigSavedEvent>(e =>
                    {
                        // 如果悬浮拖拽窗口已创建且未释放，更新其主题设置
                        if (_floatingDropZone != null && !_floatingDropZone.IsDisposed)
                        {
                            _floatingDropZone.UpdateThemeSettings();
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"订阅配置保存事件失败: {ex.Message}");
            }
        }

        protected override void OnRefresh()
        {
            base.OnRefresh();
            SubscribeToDataChanges();
            _fileTable?.Invalidate();
        }


        // --- 列配置持久化逻辑 ---

        private class ColumnConfig
        {
            public string Key { get; set; }
            public bool Visible { get; set; }
            public string Width { get; set; }
            public float FillWeight { get; set; }
            public int Index { get; set; }
        }

        private string GetConfigPath()
        {
            var configDir = System.IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "Config");
            if (!System.IO.Directory.Exists(configDir))
            {
                System.IO.Directory.CreateDirectory(configDir);
            }
            return System.IO.Path.Combine(configDir, "FileTableConfig.json");
        }

        private void SaveColumnSettings()
        {
            try
            {
                var configs = new List<ColumnConfig>();
                foreach (System.Windows.Forms.DataGridViewColumn col in _fileTable.Columns)
                {
                    configs.Add(new ColumnConfig
                    {
                        Key = col.Name,
                        Visible = col.Visible,
                        Width = col.Width.ToString(),
                        FillWeight = col.FillWeight,
                        Index = col.DisplayIndex
                    });
                }

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(configs, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(GetConfigPath(), json);
                
                AntdUI.Message.success(this.FindForm(), "列配置已保存", autoClose: 2);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存列配置失败: {ex.Message}");
                AntdUI.Message.error(this.FindForm(), "保存失败: " + ex.Message, autoClose: 3);
            }
        }



        private void RestoreColumnDefaults()
        {
            try
            {
                var path = GetConfigPath();
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
                
                // 清空当前列
                _fileTable.Columns.Clear();
                
                // 重新初始化默认列
                InitializeFileTable();
                
                AntdUI.Message.success(this.FindForm(), "已恢复默认设置", autoClose: 2);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"恢复默认设置失败: {ex.Message}");
                AntdUI.Message.error(this.FindForm(), "恢复失败: " + ex.Message, autoClose: 3);
            }
        }

        /// <summary>
        /// 阶段5：更新Excel数据（已移除Form1依赖）
        /// </summary>
        public void UpdateExcelData()
        {
            // Excel数据更新已由Presenter处理
            SubscribeToDataChanges();
        }

        #region IFileRenamePanelView 接口实现

        // 属性实现
        public string InputDirectory
        {
            get
            {
                if (_cmbInputDir != null && _cmbInputDir.SelectedIndex >= 0 && _cmbInputDir.Items.Count > 0)
                {
                    return _cmbInputDir.Items[_cmbInputDir.SelectedIndex]?.ToString() ?? "";
                }
                return "";
            }
            set
            {
                if (string.IsNullOrEmpty(value)) return;

                // 查找匹配的项并设置选中
                for (int i = 0; i < _cmbInputDir.Items.Count; i++)
                {
                    if (_cmbInputDir.Items[i]?.ToString() == value)
                    {
                        _cmbInputDir.SelectedIndex = i;
                        return;
                    }
                }
                
                // 如果没有找到，添加到第一项并选中
                _cmbInputDir.Items.Insert(0, value);
                _cmbInputDir.SelectedIndex = 0;
            }
        }

        public bool IsMonitoring
        {
            get => _btnMonitor.Text == "停止监控";
            set
            {
                if (value)
                {
                    _btnMonitor.Text = "停止监控";
                    _btnMonitor.Type = AntdUI.TTypeMini.Error;
                }
                else
                {
                    _btnMonitor.Text = "开始监控";
                    _btnMonitor.Type = AntdUI.TTypeMini.Primary;
                }
            }
        }

        public bool IsCopyMode => _btnToggleMode.Text == "剪切模式";

        // 默认启用手动模式，确保材料选择对话框会弹出
        public bool IsImmediateMode { get; set; } = true;

        public BindingList<FileRenameInfo> FileList
        {
            get => _fileTable.DataSource as BindingList<FileRenameInfo>;
            set
            {
                _fileTable.DataSource = value;
                _fileTable.Invalidate();
            }
        }

        public string SelectedRegexPattern
        {
            get
            {
                // TODO: 从 AntdUI.Select 获取选中的模式
                // 目前暂时返回null
                if (_cmbRegex != null && _cmbRegex.SelectedIndex >= 0 && _cmbRegex.Items.Count > 0)
                {
                    return _cmbRegex.Items[_cmbRegex.SelectedIndex]?.ToString();
                }
                return null;
            }
        }

        public string SelectedRegexValue
        {
            get
            {
                // ✅ 修复：从AppSettings的RegexPatterns获取选中模式对应的正则表达式值
                try
                {
                    var selectedName = SelectedRegexPattern;
                    if (string.IsNullOrEmpty(selectedName))
                    {
                        return null;
                    }

                    // 从AppSettings加载正则表达式字典
                    var regexStr = AppSettings.Instance.RegexPatterns;
                    if (string.IsNullOrEmpty(regexStr))
                    {
                        return null;
                    }

                    // 解析 "名称=表达式|名称=表达式" 格式
                    var patterns = regexStr.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var pattern in patterns)
                    {
                        var parts = pattern.Split(new[] { '=' }, 2);
                        if (parts.Length == 2 && parts[0] == selectedName)
                        {
                            return parts[1]; // 返回正则表达式值
                        }
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"获取正则表达式失败: {ex.Message}", ex);
                    return null;
                }
            }
        }

        public string CurrentConfigName { get; set; }

        public DataTable ExcelData { get; set; }

        // Excel列索引属性（用户在导入时选择的列）
        public int ExcelSearchColumnIndex { get; set; } = -1;
        public int ExcelReturnColumnIndex { get; set; } = -1;
        public int ExcelSerialColumnIndex { get; set; } = -1;

        // UI更新方法实现
        public void UpdateStatus(string message)
        {
            UpdateStatusLabel(message);
        }

        public void UpdateRegexComboBox(List<string> patterns)
        {
            if (patterns == null) return;

            // 保存当前选中的正则表达式
            string lastSelected = AppSettings.LastSelectedRegex;

            _cmbRegex.Items.Clear();
            foreach (var pattern in patterns)
            {
                _cmbRegex.Items.Add(pattern);
            }

            // 尝试恢复上次的选择
            if (!string.IsNullOrEmpty(lastSelected))
            {
                // 尝试找到匹配的项并设置选中
                for (int i = 0; i < _cmbRegex.Items.Count; i++)
                {
                    if (_cmbRegex.Items[i]?.ToString() == lastSelected)
                    {
                        _cmbRegex.SelectedIndex = i;
                        return;
                    }
                }
            }

            // 如果没有恢复成功，选择第一项
            if (_cmbRegex.Items.Count > 0)
            {
                _cmbRegex.SelectedIndex = 0;
            }
        }

        public void UpdateJsonFilesDropdown(List<string> jsonFiles)
        {
            if (jsonFiles == null) return;

            _cmbJsonFiles.Items.Clear();
            foreach (var file in jsonFiles)
            {
                _cmbJsonFiles.Items.Add(file);
            }

            // 如果Presenter已设置CurrentConfigName，则自动选中对应项（用于当日JSON自动创建/加载后同步下拉框）
            if (!string.IsNullOrEmpty(CurrentConfigName))
            {
                for (int i = 0; i < _cmbJsonFiles.Items.Count; i++)
                {
                    if (_cmbJsonFiles.Items[i]?.ToString() == CurrentConfigName)
                    {
                        _cmbJsonFiles.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        public void UpdateMaterialsContextMenu(List<string> materials)
        {
            // TODO: 更新材料右键菜单
        }

        public void RefreshFileTable()
        {
            _fileTable.Invalidate();
        }

        public void UpdateMonitorButtonState(bool isMonitoring)
        {
            IsMonitoring = isMonitoring;
        }

        public void UpdateModeButtonState(bool isCopyMode)
        {
            _btnToggleMode.Text = isCopyMode ? "剪切模式" : "复制模式";
        }

        public void UpdateImmediateModeButtonState(bool isImmediateMode)
        {
            IsImmediateMode = isImmediateMode;
            if (isImmediateMode)
            {
                _btnManualMode.Text = "已启动手动模式";
                _btnBatchMode.Text = "批量模式";
            }
            else
            {
                _btnManualMode.Text = "手动模式";
                _btnBatchMode.Text = "已启动批量模式";
            }
        }

        public void ShowError(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                UpdateStatusLabel($"错误: {message}");
                // 添加 AntdUI 全局错误提示
                AntdUI.Message.error(this.FindForm(), message, autoClose: 3);
            }
        }

        public void ShowSuccess(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                UpdateStatusLabel($"成功: {message}");
                // 添加 AntdUI 全局成功提示
                AntdUI.Message.success(this.FindForm(), message, autoClose: 3);
            }
        }

        public void ShowWarning(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                UpdateStatusLabel($"警告: {message}");
                // 添加 AntdUI 全局警告提示
                AntdUI.Message.warn(this.FindForm(), message, autoClose: 3);
            }
        }

        public void ShowInfo(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                UpdateStatusLabel(message);
            }
        }

        public new bool ShowConfirm(string message, string title = "确认")
        {
            return System.Windows.Forms.MessageBox.Show(message, title,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        // 对话框方法实现
        public string ShowFolderBrowser(string description = "选择文件夹", string selectedPath = "")
        {
            // 使用 Ookii.Dialogs 提供更好的文件夹选择体验
            using (var dialog = new Ookii.Dialogs.WinForms.VistaFolderBrowserDialog())
            {
                dialog.Description = description;
                dialog.UseDescriptionForTitle = true; // 将描述用作对话框标题

                if (!string.IsNullOrEmpty(selectedPath) && Directory.Exists(selectedPath))
                {
                    dialog.SelectedPath = selectedPath;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.SelectedPath;
                }
            }
            return null;
        }

        /// <summary>
        /// 通用输入对话框
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="prompt">提示语</param>
        /// <param name="value">默认值/返回值</param>
        /// <returns>DialogResult</returns>
        private DialogResult InputBox(string title, string prompt, ref string value)
        {
            using (var form = new Form())
            {
                var label = new Label();
                var textBox = new TextBox();
                var buttonOk = new Button();
                var buttonCancel = new Button();

                form.Text = title;
                label.Text = prompt;
                textBox.Text = value;

                buttonOk.Text = "确定";
                buttonCancel.Text = "取消";
                buttonOk.DialogResult = DialogResult.OK;
                buttonCancel.DialogResult = DialogResult.Cancel;

                label.SetBounds(9, 20, 372, 13);
                textBox.SetBounds(12, 36, 372, 20);
                buttonOk.SetBounds(228, 72, 75, 23);
                buttonCancel.SetBounds(309, 72, 75, 23);

                label.AutoSize = true;
                textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
                buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

                form.ClientSize = new Size(396, 107);
                form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
                form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;

                DialogResult result = form.ShowDialog();
                value = textBox.Text;
                return result;
            }
        }

        public string ShowSaveFileDialog(string filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*", string defaultFileName = "")
        {
            using (var dialog = new SaveFileDialog { Filter = filter, FileName = defaultFileName })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }

        public string ShowOpenFileDialog(string filter = "Excel文件 (*.xlsx;*.xls)|*.xlsx;*.xls|所有文件 (*.*)|*.*")
        {
            using (var dialog = new OpenFileDialog { Filter = filter })
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return dialog.FileName;
                }
            }
            return null;
        }

        public DialogResult ShowMaterialSelectionDialog(
            List<string> materials,
            string fileName,
            string regexResult,
            string width,
            string height,
            string tetBleed,
            bool isColumnCombineMode,
            List<string> columnNames,
            Dictionary<string, List<string>> columnItemsMap,
            int initialSerialNumber,
            out MaterialSelectionResult result)
        {
            result = null;

            try
            {
                // 获取 Excel 数据（如果有）
                var excelData = ExcelData;
                var searchColIdx = -1;
                var returnColIdx = -1;
                var serialColIdx = -1;
                var newColIdx = -1;

                if (excelData != null && excelData.Columns.Count > 0)
                {
                    // ✅ 修复：使用从 Presenter 同步的列索引，而不是通过列名查找
                    searchColIdx = ExcelSearchColumnIndex;
                    returnColIdx = ExcelReturnColumnIndex;
                    serialColIdx = ExcelSerialColumnIndex;
                    newColIdx = excelData.Columns.Count - 1; // 最后一列
                    
                    LogHelper.Debug($"[ShowMaterialSelectionDialog] 使用列索引: search={searchColIdx}, return={returnColIdx}, serial={serialColIdx}");
                }

                using (var dialog = new MaterialSelectFormModern(
                    materials: materials,
                    fileName: fileName,
                    regexResult: regexResult,
                    opacity: AppSettings.Opacity,
                    width: width,
                    height: height,
                    excelData: excelData,
                    searchColumnIndex: searchColIdx,
                    returnColumnIndex: returnColIdx,
                    serialColumnIndex: serialColIdx,
                    newColumnIndex: newColIdx,
                    serialNumber: initialSerialNumber.ToString()))
                {
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        result = new MaterialSelectionResult
                        {
                            SelectedMaterial = dialog.SelectedMaterial,
                            SelectedQuantity = dialog.Quantity,
                            SelectedSerialNumber = dialog.SerialNumber,
                            ColumnValues = new Dictionary<string, string>(),
                            IsColumnCombineMode = isColumnCombineMode,
                            ExportPath = dialog.SelectedExportPath ?? AppSettings.LastExportPath ?? "",
                            // ✅ 修复:从对话框读取订单号、尺寸、工艺等值
                            OrderNumber = dialog.OrderNumber ?? "",
                            Dimensions = dialog.AdjustedDimensions ?? "",
                            // ✅ 修复:工艺 = 颜色模式 + 膜类型(使用 FixedField)
                            Process = dialog.FixedField ?? "",
                            // ✅ 修复:从对话框读取行数和列数
                            LayoutRows = dialog.GetRows() > 0 ? dialog.GetRows().ToString() : "",
                            LayoutColumns = dialog.GetColumns() > 0 ? dialog.GetColumns().ToString() : "",
                            // ✅ 修复:从对话框读取形状处理信息
                            SelectedShape = (int)dialog.SelectedShape,
                            RoundRadius = dialog.RoundRadius,
                            IsShapeSelected = dialog.GetIsShapeSelected(),
                            CornerRadius = dialog.GetCompatibleCornerRadius(),
                            // ✅ 修复:从对话框读取旋转信息
                            RotationAngle = dialog.GetRotationAngle(),
                            NeedsRotation = dialog.GetRotationAngle() != 0,
                            // ✅ 修复:从对话框读取排版信息(用于折手模式空白页功能)
                            EnableImposition = dialog.GetIsImpositionEnabled(),
                            LayoutMode = dialog.GetLayoutMode(),
                            LayoutQuantity = dialog.GetLayoutQuantity(),
                            // ✅ 修复:从对话框读取标识页信息
                            AddIdentifierPage = dialog.AddIdentifierPage,
                            IdentifierPageContent = dialog.AddIdentifierPage ? dialog.GenerateIdentifierPageContent() : "",
                            // ✅ 修复:从对话框读取排版材料类型
                            ImpositionMaterialType = dialog.ImpositionMaterialType
                        };
                        return DialogResult.OK;
                    }
                    return DialogResult.Cancel;
                }
            }
            catch (Exception ex)
            {
                ShowError($"显示材料选择对话框失败: {ex.Message}");
                return DialogResult.Cancel;
            }
        }

        // 进度显示方法实现
        public void ShowProgress(string message)
        {
            ShowLoading(message);
        }

        public void UpdateProgress(int current, int total, string message = "")
        {
            // TODO: 实现进度更新
        }

        public void HideProgress()
        {
            HideLoading();
        }

        #endregion

        #region IExcelParentView 接口实现

        /// <summary>
        /// IExcelParentView 接口实现 - 更新状态
        /// </summary>
        void IExcelParentView.UpdateStatus(string message)
        {
            UpdateStatus(message);
        }

        /// <summary>
        /// IExcelParentView 接口实现 - 显示错误
        /// </summary>
        void IExcelParentView.ShowError(string message)
        {
            ShowError(message);
        }

        #endregion

        /// <summary>
        /// 阶段5：清理资源（已移除Form1依赖）
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 释放 Presenter
                // (_presenter as IDisposable)?.Dispose();

                _contextMenu?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
