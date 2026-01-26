namespace WindowsFormsApp3.Forms.Main
{
    partial class MainShellForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Controls usage
        /// </summary>
        private System.Windows.Forms.SplitContainer mainContainer;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Panel logoPanel;

        private System.Windows.Forms.FlowLayoutPanel navPanel;
        private System.Windows.Forms.Panel contentPanel;
        
        // Window Controls (Right side)
        private System.Windows.Forms.Panel headerPanel;
        private System.Windows.Forms.Panel panelCollapseWrapper;
        private AntdUI.Button btnMin;
        private AntdUI.Button btnMax;
        private AntdUI.Button btnClose;

        
        // Window Control Buttons
        private AntdUI.Button btnCollapse;
        private Krypton.Toolkit.KryptonManager kryptonManager;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.kryptonManager = new Krypton.Toolkit.KryptonManager(this.components);
            this.mainContainer = new System.Windows.Forms.SplitContainer();
            this.navPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.panelCollapseWrapper = new System.Windows.Forms.Panel();
            this.btnCollapse = new AntdUI.Button();
            this.logoPanel = new System.Windows.Forms.Panel();
            this.titleLabel = new System.Windows.Forms.Label();
            this.versionLabel = new System.Windows.Forms.Label();
            this.contentPanel = new System.Windows.Forms.Panel();
            this.headerPanel = new System.Windows.Forms.Panel();
            this.btnMin = new AntdUI.Button();
            this.btnMax = new AntdUI.Button();
            this.btnClose = new AntdUI.Button();

            // 
            // kryptonManager
            // 
            this.kryptonManager.GlobalPaletteMode = Krypton.Toolkit.PaletteMode.Office2010Silver;
            ((System.ComponentModel.ISupportInitialize)(this.mainContainer)).BeginInit();
            this.mainContainer.Panel1.SuspendLayout();
            this.mainContainer.Panel2.SuspendLayout();
            this.mainContainer.SuspendLayout();
            this.panelCollapseWrapper.SuspendLayout();
            this.logoPanel.SuspendLayout();
            this.headerPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainContainer
            // 
            this.mainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.mainContainer.Location = new System.Drawing.Point(0, 0);
            this.mainContainer.Margin = new System.Windows.Forms.Padding(2);
            this.mainContainer.Name = "mainContainer";
            // 
            // mainContainer.Panel1
            // 
            this.mainContainer.Panel1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(41)))), ((int)(((byte)(46)))));
            this.mainContainer.Panel1.Controls.Add(this.navPanel);
            this.mainContainer.Panel1.Controls.Add(this.panelCollapseWrapper);
            this.mainContainer.Panel1.Controls.Add(this.logoPanel);
            // 
            // mainContainer.Panel2
            // 
            this.mainContainer.Panel2.Controls.Add(this.contentPanel);
            this.mainContainer.Panel2.Controls.Add(this.headerPanel);
            this.mainContainer.Size = new System.Drawing.Size(975, 698);
            this.mainContainer.SplitterDistance = 170;
            this.mainContainer.SplitterWidth = 1;
            this.mainContainer.TabIndex = 0;
            // 
            // navPanel
            // 
            this.navPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(36)))), ((int)(((byte)(41)))), ((int)(((byte)(46)))));
            this.navPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navPanel.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.navPanel.Location = new System.Drawing.Point(0, 50);
            this.navPanel.Margin = new System.Windows.Forms.Padding(2);
            this.navPanel.Name = "navPanel";
            this.navPanel.Padding = new System.Windows.Forms.Padding(5, 10, 0, 0);
            this.navPanel.Size = new System.Drawing.Size(170, 602);
            this.navPanel.TabIndex = 0;
            this.navPanel.WrapContents = false;
            // 
            // panelCollapseWrapper
            // 
            this.panelCollapseWrapper.BackColor = System.Drawing.Color.Transparent;
            this.panelCollapseWrapper.Controls.Add(this.btnCollapse);
            this.panelCollapseWrapper.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelCollapseWrapper.Location = new System.Drawing.Point(0, 652);
            this.panelCollapseWrapper.Name = "panelCollapseWrapper";
            this.panelCollapseWrapper.Size = new System.Drawing.Size(170, 46);
            this.panelCollapseWrapper.TabIndex = 2;
            // 
            // btnCollapse
            // 
            this.btnCollapse.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnCollapse.ForeColor = System.Drawing.Color.White;
            this.btnCollapse.Ghost = true;
            this.btnCollapse.IconSvg = "MenuFoldOutlined";
            this.btnCollapse.Location = new System.Drawing.Point(0, 0);
            this.btnCollapse.Name = "btnCollapse";
            this.btnCollapse.Size = new System.Drawing.Size(48, 46);
            this.btnCollapse.TabIndex = 4;
            this.btnCollapse.TabStop = false;
            this.btnCollapse.Click += new System.EventHandler(this.BtnCollapse_Click);
            // 
            // logoPanel
            // 
            this.logoPanel.BackColor = System.Drawing.Color.Transparent;
            this.logoPanel.Controls.Add(this.titleLabel);
            this.logoPanel.Controls.Add(this.versionLabel);
            this.logoPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.logoPanel.Location = new System.Drawing.Point(0, 0);
            this.logoPanel.Name = "logoPanel";
            this.logoPanel.Size = new System.Drawing.Size(170, 50);
            this.logoPanel.TabIndex = 0;
            this.logoPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitlePanel_MouseDown);
            // 
            // titleLabel
            // 
            this.titleLabel.AutoSize = true;
            this.titleLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.titleLabel.ForeColor = System.Drawing.Color.White;
            this.titleLabel.Location = new System.Drawing.Point(7, 12);
            this.titleLabel.Margin = new System.Windows.Forms.Padding(0);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new System.Drawing.Size(112, 27);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "大诚工具箱";
            this.titleLabel.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
            this.titleLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitlePanel_MouseDown);
            // 
            // versionLabel
            // 
            this.versionLabel.AutoSize = true;
            this.versionLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.versionLabel.ForeColor = System.Drawing.Color.DarkGray;
            this.versionLabel.Location = new System.Drawing.Point(120, 22);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new System.Drawing.Size(41, 17);
            this.versionLabel.TabIndex = 1;
            this.versionLabel.Text = "v2.3.8";
            this.versionLabel.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.versionLabel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.TitlePanel_MouseDown);
            // 
            // contentPanel
            // 
            this.contentPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(0, 32);
            this.contentPanel.Margin = new System.Windows.Forms.Padding(2);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(804, 666);
            this.contentPanel.TabIndex = 0;
            // 
            // headerPanel
            // 
            this.headerPanel.BackColor = System.Drawing.Color.White;
            this.headerPanel.Controls.Add(this.btnMin);
            this.headerPanel.Controls.Add(this.btnMax);
            this.headerPanel.Controls.Add(this.btnClose);
            this.headerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.headerPanel.Location = new System.Drawing.Point(0, 0);
            this.headerPanel.Name = "headerPanel";
            this.headerPanel.Size = new System.Drawing.Size(804, 32);
            this.headerPanel.TabIndex = 1;
            this.headerPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.HeaderPanel_MouseDown);
            // 
            // btnMin
            // 
            this.btnMin.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnMin.Ghost = true;
            this.btnMin.IconRatio = 0.6F;
            this.btnMin.IconSvg = "MinusOutlined";
            this.btnMin.Location = new System.Drawing.Point(708, 0);
            this.btnMin.Name = "btnMin";
            this.btnMin.Size = new System.Drawing.Size(32, 32);
            this.btnMin.TabIndex = 0;
            this.btnMin.TabStop = false;
            this.btnMin.Click += new System.EventHandler(this.BtnMin_Click);
            // 
            // btnMax
            // 
            this.btnMax.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnMax.Ghost = true;
            this.btnMax.IconRatio = 0.6F;
            this.btnMax.IconSvg = "BorderOutlined";
            this.btnMax.Location = new System.Drawing.Point(740, 0);
            this.btnMax.Name = "btnMax";
            this.btnMax.Size = new System.Drawing.Size(32, 32);
            this.btnMax.TabIndex = 1;
            this.btnMax.TabStop = false;
            this.btnMax.Click += new System.EventHandler(this.BtnMax_Click);
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.Ghost = true;
            this.btnClose.IconRatio = 0.6F;
            this.btnClose.IconSvg = "CloseOutlined";
            this.btnClose.Location = new System.Drawing.Point(772, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(32, 32);
            this.btnClose.TabIndex = 2;
            this.btnClose.TabStop = false;
            this.btnClose.Click += new System.EventHandler(this.BtnClose_Click);
 
            // MainShellForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1200, 750);
            this.Controls.Add(this.mainContainer);

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(900, 640);
            this.Name = "MainShellForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "大诚工具箱";
            this.mainContainer.Panel1.ResumeLayout(false);
            this.mainContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.mainContainer)).EndInit();
            this.mainContainer.ResumeLayout(false);
            this.panelCollapseWrapper.ResumeLayout(false);
            this.logoPanel.ResumeLayout(false);
            this.logoPanel.PerformLayout();
            this.headerPanel.ResumeLayout(false);

            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
