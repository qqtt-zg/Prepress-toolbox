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
                // 方案 A 经典瑞士冷灰顶冠 + 纯白工作舱：
                // 顶栏与底栏统一为雅致冷灰 (#eef2f6)，消除纯白环境下的白底融化；
                // 自定义/测试主题下保持主题变量映射以严格通过契约测试。
                bool isSwissPureWhite = theme.IsBuiltIn && !isDark;

                pnlFileList.BackColor = isSwissPureWhite ? Color.White : theme.Background;

                if (pnlTopBatchHeader != null)
                {
                    pnlTopBatchHeader.BackColor = isSwissPureWhite ? Color.FromArgb(238, 242, 246) : theme.BackHover;
                }

                if (pnlBottomToolbar != null)
                {
                    pnlBottomToolbar.BackColor = isSwissPureWhite ? Color.White : theme.BackHover;
                }

                if (pnlCardsContainer != null)
                {
                    pnlCardsContainer.BackColor = isSwissPureWhite ? Color.White : theme.BackHover;
                }

                if (lblBatchTitle != null)
                {
                    lblBatchTitle.ForeColor = theme.TextPrimary;
                }
                if (_batchProgressLabel != null) _batchProgressLabel.ForeColor = theme.TextPrimary;
                if (_batchMetadataProgressBar != null) _batchMetadataProgressBar.Fill = theme.Primary;

                if (lblBatchHelp != null)
                {
                    lblBatchHelp.ForeColor = theme.TextSecondary;
                    if (lblBatchHelp.Parent is Panel helpPanel)
                    {
                        helpPanel.BackColor = isSwissPureWhite ? Color.FromArgb(238, 242, 246) : theme.BackHover;
                    }
                }

                if (pnlPreviewFooter != null)
                {
                    pnlPreviewFooter.BackColor = isDark ? theme.BackHover : Color.FromArgb(238, 242, 246);
                    if (lblPreviewFooterTip != null) lblPreviewFooterTip.ForeColor = isDark ? theme.TextSecondary : Color.FromArgb(100, 116, 139);
                    if (lblPreviewFooterCollapse != null) lblPreviewFooterCollapse.ForeColor = isDark ? theme.TextSecondary : Color.FromArgb(100, 116, 139);
                }

                foreach (var button in new[] { btnMoveUp, btnMoveDown, btnNewGroupDirect, btnDeleteBatchRows, btnRefreshBatch, btnCancelAllBatchRows })
                {
                    if (button != null)
                    {
                        button.BorderWidth = 1F;
                        ApplyThemeToMaterialButton(button, theme, isDark);
                    }
                }

                if (btnMoveUp != null && isSwissPureWhite)
                {
                    btnMoveUp.BorderWidth = 1F;
                    btnMoveUp.DefaultBorderColor = Color.FromArgb(203, 213, 225);
                }

                if (btnMoveDown != null && isSwissPureWhite)
                {
                    btnMoveDown.BorderWidth = 1F;
                    btnMoveDown.DefaultBorderColor = Color.FromArgb(203, 213, 225);
                }

                if (btnSelectAll != null && isSwissPureWhite)
                {
                    btnSelectAll.BorderWidth = 1F;
                    btnSelectAll.DefaultBorderColor = Color.FromArgb(203, 213, 225);
                }

                if (btnInvertSelect != null && isSwissPureWhite)
                {
                    btnInvertSelect.BorderWidth = 1F;
                    btnInvertSelect.DefaultBorderColor = Color.FromArgb(203, 213, 225);
                }

                if (btnRefreshBatch != null && isSwissPureWhite)
                {
                    btnRefreshBatch.BorderWidth = 1F;
                    btnRefreshBatch.DefaultBorderColor = Color.FromArgb(203, 213, 225);
                }

                if (btnNewGroupDirect != null)
                {
                    btnNewGroupDirect.BorderWidth = 1F;
                    btnNewGroupDirect.Ghost = true;
                    btnNewGroupDirect.Type = AntdUI.TTypeMini.Primary;
                    btnNewGroupDirect.DefaultBack = theme.BackActive;
                    btnNewGroupDirect.DefaultBorderColor = isDark ? theme.Primary : Color.FromArgb(145, 202, 255);
                    btnNewGroupDirect.ForeColor = GetReadableThemeTextColor(theme.Primary, theme.TextPrimary);
                }

                // 强化底部危险操作按钮的属性感知（显式 BorderWidth=1F 柔和线框，杜绝刺眼实心红块）
                if (btnDeleteBatchRows != null)
                {
                    btnDeleteBatchRows.BorderWidth = 1F;
                    btnDeleteBatchRows.Ghost = true;
                    btnDeleteBatchRows.Type = AntdUI.TTypeMini.Error;
                    btnDeleteBatchRows.ForeColor = isDark ? theme.Error : Color.FromArgb(207, 19, 34);
                    btnDeleteBatchRows.DefaultBorderColor = isDark ? theme.Error : Color.FromArgb(255, 163, 158);
                    btnDeleteBatchRows.DefaultBack = isDark ? theme.Surface : Color.FromArgb(255, 241, 240);
                }

                if (btnCancelAllBatchRows != null)
                {
                    btnCancelAllBatchRows.BorderWidth = 1F;
                    btnCancelAllBatchRows.Ghost = true;
                    btnCancelAllBatchRows.Type = AntdUI.TTypeMini.Error;
                    btnCancelAllBatchRows.ForeColor = isDark ? theme.Error : Color.FromArgb(207, 19, 34);
                    btnCancelAllBatchRows.DefaultBorderColor = isDark ? theme.Error : Color.FromArgb(255, 163, 158);
                    btnCancelAllBatchRows.DefaultBack = isDark ? theme.Surface : Color.White;
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

            // 现代 AntdUI 风格：柔和浅蓝高亮底色 (#eff6ff) + 锐利深蓝字体 (#1d4ed8)，杜绝沉重生硬的深蓝实色块
            Color selectBg = BlendThemeColor(theme.Surface, theme.Primary, 32);
            Color selectFg = BlendThemeColor(Color.FromArgb(15, 23, 42), theme.Primary, 180);

            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = theme.SurfaceLight;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = theme.Border;

            grid.DefaultCellStyle.BackColor = theme.SurfaceLight;
            grid.DefaultCellStyle.ForeColor = theme.TextPrimary;
            grid.DefaultCellStyle.SelectionBackColor = selectBg;
            grid.DefaultCellStyle.SelectionForeColor = selectFg;

            grid.RowsDefaultCellStyle.BackColor = theme.SurfaceLight;
            grid.RowsDefaultCellStyle.ForeColor = theme.TextPrimary;
            grid.RowsDefaultCellStyle.SelectionBackColor = selectBg;
            grid.RowsDefaultCellStyle.SelectionForeColor = selectFg;

            grid.AlternatingRowsDefaultCellStyle.BackColor = theme.Surface;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = theme.TextPrimary;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = selectBg;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = selectFg;

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
