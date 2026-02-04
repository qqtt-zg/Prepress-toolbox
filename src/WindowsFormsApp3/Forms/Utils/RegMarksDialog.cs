using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Utils
{
    /// <summary>
    /// 套准标记配置对话框
    /// </summary>
    public partial class RegMarksDialog : Form
    {
        /// <summary>
        /// 配置参数
        /// </summary>
        public RegMarksOptions Options { get; private set; }

        public RegMarksDialog()
        {
            InitializeComponent();
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                Options = new RegMarksOptions(); // 默认值
            }
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
