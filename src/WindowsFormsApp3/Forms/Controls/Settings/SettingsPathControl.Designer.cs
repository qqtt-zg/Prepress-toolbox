namespace WindowsFormsApp3.Forms.Controls.Settings
{
    partial class SettingsPathControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridView dgvExportPaths;
        private AntdUI.Button btnAdd;
        private AntdUI.Button btnDelete;
        private AntdUI.Button btnMoveUp;
        private AntdUI.Button btnMoveDown;
        private AntdUI.Label lblStatus;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle headerStyle = new System.Windows.Forms.DataGridViewCellStyle();

            this.dgvExportPaths = new System.Windows.Forms.DataGridView();
            this.btnAdd = new AntdUI.Button();
            this.btnDelete = new AntdUI.Button();
            this.btnMoveUp = new AntdUI.Button();
            this.btnMoveDown = new AntdUI.Button();
            this.lblStatus = new AntdUI.Label(); // Assuming AntdUI.Label exists, typically "Label"

            ((System.ComponentModel.ISupportInitialize)(this.dgvExportPaths)).BeginInit();
            this.SuspendLayout();

            // 
            // dgvExportPaths
            // 
            this.dgvExportPaths.AllowUserToAddRows = false;
            this.dgvExportPaths.BackgroundColor = System.Drawing.Color.White;
            this.dgvExportPaths.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvExportPaths.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            headerStyle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            headerStyle.ForeColor = System.Drawing.Color.Black;
            headerStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            headerStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvExportPaths.ColumnHeadersDefaultCellStyle = headerStyle;
            this.dgvExportPaths.EnableHeadersVisualStyles = false;
            
            this.dgvExportPaths.Location = new System.Drawing.Point(20, 20);
            this.dgvExportPaths.MultiSelect = true;
            this.dgvExportPaths.Name = "dgvExportPaths";
            this.dgvExportPaths.RowHeadersVisible = false;
            this.dgvExportPaths.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvExportPaths.Size = new System.Drawing.Size(700, 300);
            this.dgvExportPaths.TabIndex = 0;
            this.dgvExportPaths.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.DgvExportPaths_CellValueChanged);
            this.dgvExportPaths.CurrentCellDirtyStateChanged += new System.EventHandler(this.DgvExportPaths_CurrentCellDirtyStateChanged);

            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(20, 340);
            this.btnAdd.Size = new System.Drawing.Size(100, 32);
            this.btnAdd.Text = "添加路径";
            this.btnAdd.Type = AntdUI.TTypeMini.Primary;
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);

            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(130, 340);
            this.btnDelete.Size = new System.Drawing.Size(100, 32);
            this.btnDelete.Text = "删除选中";
            this.btnDelete.Type = AntdUI.TTypeMini.Error;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Location = new System.Drawing.Point(510, 340);
            this.btnMoveUp.Size = new System.Drawing.Size(100, 32);
            this.btnMoveUp.Text = "上移";
            this.btnMoveUp.Click += new System.EventHandler(this.BtnMoveUp_Click);

            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Location = new System.Drawing.Point(620, 340);
            this.btnMoveDown.Size = new System.Drawing.Size(100, 32);
            this.btnMoveDown.Text = "下移";
            this.btnMoveDown.Click += new System.EventHandler(this.BtnMoveDown_Click);

            // 
            // lblStatus
            // 
            this.lblStatus.Location = new System.Drawing.Point(20, 390);
            this.lblStatus.Size = new System.Drawing.Size(700, 32);
            this.lblStatus.Text = "路径状态...";
            
            // 
            // SettingsPathControl
            // 
            this.Controls.Add(this.dgvExportPaths);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnMoveUp);
            this.Controls.Add(this.btnMoveDown);
            this.Controls.Add(this.lblStatus);
            this.Name = "SettingsPathControl";
            this.Size = new System.Drawing.Size(750, 450);
            
            ((System.ComponentModel.ISupportInitialize)(this.dgvExportPaths)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
