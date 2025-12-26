namespace WindowsFormsApp3.Forms.Main
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                // 调用Form1中的Timer释放方法
                (this as WindowsFormsApp3.Forms.Main.Form1)?.DisposeTimer();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.菜单ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.设置ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.性能监控ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.打开日志ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.关于ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.退出ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnClearExcel = new System.Windows.Forms.Button();
            this.cmbJsonFiles = new System.Windows.Forms.ComboBox();
            this.btnSaveJson = new System.Windows.Forms.Button();
            this.btnRename = new System.Windows.Forms.Button();
            this.btnImportExcel = new System.Windows.Forms.Button();
            this.cmbRegex = new System.Windows.Forms.ComboBox();
            this.btnExportExcel = new System.Windows.Forms.Button();
            this.lblRegex = new System.Windows.Forms.Label();
            this.btnSelectInputDir = new System.Windows.Forms.Button();
            this.btnMonitor = new System.Windows.Forms.Button();
            this.btnImmediateRename = new System.Windows.Forms.Button();
            this.txtInputDir = new System.Windows.Forms.ComboBox();
            this.btnToggleMode = new System.Windows.Forms.Button();
            this.lblInputDir = new System.Windows.Forms.Label();
            this.btnStopImmediateRename = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.dgvExcelData = new System.Windows.Forms.DataGridView();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.dgvFiles = new System.Windows.Forms.DataGridView();
            this.colSerialNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOriginalName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colNewName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRegexResult = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colOrderNumber = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colMaterial = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colQuantity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDimensions = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColNotes = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ColTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.statusStrip.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvExcelData)).BeginInit();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.BackColor = System.Drawing.SystemColors.Window;
            this.toolStripStatusLabel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.toolStripStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.toolStripStatusLabel.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(104, 34);
            this.toolStripStatusLabel.Text = "状态：未开始监控";
            // 
            // statusStrip
            // 
            this.statusStrip.BackColor = System.Drawing.SystemColors.Window;
            this.statusStrip.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(3, 704);
            this.statusStrip.Margin = new System.Windows.Forms.Padding(3);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1228, 44);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.Stretch = false;
            this.statusStrip.TabIndex = 17;
            // 
            // 菜单ToolStripMenuItem
            // 
            this.菜单ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.设置ToolStripMenuItem,
            this.性能监控ToolStripMenuItem,
            this.打开日志ToolStripMenuItem,
            this.关于ToolStripMenuItem,
            this.退出ToolStripMenuItem});
            this.菜单ToolStripMenuItem.Name = "菜单ToolStripMenuItem";
            this.菜单ToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.菜单ToolStripMenuItem.Text = "菜单";
            // 
            // 设置ToolStripMenuItem
            // 
            this.设置ToolStripMenuItem.Name = "设置ToolStripMenuItem";
            this.设置ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.设置ToolStripMenuItem.Text = "参数设置";
            this.设置ToolStripMenuItem.Click += new System.EventHandler(this.参数设置ToolStripMenuItem_Click);
            // 
            // 性能监控ToolStripMenuItem
            // 
            this.性能监控ToolStripMenuItem.Name = "性能监控ToolStripMenuItem";
            this.性能监控ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.性能监控ToolStripMenuItem.Text = "性能监控工具";
            this.性能监控ToolStripMenuItem.Click += new System.EventHandler(this.性能监控ToolStripMenuItem_Click);
            // 
            // 打开日志ToolStripMenuItem
            // 
            this.打开日志ToolStripMenuItem.Name = "打开日志ToolStripMenuItem";
            this.打开日志ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.打开日志ToolStripMenuItem.Text = "打开日志";
            this.打开日志ToolStripMenuItem.Click += new System.EventHandler(this.打开日志ToolStripMenuItem_Click);
            // 
            // 关于ToolStripMenuItem
            // 
            this.关于ToolStripMenuItem.Name = "关于ToolStripMenuItem";
            this.关于ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.关于ToolStripMenuItem.Text = "关于";
            this.关于ToolStripMenuItem.Click += new System.EventHandler(this.关于ToolStripMenuItem_Click);
            // 
            // 退出ToolStripMenuItem
            // 
            this.退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            this.退出ToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.退出ToolStripMenuItem.Text = "退出";
            this.退出ToolStripMenuItem.Click += new System.EventHandler(this.退出ToolStripMenuItem_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.SystemColors.Window;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.菜单ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(3, 3);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.menuStrip1.Size = new System.Drawing.Size(1228, 25);
            this.menuStrip1.TabIndex = 46;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.btnClearExcel);
            this.groupBox3.Controls.Add(this.cmbJsonFiles);
            this.groupBox3.Controls.Add(this.btnSaveJson);
            this.groupBox3.Controls.Add(this.btnRename);
            this.groupBox3.Controls.Add(this.btnImportExcel);
            this.groupBox3.Controls.Add(this.cmbRegex);
            this.groupBox3.Controls.Add(this.btnExportExcel);
            this.groupBox3.Controls.Add(this.lblRegex);
            this.groupBox3.Controls.Add(this.btnSelectInputDir);
            this.groupBox3.Controls.Add(this.btnMonitor);
            this.groupBox3.Controls.Add(this.btnImmediateRename);
            this.groupBox3.Controls.Add(this.txtInputDir);
            this.groupBox3.Controls.Add(this.btnToggleMode);
            this.groupBox3.Controls.Add(this.lblInputDir);
            this.groupBox3.Controls.Add(this.btnStopImmediateRename);
            this.groupBox3.Location = new System.Drawing.Point(15, 31);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(1206, 85);
            this.groupBox3.TabIndex = 1;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "功能区";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label1.Location = new System.Drawing.Point(303, 55);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 17);
            this.label1.TabIndex = 45;
            this.label1.Text = "重命名信息:";
            // 
            // btnClearExcel
            // 
            this.btnClearExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearExcel.BackColor = System.Drawing.SystemColors.Window;
            this.btnClearExcel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnClearExcel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnClearExcel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearExcel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnClearExcel.Location = new System.Drawing.Point(1018, 17);
            this.btnClearExcel.Margin = new System.Windows.Forms.Padding(0);
            this.btnClearExcel.Name = "btnClearExcel";
            this.btnClearExcel.Size = new System.Drawing.Size(75, 25);
            this.btnClearExcel.TabIndex = 43;
            this.btnClearExcel.Text = "清除Excel";
            this.btnClearExcel.UseVisualStyleBackColor = false;
            this.btnClearExcel.Click += new System.EventHandler(this.BtnClearExcel_Click);
            // 
            // cmbJsonFiles
            // 
            this.cmbJsonFiles.Location = new System.Drawing.Point(383, 51);
            this.cmbJsonFiles.Margin = new System.Windows.Forms.Padding(0);
            this.cmbJsonFiles.Name = "cmbJsonFiles";
            this.cmbJsonFiles.Size = new System.Drawing.Size(147, 25);
            this.cmbJsonFiles.TabIndex = 44;
            this.cmbJsonFiles.SelectedIndexChanged += new System.EventHandler(this.cmbJsonFiles_SelectedIndexChanged);
            // 
            // btnSaveJson
            // 
            this.btnSaveJson.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnSaveJson.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnSaveJson.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveJson.Location = new System.Drawing.Point(542, 51);
            this.btnSaveJson.Margin = new System.Windows.Forms.Padding(0);
            this.btnSaveJson.Name = "btnSaveJson";
            this.btnSaveJson.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnSaveJson.Size = new System.Drawing.Size(48, 25);
            this.btnSaveJson.TabIndex = 44;
            this.btnSaveJson.Text = "保存";
            this.btnSaveJson.UseVisualStyleBackColor = true;
            this.btnSaveJson.Click += new System.EventHandler(this.btnSaveJson_Click);
            // 
            // btnRename
            // 
            this.btnRename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRename.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnRename.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRename.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnRename.Location = new System.Drawing.Point(1109, 51);
            this.btnRename.Margin = new System.Windows.Forms.Padding(0);
            this.btnRename.Name = "btnRename";
            this.btnRename.Size = new System.Drawing.Size(75, 25);
            this.btnRename.TabIndex = 16;
            this.btnRename.Text = "开始更改";
            this.btnRename.UseVisualStyleBackColor = true;
            this.btnRename.Click += new System.EventHandler(this.BtnRename_Click);
            // 
            // btnImportExcel
            // 
            this.btnImportExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImportExcel.BackColor = System.Drawing.SystemColors.Window;
            this.btnImportExcel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnImportExcel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnImportExcel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImportExcel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnImportExcel.Location = new System.Drawing.Point(654, 51);
            this.btnImportExcel.Margin = new System.Windows.Forms.Padding(0);
            this.btnImportExcel.Name = "btnImportExcel";
            this.btnImportExcel.Size = new System.Drawing.Size(75, 25);
            this.btnImportExcel.TabIndex = 42;
            this.btnImportExcel.Text = "导入Excel";
            this.btnImportExcel.UseVisualStyleBackColor = false;
            this.btnImportExcel.Click += new System.EventHandler(this.BtnImportExcel_Click);
            // 
            // cmbRegex
            // 
            this.cmbRegex.IntegralHeight = false;
            this.cmbRegex.Location = new System.Drawing.Point(110, 51);
            this.cmbRegex.Name = "cmbRegex";
            this.cmbRegex.Size = new System.Drawing.Size(176, 25);
            this.cmbRegex.TabIndex = 7;
            // 
            // btnExportExcel
            // 
            this.btnExportExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportExcel.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnExportExcel.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExportExcel.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnExportExcel.Location = new System.Drawing.Point(750, 51);
            this.btnExportExcel.Margin = new System.Windows.Forms.Padding(0);
            this.btnExportExcel.Name = "btnExportExcel";
            this.btnExportExcel.Size = new System.Drawing.Size(75, 25);
            this.btnExportExcel.TabIndex = 43;
            this.btnExportExcel.Text = "导出Excel";
            this.btnExportExcel.UseVisualStyleBackColor = true;
            this.btnExportExcel.Click += new System.EventHandler(this.BtnExportExcel_Click);
            // 
            // lblRegex
            // 
            this.lblRegex.AutoSize = true;
            this.lblRegex.Location = new System.Drawing.Point(20, 55);
            this.lblRegex.Name = "lblRegex";
            this.lblRegex.Size = new System.Drawing.Size(80, 17);
            this.lblRegex.TabIndex = 6;
            this.lblRegex.Text = "正则表达式：";
            // 
            // btnSelectInputDir
            // 
            this.btnSelectInputDir.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnSelectInputDir.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSelectInputDir.Location = new System.Drawing.Point(602, 18);
            this.btnSelectInputDir.Name = "btnSelectInputDir";
            this.btnSelectInputDir.Size = new System.Drawing.Size(36, 25);
            this.btnSelectInputDir.TabIndex = 2;
            this.btnSelectInputDir.Text = "...";
            this.btnSelectInputDir.UseVisualStyleBackColor = true;
            this.btnSelectInputDir.Click += new System.EventHandler(this.BtnSelectInputDir_Click);
            // 
            // btnMonitor
            // 
            this.btnMonitor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMonitor.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnMonitor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnMonitor.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnMonitor.Location = new System.Drawing.Point(750, 18);
            this.btnMonitor.Margin = new System.Windows.Forms.Padding(0);
            this.btnMonitor.Name = "btnMonitor";
            this.btnMonitor.Size = new System.Drawing.Size(75, 25);
            this.btnMonitor.TabIndex = 15;
            this.btnMonitor.Text = "开始监控";
            this.btnMonitor.UseVisualStyleBackColor = true;
            this.btnMonitor.Click += new System.EventHandler(this.BtnMonitor_Click);
            // 
            // btnImmediateRename
            // 
            this.btnImmediateRename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImmediateRename.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnImmediateRename.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnImmediateRename.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnImmediateRename.Location = new System.Drawing.Point(846, 18);
            this.btnImmediateRename.Margin = new System.Windows.Forms.Padding(0);
            this.btnImmediateRename.Name = "btnImmediateRename";
            this.btnImmediateRename.Size = new System.Drawing.Size(102, 25);
            this.btnImmediateRename.TabIndex = 41;
            this.btnImmediateRename.Text = "手动模式";
            this.btnImmediateRename.UseVisualStyleBackColor = true;
            this.btnImmediateRename.Click += new System.EventHandler(this.btnImmediateRename_Click);
            // 
            // txtInputDir
            // 
            this.txtInputDir.IntegralHeight = false;
            this.txtInputDir.ItemHeight = 17;
            this.txtInputDir.Location = new System.Drawing.Point(110, 18);
            this.txtInputDir.Name = "txtInputDir";
            this.txtInputDir.Size = new System.Drawing.Size(482, 25);
            this.txtInputDir.TabIndex = 1;
            this.txtInputDir.TextChanged += new System.EventHandler(this.txtInputDir_TextChanged);
            // 
            // btnToggleMode
            // 
            this.btnToggleMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnToggleMode.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnToggleMode.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnToggleMode.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnToggleMode.Location = new System.Drawing.Point(1018, 51);
            this.btnToggleMode.Margin = new System.Windows.Forms.Padding(0);
            this.btnToggleMode.Name = "btnToggleMode";
            this.btnToggleMode.Size = new System.Drawing.Size(75, 25);
            this.btnToggleMode.TabIndex = 40;
            this.btnToggleMode.Text = "复制模式";
            this.btnToggleMode.UseVisualStyleBackColor = true;
            this.btnToggleMode.Click += new System.EventHandler(this.BtnToggleMode_Click);
            // 
            // lblInputDir
            // 
            this.lblInputDir.AutoSize = true;
            this.lblInputDir.Location = new System.Drawing.Point(20, 22);
            this.lblInputDir.Name = "lblInputDir";
            this.lblInputDir.Size = new System.Drawing.Size(92, 17);
            this.lblInputDir.TabIndex = 0;
            this.lblInputDir.Text = "添加目录路径：";
            // 
            // btnStopImmediateRename
            // 
            this.btnStopImmediateRename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStopImmediateRename.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            this.btnStopImmediateRename.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStopImmediateRename.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnStopImmediateRename.Location = new System.Drawing.Point(846, 51);
            this.btnStopImmediateRename.Margin = new System.Windows.Forms.Padding(0);
            this.btnStopImmediateRename.Name = "btnStopImmediateRename";
            this.btnStopImmediateRename.Size = new System.Drawing.Size(102, 25);
            this.btnStopImmediateRename.TabIndex = 42;
            this.btnStopImmediateRename.Text = "批量模式";
            this.btnStopImmediateRename.UseVisualStyleBackColor = true;
            this.btnStopImmediateRename.Click += new System.EventHandler(this.btnStopImmediateRename_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.dgvExcelData);
            this.tabPage2.Location = new System.Drawing.Point(4, 26);
            this.tabPage2.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(1198, 540);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "数据库";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // dgvExcelData
            // 
            this.dgvExcelData.AllowUserToAddRows = false;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(229)))), ((int)(((byte)(229)))));
            dataGridViewCellStyle1.Font = new System.Drawing.Font("微软雅黑", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.Color.White;
            this.dgvExcelData.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvExcelData.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvExcelData.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvExcelData.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgvExcelData.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.Window;
            this.dgvExcelData.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvExcelData.ColumnHeadersHeight = 24;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvExcelData.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvExcelData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvExcelData.EnableHeadersVisualStyles = false;
            this.dgvExcelData.GridColor = System.Drawing.SystemColors.Control;
            this.dgvExcelData.Location = new System.Drawing.Point(0, 0);
            this.dgvExcelData.Margin = new System.Windows.Forms.Padding(0);
            this.dgvExcelData.Name = "dgvExcelData";
            this.dgvExcelData.ReadOnly = true;
            this.dgvExcelData.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.Window;
            this.dgvExcelData.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvExcelData.RowHeadersWidth = 30;
            this.dgvExcelData.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewCellStyle5.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("微软雅黑", 9F);
            dataGridViewCellStyle5.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            dataGridViewCellStyle5.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle5.SelectionForeColor = System.Drawing.Color.White;
            this.dgvExcelData.RowsDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvExcelData.Size = new System.Drawing.Size(1198, 540);
            this.dgvExcelData.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.White;
            this.tabPage1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.tabPage1.Controls.Add(this.dgvFiles);
            this.tabPage1.Location = new System.Drawing.Point(4, 26);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(0);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(1198, 540);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "重命名信息";
            // 
            // dgvFiles
            // 
            this.dgvFiles.AllowUserToAddRows = false;
            dataGridViewCellStyle6.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(229)))), ((int)(((byte)(229)))), ((int)(((byte)(229)))));
            dataGridViewCellStyle6.Font = new System.Drawing.Font("微软雅黑", 9F);
            dataGridViewCellStyle6.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            dataGridViewCellStyle6.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle6.SelectionForeColor = System.Drawing.Color.White;
            this.dgvFiles.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle6;
            this.dgvFiles.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvFiles.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvFiles.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
            this.dgvFiles.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(115)))), ((int)(((byte)(186)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.Orange;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvFiles.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.colCompositeColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLayoutRows = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colLayoutColumns = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dgvFiles.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colSerialNumber,
            this.colOriginalName,
            this.colNewName,
            this.colRegexResult,
            this.colOrderNumber,
            this.colMaterial,
            this.colQuantity,
            this.colDimensions,
            this.colCompositeColumn,
            this.colLayoutRows,
            this.colLayoutColumns,
            this.ColNotes,
            this.ColTime});
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvFiles.DefaultCellStyle = dataGridViewCellStyle8;
            this.dgvFiles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvFiles.EnableHeadersVisualStyles = false;
            this.dgvFiles.GridColor = System.Drawing.SystemColors.Control;
            this.dgvFiles.Location = new System.Drawing.Point(0, 0);
            this.dgvFiles.Name = "dgvFiles";
            this.dgvFiles.ReadOnly = true;
            this.dgvFiles.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle9.BackColor = System.Drawing.SystemColors.ButtonFace;
            dataGridViewCellStyle9.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvFiles.RowHeadersDefaultCellStyle = dataGridViewCellStyle9;
            this.dgvFiles.RowHeadersWidth = 30;
            this.dgvFiles.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle10.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("微软雅黑", 9F);
            dataGridViewCellStyle10.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.Color.YellowGreen;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            this.dgvFiles.RowsDefaultCellStyle = dataGridViewCellStyle10;
            this.dgvFiles.RowTemplate.Height = 23;
            this.dgvFiles.Size = new System.Drawing.Size(1198, 540);
            this.dgvFiles.TabIndex = 0;
            this.dgvFiles.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvFiles_CellContentClick);
            // 
            // colSerialNumber
            // 
            this.colSerialNumber.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colSerialNumber.FillWeight = 4F;
            this.colSerialNumber.HeaderText = "序号";
            this.colSerialNumber.MinimumWidth = 3;
            this.colSerialNumber.Name = "colSerialNumber";
            this.colSerialNumber.ReadOnly = true;
            this.colSerialNumber.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colOriginalName
            // 
            this.colOriginalName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colOriginalName.DataPropertyName = "OriginalName";
            this.colOriginalName.FillWeight = 20F;
            this.colOriginalName.HeaderText = "原文件名";
            this.colOriginalName.Name = "colOriginalName";
            this.colOriginalName.ReadOnly = true;
            this.colOriginalName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colNewName
            // 
            this.colNewName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colNewName.DataPropertyName = "NewName";
            this.colNewName.FillWeight = 18F;
            this.colNewName.HeaderText = "新文件名";
            this.colNewName.Name = "colNewName";
            this.colNewName.ReadOnly = true;
            this.colNewName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.colNewName.Visible = false;
            // 
            // colRegexResult
            // 
            this.colRegexResult.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colRegexResult.DataPropertyName = "RegexResult";
            this.colRegexResult.FillWeight = 10F;
            this.colRegexResult.HeaderText = "正则结果";
            this.colRegexResult.Name = "colRegexResult";
            this.colRegexResult.ReadOnly = true;
            this.colRegexResult.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colOrderNumber
            // 
            this.colOrderNumber.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colOrderNumber.DataPropertyName = "OrderNumber";
            this.colOrderNumber.FillWeight = 8F;
            this.colOrderNumber.HeaderText = "订单号";
            this.colOrderNumber.Name = "colOrderNumber";
            this.colOrderNumber.ReadOnly = true;
            this.colOrderNumber.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colMaterial
            // 
            this.colMaterial.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colMaterial.DataPropertyName = "Material";
            this.colMaterial.FillWeight = 8F;
            this.colMaterial.HeaderText = "材料";
            this.colMaterial.Name = "colMaterial";
            this.colMaterial.ReadOnly = true;
            this.colMaterial.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colQuantity
            // 
            this.colQuantity.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colQuantity.DataPropertyName = "Quantity";
            this.colQuantity.FillWeight = 8F;
            this.colQuantity.HeaderText = "数量";
            this.colQuantity.Name = "colQuantity";
            this.colQuantity.ReadOnly = true;
            this.colQuantity.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colDimensions
            // 
            this.colDimensions.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colDimensions.DataPropertyName = "Dimensions";
            this.colDimensions.FillWeight = 8F;
            this.colDimensions.HeaderText = "尺寸";
            this.colDimensions.Name = "colDimensions";
            this.colDimensions.ReadOnly = true;
            this.colDimensions.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colCompositeColumn
            // 
            this.colCompositeColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colCompositeColumn.DataPropertyName = "CompositeColumn";
            this.colCompositeColumn.FillWeight = 8F;
            this.colCompositeColumn.HeaderText = "列组合";
            this.colCompositeColumn.Name = "colCompositeColumn";
            this.colCompositeColumn.ReadOnly = true;
            this.colCompositeColumn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colLayoutRows
            // 
            this.colLayoutRows.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLayoutRows.DataPropertyName = "LayoutRows";
            this.colLayoutRows.FillWeight = 6F;
            this.colLayoutRows.HeaderText = "行数";
            this.colLayoutRows.Name = "colLayoutRows";
            this.colLayoutRows.ReadOnly = true;
            this.colLayoutRows.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // colLayoutColumns
            // 
            this.colLayoutColumns.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colLayoutColumns.DataPropertyName = "LayoutColumns";
            this.colLayoutColumns.FillWeight = 6F;
            this.colLayoutColumns.HeaderText = "列数";
            this.colLayoutColumns.Name = "colLayoutColumns";
            this.colLayoutColumns.ReadOnly = true;
            this.colLayoutColumns.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // ColNotes
            // 
            this.ColNotes.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColNotes.DataPropertyName = "Process";
            this.ColNotes.FillWeight = 8F;
            this.ColNotes.HeaderText = "工艺";
            this.ColNotes.Name = "ColNotes";
            this.ColNotes.ReadOnly = true;
            this.ColNotes.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // ColTime
            // 
            this.ColTime.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ColTime.DataPropertyName = "Time";
            this.ColTime.FillWeight = 7F;
            this.ColTime.HeaderText = "时间";
            this.ColTime.Name = "ColTime";
            this.ColTime.ReadOnly = true;
            this.ColTime.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.ColTime.Visible = false;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.tabControl1.Location = new System.Drawing.Point(15, 123);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1206, 570);
            this.tabControl1.TabIndex = 1;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(1234, 729);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ForeColor = System.Drawing.SystemColors.WindowText;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Padding = new System.Windows.Forms.Padding(3);
            this.Text = "大诚工具箱";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvExcelData)).EndInit();
            this.tabPage1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvFiles)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripMenuItem 菜单ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 设置ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 性能监控ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 打开日志ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 关于ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 退出ToolStripMenuItem;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.ComboBox txtInputDir;
        private System.Windows.Forms.ComboBox cmbRegex;
        private System.Windows.Forms.Label lblRegex;
        private System.Windows.Forms.Button btnSelectInputDir;

        private System.Windows.Forms.Label lblInputDir;
        private System.Windows.Forms.Button btnRename;
        private System.Windows.Forms.Button btnMonitor;
        private System.Windows.Forms.Button btnToggleMode;
        private System.Windows.Forms.Button btnImmediateRename;
        private System.Windows.Forms.Button btnStopImmediateRename;
        private System.Windows.Forms.ComboBox cmbJsonFiles;
        private System.Windows.Forms.Button btnSaveJson;
        private System.Windows.Forms.Button btnExportExcel;
        private System.Windows.Forms.Button btnImportExcel;
        private System.Windows.Forms.Button btnClearExcel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dgvExcelData;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.DataGridView dgvFiles;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSerialNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOriginalName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colNewName;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRegexResult;
        private System.Windows.Forms.DataGridViewTextBoxColumn colOrderNumber;
        private System.Windows.Forms.DataGridViewTextBoxColumn colMaterial;
        private System.Windows.Forms.DataGridViewTextBoxColumn colQuantity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDimensions;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCompositeColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLayoutRows;
        private System.Windows.Forms.DataGridViewTextBoxColumn colLayoutColumns;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColNotes;
        private System.Windows.Forms.DataGridViewTextBoxColumn ColTime;
        private System.Windows.Forms.TabControl tabControl1;
    }
}