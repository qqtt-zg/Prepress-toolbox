using System;
using Xunit;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Tests.Models
{
    public class MaterialSelectionPresetTests
    {
        [Fact]
        public void Clone_Should_Create_Deep_Copy_Including_DisabledOptions()
        {
            // Arrange
            var original = new MaterialSelectionPreset
            {
                Name = "Test Preset",
                SelectedMaterial = "PVC",
                TetBleed = 2.5,
                ColorMode = "黑白",
                FilmType = "哑膜",
                AddIdentifierPage = true,
                ShapeState = "RoundRect",
                IsDualCopy = true,
                ExportPath = @"C:\Test",
                RoundRadius = 5.0,
                MaterialType = "RollMaterial",
                LayoutMode = "Folding",
                EnableImposition = true,
                DisabledOptions = PresetIgnoreOptions.TetBleed | PresetIgnoreOptions.ExportPath
            };

            // Act
            var clone = original.Clone();

            // Assert
            Assert.NotSame(original, clone);
            Assert.Equal(original.Name, clone.Name);
            Assert.Equal(original.SelectedMaterial, clone.SelectedMaterial);
            Assert.Equal(original.TetBleed, clone.TetBleed);
            Assert.Equal(original.ColorMode, clone.ColorMode);
            Assert.Equal(original.FilmType, clone.FilmType);
            Assert.Equal(original.AddIdentifierPage, clone.AddIdentifierPage);
            Assert.Equal(original.ShapeState, clone.ShapeState);
            Assert.Equal(original.IsDualCopy, clone.IsDualCopy);
            Assert.Equal(original.ExportPath, clone.ExportPath);
            Assert.Equal(original.RoundRadius, clone.RoundRadius);
            Assert.Equal(original.MaterialType, clone.MaterialType);
            Assert.Equal(original.LayoutMode, clone.LayoutMode);
            Assert.Equal(original.EnableImposition, clone.EnableImposition);
            Assert.Equal(original.DisabledOptions, clone.DisabledOptions);
        }

        [Fact]
        public void CopyFrom_Should_Copy_All_Properties_Including_DisabledOptions()
        {
            // Arrange
            var target = new MaterialSelectionPreset
            {
                Name = "Old Name",
                SelectedMaterial = "Old Material",
                DisabledOptions = PresetIgnoreOptions.None
            };

            var source = new MaterialSelectionPreset
            {
                Name = "New Name",
                SelectedMaterial = "New Material",
                TetBleed = 3.0,
                ColorMode = "彩色",
                FilmType = "光膜",
                AddIdentifierPage = false,
                ShapeState = "Circle",
                IsDualCopy = false,
                ExportPath = @"D:\Output",
                RoundRadius = 10.0,
                MaterialType = "FlatSheet",
                LayoutMode = "Continuous",
                EnableImposition = false,
                DisabledOptions = PresetIgnoreOptions.ColorMode | PresetIgnoreOptions.Shape
            };

            // Act
            target.CopyFrom(source);

            // Assert
            Assert.Equal(source.Name, target.Name);
            Assert.Equal(source.SelectedMaterial, target.SelectedMaterial);
            Assert.Equal(source.TetBleed, target.TetBleed);
            Assert.Equal(source.ColorMode, target.ColorMode);
            Assert.Equal(source.FilmType, target.FilmType);
            Assert.Equal(source.AddIdentifierPage, target.AddIdentifierPage);
            Assert.Equal(source.ShapeState, target.ShapeState);
            Assert.Equal(source.IsDualCopy, target.IsDualCopy);
            Assert.Equal(source.ExportPath, target.ExportPath);
            Assert.Equal(source.RoundRadius, target.RoundRadius);
            Assert.Equal(source.MaterialType, target.MaterialType);
            Assert.Equal(source.LayoutMode, target.LayoutMode);
            Assert.Equal(source.EnableImposition, target.EnableImposition);
            Assert.Equal(source.DisabledOptions, target.DisabledOptions);
        }
    }
}
