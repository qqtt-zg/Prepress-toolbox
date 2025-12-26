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
        private AntdUI.Input _txtInputDir;
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
        
        // AntdUI Table - 显示文件列表
        private AntdUI.Table _fileTable;
        
        // JSON 管理控件
        private AntdUI.Select _cmbJsonFiles;
        private AntdUI.Button _btnSaveJson;
        
        // 状态栏控件
        private System.Windows.Forms.Panel _statusPanel;
        private AntdUI.Label _statusLabel;

        private void InitializeComponent()
        {
            this._topControlPanel = new System.Windows.Forms.Panel();
            this._layoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this._inputPanel = new System.Windows.Forms.Panel();
            this._txtInputDir = new AntdUI.Input();
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
            this._fileTable = new AntdUI.Table();
            this._statusPanel = new System.Windows.Forms.Panel();
            this._statusLabel = new AntdUI.Label();
            this._topControlPanel.SuspendLayout();
            this._layoutPanel.SuspendLayout();
            this._inputPanel.SuspendLayout();
            this._regexPanel.SuspendLayout();
            this._topBtnFlow.SuspendLayout();
            this._bottomActionFlow.SuspendLayout();
            this._renameActionFlow.SuspendLayout();
            this._statusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _topControlPanel
            // 
            this._topControlPanel.BackColor = System.Drawing.Color.White;
            this._topControlPanel.Controls.Add(this._layoutPanel);
            this._topControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._topControlPanel.Location = new System.Drawing.Point(0, 0);
            this._topControlPanel.Name = "_topControlPanel";
            this._topControlPanel.Padding = new System.Windows.Forms.Padding(10, 1, 10, 5);
            this._topControlPanel.Size = new System.Drawing.Size(1006, 110);
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
            this._layoutPanel.Size = new System.Drawing.Size(986, 104);
            this._layoutPanel.TabIndex = 0;
            // 
            // _inputPanel
            // 
            this._inputPanel.Controls.Add(this._txtInputDir);
            this._inputPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._inputPanel.Location = new System.Drawing.Point(0, 0);
            this._inputPanel.Margin = new System.Windows.Forms.Padding(0);
            this._inputPanel.Name = "_inputPanel";
            this._inputPanel.Padding = new System.Windows.Forms.Padding(0, 5, 5, 5);
            this._inputPanel.Size = new System.Drawing.Size(478, 52);
            this._inputPanel.TabIndex = 0;
            // 
            // _txtInputDir
            // 
            this._txtInputDir.Dock = System.Windows.Forms.DockStyle.Fill;
            this._txtInputDir.Location = new System.Drawing.Point(0, 5);
            this._txtInputDir.Name = "_txtInputDir";
            this._txtInputDir.PlaceholderText = "请输入或选择文件夹路径...";
            this._txtInputDir.ReadOnly = true;
            this._txtInputDir.Size = new System.Drawing.Size(473, 42);
            this._txtInputDir.TabIndex = 0;
            // 
            // _regexPanel
            // 
            this._regexPanel.Controls.Add(this._cmbRegex);
            this._regexPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._regexPanel.Location = new System.Drawing.Point(478, 0);
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
            this._topBtnFlow.Location = new System.Drawing.Point(638, 0);
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
            this._bottomActionFlow.Size = new System.Drawing.Size(638, 52);
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
            this._renameActionFlow.Location = new System.Drawing.Point(638, 52);
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
            this._fileTable.AutoSizeColumnsMode = AntdUI.ColumnsMode.Fill;
            this._fileTable.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(217)))), ((int)(((byte)(217)))));
            this._fileTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this._fileTable.EmptyHeader = true;
            this._fileTable.EmptyText = "暂无数据，请选择文件夹或导入表格";
            this._fileTable.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this._fileTable.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this._fileTable.Gap = 8;
            this._fileTable.Gaps = new System.Drawing.Size(8, 8);
            this._fileTable.Location = new System.Drawing.Point(0, 110);
            this._fileTable.Name = "_fileTable";
            this._fileTable.RowHeight = 40;
            this._fileTable.RowHeightHeader = 40;
            this._fileTable.Size = new System.Drawing.Size(1006, 362);
            this._fileTable.TabIndex = 0;
            // 
            // _statusPanel
            // 
            this._statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this._statusPanel.Controls.Add(this._statusLabel);
            this._statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._statusPanel.Location = new System.Drawing.Point(0, 472);
            this._statusPanel.Name = "_statusPanel";
            this._statusPanel.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this._statusPanel.Size = new System.Drawing.Size(1006, 30);
            this._statusPanel.TabIndex = 2;
            // 
            // _statusLabel
            // 
            this._statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._statusLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this._statusLabel.Location = new System.Drawing.Point(10, 5);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(986, 20);
            this._statusLabel.TabIndex = 0;
            this._statusLabel.Text = "状态：未开始监控";
            // 
            // FileRenamePanel
            // 
            this.Controls.Add(this._fileTable);
            this.Controls.Add(this._statusPanel);
            this.Controls.Add(this._topControlPanel);
            this.Name = "FileRenamePanel";
            this.Size = new System.Drawing.Size(1006, 502);
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
            this._statusPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
