using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp3.Forms.Panels
{
    partial class PdfInspectorPanel
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
			this._toolbarPanel = new System.Windows.Forms.Panel();
			this._openButton = new AntdUI.Button();
			this._inspector = new WindowsFormsApp3.Forms.Controls.PdfInspectorControl();
			this._toolbarPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// _toolbarPanel
			// 
			this._toolbarPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
			this._toolbarPanel.Controls.Add(this._openButton);
			this._toolbarPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this._toolbarPanel.Location = new System.Drawing.Point(0, 0);
			this._toolbarPanel.Name = "_toolbarPanel";
			this._toolbarPanel.Padding = new System.Windows.Forms.Padding(10);
			this._toolbarPanel.Size = new System.Drawing.Size(940, 50);
			this._toolbarPanel.TabIndex = 0;
			// 
			// _openButton
			// 
			this._openButton.Location = new System.Drawing.Point(10, 10);
			this._openButton.Name = "_openButton";
			this._openButton.Size = new System.Drawing.Size(100, 30);
			this._openButton.TabIndex = 0;
			this._openButton.Text = "打开PDF";
			this._openButton.Type = AntdUI.TTypeMini.Primary;
			this._openButton.Click += new System.EventHandler(this.OpenButton_Click);
			// 
			// _inspector
			// 
			this._inspector.BackColor = System.Drawing.Color.White;
			this._inspector.Dock = System.Windows.Forms.DockStyle.Fill;
			this._inspector.Location = new System.Drawing.Point(0, 50);
			this._inspector.Name = "_inspector";
			this._inspector.Padding = new System.Windows.Forms.Padding(10);
			this._inspector.Size = new System.Drawing.Size(940, 476);
			this._inspector.TabIndex = 1;
			this._inspector.PageSelected += new System.EventHandler<int>(this.Inspector_PageSelected);
			// 
			// PdfInspectorPanel
			// 
			this.Controls.Add(this._inspector);
			this.Controls.Add(this._toolbarPanel);
			this.Name = "PdfInspectorPanel";
			this.Size = new System.Drawing.Size(940, 526);
			this._toolbarPanel.ResumeLayout(false);
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel _toolbarPanel;
        private AntdUI.Button _openButton;
        private WindowsFormsApp3.Forms.Controls.PdfInspectorControl _inspector;
    }
}
