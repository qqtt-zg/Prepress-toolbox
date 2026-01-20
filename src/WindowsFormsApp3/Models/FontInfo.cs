using System.Collections.Generic;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// PDF字体信息
    /// </summary>
    public class FontInfo
    {
        /// <summary>
        /// 字体名称
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// 字体类型 (TrueType, Type1, Type3, CIDFont等)
        /// </summary>
        public string FontType { get; set; }

        /// <summary>
        /// 字体子类型
        /// </summary>
        public string FontSubtype { get; set; }

        /// <summary>
        /// 嵌入状态
        /// </summary>
        public FontEmbeddingStatus EmbeddingStatus { get; set; }

        /// <summary>
        /// 是否为子集嵌入
        /// </summary>
        public bool IsSubset { get; set; }

        /// <summary>
        /// 字体编码
        /// </summary>
        public string Encoding { get; set; }

        /// <summary>
        /// 使用该字体的页面列表
        /// </summary>
        public List<int> UsedPages { get; set; } = new List<int>();

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 是否有问题
        /// </summary>
        public bool HasIssues { get; set; }

        /// <summary>
        /// 问题描述列表
        /// </summary>
        public List<string> Issues { get; set; } = new List<string>();

        /// <summary>
        /// 字体文件大小（如果嵌入）
        /// </summary>
        public long? EmbeddedSize { get; set; }

        /// <summary>
        /// 是否为标准14字体
        /// </summary>
        public bool IsStandardFont { get; set; }

        /// <summary>
        /// 获取嵌入状态的显示文本
        /// </summary>
        public string EmbeddingStatusText
        {
            get
            {
                switch (EmbeddingStatus)
                {
                    case FontEmbeddingStatus.FullyEmbedded:
                        return "完全嵌入";
                    case FontEmbeddingStatus.SubsetEmbedded:
                        return "子集嵌入";
                    case FontEmbeddingStatus.NotEmbedded:
                        return "未嵌入";
                    default:
                        return "未知";
                }
            }
        }

        /// <summary>
        /// 获取状态图标
        /// </summary>
        public string StatusIcon
        {
            get
            {
                if (HasIssues)
                    return "⚠";
                else if (EmbeddingStatus == FontEmbeddingStatus.FullyEmbedded)
                    return "✓";
                else if (EmbeddingStatus == FontEmbeddingStatus.SubsetEmbedded)
                    return "◐";
                else
                    return "✗";
            }
        }

        /// <summary>
        /// 获取使用页面的显示文本
        /// </summary>
        public string UsedPagesText
        {
            get
            {
                if (UsedPages.Count == 0)
                    return "-";
                else if (UsedPages.Count <= 3)
                    return string.Join(", ", UsedPages);
                else
                    return $"{UsedPages[0]}, {UsedPages[1]}, {UsedPages[2]}... ({UsedPages.Count}页)";
            }
        }
    }

    /// <summary>
    /// 字体嵌入状态
    /// </summary>
    public enum FontEmbeddingStatus
    {
        /// <summary>
        /// 完全嵌入
        /// </summary>
        FullyEmbedded,

        /// <summary>
        /// 子集嵌入（只嵌入使用的字符）
        /// </summary>
        SubsetEmbedded,

        /// <summary>
        /// 未嵌入
        /// </summary>
        NotEmbedded
    }

    /// <summary>
    /// PDF文档字体信息汇总
    /// </summary>
    public class DocumentFontInfo
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 所有字体列表
        /// </summary>
        public List<FontInfo> Fonts { get; set; } = new List<FontInfo>();

        /// <summary>
        /// 字体总数
        /// </summary>
        public int TotalFonts => Fonts.Count;

        /// <summary>
        /// 完全嵌入的字体数
        /// </summary>
        public int FullyEmbeddedCount => Fonts.FindAll(f => f.EmbeddingStatus == FontEmbeddingStatus.FullyEmbedded).Count;

        /// <summary>
        /// 子集嵌入的字体数
        /// </summary>
        public int SubsetEmbeddedCount => Fonts.FindAll(f => f.EmbeddingStatus == FontEmbeddingStatus.SubsetEmbedded).Count;

        /// <summary>
        /// 未嵌入的字体数
        /// </summary>
        public int NotEmbeddedCount => Fonts.FindAll(f => f.EmbeddingStatus == FontEmbeddingStatus.NotEmbedded).Count;

        /// <summary>
        /// 有问题的字体数
        /// </summary>
        public int ProblematicFontsCount => Fonts.FindAll(f => f.HasIssues).Count;

        /// <summary>
        /// 是否有字体问题
        /// </summary>
        public bool HasFontIssues => ProblematicFontsCount > 0;
    }
}
