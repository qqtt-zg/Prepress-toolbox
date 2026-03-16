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
            // 带层次缩进的显示名称
            public string FolderNameWithIndent
            {
                get
                {
                    var name = System.IO.Path.GetFileName(FullPath);
                    if (IsSubFolder && Depth > 0)
                    {
                        return new string('　', Depth) + "└─ " + name; // 使用全角空格
                    }
                    return name;
                }
            }
            // 计算深度（相对于根路径的层级）
            public int Depth { get; set; } = 0;
            public bool IncludeSubFolders { get; set; } = true;
            // 标记是否为子文件夹
            public bool IsSubFolder { get; set; } = false;
            // 预设参数绑定
            public string PresetMaterial { get; set; } = "";      // 预设材料
            public string PresetBleed { get; set; } = "";        // 预设出血值
            public string PresetColorMode { get; set; } = "";    // 预设颜色模式
            public string PresetFilmType { get; set; } = "";     // 预设膜类型
            public bool PresetAddIdentifierPage { get; set; } = false; // 预设标识页
            public string PresetShape { get; set; } = "";         // 预设形状
            public bool PresetDualCopy { get; set; } = false;    // 预设一式两联
        }

        private void InitializeExportPathsDataGridView()
        {
             dgvExportPaths.AutoGenerateColumns = false;
             dgvExportPaths.EditMode = DataGridViewEditMode.EditOnEnter; // 点击即可编辑
             dgvExportPaths.Columns.Clear();

            // FolderName Column
            var nameCol = new DataGridViewTextBoxColumn();
            nameCol.DataPropertyName = "FolderNameWithIndent";
            nameCol.HeaderText = "文件夹名称";
            nameCol.Name = "FolderName";
            nameCol.Width = 180;
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

            // 预设材料列
            var materialCol = new DataGridViewComboBoxColumn();
            materialCol.DataPropertyName = "PresetMaterial";
            materialCol.HeaderText = "预设材料";
            materialCol.Name = "PresetMaterial";
            materialCol.Width = 80;
            materialCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            materialCol.Items.AddRange(new object[] { "", "铜版纸", "胶版纸", "特种纸", "不干胶", "PET" });
            dgvExportPaths.Columns.Add(materialCol);

            // 预设出血位列
            var bleedCol = new DataGridViewComboBoxColumn();
            bleedCol.DataPropertyName = "PresetBleed";
            bleedCol.HeaderText = "预设出血";
            bleedCol.Name = "PresetBleed";
            bleedCol.Width = 60;
            bleedCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            bleedCol.Items.AddRange(new object[] { "", "0", "2", "3", "1.5", "1" });
            dgvExportPaths.Columns.Add(bleedCol);

            // 预设颜色模式列
            var colorModeCol = new DataGridViewComboBoxColumn();
            colorModeCol.DataPropertyName = "PresetColorMode";
            colorModeCol.HeaderText = "预设颜色";
            colorModeCol.Name = "PresetColorMode";
            colorModeCol.Width = 60;
            colorModeCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            colorModeCol.Items.AddRange(new object[] { "", "彩色", "黑白" });
            dgvExportPaths.Columns.Add(colorModeCol);

            // 预设膜类型列
            var filmTypeCol = new DataGridViewComboBoxColumn();
            filmTypeCol.DataPropertyName = "PresetFilmType";
            filmTypeCol.HeaderText = "预设膜";
            filmTypeCol.Name = "PresetFilmType";
            filmTypeCol.Width = 60;
            filmTypeCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            filmTypeCol.Items.AddRange(new object[] { "", "光膜", "哑膜", "不过膜", "红膜" });
            dgvExportPaths.Columns.Add(filmTypeCol);

            // 预设标识页列（复选框）
            var identifierPageCol = new DataGridViewCheckBoxColumn();
            identifierPageCol.DataPropertyName = "PresetAddIdentifierPage";
            identifierPageCol.HeaderText = "标识页";
            identifierPageCol.Name = "PresetAddIdentifierPage";
            identifierPageCol.Width = 50;
            dgvExportPaths.Columns.Add(identifierPageCol);

            // 预设形状列
            var shapeCol = new DataGridViewComboBoxColumn();
            shapeCol.DataPropertyName = "PresetShape";
            shapeCol.HeaderText = "预设形状";
            shapeCol.Name = "PresetShape";
            shapeCol.Width = 60;
            shapeCol.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            shapeCol.Items.AddRange(new object[] { "", "直角", "圆形", "异形", "圆角矩形" });
            dgvExportPaths.Columns.Add(shapeCol);

            // 预设一式两联列（复选框）
            var dualCopyCol = new DataGridViewCheckBoxColumn();
            dualCopyCol.DataPropertyName = "PresetDualCopy";
            dualCopyCol.HeaderText = "一式两联";
            dualCopyCol.Name = "PresetDualCopy";
            dualCopyCol.Width = 60;
            dgvExportPaths.Columns.Add(dualCopyCol);
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

                // 加载预设配置
                var presetSettingsObj = AppSettings.Get("ExportPathPresetSettings");
                Dictionary<string, Dictionary<string, object>> presetSettings = null;
                if (presetSettingsObj != null)
                {
                    try
                    {
                        var json = presetSettingsObj.ToString();
                        presetSettings = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(json);
                    }
                    catch
                    {
                        presetSettings = new Dictionary<string, Dictionary<string, object>>();
                    }
                }
                else
                {
                    presetSettings = new Dictionary<string, Dictionary<string, object>>();
                }

                foreach (var path in exportPaths)
                {
                    bool exists = Directory.Exists(path);
                    string displayPath = exists ? path : path + " (路径不存在)";
                    bool include = true;
                    if (checkboxSettings.ContainsKey(path)) include = checkboxSettings[path];

                    // 获取预设配置
                    var preset = presetSettings.ContainsKey(path) ? presetSettings[path] : null;

                    var item = new ExportPathItem
                    {
                        FullPath = displayPath,
                        IncludeSubFolders = include
                    };

                    // 应用预设配置
                    if (preset != null)
                    {
                        if (preset.ContainsKey("PresetMaterial")) item.PresetMaterial = preset["PresetMaterial"]?.ToString() ?? "";
                        if (preset.ContainsKey("PresetBleed")) item.PresetBleed = preset["PresetBleed"]?.ToString() ?? "";
                        if (preset.ContainsKey("PresetColorMode")) item.PresetColorMode = preset["PresetColorMode"]?.ToString() ?? "";
                        if (preset.ContainsKey("PresetFilmType")) item.PresetFilmType = preset["PresetFilmType"]?.ToString() ?? "";
                        if (preset.ContainsKey("PresetAddIdentifierPage")) item.PresetAddIdentifierPage = preset["PresetAddIdentifierPage"]?.ToString() == "True";
                        if (preset.ContainsKey("PresetShape")) item.PresetShape = preset["PresetShape"]?.ToString() ?? "";
                        if (preset.ContainsKey("PresetDualCopy")) item.PresetDualCopy = preset["PresetDualCopy"]?.ToString() == "True";
                    }

                    bindingList.Add(item);
                }

                // 加载预设配置中的子文件夹（持久化的子路径）
                foreach (var presetPath in presetSettings.Keys)
                {
                    // 跳过根路径（已在上面处理）
                    if (exportPaths.Contains(presetPath)) continue;

                    // 检查是否已存在
                    if (bindingList.Any(x => x.FullPath == presetPath)) continue;

                    try
                    {
                        bool exists = System.IO.Directory.Exists(presetPath);
                        string displayPath = exists ? presetPath : presetPath + " (路径不存在)";
                        bool include = true;
                        if (checkboxSettings.ContainsKey(presetPath)) include = checkboxSettings[presetPath];

                        // 计算深度（相对于最近配置的父路径）
                        int depth = 1;
                        var cleanPresetPath = presetPath;
                        if (cleanPresetPath.EndsWith(" (路径不存在)"))
                            cleanPresetPath = cleanPresetPath.Substring(0, cleanPresetPath.Length - " (路径不存在)".Length);

                        // 查找最近的父路径并计算深度
                        foreach (var rootPath in exportPaths)
                        {
                            if (cleanPresetPath.StartsWith(rootPath + Path.DirectorySeparatorChar))
                            {
                                string relativePath = cleanPresetPath.Substring(rootPath.Length + 1);
                                depth = relativePath.Split(Path.DirectorySeparatorChar).Length;
                                break;
                            }
                        }

                        var preset = presetSettings[presetPath];
                        var item = new ExportPathItem
                        {
                            FullPath = displayPath,
                            IncludeSubFolders = include,
                            IsSubFolder = true,
                            Depth = depth
                        };

                        // 应用预设配置
                        if (preset != null)
                        {
                            if (preset.ContainsKey("PresetMaterial")) item.PresetMaterial = preset["PresetMaterial"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetBleed")) item.PresetBleed = preset["PresetBleed"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetColorMode")) item.PresetColorMode = preset["PresetColorMode"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetFilmType")) item.PresetFilmType = preset["PresetFilmType"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetAddIdentifierPage")) item.PresetAddIdentifierPage = preset["PresetAddIdentifierPage"]?.ToString() == "True";
                            if (preset.ContainsKey("PresetShape")) item.PresetShape = preset["PresetShape"]?.ToString() ?? "";
                            if (preset.ContainsKey("PresetDualCopy")) item.PresetDualCopy = preset["PresetDualCopy"]?.ToString() == "True";
                        }

                        bindingList.Add(item);
                    }
                    catch (Exception subEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"加载子路径失败: {presetPath}, {subEx.Message}");
                    }
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

        public void SaveSettings()
        {
            var list = dgvExportPaths.DataSource as BindingList<ExportPathItem>;
            if (list == null) return;

            var pathList = new List<string>();
            var checkMap = new Dictionary<string, bool>();
            var presetMap = new Dictionary<string, Dictionary<string, object>>();

            foreach (var item in list)
            {
                string cleanPath = item.FullPath;
                if (cleanPath.EndsWith(" (路径不存在)"))
                    cleanPath = cleanPath.Substring(0, cleanPath.Length - " (路径不存在)".Length);

                // 只保存根路径，不保存子文件夹（子文件夹是动态加载的）
                if (!item.IsSubFolder)
                {
                    pathList.Add(cleanPath);
                    checkMap[cleanPath] = item.IncludeSubFolders;
                }

                // 保存所有路径（包括子文件夹）的预设配置
                var preset = new Dictionary<string, object>();
                if (!string.IsNullOrEmpty(item.PresetMaterial)) preset["PresetMaterial"] = item.PresetMaterial;
                if (!string.IsNullOrEmpty(item.PresetBleed)) preset["PresetBleed"] = item.PresetBleed;
                if (!string.IsNullOrEmpty(item.PresetColorMode)) preset["PresetColorMode"] = item.PresetColorMode;
                if (!string.IsNullOrEmpty(item.PresetFilmType)) preset["PresetFilmType"] = item.PresetFilmType;
                if (item.PresetAddIdentifierPage) preset["PresetAddIdentifierPage"] = item.PresetAddIdentifierPage.ToString();
                if (!string.IsNullOrEmpty(item.PresetShape)) preset["PresetShape"] = item.PresetShape;
                if (item.PresetDualCopy) preset["PresetDualCopy"] = item.PresetDualCopy.ToString();

                if (preset.Count > 0)
                {
                    presetMap[cleanPath] = preset;
                }
            }

            AppSettings.ExportPaths = pathList;
            AppSettings.Set("ExportPathCheckboxSettings", checkMap);
            AppSettings.Set("ExportPathPresetSettings", presetMap);
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
                    
                    SaveSettings();
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
                 SaveSettings();
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
            SaveSettings();
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
            SaveSettings();
        }

        private void DgvExportPaths_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var column = dgvExportPaths.Columns[e.ColumnIndex];
                var list = dgvExportPaths.DataSource as BindingList<ExportPathItem>;

                // 检查是否是"包含子文件夹"列的值发生变化
                if (column.Name == "IncludeSubFolders" && list != null)
                {
                    var item = list[e.RowIndex];
                    bool includeSubFolders = item.IncludeSubFolders;

                    // 清理该路径下已有的子文件夹项
                    var subFolderItems = list.Where(x => x.FullPath.StartsWith(item.FullPath + Path.DirectorySeparatorChar)).ToList();
                    foreach (var subItem in subFolderItems)
                    {
                        list.Remove(subItem);
                    }

                    // 如果勾选了包含子文件夹，加载子文件夹
                    if (includeSubFolders && Directory.Exists(item.FullPath))
                    {
                        try
                        {
                            var subDirs = Directory.GetDirectories(item.FullPath);
                            foreach (var subDir in subDirs.OrderBy(d => d))
                            {
                                // 检查是否已经存在（可能由其他路径添加）
                                if (!list.Any(x => x.FullPath == subDir))
                                {
                                    var subItem = new ExportPathItem
                                    {
                                        FullPath = subDir,
                                        IncludeSubFolders = false,
                                        IsSubFolder = true,
                                        Depth = 1,
                                        // 继承父路径的预设配置
                                        PresetMaterial = item.PresetMaterial,
                                        PresetBleed = item.PresetBleed,
                                        PresetColorMode = item.PresetColorMode,
                                        PresetFilmType = item.PresetFilmType,
                                        PresetAddIdentifierPage = item.PresetAddIdentifierPage,
                                        PresetShape = item.PresetShape,
                                        PresetDualCopy = item.PresetDualCopy
                                    };
                                    list.Add(subItem);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Forms.MessageBox.Show("加载子文件夹失败: " + ex.Message);
                        }
                    }
                }

                SaveSettings();
            }
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
