namespace WindowsFormsApp3.Forms.Utils
{
    partial class RegMarksDialog
    {
        private System.ComponentModel.IContainer components = null;
        
        private AntdUI.InputNumber numOuterDiameter;
        private AntdUI.InputNumber numInnerDiameter;
        private AntdUI.InputNumber numCrossLength;
        private AntdUI.InputNumber numOffset;
        private AntdUI.Radio radioAll;
        private AntdUI.Radio radioCurrent;
        private AntdUI.Radio radioCustom;
        private AntdUI.Input txtPageRange;
        private AntdUI.Button btnOK;
        private AntdUI.Button btnCancel;
        private System.Windows.Forms.Label lblTitle;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
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
            this.SuspendLayout();
            
            // 窗体基本设置
            this.Text = "套准标记设置";
            this.Size = new System.Drawing.Size(450, 540);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = System.Drawing.Color.White;
            
            // 安全地设置字体
            try
            {
                this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            }
            catch
            {
                // 设计时如果字体不可用，使用默认字体
                this.Font = System.Drawing.SystemFonts.DefaultFont;
            }

            int leftMargin = 30;
            int topMargin = 20;
            int labelWidth = 100;
            int controlWidth = 280;
            int rowHeight = 50;
            int currentY = topMargin;

            // 标题
            System.Drawing.Font titleFont;
            try
            {
                titleFont = new System.Drawing.Font("Microsoft YaHei UI", 12F, System.Drawing.FontStyle.Bold);
            }
            catch
            {
                titleFont = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 12F, System.Drawing.FontStyle.Bold);
            }
            
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblTitle.Text = "套准标记参数设置";
            this.lblTitle.Font = titleFont;
            this.lblTitle.Location = new System.Drawing.Point(leftMargin, currentY);
            this.lblTitle.Size = new System.Drawing.Size(380, 30);
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.Controls.Add(this.lblTitle);
            currentY += 40;

            // 外圆直径
            var lblOuterDiameter = new System.Windows.Forms.Label();
            lblOuterDiameter.Text = "外圆直径:";
            lblOuterDiameter.Location = new System.Drawing.Point(leftMargin, currentY + 8);
            lblOuterDiameter.Size = new System.Drawing.Size(labelWidth, 24);
            lblOuterDiameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblOuterDiameter.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.Controls.Add(lblOuterDiameter);

            this.numOuterDiameter = new AntdUI.InputNumber();
            this.numOuterDiameter.Location = new System.Drawing.Point(leftMargin + labelWidth, currentY);
            this.numOuterDiameter.Size = new System.Drawing.Size(150, 36);
            this.numOuterDiameter.Value = 6M;
            this.numOuterDiameter.Minimum = 3M;
            this.numOuterDiameter.Maximum = 15M;
            this.numOuterDiameter.DecimalPlaces = 1;
            this.Controls.Add(this.numOuterDiameter);

            var lblOuterUnit = new System.Windows.Forms.Label();
            lblOuterUnit.Text = "mm";
            lblOuterUnit.Location = new System.Drawing.Point(leftMargin + labelWidth + 160, currentY + 8);
            lblOuterUnit.Size = new System.Drawing.Size(40, 24);
            lblOuterUnit.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            this.Controls.Add(lblOuterUnit);
            currentY += rowHeight;

            // 内圆直径
            var lblInnerDiameter = new System.Windows.Forms.Label();
            lblInnerDiameter.Text = "内圆直径:";
            lblInnerDiameter.Location = new System.Drawing.Point(leftMargin, currentY + 8);
            lblInnerDiameter.Size = new System.Drawing.Size(labelWidth, 24);
            lblInnerDiameter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblInnerDiameter.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.Controls.Add(lblInnerDiameter);

            this.numInnerDiameter = new AntdUI.InputNumber();
            this.numInnerDiameter.Location = new System.Drawing.Point(leftMargin + labelWidth, currentY);
            this.numInnerDiameter.Size = new System.Drawing.Size(150, 36);
            this.numInnerDiameter.Value = 4M;
            this.numInnerDiameter.Minimum = 2M;
            this.numInnerDiameter.Maximum = 10M;
            this.numInnerDiameter.DecimalPlaces = 1;
            this.Controls.Add(this.numInnerDiameter);

            var lblInnerUnit = new System.Windows.Forms.Label();
            lblInnerUnit.Text = "mm";
            lblInnerUnit.Location = new System.Drawing.Point(leftMargin + labelWidth + 160, currentY + 8);
            lblInnerUnit.Size = new System.Drawing.Size(40, 24);
            lblInnerUnit.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            this.Controls.Add(lblInnerUnit);
            currentY += rowHeight;

            // 十字线长度
            var lblCrossLength = new System.Windows.Forms.Label();
            lblCrossLength.Text = "十字线长度:";
            lblCrossLength.Location = new System.Drawing.Point(leftMargin, currentY + 8);
            lblCrossLength.Size = new System.Drawing.Size(labelWidth, 24);
            lblCrossLength.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblCrossLength.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.Controls.Add(lblCrossLength);

            this.numCrossLength = new AntdUI.InputNumber();
            this.numCrossLength.Location = new System.Drawing.Point(leftMargin + labelWidth, currentY);
            this.numCrossLength.Size = new System.Drawing.Size(150, 36);
            this.numCrossLength.Value = 8M;
            this.numCrossLength.Minimum = 4M;
            this.numCrossLength.Maximum = 20M;
            this.numCrossLength.DecimalPlaces = 1;
            this.Controls.Add(this.numCrossLength);

