using System;

namespace WindowsFormsApp3.Interfaces
{
    /// <summary>
    /// 尺寸计算服务接口
    /// 提供 PDF 尺寸识别和最终尺寸计算功能
    /// </summary>
    public interface IDimensionCalculationService
    {
        /// <summary>
        /// 识别 PDF 文件的尺寸
        /// </summary>
        /// <param name="pdfPath">PDF 文件路径</param>
        /// <param name="width">输出宽度（毫米）</param>
        /// <param name="height">输出高度（毫米）</param>
        /// <returns>是否成功识别</returns>
        bool RecognizePdfDimensions(string pdfPath, out double width, out double height);

        /// <summary>
        /// 计算最终尺寸（含出血和形状代号）
        /// </summary>
        /// <param name="width">PDF 原始宽度</param>
        /// <param name="height">PDF 原始高度</param>
        /// <param name="tetBleed">出血值</param>
        /// <param name="cornerRadius">圆角半径（可选，默认为 "0"）</param>
        /// <param name="addPdfLayers">是否添加形状处理（可选，默认为 false）</param>
        /// <returns>格式化的尺寸字符串，如 "84x54R5"</returns>
        string CalculateFinalDimensions(double width, double height, double tetBleed, 
            string cornerRadius = "0", bool addPdfLayers = false);
    }
}
