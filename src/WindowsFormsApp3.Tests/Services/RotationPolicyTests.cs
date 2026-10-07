using System.Threading.Tasks;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Xunit;

namespace WindowsFormsApp3.Tests.Services
{
    public class RotationPolicyTests
    {
        private static ImpositionPdfInfo Page(float width = 80, float height = 40)
        {
            return new ImpositionPdfInfo { FirstPageSize = new PageSize { Width = width, Height = height }, PageCount = 1 };
        }

        private static FlatSheetConfiguration Flat(RollRotationMode mode)
        {
            return new FlatSheetConfiguration
            {
                PaperWidth = 280, PaperHeight = 100,
                MarginTop = 0, MarginBottom = 0, MarginLeft = 0, MarginRight = 0,
                RotationMode = mode
            };
        }

        private static RollMaterialConfiguration Roll(RollRotationMode mode)
        {
            return new RollMaterialConfiguration
            {
                FixedWidth = 200, MinLength = 100,
                MarginTop = 0, MarginBottom = 0, MarginLeft = 0, MarginRight = 0,
                RotationMode = mode
            };
        }

        [Theory]
        [InlineData(RollRotationMode.Auto, 270)]
        [InlineData(RollRotationMode.Force0Degree, 0)]
        [InlineData(RollRotationMode.Force270Degree, 270)]
        public async Task FlatPolicy_ShouldControlStandardAndRepeatedLayouts(RollRotationMode mode, int angle)
        {
            var service = new ImpositionService();
            var normal = await service.CalculateFlatSheetLayoutAsync(Flat(mode), Page());
            var repeated = await service.CalculateOptimalEvenColumnsLayoutAsync(Flat(mode), Page(), 2, CopyMode.FixedNoRotationByColumn);
            Assert.True(normal.Success, normal.ErrorMessage);
            Assert.True(repeated.Success, repeated.ErrorMessage);
            Assert.Equal(angle, normal.RotationAngle);
            Assert.Equal(angle, repeated.RotationAngle);
        }

        [Theory]
        [InlineData(RollRotationMode.Auto, 270)]
        [InlineData(RollRotationMode.Force0Degree, 0)]
        [InlineData(RollRotationMode.Force270Degree, 270)]
        public async Task RollPolicy_ShouldControlStandardAndRepeatedLayouts(RollRotationMode mode, int angle)
        {
            var service = new ImpositionService();
            var normal = await service.CalculateRollMaterialLayoutAsync(Roll(mode), Page(), default, RollRotationMode.Force270Degree);
            var repeated = await service.CalculateOptimalEvenColumnsLayoutAsync(Roll(mode), Page(), 2, CopyMode.FixedNoRotationByColumn);
            Assert.True(normal.Success, normal.ErrorMessage);
            Assert.True(repeated.Success, repeated.ErrorMessage);
            Assert.Equal(angle, normal.RotationAngle);
            Assert.Equal(angle, repeated.RotationAngle);
        }

        [Fact]
        public async Task FixedDirection_ShouldNotFallBackWhenItCannotFit()
        {
            var service = new ImpositionService();
            var flat = Flat(RollRotationMode.Force270Degree);
            flat.PaperWidth = 100;
            flat.PaperHeight = 60;
            Assert.False((await service.CalculateFlatSheetLayoutAsync(flat, Page())).Success);
            Assert.False((await service.CalculateOptimalEvenColumnsLayoutAsync(flat, Page(), 2)).Success);
            var roll = Roll(RollRotationMode.Force270Degree);
            roll.FixedWidth = 60;
            Assert.False((await service.CalculateRollMaterialLayoutAsync(roll, Page(40, 80))).Success);
            Assert.False((await service.CalculateOptimalEvenColumnsLayoutAsync(roll, Page(40, 80), 2)).Success);
        }

        [Fact]
        public async Task Fixed270_ShouldApplyToSquareAndManualGrid()
        {
            var flat = Flat(RollRotationMode.Force270Degree);
            flat.Rows = 1;
            flat.Columns = 1;
            var result = await new ImpositionService().CalculateFlatSheetLayoutAsync(flat, Page(40, 40));
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(270, result.RotationAngle);
            var roll = Roll(RollRotationMode.Force270Degree);
            roll.Rows = 1;
            roll.Columns = 1;
            var repeatedRoll = await new ImpositionService().CalculateOptimalEvenColumnsLayoutAsync(
                roll, Page(40, 40), 2, CopyMode.FixedNoRotationByColumn);
            Assert.True(repeatedRoll.Success, repeatedRoll.ErrorMessage);
            Assert.Equal(270, repeatedRoll.RotationAngle);
        }

        [Fact]
        public void Migration_ShouldRetainLegacyRestriction_AndPreserveExplicitPolicy()
        {
            var old = new TemporaryImpositionParameters { IsForceRotationEnabled = true };
            old.MigrateRotationModes(CopyMode.FixedNoRotationByRow, 270);
            Assert.Equal(RollRotationMode.Force0Degree, old.FlatSheetRotationMode);
            Assert.Equal(RollRotationMode.Force270Degree, old.RollMaterialRotationMode);
            old.FlatSheetRotationMode = RollRotationMode.Auto;
            old.MigrateRotationModes(CopyMode.FixedNoRotationByColumn);
            Assert.Equal(RollRotationMode.Auto, old.FlatSheetRotationMode);
        }
    }
}
