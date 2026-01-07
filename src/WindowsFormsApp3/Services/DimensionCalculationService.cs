using System;
using System.IO;
using System.Threading;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 尺寸计算服务实现
    /// 从 SettingsForm 迁移的 PDF 尺寸识别和计算逻辑
    /// </summary>
    public class DimensionCalculationService : IDimensionCalculationService
    {
        private readonly IPdfDimensionService _pdfDimensionService;
        private readonly ILogger _logger;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pdfDimensionService">PDF 尺寸服务</param>
        /// <param name="logger">日志服务（可选）</param>
        public DimensionCalculationService(IPdfDimensionService pdfDimensionService, ILogger logger = null)
        {
            _pdfDimensionService = pdfDimensionService ?? throw new ArgumentNullException(nameof(pdfDimensionService));
            _logger = logger;
        }

        /// <inheritdoc />
        public bool RecognizePdfDimensions(string pdfPath, out double width, out double height)
        {
            width = 0;
            height = 0;

            if (string.IsNullOrEmpty(pdfPath))
            {
                _logger?.LogWarning("PDF 路径为空");
                return false;
            }

            if (!File.Exists(pdfPath))
            {
                _logger?.LogWarning($"PDF 文件不存在: {pdfPath}");
                return false;
            }

            int retryCount = 3;
            int delayMs = 100;

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    if (_pdfDimensionService.GetFirstPageSize(pdfPath, out double w, out double h))
                    {
                        width = w;
                        height = h;
                        _logger?.LogInformation($"成功识别 PDF 尺寸: {width}x{height} mm");
                        return true;
                    }
                    else
                    {
                        _logger?.LogWarning($"无法获取 PDF 文件尺寸: {pdfPath}");
                        return false;
                    }
                }
                catch (IOException ex)
                {
                    if (i == retryCount - 1)
                    {
                        _logger?.LogError(ex, $"PDF 文件正在被使用或未保存完成: {pdfPath}");
                        return false;
                    }
                    Thread.Sleep(delayMs);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"PDF 尺寸识别失败: {pdfPath}");
                    return false;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public string CalculateFinalDimensions(double width, double height, double tetBleed, 
            string cornerRadius = "0", bool addPdfLayers = false)
        {
            // 应用公式: 长-tetBleed*2, 宽-tetBleed*2
            double finalWidth = CustomRound(width - tetBleed * 2);
            double finalHeight = CustomRound(height - tetBleed * 2);

            // 基础尺寸格式
            string dimensions = $"{finalWidth}x{finalHeight}";

            // 当启用形状处理时，根据 cornerRadius 添加形状信息
            if (addPdfLayers && !string.IsNullOrEmpty(cornerRadius))
            {
                string trimmedRadius = cornerRadius.Trim();
                
                // 从 AppSettings 读取形状代号
                var zeroShapeCode = AppSettings.Get("ZeroShapeCode") as string ?? "Z";
                var roundShapeCode = AppSettings.Get("RoundShapeCode") as string ?? "R";
                var ellipseShapeCode = AppSettings.Get("EllipseShapeCode") as string ?? "Y";
                var circleShapeCode = AppSettings.Get("CircleShapeCode") as string ?? "C";
                var hideRadiusValue = AppSettings.Get("HideRadiusValue") as bool? ?? false;

                if (trimmedRadius.Equals("R", StringComparison.OrdinalIgnoreCase))
                {
                    dimensions += circleShapeCode; // 使用圆形代号
                }
                else if (trimmedRadius.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    dimensions += ellipseShapeCode;
                }
                else if (int.TryParse(trimmedRadius, out int numRadius) && numRadius > 0)
                {
                    // 对于数字半径值，添加前缀
                    if (hideRadiusValue)
                    {
                        // 如果隐藏半径数值复选框被勾选，只添加代号不添加数字
                        dimensions += roundShapeCode;
                    }
                    else
                    {
                        // 否则添加代号和数字
                        dimensions += roundShapeCode + numRadius;
                    }
                }
                else if (trimmedRadius.Equals("0"))
                {
                    // 当形状输入为 "0" 时，添加代号
                    dimensions += zeroShapeCode;
                }
            }

            return dimensions;
        }

        /// <summary>
        /// 自定义四舍五入到十分位
        /// </summary>
        private static double CustomRound(double value)
        {
            return Math.Round(value, 1);
        }
    }
}
