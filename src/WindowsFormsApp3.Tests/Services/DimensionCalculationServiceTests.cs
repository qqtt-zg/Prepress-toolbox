using System;
using System.IO;
using Xunit;
using Moq;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Interfaces;

namespace WindowsFormsApp3.Tests.Services
{
    /// <summary>
    /// DimensionCalculationService 单元测试
    /// </summary>
    public class DimensionCalculationServiceTests : IDisposable
    {
        private readonly Mock<IPdfDimensionService> _mockPdfDimensionService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly DimensionCalculationService _service;
        private readonly string _testDirectory;

        public DimensionCalculationServiceTests()
        {
            _mockPdfDimensionService = new Mock<IPdfDimensionService>();
            _mockLogger = new Mock<ILogger>();
            _service = new DimensionCalculationService(_mockPdfDimensionService.Object, _mockLogger.Object);
            
            // 创建测试目录
            _testDirectory = Path.Combine(Path.GetTempPath(), "DimensionCalcTests_" + DateTime.Now.Ticks.ToString());
            Directory.CreateDirectory(_testDirectory);
        }

        #region RecognizePdfDimensions Tests

        [Fact]
        public void RecognizePdfDimensions_Should_Return_False_When_Path_Is_Null()
        {
            // Act
            bool result = _service.RecognizePdfDimensions(null, out double width, out double height);

            // Assert
            Assert.False(result);
            Assert.Equal(0, width);
            Assert.Equal(0, height);
        }

        [Fact]
        public void RecognizePdfDimensions_Should_Return_False_When_Path_Is_Empty()
        {
            // Act
            bool result = _service.RecognizePdfDimensions(string.Empty, out double width, out double height);

            // Assert
            Assert.False(result);
            Assert.Equal(0, width);
            Assert.Equal(0, height);
        }

        [Fact]
        public void RecognizePdfDimensions_Should_Return_False_When_File_Not_Exists()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_testDirectory, "nonexistent.pdf");

            // Act
            bool result = _service.RecognizePdfDimensions(nonExistentPath, out double width, out double height);

            // Assert
            Assert.False(result);
            Assert.Equal(0, width);
            Assert.Equal(0, height);
        }

        [Fact]
        public void RecognizePdfDimensions_Should_Return_True_When_Service_Succeeds()
        {
            // Arrange
            string testFilePath = Path.Combine(_testDirectory, "test.pdf");
            File.WriteAllText(testFilePath, "dummy content");
            
            _mockPdfDimensionService
                .Setup(s => s.GetFirstPageSize(testFilePath, out It.Ref<double>.IsAny, out It.Ref<double>.IsAny, true))
                .Returns((string path, out double w, out double h, bool useCropBox) =>
                {
                    w = 210.0;
                    h = 297.0;
                    return true;
                });

            // Act
            bool result = _service.RecognizePdfDimensions(testFilePath, out double width, out double height);

            // Assert
            Assert.True(result);
            Assert.Equal(210.0, width);
            Assert.Equal(297.0, height);
        }

        [Fact]
        public void RecognizePdfDimensions_Should_Return_False_When_Service_Fails()
        {
            // Arrange
            string testFilePath = Path.Combine(_testDirectory, "invalid.pdf");
            File.WriteAllText(testFilePath, "invalid");
            
            _mockPdfDimensionService
                .Setup(s => s.GetFirstPageSize(testFilePath, out It.Ref<double>.IsAny, out It.Ref<double>.IsAny, It.IsAny<bool>()))
                .Returns(false);

            // Act
            bool result = _service.RecognizePdfDimensions(testFilePath, out double width, out double height);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CalculateFinalDimensions Tests

        [Theory]
        [InlineData(100, 50, 0, "100x50")] // 无出血值
        [InlineData(100, 50, 3, "94x44")]   // 3mm 出血值: 100-6=94, 50-6=44
        [InlineData(210, 297, 5, "200x287")] // A4 尺寸减去 5mm 出血: 210-10=200, 297-10=287
        [InlineData(84.5, 54.5, 2, "80.5x50.5")] // 小数尺寸: 84.5-4=80.5, 54.5-4=50.5
        public void CalculateFinalDimensions_Should_Calculate_Correct_Dimensions(
            double width, double height, double bleed, string expected)
        {
            // Act
            string result = _service.CalculateFinalDimensions(width, height, bleed);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CalculateFinalDimensions_Should_Not_Add_Shape_Code_When_AddPdfLayers_Is_False()
        {
            // Act
            string result = _service.CalculateFinalDimensions(100, 50, 3, "5", false);

            // Assert
            Assert.Equal("94x44", result); // 没有形状代号
        }

        [Theory]
        [InlineData("0", "94x44Z")]     // 输入 "0" 应添加 Z（直角）
        [InlineData("R", "94x44C")]     // 输入 "R" 应添加 C（正圆）
        [InlineData("Y", "94x44Y")]     // 输入 "Y" 应添加 Y（椭圆）
        public void CalculateFinalDimensions_Should_Add_Correct_Shape_Code(
            string cornerRadius, string expected)
        {
            // Arrange - 使用 AppSettings 默认值
            // 注意：这些测试假设 AppSettings 返回默认值

            // Act
            string result = _service.CalculateFinalDimensions(100, 50, 3, cornerRadius, true);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("5")]   // 5mm 圆角
        [InlineData("10")]  // 10mm 圆角
        [InlineData("15")]  // 15mm 圆角
        public void CalculateFinalDimensions_Should_Add_Round_Shape_Code_With_Radius(string radiusValue)
        {
            // Act
            string result = _service.CalculateFinalDimensions(100, 50, 3, radiusValue, true);

            // Assert
            // 默认圆角代号为 "R" 加上数值
            Assert.StartsWith("94x44", result);
            Assert.Contains("R", result);
            Assert.Contains(radiusValue, result);
        }

        [Fact]
        public void CalculateFinalDimensions_Should_Handle_Empty_CornerRadius()
        {
            // Act
            string result = _service.CalculateFinalDimensions(100, 50, 3, "", true);

            // Assert
            Assert.Equal("94x44", result); // 空值不添加形状代号
        }

        [Fact]
        public void CalculateFinalDimensions_Should_Handle_Null_CornerRadius()
        {
            // Act
            string result = _service.CalculateFinalDimensions(100, 50, 3, null, true);

            // Assert
            Assert.Equal("94x44", result); // null 不添加形状代号
        }

        [Fact]
        public void CalculateFinalDimensions_Should_Round_To_One_Decimal()
        {
            // Arrange - 使用会产生多位小数的值
            double width = 100.36;
            double height = 50.44;
            double bleed = 3;
            // 100.36 - 6 = 94.36 -> 四舍五入到 94.4
            // 50.44 - 6 = 44.44 -> 四舍五入到 44.4

            // Act
            string result = _service.CalculateFinalDimensions(width, height, bleed);

            // Assert
            Assert.Equal("94.4x44.4", result);
        }

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_PdfDimensionService_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new DimensionCalculationService(null, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_Should_Allow_Null_Logger()
        {
            // Act
            var service = new DimensionCalculationService(_mockPdfDimensionService.Object, null);

            // Assert
            Assert.NotNull(service);
        }

        #endregion

        public void Dispose()
        {
            // 清理测试目录
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, true);
                }
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }
}
