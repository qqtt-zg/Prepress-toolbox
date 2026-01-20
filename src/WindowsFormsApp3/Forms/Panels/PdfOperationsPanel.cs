using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Controls;
using WindowsFormsApp3.Forms.Utils;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// PDF操作面板 - 使用CefSharp + PDF.js提供现代化PDF预览体验
    /// 支持Windows 7及以上系统
    /// </summary>
    public partial class PdfOperationsPanel : BasePanelControl
    {
        #region Properties

        public override string PanelKey => "pdf_operations";
        public override string DisplayName => "PDF操作";
        public override string IconName => "FilePdfOutlined";

        private string _currentFilePath;
        private string _originalFilePath; // 原始文件路径
        private int _currentPage = 0;
        private int _totalPages = 0;
        
        // 历史栈系统
        private Stack<byte[]> _undoStack = new Stack<byte[]>(); // 撤回栈
        private Stack<byte[]> _redoStack = new Stack<byte[]>(); // 重做栈
        private byte[] _originalPdfBytes; // 原始PDF字节数组
        private byte[] _currentPdfBytes; // 当前显示的PDF字节数组
        
        private const int MAX_HISTORY_SIZE = 10; // 最大历史记录数
        
        // 进度条相关
        private DateTime _progressStartTime;
        private const int MIN_PROGRESS_DISPLAY_TIME_MS = 300; // 最小显示时间：300ms

        #endregion

        #region Constructor

        public PdfOperationsPanel()
        {
            InitializeComponent();
            
            // 确保进度条在最上层并覆盖整个状态栏宽度
            _progressOverlay.BringToFront();
            
            // 监听状态栏大小变化，动态调整进度条宽度
            statusPanel.SizeChanged += (s, e) =>
            {
                _progressOverlay.Width = statusPanel.Width;
            };
        }

        #endregion

        #region Initialization

        private void PdfOperationsPanel_Load(object sender, EventArgs e)
        {
            // 在面板加载后初始化浏览器
            InitializeBrowserAsync();
        }

        private async void InitializeBrowserAsync()
        {
            try
            {
                UpdateStatus("正在初始化PDF预览组件...");
                
                // 初始化CefSharp浏览器
                _pdfPreview.InitializeBrowser();
                
                UpdateStatus("就绪");
                LogHelper.Info("[PdfOperationsPanel] CefSharp浏览器初始化完成");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfOperationsPanel] 初始化失败: {ex.Message}", ex);
                UpdateStatus($"初始化失败: {ex.Message}");
                ShowError($"PDF预览组件初始化失败: {ex.Message}");
            }
        }

        #endregion

        #region CefSharp Event Handlers

        private void PdfPreview_PdfLoaded(object sender, PdfLoadedEventArgs e)
        {
            _totalPages = e.TotalPages;
            _currentPage = e.CurrentPage;
            
            LogHelper.Info($"[PdfOperationsPanel] PDF已加载，共{_totalPages}页");
            
            // 文件信息现在由 viewer.html 显示，不需要在此更新

            EnableControls(true);
            UpdatePageInfo();
            
            // 如果检查器窗口可见，加载检查器信息
            if (_inspectorForm != null && _inspectorForm.Visible && !string.IsNullOrEmpty(_currentFilePath))
            {
                _inspectorForm.LoadPdf(_currentFilePath, _currentPage);
            }
            
            // 获取并显示页面尺寸
            string statusText = $"已加载: {Path.GetFileName(_currentFilePath)}";
            try
            {
                if (IText7PdfTools.GetFirstPageSize(_currentFilePath, out double width, out double height))
                {
                    statusText += $" | 页面尺寸: {width:F1} × {height:F1} mm";
                }
            }
            catch { }
            
            UpdateStatus(statusText);
        }

        private void PdfPreview_PageChanged(object sender, PageChangedEventArgs e)
        {
            _currentPage = e.CurrentPage;
            _totalPages = e.TotalPages;
            UpdatePageInfo();
            
            // 同步检查器页面
            if (_inspectorForm != null && _inspectorForm.Visible && !string.IsNullOrEmpty(_currentFilePath))
            {
                _inspectorForm.SwitchToPage(_currentPage);
            }
        }

        private void PdfPreview_LoadError(object sender, PdfLoadErrorEventArgs e)
        {
            LogHelper.Error($"[PdfOperationsPanel] PDF加载错误: {e.Error}");
            ShowError($"PDF加载失败: {e.Error}");
            UpdateStatus("加载失败");
        }

        #endregion

        #region Event Handlers

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "PDF文件|*.pdf|所有文件|*.*";
                openDialog.Title = "选择PDF文件";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    _ = LoadPdfFileAsync(openDialog.FileName);
                }
            }
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                ShowWarning("没有打开的文件");
                return;
            }

            if (_currentPdfBytes == null || _currentPdfBytes == _originalPdfBytes)
            {
                ShowWarning("没有需要保存的更改");
                return;
            }

            try
            {
                _btnSave.Enabled = false;
                
                // 显示进度条
                ShowBottomProgress();
                UpdateBottomProgress(30);
                
                // 保存当前状态到原文件 (异步)
                await Task.Run(() => File.WriteAllBytes(_originalFilePath, _currentPdfBytes));
                
                UpdateBottomProgress(70);
                
                // 保存后更新原始字节并清除历史
                _originalPdfBytes = _currentPdfBytes;
                _undoStack.Clear();
                _redoStack.Clear();
                UpdateHistoryButtons();
                
                UpdateBottomProgress(100);
                
                ShowSuccess("文件已保存");
                UpdateStatus($"已保存: {Path.GetFileName(_originalFilePath)}");
                
                LogHelper.Info("[PdfOperationsPanel] 文件已保存，历史已清除");
                
                // 延迟隐藏进度条
                await Task.Delay(100);
                HideBottomProgress();
            }
            catch (Exception ex)
            {
                HideBottomProgress();
                LogHelper.Error($"[PdfOperationsPanel] 保存文件失败: {ex.Message}", ex);
                ShowError($"保存失败: {ex.Message}");
            }
            finally
            {
                // 恢复按钮状态（如果有必要，UpdateHistoryButtons已经处理了）
                UpdateHistoryButtons();
            }
        }

        private void BtnSaveAs_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                ShowWarning("没有打开的文件");
                return;
            }

            // 使用当前状态的数据
            byte[] dataToSave = _currentPdfBytes ?? _originalPdfBytes;

            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "PDF文件|*.pdf";
                bool hasChanges = _undoStack.Count > 0 || _redoStack.Count > 0;
                saveDialog.FileName = Path.GetFileNameWithoutExtension(_originalFilePath) + 
                    (hasChanges ? "_转曲.pdf" : "_副本.pdf");
                saveDialog.InitialDirectory = Path.GetDirectoryName(_originalFilePath);

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // 显示进度条
                        ShowBottomProgress();
                        UpdateBottomProgress(30);
                        
                        File.WriteAllBytes(saveDialog.FileName, dataToSave);
                        
                        UpdateBottomProgress(100);
                        
                        ShowSuccess($"文件已另存为:\n{Path.GetFileName(saveDialog.FileName)}");
                        
                        // 延迟隐藏进度条
                        Task.Delay(100).ContinueWith(_ => HideBottomProgress());
                        
                        // 询问是否打开新文件
                        var result = AntdUI.Modal.open(new AntdUI.Modal.Config(this.FindForm(), "打开文件", 
                            "是否打开另存的文件？")
                        {
                            Icon = AntdUI.TType.Info,
                            OkText = "打开",
                            CancelText = "取消"
                        });

                        if (result == DialogResult.OK)
                        {
                            _ = LoadPdfFileAsync(saveDialog.FileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        HideBottomProgress();
                        LogHelper.Error($"[PdfOperationsPanel] 另存为失败: {ex.Message}", ex);
                        ShowError($"另存为失败: {ex.Message}");
                    }
                }
            }
        }

        private void BtnUndo_Click(object sender, EventArgs e)
        {
            if (_undoStack.Count == 0)
            {
                return; // 按钮应该是禁用的，但以防万一
            }

            try
            {
                // 将当前状态压入重做栈
                _redoStack.Push(_currentPdfBytes);
                
                // 从撤回栈弹出上一个状态
                _currentPdfBytes = _undoStack.Pop();
                
                UpdateHistoryButtons();
                ShowPdfPreview(_currentPdfBytes, quickMode: true);
                
                bool isOriginal = _undoStack.Count == 0;
                UpdateStatus(isOriginal 
                    ? $"已加载: {Path.GetFileName(_originalFilePath)}"
                    : $"已撤回 (未保存) - {Path.GetFileName(_originalFilePath)}");
                
                LogHelper.Info($"[PdfOperationsPanel] 撤回操作，剩余历史: {_undoStack.Count}, 可重做: {_redoStack.Count}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfOperationsPanel] 撤回操作失败: {ex.Message}", ex);
                ShowError($"撤回失败: {ex.Message}");
            }
        }

        private void BtnRedo_Click(object sender, EventArgs e)
        {
            if (_redoStack.Count == 0)
            {
                return; // 按钮应该是禁用的，但以防万一
            }

            try
            {
                // 将当前状态压入撤回栈
                _undoStack.Push(_currentPdfBytes);
                
                // 从重做栈弹出
                _currentPdfBytes = _redoStack.Pop();
                
                UpdateHistoryButtons();
                ShowPdfPreview(_currentPdfBytes, quickMode: true);
                
                UpdateStatus($"已重做 (未保存) - {Path.GetFileName(_originalFilePath)}");
                
                LogHelper.Info($"[PdfOperationsPanel] 重做操作，历史: {_undoStack.Count}, 剩余重做: {_redoStack.Count}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfOperationsPanel] 重做操作失败: {ex.Message}", ex);
                ShowError($"重做失败: {ex.Message}");
            }
        }

        private async void BtnRotateCW_Click(object sender, EventArgs e)
        {
            await _pdfPreview.RotateClockwiseAsync();
        }

        private async void BtnRotateCCW_Click(object sender, EventArgs e)
        {
            await _pdfPreview.RotateCounterClockwiseAsync();
        }

        private async void BtnCropMarks_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                ShowWarning("请先打开PDF文件");
                return;
            }

            using (var dialog = new Forms.Utils.CropMarksDialog())
            {
                // TODO: 应用当前主题，现在使用默认设置
                // dialog.ApplyTheme(false); // 默认浅色主题
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await ApplyCropMarksAsync(dialog.Options);
                }
            }
        }

        private async void BtnRegMarks_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                ShowWarning("请先打开PDF文件");
                return;
            }

            using (var dialog = new Forms.Utils.RegMarksDialog())
            {
                // TODO: 应用当前主题，现在使用默认设置
                // dialog.ApplyTheme(false); // 默认浅色主题
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    await ApplyRegMarksAsync(dialog.Options);
                }
            }
        }

        private void BtnInspector_Click(object sender, EventArgs e)
        {
            ShowInspector();
        }

        private void Inspector_PageSelected(object sender, int pageNumber)
        {
            // 同步PDF预览到选中的页面
            if (_pdfPreview != null && pageNumber >= 1 && pageNumber <= _totalPages)
            {
                _currentPage = pageNumber;
                _ = _pdfPreview.GoToPageAsync(pageNumber);
            }
        }

        #endregion

        #region Inspector Operations

        /// <summary>
        /// 显示检查器窗口
        /// </summary>
        private void ShowInspector()
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                ShowWarning("请先打开PDF文件");
                return;
            }

            // 创建或显示检查器窗口
            if (_inspectorForm == null || _inspectorForm.IsDisposed)
            {
                _inspectorForm = new PdfInspectorForm
                {
                    Owner = this.FindForm() // 设置主窗口为所有者
                };
                _inspectorForm.PageSelected += Inspector_PageSelected;
                _inspectorForm.FontOutlineStarted += Inspector_FontOutlineStarted;
                _inspectorForm.FontOutlineCompleted += Inspector_FontOutlineCompleted;
            }

            // 加载当前PDF
            _inspectorForm.LoadPdf(_currentFilePath, _currentPage);

            // 显示窗口
            _inspectorForm.Show();

            LogHelper.Debug($"[PdfOperationsPanel] 检查器窗口已显示");
        }

        /// <summary>
        /// 处理检查器的字体转曲开始事件
        /// </summary>
        private void Inspector_FontOutlineStarted(object sender, EventArgs e)
        {
            // 显示进度条
            ShowBottomProgress();
            UpdateBottomProgress(10);
            UpdateStatus("正在转曲，请稍候...");
        }

        /// <summary>
        /// 处理检查器的字体转曲完成事件
        /// </summary>
        private void Inspector_FontOutlineCompleted(object sender, WindowsFormsApp3.Forms.Controls.FontOutlineCompletedEventArgs e)
        {
            try
            {
                LogHelper.Info("[PdfOperationsPanel] 收到字体转曲完成事件");
                
                // 显示进度条
                ShowBottomProgress();
                UpdateBottomProgress(20);
                
                // 将当前状态压入撤回栈
                _undoStack.Push(_currentPdfBytes);
                
                // 限制历史栈大小
                LimitHistorySize();
                
                UpdateBottomProgress(40);
                
                // 更新当前状态
                _currentPdfBytes = e.PdfBytes;
                
                // 清除重做栈（新操作清除重做历史）
                _redoStack.Clear();
                
                UpdateBottomProgress(60);
                
                UpdateHistoryButtons();
                
                // 更新状态栏
                UpdateStatus($"已转曲 (未保存) - {Path.GetFileName(_originalFilePath)}");
                
                UpdateBottomProgress(80);
                
                // 显示转曲后的PDF
                ShowPdfPreview(_currentPdfBytes);
                
                UpdateBottomProgress(100);
                
                LogHelper.Info($"[PdfOperationsPanel] 转曲完成，历史栈深度: {_undoStack.Count}");
                
                // 延迟隐藏进度条（ShowPdfPreview会显示自己的进度条）
                Task.Delay(100).ContinueWith(_ => HideBottomProgress());
            }
            catch (Exception ex)
            {
                HideBottomProgress();
                LogHelper.Error($"[PdfOperationsPanel] 处理转曲完成事件失败: {ex.Message}", ex);
                ShowError($"加载转曲后的PDF失败: {ex.Message}");
            }
        }

        #endregion

        #region PDF Operations

        /// <summary>
        /// 异步加载PDF文件
        /// </summary>
        private async Task LoadPdfFileAsync(string filePath)
        {
            try
            {
                // 显示进度条
                ShowBottomProgress();
                UpdateBottomProgress(10);
                
                _currentFilePath = filePath;
                _originalFilePath = filePath;
                
                UpdateBottomProgress(20);
                
                // 读取原始PDF字节
                _originalPdfBytes = File.ReadAllBytes(filePath);
                _currentPdfBytes = _originalPdfBytes;
                
                UpdateBottomProgress(40);
                
                // 清除历史栈
                _undoStack.Clear();
                _redoStack.Clear();
                UpdateHistoryButtons();
                
                UpdateBottomProgress(50);
                UpdateStatus($"正在加载: {Path.GetFileName(filePath)}");

                // 使用CefSharp加载PDF
                await _pdfPreview.LoadPdfAsync(filePath);
                
                UpdateBottomProgress(100);
                
                LogHelper.Info($"[PdfOperationsPanel] PDF已加载: {filePath}");
                
                // 等待一小段时间让用户看到100%
                await Task.Delay(100);
                HideBottomProgress();
            }
            catch (Exception ex)
            {
                HideBottomProgress();
                ShowError($"加载PDF失败: {ex.Message}");
                UpdateStatus("加载失败");
            }
        }

        /// <summary>
        /// 应用裁切标记
        /// </summary>
        private async Task ApplyCropMarksAsync(CropMarksOptions options)
        {
            try
            {
                // 显示进度条
                ShowBottomProgress();
                UpdateBottomProgress(10);
                UpdateStatus("正在添加裁切标记...");

                string outputPath = GenerateOutputPath(_currentFilePath, "_CropMarks");
                bool success = await Task.Run(() =>
                    PdfPrepressMarks.AddCropMarks(_currentFilePath, outputPath, options)
                );

                if (success)
                {
                    // 重新加载处理后的PDF
                    await LoadPdfFileAsync(outputPath);
                    _currentFilePath = outputPath;

                    ShowSuccess("裁切标记添加成功");
                    UpdateStatus("就绪");
                }
                else
                {
                    ShowError("添加裁切标记失败");
                    UpdateStatus("操作失败");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfOperationsPanel] 添加裁切标记异常: {ex.Message}", ex);
                ShowError($"添加裁切标记失败: {ex.Message}");
                UpdateStatus("操作失败");
            }
        }

        /// <summary>
        /// 应用套准标记
        /// </summary>
        private async Task ApplyRegMarksAsync(RegMarksOptions options)
        {
            try
            {
                // 显示进度条
                ShowBottomProgress();
                UpdateBottomProgress(10);
                UpdateStatus("正在添加套准标记...");

                string outputPath = GenerateOutputPath(_currentFilePath, "_RegMarks");
                bool success = await Task.Run(() =>
                    PdfPrepressMarks.AddRegistrationMarks(_currentFilePath, outputPath, options)
                );

                if (success)
                {
                    // 重新加载处理后的PDF
                    await LoadPdfFileAsync(outputPath);
                    _currentFilePath = outputPath;

                    ShowSuccess("套准标记添加成功");
                    UpdateStatus("就绪");
                }
                else
                {
                    ShowError("添加套准标记失败");
                    UpdateStatus("操作失败");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[PdfOperationsPanel] 添加套准标记异常: {ex.Message}", ex);
                ShowError($"添加套准标记失败: {ex.Message}");
                UpdateStatus("操作失败");
            }
        }

        /// <summary>
        /// 生成输出文件路径
        /// </summary>
        private string GenerateOutputPath(string originalPath, string suffix)
        {
            string directory = Path.GetDirectoryName(originalPath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            return Path.Combine(directory, $"{fileNameWithoutExt}{suffix}{extension}");
        }

        /// <summary>
        /// 更新页面信息
        /// </summary>
        private void UpdatePageInfo()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(UpdatePageInfo));
                return;
            }

            // 页码信息现在由 viewer.html 中的工具栏显示，不需要在此更新
        }

        /// <summary>
        /// 启用/禁用控件
        /// </summary>
        private void EnableControls(bool enabled)
        {
            // 保存按钮：有历史记录就可以保存
            bool hasChanges = _undoStack.Count > 0 || _redoStack.Count > 0;
            _btnSave.Enabled = enabled && hasChanges;
            _btnSaveAs.Enabled = enabled;
            _btnUndo.Enabled = enabled && _undoStack.Count > 0;
            _btnRedo.Enabled = enabled && _redoStack.Count > 0;
            _btnCropMarks.Enabled = enabled;
            _btnRegMarks.Enabled = enabled;
            _btnInspector.Enabled = enabled;
        }

        /// <summary>
        /// 更新历史按钮状态和文本
        /// </summary>
        private void UpdateHistoryButtons()
        {
            _btnUndo.Enabled = _undoStack.Count > 0;
            _btnRedo.Enabled = _redoStack.Count > 0;
            
            // 显示操作计数
            _btnUndo.Text = _undoStack.Count > 0 ? $"撤回 ({_undoStack.Count})" : "撤回";
            _btnRedo.Text = _redoStack.Count > 0 ? $"重做 ({_redoStack.Count})" : "重做";
            
            // 更新保存按钮
            bool hasChanges = _undoStack.Count > 0 || _redoStack.Count > 0;
            _btnSave.Enabled = hasChanges;
        }

        /// <summary>
        /// 限制历史栈大小
        /// </summary>
        private void LimitHistorySize()
        {
            if (_undoStack.Count > MAX_HISTORY_SIZE)
            {
                var items = _undoStack.ToList();
                items.RemoveAt(items.Count - 1); // 移除最旧的
                _undoStack = new Stack<byte[]>(items.AsEnumerable().Reverse());
                
                LogHelper.Debug($"[PdfOperationsPanel] 历史栈已限制到 {MAX_HISTORY_SIZE} 项");
            }
        }

        /// <summary>
        /// 显示PDF预览（支持快速模式）
        /// </summary>
        private async void ShowPdfPreview(byte[] pdfBytes, bool quickMode = false)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), 
                    $"temp_preview_{Guid.NewGuid()}.pdf");
                File.WriteAllBytes(tempPath, pdfBytes);
                
                // 更新当前文件路径为临时文件
                _currentFilePath = tempPath;
                
                // 根据模式选择加载函数
                if (quickMode)
                {
                    // 快速模式：显示进度条并使用 loadPdfQuick
                    ShowBottomProgress();
                    UpdateBottomProgress(10);
                    
                    string fileUrl = $"file:///{tempPath.Replace("\\", "/")}";
                    await _pdfPreview.ExecuteScriptAsync($"loadPdfQuick('{fileUrl}');");
                    
                    // 模拟进度更新（实际进度由JS控制）
                    await Task.Delay(50);
                    UpdateBottomProgress(30);
                    await Task.Delay(100);
                    UpdateBottomProgress(60);
                    await Task.Delay(100);
                    UpdateBottomProgress(90);
                    await Task.Delay(50);
                    UpdateBottomProgress(100);
                    await Task.Delay(100);
                    HideBottomProgress();
                }
                else
                {
                    // 标准模式：也显示进度条
                    ShowBottomProgress();
                    UpdateBottomProgress(20);
                    
                    await _pdfPreview.LoadPdfAsync(tempPath);
                    
                    UpdateBottomProgress(100);
                    await Task.Delay(100);
                    HideBottomProgress();
                }
                
                // 如果检查器窗口打开，重新加载检查器
                if (_inspectorForm != null && _inspectorForm.Visible)
                {
                    _inspectorForm.LoadPdf(tempPath, _currentPage);
                }
                
                LogHelper.Debug($"[PdfOperationsPanel] 显示预览: {(quickMode ? "快速模式" : "标准模式")}");
            }
            catch (Exception ex)
            {
                HideBottomProgress();
                LogHelper.Error($"[PdfOperationsPanel] 显示预览失败: {ex.Message}", ex);
                ShowError($"显示预览失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示底部进度条
        /// </summary>
        private void ShowBottomProgress()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(ShowBottomProgress));
                return;
            }
            
            _progressOverlay.StartIndeterminate();
            _progressStartTime = DateTime.Now;
            
            LogHelper.Info($"[进度条] 显示进度叠加层");
        }

        /// <summary>
        /// 更新底部进度条
        /// </summary>
        private void UpdateBottomProgress(int percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(UpdateBottomProgress), percent);
                return;
            }
            
            _progressOverlay.Progress = percent;
            
            LogHelper.Debug($"[进度条] 更新进度: {percent}%");
        }

        /// <summary>
        /// 隐藏底部进度条
        /// </summary>
        private async void HideBottomProgress()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(HideBottomProgress));
                return;
            }
            
            // 计算已显示时长
            var displayTime = (DateTime.Now - _progressStartTime).TotalMilliseconds;
            
            // 如果显示时间不足最小时间，等待
            if (displayTime < MIN_PROGRESS_DISPLAY_TIME_MS)
            {
                var remainingTime = (int)(MIN_PROGRESS_DISPLAY_TIME_MS - displayTime);
                await Task.Delay(remainingTime);
            }
            
            _progressOverlay.Stop();
            
            var totalTime = (DateTime.Now - _progressStartTime).TotalMilliseconds;
            LogHelper.Debug($"[进度条] 已隐藏，显示时长: {totalTime:F0}ms");
        }

        /// <summary>
        /// 处理键盘快捷键
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl+Z: 撤回
            if (keyData == (Keys.Control | Keys.Z))
            {
                if (_btnUndo.Enabled)
                {
                    BtnUndo_Click(null, null);
                }
                return true;
            }
            // Ctrl+Y 或 Ctrl+Shift+Z: 重做
            else if (keyData == (Keys.Control | Keys.Y) || 
                     keyData == (Keys.Control | Keys.Shift | Keys.Z))
            {
                if (_btnRedo.Enabled)
                {
                    BtnRedo_Click(null, null);
                }
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// 更新状态栏
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateStatus(message)));
                return;
            }

            if (_lblStatus != null)
            {
                _lblStatus.Text = message;
            }
        }

        /// <summary>
        /// 应用主题（供外部调用）
        /// </summary>
        /// <param name="isDark">是否为深色模式</param>
        public void ApplyTheme(bool isDark)
        {
            // 应用到 PDF 预览控件
            _pdfPreview?.ApplyTheme(isDark);
        }

        #endregion
    }
}
