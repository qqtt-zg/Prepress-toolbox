using System.Threading;
using System.Threading.Tasks;
using Moq;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Services;
using Xunit;

namespace WindowsFormsApp3.Tests.Services
{
    public class ImpositionEditorPresenterTests
    {
        private class MemorySettings : ImpositionSettingsService
        {
            public TemporaryImpositionParameters Saved;
            public override TemporaryImpositionParameters Load() => Parameters();
            public override void Save(TemporaryImpositionParameters parameters) => Saved = parameters.Clone();
        }

        private static TemporaryImpositionParameters Parameters() => new TemporaryImpositionParameters
        {
            PaperWidth = 320, PaperHeight = 464, FixedWidth = 320, MinLength = 420,
            MarginLeft = 10, MarginRight = 30, MarginTop = 15, MarginBottom = 35,
            RollMarginLeft = 10, RollMarginRight = 30, RollMarginTop = 15, RollMarginBottom = 35,
            FlatSheetRotationMode = RollRotationMode.Force0Degree, RollMaterialRotationMode = RollRotationMode.Force0Degree
        };

        [Fact]
        public void TemporarySubmit_ShouldCommitOnlyCurrentMaterial_WithoutSavingDefaults()
        {
            var original = Parameters();
            var settings = new MemorySettings();
            var p = new ImpositionEditorPresenter(new ImpositionService(), false, true, original, settings: settings);
            p.Flat.Width = "350";
            p.Roll.Width = "invalid";
            p.Enabled = false;
            Assert.True(p.TrySubmit(null, out var value, out _));
            Assert.Equal(350, value.PaperWidth);
            Assert.Equal(320, value.FixedWidth);
            Assert.Equal(320, original.PaperWidth);
            Assert.Null(settings.Saved);
            p.Cancel();
            Assert.Equal("320", p.Flat.Width);
            Assert.Equal("320", p.Roll.Width);
        }

        [Fact]
        public void InactiveInvalidInputs_ShouldNotBlockAutomaticOrMultiple()
        {
            var p = new ImpositionEditorPresenter(new ImpositionService(), false, false, Parameters(), settings: new MemorySettings());
            p.Roll.Rows = p.Roll.Columns = p.Roll.Quantity = p.Roll.Multiple = "invalid";
            p.Roll.Strategy = ImpositionLayoutStrategy.Automatic;
            Assert.True(p.TryGetParameters(false, out var automatic, out _));
            Assert.Equal(0, automatic.RequestedRows);
            Assert.False(automatic.IsLayoutMultipleEnabled);
            p.Roll.Strategy = ImpositionLayoutStrategy.Multiple;
            p.Roll.Multiple = "3";
            Assert.True(p.TryGetParameters(false, out var multiple, out _));
            Assert.Equal(0, multiple.RequestedLayoutQuantity);
            Assert.Equal(3, multiple.LayoutMultiple);
            p.Roll.Strategy = ImpositionLayoutStrategy.FixedGrid;
            p.Roll.AutoRows = p.Roll.AutoColumns = false;
            Assert.False(p.TryGetParameters(false, out _, out _));
            Assert.Equal("invalid", p.Roll.Rows);
        }

        [Fact]
        public async Task Defaults_ShouldSaveDespiteUnusableSample_AndClearFixedRows()
        {
            var initial = Parameters(); initial.RequestedRows = 3; initial.RequestedColumns = 2;
            var settings = new MemorySettings();
            var p = new ImpositionEditorPresenter(new ImpositionService(), true, true, initial, settings: settings);
            p.Flat.Strategy = ImpositionLayoutStrategy.Automatic;
            p.SyncCommonLayout();
            p.SetReference("2000", "3000");
            Assert.True(p.TryGetParameters(true, out var draft, out _));
            var result = Assert.Single(await p.PreviewAsync(draft, CancellationToken.None));
            Assert.False(result.Success);
            Assert.True(p.TrySubmit(result, out _, out _));
            Assert.Equal(0, settings.Saved.RequestedRows);
            Assert.Equal(0, settings.Saved.RequestedColumns);
            Assert.Equal(320, settings.Saved.PaperWidth);
            Assert.Equal(3, initial.RequestedRows);
        }

        [Fact]
        public void Defaults_ShouldRejectInvalidOtherMaterial_AndTemporaryOnlyStrategies()
        {
            var settings = new MemorySettings();
            var p = new ImpositionEditorPresenter(new ImpositionService(), true, true, null, settings: settings);
            p.Roll.Width = "NaN";
            Assert.False(p.TrySubmit(null, out _, out var error));
            Assert.Contains("卷装", error);
            Assert.Null(settings.Saved);
            p.Roll.Width = "320";
            p.Flat.Strategy = ImpositionLayoutStrategy.Multiple;
            Assert.False(p.TrySubmit(null, out _, out _));
        }

