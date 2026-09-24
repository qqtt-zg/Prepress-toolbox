using System.Drawing;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class MaterialSelectFormSizeContractTests
    {
        [Fact]
        public void SizeConstants_MustMatch_V5WorkbenchContract()
        {
            Assert.Equal(520, MaterialSelectFormModern.BATCH_PANEL_WIDTH);
            Assert.Equal(360, MaterialSelectFormModern.STATION_WIDTH);
            Assert.Equal(880, MaterialSelectFormModern.WORKBENCH_WIDTH);
            Assert.Equal(508, MaterialSelectFormModern.BASE_CLIENT_HEIGHT);
            Assert.Equal(245, MaterialSelectFormModern.MAX_PREVIEW_HEIGHT);
            Assert.Equal(753, MaterialSelectFormModern.EXPANDED_CLIENT_HEIGHT);
            Assert.Equal(16, MaterialSelectFormModern.COLLAPSE_BAR_HEIGHT);
            Assert.Equal(26, MaterialSelectFormModern.TITLE_BAR_HEIGHT);
        }

        [Theory]
        [InlineData(false, false, 360, 508)] // 状态1：单工位折叠
        [InlineData(true, false, 880, 508)]  // 状态2：双联体常态折叠
        [InlineData(false, true, 360, 753)]  // 状态3：单工位展开预览
        [InlineData(true, true, 880, 753)]   // 状态4：双联体展开预览
        public void FourStandardStates_ShouldHavePreciseClientSize(bool isBatchExpanded, bool isPreviewExpanded, int expectedWidth, int expectedHeight)
        {
            int width = isBatchExpanded ? MaterialSelectFormModern.WORKBENCH_WIDTH : MaterialSelectFormModern.STATION_WIDTH;
            int height = isPreviewExpanded ? MaterialSelectFormModern.EXPANDED_CLIENT_HEIGHT : MaterialSelectFormModern.BASE_CLIENT_HEIGHT;

            Assert.Equal(expectedWidth, width);
            Assert.Equal(expectedHeight, height);
            Assert.Equal(expectedHeight - MaterialSelectFormModern.BASE_CLIENT_HEIGHT, isPreviewExpanded ? 245 : 0);
        }
    }
}
