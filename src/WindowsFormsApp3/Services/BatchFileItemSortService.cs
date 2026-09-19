using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 左侧待处理文件排序服务；未匹配项始终排在匹配项之前。
    /// </summary>
    public static class BatchFileItemSortService
    {
        public static List<BatchFileItem> Sort(
            IEnumerable<BatchFileItem> items,
            string propertyName,
            bool ascending)
        {
            var source = (items ?? Enumerable.Empty<BatchFileItem>())
                .Where(item => item != null)
                .OrderBy(item => item.IsExcelMatched ? 1 : 0);

            IOrderedEnumerable<BatchFileItem> sorted = propertyName switch
            {
                nameof(BatchFileItem.SerialNumber) => OrderText(source, item => item.SerialNumber, ascending),
                nameof(BatchFileItem.FileName) => OrderText(source, item => item.FileName, ascending),
                nameof(BatchFileItem.OrderNumber) => OrderText(source, item => item.OrderNumber, ascending),
                nameof(BatchFileItem.Quantity) => OrderNumber(source, item => ParseLong(item.Quantity), ascending),
                nameof(BatchFileItem.PageCount) => OrderNumber(source, item => item.PageCount ?? int.MinValue, ascending),
                nameof(BatchFileItem.Dimensions) => OrderNumber(source, item => ParseDimensionArea(item.Dimensions), ascending),
                nameof(BatchFileItem.LayoutInfo) => OrderText(source, item => item.LayoutInfo, ascending),
                _ => OrderNumber(source, item => item.Index, ascending)
            };

            return sorted.ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static IOrderedEnumerable<BatchFileItem> OrderText(
            IOrderedEnumerable<BatchFileItem> source,
            Func<BatchFileItem, string> selector,
            bool ascending)
        {
            return ascending
                ? source.ThenBy(selector, StringComparer.OrdinalIgnoreCase)
                : source.ThenByDescending(selector, StringComparer.OrdinalIgnoreCase);
        }

        private static IOrderedEnumerable<BatchFileItem> OrderNumber<T>(
            IOrderedEnumerable<BatchFileItem> source,
            Func<BatchFileItem, T> selector,
            bool ascending)
        {
            return ascending ? source.ThenBy(selector) : source.ThenByDescending(selector);
        }

        private static long ParseLong(string value)
        {
            return long.TryParse(value, out long result) ? result : long.MinValue;
        }

        private static double ParseDimensionArea(string dimensions)
        {
            if (string.IsNullOrWhiteSpace(dimensions)) return 0;
            Match match = Regex.Match(dimensions, @"([0-9]+(?:\.[0-9]+)?)\s*[xX*×]\s*([0-9]+(?:\.[0-9]+)?)");
            if (!match.Success ||
                !double.TryParse(match.Groups[1].Value, out double width) ||
                !double.TryParse(match.Groups[2].Value, out double height))
            {
                return 0;
            }

            return width * height;
        }
    }
}
