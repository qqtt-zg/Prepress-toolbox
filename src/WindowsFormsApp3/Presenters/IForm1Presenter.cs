using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows.Forms;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Presenters
{
    /// <summary>
    /// Form1演示器接口，定义了演示器需要实现的方法和功能
    /// </summary>
    public interface IForm1Presenter
    {
        /// <summary>
        /// 初始化演示器
        /// </summary>
        void Initialize();

        /// <summary>
        /// 处理即时重命名切换事件
        /// </summary>
        void HandleImmediateRenameToggle();

        /// <summary>
        /// 处理模式切换事件
        /// </summary>
        void HandleModeToggle();

        /// <summary>
        /// 处理文件监控事件
        /// </summary>
        void HandleFileMonitoring();

        /// <summary>
        /// 处理Excel导入事件
        /// </summary>
        void HandleExcelImport();

        /// <summary>
        /// 处理配置导入导出事件
        /// </summary>
        void HandleConfigImportExport();

        /// <summary>
        /// 处理撤销操作
        /// </summary>
        void HandleUndo();

        /// <summary>
        /// 处理重做操作
        /// </summary>
        void HandleRedo();

        /// <summary>
        /// 处理单元格值变化事件
        /// </summary>
        void HandleCellValueChanged(int rowIndex, int columnIndex, object oldValue, object newValue);

        /// <summary>
        /// 处理列头点击排序事件
        /// </summary>
        void HandleColumnHeaderMouseClick(int columnIndex);

        /// <summary>
        /// 加载文件到视图
        /// </summary>
        void LoadFiles(IEnumerable<FileInfo> files);

        /// <summary>
        /// 更新文件列表
        /// </summary>
        void UpdateFileList();

        /// <summary>
        /// 保存设置
        /// </summary>
        void SaveSettings();

        /// <summary>
        /// 加载设置
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// 处理Excel导入完成事件
        /// </summary>
        void HandleExcelImportComplete(DataTable importedData);

        /// <summary>
        /// 处理清除Excel数据事件
        /// </summary>
        void HandleClearExcelData();

        /// <summary>
        /// 处理模式切换事件（新方法）
        /// </summary>
        void HandleToggleMode();

        /// <summary>
        /// 处理快捷键按下事件
        /// </summary>
        void HandleKeyDown(KeyEventArgs e);

        /// <summary>
        /// 处理表单加载完成事件
        /// </summary>
        void HandleFormLoadComplete();

        /// <summary>
        /// 处理表单关闭事件（新方法）
        /// </summary>
        void HandleFormClosing(FormClosingEventArgs e);

        /// <summary>
        /// 处理表单调整大小事件（新方法）
        /// </summary>
        void HandleResize();
        
        /// <summary>
        /// 处理添加文件到网格事件
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="material">材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="unit">单位</param>
        /// <param name="adjustedDimensions">调整后的尺寸</param>
        /// <param name="fixedField">固定字段</param>
        /// <param name="serialNumber">序号</param>
        void HandleAddFileToGrid(FileInfo fileInfo, string material, string orderNumber, string quantity, string unit, string adjustedDimensions, string fixedField, string serialNumber, string compositeColumnValue = "");
        
        /// <summary>
        /// 处理立即重命名文件事件
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="selectedMaterial">选择的材料</param>
        /// <param name="orderNumber">订单号</param>
        /// <param name="quantity">数量</param>
        /// <param name="unit">单位</param>
        /// <param name="exportPath">导出路径</param>
        /// <param name="tetBleed">TET出血</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="fixedField">固定字段</param>
        /// <param name="serialNumber">序号</param>
        /// <param name="cornerRadius">圆角半径</param>
        /// <param name="usePdfLastPage">是否使用PDF最后一页</param>
        /// <param name="addPdfLayers">是否添加PDF图层</param>
        /// <param name="compositeColumnValue">列组合值</param>
        void HandleRenameFileImmediately(FileInfo fileInfo, string selectedMaterial, string orderNumber, string quantity, string unit, string exportPath, double tetBleed, string width, string height, string fixedField, string serialNumber, string cornerRadius, bool usePdfLastPage, bool addPdfLayers, string compositeColumnValue = "");
        
        /// <summary>
        /// 处理重命名按钮点击事件
        /// </summary>
        void HandleRenameClick();
        
        /// <summary>
        /// 处理选择输入目录按钮点击事件
        /// </summary>
        void HandleSelectInputDirClick();
        
        /// <summary>
        /// 处理管理正则表达式按钮点击事件
        /// </summary>
        void HandleManageRegexClick();
        
        /// <summary>
        /// 处理正则表达式测试按钮点击事件
        /// </summary>
        void HandleRegexTestClick();
        
        /// <summary>
        /// 处理管理导出路径按钮点击事件
        /// </summary>
        void HandleManageExportPathsClick();
        
        /// <summary>
        /// 处理切换模式按钮点击事件
        /// </summary>
        void HandleToggleModeClick();
        
        /// <summary>
        /// 处理监控按钮点击事件
        /// </summary>
        void HandleMonitorClick();
        
        /// <summary>
        /// 处理导出Excel按钮点击事件
        /// </summary>
        void HandleExportExcelClick();
        
        /// <summary>
        /// 处理显示材料选择对话框事件
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        void HandleShowMaterialSelectionDialog(FileInfo fileInfo, string width, string height);
        
    }
}