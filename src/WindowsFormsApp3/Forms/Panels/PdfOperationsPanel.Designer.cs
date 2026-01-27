using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Controls;

namespace WindowsFormsApp3.Forms.Panels
{
    partial class PdfOperationsPanel
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
            if (disposing)
            {
                _pdfPreview?.Dispose();
                _inspectorForm?.Dispose();
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
			this.mainContainer = new System.Windows.Forms.TableLayoutPanel();
			this.previewContainer = new System.Windows.Forms.TableLayoutPanel();
			this.toolbarPanel = new System.Windows.Forms.Panel();
			this.toolbarFlowLayout = new System.Windows.Forms.FlowLayoutPanel();
			this.btnOpen = new AntdUI.Button();
			this._btnSave = new AntdUI.Button();
			this._btnSaveAs = new AntdUI.Button();
			this._btnUndo = new AntdUI.Button();
			this._btnRedo = new AntdUI.Button();
			this.separator1 = new System.Windows.Forms.Panel();
			this.lblMarks = new System.Windows.Forms.Label();
			this._btnCropMarks = new AntdUI.Button();
			this._btnRegMarks = new AntdUI.Button();
			this.separator2 = new System.Windows.Forms.Panel();
			this._btnInspector = new AntdUI.Button();
			this.previewPanel = new System.Windows.Forms.Panel();
			this._pdfPreview = new WindowsFormsApp3.Controls.CefPdfPreviewControl();
			this.statusPanel = new System.Windows.Forms.Panel();
			this._progressOverlay = new WindowsFormsApp3.Controls.StatusBarProgressOverlay();
			this._lblStatus = new System.Windows.Forms.Label();
			this.mainContainer.SuspendLayout();
			this.previewContainer.SuspendLayout();
			this.toolbarPanel.SuspendLayout();
			this.toolbarFlowLayout.SuspendLayout();
			this.previewPanel.SuspendLayout();
			this.statusPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainContainer
			// 
			this.mainContainer.ColumnCount = 1;
			this.mainContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mainContainer.Controls.Add(this.previewContainer, 0, 0);
			this.mainContainer.Controls.Add(this.statusPanel, 0, 1);
			this.mainContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainContainer.Location = new System.Drawing.Point(0, 0);
			this.mainContainer.Margin = new System.Windows.Forms.Padding(0);
			this.mainContainer.Name = "mainContainer";
			this.mainContainer.RowCount = 2;
			this.mainContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mainContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.mainContainer.Size = new System.Drawing.Size(940, 501);
			this.mainContainer.TabIndex = 0;
			// 
			// previewContainer
			// 
			this.previewContainer.ColumnCount = 1;
			this.previewContainer.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.previewContainer.Controls.Add(this.toolbarPanel, 0, 0);
			this.previewContainer.Controls.Add(this.previewPanel, 0, 1);
			this.previewContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.previewContainer.Location = new System.Drawing.Point(0, 0);
			this.previewContainer.Margin = new System.Windows.Forms.Padding(0);
			this.previewContainer.Name = "previewContainer";
			this.previewContainer.RowCount = 2;
			this.previewContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 50F));
			this.previewContainer.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.previewContainer.Size = new System.Drawing.Size(940, 471);
			this.previewContainer.TabIndex = 0;
			// 
			// toolbarPanel
			// 
			this.toolbarPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
			this.toolbarPanel.Controls.Add(this.toolbarFlowLayout);
			this.toolbarPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolbarPanel.Location = new System.Drawing.Point(0, 0);
			this.toolbarPanel.Margin = new System.Windows.Forms.Padding(0);
			this.toolbarPanel.Name = "toolbarPanel";
			this.toolbarPanel.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
			this.toolbarPanel.Size = new System.Drawing.Size(940, 50);
			this.toolbarPanel.TabIndex = 0;
			// 
			// toolbarFlowLayout
			// 
			this.toolbarFlowLayout.Controls.Add(this.btnOpen);
			this.toolbarFlowLayout.Controls.Add(this._btnSave);
			this.toolbarFlowLayout.Controls.Add(this._btnSaveAs);
			this.toolbarFlowLayout.Controls.Add(this._btnUndo);
			this.toolbarFlowLayout.Controls.Add(this._btnRedo);
			this.toolbarFlowLayout.Controls.Add(this.separator1);
			this.toolbarFlowLayout.Controls.Add(this.lblMarks);
			this.toolbarFlowLayout.Controls.Add(this._btnCropMarks);
			this.toolbarFlowLayout.Controls.Add(this._btnRegMarks);
			this.toolbarFlowLayout.Controls.Add(this.separator2);
			this.toolbarFlowLayout.Controls.Add(this._btnInspector);
			this.toolbarFlowLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolbarFlowLayout.Location = new System.Drawing.Point(10, 8);
			this.toolbarFlowLayout.Name = "toolbarFlowLayout";
			this.toolbarFlowLayout.Size = new System.Drawing.Size(920, 34);
			this.toolbarFlowLayout.TabIndex = 0;
			this.toolbarFlowLayout.WrapContents = false;
			// 
			// btnOpen
			// 
			this.btnOpen.Location = new System.Drawing.Point(0, 0);
			this.btnOpen.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
			this.btnOpen.Name = "btnOpen";
			this.btnOpen.Size = new System.Drawing.Size(90, 32);
			this.btnOpen.TabIndex = 0;
			this.btnOpen.Text = "打开PDF";
			this.btnOpen.Type = AntdUI.TTypeMini.Primary;
			this.btnOpen.Click += new System.EventHandler(this.BtnOpen_Click);
			// 
			// _btnSave
			// 
			this._btnSave.Enabled = false;
			this._btnSave.Location = new System.Drawing.Point(98, 0);
			this._btnSave.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
			this._btnSave.Name = "_btnSave";
			this._btnSave.Size = new System.Drawing.Size(70, 32);
			this._btnSave.TabIndex = 1;
			this._btnSave.Text = "保存";
			this._btnSave.Click += new System.EventHandler(this.BtnSave_Click);
			// 
			// _btnSaveAs
			// 
			this._btnSaveAs.Enabled = false;
			this._btnSaveAs.Location = new System.Drawing.Point(176, 0);
			this._btnSaveAs.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
			this._btnSaveAs.Name = "_btnSaveAs";
			this._btnSaveAs.Size = new System.Drawing.Size(80, 32);
			this._btnSaveAs.TabIndex = 2;
			this._btnSaveAs.Text = "另存为";
			this._btnSaveAs.Click += new System.EventHandler(this.BtnSaveAs_Click);
			// 
			// _btnUndo
			// 
			this._btnUndo.Enabled = false;
			this._btnUndo.IconSvg = "UndoOutlined";
			this._btnUndo.Location = new System.Drawing.Point(264, 0);
			this._btnUndo.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
			this._btnUndo.Name = "_btnUndo";
			this._btnUndo.Size = new System.Drawing.Size(80, 32);
			this._btnUndo.TabIndex = 3;
			this._btnUndo.Text = "撤回";
			this._btnUndo.Click += new System.EventHandler(this.BtnUndo_Click);
			// 
			// _btnRedo
			// 
			this._btnRedo.Enabled = false;
			this._btnRedo.IconSvg = "RedoOutlined";
			this._btnRedo.Location = new System.Drawing.Point(352, 0);
			this._btnRedo.Margin = new System.Windows.Forms.Padding(0, 0, 20, 0);
			this._btnRedo.Name = "_btnRedo";
			this._btnRedo.Size = new System.Drawing.Size(80, 32);
			this._btnRedo.TabIndex = 4;
			this._btnRedo.Text = "重做";
			this._btnRedo.Click += new System.EventHandler(this.BtnRedo_Click);
			// 
			// separator1
			// 
			this.separator1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
			this.separator1.Location = new System.Drawing.Point(452, 4);
			this.separator1.Margin = new System.Windows.Forms.Padding(0, 4, 20, 0);
			this.separator1.Name = "separator1";
			this.separator1.Size = new System.Drawing.Size(1, 24);
			this.separator1.TabIndex = 2;
			// 
			// lblMarks
			// 
			this.lblMarks.AutoSize = true;
			this.lblMarks.Font = new System.Drawing.Font("Microsoft YaHei UI", 10F, System.Drawing.FontStyle.Bold);
			this.lblMarks.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
			this.lblMarks.Location = new System.Drawing.Point(473, 6);
			this.lblMarks.Margin = new System.Windows.Forms.Padding(0, 6, 12, 0);
			this.lblMarks.Name = "lblMarks";
			this.lblMarks.Size = new System.Drawing.Size(69, 19);
			this.lblMarks.TabIndex = 3;
			this.lblMarks.Text = "印刷标记:";
			// 
			// _btnCropMarks
			// 
			this._btnCropMarks.Enabled = false;
			this._btnCropMarks.Location = new System.Drawing.Point(554, 0);
			this._btnCropMarks.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
			this._btnCropMarks.Name = "_btnCropMarks";
			this._btnCropMarks.Size = new System.Drawing.Size(120, 32);
			this._btnCropMarks.TabIndex = 5;
			this._btnCropMarks.Text = "添加裁切标记";
			this._btnCropMarks.Click += new System.EventHandler(this.BtnCropMarks_Click);
			// 
			// _btnRegMarks
			// 
			this._btnRegMarks.Enabled = false;
			this._btnRegMarks.Location = new System.Drawing.Point(682, 0);
			this._btnRegMarks.Margin = new System.Windows.Forms.Padding(0, 0, 20, 0);
			this._btnRegMarks.Name = "_btnRegMarks";
			this._btnRegMarks.Size = new System.Drawing.Size(120, 32);
			this._btnRegMarks.TabIndex = 6;
			this._btnRegMarks.Text = "添加套准标记";
			this._btnRegMarks.Click += new System.EventHandler(this.BtnRegMarks_Click);
			// 
			// separator2
			// 
			this.separator2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(220)))), ((int)(((byte)(220)))));
			this.separator2.Location = new System.Drawing.Point(822, 4);
			this.separator2.Margin = new System.Windows.Forms.Padding(0, 4, 20, 0);
			this.separator2.Name = "separator2";
			this.separator2.Size = new System.Drawing.Size(1, 24);
			this.separator2.TabIndex = 7;
			// 
			// _btnInspector
			// 
			this._btnInspector.Enabled = false;
			this._btnInspector.IconSvg = "FileSearchOutlined";
			this._btnInspector.Location = new System.Drawing.Point(843, 0);
			this._btnInspector.Margin = new System.Windows.Forms.Padding(0, 0, 8, 0);
			this._btnInspector.Name = "_btnInspector";
			this._btnInspector.Size = new System.Drawing.Size(100, 32);
			this._btnInspector.TabIndex = 8;
			this._btnInspector.Text = "检查器";
			this._btnInspector.Click += new System.EventHandler(this.BtnInspector_Click);
			// 
			// previewPanel
			// 
			this.previewPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
			this.previewPanel.Controls.Add(this._pdfPreview);
			this.previewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.previewPanel.Location = new System.Drawing.Point(0, 50);
			this.previewPanel.Margin = new System.Windows.Forms.Padding(0);
			this.previewPanel.Name = "previewPanel";
			this.previewPanel.Size = new System.Drawing.Size(940, 421);
			this.previewPanel.TabIndex = 1;
			// 
			// _pdfPreview
			// 
			this._pdfPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(45)))), ((int)(((byte)(45)))));
			this._pdfPreview.Dock = System.Windows.Forms.DockStyle.Fill;
			this._pdfPreview.Location = new System.Drawing.Point(0, 0);
			this._pdfPreview.Name = "_pdfPreview";
			this._pdfPreview.Size = new System.Drawing.Size(940, 421);
			this._pdfPreview.TabIndex = 0;
			this._pdfPreview.PdfLoaded += new System.EventHandler<WindowsFormsApp3.Controls.PdfLoadedEventArgs>(this.PdfPreview_PdfLoaded);
			this._pdfPreview.PageChanged += new System.EventHandler<WindowsFormsApp3.Controls.PageChangedEventArgs>(this.PdfPreview_PageChanged);
			this._pdfPreview.LoadError += new System.EventHandler<WindowsFormsApp3.Controls.PdfLoadErrorEventArgs>(this.PdfPreview_LoadError);
			// 
			// statusPanel
			// 
			this.statusPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(245)))), ((int)(((byte)(245)))));
			this.statusPanel.Controls.Add(this._progressOverlay);
			this.statusPanel.Controls.Add(this._lblStatus);
			this.statusPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.statusPanel.Location = new System.Drawing.Point(0, 471);
			this.statusPanel.Margin = new System.Windows.Forms.Padding(0);
			this.statusPanel.Name = "statusPanel";
			this.statusPanel.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
			this.statusPanel.Size = new System.Drawing.Size(940, 30);
			this.statusPanel.TabIndex = 1;
			// 
			// _progressOverlay
			// 
			this._progressOverlay.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this._progressOverlay.BackColor = System.Drawing.Color.Transparent;
			this._progressOverlay.Location = new System.Drawing.Point(0, 0);
			this._progressOverlay.Margin = new System.Windows.Forms.Padding(0);
			this._progressOverlay.Name = "_progressOverlay";
			this._progressOverlay.Progress = 0;
			this._progressOverlay.ProgressColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(24)))), ((int)(((byte)(144)))), ((int)(((byte)(255)))));
			this._progressOverlay.Size = new System.Drawing.Size(940, 4);
			this._progressOverlay.TabIndex = 2;
			this._progressOverlay.Visible = false;
			// 
			// _lblStatus
			// 
			this._lblStatus.AutoSize = true;
			this._lblStatus.BackColor = System.Drawing.Color.Transparent;
			this._lblStatus.Dock = System.Windows.Forms.DockStyle.Left;
			this._lblStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
			this._lblStatus.Location = new System.Drawing.Point(10, 0);
			this._lblStatus.Name = "_lblStatus";
			this._lblStatus.Padding = new System.Windows.Forms.Padding(0, 7, 0, 0);
			this._lblStatus.Size = new System.Drawing.Size(29, 19);
			this._lblStatus.TabIndex = 0;
			this._lblStatus.Text = "就绪";
			// 
			// PdfOperationsPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.mainContainer);
			this.Name = "PdfOperationsPanel";
			this.Size = new System.Drawing.Size(940, 501);
			this.Load += new System.EventHandler(this.PdfOperationsPanel_Load);
			this.mainContainer.ResumeLayout(false);
			this.previewContainer.ResumeLayout(false);
			this.toolbarPanel.ResumeLayout(false);
			this.toolbarFlowLayout.ResumeLayout(false);
			this.toolbarFlowLayout.PerformLayout();
			this.previewPanel.ResumeLayout(false);
			this.statusPanel.ResumeLayout(false);
			this.statusPanel.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainContainer;
        private System.Windows.Forms.TableLayoutPanel previewContainer;
        private System.Windows.Forms.Panel toolbarPanel;
        private System.Windows.Forms.FlowLayoutPanel toolbarFlowLayout;
        private AntdUI.Button btnOpen;
        private AntdUI.Button _btnSave;
        private AntdUI.Button _btnSaveAs;
        private AntdUI.Button _btnUndo;
        private AntdUI.Button _btnRedo;
        private System.Windows.Forms.Panel separator1;
        private System.Windows.Forms.Label lblMarks;
        private AntdUI.Button _btnCropMarks;
        private AntdUI.Button _btnRegMarks;
        private System.Windows.Forms.Panel separator2;
        private AntdUI.Button _btnInspector;
        private System.Windows.Forms.Panel previewPanel;
        private WindowsFormsApp3.Controls.CefPdfPreviewControl _pdfPreview;
        private System.Windows.Forms.Panel statusPanel;
        private WindowsFormsApp3.Controls.StatusBarProgressOverlay _progressOverlay;
        private System.Windows.Forms.Label _lblStatus;
        private WindowsFormsApp3.Forms.Utils.PdfInspectorForm _inspectorForm;
    }
}
