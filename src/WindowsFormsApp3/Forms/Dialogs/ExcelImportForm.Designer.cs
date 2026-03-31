namespace WindowsFormsApp3 {
    partial class ExcelImportForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.label1 = new AntdUI.Label();
            this.btnOK = new AntdUI.Button();
            this.btnCancel = new AntdUI.Button();
            this.label2 = new AntdUI.Label();
            this.cmbReturnColumn = new AntdUI.Select();
            this.label3 = new AntdUI.Label();
            this.cmbSearchColumn = new AntdUI.Select();
            this.cmbNewColumn = new AntdUI.Select();
            this.label4 = new AntdUI.Label();
            this.chkImportSerialColumn = new AntdUI.Checkbox();
            this.dgvPreview = new AntdUI.Table();
            this.txtReturnColumnParams = new AntdUI.Input();
            this.txtSearchColumnParams = new AntdUI.Input();
            this.txtNewColumnParams = new AntdUI.Input();
            this.label5 = new AntdUI.Label();
            this.cmbRegex2 = new AntdUI.Select();
            this.clbCompositeColumns = new AntdUI.Table();
            this.txtCompositeSeparator = new AntdUI.Input();
            this.btnSelectAllColumns = new AntdUI.Button();
            this.btnClearAllColumns = new AntdUI.Button();
            this.labelCompositeColumns = new AntdUI.Label();
            this.labelSeparator = new AntdUI.Label();
            this.chkEnableComposite = new AntdUI.Checkbox();
            this.chkEnableSerialSearchResultToRegex = new AntdUI.Checkbox();
            this.cmbSerialSearchResultColumn = new AntdUI.Select();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 23);
            this.label1.TabIndex = 2;
            this.label1.Text = "已导入Excel数据：";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(490, 527);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 32);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "确定";
            this.btnOK.Type = AntdUI.TTypeMini.Primary;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Ghost = true;
            this.btnCancel.Location = new System.Drawing.Point(490, 565);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 32);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "取消";
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(20, 468);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 23);
            this.label2.TabIndex = 0;
            this.label2.Text = "数量列选择：";
            // 
            // cmbReturnColumn
            // 
            this.cmbReturnColumn.Location = new System.Drawing.Point(127, 465);
            this.cmbReturnColumn.Name = "cmbReturnColumn";
            this.cmbReturnColumn.Size = new System.Drawing.Size(175, 32);
            this.cmbReturnColumn.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(20, 431);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 23);
            this.label3.TabIndex = 2;
            this.label3.Text = "搜索列选择：";
            this.label3.Click += new System.EventHandler(this.label3_Click);
            // 
            // cmbSearchColumn
            // 
            this.cmbSearchColumn.Location = new System.Drawing.Point(127, 428);
            this.cmbSearchColumn.Name = "cmbSearchColumn";
            this.cmbSearchColumn.Size = new System.Drawing.Size(175, 32);
            this.cmbSearchColumn.TabIndex = 3;
            // 
            // cmbNewColumn
            // 
            this.cmbNewColumn.Location = new System.Drawing.Point(127, 387);
            this.cmbNewColumn.Name = "cmbNewColumn";
            this.cmbNewColumn.Size = new System.Drawing.Size(175, 32);
            this.cmbNewColumn.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(20, 390);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 23);
            this.label4.TabIndex = 4;
            this.label4.Text = "序号列选择：";
            // 
            // chkImportSerialColumn
            // 
            this.chkImportSerialColumn.Checked = true;
            this.chkImportSerialColumn.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkImportSerialColumn.Location = new System.Drawing.Point(253, 358);
            this.chkImportSerialColumn.Name = "chkImportSerialColumn";
            this.chkImportSerialColumn.Size = new System.Drawing.Size(150, 23);
            this.chkImportSerialColumn.TabIndex = 6;
            this.chkImportSerialColumn.Text = "导入序号列数据";
            // 
            // dgvPreview
            // 
            this.dgvPreview.Gap = 12;
            this.dgvPreview.Location = new System.Drawing.Point(12, 41);
            this.dgvPreview.Name = "dgvPreview";
            this.dgvPreview.Size = new System.Drawing.Size(669, 300);
            this.dgvPreview.TabIndex = 7;
            // 
            // txtReturnColumnParams
            // 
            this.txtReturnColumnParams.Location = new System.Drawing.Point(308, 465);
            this.txtReturnColumnParams.Name = "txtReturnColumnParams";
            this.txtReturnColumnParams.Size = new System.Drawing.Size(150, 32);
            this.txtReturnColumnParams.TabIndex = 9;
            // 
            // txtSearchColumnParams
            // 
            this.txtSearchColumnParams.Location = new System.Drawing.Point(308, 428);
            this.txtSearchColumnParams.Name = "txtSearchColumnParams";
            this.txtSearchColumnParams.Size = new System.Drawing.Size(150, 32);
            this.txtSearchColumnParams.TabIndex = 8;
            // 
            // txtNewColumnParams
            // 
            this.txtNewColumnParams.Location = new System.Drawing.Point(308, 387);
            this.txtNewColumnParams.Name = "txtNewColumnParams";
            this.txtNewColumnParams.Size = new System.Drawing.Size(150, 32);
            this.txtNewColumnParams.TabIndex = 7;
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(20, 510);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(100, 23);
            this.label5.TabIndex = 10;
            this.label5.Text = "正则表达式：";
            // 
            // cmbRegex2
            // 
            this.cmbRegex2.Location = new System.Drawing.Point(127, 507);
            this.cmbRegex2.Name = "cmbRegex2";
            this.cmbRegex2.Size = new System.Drawing.Size(175, 32);
            this.cmbRegex2.TabIndex = 11;
            // 
            // clbCompositeColumns
            // 
            this.clbCompositeColumns.Enabled = false;
            this.clbCompositeColumns.Gap = 12;
            this.clbCompositeColumns.Location = new System.Drawing.Point(480, 387);
            this.clbCompositeColumns.Name = "clbCompositeColumns";
            this.clbCompositeColumns.Size = new System.Drawing.Size(168, 132);
            this.clbCompositeColumns.TabIndex = 10;
            // 
            // txtCompositeSeparator
            // 
            this.txtCompositeSeparator.Enabled = false;
            this.txtCompositeSeparator.Location = new System.Drawing.Point(367, 507);
            this.txtCompositeSeparator.Name = "txtCompositeSeparator";
            this.txtCompositeSeparator.Size = new System.Drawing.Size(91, 32);
            this.txtCompositeSeparator.TabIndex = 11;
            this.txtCompositeSeparator.Text = ",";
            // 
            // btnSelectAllColumns
            // 
            this.btnSelectAllColumns.Enabled = false;
            this.btnSelectAllColumns.Ghost = true;
            this.btnSelectAllColumns.Location = new System.Drawing.Point(571, 527);
            this.btnSelectAllColumns.Name = "btnSelectAllColumns";
            this.btnSelectAllColumns.Size = new System.Drawing.Size(77, 32);
            this.btnSelectAllColumns.TabIndex = 12;
            this.btnSelectAllColumns.Text = "全选";
            this.btnSelectAllColumns.Click += new System.EventHandler(this.btnSelectAllColumns_Click);
            // 
            // btnClearAllColumns
            // 
            this.btnClearAllColumns.Enabled = false;
            this.btnClearAllColumns.Ghost = true;
            this.btnClearAllColumns.Location = new System.Drawing.Point(571, 565);
            this.btnClearAllColumns.Name = "btnClearAllColumns";
            this.btnClearAllColumns.Size = new System.Drawing.Size(77, 32);
            this.btnClearAllColumns.TabIndex = 13;
            this.btnClearAllColumns.Text = "取消全选";
            this.btnClearAllColumns.Click += new System.EventHandler(this.btnClearAllColumns_Click);
            // 
            // labelCompositeColumns
            // 
            this.labelCompositeColumns.Enabled = false;
            this.labelCompositeColumns.Location = new System.Drawing.Point(600, 360);
            this.labelCompositeColumns.Name = "labelCompositeColumns";
            this.labelCompositeColumns.Size = new System.Drawing.Size(100, 23);
            this.labelCompositeColumns.TabIndex = 14;
            this.labelCompositeColumns.Text = "选择组合列";
            // 
            // labelSeparator
            // 
            this.labelSeparator.Enabled = false;
            this.labelSeparator.Location = new System.Drawing.Point(308, 510);
            this.labelSeparator.Name = "labelSeparator";
            this.labelSeparator.Size = new System.Drawing.Size(53, 23);
            this.labelSeparator.TabIndex = 15;
            this.labelSeparator.Text = "分隔符：";
            // 
            // chkEnableComposite
            // 
            this.chkEnableComposite.Location = new System.Drawing.Point(480, 359);
            this.chkEnableComposite.Name = "chkEnableComposite";
            this.chkEnableComposite.Size = new System.Drawing.Size(120, 23);
            this.chkEnableComposite.TabIndex = 9;
            this.chkEnableComposite.Text = "启用列组合";
            // 
            // chkEnableSerialSearchResultToRegex
            // 
            this.chkEnableSerialSearchResultToRegex.Location = new System.Drawing.Point(308, 556);
            this.chkEnableSerialSearchResultToRegex.Name = "chkEnableSerialSearchResultToRegex";
            this.chkEnableSerialSearchResultToRegex.Size = new System.Drawing.Size(160, 23);
            this.chkEnableSerialSearchResultToRegex.TabIndex = 16;
            this.chkEnableSerialSearchResultToRegex.Text = "序号搜索结果更新正则";
            // 
            // cmbSerialSearchResultColumn
            // 
            this.cmbSerialSearchResultColumn.Location = new System.Drawing.Point(127, 551);
            this.cmbSerialSearchResultColumn.Name = "cmbSerialSearchResultColumn";
            this.cmbSerialSearchResultColumn.Size = new System.Drawing.Size(175, 32);
            this.cmbSerialSearchResultColumn.TabIndex = 17;
            // 
            // ExcelImportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(693, 620);
            this.Controls.Add(this.dgvPreview);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.cmbSearchColumn);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cmbReturnColumn);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cmbNewColumn);
            this.Controls.Add(this.chkImportSerialColumn);
            this.Controls.Add(this.txtNewColumnParams);
            this.Controls.Add(this.txtSearchColumnParams);
            this.Controls.Add(this.txtReturnColumnParams);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cmbRegex2);
            this.Controls.Add(this.chkEnableComposite);
            this.Controls.Add(this.chkEnableSerialSearchResultToRegex);
            this.Controls.Add(this.cmbSerialSearchResultColumn);
            this.Controls.Add(this.clbCompositeColumns);
            this.Controls.Add(this.txtCompositeSeparator);
            this.Controls.Add(this.btnSelectAllColumns);
            this.Controls.Add(this.btnClearAllColumns);
            this.Controls.Add(this.labelCompositeColumns);
            this.Controls.Add(this.labelSeparator);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExcelImportForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Excel导入设置";
            this.Load += new System.EventHandler(this.ExcelImportForm_Load);
            this.ResumeLayout(false);

        }

        private AntdUI.Label label1;
        private AntdUI.Button btnOK;
        private AntdUI.Button btnCancel;
        private AntdUI.Label label2;
        private AntdUI.Select cmbReturnColumn;
        private AntdUI.Label label3;
        private AntdUI.Select cmbSearchColumn;
        private AntdUI.Label label4;
        private AntdUI.Select cmbNewColumn;
        private AntdUI.Checkbox chkImportSerialColumn;
        private AntdUI.Table dgvPreview;
        private AntdUI.Input txtNewColumnParams;
        private AntdUI.Input txtSearchColumnParams;
        private AntdUI.Input txtReturnColumnParams;
        private AntdUI.Label label5;
        private AntdUI.Select cmbRegex2;
        private AntdUI.Checkbox chkEnableComposite;
        private AntdUI.Checkbox chkEnableSerialSearchResultToRegex;
        private AntdUI.Select cmbSerialSearchResultColumn;
        private AntdUI.Table clbCompositeColumns;
        private AntdUI.Input txtCompositeSeparator;
        private AntdUI.Button btnSelectAllColumns;
        private AntdUI.Button btnClearAllColumns;
        private AntdUI.Label labelCompositeColumns;
        private AntdUI.Label labelSeparator;
    }
}