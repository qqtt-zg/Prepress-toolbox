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
        
        // Return values
        public PageBoxInfo ResultInfo { get; private set; }
        public bool ApplyToAllPages { get; private set; }

        private System.Windows.Forms.Panel _mainPanel;
        private AntdUI.Checkbox _applyToAllCheckbox;
        
        // Input controls for each box type
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
            
            // Bottom Panel (Buttons)
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
            // Initializing location manually isn't needed if we use simple docking with spacers or margins, 
            // but for simplicity let's stick to absolute positioning or adding spacer.
            // Let's use specific location hack for now to match previous style or just separate them.
            // Actually, Dock=Right stacks them right-to-left. 
            // We need Cancel (Rightmost), then OK (Left of Cancel).
            // So Add Cancel first (Dock Right), then Add OK (Dock Right)? 
            // No, first added is right-most.
            // Wait, Dock=Right: First added goes to the right edge. Second added goes to the left of the First.
            // So: Add Cancel, then Add OK.
            
            // Add a spacer between buttons
            var spacer = new System.Windows.Forms.Panel { Dock = DockStyle.Right, Width = 10 };

            _applyToAllCheckbox = new AntdUI.Checkbox
            {
                Text = "应用到所有页面",
                Location = new Point(15, 20),
                AutoSize = true
            };

            bottomPanel.Controls.Add(_applyToAllCheckbox);
            bottomPanel.Controls.Add(btnOk);   // Inner Right
            bottomPanel.Controls.Add(spacer); 
            bottomPanel.Controls.Add(btnCancel); // Outermost Right
            
            btnOk.Click += BtnOk_Click;

            // Main Content
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

            // Add in reverse order for Dock=Top to stack correctly (Bottom to Top in code -> Top to Bottom in UI)
            // Or just use BringToFront if added in normal order.
            // Let's add them in Reverse order: Trim, Bleed, Crop, Media.
            // Then Media will be at the top.
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
                // ArtBox ignored for now
            };
            
            ApplyToAllPages = _applyToAllCheckbox.Checked;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // Helper control for editing a BoxDimension
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
            this.Height = 160; // Fixed height for consistency
            
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
            
            // Define columns: Label(15%), Input(35%), Label(15%), Input(35%)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            
            // Row styles
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // Row 0: Left, Bottom (Note: PDF coord system usually Left/Bottom is origin)
            // Left (X)
            AddLabel(grid, "左 (X):", 0, 0);
            _inputLeft = CreateInput();
            grid.Controls.Add(_inputLeft, 1, 0);

            // Bottom (Y)
            AddLabel(grid, "下 (Y):", 2, 0);
            _inputBottom = CreateInput();
            grid.Controls.Add(_inputBottom, 3, 0);
            
            // Row 1: Right, Top
            // Right
            AddLabel(grid, "右:", 0, 1);
            _inputRight = CreateInput();
            grid.Controls.Add(_inputRight, 1, 1);

            // Top
            AddLabel(grid, "上:", 2, 1);
            _inputTop = CreateInput();
            grid.Controls.Add(_inputTop, 3, 1);
            
            // Row 2: Width, Height
            AddLabel(grid, "宽度:", 0, 2, Color.Gray);
            _inputWidth = CreateInput();
            grid.Controls.Add(_inputWidth, 1, 2);

            AddLabel(grid, "高度:", 2, 2, Color.Gray);
            _inputHeight = CreateInput();
            grid.Controls.Add(_inputHeight, 3, 2);
            
            // Events
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
                Anchor = AnchorStyles.Right | AnchorStyles.Left | AnchorStyles.Top, // Vertically center? Top is fine
                Location = new Point(0, 8), // Offset for alignment
                ForeColor = color ?? Color.Black
            };
             // Center vertically in cell
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
