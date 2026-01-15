namespace WindowsFormsApp3.Forms.Controls.Settings
{
    partial class ThemeEditorControl
    {
        private System.ComponentModel.IContainer components = null;

        // 主题选择
        private AntdUI.Label lblThemeSelect;
        private AntdUI.Select cmbThemes;
        private AntdUI.Button btnNewTheme;

        // 颜色组 - 背景色
        private AntdUI.Label lblBackgroundGroup;
        private AntdUI.Label lblBackground;
        private AntdUI.ColorPicker cpBackground;
        private AntdUI.Label lblSurface;
        private AntdUI.ColorPicker cpSurface;
        private AntdUI.Label lblSurfaceLight;
        private AntdUI.ColorPicker cpSurfaceLight;

        // 颜色组 - 文字色
        private AntdUI.Label lblTextGroup;
        private AntdUI.Label lblTextPrimary;
        private AntdUI.ColorPicker cpTextPrimary;
        private AntdUI.Label lblTextSecondary;
        private AntdUI.ColorPicker cpTextSecondary;

        // 颜色组 - 边框色
        private AntdUI.Label lblBorderGroup;
        private AntdUI.Label lblBorder;
        private AntdUI.ColorPicker cpBorder;

        // 颜色组 - 强调色
        private AntdUI.Label lblAccentGroup;
        private AntdUI.Label lblPrimary;
        private AntdUI.ColorPicker cpPrimary;
        private AntdUI.Label lblSuccess;
        private AntdUI.ColorPicker cpSuccess;
        private AntdUI.Label lblWarning;
        private AntdUI.ColorPicker cpWarning;
        private AntdUI.Label lblError;
        private AntdUI.ColorPicker cpError;

        // 颜色组 - 交互色
        private AntdUI.Label lblInteractionGroup;
        private AntdUI.Label lblBackActive;
        private AntdUI.ColorPicker cpBackActive;
        private AntdUI.Label lblBackHover;
        private AntdUI.ColorPicker cpBackHover;
        private System.Windows.Forms.FlowLayoutPanel flowColors;

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
			this.lblThemeSelect = new AntdUI.Label();
			this.cmbThemes = new AntdUI.Select();
			this.btnNewTheme = new AntdUI.Button();
			this.lblBackgroundGroup = new AntdUI.Label();
			this.lblBackground = new AntdUI.Label();
			this.cpBackground = new AntdUI.ColorPicker();
			this.lblSurface = new AntdUI.Label();
			this.cpSurface = new AntdUI.ColorPicker();
			this.lblSurfaceLight = new AntdUI.Label();
			this.cpSurfaceLight = new AntdUI.ColorPicker();
			this.lblTextGroup = new AntdUI.Label();
			this.lblTextPrimary = new AntdUI.Label();
			this.cpTextPrimary = new AntdUI.ColorPicker();
			this.lblTextSecondary = new AntdUI.Label();
			this.cpTextSecondary = new AntdUI.ColorPicker();
			this.lblBorderGroup = new AntdUI.Label();
			this.lblBorder = new AntdUI.Label();
			this.cpBorder = new AntdUI.ColorPicker();
			this.lblAccentGroup = new AntdUI.Label();
			this.lblPrimary = new AntdUI.Label();
			this.cpPrimary = new AntdUI.ColorPicker();
			this.lblSuccess = new AntdUI.Label();
			this.cpSuccess = new AntdUI.ColorPicker();
			this.lblWarning = new AntdUI.Label();
			this.cpWarning = new AntdUI.ColorPicker();
			this.lblError = new AntdUI.Label();
			this.cpError = new AntdUI.ColorPicker();
			this.lblInteractionGroup = new AntdUI.Label();
			this.lblBackActive = new AntdUI.Label();
			this.cpBackActive = new AntdUI.ColorPicker();
			this.lblBackHover = new AntdUI.Label();
			this.cpBackHover = new AntdUI.ColorPicker();
			this.flowColors = new System.Windows.Forms.FlowLayoutPanel();
			this.btnPreview = new AntdUI.Button();
			this.lblPreviewText = new AntdUI.Label();
			this.pnlPreviewSurface = new System.Windows.Forms.Panel();
			this.lblPreviewTitle = new AntdUI.Label();
			this.pnlPreview = new System.Windows.Forms.Panel();
			this.pnlColorConfig = new System.Windows.Forms.Panel();
			this.btnSave = new AntdUI.Button();
			this.btnApply = new AntdUI.Button();
			this.btnImport = new AntdUI.Button();
			this.btnDelete = new AntdUI.Button();
			this.btnExport = new AntdUI.Button();
			this.pnlPreview.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblThemeSelect
			// 
			this.lblThemeSelect.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
			this.lblThemeSelect.Location = new System.Drawing.Point(20, 20);
			this.lblThemeSelect.Name = "lblThemeSelect";
			this.lblThemeSelect.Size = new System.Drawing.Size(100, 30);
			this.lblThemeSelect.TabIndex = 0;
			this.lblThemeSelect.Text = "选择主题:";
			// 
			// cmbThemes
			// 
			this.cmbThemes.Location = new System.Drawing.Point(130, 15);
			this.cmbThemes.Name = "cmbThemes";
			this.cmbThemes.Size = new System.Drawing.Size(250, 40);
			this.cmbThemes.TabIndex = 1;
			// 
			// btnNewTheme
			// 
			this.btnNewTheme.Location = new System.Drawing.Point(400, 15);
			this.btnNewTheme.Name = "btnNewTheme";
			this.btnNewTheme.Size = new System.Drawing.Size(120, 40);
			this.btnNewTheme.TabIndex = 2;
			this.btnNewTheme.Text = "新建主题";
			this.btnNewTheme.Type = AntdUI.TTypeMini.Primary;
			this.btnNewTheme.Click += new System.EventHandler(this.btnNewTheme_Click);
			// 
			// lblBackgroundGroup
			// 
			this.lblBackgroundGroup.Location = new System.Drawing.Point(0, 0);
			this.lblBackgroundGroup.Name = "lblBackgroundGroup";
			this.lblBackgroundGroup.Size = new System.Drawing.Size(0, 0);
			this.lblBackgroundGroup.TabIndex = 0;
			// 
			// lblBackground
			// 
			this.lblBackground.Location = new System.Drawing.Point(0, 0);
			this.lblBackground.Name = "lblBackground";
			this.lblBackground.Size = new System.Drawing.Size(0, 0);
			this.lblBackground.TabIndex = 0;
			// 
			// cpBackground
			// 
			this.cpBackground.Location = new System.Drawing.Point(0, 0);
			this.cpBackground.Name = "cpBackground";
			this.cpBackground.Size = new System.Drawing.Size(0, 0);
			this.cpBackground.TabIndex = 0;
			this.cpBackground.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblSurface
			// 
			this.lblSurface.Location = new System.Drawing.Point(0, 0);
			this.lblSurface.Name = "lblSurface";
			this.lblSurface.Size = new System.Drawing.Size(0, 0);
			this.lblSurface.TabIndex = 0;
			// 
			// cpSurface
			// 
			this.cpSurface.Location = new System.Drawing.Point(0, 0);
			this.cpSurface.Name = "cpSurface";
			this.cpSurface.Size = new System.Drawing.Size(0, 0);
			this.cpSurface.TabIndex = 0;
			this.cpSurface.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblSurfaceLight
			// 
			this.lblSurfaceLight.Location = new System.Drawing.Point(0, 0);
			this.lblSurfaceLight.Name = "lblSurfaceLight";
			this.lblSurfaceLight.Size = new System.Drawing.Size(0, 0);
			this.lblSurfaceLight.TabIndex = 0;
			// 
			// cpSurfaceLight
			// 
			this.cpSurfaceLight.Location = new System.Drawing.Point(0, 0);
			this.cpSurfaceLight.Name = "cpSurfaceLight";
			this.cpSurfaceLight.Size = new System.Drawing.Size(0, 0);
			this.cpSurfaceLight.TabIndex = 0;
			this.cpSurfaceLight.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblTextGroup
			// 
			this.lblTextGroup.Location = new System.Drawing.Point(0, 0);
			this.lblTextGroup.Name = "lblTextGroup";
			this.lblTextGroup.Size = new System.Drawing.Size(0, 0);
			this.lblTextGroup.TabIndex = 0;
			// 
			// lblTextPrimary
			// 
			this.lblTextPrimary.Location = new System.Drawing.Point(0, 0);
			this.lblTextPrimary.Name = "lblTextPrimary";
			this.lblTextPrimary.Size = new System.Drawing.Size(0, 0);
			this.lblTextPrimary.TabIndex = 0;
			// 
			// cpTextPrimary
			// 
			this.cpTextPrimary.Location = new System.Drawing.Point(0, 0);
			this.cpTextPrimary.Name = "cpTextPrimary";
			this.cpTextPrimary.Size = new System.Drawing.Size(0, 0);
			this.cpTextPrimary.TabIndex = 0;
			this.cpTextPrimary.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblTextSecondary
			// 
			this.lblTextSecondary.Location = new System.Drawing.Point(0, 0);
			this.lblTextSecondary.Name = "lblTextSecondary";
			this.lblTextSecondary.Size = new System.Drawing.Size(0, 0);
			this.lblTextSecondary.TabIndex = 0;
			// 
			// cpTextSecondary
			// 
			this.cpTextSecondary.Location = new System.Drawing.Point(0, 0);
			this.cpTextSecondary.Name = "cpTextSecondary";
			this.cpTextSecondary.Size = new System.Drawing.Size(0, 0);
			this.cpTextSecondary.TabIndex = 0;
			this.cpTextSecondary.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblBorderGroup
			// 
			this.lblBorderGroup.Location = new System.Drawing.Point(0, 0);
			this.lblBorderGroup.Name = "lblBorderGroup";
			this.lblBorderGroup.Size = new System.Drawing.Size(0, 0);
			this.lblBorderGroup.TabIndex = 0;
			// 
			// lblBorder
			// 
			this.lblBorder.Location = new System.Drawing.Point(0, 0);
			this.lblBorder.Name = "lblBorder";
			this.lblBorder.Size = new System.Drawing.Size(0, 0);
			this.lblBorder.TabIndex = 0;
			// 
			// cpBorder
			// 
			this.cpBorder.Location = new System.Drawing.Point(0, 0);
			this.cpBorder.Name = "cpBorder";
			this.cpBorder.Size = new System.Drawing.Size(0, 0);
			this.cpBorder.TabIndex = 0;
			this.cpBorder.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblAccentGroup
			// 
			this.lblAccentGroup.Location = new System.Drawing.Point(0, 0);
			this.lblAccentGroup.Name = "lblAccentGroup";
			this.lblAccentGroup.Size = new System.Drawing.Size(0, 0);
			this.lblAccentGroup.TabIndex = 0;
			// 
			// lblPrimary
			// 
			this.lblPrimary.Location = new System.Drawing.Point(0, 0);
			this.lblPrimary.Name = "lblPrimary";
			this.lblPrimary.Size = new System.Drawing.Size(0, 0);
			this.lblPrimary.TabIndex = 0;
			// 
			// cpPrimary
			// 
			this.cpPrimary.Location = new System.Drawing.Point(0, 0);
			this.cpPrimary.Name = "cpPrimary";
			this.cpPrimary.Size = new System.Drawing.Size(0, 0);
			this.cpPrimary.TabIndex = 0;
			this.cpPrimary.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblSuccess
			// 
			this.lblSuccess.Location = new System.Drawing.Point(0, 0);
			this.lblSuccess.Name = "lblSuccess";
			this.lblSuccess.Size = new System.Drawing.Size(0, 0);
			this.lblSuccess.TabIndex = 0;
			// 
			// cpSuccess
			// 
			this.cpSuccess.Location = new System.Drawing.Point(0, 0);
			this.cpSuccess.Name = "cpSuccess";
			this.cpSuccess.Size = new System.Drawing.Size(0, 0);
			this.cpSuccess.TabIndex = 0;
			this.cpSuccess.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblWarning
			// 
			this.lblWarning.Location = new System.Drawing.Point(0, 0);
			this.lblWarning.Name = "lblWarning";
			this.lblWarning.Size = new System.Drawing.Size(0, 0);
			this.lblWarning.TabIndex = 0;
			// 
			// cpWarning
			// 
			this.cpWarning.Location = new System.Drawing.Point(0, 0);
			this.cpWarning.Name = "cpWarning";
			this.cpWarning.Size = new System.Drawing.Size(0, 0);
			this.cpWarning.TabIndex = 0;
			this.cpWarning.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblError
			// 
			this.lblError.Location = new System.Drawing.Point(0, 0);
			this.lblError.Name = "lblError";
			this.lblError.Size = new System.Drawing.Size(0, 0);
			this.lblError.TabIndex = 0;
			// 
			// cpError
			// 
			this.cpError.Location = new System.Drawing.Point(0, 0);
			this.cpError.Name = "cpError";
			this.cpError.Size = new System.Drawing.Size(0, 0);
			this.cpError.TabIndex = 0;
			this.cpError.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblInteractionGroup
			// 
			this.lblInteractionGroup.Location = new System.Drawing.Point(0, 0);
			this.lblInteractionGroup.Name = "lblInteractionGroup";
			this.lblInteractionGroup.Size = new System.Drawing.Size(0, 0);
			this.lblInteractionGroup.TabIndex = 0;
			// 
			// lblBackActive
			// 
			this.lblBackActive.Location = new System.Drawing.Point(0, 0);
			this.lblBackActive.Name = "lblBackActive";
			this.lblBackActive.Size = new System.Drawing.Size(0, 0);
			this.lblBackActive.TabIndex = 0;
			// 
			// cpBackActive
			// 
			this.cpBackActive.Location = new System.Drawing.Point(0, 0);
			this.cpBackActive.Name = "cpBackActive";
			this.cpBackActive.Size = new System.Drawing.Size(0, 0);
			this.cpBackActive.TabIndex = 0;
			this.cpBackActive.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// lblBackHover
			// 
			this.lblBackHover.Location = new System.Drawing.Point(0, 0);
			this.lblBackHover.Name = "lblBackHover";
			this.lblBackHover.Size = new System.Drawing.Size(0, 0);
			this.lblBackHover.TabIndex = 0;
			// 
			// cpBackHover
			// 
			this.cpBackHover.Location = new System.Drawing.Point(0, 0);
			this.cpBackHover.Name = "cpBackHover";
			this.cpBackHover.Size = new System.Drawing.Size(0, 0);
			this.cpBackHover.TabIndex = 0;
			this.cpBackHover.Value = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(119)))), ((int)(((byte)(255)))));
			// 
			// flowColors
			// 
			this.flowColors.Location = new System.Drawing.Point(0, 0);
			this.flowColors.Name = "flowColors";
			this.flowColors.Size = new System.Drawing.Size(200, 100);
			this.flowColors.TabIndex = 0;
			// 
			// btnPreview
			// 
			this.btnPreview.Location = new System.Drawing.Point(20, 320);
			this.btnPreview.Name = "btnPreview";
			this.btnPreview.Size = new System.Drawing.Size(120, 40);
			this.btnPreview.TabIndex = 3;
			this.btnPreview.Text = "示例按钮";
			// 
			// lblPreviewText
			// 
			this.lblPreviewText.Location = new System.Drawing.Point(20, 240);
			this.lblPreviewText.Name = "lblPreviewText";
			this.lblPreviewText.Size = new System.Drawing.Size(300, 60);
			this.lblPreviewText.TabIndex = 2;
			this.lblPreviewText.Text = "这是预览文本，用于展示主题效果。";
			// 
			// pnlPreviewSurface
			// 
			this.pnlPreviewSurface.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlPreviewSurface.Location = new System.Drawing.Point(20, 70);
			this.pnlPreviewSurface.Name = "pnlPreviewSurface";
			this.pnlPreviewSurface.Size = new System.Drawing.Size(300, 150);
			this.pnlPreviewSurface.TabIndex = 1;
			// 
			// lblPreviewTitle
			// 
			this.lblPreviewTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
			this.lblPreviewTitle.Location = new System.Drawing.Point(529, 15);
			this.lblPreviewTitle.Name = "lblPreviewTitle";
			this.lblPreviewTitle.Size = new System.Drawing.Size(300, 40);
			this.lblPreviewTitle.TabIndex = 0;
			this.lblPreviewTitle.Text = "主题预览";
			// 
			// pnlPreview
			// 
			this.pnlPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.pnlPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlPreview.Controls.Add(this.pnlPreviewSurface);
			this.pnlPreview.Controls.Add(this.lblPreviewText);
			this.pnlPreview.Controls.Add(this.btnPreview);
			this.pnlPreview.Location = new System.Drawing.Point(529, 61);
			this.pnlPreview.Name = "pnlPreview";
			this.pnlPreview.Padding = new System.Windows.Forms.Padding(10);
			this.pnlPreview.Size = new System.Drawing.Size(362, 523);
			this.pnlPreview.TabIndex = 4;
			// 
			// pnlColorConfig
			// 
			this.pnlColorConfig.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.pnlColorConfig.AutoScroll = true;
			this.pnlColorConfig.Location = new System.Drawing.Point(20, 61);
			this.pnlColorConfig.Name = "pnlColorConfig";
			this.pnlColorConfig.Padding = new System.Windows.Forms.Padding(20, 0, 20, 0);
			this.pnlColorConfig.Size = new System.Drawing.Size(500, 523);
			this.pnlColorConfig.TabIndex = 3;
			// 
			// btnSave
			// 
			this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnSave.Location = new System.Drawing.Point(36, 590);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(84, 40);
			this.btnSave.TabIndex = 15;
			this.btnSave.Text = "保存";
			this.btnSave.Type = AntdUI.TTypeMini.Success;
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			// 
			// btnApply
			// 
			this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnApply.Location = new System.Drawing.Point(130, 590);
			this.btnApply.Name = "btnApply";
			this.btnApply.Size = new System.Drawing.Size(84, 40);
			this.btnApply.TabIndex = 16;
			this.btnApply.Text = "应用";
			this.btnApply.Type = AntdUI.TTypeMini.Primary;
			this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
			// 
			// btnImport
			// 
			this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnImport.Location = new System.Drawing.Point(400, 590);
			this.btnImport.Name = "btnImport";
			this.btnImport.Size = new System.Drawing.Size(84, 40);
			this.btnImport.TabIndex = 19;
			this.btnImport.Text = "导入";
			this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
			// 
			// btnDelete
			// 
			this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnDelete.Location = new System.Drawing.Point(220, 590);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Size = new System.Drawing.Size(84, 40);
			this.btnDelete.TabIndex = 17;
			this.btnDelete.Text = "删除";
			this.btnDelete.Type = AntdUI.TTypeMini.Error;
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
			// 
			// btnExport
			// 
			this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnExport.Location = new System.Drawing.Point(310, 590);
			this.btnExport.Name = "btnExport";
			this.btnExport.Size = new System.Drawing.Size(84, 40);
			this.btnExport.TabIndex = 18;
			this.btnExport.Text = "导出";
			this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
			// 
			// ThemeEditorControl
			// 
			this.Controls.Add(this.lblPreviewTitle);
			this.Controls.Add(this.btnSave);
			this.Controls.Add(this.lblThemeSelect);
			this.Controls.Add(this.btnApply);
			this.Controls.Add(this.cmbThemes);
			this.Controls.Add(this.btnImport);
			this.Controls.Add(this.btnDelete);
			this.Controls.Add(this.btnNewTheme);
			this.Controls.Add(this.btnExport);
			this.Controls.Add(this.pnlColorConfig);
			this.Controls.Add(this.pnlPreview);
			this.Name = "ThemeEditorControl";
			this.Size = new System.Drawing.Size(920, 650);
			this.pnlPreview.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        private AntdUI.Button btnPreview;
        private AntdUI.Label lblPreviewText;
        private System.Windows.Forms.Panel pnlPreviewSurface;
        private AntdUI.Label lblPreviewTitle;
        private System.Windows.Forms.Panel pnlPreview;
        private System.Windows.Forms.Panel pnlColorConfig;
        private AntdUI.Button btnSave;
        private AntdUI.Button btnApply;
        private AntdUI.Button btnImport;
        private AntdUI.Button btnDelete;
        private AntdUI.Button btnExport;
    }
}
