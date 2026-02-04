using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Forms.Panels;

namespace WindowsFormsApp3.Forms.Utils
{
    /// <summary>
    /// 纯代码实现的悬浮拖拽窗：拖入PDF后回调交给调用方处理（复制到监控目录）。
    /// </summary>
    public class FloatingDropZoneForm : Form
    {
        private const int ResizeHandleSize = 8;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == FloatingDropZoneNative.WM_NCHITTEST)
            {
                base.WndProc(ref m);

                if ((int)m.Result == FloatingDropZoneNative.HTCLIENT)
                {
                    // lParam contains screen coordinates (need signed conversion to support negative coords)
                    int x = unchecked((short)(m.LParam.ToInt64() & 0xFFFF));
                    int y = unchecked((short)((m.LParam.ToInt64() >> 16) & 0xFFFF));
                    var cursor = PointToClient(new Point(x, y));

                    bool left = cursor.X <= ResizeHandleSize;
                    bool right = cursor.X >= ClientSize.Width - ResizeHandleSize;
                    bool top = cursor.Y <= ResizeHandleSize;
                    bool bottom = cursor.Y >= ClientSize.Height - ResizeHandleSize;

                    if (left && top) m.Result = (IntPtr)FloatingDropZoneNative.HTTOPLEFT;
                    else if (right && top) m.Result = (IntPtr)FloatingDropZoneNative.HTTOPRIGHT;
                    else if (left && bottom) m.Result = (IntPtr)FloatingDropZoneNative.HTBOTTOMLEFT;
                    else if (right && bottom) m.Result = (IntPtr)FloatingDropZoneNative.HTBOTTOMRIGHT;
                    else if (left) m.Result = (IntPtr)FloatingDropZoneNative.HTLEFT;
                    else if (right) m.Result = (IntPtr)FloatingDropZoneNative.HTRIGHT;
                    else if (top) m.Result = (IntPtr)FloatingDropZoneNative.HTTOP;
                    else if (bottom) m.Result = (IntPtr)FloatingDropZoneNative.HTBOTTOM;
                }
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// 设置窗口透明样式（用于 Popcat 功能）
        /// 使用 DWM API 实现 Acrylic 效果或基本透明效果，根据 Windows 版本自动选择最佳方案
        /// </summary>
        private void SetTransparentWindow()
        {
            try
            {
                if (!IsHandleCreated)
                {
                    LogHelper.Warn("窗口句柄未创建，无法设置透明效果");
                    return;
                }

                // 确保窗口已显示，避免在窗口显示前设置透明度导致显示异常
                if (!Visible)
                {
                    LogHelper.Info("窗口未显示，延迟应用透明效果");
                    return;
                }

                // 计算透明度 Alpha 值（0-255）
                byte alphaValue = (byte)(_opacity * 255);
                
                // 如果透明度为100%（完全不透明），移除分层窗口样式，恢复普通窗口显示
                if (_opacity >= 1.0)
                {
                    // 禁用 DWM 透明效果
                    if (_transparencyMode != TransparencyMode.None)
                    {
                        FloatingDropZoneDwm.DisableDwmEffect(this.Handle);
                        _transparencyMode = TransparencyMode.None;
                    }
                    
                    // 移除分层窗口样式
                    try
                    {
                        int exStyle = FloatingDropZoneNative.GetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE);
                        if ((exStyle & FloatingDropZoneNative.WS_EX_LAYERED) != 0)
                        {
                            FloatingDropZoneNative.SetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE, 
                                exStyle & ~FloatingDropZoneNative.WS_EX_LAYERED);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Warn($"移除分层窗口样式失败: {ex.Message}");
                    }
                    
                    // 恢复普通窗口显示
                    BackColor = _defaultBackColor;
                    TransparencyKey = Color.Empty;
                    
                    // 刷新窗口
                    Invalidate();
                    Update();
                    
                    LogHelper.Info($"透明度为100%，已移除透明效果，恢复普通窗口显示");
                    return;
                }

                if (_popcatEnabled)
                {
                    // 禁用之前的透明效果
                    if (_transparencyMode != TransparencyMode.None)
                    {
                        FloatingDropZoneDwm.DisableDwmEffect(this.Handle);
                        _transparencyMode = TransparencyMode.None;
                    }

                    // 先确保窗口正常显示，使用默认背景色
                    // 清除窗口内容，避免显示多余内容
                    BackColor = _defaultBackColor;
                    TransparencyKey = Color.Empty;
                    
                    // 移除分层窗口样式，恢复普通窗口显示
                    try
                    {
                        int exStyle = FloatingDropZoneNative.GetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE);
                        if ((exStyle & FloatingDropZoneNative.WS_EX_LAYERED) != 0)
                        {
                            FloatingDropZoneNative.SetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE, 
                                exStyle & ~FloatingDropZoneNative.WS_EX_LAYERED);
                        }
                    }
                    catch { }
                    
                    // 强制刷新窗口，清除多余内容
                    Invalidate();
                    Update();
                    Refresh();

                    // 根据 Windows 版本自动选择最佳的透明效果方案
                    TransparencyMode mode = FloatingDropZoneDwm.EnableTransparency(this.Handle);
                    
                    if (mode != TransparencyMode.None)
                    {
                        _transparencyMode = mode;
                        
                        // 处理待处理的消息，确保窗口内容已清除
                        Application.DoEvents();
                        
                        // 设置窗口背景为透明，让 DWM 处理
                        BackColor = Color.Transparent;
                        TransparencyKey = Color.Empty;
                        
                        // 在 DWM 效果基础上，应用透明度设置
                        try
                        {
                            // 确保窗口有分层样式
                            int exStyle = FloatingDropZoneNative.GetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE);
                            if ((exStyle & FloatingDropZoneNative.WS_EX_LAYERED) == 0)
                            {
                                FloatingDropZoneNative.SetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE, 
                                    exStyle | FloatingDropZoneNative.WS_EX_LAYERED);
                            }
                            
                            // 使用 LWA_ALPHA 设置整体透明度
                            FloatingDropZoneNative.SetLayeredWindowAttributes(this.Handle, 0, alphaValue, 
                                FloatingDropZoneNative.LWA_ALPHA);
                            
                            // 再次刷新窗口，确保透明效果正确应用
                            Invalidate();
                            Update();
                            
                            LogHelper.Info($"已启用透明效果，模式: {mode}, 透明度: {_opacity} (Alpha: {alphaValue})");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Warn($"应用透明度设置失败: {ex.Message}");
                            LogHelper.Info($"已启用透明效果，模式: {mode}（未应用透明度设置）");
                        }
                    }
                    else
                    {
                        // 如果 DWM 方案都失败，回退到 SetLayeredWindowAttributes
                        LogHelper.Info("DWM 方案失败，使用 SetLayeredWindowAttributes 作为回退方案");
                        try
                        {
                            // 获取当前窗口样式
                            int exStyle = FloatingDropZoneNative.GetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE);
                            // 添加分层窗口样式，支持透明
                            FloatingDropZoneNative.SetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE, 
                                exStyle | FloatingDropZoneNative.WS_EX_LAYERED);
                            
                            // 使用颜色键透明，将 Magenta 设为透明键
                            Color transparentKey = Color.Magenta;
                            BackColor = transparentKey;
                            TransparencyKey = transparentKey;
                            FloatingDropZoneNative.SetLayeredWindowAttributes(this.Handle, 
                                (uint)transparentKey.ToArgb() & 0x00FFFFFF, alphaValue, 
                                FloatingDropZoneNative.LWA_COLORKEY | FloatingDropZoneNative.LWA_ALPHA);
                            _transparencyMode = TransparencyMode.None;
                            LogHelper.Info($"已使用 SetLayeredWindowAttributes 回退方案，透明度: {_opacity} (Alpha: {alphaValue})");
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error($"SetLayeredWindowAttributes 回退方案也失败: {ex.Message}");
                            _transparencyMode = TransparencyMode.None;
                        }
                    }
                }
                else
                {
                    // 禁用 DWM 透明效果
                    if (_transparencyMode != TransparencyMode.None)
                    {
                        FloatingDropZoneDwm.DisableDwmEffect(this.Handle);
                        _transparencyMode = TransparencyMode.None;
                    }
                    
                    // 使用 SetLayeredWindowAttributes 的 LWA_ALPHA 实现透明度
                    try
                    {
                        // 获取当前窗口样式
                        int exStyle = FloatingDropZoneNative.GetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE);
                        // 添加分层窗口样式，支持透明
                        FloatingDropZoneNative.SetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE, 
                            exStyle | FloatingDropZoneNative.WS_EX_LAYERED);
                        
                        // 使用 LWA_ALPHA 设置整体透明度
                        FloatingDropZoneNative.SetLayeredWindowAttributes(this.Handle, 0, alphaValue, 
                            FloatingDropZoneNative.LWA_ALPHA);
                        
                        TransparencyKey = Color.Empty;
                        LogHelper.Info($"已设置窗口透明度: {_opacity} (Alpha: {alphaValue})");
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error($"设置窗口透明度失败: {ex.Message}");
                        // 如果失败，尝试移除分层窗口样式
                        try
                        {
                            int exStyle = FloatingDropZoneNative.GetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE);
                            if ((exStyle & FloatingDropZoneNative.WS_EX_LAYERED) != 0)
                            {
                                FloatingDropZoneNative.SetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE, 
                                    exStyle & ~FloatingDropZoneNative.WS_EX_LAYERED);
                            }
                        }
                        catch { }
                        TransparencyKey = Color.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"设置窗口透明样式失败: {ex.Message}, 堆栈: {ex.StackTrace}");
                _transparencyMode = TransparencyMode.None;
            }
        }


        private const string PersistKeyPrefix = "FloatingDropZone";

        // Popcat 图标可用尺寸列表
        private static readonly int[] AvailableIconSizes = { 32, 48, 64, 96, 128, 256 };

        private readonly Action<string, DragDropEffects> _onPdfDropped;
        private Func<DropOperationMode> _getDropOperationMode; // 获取操作模式的回调函数
        private readonly Label _label;
        private PictureBox _popcatIcon;
        private Color _defaultBackColor;
        private Color _dragBackColor;
        private bool _popcatEnabled;
        private TransparencyMode _transparencyMode = TransparencyMode.None; // 当前使用的透明效果模式
        private bool _shownEventSubscribed; // 标志：Shown 事件是否已订阅
        private double _opacity = 1.0; // 透明度设置（0.0-1.0）
        private System.Windows.Forms.Timer _transparencyTimer; // 用于延迟应用透明效果的定时器

        private bool _isDragging;
        private Point _dragStart;
        private string _currentIconFileName = "cat_full.ico"; // 跟踪当前显示的图标文件名
        private bool _showBorder = false; // 是否显示边框（默认不显示）
        private DropOperationMode _dropOperationMode = DropOperationMode.Default; // 当前操作模式
        private string _targetDirectory = string.Empty; // 目标目录（用于判断分区）

        /// <summary>
        /// 仅供 WinForms 设计器实例化使用：提供无参构造函数，避免设计器因无法创建实例而报错。
        /// </summary>
        public FloatingDropZoneForm()
            : this((_, __) => { }, null, string.Empty)
        {
        }

        public FloatingDropZoneForm(Action<string, DragDropEffects> onPdfDropped, Func<DropOperationMode> getDropOperationMode = null, string targetDirectory = "")
        {
            _onPdfDropped = onPdfDropped;
            _getDropOperationMode = getDropOperationMode;
            _targetDirectory = targetDirectory;

            // 启用双缓冲和重绘优化，支持透明背景
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);

            // 无边框显示（通过WM_NCHITTEST实现边缘缩放）
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            // 最小尺寸设置为很小的值，给用户更多灵活性，但避免窗口太小无法操作
            MinimumSize = new Size(50, 30);
            MaximumSize = new Size(600, 300);
            AllowDrop = true;

            // 从主题加载设置
            ApplyThemeSettings();

            LoadPersistedBoundsOrDefault();
            
            // 加载边框显示设置
            LoadBorderSetting();
            
            // Popcat模式下确保窗口为正方形（如果窗口句柄已创建）
            // 如果句柄未创建，会在窗口显示后通过其他调用确保
            if (IsHandleCreated)
            {
                EnsureSquareWindow();
            }

            // 创建 Popcat 图标 PictureBox（初始隐藏，根据 Popcat 功能状态显示/隐藏）
            _popcatIcon = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Visible = false
            };
            // 确保图标在最上层
            _popcatIcon.BringToFront();
            Controls.Add(_popcatIcon);

            // 创建文本标签（当 Popcat 未启用时显示）
            _label = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                Text = "拖入PDF"
            };
            Controls.Add(_label);

            // 根据 Popcat 功能状态更新显示
            UpdatePopcatIconDisplay();

            // Drag&Drop
            DragEnter += OnDragEnter;
            DragOver += OnDragOver;
            DragLeave += OnDragLeave;
            DragDrop += OnDragDrop;

            // Move window (click-drag)
            // 注意：为了支持拖拽缩放，这里只在“非边缘区域”触发移动，避免与系统Resize命中冲突
            const int resizeBorder = 8;

            bool IsInResizeZone(Point p)
            {
                return p.X <= resizeBorder || p.Y <= resizeBorder ||
                       p.X >= ClientSize.Width - resizeBorder ||
                       p.Y >= ClientSize.Height - resizeBorder;
            }

            void BeginDrag(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && !IsInResizeZone(e.Location))
                {
                    _isDragging = true;
                    _dragStart = e.Location;
                }
            }

            void MoveDrag(object s, MouseEventArgs e)
            {
                if (_isDragging)
                {
                    Location = new Point(Left + e.X - _dragStart.X, Top + e.Y - _dragStart.Y);
                }
            }

            void EndDrag(object s, MouseEventArgs e)
            {
                _isDragging = false;
            }

            MouseDown += BeginDrag;
            MouseMove += MoveDrag;
            MouseUp += EndDrag;

            // Label会拦截鼠标事件，必须也绑定到Label上才能拖动
            _label.MouseDown += BeginDrag;
            _label.MouseMove += MoveDrag;
            _label.MouseUp += EndDrag;

            // Popcat 图标也需要支持拖动
            _popcatIcon.MouseDown += BeginDrag;
            _popcatIcon.MouseMove += MoveDrag;
            _popcatIcon.MouseUp += EndDrag;

            // Right click menu
            var menu = new ContextMenuStrip();
            
            // 添加"显示边框"复选框菜单项
            var showBorderItem = new ToolStripMenuItem("显示边框");
            showBorderItem.Checked = _showBorder;
            showBorderItem.Click += (s, e) =>
            {
                _showBorder = !_showBorder;
                showBorderItem.Checked = _showBorder;
                SaveBorderSetting();
                Invalidate(); // 刷新窗口以显示/隐藏边框
            };
            menu.Items.Add(showBorderItem);
            
            menu.Items.Add("隐藏", null, (s, e) =>
            {
                PersistBounds();
                Hide();
            });
            ContextMenuStrip = menu;

            // Persist when move/resize ends
            Move += (s, e) =>
            {
                PersistBounds();
            };
            ResizeEnd += (s, e) =>
            {
                PersistBounds();
                // Popcat模式下保持窗口为正方形
                EnsureSquareWindow();
                // 窗口大小改变后，如果Popcat功能启用，重新加载当前图标以确保图标大小正确
                // 使用当前图标文件名，让Icon类根据新的窗口大小自动选择最合适的尺寸
                if (_popcatEnabled && _popcatIcon != null)
                {
                    LoadPopcatIcon(_currentIconFileName);
                    // 强制刷新窗口，确保图标正确显示
                    Invalidate();
                    Update();
                }
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 避免被用户误关闭导致对象释放：改为隐藏
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                PersistBounds();
                Hide();
                return;
            }

            PersistBounds();
            base.OnFormClosing(e);
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            LogHelper.Info("OnDragEnter 事件触发");
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                LogHelper.Info("拖拽进入：数据不包含文件");
                e.Effect = DragDropEffects.None;
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Any(IsPdfFile))
            {
                LogHelper.Info($"拖拽进入：检测到 PDF 文件，文件数量: {files.Length}");
                
                // 获取当前操作模式
                DropOperationMode mode = _dropOperationMode;
                if (_getDropOperationMode != null)
                {
                    mode = _getDropOperationMode();
                }
                
                // 判断操作类型
                var firstPdf = files.FirstOrDefault(IsPdfFile);
                var dropEffect = DetermineDropEffect(firstPdf, mode);
                
                // 确保效果被允许，优先使用请求的效果，如果不可用则回退
                // 这样可以让Windows系统自动显示拖拽提示文字（"复制到..."、"移动到..."）
                if ((e.AllowedEffect & dropEffect) != DragDropEffects.None)
                {
                    e.Effect = dropEffect;
                }
                else if ((e.AllowedEffect & DragDropEffects.Copy) != DragDropEffects.None)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else if ((e.AllowedEffect & DragDropEffects.Move) != DragDropEffects.None)
                {
                    e.Effect = DragDropEffects.Move;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
                
                // 如果启用了 Popcat 功能，切换到空图标；否则使用背景色
                if (_popcatEnabled && _popcatIcon != null)
                {
                    _currentIconFileName = "cat_empty.ico";
                    LoadPopcatIcon(_currentIconFileName);
                }
                else
                {
                    BackColor = _dragBackColor;
                }
                
                // 根据操作类型显示不同的提示文本
                string operationText = dropEffect == DragDropEffects.Move ? "移动" : "复制";
                _label.Text = $"松开以{operationText}";
            }
            else
            {
                // 即使不是 PDF 文件，如果启用了 Popcat 功能，也允许拖拽
                if (_popcatEnabled && files != null && files.Length > 0)
                {
                    LogHelper.Info($"拖拽进入：检测到非 PDF 文件但 Popcat 功能已启用，文件数量: {files.Length}");
                    // 确保效果被允许
                    if ((e.AllowedEffect & DragDropEffects.Copy) != DragDropEffects.None)
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }
                    
                    // 切换到空图标
                    if (_popcatIcon != null)
                    {
                        _currentIconFileName = "cat_empty.ico";
                        LoadPopcatIcon(_currentIconFileName);
                    }
                    _label.Text = "松开以切换图标";
                }
                else
                {
                    LogHelper.Info($"拖拽进入：不是 PDF 文件且 Popcat 功能未启用");
                    e.Effect = DragDropEffects.None;
                }
            }
        }

        private void OnDragOver(object sender, DragEventArgs e)
        {
            // 在拖拽过程中持续更新操作类型，让Windows系统自动显示拖拽提示文字和光标
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.None;
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Any(IsPdfFile))
            {
                // 获取当前操作模式
                DropOperationMode mode = _dropOperationMode;
                if (_getDropOperationMode != null)
                {
                    mode = _getDropOperationMode();
                }
                
                // 判断操作类型
                var firstPdf = files.FirstOrDefault(IsPdfFile);
                var dropEffect = DetermineDropEffect(firstPdf, mode);
                
                // 确保效果被允许，优先使用请求的效果，如果不可用则回退
                if ((e.AllowedEffect & dropEffect) != DragDropEffects.None)
                {
                    e.Effect = dropEffect;
                }
                else if ((e.AllowedEffect & DragDropEffects.Copy) != DragDropEffects.None)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else if ((e.AllowedEffect & DragDropEffects.Move) != DragDropEffects.None)
                {
                    e.Effect = DragDropEffects.Move;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else if (_popcatEnabled && files != null && files.Length > 0)
            {
                // 即使不是 PDF 文件，如果启用了 Popcat 功能，也允许拖拽
                if ((e.AllowedEffect & DragDropEffects.Copy) != DragDropEffects.None)
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void OnDragLeave(object sender, EventArgs e)
        {
            // 如果启用了 Popcat 功能，切换回满图标；否则恢复背景色
            if (_popcatEnabled && _popcatIcon != null)
            {
                _currentIconFileName = "cat_full.ico";
                LoadPopcatIcon(_currentIconFileName);
            }
            else
            {
                BackColor = _defaultBackColor;
            }
            _label.Text = "拖入PDF";
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            LogHelper.Info("OnDragDrop 事件触发");
            
            // 如果启用了 Popcat 功能，切换回满图标；否则恢复背景色
            if (_popcatEnabled && _popcatIcon != null)
            {
                _currentIconFileName = "cat_full.ico";
                LoadPopcatIcon(_currentIconFileName);
            }
            else
            {
                BackColor = _defaultBackColor;
            }
            _label.Text = "拖入PDF";

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                LogHelper.Info("拖拽数据不包含文件");
                return;
            }

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null)
            {
                LogHelper.Info("拖拽文件列表为空");
                return;
            }

            LogHelper.Info($"检测到 {files.Length} 个文件被拖入，Popcat 功能状态: {_popcatEnabled}");

            // 执行原有的 PDF 导入功能
            var firstPdf = files.FirstOrDefault(IsPdfFile);
            if (string.IsNullOrEmpty(firstPdf))
            {
                LogHelper.Info("拖拽的文件中没有 PDF 文件");
                return;
            }

            // 获取当前操作模式并判断操作类型
            DropOperationMode mode = _dropOperationMode;
            if (_getDropOperationMode != null)
            {
                mode = _getDropOperationMode();
            }
            var dropEffect = DetermineDropEffect(firstPdf, mode);
            
            LogHelper.Info($"找到 PDF 文件: {firstPdf}, 操作类型: {dropEffect}");
            _onPdfDropped?.Invoke(firstPdf, dropEffect);
        }

        /// <summary>
        /// 从当前主题应用设置
        /// </summary>
        private void ApplyThemeSettings()
        {
            try
            {
                var themeManager = Services.ServiceLocator.Instance.GetThemeManager();
                if (themeManager != null)
                {
                    var theme = themeManager.GetCurrentTheme();
                    if (theme != null)
                    {
                        LogHelper.Info($"开始应用主题设置 - 主题名称: {theme.Name}");
                        
                        _defaultBackColor = theme.FloatingDropZoneBackColor;
                        _dragBackColor = theme.FloatingDropZoneBackColorDrag;
                        
                        // 保存透明度设置（不直接设置 Form.Opacity，而是使用 SetLayeredWindowAttributes）
                        _opacity = Math.Max(0.0, Math.Min(1.0, theme.FloatingDropZoneOpacity));
                        LogHelper.Info($"应用主题设置 - 背景色: {_defaultBackColor}, 透明度: {_opacity}");
                        
                        // 应用背景色
                        BackColor = _defaultBackColor;
                        
                        // 初始化 Popcat 功能（仅用于悬浮窗口图标显示）
                        _popcatEnabled = theme.FloatingDropZonePopcatEnabled;

                        // 更新 Popcat 图标显示
                        UpdatePopcatIconDisplay();
                        
                        // Popcat模式下确保窗口为正方形
                        EnsureSquareWindow();
                    }
                    else
                    {
                        LogHelper.Warn("主题为 null，使用默认设置");
                        // 使用默认值
                        ApplyDefaultSettings();
                    }
                }
                else
                {
                    LogHelper.Warn("ThemeManager 为 null，使用默认设置");
                    // 使用默认值
                    ApplyDefaultSettings();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"应用主题设置时发生错误: {ex.Message}, 堆栈: {ex.StackTrace}");
                // 使用默认值
                ApplyDefaultSettings();
            }
        }

        /// <summary>
        /// 应用默认设置
        /// </summary>
        private void ApplyDefaultSettings()
        {
            _defaultBackColor = Color.FromArgb(70, 130, 180);
            _dragBackColor = Color.FromArgb(50, 100, 150);
            BackColor = _defaultBackColor;
            _opacity = 0.92;
        }

        /// <summary>
        /// 更新主题设置（用于主题切换时）
        /// </summary>
        public void UpdateThemeSettings()
        {
            LogHelper.Info("开始更新悬浮拖拽窗口主题设置");
            
            // 保存旧的 Popcat 状态
            bool oldPopcatEnabled = _popcatEnabled;
            
            ApplyThemeSettings();
            
            // Popcat 功能状态改变时，更新图标显示（仅用于悬浮窗口图标显示）
            if (oldPopcatEnabled != _popcatEnabled)
            {
                LogHelper.Info($"Popcat 功能状态已改变: {oldPopcatEnabled} -> {_popcatEnabled}");
            }
            
            // 更新 Popcat 图标显示（会根据 Popcat 状态设置背景色）
            UpdatePopcatIconDisplay();
            
            // 如果不在拖拽状态且 Popcat 未启用，恢复默认背景色
            if (!_isDragging && !_popcatEnabled && BackColor != _defaultBackColor)
            {
                BackColor = _defaultBackColor;
            }
            
            // 重新应用透明度设置（因为透明度可能已更改）
            // 确保窗口已显示后再应用，避免显示异常
            if (IsHandleCreated && Visible)
            {
                SetTransparentWindow();
            }
            else if (IsHandleCreated)
            {
                // 窗口未显示，延迟到显示时再应用
                if (!_shownEventSubscribed)
                {
                    Shown += OnWindowShown;
                    _shownEventSubscribed = true;
                }
            }
            
            // 更新窗口尺寸
            UpdateWindowSizeFromTheme();
            LogHelper.Info("悬浮拖拽窗口主题设置更新完成");
        }

        /// <summary>
        /// 获取最接近的可用图标尺寸
        /// </summary>
        /// <param name="requestedSize">请求的尺寸</param>
        /// <returns>最接近的可用尺寸</returns>
        private int GetNearestAvailableSize(int requestedSize)
        {
            // 如果请求的尺寸在可用列表中，直接返回
            if (AvailableIconSizes.Contains(requestedSize))
                return requestedSize;
            
            // 找到最接近的尺寸
            int nearest = AvailableIconSizes[0];
            int minDiff = Math.Abs(requestedSize - nearest);
            
            foreach (int size in AvailableIconSizes)
            {
                int diff = Math.Abs(requestedSize - size);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    nearest = size;
                }
            }
            
            return nearest;
        }

        /// <summary>
        /// 加载 Popcat 图标
        /// </summary>
        private void LoadPopcatIcon(string iconFileName)
        {
            try
            {
                if (_popcatIcon == null) return;

                // 释放旧图标
                if (_popcatIcon.Image != null)
                {
                    _popcatIcon.Image.Dispose();
                    _popcatIcon.Image = null;
                }

                // 根据控件实际大小确定需要的图标尺寸
                // 使用控件的最大边作为图标尺寸，让Icon类自动从多尺寸ICO中选择最接近的尺寸
                int iconSize = Math.Max(_popcatIcon.Width, _popcatIcon.Height);
                if (iconSize <= 0)
                {
                    // 如果控件大小还未确定，使用窗口大小
                    iconSize = Math.Max(Width, Height);
                }
                if (iconSize <= 0)
                {
                    iconSize = 64; // 默认值
                }
                
                LogHelper.Info($"加载 Popcat 图标，控件大小: {_popcatIcon.Width}x{_popcatIcon.Height}, 窗口大小: {Width}x{Height}, 使用图标尺寸: {iconSize}, 文件名: {iconFileName}");
                
                // 直接加载 cat_full.ico 或 cat_empty.ico（包含多尺寸）
                // Icon类会自动从多尺寸ICO中选择最接近 iconSize 的尺寸
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourcePaths = new[]
                {
                    $"WindowsFormsApp3.Resources.PopcatIcons.{iconFileName}",
                    $"Resources.PopcatIcons.{iconFileName}",
                    $"PopcatIcons.{iconFileName}",
                    // 后备：尝试旧的路径（兼容性）
                    $"WindowsFormsApp3.Resources.{iconFileName}",
                    $"Resources.{iconFileName}",
                    iconFileName
                };

                string resourcePath = null;
                foreach (var path in resourcePaths)
                {
                    var stream = assembly.GetManifestResourceStream(path);
                    if (stream != null)
                    {
                        resourcePath = path;
                        stream.Dispose();
                        LogHelper.Info($"从嵌入资源找到图标: {path}");
                        break;
                    }
                }

                if (resourcePath != null)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourcePath))
                    {
                        if (stream != null)
                        {
                            // 根据控件大小动态选择图标尺寸
                            // Icon类会自动从多尺寸ICO中选择最接近 iconSize 的尺寸
                            
                            // 尝试直接作为图像加载（如果图标文件实际上是PNG格式）
                            try
                            {
                                stream.Position = 0; // 重置流位置
                                Image loadedImage = Image.FromStream(stream);
                                Bitmap originalImage = new Bitmap(loadedImage);
                                
                                // 转换为32位ARGB格式，确保透明通道被保留
                                // DWM 会自动处理透明通道，但我们需要确保格式正确
                                Bitmap bitmap;
                                if (originalImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                                {
                                    // 已经是32位ARGB格式，直接克隆
                                    bitmap = originalImage.Clone(new Rectangle(0, 0, originalImage.Width, originalImage.Height), 
                                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                }
                                else
                                {
                                    // 转换为32位ARGB格式
                                    bitmap = new Bitmap(originalImage.Width, originalImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                    using (Graphics g = Graphics.FromImage(bitmap))
                                    {
                                        // 使用 SourceOver 模式以正确处理透明通道，避免重影问题
                                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                        g.Clear(Color.Transparent);
                                        g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);
                                    }
                                }
                                
                                originalImage.Dispose();
                                loadedImage.Dispose();
                                
                                _popcatIcon.Image = bitmap;
                                LogHelper.Info($"成功从流加载图像: {iconFileName}, 格式: {bitmap.PixelFormat}, 大小: {bitmap.Width}x{bitmap.Height}");
                            }
                            catch
                            {
                                // 如果直接加载失败，尝试作为图标加载
                                stream.Position = 0;
                                using (Icon icon = new Icon(stream, iconSize, iconSize))
                                {
                                    // 直接使用Icon.ToBitmap()，它应该保留透明通道
                                    Bitmap bitmap = icon.ToBitmap();
                                    // 如果位图不是32位ARGB格式，转换为32位ARGB
                                    if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                                    {
                                        Bitmap argbBitmap = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                        using (Graphics g = Graphics.FromImage(argbBitmap))
                                        {
                                            // 使用 SourceOver 模式以正确处理透明通道，避免重影问题
                                            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                            g.Clear(Color.Transparent);
                                            g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                                        }
                                        bitmap.Dispose();
                                        bitmap = argbBitmap;
                                    }
                                    _popcatIcon.Image = bitmap;
                                    LogHelper.Info($"成功加载图标: {iconFileName}, 大小: {bitmap.Width}x{bitmap.Height}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    // 如果资源中找不到，尝试从文件系统加载
                    LogHelper.Info($"嵌入资源中未找到图标，尝试从文件系统加载: {iconFileName}");
                    
                    // 优先从 PopcatIcons 文件夹加载
                    string iconPath = Path.Combine(Application.StartupPath, "Resources", "PopcatIcons", iconFileName);
                    if (!File.Exists(iconPath))
                    {
                        // 尝试从项目资源目录加载
                        iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "PopcatIcons", iconFileName);
                    }
                    if (!File.Exists(iconPath))
                    {
                        // 后备：尝试旧的路径（兼容性）
                        iconPath = Path.Combine(Application.StartupPath, "Resources", iconFileName);
                    }
                    if (!File.Exists(iconPath))
                    {
                        // 尝试从项目资源目录加载（旧路径）
                        iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", iconFileName);
                    }
                    
                    if (File.Exists(iconPath))
                    {
                        LogHelper.Info($"从文件系统找到图标: {iconPath}");
                        
                        // 根据控件大小动态选择图标尺寸
                        // Icon类会自动从多尺寸ICO中选择最接近 iconSize 的尺寸
                        
                        // 尝试直接作为图像加载（如果图标文件实际上是PNG格式）
                        try
                        {
                            Image loadedImage = Image.FromFile(iconPath);
                            Bitmap originalImage = new Bitmap(loadedImage);
                            
                            // 转换为32位ARGB格式，确保透明通道被保留
                            // DWM 会自动处理透明通道，但我们需要确保格式正确
                            Bitmap bitmap;
                            if (originalImage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                            {
                                // 已经是32位ARGB格式，直接克隆
                                bitmap = originalImage.Clone(new Rectangle(0, 0, originalImage.Width, originalImage.Height), 
                                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                            }
                            else
                            {
                                // 转换为32位ARGB格式
                                bitmap = new Bitmap(originalImage.Width, originalImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                using (Graphics g = Graphics.FromImage(bitmap))
                                {
                                    // 使用 SourceOver 模式以正确处理透明通道，避免重影问题
                                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                    g.Clear(Color.Transparent);
                                    g.DrawImage(originalImage, 0, 0, originalImage.Width, originalImage.Height);
                                }
                            }
                            
                            originalImage.Dispose();
                            loadedImage.Dispose();
                            
                            _popcatIcon.Image = bitmap;
                            LogHelper.Info($"成功从文件加载图像: {iconPath}, 格式: {bitmap.PixelFormat}, 大小: {bitmap.Width}x{bitmap.Height}");
                        }
                        catch
                        {
                            // 如果直接加载失败，尝试作为图标加载
                            using (Icon icon = new Icon(iconPath, iconSize, iconSize))
                            {
                                // 直接使用Icon.ToBitmap()，它应该保留透明通道
                                Bitmap bitmap = icon.ToBitmap();
                                // 如果位图不是32位ARGB格式，转换为32位ARGB
                                if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
                                {
                                    Bitmap argbBitmap = new Bitmap(bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                                    using (Graphics g = Graphics.FromImage(argbBitmap))
                                    {
                                        // 使用 SourceOver 模式以正确处理透明通道，避免重影问题
                                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                                        g.Clear(Color.Transparent);
                                        g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
                                    }
                                    bitmap.Dispose();
                                    bitmap = argbBitmap;
                                }
                                _popcatIcon.Image = bitmap;
                                LogHelper.Info($"成功从文件加载图标: {iconPath}, 大小: {bitmap.Width}x{bitmap.Height}");
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Warn($"未找到 Popcat 图标文件: {iconFileName} (尝试的路径: {iconPath})");
                    }
                }
                
                // 图标加载后，如果使用 DWM 透明效果，需要刷新窗口
                if (_popcatEnabled && IsHandleCreated && _popcatIcon != null && _popcatIcon.Image != null && _transparencyMode != TransparencyMode.None)
                {
                    // DWM 会自动处理透明效果，只需要刷新窗口
                    Invalidate();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载 Popcat 图标失败: {ex.Message}, 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 更新 Popcat 图标显示
        /// </summary>
        private void UpdatePopcatIconDisplay()
        {
            try
            {
                if (_popcatIcon == null) return;

                if (_popcatEnabled)
                {
                    // 启用 Popcat 功能时，显示图标，隐藏文本标签，移除背景色
                    // 先加载图标，确保图标加载完成
                    _currentIconFileName = "cat_full.ico";
                    LoadPopcatIcon(_currentIconFileName);
                    
                    // 检查图标是否成功加载
                    if (_popcatIcon.Image == null)
                    {
                        LogHelper.Warn("UpdatePopcatIconDisplay: 图标加载失败，无法启用 Popcat 功能");
                        _popcatEnabled = false;
                        _label.Visible = true;
                        _label.Text = "拖入PDF";
                        BackColor = _defaultBackColor;
                        return;
                    }
                    
                    // Popcat模式下确保窗口为正方形
                    EnsureSquareWindow();
                    
                    _popcatIcon.Visible = false; // 隐藏 PictureBox，使用自定义绘制
                    _label.Visible = false;
                    
                    // 启用双缓冲和自定义绘制
                    SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
                    
                    // 先使用默认背景色，确保窗口正常显示
                    // 透明效果将在窗口完全显示后（Shown 事件）应用，避免显示异常
                    BackColor = _defaultBackColor;
                    TransparencyKey = Color.Empty;
                    
                    // 使用 Windows API 设置透明窗口（在窗口句柄创建后）
                    // 注意：不在窗口显示前设置透明度，避免显示异常
                    // 透明度设置将在窗口完全显示后（Shown 事件）应用
                    if (!_shownEventSubscribed)
                    {
                        Shown += OnWindowShown;
                        _shownEventSubscribed = true;
                    }
                    
                    // 如果窗口句柄已创建且窗口已显示，延迟应用透明效果
                    // 使用 BeginInvoke 确保在窗口完全显示后再应用
                    if (IsHandleCreated && Visible)
                    {
                        BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                SetTransparentWindow();
                            }
                            catch (Exception ex)
                            {
                                LogHelper.Error($"延迟应用透明效果失败: {ex.Message}");
                            }
                        }));
                    }
                }
                else
                {
                    // 禁用 Popcat 功能时，隐藏图标，显示文本标签，恢复背景色
                    _popcatIcon.Visible = false;
                    _label.Visible = true;
                    
                    // 恢复背景色
                    BackColor = _defaultBackColor;
                    TransparencyKey = Color.Empty;
                    _transparencyMode = TransparencyMode.None;
                    
                    // 移除透明窗口样式
                    if (IsHandleCreated)
                    {
                        SetTransparentWindow();
                    }
                    
                    // 释放图标资源
                    if (_popcatIcon.Image != null)
                    {
                        _popcatIcon.Image.Dispose();
                        _popcatIcon.Image = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"更新 Popcat 图标显示失败: {ex.Message}, 堆栈: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 窗口显示时的事件处理
        /// </summary>
        private void OnWindowShown(object sender, EventArgs e)
        {
            try
            {
                if (IsHandleCreated && Visible)
                {
                    // 窗口完全显示后，再应用透明效果，确保窗口正常显示
                    // 使用 BeginInvoke 确保在窗口完全显示后再执行
                    BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            if (_popcatEnabled)
                            {
                                // 重新应用透明效果，确保窗口正常显示
                                SetTransparentWindow();
                                
                                // DWM 透明效果会自动处理，只需要刷新窗口
                                if (_transparencyMode != TransparencyMode.None)
                                {
                                    Invalidate();
                                    LogHelper.Info($"OnWindowShown: 使用 {_transparencyMode} 透明效果，窗口已刷新");
                                }
                                else
                                {
                                    LogHelper.Info("OnWindowShown: 未使用透明效果，窗口应已正确显示");
                                }
                            }
                            else
                            {
                                // 非 Popcat 模式，确保透明度设置正确
                                SetTransparentWindow();
                                Invalidate();
                                LogHelper.Info("OnWindowShown: 已应用透明度设置");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error($"OnWindowShown 延迟处理失败: {ex.Message}");
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"OnWindowShown 处理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从主题更新窗口尺寸
        /// </summary>
        private void UpdateWindowSizeFromTheme()
        {
            try
            {
                var themeManager = Services.ServiceLocator.Instance.GetThemeManager();
                if (themeManager != null)
                {
                    var theme = themeManager.GetCurrentTheme();
                    if (theme != null)
                    {
                        // 获取主题的默认尺寸
                        int themeWidth = theme.FloatingDropZoneDefaultWidth;
                        int themeHeight = theme.FloatingDropZoneDefaultHeight;

                        // 不检查保存的尺寸，直接使用主题尺寸
                        // 但应用合理的最小值限制（16x16）以确保基本可见性和可操作性
                        const int minThemeSize = 16;
                        themeWidth = Math.Max(minThemeSize, Math.Min(MaximumSize.Width, themeWidth));
                        themeHeight = Math.Max(minThemeSize, Math.Min(MaximumSize.Height, themeHeight));

                        // 总是更新窗口尺寸为主题默认值
                        // 如果用户想要保留手动调整的尺寸，可以在调整后重新保存
                        this.Size = new Size(themeWidth, themeHeight);
                        LogHelper.Info($"窗口尺寸已更新为主题默认值: {themeWidth}x{themeHeight}");
                        
                        // 如果 Popcat 功能已启用，更新图标大小
                        // 注意：无论 _popcatIcon.Visible 是否为 true，都需要更新图标大小
                        if (_popcatEnabled && _popcatIcon != null)
                        {
                            _currentIconFileName = "cat_full.ico";
                            LoadPopcatIcon(_currentIconFileName);
                            // 强制刷新窗口，确保图标正确显示
                            Invalidate();
                            Update();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn($"更新窗口尺寸失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 重写 OnPaintBackground 以实现透明背景（当 Popcat 功能启用时）
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (_popcatEnabled)
            {
                // Popcat 功能启用时
                if (_transparencyMode == TransparencyMode.Acrylic || _transparencyMode == TransparencyMode.Dwm)
                {
                    // 使用 DWM 透明效果时，背景会自动透明，不需要绘制
                    // 让 DWM 处理背景
                    return;
                }
                else
                {
                    // 使用回退方案（SetLayeredWindowAttributes）时，使用透明键颜色填充背景
                    e.Graphics.Clear(BackColor);
                    return;
                }
            }
            
            // Popcat 功能未启用时，绘制背景色
            // 使用分层窗口时，背景色会通过 SetLayeredWindowAttributes 的 LWA_ALPHA 实现透明度
            e.Graphics.Clear(BackColor);
        }
        
        /// <summary>
        /// 重写 OnPaint 以直接绘制透明图像
        /// 使用 DWM 透明效果时，需要绘制图标以显示在透明背景上
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            // 如果启用了 Popcat 功能
            if (_popcatEnabled)
            {
                // 绘制图标（DWM 会自动处理透明背景）
                if (_popcatIcon != null && _popcatIcon.Image != null)
                {
                    Graphics g = e.Graphics;
                    
                    // 先清除绘制区域，避免重影
                    g.Clear(Color.Transparent);
                    
                    // 设置高质量渲染参数
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    // 使用 PixelOffsetMode.Half 避免像素偏移导致的重影
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    
                    Bitmap image = _popcatIcon.Image as Bitmap;
                    if (image != null)
                    {
                        float scaleX = (float)ClientSize.Width / image.Width;
                        float scaleY = (float)ClientSize.Height / image.Height;
                        float scale = Math.Min(scaleX, scaleY);
                        
                        int drawWidth = (int)(image.Width * scale);
                        int drawHeight = (int)(image.Height * scale);
                        int drawX = (ClientSize.Width - drawWidth) / 2;
                        int drawY = (ClientSize.Height - drawHeight) / 2;
                        
                        // 小尺寸时使用高质量双线性插值，避免重影同时保持质量
                        // 大尺寸时使用高质量双三次插值
                        if (drawWidth < 128 || drawHeight < 128)
                        {
                            // 小尺寸时使用双线性插值，避免双三次插值可能导致的边缘重影
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        }
                        else
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        }
                        
                        // 使用 ImageAttributes 确保透明通道正确处理
                        using (System.Drawing.Imaging.ImageAttributes imgAttributes = new System.Drawing.Imaging.ImageAttributes())
                        {
                            imgAttributes.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                            g.DrawImage(
                                image,
                                new Rectangle(drawX, drawY, drawWidth, drawHeight),
                                0, 0, image.Width, image.Height,
                                GraphicsUnit.Pixel,
                                imgAttributes);
                        }
                    }
                    
                    // 如果启用了边框显示，绘制半透明边框
                    if (_showBorder)
                    {
                        // 使用半透明白色边框，足够可见但不突兀
                        Color borderColor = Color.FromArgb(150, 255, 255, 255);
                        using (Pen borderPen = new Pen(borderColor, 1))
                        {
                            g.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
                        }
                    }
                    
                    return;
                }
            }
            
            // Popcat 功能未启用时，使用标准绘制
            base.OnPaint(e);
        }

        /// <summary>
        /// 重写 Show 方法，确保窗口显示时先使用默认背景色，然后再应用透明效果
        /// </summary>
        public new void Show()
        {
            // 在显示前，先确保使用默认背景色，避免显示多余内容
            // 移除所有透明效果，恢复普通窗口显示
            if (IsHandleCreated)
            {
                try
                {
                    // 禁用 DWM 透明效果
                    if (_transparencyMode != TransparencyMode.None)
                    {
                        FloatingDropZoneDwm.DisableDwmEffect(this.Handle);
                        _transparencyMode = TransparencyMode.None;
                    }
                    
                    // 移除分层窗口样式
                    int exStyle = FloatingDropZoneNative.GetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE);
                    if ((exStyle & FloatingDropZoneNative.WS_EX_LAYERED) != 0)
                    {
                        FloatingDropZoneNative.SetWindowLong(this.Handle, FloatingDropZoneNative.GWL_EXSTYLE, 
                            exStyle & ~FloatingDropZoneNative.WS_EX_LAYERED);
                    }
                }
                catch { }
            }
            
            // 设置默认背景色
            if (_popcatEnabled)
            {
                // Popcat 模式：先使用默认背景色，显示后再应用透明效果
                BackColor = _defaultBackColor;
                TransparencyKey = Color.Empty;
            }
            else
            {
                // 非 Popcat 模式：使用默认背景色
                BackColor = _defaultBackColor;
                TransparencyKey = Color.Empty;
            }
            
            base.Show();
            
            // 强制刷新窗口，确保使用默认背景色显示
            if (IsHandleCreated)
            {
                Invalidate();
                Update();
                Refresh();
            }
            
            // 窗口显示后，延迟应用透明效果，确保窗口正常显示
            if (IsHandleCreated)
            {
                // 使用定时器延迟应用透明效果，避免阻塞UI线程
                if (_transparencyTimer == null)
                {
                    _transparencyTimer = new System.Windows.Forms.Timer();
                    _transparencyTimer.Interval = 150; // 150ms延迟，确保窗口已完全显示
                    _transparencyTimer.Tick += (s, e) =>
                    {
                        _transparencyTimer.Stop();
                        try
                        {
                            // 只有在需要透明度时才应用（Popcat模式或透明度小于100%）
                            if (_popcatEnabled || _opacity < 1.0)
                            {
                                SetTransparentWindow();
                            }
                            else
                            {
                                // 透明度为100%时，确保移除透明效果
                                SetTransparentWindow();
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error($"窗口显示后应用透明效果失败: {ex.Message}");
                        }
                    };
                }
                _transparencyTimer.Start();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 清理定时器
            if (_transparencyTimer != null)
            {
                _transparencyTimer.Stop();
                _transparencyTimer.Dispose();
                _transparencyTimer = null;
            }
            
            base.OnFormClosed(e);
        }

        /// <summary>
        /// 加载保存的窗口位置和尺寸，或使用默认值
        /// </summary>
        /// <param name="forceUseThemeSize">如果为true，强制使用主题尺寸，忽略保存的尺寸</param>
        private void LoadPersistedBoundsOrDefault(bool forceUseThemeSize = false)
        {
            try
            {
                var x = AppSettings.GetValue<int>($"{PersistKeyPrefix}.X", 200);
                var y = AppSettings.GetValue<int>($"{PersistKeyPrefix}.Y", 200);
                var w = AppSettings.GetValue<int>($"{PersistKeyPrefix}.W", 0);
                var h = AppSettings.GetValue<int>($"{PersistKeyPrefix}.H", 0);

                // 如果强制使用主题尺寸，或者未保存过尺寸，使用主题的默认尺寸
                if (forceUseThemeSize || w == 0 || h == 0)
                {
                    try
                    {
                        var themeManager = Services.ServiceLocator.Instance.GetThemeManager();
                        if (themeManager != null)
                        {
                            var theme = themeManager.GetCurrentTheme();
                            if (theme != null)
                            {
                                w = theme.FloatingDropZoneDefaultWidth;
                                h = theme.FloatingDropZoneDefaultHeight;
                            }
                        }
                    }
                    catch { }

                    // 如果主题也没有，使用硬编码默认值
                    if (w == 0) w = 180;
                    if (h == 0) h = 80;
                }

                // Clamp to allowed sizes
                // 应用合理的最小值限制（16x16）以确保基本可见性和可操作性
                const int minThemeSize = 16;
                if (w < minThemeSize) w = minThemeSize;
                if (h < minThemeSize) h = minThemeSize;
                if (w > MaximumSize.Width) w = MaximumSize.Width;
                if (h > MaximumSize.Height) h = MaximumSize.Height;

                Size = new Size(w, h);
                Location = new Point(x, y);
            }
            catch
            {
                Size = new Size(180, 80);
                Location = new Point(200, 200);
            }
        }

        /// <summary>
        /// 加载边框显示设置
        /// </summary>
        private void LoadBorderSetting()
        {
            try
            {
                if (!AppSettings.IsInitialized)
                {
                    return;
                }
                _showBorder = AppSettings.GetValue<bool>($"{PersistKeyPrefix}.ShowBorder", false);
            }
            catch
            {
                _showBorder = false;
            }
        }

        /// <summary>
        /// 保存边框显示设置
        /// </summary>
        private void SaveBorderSetting()
        {
            try
            {
                if (!AppSettings.IsInitialized)
                {
                    return;
                }
                AppSettings.SetValue($"{PersistKeyPrefix}.ShowBorder", _showBorder);
                AppSettings.Save();
            }
            catch { }
        }

        /// <summary>
        /// 设置拖拽操作模式
        /// </summary>
        public void SetDropOperationMode(DropOperationMode mode)
        {
            _dropOperationMode = mode;
        }
        
        /// <summary>
        /// 设置目标目录（用于判断分区）
        /// </summary>
        public void SetTargetDirectory(string targetDirectory)
        {
            _targetDirectory = targetDirectory ?? string.Empty;
        }

        /// <summary>
        /// 判断拖拽操作类型（复制或移动）
        /// </summary>
        private DragDropEffects DetermineDropEffect(string sourceFile, DropOperationMode mode)
        {
            switch (mode)
            {
                case DropOperationMode.ForceCopy:
                    return DragDropEffects.Copy;
                case DropOperationMode.ForceMove:
                    return DragDropEffects.Move;
                case DropOperationMode.Default:
                default:
                    // 智能判断：同分区→移动，跨分区→复制
                    try
                    {
                        if (string.IsNullOrEmpty(_targetDirectory))
                        {
                            // 如果没有目标目录，默认复制（更安全）
                            return DragDropEffects.Copy;
                        }
                        
                        var sourceDrive = Path.GetPathRoot(sourceFile);
                        var targetDrive = Path.GetPathRoot(_targetDirectory);
                        
                        if (string.IsNullOrEmpty(sourceDrive) || string.IsNullOrEmpty(targetDrive))
                        {
                            return DragDropEffects.Copy; // 默认复制，更安全
                        }
                        
                        return string.Equals(sourceDrive, targetDrive, StringComparison.OrdinalIgnoreCase)
                            ? DragDropEffects.Move
                            : DragDropEffects.Copy;
                    }
                    catch
                    {
                        return DragDropEffects.Copy; // 默认复制，更安全
                    }
            }
        }


        /// <summary>
        /// 确保窗口为正方形（Popcat模式下）
        /// </summary>
        private void EnsureSquareWindow()
        {
            if (_popcatEnabled && IsHandleCreated)
            {
                int size = Math.Max(Width, Height);
                if (Width != size || Height != size)
                {
                    Size = new Size(size, size);
                }
            }
        }

        private void PersistBounds()
        {
            try
            {
                // 设计期或未初始化时跳过
                if (!AppSettings.IsInitialized)
                {
                    return;
                }

                var bounds = this.Bounds;
                AppSettings.SetValue($"{PersistKeyPrefix}.X", bounds.X);
                AppSettings.SetValue($"{PersistKeyPrefix}.Y", bounds.Y);
                AppSettings.SetValue($"{PersistKeyPrefix}.W", bounds.Width);
                AppSettings.SetValue($"{PersistKeyPrefix}.H", bounds.Height);
                AppSettings.Save();
            }
            catch
            {
                // ignore
            }
        }

        private static bool IsPdfFile(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   File.Exists(path) &&
                   string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase);
        }

    }
}
