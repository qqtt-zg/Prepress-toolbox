using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntdUI;
using Moq;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Forms.Controls.Settings;
using WindowsFormsApp3.Forms.Main;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Services;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class TemporaryImpositionParametersFormTests
    {
        private static void Click(Control control) => typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(control, new object[] { EventArgs.Empty });
        private static Control Find(Control control, string name) => control.Controls.Find(name, true).Single();
        private static void Set(Control control, string name, string value) => ((Input)Find(control, name)).Text = value;
        private static TemporaryImpositionParameters Parameters() => new TemporaryImpositionParameters
        {
            PaperWidth = 320, PaperHeight = 464, FixedWidth = 320, MinLength = 420,
            MarginTop = 10, MarginBottom = 10, MarginLeft = 10, MarginRight = 10,
            RollMarginTop = 10, RollMarginBottom = 10, RollMarginLeft = 10, RollMarginRight = 10,
            RequestedRows = 4, RequestedColumns = 2, Strategy = ImpositionLayoutStrategy.FixedGrid,
            FlatSheetRotationMode = RollRotationMode.Force0Degree, RollMaterialRotationMode = RollRotationMode.Force270Degree
        };
        private static TemporaryImpositionParametersForm Editor(TemporaryImpositionParameters source = null, bool quantity = false) =>
            new TemporaryImpositionParametersForm(true, source ?? Parameters(), pdfInfo:
                new ImpositionPdfInfo { HasCropBox = true, CropBoxWidth = 90, CropBoxHeight = 80 }, focusLayoutQuantity: quantity);

        [WinFormsFact]
        public async Task MultipleStrategy_ShouldIgnoreManualGrid_AndLeaveSourceUnchanged()
        {
            var source = Parameters();
            using (var form = Editor(source))
            {
                Set(form, "requestedRows", "3"); Set(form, "requestedColumns", "3");
                Click(Find(form, "strategyMultiple"));
                await form.Editor.RefreshPreviewAsync();
                Assert.True(Find(form, "applyParameters").Enabled);
                Assert.Empty(form.Controls.Find("layoutMultipleEnabled", true));
                Click(Find(form, "applyParameters"));
                Assert.Equal(DialogResult.OK, form.DialogResult);
                Assert.Equal(ImpositionLayoutStrategy.Multiple, form.Parameters.Strategy);
                Assert.Equal(0, form.Parameters.RequestedRows);
                Assert.Equal(0, form.Parameters.RequestedColumns);
                Assert.True(form.Parameters.IsLayoutMultipleEnabled);
                Assert.Equal(4, source.RequestedRows);
                Assert.False(source.IsLayoutMultipleEnabled);
            }
        }

        [WinFormsFact]
        public async Task SpacingAndGrid_ShouldApplyExactPair_AndRoundTrip()
        {
            var source = Parameters();
            using (var form = Editor(source))
            {
                Set(form, "spacingX", "3");
                Set(form, "spacingY", "4");
                Set(form, "requestedRows", "2");
                Set(form, "requestedColumns", "3");
                await form.Editor.RefreshPreviewAsync();
                Click(Find(form, "applyParameters"));
                Assert.Equal(DialogResult.OK, form.DialogResult);
                Assert.Equal(3, form.Parameters.SpacingX);
                Assert.Equal(4, form.Parameters.SpacingY);
                Assert.Equal(2, form.Parameters.RequestedRows);
                Assert.Equal(3, form.Parameters.RequestedColumns);
            }
        }

        [WinFormsFact]
        public async Task RowsAndColumnsInputs_ShouldAlwaysBeEnabledAndEditable()
        {
            using (var form = Editor())
            {
                var inputRows = (Input)Find(form, "requestedRows");
                var inputCols = (Input)Find(form, "requestedColumns");
                Assert.True(inputRows.Enabled);
                Assert.True(inputCols.Enabled);

                // 用户输入具体数值 (3行 x 3列，成品 90x80 可在 320x464 上放得下)
                inputRows.Text = "3";
                inputCols.Text = "3";
                await form.Editor.RefreshPreviewAsync();
                Assert.True(inputRows.Enabled);
                Assert.True(inputCols.Enabled);

                Click(Find(form, "applyParameters"));
                Assert.Equal(DialogResult.OK, form.DialogResult);
                Assert.Equal(3, form.Parameters.RequestedRows);
                Assert.Equal(3, form.Parameters.RequestedColumns);
            }
        }

        [WinFormsFact]
        public async Task Automatic_ShouldIgnoreInvalidCachedInputs_AndRestoreTheirText()
        {
            using (var form = Editor())
            {
                Set(form, "requestedRows", "invalid");
                await form.Editor.RefreshPreviewAsync();
                Assert.False(Find(form, "applyParameters").Enabled);
                Click(Find(form, "restoreAutomaticLayout"));
                await form.Editor.RefreshPreviewAsync();
                Assert.True(Find(form, "applyParameters").Enabled);
                Click(Find(form, "applyParameters"));
                Assert.Equal(0, form.Parameters.RequestedRows);
                Assert.False(form.Parameters.IsLayoutMultipleEnabled);
            }
        }

        [WinFormsFact]
        public async Task MaterialSwitch_ShouldKeepDrafts_AndSubmitOnlySelectedMaterial()
        {
            var source = Parameters();
            using (var form = Editor(source))
            {
                Set(form, "materialWidth", "345");
                Click(Find(form, "materialRoll"));
                Set(form, "materialWidth", "330");
                Click(Find(form, "materialFlat"));
                Assert.Equal("345", ((Input)Find(form, "materialWidth")).Text);
                Click(Find(form, "materialRoll"));
                await form.Editor.RefreshPreviewAsync();
                Click(Find(form, "applyParameters"));
                Assert.False(form.IsFlatSheet);
                Assert.Equal(330, form.Parameters.FixedWidth);
                Assert.Equal(320, form.Parameters.PaperWidth);
                Assert.Equal(320, source.FixedWidth);
                Assert.Equal(10, form.Parameters.MarginTop);
            }
        }

        [WinFormsFact]
        public async Task RotationSelectionAndCancel_ShouldRemainIsolated()
        {
            var source = Parameters();
            using (var form = Editor(source))
            {
                Click(Find(form, "rotationPolicy0"));
                await form.Editor.RefreshPreviewAsync();
                Assert.Equal(RollRotationMode.Force0Degree, source.FlatSheetRotationMode);
                Click(Find(form, "applyParameters"));
                Assert.Equal(RollRotationMode.Auto, form.Parameters.FlatSheetRotationMode);
                Assert.Equal(RollRotationMode.Force270Degree, form.Parameters.RollMaterialRotationMode);
            }
            using (var form = Editor(source))
            {
                Set(form, "materialWidth", "350");
                Click(Find(form, "cancelParameters"));
                Assert.Equal(DialogResult.Cancel, form.DialogResult);
                Assert.Equal(320, source.PaperWidth);
                Assert.Equal(320, form.Parameters.PaperWidth);
            }
        }

        [WinFormsFact]
        public async Task NoFile_ShouldRequireDisabledImpositionBeforeApply()
        {
            using (var form = new TemporaryImpositionParametersForm(true, Parameters()))
            {
                await form.Editor.RefreshPreviewAsync();
                Assert.False(Find(form, "applyParameters").Enabled);
                ((Checkbox)Find(form, "impositionEnabled")).Checked = false;
                await form.Editor.RefreshPreviewAsync();
                Assert.True(Find(form, "applyParameters").Enabled);
                Click(Find(form, "applyParameters"));
                Assert.False(form.Parameters.Enabled);
            }
        }

        [WinFormsFact]
        public void SettingsAndTemporaryHosts_ShouldUseSameEditor_AndGlobalDisablesTemporaryModes()
        {
            using (var settings = new SettingsImpositionControl())
            using (var temporary = Editor())
            {
                Assert.Equal(settings.Editor.GetType(), temporary.Editor.GetType());
                Assert.False(Find(settings, "strategyMultiple").Enabled);
                Assert.True(Find(temporary, "strategyMultiple").Enabled);
            }
        }

        private static void AssertInside(Control parent)
        {
            if (parent is ScrollableControl scrollable) Assert.False(scrollable.AutoScroll);
            foreach (Control control in parent.Controls)
            {
                if (!control.Visible) continue;
                Assert.True(control.Left >= 0 && control.Top >= 0 && control.Right <= parent.ClientSize.Width + 1 && control.Bottom <= parent.ClientSize.Height + 1,
                    $"{control.Name}/{control.Text}: {control.Bounds} 超出 {parent.Name} {parent.ClientSize}");
                AssertInside(control);
            }
        }

        [WinFormsFact]
        public async Task NativeEditor_ShouldFitDesktopAndCompactSizes_InAllModes()
        {
            string folder = Environment.GetEnvironmentVariable("PREPRESS_SCHEME_A_SCREENSHOTS");
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            foreach (var size in new[] { new Size(1160, 720), new Size(1040, 620), new Size(941, 529), new Size(800, 520), new Size(880, 445) })
            using (var form = Editor())
            {
                form.MinimumSize = Size.Empty;
                form.ClientSize = size;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-4000, -4000);
                form.Show();
                foreach (var material in new[] { "materialFlat", "materialRoll" })
                foreach (var mode in new[] { "strategyGrid", "strategyMultiple" })
                {
                    Click(Find(form, material)); Click(Find(form, mode));
                    await form.Editor.RefreshPreviewAsync();
                    form.Refresh(); Application.DoEvents(); AssertInside(form.Editor);
                }
                Click(Find(form, "materialFlat")); Click(Find(form, "strategyGrid"));
                Set(form, "requestedRows", "5"); Set(form, "requestedColumns", "3");
                await form.Editor.RefreshPreviewAsync();
                Application.DoEvents(); AssertInside(form.Editor);
                if (!string.IsNullOrEmpty(folder))
                {
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                        bitmap.Save(Path.Combine(folder, $"native-{size.Width}x{size.Height}.png"));
                    }
                }
                form.Close();
            }
        }

        [WinFormsFact]
        public async Task SettingsEditor_ShouldFitActualSettingsTab_WithWarning()
        {
            var presenter = new ImpositionEditorPresenter(new ImpositionService(), true, true, Parameters());
            using (var host = new Form { ClientSize = new Size(941, 529), StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000) })
            using (var editor = new ImpositionParametersEditor(presenter) { Dock = DockStyle.Fill })
            {
                host.Controls.Add(editor); host.Show();
                await editor.RefreshPreviewAsync();
                Application.DoEvents(); AssertInside(editor);
                Assert.True(Find(editor, "applyParameters").Enabled);
                Assert.Contains("大版规格就绪", Find(editor, "layoutValidationStatus").Text);
                string folder = Environment.GetEnvironmentVariable("PREPRESS_SCHEME_A_SCREENSHOTS");
                if (!string.IsNullOrEmpty(folder))
                using (var bitmap = new Bitmap(host.ClientSize.Width, host.ClientSize.Height))
                {
                    host.DrawToBitmap(bitmap, new Rectangle(0, 0, host.ClientSize.Width, host.ClientSize.Height));
                    bitmap.Save(Path.Combine(folder, "settings-941x529-native.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
                host.Close();
            }
        }

        [WinFormsFact]
        public async Task OlderPreviewCompletingLast_ShouldNotReplaceLatestResult()
        {
            var pending = new TaskCompletionSource<System.Collections.Generic.List<ImpositionResult>>();
            var latest = new System.Collections.Generic.List<ImpositionResult> { new ImpositionResult
                { Success = true, IsPrintable = true, Rows = 4, Columns = 2, OptimalLayoutQuantity = 8, CellWidth = 90, CellHeight = 80 } };
            var service = new Mock<IImpositionService>();
            service.SetupSequence(s => s.GetLayoutChoicesAsync(It.IsAny<object>(), It.IsAny<ImpositionPdfInfo>(), It.IsAny<int>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(pending.Task).ReturnsAsync(latest);
            using (var form = new TemporaryImpositionParametersForm(true, Parameters(), pdfInfo:
                new ImpositionPdfInfo { HasCropBox = true, CropBoxWidth = 90, CropBoxHeight = 80 }, impositionService: service.Object))
            {
                var first = form.Editor.RefreshPreviewAsync();
                Set(form, "materialWidth", "350");
                pending.SetResult(new System.Collections.Generic.List<ImpositionResult> { new ImpositionResult
                    { Success = true, IsPrintable = true, Rows = 1, Columns = 1, OptimalLayoutQuantity = 1 } });
                await first;
                Assert.Contains("8 个", Find(form, "layoutResultSummary").Text);
                Assert.True(Find(form, "applyParameters").Enabled);
            }
        }

        [WinFormsFact]
        public async Task LongRoll_ShouldRenderDetailAndPanWithoutCreatingPerPageControls()
        {
            var p = Parameters(); p.RollMaterialRotationMode = RollRotationMode.Force0Degree;
            using (var form = Editor(p))
            {
                form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-4000, -4000); form.Show();
                Click(Find(form, "materialRoll"));
                Set(form, "requestedRows", "2000"); Set(form, "requestedColumns", "2");
                await form.Editor.RefreshPreviewAsync();
                Assert.Contains("4000 个", Find(form, "layoutResultSummary").Text);
                Click(Find(form, "previewView3")); Click(Find(form, "previewView1"));
                ((Slider)Find(form, "previewPosition")).Value = 100;
                ((Slider)Find(form, "previewPositionX")).Value = 100;
                Application.DoEvents(); AssertInside(form.Editor);
                using (var bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                    string folder = Environment.GetEnvironmentVariable("PREPRESS_SCHEME_A_SCREENSHOTS");
                    if (!string.IsNullOrEmpty(folder)) bitmap.Save(Path.Combine(folder, "long-roll-detail.png"));
                }
                Assert.True(Find(form, "impositionPreviewCanvas").Controls.Count == 0);
                form.Close();
            }
        }
    }
}
