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
        private const string RenameNotificationKey = "ShowRenameCompleteNotification";
        private const string AutoSaveIntervalSecondsKey = "AutoSaveIntervalSeconds";
        private const string EnableDailyJsonKey = "EnableDailyJson";
        private const string AlwaysOutputBothLayoutCountsKey = "AlwaysOutputBothLayoutCounts";

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

            // Rename Notification
            chkRenameNotification.Checked = AppSettings.ShowRenameCompleteNotification;

            // 自动保存频率（秒）
            object autoSaveSeconds = AppSettings.Get(AutoSaveIntervalSecondsKey);
            if (autoSaveSeconds is int seconds)
            {
                numAutoSaveSeconds.Value = Math.Max(numAutoSaveSeconds.Minimum, Math.Min(numAutoSaveSeconds.Maximum, seconds));
            }
            else
            {
                numAutoSaveSeconds.Value = 60;
            }

            // 当日JSON自动创建/加载开关
            object enableDailyJson = AppSettings.Get(EnableDailyJsonKey);
            chkEnableDailyJson.Checked = enableDailyJson is bool b ? b : true;

            // 同时输出排版模式布局数开关
            object alwaysOutputBothLayoutCounts = AppSettings.Get(AlwaysOutputBothLayoutCountsKey);
            chkAlwaysOutputBothLayoutCounts.Checked = alwaysOutputBothLayoutCounts is bool b2 ? b2 : false;

            // 开关变化时实时启用/禁用自动保存相关控件
            chkEnableDailyJson.CheckedChanged += (s, e) => UpdateAutoSaveControlsEnabledState();

            UpdateAutoSaveControlsEnabledState();
        }

        public void SaveSettings()
        {
            AppSettings.Set(SeparatorKey, txtSeparator.Text);
            AppSettings.Set(UnitKey, txtUnit.Text);
            AppSettings.Set(OpacityKey, sliderOpacity.Value / 100.0);
            
            if (!string.IsNullOrEmpty(txtHotkey.Text))
            {
                AppSettings.Set(HotkeyKey, txtHotkey.Text.Trim());
            }

            AppSettings.ShowRenameCompleteNotification = chkRenameNotification.Checked;

            // 当关闭“当日JSON自动创建/加载”时，定时自动保存也应当禁用
            if (chkEnableDailyJson.Checked)
            {
                // 自动保存频率（秒）
                AppSettings.Set(AutoSaveIntervalSecondsKey, (int)numAutoSaveSeconds.Value);
            }

            // 当日JSON自动创建/加载开关
            AppSettings.Set(EnableDailyJsonKey, chkEnableDailyJson.Checked);

            // 同时输出排版模式布局数开关
            AppSettings.Set(AlwaysOutputBothLayoutCountsKey, chkAlwaysOutputBothLayoutCounts.Checked);

            AppSettings.Save();
            
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateAutoSaveControlsEnabledState()
        {
            bool enabled = chkEnableDailyJson.Checked;
            numAutoSaveSeconds.Enabled = enabled;
            lblAutoSaveSeconds.Enabled = enabled;
        }
    }
}
