using Newtonsoft.Json;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 事件组配置服务
    /// 提供 EventGroup 配置的读取功能
    /// 从 SettingsForm 迁移的静态方法
    /// </summary>
    public static class EventGroupConfigurationService
    {
        /// <summary>
        /// 获取 EventGroup 配置
        /// </summary>
        /// <returns>EventGroup 配置对象</returns>
        public static EventGroupConfiguration GetEventGroupConfiguration()
        {
            try
            {
                // 获取当前选中的预设方案名称
                string currentPresetName = AppSettings.Instance["LastSelectedEventPreset"]?.ToString() ?? "全功能配置";
                LogHelper.Debug($"[EventGroupConfigurationService] 当前选中的预设方案: {currentPresetName}");
                
                // 从 CustomSettings 中获取对应预设方案的配置
                string presetKey = $"EventItemsPreset_{currentPresetName}";
                string configJson = AppSettings.Instance[presetKey]?.ToString() ?? "";
                
                LogHelper.Debug($"[EventGroupConfigurationService] 预设方案配置键: {presetKey}");
                LogHelper.Debug($"[EventGroupConfigurationService] 配置JSON长度: {configJson.Length}");
                
                if (string.IsNullOrEmpty(configJson))
                {
                    LogHelper.Debug($"[EventGroupConfigurationService] 预设方案 {currentPresetName} 配置为空，使用默认配置");
                    return EventGroupConfiguration.GetDefault();
                }

                // 尝试解析 JSON 配置
                var config = JsonConvert.DeserializeObject<EventGroupConfiguration>(configJson);
                if (config == null)
                {
                    LogHelper.Debug($"[EventGroupConfigurationService] 预设方案 {currentPresetName} 解析失败，使用默认配置");
                    return EventGroupConfiguration.GetDefault();
                }
                
                LogHelper.Debug($"[EventGroupConfigurationService] 成功加载预设方案 {currentPresetName}，包含 {config.Groups?.Count ?? 0} 个分组，{config.Items?.Count ?? 0} 个项目");
                
                // ✅ 自动迁移：将 "排版模式" 重命名为 "材料类型"
                bool modified = false;
                if (config.Items != null)
                {
                    foreach (var item in config.Items)
                    {
                        if (item.Name == "排版模式")
                        {
                            item.Name = "材料类型";
                            modified = true;
                            LogHelper.Debug($"[EventGroupConfigurationService] 自动迁移：已将项目 '排版模式' 重命名为 '材料类型'");
                        }
                    }
                }

                if (modified)
                {
                    // 可选：保存回配置？暂时只在内存中修改即可生效
                    // AppSettings.Instance[presetKey] = JsonConvert.SerializeObject(config);
                    // AppSettings.Save(); 
                }

                return config;
            }
            catch (System.Exception ex)
            {
                LogHelper.Debug($"[EventGroupConfigurationService] 获取EventGroup配置时出错: {ex.Message}");
                return EventGroupConfiguration.GetDefault();
            }
        }
    }
}
