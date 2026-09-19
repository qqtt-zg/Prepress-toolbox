using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3
{
    public partial class MaterialSelectFormModern
    {
        /// <summary>
        /// 应用主题到左侧待处理工作台及其动态创建的分组卡片。
        /// </summary>
        private void ApplyThemeToBatchFileList(ThemeDefinition theme, bool isDark)
        {
            if (pnlFileList == null)
            {
                return;
            }

            pnlFileList.SuspendLayout();
            try
            {
                pnlFileList.BackColor = theme.Background;

                if (pnlTopBatchHeader != null)
                {
                    pnlTopBatchHeader.BackColor = theme.BackHover;
                }

                if (pnlBottomToolbar != null)
                {
                    pnlBottomToolbar.BackColor = theme.BackHover;
                }

                if (pnlCardsContainer != null)
                {
                    pnlCardsContainer.BackColor = theme.BackHover;
                }

                if (lblBatchTitle != null)
                {
                    lblBatchTitle.ForeColor = theme.TextPrimary;
                }

                if (lblBatchHelp != null)
                {
                    lblBatchHelp.ForeColor = theme.TextSecondary;
                    if (lblBatchHelp.Parent is Panel helpPanel)
                    {
                        helpPanel.BackColor = theme.BackHover;
                    }
                }

                foreach (var button in new[] { btnMoveUp, btnMoveDown, btnNewGroupDirect, btnDeleteBatchRows })
                {
                    if (button != null)
                    {
                        ApplyThemeToMaterialButton(button, theme, isDark);
                    }
                }

                ApplyThemeToBatchDataGridView(dgvBatchFiles, theme);

                if (pnlCardsContainer == null)
                {
                    return;
                }

                foreach (Control control in pnlCardsContainer.Controls)
                {
                    if (control is not Panel card)
                    {
                        continue;
                    }

                    card.BackColor = theme.Surface;
                    foreach (Control child in card.Controls)
                    {
                        if (child is BatchGroupHeaderCard headerCard)
                        {
                            headerCard.ApplyTheme(theme);
                        }
                        else if (child is DataGridView groupGrid)
                        {
                            ApplyThemeToBatchDataGridView(groupGrid, theme);
                        }
                    }

                    card.Invalidate();
                }
            }
            finally
            {
                pnlFileList.ResumeLayout(true);
            }
        }

        /// <summary>
        /// 统一设置左侧文件表格的主题样式，保留已有的选择与编辑行为。
        /// </summary>
        private void ApplyThemeToBatchDataGridView(DataGridView grid, ThemeDefinition theme)
        {
            if (grid == null)
            {
                return;
            }

            Color selectedText = GetReadableThemeTextColor(theme.Primary, theme.TextPrimary);

            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = theme.SurfaceLight;
            grid.GridColor = theme.Border;

            grid.DefaultCellStyle.BackColor = theme.SurfaceLight;
            grid.DefaultCellStyle.ForeColor = theme.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = theme.Primary;
            grid.DefaultCellStyle.SelectionForeColor = selectedText;

            grid.RowsDefaultCellStyle.BackColor = theme.SurfaceLight;
            grid.RowsDefaultCellStyle.ForeColor = theme.TextPrimary;
            grid.RowsDefaultCellStyle.SelectionBackColor = theme.Primary;
            grid.RowsDefaultCellStyle.SelectionForeColor = selectedText;

            grid.AlternatingRowsDefaultCellStyle.BackColor = theme.Surface;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = theme.TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = theme.Primary;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = selectedText;

            grid.ColumnHeadersDefaultCellStyle.BackColor = theme.BackHover;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = theme.TextPrimary;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = theme.BackHover;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = theme.TextPrimary;

            grid.Invalidate();
        }

        private static Color BlendThemeColor(Color background, Color foreground, int foregroundAlpha)
        {
            int alpha = Math.Max(0, Math.Min(255, foregroundAlpha));
            int inverseAlpha = 255 - alpha;
            return Color.FromArgb(
                (background.R * inverseAlpha + foreground.R * alpha) / 255,
                (background.G * inverseAlpha + foreground.G * alpha) / 255,
                (background.B * inverseAlpha + foreground.B * alpha) / 255);
        }

        private static Color GetReadableThemeTextColor(Color background, Color preferred)
        {
            if (GetThemeContrastRatio(background, preferred) >= 4.5D)
            {
                return preferred;
            }

            return GetThemeContrastRatio(background, Color.White) >= GetThemeContrastRatio(background, Color.Black)
                ? Color.White
                : Color.Black;
        }

        private static double GetThemeContrastRatio(Color first, Color second)
        {
            double firstLuminance = GetThemeRelativeLuminance(first);
            double secondLuminance = GetThemeRelativeLuminance(second);
            return (Math.Max(firstLuminance, secondLuminance) + 0.05D) /
                   (Math.Min(firstLuminance, secondLuminance) + 0.05D);
        }

        private static double GetThemeRelativeLuminance(Color color)
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
    }
}