        [Fact]
        public async Task AsymmetricMargins_ShouldCenterInsidePrintableRegion()
        {
            var source = Parameters(); source.RequestedRows = 3; source.RequestedColumns = 2;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), false,
                new ImpositionPdfInfo { HasCropBox = true, CropBoxWidth = 90, CropBoxHeight = 80 }).PreviewAsync(source, CancellationToken.None));
            var g = ImpositionEditorPresenter.Geometry(source, false, result);
            Assert.Equal(60, g.Left);
            Assert.Equal(80, g.Right);
            Assert.Equal(80, g.Top);
            Assert.Equal(100, g.Bottom);
        }

        [Fact]
        public void TemporaryWithoutFile_ShouldRequireDisableBeforeSubmit()
        {
            var p = new ImpositionEditorPresenter(new ImpositionService(), false, true, Parameters(), settings: new MemorySettings());
            Assert.False(p.TrySubmit(null, out _, out _));
            p.Enabled = false;
            Assert.True(p.TrySubmit(null, out _, out _));
        }

        [Fact]
        public void CommittingOneMaterial_ShouldPreserveOtherMaterialsLayoutAcrossReopen()
        {
            var original = Parameters(); original.RequestedRows = 4; original.RequestedColumns = 2;
            var p = new ImpositionEditorPresenter(new ImpositionService(), false, true, original, settings: new MemorySettings());
            p.Enabled = false;
            p.Flat.Strategy = ImpositionLayoutStrategy.Multiple;
            p.Flat.Multiple = "3";
            Assert.True(p.TrySubmit(null, out var committed, out _));
            Assert.Equal(ImpositionLayoutStrategy.Multiple, committed.EffectiveLayout(true).Strategy);
            Assert.Equal(0, committed.EffectiveLayout(true).RequestedRows);
            Assert.Equal(4, committed.EffectiveLayout(false).RequestedRows);
            Assert.Equal(2, committed.EffectiveLayout(false).RequestedColumns);
            var reopened = new ImpositionEditorPresenter(new ImpositionService(), false, false, committed, settings: new MemorySettings());
            Assert.Equal(ImpositionLayoutStrategy.FixedGrid, reopened.Roll.Strategy);
            Assert.Equal("4", reopened.Roll.Rows);
            Assert.Equal("3", reopened.Flat.Multiple);
            committed.Clone().FlatLayout.Multiple = 9;
            Assert.Equal(3, committed.FlatLayout.Multiple);
        }

        [Fact]
        public async Task PaperPdfAndReferencePdf_ShouldUpdateDifferentParameters()
        {
            var service = new Mock<IImpositionService>();
            service.Setup(s => s.AnalyzePdfFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ImpositionPdfInfo { HasCropBox = true, CropBoxWidth = 200, CropBoxHeight = 300 });
            var p = new ImpositionEditorPresenter(service.Object, true, true, Parameters(), settings: new MemorySettings());
            Assert.Null(await p.ReadPdfAsync("paper.pdf", true, CancellationToken.None));
            Assert.Equal("200", p.Flat.Width);
            Assert.Equal(90, p.PdfInfo.GetEffectivePageWidth());
            Assert.Null(await p.ReadPdfAsync("reference.pdf", false, CancellationToken.None));
            Assert.Equal(200, p.PdfInfo.GetEffectivePageWidth());
            Assert.Equal("200", p.Flat.Width);
            Assert.False(p.SetReference("invalid", "80"));
            Assert.Equal("invalid", p.ReferenceWidth);
            Assert.True(p.TrySubmit(null, out _, out _));
        }

        [Fact]
        public async Task PdfReadFailure_ShouldPreserveMaterialAndReference()
        {
            var service = new Mock<IImpositionService>();
            service.Setup(s => s.AnalyzePdfFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.IO.IOException("test"));
            var p = new ImpositionEditorPresenter(service.Object, true, true, Parameters(), settings: new MemorySettings());
            Assert.NotNull(await p.ReadPdfAsync("bad.pdf", true, CancellationToken.None));
            Assert.Equal("320", p.Flat.Width);
            Assert.NotNull(await p.ReadPdfAsync("bad.pdf", false, CancellationToken.None));
            Assert.Equal(90, p.PdfInfo.GetEffectivePageWidth());
        }
    }
}
