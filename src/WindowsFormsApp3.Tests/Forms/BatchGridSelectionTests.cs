using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class BatchGridSelectionTests
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void ShouldLoadSelectedBatchPreview_ShouldRequireExpandedPreview(bool isPreviewExpanded, bool expected)
        {
            Assert.Equal(expected, MaterialSelectFormModern.ShouldLoadSelectedBatchPreview(isPreviewExpanded));
        }

        [Fact]
        public void SelectBatchGridRow_InCellSelectMode_ShouldSelectEveryCellInRow()
        {
            using var grid = CreateGrid();

            MaterialSelectFormModern.SelectBatchGridRow(grid, 1, false);

            Assert.All(grid.Rows[1].Cells.Cast<DataGridViewCell>(), cell => Assert.True(cell.Selected));
            Assert.DoesNotContain(grid.Rows[0].Cells.Cast<DataGridViewCell>(), cell => cell.Selected);
            Assert.Equal(1, grid.CurrentCell.RowIndex);
        }

        [Fact]
        public void SyncBatchGridSelectionState_ShouldPersistSelectedRowsInItems()
        {
            var items = new BindingList<BatchFileItem>(new List<BatchFileItem>
            {
                new BatchFileItem { FileName = "A.pdf" },
                new BatchFileItem { FileName = "B.pdf" }
            });
            using var grid = CreateGrid(items);
            grid.Rows[1].Cells[0].Selected = true;

            MaterialSelectFormModern.SyncBatchGridSelectionState(grid, items);

            Assert.False(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
        }

        [Fact]
        public void RestoreBatchGridSelection_ShouldRestoreSelectionAfterGridRebuild()
        {
            var items = new BindingList<BatchFileItem>(new List<BatchFileItem>
            {
                new BatchFileItem { FileName = "A.pdf" },
                new BatchFileItem { FileName = "B.pdf", IsSelected = true }
            });
            using var grid = CreateGrid(items);

            MaterialSelectFormModern.RestoreBatchGridSelection(grid, items);

            Assert.All(grid.Rows[1].Cells.Cast<DataGridViewCell>(), cell => Assert.True(cell.Selected));
        }

        private static DataGridView CreateGrid(BindingList<BatchFileItem> items = null)
        {
            var grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.CellSelect
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BatchFileItem.FileName) });
            grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BatchFileItem.Quantity) });
            grid.BindingContext = new BindingContext();
            grid.DataSource = items ?? new BindingList<BatchFileItem>(new List<BatchFileItem>
            {
                new BatchFileItem { FileName = "A.pdf" },
                new BatchFileItem { FileName = "B.pdf" }
            });
            _ = grid.Handle;
            grid.ClearSelection();
            return grid;
        }
    }
}
