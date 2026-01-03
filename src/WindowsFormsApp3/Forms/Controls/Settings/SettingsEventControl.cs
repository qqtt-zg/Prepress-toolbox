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
            
            // Load Data
            LoadEventGroupConfigs();
            LoadEventGroups();
            LoadEventItems();
            LoadEventPresets();
        }
        
        private void SetupTreeView()
        {
            // TreeView 的额外配置在这里进行（如果需要）
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
                SaveEventGroupConfigs();
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
            string[] allItems = { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号", "列组合", "行数", "列数" };
            
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

        private void SaveEventGroupConfigs()
        {
             try
             {
                 var settingsForm = new SettingsForm(); // Temporary instance to access Helper method if possible or duplicate logic
                 // SettingsForm.ToCompactJson is instance method.
                 // We should just use JsonConvert
                 var json = JsonConvert.SerializeObject(_eventGroupConfigs, Formatting.None);
                 AppSettings.Set("EventGroupConfigs", json);
             }
             catch (Exception ex)
             {
                 LogHelper.Error("SaveEventGroupConfigs error", ex);
             }
        }
        
        // --- Preset Logic (Simplified for brevity, logic mostly same as SettingsForm) ---
        // Note: For full fidelity we need GetEventItemsPresetNames, LoadEventPresets etc.
        
        private void LoadEventPresets()
        {
            // Ported logic
            var presetNames = GetEventItemsPresetNames();
            eventGroupsTreeView.GetPresetsComboBox().Items.Clear();
            eventGroupsTreeView.GetPresetsComboBox().Items.AddRange(presetNames.ToArray());

            var last = AppSettings.Get("LastSelectedEventPreset") as string;
            if(!string.IsNullOrEmpty(last) && presetNames.Contains(last))
                eventGroupsTreeView.GetPresetsComboBox().SelectedItem = last;
            else if (presetNames.Count > 0)
                eventGroupsTreeView.GetPresetsComboBox().SelectedIndex = 0;
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
                var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedPreset))
                {
                    MessageBox.Show("请先选择一个预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 获取当前配置
                // 保存预设
                SavePresetConfiguration(selectedPreset);
                
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
                var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedPreset)) return;

                // 记录最后选择的预设
                AppSettings.Set("LastSelectedEventPreset", selectedPreset);
                AppSettings.Save();

                // 加载预设配置
                LoadPresetConfiguration(selectedPreset);
            }
            catch (Exception ex)
            {
                LogHelper.Error("加载预设失败", ex);
            }
        }

        private void TreeViewEvents_PresetDeleted(object sender, EventArgs e)
        {
            try
            {
                var selectedPreset = eventGroupsTreeView.GetPresetsComboBox().SelectedItem?.ToString();
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
                eventGroupsTreeView.GetPresetsComboBox().SelectedItem = newPresetName;

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
            var config = _eventGroupConfigs.FirstOrDefault(g => g.Group == e.Group);
            if(config != null) config.IsPreserved = e.IsPreserved;
            SaveEventGroupConfigs();
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
                            config.Items.Add(new Models.EventItem
                            {
                                Name = itemNode.Text,
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
            if (config?.Groups == null || config?.Items == null) return;

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
                    }
                    
                    // 更新项目状态
                    foreach (TreeNode itemNode in groupNode.Nodes)
                    {
                        if (itemNode.Tag is TreeNodeData itemData && itemData.NodeType == TreeNodeType.Item)
                        {
                            var itemConfig = config.Items.FirstOrDefault(i => i.Name == itemNode.Text && i.GroupId == groupId);
                            if (itemConfig != null)
                            {
                                itemNode.Checked = itemConfig.IsEnabled;
                                itemData.IsEnabled = itemConfig.IsEnabled;
                            }
                        }
                    }
                }
            }
            
            // 刷新保留状态视觉
            eventGroupsTreeView.RefreshPreserveVisuals();
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
