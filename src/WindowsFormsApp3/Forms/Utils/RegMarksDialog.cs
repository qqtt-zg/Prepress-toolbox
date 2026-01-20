using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Utils
{
    /// <summary>
    /// 套准标记配置对话框
    /// </summary>
    public class RegMarksDialog : Form
    {
        private AntdUI.InputNumber numOuterDiameter;
        private AntdUI.InputNumber numInnerDiameter;
        private AntdUI.InputNumber numCrossLength;
        private AntdUI.InputNumber numOffset;
        private AntdUI.Radio radioAll;
        private AntdUI.Radio radioCurrent;
        private AntdUI.Radio radioCustom;
        private AntdUI.Input txtPageRange;
        private AntdUI.Button btnOK;
        private AntdUI.Button btnCancel;
        private Label lblTitle;

        /// <summary>
        /// 配置参数
        /// </summary>
        public RegMarksOptions Options { get; private set; }

        public RegMarksDialog()
        {
            InitializeComponent();
            Options = new RegMarksOptions(); // 默认值
        }

        private void InitializeComponent()
        {
            // 窗体基本设置
            this.Text = "套准标记设置";
            this.Size = new Size(450, 540);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Microsoft YaHei UI", 9F);

            int leftMargin = 30;
            int topMargin = 20;
            int labelWidth = 100;
            int controlWidth = 280;
            int rowHeight = 50;
            int currentY = topMargin;

            // 标题
            lblTitle = new Label
            {
                Text = "套准标记参数设置",
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
                Location = new Point(leftMargin, currentY),
                Size = new Size(380, 30),
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            this.Controls.Add(lblTitle);
            currentY += 40;

            // 外圆直径
            var lblOuterDiameter = new Label
            {
                Text = "外圆直径:",
                Location = new Point(leftMargin, currentY + 8),
                Size = new Size(labelWidth, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            this.Controls.Add(lblOuterDiameter);

            numOuterDiameter = new AntdUI.InputNumber
            {
                Location = new Point(leftMargin + labelWidth, currentY),
                Size = new Size(150, 36),
                Value = 6M,
                Minimum = 3M,
                Maximum = 15M,
                DecimalPlaces = 1
            };
            this.Controls.Add(numOuterDiameter);

            var lblOuterUnit = new Label
            {
                Text = "mm",
                Location = new Point(leftMargin + labelWidth + 160, currentY + 8),
                Size = new Size(40, 24),
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            this.Controls.Add(lblOuterUnit);
            currentY += rowHeight;

            // 内圆直径
            var lblInnerDiameter = new Label
            {
                Text = "内圆直径:",
                Location = new Point(leftMargin, currentY + 8),
                Size = new Size(labelWidth, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            this.Controls.Add(lblInnerDiameter);

            numInnerDiameter = new AntdUI.InputNumber
            {
                Location = new Point(leftMargin + labelWidth, currentY),
                Size = new Size(150, 36),
                Value = 4M,
                Minimum = 2M,
                Maximum = 10M,
                DecimalPlaces = 1
            };
            this.Controls.Add(numInnerDiameter);

            var lblInnerUnit = new Label
            {
                Text = "mm",
                Location = new Point(leftMargin + labelWidth + 160, currentY + 8),
                Size = new Size(40, 24),
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            this.Controls.Add(lblInnerUnit);
            currentY += rowHeight;

            // 十字线长度
            var lblCrossLength = new Label
            {
                Text = "十字线长度:",
                Location = new Point(leftMargin, currentY + 8),
                Size = new Size(labelWidth, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            this.Controls.Add(lblCrossLength);

            numCrossLength = new AntdUI.InputNumber
            {
                Location = new Point(leftMargin + labelWidth, currentY),
                Size = new Size(150, 36),
                Value = 8M,
                Minimum = 4M,
                Maximum = 20M,
                DecimalPlaces = 1
            };
            this.Controls.Add(numCrossLength);

            var lblCrossUnit = new Label
            {
                Text = "mm",
                Location = new Point(leftMargin + labelWidth + 160, currentY + 8),
                Size = new Size(40, 24),
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            this.Controls.Add(lblCrossUnit);
            currentY += rowHeight;

            // 偏移距离
            var lblOffset = new Label
            {
                Text = "偏移距离:",
                Location = new Point(leftMargin, currentY + 8),
                Size = new Size(labelWidth, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            this.Controls.Add(lblOffset);

            numOffset = new AntdUI.InputNumber
            {
                Location = new Point(leftMargin + labelWidth, currentY),
                Size = new Size(150, 36),
                Value = 10M,
                Minimum = 5M,
                Maximum = 30M,
                DecimalPlaces = 1
            };
            this.Controls.Add(numOffset);

            var lblOffsetUnit = new Label
            {
                Text = "mm",
                Location = new Point(leftMargin + labelWidth + 160, currentY + 8),
                Size = new Size(40, 24),
                ForeColor = Color.FromArgb(120, 120, 120)
            };
            this.Controls.Add(lblOffsetUnit);
            currentY += rowHeight + 10;

            // 分隔线
            var separator = new Panel
            {
                Location = new Point(leftMargin, currentY),
                Size = new Size(controlWidth + 90, 1),
                BackColor = Color.FromArgb(230, 230, 230)
            };
            this.Controls.Add(separator);
            currentY += 15;

            // 应用范围标题
            var lblRangeTitle = new Label
            {
                Text = "应用页面范围:",
                Location = new Point(leftMargin, currentY),
                Size = new Size(200, 24),
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            this.Controls.Add(lblRangeTitle);
            currentY += 30;

            // 全部页面
            radioAll = new AntdUI.Radio
            {
                Text = "全部页面",
                Location = new Point(leftMargin, currentY),
                Size = new Size(120, 30),
                Checked = true
            };
            radioAll.CheckedChanged += RadioButtons_CheckedChanged;
            this.Controls.Add(radioAll);
            currentY += 35;

            // 当前页
            radioCurrent = new AntdUI.Radio
            {
                Text = "当前页面",
                Location = new Point(leftMargin, currentY),
                Size = new Size(120, 30)
            };
            radioCurrent.CheckedChanged += RadioButtons_CheckedChanged;
            this.Controls.Add(radioCurrent);
            currentY += 35;

            // 自定义范围
            radioCustom = new AntdUI.Radio
            {
                Text = "指定页面:",
                Location = new Point(leftMargin, currentY),
                Size = new Size(100, 30)
            };
            radioCustom.CheckedChanged += RadioButtons_CheckedChanged;
            this.Controls.Add(radioCustom);

            txtPageRange = new AntdUI.Input
            {
                Location = new Point(leftMargin + 100, currentY - 3),
                Size = new Size(280, 36),
                PlaceholderText = "例如: 1,3,5-7,10",
                Enabled = false
            };
            this.Controls.Add(txtPageRange);
            currentY += 55;

            // 按钮区域
            btnCancel = new AntdUI.Button
            {
                Text = "取消",
                Location = new Point(this.ClientSize.Width - 190, currentY),
                Size = new Size(80, 36)
            };
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);

            btnOK = new AntdUI.Button
            {
                Text = "应用",
                Type = AntdUI.TTypeMini.Primary,
                Location = new Point(this.ClientSize.Width - 100, currentY),
                Size = new Size(80, 36)
            };
            btnOK.Click += BtnOK_Click;
            this.Controls.Add(btnOK);
        }

        private void RadioButtons_CheckedChanged(object sender, EventArgs e)
        {
            // 只有自定义范围时启用文本框
            txtPageRange.Enabled = radioCustom.Checked;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 验证输入
            if (radioCustom.Checked && string.IsNullOrWhiteSpace(txtPageRange.Text))
            {
                AntdUI.Message.error(this, "请输入页面范围", autoClose: 3);
                return;
            }

            // 验证内圆必须小于外圆
            if (numInnerDiameter.Value >= numOuterDiameter.Value)
            {
                AntdUI.Message.error(this, "内圆直径必须小于外圆直径", autoClose: 3);
                return;
            }

            // 构建配置对象
            Options = new RegMarksOptions
            {
                OuterDiameterMM = (double)numOuterDiameter.Value,
                InnerDiameterMM = (double)numInnerDiameter.Value,
                CrossLengthMM = (double)numCrossLength.Value,
                OffsetMM = (double)numOffset.Value,
                PageRangeType = radioAll.Checked ? PageRangeType.All :
                               radioCurrent.Checked ? PageRangeType.Current :
                               PageRangeType.Custom,
                PageRange = txtPageRange.Text?.Trim() ?? ""
            };

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        /// <summary>
        /// 应用主题
        /// </summary>
        public void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                this.BackColor = Color.FromArgb(30, 30, 30);
                lblTitle.ForeColor = Color.FromArgb(220, 220, 220);

                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl is Label lbl && lbl != lblTitle)
                    {
                        lbl.ForeColor = Color.FromArgb(180, 180, 180);
                    }
                }
            }
            else
            {
                this.BackColor = Color.White;
                lblTitle.ForeColor = Color.FromArgb(50, 50, 50);

                foreach (Control ctrl in this.Controls)
                {
                    if (ctrl is Label lbl && lbl != lblTitle)
                    {
                        lbl.ForeColor = Color.FromArgb(80, 80, 80);
                    }
                }
            }
        }
    }
}
