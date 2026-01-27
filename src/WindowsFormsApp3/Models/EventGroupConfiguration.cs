using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// 事件项目分组配置
    /// 用于管理重命名规则项的分组和前缀设置
    /// </summary>
    public class EventGroupConfiguration
    {
        public List<EventGroup> Groups { get; set; } = new List<EventGroup>();
        public List<EventItem> Items { get; set; } = new List<EventItem>();
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 获取默认配置的静态方法
        /// </summary>
        /// <returns>默认的EventGroup配置</returns>
        public static EventGroupConfiguration GetDefault()
        {
            return DefaultEventGroups.GetDefaultConfiguration();
        }

        /// <summary>
        /// 获取启用的分组项目（按排序）
        /// </summary>
        public List<(EventGroup group, EventItem item)> GetEnabledGroupedItems()
        {
            return Groups.Where(g => g.IsEnabled)
                        .OrderBy(g => g.SortOrder)
                        .SelectMany(g => Items.Where(i => i.GroupId == g.Id && i.IsEnabled)
                                               .OrderBy(i => i.SortOrder)
                                               .Select(i => (g, i)))
                        .ToList();
        }

        /// <summary>
        /// 获取未分组项目
        /// </summary>
        public List<EventItem> GetUngroupedItems()
        {
            return Items.Where(i => string.IsNullOrEmpty(i.GroupId) && i.IsEnabled)
                       .OrderBy(i => i.SortOrder)
                       .ToList();
        }

        /// <summary>
        /// 获取所有启用的项目（按分组和排序）
        /// </summary>
        public List<EventItem> GetAllEnabledItems()
        {
            var result = new List<EventItem>();

            // 添加启用的分组项目
            var groupedItems = GetEnabledGroupedItems();
            result.AddRange(groupedItems.Select(x => x.item));

            // 添加未分组项目
            result.AddRange(GetUngroupedItems());

            return result;
        }

        /// <summary>
        /// 根据项目名称获取分组前缀
        /// </summary>
        public string GetPrefixForItem(string itemName)
        {
            var item = Items.FirstOrDefault(i => i.Name == itemName);
            if (item == null || string.IsNullOrEmpty(item.GroupId))
                return string.Empty;

            var group = Groups.FirstOrDefault(g => g.Id == item.GroupId);
            return group?.IsEnabled == true ? group.Prefix : string.Empty;
        }

        /// <summary>
        /// 移动项目到指定分组
        /// </summary>
        public void MoveItemToGroup(string itemName, string groupId)
        {
            var item = Items.FirstOrDefault(i => i.Name == itemName);
            if (item != null)
            {
                item.GroupId = groupId;
            }
        }

        /// <summary>
        /// 更新分组排序
        /// </summary>
        public void UpdateGroupSortOrder(string groupId, int newSortOrder)
        {
            var group = Groups.FirstOrDefault(g => g.Id == groupId);
            if (group != null)
            {
                group.SortOrder = newSortOrder;
            }
        }

        /// <summary>
        /// 更新项目排序
        /// </summary>
        public void UpdateItemSortOrder(string itemName, int newSortOrder)
        {
            var item = Items.FirstOrDefault(i => i.Name == itemName);
            if (item != null)
            {
                item.SortOrder = newSortOrder;
            }
        }
    }

    /// <summary>
    /// 事件分组
    /// </summary>
    public class EventGroup
    {
        public string Id { get; set; } = "";              // 唯一标识：order, material等
        public string DisplayName { get; set; } = "";     // 显示名称：订单组、材料组等
        public string Prefix { get; set; } = "";         // 前缀：&ID-, &MT-等
        public bool IsEnabled { get; set; } = true;      // 分组是否启用
        public int SortOrder { get; set; } = 0;          // 分组排序顺序
        public bool IsPreserved { get; set; } = false;   // 分组是否为保留分组（返单时保留原数据）
    }

    /// <summary>
    /// 事件项目
    /// </summary>
    public class EventItem
    {
        public string Name { get; set; } = "";           // 项目名称：订单号、材料等
        public string GroupId { get; set; } = "";        // 所属分组ID，空字符串表示未分组
        public bool IsEnabled { get; set; } = true;      // 项目是否启用
        public int SortOrder { get; set; } = 0;          // 项目排序顺序
    }

    /// <summary>
    /// 默认分组配置
    /// </summary>
    public static class DefaultEventGroups
    {
        public static readonly List<EventGroup> BuiltInGroups = new List<EventGroup>
        {
            new EventGroup { Id = "order", DisplayName = "订单组", Prefix = "&ID-", SortOrder = 1 },
            new EventGroup { Id = "material", DisplayName = "材料组", Prefix = "&MT-", SortOrder = 2 },
            new EventGroup { Id = "quantity", DisplayName = "数量组", Prefix = "&DN-", SortOrder = 3 },
            new EventGroup { Id = "process", DisplayName = "工艺组", Prefix = "&DP-", SortOrder = 4 },
            new EventGroup { Id = "customer", DisplayName = "客户组", Prefix = "&CU-", SortOrder = 5 },
            new EventGroup { Id = "remark", DisplayName = "备注组", Prefix = "&MK-", SortOrder = 6 },
            new EventGroup { Id = "row", DisplayName = "行数组", Prefix = "&Row-", SortOrder = 7 },
            new EventGroup { Id = "column", DisplayName = "列数组", Prefix = "&Col-", SortOrder = 8 },
            new EventGroup { Id = "", DisplayName = "未分组", Prefix = "", SortOrder = 9 } // ✅ 修复：添加默认的未分组，对应 GroupId="" 的项目
        };

        public static readonly List<EventItem> BuiltInItems = new List<EventItem>
        {
            new EventItem { Name = "正则结果", GroupId = "", SortOrder = 1 },
            new EventItem { Name = "订单号", GroupId = "order", SortOrder = 1 },
            new EventItem { Name = "材料", GroupId = "material", SortOrder = 1 },
            new EventItem { Name = "数量", GroupId = "quantity", SortOrder = 1 },
            new EventItem { Name = "工艺", GroupId = "process", SortOrder = 1 },
            new EventItem { Name = "尺寸", GroupId = "", SortOrder = 2 },
            new EventItem { Name = "序号", GroupId = "", SortOrder = 3 },
            new EventItem { Name = "列组合", GroupId = "", SortOrder = 4 },
            new EventItem { Name = "材料类型", GroupId = "", SortOrder = 5 },
            new EventItem { Name = "行数", GroupId = "row", SortOrder = 1 },
            new EventItem { Name = "列数", GroupId = "column", SortOrder = 1 }
        };

        public static EventGroupConfiguration GetDefaultConfiguration()
        {
            return new EventGroupConfiguration
            {
                Groups = new List<EventGroup>(BuiltInGroups),
                Items = new List<EventItem>(BuiltInItems)
            };
        }
    }
}