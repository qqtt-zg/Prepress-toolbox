using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Presenters
{
    /// <summary>临时排版草稿预览；配置映射、布局计算和异常处理不放在窗口内。</summary>
    public class TemporaryImpositionParametersPresenter
    {
        private readonly IImpositionService _service;
        private readonly bool _isFlatSheet;
        private readonly ImpositionPdfInfo _pdfInfo;

        public TemporaryImpositionParametersPresenter(IImpositionService service, bool isFlatSheet, ImpositionPdfInfo pdfInfo)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _isFlatSheet = isFlatSheet;
            _pdfInfo = pdfInfo;
        }

        public static object CreateConfiguration(TemporaryImpositionParameters p, bool isFlatSheet)
        {
            p = p.EffectiveLayout(isFlatSheet);
            int multiple = p.IsLayoutMultipleEnabled ? p.LayoutMultiple : 1;
            var direction = p.LayoutConstraintDirection ?? CopyMode.AutoByColumn;
            if (isFlatSheet)
                return new FlatSheetConfiguration
                {
                    PaperWidth = p.PaperWidth, PaperHeight = p.PaperHeight,
                    MarginTop = p.MarginTop, MarginBottom = p.MarginBottom,
                    MarginLeft = p.MarginLeft, MarginRight = p.MarginRight,
                    RotationMode = p.FlatSheetRotationMode,
                    Rows = p.RequestedRows, Columns = p.RequestedColumns,
                    LayoutMultiple = multiple, LayoutMultipleDirection = direction
                };
            return new RollMaterialConfiguration
            {
                FixedWidth = p.FixedWidth, MinLength = p.MinLength,
                MarginTop = p.RollMarginTop, MarginBottom = p.RollMarginBottom,
                MarginLeft = p.RollMarginLeft, MarginRight = p.RollMarginRight,
                RotationMode = p.RollMaterialRotationMode,
                Rows = p.RequestedRows, Columns = p.RequestedColumns, UseExactRows = true,
                LayoutMultiple = multiple, LayoutMultipleDirection = direction
            };
        }

        public static async Task<ImpositionPdfInfo> AnalyzePreviewAsync(IImpositionService service,
            string filePath, PdfPageBoxSource source, int referencePage, float fallbackWidth, float fallbackHeight)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    return await service.AnalyzePdfFileAsync(filePath, source, referencePage);
                return fallbackWidth > 0 && fallbackHeight > 0
                    ? new ImpositionPdfInfo { HasCropBox = true, CropBoxWidth = fallbackWidth, CropBoxHeight = fallbackHeight }
                    : null;
            }
            catch (Exception ex)
            {
                LogHelper.Error("读取临时排版预览的 PDF 尺寸失败", ex);
                return null;
            }
        }

        public async Task<List<ImpositionResult>> PreviewAsync(TemporaryImpositionParameters p, CancellationToken token)
        {
            try
            {
                if (_pdfInfo == null) return new List<ImpositionResult>();
                return await _service.GetLayoutChoicesAsync(CreateConfiguration(p, _isFlatSheet), _pdfInfo,
                    p.EffectiveLayout(_isFlatSheet).RequestedLayoutQuantity, token);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                LogHelper.Error("临时排版预览失败", ex);
                return new List<ImpositionResult>
                {
                    new ImpositionResult { Success = false, IsPrintable = false, ErrorMessage = "排版预览失败，请检查参数后重试。" }
                };
            }
        }
    }
}
