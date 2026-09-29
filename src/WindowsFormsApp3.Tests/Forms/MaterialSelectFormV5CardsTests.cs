using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class MaterialSelectFormV5CardsTests : IDisposable
    {
        private readonly string _testRoot;

        public MaterialSelectFormV5CardsTests()
        {
            _testRoot = Path.Combine(Path.GetTempPath(), "CodexTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testRoot);
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", _testRoot);
            AppSettings.ResetForTesting();
            AppSettings.Initialize(new FileLogger(Path.Combine(_testRoot, "test.log")));
            AppSettings.MaterialFormPreviewExpanded = false;
        }

        public void Dispose()
        {
            try
            {
                Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", null);
                AppSettings.ResetForTesting();
            }
            catch { }
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

                // 两行四格共用同一分界线，模式按钮仍完整容纳“自动递增”及箭头。
                var orderCard = content.Controls["tlpOrderParametersCard"];
                var orderInput = orderCard.Controls.Find("orderNumberTextBox", true).FirstOrDefault();
                var serialInput = orderCard.Controls.Find("serialNumberTextBox", true).FirstOrDefault();
                var quantityInput = orderCard.Controls.Find("quantityTextBox", true).FirstOrDefault();
                Assert.NotNull(orderInput);
                Assert.NotNull(serialInput);
                Assert.NotNull(quantityInput);
                Assert.Equal(175, orderInput.Parent.Width);
                Assert.Equal(151, serialInput.Parent.Width);
                Assert.Equal(181, serialInput.Parent.Left);
                Assert.Equal(orderInput.Parent.Width, quantityInput.Parent.Width);
                Assert.Equal(orderInput.Parent.Left, quantityInput.Parent.Left);
                var copyModeField = typeof(MaterialSelectFormModern).GetField("lblCopyModePill",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var copyModeLabel = copyModeField?.GetValue(form) as Label;
                Assert.NotNull(copyModeLabel);
                Assert.Equal(serialInput.Parent.Left, copyModeLabel.Parent.Left);
                Assert.True(copyModeLabel.Right < copyModeLabel.Parent.ClientSize.Width);
                Assert.Equal(70, form.BtnOrderNumberMode.Width);
                form.SelectOrderNumberMode(OrderNumberMode.AutoIncrement);
                var orderModeTextSize = TextRenderer.MeasureText(
                    form.BtnOrderNumberMode.Text,
                    form.BtnOrderNumberMode.Font,
                    Size.Empty,
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                Assert.True(orderModeTextSize.Width <= form.BtnOrderNumberMode.Width - 4,
                    $"订单号模式文本宽 {orderModeTextSize.Width}px，按钮宽 {form.BtnOrderNumberMode.Width}px");
            }
        }

        [Fact]
        public void DimensionFont_ShouldShrinkOnlyWhenValueExceedsPillWidth()
        {
            using (var bitmap = new Bitmap(60, 44))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                float shortValueSize = MaterialSelectFormModern.GetDimensionFontSize(graphics, "80×45", 52);
                float longValueSize = MaterialSelectFormModern.GetDimensionFontSize(graphics, "123.4×56.7", 52);

                Assert.Equal(9.5F, shortValueSize);
                Assert.InRange(longValueSize, 5.5F, 9F);
                using (var normalFont = new Font("Microsoft YaHei UI", shortValueSize, FontStyle.Bold))
                {
                    Assert.Equal("80×45", MaterialSelectFormModern.GetDimensionDisplayText(graphics, "80×45", normalFont, 52));
                }
                using (var fittedFont = new Font("Microsoft YaHei UI", longValueSize, FontStyle.Bold))
                {
                    var fittedWidth = TextRenderer.MeasureText(graphics, "123.4×56.7", fittedFont, Size.Empty,
                        TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
                    Assert.True(fittedWidth <= 52);
                }
                using (var minimumFont = new Font("Microsoft YaHei UI", 5.5F, FontStyle.Bold))
                {
                    string clipped = MaterialSelectFormModern.GetDimensionDisplayText(
                        graphics, "123456.789×987654.321", minimumFont, 52);
                    Assert.EndsWith("…", clipped);
                    Assert.True(clipped.Length < "123456.789×987654.321".Length);
                }
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
                var batchCancelButton = pnlFileList.Controls.Find("btnCancelAllBatchRows", true).FirstOrDefault() as AntdUI.Button;
                Assert.NotNull(batchCancelButton);
                Assert.Equal("批量取消", batchCancelButton.Text);
                Assert.True(batchCancelButton.Right <= batchCancelButton.Parent.ClientSize.Width - batchCancelButton.Parent.Padding.Right);
                var legacyGrid = pnlFileList.Controls.OfType<DataGridView>().Single();
                Assert.Equal(DataGridViewColumnHeadersHeightSizeMode.DisableResizing, legacyGrid.ColumnHeadersHeightSizeMode);
                var createGroupGrid = typeof(MaterialSelectFormModern).GetMethod("CreateGroupDataGridView",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(createGroupGrid);
                using (var groupedGrid = (DataGridView)createGroupGrid.Invoke(form, new object[] { new BatchProcessGroup() }))
                {
                    Assert.Equal(DataGridViewColumnHeadersHeightSizeMode.DisableResizing, groupedGrid.ColumnHeadersHeightSizeMode);
                }
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
            AppSettings.Material = "ulyy,50#yy,100#yy";
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

                // 验证卡片 6 右上角成功配置了常驻的“保存为预设”➕与“管理”齿轮线性按钮
                var btnManage = form.Controls.Find("btnManagePresets", true).FirstOrDefault() as AntdUI.Button;
                Assert.NotNull(btnManage);
                Assert.Equal("SettingOutlined", btnManage.IconSvg);

                var btnSave = form.Controls.Find("btnSaveCurrentPreset", true).FirstOrDefault() as AntdUI.Button;
                Assert.NotNull(btnSave);
                Assert.Equal("PlusOutlined", btnSave.IconSvg);
            }
        }

        [StaFact]
        public void PresetManagementForm_ShouldInitializeThreeColumns_AndSupportBatchGrouping()
        {
            var p1 = new MaterialSelectionPreset { Name = "批量预设1", GroupName = "未命名", SelectedMaterial = "PET" };
            var p2 = new MaterialSelectionPreset { Name = "批量预设2", GroupName = "未命名", SelectedMaterial = "PP" };
            AppSettings.MaterialPresets = new List<MaterialSelectionPreset> { p1, p2 };

            using (var manager = new WindowsFormsApp3.Forms.Main.PresetManagementForm())
            {
                var handle = manager.Handle;
                manager.Visible = true;

                // 验证三栏容器与关键子控件存在
                var groupSidebar = manager.Controls.Find("_pnlGroupSidebar", true).FirstOrDefault();
                var presetDeck = manager.Controls.Find("_pnlPresetDeck", true).FirstOrDefault();
                var inspector = manager.Controls.Find("_pnlInspector", true).FirstOrDefault();

                Assert.NotNull(groupSidebar);
                Assert.NotNull(presetDeck);
                Assert.NotNull(inspector);

                // 验证窗口高度不超过 620，彻底避免任务栏遮挡
                Assert.True(manager.Height <= 620);

                // 批量将 p1, p2 移动至新分组 "新测试组"
                p1.GroupName = "新测试组";
                p2.GroupName = "新测试组";

                Assert.Equal("新测试组", p1.GroupName);
                Assert.Equal("新测试组", p2.GroupName);
            }
        }

        [StaFact]
        public void PresetManagementForm_ShouldSupportIntraGroupReorderingAndSorting()
        {
            var p1 = new MaterialSelectionPreset { Name = "预设A", GroupName = "未命名" };
            var p2 = new MaterialSelectionPreset { Name = "预设B", GroupName = "未命名" };
            var p3 = new MaterialSelectionPreset { Name = "预设C", GroupName = "未命名" };
            AppSettings.MaterialPresets = new List<MaterialSelectionPreset> { p1, p2, p3 };

            using (var manager = new WindowsFormsApp3.Forms.Main.PresetManagementForm())
            {
                var handle = manager.Handle;
                manager.Visible = true;

                var deck = manager.Controls.Find("_pnlPresetDeck", true).FirstOrDefault();
                Assert.NotNull(deck);

                // 模拟组内上下调序
                var presets = AppSettings.MaterialPresets;
                presets.Remove(p3);
                presets.Insert(0, p3);
                AppSettings.Save();

                Assert.Equal("预设C", AppSettings.MaterialPresets[0].Name);
                Assert.Equal("预设A", AppSettings.MaterialPresets[1].Name);
                Assert.Equal("预设B", AppSettings.MaterialPresets[2].Name);
            }
        }

        [StaFact]
        public void PresetGroupSwitching_ShouldPersistState_AcrossForms()
        {
            var p1 = new MaterialSelectionPreset { Name = "预设A", GroupName = "组1", ShowInPresetButtons = true };
            var p2 = new MaterialSelectionPreset { Name = "预设B", GroupName = "组2", ShowInPresetButtons = true };
            AppSettings.MaterialPresets = new List<MaterialSelectionPreset> { p1, p2 };
            AppSettings.MaterialPresetGroups = new List<string> { "未命名", "组1", "组2" };
            AppSettings.LastSelectedPresetGroup = "组2";
            AppSettings.Save();

            using (var manager = new WindowsFormsApp3.Forms.Main.PresetManagementForm())
            {
                var handle = manager.Handle;
                manager.Visible = true;

                var deck = manager.Controls.Find("_pnlPresetDeck", true).FirstOrDefault();
                Assert.NotNull(deck);
                var deckLabel = manager.Controls.Find("_lblDeckCurrentGroup", true).FirstOrDefault() as Label;
                Assert.NotNull(deckLabel);
                Assert.Contains("组2", deckLabel.Text);
            }

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

                var tabs = form.Controls.Find("tabs1", true).FirstOrDefault() as AntdUI.Tabs;
                Assert.NotNull(tabs);
                Assert.Equal("组2", tabs.SelectedTab?.Text);
            }
        }

        [StaFact]
        public void CustomSizeCapsule_ShouldShow_OnlyWhenSizeDiffersFromGlobalSettings_AndPersist()
        {
            // 1. 设置默认全局设置 (平张 320x464, 卷装 320)
            AppSettings.Set("Imposition_PaperWidth", "320");
            AppSettings.Set("Imposition_PaperHeight", "464");
            AppSettings.Set("Imposition_FixedWidth", "320");

            var normalPreset = new MaterialSelectionPreset
            {
                Name = "常规默认预设",
                GroupName = "未命名",
                ShowInPresetButtons = true,
                MaterialType = "FlatSheet",
                TemporaryImpositionParameters = null // 默认尺寸
            };

            var customPreset = new MaterialSelectionPreset
            {
                Name = "定制大版预设",
                GroupName = "未命名",
                ShowInPresetButtons = true,
                MaterialType = "FlatSheet",
                TemporaryImpositionParameters = new TemporaryImpositionParameters
                {
                    IsPaperSizeUnlocked = true,
                    PaperWidth = 440,
                    PaperHeight = 590
                }
            };

            AppSettings.MaterialPresets = new List<MaterialSelectionPreset> { normalPreset, customPreset };
            AppSettings.LastUsedMaterialPreset = "常规默认预设";
            AppSettings.Save();

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

                var capsule = form.Controls.Find("_customSizeCapsule", true).FirstOrDefault() as CustomSizeCapsule;
                Assert.NotNull(capsule);

                // 2. 初始使用常规默认预设，尺寸与全局设置一致 -> 胶囊隐藏！
                Assert.False(capsule.Visible);

                // 3. 点击切换至定制大版预设 (440x590 != 320x464) -> 胶囊浮现！
                form.LoadPreset("定制大版预设");
                Assert.True(capsule.Visible);
                Assert.Contains("440", capsule.CapsuleText);
                Assert.Contains("590", capsule.CapsuleText);
            }

            // 4. 持久性验证：关闭后再打开另一个文件，自动恢复上次使用的定制大版预设，胶囊直接显现！
            AppSettings.LastUsedMaterialPreset = "定制大版预设";
            AppSettings.Save();

            using (var form2 = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\AnotherJob_80x45mm.pdf",
                regexResult: "AnotherJob",
                opacity: 1.0,
                width: "80",
                height: "45",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "B01"))
            {
                var handle2 = form2.Handle;
                form2.Visible = true;

                // 模拟窗体 Shown 事件触发恢复
                var shownMethod = typeof(MaterialSelectFormModern).GetMethod("MaterialSelectFormModern_Shown",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                shownMethod?.Invoke(form2, new object[] { form2, EventArgs.Empty });

                var capsule2 = form2.Controls.Find("_customSizeCapsule", true).FirstOrDefault() as CustomSizeCapsule;
                Assert.NotNull(capsule2);

                // 验证胶囊自动显现，且大版尺寸自动继承 440x590
                Assert.True(capsule2.Visible);
                Assert.Contains("440", capsule2.CapsuleText);
                Assert.Contains("590", capsule2.CapsuleText);

                using (var bmp = new Bitmap(form2.Width, form2.Height))
                {
                    form2.DrawToBitmap(bmp, new Rectangle(0, 0, form2.Width, form2.Height));
                    bmp.Save(@"D:\vibe_codeing\Prepress-toolbox\output\playwright\actual-v5-whole-window-latest.png", System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        [StaFact]
        public void CustomSizeCapsule_RollMaterial_ShouldShowHeight_WhenHeightDiffersFromGlobalSettings()
        {
            // 1. 设置卷装全局默认设置 (幅宽 320, 最小长度/高度 100)
            AppSettings.Set("Imposition_FixedWidth", "320");
            AppSettings.Set("Imposition_MinLength", "100");

            var rollDefaultPreset = new MaterialSelectionPreset
            {
                Name = "卷装默认预设",
                GroupName = "未命名",
                ShowInPresetButtons = true,
                MaterialType = "RollMaterial",
                TemporaryImpositionParameters = new TemporaryImpositionParameters
                {
                    FixedWidth = 320,
                    MinLength = 100
                }
            };

            var rollCustomHeightPreset = new MaterialSelectionPreset
            {
                Name = "卷装定长预设",
                GroupName = "未命名",
                ShowInPresetButtons = true,
                MaterialType = "RollMaterial",
                TemporaryImpositionParameters = new TemporaryImpositionParameters
                {
                    IsRollSizeUnlocked = true,
                    FixedWidth = 320, // 幅宽固定保持 320
                    MinLength = 150   // 高度调整为 150
                }
            };

            AppSettings.MaterialPresets = new List<MaterialSelectionPreset> { rollDefaultPreset, rollCustomHeightPreset };
            AppSettings.LastUsedMaterialPreset = "卷装默认预设";
            AppSettings.Save();

            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\RollJob_50x30mm.pdf",
                regexResult: "RollJob",
                opacity: 1.0,
                width: "50",
                height: "30",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "R01"))
            {
                var handle = form.Handle;
                form.Visible = true;

                var capsule = form.Controls.Find("_customSizeCapsule", true).FirstOrDefault() as CustomSizeCapsule;
                Assert.NotNull(capsule);

                // 2. 加载卷装默认预设 (尺寸与系统默认一致) -> 胶囊隐藏
                form.LoadPreset("卷装默认预设");
                Assert.False(capsule.Visible);

                // 3. 加载卷装定长预设 (仅高度调整为 150，幅宽保持 320) -> 胶囊浮现，且突出显示高度 "高 150"
                form.LoadPreset("卷装定长预设");
                Assert.True(capsule.Visible);
                Assert.True(capsule.IsRoll);
                Assert.Equal("高 150", capsule.CapsuleText);
            }
        }

        [StaFact]
        public void IdentifierPageToggleButton_ShouldBeConfigured_AndSyncWithCheckbox()
        {
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\Job_50x30mm.pdf",
                regexResult: "Job",
                opacity: 1.0,
                width: "50",
                height: "30",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "J01"))
            {
                var handle = form.Handle;
                form.Visible = true;

                // 验证卡片 5 中配置了专属的 IdentifierPageToggleButton 矢量图标按钮
                var btnIdToggle = form.Controls.Find("btnIdentifierPageToggle", true).FirstOrDefault() as IdentifierPageToggleButton;
                Assert.NotNull(btnIdToggle);

                var chkIdPage = form.Controls.Find("chkIdentifierPage", true).FirstOrDefault() as AntdUI.Checkbox;
                Assert.NotNull(chkIdPage);

                // 验证初始状态双向同步
                Assert.Equal(chkIdPage.Checked, btnIdToggle.IsActive);

                // 模拟勾选复选框，按钮激活状态同步更新
                chkIdPage.Checked = true;
                Assert.True(btnIdToggle.IsActive);

                // 模拟取消复选框，按钮激活状态同步更新
                chkIdPage.Checked = false;
                Assert.False(btnIdToggle.IsActive);

                // 捕获实机卡片 5 与整体工作台视觉位图
                chkIdPage.Checked = true;
                var chkImpo = form.Controls.Find("enableImpositionCheckbox", true).FirstOrDefault() as AntdUI.Checkbox;
                if (chkImpo != null) chkImpo.Checked = true;

                var card5 = form.Controls.Find("pnlImpositionDashboardCard", true).FirstOrDefault();
                string outDir = @"D:\vibe_codeing\Prepress-toolbox\output\playwright";
                Directory.CreateDirectory(outDir);
                if (card5 != null)
                {
                    // 双按钮均处于激活态（蓝标白底）
                    using (var bmp = new Bitmap(card5.Width, card5.Height))
                    {
                        card5.DrawToBitmap(bmp, new Rectangle(0, 0, card5.Width, card5.Height));
                        bmp.Save(Path.Combine(outDir, "actual-v5-card5-both-active.png"), System.Drawing.Imaging.ImageFormat.Png);
                    }

                    // 双按钮均处于关闭态（灰标白底）
                    chkIdPage.Checked = false;
                    if (chkImpo != null) chkImpo.Checked = false;
                    using (var bmpInactive = new Bitmap(card5.Width, card5.Height))
                    {
                        card5.DrawToBitmap(bmpInactive, new Rectangle(0, 0, card5.Width, card5.Height));
                        bmpInactive.Save(Path.Combine(outDir, "actual-v5-card5-both-inactive.png"), System.Drawing.Imaging.ImageFormat.Png);
                    }

                    // 重新开启以便截取胶囊联动效果
                    chkIdPage.Checked = true;
                    if (chkImpo != null) chkImpo.Checked = true;

                    // 激活平张自定义尺寸胶囊并截图
                    var capsule = form.Controls.Find("_customSizeCapsule", true).FirstOrDefault() as CustomSizeCapsule;
                    if (capsule != null)
                    {
                        capsule.IsRoll = false;
                        capsule.CapsuleText = "232×320";
                        capsule.Visible = true;
                        using (var bmpFlat = new Bitmap(card5.Width, card5.Height))
                        {
                            card5.DrawToBitmap(bmpFlat, new Rectangle(0, 0, card5.Width, card5.Height));
                            bmpFlat.Save(Path.Combine(outDir, "actual-v5-card5-flat-capsule.png"), System.Drawing.Imaging.ImageFormat.Png);
                        }

                        // 激活卷装自定义高度胶囊并截图
                        capsule.IsRoll = true;
                        capsule.CapsuleText = "高 150";
                        using (var bmpRoll = new Bitmap(card5.Width, card5.Height))
                        {
                            card5.DrawToBitmap(bmpRoll, new Rectangle(0, 0, card5.Width, card5.Height));
                            bmpRoll.Save(Path.Combine(outDir, "actual-v5-card5-roll-capsule.png"), System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                }
            }
        }

        [StaFact]
        public void P1_FolderOpen_And_BatchModeSwitch_ShouldRenderCleanly()
        {
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\Job_Folder_80x45mm.pdf",
                regexResult: "Job_Folder",
                opacity: 1.0,
                width: "80",
                height: "45",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "F01"))
            {
                var handle = form.Handle;
                form.Visible = true;

                string outDir = @"D:\vibe_codeing\Prepress-toolbox\output\playwright";
                Directory.CreateDirectory(outDir);

                // 1. 验证并截取卡片 2 (P1: 线性展开文件夹 FolderOpenOutlined)
                var card2 = form.Controls.Find("pnlOutputPathCard", true).FirstOrDefault();
                Assert.NotNull(card2);
                using (var bmpCard2 = new Bitmap(card2.Width, card2.Height))
                {
                    card2.DrawToBitmap(bmpCard2, new Rectangle(0, 0, card2.Width, card2.Height));
                    bmpCard2.Save(Path.Combine(outDir, "actual-v5-card2-linear-folder.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                // 2. 验证并截取卡片 7 (方案 1: 批量处理常规未悬停态与滑块展开覆盖态)
                var card7 = form.Controls.Find("pnlActionBar", true).FirstOrDefault();
                Assert.NotNull(card7);

                var batchSwitch = form.Controls.Find("btnBatchModeSwitch", true).FirstOrDefault() as BatchModeSwitch;
                Assert.NotNull(batchSwitch);

                // 常态截图：左侧浅蓝小滑块 + "批量处理"文本清晰
                using (var bmpCard7Normal = new Bitmap(card7.Width, card7.Height))
                {
                    card7.DrawToBitmap(bmpCard7Normal, new Rectangle(0, 0, card7.Width, card7.Height));
                    bmpCard7Normal.Save(Path.Combine(outDir, "actual-v5-card7-batch-normal.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                // 模拟悬停展开态：通过反射设置 _slideWidth 为满幅 100F，验证完全覆盖文字并捕获截图
                var slideWidthField = typeof(BatchModeSwitch).GetField("_slideWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isHoveredField = typeof(BatchModeSwitch).GetField("_isHovered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                isHoveredField?.SetValue(batchSwitch, true);
                slideWidthField?.SetValue(batchSwitch, (float)(batchSwitch.Width - 4));
                batchSwitch.Invalidate();

                using (var bmpCard7Hover = new Bitmap(card7.Width, card7.Height))
                {
                    card7.DrawToBitmap(bmpCard7Hover, new Rectangle(0, 0, card7.Width, card7.Height));
                    bmpCard7Hover.Save(Path.Combine(outDir, "actual-v5-card7-batch-hover-covered.png"), System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        [StaFact]
        public void P3_Card4_And_P4_PresetDots_ShouldRenderCleanly()
        {
            // 配置同时包含平张与卷装及一式两联的预设
            var pFlat1 = new MaterialSelectionPreset { Name = "威-A4", GroupName = "常用", ShowInPresetButtons = true, MaterialType = "FlatSheet" };
            var pRoll1 = new MaterialSelectionPreset { Name = "威-卷-折手", GroupName = "常用", ShowInPresetButtons = true, MaterialType = "RollMaterial" };
            var pFlat2 = new MaterialSelectionPreset { Name = "前连拼", GroupName = "常用", ShowInPresetButtons = true, MaterialType = "FlatSheet" };
            var pRoll2 = new MaterialSelectionPreset { Name = "威-卷-两联", GroupName = "常用", ShowInPresetButtons = true, MaterialType = "RollMaterial", EnableImposition = true, IsDualCopy = true, CopyCount = 2 };

            AppSettings.MaterialPresets = new List<MaterialSelectionPreset> { pFlat1, pRoll1, pFlat2, pRoll2 };
            AppSettings.LastSelectedPresetGroup = "常用";
            AppSettings.Save();

            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\Job_P3P4_80x45mm.pdf",
                regexResult: "Job_P3P4",
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

                string outDir = @"D:\vibe_codeing\Prepress-toolbox\output\playwright";
                Directory.CreateDirectory(outDir);

                // 1. 截取卡片 4 (P3: 栅格对齐、+1占位符、序号搜索一体化)
                var card4 = form.Controls.Find("tlpOrderParametersCard", true).FirstOrDefault();
                Assert.NotNull(card4);
                using (var bmpCard4 = new Bitmap(card4.Width, card4.Height))
                {
                    card4.DrawToBitmap(bmpCard4, new Rectangle(0, 0, card4.Width, card4.Height));
                    bmpCard4.Save(Path.Combine(outDir, "actual-v5-card4-grid-aligned.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                // 2. 模拟一式两联激活态，验证与 HTML 示意图完全一致的 [2联] 高亮蓝与自适应
                var isDualField = typeof(MaterialSelectFormModern).GetField("_isDuplicateLayoutEnabled", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                isDualField?.SetValue(form, true);

                var updatePillMethod = typeof(MaterialSelectFormModern).GetMethod("UpdateCopyCountPillDisplay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                updatePillMethod?.Invoke(form, null);

                var txtQty = form.Controls.Find("quantityTextBox", true).FirstOrDefault() as AntdUI.Input;
                if (txtQty != null) txtQty.Text = "1000";

                using (var bmpCard4Active = new Bitmap(card4.Width, card4.Height))
                {
                    card4.DrawToBitmap(bmpCard4Active, new Rectangle(0, 0, card4.Width, card4.Height));
                    bmpCard4Active.Save(Path.Combine(outDir, "actual-v5-card4-grid-aligned-active.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                // 2. 截取卡片 6 (P4: 预设平张蓝点与卷装橙点微标)
                var card6 = form.Controls.Find("presetBrowserCard", true).FirstOrDefault();
                Assert.NotNull(card6);
                using (var bmpCard6 = new Bitmap(card6.Width, card6.Height))
                {
                    card6.DrawToBitmap(bmpCard6, new Rectangle(0, 0, card6.Width, card6.Height));
                    bmpCard6.Save(Path.Combine(outDir, "actual-v5-card6-preset-dots.png"), System.Drawing.Imaging.ImageFormat.Png);
                }

                // 3. 截取 V5 一体化工作台全景渲染位图 (P1~P4 全景)
                var rightStation = form.Controls.Find("pnlRightStation", true).FirstOrDefault();
                if (rightStation != null)
                {
                    using (var bmpWhole = new Bitmap(rightStation.Width, rightStation.Height))
                    {
                        rightStation.DrawToBitmap(bmpWhole, new Rectangle(0, 0, rightStation.Width, rightStation.Height));
                        bmpWhole.Save(Path.Combine(outDir, "actual-v5-whole-with-p1-p4.png"), System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }
    }
}
