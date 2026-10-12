using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Services;
using Xunit;

namespace WindowsFormsApp3.Tests.Services
{
    public class TemporaryLayoutTests
    {
        private static ImpositionPdfInfo Page() => new ImpositionPdfInfo
        {
            HasCropBox = true, CropBoxWidth = 90, CropBoxHeight = 80
        };

        private static TemporaryImpositionParameters Parameters() => new TemporaryImpositionParameters
        {
            PaperWidth = 320, PaperHeight = 464,
            MarginTop = 10, MarginBottom = 10, MarginLeft = 10, MarginRight = 10,
            FixedWidth = 320, MinLength = 350,
            RollMarginTop = 10, RollMarginBottom = 10, RollMarginLeft = 10, RollMarginRight = 10,
            FlatSheetRotationMode = RollRotationMode.Force0Degree,
            RollMaterialRotationMode = RollRotationMode.Force0Degree
        };

        [Theory]
        [InlineData(CopyMode.AutoByColumn, 5, 2)]
        [InlineData(CopyMode.AutoByRow, 4, 3)]
        public async Task Multiple_ShouldAdjustOnlyAutomaticAxis(CopyMode direction, int rows, int columns)
        {
            var p = Parameters();
            p.IsLayoutMultipleEnabled = true;
            p.LayoutMultiple = 2;
            p.LayoutConstraintDirection = direction;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), true, Page())
                .PreviewAsync(p, CancellationToken.None));
            Assert.True(result.Success);
            Assert.Equal(rows, result.Rows);
            Assert.Equal(columns, result.Columns);
            Assert.Equal(rows * columns, result.OptimalLayoutQuantity);
        }

        [Fact]
        public async Task ManualColumns_ConflictingWithMultiple_ShouldFailWithoutRounding()
        {
            var p = Parameters();
            p.RequestedRows = 3;
            p.RequestedColumns = 3;
            p.IsLayoutMultipleEnabled = true;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), true, Page())
                .PreviewAsync(p, CancellationToken.None));
            Assert.False(result.Success);
            Assert.Contains("手动列数 3", result.ErrorMessage);
            Assert.Equal(3, p.RequestedColumns);
        }

        [Fact]
        public async Task MultipleLargerThanCapacity_ShouldFailWithoutForcingExtraCells()
        {
            var p = Parameters();
            p.IsLayoutMultipleEnabled = true;
            p.LayoutMultiple = 4;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), true, Page())
                .PreviewAsync(p, CancellationToken.None));
            Assert.False(result.Success);
            Assert.Equal(0, result.OptimalLayoutQuantity);
        }

        [Fact]
        public async Task Quantity_ShouldOfferFeasibleFactorPairs_AndNotMutateParameters()
        {
            var p = Parameters();
            p.RequestedLayoutQuantity = 6;
            var results = await new TemporaryImpositionParametersPresenter(new ImpositionService(), true, Page())
                .PreviewAsync(p, CancellationToken.None);
            Assert.Contains(results, r => r.Rows == 2 && r.Columns == 3);
            Assert.Contains(results, r => r.Rows == 3 && r.Columns == 2);
            Assert.All(results, r => { Assert.True(r.Success); Assert.Equal(6, r.Rows * r.Columns); });
            Assert.Equal(0, p.RequestedRows);
            Assert.Equal(0, p.RequestedColumns);
        }

        [Fact]
        public async Task Quantity_WithColumnMultiple_ShouldFilterConflictingPairs()
        {
            var p = Parameters();
            p.RequestedLayoutQuantity = 6;
            p.IsLayoutMultipleEnabled = true;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), true, Page())
                .PreviewAsync(p, CancellationToken.None));
            Assert.True(result.Success);
            Assert.Equal(3, result.Rows);
            Assert.Equal(2, result.Columns);
        }

        [Fact]
        public async Task UnavailableQuantity_ShouldFailInsteadOfChangingRequestedQuantity()
        {
            var p = Parameters();
            p.RequestedLayoutQuantity = 17;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), true, Page())
                .PreviewAsync(p, CancellationToken.None));
            Assert.False(result.Success);
            Assert.Contains("布局数 17", result.ErrorMessage);
        }

        [Fact]
        public async Task RollRowMultiple_ShouldRoundUpToMeetMinimumLength()
        {
            var p = Parameters();
            p.IsLayoutMultipleEnabled = true;
            p.LayoutMultiple = 3;
            p.LayoutConstraintDirection = CopyMode.AutoByRow;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), false, Page())
                .PreviewAsync(p, CancellationToken.None));
            Assert.True(result.Success);
            Assert.Equal(6, result.Rows);
            Assert.True(result.ActualMaterialLength >= p.MinLength);
        }

        [Fact]
        public async Task RollExactManualRows_ShouldKeepRowsAndLeaveBlankBelowMinimumLength()
        {
            var p = Parameters();
            p.RequestedRows = 2;
            p.RequestedColumns = 2;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), false, Page())
                .PreviewAsync(p, CancellationToken.None));
            Assert.True(result.Success);
            Assert.Equal(2, result.Rows);
            Assert.Equal(4, result.OptimalLayoutQuantity);
            Assert.Equal(p.MinLength, result.ActualMaterialLength);
            var geometry = ImpositionEditorPresenter.Geometry(p, false, result);
            Assert.Equal(95, geometry.Top);
            Assert.Equal(95, geometry.Bottom);
        }

        [Fact]
        public async Task LongRollAndLargeQuantity_ShouldHaveNoLegacyEightRowOrNinetySixPageLimit()
        {
            var p = Parameters();
            p.RequestedRows = 2000;
            p.RequestedColumns = 2;
            var presenter = new TemporaryImpositionParametersPresenter(new ImpositionService(), false, Page());
            var grid = Assert.Single(await presenter.PreviewAsync(p, CancellationToken.None));
            Assert.True(grid.Success);
            Assert.Equal(160020, grid.ActualMaterialLength);
            p.RequestedLayoutQuantity = 997;
            var quantity = Assert.Single(await presenter.PreviewAsync(p, CancellationToken.None));
            Assert.True(quantity.Success);
            Assert.Equal(997, quantity.OptimalLayoutQuantity);
        }

        [Theory]
        [InlineData(ImpositionLayoutStrategy.Automatic, 5, 3)]
        [InlineData(ImpositionLayoutStrategy.Multiple, 5, 2)]
        public async Task ExplicitStrategy_ShouldIgnoreCachedFixedGridAndQuantity(ImpositionLayoutStrategy strategy, int rows, int columns)
        {
            var p = Parameters();
            p.Strategy = strategy;
            p.RequestedRows = 3;
            p.RequestedColumns = 3;
            p.RequestedLayoutQuantity = 17;
            p.IsLayoutMultipleEnabled = true;
            var result = Assert.Single(await new TemporaryImpositionParametersPresenter(new ImpositionService(), true, Page()).PreviewAsync(p, CancellationToken.None));
            Assert.True(result.Success);
            Assert.Equal(rows, result.Rows);
            Assert.Equal(columns, result.Columns);
            Assert.Equal(17, p.RequestedLayoutQuantity);
        }

        [Fact]
        public async Task PageCopySettings_ShouldHaveNoEffectOnStandardLayout()
        {
            var service = new ImpositionService();
            var config = (FlatSheetConfiguration)TemporaryImpositionParametersPresenter.CreateConfiguration(Parameters(), true);
            config.CopyCount = 3;
            config.CopyMode = CopyMode.AutoByRow;
            var result = await service.CalculateFlatSheetLayoutAsync(config, Page());
            Assert.Equal(5, result.Rows);
            Assert.Equal(3, result.Columns);
            Assert.Equal(15, result.OptimalLayoutQuantity);
        }

        [Fact]
        public async Task AutomaticLayout_ShouldFailWhenNoPageFits()
        {
            var p = Parameters();
            p.PaperWidth = 50;
            p.PaperHeight = 60;
            p.FixedWidth = 50;
            var service = new ImpositionService();
            Assert.False((await new TemporaryImpositionParametersPresenter(service, true, Page())
                .PreviewAsync(p, CancellationToken.None)).Single().Success);
            Assert.False((await new TemporaryImpositionParametersPresenter(service, false, Page())
                .PreviewAsync(p, CancellationToken.None)).Single().Success);
        }

        [Fact]
        public void PersistentClone_ShouldDropLayoutOverrides_AndKeepDimensionsAndRotation()
        {
            var p = Parameters();
            p.RequestedRows = 3;
            p.RequestedColumns = 2;
            p.RequestedLayoutQuantity = 6;
            p.IsLayoutMultipleEnabled = true;
            var saved = p.CloneWithoutLayoutOverrides();
            Assert.Equal(0, saved.RequestedRows);
            Assert.Equal(0, saved.RequestedColumns);
            Assert.Equal(0, saved.RequestedLayoutQuantity);
            Assert.False(saved.IsLayoutMultipleEnabled);
            Assert.Equal(p.PaperWidth, saved.PaperWidth);
            Assert.Equal(p.FlatSheetRotationMode, saved.FlatSheetRotationMode);
            Assert.Equal(3, p.RequestedRows);
            Assert.True(p.IsLayoutMultipleEnabled);
        }
    }
}
