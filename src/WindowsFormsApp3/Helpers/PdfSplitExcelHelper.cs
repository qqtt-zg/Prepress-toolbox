using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OfficeOpenXml;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Helpers
{
    /// <summary>
    /// Excel行数据（包含文件名、页数）
    /// </summary>
    public class ExcelRowData
    {
        public string FileName { get; set; }
        public string OrderNumber { get; set; }
        public string Quantity { get; set; }
        public int PageCount { get; set; }
    }

    /// <summary>
    /// PDF拆分Excel帮助类
    /// 用于读取Excel/CSV文件中的文件名和页数数据
    /// </summary>
    public class PdfSplitExcelHelper
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public PdfSplitExcelHelper()
        {
        }

        /// <summary>
        /// 从Excel/CSV文件读取拆分信息列表
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileNameColumnIndex">文件名列索引（0-based）</param>
        /// <param name="pageCountColumnIndex">页数列索引（0-based）</param>
        /// <param name="hasHeader">是否包含表头</param>
        /// <returns>拆分信息列表</returns>
        public List<SplitFileInfo> ReadSplitInfoFromFile(string filePath, int fileNameColumnIndex = 0, int pageCountColumnIndex = 1, bool hasHeader = true)
        {
            var result = new List<SplitFileInfo>();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("指定的文件不存在", filePath);
            }

            string extension = Path.GetExtension(filePath).ToLower();
            if (extension == ".csv")
            {
                result = ReadFromCsv(filePath, fileNameColumnIndex, pageCountColumnIndex, hasHeader);
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                result = ReadFromExcel(filePath, fileNameColumnIndex, pageCountColumnIndex, hasHeader);
            }
            else
            {
                throw new NotSupportedException($"不支持的文件格式: {extension}");
            }

            return result;
        }

        /// <summary>
        /// 从Excel/CSV文件读取拆分信息列表（包含订单号和数量）
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileNameColumnIndex">文件名列索引（0-based）</param>
        /// <param name="pageCountColumnIndex">页数列索引（0-based）</param>
        /// <param name="orderNumberColumnIndex">订单号列索引（0-based）</param>
        /// <param name="quantityColumnIndex">数量列索引（0-based）</param>
        /// <param name="hasHeader">是否包含表头</param>
        /// <returns>Excel行数据列表</returns>
        public List<ExcelRowData> ReadSplitInfoWithOrderFromFile(
            string filePath,
            int fileNameColumnIndex = 0,
            int pageCountColumnIndex = 1,
            int orderNumberColumnIndex = 2,
            int quantityColumnIndex = 3,
            bool hasHeader = true)
        {
            var result = new List<ExcelRowData>();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("指定的文件不存在", filePath);
            }

            string extension = Path.GetExtension(filePath).ToLower();
            if (extension == ".csv")
            {
                result = ReadFromCsvWithOrder(filePath, fileNameColumnIndex, pageCountColumnIndex, orderNumberColumnIndex, quantityColumnIndex, hasHeader);
            }
            else if (extension == ".xlsx" || extension == ".xls")
            {
                result = ReadFromExcelWithOrder(filePath, fileNameColumnIndex, pageCountColumnIndex, orderNumberColumnIndex, quantityColumnIndex, hasHeader);
            }
            else
            {
                throw new NotSupportedException($"不支持的文件格式: {extension}");
            }

            return result;
        }

        /// <summary>
        /// 从Excel文件读取数据（包含订单号和数量）
        /// </summary>
        private List<ExcelRowData> ReadFromExcelWithOrder(string filePath, int fileNameColumnIndex, int pageCountColumnIndex, int orderNumberColumnIndex, int quantityColumnIndex, bool hasHeader)
        {
            var result = new List<ExcelRowData>();

            // 设置EPPlus许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    throw new InvalidOperationException("Excel文件中没有工作表");
                }

                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                if (worksheet.Dimension == null)
                {
                    throw new InvalidOperationException("Excel工作表为空");
                }

                int rowCount = worksheet.Dimension.Rows;
                int startRow = hasHeader ? 2 : 1;

                for (int row = startRow; row <= rowCount; row++)
                {
                    try
                    {
                        // 获取文件名
                        var fileNameCell = worksheet.Cells[row, fileNameColumnIndex + 1];
                        string fileName = fileNameCell?.Text?.Trim();

                        // 获取页数
                        var pageCountCell = worksheet.Cells[row, pageCountColumnIndex + 1];
                        object pageCountValue = pageCountCell?.Value;
                        int pageCount = 0;

                        if (pageCountValue != null)
                        {
                            if (pageCountValue is int intValue)
                            {
                                pageCount = intValue;
                            }
                            else if (pageCountValue is double doubleValue)
                            {
                                pageCount = (int)doubleValue;
                            }
                            else if (pageCountValue is string stringValue && int.TryParse(stringValue, out int parsedValue))
                            {
                                pageCount = parsedValue;
                            }
                        }

                        // 获取订单号
                        string orderNumber = "";
                        if (orderNumberColumnIndex >= 0)
                        {
                            var orderCell = worksheet.Cells[row, orderNumberColumnIndex + 1];
                            orderNumber = orderCell?.Text?.Trim() ?? "";
                        }

                        // 获取数量
                        string quantity = "";
                        if (quantityColumnIndex >= 0)
                        {
                            var quantityCell = worksheet.Cells[row, quantityColumnIndex + 1];
                            object quantityValue = quantityCell?.Value;
                            if (quantityValue != null)
                            {
                                if (quantityValue is string stringValue)
                                    quantity = stringValue.Trim();
                                else
                                    quantity = quantityValue.ToString();
                            }
                        }

                        // 跳过空行
                        if (string.IsNullOrEmpty(fileName) && pageCount <= 0)
                            continue;

                        result.Add(new ExcelRowData
                        {
                            FileName = fileName ?? $"文件{row}",
                            PageCount = pageCount,
                            OrderNumber = orderNumber,
                            Quantity = quantity
                        });
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Warn($"读取Excel第{row}行数据时出错: {ex.Message}");
                    }
                }
            }

            LogHelper.Info($"从Excel文件读取拆分信息成功 - 文件: {filePath}, 记录数: {result.Count}");
            return result;
        }

        /// <summary>
        /// 从CSV文件读取数据（包含订单号和数量）
        /// </summary>
        private List<ExcelRowData> ReadFromCsvWithOrder(string filePath, int fileNameColumnIndex, int pageCountColumnIndex, int orderNumberColumnIndex, int quantityColumnIndex, bool hasHeader)
        {
            var result = new List<ExcelRowData>();

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            int startRow = hasHeader ? 1 : 0;

            for (int i = startRow; i < lines.Length; i++)
            {
                try
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    // 简单解析CSV（支持基本逗号分隔）
                    var columns = ParseCsvLine(line);

                    int maxIndex = Math.Max(Math.Max(fileNameColumnIndex, pageCountColumnIndex), Math.Max(orderNumberColumnIndex, quantityColumnIndex));
                    if (columns.Count <= maxIndex)
                        continue;

                    string fileName = columns[fileNameColumnIndex].Trim();
                    string pageCountStr = columns[pageCountColumnIndex].Trim();
                    string orderNumber = orderNumberColumnIndex >= 0 && orderNumberColumnIndex < columns.Count
                        ? columns[orderNumberColumnIndex].Trim()
                        : "";
                    string quantity = quantityColumnIndex >= 0 && quantityColumnIndex < columns.Count
                        ? columns[quantityColumnIndex].Trim()
                        : "";

                    if (!int.TryParse(pageCountStr, out int pageCount))
                        pageCount = 0;

                    // 跳过空行
                    if (string.IsNullOrEmpty(fileName) && pageCount <= 0)
                        continue;

                    result.Add(new ExcelRowData
                    {
                        FileName = fileName ?? $"文件{i + 1}",
                        PageCount = pageCount,
                        OrderNumber = orderNumber,
                        Quantity = quantity
                    });
                }
                catch (Exception ex)
                {
                    LogHelper.Warn($"读取CSV第{i + 1}行数据时出错: {ex.Message}");
                }
            }

            LogHelper.Info($"从CSV文件读取拆分信息成功 - 文件: {filePath}, 记录数: {result.Count}");
            return result;
        }

        /// <summary>
        /// 从Excel文件读取数据
        /// </summary>
        private List<SplitFileInfo> ReadFromExcel(string filePath, int fileNameColumnIndex, int pageCountColumnIndex, bool hasHeader)
        {
            var result = new List<SplitFileInfo>();

            // 设置EPPlus许可证上下文
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                if (package.Workbook.Worksheets.Count == 0)
                {
                    throw new InvalidOperationException("Excel文件中没有工作表");
                }

                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                if (worksheet.Dimension == null)
                {
                    throw new InvalidOperationException("Excel工作表为空");
                }

                int rowCount = worksheet.Dimension.Rows;
                int startRow = hasHeader ? 2 : 1;

                for (int row = startRow; row <= rowCount; row++)
                {
                    try
                    {
                        // 获取文件名
                        var fileNameCell = worksheet.Cells[row, fileNameColumnIndex + 1];
                        string fileName = fileNameCell?.Text?.Trim();

                        // 获取页数
                        var pageCountCell = worksheet.Cells[row, pageCountColumnIndex + 1];
                        object pageCountValue = pageCountCell?.Value;
                        int pageCount = 0;

                        if (pageCountValue != null)
                        {
                            if (pageCountValue is int intValue)
                            {
                                pageCount = intValue;
                            }
                            else if (pageCountValue is double doubleValue)
                            {
                                pageCount = (int)doubleValue;
                            }
                            else if (pageCountValue is string stringValue && int.TryParse(stringValue, out int parsedValue))
                            {
                                pageCount = parsedValue;
                            }
                        }

                        // 跳过空行
                        if (string.IsNullOrEmpty(fileName) && pageCount <= 0)
                            continue;

                        result.Add(new SplitFileInfo
                        {
                            FileName = fileName ?? $"文件{row}",
                            PageCount = pageCount
                        });
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Warn($"读取Excel第{row}行数据时出错: {ex.Message}");
                    }
                }
            }

            LogHelper.Info($"从Excel文件读取拆分信息成功 - 文件: {filePath}, 记录数: {result.Count}");
            return result;
        }

        /// <summary>
        /// 从CSV文件读取数据
        /// </summary>
        private List<SplitFileInfo> ReadFromCsv(string filePath, int fileNameColumnIndex, int pageCountColumnIndex, bool hasHeader)
        {
            var result = new List<SplitFileInfo>();

            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            int startRow = hasHeader ? 1 : 0;

            for (int i = startRow; i < lines.Length; i++)
            {
                try
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    // 简单解析CSV（支持基本逗号分隔）
                    var columns = ParseCsvLine(line);

                    if (columns.Count <= Math.Max(fileNameColumnIndex, pageCountColumnIndex))
                        continue;

                    string fileName = columns[fileNameColumnIndex].Trim();
                    string pageCountStr = columns[pageCountColumnIndex].Trim();

                    if (!int.TryParse(pageCountStr, out int pageCount))
                        pageCount = 0;

                    // 跳过空行
                    if (string.IsNullOrEmpty(fileName) && pageCount <= 0)
                        continue;

                    result.Add(new SplitFileInfo
                    {
                        FileName = fileName ?? $"文件{i + 1}",
                        PageCount = pageCount
                    });
                }
                catch (Exception ex)
                {
                    LogHelper.Warn($"读取CSV第{i + 1}行数据时出错: {ex.Message}");
                }
            }

            LogHelper.Info($"从CSV文件读取拆分信息成功 - 文件: {filePath}, 记录数: {result.Count}");
            return result;
        }

        /// <summary>
        /// 简单解析CSV行（处理引号包围的值）
        /// </summary>
        private List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var currentValue = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // 转义的引号
                        currentValue.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            result.Add(currentValue.ToString());
            return result;
        }
    }
}
