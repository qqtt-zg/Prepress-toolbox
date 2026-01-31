using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AntdUI;

namespace WindowsFormsApp3
{
    public class FileRenameInfo : INotifyPropertyChanged
    {
        private string _serialNumber;
        private string _originalName;
        private string _newName;
        private string _fullPath;
        private string _regexResult;
        private string _orderNumber;
        private string _material;
        private string _quantity;
        private string _dimensions;
        private string _shape;
        private string _process;
        private string _layoutRows;
        private string _layoutColumns;
        private string _time;
        private string _status;
        private string _errorMessage;
        private int? _pageCount;
        private string _compositeColumn; // 添加列组合属性
        private string _width;      // PDF 宽度
        private string _height;     // PDF 高度
        private string _tetBleed;   // 出血值
        private string _fileExtension; // 文件扩展名
        private string _impositionMode; // 排版模式（"平张"或"卷装"）

        // 保留模式相关属性
        private Dictionary<string, string> _backupData = new Dictionary<string, string>();
        private bool _isPreserveMode = false;
        private Dictionary<string, string> _fieldPrefixMapping = new Dictionary<string, string>(); // 字段到前缀的映射

        public int? PageCount
        {
            get { return _pageCount; }
            set { _pageCount = value; OnPropertyChanged(); }
        }

        public string SerialNumber
        {
            get { return _serialNumber; }
            set { _serialNumber = value; OnPropertyChanged(); }
        }
        public string OriginalName
        {
            get { return _originalName; }
            set { _originalName = value; OnPropertyChanged(); }
        }
        public string NewName
        {
            get { return _newName; }
            set { _newName = value; OnPropertyChanged(); }
        }
        public string FullPath
        {
            get { return _fullPath; }
            set { _fullPath = value; OnPropertyChanged(); }
        }
        public string RegexResult
        {
            get { return _regexResult; }
            set { _regexResult = value; OnPropertyChanged(); OnPropertyChanged("StatusBadge"); }
        }

        // 添加 Badge 属性用于 UI 显示
        [Newtonsoft.Json.JsonIgnore]
        public AntdUI.CellBadge StatusBadge
        {
            get
            {
                if (string.IsNullOrEmpty(RegexResult)) return null;
                if (RegexResult == "OK" || RegexResult == "成功") return new AntdUI.CellBadge(AntdUI.TState.Success, RegexResult);
                if (RegexResult.StartsWith("Error") || RegexResult.Contains("失败")) return new AntdUI.CellBadge(AntdUI.TState.Error, RegexResult);
                return new AntdUI.CellBadge(AntdUI.TState.Default, RegexResult);
            }
        }

