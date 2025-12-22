namespace WindowsFormsApp3.Forms.Controls.Settings
{
    partial class SettingsMaterialControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.GroupBox grpMaterial;
        private AntdUI.Label lblMaterial;
        private AntdUI.Input txtMaterial;
        
        private System.Windows.Forms.GroupBox grpBleed;
        private AntdUI.Label lblBleed;
        private AntdUI.Input txtBleed;
        
        private System.Windows.Forms.GroupBox grpShapes;
        private AntdUI.Label lblShapeZero;
        private AntdUI.Input txtShapeZero;
        private AntdUI.Label lblShapeRound;
        private AntdUI.Input txtShapeRound;
        private AntdUI.Label lblShapeEllipse;
        private AntdUI.Input txtShapeEllipse;
        private AntdUI.Label lblShapeCircle;
        private AntdUI.Input txtShapeCircle;
        private AntdUI.Checkbox chkHideRadius;

        private AntdUI.Button btnSave;

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
            // Inits
            this.grpMaterial = new System.Windows.Forms.GroupBox();
            this.lblMaterial = new AntdUI.Label();
            this.txtMaterial = new AntdUI.Input();
            
            this.grpBleed = new System.Windows.Forms.GroupBox();
            this.lblBleed = new AntdUI.Label();
            this.txtBleed = new AntdUI.Input();

            this.grpShapes = new System.Windows.Forms.GroupBox();
            this.lblShapeZero = new AntdUI.Label();
            this.txtShapeZero = new AntdUI.Input();
            this.lblShapeRound = new AntdUI.Label();
            this.txtShapeRound = new AntdUI.Input();
            this.lblShapeEllipse = new AntdUI.Label();
            this.txtShapeEllipse = new AntdUI.Input();
            this.lblShapeCircle = new AntdUI.Label();
            this.txtShapeCircle = new AntdUI.Input();
            this.chkHideRadius = new AntdUI.Checkbox();

            this.btnSave = new AntdUI.Button();

            // 
            // grpMaterial
            // 
            this.grpMaterial.Controls.Add(this.lblMaterial);
            this.grpMaterial.Controls.Add(this.txtMaterial);
            this.grpMaterial.Location = new System.Drawing.Point(20, 20);
            this.grpMaterial.Size = new System.Drawing.Size(700, 80);
            this.grpMaterial.Text = "材料设置";
            
            this.lblMaterial.Location = new System.Drawing.Point(20, 30);
            this.lblMaterial.Size = new System.Drawing.Size(80, 32);
            this.lblMaterial.Text = "默认材料:";
            
            this.txtMaterial.Location = new System.Drawing.Point(110, 30);
            this.txtMaterial.Size = new System.Drawing.Size(570, 32);

            // 
            // grpBleed
            // 
            this.grpBleed.Controls.Add(this.lblBleed);
            this.grpBleed.Controls.Add(this.txtBleed);
            this.grpBleed.Location = new System.Drawing.Point(20, 120);
            this.grpBleed.Size = new System.Drawing.Size(700, 80);
            this.grpBleed.Text = "出血设置";

            this.lblBleed.Location = new System.Drawing.Point(20, 30);
            this.lblBleed.Size = new System.Drawing.Size(80, 32);
            this.lblBleed.Text = "出血值:";
            
            this.txtBleed.Location = new System.Drawing.Point(110, 30);
            this.txtBleed.Size = new System.Drawing.Size(570, 32);
            this.txtBleed.PlaceholderText = "使用逗号分隔，例如 3,5,10";

            // 
            // grpShapes
            // 
            this.grpShapes.Controls.Add(this.lblShapeZero);
            this.grpShapes.Controls.Add(this.txtShapeZero);
            this.grpShapes.Controls.Add(this.lblShapeRound);
            this.grpShapes.Controls.Add(this.txtShapeRound);
            this.grpShapes.Controls.Add(this.lblShapeEllipse);
            this.grpShapes.Controls.Add(this.txtShapeEllipse);
            this.grpShapes.Controls.Add(this.lblShapeCircle);
            this.grpShapes.Controls.Add(this.txtShapeCircle);
            this.grpShapes.Controls.Add(this.chkHideRadius);
            this.grpShapes.Location = new System.Drawing.Point(20, 220);
            this.grpShapes.Size = new System.Drawing.Size(700, 180);
            this.grpShapes.Text = "形状代号";

            // Row 1
            this.lblShapeZero.Location = new System.Drawing.Point(20, 30);
            this.lblShapeZero.Size = new System.Drawing.Size(80, 32);
            this.lblShapeZero.Text = "直角 (Z):";
            this.txtShapeZero.Location = new System.Drawing.Point(110, 30);
            this.txtShapeZero.Size = new System.Drawing.Size(200, 32);

            this.lblShapeRound.Location = new System.Drawing.Point(360, 30);
            this.lblShapeRound.Size = new System.Drawing.Size(80, 32);
            this.lblShapeRound.Text = "圆角 (R):";
            this.txtShapeRound.Location = new System.Drawing.Point(450, 30);
            this.txtShapeRound.Size = new System.Drawing.Size(200, 32);

            // Added Checkbox
            this.chkHideRadius.Location = new System.Drawing.Point(450, 65);
            this.chkHideRadius.Name = "chkHideRadius";
            this.chkHideRadius.Size = new System.Drawing.Size(150, 32);
            this.chkHideRadius.Text = "隐藏半径数值(R)";
            this.chkHideRadius.AutoCheck = true;

            // Row 2 (Shifted down to 100)
            this.lblShapeEllipse.Location = new System.Drawing.Point(20, 100);
            this.lblShapeEllipse.Size = new System.Drawing.Size(80, 32);
            this.lblShapeEllipse.Text = "异形 (Y):";
            this.txtShapeEllipse.Location = new System.Drawing.Point(110, 100);
            this.txtShapeEllipse.Size = new System.Drawing.Size(200, 32);

            this.lblShapeCircle.Location = new System.Drawing.Point(360, 100);
            this.lblShapeCircle.Size = new System.Drawing.Size(80, 32);
            this.lblShapeCircle.Text = "圆形 (C):";
            this.txtShapeCircle.Location = new System.Drawing.Point(450, 100);
            this.txtShapeCircle.Size = new System.Drawing.Size(200, 32);

            // Save Button
            this.btnSave.Location = new System.Drawing.Point(600, 420);
            this.btnSave.Size = new System.Drawing.Size(120, 40);
            this.btnSave.Text = "保存设置";
            this.btnSave.Type = AntdUI.TTypeMini.Primary;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            // 
            // SettingsMaterialControl
            // 
            this.Controls.Add(this.grpMaterial);
            this.Controls.Add(this.grpBleed);
            this.Controls.Add(this.grpShapes);
            this.Controls.Add(this.btnSave);
            this.Name = "SettingsMaterialControl";
            this.Size = new System.Drawing.Size(750, 500);

            this.grpMaterial.ResumeLayout(false);
            this.grpBleed.ResumeLayout(false);
            this.grpShapes.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
