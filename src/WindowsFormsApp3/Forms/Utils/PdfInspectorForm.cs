using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Forms.Utils
{
    /// <summary>
    /// PDF检查器独立窗口
    /// 类似Enfocus PitStop Pro的Inspector窗口
    /// </summary>
    public class PdfInspectorForm : Form
    {
        private PdfInspectorControl _inspectorControl;
        private string _currentFilePath;
        private int _currentPage = 1;

        // 事件：当用户在检查器中选择页面时触发
        public event EventHandler<int> PageSelected;

        // 事件：当字体转曲开始时触发
        public event EventHandler FontOutlineStarted;

        // 事件：当字体转曲完成时触发
        public event EventHandler<WindowsFormsApp3.Forms.Controls.FontOutlineCompletedEventArgs> FontOutlineCompleted;

        public PdfInspectorForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 窗体设置
            this.Text = "PDF 检查器";
            this.Size = new Size(450, 700);
            this.MinimumSize = new Size(400, 500);
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.ShowInTaskbar = false; // 不在任务栏显示
            this.Icon = this.Owner?.Icon; // 使用主窗口图标

            // 创建检查器控件
            _inspectorControl = new PdfInspectorControl
            {
                Dock = DockStyle.Fill
            };
            _inspectorControl.PageSelected += InspectorControl_PageSelected;
            _inspectorControl.FontOutlineStarted += InspectorControl_FontOutlineStarted;
            _inspectorControl.FontOutlineCompleted += InspectorControl_FontOutlineCompleted;

            this.Controls.Add(_inspectorControl);

            // 窗口关闭时隐藏而不是销毁
            this.FormClosing += PdfInspectorForm_FormClosing;

            // 记住窗口位置
            this.Load += PdfInspectorForm_Load;
            this.LocationChanged += PdfInspectorForm_LocationChanged;
            this.SizeChanged += PdfInspectorForm_SizeChanged;

            this.ResumeLayout(false);
        }

        /// <summary>
        /// 加载PDF文件
        /// </summary>
        public void LoadPdf(string filePath, int currentPage = 1)
        {
            _currentFilePath = filePath;
            _currentPage = currentPage;

            if (_inspectorControl != null)
            {
                _inspectorControl.LoadPdf(filePath, currentPage);
                this.Text = $"PDF 检查器 - {System.IO.Path.GetFileName(filePath)}";
            }
        }

        /// <summary>
        /// 切换到指定页面
        /// </summary>
        public void SwitchToPage(int pageNumber)
        {
            _currentPage = pageNumber;
            _inspectorControl?.SwitchToPage(pageNumber);
        }

        /// <summary>
        /// 刷新检查器
        /// </summary>
        public void RefreshInspector()
        {
            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                _inspectorControl?.LoadPdf(_currentFilePath, _currentPage);
            }
        }

        /// <summary>
        /// 检查器控件页面选择事件
        /// </summary>
        private void InspectorControl_PageSelected(object sender, int pageNumber)
        {
            _currentPage = pageNumber;
            PageSelected?.Invoke(this, pageNumber);
        }

        /// <summary>
        /// 检查器控件字体转曲开始事件
        /// </summary>
        private void InspectorControl_FontOutlineStarted(object sender, EventArgs e)
        {
            // 转发事件
            FontOutlineStarted?.Invoke(this, e);
        }

        /// <summary>
        /// 检查器控件字体转曲完成事件
        /// </summary>
        private void InspectorControl_FontOutlineCompleted(object sender, WindowsFormsApp3.Forms.Controls.FontOutlineCompletedEventArgs e)
        {
            // 转发事件
            FontOutlineCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// 窗口关闭时隐藏而不是销毁
        /// </summary>
        private void PdfInspectorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        /// <summary>
        /// 窗口加载时恢复位置
        /// </summary>
        private void PdfInspectorForm_Load(object sender, EventArgs e)
        {
            RestoreWindowPosition();
        }

        /// <summary>
        /// 位置改变时保存
        /// </summary>
        private void PdfInspectorForm_LocationChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                SaveWindowPosition();
            }
        }

        /// <summary>
        /// 尺寸改变时保存
        /// </summary>
        private void PdfInspectorForm_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                SaveWindowPosition();
            }
        }

        /// <summary>
        /// 保存窗口位置和尺寸
        /// </summary>
        private void SaveWindowPosition()
        {
            try
            {
                AppSettings.Set("PdfInspector_WindowX", this.Location.X);
                AppSettings.Set("PdfInspector_WindowY", this.Location.Y);
                AppSettings.Set("PdfInspector_WindowWidth", this.Width);
                AppSettings.Set("PdfInspector_WindowHeight", this.Height);
            }
            catch (Exception ex)
            {
                LogHelper.Error($"保存检查器窗口位置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复窗口位置和尺寸
        /// </summary>
        private void RestoreWindowPosition()
        {
            try
            {
                var x = AppSettings.Get("PdfInspector_WindowX");
                var y = AppSettings.Get("PdfInspector_WindowY");
                var width = AppSettings.Get("PdfInspector_WindowWidth");
                var height = AppSettings.Get("PdfInspector_WindowHeight");

                if (x != null && y != null)
                {
                    var location = new Point(Convert.ToInt32(x), Convert.ToInt32(y));
                    
                    // 确保窗口在屏幕范围内
                    if (IsLocationVisible(location))
                    {
                        this.Location = location;
                    }
                    else
                    {
                        // 默认位置：主窗口右侧
                        PositionNextToOwner();
                    }
                }
                else
                {
                    // 首次打开：主窗口右侧
                    PositionNextToOwner();
                }

                if (width != null && height != null)
                {
                    this.Size = new Size(Convert.ToInt32(width), Convert.ToInt32(height));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"恢复检查器窗口位置失败: {ex.Message}");
                PositionNextToOwner();
            }
        }

        /// <summary>
        /// 定位到主窗口右侧
        /// </summary>
        private void PositionNextToOwner()
        {
            if (this.Owner != null)
            {
                // 主窗口右侧，稍微偏移
                int x = this.Owner.Right + 10;
                int y = this.Owner.Top;

                // 确保不超出屏幕
                var screen = Screen.FromControl(this.Owner);
                if (x + this.Width > screen.WorkingArea.Right)
                {
                    x = screen.WorkingArea.Right - this.Width - 10;
                }

                this.Location = new Point(x, y);
            }
            else
            {
                // 屏幕右侧
                var screen = Screen.PrimaryScreen;
                int x = screen.WorkingArea.Right - this.Width - 20;
                int y = screen.WorkingArea.Top + 50;
                this.Location = new Point(x, y);
            }
        }

        /// <summary>
        /// 检查位置是否在可见屏幕范围内
        /// </summary>
        private bool IsLocationVisible(Point location)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.Contains(location))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 显示窗口并激活
        /// </summary>
        public new void Show()
        {
            base.Show();
            this.Activate();
            this.BringToFront();
        }

        /// <summary>
        /// 切换显示/隐藏
        /// </summary>
        public void ToggleVisibility()
        {
            if (this.Visible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inspectorControl?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
