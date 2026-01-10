using System;
using System.Drawing;
using Newtonsoft.Json;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// 主题配色方案定义
    /// </summary>
    public class ThemeDefinition
    {
        /// <summary>
        /// 主题名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 是否为内置主题（内置主题不可删除）
        /// </summary>
        public bool IsBuiltIn { get; set; }

        #region 背景颜色

        /// <summary>
        /// 主背景色
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color Background { get; set; }

        /// <summary>
        /// 表面/卡片背景色
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color Surface { get; set; }

        /// <summary>
        /// 浅色表面/输入框背景色
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color SurfaceLight { get; set; }

        #endregion

        #region 文字颜色

        /// <summary>
        /// 主文字颜色
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color TextPrimary { get; set; }

        /// <summary>
        /// 次要文字颜色
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color TextSecondary { get; set; }

        #endregion

        #region 边框颜色

        /// <summary>
        /// 边框颜色
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color Border { get; set; }

        #endregion

        #region 强调色

        /// <summary>
        /// 主要强调色（Primary）
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color Primary { get; set; }

        /// <summary>
        /// 成功色（Success）
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color Success { get; set; }

        /// <summary>
        /// 警告色（Warning）
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color Warning { get; set; }

        /// <summary>
        /// 错误色（Error）
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color Error { get; set; }

        #endregion

        #region 交互色

        /// <summary>
        /// 激活状态背景色
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color BackActive { get; set; }

        /// <summary>
        /// 悬停状态背景色
        /// </summary>
        [JsonConverter(typeof(ColorConverter))]
        public Color BackHover { get; set; }

        #endregion

        /// <summary>
        /// 克隆主题定义
        /// </summary>
        public ThemeDefinition Clone()
        {
            return new ThemeDefinition
            {
                Name = this.Name,
                IsBuiltIn = this.IsBuiltIn,
                Background = this.Background,
                Surface = this.Surface,
                SurfaceLight = this.SurfaceLight,
                TextPrimary = this.TextPrimary,
                TextSecondary = this.TextSecondary,
                Border = this.Border,
                Primary = this.Primary,
                Success = this.Success,
                Warning = this.Warning,
                Error = this.Error,
                BackActive = this.BackActive,
                BackHover = this.BackHover
            };
        }
    }

    /// <summary>
    /// Color 类型的 JSON 转换器
    /// </summary>
    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.A},{value.R},{value.G},{value.B}");
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var colorString = reader.Value?.ToString();
            if (string.IsNullOrEmpty(colorString))
                return Color.Black;

            var parts = colorString.Split(',');
            if (parts.Length == 4)
            {
                return Color.FromArgb(
                    int.Parse(parts[0]),
                    int.Parse(parts[1]),
                    int.Parse(parts[2]),
                    int.Parse(parts[3])
                );
            }

            return Color.Black;
        }
    }
}
