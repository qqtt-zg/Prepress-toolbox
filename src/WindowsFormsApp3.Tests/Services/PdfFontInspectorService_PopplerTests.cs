using System;
using System.IO;
using Xunit;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Tests.Services
{
    public class PdfFontInspectorService_PopplerTests
    {
        private readonly PdfFontInspectorService_Poppler _service;
        private readonly string _testPdfPath;

        public PdfFontInspectorService_PopplerTests()
        {
            _service = new PdfFontInspectorService_Poppler();
            
            // Locate the test file from PdfiumViewer.Test
            string solutionRoot = FindSolutionRoot(AppDomain.CurrentDomain.BaseDirectory);
            _testPdfPath = Path.Combine(solutionRoot, "src", "PdfiumViewer", "PdfiumViewer.Test", "Example1.pdf");
        }

        private string FindSolutionRoot(string currentPath)
        {
            var dir = new DirectoryInfo(currentPath);
            while (dir != null && dir.GetFiles("WindowsFormsApp3.sln").Length == 0)
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? throw new FileNotFoundException("Could not find solution root");
        }

        [Fact]
        public void IsPdffontsAvailable_Should_Return_True()
        {
            // Act
            bool available = _service.IsPdffontsAvailable();

            // Assert
            Assert.True(available, "pdffonts.exe should be available in the test environment");
        }

        [Fact]
        public void InspectFonts_Should_Return_FontInfo_For_Valid_Pdf()
        {
            // Arrange
            if (!File.Exists(_testPdfPath))
            {
                // Fallback for CI/CD or if file moved, create a dummy empty file to at least test file existence check
                // But Poppler needs a valid PDF. 
                // We'll skip if not found, but we expect it to be there based on glob result.
                Assert.True(false, $"Test PDF file not found at: {_testPdfPath}");
            }

            // Act
            var info = _service.InspectFonts(_testPdfPath);

            // Assert
            Assert.NotNull(info);
            Assert.Equal(_testPdfPath, info.FilePath);
            
            // We assume Example1.pdf has some content. 
            // Even if it has no fonts, the tool should run successfully.
            // But usually PDF has at least one font or the tool returns "0 fonts".
            // The key is it shouldn't crash or return null.
            Assert.NotNull(info.Fonts);
            
            // Check logs (via console output usually) or just verifying IsPdffontsAvailable was used
        }

        [Fact]
        public void GetPdffontsVersion_Should_Return_Version_String()
        {
            // Act
            string version = _service.GetPdffontsVersion();

            // Assert
            Assert.NotNull(version);
            Assert.Contains("pdffonts version", version);
        }
    }
}
