using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp3.Forms.Panels
{
    partial class PdfSplitPanel
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this._topControlPanel = new System.Windows.Forms.Panel();
            this._pdfRow = new System.Windows.Forms.FlowLayoutPanel();
            this._lblPdfIcon = new AntdUI.Label();
            this._lblPdf = new AntdUI.Label();
            this._txtPdfPath = new AntdUI.Input();
            this._btnBrowsePdf = new AntdUI.Button();
            this._lblSourcePageCount = new AntdUI.Label();
            this._excelRow = new System.Windows.Forms.FlowLayoutPanel();
            this._lblExcelIcon = new AntdUI.Label();
            this._lblExcel = new AntdUI.Label();
            this._txtExcelPath = new AntdUI.Input();
            this._btnBrowseExcel = new AntdUI.Button();
            this._dgvPreview = new Krypton.Toolkit.KryptonDataGridView();
            this.ColumnIndex = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnFileName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnPageCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnStartPage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColumnEndPage = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._bottomPanel = new System.Windows.Forms.Panel();
            this._btnExecuteSplit = new AntdUI.Button();
            this._btnCancel = new AntdUI.Button();
            this._progressBar = new AntdUI.Progress();
            this._lblStatus = new AntdUI.Label();
            this._topControlPanel.SuspendLayout();
            this._pdfRow.SuspendLayout();
            this._excelRow.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._dgvPreview)).BeginInit();
            this._bottomPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _topControlPanel
            // 
            this._topControlPanel.Controls.Add(this._pdfRow);
            this._topControlPanel.Controls.Add(this._excelRow);
            this._topControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._topControlPanel.Location = new System.Drawing.Point(0, 0);
            this._topControlPanel.Name = "_topControlPanel";
            this._topControlPanel.Padding = new System.Windows.Forms.Padding(15, 10, 15, 5);
            this._topControlPanel.Size = new System.Drawing.Size(946, 80);
            this._topControlPanel.TabIndex = 0;
            // 
            // _pdfRow
            // 
            this._pdfRow.Controls.Add(this._lblPdfIcon);
            this._pdfRow.Controls.Add(this._lblPdf);
            this._pdfRow.Controls.Add(this._txtPdfPath);
            this._pdfRow.Controls.Add(this._btnBrowsePdf);
            this._pdfRow.Controls.Add(this._lblSourcePageCount);
            this._pdfRow.Dock = System.Windows.Forms.DockStyle.Top;
            this._pdfRow.Location = new System.Drawing.Point(15, 42);
            this._pdfRow.Name = "_pdfRow";
            this._pdfRow.Size = new System.Drawing.Size(916, 32);
            this._pdfRow.TabIndex = 0;
            this._pdfRow.WrapContents = false;
            // 
            // _lblPdfIcon
            // 
            this._lblPdfIcon.Location = new System.Drawing.Point(0, 0);
            this._lblPdfIcon.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._lblPdfIcon.Name = "_lblPdfIcon";
            this._lblPdfIcon.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this._lblPdfIcon.Size = new System.Drawing.Size(24, 30);
            this._lblPdfIcon.TabIndex = 0;
            this._lblPdfIcon.Text = "📄";
            // 
            // _lblPdf
            // 
            this._lblPdf.Location = new System.Drawing.Point(29, 0);
            this._lblPdf.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this._lblPdf.Name = "_lblPdf";
            this._lblPdf.Size = new System.Drawing.Size(70, 30);
            this._lblPdf.TabIndex = 1;
            this._lblPdf.Text = "源PDF：";
            // 
            // _txtPdfPath
            // 
            this._txtPdfPath.Location = new System.Drawing.Point(112, 3);
            this._txtPdfPath.Name = "_txtPdfPath";
            this._txtPdfPath.PlaceholderText = "拖拽PDF文件到此处或点击选择...";
            this._txtPdfPath.ReadOnly = true;
            this._txtPdfPath.Size = new System.Drawing.Size(480, 30);
            this._txtPdfPath.TabIndex = 2;
            // 
            // _btnBrowsePdf
            // 
            this._btnBrowsePdf.IconSvg = "FolderOpenOutlined";
            this._btnBrowsePdf.Location = new System.Drawing.Point(600, 0);
            this._btnBrowsePdf.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this._btnBrowsePdf.Name = "_btnBrowsePdf";
            this._btnBrowsePdf.Size = new System.Drawing.Size(90, 30);
            this._btnBrowsePdf.TabIndex = 3;
            this._btnBrowsePdf.Text = "选择文件";
            this._btnBrowsePdf.Click += new System.EventHandler(this.BtnBrowsePdf_Click);
            // 
            // _lblSourcePageCount
            // 
            this._lblSourcePageCount.ForeColor = System.Drawing.Color.Gray;
            this._lblSourcePageCount.Location = new System.Drawing.Point(698, 3);
            this._lblSourcePageCount.Name = "_lblSourcePageCount";
            this._lblSourcePageCount.Padding = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this._lblSourcePageCount.Size = new System.Drawing.Size(230, 30);
            this._lblSourcePageCount.TabIndex = 4;
            this._lblSourcePageCount.Text = "共 0 页";
            // 
            // _excelRow
            // 
            this._excelRow.Controls.Add(this._lblExcelIcon);
            this._excelRow.Controls.Add(this._lblExcel);
            this._excelRow.Controls.Add(this._txtExcelPath);
            this._excelRow.Controls.Add(this._btnBrowseExcel);
            this._excelRow.Dock = System.Windows.Forms.DockStyle.Top;
            this._excelRow.Location = new System.Drawing.Point(15, 10);
            this._excelRow.Name = "_excelRow";
            this._excelRow.Size = new System.Drawing.Size(916, 32);
            this._excelRow.TabIndex = 1;
            this._excelRow.WrapContents = false;
            // 
            // _lblExcelIcon
            // 
            this._lblExcelIcon.Location = new System.Drawing.Point(0, 0);
            this._lblExcelIcon.Margin = new System.Windows.Forms.Padding(0, 0, 5, 0);
            this._lblExcelIcon.Name = "_lblExcelIcon";
            this._lblExcelIcon.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this._lblExcelIcon.Size = new System.Drawing.Size(24, 30);
            this._lblExcelIcon.TabIndex = 0;
            this._lblExcelIcon.Text = "📊";
            // 
            // _lblExcel
            // 
            this._lblExcel.Location = new System.Drawing.Point(29, 0);
            this._lblExcel.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
            this._lblExcel.Name = "_lblExcel";
            this._lblExcel.Size = new System.Drawing.Size(70, 30);
            this._lblExcel.TabIndex = 1;
            this._lblExcel.Text = "Excel：";
            // 
            // _txtExcelPath
            // 
            this._txtExcelPath.Location = new System.Drawing.Point(112, 3);
            this._txtExcelPath.Name = "_txtExcelPath";
            this._txtExcelPath.PlaceholderText = "拖拽Excel文件到此处或点击选择...";
            this._txtExcelPath.ReadOnly = true;
            this._txtExcelPath.Size = new System.Drawing.Size(480, 30);
            this._txtExcelPath.TabIndex = 2;
            this._txtExcelPath.TextChanged += new System.EventHandler(this.TxtExcelPath_TextChanged);
            this._txtExcelPath.DoubleClick += new System.EventHandler(this.TxtExcelPath_DoubleClick);
            // 
            // _btnBrowseExcel
            // 
            this._btnBrowseExcel.IconSvg = "FolderOpenOutlined";
            this._btnBrowseExcel.Location = new System.Drawing.Point(600, 0);
            this._btnBrowseExcel.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this._btnBrowseExcel.Name = "_btnBrowseExcel";
            this._btnBrowseExcel.Size = new System.Drawing.Size(90, 30);
            this._btnBrowseExcel.TabIndex = 3;
            this._btnBrowseExcel.Text = "选择文件";
            this._btnBrowseExcel.Click += new System.EventHandler(this.BtnBrowseExcel_Click);
            // 
            // _dgvPreview
            // 
            this._dgvPreview.AllowUserToAddRows = false;
            this._dgvPreview.AllowUserToOrderColumns = true;
            this._dgvPreview.AllowUserToResizeRows = false;
            this._dgvPreview.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._dgvPreview.ColumnHeadersHeight = 35;
            this._dgvPreview.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._dgvPreview.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnIndex,
            this.ColumnFileName,
            this.ColumnPageCount,
            this.ColumnStartPage,
            this.ColumnEndPage});
            this._dgvPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this._dgvPreview.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._dgvPreview.Location = new System.Drawing.Point(0, 80);
            this._dgvPreview.Name = "_dgvPreview";
            this._dgvPreview.ReadOnly = true;
            this._dgvPreview.RowHeadersVisible = false;
            this._dgvPreview.RowHeadersWidth = 30;
            this._dgvPreview.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this._dgvPreview.RowTemplate.Height = 36;
            this._dgvPreview.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dgvPreview.Size = new System.Drawing.Size(946, 365);
            this._dgvPreview.StateCommon.Background.Color1 = System.Drawing.Color.White;
            this._dgvPreview.StateCommon.BackStyle = Krypton.Toolkit.PaletteBackStyle.GridBackgroundList;
            this._dgvPreview.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._dgvPreview.StateCommon.DataCell.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this._dgvPreview.StateCommon.DataCell.Content.TextH = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._dgvPreview.StateCommon.DataCell.Content.TextV = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._dgvPreview.StateCommon.HeaderColumn.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this._dgvPreview.StateCommon.HeaderColumn.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this._dgvPreview.StateCommon.HeaderColumn.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._dgvPreview.StateCommon.HeaderColumn.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Bottom;
            this._dgvPreview.StateCommon.HeaderColumn.Content.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._dgvPreview.StateCommon.HeaderColumn.Content.MultiLine = Krypton.Toolkit.InheritBool.False;
            this._dgvPreview.StateCommon.HeaderColumn.Content.TextH = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._dgvPreview.StateCommon.HeaderRow.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this._dgvPreview.StateCommon.HeaderRow.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this._dgvPreview.StateCommon.HeaderRow.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._dgvPreview.StateCommon.HeaderRow.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Right;
            this._dgvPreview.StateSelected.DataCell.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(240)))), ((int)(((byte)(255)))));
            this._dgvPreview.StateSelected.DataCell.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(225)))), ((int)(((byte)(240)))), ((int)(((byte)(255)))));
            this._dgvPreview.StateSelected.DataCell.Content.Color1 = System.Drawing.Color.Black;
            this._dgvPreview.TabIndex = 1;
            // 
            // ColumnIndex
            // 
            this.ColumnIndex.HeaderText = "序号";
            this.ColumnIndex.Name = "ColumnIndex";
            this.ColumnIndex.ReadOnly = true;
            this.ColumnIndex.Width = 60;
            // 
            // ColumnFileName
            // 
            this.ColumnFileName.HeaderText = "文件名";
            this.ColumnFileName.Name = "ColumnFileName";
            this.ColumnFileName.ReadOnly = true;
            this.ColumnFileName.Width = 320;
            // 
            // ColumnPageCount
            // 
            this.ColumnPageCount.HeaderText = "页数";
            this.ColumnPageCount.Name = "ColumnPageCount";
            this.ColumnPageCount.ReadOnly = true;
            this.ColumnPageCount.Width = 80;
            // 
            // ColumnStartPage
            // 
            this.ColumnStartPage.HeaderText = "起始页";
            this.ColumnStartPage.Name = "ColumnStartPage";
            this.ColumnStartPage.ReadOnly = true;
            this.ColumnStartPage.Width = 80;
            // 
            // ColumnEndPage
            // 
            this.ColumnEndPage.HeaderText = "结束页";
            this.ColumnEndPage.Name = "ColumnEndPage";
            this.ColumnEndPage.ReadOnly = true;
            this.ColumnEndPage.Width = 80;
            // 
            // _bottomPanel
            // 
            this._bottomPanel.Controls.Add(this._btnExecuteSplit);
            this._bottomPanel.Controls.Add(this._btnCancel);
            this._bottomPanel.Controls.Add(this._progressBar);
            this._bottomPanel.Controls.Add(this._lblStatus);
            this._bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._bottomPanel.Location = new System.Drawing.Point(0, 445);
            this._bottomPanel.Name = "_bottomPanel";
            this._bottomPanel.Padding = new System.Windows.Forms.Padding(15, 5, 15, 10);
            this._bottomPanel.Size = new System.Drawing.Size(946, 60);
            this._bottomPanel.TabIndex = 2;
            // 
            // _btnExecuteSplit
            // 
            this._btnExecuteSplit.IconSvg = "CheckOutlined";
            this._btnExecuteSplit.Location = new System.Drawing.Point(15, 15);
            this._btnExecuteSplit.Name = "_btnExecuteSplit";
            this._btnExecuteSplit.Size = new System.Drawing.Size(100, 32);
            this._btnExecuteSplit.TabIndex = 0;
            this._btnExecuteSplit.Text = "执行拆分";
            this._btnExecuteSplit.Type = AntdUI.TTypeMini.Primary;
            this._btnExecuteSplit.Click += new System.EventHandler(this.BtnExecuteSplit_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.Enabled = false;
            this._btnCancel.Location = new System.Drawing.Point(120, 15);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(80, 32);
            this._btnCancel.TabIndex = 1;
            this._btnCancel.Text = "取消";
            this._btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // _progressBar
            // 
            this._progressBar.Location = new System.Drawing.Point(215, 15);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(400, 30);
            this._progressBar.TabIndex = 2;
            // 
            // _lblStatus
            // 
            this._lblStatus.ForeColor = System.Drawing.Color.Gray;
            this._lblStatus.Location = new System.Drawing.Point(625, 15);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(300, 30);
            this._lblStatus.TabIndex = 3;
            this._lblStatus.Text = "就绪";
            // 
            // PdfSplitPanel
            // 
            this.Controls.Add(this._dgvPreview);
            this.Controls.Add(this._topControlPanel);
            this.Controls.Add(this._bottomPanel);
            this.Name = "PdfSplitPanel";
            this.Size = new System.Drawing.Size(946, 505);
            this._topControlPanel.ResumeLayout(false);
            this._pdfRow.ResumeLayout(false);
            this._excelRow.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this._dgvPreview)).EndInit();
            this._bottomPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel _topControlPanel;
        private System.Windows.Forms.FlowLayoutPanel _pdfRow;
        private System.Windows.Forms.FlowLayoutPanel _excelRow;
        private AntdUI.Label _lblPdfIcon;
        private AntdUI.Label _lblPdf;
        private AntdUI.Input _txtPdfPath;
        private AntdUI.Button _btnBrowsePdf;
        private AntdUI.Label _lblSourcePageCount;
        private AntdUI.Label _lblExcelIcon;
        private AntdUI.Label _lblExcel;
        private AntdUI.Input _txtExcelPath;
        private AntdUI.Button _btnBrowseExcel;
        private Krypton.Toolkit.KryptonDataGridView _dgvPreview;
        private System.Windows.Forms.Panel _bottomPanel;
        private AntdUI.Button _btnExecuteSplit;
        private AntdUI.Button _btnCancel;
        private AntdUI.Progress _progressBar;
        private AntdUI.Label _lblStatus;
        private DataGridViewTextBoxColumn ColumnIndex;
        private DataGridViewTextBoxColumn ColumnFileName;
        private DataGridViewTextBoxColumn ColumnPageCount;
        private DataGridViewTextBoxColumn ColumnStartPage;
        private DataGridViewTextBoxColumn ColumnEndPage;
    }
}
