using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    public partial class SettingsRegexControl : UserControl
    {
        private Dictionary<string, string> RegexPatterns = new Dictionary<string, string>();

        public SettingsRegexControl()
        {
            InitializeComponent();
            LoadRegexPatterns();
        }

        private void LoadRegexPatterns()
        {
            RegexPatterns.Clear();
            if (!string.IsNullOrEmpty(AppSettings.RegexPatterns))
            {
                string[] patterns = AppSettings.RegexPatterns.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string pattern in patterns)
                {
                    string[] parts = pattern.Split(new[] { '=' }, 2);
                    if (parts.Length == 2 && !RegexPatterns.ContainsKey(parts[0]))
                    {
                        RegexPatterns.Add(parts[0], parts[1]);
                    }
                }
            }
            BindData();
        }

        private void BindData()
        {
            var bindingList = new BindingList<KeyValuePair<string, string>>(RegexPatterns.ToList());
            dgvRegex.DataSource = bindingList;
            if (dgvRegex.Columns.Count > 0)
            {
                dgvRegex.Columns["Key"].HeaderText = "名称";
                dgvRegex.Columns["Value"].HeaderText = "正则表达式";
                dgvRegex.Columns["Value"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void SaveRegexSettings()
        {
            AppSettings.RegexPatterns = string.Join("|", RegexPatterns.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            AppSettings.Save();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var name = txtName.Text.Trim();
            var pattern = txtPattern.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(pattern))
            {
                MessageBox.Show("请输入正则名称和表达式");
                return;
            }

            if (RegexPatterns.ContainsKey(name))
            {
                MessageBox.Show("该正则名称已存在");
                return;
            }

            RegexPatterns.Add(name, pattern);
            SaveRegexSettings();
            BindData();
            txtName.Text = "";
            txtPattern.Text = "";
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvRegex.SelectedRows.Count == 0)
            {
                MessageBox.Show("请选择要删除的正则表达式");
                return;
            }

            var selected = (KeyValuePair<string, string>)dgvRegex.SelectedRows[0].DataBoundItem;
            RegexPatterns.Remove(selected.Key);
            SaveRegexSettings();
            BindData();
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
             try
            {
                string pattern = txtPattern.Text;
                // If text box is empty, try using selected row
                if (string.IsNullOrEmpty(pattern) && dgvRegex.SelectedRows.Count > 0)
                {
                    var selected = (KeyValuePair<string, string>)dgvRegex.SelectedRows[0].DataBoundItem;
                    pattern = selected.Value;
                }

                if (string.IsNullOrEmpty(pattern))
                {
                    txtTestResult.Text = "请先选择或输入正则表达式";
                    return;
                }

                string input = txtTestInput.Text;
                if (string.IsNullOrEmpty(input))
                {
                    txtTestResult.Text = "请输入测试文本";
                    return;
                }

                var match = System.Text.RegularExpressions.Regex.Match(input, pattern);
                if (match.Success)
                {
                    string result = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    txtTestResult.Text = $"匹配成功: {result}";
                }
                else
                {
                    txtTestResult.Text = "未找到匹配项";
                }
            }
            catch (Exception ex)
            {
                txtTestResult.Text = $"正则表达式错误: {ex.Message}";
            }
        }

        private void DgvRegex_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvRegex.SelectedRows.Count > 0)
            {
                var selected = (KeyValuePair<string, string>)dgvRegex.SelectedRows[0].DataBoundItem;
                txtName.Text = selected.Key;
                txtPattern.Text = selected.Value;
            }
        }
    }
}
