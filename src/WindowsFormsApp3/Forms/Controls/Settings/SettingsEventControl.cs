using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using WindowsFormsApp3.Controls;
using WindowsFormsApp3.EventArguments;
using WindowsFormsApp3.Forms.Dialogs; // For EventGroup enum
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    public partial class SettingsEventControl : UserControl
    {
        // eventGroupsTreeView 已在 Designer.cs 中声明
        private List<EventGroupConfig> _eventGroupConfigs;
        private List<WindowsFormsApp3.Forms.Dialogs.EventItem> _eventItems;

        public SettingsEventControl()
        {
            InitializeComponent();
            
            // 仅在运行时加载设置，避免设计器问题
            if (System.ComponentModel.LicenseManager.UsageMode != System.ComponentModel.LicenseUsageMode.Designtime)
            {
                InitializeControl();
            }
        }

        private void InitializeControl()
        {
            // eventGroupsTreeView 已由设计器创建并添加到 Controls
            
            // Init Logic
            SetupTreeView();
            InitializeEventGroups();
            InitializeTreeViewEvents();
            
            // 数据加载移至 OnLoad 以确保控件尺寸正确
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!DesignMode)
            {
                // 1. 先加载预设列表
                LoadEventPresetsList();

                // 2. 加载分组配置（基础配置，包含上次会话的排序和状态）
                LoadEventGroupConfigs();

                // 3. 获取要使用的预设配置（用于加载项目结构）
                var presetConfig = GetPresetConfigurationToLoad();

                // 4. 构建 TreeView
                LoadEventGroups();
                // 关键修正：启动时不从预设覆盖分组属性（保留上次会话的状态），只加载项目结构
                LoadEventItemsWithPreset(presetConfig, applyGroupProperties: false);
            }
        }
        
        private void SetupTreeView()
        {
            // TreeView 的额外配置在这里进行
            if (eventGroupsTreeView != null)
            {
                // 强制使用 Dock 填充，确保随父控件调整大小
                eventGroupsTreeView.Dock = DockStyle.Fill;
                // 移除 Anchor 设置，避免与 Dock 冲突
                eventGroupsTreeView.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom; 
                // 注意：虽然 Dock 会自动处理 Anchor，但显示设置 Dock 是最稳健的
                
                // 确保布局刷新
                eventGroupsTreeView.PerformLayout();
            }
        }

        private void InitializeEventGroups()
        {
            _eventGroupConfigs = new List<EventGroupConfig>
            {
                new EventGroupConfig { Group = EventGroup.Order, DisplayName = "订单组", Prefix = "&ID-", IsEnabled = true, SortOrder = 1 },
                new EventGroupConfig { Group = EventGroup.Material, DisplayName = "材料组", Prefix = "&MT-", IsEnabled = true, SortOrder = 2 },
                new EventGroupConfig { Group = EventGroup.Quantity, DisplayName = "数量组", Prefix = "&DN-", IsEnabled = true, SortOrder = 3 },
                new EventGroupConfig { Group = EventGroup.Process, DisplayName = "工艺组", Prefix = "&DP-", IsEnabled = true, SortOrder = 4 },
                new EventGroupConfig { Group = EventGroup.Customer, DisplayName = "客户组", Prefix = "&CU-", IsEnabled = true, SortOrder = 5 },
                new EventGroupConfig { Group = EventGroup.Remark, DisplayName = "备注组", Prefix = "&MK-", IsEnabled = true, SortOrder = 6 },
                new EventGroupConfig { Group = EventGroup.Row, DisplayName = "行数组", Prefix = "&Row-", IsEnabled = true, SortOrder = 7 },
                new EventGroupConfig { Group = EventGroup.Column, DisplayName = "列数组", Prefix = "&Col-", IsEnabled = true, SortOrder = 8 },
                new EventGroupConfig { Group = EventGroup.Ungrouped, DisplayName = "未分组", Prefix = "", IsEnabled = true, SortOrder = 9 }
            };
        }

        private void InitializeTreeViewEvents()
        {
            // Event passthrough or handling
            // Since EventGroupsTreeView handles UI events, we focus on logic events
            eventGroupsTreeView.PresetSaved += TreeViewEvents_PresetSaved;
            eventGroupsTreeView.PresetLoaded += TreeViewEvents_PresetLoaded;
            eventGroupsTreeView.PresetDeleted += TreeViewEvents_PresetDeleted;
            eventGroupsTreeView.PresetSaveAs += TreeViewEvents_PresetSaveAs;

            eventGroupsTreeView.PreserveStateChanged += TreeViewEvents_PreserveStateChanged;
            eventGroupsTreeView.ConfigurationSaveRequested += (s, e) => {
                SaveGroupConfigsInternal();
                AppSettings.Save();
            };
        }

        private void LoadEventGroupConfigs()
        {
             try
            {
                string groupConfigsJson = AppSettings.Get("EventGroupConfigs") as string;
                if (!string.IsNullOrEmpty(groupConfigsJson))
                {
                    _eventGroupConfigs = JsonConvert.DeserializeObject<List<EventGroupConfig>>(groupConfigsJson);
                    if (_eventGroupConfigs == null) _eventGroupConfigs = new List<EventGroupConfig>();
                }
                else
                {
                    InitializeEventGroups();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载分组配置失败: {ex.Message}", ex);
                InitializeEventGroups();
            }
        }

        private void LoadEventGroups()
        {
            eventGroupsTreeView.TreeView.Nodes.Clear();
            var sortedGroups = _eventGroupConfigs.OrderBy(g => g.SortOrder).ToList();
            
            foreach (var groupConfig in sortedGroups)
            {
                var displayText = groupConfig.DisplayName;
                if (!string.IsNullOrEmpty(groupConfig.Prefix) && !displayText.StartsWith(groupConfig.Prefix))
                {
                    displayText = $"{groupConfig.Prefix} {displayText}";
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
                groupNode.NodeFont = new Font(eventGroupsTreeView.TreeView.Font, FontStyle.Bold);

                eventGroupsTreeView.TreeView.Nodes.Add(groupNode);
            }
            eventGroupsTreeView.TreeView.ExpandAll();
            eventGroupsTreeView.RefreshPreserveVisuals();
        }

        private void LoadEventItems()
        {
            _eventItems = new List<WindowsFormsApp3.Forms.Dialogs.EventItem>();
            string[] allItems = { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号", "列组合", "排版模式", "行数", "列数" };
            
            // Load saved items
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
                        bool.TryParse(items[i + 1], out bool isChecked);
                        savedItemsDict[text] = isChecked;
                    }
                }
            }

            int sortOrder = 1;
            foreach (string itemName in allItems)
            {
                EventGroup itemGroup = GetDefaultGroupForItem(itemName);
                var eventItem = new WindowsFormsApp3.Forms.Dialogs.EventItem
                {
                    Name = itemName,
                    IsEnabled = savedItemsDict.ContainsKey(itemName) ? savedItemsDict[itemName] : true,
                    Group = itemGroup,
                    SortOrder = sortOrder++
                };
                _eventItems.Add(eventItem);

                // Add to TreeView
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
            eventGroupsTreeView.TreeView.ExpandAll();
        }
        
        /// <summary>
        /// 从预设配置同步分组顺序和状态到全局配置
        /// </summary>
        private void SyncGlobalConfigFromPreset(Models.EventGroupConfiguration presetConfig)
        {
            if (presetConfig?.Groups == null || _eventGroupConfigs == null) return;

            LogHelper.Debug($"[SyncGlobalConfigFromPreset] 开始从预设同步 {presetConfig.Groups.Count} 个分组的配置");

            foreach (var presetGroup in presetConfig.Groups)
            {
                // 将预设中的字符串ID转换为枚举
                var groupEnum = ConvertGroupIdToEnum(presetGroup.Id);
                if (groupEnum.HasValue)
                {
                    // 在全局配置中找到对应分组
                    var globalGroup = _eventGroupConfigs.FirstOrDefault(g => g.Group == groupEnum.Value);
                    if (globalGroup != null)
                    {
                        // 同步关键属性：排序、启用状态、保留状态
                        globalGroup.SortOrder = presetGroup.SortOrder;
                        globalGroup.IsEnabled = presetGroup.IsEnabled;
                        globalGroup.IsPreserved = presetGroup.IsPreserved;
                        
                        // 甚至可以同步显示名称（如果允许预设修改名称）
                        if (!string.IsNullOrEmpty(presetGroup.DisplayName))
                        {
                            globalGroup.DisplayName = presetGroup.DisplayName;
                        }
                    }
                }
            }
            
            // 重新排序 _eventGroupConfigs，确保后续 LoadEventGroups 使用正确顺序
            _eventGroupConfigs = _eventGroupConfigs.OrderBy(g => g.SortOrder).ToList();
            
            LogHelper.Debug("[SyncGlobalConfigFromPreset] 同步完成");
        }

        /// <summary>
        /// 仅加载预设列表到下拉框（不触发加载配置）
        /// </summary>
        private void LoadEventPresetsList()
        {
            var presetNames = GetEventItemsPresetNames();
            eventGroupsTreeView.GetPresetsComboBox().Items.Clear();
            foreach (var name in presetNames)
            {
                eventGroupsTreeView.GetPresetsComboBox().Items.Add(name);
            }

            var last = AppSettings.Get("LastSelectedEventPreset") as string;
            if(!string.IsNullOrEmpty(last) && presetNames.Contains(last))
                eventGroupsTreeView.GetPresetsComboBox().SelectedValue = last;
            else if (presetNames.Count > 0)
                eventGroupsTreeView.GetPresetsComboBox().SelectedIndex = 0;
        }
        
        /// <summary>
        /// 获取要加载的预设配置
        /// </summary>
        private Models.EventGroupConfiguration GetPresetConfigurationToLoad()
        {
            try
            {
                var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedValue?.ToString();
                if (string.IsNullOrEmpty(selectedPreset))
                    return null;
                    
                if (selectedPreset == "全功能配置")
                {
                    // 全功能配置使用默认配置
                    return Models.DefaultEventGroups.GetDefaultConfiguration();
                }

                // 从 AppSettings 加载预设
                var presetsKey = $"EventItemsPreset_{selectedPreset}";
                var configJson = AppSettings.Get(presetsKey) as string;
                
                if (!string.IsNullOrEmpty(configJson))
                {
                    var config = JsonConvert.DeserializeObject<Models.EventGroupConfiguration>(configJson);
                    LogHelper.Debug($"[GetPresetConfigurationToLoad] 加载预设 {selectedPreset} 成功, {config?.Groups?.Count} 个分组, {config?.Items?.Count} 个项目");
                    return config;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取预设配置失败: {ex.Message}", ex);
            }
            return null;
        }
        
        /// <summary>
        /// 使用预设配置加载事件项目到 TreeView
        /// </summary>
        /// <param name="presetConfig">预设配置</param>
        /// <param name="applyGroupProperties">是否应用分组属性（如启用状态、保留状态）。在软件启动加载时应设为 false 以保留上次会话的状态；在切换预设时应设为 true。</param>
        private void LoadEventItemsWithPreset(Models.EventGroupConfiguration presetConfig, bool applyGroupProperties = true)
        {
            _eventItems = new List<WindowsFormsApp3.Forms.Dialogs.EventItem>();
            string[] allItems = { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号", "列组合", "材料类型", "行数", "列数" };
            
            int sortOrder = 1;
            foreach (string itemName in allItems)
            {
                // 优先从预设配置获取分组和状态
                EventGroup itemGroup = GetDefaultGroupForItem(itemName);
                bool isEnabled = true;
                
                if (presetConfig?.Items != null)
                {
                    // ✅ 自动迁移：检查预设中是否存在 "排版模式"，如果存在且当前查找的是 "材料类型"，则使用之
                    var searchName = itemName;
                    if (itemName == "材料类型")
                    {
                        var legacyItem = presetConfig.Items.FirstOrDefault(i => i.Name == "排版模式");
                        if (legacyItem != null)
                        {
                            // 找到了旧名称的配置，使用它作为当前项目配置
                            isEnabled = legacyItem.IsEnabled;
                            itemGroup = ConvertGroupIdToEnum(legacyItem.GroupId) ?? GetDefaultGroupForItem(itemName);
                            LogHelper.Debug("[LoadEventItemsWithPreset] 自动迁移：将预设中的 '排版模式' 应用于 '材料类型'");
                            // 我们不直接修改 presetConfig，因为它是传入的只读引用（或是服务层管理的），只在 UI 加载时适配
                            goto ItemFound; // 跳过常规查找
                        }
                    }

                    var presetItem = presetConfig.Items.FirstOrDefault(i => i.Name == searchName);
                    if (presetItem != null)
                    {
                        isEnabled = presetItem.IsEnabled;
                        // 将 GroupId 转换为 EventGroup 枚举
                        itemGroup = ConvertGroupIdToEnum(presetItem.GroupId) ?? GetDefaultGroupForItem(itemName);
                    }
                    
                    ItemFound:;
                }
                
                var eventItem = new WindowsFormsApp3.Forms.Dialogs.EventItem
                {
                    Name = itemName,
                    IsEnabled = isEnabled,
                    Group = itemGroup,
                    SortOrder = sortOrder++
                };
                _eventItems.Add(eventItem);

                // Add to TreeView
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
            
            // 应用分组状态（仅当请求时）
            if (applyGroupProperties && presetConfig?.Groups != null)
            {
                foreach (TreeNode groupNode in eventGroupsTreeView.TreeView.Nodes)
                {
                    if (groupNode.Tag is TreeNodeData groupData && groupData.NodeType == TreeNodeType.Group)
                    {
                        string groupId = GetGroupId(groupData.Group);
                        var groupConfig = presetConfig.Groups.FirstOrDefault(g => g.Id == groupId);
                        if (groupConfig != null)
                        {
                            groupNode.Checked = groupConfig.IsEnabled;
                            groupData.IsEnabled = groupConfig.IsEnabled;
                            groupData.IsPreserved = groupConfig.IsPreserved;
                            
                            // 同时更新 _eventGroupConfigs
                            var internalConfig = _eventGroupConfigs.FirstOrDefault(g => g.Group == groupData.Group);
                            if (internalConfig != null)
                            {
                                internalConfig.IsPreserved = groupConfig.IsPreserved;
                                internalConfig.IsEnabled = groupConfig.IsEnabled;
                            }
                        }
                    }
                }
            }
            
            eventGroupsTreeView.TreeView.ExpandAll();
            eventGroupsTreeView.RefreshPreserveVisuals();
            LogHelper.Debug($"[LoadEventItemsWithPreset] TreeView 构建完成 (ApplyGroupProperties={applyGroupProperties})");
        }
        
        /// <summary>
        /// 将 GroupId 字符串转换为 EventGroup 枚举
        /// </summary>
        private EventGroup? ConvertGroupIdToEnum(string groupId)
        {
            return groupId?.ToLower() switch
            {
                "order" => EventGroup.Order,
                "material" => EventGroup.Material,
                "quantity" => EventGroup.Quantity,
                "process" => EventGroup.Process,
                "customer" => EventGroup.Customer,
                "remark" => EventGroup.Remark,
                "row" => EventGroup.Row,
                "column" => EventGroup.Column,
                "ungrouped" => EventGroup.Ungrouped,
                _ => null
            };
        }

        private EventGroup GetDefaultGroupForItem(string itemName)
        {
             return itemName switch
            {
                "订单号" => EventGroup.Order,
                "材料" => EventGroup.Material,
                "数量" => EventGroup.Quantity,
                "工艺" => EventGroup.Process,
                "行数" => EventGroup.Row,
                "列数" => EventGroup.Column,
                _ => EventGroup.Ungrouped
            };
        }

        private TreeNode FindGroupNode(EventGroup group)
        {
            foreach (TreeNode node in eventGroupsTreeView.TreeView.Nodes)
            {
                if (node.Tag is TreeNodeData data && data.Group == group)
                    return node;
            }
            return null;
        }

        /// <summary>
        /// 保存事件分组设置
        /// </summary>
        public void SaveSettings()
        {
             try
             {
                 // 从 UI 同步排序顺序
                 SyncGroupSortOrdersFromUI();

                 // 保存分组配置
                 var json = JsonConvert.SerializeObject(_eventGroupConfigs, Formatting.None);
                 AppSettings.Set("EventGroupConfigs", json);
                 
                 // 保存当前预设（关键修复：确保拖拽后的顺序也被保存到预设中）
                 var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedValue?.ToString();
                 if (!string.IsNullOrEmpty(selectedPreset) && selectedPreset != "全功能配置")
                 {
                     SavePresetConfiguration(selectedPreset);
                     LogHelper.Debug($"[SaveSettings] 同步保存预设 '{selectedPreset}'");
                 }
                 
                 AppSettings.Save();
             }
             catch (Exception ex)
             {
                 LogHelper.Error("SaveSettings error", ex);
             }
        }
        
        /// <summary>
        /// 内部方法：仅保存分组配置（不保存预设）
        /// </summary>
        private void SaveGroupConfigsInternal()
        {
             try
             {
                 // 从 UI 同步排序顺序
                 SyncGroupSortOrdersFromUI();

                 var json = JsonConvert.SerializeObject(_eventGroupConfigs, Formatting.None);
                 AppSettings.Set("EventGroupConfigs", json);

                 // 如果当前正在使用某个预设，也应该更新该预设的排序信息
                 // 否则下次加载预设时，旧的排序会覆盖新的排序
                 var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedValue?.ToString();
                 if (!string.IsNullOrEmpty(selectedPreset) && selectedPreset != "全功能配置")
                 {
                     SavePresetConfiguration(selectedPreset);
                     LogHelper.Debug($"[SaveGroupConfigsInternal] 自动更新预设 '{selectedPreset}' 以保存排序更改");
                 }
             }
             catch (Exception ex)
             {
                 LogHelper.Error("SaveGroupConfigsInternal error", ex);
             }
        }

        /// <summary>
        /// 从 TreeView 同步分组排序顺序到配置对象
        /// </summary>
        private void SyncGroupSortOrdersFromUI()
        {
            if (eventGroupsTreeView?.TreeView?.Nodes == null) return;

            foreach (TreeNode node in eventGroupsTreeView.TreeView.Nodes)
            {
                if (node.Tag is TreeNodeData nodeData && nodeData.NodeType == TreeNodeType.Group)
                {
                    var config = _eventGroupConfigs.FirstOrDefault(g => g.Group == nodeData.Group);
                    if (config != null)
                    {
                        config.SortOrder = node.Index;
                    }
                }
            }
        }
        
        // --- Preset Logic (Simplified for brevity, logic mostly same as SettingsForm) ---
        // Note: For full fidelity we need GetEventItemsPresetNames, LoadEventPresets etc.
        
        private void LoadEventPresets()
        {
            // Ported logic
            var presetNames = GetEventItemsPresetNames();
            eventGroupsTreeView.GetPresetsComboBox().Items.Clear();
            foreach (var name in presetNames)
            {
                eventGroupsTreeView.GetPresetsComboBox().Items.Add(name);
            }

            var last = AppSettings.Get("LastSelectedEventPreset") as string;
            string presetToLoad = null;
            
            if(!string.IsNullOrEmpty(last) && presetNames.Contains(last))
            {
                eventGroupsTreeView.GetPresetsComboBox().SelectedValue = last;
                presetToLoad = last;
            }
            else if (presetNames.Count > 0)
            {
                eventGroupsTreeView.GetPresetsComboBox().SelectedIndex = 0;
                presetToLoad = presetNames[0];
            }
            
            // 初始化时手动加载预设配置到 TreeView
            // 因为设置 SelectedItem 时事件可能不会触发或 TreeView 已构建
            if (!string.IsNullOrEmpty(presetToLoad))
            {
                LoadPresetConfiguration(presetToLoad);
            }
        }

        private List<string> GetEventItemsPresetNames()
        {
            var list = new List<string> { "全功能配置" };
            var custom = AppSettings.Get("EventItemsCustomPresets") as string;
            if (!string.IsNullOrEmpty(custom))
            {
                foreach(var p in custom.Split(new[]{'|'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    if(!list.Contains(p)) list.Add(p);
                }
            }
            return list;
        }

        private void TreeViewEvents_PresetSaved(object sender, EventArgs e)
        {
            try
            {
                var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedValue?.ToString();
                if (string.IsNullOrEmpty(selectedPreset))
                {
                    MessageBox.Show("请先选择一个预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 获取当前配置
                // 保存预设
                SavePresetConfiguration(selectedPreset);
                
                // 更新最后选择的预设（确保下次启动时能加载正确的预设）
                AppSettings.Set("LastSelectedEventPreset", selectedPreset);
                AppSettings.Save();
                
                MessageBox.Show($"预设 '{selectedPreset}' 已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogHelper.Error("保存预设失败", ex);
                MessageBox.Show($"保存预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void TreeViewEvents_PresetLoaded(object sender, EventArgs e)
        {
            try
            {
                var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedValue?.ToString();
                if (string.IsNullOrEmpty(selectedPreset)) return;

                // 记录最后选择的预设
                AppSettings.Set("LastSelectedEventPreset", selectedPreset);
                AppSettings.Save();

                // 完全重建 TreeView（而不只是更新状态）
                RebuildTreeViewWithPreset(selectedPreset);
            }
            catch (Exception ex)
            {
                LogHelper.Error("加载预设失败", ex);
            }
        }
        
        /// <summary>
        /// 完全重建 TreeView（用于预设切换）
        /// </summary>
        private void RebuildTreeViewWithPreset(string presetName)
        {
            // 获取预设配置
            Models.EventGroupConfiguration presetConfig = null;
            
            if (presetName == "全功能配置")
            {
                presetConfig = Models.DefaultEventGroups.GetDefaultConfiguration();
            }
            else
            {
                var presetsKey = $"EventItemsPreset_{presetName}";
                var configJson = AppSettings.Get(presetsKey) as string;
                if (!string.IsNullOrEmpty(configJson))
                {
                    presetConfig = JsonConvert.DeserializeObject<Models.EventGroupConfiguration>(configJson);
                }
            }
            
            if (presetConfig == null)
            {
                LogHelper.Debug($"[RebuildTreeViewWithPreset] 预设 {presetName} 配置为空，使用默认配置");
                presetConfig = Models.DefaultEventGroups.GetDefaultConfiguration();
            }

            // 关键修复：在重建 TreeView 之前，先将预设中的分组顺序同步到内存配置中
            // 这样 LoadEventGroups() 才能使用正确的顺序
            SyncGlobalConfigFromPreset(presetConfig);
            
            // 清除现有 TreeView 内容
            eventGroupsTreeView.TreeView.Nodes.Clear();
            
            // 重新构建分组节点
            LoadEventGroups();
            
            // 使用预设配置构建项目
            // 关键修正：切换预设时，需要完全应用预设中的所有属性（包括分组启用状态等）
            LoadEventItemsWithPreset(presetConfig, applyGroupProperties: true);
            
            LogHelper.Debug($"[RebuildTreeViewWithPreset] TreeView 重建完成, 预设: {presetName}");
        }

        private void TreeViewEvents_PresetDeleted(object sender, EventArgs e)
        {
            try
            {
                var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedValue?.ToString();
                if (string.IsNullOrEmpty(selectedPreset))
                {
                    MessageBox.Show("请先选择一个预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 内置预设不能删除
                if (selectedPreset == "全功能配置")
                {
                    MessageBox.Show("内置预设不能删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (MessageBox.Show($"确定要删除预设 '{selectedPreset}' 吗？", "确认删除", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DeletePreset(selectedPreset);
                    LoadEventPresets();
                    MessageBox.Show($"预设 '{selectedPreset}' 已删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error("删除预设失败", ex);
                MessageBox.Show($"删除预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

       private void TreeViewEvents_PresetSaveAs(object sender, EventArgs e)
       {
            try
            {
                // 使用 InputBox 获取新预设名称
                var newPresetName = Microsoft.VisualBasic.Interaction.InputBox(
                    "请输入新预设的名称:", 
                    "另存预设方案", 
                    "", 
                    -1, -1);

                if (string.IsNullOrWhiteSpace(newPresetName))
                {
                    return; // 用户取消
                }

                // 检查是否是内置预设名
                if (newPresetName == "全功能配置")
                {
                    MessageBox.Show("不能使用内置预设名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 检查是否已存在
                var existingPresets = GetEventItemsPresetNames();
                if (existingPresets.Contains(newPresetName))
                {
                    if (MessageBox.Show($"预设 '{newPresetName}' 已存在，是否覆盖？", "确认", 
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        return;
                    }
                }

                // 获取当前配置并保存
                // 保存预设
                SavePresetConfiguration(newPresetName);

                // 添加到自定义预设列表
                AddCustomPresetName(newPresetName);

                // 刷新预设列表并选择新预设
                LoadEventPresets();
                eventGroupsTreeView.GetPresetsComboBox().SelectedValue = newPresetName;

                MessageBox.Show($"预设 '{newPresetName}' 已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogHelper.Error("另存预设失败", ex);
                MessageBox.Show($"另存预设失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
       }

       private void TreeViewEvents_PreserveStateChanged(object sender, PreserveStateChangedEventArgs e)
       {
            // 更新内部配置
            var config = _eventGroupConfigs.FirstOrDefault(g => g.Group == e.Group);
            if(config != null) config.IsPreserved = e.IsPreserved;
            
            // 同时更新 TreeNode 的 Tag 数据
            foreach (TreeNode groupNode in eventGroupsTreeView.TreeView.Nodes)
            {
                if (groupNode.Tag is TreeNodeData groupData && 
                    groupData.NodeType == TreeNodeType.Group && 
                    groupData.Group == e.Group)
                {
                    groupData.IsPreserved = e.IsPreserved;
                    LogHelper.Debug($"[TreeViewEvents_PreserveStateChanged] 分组 {e.Group} 保留状态更新为: {e.IsPreserved}");
                    break;
                }
            }
            
            SaveGroupConfigsInternal();
       }

        #region 预设辅助方法

        /// <summary>
        /// 获取当前事件配置 (EventGroupConfiguration 对象)
        /// </summary>
        private Models.EventGroupConfiguration GetCurrentEventGroupConfiguration()
        {
            var config = new Models.EventGroupConfiguration
            {
                Groups = new List<Models.EventGroup>(),
                Items = new List<Models.EventItem>()
            };
            
            foreach (TreeNode groupNode in eventGroupsTreeView.TreeView.Nodes)
            {
                if (groupNode.Tag is TreeNodeData groupData && groupData.NodeType == TreeNodeType.Group)
                {
                    // 获取分组 ID
                    string groupId = GetGroupId(groupData.Group);
                    
                    // 获取分组配置
                    var groupConfig = _eventGroupConfigs.FirstOrDefault(g => g.Group == groupData.Group);
                    
                    // 保存分组配置
                    config.Groups.Add(new Models.EventGroup
                    {
                        Id = groupId,
                        DisplayName = groupConfig?.DisplayName ?? groupData.Group.ToString(),
                        Prefix = groupConfig?.Prefix ?? "",
                        IsEnabled = groupNode.Checked,
                        SortOrder = groupNode.Index,
                        IsPreserved = groupData.IsPreserved
                    });
                    
                    // 保存该分组下的所有项目
                    foreach (TreeNode itemNode in groupNode.Nodes)
                    {
                        if (itemNode.Tag is TreeNodeData itemData && itemData.NodeType == TreeNodeType.Item)
                        {
                            // 使用 itemData.ItemName 而非 itemNode.Text，避免保存带有 [*] 前缀的显示名称
                            config.Items.Add(new Models.EventItem
                            {
                                Name = itemData.ItemName ?? itemNode.Text.Replace("[*] ", ""),
                                GroupId = groupId,
                                IsEnabled = itemNode.Checked,
                                SortOrder = itemNode.Index
                            });
                        }
                    }
                }
            }
            
            return config;
        }

        /// <summary>
        /// 根据 EventGroup 枚举获取分组 ID
        /// </summary>
        private string GetGroupId(EventGroup? group)
        {
            if (!group.HasValue) return "ungrouped";
            
            return group.Value switch
            {
                EventGroup.Order => "order",
                EventGroup.Material => "material",
                EventGroup.Quantity => "quantity",
                EventGroup.Process => "process",
                EventGroup.Customer => "customer",
                EventGroup.Remark => "remark",
                EventGroup.Row => "row",
                EventGroup.Column => "column",
                EventGroup.Ungrouped => "ungrouped",
                _ => "ungrouped"
            };
        }

        /// <summary>
        /// 保存预设配置 (使用 EventItemsPreset_ 键前缀和 JSON 格式)
        /// </summary>
        private void SavePresetConfiguration(string presetName, string _obsolete = null)
        {
            try
            {
                var config = GetCurrentEventGroupConfiguration();
                var configJson = JsonConvert.SerializeObject(config, Formatting.Indented);
                
                // 使用与 SettingsForm 相同的键前缀
                var presetsKey = $"EventItemsPreset_{presetName}";
                AppSettings.Set(presetsKey, configJson);
                AppSettings.Save();
                
                LogHelper.Debug($"预设保存成功: {presetsKey}, JSON长度: {configJson.Length}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存预设配置失败: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// 加载预设配置 (使用 EventItemsPreset_ 键前缀和 JSON 格式)
        /// </summary>
        private void LoadPresetConfiguration(string presetName)
        {
            try
            {
                if (presetName == "全功能配置")
                {
                    // 全功能配置 - 使用默认配置
                    var fullConfig = Models.DefaultEventGroups.GetDefaultConfiguration();
                    ApplyEventGroupConfiguration(fullConfig);
                    return;
                }

                // 使用与 SettingsForm 相同的键前缀
                var presetsKey = $"EventItemsPreset_{presetName}";
                var configJson = AppSettings.Get(presetsKey) as string;
                
                if (string.IsNullOrEmpty(configJson))
                {
                    LogHelper.Debug($"预设 {presetName} 配置为空");
                    return;
                }

                var config = JsonConvert.DeserializeObject<Models.EventGroupConfiguration>(configJson);
                if (config != null)
                {
                    ApplyEventGroupConfiguration(config);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载预设配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 应用 EventGroupConfiguration 到 TreeView
        /// </summary>
        private void ApplyEventGroupConfiguration(Models.EventGroupConfiguration config)
        {
            if (config?.Groups == null || config?.Items == null) 
            {
                LogHelper.Debug($"[ApplyEventGroupConfiguration] 配置为空或不完整, Groups={config?.Groups?.Count}, Items={config?.Items?.Count}");
                return;
            }
            
            LogHelper.Debug($"[ApplyEventGroupConfiguration] 开始应用配置: {config.Groups.Count} 个分组, {config.Items.Count} 个项目");

            // 更新分组和项目的勾选状态
            foreach (TreeNode groupNode in eventGroupsTreeView.TreeView.Nodes)
            {
                if (groupNode.Tag is TreeNodeData groupData && groupData.NodeType == TreeNodeType.Group)
                {
                    string groupId = GetGroupId(groupData.Group);
                    
                    // 查找对应的分组配置
                    var groupConfig = config.Groups.FirstOrDefault(g => g.Id == groupId);
                    if (groupConfig != null)
                    {
                        groupNode.Checked = groupConfig.IsEnabled;
                        groupData.IsEnabled = groupConfig.IsEnabled;
                        groupData.IsPreserved = groupConfig.IsPreserved;

                        // 同步更新 _eventGroupConfigs 中的 IsPreserved 状态
                        var internalConfig = _eventGroupConfigs.FirstOrDefault(g => g.Group == groupData.Group);
                        if (internalConfig != null)
                        {
                            internalConfig.IsPreserved = groupConfig.IsPreserved;
                            internalConfig.IsEnabled = groupConfig.IsEnabled;
                        }

                        LogHelper.Debug($"[ApplyEventGroupConfiguration] 分组 {groupId}: IsEnabled={groupConfig.IsEnabled}, IsPreserved={groupConfig.IsPreserved}");
                    }
                    
                    // 更新项目状态 - 只按名称匹配，不限制 GroupId
                    foreach (TreeNode itemNode in groupNode.Nodes)
                    {
                        if (itemNode.Tag is TreeNodeData itemData && itemData.NodeType == TreeNodeType.Item)
                        {
                            // 先尝试精确匹配（名称+分组），再尝试只匹配名称
                            var itemConfig = config.Items.FirstOrDefault(i => i.Name == itemNode.Text && i.GroupId == groupId)
                                          ?? config.Items.FirstOrDefault(i => i.Name == itemNode.Text);
                            if (itemConfig != null)
                            {
                                itemNode.Checked = itemConfig.IsEnabled;
                                itemData.IsEnabled = itemConfig.IsEnabled;
                                LogHelper.Debug($"[ApplyEventGroupConfiguration] 项目 {itemNode.Text}: IsEnabled={itemConfig.IsEnabled}");
                            }
                        }
                    }
                }
            }
            
            // 刷新保留状态视觉
            eventGroupsTreeView.RefreshPreserveVisuals();
            LogHelper.Debug("[ApplyEventGroupConfiguration] 配置应用完成");
        }

        /// <summary>
        /// 删除预设 (使用 EventItemsPreset_ 键前缀)
        /// </summary>
        private void DeletePreset(string presetName)
        {
            // 删除预设配置 - 使用与 SettingsForm 相同的键前缀
            var presetsKey = $"EventItemsPreset_{presetName}";
            AppSettings.Set(presetsKey, null);
            
            // 从自定义预设列表中移除
            var customPresets = AppSettings.Get("EventItemsCustomPresets") as string ?? "";
            var presetList = customPresets.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            presetList.Remove(presetName);
            AppSettings.Set("EventItemsCustomPresets", string.Join("|", presetList));
            
            AppSettings.Save();
        }

        /// <summary>
        /// 添加自定义预设名称到列表
        /// </summary>
        private void AddCustomPresetName(string presetName)
        {
            var customPresets = AppSettings.Get("EventItemsCustomPresets") as string ?? "";
            var presetList = customPresets.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            if (!presetList.Contains(presetName))
            {
                presetList.Add(presetName);
                AppSettings.Set("EventItemsCustomPresets", string.Join("|", presetList));
                AppSettings.Save();
            }
        }

        #endregion
    }
    
    // Helper Class if not accessible
    public class EventGroupConfig
    {
        public EventGroup Group { get; set; }
        public string DisplayName { get; set; }
        public string Prefix { get; set; }
        public bool IsEnabled { get; set; }
        public int SortOrder { get; set; }
        public bool IsPreserved { get; set; }
    }
}
