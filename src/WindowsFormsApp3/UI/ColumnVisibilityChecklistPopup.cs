using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.UI
{
    /// <summary>
    /// 文件列表列头使用的可持续多选清单弹层。
    /// </summary>
    internal sealed class ColumnVisibilityChecklistPopup
    {
        private const int BaseDpi = 96;
        private const int PopupWidth = 280;
        private const int RowHeight = 30;
        private const int FooterHeight = 42;
        private const int PopupPadding = 8;
        private const int ButtonWidth = 88;
        private const int PopupShadow = 8;

        private readonly DataGridView _grid;
        private readonly Action _saveSettings;
        private readonly Action _restoreDefaults;
        private ToolStripDropDown _activeDropDown;

        public ColumnVisibilityChecklistPopup(
            DataGridView grid,
            Action saveSettings,
            Action restoreDefaults)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _saveSettings = saveSettings ?? throw new ArgumentNullException(nameof(saveSettings));
            _restoreDefaults = restoreDefaults ?? throw new ArgumentNullException(nameof(restoreDefaults));
        }

        /// <summary>
        /// 在列头下方显示清单。勾选项不会关闭弹层，便于连续调整多列。
        /// </summary>
        public void ShowAtScreenLocation(int columnIndex, Point mouseScreenLocation)
        {
            if (columnIndex < 0 || columnIndex >= _grid.Columns.Count)
            {
                return;
            }

            var dpiScale = GetDpiScale(_grid);
            var content = CreateContent(dpiScale);
            _activeDropDown?.Close(ToolStripDropDownCloseReason.CloseCalled);

            var dropDown = CreateDropDown(content);
            _activeDropDown = dropDown;
            dropDown.Closed += (sender, args) =>
            {
                if (ReferenceEquals(_activeDropDown, dropDown))
                {
                    _activeDropDown = null;
                }

                // Closed 回调发生时 ToolStripManager 仍可能继续访问此对象，必须等当前消息处理完再释放。
                if (!_grid.IsDisposed && _grid.IsHandleCreated)
                {
                    ScheduleDropDownDisposal(
                        dropDown,
                        action => _grid.BeginInvoke(action));
                }
            };

            var workingArea = Screen.FromPoint(mouseScreenLocation).WorkingArea;
            var location = ClampTopLeftToWorkingArea(mouseScreenLocation, dropDown.Size, workingArea);
            dropDown.Show(location);
        }

        internal static void ScheduleDropDownDisposal(
            ToolStripDropDown dropDown,
            Action<Action> schedule)
        {
            if (dropDown == null)
            {
                throw new ArgumentNullException(nameof(dropDown));
            }

            if (schedule == null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            schedule(() =>
            {
                if (!dropDown.IsDisposed)
                {
                    dropDown.Dispose();
                }
            });
        }

        internal static ToolStripDropDown CreateDropDown(Control content)
        {
            var host = new ToolStripControlHost(content)
            {
                AutoSize = false,
                Size = content.Size,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            var dropDown = new BorderlessToolStripDropDown
            {
                AutoSize = false,
                Padding = Padding.Empty,
                Margin = Padding.Empty,
                BackColor = content.BackColor,
                DropShadowEnabled = false,
                Renderer = new BorderlessToolStripRenderer()
            };
            dropDown.Items.Add(host);
            dropDown.Size = dropDown.GetPreferredSize(Size.Empty);
            return dropDown;
        }

        private Control CreateContent(float dpiScale)
        {
            var tokens = GetCurrentTokens();
            var popupWidth = ScaleDimension(PopupWidth, dpiScale);
            var rowHeight = ScaleDimension(RowHeight, dpiScale);
            var footerHeight = ScaleDimension(FooterHeight, dpiScale);
            var popupPadding = ScaleDimension(PopupPadding, dpiScale);
            var popupShadow = ScaleDimension(PopupShadow, dpiScale);
            var listHeight = CalculateListHeight(_grid.Columns.Count, rowHeight);
            var surfaceColor = tokens?.Surface ?? SystemColors.Window;
            var content = new AntdUI.Panel
            {
                Size = new Size(
                    popupWidth,
                    CalculateContentHeight(listHeight, footerHeight, popupPadding, popupShadow)),
                Radius = ScaleDimension(8, dpiScale),
                Shadow = popupShadow,
                Padding = new Padding(popupPadding),
                Back = surfaceColor,
                BackColor = surfaceColor,
                ForeColor = tokens?.Foreground ?? SystemColors.WindowText,
                // 保持内容背景不透明，避免弹层首次绘制时短暂透出下方表格。
                AutoContainerBgTransparent = false
            };
            content.SuspendLayout();

            var list = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = false,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = tokens?.Surface ?? SystemColors.Window,
                Padding = Padding.Empty
            };
            list.SuspendLayout();

            foreach (DataGridViewColumn column in _grid.Columns)
            {
                var checkBox = new AntdUI.Checkbox
                {
                    Text = column.HeaderText,
                    Checked = column.Visible,
                    AutoCheck = true,
                    AutoSize = false,
                    Size = new Size(popupWidth - ScaleDimension(40, dpiScale), rowHeight),
                    Margin = Padding.Empty,
                    Tag = column.Name,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = tokens?.Foreground ?? SystemColors.WindowText,
                    Fill = tokens?.Primary ?? SystemColors.Highlight
                };

                checkBox.CheckedChanged += ColumnVisibilityChanged;
                list.Controls.Add(checkBox);
            }

            var footer = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = footerHeight,
                BackColor = tokens?.Surface ?? SystemColors.Window
            };

            var restoreButton = new AntdUI.Button
            {
                Text = "恢复原始",
                Dock = DockStyle.Right,
                Width = ScaleDimension(ButtonWidth, dpiScale),
                Type = AntdUI.TTypeMini.Default,
                Radius = ScaleDimension(6, dpiScale),
                BackColor = tokens?.Surface ?? SystemColors.Window,
                BackHover = tokens?.Hover ?? SystemColors.ControlLight,
                BackActive = tokens?.Active ?? SystemColors.ControlDark,
                ForeColor = tokens?.Foreground ?? SystemColors.WindowText,
                ForeHover = tokens?.Foreground ?? SystemColors.WindowText,
                ForeActive = tokens?.Foreground ?? SystemColors.WindowText,
                DefaultBack = tokens?.Surface ?? SystemColors.Window,
                DefaultBorderColor = tokens?.Border ?? SystemColors.WindowFrame,
                BorderWidth = 1
            };
            restoreButton.Click += (sender, args) => _restoreDefaults();

            var saveButton = new AntdUI.Button
            {
                Text = "保存配置",
                Dock = DockStyle.Right,
                Width = ScaleDimension(ButtonWidth, dpiScale),
                Type = AntdUI.TTypeMini.Primary,
                Radius = ScaleDimension(6, dpiScale),
                BackColor = tokens?.Primary ?? SystemColors.Highlight,
                BackHover = tokens?.Hover ?? SystemColors.ControlLight,
                BackActive = tokens?.Active ?? SystemColors.ControlDark,
                ForeColor = tokens?.Surface ?? SystemColors.Window,
                ForeHover = tokens?.Surface ?? SystemColors.Window,
                ForeActive = tokens?.Surface ?? SystemColors.Window
            };
            saveButton.Click += (sender, args) => _saveSettings();

            footer.Controls.Add(restoreButton);
            footer.Controls.Add(new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Right,
                Width = ScaleDimension(8, dpiScale)
            });
            footer.Controls.Add(saveButton);

            content.Controls.Add(list);
            content.Controls.Add(footer);
            list.ResumeLayout(false);
            content.ResumeLayout(true);
            content.PerformLayout();
            return content;
        }

        internal static int CalculateListHeight(int columnCount, int rowHeight)
        {
            return Math.Max(rowHeight, Math.Max(0, columnCount) * rowHeight);
        }

        internal static int CalculateContentHeight(
            int listHeight,
            int footerHeight,
            int popupPadding,
            int popupShadow)
        {
            return listHeight + footerHeight + popupPadding * 2 + popupShadow * 2;
        }

        internal static float GetDpiScale(Control control)
        {
            if (control == null)
            {
                return 1F;
            }

            var deviceDpi = control.DeviceDpi;
            return deviceDpi > 0 ? deviceDpi / (float)BaseDpi : 1F;
        }

        internal static int ScaleDimension(int value, float dpiScale)
        {
            return Math.Max(1, (int)Math.Round(value * Math.Max(1F, dpiScale)));
        }

        /// <summary>
        /// 将下拉菜单左上角限制在屏幕工作区内。
        /// </summary>
        internal static Point ClampTopLeftToWorkingArea(Point location, Size popupSize, Rectangle workingArea)
        {
            if (workingArea.Width <= 0 || workingArea.Height <= 0)
            {
                return location;
            }

            var x = workingArea.Width <= popupSize.Width
                ? workingArea.Left
                : Math.Max(workingArea.Left, Math.Min(workingArea.Right - popupSize.Width, location.X));
            var y = workingArea.Height <= popupSize.Height
                ? workingArea.Top
                : Math.Max(workingArea.Top, Math.Min(workingArea.Bottom - popupSize.Height, location.Y));
            return new Point(x, y);
        }

        private void ColumnVisibilityChanged(object sender, EventArgs args)
        {
            if (!(sender is AntdUI.Checkbox checkBox) || !(checkBox.Tag is string columnName))
            {
                return;
            }

            var column = _grid.Columns[columnName];
            if (column != null)
            {
                column.Visible = checkBox.Checked;
            }
        }

        private static PopupThemeTokens GetCurrentTokens()
        {
            try
            {
                var theme = Services.ServiceLocator.Instance.GetThemeManager()?.GetCurrentTheme();
                if (theme != null)
                {
                    return AntdUiThemeBridge.CurrentTokens ?? PopupThemeTokens.FromTheme(theme);
                }
            }
            catch
            {
                // 弹层仍可使用系统色，避免主题服务不可用时阻断列管理。
            }

            return AntdUiThemeBridge.CurrentTokens;
        }

        /// <summary>
        /// ToolStripDropDown 默认会绘制一圈灰色菜单边框，此渲染器只取消该边框。
        /// </summary>
        internal sealed class BorderlessToolStripRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            {
                using (var brush = new SolidBrush(e.ToolStrip.BackColor))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }

            protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
            {
                // 内容面板自身负责圆角和阴影，外层无需重复绘制边框。
            }
        }

        internal sealed class BorderlessToolStripDropDown : ToolStripDropDown
        {
            private const int WindowStyleBorder = 0x00800000;
            private const int ExtendedStyleClientEdge = 0x00000200;

            protected override CreateParams CreateParams
            {
                get
                {
                    var createParams = base.CreateParams;
                    createParams.Style &= ~WindowStyleBorder;
                    createParams.ExStyle &= ~ExtendedStyleClientEdge;
                    return createParams;
                }
            }

            internal bool HasNativeBorder
            {
                get
                {
                    var createParams = CreateParams;
                    return (createParams.Style & WindowStyleBorder) != 0 ||
                        (createParams.ExStyle & ExtendedStyleClientEdge) != 0;
                }
            }
        }
    }
}
