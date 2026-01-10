namespace WindowsFormsApp3.Forms.Panels
{
    partial class FileRenamePanel
    {
        private System.ComponentModel.IContainer components = null;
        
        // 顶部控制面板
        private System.Windows.Forms.Panel _topControlPanel;
        
        // 布局容器
        private System.Windows.Forms.TableLayoutPanel _layoutPanel;
        private System.Windows.Forms.Panel _inputPanel;
        private System.Windows.Forms.Panel _regexPanel;
        private System.Windows.Forms.FlowLayoutPanel _topBtnFlow;
        private System.Windows.Forms.FlowLayoutPanel _bottomActionFlow;
        private System.Windows.Forms.FlowLayoutPanel _renameActionFlow;
        
        // AntdUI 输入控件
        private AntdUI.Select _cmbInputDir;
        private AntdUI.Select _cmbRegex;
        
        // AntdUI 按钮控件
        private AntdUI.Button _btnSelectDir;
        private AntdUI.Button _btnMonitor;
        private AntdUI.Button _btnImportExcel;
        private AntdUI.Button _btnClearExcel;
        private AntdUI.Button _btnExportExcel;
        private AntdUI.Button _btnToggleMode;
        private AntdUI.Button _btnManualMode;
        private AntdUI.Button _btnBatchMode;
        private AntdUI.Button _btnRename;
        
        // Krypton DataGridView - 显示文件列表
        private Krypton.Toolkit.KryptonDataGridView _fileTable;
        
        // JSON 管理控件
        private AntdUI.Select _cmbJsonFiles;
        private AntdUI.Button _btnSaveJson;
        
        // 状态栏控件
        private System.Windows.Forms.Panel _statusPanel;
        private System.Windows.Forms.FlowLayoutPanel _statusFlowPanel;
        private AntdUI.Label _statusLabel;
        private AntdUI.Label _modeStatusLabel;
        private AntdUI.Label _eventPreviewLabel;

        private void InitializeComponent()
        {
            this._topControlPanel = new System.Windows.Forms.Panel();
            this._layoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this._inputPanel = new System.Windows.Forms.Panel();
            this._cmbInputDir = new AntdUI.Select();
            this._regexPanel = new System.Windows.Forms.Panel();
            this._cmbRegex = new AntdUI.Select();
            this._topBtnFlow = new System.Windows.Forms.FlowLayoutPanel();
            this._btnSelectDir = new AntdUI.Button();
            this._btnMonitor = new AntdUI.Button();
            this._bottomActionFlow = new System.Windows.Forms.FlowLayoutPanel();
            this._cmbJsonFiles = new AntdUI.Select();
            this._btnSaveJson = new AntdUI.Button();
            this._btnImportExcel = new AntdUI.Button();
            this._btnClearExcel = new AntdUI.Button();
            this._btnExportExcel = new AntdUI.Button();
            this._renameActionFlow = new System.Windows.Forms.FlowLayoutPanel();
            this._btnToggleMode = new AntdUI.Button();
            this._btnManualMode = new AntdUI.Button();
            this._btnBatchMode = new AntdUI.Button();
            this._btnRename = new AntdUI.Button();
            this._fileTable = new Krypton.Toolkit.KryptonDataGridView();
            this.SerialNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OriginalName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NewName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RegexResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.OrderNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Material = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Quantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Dimensions = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CompositeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LayoutRows = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.LayoutColumns = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Process = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.PageCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Time = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._statusPanel = new System.Windows.Forms.Panel();
            this._statusFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this._statusLabel = new AntdUI.Label();
            this._modeStatusLabel = new AntdUI.Label();
            this._eventPreviewLabel = new AntdUI.Label();
            this._topControlPanel.SuspendLayout();
            this._layoutPanel.SuspendLayout();
            this._inputPanel.SuspendLayout();
            this._regexPanel.SuspendLayout();
            this._topBtnFlow.SuspendLayout();
            this._bottomActionFlow.SuspendLayout();
            this._renameActionFlow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._fileTable)).BeginInit();
            this._statusPanel.SuspendLayout();
            this._statusFlowPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _topControlPanel
            // 
            this._topControlPanel.Controls.Add(this._layoutPanel);
            this._topControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._topControlPanel.Location = new System.Drawing.Point(0, 0);
            this._topControlPanel.Name = "_topControlPanel";
            this._topControlPanel.Padding = new System.Windows.Forms.Padding(10, 1, 10, 5);
            this._topControlPanel.Size = new System.Drawing.Size(935, 110);
            this._topControlPanel.TabIndex = 1;
            // 
            // _layoutPanel
            // 
            this._layoutPanel.ColumnCount = 3;
            this._layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 160F));
            this._layoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this._layoutPanel.Controls.Add(this._inputPanel, 0, 0);
            this._layoutPanel.Controls.Add(this._regexPanel, 1, 0);
            this._layoutPanel.Controls.Add(this._topBtnFlow, 2, 0);
            this._layoutPanel.Controls.Add(this._bottomActionFlow, 0, 1);
            this._layoutPanel.Controls.Add(this._renameActionFlow, 2, 1);
            this._layoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._layoutPanel.Location = new System.Drawing.Point(10, 1);
            this._layoutPanel.Name = "_layoutPanel";
            this._layoutPanel.RowCount = 2;
            this._layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._layoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._layoutPanel.Size = new System.Drawing.Size(915, 104);
            this._layoutPanel.TabIndex = 0;
            // 
            // _inputPanel
            // 
            this._inputPanel.Controls.Add(this._cmbInputDir);
            this._inputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._inputPanel.Location = new System.Drawing.Point(0, 0);
            this._inputPanel.Margin = new System.Windows.Forms.Padding(0);
            this._inputPanel.Name = "_inputPanel";
            this._inputPanel.Padding = new System.Windows.Forms.Padding(0, 5, 5, 5);
            this._inputPanel.Size = new System.Drawing.Size(407, 52);
            this._inputPanel.TabIndex = 0;
            // 
            // _cmbInputDir
            // 
            this._cmbInputDir.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbInputDir.Location = new System.Drawing.Point(0, 5);
            this._cmbInputDir.Name = "_cmbInputDir";
            this._cmbInputDir.PlaceholderText = "请选择文件夹路径...";
            this._cmbInputDir.Size = new System.Drawing.Size(402, 42);
            this._cmbInputDir.TabIndex = 0;
            // 
            // _regexPanel
            // 
            this._regexPanel.Controls.Add(this._cmbRegex);
            this._regexPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._regexPanel.Location = new System.Drawing.Point(407, 0);
            this._regexPanel.Margin = new System.Windows.Forms.Padding(0);
            this._regexPanel.Name = "_regexPanel";
            this._regexPanel.Padding = new System.Windows.Forms.Padding(5);
            this._regexPanel.Size = new System.Drawing.Size(160, 52);
            this._regexPanel.TabIndex = 1;
            // 
            // _cmbRegex
            // 
            this._cmbRegex.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cmbRegex.List = true;
            this._cmbRegex.Location = new System.Drawing.Point(5, 5);
            this._cmbRegex.Name = "_cmbRegex";
            this._cmbRegex.PlaceholderText = "选择正则规则";
            this._cmbRegex.Size = new System.Drawing.Size(150, 42);
            this._cmbRegex.TabIndex = 0;
            // 
            // _topBtnFlow
            // 
            this._topBtnFlow.AutoSize = true;
            this._topBtnFlow.Controls.Add(this._btnSelectDir);
            this._topBtnFlow.Controls.Add(this._btnMonitor);
            this._topBtnFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._topBtnFlow.Location = new System.Drawing.Point(567, 0);
            this._topBtnFlow.Margin = new System.Windows.Forms.Padding(0);
            this._topBtnFlow.Name = "_topBtnFlow";
            this._topBtnFlow.Size = new System.Drawing.Size(348, 52);
            this._topBtnFlow.TabIndex = 2;
            this._topBtnFlow.WrapContents = false;
            // 
            // _btnSelectDir
            // 
            this._btnSelectDir.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnSelectDir.IconSvg = "FolderOpenOutlined";
            this._btnSelectDir.Location = new System.Drawing.Point(0, 0);
            this._btnSelectDir.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnSelectDir.Name = "_btnSelectDir";
            this._btnSelectDir.Size = new System.Drawing.Size(94, 32);
            this._btnSelectDir.TabIndex = 0;
            this._btnSelectDir.Text = "选择文件夹";
            // 
            // _btnMonitor
            // 
            this._btnMonitor.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnMonitor.IconSvg = "EyeOutlined";
            this._btnMonitor.Location = new System.Drawing.Point(99, 0);
            this._btnMonitor.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnMonitor.Name = "_btnMonitor";
            this._btnMonitor.Size = new System.Drawing.Size(82, 32);
            this._btnMonitor.TabIndex = 1;
            this._btnMonitor.Text = "开始监控";
            this._btnMonitor.Type = AntdUI.TTypeMini.Primary;
            // 
            // _bottomActionFlow
            // 
            this._layoutPanel.SetColumnSpan(this._bottomActionFlow, 2);
            this._bottomActionFlow.Controls.Add(this._cmbJsonFiles);
            this._bottomActionFlow.Controls.Add(this._btnSaveJson);
            this._bottomActionFlow.Controls.Add(this._btnImportExcel);
            this._bottomActionFlow.Controls.Add(this._btnClearExcel);
            this._bottomActionFlow.Controls.Add(this._btnExportExcel);
            this._bottomActionFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._bottomActionFlow.Location = new System.Drawing.Point(0, 52);
            this._bottomActionFlow.Margin = new System.Windows.Forms.Padding(0);
            this._bottomActionFlow.Name = "_bottomActionFlow";
            this._bottomActionFlow.Size = new System.Drawing.Size(567, 52);
            this._bottomActionFlow.TabIndex = 3;
            this._bottomActionFlow.WrapContents = false;
            // 
            // _cmbJsonFiles
            // 
            this._cmbJsonFiles.List = true;
            this._cmbJsonFiles.Location = new System.Drawing.Point(0, 0);
            this._cmbJsonFiles.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._cmbJsonFiles.Name = "_cmbJsonFiles";
            this._cmbJsonFiles.PlaceholderText = "选择历史记录";
            this._cmbJsonFiles.Size = new System.Drawing.Size(120, 32);
            this._cmbJsonFiles.TabIndex = 0;
            // 
            // _btnSaveJson
            // 
            this._btnSaveJson.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnSaveJson.IconSvg = "SaveOutlined";
            this._btnSaveJson.Location = new System.Drawing.Point(125, 0);
            this._btnSaveJson.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this._btnSaveJson.Name = "_btnSaveJson";
            this._btnSaveJson.Size = new System.Drawing.Size(58, 32);
            this._btnSaveJson.TabIndex = 1;
            this._btnSaveJson.Text = "保存";
            // 
            // _btnImportExcel
            // 
            this._btnImportExcel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnImportExcel.IconSvg = "FileExcelOutlined";
            this._btnImportExcel.Location = new System.Drawing.Point(193, 0);
            this._btnImportExcel.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnImportExcel.Name = "_btnImportExcel";
            this._btnImportExcel.Size = new System.Drawing.Size(82, 32);
            this._btnImportExcel.TabIndex = 0;
            this._btnImportExcel.Text = "导入表格";
            this._btnImportExcel.Type = AntdUI.TTypeMini.Success;
            // 
            // _btnClearExcel
            // 
            this._btnClearExcel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnClearExcel.IconSvg = "DeleteOutlined";
            this._btnClearExcel.Location = new System.Drawing.Point(280, 0);
            this._btnClearExcel.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnClearExcel.Name = "_btnClearExcel";
            this._btnClearExcel.Size = new System.Drawing.Size(82, 32);
            this._btnClearExcel.TabIndex = 1;
            this._btnClearExcel.Text = "清除表格";
            // 
            // _btnExportExcel
            // 
            this._btnExportExcel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnExportExcel.IconSvg = "ExportOutlined";
            this._btnExportExcel.Location = new System.Drawing.Point(367, 0);
            this._btnExportExcel.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnExportExcel.Name = "_btnExportExcel";
            this._btnExportExcel.Size = new System.Drawing.Size(82, 32);
            this._btnExportExcel.TabIndex = 2;
            this._btnExportExcel.Text = "导出表格";
            // 
            // _renameActionFlow
            // 
            this._renameActionFlow.AutoSize = true;
            this._renameActionFlow.Controls.Add(this._btnToggleMode);
            this._renameActionFlow.Controls.Add(this._btnManualMode);
            this._renameActionFlow.Controls.Add(this._btnBatchMode);
            this._renameActionFlow.Controls.Add(this._btnRename);
            this._renameActionFlow.Dock = System.Windows.Forms.DockStyle.Fill;
            this._renameActionFlow.Location = new System.Drawing.Point(567, 52);
            this._renameActionFlow.Margin = new System.Windows.Forms.Padding(0);
            this._renameActionFlow.Name = "_renameActionFlow";
            this._renameActionFlow.Size = new System.Drawing.Size(348, 52);
            this._renameActionFlow.TabIndex = 4;
            this._renameActionFlow.WrapContents = false;
            // 
            // _btnToggleMode
            // 
            this._btnToggleMode.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnToggleMode.IconSvg = "SwapOutlined";
            this._btnToggleMode.Location = new System.Drawing.Point(0, 0);
            this._btnToggleMode.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnToggleMode.Name = "_btnToggleMode";
            this._btnToggleMode.Size = new System.Drawing.Size(82, 32);
            this._btnToggleMode.TabIndex = 0;
            this._btnToggleMode.Text = "复制模式";
            // 
            // _btnManualMode
            // 
            this._btnManualMode.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnManualMode.IconSvg = "ThunderboltOutlined";
            this._btnManualMode.Location = new System.Drawing.Point(87, 0);
            this._btnManualMode.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnManualMode.Name = "_btnManualMode";
            this._btnManualMode.Size = new System.Drawing.Size(82, 32);
            this._btnManualMode.TabIndex = 1;
            this._btnManualMode.Text = "手动模式";
            this._btnManualMode.Type = AntdUI.TTypeMini.Warn;
            // 
            // _btnBatchMode
            // 
            this._btnBatchMode.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnBatchMode.IconSvg = "AppstoreOutlined";
            this._btnBatchMode.Location = new System.Drawing.Point(174, 0);
            this._btnBatchMode.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnBatchMode.Name = "_btnBatchMode";
            this._btnBatchMode.Size = new System.Drawing.Size(82, 32);
            this._btnBatchMode.TabIndex = 2;
            this._btnBatchMode.Text = "批量模式";
            this._btnBatchMode.Type = AntdUI.TTypeMini.Primary;
            // 
            // _btnRename
            // 
            this._btnRename.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._btnRename.IconSvg = "EditOutlined";
            this._btnRename.Location = new System.Drawing.Point(261, 0);
            this._btnRename.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._btnRename.Name = "_btnRename";
            this._btnRename.Size = new System.Drawing.Size(82, 32);
            this._btnRename.TabIndex = 3;
            this._btnRename.Text = "开始更改";
            this._btnRename.Type = AntdUI.TTypeMini.Primary;
            // 
            // _fileTable
            // 
            this._fileTable.AllowUserToAddRows = false;
            this._fileTable.AllowUserToResizeRows = false;
            this._fileTable.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._fileTable.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._fileTable.ColumnHeadersHeight = 35;
            this._fileTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._fileTable.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.SerialNumber,
            this.OriginalName,
            this.NewName,
            this.RegexResult,
            this.OrderNumber,
            this.Material,
            this.Quantity,
            this.Dimensions,
            this.CompositeColumn,
            this.LayoutRows,
            this.LayoutColumns,
            this.Process,
            this.PageCount,
            this.Time});
            this._fileTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this._fileTable.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._fileTable.HideOuterBorders = true;
            this._fileTable.Location = new System.Drawing.Point(0, 110);
            this._fileTable.Name = "_fileTable";
            this._fileTable.ReadOnly = true;
            this._fileTable.RowHeadersWidth = 30;
            this._fileTable.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this._fileTable.RowTemplate.Height = 36;
            this._fileTable.Size = new System.Drawing.Size(935, 363);
            this._fileTable.StateCommon.Background.Color1 = System.Drawing.Color.White;
            this._fileTable.StateCommon.BackStyle = Krypton.Toolkit.PaletteBackStyle.GridBackgroundList;
            this._fileTable.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._fileTable.StateCommon.DataCell.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this._fileTable.StateCommon.DataCell.Content.TextH = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._fileTable.StateCommon.DataCell.Content.TextV = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._fileTable.StateCommon.HeaderColumn.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this._fileTable.StateCommon.HeaderColumn.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this._fileTable.StateCommon.HeaderColumn.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._fileTable.StateCommon.HeaderColumn.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Bottom;
            this._fileTable.StateCommon.HeaderColumn.Content.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._fileTable.StateCommon.HeaderColumn.Content.MultiLine = Krypton.Toolkit.InheritBool.False;
            this._fileTable.StateCommon.HeaderColumn.Content.TextH = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._fileTable.StateCommon.HeaderRow.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this._fileTable.StateCommon.HeaderRow.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this._fileTable.StateCommon.HeaderRow.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._fileTable.StateCommon.HeaderRow.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Right;
            this._fileTable.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(240)))), ((int)(((byte)(255)))));
            this._fileTable.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(240)))), ((int)(((byte)(255)))));
            this._fileTable.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.Black;
            this._fileTable.TabIndex = 0;
            // 
            // SerialNumber
            // 
            this.SerialNumber.DataPropertyName = "SerialNumber";
            this.SerialNumber.FillWeight = 3F;
            this.SerialNumber.HeaderText = "序号";
            this.SerialNumber.MinimumWidth = 40;
            this.SerialNumber.Name = "SerialNumber";
            this.SerialNumber.ReadOnly = true;
            // 
            // OriginalName
            // 
            this.OriginalName.DataPropertyName = "OriginalName";
            this.OriginalName.FillWeight = 14F;
            this.OriginalName.HeaderText = "原文件名";
            this.OriginalName.MinimumWidth = 70;
            this.OriginalName.Name = "OriginalName";
            this.OriginalName.ReadOnly = true;
            // 
            // NewName
            // 
            this.NewName.DataPropertyName = "NewName";
            this.NewName.FillWeight = 10F;
            this.NewName.HeaderText = "新文件名";
            this.NewName.MinimumWidth = 70;
            this.NewName.Name = "NewName";
            this.NewName.ReadOnly = true;
            // 
            // RegexResult
            // 
            this.RegexResult.DataPropertyName = "RegexResult";
            this.RegexResult.FillWeight = 7F;
            this.RegexResult.HeaderText = "正则结果";
            this.RegexResult.MinimumWidth = 70;
            this.RegexResult.Name = "RegexResult";
            this.RegexResult.ReadOnly = true;
            // 
            // OrderNumber
            // 
            this.OrderNumber.DataPropertyName = "OrderNumber";
            this.OrderNumber.FillWeight = 6F;
            this.OrderNumber.HeaderText = "订单号";
            this.OrderNumber.MinimumWidth = 60;
            this.OrderNumber.Name = "OrderNumber";
            this.OrderNumber.ReadOnly = true;
            // 
            // Material
            // 
            this.Material.DataPropertyName = "Material";
            this.Material.FillWeight = 6F;
            this.Material.HeaderText = "材料";
            this.Material.MinimumWidth = 40;
            this.Material.Name = "Material";
            this.Material.ReadOnly = true;
            // 
            // Quantity
            // 
            this.Quantity.DataPropertyName = "Quantity";
            this.Quantity.FillWeight = 4F;
            this.Quantity.HeaderText = "数量";
            this.Quantity.MinimumWidth = 40;
            this.Quantity.Name = "Quantity";
            this.Quantity.ReadOnly = true;
            // 
            // Dimensions
            // 
            this.Dimensions.DataPropertyName = "Dimensions";
            this.Dimensions.FillWeight = 6F;
            this.Dimensions.HeaderText = "尺寸";
            this.Dimensions.MinimumWidth = 40;
            this.Dimensions.Name = "Dimensions";
            this.Dimensions.ReadOnly = true;
            // 
            // CompositeColumn
            // 
            this.CompositeColumn.DataPropertyName = "CompositeColumn";
            this.CompositeColumn.FillWeight = 6F;
            this.CompositeColumn.HeaderText = "列组合";
            this.CompositeColumn.MinimumWidth = 60;
            this.CompositeColumn.Name = "CompositeColumn";
            this.CompositeColumn.ReadOnly = true;
            // 
            // LayoutRows
            // 
            this.LayoutRows.DataPropertyName = "LayoutRows";
            this.LayoutRows.FillWeight = 4F;
            this.LayoutRows.HeaderText = "行数";
            this.LayoutRows.MinimumWidth = 40;
            this.LayoutRows.Name = "LayoutRows";
            this.LayoutRows.ReadOnly = true;
            // 
            // LayoutColumns
            // 
            this.LayoutColumns.DataPropertyName = "LayoutColumns";
            this.LayoutColumns.FillWeight = 4F;
            this.LayoutColumns.HeaderText = "列数";
            this.LayoutColumns.MinimumWidth = 40;
            this.LayoutColumns.Name = "LayoutColumns";
            this.LayoutColumns.ReadOnly = true;
            // 
            // Process
            // 
            this.Process.DataPropertyName = "Process";
            this.Process.FillWeight = 5F;
            this.Process.HeaderText = "工艺";
            this.Process.MinimumWidth = 40;
            this.Process.Name = "Process";
            this.Process.ReadOnly = true;
            // 
            // PageCount
            // 
            this.PageCount.DataPropertyName = "PageCount";
            this.PageCount.FillWeight = 4F;
            this.PageCount.HeaderText = "页数";
            this.PageCount.MinimumWidth = 40;
            this.PageCount.Name = "PageCount";
            this.PageCount.ReadOnly = true;
            // 
            // Time
            // 
            this.Time.DataPropertyName = "Time";
            this.Time.FillWeight = 5F;
            this.Time.HeaderText = "时间";
            this.Time.MinimumWidth = 40;
            this.Time.Name = "Time";
            this.Time.ReadOnly = true;
            // 
            // _statusPanel
            // 
            this._statusPanel.Controls.Add(this._statusFlowPanel);
            this._statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._statusPanel.Location = new System.Drawing.Point(0, 473);
            this._statusPanel.Name = "_statusPanel";
            this._statusPanel.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this._statusPanel.Size = new System.Drawing.Size(935, 30);
            this._statusPanel.TabIndex = 2;
            // 
            // _statusFlowPanel
            // 
            this._statusFlowPanel.Controls.Add(this._statusLabel);
            this._statusFlowPanel.Controls.Add(this._modeStatusLabel);
            this._statusFlowPanel.Controls.Add(this._eventPreviewLabel);
            this._statusFlowPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._statusFlowPanel.Location = new System.Drawing.Point(10, 5);
            this._statusFlowPanel.Margin = new System.Windows.Forms.Padding(0);
            this._statusFlowPanel.Name = "_statusFlowPanel";
            this._statusFlowPanel.Size = new System.Drawing.Size(915, 20);
            this._statusFlowPanel.TabIndex = 0;
            this._statusFlowPanel.WrapContents = false;
            // 
            // _statusLabel
            // 
            this._statusLabel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._statusLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._statusLabel.Location = new System.Drawing.Point(0, 0);
            this._statusLabel.Margin = new System.Windows.Forms.Padding(0, 0, 20, 0);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(96, 16);
            this._statusLabel.TabIndex = 0;
            this._statusLabel.Text = "状态：未开始监控";
            // 
            // _modeStatusLabel
            // 
            this._modeStatusLabel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._modeStatusLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._modeStatusLabel.Location = new System.Drawing.Point(116, 0);
            this._modeStatusLabel.Margin = new System.Windows.Forms.Padding(0, 0, 20, 0);
            this._modeStatusLabel.Name = "_modeStatusLabel";
            this._modeStatusLabel.Size = new System.Drawing.Size(116, 16);
            this._modeStatusLabel.TabIndex = 1;
            this._modeStatusLabel.Text = "复制 │ 手动 │ 未监控";
            // 
            // _eventPreviewLabel
            // 
            this._eventPreviewLabel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._eventPreviewLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._eventPreviewLabel.Location = new System.Drawing.Point(252, 0);
            this._eventPreviewLabel.Margin = new System.Windows.Forms.Padding(0);
            this._eventPreviewLabel.Name = "_eventPreviewLabel";
            this._eventPreviewLabel.Size = new System.Drawing.Size(164, 16);
            this._eventPreviewLabel.TabIndex = 2;
            this._eventPreviewLabel.Text = "组合：正则结果_订单号_材料...";
            // 
            // FileRenamePanel
            // 
            this.Controls.Add(this._fileTable);
            this.Controls.Add(this._statusPanel);
            this.Controls.Add(this._topControlPanel);
            this.Name = "FileRenamePanel";
            this.Size = new System.Drawing.Size(935, 503);
            this._topControlPanel.ResumeLayout(false);
            this._layoutPanel.ResumeLayout(false);
            this._layoutPanel.PerformLayout();
            this._inputPanel.ResumeLayout(false);
            this._regexPanel.ResumeLayout(false);
            this._topBtnFlow.ResumeLayout(false);
            this._topBtnFlow.PerformLayout();
            this._bottomActionFlow.ResumeLayout(false);
            this._bottomActionFlow.PerformLayout();
            this._renameActionFlow.ResumeLayout(false);
            this._renameActionFlow.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._fileTable)).EndInit();
            this._statusPanel.ResumeLayout(false);
            this._statusFlowPanel.ResumeLayout(false);
            this._statusFlowPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.DataGridViewTextBoxColumn SerialNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn OriginalName;
        private System.Windows.Forms.DataGridViewTextBoxColumn NewName;
        private System.Windows.Forms.DataGridViewTextBoxColumn RegexResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn OrderNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn Material;
        private System.Windows.Forms.DataGridViewTextBoxColumn Quantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn Dimensions;
        private System.Windows.Forms.DataGridViewTextBoxColumn CompositeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn LayoutRows;
        private System.Windows.Forms.DataGridViewTextBoxColumn LayoutColumns;
        private System.Windows.Forms.DataGridViewTextBoxColumn Process;
        private System.Windows.Forms.DataGridViewTextBoxColumn PageCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn Time;
    }
}
