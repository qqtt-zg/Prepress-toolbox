using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    public partial class SettingsMaterialControl : UserControl
    {
        private const string HideRadiusKey = "HideRadiusValue";

        public SettingsMaterialControl()
        {
            InitializeComponent();
            
            // 仅在运行时加载设置，避免设计器问题
            if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
            {
                LoadSettings();
            }
        }

        private void LoadSettings()
        {
            txtMaterial.Text = AppSettings.Material ?? "";
            txtBleed.Text = AppSettings.TetBleedValues ?? "3,5,10";

            txtShapeZero.Text = GetShapeCode("ZeroShapeCode", "Z");
            txtShapeRound.Text = GetShapeCode("RoundShapeCode", "R");
            txtShapeEllipse.Text = GetShapeCode("EllipseShapeCode", "Y");
            txtShapeCircle.Text = GetShapeCode("CircleShapeCode", "C");
            
            object hideRadius = AppSettings.Get(HideRadiusKey);
            chkHideRadius.Checked = hideRadius != null && Convert.ToBoolean(hideRadius);
        }

        private string GetShapeCode(string key, string defaultValue)
        {
             var val = AppSettings.Get(key) as string;
             return string.IsNullOrEmpty(val) ? defaultValue : val;
        }

        public void SaveSettings()
        {
            // Material
            if (!string.IsNullOrEmpty(txtMaterial.Text))
            {
                AppSettings.Material = txtMaterial.Text.Trim();
            }

            // Bleed
            if (!string.IsNullOrEmpty(txtBleed.Text))
            {
                AppSettings.TetBleedValues = txtBleed.Text.Trim();
            }

            // Shape Codes
            AppSettings.Set("ZeroShapeCode", txtShapeZero.Text.Trim());
            AppSettings.Set("RoundShapeCode", txtShapeRound.Text.Trim());
            AppSettings.Set("EllipseShapeCode", txtShapeEllipse.Text.Trim());
            AppSettings.Set("CircleShapeCode", txtShapeCircle.Text.Trim());

            // Hide Radius
            AppSettings.Set(HideRadiusKey, chkHideRadius.Checked);

            AppSettings.Save();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("材料设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
