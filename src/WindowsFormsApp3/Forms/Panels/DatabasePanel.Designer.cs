namespace WindowsFormsApp3.Forms.Panels
{
    partial class DatabasePanel
    {
        private System.ComponentModel.IContainer components = null;
        
        // AntdUI Table - 显示数据库数据
        private AntdUI.Table _excelTable;

        private void InitializeComponent()
        {
            this._excelTable = new AntdUI.Table();
            this.SuspendLayout();
            // 
            // _excelTable
            // 
            this._excelTable.AutoSizeColumnsMode = AntdUI.ColumnsMode.Fill;
            this._excelTable.Bordered = true;
            this._excelTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this._excelTable.Gap = 12;
            this._excelTable.Location = new System.Drawing.Point(0, 0);
            this._excelTable.Name = "_excelTable";
            this._excelTable.Size = new System.Drawing.Size(935, 503);
            this._excelTable.TabIndex = 0;
            // 
            // DatabasePanel
            // 
            this.Controls.Add(this._excelTable);
            this.Name = "DatabasePanel";
            this.Size = new System.Drawing.Size(935, 503);
            this.ResumeLayout(false);

        }
    }
}
