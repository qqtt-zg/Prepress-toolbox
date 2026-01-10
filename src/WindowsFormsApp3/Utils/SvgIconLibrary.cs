using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// SVG图标库 - 提供预定义的SVG路径
    /// </summary>
    public static class SvgIconLibrary
    {
        /// <summary>
        /// 闪光图标
        /// </summary>
        public static string Sparkle => @"
            <svg viewBox=""0 0 24 24"" fill=""none"">
                <path d=""M12 2L14 9L21 11L14 13L12 20L10 13L3 11L10 9L12 2Z"" fill=""url(#sparkle-gradient)""/>
                <defs>
                    <radialGradient id=""sparkle-gradient"">
                        <stop offset=""0%"" style=""stop-color:#FFFF00""/>
                        <stop offset=""50%"" style=""stop-color:#FFCC00""/>
                        <stop offset=""100%"" style=""stop-color:#FFD700""/>
                    </radialGradient>
                </defs>
            </svg>";

        /// <summary>
        /// 月亮图标
        /// </summary>
        public static string Moon => @"
            <svg viewBox=""0 0 24 24"" fill=""none"">
                <path d=""M21 12.79A9 9 0 1 1 11.21 3 7 7 0 0 21 12.79z"" fill=""url(#moon-gradient)""/>
                <defs>
                    <linearGradient id=""moon-gradient"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
                        <stop offset=""0%"" style=""stop-color:#4A5568""/>
                        <stop offset=""100%"" style=""stop-color:#2D3748""/>
                    </linearGradient>
                </defs>
            </svg>";

        /// <summary>
        /// 禁止图标
        /// </summary>
        public static string Prohibited => @"
            <svg viewBox=""0 0 24 24"" fill=""none"">
                <circle cx=""12"" cy=""12"" r=""10"" fill=""none"" stroke=""#DC2626"" stroke-width=""2""/>
                <line x1=""8"" y1=""8"" x2=""16"" y2=""16"" stroke=""#DC2626"" stroke-width=""2"" stroke-linecap=""round""/>
            </svg>";

        /// <summary>
        /// 红色圆形图标
        /// </summary>
        public static string RedCircle => @"
            <svg viewBox=""0 0 24 24"" fill=""none"">
                <circle cx=""12"" cy=""12"" r=""10"" fill=""url(#red-gradient)""/>
                <circle cx=""12"" cy=""12"" r=""8"" fill=""none"" stroke=""#99B1FB"" stroke-width=""1"" opacity=""0.5""/>
                <defs>
                    <radialGradient id=""red-gradient"">
                        <stop offset=""0%"" style=""stop-color:#EF4444""/>
                        <stop offset=""100%"" style=""stop-color:#DC2626""/>
                    </radialGradient>
                </defs>
            </svg>";

        /// <summary>
        /// 太阳图标
        /// </summary>
        public static string Sun => @"
            <svg viewBox=""0 0 24 24"" fill=""none"">
                <circle cx=""12"" cy=""12"" r=""8"" fill=""#FFA500""/>
                <path d=""M12 2v2l2 2 0-2-2m-2 0v-2l-2-2 0 2 2z"" fill=""#FFA500""/>
            </svg>";

        /// <summary>
        /// 彩虹渐变图标
        /// </summary>
        public static string Rainbow => @"
            <svg viewBox=""0 0 24 24"" fill=""none"">
                <circle cx=""12"" cy=""12"" r=""10"" fill=""url(#rainbow-gradient)""/>
                <defs>
                    <linearGradient id=""rainbow-gradient"" x1=""0%"" y1=""0%"" x2=""100%"" y2=""100%"">
                        <stop offset=""0%"" style=""stop-color:#FF0000""/>
                        <stop offset=""16.66%"" style=""stop-color:#FF7F00""/>
                        <stop offset=""33.33%"" style=""stop-color:#FFFF00""/>
                        <stop offset=""50%"" style=""stop-color:#00FF00""/>
                        <stop offset=""66.66%"" style=""stop-color:#0000FF""/>
                        <stop offset=""83.33%"" style=""stop-color:#4B0082""/>
                        <stop offset=""100%"" style=""stop-color:#9400D3""/>
                    </linearGradient>
                </defs>
            </svg>";

        /// <summary>
        /// 黑色圆形图标
        /// </summary>
        public static string BlackCircle => @"
            <svg viewBox=""0 0 24 24"" fill=""none"">
                <circle cx=""12"" cy=""12"" r=""10"" fill=""#000000""/>
            </svg>";

        /// <summary>
        /// 文件夹图标
        /// </summary>
        public static string Folder => @"
            <svg viewBox=""0 0 1024 1024"">
                <path d=""M880 298.4H521L403.7 186.2l-1.5-1.4c-1.7-1.6-4-2.5-6.3-2.5H144c-17.7 0-32 14.3-32 32v647.4c0 17.7 14.3 32 32 32h736c17.7 0 32-14.3 32-32V330.4c0-17.7-14.3-32-32-32z"" fill=""#FFC107""/>
            </svg>";

        /// <summary>
        /// 获取所有预定义图标的列表
        /// </summary>
        public static Dictionary<string, string> GetAllIcons()
        {
            return new Dictionary<string, string>
            {
                { "sparkle", Sparkle },
                { "moon", Moon },
                { "prohibited", Prohibited },
                { "red-circle", RedCircle },
                { "sun", Sun },
                { "rainbow", Rainbow },
                { "black-circle", BlackCircle }
            };
        }
    }
}