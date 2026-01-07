using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Forms.Dialogs;
using WindowsFormsApp3.Controls;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.EventArguments;
using Ookii.Dialogs.WinForms;

namespace WindowsFormsApp3
{
    [Obsolete("此类已弃用。请使用: EventGroupConfigurationService.GetEventGroupConfiguration(), LayoutResultsCache, DimensionCalculationService。")]
    public partial class SettingsForm : Form
    {
        // 导出路径配置变更事件
        public event EventHandler ExportPathSettingsChanged;
        
        /// <summary>
        /// 设置保存完成事件
        /// </summary>
        public event EventHandler SettingsSaved;
        
        /// <summary>
        /// 将对象序列化为美观的JSON字符串，提高配置文件的可读性
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <returns>格式化的JSON字符串</returns>
        /// <summary>
        /// 将对象序列化为JSON字符串，根据使用场景选择合适的格式化策略
        /// </summary>
        /// <param name="obj">要序列化的对象</param>
        /// <param name="useCompactFormat">是否使用紧凑格式（默认false，使用缩进格式）</param>
        /// <returns>JSON字符串</returns>
        private static string ToFormattedJson(object obj, bool useCompactFormat = false)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = useCompactFormat ? Formatting.None : Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Populate,
                    StringEscapeHandling = StringEscapeHandling.Default
                };
                
                return JsonConvert.SerializeObject(obj, settings);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"JSON序列化失败: {ex.Message}", ex);
                // 降级为简单序列化
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
        }
        
        /// <summary>
        /// 生成紧凑格式的JSON（用于节省空间）
        /// </summary>
        private static string ToCompactJson(object obj)
        {
            return ToFormattedJson(obj, true);
        }
        public Dictionary<string, string> RegexPatterns { get; private set; }
        public double PdfWidth { get; private set; }
        public double PdfHeight { get; private set; }
        private bool isRecordingToggle = false;
