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
        private EventGroupsTreeView eventGroupsTreeView;
        private List<EventGroupConfig> _eventGroupConfigs;
        private List<WindowsFormsApp3.Forms.Dialogs.EventItem> _eventItems;

        public SettingsEventControl()
        {
            InitializeComponent();
            InitializeControl();
        }

        private void InitializeControl()
        {
            // Init EventGroupsTreeView
            eventGroupsTreeView = new EventGroupsTreeView();
            eventGroupsTreeView.Dock = DockStyle.Fill;
            this.Controls.Add(eventGroupsTreeView);

            // Init Logic
            InitializeEventGroups();
            InitializeTreeViewEvents();
            
            // Load Data
            LoadEventGroupConfigs();
            LoadEventGroups();
            LoadEventItems();
            LoadEventPresets();
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
            // Implement Logic
            MessageBox.Show("预设保存逻辑待完善 (Porting...)", "提示");
        }
        
        private void TreeViewEvents_PresetLoaded(object sender, EventArgs e)
        {
             // Implement Logic
        }

        private void TreeViewEvents_PresetDeleted(object sender, EventArgs e)
        {
             // Implement Logic
        }

       private void TreeViewEvents_PresetSaveAs(object sender, EventArgs e)
       {
            // Implement Logic
       }

       private void TreeViewEvents_PreserveStateChanged(object sender, PreserveStateChangedEventArgs e)
       {
            var config = _eventGroupConfigs.FirstOrDefault(g => g.Group == e.Group);
            if(config != null) config.IsPreserved = e.IsPreserved;
            SaveEventGroupConfigs();
       }
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
