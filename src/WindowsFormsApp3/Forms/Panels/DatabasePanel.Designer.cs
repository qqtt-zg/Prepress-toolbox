namespace WindowsFormsApp3.Forms.Panels
{
    partial class DatabasePanel
    {
        private System.ComponentModel.IContainer components = null;
        
        // 顶部控制面板（与 FileRenamePanel 一致的高度）
        private System.Windows.Forms.Panel _topControlPanel;
        
        // Krypton DataGridView - 显示数据库/Excel数据
        private Krypton.Toolkit.KryptonDataGridView _excelTable;
        
        // 底部状态栏（与 FileRenamePanel 一致）
        private System.Windows.Forms.Panel _statusPanel;
        private AntdUI.Label _statusLabel;

        private void InitializeComponent()
        {
            this._topControlPanel = new System.Windows.Forms.Panel();
            this._excelTable = new Krypton.Toolkit.KryptonDataGridView();
            this._statusPanel = new System.Windows.Forms.Panel();
            this._statusLabel = new AntdUI.Label();
            ((System.ComponentModel.ISupportInitialize)(this._excelTable)).BeginInit();
            this._statusPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _topControlPanel
            // 
            this._topControlPanel.BackColor = System.Drawing.Color.White;
            this._topControlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this._topControlPanel.Location = new System.Drawing.Point(0, 0);
            this._topControlPanel.Name = "_topControlPanel";
            this._topControlPanel.Padding = new System.Windows.Forms.Padding(10, 1, 10, 5);
            this._topControlPanel.Size = new System.Drawing.Size(935, 110);
            this._topControlPanel.TabIndex = 1;
            // 
            // _excelTable
            // 
            this._excelTable.AllowUserToAddRows = false;
            this._excelTable.AllowUserToResizeRows = false;
            this._excelTable.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this._excelTable.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._excelTable.ColumnHeadersHeight = 35;
            this._excelTable.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            this._excelTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this._excelTable.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._excelTable.HideOuterBorders = true;
            this._excelTable.Location = new System.Drawing.Point(0, 110);
            this._excelTable.MultiSelect = false;
            this._excelTable.Name = "_excelTable";
            this._excelTable.ReadOnly = true;
            this._excelTable.RowHeadersWidth = 30;
            this._excelTable.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            this._excelTable.RowTemplate.Height = 36;
            this._excelTable.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._excelTable.Size = new System.Drawing.Size(935, 363);
            this._excelTable.StateCommon.Background.Color1 = System.Drawing.Color.White;
            this._excelTable.StateCommon.BackStyle = Krypton.Toolkit.PaletteBackStyle.GridBackgroundList;
            this._excelTable.StateCommon.DataCell.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this._excelTable.StateCommon.DataCell.Border.DrawBorders = ((Krypton.Toolkit.PaletteDrawBorders)((Krypton.Toolkit.PaletteDrawBorders.Bottom | Krypton.Toolkit.PaletteDrawBorders.Right)));
            this._excelTable.StateCommon.DataCell.Content.TextH = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._excelTable.StateCommon.DataCell.Content.TextV = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._excelTable.StateCommon.HeaderColumn.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this._excelTable.StateCommon.HeaderColumn.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this._excelTable.StateCommon.HeaderColumn.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._excelTable.StateCommon.HeaderColumn.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Bottom;
            this._excelTable.StateCommon.HeaderColumn.Content.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this._excelTable.StateCommon.HeaderColumn.Content.MultiLine = Krypton.Toolkit.InheritBool.False;
            this._excelTable.StateCommon.HeaderColumn.Content.TextH = Krypton.Toolkit.PaletteRelativeAlign.Center;
            this._excelTable.StateCommon.HeaderRow.Back.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this._excelTable.StateCommon.HeaderRow.Back.Color2 = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
            this._excelTable.StateCommon.HeaderRow.Border.Color1 = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(230)))), ((int)(((byte)(230)))));
            this._excelTable.StateCommon.HeaderRow.Border.DrawBorders = Krypton.Toolkit.PaletteDrawBorders.Right;
            this._excelTable.TabIndex = 0;
            // 
            // _statusPanel
            // 
            this._statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this._statusPanel.Controls.Add(this._statusLabel);
            this._statusPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._statusPanel.Location = new System.Drawing.Point(0, 473);
            this._statusPanel.Name = "_statusPanel";
            this._statusPanel.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this._statusPanel.Size = new System.Drawing.Size(935, 30);
            this._statusPanel.TabIndex = 2;
            // 
            // _statusLabel
            // 
            this._statusLabel.AutoSizeMode = AntdUI.TAutoSize.Auto;
            this._statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._statusLabel.Font = new System.Drawing.Font("微软雅黑", 9F);
            this._statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(102)))), ((int)(((byte)(102)))));
            this._statusLabel.Location = new System.Drawing.Point(10, 5);
            this._statusLabel.Margin = new System.Windows.Forms.Padding(0);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(915, 20);
            this._statusLabel.TabIndex = 0;
            this._statusLabel.Text = "状态：等待数据...";
            // 
            // DatabasePanel
            // 
            this.Controls.Add(this._excelTable);
            this.Controls.Add(this._statusPanel);
            this.Controls.Add(this._topControlPanel);
            this.Name = "DatabasePanel";
            this.Size = new System.Drawing.Size(935, 503);
            ((System.ComponentModel.ISupportInitialize)(this._excelTable)).EndInit();
            this._statusPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
    }
}
