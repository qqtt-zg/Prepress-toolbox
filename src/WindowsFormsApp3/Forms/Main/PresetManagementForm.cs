using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Main
{
    /// <summary>
    /// 预设管理窗口
    /// 用于查看、添加、编辑和删除材料预设
    /// </summary>
    public class PresetManagementForm : Form
    {
        private DataGridView _dgvPresets;
        private Button _btnAdd;
        private Button _btnEdit;
        private Button _btnDelete;
        private Button _btnOK;
        private Button _btnCancel;
        private Label _lblTitle;

        private List<MaterialSelectionPreset> _presets;
        private MaterialSelectionPreset _selectedPreset;

        public PresetManagementForm()
        {
            InitializeComponent();
            LoadPresets();
        }

        private void InitializeComponent()
        {
            this.Text = "预设管理";
            this.Size = new Size(700, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            // 标题
            _lblTitle = new Label
            {
                Text = "材料预设管理",
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                Location = new Point(20, 15),
                AutoSize = true
            };
            this.Controls.Add(_lblTitle);

            // DataGridView
            _dgvPresets = new DataGridView
            {
                Location = new Point(20, 50),
                Size = new Size(660, 300),
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(240, 240, 240),
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 35,
                RowTemplate = { Height = 32 }
            };
            _dgvPresets.Columns.Add("Name", "预设名称");
            _dgvPresets.Columns.Add("MaterialType", "材料类型");
            _dgvPresets.Columns.Add("LayoutMode", "排版模式");
            _dgvPresets.Columns.Add("EnableImposition", "启用排版");
            _dgvPresets.Columns.Add("SelectedMaterial", "材料");
            _dgvPresets.Columns.Add("TetBleed", "出血");
            _dgvPresets.Columns.Add("ColorMode", "颜色模式");
            _dgvPresets.Columns.Add("FilmType", "膜类型");
            _dgvPresets.Columns.Add("ShapeState", "形状");
            _dgvPresets.Columns.Add("ExportPath", "导出路径");
            _dgvPresets.Columns["Name"].Width = 70;
            _dgvPresets.Columns["MaterialType"].Width = 55;
            _dgvPresets.Columns["LayoutMode"].Width = 55;
            _dgvPresets.Columns["EnableImposition"].Width = 55;
            _dgvPresets.Columns["SelectedMaterial"].Width = 60;
            _dgvPresets.Columns["TetBleed"].Width = 40;
            _dgvPresets.Columns["ColorMode"].Width = 55;
            _dgvPresets.Columns["FilmType"].Width = 55;
            _dgvPresets.Columns["ShapeState"].Width = 60;
            _dgvPresets.Columns["ExportPath"].Width = 140;
            _dgvPresets.SelectionChanged += DgvPresets_SelectionChanged;
            _dgvPresets.DoubleClick += DgvPresets_DoubleClick;
            this.Controls.Add(_dgvPresets);

            // 按钮
            _btnAdd = new Button
            {
                Text = "添加",
                Location = new Point(20, 365),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.System
            };
            _btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(_btnAdd);

            _btnEdit = new Button
            {
                Text = "编辑",
                Location = new Point(110, 365),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.System,
                Enabled = false
            };
            _btnEdit.Click += BtnEdit_Click;
            this.Controls.Add(_btnEdit);

            _btnDelete = new Button
            {
                Text = "删除",
                Location = new Point(200, 365),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.System,
                Enabled = false
            };
            _btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(_btnDelete);

            _btnOK = new Button
            {
                Text = "确定",
                Location = new Point(510, 365),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.OK
            };
            this.Controls.Add(_btnOK);

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(600, 365),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;
        }

        private void LoadPresets()
        {
            _presets = AppSettings.MaterialPresets ?? new List<MaterialSelectionPreset>();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            _dgvPresets.Rows.Clear();
            foreach (var preset in _presets)
            {
                int rowIndex = _dgvPresets.Rows.Add();
                var row = _dgvPresets.Rows[rowIndex];
                row.Cells["Name"].Value = preset.Name;
                row.Cells["MaterialType"].Value = GetMaterialTypeDisplayName(preset.MaterialType);
                row.Cells["LayoutMode"].Value = GetLayoutModeDisplayName(preset.LayoutMode);
                row.Cells["EnableImposition"].Value = GetImpositionDisplayName(preset.EnableImposition);
                row.Cells["SelectedMaterial"].Value = preset.SelectedMaterial;
                row.Cells["TetBleed"].Value = preset.TetBleed;
                row.Cells["ColorMode"].Value = preset.ColorMode;
                row.Cells["FilmType"].Value = preset.FilmType;
                row.Cells["ShapeState"].Value = GetShapeDisplayName(preset.ShapeState);
                row.Cells["ExportPath"].Value = preset.ExportPath;
                row.Tag = preset;
            }
        }

        private string GetShapeDisplayName(string shapeState)
        {
            return shapeState switch
            {
                "RightAngle" => "直角",
                "Circle" => "圆形",
                "Special" => "异形",
                "RoundRect" => "圆角矩形",
                _ => shapeState
            };
        }

        private string GetMaterialTypeDisplayName(string materialType)
        {
            return materialType switch
            {
                "FlatSheet" => "平张",
                "RollMaterial" => "卷装",
                _ => materialType
            };
        }

        private string GetLayoutModeDisplayName(string layoutMode)
        {
            return layoutMode switch
            {
                "Continuous" => "连拼",
                "Folding" => "折手",
                _ => layoutMode
            };
        }

        private string GetImpositionDisplayName(bool enableImposition)
        {
            return enableImposition ? "是" : "否";
        }

        private void DgvPresets_SelectionChanged(object sender, EventArgs e)
        {
            if (_dgvPresets.SelectedRows.Count > 0)
            {
                _selectedPreset = _dgvPresets.SelectedRows[0].Tag as MaterialSelectionPreset;
                _btnEdit.Enabled = _selectedPreset != null;
                _btnDelete.Enabled = _selectedPreset != null;
            }
            else
            {
                _selectedPreset = null;
                _btnEdit.Enabled = false;
                _btnDelete.Enabled = false;
            }
        }

        private void DgvPresets_DoubleClick(object sender, EventArgs e)
        {
            if (_selectedPreset != null)
            {
                EditPreset(_selectedPreset);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var newPreset = new MaterialSelectionPreset
            {
                Name = "",
                MaterialType = "FlatSheet",
                LayoutMode = "Continuous",
                EnableImposition = false,
                SelectedMaterial = "",
                TetBleed = 0,
                ColorMode = "彩色",
                FilmType = "光膜",
                AddIdentifierPage = false,
                ShapeState = "RightAngle",
                IsDualCopy = false,
                ExportPath = "",
                RoundRadius = 0
            };

            using (var dialog = new PresetEditDialog(newPreset, true))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _presets.Add(dialog.Preset);
                    SaveAndRefresh();
                }
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_selectedPreset != null)
            {
                EditPreset(_selectedPreset);
            }
        }

        private void EditPreset(MaterialSelectionPreset preset)
        {
            // 创建副本用于编辑
            var editPreset = new MaterialSelectionPreset
            {
                Name = preset.Name,
                MaterialType = preset.MaterialType,
                LayoutMode = preset.LayoutMode,
                EnableImposition = preset.EnableImposition,
                SelectedMaterial = preset.SelectedMaterial,
                TetBleed = preset.TetBleed,
                ColorMode = preset.ColorMode,
                FilmType = preset.FilmType,
                AddIdentifierPage = preset.AddIdentifierPage,
                ShapeState = preset.ShapeState,
                IsDualCopy = preset.IsDualCopy,
                ExportPath = preset.ExportPath,
                RoundRadius = preset.RoundRadius
            };

            using (var dialog = new PresetEditDialog(editPreset, false))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    // 更新原预设
                    preset.Name = dialog.Preset.Name;
                    preset.MaterialType = dialog.Preset.MaterialType;
                    preset.LayoutMode = dialog.Preset.LayoutMode;
                    preset.SelectedMaterial = dialog.Preset.SelectedMaterial;
                    preset.TetBleed = dialog.Preset.TetBleed;
                    preset.ColorMode = dialog.Preset.ColorMode;
                    preset.FilmType = dialog.Preset.FilmType;
                    preset.AddIdentifierPage = dialog.Preset.AddIdentifierPage;
                    preset.ShapeState = dialog.Preset.ShapeState;
                    preset.IsDualCopy = dialog.Preset.IsDualCopy;
                    preset.EnableImposition = dialog.Preset.EnableImposition;
                    preset.ExportPath = dialog.Preset.ExportPath;
                    preset.RoundRadius = dialog.Preset.RoundRadius;
                    SaveAndRefresh();
                }
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPreset != null)
            {
                var result = MessageBox.Show(
                    $"确定要删除预设\"{_selectedPreset.Name}\"吗？",
                    "确认删除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _presets.Remove(_selectedPreset);
                    SaveAndRefresh();
                }
            }
        }

        private void SaveAndRefresh()
        {
            AppSettings.MaterialPresets = _presets;
            AppSettings.Save();
            RefreshGrid();
        }
    }
}
