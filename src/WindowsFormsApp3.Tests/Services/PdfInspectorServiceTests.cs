using System;
using System.IO;
using System.Linq;
using Xunit;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Tests.Services
{
    /// <summary>
    /// PDF检查器服务测试
    /// </summary>
    public class PdfInspectorServiceTests
    {
        private readonly PdfInspectorService _service;

        public PdfInspectorServiceTests()
        {
            _service = new PdfInspectorService();
        }

        [Fact]
        public void InspectPdf_Should_Return_Valid_Info_For_Valid_Pdf()
        {
            // Arrange
            string testPdfPath = GetTestPdfPath();
            if (!File.Exists(testPdfPath))
            {
                // 跳过测试如果测试文件不存在
                return;
            }

            // Act
            var info = _service.InspectPdf(testPdfPath);

            // Assert
            Assert.NotNull(info);
            Assert.Equal(testPdfPath, info.FilePath);
            Assert.True(info.TotalPages > 0);
            Assert.NotNull(info.CurrentPageBoxes);
            Assert.NotNull(info.AllPageBoxes);
            Assert.Equal(info.TotalPages, info.AllPageBoxes.Count);
        }

        [Fact]
        public void InspectPdf_Should_Handle_Non_Existent_File()
        {
            // Arrange
            string nonExistentPath = "non_existent_file.pdf";

            // Act
            var info = _service.InspectPdf(nonExistentPath);

            // Assert
            Assert.NotNull(info);
            Assert.Equal(0, info.TotalPages);
            Assert.Empty(info.AllPageBoxes);
        }

        [Fact]
        public void GetBleedInfo_Should_Calculate_Bleed_Correctly()
        {
            // Arrange
            var pageInfo = new PageBoxInfo
            {
                PageNumber = 1,
                TrimBox = new BoxDimension
                {
                    IsDefined = true,
                    Left = 8.5, // 3mm in points
                    Bottom = 8.5,
                    Right = 208.5, // 200 + 8.5
                    Top = 208.5
                },
                BleedBox = new BoxDimension
                {
                    IsDefined = true,
                    Left = 0,
                    Bottom = 0,
                    Right = 217, // 200 + 8.5 + 8.5
                    Top = 217
                }
            };

            // Act
            var bleedInfo = _service.GetBleedInfo(pageInfo);

            // Assert
            Assert.True(bleedInfo.IsUniform);
            Assert.True(Math.Abs(bleedInfo.UniformValue - 3.0) < 0.1); // 约3mm
        }

        [Fact]
        public void BoxDimension_Should_Convert_Units_Correctly()
        {
            // Arrange - 创建一个210x297mm (A4) 的页面框
            var box = new BoxDimension
            {
                IsDefined = true,
                Left = 0,
                Bottom = 0,
                Right = 595.28, // 210mm in points
                Top = 841.89    // 297mm in points
            };

            // Act & Assert - 毫米
            Assert.True(Math.Abs(box.WidthMm - 210) < 0.5);
            Assert.True(Math.Abs(box.HeightMm - 297) < 0.5);

            // Act & Assert - 英寸
            Assert.True(Math.Abs(box.WidthInch - 8.27) < 0.1);
            Assert.True(Math.Abs(box.HeightInch - 11.69) < 0.1);

            // Act & Assert - 点
            Assert.True(Math.Abs(box.Width - 595.28) < 0.1);
            Assert.True(Math.Abs(box.Height - 841.89) < 0.1);
        }

        [Fact]
        public void BoxDimension_GetFormattedSize_Should_Return_Correct_Format()
        {
            // Arrange
            var box = new BoxDimension
            {
                IsDefined = true,
                Left = 0,
                Bottom = 0,
                Right = 595.28,
                Top = 841.89
            };

            // Act
            string mmFormat = box.GetFormattedSize(MeasurementUnit.Millimeter);
            string inchFormat = box.GetFormattedSize(MeasurementUnit.Inch);
            string ptFormat = box.GetFormattedSize(MeasurementUnit.Point);

            // Assert
            Assert.Contains("mm", mmFormat);
            Assert.Contains("in", inchFormat);
            Assert.Contains("pt", ptFormat);
            Assert.Contains("×", mmFormat);
        }

        [Fact]
        public void PageBoxIssue_Should_Have_Correct_Properties()
        {
            // Arrange & Act
            var issue = new PageBoxIssue
            {
                PageNumber = 1,
                Type = IssueType.InvalidSize,
                Severity = IssueSeverity.Error,
                BoxType = "MediaBox",
                Description = "MediaBox尺寸无效"
            };

            // Assert
            Assert.Equal(1, issue.PageNumber);
            Assert.Equal(IssueType.InvalidSize, issue.Type);
            Assert.Equal(IssueSeverity.Error, issue.Severity);
            Assert.Equal("MediaBox", issue.BoxType);
            Assert.NotEmpty(issue.Description);
        }

        [Fact]
        public void PdfInspectorInfo_Should_Initialize_Collections()
        {
            // Arrange & Act
            var info = new PdfInspectorInfo();

            // Assert
            Assert.NotNull(info.AllPageBoxes);
            Assert.NotNull(info.Issues);
            Assert.Empty(info.AllPageBoxes);
            Assert.Empty(info.Issues);
        }

        [Theory]
        [InlineData(MeasurementUnit.Millimeter)]
        [InlineData(MeasurementUnit.Inch)]
        [InlineData(MeasurementUnit.Point)]
        public void BoxDimension_Should_Support_All_Units(MeasurementUnit unit)
        {
            // Arrange
            var box = new BoxDimension
            {
                IsDefined = true,
                Left = 0,
                Bottom = 0,
                Right = 100,
                Top = 100
            };

            // Act
            string formatted = box.GetFormattedSize(unit);

            // Assert
            Assert.NotEmpty(formatted);
            Assert.Contains("×", formatted);
        }

        [Fact]
        public void BleedInfo_ToString_Should_Format_Correctly()
        {
            // Arrange - 统一出血
            var uniformBleed = new BleedInfo
            {
                Left = 3.0,
                Right = 3.0,
                Top = 3.0,
                Bottom = 3.0,
                IsUniform = true,
                UniformValue = 3.0
            };

            // Act
            string uniformStr = uniformBleed.ToString();

            // Assert
            Assert.Contains("3", uniformStr);
            Assert.Contains("统一", uniformStr);

            // Arrange - 非统一出血
            var nonUniformBleed = new BleedInfo
            {
                Left = 3.0,
                Right = 5.0,
                Top = 3.0,
                Bottom = 3.0,
                IsUniform = false
            };

            // Act
            string nonUniformStr = nonUniformBleed.ToString();

            // Assert
            Assert.Contains("上", nonUniformStr);
            Assert.Contains("下", nonUniformStr);
            Assert.Contains("左", nonUniformStr);
            Assert.Contains("右", nonUniformStr);
        }

        /// <summary>
        /// 获取测试PDF文件路径
        /// </summary>
        private string GetTestPdfPath()
        {
            // 尝试多个可能的测试文件位置
            string[] possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFiles", "test.pdf"),
                Path.Combine(Directory.GetCurrentDirectory(), "TestFiles", "test.pdf"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "test.pdf")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // 如果都不存在，返回第一个路径（测试会被跳过）
            return possiblePaths[0];
        }
    }
}
