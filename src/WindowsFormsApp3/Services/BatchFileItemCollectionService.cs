using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 同步维护待处理文件主列表与分组投影。
    /// </summary>
    public static class BatchFileItemCollectionService
    {
        /// <summary>
        /// 将选中的文件作为连续块上移或下移一个位置。
        /// </summary>
        public static bool MoveByOffset(
            BindingList<BatchFileItem> batchItems,
            IEnumerable<string> filePaths,
            int offset)
        {
            if (batchItems == null || offset == 0) return false;

            var paths = new HashSet<string>(
                filePaths?.Where(path => !string.IsNullOrWhiteSpace(path)) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            if (paths.Count == 0) return false;

            bool moved = false;
            if (offset < 0)
            {
                for (int index = 1; index < batchItems.Count; index++)
                {
                    if (paths.Contains(batchItems[index].FilePath ?? string.Empty) &&
                        !paths.Contains(batchItems[index - 1].FilePath ?? string.Empty))
                    {
                        Swap(batchItems, index, index - 1);
                        moved = true;
                    }
                }
            }
            else
            {
                for (int index = batchItems.Count - 2; index >= 0; index--)
                {
                    if (paths.Contains(batchItems[index].FilePath ?? string.Empty) &&
                        !paths.Contains(batchItems[index + 1].FilePath ?? string.Empty))
                    {
                        Swap(batchItems, index, index + 1);
                        moved = true;
                    }
                }
            }

            return moved;
        }

        public static void RemoveByPaths(
            BindingList<BatchFileItem> batchItems,
            List<BatchProcessGroup> groups,
            IEnumerable<string> filePaths)
        {
            if (batchItems == null || groups == null) return;

            var paths = new HashSet<string>(
                filePaths?.Where(path => !string.IsNullOrWhiteSpace(path)) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);
            if (paths.Count == 0) return;

            for (int index = batchItems.Count - 1; index >= 0; index--)
            {
                if (paths.Contains(batchItems[index].FilePath ?? string.Empty))
                {
                    batchItems.RemoveAt(index);
                }
            }

            foreach (BatchProcessGroup group in groups)
            {
                group.Items.RemoveAll(item => paths.Contains(item.FilePath ?? string.Empty));
            }
            groups.RemoveAll(group => group.Items.Count == 0);
        }

        private static void Swap(BindingList<BatchFileItem> items, int firstIndex, int secondIndex)
        {
            BatchFileItem item = items[firstIndex];
            items[firstIndex] = items[secondIndex];
            items[secondIndex] = item;
        }
    }
}
