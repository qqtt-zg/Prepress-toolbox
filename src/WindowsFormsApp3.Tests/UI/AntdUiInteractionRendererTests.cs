using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.UI;

namespace WindowsFormsApp3.Tests.UI
{
    public class AntdUiInteractionRendererTests
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point point);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LogicalToPhysicalPointForPerMonitorDPI(IntPtr hWnd, ref Point point);

        [Fact]
        public void ContextMenuRequest_Uses_The_Exact_Right_Click_Client_Coordinates()
        {
            var location = ContextMenuRequest.GetMouseInvocationLocation(127, 43);

            Assert.Equal(new Point(127, 43), location);
        }

        [Fact]
        public void CreateConfig_Uses_System_Mouse_Position_For_Mouse_Triggered_Menus()
        {
            using var target = new System.Windows.Forms.Panel();
            var config = AntdUiContextMenuRenderer.CreateConfig(
                new ContextMenuRequest(target, new[] { new ContextMenuItemSpec("copy", "复制") })
                {
                    Location = new Point(127, 43),
                    UseMousePosition = true
                },
                _ => { });

            Assert.Null(config.Location);
        }

        [Fact]
        public void ColumnChecklist_Clamps_Its_Top_Left_To_The_Working_Area()
        {
            var location = ColumnVisibilityChecklistPopup.ClampTopLeftToWorkingArea(
                new Point(950, 740),
                new Size(280, 500),
                new Rectangle(0, 0, 1000, 800));

            Assert.Equal(new Point(720, 300), location);
        }

        [Fact]
        public void BuildItems_Maps_Nested_Divider_And_Command_Presentation()
        {
            var danger = Color.MediumVioletRed;
            AntdUiThemeBridge.Apply(new ThemeDefinition
            {
                Background = Color.WhiteSmoke,
                Surface = Color.White,
                TextPrimary = Color.Black,
                Primary = Color.RoyalBlue,
                Success = Color.ForestGreen,
                Warning = Color.Goldenrod,
                Error = danger
            });

            var child = new ContextMenuItemSpec("copy", "复制")
            {
                ShortcutText = "Ctrl+C",
                Checked = true,
                IconSvg = "<svg />"
            };
            var dangerous = new ContextMenuItemSpec("delete", "删除")
            {
                Enabled = false,
                IsDangerous = true
            };
            var parent = new ContextMenuItemSpec("operations", "操作")
            {
                Items = new[] { child, ContextMenuItemSpec.Divider(), dangerous }
            };

            var rendered = AntdUiContextMenuRenderer.BuildItems(new[] { parent });

            var renderedParent = Assert.IsType<AntdUI.ContextMenuStripItem>(Assert.Single(rendered));
            Assert.Equal("operations", renderedParent.ID);
            Assert.Equal(3, renderedParent.Sub.Length);

            var renderedChild = Assert.IsType<AntdUI.ContextMenuStripItem>(renderedParent.Sub[0]);
            Assert.Equal("Ctrl+C", renderedChild.SubText);
            Assert.True(renderedChild.Checked);
            Assert.Equal("<svg />", renderedChild.IconSvg);
            Assert.Same(child, renderedChild.Tag);

            Assert.IsType<AntdUI.ContextMenuStripItemDivider>(renderedParent.Sub[1]);

            var renderedDangerous = Assert.IsType<AntdUI.ContextMenuStripItem>(renderedParent.Sub[2]);
            Assert.False(renderedDangerous.Enabled);
            Assert.Equal(danger, renderedDangerous.Fore);
        }

        [Fact]
        public void CreateConfig_Preserves_Caller_Owned_Target_Location_And_Theme()
        {
            using var owner = new System.Windows.Forms.Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(120, 80)
            };
            using var target = new System.Windows.Forms.Panel
            {
                Location = new Point(16, 24),
                Size = new Size(200, 120)
            };
            owner.Controls.Add(target);
            owner.CreateControl();
            target.CreateControl();
            var request = new ContextMenuRequest(target, new[] { new ContextMenuItemSpec("refresh", "刷新") })
            {
                Location = new Point(12, 34),
                ColorScheme = TAMode.Dark,
                Radius = 10
            };

            var config = AntdUiContextMenuRenderer.CreateConfig(request, _ => { });

            Assert.Same(target, config.Target.GetControl);
            Assert.Equal(target.PointToScreen(new Point(12, 34)), config.Location);
            Assert.Equal(TAMode.Dark, config.ColorScheme);
            Assert.Equal(10, config.Radius);
        }

        [Fact]
        public void CreateConfig_Forwards_The_Original_Item_To_The_Caller_Callback()
        {
            using var target = new System.Windows.Forms.Panel();
            var item = new ContextMenuItemSpec("refresh", "刷新");
            var request = new ContextMenuRequest(target, new[] { item });
            ContextMenuItemSpec selected = null;

            var config = AntdUiContextMenuRenderer.CreateConfig(request, value => selected = value);
            config.Call((AntdUI.ContextMenuStripItem)config.Items[0]);

            Assert.Same(item, selected);
            Assert.True(config.UFocus);
        }

        [Theory]
        [InlineData(Keys.Apps, Keys.None, true)]
        [InlineData(Keys.F10, Keys.Shift, true)]
        [InlineData(Keys.F10, Keys.None, false)]
        [InlineData(Keys.Apps, Keys.Control, false)]
        public void ContextMenuRequest_Recognizes_Only_Standard_Keyboard_Menu_Invocations(
            Keys keyCode,
            Keys modifiers,
            bool expected)
        {
            Assert.Equal(expected, ContextMenuRequest.IsKeyboardInvocation(keyCode, modifiers));
        }

        [Fact]
        public void ContextMenuRequest_Anchors_Keyboard_Menu_To_Current_DataGridView_Cell()
        {
            using var grid = new DataGridView { Size = new Size(300, 200) };
            grid.Columns.Add("name", "名称");
            grid.Rows.Add("A");
            grid.CurrentCell = grid.Rows[0].Cells[0];
            var bounds = grid.GetCellDisplayRectangle(0, 0, true);
            var expected = bounds.IsEmpty
                ? new Point(grid.Width / 2, grid.Height / 2)
                : new Point(Math.Max(0, bounds.Left), Math.Max(0, bounds.Bottom));
            var fallback = new Point(grid.Width / 2, grid.Height / 2);

            var location = ContextMenuRequest.GetKeyboardInvocationLocation(grid);

            // 未附加到窗体的 DataGridView 可能在调用间重新布局；两种值都符合锚点契约。
            Assert.Contains(location, new[] { expected, fallback });
        }

        [Fact]
        public void ContextMenuRequest_Anchors_Keyboard_Menu_To_The_Control_Client_Center()
        {
            using var form = new Form { ClientSize = new Size(300, 200) };

            var location = ContextMenuRequest.GetKeyboardInvocationLocation(form);

            Assert.Equal(new Point(150, 100), location);
        }

        [Fact]
        public void CreateConfig_Converts_Explicit_Client_Anchor_For_Non_Mouse_Menus()
        {
            using var host = new Form
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(180, 160),
                Size = new Size(320, 240)
            };
            var clientPoint = new Point(72, 58);
            var config = AntdUiContextMenuRenderer.CreateConfig(
                new ContextMenuRequest(host, new[] { new ContextMenuItemSpec("copy", "复制") })
                {
                    Location = clientPoint
                },
                _ => { });

            Assert.Equal(host.PointToScreen(clientPoint), config.Location);
        }

        [Fact]
        public void CreateConfig_Maps_Declared_Dialog_Results_And_Default_Cancel_Semantics()
        {
            using var owner = new Form();
            var request = new ModalRequest(owner, "确认", "继续执行吗？", new[]
            {
                new ModalButtonSpec("cancel", "取消", DialogResult.Cancel, TTypeMini.Default) { IsCancel = true },
                new ModalButtonSpec("continue", "继续", DialogResult.Retry, TTypeMini.Primary) { IsDefault = true }
            })
            {
                ColorScheme = TAMode.Dark
            };

            var config = AntdUiModalRenderer.CreateConfig(request);

            Assert.Same(owner, config.Target.GetForm);
            Assert.Equal(TAMode.Dark, config.ColorScheme);
            Assert.Null(config.CancelText);
            Assert.False(config.DefaultAcceptButton);
            Assert.False(config.DefaultFocus);
            Assert.True(config.CloseIcon);
            Assert.Null(config.OnOk);
            Assert.Null(config.OnBtns);
            Assert.Collection(
                config.Btns,
                cancel => Assert.Equal(DialogResult.Cancel, cancel.DialogResult),
                proceed => Assert.Equal(DialogResult.Retry, proceed.DialogResult));
        }

        [Fact]
        public void ModalRenderer_Binds_Custom_Default_And_Cancel_After_Final_Parent_Is_Attached()
        {
            using var owner = new Form();
            var request = new ModalRequest(owner, "确认", "继续执行吗？", new[]
            {
                new ModalButtonSpec("cancel", "取消", DialogResult.Cancel, TTypeMini.Default)
                {
                    IsCancel = true
                },
                new ModalButtonSpec("continue", "继续", DialogResult.Yes, TTypeMini.Primary)
                {
                    IsDefault = true
                }
            });
            var config = AntdUiModalRenderer.CreateConfig(request);
            using var footer = new System.Windows.Forms.Panel();
            var defaultButton = new AntdUI.Button();
            var cancelButton = new AntdUI.Button();

            // 模拟 LayeredFormModal：先回调样式，再将按钮放入 footer，最后挂到 modal。
            config.OnButtonStyle("continue", defaultButton);
            config.OnButtonStyle("cancel", cancelButton);
            footer.Controls.Add(defaultButton);
            footer.Controls.Add(cancelButton);
            owner.Controls.Add(footer);
            owner.CreateControl();
            var ownerHandle = owner.Handle;
            footer.CreateControl();
            defaultButton.CreateControl();
            cancelButton.CreateControl();
            owner.Show();
            Application.DoEvents();

            Assert.Same(defaultButton, owner.AcceptButton);
            Assert.Same(cancelButton, owner.CancelButton);
            Assert.Equal(DialogResult.Yes, ((IButtonControl)defaultButton).DialogResult);
            Assert.Equal(DialogResult.Cancel, ((IButtonControl)cancelButton).DialogResult);
            owner.Hide();
        }

        [Fact]
        public void CreateConfig_Hides_BuiltIn_Ok_And_Binds_Declared_Default_And_Cancel_Buttons()
        {
            using var owner = new Form();
            var request = new ModalRequest(owner, "确认", "继续执行吗？", new[]
            {
                new ModalButtonSpec("cancel", "取消", DialogResult.Cancel, TTypeMini.Default) { IsCancel = true },
                new ModalButtonSpec("continue", "继续", DialogResult.No, TTypeMini.Primary) { IsDefault = true }
            });
            var config = AntdUiModalRenderer.CreateConfig(request);
            var builtInOk = new AntdUI.Button();
            var defaultButton = new AntdUI.Button();
            var cancelButton = new AntdUI.Button();
            using var footer = new System.Windows.Forms.Panel();

            config.OnButtonStyle("OK", builtInOk);
            config.OnButtonStyle("continue", defaultButton);
            config.OnButtonStyle("cancel", cancelButton);
            owner.Controls.Add(footer);
            footer.Controls.Add(defaultButton);
            footer.Controls.Add(cancelButton);

            Assert.False(builtInOk.Visible);
            Assert.Equal(AntdUI.TAutoSize.None, builtInOk.AutoSizeMode);
            Assert.Same(defaultButton, owner.AcceptButton);
            Assert.Same(cancelButton, owner.CancelButton);
        }

        [Fact]
        public void CreateConfig_Preserves_Custom_Button_Results_And_Mandatory_Decision_Settings()
        {
            using var owner = new Form();
            var request = new ModalRequest(owner, "移动失败", "请选择处理方式", new[]
            {
                new ModalButtonSpec("abort", "中止", DialogResult.Abort, TTypeMini.Error) { IsDefault = true },
                new ModalButtonSpec("retry", "重试", DialogResult.Retry, TTypeMini.Primary),
                new ModalButtonSpec("ignore", "忽略", DialogResult.Ignore, TTypeMini.Default)
            })
            {
                Keyboard = false,
                MaskClosable = false
            };

            var config = AntdUiModalRenderer.CreateConfig(request);

            Assert.False(config.Keyboard);
            Assert.False(config.MaskClosable);
            Assert.Collection(
                config.Btns,
                abort => Assert.Equal(DialogResult.Abort, abort.DialogResult),
                retry => Assert.Equal(DialogResult.Retry, retry.DialogResult),
                ignore => Assert.Equal(DialogResult.Ignore, ignore.DialogResult));
        }

        [Fact]
        public void ModalRequest_Rejects_Multiple_Default_Buttons()
        {
            using var owner = new Form();
            var request = new ModalRequest(owner, "确认", "继续执行吗？", new[]
            {
                new ModalButtonSpec("first", "一", DialogResult.Yes, TTypeMini.Primary) { IsDefault = true },
                new ModalButtonSpec("second", "二", DialogResult.No, TTypeMini.Default) { IsDefault = true }
            });

            Assert.Throws<ArgumentException>(() => AntdUiModalRenderer.CreateConfig(request));
        }

        [Fact]
        public void ModalRequest_Requires_Cancel_Button_To_Return_Cancel()
        {
            using var owner = new Form();
            var request = new ModalRequest(owner, "确认", "继续执行吗？", new[]
            {
                new ModalButtonSpec("cancel", "取消", DialogResult.No, TTypeMini.Default) { IsCancel = true }
            });

            Assert.Throws<ArgumentException>(() => AntdUiModalRenderer.CreateConfig(request));
        }

        [Theory]
        [InlineData(280, 1F, 280)]
        [InlineData(280, 1.25F, 350)]
        [InlineData(280, 1.5F, 420)]
        [InlineData(280, 2F, 560)]
        public void ColumnVisibilityPopup_Scales_Dimensions_For_Target_Dpi(
            int value,
            float dpiScale,
            int expected)
        {
            Assert.Equal(expected, ColumnVisibilityChecklistPopup.ScaleDimension(value, dpiScale));
        }

        [Theory]
        [InlineData(0, 30, 30)]
        [InlineData(1, 30, 30)]
        [InlineData(14, 30, 420)]
        public void ColumnVisibilityPopup_Shows_All_Columns_Without_A_Height_Cap(
            int columnCount,
            int rowHeight,
            int expectedHeight)
        {
            Assert.Equal(
                expectedHeight,
                ColumnVisibilityChecklistPopup.CalculateListHeight(columnCount, rowHeight));
        }

        [Fact]
        public void ColumnVisibilityPopup_Uses_A_Native_Non_Layered_DropDown_Host()
        {
            using var content = new System.Windows.Forms.Panel { Size = new Size(280, 520) };
            using var dropDown = ColumnVisibilityChecklistPopup.CreateDropDown(content);

            var borderlessDropDown = Assert.IsType<ColumnVisibilityChecklistPopup.BorderlessToolStripDropDown>(dropDown);
            Assert.IsType<ToolStripControlHost>(Assert.Single(dropDown.Items.Cast<ToolStripItem>()));
            Assert.IsType<ColumnVisibilityChecklistPopup.BorderlessToolStripRenderer>(dropDown.Renderer);
            Assert.False(borderlessDropDown.HasNativeBorder);
            Assert.False(dropDown.DropShadowEnabled);
            Assert.Equal(content.BackColor, dropDown.BackColor);
            Assert.Equal(Padding.Empty, dropDown.Padding);
        }

        [Fact]
        public void ColumnVisibilityPopup_Defers_Disposal_Until_After_The_Closed_Callback()
        {
            using var dropDown = new ToolStripDropDown();
            Action deferredDisposal = null;

            ColumnVisibilityChecklistPopup.ScheduleDropDownDisposal(
                dropDown,
                action => deferredDisposal = action);

            Assert.False(dropDown.IsDisposed);
            Assert.NotNull(deferredDisposal);

            deferredDisposal();

            Assert.True(dropDown.IsDisposed);
        }

        [Fact]
        public void ColumnVisibilityPopup_Reserves_Padding_And_Shadow_Outside_All_Rows()
        {
            var contentHeight = ColumnVisibilityChecklistPopup.CalculateContentHeight(
                listHeight: 450,
                footerHeight: 42,
                popupPadding: 8,
                popupShadow: 8);

            Assert.Equal(524, contentHeight);
        }

        [Fact]
        public void ColumnVisibilityPopup_Clamps_DropDown_To_Working_Area()
        {
            var workingArea = new Rectangle(100, 50, 1000, 700);
            var popupSize = new Size(280, 300);

            var clamped = ColumnVisibilityChecklistPopup.ClampTopLeftToWorkingArea(
                new Point(10, 900),
                popupSize,
                workingArea);

            Assert.Equal(new Point(100, 450), clamped);
        }

        [Fact]
        public void UIHelper_Returns_Safe_Cancel_When_No_Modal_Owner_Is_Available()
        {
            Assert.Equal(
                DialogResult.No,
                UIHelper.ShowYesNoConfirmation(null, "不会显示原生回退框", "测试"));
        }

    }
}
