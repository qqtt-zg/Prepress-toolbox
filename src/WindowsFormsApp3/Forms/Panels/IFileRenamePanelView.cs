using System;
using System.Collections.Generic;
using System.ComponentModel;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// 文件重命名面板视图接口
    /// 定义面板与Presenter之间的契约
    /// </summary>
    public interface IFileRenamePanelView
    {
        #region 事件

        /// <summary>
        /// 正则表达式选择变化事件
        /// </summary>
        event EventHandler<string> RegexPatternChanged;

        #endregion

        #region 属性

        /// <summary>
        /// 输入目录路径
        /// </summary>
        string InputDirectory { get; set; }

        /// <summary>
        /// 是否正在监控
        /// </summary>
        bool IsMonitoring { get; set; }

        /// <summary>
        /// 是否为复制模式（false为剪切模式）
        /// </summary>
        bool IsCopyMode { get; }

        /// <summary>
        /// 是否为手动重命名模式
        /// </summary>
        bool IsImmediateMode { get; set; }

        /// <summary>
        /// 文件列表数据源
        /// </summary>
        BindingList<FileRenameInfo> FileList { get; set; }

        /// <summary>
        /// 选中的正则表达式模式
        /// </summary>
        string SelectedRegexPattern { get; }

        /// <summary>
        /// 选中的正则表达式值（实际正则表达式）
        /// </summary>
        string SelectedRegexValue { get; }

        /// <summary>
        /// 当前JSON配置名称
        /// </summary>
        string CurrentConfigName { get; set; }

        /// <summary>
        /// Excel导入的数据表
        /// </summary>
        System.Data.DataTable ExcelData { get; set; }

        /// <summary>
        /// Excel搜索列索引（用户在导入时选择的列）
        /// </summary>
        int ExcelSearchColumnIndex { get; set; }

        /// <summary>
        /// Excel返回列索引（数量列，用户在导入时选择的列）
        /// </summary>
        int ExcelReturnColumnIndex { get; set; }

        /// <summary>
        /// Excel序号列索引（用户在导入时选择的列）
        /// </summary>
        int ExcelSerialColumnIndex { get; set; }

        #endregion

        #region UI更新方法

        /// <summary>
        /// 更新状态栏文本
        /// </summary>
        /// <param name="message">状态消息</param>
        void UpdateStatus(string message);

        /// <summary>
        /// 更新正则表达式下拉框
        /// </summary>
        /// <param name="patterns">正则表达式模式列表</param>
        void UpdateRegexComboBox(List<string> patterns);

        /// <summary>
        /// 更新JSON文件下拉框
        /// </summary>
        /// <param name="jsonFiles">JSON文件列表</param>
        void UpdateJsonFilesDropdown(List<string> jsonFiles);

        /// <summary>
        /// 更新材料列表（用于右键菜单）
        /// </summary>
        /// <param name="materials">材料列表</param>
        void UpdateMaterialsContextMenu(List<string> materials);

        /// <summary>
        /// 刷新文件表格显示
        /// </summary>
        void RefreshFileTable();

        /// <summary>
        /// 更新监控按钮状态
        /// </summary>
        /// <param name="isMonitoring">是否正在监控</param>
        void UpdateMonitorButtonState(bool isMonitoring);

        /// <summary>
        /// 更新模式切换按钮状态
        /// </summary>
        /// <param name="isCopyMode">是否为复制模式</param>
        void UpdateModeButtonState(bool isCopyMode);

        /// <summary>
        /// 更新手动/批量模式按钮状态
        /// </summary>
        /// <param name="isImmediateMode">是否为手动模式</param>
        void UpdateImmediateModeButtonState(bool isImmediateMode);

        #endregion

        #region 消息显示方法

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        void ShowError(string message);

        /// <summary>
        /// 显示成功消息
        /// </summary>
        /// <param name="message">成功消息</param>
        void ShowSuccess(string message);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">警告消息</param>
        void ShowWarning(string message);

        /// <summary>
        /// 显示信息消息
        /// </summary>
        /// <param name="message">信息消息</param>
        void ShowInfo(string message);

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息</param>
        /// <param name="title">对话框标题</param>
        /// <returns>用户是否确认</returns>
        bool ShowConfirm(string message, string title = "确认");

        #endregion

        #region 对话框方法

        /// <summary>
        /// 显示文件夹浏览对话框
        /// </summary>
        /// <param name="description">对话框描述</param>
        /// <param name="selectedPath">初始选中路径</param>
        /// <returns>用户选择的路径，取消则返回null</returns>
        string ShowFolderBrowser(string description = "选择文件夹", string selectedPath = "");

        /// <summary>
        /// 显示文件保存对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <param name="defaultFileName">默认文件名</param>
        /// <returns>用户选择的文件路径，取消则返回null</returns>
        string ShowSaveFileDialog(string filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*", string defaultFileName = "");

        /// <summary>
        /// 显示文件打开对话框
        /// </summary>
        /// <param name="filter">文件过滤器</param>
        /// <returns>用户选择的文件路径，取消则返回null</returns>
        string ShowOpenFileDialog(string filter = "Excel文件 (*.xlsx;*.xls)|*.xlsx;*.xls|所有文件 (*.*)|*.*");

        /// <summary>
        /// 显示材料选择对话框（手动模式）
        /// </summary>
        /// <param name="materials">材料列表</param>
        /// <param name="fileName">文件名</param>
        /// <param name="regexResult">正则匹配结果</param>
        /// <param name="width">PDF宽度</param>
        /// <param name="height">PDF高度</param>
        /// <param name="tetBleed">出血值</param>
        /// <param name="isColumnCombineMode">是否启用列组合</param>
        /// <param name="columnNames">列名列表</param>
        /// <param name="columnItemsMap">列项映射</param>
        /// <param name="initialSerialNumber">初始序号</param>
        /// <param name="result">输出结果</param>
        /// <returns>对话框结果</returns>
        System.Windows.Forms.DialogResult ShowMaterialSelectionDialog(
            List<string> materials,
            string fileName,
            string regexResult,
            string width,
            string height,
            string tetBleed,
            bool isColumnCombineMode,
            List<string> columnNames,
            Dictionary<string, List<string>> columnItemsMap,
            int initialSerialNumber,
            out MaterialSelectionResult result
        );

        #endregion

        #region 进度显示方法

        /// <summary>
        /// 显示进度窗体
        /// </summary>
        /// <param name="message">进度消息</param>
        void ShowProgress(string message);

        /// <summary>
        /// 更新进度
        /// </summary>
        /// <param name="current">当前值</param>
        /// <param name="total">总数</param>
        /// <param name="message">进度消息</param>
        void UpdateProgress(int current, int total, string message = "");

        /// <summary>
        /// 隐藏进度窗体
        /// </summary>
        void HideProgress();

        #endregion
    }

    /// <summary>
    /// 单元格值变化事件参数
    /// </summary>
    public class CellValueChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 行索引
        /// </summary>
        public int RowIndex { get; set; }

        /// <summary>
        /// 列索引
        /// </summary>
        public int ColumnIndex { get; set; }

        /// <summary>
        /// 新值
        /// </summary>
        public object NewValue { get; set; }

        /// <summary>
        /// 旧值
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// 列名
        /// </summary>
        public string ColumnName { get; set; }
    }
}
