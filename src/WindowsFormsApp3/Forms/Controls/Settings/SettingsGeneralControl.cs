using System;
using System.Collections.Generic;
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
            LoadSettings();
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

            LoadTextItems();
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
            
            // chkHideRadius moved to MaterialControl
            
            SaveTextItems();
            
            AppSettings.Save();
            
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }

        // Logic for Text Items
        private Control selectedRow = null;
        private readonly string[] allTextItems = { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号", "列组合" };

        private void LoadTextItems()
        {
            pnlTextItems.Controls.Clear();
            
            // 1. Load from AppSettings
            string savedItems = AppSettings.Get("TextItems") as string;
            var loadDict = new Dictionary<string, bool>();
            var displayOrder = new List<string>();

            if (!string.IsNullOrEmpty(savedItems))
            {
                string[] parts = savedItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length % 2 == 0)
                {
                    for (int i = 0; i < parts.Length; i += 2)
                    {
                        string text = parts[i];
                        if (Array.IndexOf(allTextItems, text) > -1)
                        {
                            bool isChecked = bool.Parse(parts[i+1]);
                            loadDict[text] = isChecked;
                            displayOrder.Add(text);
                        }
                    }
                }
            }

            // 2. Add missing items (if any, e.g. first run or new items)
            foreach (var item in allTextItems)
            {
                if (!loadDict.ContainsKey(item))
                {
                    loadDict[item] = true;
                    displayOrder.Add(item);
                }
            }

            // 3. Create UI rows
            foreach (var text in displayOrder)
            {
                CreateRow(text, loadDict[text]);
            }
            
            // 4. Update Preview
            UpdateComboPreview();
            
            // 5. Connect events
            btnMoveUp.Click += BtnMoveUp_Click;
            btnMoveDown.Click += BtnMoveDown_Click;
        }

        private void CreateRow(string text, bool isChecked)
        {
            var row = new Panel
            {
                Size = new Size(pnlTextItems.Width - 25, 30),
                Margin = new Padding(0, 0, 0, 2),
                BackColor = Color.White
            };

            var chk = new AntdUI.Checkbox
            {
                Text = text,
                Checked = isChecked,
                Location = new Point(5, 5),
                Size = new Size(100, 20),
                AutoCheck = true
            };
            
            // Event to update preview when checked changes
            chk.CheckedChanged += (s, e) => UpdateComboPreview();

            // Click event for selection
            EventHandler selectHandler = (s, e) => SelectRow(row);
            row.Click += selectHandler;
            chk.Click += selectHandler; // AntdUI Checkbox might swallow click, so better bind to it too if possible
            // Note: AntdUI Checkbox might need separate handling if it doesn't propagate click. 
            // We can also add a transparent label overlay or handle MouseDown.
            
            row.Controls.Add(chk);
            pnlTextItems.Controls.Add(row);
        }

        private void SelectRow(Control row)
        {
            if (selectedRow != null)
            {
                selectedRow.BackColor = Color.White;
            }
            selectedRow = row;
            selectedRow.BackColor = Color.FromArgb(230, 247, 255); // Light blue selection
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (selectedRow == null) return;
            int index = pnlTextItems.Controls.IndexOf(selectedRow);
            if (index > 0)
            {
                pnlTextItems.Controls.SetChildIndex(selectedRow, index - 1);
                UpdateComboPreview();
            }
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (selectedRow == null) return;
            int index = pnlTextItems.Controls.IndexOf(selectedRow);
            if (index < pnlTextItems.Controls.Count - 1)
            {
                pnlTextItems.Controls.SetChildIndex(selectedRow, index + 1);
                UpdateComboPreview();
            }
        }

        private void UpdateComboPreview()
        {
            // Build the combo string based on current order and checked state
            var previewParts = new List<string>();
            foreach (Control row in pnlTextItems.Controls)
            {
                if (row is Panel p && p.Controls.Count > 0 && p.Controls[0] is AntdUI.Checkbox chk)
                {
                    if (chk.Checked)
                    {
                        previewParts.Add(chk.Text);
                    }
                }
            }
            
            // Combine with separator (using current UI value if possible, else default)
            string sep = txtSeparator.Text;
            txtComboPreview.Text = string.Join(sep, previewParts);
        }

        private void SaveTextItems()
        {
            var sb = new System.Text.StringBuilder();
            foreach (Control row in pnlTextItems.Controls)
            {
                 if (row is Panel p && p.Controls.Count > 0 && p.Controls[0] is AntdUI.Checkbox chk)
                 {
                     sb.Append($"{chk.Text}|{chk.Checked}|");
                 }
            }
            // Update AppSettings local cache or direct set? 
            // AppSettings.Set uses a dictionary usually. 
            // We must ensure this format matches what LoadTextItems expects.
            if (sb.Length > 0) sb.Length--; // Remove last pipe if logic differs, but here we append pair with pipe.
            // Actually the format is Item|Bool|Item|Bool... so trailing pipe might be issue if split doesn't handle empty.
            // My split uses RemoveEmptyEntries so trailing pipe is fine or removed.
            
            AppSettings.Set("TextItems", sb.ToString().TrimEnd('|'));
        }
    }
}
