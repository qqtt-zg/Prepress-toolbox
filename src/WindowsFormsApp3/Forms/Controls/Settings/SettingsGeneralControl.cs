using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    public partial class SettingsGeneralControl : UserControl
    {
        public event EventHandler SettingsSaved;

        private const string SeparatorKey = "Separator";
        private const string UnitKey = "Unit";
        private const string OpacityKey = "Opacity";
        private const string HideRadiusKey = "HideRadiusValue";
        private const string HotkeyKey = "ToggleMinimizeHotkey";
        
        public SettingsGeneralControl()
        {
            InitializeComponent();
            
            // 仅在运行时加载设置，避免设计器问题
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                LoadSettings();
            }
        }

        private void LoadSettings()
        {
            // Separator
            object sep = AppSettings.Get(SeparatorKey);
            txtSeparator.Text = sep != null ? sep.ToString() : "_";

            // Unit
            object unit = AppSettings.Get(UnitKey);
            txtUnit.Text = unit != null ? unit.ToString() : "mm";

            // Opacity
            object opacity = AppSettings.Get(OpacityKey);
            if (opacity is double val)
            {
                sliderOpacity.Value = (int)(val * 100);
            }
            else
            {
                sliderOpacity.Value = 100;
            }

            // Hotkey
            object hotkey = AppSettings.Get(HotkeyKey);
            txtHotkey.Text = hotkey != null ? hotkey.ToString() : "";
        }

        public void SaveSettings()
        {
            AppSettings.Set(SeparatorKey, txtSeparator.Text);
            AppSettings.Set(UnitKey, txtUnit.Text);
            AppSettings.Set(OpacityKey, sliderOpacity.Value / 100.0);
            
            if(!string.IsNullOrEmpty(txtHotkey.Text))
            {
                AppSettings.Set(HotkeyKey, txtHotkey.Text.Trim());
            }
            
            AppSettings.Save();
            
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }
    }
}
