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
        // 平张布局结果
        private static int _flatSheetRows = 0;
        private static int _flatSheetColumns = 0;
        private static int _flatSheetLayoutCount = 0;

        // 卷装布局结果
        private static int _rollMaterialRows = 0;
        private static int _rollMaterialColumns = 0;
        private static int _rollMaterialLayoutCount = 0;

        private static readonly object _lock = new object();

        /// <summary>
        /// 获取平张行数
        /// </summary>
        public static int FlatSheetRows
        {
            get { lock (_lock) return _flatSheetRows; }
        }

        /// <summary>
        /// 获取平张列数
        /// </summary>
        public static int FlatSheetColumns
        {
            get { lock (_lock) return _flatSheetColumns; }
        }

        /// <summary>
        /// 获取平张布局数
        /// </summary>
        public static int FlatSheetLayoutCount
        {
            get { lock (_lock) return _flatSheetLayoutCount; }
        }

        /// <summary>
        /// 获取卷装行数
        /// </summary>
        public static int RollMaterialRows
        {
            get { lock (_lock) return _rollMaterialRows; }
        }

        /// <summary>
        /// 获取卷装列数
        /// </summary>
        public static int RollMaterialColumns
        {
            get { lock (_lock) return _rollMaterialColumns; }
        }

        /// <summary>
        /// 获取卷装布局数
        /// </summary>
        public static int RollMaterialLayoutCount
        {
            get { lock (_lock) return _rollMaterialLayoutCount; }
        }

        /// <summary>
        /// 更新平张布局计算结果
        /// </summary>
        public static void UpdateFlatSheetLayoutResults(int rows, int columns)
        {
            lock (_lock)
            {
                _flatSheetRows = rows;
                _flatSheetColumns = columns;
                _flatSheetLayoutCount = rows * columns;
            }
            LogHelper.Debug($"[LayoutResultsCache] 更新平张布局结果: {rows}x{columns}={_flatSheetLayoutCount}");
        }

        /// <summary>
        /// 更新卷装布局计算结果
        /// </summary>
        public static void UpdateRollMaterialLayoutResults(int rows, int columns)
        {
            lock (_lock)
            {
                _rollMaterialRows = rows;
                _rollMaterialColumns = columns;
                _rollMaterialLayoutCount = rows * columns;
            }
            LogHelper.Debug($"[LayoutResultsCache] 更新卷装布局结果: {rows}x{columns}={_rollMaterialLayoutCount}");
        }

        /// <summary>
        /// 清除布局计算结果
        /// </summary>
        public static void ClearLayoutResults()
        {
            lock (_lock)
            {
                _flatSheetRows = 0;
                _flatSheetColumns = 0;
                _flatSheetLayoutCount = 0;
                _rollMaterialRows = 0;
                _rollMaterialColumns = 0;
                _rollMaterialLayoutCount = 0;
            }
            LogHelper.Debug("[LayoutResultsCache] 已清除布局计算结果");
        }
    }
}
