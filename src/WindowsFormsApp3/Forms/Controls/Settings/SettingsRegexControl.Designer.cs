namespace WindowsFormsApp3.Forms.Controls.Settings
{
    partial class SettingsRegexControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.DataGridView dgvRegex;
        private AntdUI.Label lblName;
        private AntdUI.Input txtName;
        private AntdUI.Label lblPattern;
        private AntdUI.Input txtPattern;
        private AntdUI.Button btnAdd;
        private AntdUI.Button btnDelete;
        
        private System.Windows.Forms.GroupBox grpTest;
        private AntdUI.Label lblTestInput;
        private AntdUI.Input txtTestInput;
        private AntdUI.Button btnTest;
        private AntdUI.Input txtTestResult;

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
            System.Windows.Forms.DataGridViewCellStyle rowStyle = new System.Windows.Forms.DataGridViewCellStyle();

            this.dgvRegex = new System.Windows.Forms.DataGridView();
            this.lblName = new AntdUI.Label();
            this.txtName = new AntdUI.Input();
            this.lblPattern = new AntdUI.Label();
            this.txtPattern = new AntdUI.Input();
            this.btnAdd = new AntdUI.Button();
            this.btnDelete = new AntdUI.Button();
            this.grpTest = new System.Windows.Forms.GroupBox();
            this.lblTestInput = new AntdUI.Label();
            this.txtTestInput = new AntdUI.Input();
            this.btnTest = new AntdUI.Button();
            this.txtTestResult = new AntdUI.Input();

            ((System.ComponentModel.ISupportInitialize)(this.dgvRegex)).BeginInit();
            this.grpTest.SuspendLayout();
            this.SuspendLayout();

            // 
            // dgvRegex
            // 
            this.dgvRegex.AllowUserToAddRows = false;
            this.dgvRegex.BackgroundColor = System.Drawing.Color.White;
            this.dgvRegex.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvRegex.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            
            headerStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            headerStyle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240))))); // Light gray
            headerStyle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            headerStyle.ForeColor = System.Drawing.Color.Black;
            headerStyle.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            headerStyle.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            headerStyle.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRegex.ColumnHeadersDefaultCellStyle = headerStyle;
            this.dgvRegex.EnableHeadersVisualStyles = false;
            
            this.dgvRegex.Location = new System.Drawing.Point(20, 20);
            this.dgvRegex.MultiSelect = false;
            this.dgvRegex.Name = "dgvRegex";
            this.dgvRegex.ReadOnly = true;
            this.dgvRegex.RowHeadersVisible = false;
            this.dgvRegex.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvRegex.Size = new System.Drawing.Size(700, 200);
            this.dgvRegex.TabIndex = 0;
            this.dgvRegex.SelectionChanged += new System.EventHandler(this.DgvRegex_SelectionChanged);

            // 
            // lblName
            // 
            this.lblName.Location = new System.Drawing.Point(20, 240);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(60, 32);
            this.lblName.TabIndex = 1;
            this.lblName.Text = "名称:";
            
            // 
            // txtName
            // 
            this.txtName.Location = new System.Drawing.Point(85, 240);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(150, 32);
            this.txtName.TabIndex = 2;
            
            // 
            // lblPattern
            // 
            this.lblPattern.Location = new System.Drawing.Point(250, 240);
            this.lblPattern.Name = "lblPattern";
            this.lblPattern.Size = new System.Drawing.Size(60, 32);
            this.lblPattern.TabIndex = 3;
            this.lblPattern.Text = "表达式:";

            // 
            // txtPattern
            // 
            this.txtPattern.Location = new System.Drawing.Point(315, 240);
            this.txtPattern.Name = "txtPattern";
            this.txtPattern.Size = new System.Drawing.Size(250, 32);
            this.txtPattern.TabIndex = 4;

            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(580, 240);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(60, 32);
            this.btnAdd.TabIndex = 5;
            this.btnAdd.Text = "添加";
            this.btnAdd.Type = AntdUI.TTypeMini.Primary;
            this.btnAdd.Click += new System.EventHandler(this.BtnAdd_Click);

            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(650, 240);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(60, 32);
            this.btnDelete.TabIndex = 6;
            this.btnDelete.Text = "删除";
            this.btnDelete.Type = AntdUI.TTypeMini.Error;
            this.btnDelete.Click += new System.EventHandler(this.BtnDelete_Click);

            // 
            // grpTest
            // 
            this.grpTest.Controls.Add(this.lblTestInput);
            this.grpTest.Controls.Add(this.txtTestInput);
            this.grpTest.Controls.Add(this.btnTest);
            this.grpTest.Controls.Add(this.txtTestResult);
            this.grpTest.Location = new System.Drawing.Point(20, 290);
            this.grpTest.Name = "grpTest";
            this.grpTest.Size = new System.Drawing.Size(700, 120);
            this.grpTest.TabIndex = 7;
            this.grpTest.TabStop = false;
            this.grpTest.Text = "测试匹配";

            // 
            // lblTestInput
            // 
            this.lblTestInput.Location = new System.Drawing.Point(20, 30);
            this.lblTestInput.Size = new System.Drawing.Size(60, 32);
            this.lblTestInput.Text = "测试文本:";
            
            // 
            // txtTestInput
            // 
            this.txtTestInput.Location = new System.Drawing.Point(85, 30);
            this.txtTestInput.Size = new System.Drawing.Size(480, 32);
            this.txtTestInput.PlaceholderText = "输入要测试的字符串";
            
            // 
            // btnTest
            // 
            this.btnTest.Location = new System.Drawing.Point(580, 30);
            this.btnTest.Size = new System.Drawing.Size(100, 32);
            this.btnTest.Text = "测试";
            this.btnTest.Click += new System.EventHandler(this.BtnTest_Click);

            // 
            // txtTestResult
            // 
            this.txtTestResult.Location = new System.Drawing.Point(85, 75);
            this.txtTestResult.Size = new System.Drawing.Size(595, 32);
            this.txtTestResult.ReadOnly = true;
            this.txtTestResult.PlaceholderText = "匹配结果将显示在这里";

            // 
            // SettingsRegexControl
            // 
            this.Controls.Add(this.dgvRegex);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblPattern);
            this.Controls.Add(this.txtPattern);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.grpTest);
            this.Name = "SettingsRegexControl";
            this.Size = new System.Drawing.Size(750, 450);
            
            ((System.ComponentModel.ISupportInitialize)(this.dgvRegex)).EndInit();
            this.grpTest.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
