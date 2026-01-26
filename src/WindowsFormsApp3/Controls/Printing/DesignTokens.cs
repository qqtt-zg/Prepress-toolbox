using System.Drawing;

namespace WindowsFormsApp3.Controls.Printing
{
    /// <summary>
    /// 拼版工具设计令牌
    /// </summary>
    public static class DesignTokens
    {
        // === 色彩系统 ===
        
        // 主色
        public static Color PrimaryColor = Color.FromArgb(24, 144, 255);
        public static Color PrimaryHover = Color.FromArgb(64, 169, 255);
        public static Color PrimaryActive = Color.FromArgb(9, 109, 217);
        
        // 背景色（浅色主题）
        public static Color BgPrimary = Color.White;
        public static Color BgSecondary = Color.FromArgb(250, 250, 250);
        public static Color BgTertiary = Color.FromArgb(245, 245, 245);
        
        // 文本色
        public static Color TextPrimary = Color.FromArgb(38, 38, 38);
        public static Color TextSecondary = Color.FromArgb(89, 89, 89);
        public static Color TextTertiary = Color.FromArgb(140, 140, 140);
        
        // === 间距系统 ===
        public const int SpacingXS = 4;
        public const int SpacingSM = 8;
        public const int SpacingBase = 16;
        public const int SpacingLG = 20;
        public const int SpacingXL = 24;
        public const int SpacingXXL = 32;
        
        // === 圆角 ===
        public const int BorderRadiusBase = 4;
        public const int BorderRadiusLG = 8;
        
        // === 字体 ===
        public static Font FontBase = new Font("Microsoft YaHei UI", 9F);
        public static Font FontLarge = new Font("Microsoft YaHei UI", 10F);
        public static Font FontHeading = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
    }
}
