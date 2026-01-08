using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    public partial class SettingsImpositionControl : UserControl
    {
        private readonly WindowsFormsApp3.Interfaces.IPdfDimensionService _pdfDimensionService;

        public SettingsImpositionControl()
        {
            InitializeComponent();
            
            // 仅在运行时加载设置，避免设计器问题
            if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
            {
                _pdfDimensionService = WindowsFormsApp3.Interfaces.PdfDimensionServiceFactory.GetInstance();
                LoadSettings();
            }
        }

        private void LoadSettings()
        {
            // Enable
            var enabled = AppSettings.Get("Imposition_Enabled");
            chkEnable.Checked = enabled != null && Convert.ToBoolean(enabled);

            // Flat Sheet
            txtPaperWidth.Text = AppSettings.Get("Imposition_PaperWidth") as string;
            txtPaperHeight.Text = AppSettings.Get("Imposition_PaperHeight") as string;
            txtMarginTop.Text = AppSettings.Get("Imposition_MarginTop") as string;
            txtMarginBottom.Text = AppSettings.Get("Imposition_MarginBottom") as string;
            txtMarginLeft.Text = AppSettings.Get("Imposition_MarginLeft") as string;
            txtMarginRight.Text = AppSettings.Get("Imposition_MarginRight") as string;

            // Roll
            txtFixedWidth.Text = AppSettings.Get("Imposition_FixedWidth") as string;
            txtMinLength.Text = AppSettings.Get("Imposition_MinLength") as string;
            txtRollMarginTop.Text = AppSettings.Get("Imposition_RollMarginTop") as string;
            txtRollMarginBottom.Text = AppSettings.Get("Imposition_RollMarginBottom") as string;
            txtRollMarginLeft.Text = AppSettings.Get("Imposition_RollMarginLeft") as string;
            txtRollMarginRight.Text = AppSettings.Get("Imposition_RollMarginRight") as string;

            // Common
            txtRows.Text = AppSettings.Get("Imposition_Rows") as string;
            txtCols.Text = AppSettings.Get("Imposition_Columns") as string;
        }

        private void BtnGetPdfSize_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf";
                openFileDialog.Title = "选择PDF文件";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (_pdfDimensionService.GetFirstPageSize(openFileDialog.FileName, out double width, out double height))
                        {
                            txtPaperWidth.Text = width.ToString("F2");
                            txtPaperHeight.Text = height.ToString("F2");
                            MessageBox.Show($"已获取尺寸: {width:F2} x {height:F2} mm", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                             MessageBox.Show("无法获取PDF尺寸，请检查文件是否损坏。", "失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                         MessageBox.Show("错误: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        public void SaveSettings()
        {
             AppSettings.Set("Imposition_Enabled", chkEnable.Checked);
             
             // Flat
             if(!string.IsNullOrEmpty(txtPaperWidth.Text)) AppSettings.Set("Imposition_PaperWidth", txtPaperWidth.Text.Trim());
             if(!string.IsNullOrEmpty(txtPaperHeight.Text)) AppSettings.Set("Imposition_PaperHeight", txtPaperHeight.Text.Trim());
             if(!string.IsNullOrEmpty(txtMarginTop.Text)) AppSettings.Set("Imposition_MarginTop", txtMarginTop.Text.Trim());
             if(!string.IsNullOrEmpty(txtMarginBottom.Text)) AppSettings.Set("Imposition_MarginBottom", txtMarginBottom.Text.Trim());
             if(!string.IsNullOrEmpty(txtMarginLeft.Text)) AppSettings.Set("Imposition_MarginLeft", txtMarginLeft.Text.Trim());
             if(!string.IsNullOrEmpty(txtMarginRight.Text)) AppSettings.Set("Imposition_MarginRight", txtMarginRight.Text.Trim());

             // Roll
             if(!string.IsNullOrEmpty(txtFixedWidth.Text)) AppSettings.Set("Imposition_FixedWidth", txtFixedWidth.Text.Trim());
             if(!string.IsNullOrEmpty(txtMinLength.Text)) AppSettings.Set("Imposition_MinLength", txtMinLength.Text.Trim());
             if(!string.IsNullOrEmpty(txtRollMarginTop.Text)) AppSettings.Set("Imposition_RollMarginTop", txtRollMarginTop.Text.Trim());
             if(!string.IsNullOrEmpty(txtRollMarginBottom.Text)) AppSettings.Set("Imposition_RollMarginBottom", txtRollMarginBottom.Text.Trim());
             if(!string.IsNullOrEmpty(txtRollMarginLeft.Text)) AppSettings.Set("Imposition_RollMarginLeft", txtRollMarginLeft.Text.Trim());
             if(!string.IsNullOrEmpty(txtRollMarginRight.Text)) AppSettings.Set("Imposition_RollMarginRight", txtRollMarginRight.Text.Trim());

             // Common
             if(!string.IsNullOrEmpty(txtRows.Text)) AppSettings.Set("Imposition_Rows", txtRows.Text.Trim());
             if(!string.IsNullOrEmpty(txtCols.Text)) AppSettings.Set("Imposition_Columns", txtCols.Text.Trim());

             AppSettings.Save();
        }
    }
}
