using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// PDF处理服务接口，负责PDF文件的图层管理、文件合并和页面信息获取等核心功能
    /// 提供PDF文件的基本处理能力，支持图层操作和文件合并等功能
    /// </summary>
    public interface IPdfProcessingService
    {
        /// <summary>
        /// 向PDF文件添加自定义图层
        /// </summary>
        /// <param name="sourcePdfPath">源PDF文件的完整路径</param>
        /// <param name="outputPdfPath">输出PDF文件的完整路径</param>
        /// <param name="layerInfo">包含图层名称、内容和样式等信息的图层对象</param>
        /// <returns>添加图层操作是否成功</returns>
        /// <exception cref="System.ArgumentNullException">当sourcePdfPath、outputPdfPath或layerInfo为null或空时抛出</exception>
        /// <exception cref="System.IO.FileNotFoundException">当指定的PDF文件不存在时抛出</exception>
        /// <exception cref="System.InvalidOperationException">当文件不是有效的PDF文件或添加图层失败时抛出</exception>
        /// <exception cref="System.IO.IOException">当文件访问权限不足或磁盘空间不足时抛出</exception>
        bool AddLayerToPdf(string sourcePdfPath, string outputPdfPath, PdfLayerInfo layerInfo);

        /// <summary>
        /// 将多个PDF文件合并为一个文件
        /// </summary>
        /// <param name="sourceFiles">要合并的PDF源文件路径列表，按顺序合并</param>
        /// <param name="outputFile">合并后输出的PDF文件路径</param>
        /// <returns>合并操作是否成功</returns>
        /// <exception cref="System.ArgumentNullException">当sourceFiles或outputFile为null或空时抛出</exception>
        /// <exception cref="System.IO.FileNotFoundException">当任何源文件不存在时抛出</exception>
        /// <exception cref="System.InvalidOperationException">当任何源文件不是有效的PDF文件或合并失败时抛出</exception>
        /// <exception cref="System.IO.IOException">当输出文件路径无效、访问权限不足或磁盘空间不足时抛出</exception>
        bool MergePdfFiles(List<string> sourceFiles, string outputFile);

        /// <summary>
        /// 获取指定PDF文件的总页数
        /// </summary>
        /// <param name="filePath">PDF文件的完整路径</param>
        /// <returns>PDF文件的总页数</returns>
        /// <exception cref="System.ArgumentNullException">当filePath为null或空时抛出</exception>
        /// <exception cref="System.IO.FileNotFoundException">当指定的PDF文件不存在时抛出</exception>
        /// <exception cref="System.InvalidOperationException">当文件不是有效的PDF文件或无法读取页数时抛出</exception>
        int GetPdfPageCount(string filePath);

        /// <summary>
        /// 为PDF文件添加名为"Dots_AddCounter"的图层
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="finalDimensions">最终尺寸，格式为"宽度x高度"（毫米）</param>
        /// <param name="cornerRadius">圆角半径，"0"表示直角矩形，"R"表示圆形，其他数值表示圆角矩形</param>
        /// <param name="usePdfLastPage">是否使用PDF最后一页逻辑</param>
        /// <returns>是否成功添加图层</returns>
        bool AddDotsAddCounterLayer(string filePath, string finalDimensions, string cornerRadius = "0", bool usePdfLastPage = false);

        /// <summary>
        /// 复制PDF页面（支持一式N份，按123123顺序复制）
        /// </summary>
        /// <param name="sourcePdfPath">源PDF路径</param>
        /// <param name="outputPdfPath">输出PDF路径</param>
        /// <param name="copySetCount">份数</param>
        /// <param name="progressCallback">进度回调</param>
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