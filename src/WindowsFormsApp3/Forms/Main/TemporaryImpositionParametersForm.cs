using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Forms.Main
{
    /// <summary>
    /// 编辑材料选择框本次使用的临时排版参数，不修改全局设置。
    /// </summary>
    public class TemporaryImpositionParametersForm : Form
    {
        private readonly bool _isFlatSheet;
        private readonly Input _paperWidthInput = CreateInput();
        private readonly Input _paperHeightInput = CreateInput();
        private readonly Input _fixedWidthInput = CreateInput();
        private readonly Input _minLengthInput = CreateInput();
        private readonly Input _marginTopInput = CreateInput();
        private readonly Input _marginBottomInput = CreateInput();
        private readonly Input _marginLeftInput = CreateInput();
        private readonly Input _marginRightInput = CreateInput();
        private readonly Checkbox _unlockPaperSizeCheckbox = new Checkbox { Text = "解锁纸张尺寸" };
        private readonly Checkbox _unlockRollSizeCheckbox = new Checkbox { Text = "解锁幅宽尺寸" };
        private readonly System.Windows.Forms.Panel _contentPanel = new System.Windows.Forms.Panel();

        public TemporaryImpositionParameters Parameters { get; private set; }

        public TemporaryImpositionParametersForm(bool isFlatSheet, TemporaryImpositionParameters parameters)
        {
            _isFlatSheet = isFlatSheet;
            Parameters = (parameters ?? throw new ArgumentNullException(nameof(parameters))).Clone();

            Text = "大版开料参数配置";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(500, 300);

            BuildLayout();
            LoadParameters();
        }

        private static Input CreateInput()
        {
            return new Input { Size = new Size(110, 32) };
        }

        private void BuildLayout()
        {
            var title = new AntdUI.Label
            {
                Text = "本窗口仅影响当前材料选择，保存预设后可复用。",
                Location = new Point(20, 15),
                Size = new Size(450, 28)
            };
            Controls.Add(title);

            _contentPanel.Location = new Point(20, 48);
            _contentPanel.Size = new Size(460, 180);

            if (_isFlatSheet)
            {
                _contentPanel.Controls.Add(new AntdUI.Label
                {
                    Text = "平张纸张尺寸（mm）",
                    Location = new Point(0, 5),
                    Size = new Size(150, 28)
                });
                _unlockPaperSizeCheckbox.Location = new Point(180, 5);
                _unlockPaperSizeCheckbox.Size = new Size(140, 28);
                _unlockPaperSizeCheckbox.CheckedChanged += (s, e) => UpdatePaperSizeEnabled();
                _contentPanel.Controls.Add(_unlockPaperSizeCheckbox);

                AddField(_contentPanel, "纸张宽", _paperWidthInput, 0, 42);
                AddField(_contentPanel, "纸张高", _paperHeightInput, 225, 42);
                AddMargins(_contentPanel, 84);
            }
            else
            {
                _contentPanel.Controls.Add(new AntdUI.Label
                {
                    Text = "卷装材料规格（mm）",
                    Location = new Point(0, 5),
                    Size = new Size(150, 28)
                });
                _unlockRollSizeCheckbox.Location = new Point(180, 5);
                _unlockRollSizeCheckbox.Size = new Size(140, 28);
                _unlockRollSizeCheckbox.CheckedChanged += (s, e) => UpdateRollSizeEnabled();
                _contentPanel.Controls.Add(_unlockRollSizeCheckbox);

                AddField(_contentPanel, "固定宽度", _fixedWidthInput, 0, 42);
                AddField(_contentPanel, "最小长度", _minLengthInput, 225, 42);
                AddMargins(_contentPanel, 84);
            }

            Controls.Add(_contentPanel);

            var applyButton = new AntdUI.Button
            {
                Text = "应用",
                Location = new Point(300, 245),
                Size = new Size(80, 32),
                Type = TTypeMini.Primary
            };
            applyButton.Click += ApplyButton_Click;

            var cancelButton = new AntdUI.Button
            {
                Text = "取消",
                Location = new Point(395, 245),
                Size = new Size(80, 32)
            };
            cancelButton.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.Add(applyButton);
            Controls.Add(cancelButton);
        }

        private void AddMargins(Control container, int top)
        {
            AddField(container, "上边距", _marginTopInput, 0, top);
            AddField(container, "下边距", _marginBottomInput, 225, top);
            AddField(container, "左边距", _marginLeftInput, 0, top + 42);
            AddField(container, "右边距", _marginRightInput, 225, top + 42);
        }

        private static void AddField(Control container, string labelText, Control input, int left, int top)
        {
            container.Controls.Add(new AntdUI.Label
            {
                Text = labelText,
                Location = new Point(left, top + 3),
                Size = new Size(70, 28)
            });
            input.Location = new Point(left + 75, top);
            container.Controls.Add(input);
        }

        private void LoadParameters()
        {
            if (_isFlatSheet)
            {
                _unlockPaperSizeCheckbox.Checked = Parameters.IsPaperSizeUnlocked;
                _paperWidthInput.Text = Format(Parameters.PaperWidth);
                _paperHeightInput.Text = Format(Parameters.PaperHeight);
                UpdatePaperSizeEnabled();
            }
            else
            {
                _unlockRollSizeCheckbox.Checked = Parameters.IsRollSizeUnlocked;
                _fixedWidthInput.Text = Format(Parameters.FixedWidth);
                _minLengthInput.Text = Format(Parameters.MinLength);
                UpdateRollSizeEnabled();
            }

            _marginTopInput.Text = Format(Parameters.MarginTop);
            _marginBottomInput.Text = Format(Parameters.MarginBottom);
            _marginLeftInput.Text = Format(Parameters.MarginLeft);
            _marginRightInput.Text = Format(Parameters.MarginRight);
        }

        private void UpdatePaperSizeEnabled()
        {
            _paperWidthInput.Enabled = _isFlatSheet && _unlockPaperSizeCheckbox.Checked;
            _paperHeightInput.Enabled = _isFlatSheet && _unlockPaperSizeCheckbox.Checked;
        }

        private void UpdateRollSizeEnabled()
        {
            _fixedWidthInput.Enabled = !_isFlatSheet && _unlockRollSizeCheckbox.Checked;
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            if (!TryReadFloat(_marginTopInput, "上边距", out var marginTop, false) ||
                !TryReadFloat(_marginBottomInput, "下边距", out var marginBottom, false) ||
                !TryReadFloat(_marginLeftInput, "左边距", out var marginLeft, false) ||
                !TryReadFloat(_marginRightInput, "右边距", out var marginRight, false))
            {
                return;
            }

            var updated = Parameters.Clone();
            updated.MarginTop = marginTop;
            updated.MarginBottom = marginBottom;
            updated.MarginLeft = marginLeft;
            updated.MarginRight = marginRight;

            if (_isFlatSheet)
            {
                updated.IsPaperSizeUnlocked = _unlockPaperSizeCheckbox.Checked;
                if (!TryReadFloat(_paperWidthInput, "纸张宽度", out var width, true) || !TryReadFloat(_paperHeightInput, "纸张高度", out var height, true)) return;
                if (width <= marginLeft + marginRight || height <= marginTop + marginBottom)
                {
                    MessageBox.Show("纸张可印刷区域必须大于 0。", "参数无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                updated.PaperWidth = width;
                updated.PaperHeight = height;
            }
            else
            {
                updated.IsRollSizeUnlocked = _unlockRollSizeCheckbox.Checked;
                if (!TryReadFloat(_fixedWidthInput, "固定宽度", out var fixedWidth, true)) return;
                if (!TryReadFloat(_minLengthInput, "最小长度", out var minLength, true)) return;
                if (fixedWidth <= marginLeft + marginRight)
                {
                    MessageBox.Show("固定宽度扣除左右边距后必须大于 0。", "参数无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                updated.FixedWidth = fixedWidth;
                updated.MinLength = minLength;
            }

            Parameters = updated;
            DialogResult = DialogResult.OK;
        }

        private bool TryReadFloat(Input input, string fieldName, out float value, bool mustBePositive)
        {
            if (!float.TryParse(input.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) || (mustBePositive ? value <= 0 : value < 0))
            {
                MessageBox.Show($"{fieldName}请输入有效的{(mustBePositive ? "正数" : "非负数")}。", "参数无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                input.Focus();
                return false;
            }
            return true;
        }

        private static string Format(float value) => value.ToString("0.##", CultureInfo.CurrentCulture);
    }
}
