using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 布局计算结果缓存
    /// 用于在 MaterialSelectFormModern 和其他组件之间共享布局计算结果
    /// 从 SettingsForm 迁移的静态变量和方法
    /// </summary>
    public static class LayoutResultsCache
    {
        private static int _savedLayoutRows = 0;
        private static int _savedLayoutColumns = 0;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取保存的行数
        /// </summary>
        public static int SavedLayoutRows
        {
            get { lock (_lock) return _savedLayoutRows; }
        }

        /// <summary>
        /// 获取保存的列数
        /// </summary>
        public static int SavedLayoutColumns
        {
            get { lock (_lock) return _savedLayoutColumns; }
        }

        /// <summary>
        /// 更新布局计算结果
        /// </summary>
        /// <param name="rows">行数</param>
        /// <param name="columns">列数</param>
        public static void UpdateLayoutResults(int rows, int columns)
        {
            lock (_lock)
            {
                _savedLayoutRows = rows;
                _savedLayoutColumns = columns;
            }
            LogHelper.Debug($"[LayoutResultsCache] 更新布局计算结果: 行数={rows}, 列数={columns}");
        }

        /// <summary>
        /// 清除布局计算结果
        /// </summary>
        public static void ClearLayoutResults()
        {
            lock (_lock)
            {
                _savedLayoutRows = 0;
                _savedLayoutColumns = 0;
            }
            LogHelper.Debug("[LayoutResultsCache] 已清除布局计算结果");
        }
    }
}
