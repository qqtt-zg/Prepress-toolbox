using System.ComponentModel;
using AntdUI;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp3
{
    partial class MaterialSelectFormModern
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
    
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
            AntdUI.Tabs.StyleLine styleLine1 = new AntdUI.Tabs.StyleLine();
            this.fileNameLabel = new AntdUI.Label();
            this.fileNameSeparator = new System.Windows.Forms.Panel();
            this.orderNumberLabel = new AntdUI.Label();
            this.orderNumberTextBox = new AntdUI.Input();
            this.autoIncrementCheckbox = new AntdUI.Checkbox();
            this.quantityLabel = new AntdUI.Label();
            this.quantityTextBox = new AntdUI.Input();
            this.incrementTextBox = new AntdUI.Input();
            this.serialNumberLabel = new AntdUI.Label();
            this.serialNumberTextBox = new AntdUI.Input();
            this.enableSerialSearchCheckbox = new AntdUI.Checkbox();
            this.dimensionsLabel = new AntdUI.Label();
            this.dimensionsTextBox = new AntdUI.Input();
            this.bleedDropdown = new AntdUI.Select();
            this.confirmButton = new AntdUI.Button();
            this.cancelButton = new AntdUI.Button();
            this.colorModeButton = new AntdUI.Button();
            this.filmTypeLightButton = new AntdUI.Button();
            this.filmTypeMatteButton = new AntdUI.Button();
            this.filmTypeNoneButton = new AntdUI.Button();
            this.shapeRightAngleButton = new AntdUI.Button();
            this.shapeCircleButton = new AntdUI.Button();
            this.shapeSpecialButton = new AntdUI.Button();
            this.shapeRoundRectButton = new AntdUI.Button();
            this.folderTreeView = new System.Windows.Forms.TreeView();
            this.folderTreeViewToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.tabs1 = new AntdUI.Tabs();
            this.tabPage3 = new AntdUI.TabPage();
            this.dropdown16 = new AntdUI.Select();
            this.materialButton4 = new AntdUI.Button();
            this.materialButton12 = new AntdUI.Button();
            this.materialButton5 = new AntdUI.Button();
            this.materialButton6 = new AntdUI.Button();
            this.materialButton8 = new AntdUI.Button();
            this.materialButton7 = new AntdUI.Button();
            this.materialButton15 = new AntdUI.Button();
            this.materialButton9 = new AntdUI.Button();
            this.materialButton3 = new AntdUI.Button();
            this.materialButton11 = new AntdUI.Button();
            this.materialButton14 = new AntdUI.Button();
            this.materialButton1 = new AntdUI.Button();
            this.materialButton2 = new AntdUI.Button();
            this.materialButton13 = new AntdUI.Button();
            this.materialButton10 = new AntdUI.Button();
            this.tabPage2 = new AntdUI.TabPage();
            this.radiusTextBox = new AntdUI.Input();
            this.filmTypeRedButton = new AntdUI.Button();
            this.chkIdentifierPage = new AntdUI.Checkbox();
            this.enableImpositionCheckbox = new AntdUI.Checkbox();
            this.materialTypeGroupBox = new System.Windows.Forms.GroupBox();
            this.flatSheetRadioButton = new AntdUI.Radio();
            this.rollMaterialRadioButton = new AntdUI.Radio();
            this.layoutModeGroupBox = new System.Windows.Forms.GroupBox();
            this.continuousLayoutRadioButton = new AntdUI.Radio();
            this.foldingLayoutRadioButton = new AntdUI.Radio();
            this.rowsDisplayLabel = new AntdUI.Label();
            this.columnsDisplayLabel = new AntdUI.Label();
            this.layoutCountDisplayLabel = new AntdUI.Label();
            this.rotationDisplayLabel = new AntdUI.Label();
            this.pdfSizeDisplayLabel = new AntdUI.Label();
            this.duplicateLayoutCheckbox = new AntdUI.Checkbox();
            this.copyModeComboBox = new System.Windows.Forms.ComboBox();
            this.copyTypeComboBox = new System.Windows.Forms.ComboBox();
            this.previewCollapseButton = new AntdUI.Button();
            this.pdfPreviewPanel = new System.Windows.Forms.Panel();
            this.pdfPreviewControl = new System.Windows.Forms.Panel();
            this.label1 = new AntdUI.Label();
            this.presetButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.copyCountNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.tabs1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.materialTypeGroupBox.SuspendLayout();
            this.layoutModeGroupBox.SuspendLayout();
            this.pdfPreviewPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.copyCountNumericUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // fileNameLabel
            // 
            this.fileNameLabel.BackColor = System.Drawing.Color.Transparent;
            this.fileNameLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.fileNameLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.fileNameLabel.Location = new System.Drawing.Point(2, 2);
            this.fileNameLabel.Margin = new System.Windows.Forms.Padding(0, 0, 0, 6);
            this.fileNameLabel.Name = "fileNameLabel";
            this.fileNameLabel.Padding = new System.Windows.Forms.Padding(8, 4, 8, 6);
            this.fileNameLabel.Size = new System.Drawing.Size(396, 28);
            this.fileNameLabel.TabIndex = 0;
            this.fileNameLabel.Text = "材料选择";
            // 
            // fileNameSeparator
            // 
            this.fileNameSeparator.BackColor = System.Drawing.Color.LightGray;
            this.fileNameSeparator.Dock = System.Windows.Forms.DockStyle.Top;
            this.fileNameSeparator.Location = new System.Drawing.Point(2, 30);
            this.fileNameSeparator.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.fileNameSeparator.Name = "fileNameSeparator";
            this.fileNameSeparator.Size = new System.Drawing.Size(396, 1);
            this.fileNameSeparator.TabIndex = 1;
            // 
            // orderNumberLabel
            // 
            this.orderNumberLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.orderNumberLabel.Location = new System.Drawing.Point(150, 333);
            this.orderNumberLabel.Name = "orderNumberLabel";
            this.orderNumberLabel.Size = new System.Drawing.Size(40, 25);
            this.orderNumberLabel.TabIndex = 2;
            this.orderNumberLabel.Text = "订单号:";
            this.orderNumberLabel.Click += new System.EventHandler(this.orderNumberLabel_Click);
            // 
            // orderNumberTextBox
            // 
            this.orderNumberTextBox.BorderWidth = 2F;
            this.orderNumberTextBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.orderNumberTextBox.Location = new System.Drawing.Point(197, 329);
            this.orderNumberTextBox.Name = "orderNumberTextBox";
            this.orderNumberTextBox.Size = new System.Drawing.Size(120, 32);
            this.orderNumberTextBox.TabIndex = 3;
            this.orderNumberTextBox.WaveSize = 0;
            this.orderNumberTextBox.TextChanged += new System.EventHandler(this.orderNumberTextBox_TextChanged);
            // 
            // autoIncrementCheckbox
            // 
            this.autoIncrementCheckbox.Location = new System.Drawing.Point(320, 329);
            this.autoIncrementCheckbox.Margin = new System.Windows.Forms.Padding(0);
            this.autoIncrementCheckbox.Name = "autoIncrementCheckbox";
            this.autoIncrementCheckbox.Size = new System.Drawing.Size(32, 32);
            this.autoIncrementCheckbox.TabIndex = 4;
            this.autoIncrementCheckbox.CheckedChanged += new AntdUI.BoolEventHandler(this.autoIncrementCheckbox_CheckedChanged);
            // 
            // quantityLabel
            // 
            this.quantityLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.quantityLabel.Location = new System.Drawing.Point(150, 370);
            this.quantityLabel.Name = "quantityLabel";
            this.quantityLabel.Size = new System.Drawing.Size(36, 25);
            this.quantityLabel.TabIndex = 5;
            this.quantityLabel.Text = "数量:";
            // 
            // quantityTextBox
            // 
            this.quantityTextBox.BorderWidth = 2F;
            this.quantityTextBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.quantityTextBox.JoinMode = AntdUI.TJoinMode.Left;
            this.quantityTextBox.Location = new System.Drawing.Point(197, 366);
            this.quantityTextBox.Name = "quantityTextBox";
            this.quantityTextBox.Size = new System.Drawing.Size(120, 32);
            this.quantityTextBox.TabIndex = 6;
            this.quantityTextBox.WaveSize = 0;
            this.quantityTextBox.Click += new System.EventHandler(this.quantityTextBox_Click);
            // 
            // incrementTextBox
            // 
            this.incrementTextBox.BorderWidth = 2F;
            this.incrementTextBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.incrementTextBox.JoinMode = AntdUI.TJoinMode.Right;
            this.incrementTextBox.Location = new System.Drawing.Point(317, 366);
            this.incrementTextBox.Name = "incrementTextBox";
            this.incrementTextBox.Size = new System.Drawing.Size(52, 32);
            this.incrementTextBox.TabIndex = 8;
            this.incrementTextBox.WaveSize = 0;
            // 
            // serialNumberLabel
            // 
            this.serialNumberLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.serialNumberLabel.Location = new System.Drawing.Point(150, 296);
            this.serialNumberLabel.Name = "serialNumberLabel";
            this.serialNumberLabel.Size = new System.Drawing.Size(40, 25);
            this.serialNumberLabel.TabIndex = 9;
            this.serialNumberLabel.Text = "序列号:";
            this.serialNumberLabel.Click += new System.EventHandler(this.serialNumberLabel_Click);
            // 
            // serialNumberTextBox
            // 
            this.serialNumberTextBox.BorderWidth = 2F;
            this.serialNumberTextBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.serialNumberTextBox.Location = new System.Drawing.Point(197, 292);
            this.serialNumberTextBox.Name = "serialNumberTextBox";
            this.serialNumberTextBox.Size = new System.Drawing.Size(79, 32);
            this.serialNumberTextBox.TabIndex = 10;
            this.serialNumberTextBox.WaveSize = 0;
            this.serialNumberTextBox.TextChanged += new System.EventHandler(this.serialNumberTextBox_TextChanged);
            this.serialNumberTextBox.Click += new System.EventHandler(this.serialNumberTextBox_Click);
            this.serialNumberTextBox.Enter += new System.EventHandler(this.serialNumberTextBox_Enter);
            // 
            // enableSerialSearchCheckbox
            // 
            this.enableSerialSearchCheckbox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.enableSerialSearchCheckbox.Location = new System.Drawing.Point(282, 292);
            this.enableSerialSearchCheckbox.Name = "enableSerialSearchCheckbox";
            this.enableSerialSearchCheckbox.Size = new System.Drawing.Size(35, 32);
            this.enableSerialSearchCheckbox.TabIndex = 11;
            this.enableSerialSearchCheckbox.Text = "";
            this.enableSerialSearchCheckbox.CheckedChanged += new AntdUI.BoolEventHandler(this.enableSerialSearchCheckbox_CheckedChanged);
            // 
            // dimensionsLabel
            // 
            this.dimensionsLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dimensionsLabel.Location = new System.Drawing.Point(150, 407);
            this.dimensionsLabel.Name = "dimensionsLabel";
            this.dimensionsLabel.Size = new System.Drawing.Size(36, 25);
            this.dimensionsLabel.TabIndex = 15;
            this.dimensionsLabel.Text = "尺寸:";
            // 
            // dimensionsTextBox
            // 
            this.dimensionsTextBox.BorderWidth = 2F;
            this.dimensionsTextBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dimensionsTextBox.JoinMode = AntdUI.TJoinMode.Left;
            this.dimensionsTextBox.Location = new System.Drawing.Point(197, 403);
            this.dimensionsTextBox.Name = "dimensionsTextBox";
            this.dimensionsTextBox.Size = new System.Drawing.Size(120, 32);
            this.dimensionsTextBox.TabIndex = 16;
            this.dimensionsTextBox.WaveSize = 0;
            // 
            // bleedDropdown
            // 
            this.bleedDropdown.BorderWidth = 2F;
            this.bleedDropdown.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bleedDropdown.JoinMode = AntdUI.TJoinMode.Right;
            this.bleedDropdown.Location = new System.Drawing.Point(315, 403);
            this.bleedDropdown.Margin = new System.Windows.Forms.Padding(0);
            this.bleedDropdown.Name = "bleedDropdown";
            this.bleedDropdown.Size = new System.Drawing.Size(54, 32);
            this.bleedDropdown.TabIndex = 18;
            this.bleedDropdown.WaveSize = 0;
            // 
            // confirmButton
            // 
            this.confirmButton.BorderWidth = 2F;
            this.confirmButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.confirmButton.Location = new System.Drawing.Point(243, 598);
            this.confirmButton.Name = "confirmButton";
            this.confirmButton.Size = new System.Drawing.Size(64, 28);
            this.confirmButton.TabIndex = 20;
            this.confirmButton.Text = "确认";
            this.confirmButton.WaveSize = 0;
            this.confirmButton.Click += new System.EventHandler(this.ConfirmButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.BorderWidth = 2F;
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cancelButton.Ghost = true;
            this.cancelButton.Location = new System.Drawing.Point(313, 598);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(64, 28);
            this.cancelButton.TabIndex = 21;
            this.cancelButton.Text = "取消";
            this.cancelButton.WaveSize = 0;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click_1);
            // 
            // colorModeButton
            // 
            this.colorModeButton.BorderWidth = 2F;
            this.colorModeButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.colorModeButton.Ghost = true;
            this.colorModeButton.Location = new System.Drawing.Point(313, 530);
            this.colorModeButton.Name = "colorModeButton";
            this.colorModeButton.Size = new System.Drawing.Size(64, 32);
            this.colorModeButton.TabIndex = 22;
            this.colorModeButton.Text = "彩色";
            this.colorModeButton.WaveSize = 0;
            // 
            // filmTypeLightButton
            // 
            this.filmTypeLightButton.BorderWidth = 1F;
            this.filmTypeLightButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.filmTypeLightButton.Ghost = true;
            this.filmTypeLightButton.JoinMode = AntdUI.TJoinMode.Left;
            this.filmTypeLightButton.Location = new System.Drawing.Point(150, 218);
            this.filmTypeLightButton.Name = "filmTypeLightButton";
            this.filmTypeLightButton.Size = new System.Drawing.Size(55, 32);
            this.filmTypeLightButton.TabIndex = 47;
            this.filmTypeLightButton.Text = "光膜";
            this.filmTypeLightButton.WaveSize = 0;
            // 
            // filmTypeMatteButton
            // 
            this.filmTypeMatteButton.BorderWidth = 1F;
            this.filmTypeMatteButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.filmTypeMatteButton.Ghost = true;
            this.filmTypeMatteButton.JoinMode = AntdUI.TJoinMode.LR;
            this.filmTypeMatteButton.Location = new System.Drawing.Point(205, 218);
            this.filmTypeMatteButton.Name = "filmTypeMatteButton";
            this.filmTypeMatteButton.Size = new System.Drawing.Size(55, 32);
            this.filmTypeMatteButton.TabIndex = 48;
            this.filmTypeMatteButton.Text = "哑膜";
            this.filmTypeMatteButton.WaveSize = 0;
            // 
            // filmTypeNoneButton
            // 
            this.filmTypeNoneButton.BorderWidth = 1F;
            this.filmTypeNoneButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.filmTypeNoneButton.Ghost = true;
            this.filmTypeNoneButton.JoinMode = AntdUI.TJoinMode.Right;
            this.filmTypeNoneButton.Location = new System.Drawing.Point(315, 218);
            this.filmTypeNoneButton.Name = "filmTypeNoneButton";
            this.filmTypeNoneButton.Size = new System.Drawing.Size(55, 32);
            this.filmTypeNoneButton.TabIndex = 50;
            this.filmTypeNoneButton.Text = "不过膜";
            this.filmTypeNoneButton.WaveSize = 0;
            // 
            // shapeRightAngleButton
            // 
            this.shapeRightAngleButton.BorderWidth = 1F;
            this.shapeRightAngleButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.shapeRightAngleButton.Ghost = true;
            this.shapeRightAngleButton.JoinMode = AntdUI.TJoinMode.Left;
            this.shapeRightAngleButton.Location = new System.Drawing.Point(150, 255);
            this.shapeRightAngleButton.Name = "shapeRightAngleButton";
            this.shapeRightAngleButton.Size = new System.Drawing.Size(55, 32);
            this.shapeRightAngleButton.TabIndex = 44;
            this.shapeRightAngleButton.Text = "直角";
            this.shapeRightAngleButton.WaveSize = 0;
            // 
            // shapeCircleButton
            // 
            this.shapeCircleButton.BorderWidth = 1F;
            this.shapeCircleButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.shapeCircleButton.Ghost = true;
            this.shapeCircleButton.JoinMode = AntdUI.TJoinMode.LR;
            this.shapeCircleButton.Location = new System.Drawing.Point(205, 255);
            this.shapeCircleButton.Name = "shapeCircleButton";
            this.shapeCircleButton.Size = new System.Drawing.Size(55, 32);
            this.shapeCircleButton.TabIndex = 45;
            this.shapeCircleButton.Text = "圆形";
            this.shapeCircleButton.WaveSize = 0;
            // 
            // shapeSpecialButton
            // 
            this.shapeSpecialButton.BorderWidth = 1F;
            this.shapeSpecialButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.shapeSpecialButton.Ghost = true;
            this.shapeSpecialButton.JoinMode = AntdUI.TJoinMode.LR;
            this.shapeSpecialButton.Location = new System.Drawing.Point(260, 255);
            this.shapeSpecialButton.Name = "shapeSpecialButton";
            this.shapeSpecialButton.Size = new System.Drawing.Size(55, 32);
            this.shapeSpecialButton.TabIndex = 46;
            this.shapeSpecialButton.Text = "异形";
            this.shapeSpecialButton.WaveSize = 0;
            // 
            // shapeRoundRectButton
            // 
            this.shapeRoundRectButton.BorderWidth = 1F;
            this.shapeRoundRectButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.shapeRoundRectButton.Ghost = true;
            this.shapeRoundRectButton.IconPosition = AntdUI.TAlignMini.None;
            this.shapeRoundRectButton.JoinMode = AntdUI.TJoinMode.Right;
            this.shapeRoundRectButton.Location = new System.Drawing.Point(315, 255);
            this.shapeRoundRectButton.Name = "shapeRoundRectButton";
            this.shapeRoundRectButton.Size = new System.Drawing.Size(55, 32);
            this.shapeRoundRectButton.TabIndex = 47;
            this.shapeRoundRectButton.Text = "圆角矩形";
            this.shapeRoundRectButton.WaveSize = 0;
            // 
            // folderTreeView
            // 
            this.folderTreeView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.folderTreeView.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawText;
            this.folderTreeView.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.folderTreeView.FullRowSelect = true;
            this.folderTreeView.HideSelection = false;
            this.folderTreeView.Indent = 5;
            this.folderTreeView.ItemHeight = 20;
            this.folderTreeView.Location = new System.Drawing.Point(0, 217);
            this.folderTreeView.Name = "folderTreeView";
            this.folderTreeView.ShowLines = false;
            this.folderTreeView.Size = new System.Drawing.Size(144, 286);
            this.folderTreeView.TabIndex = 22;
            this.folderTreeView.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.FolderTreeView_BeforeExpand);
            this.folderTreeView.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.FolderTreeView_AfterExpand);
            this.folderTreeView.DrawNode += new System.Windows.Forms.DrawTreeNodeEventHandler(this.FolderTreeView_DrawNode);
            this.folderTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.FolderTreeView_AfterSelect);
            this.folderTreeView.DoubleClick += new System.EventHandler(this.FolderTreeView_DoubleClick);
            this.folderTreeView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.FolderTreeView_MouseDown);
            this.folderTreeView.MouseMove += new System.Windows.Forms.MouseEventHandler(this.FolderTreeView_MouseMove);
            // 
            // tabs1
            // 
            this.tabs1.Cursor = System.Windows.Forms.Cursors.Default;
            this.tabs1.Location = new System.Drawing.Point(0, 33);
            this.tabs1.Margin = new System.Windows.Forms.Padding(0);
            this.tabs1.Name = "tabs1";
            this.tabs1.Pages.Add(this.tabPage3);
            this.tabs1.Pages.Add(this.tabPage2);
            this.tabs1.Size = new System.Drawing.Size(400, 183);
            this.tabs1.Style = styleLine1;
            this.tabs1.TabIndex = 1;
            this.tabs1.Text = "tabs1";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.dropdown16);
            this.tabPage3.Controls.Add(this.materialButton4);
            this.tabPage3.Controls.Add(this.materialButton12);
            this.tabPage3.Controls.Add(this.materialButton5);
            this.tabPage3.Controls.Add(this.materialButton6);
            this.tabPage3.Controls.Add(this.materialButton8);
            this.tabPage3.Controls.Add(this.materialButton7);
            this.tabPage3.Controls.Add(this.materialButton15);
            this.tabPage3.Controls.Add(this.materialButton9);
            this.tabPage3.Controls.Add(this.materialButton3);
            this.tabPage3.Controls.Add(this.materialButton11);
            this.tabPage3.Controls.Add(this.materialButton14);
            this.tabPage3.Controls.Add(this.materialButton1);
            this.tabPage3.Controls.Add(this.materialButton2);
            this.tabPage3.Controls.Add(this.materialButton13);
            this.tabPage3.Controls.Add(this.materialButton10);
            this.tabPage3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabPage3.Location = new System.Drawing.Point(0, 24);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(400, 159);
            this.tabPage3.TabIndex = 1;
            this.tabPage3.Text = "不干胶";
            // 
            // dropdown16
            // 
            this.dropdown16.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.dropdown16.Items.AddRange(new object[] {
            "Jack",
            "Lucy",
            "不显示箭头",
            "Tom",
            "Jerry"});
            this.dropdown16.Location = new System.Drawing.Point(294, 121);
            this.dropdown16.MaxCount = 2147483647;
            this.dropdown16.Name = "dropdown16";
            this.dropdown16.PlaceholderText = "更多材料";
            this.dropdown16.SelectionColor = System.Drawing.Color.Transparent;
            this.dropdown16.ShowIcon = false;
            this.dropdown16.Size = new System.Drawing.Size(75, 32);
            this.dropdown16.TabIndex = 53;
            this.dropdown16.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.dropdown16.WaveSize = 0;
            // 
            // materialButton4
            // 
            this.materialButton4.BorderWidth = 1F;
            this.materialButton4.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton4.Ghost = true;
            this.materialButton4.Location = new System.Drawing.Point(294, 7);
            this.materialButton4.Name = "materialButton4";
            this.materialButton4.Size = new System.Drawing.Size(75, 32);
            this.materialButton4.TabIndex = 28;
            this.materialButton4.Text = "PET环保";
            this.materialButton4.WaveSize = 0;
            // 
            // materialButton12
            // 
            this.materialButton12.BorderWidth = 1F;
            this.materialButton12.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton12.Ghost = true;
            this.materialButton12.Location = new System.Drawing.Point(294, 83);
            this.materialButton12.Name = "materialButton12";
            this.materialButton12.Size = new System.Drawing.Size(75, 32);
            this.materialButton12.TabIndex = 36;
            this.materialButton12.Text = "PET蓝色";
            this.materialButton12.WaveSize = 0;
            // 
            // materialButton5
            // 
            this.materialButton5.BorderWidth = 1F;
            this.materialButton5.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton5.Ghost = true;
            this.materialButton5.Location = new System.Drawing.Point(24, 45);
            this.materialButton5.Name = "materialButton5";
            this.materialButton5.Size = new System.Drawing.Size(75, 32);
            this.materialButton5.TabIndex = 29;
            this.materialButton5.Text = "PET透明";
            this.materialButton5.WaveSize = 0;
            // 
            // materialButton6
            // 
            this.materialButton6.BorderWidth = 1F;
            this.materialButton6.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton6.Ghost = true;
            this.materialButton6.Location = new System.Drawing.Point(114, 45);
            this.materialButton6.Name = "materialButton6";
            this.materialButton6.Size = new System.Drawing.Size(75, 32);
            this.materialButton6.TabIndex = 30;
            this.materialButton6.Text = "PET哑光";
            this.materialButton6.WaveSize = 0;
            // 
            // materialButton8
            // 
            this.materialButton8.BorderWidth = 1F;
            this.materialButton8.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton8.Ghost = true;
            this.materialButton8.Location = new System.Drawing.Point(294, 45);
            this.materialButton8.Name = "materialButton8";
            this.materialButton8.Size = new System.Drawing.Size(75, 32);
            this.materialButton8.TabIndex = 32;
            this.materialButton8.Text = "PET磨砂";
            this.materialButton8.WaveSize = 0;
            // 
            // materialButton7
            // 
            this.materialButton7.BorderWidth = 1F;
            this.materialButton7.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton7.Ghost = true;
            this.materialButton7.Location = new System.Drawing.Point(204, 45);
            this.materialButton7.Name = "materialButton7";
            this.materialButton7.Size = new System.Drawing.Size(75, 32);
            this.materialButton7.TabIndex = 31;
            this.materialButton7.Text = "PET镭射";
            this.materialButton7.WaveSize = 0;
            // 
            // materialButton15
            // 
            this.materialButton15.BorderWidth = 1F;
            this.materialButton15.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton15.Ghost = true;
            this.materialButton15.Location = new System.Drawing.Point(204, 121);
            this.materialButton15.Name = "materialButton15";
            this.materialButton15.Size = new System.Drawing.Size(75, 32);
            this.materialButton15.TabIndex = 39;
            this.materialButton15.Text = "PET银色";
            this.materialButton15.WaveSize = 0;
            // 
            // materialButton9
            // 
            this.materialButton9.BorderWidth = 1F;
            this.materialButton9.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton9.Ghost = true;
            this.materialButton9.Location = new System.Drawing.Point(24, 121);
            this.materialButton9.Name = "materialButton9";
            this.materialButton9.Size = new System.Drawing.Size(75, 32);
            this.materialButton9.TabIndex = 33;
            this.materialButton9.Text = "PET金色";
            this.materialButton9.WaveSize = 0;
            // 
            // materialButton3
            // 
            this.materialButton3.BorderWidth = 1F;
            this.materialButton3.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton3.Ghost = true;
            this.materialButton3.Location = new System.Drawing.Point(204, 7);
            this.materialButton3.Name = "materialButton3";
            this.materialButton3.Size = new System.Drawing.Size(75, 32);
            this.materialButton3.TabIndex = 27;
            this.materialButton3.Text = "PVC 材料";
            this.materialButton3.WaveSize = 0;
            // 
            // materialButton11
            // 
            this.materialButton11.BorderWidth = 1F;
            this.materialButton11.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton11.Ghost = true;
            this.materialButton11.Location = new System.Drawing.Point(204, 83);
            this.materialButton11.Name = "materialButton11";
            this.materialButton11.Size = new System.Drawing.Size(75, 32);
            this.materialButton11.TabIndex = 35;
            this.materialButton11.Text = "PET红色";
            this.materialButton11.WaveSize = 0;
            // 
            // materialButton14
            // 
            this.materialButton14.BorderWidth = 1F;
            this.materialButton14.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton14.Ghost = true;
            this.materialButton14.Location = new System.Drawing.Point(114, 121);
            this.materialButton14.Name = "materialButton14";
            this.materialButton14.Size = new System.Drawing.Size(75, 32);
            this.materialButton14.TabIndex = 38;
            this.materialButton14.Text = "PP环保";
            this.materialButton14.WaveSize = 0;
            // 
            // materialButton1
            // 
            this.materialButton1.BorderWidth = 1F;
            this.materialButton1.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton1.Ghost = true;
            this.materialButton1.IconPosition = AntdUI.TAlignMini.None;
            this.materialButton1.Location = new System.Drawing.Point(24, 7);
            this.materialButton1.Name = "materialButton1";
            this.materialButton1.Size = new System.Drawing.Size(75, 32);
            this.materialButton1.TabIndex = 29;
            this.materialButton1.Text = "PET 材料";
            this.materialButton1.WaveSize = 0;
            // 
            // materialButton2
            // 
            this.materialButton2.BorderWidth = 1F;
            this.materialButton2.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton2.Ghost = true;
            this.materialButton2.Location = new System.Drawing.Point(114, 7);
            this.materialButton2.Name = "materialButton2";
            this.materialButton2.Size = new System.Drawing.Size(75, 32);
            this.materialButton2.TabIndex = 26;
            this.materialButton2.Text = "PP 材料";
            this.materialButton2.WaveSize = 0;
            // 
            // materialButton13
            // 
            this.materialButton13.BorderWidth = 1F;
            this.materialButton13.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton13.Ghost = true;
            this.materialButton13.Location = new System.Drawing.Point(24, 83);
            this.materialButton13.Name = "materialButton13";
            this.materialButton13.Size = new System.Drawing.Size(75, 32);
            this.materialButton13.TabIndex = 37;
            this.materialButton13.Text = "PET绿色";
            this.materialButton13.WaveSize = 0;
            // 
            // materialButton10
            // 
            this.materialButton10.BorderWidth = 1F;
            this.materialButton10.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.materialButton10.Ghost = true;
            this.materialButton10.Location = new System.Drawing.Point(114, 83);
            this.materialButton10.Name = "materialButton10";
            this.materialButton10.Size = new System.Drawing.Size(75, 32);
            this.materialButton10.TabIndex = 40;
            this.materialButton10.Text = "PET白色";
            this.materialButton10.WaveSize = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabPage2.Location = new System.Drawing.Point(0, 24);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(400, 159);
            this.tabPage2.TabIndex = 2;
            this.tabPage2.Text = "书籍";
            // 
            // radiusTextBox
            // 
            this.radiusTextBox.BorderWidth = 2F;
            this.radiusTextBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.radiusTextBox.Location = new System.Drawing.Point(320, 292);
            this.radiusTextBox.Name = "radiusTextBox";
            this.radiusTextBox.Size = new System.Drawing.Size(55, 32);
            this.radiusTextBox.TabIndex = 50;
            this.radiusTextBox.Visible = false;
            // 
            // filmTypeRedButton
            // 
            this.filmTypeRedButton.BorderWidth = 1F;
            this.filmTypeRedButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.filmTypeRedButton.Ghost = true;
            this.filmTypeRedButton.JoinMode = AntdUI.TJoinMode.LR;
            this.filmTypeRedButton.Location = new System.Drawing.Point(260, 218);
            this.filmTypeRedButton.Name = "filmTypeRedButton";
            this.filmTypeRedButton.Size = new System.Drawing.Size(55, 32);
            this.filmTypeRedButton.TabIndex = 52;
            this.filmTypeRedButton.Text = "红膜";
            this.filmTypeRedButton.WaveSize = 0;
            // 
            // chkIdentifierPage
            // 
            this.chkIdentifierPage.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chkIdentifierPage.Location = new System.Drawing.Point(66, 509);
            this.chkIdentifierPage.Name = "chkIdentifierPage";
            this.chkIdentifierPage.Size = new System.Drawing.Size(78, 26);
            this.chkIdentifierPage.TabIndex = 53;
            this.chkIdentifierPage.Text = "标识页";
            this.chkIdentifierPage.CheckedChanged += new AntdUI.BoolEventHandler(this.ChkIdentifierPage_CheckedChanged);
            // 
            // enableImpositionCheckbox
            // 
            this.enableImpositionCheckbox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.enableImpositionCheckbox.Location = new System.Drawing.Point(62, 541);
            this.enableImpositionCheckbox.Name = "enableImpositionCheckbox";
            this.enableImpositionCheckbox.Size = new System.Drawing.Size(83, 26);
            this.enableImpositionCheckbox.TabIndex = 54;
            this.enableImpositionCheckbox.Text = "启用排版";
            // 
            // materialTypeGroupBox
            // 
            this.materialTypeGroupBox.Controls.Add(this.flatSheetRadioButton);
            this.materialTypeGroupBox.Controls.Add(this.rollMaterialRadioButton);
            this.materialTypeGroupBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.materialTypeGroupBox.Location = new System.Drawing.Point(151, 472);
            this.materialTypeGroupBox.Name = "materialTypeGroupBox";
            this.materialTypeGroupBox.Size = new System.Drawing.Size(226, 45);
            this.materialTypeGroupBox.TabIndex = 55;
            this.materialTypeGroupBox.TabStop = false;
            this.materialTypeGroupBox.Text = "材料类型";
            // 
            // flatSheetRadioButton
            // 
            this.flatSheetRadioButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.flatSheetRadioButton.Location = new System.Drawing.Point(10, 19);
            this.flatSheetRadioButton.Name = "flatSheetRadioButton";
            this.flatSheetRadioButton.Size = new System.Drawing.Size(64, 24);
            this.flatSheetRadioButton.TabIndex = 55;
            this.flatSheetRadioButton.Text = "平张";
            // 
            // rollMaterialRadioButton
            // 
            this.rollMaterialRadioButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.rollMaterialRadioButton.Location = new System.Drawing.Point(80, 19);
            this.rollMaterialRadioButton.Name = "rollMaterialRadioButton";
            this.rollMaterialRadioButton.Size = new System.Drawing.Size(64, 24);
            this.rollMaterialRadioButton.TabIndex = 56;
            this.rollMaterialRadioButton.Text = "卷装";
            // 
            // layoutModeGroupBox
            // 
            this.layoutModeGroupBox.Controls.Add(this.continuousLayoutRadioButton);
            this.layoutModeGroupBox.Controls.Add(this.foldingLayoutRadioButton);
            this.layoutModeGroupBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.layoutModeGroupBox.Location = new System.Drawing.Point(151, 521);
            this.layoutModeGroupBox.Name = "layoutModeGroupBox";
            this.layoutModeGroupBox.Size = new System.Drawing.Size(156, 46);
            this.layoutModeGroupBox.TabIndex = 57;
            this.layoutModeGroupBox.TabStop = false;
            this.layoutModeGroupBox.Text = "排版模式";
            // 
            // continuousLayoutRadioButton
            // 
            this.continuousLayoutRadioButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.continuousLayoutRadioButton.Location = new System.Drawing.Point(10, 15);
            this.continuousLayoutRadioButton.Name = "continuousLayoutRadioButton";
            this.continuousLayoutRadioButton.Size = new System.Drawing.Size(70, 24);
            this.continuousLayoutRadioButton.TabIndex = 57;
            this.continuousLayoutRadioButton.Text = "连拼";
            // 
            // foldingLayoutRadioButton
            // 
            this.foldingLayoutRadioButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.foldingLayoutRadioButton.Location = new System.Drawing.Point(85, 15);
            this.foldingLayoutRadioButton.Name = "foldingLayoutRadioButton";
            this.foldingLayoutRadioButton.Size = new System.Drawing.Size(70, 24);
            this.foldingLayoutRadioButton.TabIndex = 58;
            this.foldingLayoutRadioButton.Text = "折手";
            // 
            // rowsDisplayLabel
            // 
            this.rowsDisplayLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.rowsDisplayLabel.ForeColor = System.Drawing.Color.DodgerBlue;
            this.rowsDisplayLabel.Location = new System.Drawing.Point(18, 571);
            this.rowsDisplayLabel.Name = "rowsDisplayLabel";
            this.rowsDisplayLabel.Size = new System.Drawing.Size(60, 20);
            this.rowsDisplayLabel.TabIndex = 59;
            this.rowsDisplayLabel.Text = "行数: —";
            // 
            // columnsDisplayLabel
            // 
            this.columnsDisplayLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.columnsDisplayLabel.ForeColor = System.Drawing.Color.ForestGreen;
            this.columnsDisplayLabel.Location = new System.Drawing.Point(97, 571);
            this.columnsDisplayLabel.Name = "columnsDisplayLabel";
            this.columnsDisplayLabel.Size = new System.Drawing.Size(60, 20);
            this.columnsDisplayLabel.TabIndex = 60;
            this.columnsDisplayLabel.Text = "列数: —";
            // 
            // layoutCountDisplayLabel
            // 
            this.layoutCountDisplayLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.layoutCountDisplayLabel.ForeColor = System.Drawing.Color.Orange;
            this.layoutCountDisplayLabel.Location = new System.Drawing.Point(176, 571);
            this.layoutCountDisplayLabel.Name = "layoutCountDisplayLabel";
            this.layoutCountDisplayLabel.Size = new System.Drawing.Size(100, 20);
            this.layoutCountDisplayLabel.TabIndex = 61;
            this.layoutCountDisplayLabel.Text = "布局数量: —";
            // 
            // rotationDisplayLabel
            // 
            this.rotationDisplayLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.rotationDisplayLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.rotationDisplayLabel.ForeColor = System.Drawing.Color.MediumPurple;
            this.rotationDisplayLabel.Location = new System.Drawing.Point(290, 571);
            this.rotationDisplayLabel.Name = "rotationDisplayLabel";
            this.rotationDisplayLabel.Size = new System.Drawing.Size(90, 20);
            this.rotationDisplayLabel.TabIndex = 62;
            this.rotationDisplayLabel.Text = "旋转角度: —";
            // 
            // pdfSizeDisplayLabel
            // 
            this.pdfSizeDisplayLabel.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.pdfSizeDisplayLabel.ForeColor = System.Drawing.Color.SeaGreen;
            this.pdfSizeDisplayLabel.Location = new System.Drawing.Point(18, 601);
            this.pdfSizeDisplayLabel.Name = "pdfSizeDisplayLabel";
            this.pdfSizeDisplayLabel.Size = new System.Drawing.Size(210, 20);
            this.pdfSizeDisplayLabel.TabIndex = 63;
            this.pdfSizeDisplayLabel.Text = "PDF尺寸: —";
            // 
            // duplicateLayoutCheckbox
            // 
            this.duplicateLayoutCheckbox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.duplicateLayoutCheckbox.Location = new System.Drawing.Point(150, 441);
            this.duplicateLayoutCheckbox.Name = "duplicateLayoutCheckbox";
            this.duplicateLayoutCheckbox.Size = new System.Drawing.Size(55, 26);
            this.duplicateLayoutCheckbox.TabIndex = 64;
            this.duplicateLayoutCheckbox.Text = "一式";
            this.duplicateLayoutCheckbox.CheckedChanged += new AntdUI.BoolEventHandler(this.DuplicateLayoutCheckbox_CheckedChanged);
            // 
            // copyModeComboBox
            // 
            this.copyModeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.copyModeComboBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.copyModeComboBox.FormattingEnabled = true;
            this.copyModeComboBox.Items.AddRange(new object[] {
            "自适应列",
            "自适应行",
            "不旋转列",
            "不旋转行"});
            this.copyModeComboBox.Location = new System.Drawing.Point(304, 443);
            this.copyModeComboBox.Name = "copyModeComboBox";
            this.copyModeComboBox.Size = new System.Drawing.Size(76, 25);
            this.copyModeComboBox.TabIndex = 67;
            this.copyModeComboBox.SelectedIndexChanged += new System.EventHandler(this.CopyModeComboBox_SelectedIndexChanged);
            // 
            // copyTypeComboBox
            // 
            this.copyTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.copyTypeComboBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.copyTypeComboBox.FormattingEnabled = true;
            this.copyTypeComboBox.Items.AddRange(new object[] {
            "联",
            "份"});
            this.copyTypeComboBox.Location = new System.Drawing.Point(254, 442);
            this.copyTypeComboBox.Name = "copyTypeComboBox";
            this.copyTypeComboBox.Size = new System.Drawing.Size(41, 25);
            this.copyTypeComboBox.TabIndex = 66;
            this.copyTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.CopyTypeComboBox_SelectedIndexChanged);
            // 
            // previewCollapseButton
            // 
            this.previewCollapseButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            this.previewCollapseButton.Location = new System.Drawing.Point(0, 658);
            this.previewCollapseButton.Name = "previewCollapseButton";
            this.previewCollapseButton.Radius = 0;
            this.previewCollapseButton.Size = new System.Drawing.Size(400, 15);
            this.previewCollapseButton.TabIndex = 60;
            this.previewCollapseButton.Text = "▲";
            this.previewCollapseButton.WaveSize = 0;
            this.previewCollapseButton.Click += new System.EventHandler(this.PreviewCollapseButton_Click);
            // 
            // pdfPreviewPanel
            // 
            this.pdfPreviewPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
            this.pdfPreviewPanel.Controls.Add(this.pdfPreviewControl);
            this.pdfPreviewPanel.Location = new System.Drawing.Point(0, 673);
            this.pdfPreviewPanel.Name = "pdfPreviewPanel";
            this.pdfPreviewPanel.Size = new System.Drawing.Size(400, 245);
            this.pdfPreviewPanel.TabIndex = 61;
            // 
            // pdfPreviewControl
            // 
            this.pdfPreviewControl.BackColor = System.Drawing.Color.White;
            this.pdfPreviewControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pdfPreviewControl.Location = new System.Drawing.Point(0, 0);
            this.pdfPreviewControl.Margin = new System.Windows.Forms.Padding(0);
            this.pdfPreviewControl.Name = "pdfPreviewControl";
            this.pdfPreviewControl.Size = new System.Drawing.Size(400, 245);
            this.pdfPreviewControl.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.ForeColor = System.Drawing.Color.Gray;
            this.label1.Location = new System.Drawing.Point(15, 658);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 15);
            this.label1.TabIndex = 66;
            this.label1.Text = "页码: —";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // presetButtonsPanel
            // 
            this.presetButtonsPanel.BackColor = System.Drawing.Color.Transparent;
            this.presetButtonsPanel.Location = new System.Drawing.Point(0, 628);
            this.presetButtonsPanel.Name = "presetButtonsPanel";
            this.presetButtonsPanel.Size = new System.Drawing.Size(400, 30);
            this.presetButtonsPanel.TabIndex = 67;
            // 
            // copyCountNumericUpDown
            // 
            this.copyCountNumericUpDown.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.copyCountNumericUpDown.Location = new System.Drawing.Point(205, 443);
            this.copyCountNumericUpDown.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
            this.copyCountNumericUpDown.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.copyCountNumericUpDown.Name = "copyCountNumericUpDown";
            this.copyCountNumericUpDown.Size = new System.Drawing.Size(43, 23);
            this.copyCountNumericUpDown.TabIndex = 68;
            this.copyCountNumericUpDown.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.copyCountNumericUpDown.ValueChanged += new System.EventHandler(this.CopyCountNumericUpDown_ValueChanged);
            // 
            // MaterialSelectFormModern
            // 
            this.AcceptButton = this.confirmButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(400, 918);
            this.Controls.Add(this.copyCountNumericUpDown);
            this.Controls.Add(this.fileNameSeparator);
            this.Controls.Add(this.fileNameLabel);
            this.Controls.Add(this.presetButtonsPanel);
            this.Controls.Add(this.confirmButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.colorModeButton);
            this.Controls.Add(this.pdfPreviewPanel);
            this.Controls.Add(this.enableImpositionCheckbox);
            this.Controls.Add(this.materialTypeGroupBox);
            this.Controls.Add(this.layoutModeGroupBox);
            this.Controls.Add(this.rowsDisplayLabel);
            this.Controls.Add(this.columnsDisplayLabel);
            this.Controls.Add(this.layoutCountDisplayLabel);
            this.Controls.Add(this.rotationDisplayLabel);
            this.Controls.Add(this.pdfSizeDisplayLabel);
            this.Controls.Add(this.duplicateLayoutCheckbox);
            this.Controls.Add(this.copyModeComboBox);
            this.Controls.Add(this.copyTypeComboBox);
            this.Controls.Add(this.chkIdentifierPage);
            this.Controls.Add(this.filmTypeRedButton);
            this.Controls.Add(this.orderNumberLabel);
            this.Controls.Add(this.orderNumberTextBox);
            this.Controls.Add(this.autoIncrementCheckbox);
            this.Controls.Add(this.quantityLabel);
            this.Controls.Add(this.quantityTextBox);
            this.Controls.Add(this.incrementTextBox);
            this.Controls.Add(this.serialNumberLabel);
            this.Controls.Add(this.serialNumberTextBox);
            this.Controls.Add(this.enableSerialSearchCheckbox);
            this.Controls.Add(this.dimensionsLabel);
            this.Controls.Add(this.dimensionsTextBox);
            this.Controls.Add(this.bleedDropdown);
            this.Controls.Add(this.radiusTextBox);
            this.Controls.Add(this.filmTypeLightButton);
            this.Controls.Add(this.filmTypeMatteButton);
            this.Controls.Add(this.filmTypeNoneButton);
            this.Controls.Add(this.shapeRightAngleButton);
            this.Controls.Add(this.shapeCircleButton);
            this.Controls.Add(this.shapeSpecialButton);
            this.Controls.Add(this.shapeRoundRectButton);
            this.Controls.Add(this.folderTreeView);
            this.Controls.Add(this.tabs1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.previewCollapseButton);
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "MaterialSelectFormModern";
            this.Padding = new System.Windows.Forms.Padding(2);
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.tabs1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.materialTypeGroupBox.ResumeLayout(false);
            this.layoutModeGroupBox.ResumeLayout(false);
            this.pdfPreviewPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.copyCountNumericUpDown)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        // 控件声明

        // 文件名显示控件
        private AntdUI.Label fileNameLabel;
        private System.Windows.Forms.Panel fileNameSeparator;

        // 基本信息控件
        private AntdUI.Label orderNumberLabel;
        private AntdUI.Input orderNumberTextBox;
        private AntdUI.Checkbox autoIncrementCheckbox;
        private AntdUI.Label quantityLabel;
        private AntdUI.Input quantityTextBox;
        private AntdUI.Input incrementTextBox;
        private AntdUI.Label serialNumberLabel;
        private AntdUI.Input serialNumberTextBox;
        private AntdUI.Checkbox enableSerialSearchCheckbox;

        // 操作按钮
        private AntdUI.Button confirmButton;
        private AntdUI.Button cancelButton;
        private AntdUI.Button colorModeButton;
        // 膜类型选择按钮
        private AntdUI.Button filmTypeLightButton;
        private AntdUI.Button filmTypeMatteButton;
        private AntdUI.Button filmTypeNoneButton;
        // 形状选择按钮
        private AntdUI.Button shapeRightAngleButton;
        private AntdUI.Button shapeCircleButton;
        private AntdUI.Button shapeSpecialButton;
        private AntdUI.Button shapeRoundRectButton;

        // TreeView和标签页
        private System.Windows.Forms.TreeView folderTreeView;
        private System.Windows.Forms.ToolTip folderTreeViewToolTip;
        private Tabs tabs1;
        private AntdUI.TabPage tabPage2;
        private AntdUI.TabPage tabPage3;
        private AntdUI.Button materialButton4;
        private AntdUI.Button materialButton15;
        private AntdUI.Button materialButton14;
        private AntdUI.Button materialButton2;
        private AntdUI.Button materialButton13;
        private AntdUI.Button materialButton12;
        private AntdUI.Button materialButton5;
        private AntdUI.Button materialButton6;
        private AntdUI.Button materialButton8;
        private AntdUI.Button materialButton7;
        private AntdUI.Button materialButton9;
        private AntdUI.Button materialButton11;
        private AntdUI.Input radiusTextBox;
        private AntdUI.Select bleedDropdown;
        private AntdUI.Label dimensionsLabel;
        private AntdUI.Input dimensionsTextBox;
        private AntdUI.Button filmTypeRedButton;
        private AntdUI.Checkbox chkIdentifierPage;
        private AntdUI.Button materialButton10;
        private AntdUI.Button materialButton3;
        private Select dropdown16;
        private AntdUI.Button materialButton1;

        // 排版功能控件
        private AntdUI.Checkbox enableImpositionCheckbox;
        private System.Windows.Forms.GroupBox materialTypeGroupBox;
        private AntdUI.Radio flatSheetRadioButton;
        private AntdUI.Radio rollMaterialRadioButton;
        private System.Windows.Forms.GroupBox layoutModeGroupBox;
        private AntdUI.Radio continuousLayoutRadioButton;
        private AntdUI.Radio foldingLayoutRadioButton;
        private AntdUI.Label rowsDisplayLabel;
        private AntdUI.Label columnsDisplayLabel;
        private AntdUI.Label layoutCountDisplayLabel;
        private AntdUI.Label rotationDisplayLabel;
        private AntdUI.Label pdfSizeDisplayLabel;
        private AntdUI.Checkbox duplicateLayoutCheckbox;
        private System.Windows.Forms.ComboBox copyModeComboBox;
        private System.Windows.Forms.ComboBox copyTypeComboBox;

        // PDF 预览相关控件
        private AntdUI.Button previewCollapseButton;
        private System.Windows.Forms.Panel pdfPreviewPanel;
        private System.Windows.Forms.Panel pdfPreviewControl;
        private AntdUI.Label label1;

        // 预设按钮区域
        private System.Windows.Forms.FlowLayoutPanel presetButtonsPanel;
        private NumericUpDown copyCountNumericUpDown;
    }
}