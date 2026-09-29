using System;
using System.Collections.Generic;
using System.IO;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Xunit;

namespace WindowsFormsApp3.Tests.Services
{
    public class ApplicationSettingsBackupTests
    {
        [Fact]
        public void SaveSettings_ShouldCreateBakMirror_AndDailySnapshot()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "CodexBackupTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", tempDir);

            try
            {
                var logger = new FileLogger(Path.Combine(tempDir, "test.log"));
                var service = new ApplicationSettingsService(logger);

                var settings = new ApplicationSettingsService.ApplicationSettings();
                settings.MaterialPresets.Add(new MaterialSelectionPreset { Name = "测试预设1", GroupName = "测试组" });
                settings.MaterialPresetGroups.Add("测试组");

                service.SaveSettingsInternal(settings);

                var mainFile = Path.Combine(tempDir, "application_settings.json");
                Assert.True(File.Exists(mainFile), "主配置文件必须存在");

                // 第二次保存应生成 .bak 和 Backups 快照
                settings.LastSelectedPresetGroup = "测试组";
                service.SaveSettingsInternal(settings);

                var bakFile = mainFile + ".bak";
                Assert.True(File.Exists(bakFile), "必须生成 .bak 镜像文件");

                var backupDir = Path.Combine(tempDir, "Backups");
                Assert.True(Directory.Exists(backupDir), "必须生成 Backups 目录");

                var todaySnapshot = Path.Combine(backupDir, $"settings_{DateTime.Now:yyyy-MM-dd}.json");
                Assert.True(File.Exists(todaySnapshot), "必须生成每日快照");
            }
            finally
            {
                Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", null);
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void LoadSettings_ShouldAutoRecover_WhenMainFileIsCorrupted()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "CodexRecoveryTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", tempDir);

            try
            {
                var logger = new FileLogger(Path.Combine(tempDir, "test.log"));
                var service = new ApplicationSettingsService(logger);

                var settings = new ApplicationSettingsService.ApplicationSettings();
                settings.MaterialPresets.Add(new MaterialSelectionPreset { Name = "珍贵预设", GroupName = "重要组" });
                service.SaveSettingsInternal(settings);

                var mainFile = Path.Combine(tempDir, "application_settings.json");
                var bakFile = mainFile + ".bak";

                // 人为破坏主文件（写入非法损坏数据）
                File.WriteAllText(mainFile, "{ this is corrupted broken json !!!");

                // 重新加载，应该从 .bak 自动自愈恢复！
                var recoveredService = new ApplicationSettingsService(logger);
                var recoveredSettings = recoveredService.LoadSettingsInternal();

                Assert.NotNull(recoveredSettings);
                Assert.Single(recoveredSettings.MaterialPresets);
                Assert.Equal("珍贵预设", recoveredSettings.MaterialPresets[0].Name);
            }
            finally
            {
                Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", null);
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void SaveSettings_ShouldCreateEmergencyBackup_WhenPresetsSuddenlyEmptied()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "CodexEmergencyTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", tempDir);

            try
            {
                var logger = new FileLogger(Path.Combine(tempDir, "test.log"));
                var service = new ApplicationSettingsService(logger);

                var settings = new ApplicationSettingsService.ApplicationSettings();
                settings.MaterialPresets.Add(new MaterialSelectionPreset { Name = "原有预设", GroupName = "重要组" });
                service.SaveSettingsInternal(settings);

                // 模拟外部意外bug将预设清空
                settings.MaterialPresets.Clear();
                service.SaveSettingsInternal(settings);

                // 验证在 Backups 目录下自动生成了 emergency_pre_empty_presets_ 开头的防空紧急快照
                var backupDir = Path.Combine(tempDir, "Backups");
                var emergencyFiles = Directory.GetFiles(backupDir, "emergency_pre_empty_presets_*.json");
                Assert.NotEmpty(emergencyFiles);
            }
            finally
            {
                Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", null);
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void TemporaryImpositionParameters_ShouldSupportIndependentFlatAndRollActivation()
        {
            // 测试平张与卷装分开独立启动
            var tip = new TemporaryImpositionParameters
            {
                IsPaperSizeUnlocked = true,
                PaperWidth = 325,
                PaperHeight = 470,
                MarginTop = 15,
                MarginBottom = 15,
                MarginLeft = 5,
                MarginRight = 5,
                // 卷装未启动
                IsRollSizeUnlocked = false,
                FixedWidth = 0,
                MinLength = 0
            };

            Assert.True(tip.IsPaperSizeUnlocked);
            Assert.False(tip.IsRollSizeUnlocked);
            Assert.Equal(325f, tip.PaperWidth);
            Assert.Equal(0f, tip.FixedWidth);

            var cloned = tip.Clone();
            Assert.True(cloned.IsPaperSizeUnlocked);
            Assert.False(cloned.IsRollSizeUnlocked);
            Assert.Equal(325f, cloned.PaperWidth);
            Assert.Equal(0f, cloned.FixedWidth);
        }
    }
}
