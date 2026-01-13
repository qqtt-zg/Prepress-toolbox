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

        // 预览面板
        private System.Windows.Forms.Panel pnlPreview;
        private AntdUI.Label lblPreviewTitle;
        private AntdUI.Label lblPreviewText;
        private AntdUI.Button btnPreview;
        private System.Windows.Forms.Panel pnlPreviewSurface;

        // 操作按钮
        private AntdUI.Button btnSave;
        private AntdUI.Button btnApply;
        private AntdUI.Button btnDelete;
        private AntdUI.Button btnExport;
        private AntdUI.Button btnImport;

        // 布局容器
        private System.Windows.Forms.Panel pnlColorConfig;
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
            
            // 背景色组
            this.lblBackgroundGroup = new AntdUI.Label();
            this.lblBackground = new AntdUI.Label();
            this.cpBackground = new AntdUI.ColorPicker();
            this.lblSurface = new AntdUI.Label();
            this.cpSurface = new AntdUI.ColorPicker();
            this.lblSurfaceLight = new AntdUI.Label();
            this.cpSurfaceLight = new AntdUI.ColorPicker();
            
            // 文字色组
            this.lblTextGroup = new AntdUI.Label();
            this.lblTextPrimary = new AntdUI.Label();
            this.cpTextPrimary = new AntdUI.ColorPicker();
            this.lblTextSecondary = new AntdUI.Label();
            this.cpTextSecondary = new AntdUI.ColorPicker();
            
            // 边框色
            this.lblBorderGroup = new AntdUI.Label();
            this.lblBorder = new AntdUI.Label();
            this.cpBorder = new AntdUI.ColorPicker();
            
            // 强调色组
            this.lblAccentGroup = new AntdUI.Label();
            this.lblPrimary = new AntdUI.Label();
            this.cpPrimary = new AntdUI.ColorPicker();
            this.lblSuccess = new AntdUI.Label();
            this.cpSuccess = new AntdUI.ColorPicker();
            this.lblWarning = new AntdUI.Label();
            this.cpWarning = new AntdUI.ColorPicker();
            this.lblError = new AntdUI.Label();
            this.cpError = new AntdUI.ColorPicker();
            
            // 交互色组
            this.lblInteractionGroup = new AntdUI.Label();
            this.lblBackActive = new AntdUI.Label();
            this.cpBackActive = new AntdUI.ColorPicker();
            this.lblBackHover = new AntdUI.Label();
            this.cpBackHover = new AntdUI.ColorPicker();
            
            // 预览面板
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.lblPreviewTitle = new AntdUI.Label();
            this.lblPreviewText = new AntdUI.Label();
            this.btnPreview = new AntdUI.Button();
            this.pnlPreviewSurface = new System.Windows.Forms.Panel();
            
            // 操作按钮
            this.btnSave = new AntdUI.Button();
            this.btnApply = new AntdUI.Button();
            this.btnDelete = new AntdUI.Button();
            this.btnExport = new AntdUI.Button();
            this.btnImport = new AntdUI.Button();
            
            // 布局容器
            this.pnlColorConfig = new System.Windows.Forms.Panel();
            this.flowColors = new System.Windows.Forms.FlowLayoutPanel();

            this.pnlPreview.SuspendLayout();
            this.pnlColorConfig.SuspendLayout();
            this.SuspendLayout();

            // 
            // lblThemeSelect
            // 
            this.lblThemeSelect.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblThemeSelect.Location = new System.Drawing.Point(20, 20);
            this.lblThemeSelect.Name = "lblThemeSelect";
            this.lblThemeSelect.Size = new System.Drawing.Size(100, 30);
            this.lblThemeSelect.Text = "选择主题:";

            // 
            // cmbThemes
            // 
            this.cmbThemes.Location = new System.Drawing.Point(130, 15);
            this.cmbThemes.Name = "cmbThemes";
            this.cmbThemes.Size = new System.Drawing.Size(250, 40);
            // 事件绑定在运行时完成（构造函数中）

            // 
            // btnNewTheme
            // 
            this.btnNewTheme.Location = new System.Drawing.Point(400, 15);
            this.btnNewTheme.Name = "btnNewTheme";
            this.btnNewTheme.Size = new System.Drawing.Size(120, 40);
            this.btnNewTheme.Text = "新建主题";
            this.btnNewTheme.Type = AntdUI.TTypeMini.Primary;
            this.btnNewTheme.Click += new System.EventHandler(this.btnNewTheme_Click);

            // 
            // pnlColorConfig
            // 
            this.pnlColorConfig.Location = new System.Drawing.Point(20, 70);
            this.pnlColorConfig.Name = "pnlColorConfig";
            this.pnlColorConfig.Size = new System.Drawing.Size(500, 500);
            this.pnlColorConfig.AutoScroll = true;

            // 
            // pnlPreview
            // 
            this.pnlPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPreview.Location = new System.Drawing.Point(540, 70);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new System.Drawing.Size(350, 400);
            this.pnlPreview.Controls.Add(this.lblPreviewTitle);
            this.pnlPreview.Controls.Add(this.pnlPreviewSurface);
            this.pnlPreview.Controls.Add(this.lblPreviewText);
            this.pnlPreview.Controls.Add(this.btnPreview);

            // 
            // lblPreviewTitle
            // 
            this.lblPreviewTitle.Font = new System.Drawing.Font("Microsoft YaHei UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblPreviewTitle.Location = new System.Drawing.Point(20, 20);
            this.lblPreviewTitle.Size = new System.Drawing.Size(300, 40);
            this.lblPreviewTitle.Text = "主题预览";

            // 
            // pnlPreviewSurface
            // 
            this.pnlPreviewSurface.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlPreviewSurface.Location = new System.Drawing.Point(20, 70);
            this.pnlPreviewSurface.Size = new System.Drawing.Size(300, 150);

            // 
            // lblPreviewText
            // 
            this.lblPreviewText.Location = new System.Drawing.Point(20, 240);
            this.lblPreviewText.Size = new System.Drawing.Size(300, 60);
            this.lblPreviewText.Text = "这是预览文本，用于展示主题效果。";

            // 
            // btnPreview
            // 
            this.btnPreview.Location = new System.Drawing.Point(20, 320);
            this.btnPreview.Size = new System.Drawing.Size(120, 40);
            this.btnPreview.Text = "示例按钮";

            // 操作按钮
            this.btnSave.Location = new System.Drawing.Point(20, 590);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 40);
            this.btnSave.Text = "保存";
            this.btnSave.Type = AntdUI.TTypeMini.Success;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);

            this.btnApply.Location = new System.Drawing.Point(130, 590);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(100, 40);
            this.btnApply.Text = "应用";
            this.btnApply.Type = AntdUI.TTypeMini.Primary;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);

            this.btnDelete.Location = new System.Drawing.Point(240, 590);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(100, 40);
            this.btnDelete.Text = "删除";
            this.btnDelete.Type = AntdUI.TTypeMini.Error;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);

            this.btnExport.Location = new System.Drawing.Point(350, 590);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(100, 40);
            this.btnExport.Text = "导出";
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);

            this.btnImport.Location = new System.Drawing.Point(460, 590);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(100, 40);
            this.btnImport.Text = "导入";
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);

            // 
            // ThemeEditorControl
            // 
            this.Controls.Add(this.lblThemeSelect);
            this.Controls.Add(this.cmbThemes);
            this.Controls.Add(this.btnNewTheme);
            this.Controls.Add(this.pnlColorConfig);
            this.Controls.Add(this.pnlPreview);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnImport);
            this.Name = "ThemeEditorControl";
            this.Size = new System.Drawing.Size(920, 650);

            this.pnlColorConfig.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
