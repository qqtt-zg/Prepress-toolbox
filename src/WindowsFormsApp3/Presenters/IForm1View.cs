using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Presenters
{
    /// <summary>
    /// Form1视图接口，定义了视图需要实现的属性、方法和事件
    /// </summary>
    public interface IForm1View
    {
        #region 事件
        /// <summary>
        /// 即时重命名点击事件
        /// </summary>
        event EventHandler ImmediateRenameClick;

        /// <summary>
        /// 停止即时重命名点击事件
        /// </summary>
        event EventHandler StopImmediateRenameClick;

        /// <summary>
        /// 模式切换点击事件
        /// </summary>
        event EventHandler ToggleModeClick;

        /// <summary>
        /// 监控点击事件
        /// </summary>
        event EventHandler MonitorClick;

        /// <summary>
        /// 导出设置点击事件
        /// </summary>
        event EventHandler ExportSettingsClick;

        /// <summary>
        /// 单元格值变化事件
        /// </summary>
        event DataGridViewCellEventHandler CellValueChanged;

        /// <summary>
        /// 列头鼠标点击事件
        /// </summary>
        event DataGridViewCellMouseEventHandler ColumnHeaderMouseClick;

        /// <summary>
        /// 键盘按下事件
        /// </summary>
        event KeyEventHandler KeyDown;

        /// <summary>
        /// 表单加载事件
        /// </summary>
        event EventHandler FormLoad;

        /// <summary>
        /// 表单关闭事件
        /// </summary>
        event FormClosingEventHandler FormClosing;

        /// <summary>
        /// 表单大小变化事件
        /// </summary>
        event EventHandler Resize;
        #endregion

        #region 属性
        /// <summary>
        /// 文件绑定列表
        /// </summary>
        BindingList<FileRenameInfo> FileBindingList { get; set; }

        /// <summary>
        /// 是否处于即时重命名模式
        /// </summary>
        bool IsImmediateRenameActive { get; set; }

        /// <summary>
        /// 是否处于复制模式
        /// </summary>
        bool IsCopyMode { get; set; }

        /// <summary>
        /// 是否正在监控
        /// </summary>
        bool IsMonitoring { get; set; }

        /// <summary>
        /// 当前配置名称
        /// </summary>
        string CurrentConfigName { get; set; }

        /// <summary>
        /// 当前正则表达式模式
        /// </summary>
        string CurrentRegexPattern { get; set; }

        /// <summary>
        /// 系统托盘图标
        /// </summary>
        NotifyIcon TrayIcon { get; }

        /// <summary>
        /// 导入的Excel数据
        /// </summary>
        DataTable ExcelImportedData { get; set; }

        /// <summary>
        /// 窗口状态
        /// </summary>
        FormWindowState WindowState { get; set; }

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        void Hide();
        
        /// <summary>
        /// Excel新列索引
        /// </summary>
        int ExcelNewColumnIndex { get; }
        
        /// <summary>
        /// 获取Excel搜索列索引
        /// </summary>
        int ExcelSearchColumnIndex { get; }
        
        /// <summary>
        /// 获取Excel返回列索引
        /// </summary>
        int ExcelReturnColumnIndex { get; }
        #endregion

        #region 方法
        /// <summary>
        /// 初始化数据网格视图
        /// </summary>
        void InitializeDataGridView();

        /// <summary>
        /// 更新状态栏
        /// </summary>
        void UpdateStatusStrip();

        /// <summary>
        /// 更新数据网格视图编辑模式
        /// </summary>
        void UpdateDgvFilesEditMode();

        /// <summary>
        /// 更新系统托盘菜单项
        /// </summary>
        void UpdateTrayMenuItems();

        /// <summary>
        /// 显示消息框
        /// </summary>
        void ShowMessage(string message, string caption = "提示", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information);

        /// <summary>
        /// 显示消息框并返回用户选择
        /// </summary>
        DialogResult ShowMessageWithResult(string message, string caption = "提示", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information);

        /// <summary>
        /// 显示文件夹浏览器对话框
        /// </summary>
        string ShowFolderBrowserDialog(string description);

        /// <summary>
        /// 显示进度对话框
        /// </summary>
        void ShowProgressForm();

        /// <summary>
        /// 显示进度对话框并执行回调函数
        /// </summary>
        /// <param name="progressAction">在进度对话框显示期间执行的回调函数</param>
        void ShowProgressForm(Action<WindowsFormsApp3.ProgressForm> progressAction);

        /// <summary>
        /// 显示带取消功能的进度对话框并执行异步回调函数
        /// </summary>
        /// <param name="progressAction">在进度对话框显示期间执行的异步回调函数</param>
        /// <param name="cancellationTokenSource">取消令牌源</param>
        void ShowProgressFormWithCancellation(Func<WindowsFormsApp3.ProgressForm, System.Threading.CancellationToken, Task> progressAction, System.Threading.CancellationTokenSource cancellationTokenSource);

        /// <summary>
        /// 更新进度
        /// </summary>
        void UpdateProgress(int percentProgress, string statusText);

        /// <summary>
        /// 关闭进度对话框
        /// </summary>
        void CloseProgressForm();

        /// <summary>
        /// 显示文件打开对话框
        /// </summary>
        string[] ShowOpenFileDialog(string filter, string title);

        /// <summary>
        /// 显示文件保存对话框
        /// </summary>
        string ShowSaveFileDialog(string filter, string title, string defaultFileName = null);

        /// <summary>
        /// 获取导出路径
        /// </summary>
        /// <returns>导出路径</returns>
        string GetExportPath();

        /// <summary>
        /// 更新文件列表
        /// </summary>
        void UpdateFileList();
        
        /// <summary>
        /// 更新Excel数据
        /// </summary>
        void UpdateExcelData();
        
        /// <summary>
        /// 执行自动保存JSON文件数据
        /// </summary>
        void PerformAutoSave();
        
        /// <summary>
        /// 更新输入目录显示
        /// </summary>
        /// <param name="selectedPath">选中的路径</param>
        void UpdateInputDirDisplay(string selectedPath);
        
        /// <summary>
        /// 显示导出路径管理对话框
        /// </summary>
        /// <param name="exportPaths">当前导出口路径</param>
        /// <returns>用户设置的导出路径，如果用户取消则返回null</returns>
        string ShowExportPathManagerDialog(string exportPaths);
        
        /// <summary>
        /// 获取切换模式按钮的文本
        /// </summary>
        /// <returns>按钮文本</returns>
        string GetToggleModeButtonText();
        
        /// <summary>
        /// 设置切换模式按钮的文本
        /// </summary>
        /// <param name="text">按钮文本</param>
        void SetToggleModeButtonText(string text);
        
        /// <summary>
        /// 获取监控按钮的文本
        /// </summary>
        /// <returns>按钮文本</returns>
        string GetMonitorButtonText();
        
        /// <summary>
        /// 设置监控按钮的文本
        /// </summary>
        /// <param name="text">按钮文本</param>
        void SetMonitorButtonText(string text);
        
        /// <summary>
        /// 获取输入目录路径
        /// </summary>
        /// <returns>输入目录路径</returns>
        string GetInputDirPath();
        
        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="path">目录路径</param>
        /// <returns>目录是否存在</returns>
        bool DirectoryExists(string path);
        
        /// <summary>
        /// 显示材料设置对话框
        /// </summary>
        void ShowMaterialSettingsDialog();
        
        /// <summary>
        /// 设置文件系统监控器的属性
        /// </summary>
        /// <param name="path">监控路径</param>
        /// <param name="filter">文件过滤器</param>
        /// <param name="includeSubdirectories">是否包含子目录</param>
        /// <param name="enableRaisingEvents">是否启用事件</param>
        void SetWatcherProperties(string path, string filter, bool includeSubdirectories, bool enableRaisingEvents);
        
        /// <summary>
        /// 显示导入的Excel数据
        /// </summary>
        /// <param name="data">导入的数据</param>
        void DisplayImportedExcelData(DataTable data);
        
        /// <summary>
        /// 获取Excel导入表单实例
        /// </summary>
        /// <returns>Excel导入表单实例</returns>
        object GetExcelImportFormInstance();
        
        /// <summary>
        /// 设置Excel导入表单实例
        /// </summary>
        /// <param name="instance">Excel导入表单实例</param>
        void SetExcelImportFormInstance(object instance);
        
        /// <summary>
        /// 获取Excel数据网格视图控件
        /// </summary>
        /// <returns>Excel数据网格视图控件</returns>
        DataGridView GetDgvExcelData();
        
        /// <summary>
        /// 显示材料选择对话框
        /// </summary>
        /// <param name="materials">材料列表</param>
        /// <param name="fileName">文件名</param>
        /// <param name="regexResult">正则表达式结果</param>
        /// <param name="opacity">透明度</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="excelData">Excel数据</param>
        /// <param name="searchColumnIndex">搜索列索引</param>
        /// <param name="returnColumnIndex">返回列索引</param>
        /// <param name="serialColumnIndex">序号列索引</param>
        /// <param name="serialNumber">序号</param>
        /// <returns>对话框结果</returns>
        DialogResult ShowMaterialSelectionDialog(
            List<string> materials, 
            string fileName, 
            string regexResult, 
            double opacity, 
            string width, 
            string height, 
            DataTable excelData, 
            int searchColumnIndex, 
            int returnColumnIndex, 
            int serialColumnIndex, 
            string serialNumber);
        
        /// <summary>
        /// 获取材料列表
        /// </summary>
        /// <returns>材料列表</returns>
        List<string> GetMaterials();
        
        /// <summary>
        /// 获取选中的材料
        /// </summary>
        /// <returns>选中的材料</returns>
        string GetSelectedMaterial();
        
        /// <summary>
        /// 获取订单号
        /// </summary>
        /// <returns>订单号</returns>
        string GetOrderNumber();
        
        /// <summary>
        /// 获取选中的导出路径
        /// </summary>
        /// <returns>选中的导出路径</returns>
        string GetSelectedExportPath();
        
        /// <summary>
        /// 获取数量列表
        /// </summary>
        /// <returns>数量列表</returns>
        List<string> GetQuantities();
        
        /// <summary>
        /// 获取序号列表
        /// </summary>
        /// <returns>序号列表</returns>
        List<string> GetSerialNumbers();
        
        /// <summary>
        /// 获取匹配的Excel行
        /// </summary>
        /// <returns>匹配的Excel行列表</returns>
        List<DataRow> GetMatchedRows();
        
        /// <summary>
        /// 获取调整后的尺寸
        /// </summary>
        /// <returns>调整后的尺寸</returns>
        string GetAdjustedDimensions();
        
        /// <summary>
        /// 获取固定字段
        /// </summary>
        /// <returns>固定字段</returns>
        string GetFixedField();
        
        /// <summary>
        /// 获取选中的出血值
        /// </summary>
        /// <returns>选中的出血值</returns>
        double GetSelectedTetBleed();
        
        /// <summary>
        /// 获取圆角半径
        /// </summary>
        /// <returns>圆角半径</returns>
        string GetCornerRadius();
        
        /// <summary>
        /// 获取是否使用PDF最后一页
        /// </summary>
        /// <returns>是否使用PDF最后一页</returns>
        bool GetUsePdfLastPage();
        
        /// <summary>
        /// 获取是否添加PDF图层
        /// </summary>
        /// <returns>是否添加PDF图层</returns>
        bool GetAddPdfLayers();
        
        /// <summary>
        /// 获取是否添加标识页
        /// </summary>
        /// <returns>是否添加标识页</returns>
        bool GetAddIdentifierPage();
        
        /// <summary>
        /// 获取标识页文字内容
        /// </summary>
        /// <returns>标识页文字内容</returns>
        string GetIdentifierPageContent();

        /// <summary>
        /// 获取排版模式
        /// </summary>
        /// <returns>排版模式</returns>
        LayoutMode GetLayoutMode();

        /// <summary>
        /// 获取布局数量
        /// </summary>
        /// <returns>布局数量</returns>
        int GetLayoutQuantity();

        /// <summary>
        /// 获取页面旋转角度（从布局计算结果）
        /// </summary>
        /// <returns>旋转角度（0°或270°）</returns>
        int GetRotationAngle();

        /// <summary>
        /// 更新MaterialSelectFormModern的正则结果
        /// </summary>
        /// <param name="regexResult">正则结果</param>
        void UpdateMaterialSelectFormRegexResult(string regexResult);

        /// <summary>
        /// 添加匹配的行到高亮集合
        /// </summary>
        /// <param name="rows">匹配的行</param>
        void AddMatchedRows(List<DataRow> rows);
        
        /// <summary>
        /// 刷新Excel数据显示
        /// </summary>
        void RefreshExcelDataDisplay();
        
        /// <summary>
        /// 刷新视图
        /// </summary>
        void Refresh();
        
        /// <summary>
        /// 获取cmbRegex控件的选中项
        /// </summary>
        /// <returns>cmbRegex控件的选中项文本</returns>
        string GetCmbRegexSelectedItem();
        
        /// <summary>
        /// 获取当前选择的正则表达式模式（用于文件重命名）
        /// </summary>
        /// <returns>主正则表达式模式（用于文件重命名）</returns>
        string GetSelectedRegexPattern();
        
        /// <summary>
        /// 获取Excel正则表达式模式（用于数据匹配）
        /// </summary>
        /// <returns>Excel正则表达式模式（用于数据匹配）</returns>
        string GetExcelRegexPattern();
        
        /// <summary>
        /// 刷新dgvFiles控件
        /// </summary>
        void RefreshDgvFiles();

        /// <summary>
        /// 获取数据网格视图控件
        /// </summary>
        /// <returns>数据网格视图控件</returns>
        DataGridView DataGrid { get; }
    #endregion
    }
}