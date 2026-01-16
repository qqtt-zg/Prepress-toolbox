using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Controls;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// Helper class to apply themes recursively to Windows Forms controls
    /// </summary>
    public static class ThemeHelper
    {
        #region Windows API
        
        /// <summary>
        /// 设置窗口主题（用于滚动条主题化）
        /// </summary>
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);
        
        #endregion
        
        // 当前应用的主题定义
        private static ThemeDefinition _currentTheme;
        private static bool _isDark = false; // Track dark mode state

        /// <summary>
        /// 设置并应用主题
        /// </summary>
        public static void ApplyTheme(Control root, ThemeDefinition theme)
        {
            if (theme == null) return;
            _currentTheme = theme;
            ApplyTheme(root);
        }

        /// <summary>
        /// 应用当前主题（使用已设置的主题）
        /// </summary>
        private static void ApplyTheme(Control root)
        {
            if (root == null || _currentTheme == null) return;

            ApplyToControl(root);

            foreach (Control child in root.Controls)
            {
                ApplyTheme(child);
            }
        }

        /// <summary>
        /// 向后兼容：根据 isDark 参数应用主题
        /// 注意：此方法需要 ThemeManager 已经设置了对应的主题
        /// </summary>
        public static void ApplyTheme(Control root, bool isDark)
        {
            _isDark = isDark;
            if (_currentTheme == null)
            {
                // 如果没有设置主题，无法应用
                return;
            }
            
            if (root == null) return;
            ApplyToControl(root);
            
            foreach (Control child in root.Controls)
            {
                ApplyTheme(child);
            }
            
            // 🔧 新增：应用滚动条主题
            ApplyScrollBarThemeRecursive(root, isDark);
        }

        private static void ApplyToControl(Control control)
        {
            if (_currentTheme == null) return;

            // Handle AntdUI controls explicitly (they don't auto-theme)
            if (control.GetType().Namespace != null && control.GetType().Namespace.StartsWith("AntdUI"))
            {
                ApplyToAntdUIControl(control);
                return;
            }

            // Handle EventGroupsTreeView explicitly
            if (control is EventGroupsTreeView eventGroupsTree)
            {
                eventGroupsTree.ApplyTheme(_isDark);
                // Allow recursion to theme ScrollBars if any
            }

            // Handle UserControl (Settings controls, etc.)
            if (control is UserControl userControl)
            {
                userControl.BackColor = _currentTheme.Surface;
                userControl.ForeColor = _currentTheme.TextPrimary;
            }
            else if (control is Form form)
            {
                form.BackColor = _currentTheme.Background;
                form.ForeColor = _currentTheme.TextPrimary;
            }
            else if (control is SplitContainer splitContainer)
            {
                splitContainer.Panel1.BackColor = _currentTheme.Background;
                splitContainer.Panel2.BackColor = _currentTheme.Background;
                splitContainer.BackColor = _currentTheme.Border;
            }
            else if (control is Panel panel)
            {
                panel.BackColor = _currentTheme.Surface;
                panel.ForeColor = _currentTheme.TextPrimary;
            }
            else if (control is FlowLayoutPanel flowPanel)
            {
                flowPanel.BackColor = _currentTheme.Background;
                flowPanel.ForeColor = _currentTheme.TextPrimary;
            }
            else if (control is GroupBox grp)
            {
                grp.BackColor = _currentTheme.Background;
                grp.ForeColor = _currentTheme.TextPrimary;
            }
            else if (control is TabControl tabControl)
            {
                tabControl.BackColor = _currentTheme.Surface;
                tabControl.ForeColor = _currentTheme.TextPrimary;
                foreach (TabPage tabPage in tabControl.TabPages)
                {
                    tabPage.BackColor = _currentTheme.Background;
                    tabPage.ForeColor = _currentTheme.TextPrimary;
                }
            }
            else if (control is Label lbl)
            {
                lbl.ForeColor = _currentTheme.TextPrimary;
            }
            else if (control is Button btn)
            {
                btn.BackColor = _currentTheme.Surface;
                btn.ForeColor = _currentTheme.TextPrimary;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = _currentTheme.Border;
            }
            else if (control is TextBox txt)
            {
                Color inputBack = _currentTheme.InputBackground;
                if (inputBack.IsEmpty) inputBack = _currentTheme.SurfaceLight;
                txt.BackColor = inputBack;
                txt.ForeColor = _currentTheme.TextPrimary;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is RichTextBox rtb)
            {
                Color inputBack = _currentTheme.InputBackground;
                if (inputBack.IsEmpty) inputBack = _currentTheme.SurfaceLight;
                rtb.BackColor = inputBack;
                rtb.ForeColor = _currentTheme.TextPrimary;
                rtb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is ComboBox combo)
            {
                Color inputBack = _currentTheme.InputBackground;
                if (inputBack.IsEmpty) inputBack = _currentTheme.SurfaceLight;
                combo.BackColor = inputBack;
                combo.ForeColor = _currentTheme.TextPrimary;
                combo.FlatStyle = FlatStyle.Flat;
            }
            else if (control is ListBox listBox)
            {
                Color inputBack = _currentTheme.InputBackground;
                if (inputBack.IsEmpty) inputBack = _currentTheme.SurfaceLight;
                listBox.BackColor = inputBack;
                listBox.ForeColor = _currentTheme.TextPrimary;
                listBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is CheckBox chk)
            {
                chk.ForeColor = _currentTheme.TextPrimary;
            }
            else if (control is RadioButton rb)
            {
                rb.ForeColor = _currentTheme.TextPrimary;
            }
            else if (control is StatusStrip statusStrip)
            {
                statusStrip.BackColor = _currentTheme.Surface;
                statusStrip.ForeColor = _currentTheme.TextPrimary;
                foreach (ToolStripItem item in statusStrip.Items)
                {
                    item.BackColor = _currentTheme.Surface;
                    item.ForeColor = _currentTheme.TextPrimary;
                }
            }
            else if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = _currentTheme.Surface;
                toolStrip.ForeColor = _currentTheme.TextPrimary;
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    item.BackColor = _currentTheme.Surface;
                    item.ForeColor = _currentTheme.TextPrimary;
                }
            }
            else if (control is MenuStrip menuStrip)
            {
                menuStrip.BackColor = _currentTheme.Surface;
                menuStrip.ForeColor = _currentTheme.TextPrimary;
                foreach (ToolStripItem item in menuStrip.Items)
                {
                    ApplyToMenuStripItem(item);
                }
            }
            else if (control.GetType().FullName.Contains("Krypton.Toolkit.KryptonDataGridView"))
            {
                // KryptonDataGridView 需要通过 StateCommon 来设置主题
                ApplyToKryptonDataGridView(control);
            }
            else if (control is DataGridView dgv && !(control.GetType().FullName.Contains("Krypton")))
            {
                dgv.BackgroundColor = _currentTheme.Background;
                dgv.DefaultCellStyle.BackColor = _currentTheme.Surface;
                dgv.DefaultCellStyle.ForeColor = _currentTheme.TextPrimary;
                dgv.DefaultCellStyle.SelectionBackColor = _currentTheme.Primary;
                dgv.DefaultCellStyle.SelectionForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = _currentTheme.SurfaceLight;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = _currentTheme.TextPrimary;
                dgv.RowHeadersDefaultCellStyle.BackColor = _currentTheme.SurfaceLight;
                dgv.RowHeadersDefaultCellStyle.ForeColor = _currentTheme.TextPrimary;
                dgv.EnableHeadersVisualStyles = false;
                dgv.GridColor = _currentTheme.Border;
            }
        }

        private static void ApplyToMenuStripItem(ToolStripItem item)
        {
            if (_currentTheme == null) return;

            item.BackColor = _currentTheme.Surface;
            item.ForeColor = _currentTheme.TextPrimary;
            
            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem subItem in menuItem.DropDownItems)
                {
                    ApplyToMenuStripItem(subItem);
                }
            }
        }

        private static void ApplyToKryptonDataGridView(Control control)
        {
            if (_currentTheme == null) return;

            try
            {
                dynamic kryptonDgv = control;
                
                // 设置背景和数据单元格  
                kryptonDgv.StateCommon.Background.Color1 = _currentTheme.Surface;
                kryptonDgv.StateCommon.BackStyle = Krypton.Toolkit.PaletteBackStyle.GridBackgroundList;
                
                // 数据单元格
                kryptonDgv.StateCommon.DataCell.Back.Color1 = _currentTheme.Surface;
                kryptonDgv.StateCommon.DataCell.Content.Color1 = _currentTheme.TextPrimary;
                kryptonDgv.StateCommon.DataCell.Border.Color1 = _currentTheme.Border;
                
                // 列标题
                kryptonDgv.StateCommon.HeaderColumn.Back.Color1 = _currentTheme.SurfaceLight;
                kryptonDgv.StateCommon.HeaderColumn.Back.Color2 = _currentTheme.Surface;
                kryptonDgv.StateCommon.HeaderColumn.Content.Color1 = _currentTheme.TextSecondary;
                kryptonDgv.StateCommon.HeaderColumn.Border.Color1 = _currentTheme.Border;
                
                // 行标题
                kryptonDgv.StateCommon.HeaderRow.Back.Color1 = _currentTheme.SurfaceLight;
                kryptonDgv.StateCommon.HeaderRow.Back.Color2 = _currentTheme.Surface;
                kryptonDgv.StateCommon.HeaderRow.Content.Color1 = _currentTheme.TextSecondary;
                kryptonDgv.StateCommon.HeaderRow.Content.Color1 = _currentTheme.TextSecondary;
                kryptonDgv.StateCommon.HeaderRow.Border.Color1 = _currentTheme.Border;

                // 选中状态 (Selection Highlight)
                // 使用主题定义的 BackActive (激活背景色) 和 TextPrimary (主文本色)
                kryptonDgv.StateSelected.DataCell.Back.Color1 = _currentTheme.BackActive;
                kryptonDgv.StateSelected.DataCell.Back.Color2 = _currentTheme.BackActive;
                kryptonDgv.StateSelected.DataCell.Content.Color1 = _currentTheme.TextPrimary;
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Failed to theme KryptonDataGridView {control.Name}: {ex.Message}");
                #endif
            }
        }

        /// <summary>
        /// Apply theme to AntdUI controls using property-based approach
        /// </summary>
        private static void ApplyToAntdUIControl(Control control)
        {
            if (_currentTheme == null) return;

            var controlType = control.GetType();
            var typeName = controlType.Name;

            try
            {
                // AntdUI.Input
                if (typeName == "Input" || typeName == "InputNumber")
                {
                    Color inputBack = _currentTheme.InputBackground;
                    if (inputBack.IsEmpty) inputBack = _currentTheme.SurfaceLight;

                    SetPropertySafe(control, "BackColor", inputBack);
                    SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                    SetPropertySafe(control, "BorderColor", _currentTheme.Border);
                    SetPropertySafe(control, "PlaceholderColor", _currentTheme.TextSecondary);
                }
                // AntdUI.Select
                else if (typeName == "Select")
                {
                    try
                    {
                        if (control is  AntdUI.Select selectControl)
                        {
                            Color inputBack = _currentTheme.InputBackground;
                            if (inputBack.IsEmpty) inputBack = _currentTheme.SurfaceLight;
                            selectControl.BackColor = inputBack;
                            selectControl.ForeColor = _currentTheme.TextPrimary;
                            selectControl.BorderColor = _currentTheme.Border;
                            
                            // Use ColorScheme to control dropdown theme (Light/Dark)
                            selectControl.ColorScheme = _isDark ? AntdUI.TAMode.Dark : AntdUI.TAMode.Light;
                        }
                    }
                    catch
                    {
                        // Fallback
                        SetPropertySafe(control, "BackColor", _currentTheme.SurfaceLight);
                        SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                    }
                }
                // AntdUI.Button
                else if (typeName == "Button")
                {
                    var typeProperty = controlType.GetProperty("Type");
                    var typeValue = typeProperty?.GetValue(control);
                    var buttonType = typeValue?.ToString() ?? "Default";
                    
                    try
                    {
                        dynamic btnControl = control;
                        
                        switch (buttonType)
                        {
                            case "Primary":
                                btnControl.BackColor = _currentTheme.Primary;
                                btnControl.ForeColor = Color.White;
                                break;
                            case "Success":
                                btnControl.BackColor = _currentTheme.Success;
                                btnControl.ForeColor = Color.White;
                                break;
                            case "Warn":
                                btnControl.BackColor = _currentTheme.Warning;
                                btnControl.ForeColor = Color.White;
                                break;
                            case "Error":
                                btnControl.BackColor = _currentTheme.Error;
                                btnControl.ForeColor = Color.White;
                                break;
                            case "Default":
                            default:
                                btnControl.DefaultBack = _currentTheme.Surface;
                                btnControl.ForeColor = _currentTheme.TextPrimary;
                                break;
                        }
                    }
                    catch
                    {
                        // Fallback
                        if (buttonType == "Default")
                        {
                            SetPropertySafe(control, "BackColor", _currentTheme.Surface);
                            SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                        }
                    }
                }
                // AntdUI.Label
                else if (typeName == "Label")
                {
                    SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                }
                // AntdUI.Tabs
                else if (typeName == "Tabs")
                {
                    SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                    SetPropertySafe(control, "BackColor", _currentTheme.Background);
                }
                // AntdUI.TabPage
                else if (typeName == "TabPage")
                {
                    SetPropertySafe(control, "BackColor", _currentTheme.Background);
                    SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                }
                // AntdUI.Checkbox
                else if (typeName == "Checkbox")
                {
                    try
                    {
                        dynamic checkboxControl = control;
                        checkboxControl.ForeColor = _currentTheme.TextPrimary;
                    }
                    catch
                    {
                        SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                    }
                }
                // AntdUI.Switch
                else if (typeName == "Switch")
                {
                    SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                }
                // AntdUI.Menu
                else if (typeName == "Menu")
                {
                    try
                    {
                        dynamic menuControl = control;
                        menuControl.BackColor = _currentTheme.Surface;
                        menuControl.ForeColor = _currentTheme.TextPrimary;
                        menuControl.BackActive = _currentTheme.BackActive;
                        menuControl.BackHover = _currentTheme.BackHover;
                        
                        // 设置 MenuItem 颜色
                        try
                        {
                            var itemsProperty = control.GetType().GetProperty("Items");
                            if (itemsProperty != null)
                            {
                                var items = itemsProperty.GetValue(control);
                                if (items != null)
                                {
                                    foreach (var item in (System.Collections.IEnumerable)items)
                                    {
                                        try
                                        {
                                            dynamic menuItem = item;
                                            menuItem.ForeColor = _currentTheme.TextPrimary;
                                        }
                                        catch { }
                                    }
                                }
                            }
                        }
                        catch { }
                        
                        control.Invalidate();
                        control.Refresh();
                    }
                    catch
                    {
                        SetPropertySafe(control, "BackColor", _currentTheme.Surface);
                        SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                    }
                }
                // AntdUI.ColorPicker - 特殊处理
                else if (typeName == "ColorPicker")
                {
                    try
                    {
                        dynamic colorPickerControl = control;
                        // ColorPicker 需要设置边框和前景色以在深色模式下可见
                        colorPickerControl.BackColor = _currentTheme.SurfaceLight;
                        colorPickerControl.ForeColor = _currentTheme.TextPrimary;
                        colorPickerControl.BorderColor = _currentTheme.Border;
                    }
                    catch
                    {
                        // Fallback
                        SetPropertySafe(control, "BackColor", _currentTheme.SurfaceLight);
                        SetPropertySafe(control, "ForeColor", _currentTheme.TextPrimary);
                        SetPropertySafe(control, "BorderColor", _currentTheme.Border);
                    }
                }
            }
            catch
            {
                // Silently ignore property setting failures
            }
        }

        /// <summary>
        /// Set property value safely, handling AmbiguousMatchException
        /// </summary>
        private static void SetPropertySafe(Control control, string propertyName, object value)
        {
            try
            {
                var property = control.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(control, value);
                }
            }
            catch (System.Reflection.AmbiguousMatchException)
            {
                // Handle ambiguous property by using BindingFlags to get most derived
                try
                {
                    var property = control.GetType().GetProperty(
                        propertyName,
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.DeclaredOnly
                    );
                    
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(control, value);
                    }
                    else
                    {
                        // Try base class
                        var baseProperty = control.GetType().BaseType?.GetProperty(
                            propertyName,
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                        );
                        if (baseProperty != null && baseProperty.CanWrite)
                        {
                            baseProperty.SetValue(control, value);
                        }
                    }
                }
                catch { }
            }
            catch { }
        }
        
        /// <summary>
        /// 为控件应用滚动条主题
        /// </summary>
        /// <param name="control">要应用主题的控件</param>
        /// <param name="isDark">是否为深色模式</param>
        private static void ApplyScrollBarTheme(Control control, bool isDark)
        {
            if (control == null)
                return;
            
            // 如果句柄还没创建，等待 HandleCreated 事件后再应用
            if (!control.IsHandleCreated)
            {
                EventHandler handler = null;
                handler = (s, e) =>
                {
                    control.HandleCreated -= handler; // 只执行一次
                    ApplyScrollBarThemeImmediate(control, isDark);
                };
                control.HandleCreated += handler;
                return;
            }
            
            // 句柄已创建，立即应用
            ApplyScrollBarThemeImmediate(control, isDark);
        }
        
        /// <summary>
        /// 立即应用滚动条主题（句柄已创建）
        /// </summary>
        private static void ApplyScrollBarThemeImmediate(Control control, bool isDark)
        {
            if (control == null || !control.IsHandleCreated)
                return;
                
            try
            {
                // 设置滚动条主题
                // "DarkMode_Explorer" 用于深色模式（Windows 10 1809+）
                // null 恢复默认主题
                string theme = isDark ? "DarkMode_Explorer" : null;
                SetWindowTheme(control.Handle, theme, null);
            }
            catch (Exception ex)
            {
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"[ThemeHelper] 应用滚动条主题失败 {control.Name}: {ex.Message}");
                #endif
            }
        }
        
        /// <summary>
        /// 为带滚动条的控件递归应用主题
        /// </summary>
        /// <param name="control">根控件</param>
        /// <param name="isDark">是否为深色模式（如果主题未设置 UseScrollBarDarkMode 则使用此值）</param>
        public static void ApplyScrollBarThemeRecursive(Control control, bool isDark)
        {
            if (control == null) return;
            
            // 🔧 优先使用主题中的 UseScrollBarDarkMode 设置，如果未设置则使用传入的 isDark
            bool scrollBarDarkMode = _currentTheme?.UseScrollBarDarkMode ?? isDark;
            
            // 检查控件是否有滚动条
            bool hasScrollBar = false;
            
            // 🔧 优先检查 PDF 相关控件（因为 PdfPreviewControl 继承自 Panel）
            if (control is PdfPreviewControl pdfPreview)
            {
                // 🔧 特殊处理：PdfPreviewControl 有自己的主题设置方法
                pdfPreview.SetScrollBarTheme(scrollBarDarkMode);
                LogHelper.Debug($"[ThemeHelper] 已设置 PdfPreviewControl 滚动条主题: isDark={scrollBarDarkMode} (来自主题设置: {_currentTheme?.UseScrollBarDarkMode})");
                // 不要 return，继续递归处理子控件
            }
            else if (control.GetType().Name == "PdfiumPdfPreviewControl" || 
                     control.GetType().FullName?.Contains("PdfiumPdfPreviewControl") == true)
            {
                // 🔧 特殊处理：PdfiumPdfPreviewControl
                try
                {
                    dynamic pdfiumControl = control;
                    pdfiumControl.SetScrollBarTheme(scrollBarDarkMode);
                    LogHelper.Debug($"[ThemeHelper] 已设置 PdfiumPdfPreviewControl 滚动条主题: isDark={scrollBarDarkMode}");
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"[ThemeHelper] 设置 PdfiumPdfPreviewControl 主题失败: {ex.Message}");
                }
                // 不要 return，继续递归处理子控件
            }
            else if (control.GetType().Name.Contains("PdfViewer") || control.GetType().Name.Contains("PdfRenderer"))
            {
                // 🔧 特殊处理：PdfiumViewer 的 PdfViewer 或 PdfRenderer 控件
                hasScrollBar = true;
                LogHelper.Debug($"[ThemeHelper] 发现 PDF 相关控件: {control.GetType().Name}");
            }
            else if (control is Panel panel && panel.AutoScroll)
                hasScrollBar = true;
            else if (control is TextBoxBase) // TextBox, RichTextBox
                hasScrollBar = true;
            else if (control is ListBox)
                hasScrollBar = true;
            else if (control is DataGridView)
                hasScrollBar = true;
            else if (control is TreeView)
                hasScrollBar = true;
            else if (control is ListView)
                hasScrollBar = true;
            
            // 如果控件有滚动条，应用主题
            if (hasScrollBar)
            {
                ApplyScrollBarTheme(control, scrollBarDarkMode);
                
                // 🔧 特殊处理：DataGridView 的内部滚动条控件
                if (control is DataGridView dgv)
                {
                    // DataGridView 的滚动条是子控件，需要recursively找到并应用主题
                    ApplyScrollBarThemeToChildren(control, scrollBarDarkMode);
                }
            }
            
            // 递归处理子控件
            foreach (Control child in control.Controls)
            {
                ApplyScrollBarThemeRecursive(child, isDark);
            }
        }
        
        /// <summary>
        /// 为控件的所有子控件（包括深层嵌套）应用滚动条主题
        /// </summary>
        private static void ApplyScrollBarThemeToChildren(Control parent, bool isDark, int depth = 0)
        {
            if (parent == null || depth > 3) return; // 限制递归深度，避免影响太深层的控件
            
            foreach (Control child in parent.Controls)
            {
                // 滚动条控件通常是 VScrollBar 或 HScrollBar
                if (child is ScrollBar)
                {
                    ApplyScrollBarTheme(child, isDark);
                }
                else if (child.GetType().Name == "VScrollBar" || child.GetType().Name == "HScrollBar")
                {
                    ApplyScrollBarTheme(child, isDark);
                }
                
                // 只对特定容器类型继续递归（避免影响普通Panel的子控件）
                if (child.HasChildren && (child is DataGridView || child.GetType().Name.Contains("DataGridView")))
                {
                    ApplyScrollBarThemeToChildren(child, isDark, depth + 1);
                }
            }
        }
    }
}
