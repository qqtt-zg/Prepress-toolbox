namespace WindowsFormsApp3.Forms.Controls.Settings
{
    partial class SettingsEventControl
    {
        private System.ComponentModel.IContainer components = null;
        
        // 主控件 - 在设计器中声明
        private WindowsFormsApp3.Controls.EventGroupsTreeView eventGroupsTreeView;

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
            this.eventGroupsTreeView = new WindowsFormsApp3.Controls.EventGroupsTreeView();
            this.SuspendLayout();
            // 
            // eventGroupsTreeView
            // 
            this.eventGroupsTreeView.BackColor = System.Drawing.Color.White;
            this.eventGroupsTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.eventGroupsTreeView.Location = new System.Drawing.Point(0, 0);
            this.eventGroupsTreeView.Name = "eventGroupsTreeView";
            this.eventGroupsTreeView.Size = new System.Drawing.Size(800, 600);
            this.eventGroupsTreeView.TabIndex = 0;
            // 
            // SettingsEventControl
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.eventGroupsTreeView);
            this.Name = "SettingsEventControl";
            this.Size = new System.Drawing.Size(800, 600);
            this.ResumeLayout(false);
        }
    }
}
