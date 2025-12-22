namespace WindowsFormsApp3.Forms.Controls.Settings
{
    partial class SettingsImpositionControl
    {
        private System.ComponentModel.IContainer components = null;

        private AntdUI.Checkbox chkEnable;
        
        private System.Windows.Forms.GroupBox grpFlat;
        private AntdUI.Label lblPaperSize;
        private AntdUI.Input txtPaperWidth;
        private AntdUI.Label lblTime;
        private AntdUI.Input txtPaperHeight;
        private AntdUI.Button btnGetPdfSize;
        private AntdUI.Label lblMargins;
        private AntdUI.Input txtMarginTop;
        private AntdUI.Input txtMarginBottom;
        private AntdUI.Input txtMarginLeft;
        private AntdUI.Input txtMarginRight;

        private System.Windows.Forms.GroupBox grpRoll;
        private AntdUI.Label lblRollSize;
        private AntdUI.Input txtFixedWidth;
        private AntdUI.Input txtMinLength;
        private AntdUI.Label lblRollMargins;
        private AntdUI.Input txtRollMarginTop;
        private AntdUI.Input txtRollMarginBottom;
        private AntdUI.Input txtRollMarginLeft;
        private AntdUI.Input txtRollMarginRight;

        private System.Windows.Forms.GroupBox grpLayout;
        private AntdUI.Label lblLayout;
        private AntdUI.Input txtRows;
        private AntdUI.Label lblX;
        private AntdUI.Input txtCols;

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
            this.chkEnable = new AntdUI.Checkbox();
            
            this.grpFlat = new System.Windows.Forms.GroupBox();
            this.lblPaperSize = new AntdUI.Label();
            this.txtPaperWidth = new AntdUI.Input();
            this.lblTime = new AntdUI.Label();
            this.txtPaperHeight = new AntdUI.Input();
            this.btnGetPdfSize = new AntdUI.Button();
            this.lblMargins = new AntdUI.Label();
            this.txtMarginTop = new AntdUI.Input();
            this.txtMarginBottom = new AntdUI.Input();
            this.txtMarginLeft = new AntdUI.Input();
            this.txtMarginRight = new AntdUI.Input();

            this.grpRoll = new System.Windows.Forms.GroupBox();
            this.lblRollSize = new AntdUI.Label();
            this.txtFixedWidth = new AntdUI.Input();
            this.txtMinLength = new AntdUI.Input();
            this.lblRollMargins = new AntdUI.Label();
            this.txtRollMarginTop = new AntdUI.Input();
            this.txtRollMarginBottom = new AntdUI.Input();
            this.txtRollMarginLeft = new AntdUI.Input();
            this.txtRollMarginRight = new AntdUI.Input();

            this.grpLayout = new System.Windows.Forms.GroupBox();
            this.lblLayout = new AntdUI.Label();
            this.txtRows = new AntdUI.Input();
            this.lblX = new AntdUI.Label();
            this.txtCols = new AntdUI.Input();
            this.btnSave = new AntdUI.Button();

            this.grpFlat.SuspendLayout();
            this.grpRoll.SuspendLayout();
            this.grpLayout.SuspendLayout();
            this.SuspendLayout();

            // chkEnable
            this.chkEnable.Location = new System.Drawing.Point(20, 10);
            this.chkEnable.Name = "chkEnable";
            this.chkEnable.Size = new System.Drawing.Size(150, 32);
            this.chkEnable.Text = "启用排版功能";

            // grpFlat
            this.grpFlat.Controls.Add(this.lblPaperSize);
            this.grpFlat.Controls.Add(this.txtPaperWidth);
            this.grpFlat.Controls.Add(this.lblTime);
            this.grpFlat.Controls.Add(this.txtPaperHeight);
            this.grpFlat.Controls.Add(this.btnGetPdfSize);
            this.grpFlat.Controls.Add(this.lblMargins);
            this.grpFlat.Controls.Add(this.txtMarginTop);
            this.grpFlat.Controls.Add(this.txtMarginBottom);
            this.grpFlat.Controls.Add(this.txtMarginLeft);
            this.grpFlat.Controls.Add(this.txtMarginRight);
            this.grpFlat.Location = new System.Drawing.Point(20, 50);
            this.grpFlat.Size = new System.Drawing.Size(700, 120);
            this.grpFlat.Text = "平张材料";

            this.lblPaperSize.Location = new System.Drawing.Point(10, 25);
            this.lblPaperSize.Size = new System.Drawing.Size(120, 32);
            this.lblPaperSize.Text = "纸张尺寸(W x H):";

            this.txtPaperWidth.Location = new System.Drawing.Point(130, 25);
            this.txtPaperWidth.Size = new System.Drawing.Size(100, 32);
            this.txtPaperWidth.PlaceholderText = "宽";

            this.lblTime.Location = new System.Drawing.Point(235, 25);
            this.lblTime.Size = new System.Drawing.Size(20, 32);
            this.lblTime.Text = "x";

            this.txtPaperHeight.Location = new System.Drawing.Point(260, 25);
            this.txtPaperHeight.Size = new System.Drawing.Size(100, 32);
            this.txtPaperHeight.PlaceholderText = "高";

            this.btnGetPdfSize.Location = new System.Drawing.Point(370, 25);
            this.btnGetPdfSize.Size = new System.Drawing.Size(120, 32);
            this.btnGetPdfSize.Text = "从PDF获取尺寸";
            this.btnGetPdfSize.Click += new System.EventHandler(this.BtnGetPdfSize_Click);

            this.lblMargins.Location = new System.Drawing.Point(10, 70);
            this.lblMargins.Size = new System.Drawing.Size(120, 32);
            this.lblMargins.Text = "边距(上/下/左/右):";

            this.txtMarginTop.Location = new System.Drawing.Point(130, 70); this.txtMarginTop.Size = new System.Drawing.Size(80, 32); this.txtMarginTop.PlaceholderText = "上";
            this.txtMarginBottom.Location = new System.Drawing.Point(220, 70); this.txtMarginBottom.Size = new System.Drawing.Size(80, 32); this.txtMarginBottom.PlaceholderText = "下";
            this.txtMarginLeft.Location = new System.Drawing.Point(310, 70); this.txtMarginLeft.Size = new System.Drawing.Size(80, 32); this.txtMarginLeft.PlaceholderText = "左";
            this.txtMarginRight.Location = new System.Drawing.Point(400, 70); this.txtMarginRight.Size = new System.Drawing.Size(80, 32); this.txtMarginRight.PlaceholderText = "右";

            // grpRoll
            this.grpRoll.Controls.Add(this.lblRollSize);
            this.grpRoll.Controls.Add(this.txtFixedWidth);
            this.grpRoll.Controls.Add(this.txtMinLength);
            this.grpRoll.Controls.Add(this.lblRollMargins);
            this.grpRoll.Controls.Add(this.txtRollMarginTop);
            this.grpRoll.Controls.Add(this.txtRollMarginBottom);
            this.grpRoll.Controls.Add(this.txtRollMarginLeft);
            this.grpRoll.Controls.Add(this.txtRollMarginRight);
            this.grpRoll.Location = new System.Drawing.Point(20, 180);
            this.grpRoll.Size = new System.Drawing.Size(700, 120);
            this.grpRoll.Text = "卷装材料";

            this.lblRollSize.Location = new System.Drawing.Point(10, 25);
            this.lblRollSize.Size = new System.Drawing.Size(120, 32);
            this.lblRollSize.Text = "固定宽/最小长:";

            this.txtFixedWidth.Location = new System.Drawing.Point(130, 25); this.txtFixedWidth.Size = new System.Drawing.Size(100, 32); this.txtFixedWidth.PlaceholderText = "固定宽度";
            this.txtMinLength.Location = new System.Drawing.Point(240, 25); this.txtMinLength.Size = new System.Drawing.Size(100, 32); this.txtMinLength.PlaceholderText = "最小长度";

            this.lblRollMargins.Location = new System.Drawing.Point(10, 70);
            this.lblRollMargins.Size = new System.Drawing.Size(120, 32);
            this.lblRollMargins.Text = "边距(上/下/左/右):";

            this.txtRollMarginTop.Location = new System.Drawing.Point(130, 70); this.txtRollMarginTop.Size = new System.Drawing.Size(80, 32); this.txtRollMarginTop.PlaceholderText = "上";
            this.txtRollMarginBottom.Location = new System.Drawing.Point(220, 70); this.txtRollMarginBottom.Size = new System.Drawing.Size(80, 32); this.txtRollMarginBottom.PlaceholderText = "下";
            this.txtRollMarginLeft.Location = new System.Drawing.Point(310, 70); this.txtRollMarginLeft.Size = new System.Drawing.Size(80, 32); this.txtRollMarginLeft.PlaceholderText = "左";
            this.txtRollMarginRight.Location = new System.Drawing.Point(400, 70); this.txtRollMarginRight.Size = new System.Drawing.Size(80, 32); this.txtRollMarginRight.PlaceholderText = "右";

            // grpLayout
            this.grpLayout.Controls.Add(this.lblLayout);
            this.grpLayout.Controls.Add(this.txtRows);
            this.grpLayout.Controls.Add(this.lblX);
            this.grpLayout.Controls.Add(this.txtCols);
            this.grpLayout.Location = new System.Drawing.Point(20, 310);
            this.grpLayout.Size = new System.Drawing.Size(700, 70);
            this.grpLayout.Text = "布局设置";

            this.lblLayout.Location = new System.Drawing.Point(10, 25);
            this.lblLayout.Size = new System.Drawing.Size(120, 32);
            this.lblLayout.Text = "行列设置(行x列):";

            this.txtRows.Location = new System.Drawing.Point(130, 25);
            this.txtRows.Size = new System.Drawing.Size(100, 32);
            this.txtRows.PlaceholderText = "行数";

            this.lblX.Location = new System.Drawing.Point(235, 25);
            this.lblX.Size = new System.Drawing.Size(20, 32);
            this.lblX.Text = "x";

            this.txtCols.Location = new System.Drawing.Point(260, 25);
            this.txtCols.Size = new System.Drawing.Size(100, 32);
            this.txtCols.PlaceholderText = "列数";

            // Save Button
            this.btnSave.Location = new System.Drawing.Point(620, 400);
            this.btnSave.Size = new System.Drawing.Size(100, 40);
            this.btnSave.Text = "保存";
            this.btnSave.Type = AntdUI.TTypeMini.Primary;
            this.btnSave.Click += new System.EventHandler(this.BtnSave_Click);

            // SettingsImpositionControl
            this.Controls.Add(this.chkEnable);
            this.Controls.Add(this.grpFlat);
            this.Controls.Add(this.grpRoll);
            this.Controls.Add(this.grpLayout);
            this.Controls.Add(this.btnSave);
            this.Name = "SettingsImpositionControl";
            this.Size = new System.Drawing.Size(750, 460);

            this.grpFlat.ResumeLayout(false);
            this.grpRoll.ResumeLayout(false);
            this.grpLayout.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
