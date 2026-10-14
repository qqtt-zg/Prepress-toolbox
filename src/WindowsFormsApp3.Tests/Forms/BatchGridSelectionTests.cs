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

        [Fact]
        public void SelectAllAndInvertSelection_ShouldToggleCellSelectionStates()
        {
            var items = new BindingList<BatchFileItem>(new List<BatchFileItem>
            {
                new BatchFileItem { FileName = "A.pdf", Quantity = "100" },
                new BatchFileItem { FileName = "B.pdf", Quantity = "200" }
            });
            using var grid = CreateGrid(items);
            grid.SelectAll();
            MaterialSelectFormModern.SyncBatchGridSelectionState(grid, items);
            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);

            for (int i = 0; i < grid.Rows.Count; i++)
            {
                var row = grid.Rows[i];
                bool anySelected = row.Cells.Cast<DataGridViewCell>().Any(c => c.Selected);
                foreach (DataGridViewCell cell in row.Cells)
                {
                    cell.Selected = !anySelected;
                }
            }
            MaterialSelectFormModern.SyncBatchGridSelectionState(grid, items);
            Assert.False(items[0].IsSelected);
            Assert.False(items[1].IsSelected);
        }

        [Fact]
        public void CellDoubleClick_OnEditableColumn_ShouldSupportBeginEdit()
        {
            var items = new BindingList<BatchFileItem>(new List<BatchFileItem>
            {
                new BatchFileItem { FileName = "A.pdf", Quantity = "100" },
                new BatchFileItem { FileName = "B.pdf", Quantity = "200" }
            });
            using var form = new Form();
            using var grid = CreateGrid(items);
            form.Controls.Add(grid);
            _ = form.Handle;
            grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            grid.CurrentCell = grid.Rows[1].Cells[1]; // Quantity 列
            Assert.False(grid.IsCurrentCellInEditMode);

            grid.BeginEdit(true);
            Assert.True(grid.IsCurrentCellInEditMode);
            grid.EndEdit();
            Assert.False(grid.IsCurrentCellInEditMode);
        }

        [Fact]
        public void ShiftRangeSelection_ShouldSelectAllRowsInRange()
        {
            var items = new BindingList<BatchFileItem>(new List<BatchFileItem>
            {
                new BatchFileItem { FileName = "A.pdf", Quantity = "100" },
                new BatchFileItem { FileName = "B.pdf", Quantity = "200" },
                new BatchFileItem { FileName = "C.pdf", Quantity = "300" },
                new BatchFileItem { FileName = "D.pdf", Quantity = "400" }
            });
            using var grid = CreateGrid(items);
            int start = 0;
            int end = 2;
            grid.ClearSelection();
            for (int r = start; r <= end; r++)
            {
                foreach (DataGridViewCell c in grid.Rows[r].Cells)
                {
                    c.Selected = true;
                }
            }
            MaterialSelectFormModern.SyncBatchGridSelectionState(grid, items);
            Assert.True(items[0].IsSelected);
            Assert.True(items[1].IsSelected);
            Assert.True(items[2].IsSelected);
            Assert.False(items[3].IsSelected);
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
