using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WindowsFormsApp3.Models;

namespace WindowsFormsApp3.Presenters
{
    /// <summary>
    /// 文件重命名面板Presenter接口
    /// 定义业务逻辑处理方法
    /// </summary>
    public interface IFileRenamePanelPresenter
    {
        #region 初始化与生命周期

        /// <summary>
        /// 初始化Presenter
        /// </summary>
        void Initialize();

        /// <summary>
        /// 加载设置和配置
        /// </summary>
        void LoadSettings();

        /// <summary>
        /// 保存设置和配置
        /// </summary>
        void SaveSettings();

        #endregion

        #region 目录选择

        /// <summary>
        /// 处理选择输入目录
        /// </summary>
        void HandleSelectInputDir();

        /// <summary>
        /// 设置输入目录（从下拉框选择时调用）
        /// </summary>
        void SetInputDirectory(string dirPath);

        #endregion

        #region 文件监控

        /// <summary>
        /// 处理切换文件监控状态
        /// </summary>
        void HandleToggleMonitoring();

        /// <summary>
        /// 启动文件监控
        /// </summary>
        void StartMonitoring();

        /// <summary>
        /// 停止文件监控
        /// </summary>
        void StopMonitoring();

        #endregion

        #region 模式切换

        /// <summary>
        /// 处理切换复制/剪切模式
        /// </summary>
        void HandleModeToggle();

        /// <summary>
        /// 处理切换手动/批量重命名模式
        /// </summary>
        void HandleImmediateModeToggle();

        /// <summary>
        /// 启动手动模式
        /// </summary>
        void StartImmediateMode();

        /// <summary>
        /// 停止手动模式
        /// </summary>
        void StopImmediateMode();

        #endregion

        #region 文件重命名

        /// <summary>
        /// 处理批量重命名
        /// </summary>
        /// <returns>异步任务</returns>
        Task HandleRenameAsync();

        /// <summary>
        /// 处理立即重命名单个文件
        /// </summary>
        /// <param name="fileInfo">文件重命名信息</param>
        /// <returns>重命名是否成功</returns>
        Task<bool> RenameFileAsync(FileRenameInfo fileInfo);

        /// <summary>
        /// 立即重命名文件（同步版本）
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="newName">新文件名</param>
        /// <returns>重命名是否成功</returns>
        bool RenameFileImmediately(string sourcePath, string newName);

        /// <summary>
        /// 根据模式处理文件（复制或剪切）
        /// </summary>
        /// <param name="sourcePath">源文件路径</param>
        /// <param name="destPath">目标文件路径</param>
        /// <param name="isCopyMode">是否为复制模式</param>
        /// <returns>处理是否成功</returns>
        bool ProcessFileByMode(string sourcePath, string destPath, bool isCopyMode);

        #endregion

        #region Excel导入导出

        /// <summary>
        /// 处理导入Excel
        /// </summary>
        /// <returns>异步任务</returns>
        Task HandleImportExcelAsync();

        /// <summary>
        /// 处理清除Excel数据
        /// </summary>
        void HandleClearExcel();

        /// <summary>
        /// 处理导出Excel
        /// </summary>
        void HandleExportExcel();

        /// <summary>
        /// 显示导入的Excel数据
        /// </summary>
        /// <param name="data">Excel数据表</param>
        void DisplayImportedExcelData(DataTable data);

        /// <summary>
        /// 匹配Excel数据与文件列表
        /// </summary>
        void MatchExcelData();

        #endregion

        #region JSON数据管理

        /// <summary>
        /// 处理保存JSON
        /// </summary>
        void HandleSaveJson();

        /// <summary>
        /// 处理加载选定的JSON文件
        /// </summary>
        /// <param name="jsonFileName">JSON文件名</param>
        void HandleLoadJson(string jsonFileName);

        /// <summary>
        /// 执行自动保存
        /// </summary>
        void PerformAutoSave();

        /// <summary>
        /// 保存数据到JSON文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        void SaveToJsonFile(string filePath);

        /// <summary>
        /// 从JSON文件加载数据
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns>文件重命名信息列表</returns>
        List<FileRenameInfo> LoadFromJsonFile(string filePath);

