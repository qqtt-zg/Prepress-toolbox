using System;
using System.Linq;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Services
{
    /// <summary>保存单项页面框基准，供列表、排版与最终 PDF 处理共用。</summary>
    public static class BatchPageBoxSelectionService
    {
        public static PageBoxIssue GetChoiceIssue(PdfInspectorInfo inspection)
        {
            return inspection?.Issues?.FirstOrDefault(issue => issue.Type == IssueType.OutOfBounds &&
                string.Equals(issue.BoxType, "CropBox", StringComparison.OrdinalIgnoreCase))
                ?? inspection?.Issues?.FirstOrDefault(issue => issue.Type == IssueType.DifferentPageBoxes &&
                    string.Equals(issue.BoxType, "CropBox", StringComparison.OrdinalIgnoreCase));
        }

        public static void ApplyInspection(BatchFileItem item, PdfInspectorInfo inspection)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            var issue = GetChoiceIssue(inspection);
            var page = issue == null ? null : inspection.AllPageBoxes
                .FirstOrDefault(candidate => candidate.PageNumber == issue.PageNumber);
            item.PageBoxChoicePage = page?.MediaBox != null && page.CropBox != null ? page : null;
            item.PageBoxIssueType = item.PageBoxChoicePage == null ? (IssueType?)null : issue.Type;
            item.PageBoxReferencePage = item.PageBoxChoicePage?.PageNumber ?? 1;
            if (item.PageBoxChoicePage == null)
            {
                item.MediaBoxFinalDimensions = null;
                item.CropBoxFinalDimensions = null;
                item.PageBoxSource = PdfPageBoxSource.Automatic;
                return;
            }

            // 重扫时保留该项已选基准；首次发现超界采用媒体框，其余差异采用裁剪框。
            var source = item.PageBoxSource == PdfPageBoxSource.MediaBox || item.PageBoxSource == PdfPageBoxSource.CropBox
                ? item.PageBoxSource.Value
                : issue.Type == IssueType.OutOfBounds ? PdfPageBoxSource.MediaBox : PdfPageBoxSource.CropBox;
            SelectSource(item, source);
        }

        public static void SelectSource(BatchFileItem item, PdfPageBoxSource source)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (source != PdfPageBoxSource.MediaBox && source != PdfPageBoxSource.CropBox)
                throw new ArgumentOutOfRangeException(nameof(source));
            var page = item.PageBoxChoicePage ?? throw new InvalidOperationException("该文件没有可选择的页面框");
            var box = source == PdfPageBoxSource.MediaBox ? page.MediaBox : page.CropBox;
            if (box == null || !box.IsDefined || box.Width <= 0 || box.Height <= 0)
                throw new InvalidOperationException("所选页面框尺寸无效");

            item.PageBoxSource = source;
            item.PageBoxReferencePage = page.PageNumber;
            double width = box.Width * 25.4 / 72;
            double height = box.Height * 25.4 / 72;
            // 原始尺寸同时考虑页面旋转，保持与排版服务的有效尺寸一致。
            if (page.Rotation == 90 || page.Rotation == 270)
            {
                double temporary = width;
                width = height;
                height = temporary;
            }
            item.RawPdfWidth = width;
            item.RawPdfHeight = height;
        }

        public static PdfPageBoxSource ResolveSource(BatchFileItem item, MaterialSelectionResult selection)
        {
            // 已检查的普通文件明确使用自动模式，不能继承另一文件的窗口选择。
            return item?.PageBoxSource ?? selection?.PageBoxSource ?? PdfPageBoxSource.Automatic;
        }

        public static void UpdateDimensions(BatchFileItem item, IDimensionCalculationService dimensionService,
            double bleed, string cornerRadius, bool addPdfLayers, bool swapWidthHeight)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (dimensionService == null) throw new ArgumentNullException(nameof(dimensionService));

            string Calculate(double width, double height)
            {
                if (swapWidthHeight && width < height)
                {
                    double temporary = width;
                    width = height;
                    height = temporary;
                }
                return dimensionService.CalculateFinalDimensions(width, height, bleed, cornerRadius, addPdfLayers);
            }

            if (item.PageBoxChoicePage != null)
            {
                string CalculateBox(BoxDimension box)
                {
                    double width = box.Width * 25.4 / 72;
                    double height = box.Height * 25.4 / 72;
                    return item.PageBoxChoicePage.Rotation == 90 || item.PageBoxChoicePage.Rotation == 270
                        ? Calculate(height, width) : Calculate(width, height);
                }
                // 两个选项都从原始框重新计算，避免换基准或反复改出血时累计扣减。
                item.MediaBoxFinalDimensions = CalculateBox(item.PageBoxChoicePage.MediaBox);
                item.CropBoxFinalDimensions = CalculateBox(item.PageBoxChoicePage.CropBox);
                item.Dimensions = item.PageBoxSource == PdfPageBoxSource.MediaBox
                    ? item.MediaBoxFinalDimensions : item.CropBoxFinalDimensions;
                return;
            }
            item.Dimensions = Calculate(item.RawPdfWidth, item.RawPdfHeight);
        }

        public static int ResolveReferencePage(BatchFileItem item, MaterialSelectionResult selection)
        {
            return item?.PageBoxSource.HasValue == true ? item.PageBoxReferencePage : selection?.PageBoxReferencePage ?? 1;
        }
    }
}
