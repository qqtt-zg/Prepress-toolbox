using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class MaterialSelectFormV5CardsTests
    {
        public MaterialSelectFormV5CardsTests()
        {
            if (!AppSettings.IsInitialized)
            {
                AppSettings.Initialize(new FileLogger(Path.Combine(Path.GetTempPath(), "MaterialSelectFormV5CardsTests")));
            }
            AppSettings.MaterialFormPreviewExpanded = false;
        }

        [StaFact]
        public void V5RightStation_ShouldContainSevenCards_AndCorrectWidth()
        {
            AppSettings.MaterialFormPreviewExpanded = false;
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\TestFile_80x45mm.pdf",
                regexResult: "TestFile",
                opacity: 1.0,
                width: "80",
                height: "45",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "A08"))
            {
                var rightStation = form.Controls["pnlRightStation"] as Panel;
                Assert.NotNull(rightStation);
                Assert.Equal(MaterialSelectFormModern.STATION_WIDTH, rightStation.Width);

                var content = rightStation.Controls["pnlStationContent"] as Panel;
                Assert.NotNull(content);

                Assert.NotNull(content.Controls["pnlMaterialsCard"]);
                Assert.NotNull(content.Controls["pnlOutputPathCard"]);
                Assert.NotNull(content.Controls["pnlCraftMatrixCard"]);
                Assert.NotNull(content.Controls["tlpOrderParametersCard"]);
                Assert.NotNull(content.Controls["pnlImpositionDashboardCard"]);
                Assert.NotNull(content.Controls["presetBrowserCard"]);
                Assert.NotNull(content.Controls["pnlActionBar"]);

                // V5 重构后折叠条由 pnlCollapseBar 承担（自绘、双缓冲），
                // 旧的 previewCollapseButton 已废弃，仅保留在窗体上并隐藏。
                Assert.NotNull(rightStation.Controls["pnlCollapseBar"]);
                Assert.NotNull(rightStation.Controls["pdfPreviewPanel"]);
            }
        }

        [StaFact]
        public void TogglePreview_ShouldPreserveStationContentPosition_AndNotCoverContent()
        {
            AppSettings.MaterialFormPreviewExpanded = false;
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\TestFile_80x45mm.pdf",
                regexResult: "TestFile",
                opacity: 1.0,
                width: "80",
                height: "45",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "A08"))
            {
                var handle = form.Handle;
                var rightStation = form.Controls["pnlRightStation"] as Panel;
                var content = rightStation.Controls["pnlStationContent"] as Panel;
                var collapseBtn = rightStation.Controls["pnlCollapseBar"];
                var previewPanel = rightStation.Controls["pdfPreviewPanel"];
                Assert.NotNull(collapseBtn);
                Assert.NotNull(previewPanel);

                // 初始状态（常态折叠）
                Assert.Equal(MaterialSelectFormModern.BASE_CLIENT_HEIGHT, form.ClientSize.Height); // 508
                Assert.Equal(MaterialSelectFormModern.BASE_CLIENT_HEIGHT, rightStation.Height); // 508
                Assert.Equal(26, content.Top);
                Assert.Equal(466, content.Height); // 7张卡片高度固定
                Assert.Equal(492, collapseBtn.Top); // 508 - 16 = 492
                Assert.Equal(0, previewPanel.Height); // 折叠时高度为0

                // 1. 触发展开
                form.TogglePreviewState(true);
                Assert.Equal(MaterialSelectFormModern.EXPANDED_CLIENT_HEIGHT, form.ClientSize.Height); // 753
                Assert.Equal(MaterialSelectFormModern.EXPANDED_CLIENT_HEIGHT, rightStation.Height); // 753
                Assert.Equal(26, content.Top); // 内容区绝不移动！
                Assert.Equal(466, content.Height); // 内容区绝不压缩！
                Assert.Equal(492, collapseBtn.Top); // 折叠按钮位置固定！
                Assert.Equal(508, previewPanel.Top); // 预览面板从 508 处向下展开
                Assert.Equal(245, previewPanel.Height); // 展开时高度精确为 245

                // 2. 再次关闭折叠
                form.TogglePreviewState(false);
                Assert.Equal(MaterialSelectFormModern.BASE_CLIENT_HEIGHT, form.ClientSize.Height); // 回到 508
                Assert.Equal(MaterialSelectFormModern.BASE_CLIENT_HEIGHT, rightStation.Height); // 回到 508
                Assert.Equal(26, content.Top); // 内容区依然在 26！绝不向上收缩！
                Assert.Equal(466, content.Height); // 内容区依然是 466！
                Assert.Equal(492, collapseBtn.Top); // 折叠按钮贴合 508 底边！
                Assert.Equal(0, previewPanel.Height); // 折叠时高度恢复为0
            }
        }

        [StaFact]
        public void ToggleBatchFileListPanel_ShouldSwitchWidthAccurately()
        {
            AppSettings.MaterialFormPreviewExpanded = false;
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\TestFile_80x45mm.pdf",
                regexResult: "TestFile",
                opacity: 1.0,
                width: "80",
                height: "45",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "A08"))
            {
                var handle = form.Handle;
                form.Visible = true;
                var rightStation = form.Controls["pnlRightStation"] as Panel;

                // 初始状态（单工位模式）
                Assert.Equal(MaterialSelectFormModern.STATION_WIDTH, form.ClientSize.Width); // 360
                Assert.Equal(0, rightStation.Left);

                // 1. 展开批量列表
                form.ToggleBatchFileListPanel(true);
                var pnlFileList = form.Controls["pnlFileList"] as Panel;
                Assert.NotNull(pnlFileList);
                Assert.True(pnlFileList.Visible);
                Assert.Equal(MaterialSelectFormModern.BATCH_PANEL_WIDTH, pnlFileList.Width); // 520
                Assert.Equal(MaterialSelectFormModern.WORKBENCH_WIDTH, form.ClientSize.Width); // 880
                Assert.Equal(MaterialSelectFormModern.BATCH_PANEL_WIDTH, rightStation.Left); // 右侧工位位于 520 处
                Assert.Equal(MaterialSelectFormModern.STATION_WIDTH, rightStation.Width); // 右侧工位宽度保持 360

                // 2. 收回单工位模式
                form.ToggleBatchFileListPanel(false);
                Assert.False(pnlFileList.Visible);
                Assert.Equal(MaterialSelectFormModern.STATION_WIDTH, form.ClientSize.Width); // 360
                Assert.Equal(0, rightStation.Left); // 右侧工位归零
                Assert.Equal(MaterialSelectFormModern.STATION_WIDTH, rightStation.Width); // 360
            }
        }

        [Fact]
        public void InplaceRoundRadius_ShouldSwitchVisibility_AndPersistValues()
        {
            AppSettings.MaterialFormPreviewExpanded = false;
            AppSettings.Set("LastSelectedShape", "RightAngle");
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\TestFile_80x45mm.pdf",
                regexResult: "TestFile",
                opacity: 1.0,
                width: "80",
                height: "45",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "A08"))
            {
                var handle = form.Handle;
                form.Visible = true;

                // 初始默认直角，圆角按钮为可见，原地输入面板隐藏
                var slot = form.Controls.Find("pnlRoundRectSlot", true)[0] as Panel;
                var inputPanel = form.Controls.Find("pnlInplaceRoundInput", true)[0] as Panel;
                var roundBtn = form.Controls.Find("shapeRoundRectButton", true)[0] as AntdUI.Button;
                var radiusBox = form.Controls.Find("txtInplaceRoundRadius", true)[0] as TextBox;

                Assert.NotNull(slot);
                Assert.NotNull(inputPanel);
                Assert.NotNull(roundBtn);
                Assert.NotNull(radiusBox);

                Assert.True(roundBtn.Visible);
                Assert.False(inputPanel.Visible);

                // 1. 选中圆角矩形
                form.SelectShape(ShapeType.RoundRect);
                Assert.Equal(ShapeType.RoundRect, form.SelectedShape);
                Assert.False(roundBtn.Visible);
                Assert.True(inputPanel.Visible);

                // 2. 在原地输入框中输入 "8"
                radiusBox.Text = "8";
                Assert.Equal(8.0, form.RoundRadius);
                Assert.Equal("8", AppSettings.GetValue<string>("LastRoundRadius"));

                // 3. 切换回直角矩形
                form.SelectShape(ShapeType.RightAngle);
                Assert.Equal(ShapeType.RightAngle, form.SelectedShape);
                Assert.True(roundBtn.Visible);
                Assert.False(inputPanel.Visible);
                Assert.Equal(0, form.RoundRadius);
                // 上次设置的圆角值依然持久化记忆
                Assert.Equal("8", AppSettings.GetValue<string>("LastRoundRadius"));

                // 4. 再次点击圆角矩形，恢复记忆的 8
                form.SelectShape(ShapeType.RoundRect);
                Assert.Equal(ShapeType.RoundRect, form.SelectedShape);
                Assert.False(roundBtn.Visible);
                Assert.True(inputPanel.Visible);
                Assert.Equal(8.0, form.RoundRadius);
                Assert.Equal("8", radiusBox.Text);
            }
        }

        [Fact]
        public void LoadPreset_WithMaterialEnabled_ShouldSelectMaterialButtonAndProperty()
        {
            AppSettings.MaterialFormPreviewExpanded = false;
            var testPreset = new MaterialSelectionPreset
            {
                Name = "测试预设_ulyy",
                SelectedMaterial = "ulyy",
                DisabledOptions = PresetIgnoreOptions.None, // 可更改材料
                ShowInPresetButtons = true,
                GroupName = "未命名"
            };
            AppSettings.MaterialPresets = new List<MaterialSelectionPreset> { testPreset };

            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "ulyy", "50#yy", "100#yy" },
                fileName: @"C:\test\TestFile_80x45mm.pdf",
                regexResult: "TestFile",
                opacity: 1.0,
                width: "80",
                height: "45",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "A08"))
            {
                var handle = form.Handle;
                form.Visible = true;

                // 初始先选中 50#yy
                form.SelectMaterialByName("50#yy");
                Assert.Equal("50#yy", form.SelectedMaterial);

                // 应用预设 "测试预设_ulyy"
                form.LoadPreset("测试预设_ulyy");

                // 验证材料成功变为 ulyy
                Assert.Equal("ulyy", form.SelectedMaterial);

                // 验证按钮视觉高亮也变为了 Primary
                var petBtn = form.Controls.Find("materialButton1", true)[0] as AntdUI.Button;
                Assert.NotNull(petBtn);
                Assert.Equal("ulyy", petBtn.Text);
                Assert.Equal(AntdUI.TTypeMini.Primary, petBtn.Type);
            }
        }

        [Fact]
        public void LoadPresetButtons_ShouldGroupUnderUnnamed_AndFilterCorrectly()
        {
            AppSettings.MaterialFormPreviewExpanded = false;
            var p1 = new MaterialSelectionPreset { Name = "预设A", ShowInPresetButtons = true, GroupName = "未命名" };
            var p2 = new MaterialSelectionPreset { Name = "预设B", ShowInPresetButtons = true, GroupName = "" }; // 留空应自动归入未命名
            AppSettings.MaterialPresets = new List<MaterialSelectionPreset> { p1, p2 };

            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\TestFile_80x45mm.pdf",
                regexResult: "TestFile",
                opacity: 1.0,
                width: "80",
                height: "45",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "A08"))
            {
                var handle = form.Handle;
                form.Visible = true;

                var tabs = form.Controls.Find("tabs1", true)[0] as AntdUI.Tabs;
                var panel = form.Controls.Find("presetButtonsPanel", true)[0] as FlowLayoutPanel;

                Assert.NotNull(tabs);
                Assert.NotNull(panel);

                // 验证标签页包含且仅有 "未命名"
                Assert.True(tabs.Pages.Count >= 1);
                Assert.Equal("未命名", tabs.Pages[0].Text);

                // 验证 panel 中包含两个按钮
                Assert.Equal(2, panel.Controls.Count);
                Assert.Equal("预设A", panel.Controls[0].Text);
                Assert.Equal("预设B", panel.Controls[1].Text);
            }
        }
    }
}