        public string OrderNumber
        {
            get { return _orderNumber; }
            set { _orderNumber = value; OnPropertyChanged(); }
        }
        public string Material
        {
            get { return _material; }
            set { _material = value; OnPropertyChanged(); }
        }
        public string Quantity
        {
            get { return _quantity; }
            set { _quantity = value; OnPropertyChanged(); }
        }
        public string Dimensions
        {
            get { return _dimensions; }
            set { _dimensions = value; OnPropertyChanged(); }
        }
        public string Shape
        {
            get { return _shape; }
            set { _shape = value; OnPropertyChanged(); }
        }
        public string Process
        {
            get { return _process; }
            set { _process = value; OnPropertyChanged(); }
        }
        public string LayoutRows
        {
            get { return _layoutRows; }
            set 
            { 
                _layoutRows = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(LayoutCount)); 
            }
        }
        public string LayoutColumns
        {
            get { return _layoutColumns; }
            set 
            { 
                _layoutColumns = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(LayoutCount)); 
            }
        }
        public string LayoutCount
        {
            get
            {
                if (int.TryParse(LayoutRows, out int rows) && int.TryParse(LayoutColumns, out int cols))
                {
                    return (rows * cols).ToString();
                }
                return "";
            }
        }
        public string Time
        {
            get { return _time; }
            set { _time = value; OnPropertyChanged(); }
        }
        public string Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged(); }
        }
        public string ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; OnPropertyChanged(); }
        }
        // 添加列组合属性
        public string CompositeColumn
        {
            get { return _compositeColumn; }
            set
            {
                if (_compositeColumn != value)
                {
                    _compositeColumn = value;
                    System.Diagnostics.Debug.WriteLine($"FileRenameInfo.CompositeColumn 设置: '{value}' (文件: {OriginalName})");
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// PDF 宽度
        /// </summary>
        public string Width
        {
            get { return _width; }
            set { _width = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// PDF 高度
        /// </summary>
        public string Height
        {
            get { return _height; }
            set { _height = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 出血值
        /// </summary>
        public string TetBleed
        {
            get { return _tetBleed; }
            set { _tetBleed = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string FileExtension
        {
            get { return _fileExtension; }
            set { _fileExtension = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 排版模式（"平张"或"卷装"）
        /// </summary>
        public string ImpositionMode
        {
            get { return _impositionMode; }
            set { _impositionMode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 备份数据（保留模式用）
        /// </summary>
        public Dictionary<string, string> BackupData
        {
            get { return _backupData; }
            set { _backupData = value ?? new Dictionary<string, string>(); OnPropertyChanged(); }
        }

        /// <summary>
        /// 是否为保留模式
        /// </summary>
        public bool IsPreserveMode
        {
            get { return _isPreserveMode; }
            set { _isPreserveMode = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// 字段到前缀的映射（用于保留数据提取）
        /// </summary>
        public Dictionary<string, string> FieldPrefixMapping
        {
            get { return _fieldPrefixMapping; }
            set { _fieldPrefixMapping = value ?? new Dictionary<string, string>(); OnPropertyChanged(); }
        }

        /// <summary>
        /// 从原文件名备份指定字段
        /// </summary>
        public void BackupFieldFromOriginalName(string fieldName)
        {
            if (string.IsNullOrEmpty(OriginalName))
                return;

            try
            {
                string extractedValue = ExtractFieldValue(fieldName);
                
                // ✅ 修改：只有当文件名中实际存在该字段的值时，才备份
                // 如果文件名中没有该字段的前缀标记，就返回的是当前属性值或空字符串
                // 这会导致覆盖原有属性值的问题
                // 所以：只备份文件名中明确存在的前缀标记对应的值
                if (HasPrefixInOriginalName(fieldName))
                {
                    BackupData[fieldName] = extractedValue;
                    System.Diagnostics.Debug.WriteLine($"[BackupFieldFromOriginalName] 字段'{fieldName}': 从文件名备份 extractedValue='{extractedValue}'");
                }
                else
                {
                    // 文件名中没有该字段，不备份，这样恢复时就不会覆盖原属性值
                    System.Diagnostics.Debug.WriteLine($"[BackupFieldFromOriginalName] 字段'{fieldName}': 文件名中不存在此字段，不备份");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"备份字段 {fieldName} 时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查文件名中是否包含该字段的前缀标记
        /// </summary>
        private bool HasPrefixInOriginalName(string fieldName)
        {
            if (FieldPrefixMapping.ContainsKey(fieldName))
            {
                string prefix = FieldPrefixMapping[fieldName];
                return OriginalName.Contains(prefix);
            }
            return false;
        }

        /// <summary>
        /// 从原文件名提取字段值
        /// </summary>
        private string ExtractFieldValue(string fieldName)
        {
            // ✅ 新逻辑：优先使用配置中的前缀映射
            if (FieldPrefixMapping.ContainsKey(fieldName))
            {
                string prefix = FieldPrefixMapping[fieldName];
                var result = ExtractValueByPrefix(prefix);
                System.Diagnostics.Debug.WriteLine($"[ExtractFieldValue] 字段'{fieldName}' 从配置映射使用前缀'{prefix}'，提取结果='{result}'");
                return result;
            }
            
            System.Diagnostics.Debug.WriteLine($"[ExtractFieldValue] 字段'{fieldName}' 未在FieldPrefixMapping中，使用硬编码前缀");

            // 备用：使用硬编码的前缀（兼容老配置）
            switch (fieldName)
            {
                case "订单号":
                    return ExtractOrderNumber();
                case "材料":
                    return ExtractMaterial();
                case "工艺":
                    return ExtractProcess();
                case "数量":
                    return ExtractQuantity();
                case "行数":
                    return ExtractRowCount();
                case "列数":
                    return ExtractColumnCount();
                case "尺寸":
                    return ExtractDimensions();
                case "正则结果":
                    return ExtractRegexResult();
                default:
                    return "";
            }
        }

        /// <summary>
        /// 通用方法：按前缀提取值（从前缀到下一个&符号）
        /// </summary>
        /// <param name="prefix">要查找的前缀（如 "&ID-"）</param>
        /// <returns>前缀后到下一个&符号之间的内容，如果没找到返回空</returns>
        private string ExtractValueByPrefix(string prefix)
        {
            if (string.IsNullOrEmpty(OriginalName) || string.IsNullOrEmpty(prefix))
                return "";

            // 在原文件名中查找前缀的位置
            int prefixIndex = OriginalName.IndexOf(prefix);
            if (prefixIndex < 0)
                return "";  // 未找到前缀

            // 从前缀后开始查找下一个 & 符号
            int startIndex = prefixIndex + prefix.Length;
            int nextAmpersandIndex = OriginalName.IndexOf("&", startIndex);

            // 提取从前缀到下一个&（或到末尾）之间的内容
            if (nextAmpersandIndex > startIndex)
                return OriginalName.Substring(startIndex, nextAmpersandIndex - startIndex);
            else if (nextAmpersandIndex < 0)
                return OriginalName.Substring(startIndex);  // 没有下一个&，返回到末尾
            else
                return "";  // 空值
        }

        /// <summary>
        /// 提取订单号
        /// </summary>
        private string ExtractOrderNumber()
        {
            // ... existing code ...
            var prefixValue = ExtractValueByPrefix("&ID-");
            if (!string.IsNullOrEmpty(prefixValue))
                return prefixValue;
            return OrderNumber ?? "";
        }

        /// <summary>
        /// 提取材料
        /// </summary>
        private string ExtractMaterial()
        {
            // ... existing code ...
            var prefixValue = ExtractValueByPrefix("&MT-");
            if (!string.IsNullOrEmpty(prefixValue))
                return prefixValue;
            return Material ?? "";
        }

        /// <summary>
        /// 提取工艺
        /// </summary>
        private string ExtractProcess()
        {
            // ... existing code ...
            var prefixValue = ExtractValueByPrefix("&DP-");
            if (!string.IsNullOrEmpty(prefixValue))
                return prefixValue;
            return Process ?? "";
        }

        /// <summary>
        /// 提取数量
        /// </summary>
        private string ExtractQuantity()
        {
            var prefixValue = ExtractValueByPrefix("&DN-");
            if (!string.IsNullOrEmpty(prefixValue))
                return prefixValue;
            return "";
        }

        /// <summary>
        /// 提取行数
        /// </summary>
        private string ExtractRowCount()
        {
            var prefixValue = ExtractValueByPrefix("&Row-");
            if (!string.IsNullOrEmpty(prefixValue))
                return prefixValue;
            return "";
        }

        /// <summary>
        /// 提取列数
        /// </summary>
        private string ExtractColumnCount()
        {
            var prefixValue = ExtractValueByPrefix("&Col-");
            if (!string.IsNullOrEmpty(prefixValue))
                return prefixValue;
            return "";
        }

        /// <summary>
        /// 提取尺寸（使用默认前缀&CU-，但会被FieldPrefixMapping覆盖）
        /// </summary>
        private string ExtractDimensions()
        {
            // 如果有配置映射，应在ExtractFieldValue中已处理，这里只是默认值
            var prefixValue = ExtractValueByPrefix("&CU-");
            if (!string.IsNullOrEmpty(prefixValue))
                return prefixValue;
            return Dimensions ?? "";
        }

        /// <summary>
        /// 提取正则结果（使用默认前缀&ID-，但会被FieldPrefixMapping覆盖）
        /// </summary>
        private string ExtractRegexResult()
        {
            // 如果有配置映射，应在ExtractFieldValue中已处理，这里只是默认值
            var prefixValue = ExtractValueByPrefix("&ID-");
            if (!string.IsNullOrEmpty(prefixValue))
                return prefixValue;
            return RegexResult ?? "";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 创建当前对象的副本
        /// </summary>
        public FileRenameInfo Clone()
        {
            var clone = new FileRenameInfo
            {
                SerialNumber = this.SerialNumber,
                OriginalName = this.OriginalName,
                NewName = this.NewName,
                FullPath = this.FullPath,
                RegexResult = this.RegexResult,
                OrderNumber = this.OrderNumber,
                Material = this.Material,
                Quantity = this.Quantity,
                Dimensions = this.Dimensions,
                Process = this.Process,
                LayoutRows = this.LayoutRows,
                LayoutColumns = this.LayoutColumns,
                Time = this.Time,
                Status = this.Status,
                ErrorMessage = this.ErrorMessage,
                PageCount = this.PageCount,
                CompositeColumn = this.CompositeColumn,
                Width = this.Width,
                Height = this.Height,
                TetBleed = this.TetBleed,
                FileExtension = this.FileExtension,
                ImpositionMode = this.ImpositionMode,
                IsPreserveMode = this.IsPreserveMode,
                Shape = this.Shape
            };

            // 深拷贝字典
            if (this.BackupData != null)
            {
                clone.BackupData = new Dictionary<string, string>(this.BackupData);
            }

            if (this.FieldPrefixMapping != null)
            {
                clone.FieldPrefixMapping = new Dictionary<string, string>(this.FieldPrefixMapping);
            }

            return clone;
        }
    }
}