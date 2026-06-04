using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Interfaces
{
    public interface IPdfProcessingService
    {
        bool AddLayerToPdf(string sourcePdfPath, string outputPdfPath, PdfLayerInfo layerInfo);
        bool MergePdfFiles(List<string> sourcePdfPaths, string outputPdfPath);
        int GetPdfPageCount(string pdfPath);
        [Obsolete("请使用AddDotsAddCounterLayer(string, string, ShapeType, double)方法")]
        bool AddDotsAddCounterLayer(string pdfPath, string dimensions, string cornerRadius = "0", bool usePdfLastPage = false);

        /// <summary>
        /// 为PDF添加Dots_AddCounter图层（新的枚举版本）
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="dimensions">最终尺寸</param>
        /// <param name="shapeType">形状类型</param>
        /// <param name="roundRadius">圆角半径（仅用于圆角矩形）</param>
        /// <returns>是否成功添加图层</returns>
        bool AddDotsAddCounterLayer(string pdfPath, string dimensions, ShapeType shapeType, double roundRadius = 0);

        /// <summary>
        /// 添加形状图层（不进行页面统一处理，假设页面已经统一）
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="dimensions">最终尺寸</param>
        /// <param name="shapeType">形状类型</param>
        /// <param name="roundRadius">圆角半径</param>
        /// <returns>处理是否成功</returns>
        bool AddDotsAddCounterLayerOnly(string pdfPath, string dimensions, ShapeType shapeType, double roundRadius = 0);

        /// <summary>
        /// 复制PDF页面（支持一式N份，按123123顺序复制）
        /// </summary>
        /// <param name="sourcePdfPath">源PDF路径</param>
        /// <param name="outputPdfPath">输出PDF路径</param>
        /// <param name="copySetCount">份数（一式N份的N值）</param>
        /// <param name="progressCallback">进度回调 (current, total, message)</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>是否成功</returns>
        Task<bool> CopyPdfPagesAsync(
            string sourcePdfPath,
            string outputPdfPath,
            int copySetCount,
            IProgress<(int current, int total, string message)> progressCallback = null,
            CancellationToken cancellationToken = default);
    }
}