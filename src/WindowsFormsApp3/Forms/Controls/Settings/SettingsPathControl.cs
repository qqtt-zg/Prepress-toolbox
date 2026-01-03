using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;
using Ookii.Dialogs.WinForms; // Assuming Ookii is available
using Newtonsoft.Json;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    public partial class SettingsPathControl : UserControl
    {
        public SettingsPathControl()
        {
            InitializeComponent();
            InitializeExportPathsDataGridView();
            
            // 仅在运行时加载数据，避免设计器中访问未初始化的AppSettings
            if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
            {
                LoadExportPaths();
            }
        }

        public class ExportPathItem
        {
            public string FullPath { get; set; }
            public string FolderName => System.IO.Path.GetFileName(FullPath);
            public bool IncludeSubFolders { get; set; } = true;
        }

        private void InitializeExportPathsDataGridView()
        {
             dgvExportPaths.AutoGenerateColumns = false;
             dgvExportPaths.Columns.Clear();

            // FolderName Column
            var nameCol = new DataGridViewTextBoxColumn();
            nameCol.DataPropertyName = "FolderName";
            nameCol.HeaderText = "文件夹名称";
            nameCol.Name = "FolderName";
            nameCol.Width = 150;
            nameCol.ReadOnly = true;
            dgvExportPaths.Columns.Add(nameCol);

            // FullPath Column
            var pathCol = new DataGridViewTextBoxColumn();
            pathCol.DataPropertyName = "FullPath";
            pathCol.HeaderText = "完整路径";
            pathCol.Name = "FullPath";
            pathCol.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            pathCol.ReadOnly = true;
            dgvExportPaths.Columns.Add(pathCol);

            // Checkbox Column
            var checkCol = new DataGridViewCheckBoxColumn();
            checkCol.DataPropertyName = "IncludeSubFolders";
            checkCol.HeaderText = "包含子文件夹";
            checkCol.Name = "IncludeSubFolders";
            checkCol.Width = 100;
            dgvExportPaths.Columns.Add(checkCol);
        }

        private void LoadExportPaths()
        {
            try
            {
                var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                var bindingList = new BindingList<ExportPathItem>();

                // Load checkbox settings
                var checkboxSettingsObj = AppSettings.Get("ExportPathCheckboxSettings");
                Dictionary<string, bool> checkboxSettings = null;

                if (checkboxSettingsObj != null)
                {
                    try
                    {
                        checkboxSettings = checkboxSettingsObj as Dictionary<string, bool>;
                        if (checkboxSettings == null)
                        {
                            var json = checkboxSettingsObj.ToString();
                            checkboxSettings = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
                        }
                    }
                    catch
                    {
                        checkboxSettings = new Dictionary<string, bool>();
                    }
                }
                else
                {
                    checkboxSettings = new Dictionary<string, bool>();
                }

                foreach (var path in exportPaths)
                {
                    bool exists = Directory.Exists(path);
                    string displayPath = exists ? path : path + " (路径不存在)";
                    bool include = true;
                    if (checkboxSettings.ContainsKey(path)) include = checkboxSettings[path];

                    bindingList.Add(new ExportPathItem 
                    { 
                        FullPath = displayPath, 
                        IncludeSubFolders = include 
                    });
                }

                dgvExportPaths.DataSource = bindingList;
                UpdateStatus(exportPaths.Count(System.IO.Directory.Exists), exportPaths.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("加载导出路径失败: " + ex.Message);
            }
        }

        private void UpdateStatus(int valid, int total)
        {
            if (total == 0) lblStatus.Text = "暂无导出路径";
            else lblStatus.Text = $"共 {total} 个路径 (有效: {valid})";
        }

        private void SaveExportPaths()
        {
            var list = dgvExportPaths.DataSource as BindingList<ExportPathItem>;
            if (list == null) return;

            var pathList = new List<string>();
            var checkMap = new Dictionary<string, bool>();

            foreach (var item in list)
            {
                string cleanPath = item.FullPath;
                if (cleanPath.EndsWith(" (路径不存在)"))
                    cleanPath = cleanPath.Substring(0, cleanPath.Length - " (路径不存在)".Length);

                pathList.Add(cleanPath);
                checkMap[cleanPath] = item.IncludeSubFolders;
            }

            AppSettings.ExportPaths = pathList;
            AppSettings.Set("ExportPathCheckboxSettings", checkMap);
            AppSettings.Save();
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var dialog = new VistaFolderBrowserDialog())
            {
                dialog.Description = "请选择导出路径";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (AppSettings.ExportPaths != null && AppSettings.ExportPaths.Contains(dialog.SelectedPath))
                    {
                        MessageBox.Show("该路径已存在");
                        return;
                    }
                    
                    // Add via logic similar to SettingsForm directly? or just manipulate data source?
                    // Manipulating datasource then Saving is cleaner.
                    var list = dgvExportPaths.DataSource as BindingList<ExportPathItem>;
                    list.Add(new ExportPathItem { FullPath = dialog.SelectedPath, IncludeSubFolders = true });
                    
                    SaveExportPaths();
                    dgvExportPaths.Rows[dgvExportPaths.Rows.Count - 1].Selected = true;
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
             if (dgvExportPaths.SelectedRows.Count == 0) return;
             
             if (MessageBox.Show("确定要删除选中路径吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
             {
                 foreach (DataGridViewRow row in dgvExportPaths.SelectedRows)
                 {
                     if (!row.IsNewRow) dgvExportPaths.Rows.Remove(row);
                 }
                 SaveExportPaths();
             }
        }

        private void BtnMoveUp_Click(object sender, EventArgs e)
        {
            if (dgvExportPaths.SelectedRows.Count == 0) return;
            int idx = dgvExportPaths.SelectedRows[0].Index;
            if (idx <= 0) return;

            var list = dgvExportPaths.DataSource as BindingList<ExportPathItem>;
            var item = list[idx];
            list.RemoveAt(idx);
            list.Insert(idx - 1, item);
            dgvExportPaths.Rows[idx - 1].Selected = true;
            SaveExportPaths();
        }

        private void BtnMoveDown_Click(object sender, EventArgs e)
        {
            if (dgvExportPaths.SelectedRows.Count == 0) return;
            int idx = dgvExportPaths.SelectedRows[0].Index;
            var list = dgvExportPaths.DataSource as BindingList<ExportPathItem>;
            if (idx >= list.Count - 1) return;

            var item = list[idx];
            list.RemoveAt(idx);
            list.Insert(idx + 1, item);
            dgvExportPaths.Rows[idx + 1].Selected = true;
            SaveExportPaths();
        }

        private void DgvExportPaths_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
             // Save immediately on checkbox change
             if (e.RowIndex >= 0)
                SaveExportPaths();
        }

        private void DgvExportPaths_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvExportPaths.IsCurrentCellDirty)
            {
                dgvExportPaths.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
    }
}
