using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;
using IOPath = System.IO.Path;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// PDF检查器服务
    /// 提供类似Enfocus PitStop Pro Inspector的功能
    /// </summary>
    public class PdfInspectorService
    {
        /// <summary>
        /// 检查PDF文件并获取完整的检查器信息
        /// </summary>
        public PdfInspectorInfo InspectPdf(string filePath, int currentPage = 1)
        {
            var info = new PdfInspectorInfo
            {
                FilePath = filePath,
                FileName = IOPath.GetFileName(filePath),
                CurrentPage = currentPage
            };

            try
            {
                if (!File.Exists(filePath))
                {
                    LogHelper.Error($"PDF文件不存在: {filePath}");
                    return info;
                }

                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument document = new PdfDocument(reader))
                {
                    info.TotalPages = document.GetNumberOfPages();

                    // 获取所有页面的页面框信息
                    for (int i = 1; i <= info.TotalPages; i++)
                    {
                        var pageBoxInfo = GetPageBoxInfo(document, i);
                        info.AllPageBoxes.Add(pageBoxInfo);

                        if (i == currentPage)
                        {
                            info.CurrentPageBoxes = pageBoxInfo;
                        }
                    }

                    // 检测问题
                    info.Issues = DetectIssues(info.AllPageBoxes);

                    // 标记有问题的页面
                    foreach (var issue in info.Issues)
                    {
                        var page = info.AllPageBoxes.FirstOrDefault(p => p.PageNumber == issue.PageNumber);
                        if (page != null)
                        {
                            page.HasIssues = true;
                            page.IssueDescriptions.Add(issue.Description);
                        }
                    }

                    LogHelper.Info($"PDF检查完成: {filePath}, 总页数: {info.TotalPages}, 问题数: {info.Issues.Count}");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"PDF检查失败: {filePath}, 错误: {ex.Message}");
            }

            return info;
        }

        /// <summary>
        /// 获取指定页面的页面框信息
        /// </summary>
        private PageBoxInfo GetPageBoxInfo(PdfDocument document, int pageNumber)
        {
            var pageInfo = new PageBoxInfo
            {
                PageNumber = pageNumber
            };

            try
            {
                PdfPage page = document.GetPage(pageNumber);
                pageInfo.Rotation = page.GetRotation();

                // 获取各种页面框
                Rectangle mediaBox = page.GetMediaBox();
                Rectangle cropBox = page.GetCropBox();
                Rectangle trimBox = page.GetTrimBox();
                Rectangle bleedBox = page.GetBleedBox();
                Rectangle artBox = page.GetArtBox();

                // MediaBox（必须存在）
                pageInfo.MediaBox = ConvertRectangleToBoxDimension(mediaBox, true);

                // CropBox（如果未定义，默认等于MediaBox）
                pageInfo.CropBox = ConvertRectangleToBoxDimension(cropBox ?? mediaBox, cropBox != null);

                // TrimBox（如果未定义，默认等于CropBox）
                pageInfo.TrimBox = ConvertRectangleToBoxDimension(trimBox ?? cropBox ?? mediaBox, trimBox != null);

                // BleedBox（如果未定义，默认等于CropBox）
                pageInfo.BleedBox = ConvertRectangleToBoxDimension(bleedBox ?? cropBox ?? mediaBox, bleedBox != null);

                // ArtBox（如果未定义，默认等于CropBox）
                pageInfo.ArtBox = ConvertRectangleToBoxDimension(artBox ?? cropBox ?? mediaBox, artBox != null);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"获取页面框信息失败 - 页码: {pageNumber}, 错误: {ex.Message}");
            }

            return pageInfo;
        }

        /// <summary>
        /// 将iText Rectangle转换为BoxDimension
        /// </summary>
        private BoxDimension ConvertRectangleToBoxDimension(Rectangle rect, bool isDefined)
        {
            if (rect == null)
            {
                return new BoxDimension { IsDefined = false };
            }

            return new BoxDimension
            {
                IsDefined = isDefined,
                Left = rect.GetLeft(),
                Bottom = rect.GetBottom(),
                Right = rect.GetRight(),
                Top = rect.GetTop()
            };
        }

        /// <summary>
        /// 检测页面框问题
        /// </summary>
        private List<PageBoxIssue> DetectIssues(List<PageBoxInfo> allPages)
        {
            var issues = new List<PageBoxIssue>();

            if (allPages == null || allPages.Count == 0)
                return issues;

            // 检查每一页
            foreach (var page in allPages)
            {
                // 检查MediaBox是否有效
                if (!page.MediaBox.IsDefined || page.MediaBox.Width <= 0 || page.MediaBox.Height <= 0)
                {
                    issues.Add(new PageBoxIssue
                    {
                        PageNumber = page.PageNumber,
                        Type = IssueType.InvalidSize,
                        Severity = IssueSeverity.Error,
                        BoxType = "MediaBox",
                        Description = $"第{page.PageNumber}页: MediaBox尺寸无效 ({page.MediaBox.Width}×{page.MediaBox.Height})"
                    });
                }

                // 检查CropBox是否超出MediaBox
                if (page.CropBox.IsDefined && page.MediaBox.IsDefined)
                {
                    if (!IsBoxWithinBounds(page.CropBox, page.MediaBox))
                    {
                        issues.Add(new PageBoxIssue
                        {
                            PageNumber = page.PageNumber,
                            Type = IssueType.OutOfBounds,
                            Severity = IssueSeverity.Warning,
                            BoxType = "CropBox",
                            Description = $"第{page.PageNumber}页: CropBox超出MediaBox范围"
                        });
                    }
                }

                // 检查TrimBox是否超出CropBox
                if (page.TrimBox.IsDefined && page.CropBox.IsDefined)
                {
                    if (!IsBoxWithinBounds(page.TrimBox, page.CropBox))
                    {
                        issues.Add(new PageBoxIssue
                        {
                            PageNumber = page.PageNumber,
                            Type = IssueType.OutOfBounds,
                            Severity = IssueSeverity.Warning,
                            BoxType = "TrimBox",
                            Description = $"第{page.PageNumber}页: TrimBox超出CropBox范围"
                        });
                    }
                }

                // 检查BleedBox是否在TrimBox和CropBox之间
                if (page.BleedBox.IsDefined && page.TrimBox.IsDefined && page.CropBox.IsDefined)
                {
                    if (page.BleedBox.Width < page.TrimBox.Width || page.BleedBox.Height < page.TrimBox.Height)
                    {
                        issues.Add(new PageBoxIssue
                        {
                            PageNumber = page.PageNumber,
                            Type = IssueType.IncorrectOrder,
                            Severity = IssueSeverity.Warning,
                            BoxType = "BleedBox",
                            Description = $"第{page.PageNumber}页: BleedBox应该大于或等于TrimBox"
                        });
                    }
                }
            }

            // 检查页面尺寸一致性
            if (allPages.Count > 1)
            {
                var firstPageCropBox = allPages[0].CropBox;
                var inconsistentPages = allPages.Skip(1)
                    .Where(p => !IsSameDimension(p.CropBox, firstPageCropBox))
                    .ToList();

                if (inconsistentPages.Any())
                {
                    issues.Add(new PageBoxIssue
                    {
                        PageNumber = 0, // 表示多页问题
                        Type = IssueType.InconsistentSize,
                        Severity = IssueSeverity.Info,
                        BoxType = "CropBox",
                        Description = $"文档包含不同尺寸的页面 (共{inconsistentPages.Count + 1}种尺寸)"
                    });
                }

                // 检查页面方向一致性
                var firstPageOrientation = firstPageCropBox.Width > firstPageCropBox.Height ? "横向" : "纵向";
                var inconsistentOrientations = allPages.Skip(1)
                    .Where(p => (p.CropBox.Width > p.CropBox.Height ? "横向" : "纵向") != firstPageOrientation)
                    .ToList();

                if (inconsistentOrientations.Any())
                {
                    issues.Add(new PageBoxIssue
                    {
                        PageNumber = 0,
                        Type = IssueType.InconsistentOrientation,
                        Severity = IssueSeverity.Info,
                        BoxType = "CropBox",
                        Description = $"文档包含不同方向的页面 (横向/纵向混合)"
                    });
                }
            }

            return issues;
        }

        /// <summary>
        /// 检查一个框是否在另一个框的范围内
        /// </summary>
        private bool IsBoxWithinBounds(BoxDimension inner, BoxDimension outer, double tolerance = 0.1)
        {
            return inner.Left >= outer.Left - tolerance &&
                   inner.Bottom >= outer.Bottom - tolerance &&
                   inner.Right <= outer.Right + tolerance &&
                   inner.Top <= outer.Top + tolerance;
        }

        /// <summary>
        /// 检查两个框的尺寸是否相同
        /// </summary>
        private bool IsSameDimension(BoxDimension box1, BoxDimension box2, double tolerance = 0.5)
        {
            return Math.Abs(box1.Width - box2.Width) < tolerance &&
                   Math.Abs(box1.Height - box2.Height) < tolerance;
        }

        /// <summary>
        /// 获取页面框的出血尺寸
        /// </summary>
        public BleedInfo GetBleedInfo(PageBoxInfo pageInfo)
        {
            var bleedInfo = new BleedInfo();

            if (pageInfo.TrimBox.IsDefined && pageInfo.BleedBox.IsDefined)
            {
                // 计算四边的出血
                bleedInfo.Left = Math.Round((pageInfo.TrimBox.Left - pageInfo.BleedBox.Left) / 72 * 25.4, 2);
                bleedInfo.Right = Math.Round((pageInfo.BleedBox.Right - pageInfo.TrimBox.Right) / 72 * 25.4, 2);
                bleedInfo.Top = Math.Round((pageInfo.BleedBox.Top - pageInfo.TrimBox.Top) / 72 * 25.4, 2);
                bleedInfo.Bottom = Math.Round((pageInfo.TrimBox.Bottom - pageInfo.BleedBox.Bottom) / 72 * 25.4, 2);

                bleedInfo.IsUniform = Math.Abs(bleedInfo.Left - bleedInfo.Right) < 0.1 &&
                                     Math.Abs(bleedInfo.Left - bleedInfo.Top) < 0.1 &&
                                     Math.Abs(bleedInfo.Left - bleedInfo.Bottom) < 0.1;

                if (bleedInfo.IsUniform)
                {
                    bleedInfo.UniformValue = bleedInfo.Left;
                }
            }

            return bleedInfo;
        }
        
        /// <summary>
        /// 保存页面框修改
        /// </summary>
        public bool SavePageBox(string filePath, PageBoxInfo newInfo, bool applyToAllPages)
        {
            try
            {
                string tempPath = filePath + ".tmp";
                
                int startPage = 0;
                int endPage = 0;
                
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfWriter writer = new PdfWriter(tempPath))
                using (PdfDocument document = new PdfDocument(reader, writer))
                {
                    startPage = applyToAllPages ? 1 : newInfo.PageNumber;
                    endPage = applyToAllPages ? document.GetNumberOfPages() : newInfo.PageNumber;

                    for (int i = startPage; i <= endPage; i++)
                    {
                        PdfPage page = document.GetPage(i);
                        
                        // Update MediaBox (Changes physical page size)
                        if (newInfo.MediaBox.IsDefined)
                         page.SetMediaBox(new Rectangle((float)newInfo.MediaBox.Left, (float)newInfo.MediaBox.Bottom, (float)newInfo.MediaBox.Width, (float)newInfo.MediaBox.Height));
                            
                        // Update CropBox (Visible region)
                        if (newInfo.CropBox.IsDefined)
                            page.SetCropBox(new Rectangle((float)newInfo.CropBox.Left, (float)newInfo.CropBox.Bottom, (float)newInfo.CropBox.Width, (float)newInfo.CropBox.Height));

                        // Update BleedBox
                        if (newInfo.BleedBox.IsDefined)
                            page.SetBleedBox(new Rectangle((float)newInfo.BleedBox.Left, (float)newInfo.BleedBox.Bottom, (float)newInfo.BleedBox.Width, (float)newInfo.BleedBox.Height));
                        
                        // Update TrimBox
                        if (newInfo.TrimBox.IsDefined)
                            page.SetTrimBox(new Rectangle((float)newInfo.TrimBox.Left, (float)newInfo.TrimBox.Bottom, (float)newInfo.TrimBox.Width, (float)newInfo.TrimBox.Height));
                        
                        // ArtBox skipped as usually not key for simple editing
                    }
                }

                // File swap
                if (File.Exists(filePath)) File.Delete(filePath);
                File.Move(tempPath, filePath);
                
                LogHelper.Info($"成功保存页面框修改: {filePath}, 应用到页码范围: {startPage}-{endPage}");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存页面框修改失败: {ex.Message}");
                if (File.Exists(filePath + ".tmp")) File.Delete(filePath + ".tmp");
                return false;
            }
        }
    }

    /// <summary>
    /// 出血信息
    /// </summary>
    public class BleedInfo
    {
        public double Left { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
        public bool IsUniform { get; set; }
        public double UniformValue { get; set; }

        public override string ToString()
        {
            if (IsUniform)
            {
                return $"{UniformValue} mm (统一)";
            }
            else
            {
                return $"上:{Top} 下:{Bottom} 左:{Left} 右:{Right} mm";
            }
        }
    }
}