        /// <summary>
        /// 获取已保存的配置列表
        /// </summary>
        /// <returns>配置名称列表</returns>
        List<string> GetSavedConfigs();

        #endregion

        #region 正则表达式管理

        /// <summary>
        /// 加载正则表达式模式
        /// </summary>
        void LoadRegexPatterns();

        /// <summary>
        /// 保存正则表达式设置
        /// </summary>
        void SaveRegexSettings();

        /// <summary>
        /// 获取所有正则表达式模式
        /// </summary>
        /// <returns>正则表达式模式字典（显示名称 -> 正则表达式）</returns>
        Dictionary<string, string> GetRegexPatterns();

        /// <summary>
        /// 添加正则表达式模式
        /// </summary>
        /// <param name="name">模式名称</param>
        /// <param name="pattern">正则表达式</param>
        void AddRegexPattern(string name, string pattern);

        /// <summary>
        /// 移除正则表达式模式
        /// </summary>
        /// <param name="name">模式名称</param>
        void RemoveRegexPattern(string name);

        #endregion

        #region 材料管理

        /// <summary>
        /// 加载材料列表
        /// </summary>
        void LoadMaterials();

        /// <summary>
        /// 保存材料设置
        /// </summary>
        void SaveMaterialSettings();

        /// <summary>
        /// 获取所有材料
        /// </summary>
        /// <returns>材料列表</returns>
        List<string> GetMaterials();

        /// <summary>
        /// 添加材料
        /// </summary>
        /// <param name="material">材料名称</param>
        void AddMaterial(string material);

        /// <summary>
        /// 移除材料
        /// </summary>
        /// <param name="material">材料名称</param>
        void RemoveMaterial(string material);

        #endregion

        #region 数据表格操作

        /// <summary>
        /// 添加文件到表格
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        void AddFileToTable(FileRenameInfo fileInfo);

        /// <summary>
        /// 更新表格中的文件行
        /// </summary>
        /// <param name="fileInfo">文件信息</param>
        void UpdateFileInTable(FileRenameInfo fileInfo);

        /// <summary>
        /// 从表格中删除文件行
        /// </summary>
        /// <param name="rowIndex">行索引</param>
        void RemoveFileFromTable(int rowIndex);

        /// <summary>
        /// 刷新文件列表
        /// </summary>
        void RefreshFileList();

        /// <summary>
        /// 清空文件列表
        /// </summary>
        void ClearFileList();

        /// <summary>
        /// 添加空行到表格
        /// </summary>
        void AddEmptyRowToTable();

        #endregion

        #region 单元格编辑

        /// <summary>
        /// 处理单元格值变化
        /// </summary>
        /// <param name="rowIndex">行索引</param>
        /// <param name="columnIndex">列索引</param>
        /// <param name="newValue">新值</param>
        /// <param name="oldValue">旧值</param>
        void HandleCellValueChanged(int rowIndex, int columnIndex, object newValue, object oldValue);

        #endregion

        #region 排版材料类型

        /// <summary>
        /// 获取当前排版材料类型（FlatSheet 或 RollMaterial）
        /// </summary>
        string CurrentImpositionMaterialType { get; }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取导出路径
        /// </summary>
        /// <returns>导出路径</returns>
        string GetExportPath();

        /// <summary>
        /// 设置导出路径
        /// </summary>
        /// <param name="path">导出路径</param>
        void SetExportPath(string path);

        /// <summary>
        /// 从文件名中提取正则匹配结果
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>正则匹配结果</returns>
        string ExtractRegexResult(string fileName);

        /// <summary>
        /// 匹配 Excel 数据与正则结果
        /// </summary>
        /// <param name="regexResult">正则匹配结果</param>
        /// <returns>匹配的 Excel 数据列表</returns>
        List<Models.ExcelMatchData> MatchExcelData(string regexResult);

        /// <summary>
        /// 处理正则表达式变化事件
        /// </summary>
        /// <param name="newPattern">新的正则表达式</param>
        System.Threading.Tasks.Task HandleRegexPatternChangedAsync(string newPattern);

        /// <summary>
        /// 验证文件是否需要处理
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否需要处理</returns>
        bool ShouldProcessFile(string fileName);

        #endregion
    }
}
