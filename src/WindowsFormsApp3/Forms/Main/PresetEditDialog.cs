using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Main
{
    /// <summary>
    /// 预设编辑对话框
    /// 用于编辑或新建材料预设参数
    /// </summary>
    public class PresetEditDialog : Form
    {
        private TextBox _txtName;
        private ComboBox _cmbMaterialType;
        private ComboBox _cmbMaterial;
        private NumericUpDown _numTetBleed;
        private ComboBox _cmbColorMode;
        private ComboBox _cmbFilmType;
        private CheckBox _chkAddIdentifierPage;
        private ComboBox _cmbShapeState;
        private NumericUpDown _numRoundRadius;
        private Label _lblRadiusUnit;
        private ComboBox _cmbLayoutMode;
        private CheckBox _chkIsDualCopy;
        private NumericUpDown _numCopyCount;
        private ComboBox _cmbCopyMode;
        private CheckBox _chkEnableCopyCount;
        private CheckBox _chkEnableCopyMode;
        private CheckBox _chkEnableImposition;
        private ComboBox _cmbExportPath;
        private Button _btnBrowsePath;

        // 启用/禁用复选框
        private CheckBox _chkEnableMaterialType;
        private CheckBox _chkEnableMaterial;
        private CheckBox _chkEnableTetBleed;
        private CheckBox _chkEnableColorMode;
        private CheckBox _chkEnableFilmType;
        private CheckBox _chkEnableShape;
        private CheckBox _chkEnableRoundRadius;
        private CheckBox _chkEnableLayoutMode;
        private CheckBox _chkEnableImpositionOption;
        private CheckBox _chkEnableIsDualCopy;
        private CheckBox _chkEnableIdentifierPage;
        private CheckBox _chkEnableExportPath;
        private CheckBox _chkShowInPresetButtons;

        private Button _btnOK;
        private Button _btnCancel;

        private MaterialSelectionPreset _preset;
        private bool _isNew;

        public MaterialSelectionPreset Preset => _preset;

        public PresetEditDialog(MaterialSelectionPreset preset, bool isNew)
        {
            _preset = preset;
            _isNew = isNew;
            InitializeComponent();
            LoadMaterials();
            LoadExportPaths();
            LoadCurrentValues();
        }

        private void InitializeComponent()
        {
            this.Text = _isNew ? "新建预设" : "编辑预设";
            this.Size = new Size(480, 850);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;

            int chkX = 8;
            int labelX = 35;
            int controlX = 130;
            int width = 220;
            int height = 24;
            int startY = 20;
            int rowHeight = 40;

            // 第1行：预设名称（无启用复选框）
            var lblName = new Label { Text = "预设名称:", Location = new Point(labelX, startY), AutoSize = true };
            _txtName = new TextBox { Location = new Point(controlX, startY), Width = width, Height = height };
            this.Controls.Add(lblName);
            this.Controls.Add(_txtName);

            // 第2行：材料类型
            _chkEnableMaterialType = new CheckBox { Location = new Point(chkX, startY + rowHeight), AutoSize = true, Checked = true };
            var lblMaterialType = new Label { Text = "材料类型:", Location = new Point(labelX, startY + rowHeight), AutoSize = true };
            _cmbMaterialType = new ComboBox { Location = new Point(controlX, startY + rowHeight), Width = width, Height = height, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbMaterialType.Items.AddRange(new object[] { "平张", "卷装" });
            this.Controls.Add(_chkEnableMaterialType);
            this.Controls.Add(lblMaterialType);
            this.Controls.Add(_cmbMaterialType);

            // 第3行：材料
            _chkEnableMaterial = new CheckBox { Location = new Point(chkX, startY + rowHeight * 2), AutoSize = true, Checked = true };
            var lblMaterial = new Label { Text = "材料:", Location = new Point(labelX, startY + rowHeight * 2), AutoSize = true };
            _cmbMaterial = new ComboBox { Location = new Point(controlX, startY + rowHeight * 2), Width = width, Height = height, DropDownStyle = ComboBoxStyle.DropDown };
            this.Controls.Add(_chkEnableMaterial);
            this.Controls.Add(lblMaterial);
            this.Controls.Add(_cmbMaterial);

            // 第4行：出血
            _chkEnableTetBleed = new CheckBox { Location = new Point(chkX, startY + rowHeight * 3), AutoSize = true, Checked = true };
            var lblTetBleed = new Label { Text = "出血(mm):", Location = new Point(labelX, startY + rowHeight * 3), AutoSize = true };
            _numTetBleed = new NumericUpDown { Location = new Point(controlX, startY + rowHeight * 3), Width = width, Height = height, Minimum = 0, Maximum = 50, DecimalPlaces = 1 };
            this.Controls.Add(_chkEnableTetBleed);
            this.Controls.Add(lblTetBleed);
            this.Controls.Add(_numTetBleed);

            // 第5行：颜色模式
            _chkEnableColorMode = new CheckBox { Location = new Point(chkX, startY + rowHeight * 4), AutoSize = true, Checked = true };
            var lblColorMode = new Label { Text = "颜色模式:", Location = new Point(labelX, startY + rowHeight * 4), AutoSize = true };
            _cmbColorMode = new ComboBox { Location = new Point(controlX, startY + rowHeight * 4), Width = width, Height = height, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbColorMode.Items.AddRange(new object[] { "彩色", "黑白" });
            this.Controls.Add(_chkEnableColorMode);
            this.Controls.Add(lblColorMode);
            this.Controls.Add(_cmbColorMode);

            // 第6行：膜类型
            _chkEnableFilmType = new CheckBox { Location = new Point(chkX, startY + rowHeight * 5), AutoSize = true, Checked = true };
            var lblFilmType = new Label { Text = "膜类型:", Location = new Point(labelX, startY + rowHeight * 5), AutoSize = true };
            _cmbFilmType = new ComboBox { Location = new Point(controlX, startY + rowHeight * 5), Width = width, Height = height, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbFilmType.Items.AddRange(new object[] { "光膜", "哑膜", "不过膜", "红膜" });
            this.Controls.Add(_chkEnableFilmType);
            this.Controls.Add(lblFilmType);
            this.Controls.Add(_cmbFilmType);

            // 第7行：添加标识页
            _chkEnableIdentifierPage = new CheckBox { Location = new Point(chkX, startY + rowHeight * 6), AutoSize = true, Checked = true };
            var lblAddIdentifierPage = new Label { Text = "添加标识页:", Location = new Point(labelX, startY + rowHeight * 6), AutoSize = true };
            _chkAddIdentifierPage = new CheckBox { Location = new Point(controlX, startY + rowHeight * 6), AutoSize = true };
            this.Controls.Add(_chkEnableIdentifierPage);
            this.Controls.Add(lblAddIdentifierPage);
            this.Controls.Add(_chkAddIdentifierPage);

            // 第8行：形状 + 圆角半径
            _chkEnableShape = new CheckBox { Location = new Point(chkX, startY + rowHeight * 7), AutoSize = true, Checked = true };
            var lblShapeState = new Label { Text = "形状:", Location = new Point(labelX, startY + rowHeight * 7), AutoSize = true };
            _cmbShapeState = new ComboBox { Location = new Point(controlX, startY + rowHeight * 7), Width = 100, Height = height, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbShapeState.Items.AddRange(new object[] { "无", "直角", "圆形", "异形", "圆角矩形" });
            this.Controls.Add(_chkEnableShape);
            this.Controls.Add(lblShapeState);
            this.Controls.Add(_cmbShapeState);
            _cmbShapeState.SelectedIndexChanged += CmbShapeState_SelectedIndexChanged;

            var lblRoundRadius = new Label { Text = "圆角半径:", Location = new Point(controlX + 110, startY + rowHeight * 7), AutoSize = true };
            _numRoundRadius = new NumericUpDown { Location = new Point(controlX + 175, startY + rowHeight * 7), Width = 60, Height = height, Minimum = 0, Maximum = 100, DecimalPlaces = 1 };
            _lblRadiusUnit = new Label { Text = "mm", Location = new Point(controlX + 238, startY + rowHeight * 7), AutoSize = true };
            _numRoundRadius.Enabled = false;
            _lblRadiusUnit.Enabled = false;
            this.Controls.Add(lblRoundRadius);
            this.Controls.Add(_numRoundRadius);
            this.Controls.Add(_lblRadiusUnit);

            // 第8-1行：圆角半径启用复选框（与形状同行）
            _chkEnableRoundRadius = new CheckBox { Location = new Point(controlX + 110, startY + rowHeight * 7 + 22), AutoSize = true, Checked = true };
            this.Controls.Add(_chkEnableRoundRadius);

            // 第9行：排版模式
            _chkEnableLayoutMode = new CheckBox { Location = new Point(chkX, startY + rowHeight * 8), AutoSize = true, Checked = true };
            var lblLayoutMode = new Label { Text = "排版模式:", Location = new Point(labelX, startY + rowHeight * 8), AutoSize = true };
            _cmbLayoutMode = new ComboBox { Location = new Point(controlX, startY + rowHeight * 8), Width = width, Height = height, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbLayoutMode.Items.AddRange(new object[] { "连拼", "折手" });
            this.Controls.Add(_chkEnableLayoutMode);
            this.Controls.Add(lblLayoutMode);
            this.Controls.Add(_cmbLayoutMode);

            // 第10行：启用排版
            _chkEnableImpositionOption = new CheckBox { Location = new Point(chkX, startY + rowHeight * 9), AutoSize = true, Checked = true };
            var lblEnableImposition = new Label { Text = "启用排版:", Location = new Point(labelX, startY + rowHeight * 9), AutoSize = true };
            _chkEnableImposition = new CheckBox { Location = new Point(controlX, startY + rowHeight * 9), AutoSize = true };
            this.Controls.Add(_chkEnableImpositionOption);
            this.Controls.Add(lblEnableImposition);
            this.Controls.Add(_chkEnableImposition);

            // 第11行：一式N联
            _chkEnableIsDualCopy = new CheckBox { Location = new Point(chkX, startY + rowHeight * 10), AutoSize = true, Checked = true };
            var lblIsDualCopy = new Label { Text = "一式N联:", Location = new Point(labelX, startY + rowHeight * 10), AutoSize = true };
            _chkIsDualCopy = new CheckBox { Location = new Point(controlX, startY + rowHeight * 10), AutoSize = true };
            this.Controls.Add(_chkEnableIsDualCopy);
            this.Controls.Add(lblIsDualCopy);
            this.Controls.Add(_chkIsDualCopy);

            // 第11-1行：联数
            _chkEnableCopyCount = new CheckBox { Location = new Point(chkX, startY + rowHeight * 10 + 22), AutoSize = true, Checked = true };
            var lblCopyCount = new Label { Text = "联数:", Location = new Point(labelX, startY + rowHeight * 10 + 22), AutoSize = true };
            _numCopyCount = new NumericUpDown { Location = new Point(controlX, startY + rowHeight * 10 + 22), Width = 60, Height = height, Minimum = 2, Maximum = 16, Value = 2 };
            this.Controls.Add(_chkEnableCopyCount);
            this.Controls.Add(lblCopyCount);
            this.Controls.Add(_numCopyCount);

            // 第11-2行：倍数方向
            _chkEnableCopyMode = new CheckBox { Location = new Point(chkX, startY + rowHeight * 10 + 44), AutoSize = true, Checked = true };
            var lblCopyMode = new Label { Text = "倍数方向:", Location = new Point(labelX, startY + rowHeight * 10 + 44), AutoSize = true };
            _cmbCopyMode = new ComboBox { Location = new Point(controlX, startY + rowHeight * 10 + 44), Width = width, Height = height, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbCopyMode.Items.AddRange(new object[] { "自适应列", "自适应行", "不旋转列", "不旋转行" });
            this.Controls.Add(_chkEnableCopyMode);
            this.Controls.Add(lblCopyMode);
            this.Controls.Add(_cmbCopyMode);

            // 第12行：导出路径
            _chkEnableExportPath = new CheckBox { Location = new Point(chkX, startY + rowHeight * 12), AutoSize = true, Checked = true };
            var lblExportPath = new Label { Text = "导出路径:", Location = new Point(labelX, startY + rowHeight * 12), AutoSize = true };
            _cmbExportPath = new ComboBox { Location = new Point(controlX, startY + rowHeight * 12), Width = 180, Height = height, DropDownStyle = ComboBoxStyle.DropDown };
            _btnBrowsePath = new Button { Text = "浏览...", Location = new Point(controlX + 185, startY + rowHeight * 12), Width = 55, Height = height, FlatStyle = FlatStyle.System };
            _btnBrowsePath.Click += BtnBrowsePath_Click;
            this.Controls.Add(_chkEnableExportPath);
            this.Controls.Add(lblExportPath);
            this.Controls.Add(_cmbExportPath);
            this.Controls.Add(_btnBrowsePath);

            // 第13行：添加到预设按钮
            _chkShowInPresetButtons = new CheckBox { Location = new Point(chkX, startY + rowHeight * 13), AutoSize = true, Checked = false };
            var lblShowInPresetButtons = new Label { Text = "添加到预设按钮:", Location = new Point(labelX, startY + rowHeight * 13), AutoSize = true };
            this.Controls.Add(_chkShowInPresetButtons);
            this.Controls.Add(lblShowInPresetButtons);

            // 按钮
            _btnOK = new Button
            {
                Text = "确定",
                Location = new Point(280, startY + rowHeight * 14 + 10),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.OK
            };
            _btnOK.Click += BtnOK_Click;
            this.Controls.Add(_btnOK);

            _btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(370, startY + rowHeight * 14 + 10),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.System,
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnOK;
            this.CancelButton = _btnCancel;
        }

        private void CmbShapeState_SelectedIndexChanged(object sender, EventArgs e)
        {
            // "无"是索引0，"直角"是索引1，"圆形"是索引2，"异形"是索引3，"圆角矩形"是索引4
            bool isRoundRect = _cmbShapeState.SelectedIndex == 4; // 圆角矩形
            bool isShapeSelected = _cmbShapeState.SelectedIndex > 0; // 不是"无"
            _numRoundRadius.Enabled = isRoundRect;
            _lblRadiusUnit.Enabled = isRoundRect;
            _chkEnableRoundRadius.Enabled = isShapeSelected;
            _chkEnableRoundRadius.Checked = isShapeSelected && isRoundRect;
        }

        private void LoadMaterials()
        {
            // 优先从 AppSettings.Material 加载（与 MaterialSelectFormModern 保持一致）
            var materials = AppSettings.Material;
            if (string.IsNullOrEmpty(materials))
            {
                // 如果 Material 为空，尝试从 Materials 加载
                materials = AppSettings.Materials;
            }
            if (!string.IsNullOrEmpty(materials))
            {
                var materialList = materials.Split(new[] { ',', '|', '，', '、' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var m in materialList)
                {
                    var trimmed = m.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !_cmbMaterial.Items.Contains(trimmed))
                    {
                        _cmbMaterial.Items.Add(trimmed);
                    }
                }
            }
            // 如果仍然没有材料，添加默认材料列表
            if (_cmbMaterial.Items.Count == 0)
            {
                _cmbMaterial.Items.AddRange(new object[] { "PET", "PP", "PVC", "PET环保", "PET透明", "PET哑光", "PET镭射", "PET磨砂", "PET金色", "PET银色", "PET白色", "PET红色", "PET蓝色", "PET绿色", "PP环保" });
            }
        }

        private void LoadExportPaths()
        {
            try
            {
                var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                foreach (var path in exportPaths)
                {
                    if (!string.IsNullOrEmpty(path) && !_cmbExportPath.Items.Contains(path))
                    {
                        _cmbExportPath.Items.Add(path);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载导出路径失败: {ex.Message}", ex);
            }
        }

        private void LoadCurrentValues()
        {
            _txtName.Text = _preset.Name;

            // 材料类型
            _cmbMaterialType.Text = _preset.MaterialType switch
            {
                "FlatSheet" => "平张",
                "RollMaterial" => "卷装",
                _ => "平张"
            };

            _cmbMaterial.Text = _preset.SelectedMaterial;
            _numTetBleed.Value = (decimal)_preset.TetBleed;
            _cmbColorMode.Text = _preset.ColorMode;
            _cmbFilmType.Text = _preset.FilmType;
            _chkAddIdentifierPage.Checked = _preset.AddIdentifierPage;

            // 形状: "无"(0), "直角"(1), "圆形"(2), "异形"(3), "圆角矩形"(4)
            int shapeIndex = _preset.ShapeState switch
            {
                "RightAngle" => 1,
                "Circle" => 2,
                "Special" => 3,
                "RoundRect" => 4,
                _ => 0  // "无" 或空
            };
            _cmbShapeState.SelectedIndex = shapeIndex;
            _numRoundRadius.Value = (decimal)_preset.RoundRadius;
            _numRoundRadius.Enabled = shapeIndex == 4;  // 圆角矩形是索引4
            _lblRadiusUnit.Enabled = shapeIndex == 4;

            // 排版模式
            _cmbLayoutMode.Text = _preset.LayoutMode switch
            {
                "Continuous" => "连拼",
                "Folding" => "折手",
                _ => "连拼"
            };

            _chkIsDualCopy.Checked = _preset.IsDualCopy;
            _numCopyCount.Value = _preset.CopyCount > 0 ? _preset.CopyCount : 2;
            _cmbCopyMode.SelectedIndex = (int)_preset.CopyMode;
            _chkEnableImposition.Checked = _preset.EnableImposition;
            _cmbExportPath.Text = _preset.ExportPath;

            // 加载禁用状态
            _chkEnableMaterialType.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.MaterialType);
            _chkEnableMaterial.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.Material);
            _chkEnableTetBleed.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.TetBleed);
            _chkEnableColorMode.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.ColorMode);
            _chkEnableFilmType.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.FilmType);
            _chkEnableShape.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.Shape);
            _chkEnableRoundRadius.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.RoundRadius);
            _chkEnableLayoutMode.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.LayoutMode);
            _chkEnableImpositionOption.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.EnableImposition);
            _chkEnableIsDualCopy.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.IsDualCopy);
            _chkEnableCopyCount.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.CopyCount);
            _chkEnableCopyMode.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.CopyMode);
            _chkEnableIdentifierPage.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.IdentifierPage);
            _chkEnableExportPath.Checked = !_preset.DisabledOptions.HasFlag(PresetIgnoreOptions.ExportPath);

            // 加载 ShowInPresetButtons
            _chkShowInPresetButtons.Checked = _preset.ShowInPresetButtons;
        }

        private void BtnBrowsePath_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择导出路径";
                if (!string.IsNullOrEmpty(_cmbExportPath.Text) && Directory.Exists(_cmbExportPath.Text))
                {
                    dialog.SelectedPath = _cmbExportPath.Text;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _cmbExportPath.Text = dialog.SelectedPath;
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 验证
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("请输入预设名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            // 检查名称重复
            var presets = AppSettings.MaterialPresets ?? new List<MaterialSelectionPreset>();
            bool isDuplicate = false;
            foreach (var p in presets)
            {
                if (p.Name == _txtName.Text && (_isNew || p.Name != _preset.Name))
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (isDuplicate)
            {
                MessageBox.Show("预设名称已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }

            // 保存值
            _preset.Name = _txtName.Text.Trim();

            // 材料类型
            _preset.MaterialType = _cmbMaterialType.Text switch
            {
                "平张" => "FlatSheet",
                "卷装" => "RollMaterial",
                _ => "FlatSheet"
            };

            _preset.SelectedMaterial = _cmbMaterial.Text;
            _preset.TetBleed = (double)_numTetBleed.Value;
            _preset.ColorMode = _cmbColorMode.Text;
            _preset.FilmType = _cmbFilmType.Text;
            _preset.AddIdentifierPage = _chkAddIdentifierPage.Checked;

            // 形状: 索引0="无", 1="RightAngle", 2="Circle", 3="Special", 4="RoundRect"
            string[] shapeValues = { "", "RightAngle", "Circle", "Special", "RoundRect" };
            _preset.ShapeState = shapeValues[_cmbShapeState.SelectedIndex];
            _preset.RoundRadius = (double)_numRoundRadius.Value;

            // 排版模式
            _preset.LayoutMode = _cmbLayoutMode.Text switch
            {
                "连拼" => "Continuous",
                "折手" => "Folding",
                _ => "Continuous"
            };

            _preset.IsDualCopy = _chkIsDualCopy.Checked;
            _preset.CopyCount = (int)_numCopyCount.Value;
            _preset.CopyMode = (CopyMode)_cmbCopyMode.SelectedIndex;
            _preset.EnableImposition = _chkEnableImposition.Checked;
            _preset.ExportPath = _cmbExportPath.Text;

            // 根据复选框状态构建 DisabledOptions
            var disabled = PresetIgnoreOptions.None;
            if (!_chkEnableMaterialType.Checked) disabled |= PresetIgnoreOptions.MaterialType;
            if (!_chkEnableMaterial.Checked) disabled |= PresetIgnoreOptions.Material;
            if (!_chkEnableTetBleed.Checked) disabled |= PresetIgnoreOptions.TetBleed;
            if (!_chkEnableColorMode.Checked) disabled |= PresetIgnoreOptions.ColorMode;
            if (!_chkEnableFilmType.Checked) disabled |= PresetIgnoreOptions.FilmType;
            if (!_chkEnableShape.Checked) disabled |= PresetIgnoreOptions.Shape;
            if (!_chkEnableRoundRadius.Checked) disabled |= PresetIgnoreOptions.RoundRadius;
            if (!_chkEnableLayoutMode.Checked) disabled |= PresetIgnoreOptions.LayoutMode;
            if (!_chkEnableImpositionOption.Checked) disabled |= PresetIgnoreOptions.EnableImposition;
            if (!_chkEnableIsDualCopy.Checked) disabled |= PresetIgnoreOptions.IsDualCopy;
            if (!_chkEnableCopyCount.Checked) disabled |= PresetIgnoreOptions.CopyCount;
            if (!_chkEnableCopyMode.Checked) disabled |= PresetIgnoreOptions.CopyMode;
            if (!_chkEnableIdentifierPage.Checked) disabled |= PresetIgnoreOptions.IdentifierPage;
            if (!_chkEnableExportPath.Checked) disabled |= PresetIgnoreOptions.ExportPath;
            _preset.DisabledOptions = disabled;

            // 保存 ShowInPresetButtons
            _preset.ShowInPresetButtons = _chkShowInPresetButtons.Checked;
        }
    }
}
