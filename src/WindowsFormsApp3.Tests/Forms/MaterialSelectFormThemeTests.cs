using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Forms.Main;
using WindowsFormsApp3.Models;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class MaterialSelectFormThemeTests
    {
        public MaterialSelectFormThemeTests()
        {
            if (!WindowsFormsApp3.Utils.AppSettings.IsInitialized)
            {
                WindowsFormsApp3.Utils.AppSettings.Initialize(
                    new WindowsFormsApp3.Services.FileLogger(
                        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MaterialSelectFormThemeTests")));
            }
        }

        [StaFact]
        public void ApplyTheme_ShouldThemeTheEntireExistingBatchWorkbench()
        {
            var theme = CreateTheme(
                background: Color.FromArgb(30, 31, 34),
                surface: Color.FromArgb(42, 44, 48),
                surfaceLight: Color.FromArgb(55, 57, 61),
                backHover: Color.FromArgb(48, 50, 54),
                backActive: Color.FromArgb(61, 72, 84));

            using (var form = CreateForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\ThemeJob_54x84.pdf" });

                form.ApplyTheme(theme);

                var fileList = GetField<Panel>(form, "pnlFileList");
                var header = GetField<Panel>(form, "pnlTopBatchHeader");
                var toolbar = GetField<Panel>(form, "pnlBottomToolbar");
                var cardsContainer = GetField<Panel>(form, "pnlCardsContainer");
                var title = GetField<AntdUI.Label>(form, "lblBatchTitle");
                var help = GetField<Label>(form, "lblBatchHelp");
                var moveUpButton = GetField<AntdUI.Button>(form, "btnMoveUp");
                var newGroupButton = GetField<AntdUI.Button>(form, "btnNewGroupDirect");
                var fallbackGrid = GetField<DataGridView>(form, "dgvBatchFiles");
                var card = cardsContainer.Controls.OfType<Panel>().Single();
                var cardHeader = card.Controls.OfType<BatchGroupHeaderCard>().Single();
                var grid = card.Controls.OfType<DataGridView>().Single();

                Assert.Equal(theme.Background, fileList.BackColor);
                Assert.Equal(theme.BackHover, header.BackColor);
                Assert.Equal(theme.BackHover, toolbar.BackColor);
                Assert.Equal(theme.BackHover, cardsContainer.BackColor);
                Assert.Equal(theme.TextPrimary, title.ForeColor);
                Assert.Equal(theme.TextSecondary, help.ForeColor);
                Assert.Equal(theme.BackHover, help.Parent.BackColor);
                Assert.Equal(theme.Surface, moveUpButton.DefaultBack);
                Assert.Equal(theme.BackActive, newGroupButton.DefaultBack);
                Assert.Equal(theme.Surface, card.BackColor);
                Assert.Equal(theme.BackActive, cardHeader.BackColor);
                Assert.Equal(theme.SurfaceLight, fallbackGrid.BackgroundColor);
                Assert.Equal(theme.SurfaceLight, grid.BackgroundColor);
                Assert.Equal(theme.BackHover, grid.ColumnHeadersDefaultCellStyle.BackColor);
                Assert.Equal(theme.TextPrimary, grid.DefaultCellStyle.ForeColor);
                Assert.Equal(theme.Border, grid.GridColor);
            }
        }

        [StaFact]
        public void ApplyTheme_ShouldRecolorExistingAndNewBatchCardsAfterThemeSwitch()
        {
            var darkTheme = CreateTheme(
                background: Color.FromArgb(30, 31, 34),
                surface: Color.FromArgb(42, 44, 48),
                surfaceLight: Color.FromArgb(55, 57, 61),
                backHover: Color.FromArgb(48, 50, 54),
                backActive: Color.FromArgb(61, 72, 84));
            var lightTheme = CreateTheme(
                background: Color.FromArgb(234, 242, 250),
                surface: Color.FromArgb(250, 253, 255),
                surfaceLight: Color.FromArgb(255, 255, 255),
                backHover: Color.FromArgb(226, 237, 248),
                backActive: Color.FromArgb(210, 229, 247),
                textPrimary: Color.FromArgb(31, 41, 55),
                textSecondary: Color.FromArgb(71, 85, 105));

            using (var form = CreateForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\ThemeJob_54x84.pdf" });
                form.ApplyTheme(darkTheme);

                form.ApplyTheme(lightTheme);

                var cardsContainer = GetField<Panel>(form, "pnlCardsContainer");
                var existingGrid = GetOnlyCardGrid(cardsContainer);
                Assert.Equal(lightTheme.Background, GetField<Panel>(form, "pnlFileList").BackColor);
                Assert.Equal(lightTheme.SurfaceLight, existingGrid.BackgroundColor);
                Assert.Equal(lightTheme.BackHover, existingGrid.ColumnHeadersDefaultCellStyle.BackColor);

                form.AppendPendingFile(@"C:\test\ThemeJob_Second_54x84.pdf");

                var renderedGrid = GetOnlyCardGrid(cardsContainer);
                Assert.Equal(2, form.BatchFileItems.Count);
                Assert.Equal(lightTheme.SurfaceLight, renderedGrid.BackgroundColor);
                Assert.Equal(lightTheme.TextPrimary, renderedGrid.DefaultCellStyle.ForeColor);
                Assert.Equal(lightTheme.Border, renderedGrid.GridColor);
            }
        }

        [StaFact]
        public void ApplyTheme_ShouldKeepPrimaryBatchToolbarTextReadableInLightTheme()
        {
            var lightTheme = CreateTheme(
                background: Color.FromArgb(248, 249, 250),
                surface: Color.White,
                surfaceLight: Color.White,
                backHover: Color.FromArgb(245, 247, 250),
                backActive: Color.FromArgb(230, 240, 255),
                textPrimary: Color.FromArgb(33, 37, 41),
                textSecondary: Color.FromArgb(108, 117, 125));

            using (var form = CreateForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\ThemeJob_54x84.pdf" });

                form.ApplyTheme(lightTheme);

                var newGroupButton = GetField<AntdUI.Button>(form, "btnNewGroupDirect");

                Assert.Equal(AntdUI.TTypeMini.Primary, newGroupButton.Type);
                Assert.True(newGroupButton.ForeColor.HasValue, "主按钮必须设置文字颜色");
                var textColor = newGroupButton.ForeColor.Value;
                Assert.True(
                    GetContrastRatio(lightTheme.Primary, textColor) >= 4.5D,
                    $"浅色主题主按钮文字与主色背景的对比度不足: {GetContrastRatio(lightTheme.Primary, textColor):F2}");
            }
        }

        [StaFact]
        public void TopMostButton_ShouldDefaultToSavedChoice_AndToggleIt()
        {
            var previous = WindowsFormsApp3.Utils.AppSettings.MaterialFormTopMost;
            try
            {
                WindowsFormsApp3.Utils.AppSettings.MaterialFormTopMost = false;
                using (var form = CreateForm())
                {
                    Assert.False(form.TopMost);
                    Assert.Equal("置顶窗口", MaterialSelectFormModern.GetTopMostToolTip(form.TopMost));

                    form.ToggleTopMost();

                    Assert.True(form.TopMost);
                    Assert.True(WindowsFormsApp3.Utils.AppSettings.MaterialFormTopMost);
                    Assert.Equal("取消置顶", MaterialSelectFormModern.GetTopMostToolTip(form.TopMost));
                }
            }
            finally
            {
                WindowsFormsApp3.Utils.AppSettings.MaterialFormTopMost = previous;
                WindowsFormsApp3.Utils.AppSettings.CommitChanges();
            }
        }

        private static MaterialSelectFormModern CreateForm()
        {
            return new MaterialSelectFormModern(
                materials: new List<string> { "PET" },
                fileName: @"C:\test\ThemeJob_54x84.pdf",
                regexResult: "ThemeJob",
                opacity: 1.0,
                width: "54",
                height: "84",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "1");
        }

        private static ThemeDefinition CreateTheme(
            Color background,
            Color surface,
            Color surfaceLight,
            Color backHover,
            Color backActive,
            Color? textPrimary = null,
            Color? textSecondary = null)
        {
            return new ThemeDefinition
            {
                Name = "测试主题",
                Background = background,
                Surface = surface,
                SurfaceLight = surfaceLight,
                InputBackground = surfaceLight,
                TextPrimary = textPrimary ?? Color.FromArgb(238, 241, 245),
                TextSecondary = textSecondary ?? Color.FromArgb(173, 181, 189),
                Border = Color.FromArgb(104, 116, 130),
                Primary = Color.FromArgb(70, 142, 224),
                Success = Color.FromArgb(82, 184, 120),
                Warning = Color.FromArgb(232, 178, 65),
                Error = Color.FromArgb(214, 89, 89),
                AccentColor1 = Color.FromArgb(96, 165, 250),
                AccentColor2 = Color.FromArgb(45, 212, 191),
                AccentColor3 = Color.FromArgb(251, 191, 36),
                AccentColor4 = Color.FromArgb(167, 139, 250),
                BackHover = backHover,
                BackActive = backActive
            };
        }

        private static DataGridView GetOnlyCardGrid(Panel cardsContainer)
        {
            return cardsContainer.Controls
                .OfType<Panel>()
                .Single()
                .Controls
                .OfType<DataGridView>()
                .Single();
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

        private static T GetField<T>(object instance, string fieldName)
            where T : class
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return Assert.IsType<T>(field?.GetValue(instance));
        }
    }
}
