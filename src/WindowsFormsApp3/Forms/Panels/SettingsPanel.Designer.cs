namespace WindowsFormsApp3.Forms.Panels
{
    partial class SettingsPanel
    {
        private System.ComponentModel.IContainer components = null;
        private AntdUI.Tabs tabsMain;

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
            AntdUI.Tabs.StyleLine styleLine1 = new AntdUI.Tabs.StyleLine();
            this.tabsMain = new AntdUI.Tabs();
            this.SuspendLayout();
            // 
            // tabsMain
            // 
            this.tabsMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabsMain.Location = new System.Drawing.Point(0, 0);
            this.tabsMain.Name = "tabsMain";
            this.tabsMain.Size = new System.Drawing.Size(935, 616);
            this.tabsMain.Style = styleLine1;
            this.tabsMain.TabIndex = 0;
            // 
            // SettingsPanel
            // 
            this.Controls.Add(this.tabsMain);
            this.Name = "SettingsPanel";
            this.Size = new System.Drawing.Size(935, 616);
            this.ResumeLayout(false);

        }
    }
}
