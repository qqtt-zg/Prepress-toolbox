using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApp3.Utils
{
    /// <summary>
    /// Helper class to apply themes recursively to Windows Forms controls
    /// </summary>
    public static class ThemeHelper
    {
        // Dark Mode Colors - 调整为更柔和的深色调（参考 VS Code / Slack）
        private static readonly Color DarkBackground = Color.FromArgb(40, 42, 46);      // 主背景 - 深灰蓝
        private static readonly Color DarkSurface = Color.FromArgb(48, 50, 54);         // 卡片/面板背景
        private static readonly Color DarkSurfaceLight = Color.FromArgb(55, 57, 61);    // 输入框背景
        private static readonly Color DarkText = Color.FromArgb(230, 230, 235);         // 主文字
        private static readonly Color DarkTextSecondary = Color.FromArgb(160, 165, 170); // 次要文字
        private static readonly Color DarkBorder = Color.FromArgb(65, 67, 71);          // 边框

        // Light Mode Colors - 优化为更清爽的浅色调
        private static readonly Color LightBackground = Color.FromArgb(248, 249, 250);  // 主背景 - 极浅灰
        private static readonly Color LightSurface = Color.White;                        // 卡片/面板 - 纯白
        private static readonly Color LightText = Color.FromArgb(33, 37, 41);           // 主文字 - 深灰
        private static readonly Color LightTextSecondary = Color.FromArgb(108, 117, 125); // 次要文字 - 中灰
        private static readonly Color LightBorder = Color.FromArgb(222, 226, 230);      // 边框 - 浅灰

        public static void ApplyTheme(Control root, bool isDark)
        {
            if (root == null) return;

            ApplyToControl(root, isDark);

            foreach (Control child in root.Controls)
            {
                ApplyTheme(child, isDark);
            }
        }

        private static void ApplyToControl(Control control, bool isDark)
        {
            // Handle AntdUI controls explicitly (they don't auto-theme)
            if (control.GetType().Namespace != null && control.GetType().Namespace.StartsWith("AntdUI"))
            {
                ApplyToAntdUIControl(control, isDark);
                return;
            }

            // Handle UserControl (Settings controls, etc.)
            if (control is UserControl userControl)
            {
                userControl.BackColor = isDark ? DarkBackground : LightSurface;
                userControl.ForeColor = isDark ? DarkText : LightText;
            }
            else if (control is Form form)
            {
                form.BackColor = isDark ? DarkBackground : LightBackground;
                form.ForeColor = isDark ? DarkText : LightText;
            }
            else if (control is SplitContainer splitContainer)
            {
                splitContainer.Panel1.BackColor = isDark ? DarkBackground : LightSurface;
                splitContainer.Panel2.BackColor = isDark ? DarkBackground : LightSurface;
                splitContainer.BackColor = isDark ? DarkBorder : LightBorder;
            }
            else if (control is Panel panel)
            {
                panel.BackColor = isDark ? DarkBackground : LightSurface;
                panel.ForeColor = isDark ? DarkText : LightText;
            }
            else if (control is FlowLayoutPanel flowPanel)
            {
                flowPanel.BackColor = isDark ? DarkBackground : LightSurface;
                flowPanel.ForeColor = isDark ? DarkText : LightText;
            }
            else if (control is GroupBox grp)
            {
                grp.BackColor = isDark ? DarkBackground : LightBackground;
                grp.ForeColor = isDark ? DarkText : LightText;
            }
            else if (control is TabControl tabControl)
            {
                tabControl.BackColor = isDark ? DarkSurface : LightSurface;
                tabControl.ForeColor = isDark ? DarkText : LightText;
                foreach (TabPage tabPage in tabControl.TabPages)
                {
                    tabPage.BackColor = isDark ? DarkBackground : LightBackground;
                    tabPage.ForeColor = isDark ? DarkText : LightText;
                }
            }
            else if (control is Label lbl)
            {
                lbl.ForeColor = isDark ? DarkText : LightText;
            }
            else if (control is Button btn)
            {
                btn.BackColor = isDark ? DarkSurface : Color.White;
                btn.ForeColor = isDark ? DarkText : LightText;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = isDark ? DarkBorder : LightBorder;
            }
            else if (control is TextBox txt)
            {
                txt.BackColor = isDark ? DarkSurfaceLight : Color.White;
                txt.ForeColor = isDark ? DarkText : LightText;
                txt.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is RichTextBox rtb)
            {
                rtb.BackColor = isDark ? DarkSurfaceLight : Color.White;
                rtb.ForeColor = isDark ? DarkText : LightText;
                rtb.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is ComboBox combo)
            {
                combo.BackColor = isDark ? DarkSurfaceLight : Color.White;
                combo.ForeColor = isDark ? DarkText : LightText;
                combo.FlatStyle = FlatStyle.Flat;
            }
            else if (control is ListBox listBox)
            {
                listBox.BackColor = isDark ? DarkSurfaceLight : Color.White;
                listBox.ForeColor = isDark ? DarkText : LightText;
                listBox.BorderStyle = BorderStyle.FixedSingle;
            }
            else if (control is CheckBox chk)
            {
                chk.ForeColor = isDark ? DarkText : LightText;
            }
            else if (control is RadioButton rb)
            {
                rb.ForeColor = isDark ? DarkText : LightText;
            }
            else if (control is StatusStrip statusStrip)
            {
                statusStrip.BackColor = isDark ? DarkSurface : LightSurface;
                statusStrip.ForeColor = isDark ? DarkText : LightText;
                foreach (ToolStripItem item in statusStrip.Items)
                {
                    item.BackColor = isDark ? DarkSurface : LightSurface;
                    item.ForeColor = isDark ? DarkText : LightText;
                }
            }
            else if (control is ToolStrip toolStrip)
            {
                toolStrip.BackColor = isDark ? DarkSurface : LightSurface;
                toolStrip.ForeColor = isDark ? DarkText : LightText;
                foreach (ToolStripItem item in toolStrip.Items)
                {
                    item.BackColor = isDark ? DarkSurface : LightSurface;
                    item.ForeColor = isDark ? DarkText : LightText;
                }
            }
            else if (control is MenuStrip menuStrip)
            {
                menuStrip.BackColor = isDark ? DarkSurface : LightSurface;
                menuStrip.ForeColor = isDark ? DarkText : LightText;
                foreach (ToolStripItem item in menuStrip.Items)
                {
                    ApplyToMenuStripItem(item, isDark);
                }
            }
            else if (control.GetType().FullName.Contains("Krypton.Toolkit.KryptonDataGridView"))
            {
                // KryptonDataGridView 需要通过 StateCommon 来设置主题
                try
                {
                    dynamic kryptonDgv = control;
                    
                    if (isDark)
                    {
                        // 暗色模式 - 使用较浅的颜色，让表格作为内容焦点区域
                        kryptonDgv.StateCommon.Background.Color1 = Color.FromArgb(65, 67, 71);  // 较浅的灰色
                        kryptonDgv.StateCommon.BackStyle = Krypton.Toolkit.PaletteBackStyle.GridBackgroundList;
                        
                        // 数据单元格 - 使用更亮的背景
                        kryptonDgv.StateCommon.DataCell.Back.Color1 = Color.FromArgb(70, 72, 76);   // 亮灰色
                        kryptonDgv.StateCommon.DataCell.Content.Color1 = Color.FromArgb(240, 240, 245);  // 更亮的文字
                        kryptonDgv.StateCommon.DataCell.Border.Color1 = Color.FromArgb(80, 82, 86);
                        
                        // 列标题 - 稍深但仍然较亮
                        kryptonDgv.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(60, 62, 66);
                        kryptonDgv.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(55, 57, 61);
                        kryptonDgv.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(220, 220, 225);
                        kryptonDgv.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(80, 82, 86);
                        
                        // 行标题
                        kryptonDgv.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(60, 62, 66);
                        kryptonDgv.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(55, 57, 61);
                        kryptonDgv.StateCommon.HeaderRow.Content.Color1 = Color.FromArgb(220, 220, 225);
                        kryptonDgv.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(80, 82, 86);
                        
                        // 滚动条 - 尝试设置暗色主题
                        try
                        {
                            // 尝试设置滚动条相关属性
                            var scrollProps = control.GetType().GetProperties()
                                .Where(p => p.Name.Contains("Scroll") && p.CanWrite)
                                .ToList();
                            
                            #if DEBUG
                            if (scrollProps.Any())
                            {
                                var propNames = string.Join(", ", scrollProps.Select(p => p.Name));
                                System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Found scroll properties: {propNames}");
                            }
                            #endif
                            
                            // Krypton 控件的滚动条通常通过 Windows 原生控件，较难自定义
                            // 但我们可以尝试设置控件整体背景来影响滚动条区域
                        }
                        catch
                        {
                            // 忽略错误
                        }
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[ThemeHelper] KryptonDataGridView {control.Name} themed for dark mode");
                        #endif
                    }
                    else
                    {
                        // 浅色模式
                        kryptonDgv.StateCommon.Background.Color1 = Color.White;
                        kryptonDgv.StateCommon.DataCell.Back.Color1 = Color.White;
                        kryptonDgv.StateCommon.DataCell.Content.Color1 = LightText;
                        kryptonDgv.StateCommon.DataCell.Border.Color1 = Color.FromArgb(240, 240, 240);
                        
                        kryptonDgv.StateCommon.HeaderColumn.Back.Color1 = Color.FromArgb(250, 250, 250);
                        kryptonDgv.StateCommon.HeaderColumn.Back.Color2 = Color.FromArgb(245, 245, 245);
                        kryptonDgv.StateCommon.HeaderColumn.Content.Color1 = Color.FromArgb(80, 80, 80);
                        kryptonDgv.StateCommon.HeaderColumn.Border.Color1 = Color.FromArgb(230, 230, 230);
                        
                        kryptonDgv.StateCommon.HeaderRow.Back.Color1 = Color.FromArgb(250, 250, 250);
                        kryptonDgv.StateCommon.HeaderRow.Back.Color2 = Color.FromArgb(245, 245, 245);
                        kryptonDgv.StateCommon.HeaderRow.Content.Color1 = Color.FromArgb(80, 80, 80);
                        kryptonDgv.StateCommon.HeaderRow.Border.Color1 = Color.FromArgb(230, 230, 230);
                    }
                }
                catch (Exception ex)
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Failed to theme KryptonDataGridView {control.Name}: {ex.Message}");
                    #endif
                }
            }
            else if (control is DataGridView dgv && !(control.GetType().FullName.Contains("Krypton")))
            {
                dgv.BackgroundColor = isDark ? DarkBackground : SystemColors.AppWorkspace;
                dgv.DefaultCellStyle.BackColor = isDark ? DarkSurface : Color.White;
                dgv.DefaultCellStyle.ForeColor = isDark ? DarkText : LightText;
                dgv.DefaultCellStyle.SelectionBackColor = isDark ? Color.FromArgb(0, 120, 215) : SystemColors.Highlight;
                dgv.DefaultCellStyle.SelectionForeColor = Color.White;
                dgv.ColumnHeadersDefaultCellStyle.BackColor = isDark ? DarkSurfaceLight : Color.White;
                dgv.ColumnHeadersDefaultCellStyle.ForeColor = isDark ? DarkText : LightText;
                dgv.RowHeadersDefaultCellStyle.BackColor = isDark ? DarkSurfaceLight : Color.White;
                dgv.RowHeadersDefaultCellStyle.ForeColor = isDark ? DarkText : LightText;
                dgv.EnableHeadersVisualStyles = false;
                dgv.GridColor = isDark ? DarkBorder : LightBorder;
            }
        }

        private static void ApplyToMenuStripItem(ToolStripItem item, bool isDark)
        {
            item.BackColor = isDark ? DarkSurface : LightSurface;
            item.ForeColor = isDark ? DarkText : LightText;
            
            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem subItem in menuItem.DropDownItems)
                {
                    ApplyToMenuStripItem(subItem, isDark);
                }
            }
        }

        /// <summary>
        /// Apply theme to AntdUI controls using property-based approach
        /// </summary>
        private static void ApplyToAntdUIControl(Control control, bool isDark)
        {
            var controlType = control.GetType();
            var typeName = controlType.Name;

            try
            {
                // AntdUI.Input
                if (typeName == "Input")
                {
                    SetPropertySafe(control, "BackColor", isDark ? DarkSurfaceLight : Color.White);
                    SetPropertySafe(control, "ForeColor", isDark ? DarkText : LightText);
                    SetPropertySafe(control, "BorderColor", isDark ? DarkBorder : LightBorder);
                }
                // AntdUI.Select (dropdown selector) - use direct casting since reflection fails
                else if (typeName == "Select")
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Applying theme to AntdUI.Select: {control.Name}, IsDark={isDark}");
                    #endif
                    
                    // AntdUI.Select doesn't respond well to reflection due to property ambiguity
                    // Try dynamic approach
                    try
                    {
                        dynamic selectControl = control;
                        selectControl.BackColor = isDark ? DarkSurfaceLight : Color.White;
                        selectControl.ForeColor = isDark ? DarkText : LightText;
                        selectControl.BorderColor = isDark ? DarkBorder : LightBorder;
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Select {control.Name} themed using dynamic - SUCCESS");
                        #endif
                    }
                    catch (Exception ex)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Failed to theme Select {control.Name}: {ex.Message}");
                        #endif
                    }
                }
                // AntdUI.Button - adjust colors for dark mode
                else if (typeName == "Button")
                {
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Processing Button: {control.Name}");
                    #endif
                    
                    var typeProperty = controlType.GetProperty("Type");
                    var typeValue = typeProperty?.GetValue(control);
                    var buttonType = typeValue?.ToString() ?? "Default";
                    
                    #if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Button {control.Name}: Type={buttonType}, IsDark={isDark}");
                    #endif
                    
                    try
                    {
                        dynamic btnControl = control;
                        
                        if (isDark)
                        {
                            // 暗色模式：调整各类型按钮的颜色，使其更柔和
                            switch (buttonType)
                            {
                                case "Primary":
                                    btnControl.BackColor = Color.FromArgb(48, 100, 160);  // 柔和深蓝
                                    btnControl.ForeColor = Color.FromArgb(230, 240, 250);
                                    break;
                                case "Success":
                                    btnControl.BackColor = Color.FromArgb(56, 120, 80);   // 柔和深绿
                                    btnControl.ForeColor = Color.FromArgb(230, 245, 235);
                                    break;
                                case "Warn":
                                    btnControl.BackColor = Color.FromArgb(160, 100, 50);  // 柔和深橙
                                    btnControl.ForeColor = Color.FromArgb(250, 240, 230);
                                    break;
                                case "Error":
                                    btnControl.BackColor = Color.FromArgb(140, 60, 60);   // 柔和深红
                                    btnControl.ForeColor = Color.FromArgb(250, 235, 235);
                                    break;
                                case "Default":
                                default:
                                    // 默认按钮需要设置 DefaultBack，而不是 BackColor
                                    btnControl.DefaultBack = DarkSurface;
                                    btnControl.ForeColor = DarkText;
                                    
                                    #if DEBUG
                                    System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Set DefaultBack for {control.Name}");
                                    #endif
                                    break;
                            }
                            
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Button {control.Name} ({buttonType}) themed using dynamic");
                            #endif
                        }
                        else
                        {
                            // 浅色模式：对于 Default 类型，恢复白色背景
                            if (buttonType == "Default")
                            {
                                btnControl.DefaultBack = Color.White;  // 使用 DefaultBack，而不是 BackColor
                                btnControl.ForeColor = LightText;
                                
                                #if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Reset DefaultBack to white for {control.Name}");
                                #endif
                            }
                            // 其他类型让 AntdUI 自己管理
                        }
                    }
                    catch (Exception ex)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Failed to theme Button {control.Name} with dynamic: {ex.Message}");
                        #endif
                        
                        // Fallback to SetPropertySafe
                        if (isDark)
                        {
                            if (buttonType == "Default" || string.IsNullOrEmpty(buttonType))
                            {
                                SetPropertySafe(control, "BackColor", DarkSurface);
                                SetPropertySafe(control, "ForeColor", DarkText);
                            }
                        }
                    }
                }
                // AntdUI.Label
                else if (typeName == "Label")
                {
                    SetPropertySafe(control, "ForeColor", isDark ? DarkText : LightText);
                }
                // AntdUI.Tabs
                else if (typeName == "Tabs")
                {
                    SetPropertySafe(control, "ForeColor", isDark ? DarkText : LightText);
                    SetPropertySafe(control, "BackColor", isDark ? DarkBackground : LightBackground);
                }
                // AntdUI.TabPage
                else if (typeName == "TabPage")
                {
                    SetPropertySafe(control, "BackColor", isDark ? DarkBackground : LightBackground);
                    SetPropertySafe(control, "ForeColor", isDark ? DarkText : LightText);
                }
                // AntdUI.Checkbox
                else if (typeName == "Checkbox")
                {
                    try
                    {
                        dynamic checkboxControl = control;
                        checkboxControl.ForeColor = isDark ? DarkText : LightText;
                    }
                    catch
                    {
                        SetPropertySafe(control, "ForeColor", isDark ? DarkText : LightText);
                    }
                }
                // AntdUI.Switch
                else if (typeName == "Switch")
                {
                    SetPropertySafe(control, "ForeColor", isDark ? DarkText : LightText);
                }
                // AntdUI.Menu - 导航菜单
                else if (typeName == "Menu")
                {
                    try
                    {
                        dynamic menuControl = control;
                        
                        if (isDark)
                        {
                            // 暗色模式
                            menuControl.BackColor = DarkSurface;
                            menuControl.ForeColor = DarkText;
                        }
                        else
                        {
                            // 浅色模式 - 使用清爽的白色背景
                            menuControl.BackColor = Color.White;
                            menuControl.ForeColor = LightText;
                        }
                        
                        // 设置选中和悬停颜色
                        try
                        {
                            if (isDark)
                            {
                                // 暗色模式：使用深色系的选中效果
                                menuControl.BackActive = Color.FromArgb(45, 55, 65);    // 深灰蓝，更低调
                                menuControl.BackHover = Color.FromArgb(55, 57, 61);     // 深灰
                                
                                #if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Menu {control.Name} dark selection colors set");
                                #endif
                            }
                            else
                            {
                                // 浅色模式：使用浅色系的选中效果
                                menuControl.BackActive = Color.FromArgb(230, 240, 255); // 极浅蓝
                                menuControl.BackHover = Color.FromArgb(245, 247, 250);  // 浅灰
                                
                                #if DEBUG
                                System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Menu {control.Name} light selection colors set: BackActive={menuControl.BackActive}, BackHover={menuControl.BackHover}");
                                #endif
                            }
                        }
                        catch (Exception selEx)
                        {
                            #if DEBUG
                            System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Failed to set Menu selection colors: {selEx.Message}");
                            #endif
                        }
                        
                        // 尝试设置 MenuItem 的颜色
                        try
                        {
                            var itemsProperty = control.GetType().GetProperty("Items");
                            if (itemsProperty != null)
                            {
                                var items = itemsProperty.GetValue(control);
                                if (items != null)
                                {
                                    // 遍历所有 MenuItem
                                    foreach (var item in (System.Collections.IEnumerable)items)
                                    {
                                        try
                                        {
                                            dynamic menuItem = item;
                                            if (isDark)
                                            {
                                                menuItem.ForeColor = DarkText;
                                            }
                                            else
                                            {
                                                menuItem.ForeColor = LightText;
                                            }
                                        }
                                        catch
                                        {
                                            // 忽略单个 MenuItem 的错误
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // 如果无法设置 MenuItem，继续
                        }
                        
                        // 强制刷新菜单显示
                        control.Invalidate();
                        control.Refresh();
                        
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Menu {control.Name} themed, BackColor={menuControl.BackColor}, ForeColor={menuControl.ForeColor}, IsDark={isDark}");
                        #endif
                    }
                    catch (Exception ex)
                    {
                        #if DEBUG
                        System.Diagnostics.Debug.WriteLine($"[ThemeHelper] Failed to theme Menu {control.Name}: {ex.Message}");
                        #endif
                        
                        // 回退到 SetPropertySafe
                        SetPropertySafe(control, "BackColor", isDark ? DarkSurface : Color.White);
                        SetPropertySafe(control, "ForeColor", isDark ? DarkText : LightText);
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
                catch
                {
                    // Silently ignore if still fails
                }
            }
            catch
            {
                // Silently ignore other exceptions
            }
        }

        /// <summary>
        /// Try to set property using multiple name variations
        /// </summary>
        private static void SetPropertyVariations(Control control, string[] propertyNames, object value)
        {
            foreach (var propertyName in propertyNames)
            {
                if (SetPropertyIfExists(control, propertyName, value))
                {
                    return; // Successfully set, no need to try other variations
                }
            }
        }

        /// <summary>
        /// Set property value if it exists on the control
        /// </summary>
        private static bool SetPropertyIfExists(Control control, string propertyName, object value)
        {
            try
            {
                var property = control.GetType().GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(control, value);
                    return true;
                }
            }
            catch
            {
                // Silently ignore
            }
            return false;
        }
    }
}
