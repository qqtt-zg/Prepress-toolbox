using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp3.Forms.Controls
{
    partial class PdfInspectorControl
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
            AntdUI.Tabs.StyleLine styleLine1 = new AntdUI.Tabs.StyleLine();
            this._contentPanel = new System.Windows.Forms.Panel();
            this._mainTabs = new AntdUI.Tabs();
            this._headerPanel = new System.Windows.Forms.Panel();
            this._refreshButton = new AntdUI.Button();
            this._unitSelector = new AntdUI.Select();
            this._contentPanel.SuspendLayout();
            this._headerPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _contentPanel
            // 
            this._contentPanel.BackColor = System.Drawing.Color.White;
            this._contentPanel.Controls.Add(this._mainTabs);
            this._contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._contentPanel.Location = new System.Drawing.Point(0, 0); // Header removed for now as per previous code
            this._contentPanel.Name = "_contentPanel";
            this._contentPanel.Padding = new System.Windows.Forms.Padding(0);
            this._contentPanel.Size = new System.Drawing.Size(400, 600);
            this._contentPanel.TabIndex = 1;
            // 
            // _mainTabs
            // 
            this._mainTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this._mainTabs.Gap = 10;
            this._mainTabs.Location = new System.Drawing.Point(0, 0);
            this._mainTabs.Name = "_mainTabs";
            this._mainTabs.Size = new System.Drawing.Size(400, 600);
            this._mainTabs.Style = styleLine1;
            this._mainTabs.TabIndex = 0;
            // 
            // PdfInspectorControl
            // 
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this._contentPanel);
            this.Name = "PdfInspectorControl";
            this.Size = new System.Drawing.Size(400, 600);
            this._contentPanel.ResumeLayout(false);
            this._headerPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        // Note: Field definitions are still in main file or need to be moved here partially.
        // For partial class, we usually keep listeners in main file.
        // Since original file has fields defined, we will rely on partial class merging.
    }
}