            var lblCrossUnit = new System.Windows.Forms.Label();
            lblCrossUnit.Text = "mm";
            lblCrossUnit.Location = new System.Drawing.Point(leftMargin + labelWidth + 160, currentY + 8);
            lblCrossUnit.Size = new System.Drawing.Size(40, 24);
            lblCrossUnit.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            this.Controls.Add(lblCrossUnit);
            currentY += rowHeight;

            // 偏移距离
            var lblOffset = new System.Windows.Forms.Label();
            lblOffset.Text = "偏移距离:";
            lblOffset.Location = new System.Drawing.Point(leftMargin, currentY + 8);
            lblOffset.Size = new System.Drawing.Size(labelWidth, 24);
            lblOffset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lblOffset.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.Controls.Add(lblOffset);

            this.numOffset = new AntdUI.InputNumber();
            this.numOffset.Location = new System.Drawing.Point(leftMargin + labelWidth, currentY);
            this.numOffset.Size = new System.Drawing.Size(150, 36);
            this.numOffset.Value = 10M;
            this.numOffset.Minimum = 5M;
            this.numOffset.Maximum = 30M;
            this.numOffset.DecimalPlaces = 1;
            this.Controls.Add(this.numOffset);

            var lblOffsetUnit = new System.Windows.Forms.Label();
            lblOffsetUnit.Text = "mm";
            lblOffsetUnit.Location = new System.Drawing.Point(leftMargin + labelWidth + 160, currentY + 8);
            lblOffsetUnit.Size = new System.Drawing.Size(40, 24);
            lblOffsetUnit.ForeColor = System.Drawing.Color.FromArgb(120, 120, 120);
            this.Controls.Add(lblOffsetUnit);
            currentY += rowHeight + 10;

            // 分隔线
            var separator = new System.Windows.Forms.Panel();
            separator.Location = new System.Drawing.Point(leftMargin, currentY);
            separator.Size = new System.Drawing.Size(controlWidth + 90, 1);
            separator.BackColor = System.Drawing.Color.FromArgb(230, 230, 230);
            this.Controls.Add(separator);
            currentY += 15;

            // 应用范围标题
            System.Drawing.Font rangeTitleFont;
            try
            {
                rangeTitleFont = new System.Drawing.Font("Microsoft YaHei UI", 9.5F, System.Drawing.FontStyle.Bold);
            }
            catch
            {
                rangeTitleFont = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont.FontFamily, 9.5F, System.Drawing.FontStyle.Bold);
            }
            
            var lblRangeTitle = new System.Windows.Forms.Label();
            lblRangeTitle.Text = "应用页面范围:";
            lblRangeTitle.Location = new System.Drawing.Point(leftMargin, currentY);
            lblRangeTitle.Size = new System.Drawing.Size(200, 24);
            lblRangeTitle.Font = rangeTitleFont;
            lblRangeTitle.ForeColor = System.Drawing.Color.FromArgb(60, 60, 60);
            this.Controls.Add(lblRangeTitle);
            currentY += 30;

            // 全部页面
            this.radioAll = new AntdUI.Radio();
            this.radioAll.Text = "全部页面";
            this.radioAll.Location = new System.Drawing.Point(leftMargin, currentY);
            this.radioAll.Size = new System.Drawing.Size(120, 30);
            this.radioAll.Checked = true;
            this.radioAll.CheckedChanged += (s, e) => this.RadioButtons_CheckedChanged(s, e);
            this.Controls.Add(this.radioAll);
            currentY += 35;

            // 当前页
            this.radioCurrent = new AntdUI.Radio();
            this.radioCurrent.Text = "当前页面";
            this.radioCurrent.Location = new System.Drawing.Point(leftMargin, currentY);
            this.radioCurrent.Size = new System.Drawing.Size(120, 30);
            this.radioCurrent.CheckedChanged += (s, e) => this.RadioButtons_CheckedChanged(s, e);
            this.Controls.Add(this.radioCurrent);
            currentY += 35;

            // 自定义范围
            this.radioCustom = new AntdUI.Radio();
            this.radioCustom.Text = "指定页面:";
            this.radioCustom.Location = new System.Drawing.Point(leftMargin, currentY);
            this.radioCustom.Size = new System.Drawing.Size(100, 30);
            this.radioCustom.CheckedChanged += (s, e) => this.RadioButtons_CheckedChanged(s, e);
            this.Controls.Add(this.radioCustom);

            this.txtPageRange = new AntdUI.Input();
            this.txtPageRange.Location = new System.Drawing.Point(leftMargin + 100, currentY - 3);
            this.txtPageRange.Size = new System.Drawing.Size(280, 36);
            this.txtPageRange.PlaceholderText = "例如: 1,3,5-7,10";
            this.txtPageRange.Enabled = false;
            this.Controls.Add(this.txtPageRange);
            currentY += 55;

            // 按钮区域
            int formWidth = 450; // 与窗体Size.Width保持一致
            this.btnCancel = new AntdUI.Button();
            this.btnCancel.Text = "取消";
            this.btnCancel.Location = new System.Drawing.Point(formWidth - 190, currentY);
            this.btnCancel.Size = new System.Drawing.Size(80, 36);
            this.btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            this.Controls.Add(this.btnCancel);

            this.btnOK = new AntdUI.Button();
            this.btnOK.Text = "应用";
            this.btnOK.Type = AntdUI.TTypeMini.Primary;
            this.btnOK.Location = new System.Drawing.Point(formWidth - 100, currentY);
            this.btnOK.Size = new System.Drawing.Size(80, 36);
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            this.Controls.Add(this.btnOK);
            
            this.ResumeLayout(false);
        }
    }
}
