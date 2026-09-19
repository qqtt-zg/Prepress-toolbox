using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WindowsFormsApp3.Models;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class MaterialSelectionSchemeATests
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string pathName);

        public MaterialSelectionSchemeATests()
        {
            if (!WindowsFormsApp3.Utils.AppSettings.IsInitialized)
            {
                WindowsFormsApp3.Utils.AppSettings.Initialize(new WindowsFormsApp3.Services.FileLogger(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MaterialSelectionSchemeATests")));
            }
        }

        [Fact]
        public void PageCountColumn_ShouldBindToRealPageCount_AndBeReadOnly()
        {
            using var column = MaterialSelectFormModern.CreatePageCountColumn(45);

            Assert.Equal(nameof(BatchFileItem.PageCount), column.DataPropertyName);
            Assert.Equal("页数", column.HeaderText);
            Assert.Equal(45, column.Width);
            Assert.True(column.ReadOnly);
        }

        [Fact]
        public void ResolveActualPageCount_ShouldReadEveryPageFromThePdf()
        {
            var pdfPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"material-page-count-{Guid.NewGuid():N}.pdf");

            try
            {
                SetDllDirectory(System.IO.Path.Combine(AppContext.BaseDirectory, "x64"));
                using (var writer = new iText.Kernel.Pdf.PdfWriter(pdfPath))
                using (var document = new iText.Kernel.Pdf.PdfDocument(writer))
                {
                    document.AddNewPage();
                    document.AddNewPage();
                    document.AddNewPage();
                }

                Assert.Equal(3, MaterialSelectFormModern.ResolveActualPageCount(pdfPath));
            }
            finally
            {
                if (System.IO.File.Exists(pdfPath))
                {
                    System.IO.File.Delete(pdfPath);
                }
            }
        }

        [Fact]
        public void ExtractOrderNumberByRegex_ShouldMatchFullFileName_WithExtension_ConsistentWithMainShell()
        {
            string fileName = "203X203.pdf";
            string pattern = @"(.+).pdf";

            string result = MaterialSelectFormModern.ExtractOrderNumberByRegex(fileName, pattern);

            Assert.Equal("203X203", result);
        }

        [Fact]
        public void ExtractOrderNumberByRegex_ShouldExtractCorrectly_WithNamedGroup()
        {
            string fileName = "PO123456_Label_54x84-1000pcs.pdf";
            string pattern = @"(?<order>PO\d+)";

            string result = MaterialSelectFormModern.ExtractOrderNumberByRegex(fileName, pattern);

            Assert.Equal("PO123456", result);
        }

        [Fact]
        public void ExtractOrderNumberByRegex_ShouldExtractCorrectly_WithStandardGroup()
        {
            string fileName = "CustomJob_ORDER-2026-08_500pcs.pdf";
            string pattern = @"(ORDER-\d{4}-\d{2})";

            string result = MaterialSelectFormModern.ExtractOrderNumberByRegex(fileName, pattern);

            Assert.Equal("ORDER-2026-08", result);
        }

        [Fact]
        public void ExtractOrderNumberByRegex_ShouldExtractCorrectly_WithFullMatch()
        {
            string fileName = "Batch_987654321_Glossy.pdf";
            string pattern = @"\d{6,}";

            string result = MaterialSelectFormModern.ExtractOrderNumberByRegex(fileName, pattern);

            Assert.Equal("987654321", result);
        }

        [Theory]
        [InlineData("PO001", 0, "PO001")]
        [InlineData("PO001", 1, "PO002")]
        [InlineData("PO001", 9, "PO010")]
        [InlineData("PO099", 1, "PO100")]
        [InlineData("ORDER_0005", 3, "ORDER_0008")]
        [InlineData("20260901-001", 5, "20260901-006")]
        public void CalculateIncrementalOrderNumber_ShouldPreserveLeadingZeros(string baseOrder, int offset, string expected)
        {
            string result = MaterialSelectFormModern.CalculateIncrementalOrderNumber(baseOrder, offset);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void BatchFileSorting_AscendingAndDescending_ShouldWorkProperly()
        {
            var files = new List<string>
            {
                @"C:\test\Z_File_500pcs.pdf",
                @"C:\test\A_File_100pcs.pdf",
                @"C:\test\M_File_200pcs.pdf"
            };

            var items = files.Select((f, idx) => new BatchFileItem
            {
                Index = idx + 1,
                FilePath = f,
                FileName = System.IO.Path.GetFileName(f)
            }).ToList();

            // A-Z 升序
            var ascList = items.OrderBy(x => x.FileName, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal("A_File_100pcs.pdf", ascList[0].FileName);
            Assert.Equal("M_File_200pcs.pdf", ascList[1].FileName);
            Assert.Equal("Z_File_500pcs.pdf", ascList[2].FileName);

            // Z-A 降序
            var descList = items.OrderByDescending(x => x.FileName, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal("Z_File_500pcs.pdf", descList[0].FileName);
            Assert.Equal("M_File_200pcs.pdf", descList[1].FileName);
            Assert.Equal("A_File_100pcs.pdf", descList[2].FileName);
        }

        [Fact]
        public void BatchQuantityParsing_FromClipboard_ShouldExtractNumbers()
        {
            string clipboardText = "1000\r\n2000 pcs\r\n数量: 500\r\n800";
            var lines = clipboardText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var quantities = lines.Select(l =>
            {
                var match = Regex.Match(l.Trim(), @"\d+");
                return match.Success ? match.Value : l.Trim();
            }).ToList();

            Assert.Equal(4, quantities.Count);
            Assert.Equal("1000", quantities[0]);
            Assert.Equal("2000", quantities[1]);
            Assert.Equal("500", quantities[2]);
            Assert.Equal("800", quantities[3]);
        }

        [Fact]
        public void MaterialSelectionResult_ShouldContainSchemeAProperties()
        {
            var result = new MaterialSelectionResult
            {
                IsApplyToAll = true,
                OrderNumberMode = OrderNumberMode.RegexExtraction,
                SelectedOrderRegexName = "订单号_PO数字",
                SelectedOrderRegexPattern = @"PO\d+",
                BatchItems = new List<BatchFileItem>
                {
                    new BatchFileItem { Index = 1, FileName = "PO1001_54x84-100pcs.pdf", OrderNumber = "PO1001", Quantity = "100" },
                    new BatchFileItem { Index = 2, FileName = "PO1002_54x84-200pcs.pdf", OrderNumber = "PO1002", Quantity = "200" }
                }
            };

            Assert.True(result.IsApplyToAll);
            Assert.Equal(OrderNumberMode.RegexExtraction, result.OrderNumberMode);
            Assert.Equal("订单号_PO数字", result.SelectedOrderRegexName);
            Assert.Equal(2, result.BatchItems.Count);
            Assert.Equal("PO1001", result.BatchItems[0].OrderNumber);
            Assert.Equal("100", result.BatchItems[0].Quantity);
        }

        [Fact]
        public void AppendPendingFiles_ShouldDynamicallyAddFilesAndAvoidDuplicates()
        {
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\FirstFile_54x84-100pcs.pdf",
                regexResult: "FirstFile",
                opacity: 1.0,
                width: "54",
                height: "84",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "1"))
            {
                var handle = form.Handle;

                // 初始设置待处理文件列表
                form.SetPendingFiles(new[] { @"C:\test\FirstFile_54x84-100pcs.pdf" });
                Assert.Single(form.BatchFileItems);

                // 动态追加新进入监控目录的文件
                form.AppendPendingFile(@"C:\test\SecondFile_54x84-200pcs.pdf");
                form.AppendPendingFile(@"C:\test\ThirdFile_54x84-500pcs.pdf");

                Assert.Equal(3, form.BatchFileItems.Count);
                Assert.Equal("SecondFile_54x84-200pcs.pdf", form.BatchFileItems[1].FileName);
                // 仅保留 Excel 匹配，未匹配时统一默认赋值 1
                Assert.Equal("1", form.BatchFileItems[1].Quantity);
                Assert.Equal("2", form.BatchFileItems[1].SerialNumber);

                Assert.Equal("ThirdFile_54x84-500pcs.pdf", form.BatchFileItems[2].FileName);
                Assert.Equal("1", form.BatchFileItems[2].Quantity);
                Assert.Equal("3", form.BatchFileItems[2].SerialNumber);

                // 尝试重复追加已存在的文件，应自动去重
                form.AppendPendingFile(@"C:\test\SecondFile_54x84-200pcs.pdf");
                Assert.Equal(3, form.BatchFileItems.Count);
            }
        }

        [Fact]
        public void AppendPendingFiles_ShouldPreserveLockedGroupAndUseUnlockedLinkedGroup()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\Locked.pdf", @"C:\test\Linked.pdf" });
                var items = form.BatchFileItems;
                var lockedItem = items[0];
                var linkedItem = items[1];
                var lockedGroup = CreateGroup("locked", true, lockedItem);
                var linkedGroup = CreateGroup("linked", false, linkedItem);
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(lockedGroup);
                form.ProcessGroups.Add(linkedGroup);

                form.AppendPendingFile(@"C:\test\New&MT-PET.pdf");

                Assert.True(lockedGroup.IsLocked);
                Assert.Single(lockedGroup.Items);
                Assert.Same(lockedItem, lockedGroup.Items[0]);
                Assert.Equal("locked", lockedItem.GroupId);
                Assert.Equal(2, linkedGroup.Items.Count);
                Assert.Contains(linkedGroup.Items, item => item.FileName == "New&MT-PET.pdf");
                Assert.All(linkedGroup.Items, item => Assert.Equal("linked", item.GroupId));
            }
        }

        [Fact]
        public void AppendPendingFiles_ShouldCreateUnlockedFallbackWhenNoLinkedGroupExists()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\Locked.pdf" });
                var lockedItem = form.BatchFileItems[0];
                var lockedGroup = CreateGroup("locked", true, lockedItem);
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(lockedGroup);

                form.AppendPendingFile(@"C:\test\New.pdf");

                Assert.Equal(2, form.ProcessGroups.Count);
                Assert.Same(lockedGroup, form.ProcessGroups[0]);
                Assert.Single(lockedGroup.Items);
                var fallback = form.ProcessGroups[1];
                Assert.False(fallback.IsLocked);
                Assert.False(fallback.IsPreserveGroup);
                Assert.Single(fallback.Items);
                Assert.Equal("New.pdf", fallback.Items[0].FileName);
                Assert.Equal(fallback.GroupId, fallback.Items[0].GroupId);
                Assert.Equal(form.SelectedMaterial ?? "未指派材料", fallback.Material);
                Assert.Equal(form.FixedField ?? "", fallback.Process);
                Assert.Equal(string.IsNullOrEmpty(form.ColorMode) ? "彩色" : form.ColorMode, fallback.ColorMode);
                Assert.Equal(form.FilmType ?? "", fallback.FilmType);
                Assert.Equal(form.SelectedShape.ToString(), fallback.Shape);
                Assert.Equal(form.RoundRadius.ToString(), fallback.RoundRadius);
                Assert.Equal(form.SelectedExportPath ?? "", fallback.ExportPath);
            }
        }

        [Fact]
        public void AppendPendingFiles_ShouldUseFirstEligibleGroupInDisplayOrder()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\A.pdf", @"C:\test\B.pdf" });
                var items = form.BatchFileItems;
                var first = CreateGroup("first", false, items[0]);
                var second = CreateGroup("second", false, items[1]);
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(first);
                form.ProcessGroups.Add(second);

                form.AppendPendingFile(@"C:\test\New.pdf");

                Assert.Equal(2, first.Items.Count);
                Assert.Single(second.Items);
                Assert.Contains(first.Items, item => item.FileName == "New.pdf");
            }
        }

        [Fact]
        public void TryMoveFilesBetweenGroups_ShouldMoveOnlyBetweenUnlockedGroups()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\A.pdf", @"C:\test\B.pdf" });
                var items = form.BatchFileItems;
                var source = CreateGroup("source", false, items[0]);
                var target = CreateGroup("target", false, items[1]);
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(source);
                form.ProcessGroups.Add(target);

                bool moved = form.TryMoveFilesBetweenGroups("source", "target", new[] { items[0].FilePath });

                Assert.True(moved);
                Assert.DoesNotContain(source, form.ProcessGroups);
                Assert.Equal(2, target.Items.Count);
                Assert.Equal("target", items[0].GroupId);
                Assert.Equal(target.Material, items[0].Material);
            }
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void TryMoveFilesBetweenGroups_ShouldRejectLockedEndpointsAtomically(
            bool sourceLocked,
            bool targetLocked)
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\A.pdf", @"C:\test\B.pdf" });
                var items = form.BatchFileItems;
                var source = CreateGroup("source", sourceLocked, items[0]);
                var target = CreateGroup("target", targetLocked, items[1]);
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(source);
                form.ProcessGroups.Add(target);

                bool moved = form.TryMoveFilesBetweenGroups("source", "target", new[] { items[0].FilePath });

                Assert.False(moved);
                Assert.Single(source.Items);
                Assert.Single(target.Items);
                Assert.Equal("source", items[0].GroupId);
                Assert.Equal("target", items[1].GroupId);
            }
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void TryMoveFilesBetweenGroups_ShouldRejectPreserveEndpoints(
            bool sourcePreserve,
            bool targetPreserve)
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\A.pdf", @"C:\test\B.pdf" });
                var items = form.BatchFileItems;
                var source = CreateGroup("source", false, items[0]);
                var target = CreateGroup("target", false, items[1]);
                source.IsPreserveGroup = sourcePreserve;
                target.IsPreserveGroup = targetPreserve;
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(source);
                form.ProcessGroups.Add(target);

                bool moved = form.TryMoveFilesBetweenGroups("source", "target", new[] { items[0].FilePath });

                Assert.False(moved);
                Assert.Single(source.Items);
                Assert.Single(target.Items);
            }
        }

        [Fact]
        public void TryMoveFilesBetweenGroups_ShouldRejectDuplicateOwnershipWithoutPartialMutation()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\A.pdf", @"C:\test\B.pdf" });
                var items = form.BatchFileItems;
                var source = CreateGroup("source", false, items[0]);
                var target = CreateGroup("target", false, items[1]);
                var duplicateOwner = CreateGroup("duplicate", false, items[0]);
                items[0].GroupId = "source";
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(source);
                form.ProcessGroups.Add(target);
                form.ProcessGroups.Add(duplicateOwner);

                bool moved = form.TryMoveFilesBetweenGroups("source", "target", new[] { items[0].FilePath });

                Assert.False(moved);
                Assert.Single(source.Items);
                Assert.Single(target.Items);
                Assert.Single(duplicateOwner.Items);
                Assert.Equal("source", items[0].GroupId);
            }
        }

        [Fact]
        public void SortBatchFilesByFileName_ShouldPreserveGroupsAndLocks()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\Z.pdf", @"C:\test\A.pdf", @"C:\test\M.pdf" });
                var items = form.BatchFileItems;
                var lockedGroup = CreateGroup("locked", true, items[0], items[1]);
                var linkedGroup = CreateGroup("linked", false, items[2]);
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(lockedGroup);
                form.ProcessGroups.Add(linkedGroup);

                form.SortBatchFilesByFileName(true);

                Assert.Same(lockedGroup, form.ProcessGroups[0]);
                Assert.Same(linkedGroup, form.ProcessGroups[1]);
                Assert.True(lockedGroup.IsLocked);
                Assert.Equal(new[] { "A.pdf", "Z.pdf" }, lockedGroup.Items.Select(item => item.FileName));
                Assert.Equal(new[] { "A.pdf", "M.pdf", "Z.pdf" }, form.BatchFileItems.Select(item => item.FileName));
                Assert.Equal(new[] { 1, 2, 3 }, form.BatchFileItems.Select(item => item.Index));
            }
        }

        [Fact]
        public void QuantityAndDimensionSorting_ShouldPreserveGroupIdentityAndOrderingProjection()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\A.pdf", @"C:\test\B.pdf", @"C:\test\C.pdf" });
                var items = form.BatchFileItems;
                items[0].Quantity = "30";
                items[1].Quantity = "10";
                items[2].Quantity = "20";
                items[0].Dimensions = "30×30";
                items[1].Dimensions = "10×10";
                items[2].Dimensions = "20×20";
                var lockedGroup = CreateGroup("locked", true, items[0], items[1]);
                var linkedGroup = CreateGroup("linked", false, items[2]);
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(lockedGroup);
                form.ProcessGroups.Add(linkedGroup);

                form.SortBatchFilesByQuantity(true);
                Assert.Equal(new[] { "B.pdf", "A.pdf" }, lockedGroup.Items.Select(item => item.FileName));
                Assert.Equal(new[] { "B.pdf", "C.pdf", "A.pdf" }, form.BatchFileItems.Select(item => item.FileName));

                form.SortBatchFilesByDimension(false);
                Assert.Equal(new[] { "A.pdf", "B.pdf" }, lockedGroup.Items.Select(item => item.FileName));
                Assert.Equal(new[] { "A.pdf", "C.pdf", "B.pdf" }, form.BatchFileItems.Select(item => item.FileName));
                Assert.Same(lockedGroup, form.ProcessGroups[0]);
                Assert.Same(linkedGroup, form.ProcessGroups[1]);
                Assert.True(lockedGroup.IsLocked);
            }
        }

        [Fact]
        public void ToggleBatchFileListPanel_ShouldNotRebuildExistingGroups()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\A.pdf" });
                var item = form.BatchFileItems[0];
                var lockedGroup = CreateGroup("locked", true, item);
                form.ProcessGroups.Clear();
                form.ProcessGroups.Add(lockedGroup);

                form.ToggleBatchFileListPanel(true);
                form.ToggleBatchFileListPanel(false);
                form.ToggleBatchFileListPanel(true);

                Assert.Single(form.ProcessGroups);
                Assert.Same(lockedGroup, form.ProcessGroups[0]);
                Assert.True(lockedGroup.IsLocked);
                Assert.Same(item, lockedGroup.Items[0]);
            }
        }

        [Fact]
        public void PresetContextMenu_ShouldBeSuppressedForDynamicLeftPanelDescendant()
        {
            using (var form = CreateGroupTestForm())
            {
                var handle = form.Handle;
                form.SetPendingFiles(new[] { @"C:\test\A.pdf" });
                form.ToggleBatchFileListPanel(true);
                var leftPanel = form.Controls.Find("pnlFileList", true).Single();
                var dynamicContainer = leftPanel.Controls.Find("pnlCardsContainer", true).Single();

                Assert.True(form.IsPresetContextMenuSuppressed(dynamicContainer, false));
                Assert.True(form.IsPresetContextMenuSuppressed(dynamicContainer, true));
                Assert.False(form.IsPresetContextMenuSuppressed(form, false));
                Assert.True(MaterialSelectFormModern.ShouldSuppressPresetContextMenu(false, true, true));
                Assert.False(MaterialSelectFormModern.ShouldSuppressPresetContextMenu(false, true, false));
            }
        }

        [Fact]
        public void ShouldSuppressPresetContextMenu_ShouldHandleKeyboardFocusPath()
        {
            Assert.True(MaterialSelectFormModern.ShouldSuppressPresetContextMenu(false, true, true));
            Assert.False(MaterialSelectFormModern.ShouldSuppressPresetContextMenu(false, true, false));
            Assert.True(MaterialSelectFormModern.ShouldSuppressPresetContextMenu(true, false, false));
        }

        private static MaterialSelectFormModern CreateGroupTestForm()
        {
            return new MaterialSelectFormModern(
                materials: new List<string> { "PET" },
                fileName: @"C:\test\Initial.pdf",
                regexResult: "Initial",
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

        private static BatchProcessGroup CreateGroup(
            string groupId,
            bool isLocked,
            params BatchFileItem[] items)
        {
            var group = new BatchProcessGroup
            {
                GroupId = groupId,
                GroupName = groupId,
                IsLocked = isLocked,
                Material = groupId + "-material",
                Process = groupId + "-process",
                Items = items.ToList()
            };
            foreach (var item in items)
            {
                item.GroupId = group.GroupId;
                item.GroupName = group.GroupName;
                item.IsLocked = isLocked;
                item.Material = group.Material;
                item.Process = group.Process;
            }
            return group;
        }

        [Fact]
        public void MoveBatchItem_ShouldReorderItems_AndUpdateIndexAndOrderNumbers()
        {
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET" },
                fileName: @"C:\test\File1.pdf",
                regexResult: "File1",
                opacity: 1.0,
                width: "54",
                height: "84",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "1"))
            {
                var handle = form.Handle;

                form.SetPendingFiles(new[] {
                    @"C:\test\FileA_100pcs.pdf",
                    @"C:\test\FileB_200pcs.pdf",
                    @"C:\test\FileC_300pcs.pdf"
                });

                Assert.Equal(3, form.BatchFileItems.Count);
                Assert.Equal("FileA_100pcs.pdf", form.BatchFileItems[0].FileName);
                Assert.Equal("FileB_200pcs.pdf", form.BatchFileItems[1].FileName);
                Assert.Equal("FileC_300pcs.pdf", form.BatchFileItems[2].FileName);

                // 模拟拖拽：将第 0 项移动到第 2 项（移到末尾）
                form.MoveBatchItem(0, 2);

                Assert.Equal("FileB_200pcs.pdf", form.BatchFileItems[0].FileName);
                Assert.Equal(1, form.BatchFileItems[0].Index);
                Assert.Equal("FileC_300pcs.pdf", form.BatchFileItems[1].FileName);
                Assert.Equal(2, form.BatchFileItems[1].Index);
                Assert.Equal("FileA_100pcs.pdf", form.BatchFileItems[2].FileName);
                Assert.Equal(3, form.BatchFileItems[2].Index);
            }
        }
    
        [Fact]
        public void SetPendingFiles_WhenQuantityUnmatched_ShouldDefaultToOne_NotContaminatedByQuantityTextBox()
        {
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET" },
                fileName: @"C:\test\FirstFile_54x84-500pcs.pdf",
                regexResult: "FirstFile",
                opacity: 1.0,
                width: "54",
                height: "84",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "1"))
            {
                var handle = form.Handle;

                // 此时右侧输入框即使有值
                var qtyBox = form.Controls.Find("quantityTextBox", true).FirstOrDefault() as AntdUI.Input;
                if (qtyBox != null)
                {
                    qtyBox.Text = "500";
                }

                // 设置批处理文件列表：仅保留Excel匹配，所有未匹配文件统一独立默认1，绝不被右侧数量框污染
                form.SetPendingFiles(new[] {
                    @"C:\test\File1_200pcs.pdf",
                    @"C:\test\16 HYS000263 50张 YT-047 EF-081 YG-04609铭牌.pdf",
                    @"C:\test\File3_PureName.pdf"
                });

                Assert.Equal(3, form.BatchFileItems.Count);
                Assert.Equal("1", form.BatchFileItems[0].Quantity);
                Assert.Equal("1", form.BatchFileItems[1].Quantity);
                Assert.Equal("1", form.BatchFileItems[2].Quantity);
            }
        }

        [Fact]
        public void SetPendingFiles_WithExcelData_ShouldAutoFillMatchedQuantity_AndDefaultToOneForUnmatched()
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("ModelName", typeof(string));
            dt.Columns.Add("Qty", typeof(string));
            dt.Columns.Add("Serial", typeof(string));
            dt.Rows.Add("ItemA", "888", "A-008");
            dt.Rows.Add("ItemB", "999", "B-009");

            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET" },
                fileName: @"C:\test\Demo.pdf",
                regexResult: "Demo",
                opacity: 1.0,
                width: "54",
                height: "84",
                excelData: dt,
                searchColumnIndex: 0,
                returnColumnIndex: 1,
                serialColumnIndex: 2,
                newColumnIndex: -1,
                serialNumber: "1"))
            {
                var handle = form.Handle;

                form.SetPendingFiles(new[] {
                    @"C:\test\Job_ItemA_Label.pdf",
                    @"C:\test\Job_ItemB_Label.pdf",
                    @"C:\test\Job_ItemC_NotFound.pdf"
                });

                Assert.Equal(3, form.BatchFileItems.Count);
                // ItemA 命中 Excel 第一行 -> 888
                Assert.Equal("888", form.BatchFileItems[0].Quantity);
                Assert.Equal("A-008", form.BatchFileItems[0].SerialNumber);
                // ItemB 命中 Excel 第二行 -> 999
                Assert.Equal("999", form.BatchFileItems[1].Quantity);
                Assert.Equal("B-009", form.BatchFileItems[1].SerialNumber);
                // ItemC 未在 Excel 命中 -> 独立默认 1
                Assert.Equal("1", form.BatchFileItems[2].Quantity);
                Assert.Equal("3", form.BatchFileItems[2].SerialNumber);
            }
        }

        [Fact]
        public void ResolveAllExcelMatchData_WhenOneFileMatchesMultipleRows_ShouldReturnEachQuantityInExcelOrder()
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("ModelName", typeof(string));
            dt.Columns.Add("Qty", typeof(string));
            dt.Columns.Add("Serial", typeof(string));
            dt.Rows.Add("ItemA", "100", "A-001");
            dt.Rows.Add("ItemA", "200", "A-002");

            var results = MaterialSelectFormModern.ResolveAllExcelMatchData(
                dt, 0, 1, 2, "Job_ItemA_Label.pdf");

            Assert.Equal(2, results.Count);
            Assert.Equal(new[] { "100", "200" }, results.Select(item => item.Quantity));
            Assert.Equal(new[] { "A-001", "A-002" }, results.Select(item => item.SerialNumber));
            Assert.Equal(new[] { 0, 1 }, results.Select(item => item.RowIndex));
        }

        [Fact]
        public void ResolveExcelMatchData_ShouldReturnSerialNumberFromMatchedRow()
        {
            var dt = new System.Data.DataTable();
            dt.Columns.Add("ModelName", typeof(string));
            dt.Columns.Add("Qty", typeof(string));
            dt.Columns.Add("Serial", typeof(string));
            dt.Rows.Add("ItemA", "888", " A-008 ");

            ExcelMatchData result = MaterialSelectFormModern.ResolveExcelMatchData(dt, 0, 1, 2, "Job_ItemA_Label.pdf");

            Assert.True(result.HasMatch);
            Assert.Equal("888", result.Quantity);
            Assert.Equal("A-008", result.SerialNumber);
        }


        [Theory]
        [InlineData("16 HYS000263 50张 YT-047 EF-081 YG-04609铭牌.pdf", "张", "50")]
        [InlineData("19 HYS000164 80张 YT-047 EF-101  YG-04610铭牌.pdf", "张", "80")]
        [InlineData("Order_Item_120个_Mat.pdf", "个", "120")]
        [InlineData("Product_500F_Finish.pdf", "F", "500")]
        public void RegexExtractQuantityByUnit_ShouldCorrectlyExtractNumber(string fileName, string unit, string expected)
        {
            string pattern = $@"(?<=^|[^\d])(\d+)\s*{Regex.Escape(unit)}";
            var match = Regex.Match(fileName, pattern, RegexOptions.IgnoreCase);

            Assert.True(match.Success);
            Assert.Equal(expected, match.Groups[1].Value);
        }

        [Fact]
        public void BatchQuantityIncrement_ShouldAccumulateOnCurrentQuantity()
        {
            var items = new List<BatchFileItem>
            {
                new BatchFileItem { Quantity = "50" },
                new BatchFileItem { Quantity = "80" },
                new BatchFileItem { Quantity = "1" }
            };

            int delta = 10;
            foreach (var item in items)
            {
                int.TryParse(item.Quantity, out int cur);
                item.Quantity = Math.Max(1, cur + delta).ToString();
            }

            Assert.Equal("60", items[0].Quantity);
            Assert.Equal("90", items[1].Quantity);
            Assert.Equal("11", items[2].Quantity);
        }

}
}