// 静态引用，用于保存MaterialSelectFormModern实例
        private static MaterialSelectFormModern _currentMaterialSelectForm;

        // 静态保存的布局计算结果，即使MaterialSelectFormModern关闭也能使用
        private static int _savedLayoutRows = 0;
        private static int _savedLayoutColumns = 0;
        
        // 实例引用，便于访问当前MaterialSelectFormModern实例
        private MaterialSelectFormModern MaterialSelectForm => _currentMaterialSelectForm;
        private static ICompositeColumnService _compositeColumnService;
        private readonly IPdfDimensionService _pdfDimensionService;
        private readonly IImpositionService _impositionService;

        // 分组相关字段
        private List<EventGroupConfig> _eventGroupConfigs;
        private List<Forms.Dialogs.EventItem> _eventItems;
        // 加载设置时的标志，防止事件触发自动保存
        private bool _isLoadingSettings = false;

        static SettingsForm()
        {
            // 初始化列组合服务
            _compositeColumnService = ServiceLocator.Instance.GetCompositeColumnService();
        }

        public SettingsForm()
        {
            // 初始化PDF尺寸服务，统一使用IText7PdfTools
            _pdfDimensionService = PdfDimensionServiceFactory.GetInstance();

            // 初始化排版服务
            _impositionService = new ImpositionService();

            InitializeComponent();

            // 初始化分组数据
            LoadEventGroupConfigs();

            // 启用拖拽排序功能
            chkLstTextItems.AllowDrop = true;

            // 初始化透明度滑块为保存的值
            trackBarOpacity.Value = (int)(AppSettings.Opacity * 100);

            // 初始化TreeView事件
            InitializeTreeViewEvents();
            LoadEventGroups();
            LoadEventItems();
            // LoadEventPresets() 移到 LoadSettings() 方法中，确保在窗体完全加载后再执行

            // 初始化文字添加功能
            chkLstTextItems.ItemCheck -= ChkLstTextItems_ItemCheck;

            // 添加分隔符控件的事件处理
            txtSeparator.Leave += TxtSeparator_Leave;
            LoadTextItems();
            chkLstTextItems.ItemCheck += ChkLstTextItems_ItemCheck;

            // 初始化组合内容
            UpdateTextCombo();

            // 初始化正则表达式管理
            RegexPatterns = new Dictionary<string, string>();
            InitializeDataGridView();
            LoadRegexPatterns();

            // 设置Esc键关闭窗口
            Button cancelButton = new Button { DialogResult = DialogResult.Cancel };
            cancelButton.Click += (sender, e) => this.Close();
            this.CancelButton = cancelButton;

            // 绑定FormClosing事件以保存设置
            this.FormClosing += SettingsForm_FormClosing;

        }

        private bool IsModifierKey(Keys key)
        {
            return key == Keys.ControlKey || key == Keys.Menu || key == Keys.ShiftKey;
        }

        #region 正则表达式管理功能



        private void InitializeDataGridView()
        {
            dgvRegex.AutoGenerateColumns = false;
            dgvRegex.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "正则名称", DataPropertyName = "Key", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "Pattern", HeaderText = "正则表达式", DataPropertyName = "Value", Width = 300 }
            );
        }

        private void BtnTestRegex_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtRegexPattern.Text))
                {
                    txtRegexTestResult.Text = "请先选择或输入正则表达式";
                    return;
                }

                string pattern = txtRegexPattern.Text;
                string input = txtRegexTestInput.Text;
                if (string.IsNullOrEmpty(input))
                {
                    txtRegexTestResult.Text = "请输入测试文本";
                    return;
                }

                var match = System.Text.RegularExpressions.Regex.Match(input, pattern);
                if (match.Success)
                {
                    string result = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    txtRegexTestResult.Text = $"匹配成功: {result}";
                }
                else
                {
                    txtRegexTestResult.Text = "未找到匹配项";
                }
            }
            catch (Exception ex)
            {
                txtRegexTestResult.Text = $"正则表达式错误: {ex.Message}";
            }
        }

        private void LoadRegexPatterns()
        {
            RegexPatterns.Clear();
            // 从应用设置加载正则表达式
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
            var bindingList = new BindingList<KeyValuePair<string, string>>(RegexPatterns.ToList());
            dgvRegex.DataSource = bindingList;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var name = txtRegexName.Text.Trim();
            var pattern = txtRegexPattern.Text.Trim();

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
            LoadRegexPatterns();
            ClearInputFields();
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
            LoadRegexPatterns();
            ClearInputFields();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // 保存所有设置
            SaveSettings();
            DialogResult = DialogResult.OK;
            Close();
        }
        
        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            // 只保存设置，不关闭窗口
            try
            {
                SaveSettings();
                MessageBox.Show("设置已保存！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存设置失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void DgvRegex_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvRegex.SelectedRows.Count > 0)
            {
                var selected = (KeyValuePair<string, string>)dgvRegex.SelectedRows[0].DataBoundItem;
                txtRegexName.Text = selected.Key;
                txtRegexPattern.Text = selected.Value;
            }
        }

        private void ClearInputFields()
        {
            txtRegexName.Clear();
            txtRegexPattern.Clear();
        }

        private void SaveRegexSettings()
        {
            // 保存正则表达式到应用设置
            AppSettings.RegexPatterns = string.Join("|", RegexPatterns.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        }
        
        /// <summary>
        /// 显示正则表达式管理界面
        /// </summary>
        public void ShowRegexManagement()
        {
            // 由于SettingsForm中没有TabControl控件，此方法留空或提供替代实现
            // 可以考虑滚动到正则表达式相关控件的位置
        }

        #endregion

        private void LoadTextItems()
        {
            // 清除现有项
            chkLstTextItems.Items.Clear();

            // 定义PDF文字添加功能的专用选项列表（独立于chkLstEvents）
            string[] textOnlyItems = { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号", "列组合" };

            // 从应用设置加载保存的文字项
            string savedItems = AppSettings.Get("TextItems") as string;
            var savedTextItemsDict = new Dictionary<string, bool>();

            if (!string.IsNullOrEmpty(savedItems))
            {
                string[] items = savedItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                // 确保数组长度为偶数（每项+状态）
                if (items.Length % 2 == 0)
                {
                    for (int i = 0; i < items.Length; i += 2)
                    {
                        string text = items[i];
                        // 只加载属于PDF文字功能的选项
                        if (Array.Exists(textOnlyItems, item => item == text))
                        {
                            bool isChecked = false;
                            bool.TryParse(items[i + 1], out isChecked);
                            savedTextItemsDict[text] = isChecked;
                        }
                    }
                }
            }

            // 加载PDF文字功能专用的选项
            foreach (string item in textOnlyItems)
            {
                bool isChecked = savedTextItemsDict.ContainsKey(item) ? savedTextItemsDict[item] : true;
                chkLstTextItems.Items.Add(item, isChecked);
            }

            // 如果配置中包含了不属于PDF文字功能的选项，需要清理并保存
            bool configNeedsCleanup = false;
            if (!string.IsNullOrEmpty(savedItems))
            {
                string[] items = savedItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < items.Length; i += 2)
                {
                    string text = items[i];
                    if (!Array.Exists(textOnlyItems, item => item == text))
                    {
                        configNeedsCleanup = true;
                        break;
                    }
                }
            }

            if (configNeedsCleanup)
            {
                // 保存清理后的TextItems配置
                SaveSettings();
            }

            // 加载保存的组合内容
            string savedCombo = AppSettings.Get("TextComboContent") as string;
            if (!string.IsNullOrEmpty(savedCombo))
            {
                txtTextCombo.Text = savedCombo;
            }
            else
            {
                UpdateTextCombo(); // 使用默认组合
            }
        }

        /// <summary>
        /// 初始化事件分组配置
        /// </summary>
        private void InitializeEventGroups()
        {
            _eventGroupConfigs = new List<EventGroupConfig>
            {
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Order, DisplayName = "订单组", Prefix = "&ID-", IsEnabled = true, SortOrder = 1 },
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Material, DisplayName = "材料组", Prefix = "&MT-", IsEnabled = true, SortOrder = 2 },
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Quantity, DisplayName = "数量组", Prefix = "&DN-", IsEnabled = true, SortOrder = 3 },
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Process, DisplayName = "工艺组", Prefix = "&DP-", IsEnabled = true, SortOrder = 4 },
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Customer, DisplayName = "客户组", Prefix = "&CU-", IsEnabled = true, SortOrder = 5 },
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Remark, DisplayName = "备注组", Prefix = "&MK-", IsEnabled = true, SortOrder = 6 },
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Row, DisplayName = "行数组", Prefix = "&Row-", IsEnabled = true, SortOrder = 7 },
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Column, DisplayName = "列数组", Prefix = "&Col-", IsEnabled = true, SortOrder = 8 },
                new EventGroupConfig { Group = Forms.Dialogs.EventGroup.Ungrouped, DisplayName = "未分组", Prefix = "", IsEnabled = true, SortOrder = 9 }
            };
        }

        /// <summary>
        /// 初始化TreeView事件
        /// </summary>
        private void InitializeTreeViewEvents()
        {
            treeViewEvents.TreeView.AfterCheck += TreeViewEvents_AfterCheck;
            treeViewEvents.TreeView.ItemDrag += TreeViewEvents_ItemDrag;
            treeViewEvents.TreeView.DragEnter += TreeViewEvents_DragEnter;
            treeViewEvents.TreeView.DragOver += TreeViewEvents_DragOver;
            treeViewEvents.TreeView.DragDrop += TreeViewEvents_DragDrop;
            treeViewEvents.TreeView.KeyDown += TreeViewEvents_KeyDown;

            // 初始化预设事件
            treeViewEvents.PresetSaved += TreeViewEvents_PresetSaved;
            treeViewEvents.PresetLoaded += TreeViewEvents_PresetLoaded;
            treeViewEvents.PresetDeleted += TreeViewEvents_PresetDeleted;
            treeViewEvents.PresetSaveAs += TreeViewEvents_PresetSaveAs;

            // 新增：保留状态变化事件
            if (treeViewEvents is EventGroupsTreeView eventGroupsTreeView)
            {
                eventGroupsTreeView.PreserveStateChanged += TreeViewEvents_PreserveStateChanged;
                eventGroupsTreeView.ConfigurationSaveRequested += (s, e) => SaveEventGroupConfigs();
            }
        }

        /// <summary>
        /// 加载事件分组到TreeView
        /// </summary>
        private void LoadEventGroups()
        {
            treeViewEvents.TreeView.Nodes.Clear();

            System.Diagnostics.Debug.WriteLine("[LoadEventGroups] 开始加载事件分组");

            var sortedGroups = _eventGroupConfigs.OrderBy(g => g.SortOrder).ToList();
            foreach (var groupConfig in sortedGroups)
            {
                // 构建显示文本：检查DisplayName是否已经包含前缀，避免重复添加
                var displayText = groupConfig.DisplayName;
                if (!string.IsNullOrEmpty(groupConfig.Prefix))
                {
                    // 只有当DisplayName不包含前缀时才添加前缀
                    if (!displayText.StartsWith(groupConfig.Prefix))
                    {
                        displayText = $"{groupConfig.Prefix} {displayText}";
                    }
                }

                var groupNode = new TreeNode(displayText);
                groupNode.Tag = new TreeNodeData
                {
                    NodeType = TreeNodeType.Group,
                    Group = groupConfig.Group,
                    IsEnabled = groupConfig.IsEnabled,
                    IsPreserved = groupConfig.IsPreserved
                };
                groupNode.Checked = groupConfig.IsEnabled;

                // 设置分组节点图标样式
                groupNode.NodeFont = new Font(treeViewEvents.TreeView.Font, FontStyle.Bold);

                treeViewEvents.TreeView.Nodes.Add(groupNode);
            }

            treeViewEvents.TreeView.ExpandAll();

            System.Diagnostics.Debug.WriteLine("[LoadEventGroups] 事件分组加载完成，开始调用 LogDragDropStatus");

            // 刷新保留状态视觉并检查启动时的冲突
            if (treeViewEvents is EventGroupsTreeView eventGroupsTreeView)
            {
                eventGroupsTreeView.RefreshPreserveVisuals();

                // 检查并自动清理启动时的保留分组冲突
                eventGroupsTreeView.CheckAndResolveStartupConflicts();

                // 添加调试：检查拖拽状态
                eventGroupsTreeView.LogDragDropStatus();
            }
        }

        /// <summary>
        /// 加载事件项目到TreeView
        /// </summary>
        private void LoadEventItems()
        {
            // 初始化事件项目
            _eventItems = new List<Forms.Dialogs.EventItem>();
            string[] allItems = { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号", "列组合", "行数", "列数" };

            // 从应用设置加载保存的项
            string savedItems = AppSettings.EventItems;
            var savedItemsDict = new Dictionary<string, bool>();

            if (!string.IsNullOrEmpty(savedItems))
            {
                string[] items = savedItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (items.Length % 2 == 0)
                {
                    for (int i = 0; i < items.Length; i += 2)
                    {
                        string text = items[i];
                        if (text == "固定字符") text = "工艺";
                        bool isChecked = false;
                        bool.TryParse(items[i + 1], out isChecked);
                        savedItemsDict[text] = isChecked;
                    }
                }
            }

            // 为每个项目创建EventItem并添加到对应分组
            int sortOrder = 1;
            foreach (string itemName in allItems)
            {
                // 根据项目名称确定所属分组
                Forms.Dialogs.EventGroup itemGroup = GetDefaultGroupForItem(itemName);

                var eventItem = new Forms.Dialogs.EventItem
                {
                    Name = itemName,
                    IsEnabled = savedItemsDict.ContainsKey(itemName) ? savedItemsDict[itemName] : true,
                    Group = itemGroup,
                    SortOrder = sortOrder++
                };

                _eventItems.Add(eventItem);

                // 添加到对应的分组节点
                var groupNode = FindGroupNode(itemGroup);
                if (groupNode != null)
                {
                    var itemNode = new TreeNode(itemName);
                    itemNode.Tag = new TreeNodeData
                    {
                        NodeType = TreeNodeType.Item,
                        Group = itemGroup,
                        ItemName = itemName,
                        IsEnabled = eventItem.IsEnabled
                    };
                    itemNode.Checked = eventItem.IsEnabled;
                    groupNode.Nodes.Add(itemNode);
                }
            }

            treeViewEvents.TreeView.ExpandAll();
        }

        /// <summary>
        /// 根据项目名称获取默认分组
        /// </summary>
        private Forms.Dialogs.EventGroup GetDefaultGroupForItem(string itemName)
        {
            return itemName switch
            {
                "订单号" => Forms.Dialogs.EventGroup.Order,
                "材料" => Forms.Dialogs.EventGroup.Material,
                "数量" => Forms.Dialogs.EventGroup.Quantity,
                "工艺" => Forms.Dialogs.EventGroup.Process,
                "行数" => Forms.Dialogs.EventGroup.Row,
                "列数" => Forms.Dialogs.EventGroup.Column,
                "正则结果" => Forms.Dialogs.EventGroup.Ungrouped,
                "尺寸" => Forms.Dialogs.EventGroup.Ungrouped,
                "序号" => Forms.Dialogs.EventGroup.Ungrouped,
                "列组合" => Forms.Dialogs.EventGroup.Ungrouped,
                _ => Forms.Dialogs.EventGroup.Ungrouped
            };
        }

        /// <summary>
        /// 初始化EventItems预设列表
        /// </summary>
        private void LoadEventPresets()
        {
            try
            {
                // 获取保存的预设名称列表
                var presetNames = GetEventItemsPresetNames();
                treeViewEvents.RefreshPresets(presetNames);

                string presetToLoad = null;

                // 加载上次选择的预设
                var lastSelectedPreset = AppSettings.Get("LastSelectedEventPreset") as string;
                if (!string.IsNullOrEmpty(lastSelectedPreset) && presetNames.Contains(lastSelectedPreset))
                {
                    presetToLoad = lastSelectedPreset;
                    LogHelper.Debug($"准备恢复上次选择的预设: {lastSelectedPreset}");
                }
                else if (presetNames.Count > 0)
                {
                    // 如果没有上次选择或上次选择不存在，默认选择第一个
                    presetToLoad = presetNames[0];
                    LogHelper.Debug($"准备默认选择第一个预设: {presetNames[0]}");
                }

                // 设置选择的预设并触发加载
                if (!string.IsNullOrEmpty(presetToLoad))
                {
                    // 设置选择，这会触发 SelectedIndexChanged 事件，进而触发 PresetLoaded 事件
                    // PresetLoaded 事件会自动调用 LoadEventItemsPreset 和相关的保存逻辑
                    treeViewEvents.SelectedPreset = presetToLoad;
                    LogHelper.Debug($"已设置预设选择: {presetToLoad}");
                }
                else
                {
                    LogHelper.Debug("没有可用的预设方案");
                }

                LogHelper.Debug($"已加载{presetNames.Count}个EventItems预设到下拉框");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载EventItems预设失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存预设按钮点击事件
        /// </summary>
        /// <summary>
        /// 保存预设按钮点击事件
        /// </summary>
        private void TreeViewEvents_PresetSaved(object sender, EventArgs e)
        {
            try
            {
                // 获取当前下拉框选择的预设名称
                string selectedPreset = treeViewEvents.SelectedPreset;
                if (string.IsNullOrEmpty(selectedPreset))
                {
                    MessageBox.Show("请先选择要保存的预设方案", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 获取当前TreeView状态
                var currentConfig = GetCurrentEventGroupsConfiguration();
                if (SaveEventItemsPreset(selectedPreset, currentConfig))
                {
                    // 刷新预设列表
                    LoadEventPresets();
                    treeViewEvents.SelectedPreset = selectedPreset;

                    LogHelper.Debug($"已保存EventItems预设到: {selectedPreset}");
                    MessageBox.Show($"预设 '{selectedPreset}' 已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存EventItems预设失败: {ex.Message}", ex);
                MessageBox.Show($"保存预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保留状态变化事件处理
        /// </summary>
        private void TreeViewEvents_PreserveStateChanged(object sender, PreserveStateChangedEventArgs e)
        {
            try
            {
                // 更新对应的EventGroupConfig
                var groupConfig = _eventGroupConfigs.FirstOrDefault(g => g.Group == e.Group);
                if (groupConfig != null)
                {
                    groupConfig.IsPreserved = e.IsPreserved;
                    LogHelper.Debug($"分组 {e.Group} 保留状态变更为: {(e.IsPreserved ? "保留" : "不保留")} 来源: {e.Source}");
                }

                // 触发保存
                SaveEventGroupConfigs();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理保留状态变化事件失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载预设按钮点击事件
        /// </summary>
        private void TreeViewEvents_PresetLoaded(object sender, EventArgs e)
        {
            try
            {
                string presetName = treeViewEvents.SelectedPreset;
                if (string.IsNullOrEmpty(presetName))
                    return;

                if (LoadEventItemsPreset(presetName))
                {
                    // 同步更新_eventItems列表，确保与TreeView保持一致
                    SyncEventItemsFromTreeView();

                    // 保存设置到AppSettings.EventItems，确保Form1状态栏显示正确
                    SaveSettings();

                    // 保存当前选择的预设
                    AppSettings.Set("LastSelectedEventPreset", presetName);

                    // 更新Form1状态栏显示的配置名为当前EventItems预设名
                    AppSettings.LastUsedConfigName = presetName;

                    AppSettings.Save();

                    LogHelper.Debug($"已应用EventItems预设: {presetName}");
                    LogHelper.Debug($"已保存当前选择的预设: {presetName}");
                    LogHelper.Debug($"已更新Form1配置名为: {presetName}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"应用EventItems预设失败: {ex.Message}", ex);
                MessageBox.Show($"应用预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 删除预设按钮点击事件
        /// </summary>
        private void TreeViewEvents_PresetDeleted(object sender, EventArgs e)
        {
            try
            {
                string presetName = treeViewEvents.SelectedPreset;
                if (string.IsNullOrEmpty(presetName))
                {
                    MessageBox.Show("请先选择要删除的预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (MessageBox.Show(
                    $"确定要删除预设 '{presetName}' 吗？",
                    "确认删除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (DeleteEventItemsPreset(presetName))
                    {
                        LoadEventPresets();
                        LogHelper.Debug($"已删除EventItems预设: {presetName}");
                        MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("内置预设不能删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"删除EventItems预设失败: {ex.Message}", ex);
                MessageBox.Show($"删除预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

private void TreeViewEvents_PresetSaveAs(object sender, EventArgs e)
        {
            try
            {
                // 使用输入框获取新的预设名称
                using (var inputForm = new Form())
                {
                    inputForm.Text = "另存预设方案";
                    inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    inputForm.StartPosition = FormStartPosition.CenterParent;
                    inputForm.MaximizeBox = false;
                    inputForm.MinimizeBox = false;
                    inputForm.Width = 300;
                    inputForm.Height = 150;

                    var label = new Label
                    {
                        Text = "请输入预设方案名称:",
                        Location = new Point(20, 20),
                        Width = 250,
                        Font = new Font("微软雅黑", 9F)
                    };

                    var textBox = new TextBox
                    {
                        Location = new Point(20, 50),
                        Width = 250,
                        Font = new Font("微软雅黑", 9F)
                    };

                    var okButton = new Button
                    {
                        Text = "确定",
                        DialogResult = DialogResult.OK,
                        Location = new Point(105, 80),
                        Width = 80,
                        Font = new Font("微软雅黑", 9F)
                    };

                    var cancelButton = new Button
                    {
                        Text = "取消",
                        DialogResult = DialogResult.Cancel,
                        Location = new Point(190, 80),
                        Width = 80,
                        Font = new Font("微软雅黑", 9F)
                    };

                    inputForm.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
                    inputForm.AcceptButton = okButton;
                    inputForm.CancelButton = cancelButton;

                    if (inputForm.ShowDialog(this) == DialogResult.OK)
                    {
                        string presetName = textBox.Text.Trim();
                        if (!string.IsNullOrEmpty(presetName))
                        {
                            // 检查预设名称是否已存在
                            var existingPresets = GetEventItemsPresetNames();
                            if (existingPresets.Contains(presetName))
                            {
                                if (MessageBox.Show($"预设方案 '{presetName}' 已存在，是否覆盖？", "确认覆盖", 
                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                                {
                                    return;
                                }
                            }

                            // 保存当前配置为新预设
                            var currentConfig = GetCurrentEventGroupsConfiguration();
                            if (SaveEventItemsPreset(presetName, currentConfig))
                            {
                                // 刷新预设列表
                                LoadEventPresets();
                                // 选中新创建的预设
                                treeViewEvents.SelectedPreset = presetName;
                                
                                LogHelper.Info($"成功另存预设方案: {presetName}");
                            }
                            else
                            {
                                MessageBox.Show("保存预设方案失败", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("预设方案名称不能为空", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"另存预设方案失败: {ex.Message}", ex);
                MessageBox.Show($"另存预设方案失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        /// <summary>
        /// 滚动到指定的设置区域（用于导航跳转）
        /// </summary>
        /// <param name="sectionKey">区域标识</param>
        public void ScrollToSection(string sectionKey)
        {
            Control targetControl = null;
            switch (sectionKey)
            {
                case "settings_regex":
                    targetControl = dgvRegex;
                    break;
                case "settings_material":
                    targetControl = grpImpositionSettings; 
                    break;
                case "settings_path":
                    targetControl = grpExportPaths;
                    break;
                case "settings_general":
                default:
                    targetControl = this.Controls[0]; // 顶部
                    break;
            }

            if (targetControl != null)
            {
                // 尝试滚动到控件
                this.AutoScrollPosition = new Point(
                    Math.Abs(this.AutoScrollPosition.X),
                    targetControl.Top - 20 // 留出一点顶部边距
                );
                
                // 高亮闪烁一下? (可选)
                targetControl.Focus();
            }
        }
        private void SaveEventGroupConfigs()
        {
            try
            {
                // 使用紧凑格式保存分组配置，节省存储空间
                var groupConfigsJson = ToCompactJson(_eventGroupConfigs);
                AppSettings.Set("EventGroupConfigs", groupConfigsJson);
                LogHelper.Debug($"已保存{_eventGroupConfigs.Count}个分组配置（紧凑格式）");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存分组配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载分组配置状态
        /// </summary>
        private void LoadEventGroupConfigs()
        {
            try
            {
                string groupConfigsJson = AppSettings.Get("EventGroupConfigs") as string;
                if (!string.IsNullOrEmpty(groupConfigsJson))
                {
                    _eventGroupConfigs = JsonConvert.DeserializeObject<List<EventGroupConfig>>(groupConfigsJson);
                    if (_eventGroupConfigs == null)
                    {
                        _eventGroupConfigs = new List<EventGroupConfig>();
                    }
                    LogHelper.Debug($"已加载{_eventGroupConfigs.Count}个分组配置");
                }
                else
                {
                    InitializeEventGroups(); // 使用默认配置
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载分组配置失败: {ex.Message}", ex);
                InitializeEventGroups(); // 使用默认配置
            }
        }

        /// <summary>
        /// 获取EventItems预设名称列表
        /// </summary>
        private List<string> GetEventItemsPresetNames()
        {
            var presetNames = new List<string>();

            try
            {
                LogHelper.Debug("开始获取预设名称列表");

                // 添加内置预设
                presetNames.Add("全功能配置");
                LogHelper.Debug("已添加内置预设");

                // 获取自定义预设 - 使用不同的键名避免与EventItemsPresets类冲突
                var customPresetsValue = AppSettings.Get("EventItemsCustomPresets");
                if (customPresetsValue != null && customPresetsValue is string customPresetsString)
                {
                    LogHelper.Debug($"获取到自定义预设字符串: {customPresetsString}");
                    
                    if (!string.IsNullOrEmpty(customPresetsString))
                    {
                        var customList = customPresetsString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var preset in customList)
                        {
                            // 过滤掉已删除的内建预设
                            if (!string.IsNullOrEmpty(preset) &&
                                !presetNames.Contains(preset) &&
                                preset != "默认配置" && preset != "基础配置")
                            {
                                presetNames.Add(preset);
                                LogHelper.Debug($"添加自定义预设: {preset}");
                            }
                            else if (preset == "默认配置" || preset == "基础配置")
                            {
                                LogHelper.Debug($"过滤掉已删除的内建预设: {preset}");
                            }
                        }
                    }
                }
                else
                {
                    LogHelper.Debug("EventItemsCustomPresets为空或类型不正确");
                }

                LogHelper.Debug($"预设名称列表获取完成，共{presetNames.Count}个预设");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取预设名称列表失败: {ex.Message}", ex);
                // 即使失败也返回内置预设
                if (presetNames.Count == 0)
                {
                    presetNames.Add("全功能配置");
                }
            }

            return presetNames;
        }

        /// <summary>
        /// 获取当前EventGroups配置
        /// </summary>
        private EventGroupConfiguration GetCurrentEventGroupsConfiguration()
        {
            var config = new EventGroupConfiguration
            {
                Groups = new List<Models.EventGroup>(),
                Items = new List<Models.EventItem>()
            };

            try
            {
                LogHelper.Debug("开始收集TreeView当前状态");

                // 收集当前TreeView状态
                foreach (TreeNode groupNode in treeViewEvents.TreeView.Nodes)
                {
                    var groupData = groupNode.Tag as TreeNodeData;
                    if (groupData?.NodeType == TreeNodeType.Group)
                    {
                        // 跳过Ungrouped节点，稍后单独处理
                        if (groupData.Group == Forms.Dialogs.EventGroup.Ungrouped)
                            continue;

                        // 从配置中获取对应的前缀和分组ID
                        var groupConfig = _eventGroupConfigs.FirstOrDefault(g => g.Group == groupData.Group.Value);
                        var prefix = groupConfig?.Prefix ?? "";
                        var groupId = "";

                        // 根据分组类型设置对应的ID
                        switch (groupData.Group.Value)
                        {
                            case Forms.Dialogs.EventGroup.Order:
                                groupId = "order";
                                break;
                            case Forms.Dialogs.EventGroup.Material:
                                groupId = "material";
                                break;
                            case Forms.Dialogs.EventGroup.Quantity:
                                groupId = "quantity";
                                break;
                            case Forms.Dialogs.EventGroup.Process:
                                groupId = "process";
                                break;
                            case Forms.Dialogs.EventGroup.Customer:
                                groupId = "customer";
                                break;
                            case Forms.Dialogs.EventGroup.Remark:
                                groupId = "remark";
                                break;
                            case Forms.Dialogs.EventGroup.Row:
                                groupId = "row";
                                break;
                            case Forms.Dialogs.EventGroup.Column:
                                groupId = "column";
                                break;
                        }

                        // 提取原始分组名称（移除前缀和保留标识）
                        string displayName = groupNode.Text;
                        if (!string.IsNullOrEmpty(prefix) && displayName.StartsWith(prefix))
                        {
                            displayName = displayName.Substring(prefix.Length).Trim();
                        }

                        // 移除保留标识前缀
                        if (displayName.StartsWith("[保留]"))
                        {
                            displayName = displayName.Substring(4).Trim();
                        }

                        // 保存分组配置（包括所有分组，无论是否勾选）
                        config.Groups.Add(new Models.EventGroup
                        {
                            Id = groupId,
                            DisplayName = displayName,  // 保存纯的分组名称
                            Prefix = prefix,
                            IsEnabled = groupNode.Checked,  // 保存分组的勾选状态
                            SortOrder = groupNode.Index,
                            IsPreserved = groupData.IsPreserved  // 保存分组的保留状态
                        });

                        // 保存该分组下的所有项目（无论是否勾选）
                        foreach (TreeNode itemNode in groupNode.Nodes)
                        {
                            config.Items.Add(new Models.EventItem
                            {
                                Name = itemNode.Text,
                                GroupId = groupId,
                                IsEnabled = itemNode.Checked,  // 保存项目的勾选状态
                                SortOrder = itemNode.Index
                            });
                        }
                    }
                }

                // 处理未分组项目
                var ungroupedNode = treeViewEvents.TreeView.Nodes.Cast<TreeNode>()
                    .FirstOrDefault(n => (n.Tag as TreeNodeData)?.NodeType == TreeNodeType.Group &&
                                       (n.Tag as TreeNodeData)?.Group == Forms.Dialogs.EventGroup.Ungrouped);

                if (ungroupedNode != null)
                {
                    // 保存未分组配置
                    config.Groups.Add(new Models.EventGroup
                    {
                        Id = "ungrouped",
                        DisplayName = "未分组",
                        Prefix = "",
                        IsEnabled = ungroupedNode.Checked,  // 保存未分组分组的勾选状态
                        SortOrder = ungroupedNode.Index
                    });

                    // 保存所有未分组项目（无论是否勾选）
                    foreach (TreeNode itemNode in ungroupedNode.Nodes)
                    {
                        config.Items.Add(new Models.EventItem
                        {
                            Name = itemNode.Text,
                            GroupId = "ungrouped",
                            IsEnabled = itemNode.Checked,  // 保存项目的勾选状态
                            SortOrder = itemNode.Index
                        });
                    }
                }

                LogHelper.Debug($"TreeView状态收集完成，分组数: {config.Groups.Count}，项目数: {config.Items.Count}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"收集TreeView状态失败: {ex.Message}", ex);
                throw;
            }

            return config;
        }

        /// <summary>
        /// 应用EventGroups配置
        /// </summary>
        private void ApplyEventGroupsConfiguration(EventGroupConfiguration config)
        {
            try
            {
                // 检查配置是否为 null
                if (config == null)
                {
                    LogHelper.Error("EventGroupsConfiguration 配置对象为 null");
                    return;
                }

                // 清空当前TreeView
                treeViewEvents.TreeView.Nodes.Clear();

                // 检查分组是否为 null，并按SortOrder排序分组
                if (config.Groups == null)
                {
                    LogHelper.Error("EventGroupsConfiguration.Groups 为 null");
                    return;
                }
                var sortedGroups = config.Groups.Where(g => g != null).OrderBy(g => g.SortOrder).ToList();

                foreach (var group in sortedGroups)
                {
                    // 创建分组节点 - 修复Enum.Parse用法
                    Forms.Dialogs.EventGroup eventGroup;
                    if (group.Id == "order") eventGroup = Forms.Dialogs.EventGroup.Order;
                    else if (group.Id == "material") eventGroup = Forms.Dialogs.EventGroup.Material;
                    else if (group.Id == "quantity") eventGroup = Forms.Dialogs.EventGroup.Quantity;
                    else if (group.Id == "process") eventGroup = Forms.Dialogs.EventGroup.Process;
                    else if (group.Id == "customer") eventGroup = Forms.Dialogs.EventGroup.Customer;
                    else if (group.Id == "remark") eventGroup = Forms.Dialogs.EventGroup.Remark;
                    else if (group.Id == "row") eventGroup = Forms.Dialogs.EventGroup.Row;
                    else if (group.Id == "column") eventGroup = Forms.Dialogs.EventGroup.Column;
                    else if (group.Id == "ungrouped") eventGroup = Forms.Dialogs.EventGroup.Ungrouped;
                    else eventGroup = Forms.Dialogs.EventGroup.Ungrouped;

                    // 构建显示文本：检查DisplayName是否已经包含前缀，避免重复添加
                    var displayText = group.DisplayName;
                    if (!string.IsNullOrEmpty(group.Prefix))
                    {
                        // 只有当DisplayName不包含前缀时才添加前缀
                        if (!displayText.StartsWith(group.Prefix))
                        {
                            // 使用空格分隔，与LoadEventGroups保持一致
                            displayText = $"{group.Prefix} {displayText}";
                        }
                    }

                    var groupNode = new TreeNode(displayText)
                    {
                        Tag = new TreeNodeData
                        {
                            NodeType = TreeNodeType.Group,
                            Group = eventGroup,
                            IsEnabled = group.IsEnabled,
                            IsPreserved = group.IsPreserved  // 恢复保留分组状态
                        }
                    };

                    // 设置分组节点的勾选状态
                    groupNode.Checked = group.IsEnabled;

                    // 检查项目列表是否为 null
                    if (config.Items == null)
                    {
                        LogHelper.Error("EventGroupsConfiguration.Items 为 null");
                        continue; // 跳过这个分组，继续处理下一个
                    }

                    // 添加该分组的所有项目（无论是否启用）
                    var groupItems = config.Items
                        .Where(i => i != null && i.GroupId == group.Id)
                        .OrderBy(i => i.SortOrder)
                        .ToList();

                    foreach (var item in groupItems)
                    {
                        var itemNode = new TreeNode(item.Name)
                        {
                            Tag = new TreeNodeData
                            {
                                NodeType = TreeNodeType.Item,
                                ItemName = item.Name,
                                IsEnabled = item.IsEnabled
                            }
                        };

                        // 设置项目节点的勾选状态
                        itemNode.Checked = item.IsEnabled;

                        groupNode.Nodes.Add(itemNode);
                    }

                    treeViewEvents.TreeView.Nodes.Add(groupNode);
                }

                // 处理没有在配置中找到的未分组项目（兼容性处理）
                var ungroupedNodeInTree = treeViewEvents.TreeView.Nodes.Cast<TreeNode>()
                    .FirstOrDefault(n => (n.Tag as TreeNodeData)?.NodeType == TreeNodeType.Group &&
                                       (n.Tag as TreeNodeData)?.Group == Forms.Dialogs.EventGroup.Ungrouped);

                if (ungroupedNodeInTree == null)
                {
                    // 如果没有未分组节点，检查是否有未分组的项目需要显示
                    var ungroupedItems = config.Items
                        .Where(i => string.IsNullOrEmpty(i.GroupId) || i.GroupId == "ungrouped")
                        .OrderBy(i => i.SortOrder)
                        .ToList();

                    if (ungroupedItems.Any())
                    {
                        var ungroupedNode = new TreeNode("未分组")
                        {
                            Tag = new TreeNodeData
                            {
                                NodeType = TreeNodeType.Group,
                                Group = Forms.Dialogs.EventGroup.Ungrouped,
                                IsEnabled = true
                            }
                        };

                        foreach (var item in ungroupedItems)
                        {
                            var itemNode = new TreeNode(item.Name)
                            {
                                Tag = new TreeNodeData
                                {
                                    NodeType = TreeNodeType.Item,
                                    ItemName = item.Name,
                                    IsEnabled = item.IsEnabled
                                }
                            };

                            // 设置项目节点的勾选状态
                            itemNode.Checked = item.IsEnabled;

                            ungroupedNode.Nodes.Add(itemNode);
                        }

                        treeViewEvents.TreeView.Nodes.Add(ungroupedNode);
                    }
                }

                // 展开所有节点
                treeViewEvents.TreeView.ExpandAll();

                // 刷新保留状态视觉
                if (treeViewEvents is EventGroupsTreeView eventGroupsTreeView)
                {
                    eventGroupsTreeView.RefreshPreserveVisuals();
                }

                LogHelper.Info("EventGroups配置应用成功");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"应用EventGroups配置失败: {ex.Message}", ex);
                MessageBox.Show($"应用配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存EventItems预设
        /// </summary>
        private bool SaveEventItemsPreset(string presetName, EventGroupConfiguration config)
        {
            try
            {
                LogHelper.Debug($"开始保存预设方案: {presetName}");
                
                if (string.IsNullOrWhiteSpace(presetName))
                {
                    LogHelper.Error("预设名称不能为空");
                    return false;
                }

                // 智能格式化策略：
                // - 内置预设（全功能配置）使用紧凑格式
                // - 用户自定义预设使用可读格式
                var builtinPresets = new[] { "全功能配置" };
                bool isBuiltinPreset = builtinPresets.Contains(presetName);
                
                string configJson = isBuiltinPreset 
                    ? ToCompactJson(config) 
                    : ToFormattedJson(config);
                    
                string formatType = isBuiltinPreset ? "紧凑格式" : "可读格式";
                LogHelper.Debug($"预设数据序列化成功（{formatType}），长度: {configJson.Length}");
                
                AppSettings.Instance.SetValue($"EventItemsPreset_{presetName}", configJson);
                LogHelper.Debug($"预设数据保存成功: EventItemsPreset_{presetName}（{formatType}）");

                // 更新预设名称列表 - 使用新的键名避免冲突
                var currentPresets = GetEventItemsPresetNames();
                LogHelper.Debug($"当前预设数量: {currentPresets.Count}");
                
                if (!currentPresets.Contains(presetName))
                {
                    var updatedPresets = new List<string>(currentPresets) { presetName };
                    string presetsList = string.Join("|", updatedPresets);
                    AppSettings.Instance.SetValue("EventItemsCustomPresets", presetsList);
                    LogHelper.Debug($"预设名称列表更新成功: {presetsList}");
                }

                LogHelper.Debug($"预设方案保存完成: {presetName}");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存预设方案失败: {presetName} - {ex.Message}", ex);
                MessageBox.Show($"保存预设方案失败: {ex.Message}", "保存错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 加载EventItems预设
        /// </summary>
        private bool LoadEventItemsPreset(string presetName)
        {
            try
            {
                // 内置预设使用全功能配置
                if (presetName == "全功能配置")
                {
                    // 使用完整默认配置
                    var fullConfig = DefaultEventGroups.GetDefaultConfiguration();

                    // 检查默认配置是否为 null
                    if (fullConfig?.Groups == null || fullConfig?.Items == null)
                    {
                        LogHelper.Error("完整默认配置为 null 或缺少 Groups/Items 数据");
                        return false;
                    }

                    ApplyEventGroupsConfiguration(fullConfig);
                    return true;
                }
                else
                {
                    // 加载自定义预设
                    string presetKey = $"EventItemsPreset_{presetName}";
                    string configJson = AppSettings.Get(presetKey) as string;

                    if (!string.IsNullOrEmpty(configJson))
                    {
                        var config = JsonConvert.DeserializeObject<EventGroupConfiguration>(configJson);
                        if (config != null)
                        {
                            ApplyEventGroupsConfiguration(config);
                            return true;
                        }
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载EventItems预设失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 从TreeView同步事件项目到_eventItems列表
        /// </summary>
        private void SyncEventItemsFromTreeView()
        {
            try
            {
                // 清空当前的_eventItems列表
                _eventItems.Clear();

                // 遍历TreeView中的所有节点
                foreach (TreeNode groupNode in treeViewEvents.TreeView.Nodes)
                {
                    var groupData = groupNode.Tag as TreeNodeData;
                    if (groupData?.NodeType == TreeNodeType.Group && groupData.Group.HasValue)
                    {
                        // 遍历分组下的所有项目
                        foreach (TreeNode itemNode in groupNode.Nodes)
                        {
                            var itemData = itemNode.Tag as TreeNodeData;
                            if (itemData?.NodeType == TreeNodeType.Item)
                            {
                                var eventItem = new Forms.Dialogs.EventItem
                                {
                                    Name = itemData.ItemName,
                                    Group = groupData.Group.Value,
                                    IsEnabled = itemData.IsEnabled,
                                    SortOrder = _eventItems.Count + 1
                                };
                                _eventItems.Add(eventItem);
                            }
                        }
                    }
                }

                // 同步更新分组配置
                _eventGroupConfigs.Clear();
                foreach (TreeNode groupNode in treeViewEvents.TreeView.Nodes)
                {
                    var groupData = groupNode.Tag as TreeNodeData;
                    if (groupData?.NodeType == TreeNodeType.Group && groupData.Group.HasValue)
                    {
                        // 从显示文本中提取前缀和实际名称
                        var (prefix, displayName) = ExtractPrefixAndDisplayName(groupNode.Text, groupData.Group.Value);

                        var groupConfig = new EventGroupConfig
                        {
                            Group = groupData.Group.Value,
                            DisplayName = displayName,
                            Prefix = prefix,
                            IsEnabled = groupData.IsEnabled,
                            SortOrder = _eventGroupConfigs.Count + 1
                        };
                        _eventGroupConfigs.Add(groupConfig);
                    }
                }

                LogHelper.Debug($"已从TreeView同步 {_eventItems.Count} 个事件项目和 {_eventGroupConfigs.Count} 个分组配置");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"从TreeView同步事件项目失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从显示文本中提取前缀和实际名称
        /// </summary>
        private (string prefix, string displayName) ExtractPrefixAndDisplayName(string fullText, Forms.Dialogs.EventGroup group)
        {
            if (string.IsNullOrEmpty(fullText))
                return (string.Empty, string.Empty);

            // 根据分组类型确定标准前缀格式
            string standardPrefix = group switch
            {
                Forms.Dialogs.EventGroup.Customer => "&CU-",
                Forms.Dialogs.EventGroup.Remark => "&MK-",
                Forms.Dialogs.EventGroup.Order => "&ID-",
                Forms.Dialogs.EventGroup.Material => "&MT-",
                Forms.Dialogs.EventGroup.Quantity => "&DN-",
                Forms.Dialogs.EventGroup.Process => "&DP-",
                Forms.Dialogs.EventGroup.Row => "&Row-",
                Forms.Dialogs.EventGroup.Column => "&Col-",
                _ => string.Empty
            };

            // 检查文本是否以标准前缀开头
            if (fullText.StartsWith(standardPrefix))
            {
                // 提取前缀后的实际名称
                var actualName = fullText.Substring(standardPrefix.Length).Trim();
                return (standardPrefix, actualName);
            }

            // 检查是否包含标准前缀+空格的情况（如"&CU- 客户组"）
            if (fullText.StartsWith(standardPrefix + " "))
            {
                // 提取前缀和空格后的实际名称
                var actualName = fullText.Substring(standardPrefix.Length + 1).Trim();
                return (standardPrefix, actualName);
            }

            // 检查是否包含中文前缀（如"客户："），用于兼容性处理
            string chinesePrefix = group switch
            {
                Forms.Dialogs.EventGroup.Customer => "客户",
                Forms.Dialogs.EventGroup.Remark => "备注",
                Forms.Dialogs.EventGroup.Order => "订单",
                Forms.Dialogs.EventGroup.Material => "材料",
                Forms.Dialogs.EventGroup.Quantity => "数量",
                Forms.Dialogs.EventGroup.Process => "工艺",
                Forms.Dialogs.EventGroup.Row => "行",
                Forms.Dialogs.EventGroup.Column => "列",
                _ => string.Empty
            };

            if (fullText.StartsWith(chinesePrefix + ":"))
            {
                var actualName = fullText.Substring(chinesePrefix.Length + 1).Trim();
                // 返回标准前缀格式
                return (standardPrefix, actualName);
            }

            // 如果没有前缀，返回标准前缀和原始文本
            return (standardPrefix, fullText);
        }

        /// <summary>
        /// 删除EventItems预设
        /// </summary>
        private bool DeleteEventItemsPreset(string presetName)
        {
            try
            {
                // 内置预设不能删除
                var builtinPresets = new[] { "全功能配置" };
                if (builtinPresets.Contains(presetName))
                {
                    return false;
                }

                // 删除预设数据
                string presetKey = $"EventItemsPreset_{presetName}";
                AppSettings.Set(presetKey, null);

                // 从列表中移除 - 使用新的键名避免冲突
                var customPresets = AppSettings.Get("EventItemsCustomPresets") as string ?? "";
                var customList = new List<string>();
                if (!string.IsNullOrEmpty(customPresets))
                {
                    customList = customPresets.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }
                customList.Remove(presetName);
                AppSettings.Set("EventItemsCustomPresets", string.Join("|", customList));

                AppSettings.Save();
                LogHelper.Info($"成功删除预设: {presetName}");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"删除EventItems预设失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 根据分组类型查找分组节点
        /// </summary>
        private TreeNode FindGroupNode(Forms.Dialogs.EventGroup group)
        {
            foreach (TreeNode node in treeViewEvents.TreeView.Nodes)
            {
                var data = node.Tag as TreeNodeData;
                if (data != null && data.NodeType == TreeNodeType.Group && data.Group == group)
                {
                    return node;
                }
            }
            return null;
        }

        private void SaveSettings()
        {
            // ✅ 修复：在加载设置期间禁止自动保存
            if (_isLoadingSettings)
            {
                LogHelper.Debug("[SaveSettings] 当前正在加载设置，跳过自动保存");
                return;
            }
            
            // 移除了旧的管道格式存储，现在只使用新的JSON分组系统
            // 保存分组配置状态（JSON格式）
            SaveEventGroupConfigs();

            // 保存文字项设置
            List<string> textItemList = new List<string>();
            foreach (var item in chkLstTextItems.Items)
            {
                int index = chkLstTextItems.Items.IndexOf(item);
                textItemList.Add(item.ToString());
                textItemList.Add(chkLstTextItems.GetItemChecked(index).ToString());
            }
            AppSettings.Set("TextItems", string.Join("|", textItemList));

            // 保存分隔符设置 - 只在明确的保存操作中处理
            if (!string.IsNullOrEmpty(txtSeparator.Text))
            {
                AppSettings.Set("Separator", txtSeparator.Text);
            }

            // ✅ 修复：保存材料设置
            if (!string.IsNullOrEmpty(txtMaterial.Text))
            {
                AppSettings.Material = txtMaterial.Text.Trim();
                LogHelper.Debug($"[SaveSettings] 保存Material: '{txtMaterial.Text.Trim()}'");
            }

            // ✅ 修复：保存隐藏半径数值复选框状态
            AppSettings.HideRadiusValue = chkHideRadiusValue.Checked;
            LogHelper.Debug($"[SaveSettings] 保存HideRadiusValue: {chkHideRadiusValue.Checked}");

            // 立即保存到文件，确保设置持久化
            AppSettings.Save();
            LogHelper.Debug("[SaveSettings] 设置已立即保存到文件");

            LogHelper.Debug("事件分组设置已保存（仅JSON格式）");
            
            // 触发设置保存事件
            SettingsSaved?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 分隔符控件失去焦点时的处理
        /// </summary>
        private void TxtSeparator_Leave(object sender, EventArgs e)
        {
            try
            {
                // 当用户离开分隔符控件时，保存分隔符设置
                LogHelper.Debug("[TxtSeparator_Leave] 分隔符控件失去焦点，准备保存设置");
                SaveSeparator();
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[TxtSeparator_Leave] 保存分隔符设置时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 专门保存分隔符设置的方法，支持空分隔符
        /// </summary>
        private void SaveSeparator()
        {
            var separatorValue = txtSeparator.Text?.Trim() ?? "";
            var currentSeparator = AppSettings.Separator ?? "";
            LogHelper.Debug($"[SaveSeparator] 准备保存Separator: '{separatorValue}' (当前AppSettings.Separator: '{currentSeparator}')");

            if (separatorValue != currentSeparator)
            {
                AppSettings.Separator = separatorValue;
                LogHelper.Debug($"[SaveSeparator] Separator已保存: '{AppSettings.Separator}'{(string.IsNullOrEmpty(AppSettings.Separator) ? " (空分隔符)" : "")}");

                // 立即保存到文件
                AppSettings.Save();
                LogHelper.Debug("[SaveSeparator] Separator设置已立即持久化到文件");
            }
            else
            {
                LogHelper.Debug("[SaveSeparator] Separator值无变化，跳过保存");
            }
        }

/// <summary>
        /// 保存印刷排版设置
        /// </summary>
        /// <summary>
        /// 保存印刷排版设置
        /// </summary>
        private void SaveImpositionSettings()
        {
            try
            {
                LogHelper.Debug("[SaveImpositionSettings] 开始保存印刷排版设置");

                // 保存材料尺寸设置（平张材料）
                if (txtPaperWidth != null && !string.IsNullOrEmpty(txtPaperWidth.Text))
                {
                    AppSettings.Set("Imposition_PaperWidth", txtPaperWidth.Text.Trim());
                    LogHelper.Debug($"[SaveImpositionSettings] 保存纸张宽度: {txtPaperWidth.Text}");
                }

                if (txtPaperHeight != null && !string.IsNullOrEmpty(txtPaperHeight.Text))
                {
                    AppSettings.Set("Imposition_PaperHeight", txtPaperHeight.Text.Trim());
                    LogHelper.Debug($"[SaveImpositionSettings] 保存纸张高度: {txtPaperHeight.Text}");
                }

                // 根据分组控件可见性分别保存边距设置
                if (grpFlatSheetSettings != null && grpFlatSheetSettings.Visible)
                {
                    // 保存平张材料边距设置
                    if (txtMarginTop != null && !string.IsNullOrEmpty(txtMarginTop.Text))
                    {
                        AppSettings.Set("Imposition_MarginTop", txtMarginTop.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettings] 保存平张材料顶部边距: {txtMarginTop.Text}");
                    }

                    if (txtMarginBottom != null && !string.IsNullOrEmpty(txtMarginBottom.Text))
                    {
                        AppSettings.Set("Imposition_MarginBottom", txtMarginBottom.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettings] 保存平张材料底部边距: {txtMarginBottom.Text}");
                    }

                    if (txtMarginLeft != null && !string.IsNullOrEmpty(txtMarginLeft.Text))
                    {
                        AppSettings.Set("Imposition_MarginLeft", txtMarginLeft.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettings] 保存平张材料左侧边距: {txtMarginLeft.Text}");
                    }

                    if (txtMarginRight != null && !string.IsNullOrEmpty(txtMarginRight.Text))
                    {
                        AppSettings.Set("Imposition_MarginRight", txtMarginRight.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettings] 保存平张材料右侧边距: {txtMarginRight.Text}");
                    }
                }

                if (grpRollMaterialSettings != null && grpRollMaterialSettings.Visible)
                {
                    // 保存卷装材料专用边距设置
                    if (txtRollMarginTop != null && !string.IsNullOrEmpty(txtRollMarginTop.Text))
                    {
                        AppSettings.Set("Imposition_RollMarginTop", txtRollMarginTop.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettings] 保存卷装材料顶部边距: {txtRollMarginTop.Text}");
                    }

                    if (txtRollMarginBottom != null && !string.IsNullOrEmpty(txtRollMarginBottom.Text))
                    {
                        AppSettings.Set("Imposition_RollMarginBottom", txtRollMarginBottom.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettings] 保存卷装材料底部边距: {txtRollMarginBottom.Text}");
                    }

                    if (txtRollMarginLeft != null && !string.IsNullOrEmpty(txtRollMarginLeft.Text))
                    {
                        AppSettings.Set("Imposition_RollMarginLeft", txtRollMarginLeft.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettings] 保存卷装材料左侧边距: {txtRollMarginLeft.Text}");
                    }

                    if (txtRollMarginRight != null && !string.IsNullOrEmpty(txtRollMarginRight.Text))
                    {
                        AppSettings.Set("Imposition_RollMarginRight", txtRollMarginRight.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettings] 保存卷装材料右侧边距: {txtRollMarginRight.Text}");
                    }
                }

                // 保存行列设置
                if (txtRows != null && !string.IsNullOrEmpty(txtRows.Text))
                {
                    AppSettings.Set("Imposition_Rows", txtRows.Text.Trim());
                }

                if (txtColumns != null && !string.IsNullOrEmpty(txtColumns.Text))
                {
                    AppSettings.Set("Imposition_Columns", txtColumns.Text.Trim());
                }

                // 保存卷装材料设置
                if (txtFixedWidth != null && !string.IsNullOrEmpty(txtFixedWidth.Text))
                {
                    AppSettings.Set("Imposition_FixedWidth", txtFixedWidth.Text.Trim());
                }

                if (txtMinLength != null && !string.IsNullOrEmpty(txtMinLength.Text))
                {
                    AppSettings.Set("Imposition_MinLength", txtMinLength.Text.Trim());
                }

                // 保存排版启用状态
                if (chkEnableImposition != null)
                {
                    AppSettings.Set("Imposition_Enabled", chkEnableImposition.Checked);
                    LogHelper.Debug($"[SaveImpositionSettings] 保存排版启用状态: {chkEnableImposition.Checked}");
                }

            
                LogHelper.Debug("[SaveImpositionSettings] 印刷排版设置保存完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[SaveImpositionSettings] 保存印刷排版设置失败: {ex.Message}");
                MessageBox.Show($"保存印刷排版设置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

/// <summary>
        /// 加载印刷排版设置
        /// </summary>
        /// <summary>
        /// 加载印刷排版设置
        /// </summary>
        private void LoadImpositionSettings()
        {
            try
            {
                LogHelper.Debug("[LoadImpositionSettings] 开始加载印刷排版设置");

                // 加载材料尺寸设置（平张材料）
                var paperWidth = AppSettings.Get("Imposition_PaperWidth") as string;
                if (txtPaperWidth != null && !string.IsNullOrEmpty(paperWidth))
                {
                    txtPaperWidth.Text = paperWidth;
                    LogHelper.Debug($"[LoadImpositionSettings] 加载纸张宽度: {paperWidth}");
                }

                var paperHeight = AppSettings.Get("Imposition_PaperHeight") as string;
                if (txtPaperHeight != null && !string.IsNullOrEmpty(paperHeight))
                {
                    txtPaperHeight.Text = paperHeight;
                    LogHelper.Debug($"[LoadImpositionSettings] 加载纸张高度: {paperHeight}");
                }

                // 加载边距设置
                var marginTop = AppSettings.Get("Imposition_MarginTop") as string;
                if (txtMarginTop != null && !string.IsNullOrEmpty(marginTop))
                {
                    txtMarginTop.Text = marginTop;
                }

                var marginBottom = AppSettings.Get("Imposition_MarginBottom") as string;
                if (txtMarginBottom != null && !string.IsNullOrEmpty(marginBottom))
                {
                    txtMarginBottom.Text = marginBottom;
                }

                var marginLeft = AppSettings.Get("Imposition_MarginLeft") as string;
                if (txtMarginLeft != null && !string.IsNullOrEmpty(marginLeft))
                {
                    txtMarginLeft.Text = marginLeft;
                }

                var marginRight = AppSettings.Get("Imposition_MarginRight") as string;
                if (txtMarginRight != null && !string.IsNullOrEmpty(marginRight))
                {
                    txtMarginRight.Text = marginRight;
                }

                // 加载卷装材料专用边距设置
                var rollMarginTop = AppSettings.Get("Imposition_RollMarginTop") as string;
                if (txtRollMarginTop != null && !string.IsNullOrEmpty(rollMarginTop))
                {
                    txtRollMarginTop.Text = rollMarginTop;
                    LogHelper.Debug($"[LoadImpositionSettings] 加载卷装材料顶部边距: {rollMarginTop}");
                }

                var rollMarginBottom = AppSettings.Get("Imposition_RollMarginBottom") as string;
                if (txtRollMarginBottom != null && !string.IsNullOrEmpty(rollMarginBottom))
                {
                    txtRollMarginBottom.Text = rollMarginBottom;
                    LogHelper.Debug($"[LoadImpositionSettings] 加载卷装材料底部边距: {rollMarginBottom}");
                }

                var rollMarginLeft = AppSettings.Get("Imposition_RollMarginLeft") as string;
                if (txtRollMarginLeft != null && !string.IsNullOrEmpty(rollMarginLeft))
                {
                    txtRollMarginLeft.Text = rollMarginLeft;
                    LogHelper.Debug($"[LoadImpositionSettings] 加载卷装材料左侧边距: {rollMarginLeft}");
                }

                var rollMarginRight = AppSettings.Get("Imposition_RollMarginRight") as string;
                if (txtRollMarginRight != null && !string.IsNullOrEmpty(rollMarginRight))
                {
                    txtRollMarginRight.Text = rollMarginRight;
                    LogHelper.Debug($"[LoadImpositionSettings] 加载卷装材料右侧边距: {rollMarginRight}");
                }

                // 加载行列设置
                var rows = AppSettings.Get("Imposition_Rows") as string;
                if (txtRows != null && !string.IsNullOrEmpty(rows))
                {
                    txtRows.Text = rows;
                }

                var columns = AppSettings.Get("Imposition_Columns") as string;
                if (txtColumns != null && !string.IsNullOrEmpty(columns))
                {
                    txtColumns.Text = columns;
                }

                // 加载卷装材料设置
                var fixedWidth = AppSettings.Get("Imposition_FixedWidth") as string;
                if (txtFixedWidth != null && !string.IsNullOrEmpty(fixedWidth))
                {
                    txtFixedWidth.Text = fixedWidth;
                }

                var minLength = AppSettings.Get("Imposition_MinLength") as string;
                if (txtMinLength != null && !string.IsNullOrEmpty(minLength))
                {
                    txtMinLength.Text = minLength;
                }

                // 加载排版启用状态
                var impositionEnabled = AppSettings.Get("Imposition_Enabled");
                if (chkEnableImposition != null && impositionEnabled != null)
                {
                    chkEnableImposition.Checked = Convert.ToBoolean(impositionEnabled);
                    LogHelper.Debug($"[LoadImpositionSettings] 加载排版启用状态: {impositionEnabled}");
                }

              
                LogHelper.Debug("[LoadImpositionSettings] 印刷排版设置加载完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[LoadImpositionSettings] 加载印刷排版设置失败: {ex.Message}");
                MessageBox.Show($"加载印刷排版设置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadSettings()
        {
            // ✅ 修复：设置标志防止任何事件触发的自动保存
            _isLoadingSettings = true;
            LogHelper.Debug("[LoadSettings] 设置_isLoadingSettings=true，防止在加载期间自动保存");
            
            try
            {
                // 强制重新加载CustomSettings以确保获取最新值
                LogHelper.Debug("[LoadSettings] 开始强制重新加载CustomSettings");
                AppSettings.Instance.ReloadCustomSettings();

            // 加载分隔符设置 - 支持空分隔符
            var separatorFromSettings = AppSettings.Separator ?? "";
            txtSeparator.Text = separatorFromSettings;
            LogHelper.Debug($"[LoadSettings] 加载Separator: '{separatorFromSettings}'{(string.IsNullOrEmpty(separatorFromSettings) ? " (空分隔符)" : separatorFromSettings == "_" ? " (默认分隔符)" : " (自定义分隔符)")} 到控件");
            
            // 从应用设置加载单位值到控件
            txtUnit.Text = AppSettings.Unit ?? "";

            // 加载出血值设置
            txtTetBleed.Text = AppSettings.TetBleedValues ?? "3,5,10";

            // 加载材料设置 - 只有当AppSettings有值时才设置，避免空值覆盖
            var materialValue = AppSettings.Material ?? "";
            LogHelper.Debug($"[LoadSettings] 从AppSettings获取Material: '{AppSettings.Material}', txtMaterial当前值: '{txtMaterial.Text}'");
            if (!string.IsNullOrEmpty(materialValue))
            {
                txtMaterial.Text = materialValue;
                LogHelper.Debug($"[LoadSettings] 设置材料值: '{materialValue}'");
            }

            // 加载切换最小化快捷键设置 - 只有当AppSettings有值时才设置，避免空值覆盖
            var hotkeyValue = AppSettings.ToggleMinimizeHotkey ?? "";
            LogHelper.Debug($"[LoadSettings] 从AppSettings获取ToggleMinimizeHotkey: '{AppSettings.ToggleMinimizeHotkey}', txtHotkeyToggle当前值: '{txtHotkeyToggle.Text}'");
            if (!string.IsNullOrEmpty(hotkeyValue))
            {
                txtHotkeyToggle.Text = hotkeyValue;
                LogHelper.Debug($"[LoadSettings] 设置快捷键值: '{hotkeyValue}'");
            }

            // 加载膜类左右键设置 - 注释掉因为控件不存在
            // txtLeftClickFilm.Text = AppSettings.LeftClickFilm ?? "光膜,不过膜";
            // txtRightClickFilm.Text = AppSettings.RightClickFilm ?? "哑膜,红膜";

            // 加载隐藏半径数值复选框状态
            LogHelper.Debug($"[LoadSettings] 开始加载HideRadiusValue，当前值: AppSettings.HideRadiusValue={AppSettings.HideRadiusValue}");
            chkHideRadiusValue.Checked = AppSettings.HideRadiusValue;
            LogHelper.Debug($"[LoadSettings] 已将chkHideRadiusValue.Checked设置为: {chkHideRadiusValue.Checked}");
            
            // 加载形状代号设置
            var zeroShapeCode = AppSettings.Get("ZeroShapeCode") as string;
            var roundShapeCode = AppSettings.Get("RoundShapeCode") as string;
            var ellipseShapeCode = AppSettings.Get("EllipseShapeCode") as string;
            var circleShapeCode = AppSettings.Get("CircleShapeCode") as string;

            // 如果为空或null，使用默认值
            zeroShapeCode = string.IsNullOrEmpty(zeroShapeCode) ? "Z" : zeroShapeCode;
            roundShapeCode = string.IsNullOrEmpty(roundShapeCode) ? "R" : roundShapeCode;
            ellipseShapeCode = string.IsNullOrEmpty(ellipseShapeCode) ? "Y" : ellipseShapeCode;
            circleShapeCode = string.IsNullOrEmpty(circleShapeCode) ? "C" : circleShapeCode;

            LogHelper.Debug($"[LoadSettings] 从AppSettings获取形状代号: ZeroShapeCode='{zeroShapeCode}', RoundShapeCode='{roundShapeCode}', EllipseShapeCode='{ellipseShapeCode}', CircleShapeCode='{circleShapeCode}'");

            txtZeroShapeCode.Text = zeroShapeCode;
            txtRoundShapeCode.Text = roundShapeCode;
            txtEllipseShapeCode.Text = ellipseShapeCode;
            txtCircleShapeCode.Text = circleShapeCode;

            LogHelper.Debug($"[LoadSettings] 形状代号已加载到控件: txtZeroShapeCode='{txtZeroShapeCode.Text}', txtRoundShapeCode='{txtRoundShapeCode.Text}', txtEllipseShapeCode='{txtEllipseShapeCode.Text}', txtCircleShapeCode='{txtCircleShapeCode.Text}'");

            // 加载印刷排版设置
            LoadImpositionSettings();

            // 在窗体完全加载后，再加载EventItems预设
            LoadEventPresets();
            }
            finally
            {
                // ✅ 修复：加载完成后重新启用自动保存
                _isLoadingSettings = false;
                LogHelper.Debug("[LoadSettings] 设置_isLoadingSettings=false，加载完成");
            }
        }



        private void SettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();

            // 加载完设置后，初始化排版控件（确保在加载用户设置之后）
            InitializeImpositionControls();

            // 初始化导出路径管理
            InitializeExportPathsDataGridView();
            LoadExportPaths();
            InitializeExportPathsContextMenu();

          
            // 加载文字预设
            LoadTextPresets();
        }

// 导出路径管理相关事件
        private void BtnAddExportPath_Click(object sender, EventArgs e)
        {
            using (var dialog = new VistaFolderBrowserDialog())
            {
                dialog.Description = "请选择导出路径";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (!AppSettings.ExportPaths.Contains(dialog.SelectedPath))
                    {
                        // 直接添加到设置中
                        var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                        exportPaths.Add(dialog.SelectedPath);
                        AppSettings.ExportPaths = exportPaths;

                        // 获取现有复选框设置
                        var checkboxSettings = AppSettings.Get("ExportPathCheckboxSettings") as Dictionary<string, bool> ?? new Dictionary<string, bool>();
                        // 新添加的路径默认勾选
                        checkboxSettings[dialog.SelectedPath] = true;
                        AppSettings.Set("ExportPathCheckboxSettings", checkboxSettings);
                        AppSettings.Save();

                        // 刷新 DataGridView 显示
                        LoadExportPaths();

                        // 选中新添加的行
                        for (int i = 0; i < dgvExportPaths.Rows.Count; i++)
                        {
                            if (dgvExportPaths.Rows[i].Cells["FullPath"].Value?.ToString() == dialog.SelectedPath)
                            {
                                dgvExportPaths.Rows[i].Selected = true;
                                dgvExportPaths.FirstDisplayedScrollingRowIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("该路径已存在");
                    }
                }
            }
        }

        private void BtnEditExportPath_Click(object sender, EventArgs e)
        {
            // 检查是否选中了行
            if (dgvExportPaths.SelectedRows.Count == 0)
            {
                MessageBox.Show("请选择要编辑的路径");
                return;
            }

            var selectedPath = dgvExportPaths.SelectedRows[0].Cells["FullPath"].Value?.ToString();
            if (string.IsNullOrEmpty(selectedPath)) return;

            // 如果路径包含" (路径不存在)"后缀，移除它用于编辑
            if (selectedPath.EndsWith(" (路径不存在)"))
            {
                selectedPath = selectedPath.Substring(0, selectedPath.Length - " (路径不存在)".Length);
            }

            using (var dialog = new VistaFolderBrowserDialog())
            {
                dialog.Description = "请选择新的导出路径";
                dialog.SelectedPath = selectedPath;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                    var index = exportPaths.IndexOf(selectedPath);
                    if (index >= 0)
                    {
                        // 更新设置
                        exportPaths[index] = dialog.SelectedPath;
                        AppSettings.ExportPaths = exportPaths;
                        AppSettings.Save();

                        // 刷新 DataGridView 显示
                        LoadExportPaths();

                        // 保持选中状态
                        for (int i = 0; i < dgvExportPaths.Rows.Count; i++)
                        {
                            if (dgvExportPaths.Rows[i].Cells["FullPath"].Value?.ToString() == dialog.SelectedPath ||
                                dgvExportPaths.Rows[i].Cells["FullPath"].Value?.ToString() == dialog.SelectedPath + " (路径不存在)")
                            {
                                // 选中整行
                                dgvExportPaths.ClearSelection();
                                dgvExportPaths.Rows[i].Selected = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
        private void BtnDeleteExportPath_Click(object sender, EventArgs e)
        {
            // 获取所有选中的行
            if (dgvExportPaths.SelectedRows.Count == 0)
            {
                MessageBox.Show("请选择要删除的路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedPaths = new List<string>();
            foreach (DataGridViewRow row in dgvExportPaths.SelectedRows)
            {
                if (!row.IsNewRow)
                {
                    var path = row.Cells["FullPath"].Value?.ToString();
                    if (!string.IsNullOrEmpty(path))
                    {
                        // 移除" (路径不存在)"后缀
                        if (path.EndsWith(" (路径不存在)"))
                        {
                            path = path.Substring(0, path.Length - " (路径不存在)".Length);
                        }
                        selectedPaths.Add(path);
                    }
                }
            }

            if (selectedPaths.Count == 0)
                return;

            string message;
            if (selectedPaths.Count == 1)
            {
                message = $"确定要删除路径：{selectedPaths[0]} 吗？";
            }
            else
            {
                message = $"确定要删除选中的 {selectedPaths.Count} 个路径吗？\n\n";
                // 只显示前5个路径作为预览
                var previewPaths = selectedPaths.Take(5);
                message += string.Join("\n", previewPaths);
                if (selectedPaths.Count > 5)
                {
                    message += $"\n... 还有 {selectedPaths.Count - 5} 个路径";
                }
            }

            var result = MessageBox.Show(message, "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                    int deletedCount = 0;

                    foreach (string path in selectedPaths)
                    {
                        if (exportPaths.Remove(path))
                        {
                            deletedCount++;
                        }
                    }

                    if (deletedCount > 0)
                    {
                        AppSettings.ExportPaths = exportPaths;
                        AppSettings.Save();

                        // 刷新 DataGridView 显示
                        LoadExportPaths();

                        // 显示删除结果
                        if (deletedCount == 1)
                        {
                            // 单个删除不需要额外提示，LoadExportPaths 已显示统计信息
                        }
                        else
                        {
                            // 批量删除显示简要提示
                            MessageBox.Show($"成功删除 {deletedCount} 个路径", "删除完成", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"删除路径时发生错误：{ex.Message}", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnMoveUpExportPath_Click(object sender, EventArgs e)
        {
            if (dgvExportPaths.SelectedRows.Count == 0) return;
            int selectedIndex = dgvExportPaths.SelectedRows[0].Index;
            if (selectedIndex <= 0) return;

            var exportPaths = AppSettings.ExportPaths ?? new List<string>();
            if (selectedIndex < exportPaths.Count)
            {
                string selectedPath = exportPaths[selectedIndex];
                exportPaths.RemoveAt(selectedIndex);
                exportPaths.Insert(selectedIndex - 1, selectedPath);
                AppSettings.ExportPaths = exportPaths;
                AppSettings.Save();
                LoadExportPaths();

                // 选中移动后的行并确保显示区域跟随
                if (selectedIndex - 1 >= 0 && selectedIndex - 1 < dgvExportPaths.Rows.Count)
                {
                    // 清除所有选中状态
                    dgvExportPaths.ClearSelection();
                    // 选中目标行
                    dgvExportPaths.Rows[selectedIndex - 1].Selected = true;
                    // 设置当前单元格以确保行标题箭头显示正确
                    dgvExportPaths.CurrentCell = dgvExportPaths.Rows[selectedIndex - 1].Cells["FullPath"];
                    // 确保选中行在可见区域内
                    dgvExportPaths.FirstDisplayedScrollingRowIndex = selectedIndex - 1;
                }
            }
        }

        private void BtnMoveDownExportPath_Click(object sender, EventArgs e)
        {
            if (dgvExportPaths.SelectedRows.Count == 0) return;
            int selectedIndex = dgvExportPaths.SelectedRows[0].Index;

            var exportPaths = AppSettings.ExportPaths ?? new List<string>();
            if (selectedIndex >= exportPaths.Count - 1) return;

            string selectedPath = exportPaths[selectedIndex];
            exportPaths.RemoveAt(selectedIndex);
            exportPaths.Insert(selectedIndex + 1, selectedPath);
            AppSettings.ExportPaths = exportPaths;
            AppSettings.Save();
            LoadExportPaths();

            // 选中移动后的行并确保显示区域跟随
            if (selectedIndex + 1 >= 0 && selectedIndex + 1 < dgvExportPaths.Rows.Count)
            {
                // 清除所有选中状态
                dgvExportPaths.ClearSelection();
                // 选中目标行
                dgvExportPaths.Rows[selectedIndex + 1].Selected = true;
                // 设置当前单元格以确保行标题箭头显示正确
                dgvExportPaths.CurrentCell = dgvExportPaths.Rows[selectedIndex + 1].Cells["FullPath"];
                // 确保选中行在可见区域内，如果超出显示范围则调整显示位置
                int visibleRowCount = dgvExportPaths.DisplayedRowCount(false);
                int firstDisplayedIndex = dgvExportPaths.FirstDisplayedScrollingRowIndex;
                int lastDisplayedIndex = firstDisplayedIndex + visibleRowCount - 1;

                if (selectedIndex + 1 > lastDisplayedIndex)
                {
                    dgvExportPaths.FirstDisplayedScrollingRowIndex = selectedIndex + 1;
                }
            }
        }

        private void LoadExportPaths()
        {
            try
            {
                var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                var bindingList = new BindingList<ExportPathItem>();

                // 加载复选框设置
                var checkboxSettingsObj = AppSettings.Get("ExportPathCheckboxSettings");
                Dictionary<string, bool> checkboxSettings = null;

                // 尝试多种方式反序列化
                if (checkboxSettingsObj != null)
                {
                    try
                    {
                        // 尝试直接转换
                        checkboxSettings = checkboxSettingsObj as Dictionary<string, bool>;

                        if (checkboxSettings == null)
                        {
                            // 尝试JSON反序列化
                            var json = checkboxSettingsObj.ToString();
                            checkboxSettings = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"[LoadExportPaths] 反序列化复选框设置失败: {ex.Message}");
                        checkboxSettings = new Dictionary<string, bool>();
                    }
                }
                else
                {
                    checkboxSettings = new Dictionary<string, bool>();
                }

                LogHelper.Debug($"[LoadExportPaths] 加载了 {checkboxSettings.Count} 个复选框设置");
                foreach (var kvp in checkboxSettings)
                {
                    LogHelper.Debug($"[LoadExportPaths] 复选框设置: {kvp.Key} = {kvp.Value}");
                }

                foreach (var path in exportPaths)
                {
                    // 验证路径是否仍然存在
                    if (Directory.Exists(path))
                    {
                        // 获取复选框状态，默认为true
                        bool includeSubFolders = true;
                        if (checkboxSettings.ContainsKey(path))
                        {
                            includeSubFolders = checkboxSettings[path];
                        }

                        bindingList.Add(new ExportPathItem {
                            FullPath = path,
                            IncludeSubFolders = includeSubFolders
                        });
                    }
                    else
                    {
                        // 路径不存在，记录日志但仍然显示
                        bindingList.Add(new ExportPathItem {
                            FullPath = path + " (路径不存在)",
                            IncludeSubFolders = checkboxSettings.ContainsKey(path) ? checkboxSettings[path] : true
                        });
                    }
                }

                dgvExportPaths.DataSource = bindingList;

                // 更新按钮状态
                UpdateButtonStates();

                // 显示路径数量统计和操作提示
                int validPaths = exportPaths.Count(Directory.Exists);
                int totalPaths = exportPaths.Count;

                if (validPaths < totalPaths)
                {
                    // 如果有无效路径，显示警告
                    lblExportPathsStatus.Text = $"共 {totalPaths} 个路径，其中 {totalPaths - validPaths} 个路径不存在 | 支持多选删除和拖拽添加";
                    lblExportPathsStatus.ForeColor = System.Drawing.Color.Orange;
                }
                else
                {
                    if (totalPaths == 0)
                    {
                        lblExportPathsStatus.Text = "暂无导出路径 | 可拖拽文件夹添加，支持Ctrl+A全选和Delete键删除";
                        lblExportPathsStatus.ForeColor = System.Drawing.Color.Gray;
                    }
                    else
                    {
                        lblExportPathsStatus.Text = $"共 {totalPaths} 个有效路径 | 支持多选删除(Delete键)和拖拽添加";
                        lblExportPathsStatus.ForeColor = System.Drawing.Color.Green;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载导出路径时发生错误：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

private void InitializeExportPathsContextMenu()
        {
            // 创建右键菜单
            var contextMenu = new ContextMenuStrip();
            
            var addMenuItem = new ToolStripMenuItem("添加路径(&A)");
            addMenuItem.Click += (sender, e) => BtnAddExportPath_Click(null, e);
            
            var editMenuItem = new ToolStripMenuItem("编辑路径(&E)");
            editMenuItem.Click += (sender, e) => BtnEditExportPath_Click(null, e);
            
            var deleteMenuItem = new ToolStripMenuItem("删除选中路径(&D)");
            deleteMenuItem.Click += (sender, e) => BtnDeleteExportPath_Click(null, e);
            
            var selectAllMenuItem = new ToolStripMenuItem("全选(&Ctrl+A)");
            selectAllMenuItem.Click += (sender, e) => dgvExportPaths.SelectAll();
            
            contextMenu.Items.AddRange(new ToolStripItem[] {
                addMenuItem,
                new ToolStripSeparator(),
                selectAllMenuItem,
                new ToolStripSeparator(),
                editMenuItem,
                deleteMenuItem
            });
            
            // 菜单打开时更新状态
            contextMenu.Opening += (sender, e) => {
                // 检查选中的行数
                int selectedRowCount = dgvExportPaths.SelectedRows.Count;
                bool hasSelection = selectedRowCount > 0;
                bool isSingleSelection = selectedRowCount == 1;
                
                editMenuItem.Enabled = isSingleSelection;
                deleteMenuItem.Enabled = hasSelection;
                selectAllMenuItem.Enabled = dgvExportPaths.Rows.Count > 0;
                
                // 更新删除菜单项文本
                if (hasSelection) {
                    if (isSingleSelection) {
                        deleteMenuItem.Text = "删除路径(&D)";
                    } else {
                        deleteMenuItem.Text = $"删除 {selectedRowCount} 个路径(&D)";
                    }
                } else {
                    deleteMenuItem.Text = "删除路径(&D)";
                }
            };
            
            dgvExportPaths.ContextMenuStrip = contextMenu;
            
            // 添加键盘快捷键支持
            dgvExportPaths.KeyDown += DgvExportPaths_KeyDown;
        }

        private void DgvExportPaths_KeyDown(object sender, KeyEventArgs e)
        {
            // 检查是否有选中的行
            if (e.KeyCode == Keys.Delete && dgvExportPaths.SelectedRows.Count > 0)
            {
                // Delete键删除选中行（支持多选）
                BtnDeleteExportPath_Click(null, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Insert)
            {
                // Insert键在选中位置插入新路径
                if (dgvExportPaths.SelectedRows.Count == 1)
                {
                    InsertNewPathAtSelectedIndex();
                }
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.N)
            {
                // Ctrl+N 添加新路径
                BtnAddExportPath_Click(null, e);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                // F2 键编辑当前选中的行
                if (dgvExportPaths.SelectedRows.Count == 1)
                {
                    BtnEditExportPath_Click(null, e);
                    e.Handled = true;
                }
            }
            else if (e.Control && e.KeyCode == Keys.A)
            {
                // Ctrl+A 全选
                dgvExportPaths.SelectAll();
                e.Handled = true;
            }
        }

        private void InsertNewPathAtSelectedIndex()
        {
            using (var dialog = new VistaFolderBrowserDialog())
            {
                dialog.Description = "请选择要插入的导出路径";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                    int selectedIndex = dgvExportPaths.SelectedRows.Count > 0 ? dgvExportPaths.SelectedRows[0].Index : -1;

                    if (!exportPaths.Contains(dialog.SelectedPath))
                    {
                        // 在选中位置插入，如果没有选中则添加到末尾
                        if (selectedIndex >= 0 && selectedIndex < exportPaths.Count)
                        {
                            exportPaths.Insert(selectedIndex, dialog.SelectedPath);
                        }
                        else
                        {
                            exportPaths.Add(dialog.SelectedPath);
                        }

                        AppSettings.ExportPaths = exportPaths;
                        AppSettings.Save();
                        LoadExportPaths();

                        // 选中新插入的行
                        for (int i = 0; i < dgvExportPaths.Rows.Count; i++)
                        {
                            if (dgvExportPaths.Rows[i].Cells["FullPath"].Value?.ToString() == dialog.SelectedPath)
                            {
                                dgvExportPaths.Rows[i].Selected = true;
                                dgvExportPaths.FirstDisplayedScrollingRowIndex = i;
                                break;
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("该路径已存在");
                    }
                }
            }
        }

        private void InitializeExportPathsDataGridView()
        {
            dgvExportPaths.AutoGenerateColumns = false;
            dgvExportPaths.ReadOnly = false; // 允许编辑
            dgvExportPaths.AllowUserToAddRows = true; // 允许添加新行
            dgvExportPaths.AllowUserToDeleteRows = true; // 允许删除行

            // 设置行选择模式（支持点击行标题选中整行，也可以点击单元格）
            dgvExportPaths.MultiSelect = true;
            dgvExportPaths.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;

            // 设置行头可见，这样用户可以点击行头选择整行
            dgvExportPaths.RowHeadersVisible = true;
            dgvExportPaths.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            dgvExportPaths.RowHeadersWidth = 40;

            // 启用拖拽功能
            dgvExportPaths.AllowDrop = true;

            // 如果列不存在，则添加列（避免重复添加）
            // 添加复选框列
            if (!dgvExportPaths.Columns.Contains("IncludeSubFolders"))
            {
                var checkboxColumn = new DataGridViewCheckBoxColumn
                {
                    Name = "IncludeSubFolders",
                    HeaderText = "读取子文件夹",
                    DataPropertyName = "IncludeSubFolders",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells,
                    Width = 100,
                    ThreeState = false,
                    FalseValue = false,
                    TrueValue = true
                };
                dgvExportPaths.Columns.Add(checkboxColumn);
            }

            if (!dgvExportPaths.Columns.Contains("FolderName"))
            {
                dgvExportPaths.Columns.Add("FolderName", "文件夹名称");
                dgvExportPaths.Columns["FolderName"].DataPropertyName = "FolderName";
                dgvExportPaths.Columns["FolderName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvExportPaths.Columns["FolderName"].FillWeight = 25F;
                dgvExportPaths.Columns["FolderName"].ReadOnly = true; // 文件夹名称只读
            }

            if (!dgvExportPaths.Columns.Contains("FullPath"))
            {
                dgvExportPaths.Columns.Add("FullPath", "完整路径");
                dgvExportPaths.Columns["FullPath"].DataPropertyName = "FullPath";
                dgvExportPaths.Columns["FullPath"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvExportPaths.Columns["FullPath"].FillWeight = 65F;
                dgvExportPaths.Columns["FullPath"].ReadOnly = false; // 完整路径可编辑
            }

            // 添加实时保存事件处理
            dgvExportPaths.CellEndEdit += DgvExportPaths_CellEndEdit;
            dgvExportPaths.UserDeletedRow += DgvExportPaths_UserDeletedRow;
            dgvExportPaths.UserAddedRow += DgvExportPaths_UserAddedRow;
            dgvExportPaths.SelectionChanged += DgvExportPaths_SelectionChanged;
            dgvExportPaths.DataError += DgvExportPaths_DataError;

            // 添加复选框状态变化事件处理
            dgvExportPaths.CurrentCellDirtyStateChanged += DgvExportPaths_CurrentCellDirtyStateChanged;
            dgvExportPaths.CellValueChanged += DgvExportPaths_CellValueChanged;
            
            // 添加拖拽事件处理
            dgvExportPaths.DragEnter += DgvExportPaths_DragEnter;
            dgvExportPaths.DragOver += DgvExportPaths_DragOver;
            dgvExportPaths.DragDrop += DgvExportPaths_DragDrop;
            
            // 初始选择状态更新
            UpdateButtonStates();
        }

private void DgvExportPaths_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                {
                    var columnName = dgvExportPaths.Columns[e.ColumnIndex].Name;
                    if (columnName == "FullPath")
                    {
                        var editedValue = dgvExportPaths.Rows[e.RowIndex].Cells["FullPath"].Value?.ToString();
                        if (string.IsNullOrEmpty(editedValue))
                        {
                            // 如果路径为空，删除该行
                            dgvExportPaths.Rows.RemoveAt(e.RowIndex);
                            return;
                        }

                        // 验证路径有效性
                        if (!Directory.Exists(editedValue))
                        {
                            MessageBox.Show($"路径不存在：{editedValue}\n请输入有效的文件夹路径。", "路径无效",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            // 恢复原有值或删除行
                            var currentPaths = AppSettings.ExportPaths ?? new List<string>();
                            if (e.RowIndex < currentPaths.Count)
                            {
                                dgvExportPaths.Rows[e.RowIndex].Cells["FullPath"].Value = currentPaths[e.RowIndex];
                            }
                            else
                            {
                                dgvExportPaths.Rows.RemoveAt(e.RowIndex);
                            }
                            return;
                        }

                        // 检查是否重复路径
                        var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                        for (int i = 0; i < exportPaths.Count; i++)
                        {
                            if (i != e.RowIndex && exportPaths[i] == editedValue)
                            {
                                MessageBox.Show("该路径已存在！", "重复路径",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                // 恢复原有值
                                if (e.RowIndex < exportPaths.Count)
                                {
                                    dgvExportPaths.Rows[e.RowIndex].Cells["FullPath"].Value = exportPaths[e.RowIndex];
                                }
                                else
                                {
                                    dgvExportPaths.Rows.RemoveAt(e.RowIndex);
                                }
                                return;
                            }
                        }

                        // 实时保存到设置
                        SaveExportPathsFromDataGridView();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存导出路径时发生错误：{ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvExportPaths_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            try
            {
                // 实时保存删除操作
                SaveExportPathsFromDataGridView();
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"删除导出路径时发生错误：{ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvExportPaths_UserAddedRow(object sender, DataGridViewRowEventArgs e)
        {
            try
            {
                // 新行添加时，可以选择一个默认路径或提示用户
                if (e.Row.IsNewRow)
                {
                    // 可以在这里添加自动选择文件夹的逻辑
                    // 暂时不做处理，等待用户编辑
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"添加导出路径时发生错误：{ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DgvExportPaths_SelectionChanged(object sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void DgvExportPaths_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // 处理数据绑定错误
            MessageBox.Show($"数据绑定错误：{e.Exception?.Message}", "数据错误",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
        }

        private void DgvExportPaths_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            // 复选框状态变化时的处理
            try
            {
                if (dgvExportPaths.CurrentCell?.ColumnIndex >= 0 &&
                    dgvExportPaths.Columns[dgvExportPaths.CurrentCell.ColumnIndex].Name == "IncludeSubFolders")
                {
                    SaveExportPathsWithCheckboxSettings();
                    // 触发导出路径设置变更事件
                    OnExportPathSettingsChanged(EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理复选框状态变化时出错: {ex.Message}", ex);
            }
        }

        private void DgvExportPaths_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // 复选框值变化时的处理
            try
            {
                if (e.ColumnIndex >= 0 && dgvExportPaths.Columns[e.ColumnIndex].Name == "IncludeSubFolders")
                {
                    SaveExportPathsWithCheckboxSettings();
                    // 触发导出路径设置变更事件
                    OnExportPathSettingsChanged(EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"处理复选框值变化时出错: {ex.Message}", ex);
            }
        }

        private void SaveExportPathsWithCheckboxSettings()
        {
            try
            {
                // 保存路径和对应的复选框状态
                var exportPaths = new List<string>();
                var checkboxSettings = new Dictionary<string, bool>();

                foreach (DataGridViewRow row in dgvExportPaths.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        var path = row.Cells["FullPath"]?.Value?.ToString();
                        var includeSubFolders = row.Cells["IncludeSubFolders"]?.Value as bool? ?? true;

                        if (!string.IsNullOrEmpty(path))
                        {
                            exportPaths.Add(path);
                            checkboxSettings[path] = includeSubFolders;
                        }
                    }
                }

                // 保存路径列表
                AppSettings.ExportPaths = exportPaths;

                // 保存复选框设置
                LogHelper.Debug($"[SaveExportPathsWithCheckboxSettings] 准备保存 {checkboxSettings.Count} 个复选框设置");
                foreach (var kvp in checkboxSettings)
                {
                    LogHelper.Debug($"[SaveExportPathsWithCheckboxSettings] 保存复选框设置: {kvp.Key} = {kvp.Value}");
                }
                AppSettings.Set("ExportPathCheckboxSettings", checkboxSettings);
                AppSettings.Save();
                LogHelper.Info("所有设置保存成功");
                
                // 触发设置保存事件
                SettingsSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                throw new Exception("保存导出路径和复选框设置失败", ex);
            }
        }

        private void UpdateButtonStates()
        {
            // 检查选中的行
            int selectedRowCount = dgvExportPaths.SelectedRows.Count;
            bool hasSelection = selectedRowCount > 0;
            bool isSingleSelection = selectedRowCount == 1;
            int selectedIndex = hasSelection ? dgvExportPaths.SelectedRows[0].Index : -1;
            var exportPaths = AppSettings.ExportPaths ?? new List<string>();
            int itemCount = exportPaths.Count;

            btnEditExportPath.Enabled = isSingleSelection;
            btnDeleteExportPath.Enabled = hasSelection;
            
            // 移动按钮只在单选时有效
            btnMoveUpExportPath.Enabled = isSingleSelection && selectedIndex > 0;
            btnMoveDownExportPath.Enabled = isSingleSelection && selectedIndex < itemCount - 1;
            
            // 更新删除按钮文本
            if (hasSelection)
            {
                if (isSingleSelection)
                {
                    btnDeleteExportPath.Text = "删除路径";
                }
                else
                {
                    btnDeleteExportPath.Text = $"删除 {selectedRowCount} 个路径";
                }
            }
            else
            {
                btnDeleteExportPath.Text = "删除路径";
            }
        }

        private void SaveExportPathsFromDataGridView()
        {
            try
            {
                var exportPaths = new List<string>();

                // 从 DataGridView 收集所有有效路径
                foreach (DataGridViewRow row in dgvExportPaths.Rows)
                {
                    if (!row.IsNewRow) // 跳过新行模板
                    {
                        var path = row.Cells["FullPath"].Value?.ToString();
                        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                        {
                            exportPaths.Add(path);
                        }
                    }
                }

                // 保存复选框设置
                SaveExportPathsWithCheckboxSettings();

                // 直接使用AppSettings保存，确保数据同步
                AppSettings.ExportPaths = exportPaths;
                AppSettings.Save();
            }
            catch (Exception ex)
            {
                throw new Exception("保存导出路径失败", ex);
            }
        }

      // 拖拽事件处理方法
        private void DgvExportPaths_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                // 检查拖拽数据是否包含文件夹路径
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DragEnter error: {ex.Message}");
                e.Effect = DragDropEffects.None;
            }
        }

        private void DgvExportPaths_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                // 检查拖拽数据是否包含文件夹路径
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DragOver error: {ex.Message}");
                e.Effect = DragDropEffects.None;
            }
        }

        private void DgvExportPaths_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // 获取拖拽的文件路径
                    string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    
                    if (filePaths == null || filePaths.Length == 0)
                    {
                        return;
                    }

                    var exportPaths = AppSettings.ExportPaths ?? new List<string>();
                    var addedPaths = new List<string>();
                    var duplicatePaths = new List<string>();
                    var invalidPaths = new List<string>();

                    foreach (string filePath in filePaths)
                    {
                        // 检查是否为文件夹
                        if (Directory.Exists(filePath))
                        {
                            // 检查路径是否已存在
                            if (!exportPaths.Contains(filePath))
                            {
                                exportPaths.Add(filePath);
                                addedPaths.Add(filePath);
                            }
                            else
                            {
                                duplicatePaths.Add(filePath);
                            }
                        }
                        else
                        {
                            invalidPaths.Add(Path.GetFileName(filePath));
                        }
                    }

                    // 保存更新的路径列表
                    if (addedPaths.Count > 0)
                    {
                        AppSettings.ExportPaths = exportPaths;

                        // 获取现有复选框设置
                        var checkboxSettings = AppSettings.Get("ExportPathCheckboxSettings") as Dictionary<string, bool> ?? new Dictionary<string, bool>();
                        // 新添加的路径默认勾选
                        foreach (string path in addedPaths)
                        {
                            if (!checkboxSettings.ContainsKey(path))
                            {
                                checkboxSettings[path] = true;
                            }
                        }
                        AppSettings.Set("ExportPathCheckboxSettings", checkboxSettings);
                        AppSettings.Save();
                        
                        // 刷新 DataGridView 显示
                        LoadExportPaths();
                        
                        // 选中新添加的第一个路径
                        if (addedPaths.Count > 0)
                        {
                            for (int i = 0; i < dgvExportPaths.Rows.Count; i++)
                            {
                                if (dgvExportPaths.Rows[i].Cells["FullPath"].Value?.ToString() == addedPaths[0])
                                {
                                    dgvExportPaths.Rows[i].Selected = true;
                                    dgvExportPaths.FirstDisplayedScrollingRowIndex = i;
                                    break;
                                }
                            }
                        }

                        // 显示结果消息
                        string message = $"成功添加 {addedPaths.Count} 个文件夹路径";
                        if (duplicatePaths.Count > 0)
                        {
                            message += $"\n跳过 {duplicatePaths.Count} 个重复路径";
                        }
                        if (invalidPaths.Count > 0)
                        {
                            message += $"\n忽略 {invalidPaths.Count} 个无效文件";
                        }
                        
                        if (duplicatePaths.Count > 0 || invalidPaths.Count > 0)
                        {
                            MessageBox.Show(message, "拖拽添加完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        // 没有添加任何新路径
                        if (duplicatePaths.Count > 0)
                        {
                            MessageBox.Show($"所有拖拽的路径都已存在（{duplicatePaths.Count} 个）", "无新路径添加", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else if (invalidPaths.Count > 0)
                        {
                            MessageBox.Show($"拖拽的项目中没有有效的文件夹（{invalidPaths.Count} 个文件）", "无有效文件夹", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理拖拽操作时发生错误：{ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

      // 辅助方法：更新拖拽提示信息
        private void UpdateDragDropHint()
        {
            // 可以在这里添加状态栏或标签的提示信息更新
            // 例如：lblExportPathsStatus.Text = "拖拽文件夹到此处可快速添加导出路径";
        }

        public class ExportPathItem
        {
            public string FullPath { get; set; }
            public string FolderName => System.IO.Path.GetFileName(FullPath);
            public bool IncludeSubFolders { get; set; } = true; // 默认勾选
        }


        public void RecognizePdfDimensions(string pdfPath)
        {
            try
            {
                if (!File.Exists(pdfPath))
                {
                    MessageBox.Show("PDF文件不存在");
                    return;
                }

                int retryCount = 3;
                int delayMs = 100;
                bool success = false;

                for (int i = 0; i < retryCount; i++)
                {
                    try
                    {
                        // 统一使用IText7PdfTools通过服务接口
                        if (_pdfDimensionService.GetFirstPageSize(pdfPath, out double width, out double height))
                        {
                            PdfWidth = width;
                            PdfHeight = height;
                            success = true;
                            break;
                        }
                        else
                        {
                            MessageBox.Show("无法获取PDF文件尺寸");
                            return;
                        }
                    }
                    catch (IOException)
                    {
                        if (i == retryCount - 1) // 最后一次重试失败
                            throw;
                        System.Threading.Thread.Sleep(delayMs);
                    }
                }

                if (!success)
                    return;
            }
            catch (IOException ex)
            {
                MessageBox.Show($"PDF文件正在被使用或未保存完成，请稍后重试: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF尺寸识别失败: {ex.Message}");
            }
        }
        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SaveSettings();
            }
            catch (Exception ex)
            {
                // 记录错误但不阻止窗体关闭
                Console.WriteLine($"保存设置时出错: {ex.Message}");
                MessageBox.Show($"保存设置时出错: {ex.Message}", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnSetMaterial_Click(object sender, EventArgs e)
        {
            // 获取文本框内容并分割成材料列表
            string materialText = txtMaterial.Text.Trim();
            if (string.IsNullOrEmpty(materialText))
            {
                MessageBox.Show("请输入材料名称");
                return;
            }

            List<string> materials = materialText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Distinct()
                .ToList();

            if (materials.Count == 0)
            {
                MessageBox.Show("请输入有效的材料名称");
                return;
            }

            // 设置材料并显示成功消息
            MaterialManager.Instance.SetMaterials(materials);
            // 保存材料设置到应用配置
            // 保存材料设置到应用配置
            AppSettings.Material = materialText;
            MessageBox.Show("材料设置成功！");
            txtMaterial.Clear();
        }

        private void TrackBarOpacity_Scroll(object sender, EventArgs e)
        {
            // 透明度值将在点击保存按钮时统一保存
        }

    


        
        private void ChkLstTextItems_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // 延迟保存以确保选中状态已更新
            if (this.IsHandleCreated)
            {
                BeginInvoke(new Action(() =>
                {
                    SaveSettings();
                    UpdateTextCombo();
                }));
            }
            else
            {
                // 句柄未创建时直接保存
                SaveSettings();
                UpdateTextCombo();
            }
        }

        private void BtnMoveUpText_Click(object sender, EventArgs e)
        {
            if (chkLstTextItems.SelectedIndex > 0)
            {
                int index = chkLstTextItems.SelectedIndex;
                object item = chkLstTextItems.Items[index];
                bool isChecked = chkLstTextItems.GetItemChecked(index);

                chkLstTextItems.Items.RemoveAt(index);
                chkLstTextItems.Items.Insert(index - 1, item);
                chkLstTextItems.SetItemChecked(index - 1, isChecked);
                chkLstTextItems.SelectedIndex = index - 1;
                SaveSettings();
                UpdateTextCombo();
            }
        }

        private void BtnMoveDownText_Click(object sender, EventArgs e)
        {
            if (chkLstTextItems.SelectedIndex < chkLstTextItems.Items.Count - 1 && chkLstTextItems.SelectedIndex != -1)
            {
                int index = chkLstTextItems.SelectedIndex;
                object item = chkLstTextItems.Items[index];
                bool isChecked = chkLstTextItems.GetItemChecked(index);

                chkLstTextItems.Items.RemoveAt(index);
                chkLstTextItems.Items.Insert(index + 1, item);
                chkLstTextItems.SetItemChecked(index + 1, isChecked);
                chkLstTextItems.SelectedIndex = index + 1;
                SaveSettings();
                UpdateTextCombo();
            }
        }

        /// <summary>
        /// 更新组合内容显示
        /// </summary>
        private void UpdateTextCombo()
        {
            try
            {
                List<string> selectedItems = new List<string>();
                foreach (var item in chkLstTextItems.Items)
                {
                    int index = chkLstTextItems.Items.IndexOf(item);
                    if (chkLstTextItems.GetItemChecked(index))
                    {
                        selectedItems.Add(item.ToString());
                    }
                }

                string separator = AppSettings.Separator ?? "";
                string comboText = string.Join(separator, selectedItems);

                txtTextCombo.Text = comboText;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"更新组合内容失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 组合内容文本框变化事件
        /// </summary>
        private void TxtTextCombo_TextChanged(object sender, EventArgs e)
        {
            // 保存用户修改的组合内容
            AppSettings.Set("TextComboContent", txtTextCombo.Text);
        }

        /// <summary>
        /// 获取用户修改后的组合内容
        /// </summary>
        /// <returns>用户修改后的组合内容</returns>
        public string GetTextComboContent()
        {
            return txtTextCombo.Text;
        }
        private double CustomRound(double value)
        {
            // 正常四舍五入到十分位
            return Math.Round(value, 1);
        }
        public string CalculateFinalDimensions(double width, double height, double tetBleed, string cornerRadius = "0", bool addPdfLayers = false)
        {
            // 应用公式: 长-tetBleed*2, 宽-tetBleed*2
            // 修正出血值计算逻辑：确保原始尺寸为PDF实际尺寸，仅减去一次双边出血值
            double finalWidth = CustomRound(width - tetBleed * 2);
            double finalHeight = CustomRound(height - tetBleed * 2);
            
            // 基础尺寸格式
            string dimensions = $"{finalWidth}x{finalHeight}";
            
            // 当chkAddPdfLayers处于勾选状态时，根据cornerRadius添加形状信息
            if (addPdfLayers && !string.IsNullOrEmpty(cornerRadius))
            {
                string trimmedRadius = cornerRadius.Trim();
                // 从 AppSettings 读取形状代号
                var zeroShapeCode = AppSettings.Get("ZeroShapeCode") as string ?? "Z";
                var roundShapeCode = AppSettings.Get("RoundShapeCode") as string ?? "R";
                var ellipseShapeCode = AppSettings.Get("EllipseShapeCode") as string ?? "Y";
                var circleShapeCode = AppSettings.Get("CircleShapeCode") as string ?? "C";
                var hideRadiusValue = AppSettings.Get("HideRadiusValue") as bool? ?? false;
                
                if (trimmedRadius.Equals("R", StringComparison.OrdinalIgnoreCase))
                {
                    dimensions += circleShapeCode; // 使用圆形代号
                }
                else if (trimmedRadius.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    dimensions += ellipseShapeCode;
                }
                else if (int.TryParse(trimmedRadius, out int numRadius) && numRadius > 0)
                {
                    // 对于数字半径值，添加前缀
                    if (hideRadiusValue)
                    {
                        // 如果隐藏半径数值复选框被勾选，只添加代号不添加数字
                        dimensions += roundShapeCode;
                    }
                    else
                    {
                        // 否则添加代号和数字
                        dimensions += roundShapeCode + numRadius;
                    }
                }
                // 当形状输入为"0"时，添加代号
                else if (trimmedRadius.Equals("0"))
                {
                    dimensions += zeroShapeCode;
                }
            }
            
            return dimensions;
        }

        public string GenerateRenamePattern(double tetBleed)
        {
            List<string> parts = new List<string>();
            var sortedGroups = _eventGroupConfigs.OrderBy(g => g.SortOrder).ToList();

            foreach (var groupConfig in sortedGroups)
            {
                if (!groupConfig.IsEnabled) continue; // 跳过禁用的分组

                var groupItems = _eventItems
                    .Where(item => item.Group == groupConfig.Group && item.IsEnabled)
                    .OrderBy(item => item.SortOrder)
                    .ToList();

                foreach (var eventItem in groupItems)
                {
                    string text = eventItem.Name;

                    // 根据分组添加前缀
                    if (!string.IsNullOrEmpty(groupConfig.Prefix))
                    {
                        text = groupConfig.Prefix + text;
                    }

                    if (eventItem.Name == "尺寸")
                    {
                        parts.Add(CalculateFinalDimensions(PdfWidth, PdfHeight, tetBleed));
                    }
                    else
                    {
                        parts.Add(text);
                    }
                }
            }

            return string.Join(AppSettings.Separator ?? "", parts);
        }

        /// <summary>
        /// 生成PDF文字添加内容
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="material">材料</param>
        /// <param name="quantity">数量</param>
        /// <param name="process">工艺</param>
        /// <param name="dimensions">尺寸</param>
        /// <returns>要添加到PDF的文字内容</returns>
        public string GeneratePdfTextContent(string fileName = "", string material = "", string quantity = "", string process = "", string dimensions = "")
        {
            // 获取用户修改的组合内容
            string comboContent = txtTextCombo.Text.Trim();
            if (string.IsNullOrEmpty(comboContent))
            {
                return "";
            }

            // 替换组合内容中的占位符为实际值
            string result = comboContent;

            // 使用分隔符分割组合内容
            string separator = AppSettings.Separator ?? "";
            string[] parts = result.Split(new[] { separator }, StringSplitOptions.None);

            List<string> processedParts = new List<string>();
            foreach (string part in parts)
            {
                string processedPart = part;

                switch (part)
                {
                    case "正则结果":
                        // 对于文字添加功能，正则结果可能不适用，可以作为占位符
                        processedPart = "REGEX";
                        break;
                    case "订单号":
                        if (!string.IsNullOrEmpty(fileName))
                            processedPart = fileName;
                        else
                            processedPart = "订单号";
                        break;
                    case "材料":
                        if (!string.IsNullOrEmpty(material))
                            processedPart = material;
                        else
                            processedPart = "材料";
                        break;
                    case "数量":
                        if (!string.IsNullOrEmpty(quantity))
                            processedPart = quantity;
                        else
                            processedPart = "数量";
                        break;
                    case "工艺":
                        if (!string.IsNullOrEmpty(process))
                            processedPart = process;
                        else
                            processedPart = "工艺";
                        break;
                    case "尺寸":
                        if (!string.IsNullOrEmpty(dimensions))
                            processedPart = dimensions;
                        else
                            processedPart = "尺寸";
                        break;
                    case "序号":
                        processedPart = DateTime.Now.ToString("HHmmss");
                        break;
                    case "列组合":
                        processedPart = "COMBO";
                        break;
                }

                processedParts.Add(processedPart);
            }

            return string.Join(" ", processedParts);
        }

        /// <summary>
        /// 获取选中的文字项列表
        /// </summary>
        /// <returns>选中的文字项列表</returns>
        public List<string> GetSelectedTextItems()
        {
            List<string> selectedItems = new List<string>();

            foreach (var item in chkLstTextItems.Items)
            {
                int index = chkLstTextItems.Items.IndexOf(item);
                if (chkLstTextItems.GetItemChecked(index))
                {
                    selectedItems.Add(item.ToString());
                }
            }

            return selectedItems;
        }

        /// <summary>
        /// 检查是否有文字项被选中
        /// </summary>
        /// <returns>是否有文字项被选中</returns>
        public bool HasSelectedTextItems()
        {
            foreach (var item in chkLstTextItems.Items)
            {
                int index = chkLstTextItems.Items.IndexOf(item);
                if (chkLstTextItems.GetItemChecked(index))
                {
                    return true;
                }
            }
            return false;
        }



        private void LabelSeparator_Click(object sender, EventArgs e)
        {

        }

        private void LblUnit_Click(object sender, EventArgs e)
        {

        }

        private void TxtUnit_TextChanged(object sender, EventArgs e)
        {

        }

        private void LabelTetBleed_Click(object sender, EventArgs e)
        {

        }





        private void TxtTetBleed_TextChanged(object sender, EventArgs e)
        {
            // 当出血值文本框内容改变时，保存设置
            SaveSettings();
        }

        #region 切换最小化快捷键录制功能
        private void BtnRecordToggle_Click(object sender, EventArgs e)
        {
            if (!isRecordingToggle)
            {
                isRecordingToggle = true;
                btnRecordToggle.Text = "正在录制...";
                txtHotkeyToggle.Text = "";
                this.KeyPreview = true;
                this.KeyDown += RecordToggleKeyDown;
            }
            else
            {
                StopRecordingToggle();
            }
        }

        private void StopRecordingToggle()
        {
            isRecordingToggle = false;
            btnRecordToggle.Text = "录制";
            this.KeyPreview = false;
            this.KeyDown -= RecordToggleKeyDown;
        }

        private void RecordToggleKeyDown(object sender, KeyEventArgs e)
        {
            // 检查是否有有效的修饰键和单个按键
            if ((e.Control || e.Alt || e.Shift) && e.KeyCode != Keys.None && !IsModifierKey(e.KeyCode))
            {
                System.Text.StringBuilder hotkey = new System.Text.StringBuilder();
                if (e.Control) hotkey.Append("Ctrl+");
                if (e.Alt) hotkey.Append("Alt+");
                if (e.Shift) hotkey.Append("Shift+");
                hotkey.Append(e.KeyCode.ToString());
                
                txtHotkeyToggle.Text = hotkey.ToString();
                StopRecordingToggle();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                // 按ESC取消录制
                StopRecordingToggle();
                e.SuppressKeyPress = true;
            }
        }
        #endregion

        private void lblHideRadiusValue_Click(object sender, EventArgs e)
        {

        }


        
        // 检查功能是否启用
        public static bool IsEventEnabled(string eventName)
        {
            // 对于列组合功能，使用列组合服务来检查
            if (eventName == "列组合")
            {
                return _compositeColumnService.IsCompositeColumnFeatureEnabled();
            }

            // 对于行数和列数，需要检查是否有有效的布局计算结果
            if (eventName == "行数" || eventName == "列数")
            {
                // 先检查配置中是否启用了这些选项
                bool configEnabled = false;
                string savedItems = AppSettings.EventItems;
                if (!string.IsNullOrEmpty(savedItems))
                {
                    string[] items = savedItems.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    if (items.Length % 2 == 0)
                    {
                        for (int i = 0; i < items.Length; i += 2)
                        {
                            string text = items[i];
                            if (text == eventName)
                            {
                                bool.TryParse(items[i + 1], out configEnabled);
                                break;
                            }
                        }
                    }
                }

                LogHelper.Debug($"[IsEventEnabled] {eventName}: 配置启用={configEnabled}, MaterialSelectForm实例={(_currentMaterialSelectForm != null)}");

                // 如果配置中未启用，直接返回false
                if (!configEnabled)
                {
                    return false;
                }

                // 如果配置中启用了，检查是否有有效的布局计算结果
                if (_currentMaterialSelectForm != null)
                {
                    if (eventName == "行数")
                    {
                        int rows = _currentMaterialSelectForm.GetRows();
                        LogHelper.Debug($"[IsEventEnabled] 行数: 实例计算结果={rows}");
                        return rows > 0;
                    }
                    else if (eventName == "列数")
                    {
                        int columns = _currentMaterialSelectForm.GetColumns();
                        LogHelper.Debug($"[IsEventEnabled] 列数: 实例计算结果={columns}");
                        return columns > 0;
                    }
                }

                // 如果实例不存在，使用保存的结果
                if (eventName == "行数")
                {
                    LogHelper.Debug($"[IsEventEnabled] 行数: 保存的结果={_savedLayoutRows}");
                    return _savedLayoutRows > 0;
                }
                else if (eventName == "列数")
                {
                    LogHelper.Debug($"[IsEventEnabled] 列数: 保存的结果={_savedLayoutColumns}");
                    return _savedLayoutColumns > 0;
                }

                LogHelper.Debug($"[IsEventEnabled] {eventName}: 没有有效结果，返回false");
                return false;
            }

            // 对于其他功能，使用原有的实现方式
            string savedItems2 = AppSettings.EventItems;
            if (!string.IsNullOrEmpty(savedItems2))
            {
                string[] items = savedItems2.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                // 确保数组长度为偶数（每项+状态）
                if (items.Length % 2 == 0)
                {
                    for (int i = 0; i < items.Length; i += 2)
                    {
                        string text = items[i];
                        // 使用TryParse处理可能无效的布尔值字符串
                        bool isChecked = false;
                        bool.TryParse(items[i + 1], out isChecked);

                        if (text == eventName)
                        {
                            return isChecked;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 提示输入预设名称
        /// </summary>
        private string PromptForPresetName(string prompt, string title, string defaultValue)
        {
            Form inputForm = new Form
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label label = new Label { Left = 20, Top = 20, Width = 350, Text = prompt };
            TextBox textBox = new TextBox { Left = 20, Top = 50, Width = 340, Text = defaultValue };
            Button btnOk = new Button { Text = "确定", Left = 180, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "取消", Left = 270, Width = 80, Top = 80, DialogResult = DialogResult.Cancel };

            btnOk.Click += (sender, e) => { inputForm.Close(); };
            btnCancel.Click += (sender, e) => { inputForm.Close(); };

            inputForm.Controls.Add(label);
            inputForm.Controls.Add(textBox);
            inputForm.Controls.Add(btnOk);
            inputForm.Controls.Add(btnCancel);
            inputForm.AcceptButton = btnOk;
            inputForm.CancelButton = btnCancel;

            return inputForm.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }

        #region 文字预设管理

        /// <summary>
        /// 加载文字预设列表
        /// </summary>
        private void LoadTextPresets()
        {
            try
            {
                cboTextPresets.Items.Clear();

                // 添加内置预设
                cboTextPresets.Items.Add("默认文字");
                cboTextPresets.Items.Add("基本信息");
                cboTextPresets.Items.Add("时间信息");

                // 从设置加载自定义预设
                string customPresets = AppSettings.Get("TextPresets") as string;
                if (!string.IsNullOrEmpty(customPresets))
                {
                    string[] presets = customPresets.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string preset in presets)
                    {
                        if (!string.IsNullOrEmpty(preset) && !cboTextPresets.Items.Contains(preset))
                        {
                            cboTextPresets.Items.Add(preset);
                        }
                    }
                }

                // 选中当前使用的预设
                string currentPreset = AppSettings.Get("CurrentTextPreset") as string ?? "默认文字";
                if (cboTextPresets.Items.Contains(currentPreset))
                {
                    cboTextPresets.SelectedItem = currentPreset;
                }
                else if (cboTextPresets.Items.Count > 0)
                {
                    cboTextPresets.SelectedIndex = 0;
                }

                LogHelper.Debug($"已加载{cboTextPresets.Items.Count}个文字预设");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载文字预设失败: {ex.Message}", ex);
                MessageBox.Show($"加载文字预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 文字预设下拉框选择变化事件
        /// </summary>
        private void cboTextPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cboTextPresets.SelectedItem == null)
                    return;

                string presetName = cboTextPresets.SelectedItem.ToString();

                // 加载预设
                if (LoadTextPreset(presetName))
                {
                    // 刷新chkLstTextItems显示
                    chkLstTextItems.ItemCheck -= ChkLstTextItems_ItemCheck; // 临时取消事件订阅
                    LoadTextItems();
                    chkLstTextItems.ItemCheck += ChkLstTextItems_ItemCheck; // 重新订阅事件

                    LogHelper.Debug($"已应用文字预设: {presetName}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"应用文字预设失败: {ex.Message}", ex);
                MessageBox.Show($"应用文字预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 加载文字预设
        /// </summary>
        private bool LoadTextPreset(string presetName)
        {
            try
            {
                string presetKey = $"TextPreset_{presetName}";
                string textItems = AppSettings.Get(presetKey) as string;

                if (!string.IsNullOrEmpty(textItems))
                {
                    AppSettings.Set("TextItems", textItems);
                    AppSettings.Set("CurrentTextPreset", presetName);
                    return true;
                }

                // 如果没有找到预设，则使用内置预设
                switch (presetName)
                {
                    case "默认文字":
                        string defaultItems = "正则结果|True|订单号|True|材料|True|数量|True|工艺|True|尺寸|True|序号|True|列组合|True|行数|True|列数|True";
                        AppSettings.Set("TextItems", defaultItems);
                        AppSettings.Set("CurrentTextPreset", presetName);
                        return true;

                    case "基本信息":
                        string basicItems = "订单号|True|材料|True|数量|True|工艺|True|尺寸|True";
                        AppSettings.Set("TextItems", basicItems);
                        AppSettings.Set("CurrentTextPreset", presetName);
                        return true;

                    case "常用组合":
                        string commonItems = "订单号|True|材料|True|数量|True|工艺|True|序号|True";
                        AppSettings.Set("TextItems", commonItems);
                        AppSettings.Set("CurrentTextPreset", presetName);
                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载文字预设失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 另存为文字预设按钮点击事件
        /// </summary>
        private void btnSaveTextPreset_Click(object sender, EventArgs e)
        {
            try
            {
                // 使用简单的输入对话框
                string presetName = PromptForPresetName("请输入文字预设名称", "另存为文字预设", "");

                if (string.IsNullOrWhiteSpace(presetName))
                    return;

                // 获取当前文字项状态
                var currentTextItems = GetCurrentTextItems();

                // 保存为新预设
                if (SaveTextAsPreset(presetName, currentTextItems))
                {
                    // 刷新预设列表
                    LoadTextPresets();
                    cboTextPresets.SelectedItem = presetName;

                    LogHelper.Debug($"已保存文字预设: {presetName}");
                    MessageBox.Show($"文字预设 '{presetName}' 已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存文字预设失败: {ex.Message}", ex);
                MessageBox.Show($"保存文字预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 保存文字预设
        /// </summary>
        private bool SaveTextAsPreset(string presetName, string textItems)
        {
            try
            {
                string presetKey = $"TextPreset_{presetName}";
                AppSettings.Set(presetKey, textItems);

                // 更新预设列表
                string customPresets = AppSettings.Get("TextPresets") as string ?? "";
                var presetList = new List<string>();
                if (!string.IsNullOrEmpty(customPresets))
                {
                    presetList = customPresets.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                if (!presetList.Contains(presetName))
                {
                    presetList.Add(presetName);
                    AppSettings.Set("TextPresets", string.Join("|", presetList));
                }

                AppSettings.Save();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存文字预设失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 删除文字预设按钮点击事件
        /// </summary>
        private void btnDeleteTextPreset_Click(object sender, EventArgs e)
        {
            try
            {
                if (cboTextPresets.SelectedItem == null)
                {
                    MessageBox.Show("请先选择要删除的文字预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string presetName = cboTextPresets.SelectedItem.ToString();

                // 检查是否为内置预设
                if (presetName == "默认文字" || presetName == "基本信息" || presetName == "时间信息")
                {
                    MessageBox.Show("内置预设不能删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show(
                    $"确定要删除文字预设 '{presetName}' 吗？",
                    "确认删除",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (DeleteTextPreset(presetName))
                    {
                        LoadTextPresets();
                        LogHelper.Debug($"已删除文字预设: {presetName}");
                        MessageBox.Show("删除成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"删除文字预设失败: {ex.Message}", ex);
                MessageBox.Show($"删除文字预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 删除文字预设
        /// </summary>
        private bool DeleteTextPreset(string presetName)
        {
            try
            {
                // 删除预设数据
                string presetKey = $"TextPreset_{presetName}";
                AppSettings.Set(presetKey, null);

                // 更新预设列表
                string customPresets = AppSettings.Get("TextPresets") as string ?? "";
                var presetList = new List<string>();
                if (!string.IsNullOrEmpty(customPresets))
                {
                    presetList = customPresets.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                presetList.Remove(presetName);
                AppSettings.Set("TextPresets", string.Join("|", presetList));

                AppSettings.Save();
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"删除文字预设失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 获取当前chkLstTextItems的TextItems字符串
        /// </summary>
        private string GetCurrentTextItems()
        {
            List<string> items = new List<string>();
            foreach (var item in chkLstTextItems.Items)
            {
                int index = chkLstTextItems.Items.IndexOf(item);
                items.Add(item.ToString());
                items.Add(chkLstTextItems.GetItemChecked(index).ToString());
            }
            return string.Join("|", items);
        }

        #endregion

        /// <summary>
        /// 触发导出路径设置变更事件
        /// </summary>
        protected virtual void OnExportPathSettingsChanged(EventArgs e)
        {
            ExportPathSettingsChanged?.Invoke(this, e);
        }

        #region 印刷排版功能事件处理

        /// <summary>
        /// 印刷排版设置变更事件处理
        /// </summary>
        private void OnImpositionSettingsChanged(object sender, EventArgs e)
        {
            try
            {
                // SettingsForm是配置窗口，不包含计算逻辑
                // 当启用状态改变时，只保存启用状态
                LogHelper.Debug("[OnImpositionSettingsChanged] 排版启用状态已变化");

                // 只保存启用状态，不保存其他参数
                if (chkEnableImposition != null)
                {
                    AppSettings.Set("Imposition_Enabled", chkEnableImposition.Checked);
                    LogHelper.Debug($"[OnImpositionSettingsChanged] 保存排版启用状态: {chkEnableImposition.Checked}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[OnImpositionSettingsChanged] 处理排版设置变更时发生错误: {ex.Message}");
            }
        }

  
        /// <summary>
        /// 排版参数变更事件处理
        /// </summary>
        private void OnImpositionParametersChanged(object sender, EventArgs e)
        {
            try
            {
                // SettingsForm是配置窗口，不包含计算逻辑
                LogHelper.Debug("[OnImpositionParametersChanged] 排版参数已更新（仅配置，不计算）");

                // 根据发送事件的控件判断保存哪个分组控件的设置
                if (sender is Control control)
                {
                    SaveImpositionSettingsByControl(control);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[OnImpositionParametersChanged] 处理排版参数变更时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 计算并更新布局显示
        /// </summary>
        // SettingsForm是配置窗口，不包含计算逻辑
        // 计算逻辑已移至MaterialSelectFormModern

        /// <summary>
        /// 更新布局显示
        /// </summary>
        private void UpdateLayoutDisplay(string status = null)
        {
            try
            {
                if (status != null)
                {
                    // SettingsForm是配置窗口，不显示计算结果
                    return;
                }

                // 这里会根据计算结果更新显示
                // 目前暂时显示占位符
                // SettingsForm是配置窗口，不显示计算结果
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[UpdateLayoutDisplay] 更新布局显示时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新布局显示（基于计算结果）
        /// </summary>
        /// <param name="result">布局计算结果</param>
        private void UpdateLayoutDisplay(ImpositionResult result)
        {
            try
            {
                if (result == null)
                {
                    UpdateLayoutDisplay("计算结果为空");
                    return;
                }

                if (!result.Success)
                {
                    UpdateLayoutDisplay(result.ErrorMessage ?? "计算失败");
                    return;
                }

                // 更新布局结果显示
                // SettingsForm是配置窗口，不显示计算结果

                // 显示计算结果验证和提示
                DisplayCalculationValidationAndTips(result);

                LogHelper.Debug($"[UpdateLayoutDisplay] 布局结果: {result.Description}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[UpdateLayoutDisplay] 更新布局结果显示时发生错误: {ex.Message}");
                UpdateLayoutDisplay("显示错误");
            }
        }

        /// <summary>
        /// 显示计算结果验证和提示信息
        /// </summary>
        private void DisplayCalculationValidationAndTips(ImpositionResult result)
        {
            try
            {
                var tips = new List<string>();
                var validationMessages = new List<string>();

                // 基本成功性验证
                if (!result.Success)
                {
                    validationMessages.Add($"❌ 计算失败: {result.ErrorMessage}");
                }
                else
                {
                    validationMessages.Add("✅ 计算成功");
                }

                // 材料利用率分析
                if (result.SpaceUtilization < 50)
                {
                    tips.Add("⚠️ 材料利用率较低 (< 50%)，建议调整材料尺寸或布局参数");
                }
                else if (result.SpaceUtilization > 90)
                {
                    tips.Add("✅ 材料利用率优秀 (> 90%)");
                }
                else if (result.SpaceUtilization > 75)
                {
                    tips.Add("✅ 材料利用率良好 (> 75%)");
                }

                // 布局数量建议
                if (result.OptimalLayoutQuantity == 1)
                {
                    tips.Add("💡 当前布局每页只能放置1个页面，考虑使用更小的材料尺寸");
                }
                else if (result.OptimalLayoutQuantity >= 8)
                {
                    tips.Add("✅ 布局效率高，每页可放置多个页面");
                }

                // 行列配置分析
                if (result.Rows == 0 && result.Columns == 0)
                {
                    tips.Add("🔄 自动计算行列数，系统将根据材料尺寸和页面尺寸优化布局");
                }
                else if (result.Rows > 0 && result.Columns > 0)
                {
                    tips.Add($"📐 手动配置: {result.Rows}行 × {result.Columns}列 = {result.Rows * result.Columns}个位置");
                    if (result.Rows * result.Columns > result.OptimalLayoutQuantity)
                    {
                        tips.Add($"📊 实际使用: {result.OptimalLayoutQuantity}个页面，有 {result.Rows * result.Columns - result.OptimalLayoutQuantity} 个空位");
                    }
                }

                // 材料类型相关提示
                tips.Add("📄 平张模式: 适合标准尺寸的单页印刷");
                tips.Add("🔄 卷装模式: 适合大批量连续印刷");

                // 边距合理性检查 - 平张材料
                var flatConfig = GetFlatSheetConfiguration();
                if (flatConfig != null)
                {
                    var totalMarginX = flatConfig.MarginLeft + flatConfig.MarginRight;
                    var totalMarginY = flatConfig.MarginTop + flatConfig.MarginBottom;

                    if (totalMarginX > flatConfig.PaperWidth * 0.3)
                    {
                        tips.Add("⚠️ 平张材料左右边距总和较大，可能影响有效利用面积");
                    }

                    if (totalMarginY > flatConfig.PaperHeight * 0.3)
                    {
                        tips.Add("⚠️ 平张材料上下边距总和较大，可能影响有效利用面积");
                    }
                }

                // 边距合理性检查 - 卷装材料
                var rollConfig = GetRollMaterialConfiguration();
                if (rollConfig != null)
                {
                    var totalMarginX = rollConfig.MarginLeft + rollConfig.MarginRight;
                    var totalMarginY = rollConfig.MarginTop + rollConfig.MarginBottom;

                    if (totalMarginX > rollConfig.FixedWidth * 0.3)
                    {
                        tips.Add("⚠️ 卷装材料左右边距总和较大，可能影响有效利用面积");
                    }

                    if (totalMarginY > rollConfig.MinLength * 0.3)
                    {
                        tips.Add("⚠️ 卷装材料上下边距总和较大，可能影响有效利用面积");
                    }
                }

                // 显示所有提示信息
                if (validationMessages.Count > 0 || tips.Count > 0)
                {
                    var allMessages = new List<string>();
                    allMessages.AddRange(validationMessages);
                    allMessages.AddRange(tips);

                    LogHelper.Info($"[SettingsForm] 计算结果验证: {string.Join("; ", allMessages)}");

                    // 可以在这里添加UI显示逻辑，例如在状态栏或工具提示中显示这些信息
                    // 暂时通过日志输出，后续可以根据需要添加专门的显示控件
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[SettingsForm] 显示计算结果验证时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 清空布局显示
        /// </summary>
        private void ClearLayoutDisplay()
        {
            try
            {
                // SettingsForm是配置窗口，不显示计算结果

                LogHelper.Debug("[ClearLayoutDisplay] 布局显示已清空");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[ClearLayoutDisplay] 清空布局显示时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取平张材料配置
        /// </summary>
        private FlatSheetConfiguration GetFlatSheetConfiguration()
        {
            try
            {
                return new FlatSheetConfiguration
                {
                    PaperWidth = GetFloatValue(txtPaperWidth.Text, 210f),
                    PaperHeight = GetFloatValue(txtPaperHeight.Text, 297f),
                    MarginTop = GetFloatValue(txtMarginTop.Text, 10f),
                    MarginBottom = GetFloatValue(txtMarginBottom.Text, 10f),
                    MarginLeft = GetFloatValue(txtMarginLeft.Text, 10f),
                    MarginRight = GetFloatValue(txtMarginRight.Text, 10f),
                    Rows = GetIntValue(txtRows.Text, 0),
                    Columns = GetIntValue(txtColumns.Text, 0)
                };
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[GetFlatSheetConfiguration] 获取平张材料配置时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取卷装材料配置
        /// </summary>
        private RollMaterialConfiguration GetRollMaterialConfiguration()
        {
            try
            {
                return new RollMaterialConfiguration
                {
                    FixedWidth = GetFloatValue(txtFixedWidth.Text, 210f),
                    MinLength = GetFloatValue(txtMinLength.Text, 297f),
                    MarginTop = GetFloatValue(txtRollMarginTop.Text, 10f),
                    MarginBottom = GetFloatValue(txtRollMarginBottom.Text, 10f),
                    MarginLeft = GetFloatValue(txtRollMarginLeft.Text, 10f),
                    MarginRight = GetFloatValue(txtRollMarginRight.Text, 10f),
                    Rows = GetIntValue(txtRows.Text, 0),
                    Columns = GetIntValue(txtColumns.Text, 0)
                };
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[GetRollMaterialConfiguration] 获取卷装材料配置时发生错误: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取浮点数值
        /// </summary>
        private float GetFloatValue(string text, float defaultValue)
        {
            if (float.TryParse(text, out float result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 获取整数值
        /// </summary>
        private int GetIntValue(string text, int defaultValue)
        {
            if (int.TryParse(text, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// 初始化排版控件
        /// </summary>
        private void InitializeImpositionControls()
        {
            try
            {
                // 首先移除所有可能存在的事件处理器
                RemoveImpositionParameterEventHandlers();

                // 设置默认值（此时不会有事件被触发）
                SetDefaultImpositionValues();

                // 重新设置事件处理器
                SetupImpositionParameterEventHandlers();

                LogHelper.Debug("[InitializeImpositionControls] 排版控件初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[InitializeImpositionControls] 初始化排版控件时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置排版参数变化事件处理器
        /// </summary>
private void RemoveImpositionParameterEventHandlers()
        {
            try
            {
                // 移除所有事件处理器以避免在初始化时触发
                if (txtPaperWidth != null)
                {
                    txtPaperWidth.TextChanged -= OnImpositionParameterChanged;
                    txtPaperWidth.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtPaperHeight != null)
                {
                    txtPaperHeight.TextChanged -= OnImpositionParameterChanged;
                    txtPaperHeight.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtRows != null)
                {
                    txtRows.TextChanged -= OnImpositionParameterChanged;
                    txtRows.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtColumns != null)
                {
                    txtColumns.TextChanged -= OnImpositionParameterChanged;
                    txtColumns.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtFixedWidth != null)
                {
                    txtFixedWidth.TextChanged -= OnImpositionParameterChanged;
                    txtFixedWidth.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtMinLength != null)
                {
                    txtMinLength.TextChanged -= OnImpositionParameterChanged;
                    txtMinLength.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtMarginTop != null)
                {
                    txtMarginTop.TextChanged -= OnImpositionParameterChanged;
                    txtMarginTop.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtMarginBottom != null)
                {
                    txtMarginBottom.TextChanged -= OnImpositionParameterChanged;
                    txtMarginBottom.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtMarginLeft != null)
                {
                    txtMarginLeft.TextChanged -= OnImpositionParameterChanged;
                    txtMarginLeft.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtMarginRight != null)
                {
                    txtMarginRight.TextChanged -= OnImpositionParameterChanged;
                    txtMarginRight.LostFocus -= OnImpositionParameterChanged;
                }

                // 移除卷装专用边距控件的事件处理器
                if (txtRollMarginTop != null)
                {
                    txtRollMarginTop.TextChanged -= OnImpositionParameterChanged;
                    txtRollMarginTop.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtRollMarginBottom != null)
                {
                    txtRollMarginBottom.TextChanged -= OnImpositionParameterChanged;
                    txtRollMarginBottom.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtRollMarginLeft != null)
                {
                    txtRollMarginLeft.TextChanged -= OnImpositionParameterChanged;
                    txtRollMarginLeft.LostFocus -= OnImpositionParameterChanged;
                }

                if (txtRollMarginRight != null)
                {
                    txtRollMarginRight.TextChanged -= OnImpositionParameterChanged;
                    txtRollMarginRight.LostFocus -= OnImpositionParameterChanged;
                }

                
                LogHelper.Debug("[RemoveImpositionParameterEventHandlers] 排版参数事件处理器已移除");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[RemoveImpositionParameterEventHandlers] 移除排版参数事件处理器时发生错误: {ex.Message}");
            }
        }

        private void SetupImpositionParameterEventHandlers()
        {
            try
            {
                // 暂时移除事件处理器以避免在初始化时触发
                RemoveImpositionParameterEventHandlers();

                // 平张模式参数事件
                if (txtPaperWidth != null)
                {
                    txtPaperWidth.TextChanged += OnImpositionParameterChanged;
                    txtPaperWidth.LostFocus += OnImpositionParameterChanged;
                }

                if (txtPaperHeight != null)
                {
                    txtPaperHeight.TextChanged += OnImpositionParameterChanged;
                    txtPaperHeight.LostFocus += OnImpositionParameterChanged;
                }

                if (txtRows != null)
                {
                    txtRows.TextChanged += OnImpositionParameterChanged;
                    txtRows.LostFocus += OnImpositionParameterChanged;
                }

                if (txtColumns != null)
                {
                    txtColumns.TextChanged += OnImpositionParameterChanged;
                    txtColumns.LostFocus += OnImpositionParameterChanged;
                }

                // 卷装模式参数事件
                if (txtFixedWidth != null)
                {
                    txtFixedWidth.TextChanged += OnImpositionParameterChanged;
                    txtFixedWidth.LostFocus += OnImpositionParameterChanged;
                }

                if (txtMinLength != null)
                {
                    txtMinLength.TextChanged += OnImpositionParameterChanged;
                    txtMinLength.LostFocus += OnImpositionParameterChanged;
                }

                // 边距参数事件（两种模式共用）
                if (txtMarginTop != null)
                {
                    txtMarginTop.TextChanged += OnImpositionParameterChanged;
                    txtMarginTop.LostFocus += OnImpositionParameterChanged;
                }

                if (txtMarginBottom != null)
                {
                    txtMarginBottom.TextChanged += OnImpositionParameterChanged;
                    txtMarginBottom.LostFocus += OnImpositionParameterChanged;
                }

                if (txtMarginLeft != null)
                {
                    txtMarginLeft.TextChanged += OnImpositionParameterChanged;
                    txtMarginLeft.LostFocus += OnImpositionParameterChanged;
                }

                if (txtMarginRight != null)
                {
                    txtMarginRight.TextChanged += OnImpositionParameterChanged;
                    txtMarginRight.LostFocus += OnImpositionParameterChanged;
                }

                // 卷装模式专用边距参数事件
                if (txtRollMarginTop != null)
                {
                    txtRollMarginTop.TextChanged += OnImpositionParameterChanged;
                    txtRollMarginTop.LostFocus += OnImpositionParameterChanged;
                }

                if (txtRollMarginBottom != null)
                {
                    txtRollMarginBottom.TextChanged += OnImpositionParameterChanged;
                    txtRollMarginBottom.LostFocus += OnImpositionParameterChanged;
                }

                if (txtRollMarginLeft != null)
                {
                    txtRollMarginLeft.TextChanged += OnImpositionParameterChanged;
                    txtRollMarginLeft.LostFocus += OnImpositionParameterChanged;
                }

                if (txtRollMarginRight != null)
                {
                    txtRollMarginRight.TextChanged += OnImpositionParameterChanged;
                    txtRollMarginRight.LostFocus += OnImpositionParameterChanged;
                }

                
                LogHelper.Debug("[SetupImpositionParameterEventHandlers] 排版参数事件处理器设置完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[SetupImpositionParameterEventHandlers] 设置排版参数事件处理器时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 排版参数变化事件处理器
        /// </summary>
        private void OnImpositionParameterChanged(object sender, EventArgs e)
        {
            try
            {
                // SettingsForm是配置窗口，不触发计算逻辑
                // 计算逻辑由MaterialSelectFormModern处理
                LogHelper.Debug("[OnImpositionParameterChanged] 排版参数已变化（仅配置，不计算）");

                // 根据发送事件的控件判断保存哪个分组控件的设置
                if (sender is Control control)
                {
                    SaveImpositionSettingsByControl(control);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[OnImpositionParameterChanged] 处理排版参数变化时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据控件类型保存对应的排版设置
        /// </summary>
        private void SaveImpositionSettingsByControl(Control sender)
        {
            try
            {
                LogHelper.Debug("[SaveImpositionSettingsByControl] 开始保存印刷排版设置");

                // 检查发送事件的控件属于哪个分组
                bool isFlatSheetControl = IsControlInGroup(sender, grpFlatSheetSettings);
                bool isRollMaterialControl = IsControlInGroup(sender, grpRollMaterialSettings);

                LogHelper.Debug($"[SaveImpositionSettingsByControl] 控件类型: 平张={isFlatSheetControl}, 卷装={isRollMaterialControl}");

                // 保存排版启用状态（总是保存）
                if (chkEnableImposition != null)
                {
                    AppSettings.Set("Imposition_Enabled", chkEnableImposition.Checked);
                }

                // 保存行列设置（两个分组共享）
                if (txtRows != null && !string.IsNullOrEmpty(txtRows.Text))
                {
                    AppSettings.Set("Imposition_Rows", txtRows.Text.Trim());
                }

                if (txtColumns != null && !string.IsNullOrEmpty(txtColumns.Text))
                {
                    AppSettings.Set("Imposition_Columns", txtColumns.Text.Trim());
                }

                // 根据控件类型保存对应的设置
                if (isFlatSheetControl)
                {
                    // 保存平张材料设置
                    if (txtPaperWidth != null && !string.IsNullOrEmpty(txtPaperWidth.Text))
                    {
                        AppSettings.Set("Imposition_PaperWidth", txtPaperWidth.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存平张材料纸张宽度: {txtPaperWidth.Text}");
                    }

                    if (txtPaperHeight != null && !string.IsNullOrEmpty(txtPaperHeight.Text))
                    {
                        AppSettings.Set("Imposition_PaperHeight", txtPaperHeight.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存平张材料纸张高度: {txtPaperHeight.Text}");
                    }

                    // 保存平张材料边距设置
                    if (txtMarginTop != null && !string.IsNullOrEmpty(txtMarginTop.Text))
                    {
                        AppSettings.Set("Imposition_MarginTop", txtMarginTop.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存平张材料顶部边距: {txtMarginTop.Text}");
                    }

                    if (txtMarginBottom != null && !string.IsNullOrEmpty(txtMarginBottom.Text))
                    {
                        AppSettings.Set("Imposition_MarginBottom", txtMarginBottom.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存平张材料底部边距: {txtMarginBottom.Text}");
                    }

                    if (txtMarginLeft != null && !string.IsNullOrEmpty(txtMarginLeft.Text))
                    {
                        AppSettings.Set("Imposition_MarginLeft", txtMarginLeft.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存平张材料左侧边距: {txtMarginLeft.Text}");
                    }

                    if (txtMarginRight != null && !string.IsNullOrEmpty(txtMarginRight.Text))
                    {
                        AppSettings.Set("Imposition_MarginRight", txtMarginRight.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存平张材料右侧边距: {txtMarginRight.Text}");
                    }
                }
                else if (isRollMaterialControl)
                {
                    // 保存卷装材料设置
                    if (txtFixedWidth != null && !string.IsNullOrEmpty(txtFixedWidth.Text))
                    {
                        AppSettings.Set("Imposition_FixedWidth", txtFixedWidth.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存卷装材料固定宽度: {txtFixedWidth.Text}");
                    }

                    if (txtMinLength != null && !string.IsNullOrEmpty(txtMinLength.Text))
                    {
                        AppSettings.Set("Imposition_MinLength", txtMinLength.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存卷装材料最小长度: {txtMinLength.Text}");
                    }

                    // 保存卷装材料专用边距设置
                    if (txtRollMarginTop != null && !string.IsNullOrEmpty(txtRollMarginTop.Text))
                    {
                        AppSettings.Set("Imposition_RollMarginTop", txtRollMarginTop.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存卷装材料顶部边距: {txtRollMarginTop.Text}");
                    }

                    if (txtRollMarginBottom != null && !string.IsNullOrEmpty(txtRollMarginBottom.Text))
                    {
                        AppSettings.Set("Imposition_RollMarginBottom", txtRollMarginBottom.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存卷装材料底部边距: {txtRollMarginBottom.Text}");
                    }

                    if (txtRollMarginLeft != null && !string.IsNullOrEmpty(txtRollMarginLeft.Text))
                    {
                        AppSettings.Set("Imposition_RollMarginLeft", txtRollMarginLeft.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存卷装材料左侧边距: {txtRollMarginLeft.Text}");
                    }

                    if (txtRollMarginRight != null && !string.IsNullOrEmpty(txtRollMarginRight.Text))
                    {
                        AppSettings.Set("Imposition_RollMarginRight", txtRollMarginRight.Text.Trim());
                        LogHelper.Debug($"[SaveImpositionSettingsByControl] 保存卷装材料右侧边距: {txtRollMarginRight.Text}");
                    }
                }

                LogHelper.Debug("[SaveImpositionSettingsByControl] 印刷排版设置保存完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[SaveImpositionSettingsByControl] 保存印刷排版设置时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查控件是否属于指定的分组
        /// </summary>
        private bool IsControlInGroup(Control control, GroupBox groupBox)
        {
            if (control == null || groupBox == null)
                return false;

            // 检查控件是否直接在分组控件中
            Control parent = control.Parent;
            while (parent != null)
            {
                if (parent == groupBox)
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        /// <summary>
        /// 设置排版默认值
        /// </summary>
        private void SetDefaultImpositionValues()
        {
            try
            {
                // 只在控件为空时才设置默认值，避免覆盖用户已保存的设置
                if (string.IsNullOrEmpty(txtPaperWidth.Text))
                    txtPaperWidth.Text = "210";
                
                if (string.IsNullOrEmpty(txtPaperHeight.Text))
                    txtPaperHeight.Text = "297";
                
                if (string.IsNullOrEmpty(txtMarginTop.Text))
                    txtMarginTop.Text = "10";
                
                if (string.IsNullOrEmpty(txtMarginBottom.Text))
                    txtMarginBottom.Text = "10";
                
                if (string.IsNullOrEmpty(txtMarginLeft.Text))
                    txtMarginLeft.Text = "10";
                
                if (string.IsNullOrEmpty(txtMarginRight.Text))
                    txtMarginRight.Text = "10";
                
                if (string.IsNullOrEmpty(txtRows.Text))
                    txtRows.Text = "0"; // 0表示自动计算
                
                if (string.IsNullOrEmpty(txtColumns.Text))
                    txtColumns.Text = "0"; // 0表示自动计算

                if (string.IsNullOrEmpty(txtFixedWidth.Text))
                    txtFixedWidth.Text = "210";
                
                if (string.IsNullOrEmpty(txtMinLength.Text))
                    txtMinLength.Text = "297";

                // 初始化卷装专用边距控件的默认值
                if (string.IsNullOrEmpty(txtRollMarginTop.Text))
                    txtRollMarginTop.Text = "10";

                if (string.IsNullOrEmpty(txtRollMarginBottom.Text))
                    txtRollMarginBottom.Text = "10";

                if (string.IsNullOrEmpty(txtRollMarginLeft.Text))
                    txtRollMarginLeft.Text = "10";

                if (string.IsNullOrEmpty(txtRollMarginRight.Text))
                    txtRollMarginRight.Text = "10";

                // 平铺显示：确保两个设置组都显示
                grpFlatSheetSettings.Visible = true;
                grpRollMaterialSettings.Visible = true;

                LogHelper.Debug("[SetDefaultImpositionValues] 排版默认值设置完成（仅在控件为空时设置）");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[SetDefaultImpositionValues] 设置默认值时发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置当前MaterialSelectFormModern实例的引用
        /// </summary>
        /// <param name="materialSelectForm">MaterialSelectFormModern实例</param>
        public static void SetMaterialSelectFormReference(MaterialSelectFormModern materialSelectForm)
        {
            _currentMaterialSelectForm = materialSelectForm;

            // 同时保存当前的布局计算结果
            if (materialSelectForm != null)
            {
                _savedLayoutRows = materialSelectForm.GetRows();
                _savedLayoutColumns = materialSelectForm.GetColumns();
                LogHelper.Debug($"[SetMaterialSelectFormReference] 保存布局计算结果: 行数={_savedLayoutRows}, 列数={_savedLayoutColumns}");
            }
        }

        /// <summary>
        /// 清除MaterialSelectFormModern引用
        /// </summary>
        public static void ClearMaterialSelectFormReference()
        {
            _currentMaterialSelectForm = null;
        }

        /// <summary>
        /// 更新布局计算结果（由MaterialSelectFormModern在计算完成后调用）
        /// </summary>
        /// <param name="rows">行数</param>
        /// <param name="columns">列数</param>
        public static void UpdateLayoutResults(int rows, int columns)
        {
            _savedLayoutRows = rows;
            _savedLayoutColumns = columns;
            LogHelper.Debug($"[UpdateLayoutResults] 更新布局计算结果: 行数={rows}, 列数={columns}");
        }

        /// <summary>
        /// 测试方法 - 手动设置布局计算结果用于测试
        /// </summary>
        public static void TestSetLayoutResults(int rows, int columns)
        {
            _savedLayoutRows = rows;
            _savedLayoutColumns = columns;
            LogHelper.Debug($"[TestSetLayoutResults] 测试设置布局计算结果: 行数={rows}, 列数={columns}");
            MessageBox.Show($"测试设置完成: 行数={rows}, 列数={columns}\n\n现在可以测试重命名功能。", "测试布局结果", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 获取布局计算的行数（用于重命名）
        /// </summary>
        /// <returns>行数字符串，如果没有有效结果则返回空字符串</returns>
        public static string GetLayoutRowsForRenaming()
        {
            // 首先检查EventGroup配置中"行数"项目是否启用
            var eventGroupConfig = GetEventGroupConfiguration();
            bool rowItemEnabled = false;
            if (eventGroupConfig?.Items != null)
            {
                var rowItem = eventGroupConfig.Items.FirstOrDefault(item => item.Name == "行数");
                rowItemEnabled = rowItem?.IsEnabled ?? false;
                LogHelper.Debug($"[GetLayoutRowsForRenaming] EventGroup中'行数'项目启用状态: {rowItemEnabled}");
            }

            // 如果EventGroup中的"行数"项目未启用，直接返回空字符串
            if (!rowItemEnabled)
            {
                LogHelper.Debug("[GetLayoutRowsForRenaming] '行数'项目未启用，返回空字符串");
                return "";
            }

            if (_currentMaterialSelectForm != null)
            {
                int rows = _currentMaterialSelectForm.GetRows();
                string result = rows > 0 ? rows.ToString() : "";
                LogHelper.Debug($"[GetLayoutRowsForRenaming] 行数={rows}, 返回='{result}'");
                return result;
            }

            // 如果实例不存在，使用保存的结果
            string savedResult = _savedLayoutRows > 0 ? _savedLayoutRows.ToString() : "";
            LogHelper.Debug($"[GetLayoutRowsForRenaming] 使用保存的行数={_savedLayoutRows}, 返回='{savedResult}'");
            return savedResult;
        }

        /// <summary>
        /// 获取布局计算的列数（用于重命名）
        /// </summary>
        /// <returns>列数字符串，如果没有有效结果则返回空字符串</returns>
        public static string GetLayoutColumnsForRenaming()
        {
            // 首先检查EventGroup配置中"列数"项目是否启用
            var eventGroupConfig = GetEventGroupConfiguration();
            bool columnItemEnabled = false;
            if (eventGroupConfig?.Items != null)
            {
                var columnItem = eventGroupConfig.Items.FirstOrDefault(item => item.Name == "列数");
                columnItemEnabled = columnItem?.IsEnabled ?? false;
                LogHelper.Debug($"[GetLayoutColumnsForRenaming] EventGroup中'列数'项目启用状态: {columnItemEnabled}");
            }

            // 如果EventGroup中的"列数"项目未启用，直接返回空字符串
            if (!columnItemEnabled)
            {
                LogHelper.Debug("[GetLayoutColumnsForRenaming] '列数'项目未启用，返回空字符串");
                return "";
            }

            if (_currentMaterialSelectForm != null)
            {
                int columns = _currentMaterialSelectForm.GetColumns();
                string result = columns > 0 ? columns.ToString() : "";
                LogHelper.Debug($"[GetLayoutColumnsForRenaming] 列数={columns}, 返回='{result}'");
                return result;
            }

            // 如果实例不存在，使用保存的结果
            string savedResult = _savedLayoutColumns > 0 ? _savedLayoutColumns.ToString() : "";
            LogHelper.Debug($"[GetLayoutColumnsForRenaming] 使用保存的列数={_savedLayoutColumns}, 返回='{savedResult}'");
            return savedResult;
        }

        /// <summary>
        /// 清除布局计算结果
        /// </summary>
        public static void ClearLayoutResults()
        {
            _savedLayoutRows = 0;
            _savedLayoutColumns = 0;
            LogHelper.Debug($"[ClearLayoutResults] 已清除布局计算结果: 行数=0, 列数=0");
        }

        /// <summary>
        /// 获取EventGroup配置
        /// </summary>
        /// <returns>EventGroup配置对象</returns>
        public static EventGroupConfiguration GetEventGroupConfiguration()
        {
            try
            {
                // 获取当前选中的预设方案名称
                string currentPresetName = AppSettings.Instance["LastSelectedEventPreset"]?.ToString() ?? "默认配置";
                LogHelper.Debug($"当前选中的预设方案: {currentPresetName}");
                
                // 从CustomSettings中获取对应预设方案的配置
                string presetKey = $"EventItemsPreset_{currentPresetName}";
                string configJson = AppSettings.Instance[presetKey]?.ToString() ?? "";
                
                LogHelper.Debug($"预设方案配置键: {presetKey}");
                LogHelper.Debug($"配置JSON长度: {configJson.Length}");
                
                if (string.IsNullOrEmpty(configJson))
                {
                    LogHelper.Debug($"预设方案 {currentPresetName} 配置为空，使用默认配置");
                    // 如果没有找到预设方案配置，返回默认配置
                    return EventGroupConfiguration.GetDefault();
                }

                // 尝试解析JSON配置
                var config = Newtonsoft.Json.JsonConvert.DeserializeObject<EventGroupConfiguration>(configJson);
                if (config == null)
                {
                    LogHelper.Debug($"预设方案 {currentPresetName} 解析失败，使用默认配置");
                    return EventGroupConfiguration.GetDefault();
                }
                
                LogHelper.Debug($"成功加载预设方案 {currentPresetName}，包含 {config.Groups?.Count ?? 0} 个分组，{config.Items?.Count ?? 0} 个项目");
                return config;
            }
            catch (Exception ex)
            {
                LogHelper.Debug($"获取EventGroup配置时出错: {ex.Message}");
                LogHelper.Debug($"异常堆栈: {ex.StackTrace}");
                // 出错时返回默认配置
                return EventGroupConfiguration.GetDefault();
            }
        }

        /// <summary>
        /// 获取当前MaterialSelectFormModern实例的行数
        /// </summary>
        /// <returns>行数，如果没有实例或未计算则返回0</returns>
        private int GetLayoutRows()
        {
            if (_currentMaterialSelectForm != null)
            {
                return _currentMaterialSelectForm.GetRows();
            }
            return 0;
        }

        /// <summary>
        /// 获取当前MaterialSelectFormModern实例的列数
        /// </summary>
        /// <returns>列数，如果没有实例或未计算则返回0</returns>
        private int GetLayoutColumns()
        {
            if (_currentMaterialSelectForm != null)
            {
                return _currentMaterialSelectForm.GetColumns();
            }
            return 0;
        }

        #endregion

        #region TreeView事件处理

        /// <summary>
        /// TreeView节点勾选事件
        /// </summary>
        private void TreeViewEvents_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            var nodeData = e.Node.Tag as TreeNodeData;
            if (nodeData == null) return;

            // 更新数据
            nodeData.IsEnabled = e.Node.Checked;

            if (nodeData.NodeType == TreeNodeType.Group)
            {
                // 分组节点：更新分组配置和所有子项目
                var groupConfig = _eventGroupConfigs.FirstOrDefault(g => g.Group == nodeData.Group);
                if (groupConfig != null)
                {
                    groupConfig.IsEnabled = e.Node.Checked;
                }

                // 更新所有子项目的勾选状态
                foreach (TreeNode childNode in e.Node.Nodes)
                {
                    childNode.Checked = e.Node.Checked;
                    var childData = childNode.Tag as TreeNodeData;
                    if (childData != null)
                    {
                        childData.IsEnabled = e.Node.Checked;
                        // 更新EventItem
                        var eventItem = _eventItems.FirstOrDefault(item => item.Name == childData.ItemName);
                        if (eventItem != null)
                        {
                            eventItem.IsEnabled = e.Node.Checked;
                        }
                    }
                }
            }
            else if (nodeData.NodeType == TreeNodeType.Item)
            {
                // 项目节点：更新对应的EventItem
                var eventItem = _eventItems.FirstOrDefault(item => item.Name == nodeData.ItemName);
                if (eventItem != null)
                {
                    eventItem.IsEnabled = e.Node.Checked;
                }
            }

            // 保存设置
            SaveSettings();
        }

        /// <summary>
        /// TreeView拖拽开始事件
        /// </summary>
        private void TreeViewEvents_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node)
            {
                treeViewEvents.TreeView.DoDragDrop(node, DragDropEffects.Move | DragDropEffects.Copy);
            }
        }

        /// <summary>
        /// TreeView拖拽进入事件
        /// </summary>
        private void TreeViewEvents_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// TreeView拖拽经过事件
        /// </summary>
        private void TreeViewEvents_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                Point pt = treeViewEvents.TreeView.PointToClient(new Point(e.X, e.Y));
                TreeNode targetNode = treeViewEvents.TreeView.GetNodeAt(pt);

                if (targetNode != null)
                {
                    treeViewEvents.TreeView.SelectedNode = targetNode;
                    e.Effect = DragDropEffects.Move;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// TreeView拖拽放置事件
        /// </summary>
        private void TreeViewEvents_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(TreeNode))) return;

            TreeNode draggedNode = e.Data.GetData(typeof(TreeNode)) as TreeNode;
            if (draggedNode == null) return;

            Point pt = treeViewEvents.TreeView.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeViewEvents.TreeView.GetNodeAt(pt);

            if (targetNode == null || draggedNode == targetNode) return;

            var draggedData = draggedNode.Tag as TreeNodeData;
            var targetData = targetNode.Tag as TreeNodeData;

            if (draggedData == null || targetData == null) return;

            // 处理不同类型的拖拽
            if (draggedData.NodeType == TreeNodeType.Item && targetData.NodeType == TreeNodeType.Group)
            {
                // 项目拖拽到分组
                MoveItemToGroup(draggedNode, targetNode);
            }
            else if (draggedData.NodeType == TreeNodeType.Group && targetData.NodeType == TreeNodeType.Group)
            {
                // 分组之间调整顺序
                MoveGroupOrder(draggedNode, targetNode);
            }
            else if (draggedData.NodeType == TreeNodeType.Item && targetData.NodeType == TreeNodeType.Item)
            {
                // 项目在分组内调整顺序
                MoveItemOrder(draggedNode, targetNode);
            }
        }

        /// <summary>
        /// TreeView键盘事件
        /// </summary>
        private void TreeViewEvents_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && treeViewEvents.TreeView.SelectedNode != null)
            {
                var nodeData = treeViewEvents.TreeView.SelectedNode.Tag as TreeNodeData;
                if (nodeData != null && nodeData.NodeType == TreeNodeType.Item)
                {
                    // 将项目移回未分组
                    var ungroupedNode = FindGroupNode(Forms.Dialogs.EventGroup.Ungrouped);
                    if (ungroupedNode != null)
                    {
                        MoveItemToGroup(treeViewEvents.TreeView.SelectedNode, ungroupedNode);
                    }
                }
            }
        }

        /// <summary>
        /// 将项目移动到指定分组
        /// </summary>
        private void MoveItemToGroup(TreeNode itemNode, TreeNode targetGroupNode)
        {
            if (itemNode == null || targetGroupNode == null) return;

            var itemData = itemNode.Tag as TreeNodeData;
            var groupData = targetGroupNode.Tag as TreeNodeData;

            if (itemData?.NodeType != TreeNodeType.Item || groupData?.NodeType != TreeNodeType.Group) return;

            // 更新EventItem的分组
            var eventItem = _eventItems.FirstOrDefault(item => item.Name == itemData.ItemName);
            if (eventItem != null)
            {
                eventItem.Group = groupData.Group.Value;
            }

            // 移动节点
            itemNode.Remove();
            targetGroupNode.Nodes.Add(itemNode);

            // 更新节点数据
            itemData.Group = groupData.Group.Value;

            // 展开目标分组
            targetGroupNode.Expand();

            // 保存设置
            SaveSettings();
        }

        /// <summary>
        /// 调整分组顺序
        /// </summary>
        private void MoveGroupOrder(TreeNode draggedGroupNode, TreeNode targetGroupNode)
        {
            if (draggedGroupNode == null || targetGroupNode == null) return;

            var draggedData = draggedGroupNode.Tag as TreeNodeData;
            var targetData = targetGroupNode.Tag as TreeNodeData;

            if (draggedData?.NodeType != TreeNodeType.Group || targetData?.NodeType != TreeNodeType.Group) return;

            // 获取当前所有分组配置
            var allConfigs = _eventGroupConfigs.Where(g => g.IsEnabled).OrderBy(g => g.SortOrder).ToList();
            var draggedConfig = allConfigs.FirstOrDefault(g => g.Group == draggedData.Group);
            var targetConfig = allConfigs.FirstOrDefault(g => g.Group == targetData.Group);

            if (draggedConfig == null || targetConfig == null) return;

            int draggedIndex = allConfigs.IndexOf(draggedConfig);
            int targetIndex = allConfigs.IndexOf(targetConfig);

            System.Diagnostics.Debug.WriteLine($"[MoveGroupOrder] 插入式排序: 拖拽='{draggedConfig.DisplayName}'(索引:{draggedIndex}) -> 目标='{targetConfig.DisplayName}'(索引:{targetIndex})");

            // 实现插入式排序逻辑
            if (draggedIndex < targetIndex)
            {
                // 从上往下拖拽：插入到目标节点后面
                System.Diagnostics.Debug.WriteLine($"[MoveGroupOrder] 从上往下拖拽，将 '{draggedConfig.DisplayName}' 插入到 '{targetConfig.DisplayName}' 后面");

                // 移除被拖拽的配置
                allConfigs.RemoveAt(draggedIndex);

                // 插入到目标位置（原targetIndex因为删除会减1）
                int newTargetIndex = allConfigs.IndexOf(targetConfig);
                allConfigs.Insert(newTargetIndex + 1, draggedConfig);
            }
            else if (draggedIndex > targetIndex)
            {
                // 从下往上拖拽：插入到目标节点前面
                System.Diagnostics.Debug.WriteLine($"[MoveGroupOrder] 从下往上拖拽，将 '{draggedConfig.DisplayName}' 插入到 '{targetConfig.DisplayName}' 前面");

                // 移除被拖拽的配置
                allConfigs.RemoveAt(draggedIndex);

                // 插入到目标位置
                int newTargetIndex = allConfigs.IndexOf(targetConfig);
                allConfigs.Insert(newTargetIndex, draggedConfig);
            }
            else
            {
                // 位置相同，无需移动
                System.Diagnostics.Debug.WriteLine($"[MoveGroupOrder] 位置相同，无需移动");
                return;
            }

            // 更新所有配置的SortOrder
            for (int i = 0; i < allConfigs.Count; i++)
            {
                allConfigs[i].SortOrder = i;
                System.Diagnostics.Debug.WriteLine($"[MoveGroupOrder] 更新 '{allConfigs[i].DisplayName}' 的SortOrder为 {i}");
            }

            // 更新_eventGroupConfigs中的配置
            foreach (var config in allConfigs)
            {
                var existingConfig = _eventGroupConfigs.FirstOrDefault(g => g.Group == config.Group);
                if (existingConfig != null)
                {
                    existingConfig.SortOrder = config.SortOrder;
                }
            }

            // 直接调整TreeView中的节点顺序，避免重新加载导致显示格式丢失
            AdjustTreeViewNodeOrderWithInsert(draggedGroupNode, targetGroupNode);

            // 保存设置
            SaveSettings();

            System.Diagnostics.Debug.WriteLine($"[MoveGroupOrder] 插入式排序完成");
        }

        /// <summary>
        /// 直接调整TreeView中的节点顺序，避免重新加载
        /// </summary>
        private void AdjustTreeViewNodeOrder(TreeNode draggedNode, TreeNode targetNode)
        {
            try
            {
                // 获取TreeView中的所有分组节点
                var groupNodes = new List<TreeNode>();
                foreach (TreeNode node in treeViewEvents.TreeView.Nodes)
                {
                    if (node.Tag is TreeNodeData data && data.NodeType == TreeNodeType.Group)
                    {
                        groupNodes.Add(node);
                    }
                }

                // 按照配置的SortOrder重新排序节点
                var sortedNodes = groupNodes.OrderBy(n =>
                {
                    if (n.Tag is TreeNodeData nodeData)
                    {
                        var config = _eventGroupConfigs.FirstOrDefault(g => g.Group == nodeData.Group);
                        return config?.SortOrder ?? int.MaxValue;
                    }
                    return int.MaxValue;
                }).ToList();

                // 清空并重新添加排序后的节点
                treeViewEvents.TreeView.Nodes.Clear();
                foreach (var node in sortedNodes)
                {
                    treeViewEvents.TreeView.Nodes.Add(node);
                }

                // 刷新保留状态视觉（不重新加载节点）
                if (treeViewEvents is EventGroupsTreeView eventGroupsTreeView)
                {
                    eventGroupsTreeView.RefreshPreserveVisuals();
                }

                System.Diagnostics.Debug.WriteLine($"[AdjustTreeViewNodeOrder] 成功调整分组顺序，节点总数: {sortedNodes.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdjustTreeViewNodeOrder] 调整分组顺序失败: {ex.Message}");
                LogHelper.Error($"调整分组顺序失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 插入式调整TreeView中的节点顺序
        /// </summary>
        private void AdjustTreeViewNodeOrderWithInsert(TreeNode draggedNode, TreeNode targetNode)
        {
            try
            {
                // 获取TreeView中的所有分组节点
                var groupNodes = new List<TreeNode>();
                foreach (TreeNode node in treeViewEvents.TreeView.Nodes)
                {
                    if (node.Tag is TreeNodeData data && data.NodeType == TreeNodeType.Group)
                    {
                        groupNodes.Add(node);
                    }
                }

                int draggedIndex = groupNodes.IndexOf(draggedNode);
                int targetIndex = groupNodes.IndexOf(targetNode);

                System.Diagnostics.Debug.WriteLine($"[AdjustTreeViewNodeOrderWithInsert] TreeView插入式排序: 拖拽='{draggedNode.Text}'(索引:{draggedIndex}) -> 目标='{targetNode.Text}'(索引:{targetIndex})");

                if (draggedIndex < targetIndex)
                {
                    // 从上往下拖拽：插入到目标节点后面
                    System.Diagnostics.Debug.WriteLine($"[AdjustTreeViewNodeOrderWithInsert] 从上往下拖拽，将 '{draggedNode.Text}' 插入到 '{targetNode.Text}' 后面");

                    groupNodes.RemoveAt(draggedIndex);
                    int newTargetIndex = groupNodes.IndexOf(targetNode);
                    groupNodes.Insert(newTargetIndex + 1, draggedNode);
                }
                else if (draggedIndex > targetIndex)
                {
                    // 从下往上拖拽：插入到目标节点前面
                    System.Diagnostics.Debug.WriteLine($"[AdjustTreeViewNodeOrderWithInsert] 从下往上拖拽，将 '{draggedNode.Text}' 插入到 '{targetNode.Text}' 前面");

                    groupNodes.RemoveAt(draggedIndex);
                    int newTargetIndex = groupNodes.IndexOf(targetNode);
                    groupNodes.Insert(newTargetIndex, draggedNode);
                }
                else
                {
                    // 位置相同，无需移动
                    System.Diagnostics.Debug.WriteLine($"[AdjustTreeViewNodeOrderWithInsert] 位置相同，无需移动");
                    return;
                }

                // 清空并重新添加排序后的节点
                treeViewEvents.TreeView.Nodes.Clear();
                foreach (var node in groupNodes)
                {
                    treeViewEvents.TreeView.Nodes.Add(node);
                }

                // 刷新保留状态视觉（不重新加载节点）
                if (treeViewEvents is EventGroupsTreeView eventGroupsTreeView)
                {
                    eventGroupsTreeView.RefreshPreserveVisuals();
                }

                System.Diagnostics.Debug.WriteLine($"[AdjustTreeViewNodeOrderWithInsert] 插入式排序完成，新顺序: {string.Join(" -> ", groupNodes.Select(n => n.Text))}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AdjustTreeViewNodeOrderWithInsert] 插入式排序失败: {ex.Message}");
                LogHelper.Error($"插入式排序失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 调整项目顺序
        /// </summary>
        private void MoveItemOrder(TreeNode draggedItemNode, TreeNode targetItemNode)
        {
            // 简单实现：在相同分组内调整顺序
            if (draggedItemNode.Parent == targetItemNode.Parent && draggedItemNode.Parent != null)
            {
                TreeNode parentNode = draggedItemNode.Parent;
                int draggedIndex = draggedItemNode.Index;
                int targetIndex = targetItemNode.Index;

                draggedItemNode.Remove();
                parentNode.Nodes.Insert(targetIndex, draggedItemNode);
            }
        }

        #endregion
    }
}