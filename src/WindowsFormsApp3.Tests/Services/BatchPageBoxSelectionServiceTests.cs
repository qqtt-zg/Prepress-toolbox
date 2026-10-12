using System;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using Xunit;

namespace WindowsFormsApp3.Tests.Services
{
    public class BatchPageBoxSelectionServiceTests
    {
        [Theory]
        [InlineData(IssueType.DifferentPageBoxes, PdfPageBoxSource.CropBox)]
        [InlineData(IssueType.OutOfBounds, PdfPageBoxSource.MediaBox)]
        public void Inspection_UsesAffectedPageAndPreservesChoiceOnRefresh(IssueType issueType, PdfPageBoxSource expected)
        {
            var inspection = CreateInspection(issueType);
            var item = new BatchFileItem();
            BatchPageBoxSelectionService.ApplyInspection(item, inspection);
            Assert.Equal(expected, item.PageBoxSource);
            Assert.Equal(2, item.PageBoxReferencePage);

            var opposite = expected == PdfPageBoxSource.MediaBox ? PdfPageBoxSource.CropBox : PdfPageBoxSource.MediaBox;
            BatchPageBoxSelectionService.SelectSource(item, opposite);
            BatchPageBoxSelectionService.ApplyInspection(item, inspection);
            Assert.Equal(opposite, item.PageBoxSource);
            Assert.Equal(2, item.PageBoxReferencePage);
        }

        [Fact]
        public void SelectingOneFile_DoesNotChangeOtherFilesOrInheritWindowChoice()
        {
            var first = new BatchFileItem();
            var second = new BatchFileItem();
            var normal = new BatchFileItem();
            BatchPageBoxSelectionService.ApplyInspection(first, CreateInspection(IssueType.DifferentPageBoxes));
            BatchPageBoxSelectionService.ApplyInspection(second, CreateInspection(IssueType.DifferentPageBoxes));
            BatchPageBoxSelectionService.ApplyInspection(normal, new PdfInspectorInfo());
            BatchPageBoxSelectionService.SelectSource(first, PdfPageBoxSource.MediaBox);

            var window = new MaterialSelectionResult { PageBoxSource = PdfPageBoxSource.MediaBox, PageBoxReferencePage = 9 };
            Assert.Equal(PdfPageBoxSource.MediaBox, BatchPageBoxSelectionService.ResolveSource(first, window));
            Assert.Equal(PdfPageBoxSource.CropBox, BatchPageBoxSelectionService.ResolveSource(second, window));
            Assert.Equal(PdfPageBoxSource.Automatic, BatchPageBoxSelectionService.ResolveSource(normal, window));
            Assert.Equal(1, BatchPageBoxSelectionService.ResolveReferencePage(normal, window));
            Assert.Equal(2, BatchPageBoxSelectionService.ResolveReferencePage(first, window));
            Assert.Equal(PdfPageBoxSource.MediaBox, BatchPageBoxSelectionService.ResolveSource(new BatchFileItem(), window));
        }

        [Fact]
        public void SelectedDimensions_AccountForPageRotation()
        {
            var inspection = CreateInspection(IssueType.DifferentPageBoxes);
            inspection.AllPageBoxes[0].Rotation = 90;
            var item = new BatchFileItem();
            BatchPageBoxSelectionService.ApplyInspection(item, inspection);
            Assert.Equal(100 * 25.4 / 72, item.RawPdfWidth, 8);
            Assert.Equal(200 * 25.4 / 72, item.RawPdfHeight, 8);
            BatchPageBoxSelectionService.SelectSource(item, PdfPageBoxSource.MediaBox);
            Assert.Equal(140 * 25.4 / 72, item.RawPdfWidth, 8);
            Assert.Equal(280 * 25.4 / 72, item.RawPdfHeight, 8);
        }

        internal static PdfInspectorInfo CreateInspection(IssueType type)
        {
            var info = new PdfInspectorInfo();
            info.AllPageBoxes.Add(new PageBoxInfo
            {
                PageNumber = 2,
                MediaBox = new BoxDimension { IsDefined = true, Left = 0, Bottom = 0, Right = 280, Top = 140 },
                CropBox = new BoxDimension { IsDefined = true, Left = 10, Bottom = 10, Right = 210, Top = 110 }
            });
            info.Issues.Add(new PageBoxIssue { PageNumber = 2, Type = type, BoxType = "CropBox" });
            return info;
        }
    }
}
