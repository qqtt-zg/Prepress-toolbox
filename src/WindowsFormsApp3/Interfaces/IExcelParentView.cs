using System;

namespace WindowsFormsApp3.Interfaces
{
    /// <summary>
    /// Excel 导入功能的父视图接口
    /// 用于解耦 ExcelImportForm 对 Form1 的直接依赖
    /// </summary>
    public interface IExcelParentView
    {
        /// <summary>
        /// 更新状态栏文本
        /// </summary>
        /// <param name="message">状态消息</param>
        void UpdateStatus(string message);

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误消息</param>
        void ShowError(string message);
    }

    /// <summary>
    /// Excel 导入表单接口
    /// 定义 Excel 导入表单的公共契约
    /// </summary>
    public interface IExcelImportForm
    {
        /// <summary>
        /// 导入的数据表
        /// </summary>
        System.Data.DataTable ImportedData { get; }

        /// <summary>
        /// 搜索列索引
        /// </summary>
        int SearchColumnIndex { get; }

        /// <summary>
        /// 返回列（数量列）索引
        /// </summary>
        int ReturnColumnIndex { get; }

        /// <summary>
        /// 序号列索引
        /// </summary>
        int NewColumnIndex { get; }

        /// <summary>
        /// 选中的正则表达式模式
        /// </summary>
        string SelectedRegexPattern { get; }

        /// <summary>
        /// 是否为嵌入模式
        /// </summary>
        bool IsEmbedded { get; set; }

        /// <summary>
        /// 数据导入完成事件
        /// </summary>
        event EventHandler<System.Data.DataTable> OnDataImported;
    }
}
