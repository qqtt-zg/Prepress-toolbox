using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using Xunit;

namespace WindowsFormsApp3.Tests.Services
{
    public class BatchFileItemSortServiceTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Sort_ShouldKeepUnmatchedRowsBeforeMatchedRows_InBothDirections(bool ascending)
        {
            var items = new List<BatchFileItem>
            {
                new BatchFileItem { FileName = "A.pdf", Quantity = "10", IsExcelMatched = true },
                new BatchFileItem { FileName = "Z.pdf", Quantity = "30", IsExcelMatched = false },
                new BatchFileItem { FileName = "B.pdf", Quantity = "20", IsExcelMatched = true },
                new BatchFileItem { FileName = "Y.pdf", Quantity = "40", IsExcelMatched = false }
            };

            List<BatchFileItem> result = BatchFileItemSortService.Sort(items, nameof(BatchFileItem.Quantity), ascending);

            Assert.Equal(new[] { false, false, true, true }, result.Select(item => item.IsExcelMatched));
            Assert.Equal(
                ascending ? new[] { "Z.pdf", "Y.pdf" } : new[] { "Y.pdf", "Z.pdf" },
                result.Take(2).Select(item => item.FileName));
        }

        [Fact]
        public void Sort_ShouldSupportEveryVisibleColumn()
        {
            var items = new List<BatchFileItem>
            {
                new BatchFileItem { Index = 2, SerialNumber = "02", FileName = "B.pdf", OrderNumber = "B", Quantity = "20", PageCount = 2, Dimensions = "20×20", LayoutInfo = "2版" },
                new BatchFileItem { Index = 1, SerialNumber = "01", FileName = "A.pdf", OrderNumber = "A", Quantity = "10", PageCount = 1, Dimensions = "10×10", LayoutInfo = "1版" }
            };
            string[] properties =
            {
                nameof(BatchFileItem.SerialNumber), nameof(BatchFileItem.FileName), nameof(BatchFileItem.OrderNumber),
                nameof(BatchFileItem.Quantity), nameof(BatchFileItem.PageCount), nameof(BatchFileItem.Dimensions),
                nameof(BatchFileItem.LayoutInfo)
            };

            foreach (string property in properties)
            {
                List<BatchFileItem> result = BatchFileItemSortService.Sort(items, property, true);
                Assert.Equal("A.pdf", result[0].FileName);
            }
        }

        [Fact]
        public void RemoveByPaths_ShouldRemoveItemsFromBatchAndGroupsAndDropEmptyGroups()
        {
            var keep = new BatchFileItem { FilePath = @"C:\monitor\keep.pdf" };
            var remove = new BatchFileItem { FilePath = @"C:\monitor\remove.pdf" };
            var batchItems = new BindingList<BatchFileItem>(new List<BatchFileItem> { keep, remove });
            var groups = new List<BatchProcessGroup>
            {
                new BatchProcessGroup { GroupId = "keep", Items = new List<BatchFileItem> { keep } },
                new BatchProcessGroup { GroupId = "remove", Items = new List<BatchFileItem> { remove } }
            };

            BatchFileItemCollectionService.RemoveByPaths(batchItems, groups, new[] { remove.FilePath });

            Assert.Single(batchItems);
            Assert.Same(keep, batchItems[0]);
            Assert.Single(groups);
            Assert.Equal("keep", groups[0].GroupId);
        }

        [Fact]
        public void MoveByOffset_ShouldMoveSelectedContiguousRowsAsOneBlock()
        {
            var items = new BindingList<BatchFileItem>(new List<BatchFileItem>
            {
                new BatchFileItem { FilePath = @"C:\monitor\A.pdf" },
                new BatchFileItem { FilePath = @"C:\monitor\B.pdf" },
                new BatchFileItem { FilePath = @"C:\monitor\C.pdf" },
                new BatchFileItem { FilePath = @"C:\monitor\D.pdf" }
            });

            bool moved = BatchFileItemCollectionService.MoveByOffset(
                items,
                new[] { @"C:\monitor\B.pdf", @"C:\monitor\C.pdf" },
                -1);

            Assert.True(moved);
            Assert.Equal(new[] { "B.pdf", "C.pdf", "A.pdf", "D.pdf" }, items.Select(item => System.IO.Path.GetFileName(item.FilePath)));
        }
    }
}
