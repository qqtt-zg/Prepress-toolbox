using System;
using System.Drawing;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms
{
    public class PageBoxEditForm : Window
    {
        private PageBoxInfo _originalInfo;
        private MeasurementUnit _unit;
        
        // 返回值
        public PageBoxInfo ResultInfo { get; private set; }
        public bool ApplyToAllPages { get; private set; }

        private System.Windows.Forms.Panel _mainPanel;
        private AntdUI.Checkbox _applyToAllCheckbox;
        
        // 每种框类型的输入控件
        private BoxInputGroup _mediaBoxInput;
        private BoxInputGroup _cropBoxInput;
        private BoxInputGroup _bleedBoxInput;
        private BoxInputGroup _trimBoxInput;

        public PageBoxEditForm(PageBoxInfo info, MeasurementUnit unit = MeasurementUnit.Millimeter)
        {
            _originalInfo = info;
            _unit = unit;
            
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "编辑页面几何框";
            this.Size = new Size(500, 700);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // 底部面板 (按钮)
            var bottomPanel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(15)
            };
            
            var btnCancel = new AntdUI.Button
            {
                Text = "取消",
                Dock = DockStyle.Right,
                Width = 80,
                Type = TTypeMini.Default
            };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            var btnOk = new AntdUI.Button
            {
                Text = "确定",
                Dock = DockStyle.Right,
                Width = 80,
                Type = TTypeMini.Primary,
            };
            // 简单起见，我们使用特定的位置或添加间隔。
            // Dock=Right 会从右向左堆叠。
            // 我们需要取消按钮（最右），然后是确定按钮（取消按钮的左侧）。
            // 所以：先添加取消按钮，再添加确定按钮。
            
            // 在按钮之间添加间隔
            var spacer = new System.Windows.Forms.Panel { Dock = DockStyle.Right, Width = 10 };

            _applyToAllCheckbox = new AntdUI.Checkbox
            {
                Text = "应用到所有页面",
                Location = new Point(15, 20),
                AutoSize = true
            };

            bottomPanel.Controls.Add(_applyToAllCheckbox);
            bottomPanel.Controls.Add(btnOk);   // 内侧右边
            bottomPanel.Controls.Add(spacer); 
            bottomPanel.Controls.Add(btnCancel); // 最外侧右边
            
            btnOk.Click += BtnOk_Click;

            // 主要内容
            _mainPanel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(10)
            };

            _mediaBoxInput = new BoxInputGroup("MediaBox (物理页面)", _unit) { Dock = DockStyle.Top };
            _cropBoxInput = new BoxInputGroup("CropBox (可见区域)", _unit) { Dock = DockStyle.Top };
            _bleedBoxInput = new BoxInputGroup("BleedBox (出血区域)", _unit) { Dock = DockStyle.Top };
            _trimBoxInput = new BoxInputGroup("TrimBox (成品尺寸)", _unit) { Dock = DockStyle.Top };

            // 以相反的顺序添加，以便 Dock=Top 能够正确堆叠（代码中从下到上 -> UI中从上到下）
            // 或者如果按正常顺序添加，使用 BringToFront。
            // 让我们按相反顺序添加：Trim, Bleed, Crop, Media。
            // 这样 Media 就会在顶部。
            _mainPanel.Controls.Add(_trimBoxInput);
            _mainPanel.Controls.Add(_bleedBoxInput);
            _mainPanel.Controls.Add(_cropBoxInput);
            _mainPanel.Controls.Add(_mediaBoxInput);
            
            this.Controls.Add(_mainPanel);
            this.Controls.Add(bottomPanel);
        }

        private void LoadData()
        {
            _mediaBoxInput.SetValue(_originalInfo.MediaBox);
            _cropBoxInput.SetValue(_originalInfo.CropBox);
            _bleedBoxInput.SetValue(_originalInfo.BleedBox);
            _trimBoxInput.SetValue(_originalInfo.TrimBox);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            ResultInfo = new PageBoxInfo
            {
                PageNumber = _originalInfo.PageNumber,
                Rotation = _originalInfo.Rotation,
                MediaBox = _mediaBoxInput.GetValue(),
                CropBox = _cropBoxInput.GetValue(),
                BleedBox = _bleedBoxInput.GetValue(),
                TrimBox = _trimBoxInput.GetValue()
                // 目前忽略 ArtBox
            };
            
            ApplyToAllPages = _applyToAllCheckbox.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // 用于编辑 BoxDimension 的辅助控件
    public class BoxInputGroup : System.Windows.Forms.Panel
    {
        private MeasurementUnit _unit;
        private InputNumber _inputLeft, _inputBottom, _inputRight, _inputTop, _inputWidth, _inputHeight;
        private bool _isUpdating = false;

        public BoxInputGroup(string title, MeasurementUnit unit)
        {
            _unit = unit;
            Initialize(title);
        }

        private void Initialize(string title)
        {
            this.Padding = new Padding(10);
            this.Height = 160; // 固定高度以保持一致性
            
            var lblTitle = new AntdUI.Label
            {
                Text = title,
                Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 35
            };
            this.Controls.Add(lblTitle);

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3,
                Padding = new Padding(0, 5, 0, 0)
            };
            
            // 定义列：标签(15%)，输入(35%)，标签(15%)，输入(35%)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            
            // 行样式
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // 第0行：左，下（注意：PDF坐标系通常以左下角为原点）
            // 左 (X)
            AddLabel(grid, "左 (X):", 0, 0);
            _inputLeft = CreateInput();
            grid.Controls.Add(_inputLeft, 1, 0);

            // 下 (Y)
            AddLabel(grid, "下 (Y):", 2, 0);
            _inputBottom = CreateInput();
            grid.Controls.Add(_inputBottom, 3, 0);
            
            // 第1行：右，上
            // 右
            AddLabel(grid, "右:", 0, 1);
            _inputRight = CreateInput();
            grid.Controls.Add(_inputRight, 1, 1);

            // 上
            AddLabel(grid, "上:", 2, 1);
            _inputTop = CreateInput();
            grid.Controls.Add(_inputTop, 3, 1);
            
            // 第2行：宽度，高度
            AddLabel(grid, "宽度:", 0, 2, Color.Gray);
            _inputWidth = CreateInput();
            grid.Controls.Add(_inputWidth, 1, 2);

            AddLabel(grid, "高度:", 2, 2, Color.Gray);
            _inputHeight = CreateInput();
            grid.Controls.Add(_inputHeight, 3, 2);
            
            // 事件
            _inputLeft.ValueChanged += (s, v) => RecalculateSize();
            _inputRight.ValueChanged += (s, v) => RecalculateSize();
            _inputBottom.ValueChanged += (s, v) => RecalculateSize();
            _inputTop.ValueChanged += (s, v) => RecalculateSize();
            
            _inputWidth.ValueChanged += (s, v) => RecalculateBoxFromSize();
            _inputHeight.ValueChanged += (s, v) => RecalculateBoxFromSize();

            this.Controls.Add(grid);
            grid.BringToFront();
        }

        private void AddLabel(TableLayoutPanel grid, string text, int col, int row, Color? color = null)
        {
            var label = new AntdUI.Label 
            { 
                Text = text, 
                AutoSize = true,
                Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top, // 垂直居中？顶部也可以
                Location = new Point(0, 8), // 偏移以对齐
                ForeColor = color ?? Color.Black
            };
             // 在单元格中垂直居中
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Dock = DockStyle.Fill;
            
            grid.Controls.Add(label, col, row);
        }

        private InputNumber CreateInput()
        {
            return new InputNumber 
            { 
                Dock = DockStyle.Fill,
                Minimum = -10000, 
                Maximum = 10000
            };
        }

        private void RecalculateSize()
        {
            if (_isUpdating) return;
            _isUpdating = true;
            _inputWidth.Value = _inputRight.Value - _inputLeft.Value;
            _inputHeight.Value = _inputTop.Value - _inputBottom.Value;
            _isUpdating = false;
        }

        private void RecalculateBoxFromSize()
        {
            if (_isUpdating) return;
            _isUpdating = true;
            _inputRight.Value = _inputLeft.Value + _inputWidth.Value;
            _inputTop.Value = _inputBottom.Value + _inputHeight.Value;
            _isUpdating = false;
        }

        public void SetValue(BoxDimension box)
        {
            _isUpdating = true;
            if (box.IsDefined)
            {
                double factor = GetConversionFactor(_unit);
                _inputLeft.Value = (decimal)(box.Left * factor);
                _inputBottom.Value = (decimal)(box.Bottom * factor);
                _inputRight.Value = (decimal)(box.Right * factor);
                _inputTop.Value = (decimal)(box.Top * factor);
                
                _inputWidth.Value = _inputRight.Value - _inputLeft.Value;
                _inputHeight.Value = _inputTop.Value - _inputBottom.Value;
            }
            else
            {
                _inputLeft.Value = 0;
                _inputBottom.Value = 0;
                _inputRight.Value = 0;
                _inputTop.Value = 0;
                _inputWidth.Value = 0;
                _inputHeight.Value = 0;
            }
            _isUpdating = false;
        }

        public BoxDimension GetValue()
        {
            double factor = 1.0 / GetConversionFactor(_unit); 
            return new BoxDimension
            {
                IsDefined = true,
                Left = (double)((double)_inputLeft.Value * factor),
                Bottom = (double)((double)_inputBottom.Value * factor),
                Right = (double)((double)_inputRight.Value * factor),
                Top = (double)((double)_inputTop.Value * factor)
            };
        }

        private double GetConversionFactor(MeasurementUnit unit)
        {
            switch (unit)
            {
                case MeasurementUnit.Millimeter: return 25.4 / 72.0;
                case MeasurementUnit.Inch: return 1.0 / 72.0;
                case MeasurementUnit.Point: return 1.0;
                default: return 1.0;
            }
        }
    }
}
