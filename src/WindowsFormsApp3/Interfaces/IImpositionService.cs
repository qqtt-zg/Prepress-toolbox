using System;
using System.Threading;
using System.Threading.Tasks;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Interfaces
{
    /// <summary>
    /// 印刷排版服务接口
    /// 提供平张和卷装材料的布局计算、空白页补充等核心排版功能
    /// </summary>
    public interface IImpositionService
    {
        #region 核心布局计算

        /// <summary>
        /// 计算平张模式的布局
        /// </summary>
        /// <param name="config">平张模式配置</param>
        /// <param name="pdfInfo">PDF文件信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>布局计算结果</returns>
        Task<ImpositionResult> CalculateFlatSheetLayoutAsync(
            FlatSheetConfiguration config,
            ImpositionPdfInfo pdfInfo,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 计算卷装模式的布局
        /// </summary>
        /// <param name="config">卷装模式配置</param>
        /// <param name="pdfInfo">PDF文件信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="rotationMode">卷装旋转模式（可选，默认null表示自动计算）</param>
        /// <returns>布局计算结果</returns>
        Task<ImpositionResult> CalculateRollMaterialLayoutAsync(
            RollMaterialConfiguration config,
            ImpositionPdfInfo pdfInfo,
            CancellationToken cancellationToken = default,
            RollRotationMode? rotationMode = null);

        /// <summary>
        /// 计算最优偶数列布局（一式两联）
        /// </summary>
        /// <param name="config">排版配置（平张或卷装）</param>
        /// <param name="pdfInfo">PDF文件信息</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>布局计算结果（确保列数为最优偶数）</returns>
        Task<ImpositionResult> CalculateOptimalEvenColumnsLayoutAsync(
            object config,
            ImpositionPdfInfo pdfInfo,
            CancellationToken cancellationToken = default);

        #endregion

        #region PDF文件分析

        /// <summary>
        /// 分析PDF文件信息（返回排版专用的PDF信息）
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>排版用PDF文件信息</returns>
        Task<ImpositionPdfInfo> AnalyzePdfFileAsync(
            string filePath,
            CancellationToken cancellationToken = default);

        #endregion

        #region 空白页计算

        /// <summary>
        /// 计算需要补充的空白页数量
        /// </summary>
        /// <param name="currentPageCount">当前页数</param>
        /// <param name="layoutQuantity">布局数量</param>
        /// <returns>空白页数量</returns>
        int CalculateBlankPagesNeeded(int currentPageCount, int layoutQuantity);

        /// <summary>
        /// 异步计算需要补充的空白页数量
        /// </summary>
        /// <param name="currentPageCount">当前页数</param>
        /// <param name="layoutQuantity">布局数量</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>空白页数量</returns>
        Task<int> CalculateBlankPagesNeededAsync(
            int currentPageCount,
            int layoutQuantity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 计算空白页插入方案
        /// </summary>
        /// <param name="currentPageCount">当前页数</param>
        /// <param name="layoutQuantity">布局数量</param>
        /// <param name="layoutMode">排版模式</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>空白页补充结果</returns>
        Task<BlankPageResult> CalculateBlankPageInsertionAsync(
            int currentPageCount,
            int layoutQuantity,
            LayoutMode layoutMode,
            CancellationToken cancellationToken = default);

        #endregion

        #region 标准配置获取

        /// <summary>
        /// 获取标准平张配置
        /// </summary>
        /// <returns>标准平张配置</returns>
        FlatSheetConfiguration GetStandardFlatSheetConfiguration();

        /// <summary>
        /// 获取标准卷装配置
        /// </summary>
        /// <returns>标准卷装配置</returns>
        RollMaterialConfiguration GetStandardRollMaterialConfiguration();

        /// <summary>
        /// 获取所有预设的平张配置
        /// </summary>
        /// <returns>平张配置列表</returns>
        System.Collections.Generic.List<FlatSheetConfiguration> GetPresetFlatSheetConfigurations();

        /// <summary>
        /// 获取所有预设的卷装配置
        /// </summary>
        /// <returns>卷装配置列表</returns>
        System.Collections.Generic.List<RollMaterialConfiguration> GetPresetRollMaterialConfigurations();

        #endregion

        #region 配置验证

        /// <summary>
        /// 验证平张配置
        /// </summary>
        /// <param name="config">平张配置</param>
        /// <returns>验证结果</returns>
        ImpositionValidationResult ValidateFlatSheetConfiguration(FlatSheetConfiguration config);

        /// <summary>
        /// 验证卷装配置
        /// </summary>
        /// <param name="config">卷装配置</param>
        /// <returns>验证结果</returns>
        ImpositionValidationResult ValidateRollMaterialConfiguration(RollMaterialConfiguration config);

        /// <summary>
        /// 验证PDF文件是否适合排版
        /// </summary>
        /// <param name="pdfInfo">PDF文件信息</param>
        /// <returns>验证结果</returns>
        ImpositionValidationResult ValidatePdfForImposition(ImpositionPdfInfo pdfInfo);

        #endregion

        #region 批量处理支持

        /// <summary>
        /// 批量计算多个PDF文件的布局
        /// </summary>
        /// <param name="files">PDF文件路径列表</param>
        /// <param name="config">排版配置（平张或卷装）</param>
        /// <param name="materialType">材料类型</param>
        /// <param name="progressCallback">进度回调</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>批量计算结果</returns>
        Task<System.Collections.Generic.List<ImpositionResult>> BatchCalculateLayoutAsync(
            System.Collections.Generic.List<string> files,
            object config,
            MaterialType materialType,
            IProgress<ImpositionProgress> progressCallback = null,
            CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// 印刷排版进度信息
    /// </summary>
    public class ImpositionProgress
    {
        /// <summary>
        /// 当前处理的文件索引
        /// </summary>
        public int CurrentFileIndex { get; set; }

        /// <summary>
        /// 总文件数
        /// </summary>
        public int TotalFiles { get; set; }

        /// <summary>
        /// 进度百分比（0-100）
        /// </summary>
        public int ProgressPercentage => TotalFiles > 0 ? (int)((double)CurrentFileIndex / TotalFiles * 100) : 0;

        /// <summary>
        /// 当前处理的文件名
        /// </summary>
        public string CurrentFileName { get; set; }

        /// <summary>
        /// 当前操作描述
        /// </summary>
        public string CurrentOperation { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; } = DateTime.Now;
    }
}