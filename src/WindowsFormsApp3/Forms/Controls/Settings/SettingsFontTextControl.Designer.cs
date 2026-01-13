namespace WindowsFormsApp3.Forms.Controls.Settings
{
    partial class SettingsFontTextControl
    {
        private System.ComponentModel.IContainer components = null;

        // 文字项管理控件
        private System.Windows.Forms.Panel grpTextSettings;
        private System.Windows.Forms.FlowLayoutPanel pnlTextItems;
        private AntdUI.Input txtComboPreview;
        
        // 字体选择控件
        private AntdUI.Label lblFontFamily;
        private AntdUI.Select cmbFontFamily;
        private AntdUI.Button btnReloadFonts;
        private AntdUI.Input txtFontPreview;
        private AntdUI.Label lblPreviewHint;
        private System.Windows.Forms.PictureBox picPdfPreview;

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
            this.grpTextSettings = new System.Windows.Forms.Panel();
            this.pnlTextItems = new System.Windows.Forms.FlowLayoutPanel();
            this.txtComboPreview = new AntdUI.Input();
            this.lblFontFamily = new AntdUI.Label();
            this.cmbFontFamily = new AntdUI.Select();
            this.btnReloadFonts = new AntdUI.Button();
            this.txtFontPreview = new AntdUI.Input();
            this.lblPreviewHint = new AntdUI.Label();
            this.picPdfPreview = new System.Windows.Forms.PictureBox();
            this.grpTextSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPdfPreview)).BeginInit();
            this.SuspendLayout();
            // 
            // grpTextSettings
            // 
            this.grpTextSettings.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.grpTextSettings.Controls.Add(this.pnlTextItems);
            this.grpTextSettings.Controls.Add(this.txtComboPreview);
            this.grpTextSettings.Location = new System.Drawing.Point(438, 86);
            this.grpTextSettings.Name = "grpTextSettings";
            this.grpTextSettings.Size = new System.Drawing.Size(310, 428);
            this.grpTextSettings.TabIndex = 8;
            // 
            // pnlTextItems
            // 
            this.pnlTextItems.AllowDrop = true;
            this.pnlTextItems.AutoScroll = true;
            this.pnlTextItems.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlTextItems.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.pnlTextItems.Location = new System.Drawing.Point(20, 16);
            this.pnlTextItems.Name = "pnlTextItems";
            this.pnlTextItems.Size = new System.Drawing.Size(270, 304);
            this.pnlTextItems.TabIndex = 0;
            this.pnlTextItems.WrapContents = false;
            // 
            // txtComboPreview
            // 
            this.txtComboPreview.Location = new System.Drawing.Point(20, 323);
            this.txtComboPreview.Multiline = true;
            this.txtComboPreview.Name = "txtComboPreview";
            this.txtComboPreview.ReadOnly = true;
            this.txtComboPreview.Size = new System.Drawing.Size(270, 100);
            this.txtComboPreview.TabIndex = 3;
            // 
            // lblFontFamily
            // 
            this.lblFontFamily.Location = new System.Drawing.Point(20, 20);
            this.lblFontFamily.Name = "lblFontFamily";
            this.lblFontFamily.Size = new System.Drawing.Size(100, 32);
            this.lblFontFamily.TabIndex = 0;
            this.lblFontFamily.Text = "标识页字体:";
            // 
            // cmbFontFamily
            // 
            this.cmbFontFamily.Location = new System.Drawing.Point(130, 20);
            this.cmbFontFamily.MaxCount = 24;
            this.cmbFontFamily.Name = "cmbFontFamily";
            this.cmbFontFamily.Size = new System.Drawing.Size(200, 32);
            this.cmbFontFamily.TabIndex = 1;
            this.cmbFontFamily.SelectedIndexChanged += new AntdUI.IntEventHandler(this.CmbFontFamily_SelectedIndexChanged);
            // 
            // btnReloadFonts
            // 
            this.btnReloadFonts.Location = new System.Drawing.Point(350, 20);
            this.btnReloadFonts.Name = "btnReloadFonts";
            this.btnReloadFonts.Size = new System.Drawing.Size(100, 32);
            this.btnReloadFonts.TabIndex = 2;
            this.btnReloadFonts.Text = "重新加载";
            this.btnReloadFonts.Click += new System.EventHandler(this.BtnReloadFonts_Click);
            // 
            // txtFontPreview
            // 
            this.txtFontPreview.Location = new System.Drawing.Point(20, 85);
            this.txtFontPreview.Multiline = true;
            this.txtFontPreview.Name = "txtFontPreview";
            this.txtFontPreview.PlaceholderText = "在此输入测试文本...";
            this.txtFontPreview.Size = new System.Drawing.Size(354, 84);
            this.txtFontPreview.TabIndex = 4;
            this.txtFontPreview.Text = "字体预览: 中文测试 ABC 123";
            this.txtFontPreview.TextChanged += new System.EventHandler(this.TxtFontPreview_TextChanged);
            // 
            // lblPreviewHint
            // 
            this.lblPreviewHint.Location = new System.Drawing.Point(20, 60);
            this.lblPreviewHint.Name = "lblPreviewHint";
            this.lblPreviewHint.Size = new System.Drawing.Size(600, 20);
            this.lblPreviewHint.TabIndex = 3;
            this.lblPreviewHint.Text = "字体预览（左侧输入测试文本，右侧显示实际PDF渲染效果）:";
            // 
            // picPdfPreview
            // 
            this.picPdfPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picPdfPreview.Location = new System.Drawing.Point(25, 177);
            this.picPdfPreview.Name = "picPdfPreview";
            this.picPdfPreview.Size = new System.Drawing.Size(342, 173);
            this.picPdfPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPdfPreview.TabIndex = 5;
            this.picPdfPreview.TabStop = false;
            // 
            // SettingsFontTextControl
            // 
            this.Controls.Add(this.lblFontFamily);
            this.Controls.Add(this.cmbFontFamily);
            this.Controls.Add(this.btnReloadFonts);
            this.Controls.Add(this.lblPreviewHint);
            this.Controls.Add(this.txtFontPreview);
            this.Controls.Add(this.picPdfPreview);
            this.Controls.Add(this.grpTextSettings);
            this.Name = "SettingsFontTextControl";
            this.Size = new System.Drawing.Size(800, 600);
            this.grpTextSettings.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picPdfPreview)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
