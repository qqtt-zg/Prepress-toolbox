using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Forms.Controls
{
    /// <summary>
    /// 方案五：智能工艺分组卡片头部控件（用于统一呈现公共工艺参数）
    /// </summary>
    public class BatchGroupHeaderCard : Panel
    {
        private readonly BatchProcessGroup _group;
        public BatchProcessGroup Group => _group;

        public event EventHandler HeaderClicked;
        public event EventHandler LockBadgeClicked;

        private Rectangle _lockBadgeRect;
        private ThemeDefinition _theme;

        public BatchGroupHeaderCard(BatchProcessGroup group)
        {
            _group = group ?? throw new ArgumentNullException(nameof(group));
            this.Dock = DockStyle.Top;
            this.Height = 36;
            this.Padding = new Padding(6, 4, 6, 4);
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;
            this.MouseDown += BatchGroupHeaderCard_MouseDown;

            UpdateCardStyle();
        }

        /// <summary>
        /// 应用当前界面的主题配色。
        /// </summary>
        public void ApplyTheme(ThemeDefinition theme)
        {
            _theme = theme;
            UpdateCardStyle();
        }

        private void BatchGroupHeaderCard_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_lockBadgeRect.Contains(e.Location))
                {
                    LockBadgeClicked?.Invoke(this, EventArgs.Empty);
                    return;
                }
                HeaderClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        public void UpdateCardStyle()
        {
            if (_theme != null)
            {
                if (_group.IsPreserveGroup)
                {
                    BackColor = Blend(_theme.Surface, _theme.AccentColor4, 36);
                }
                else if (_group.IsLocked)
                {
                    BackColor = _theme.BackHover;
                }
                else
                {
                    BackColor = _theme.BackActive;
                }

                Invalidate();
                return;
            }

            if (_group.IsPreserveGroup)
            {
                this.BackColor = Color.FromArgb(250, 245, 255); // 返单淡紫色
            }
            else if (_group.IsLocked)
            {
                this.BackColor = Color.FromArgb(245, 245, 245);
            }
            else
            {
                this.BackColor = Color.FromArgb(240, 247, 255); // 新单浅蓝色
            }
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var theme = _theme;

            // 仅绘制底部分割细线（杜绝与外层 cardPanel 形成双层生硬线框）
            Color dividerColor = theme == null
                ? (_group.IsPreserveGroup ? Color.FromArgb(233, 213, 255) : Color.FromArgb(226, 232, 240))
                : theme.Border;
            using (var pen = new Pen(dividerColor, 1F))
            {
                g.DrawLine(pen, 0, this.Height - 1, this.Width, this.Height - 1);
            }

            int centerY = this.Height / 2;

            // 1. 先计算并绘制右侧状态指示标签（纯净微圆角胶囊）
            string statusText = (_group.IsPreserveGroup || _group.IsLocked) ? "已锁定" : "联动";
            Color statusBg = theme == null
                ? ((_group.IsPreserveGroup || _group.IsLocked) ? Color.FromArgb(243, 232, 255) : Color.FromArgb(240, 253, 244))
                : Blend(theme.Surface, (_group.IsPreserveGroup || _group.IsLocked) ? theme.AccentColor4 : theme.Success, 36);
            Color statusFg = theme == null
                ? ((_group.IsPreserveGroup || _group.IsLocked) ? Color.FromArgb(109, 40, 217) : Color.FromArgb(21, 128, 61))
                : GetReadableTextColor(statusBg, theme.TextPrimary);
            Color statusBorder = theme == null
                ? ((_group.IsPreserveGroup || _group.IsLocked) ? Color.FromArgb(216, 180, 254) : Color.FromArgb(187, 247, 208))
                : ((_group.IsPreserveGroup || _group.IsLocked) ? theme.AccentColor4 : theme.Success);

            int sW = 54;
            using (var statFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold))
            {
                var sSize = g.MeasureString(statusText, statFont);
                sW = (int)Math.Ceiling(sSize.Width) + 14;
                int sH = 20;
                int sX = this.Width - sW - 8;
                int sY = centerY - (sH / 2);

                _lockBadgeRect = new Rectangle(sX, sY, sW, sH);
                using (var sbBrush = new SolidBrush(statusBg))
                {
                    FillRoundRect(g, sbBrush, _lockBadgeRect, 3);
                }
                using (var pen = new Pen(statusBorder, 1F))
                {
                    DrawRoundRect(g, pen, _lockBadgeRect, 3);
                }
                using (var textBrush = new SolidBrush(statusFg))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    g.DrawString(statusText, statFont, textBrush, new RectangleF(sX, sY, sW, sH), sf);
                }
            }

            // 左侧绘制区域边界限制
            int rightBoundary = this.Width - sW - 14;
            int curX = 8;

            // 2. 折叠箭头
            string arrow = _group.IsCollapsed ? "▸" : "▾";
            using (var arrowFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(theme?.TextSecondary ?? Color.FromArgb(100, 116, 139)))
            {
                g.DrawString(arrow, arrowFont, brush, curX, centerY - 7);
                curX += 13;
            }

            // 3. 组标题（如【一组】、【二组】）
            Color titleColor = theme == null
                ? (_group.IsPreserveGroup
                    ? Color.FromArgb(114, 46, 209)
                    : (_group.IsLocked ? Color.FromArgb(38, 38, 38) : Color.FromArgb(15, 23, 42)))
                : GetReadableTextColor(BackColor, theme.TextPrimary);
            using (var titleFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(titleColor))
            {
                string title = _group.GroupName ?? "【一组】";
                g.DrawString(title, titleFont, brush, curX, centerY - 7);
                var size = g.MeasureString(title, titleFont);
                curX += (int)Math.Ceiling(size.Width) + 5;
            }

            // 4. 款数徽章（精致圆角胶囊）
            int count = _group.Items?.Count ?? 0;
            string countText = _group.IsPreserveGroup ? $"{count}款返单" : $"{count}款新单";
            using (var countFont = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Bold))
            {
                var cSize = g.MeasureString(countText, countFont);
                int badgeW = (int)Math.Ceiling(cSize.Width) + 8;
                int badgeH = 18;
                int badgeY = centerY - (badgeH / 2);
                Color badgeBg = theme == null
                    ? (_group.IsPreserveGroup ? Color.FromArgb(243, 232, 255) : Color.FromArgb(224, 231, 255))
                    : Blend(theme.Surface, _group.IsPreserveGroup ? theme.AccentColor4 : theme.Primary, 30);
                Color badgeFg = theme == null
                    ? (_group.IsPreserveGroup ? Color.FromArgb(114, 46, 209) : Color.FromArgb(29, 78, 216))
                    : GetReadableTextColor(badgeBg, theme.TextPrimary);
                Color badgeBorder = theme == null
                    ? (_group.IsPreserveGroup ? Color.FromArgb(216, 180, 254) : Color.FromArgb(191, 219, 254))
                    : (_group.IsPreserveGroup ? theme.AccentColor4 : theme.Primary);

                var badgeRect = new Rectangle(curX, badgeY, badgeW, badgeH);
                using (var bgBrush = new SolidBrush(badgeBg))
                {
                    FillRoundRect(g, bgBrush, badgeRect, 3);
                }
                using (var borderPen = new Pen(badgeBorder, 1F))
                {
                    DrawRoundRect(g, borderPen, badgeRect, 3);
                }
                using (var textBrush = new SolidBrush(badgeFg))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    g.DrawString(countText, countFont, textBrush, new RectangleF(curX, badgeY, badgeW, badgeH), sf);
                }
                curX += badgeW + 6;
            }

            // 5. 方案 B：主副信息分离 —— 仅保留 1~2 个核心工艺高亮胶囊，次级信息转为精炼单行灰色文字
            if (_group.IsPreserveGroup)
            {
                DrawPill(g, ref curX, centerY, rightBoundary, "返单前缀",
                    theme == null ? Color.FromArgb(249, 240, 255) : Blend(theme.Surface, theme.AccentColor4, 36),
                    theme == null ? Color.FromArgb(114, 46, 209) : GetReadableTextColor(Blend(theme.Surface, theme.AccentColor4, 36), theme.TextPrimary),
                    theme == null ? Color.FromArgb(211, 173, 247) : theme.AccentColor4);
            }

            // 核心高亮胶囊 1：材料（暖金琥珀色，视觉主焦点）
            string matText = string.IsNullOrEmpty(_group.Material) ? "未指材料" : _group.Material;
            DrawPill(g, ref curX, centerY, rightBoundary, matText,
                theme == null
                    ? (_group.IsPreserveGroup ? Color.FromArgb(249, 240, 255) : Color.FromArgb(254, 243, 199))
                    : Blend(theme.Surface, _group.IsPreserveGroup ? theme.AccentColor4 : theme.Warning, 34),
                theme == null
                    ? (_group.IsPreserveGroup ? Color.FromArgb(114, 46, 209) : Color.FromArgb(180, 83, 9))
                    : GetReadableTextColor(Blend(theme.Surface, _group.IsPreserveGroup ? theme.AccentColor4 : theme.Warning, 34), theme.TextPrimary),
                theme == null
                    ? (_group.IsPreserveGroup ? Color.FromArgb(211, 173, 247) : Color.FromArgb(253, 230, 138))
                    : (_group.IsPreserveGroup ? theme.AccentColor4 : theme.Warning));

            // 解析工艺膜与颜色模式
            string colorMode = _group.ColorMode;
            string filmType = _group.FilmType;
            if (string.IsNullOrEmpty(colorMode) && string.IsNullOrEmpty(filmType) && !string.IsNullOrEmpty(_group.Process))
            {
                if (_group.Process.Contains("黑白"))
                {
                    colorMode = "黑白";
                    filmType = _group.Process.Replace("黑白", "").Trim();
                }
                else if (_group.Process.Contains("彩色"))
                {
                    colorMode = "彩色";
                    filmType = _group.Process.Replace("彩色", "").Trim();
                }
                else
                {
                    filmType = _group.Process;
                }
            }
            if (string.IsNullOrEmpty(colorMode)) colorMode = "黑白";

            // 核心高亮胶囊 2：覆膜（仅当有覆膜且不为“不过膜”时显示清新浅绿胶囊）
            if (!string.IsNullOrEmpty(filmType) && filmType != "不过膜")
            {
                DrawPill(g, ref curX, centerY, rightBoundary, filmType,
                    theme == null ? Color.FromArgb(240, 253, 244) : Blend(theme.Surface, theme.Success, 34),
                    theme == null ? Color.FromArgb(21, 128, 61) : GetReadableTextColor(Blend(theme.Surface, theme.Success, 34), theme.TextPrimary),
                    theme == null ? Color.FromArgb(187, 247, 208) : theme.Success);
            }

            // 次级工艺特征文本列表（平张/卷装 · 连拼/折手 · 彩色/黑白 · 切刀形状）
            var subTraits = new List<string>();

            string matType = !string.IsNullOrEmpty(_group.MaterialType) ? _group.MaterialType : "";
            string layPattern = !string.IsNullOrEmpty(_group.LayoutPattern) ? _group.LayoutPattern : "";
            if (string.IsNullOrEmpty(matType) && string.IsNullOrEmpty(layPattern) && !string.IsNullOrEmpty(_group.ImpositionMode))
            {
                var parts = _group.ImpositionMode.Split(new[] { '·', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0) matType = parts[0];
                if (parts.Length > 1) layPattern = parts[1];
            }
            if (string.IsNullOrEmpty(matType)) matType = "平张";
            if (string.IsNullOrEmpty(layPattern)) layPattern = "连拼";
            subTraits.Add(matType);
            subTraits.Add(layPattern);
            subTraits.Add(colorMode);

            string shapeName = "直角";
            if (_group.Shape == "Round" || _group.Shape == "圆角" || _group.Shape == "RoundRect")
            {
                shapeName = (!string.IsNullOrEmpty(_group.RoundRadius) && _group.RoundRadius != "0") ? $"圆角R{_group.RoundRadius}" : "圆角";
                subTraits.Add(shapeName);
            }
            else if (_group.Shape == "Special" || _group.Shape == "异形")
            {
                subTraits.Add("异形");
            }
            else
            {
                subTraits.Add("直角");
            }

            if (filmType == "不过膜")
            {
                subTraits.Add("不过膜");
            }

            // 绘制优雅的次级文本单行（以中圆点 · 连接，杜绝彩虹碎片化）
            string subTraitsText = string.Join(" · ", subTraits);
            int availSubW = rightBoundary - curX;
            if (availSubW > 24)
            {
                using (var subFont = new Font("Microsoft YaHei UI", 8F))
                using (var subBrush = new SolidBrush(theme?.TextSecondary ?? Color.FromArgb(100, 116, 139)))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Near,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    g.DrawString(subTraitsText, subFont, subBrush, new RectangleF(curX, 0, availSubW, this.Height), sf);
                }
            }
        }

        private static Color Blend(Color background, Color foreground, int foregroundAlpha)
        {
            int alpha = Math.Max(0, Math.Min(255, foregroundAlpha));
            int inverseAlpha = 255 - alpha;
            return Color.FromArgb(
                (background.R * inverseAlpha + foreground.R * alpha) / 255,
                (background.G * inverseAlpha + foreground.G * alpha) / 255,
                (background.B * inverseAlpha + foreground.B * alpha) / 255);
        }

        private static Color GetReadableTextColor(Color background, Color preferred)
        {
            if (GetContrastRatio(background, preferred) >= 4.5D)
            {
                return preferred;
            }

            return GetContrastRatio(background, Color.White) >= GetContrastRatio(background, Color.Black)
                ? Color.White
                : Color.Black;
        }

        private static double GetContrastRatio(Color first, Color second)
        {
            double firstLuminance = GetRelativeLuminance(first);
            double secondLuminance = GetRelativeLuminance(second);
            return (Math.Max(firstLuminance, secondLuminance) + 0.05D) /
                   (Math.Min(firstLuminance, secondLuminance) + 0.05D);
        }

        private static double GetRelativeLuminance(Color color)
        {
            double ConvertChannel(int channel)
            {
                double value = channel / 255D;
                return value <= 0.03928D
                    ? value / 12.92D
                    : Math.Pow((value + 0.055D) / 1.055D, 2.4D);
            }

            return 0.2126D * ConvertChannel(color.R) +
                   0.7152D * ConvertChannel(color.G) +
                   0.0722D * ConvertChannel(color.B);
        }

        private void DrawPill(Graphics g, ref int curX, int centerY, int rightBoundary, string text, Color bg, Color fg, Color border)
        {
            if (string.IsNullOrEmpty(text)) return;
            using (var font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold))
            {
                var size = g.MeasureString(text, font);
                int pillW = Math.Max(34, (int)Math.Ceiling(size.Width) + 12);
                int pillH = 20;
                int pillY = centerY - (pillH / 2);

                if (curX + pillW > rightBoundary)
                {
                    int availW = rightBoundary - curX;
                    if (availW < 20) return;
                    pillW = availW;
                }

                var pillRect = new Rectangle(curX, pillY, pillW, pillH);
                using (var brush = new SolidBrush(bg))
                {
                    FillRoundRect(g, brush, pillRect, 3);
                }
                using (var pen = new Pen(border, 1F))
                {
                    DrawRoundRect(g, pen, pillRect, 3);
                }
                using (var textBrush = new SolidBrush(fg))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter,
                        FormatFlags = StringFormatFlags.NoWrap
                    };
                    g.DrawString(text, font, textBrush, new RectangleF(curX, pillY, pillW, pillH), sf);
                }
                curX += pillW + 6;
            }
        }

        private static void FillRoundRect(Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using (var path = CreateRoundRectPath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        private static void DrawRoundRect(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (var path = CreateRoundRectPath(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static GraphicsPath CreateRoundRectPath(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0)
            {
                path.AddRectangle(r);
                return path;
            }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// 将完整路径格式化为最多三级显示（如 威立德→卷装→折手KM）
        /// </summary>
        public static string FormatExportPathSummary(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return "根目录";
            try
            {
                string p = fullPath.Replace('/', '\\').TrimEnd('\\');

                List<string> configuredRoots = null;
                try
                {
                    configuredRoots = WindowsFormsApp3.Utils.AppSettings.ExportPaths;
                }
                catch { }
                if (configuredRoots != null && configuredRoots.Count > 0)
                {
                    foreach (var root in configuredRoots)
                    {
                        if (string.IsNullOrWhiteSpace(root)) continue;
                        string r = root.Replace('/', '\\').TrimEnd('\\');
                        if (p.StartsWith(r, StringComparison.OrdinalIgnoreCase))
                        {
                            string rel = p.Substring(r.Length).Trim('\\');
                            if (!string.IsNullOrEmpty(rel))
                            {
                                var segs = rel.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                                if (segs.Length > 3)
                                {
                                    segs = segs.Skip(segs.Length - 3).ToArray();
                                }
                                return string.Join("→", segs);
                            }
                        }
                    }
                }

                // 兜底：去除盘符后提取末尾最多三级
                int colonIdx = p.IndexOf(':');
                string rest = colonIdx >= 0 ? p.Substring(colonIdx + 1) : p;
                var parts = rest.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 3)
                {
                    parts = parts.Skip(parts.Length - 3).ToArray();
                }

                if (parts.Length > 0)
                {
                    return string.Join("→", parts);
                }

                return fullPath;
            }
            catch
            {
                return fullPath;
            }
        }
    }
}
