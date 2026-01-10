using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Models
{
    /// <summary>
    /// 文件名组件配置DTO，用于构建新文件名
    /// </summary>
    public class FileNameComponents
    {
        /// <summary>
        /// 正则结果
        /// </summary>
        public string RegexResult { get; set; }

        /// <summary>
        /// 订单号
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// 材料
        /// </summary>
        public string Material { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public string Quantity { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 尺寸
        /// </summary>
        public string Dimensions { get; set; }

        /// <summary>
        /// 工艺
        /// </summary>
        public string Process { get; set; }

        /// <summary>
        /// 序号
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// 组合列值
        /// </summary>
        public string CompositeColumn { get; set; }

        /// <summary>
        /// 布局行数
        /// </summary>
        public string LayoutRows { get; set; }

        /// <summary>
        /// 布局列数
        /// </summary>
        public string LayoutColumns { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// 分隔符
        /// </summary>
        public string Separator { get; set; }

        /// <summary>
        /// 启用的组件配置
        /// </summary>
        public FileNameComponentsConfig EnabledComponents { get; set; }

        /// <summary>
        /// 组件顺序列表
        /// </summary>
        public List<string> ComponentOrder { get; set; } = new List<string>();

        /// <summary>
        /// 前缀字典，用于存储各分组的前缀
        /// </summary>
        public Dictionary<string, string> Prefixes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 保留分组数据，用于返单场景
        /// </summary>
        public Dictionary<string, string> PreserveGroupData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 保留分组配置，用于确定哪些分组应该被保留
        /// </summary>
        public Dictionary<string, bool> PreserveGroupConfig { get; set; } = new Dictionary<string, bool>();

        /// <summary>
        /// 原始文件名，用于检测返单场景
        /// </summary>
        public string OriginalFileName { get; set; }

        /// <summary>
        /// 构建文件名
        /// </summary>
        /// <returns>构建的文件名</returns>
        public string BuildFileName()
        {
            var newNameParts = new List<string>();
            
            try
            {
                // 添加LogHelper日志以追踪执行
                LogHelper.Debug("[BuildFileName] 开始构建文件名");
                LogHelper.Debug($"[BuildFileName] 输入参数 - LayoutRows='{LayoutRows}', LayoutColumns='{LayoutColumns}', Separator='{Separator}'");
                LogHelper.Debug($"[BuildFileName] 原始文件名: '{OriginalFileName}'");

                // 检测返单场景并提取保留分组数据
                bool shouldExtractPreserve = !string.IsNullOrEmpty(OriginalFileName) && (PreserveGroupData == null || PreserveGroupData.Count == 0);
                LogHelper.Debug($"[BuildFileName] 保留分组提取条件 - OriginalFileName非空: {!string.IsNullOrEmpty(OriginalFileName)}, PreserveGroupData为null: {PreserveGroupData == null}, PreserveGroupData为空: {(PreserveGroupData?.Count ?? 0) == 0}, 应该提取: {shouldExtractPreserve}");

                if (shouldExtractPreserve)
                {
                    LogHelper.Debug($"[BuildFileName] 开始提取保留分组数据，原始文件名: '{OriginalFileName}'");
                    PreserveGroupData = ExtractPreserveGroupData(OriginalFileName);
                    LogHelper.Debug($"[BuildFileName] 提取完成，保留分组数据: {(PreserveGroupData?.Count ?? 0)} 个分组");
                }
                else
                {
                    LogHelper.Debug($"[BuildFileName] 跳过保留分组提取 - 原因: OriginalFileName为空或PreserveGroupData已有数据");
                }

                LogHelper.Debug($"[BuildFileName] 保留分组数据: {(PreserveGroupData?.Count ?? 0)} 个分组");
                if (PreserveGroupData != null)
                {
                    foreach (var kvp in PreserveGroupData)
                    {
                        LogHelper.Debug($"[BuildFileName]   保留分组: {kvp.Key} = {kvp.Value}");
                    }
                }
                LogHelper.Debug($"[BuildFileName] 组件可用性 - 正则结果:{(EnabledComponents?.RegexResultEnabled ?? false)}, 订单号:{(EnabledComponents?.OrderNumberEnabled ?? false)}, 材料:{(EnabledComponents?.MaterialEnabled ?? false)}, 数量:{(EnabledComponents?.QuantityEnabled ?? false)}, 工艺:{(EnabledComponents?.ProcessEnabled ?? false)}, 尺寸:{(EnabledComponents?.DimensionsEnabled ?? false)}, 行数:{(EnabledComponents?.LayoutRowsEnabled ?? false)}, 列数:{(EnabledComponents?.LayoutColumnsEnabled ?? false)}, 序号:{(EnabledComponents?.SerialNumberEnabled ?? false)}");
                LogHelper.Debug($"[BuildFileName] 组件值 - 正则结果:{RegexResult}, 订单号:{OrderNumber}, 材料:{Material}, 数量:{Quantity}{Unit}, 工艺:{Process}, 尺寸:{Dimensions}, 行数:{LayoutRows}, 列数:{LayoutColumns}, 序号:{SerialNumber}");

                System.Console.WriteLine($"BuildFileName: 开始构建文件名，分隔符 = '{Separator}'");
                System.Console.WriteLine($"BuildFileName: 组件可用性 - 正则结果:{(EnabledComponents?.RegexResultEnabled ?? false)}, 订单号:{(EnabledComponents?.OrderNumberEnabled ?? false)}, 材料:{(EnabledComponents?.MaterialEnabled ?? false)}, 数量:{(EnabledComponents?.QuantityEnabled ?? false)}, 工艺:{(EnabledComponents?.ProcessEnabled ?? false)}, 尺寸:{(EnabledComponents?.DimensionsEnabled ?? false)}, 序号:{(EnabledComponents?.SerialNumberEnabled ?? false)}");
                System.Console.WriteLine($"BuildFileName: 组件值 - 正则结果:{RegexResult}, 订单号:{OrderNumber}, 材料:{Material}, 数量:{Quantity}{Unit}, 工艺:{Process}, 尺寸:{Dimensions}, 序号:{SerialNumber}");

                // 诊断日志：检查 Prefixes 字典
                LogHelper.Debug($"[BuildFileName] Prefixes字典大小: {(Prefixes?.Count ?? 0)}");
                if (Prefixes != null && Prefixes.Count > 0)
                {
                    LogHelper.Debug($"[BuildFileName] Prefixes内容: {string.Join(", ", Prefixes.Select(kvp => kvp.Key + "=" + kvp.Value))}");
                }

                // ✅ 优先客: 按分组格式构建文件名（需要自动检测是否有多个分组，脚不需要为PreserveGroupData）
                // 为什么？因为分组格式是根据使用者配置的分组结构来构建的，不管是新文档还是返单，都应该遭守
                if (ShouldUseGroupedFormat())
                {
                    LogHelper.Debug($"[BuildFileName] 检测到多个分组，优先使用分组格式构建文件名");
                    System.Console.WriteLine($"BuildFileName: 优先使用分组格式构建文件名");
                    BuildFileNameByGroupFormat(newNameParts);
                }
                // 如果没有有许多分组，但有保留分组数据，使用混合逻辑：保留数据+当前数据
                else if (PreserveGroupData != null && PreserveGroupData.Count > 0)
                {
                    LogHelper.Debug($"[BuildFileName] 检测到保留分组数据，使用混合逻辑构建文件名（保留数据+当前数据）");
                    System.Console.WriteLine($"BuildFileName: 检测到保留分组数据，使用混合逻辑构建文件名（保留数据+当前数据）");

                    // 如果有自定义组件顺序，按顺序处理所有组件
                    if (ComponentOrder != null && ComponentOrder.Count > 0)
                    {
                        LogHelper.Debug($"[BuildFileName] 使用预设组件顺序处理返单文件: [{string.Join(", ", ComponentOrder)}]");

                        foreach (string componentType in ComponentOrder)
                        {
                            string componentValue = null;
                            string prefix = GetPrefixForComponent(componentType);

                            // 根据组件类型获取对应的值
                            switch (componentType)
                            {
                                case "正则结果":
                                    componentValue = GetValueWithPreserveSupport("正则结果", RegexResult);
                                    break;
                                case "订单号":
                                    componentValue = GetValueWithPreserveSupport("订单号", OrderNumber);
                                    break;
                                case "材料":
                                    componentValue = GetValueWithPreserveSupport("材料", Material);
                                    break;
                                case "数量":
                                    componentValue = GetValueWithPreserveSupport("数量", Quantity);
                                    // 只有当保留值不包含单位且当前有单位时才添加单位
                                    if (!string.IsNullOrEmpty(componentValue) && !string.IsNullOrEmpty(Unit))
                                    {
                                        // 检查保留值是否已经以单位结尾
                                        if (!componentValue.EndsWith(Unit))
                                        {
                                            componentValue += Unit;
                                        }
                                    }
                                    break;
                                case "工艺":
                                    componentValue = GetValueWithPreserveSupport("工艺", Process);
                                    break;
                                case "尺寸":
                                    componentValue = GetValueWithPreserveSupport("尺寸", Dimensions);
                                    break;
                                case "序号":
                                    componentValue = GetValueWithPreserveSupport("序号", SerialNumber);
                                    break;
                                case "行数":
                                    componentValue = GetValueWithPreserveSupport("行数", LayoutRows);
                                    break;
                                case "列数":
                                    componentValue = GetValueWithPreserveSupport("列数", LayoutColumns);
                                    break;
                                case "列组合":
                                    componentValue = GetValueWithPreserveSupport("列组合", CompositeColumn);
                                    break;
                            }

                            if (!string.IsNullOrEmpty(componentValue))
                            {
                                newNameParts.Add(prefix + componentValue);
                                LogHelper.Debug($"[BuildFileName] 混合逻辑添加 {componentType} = '{prefix}{componentValue}'");
                            }
                        }
                    }
                    else
                    {
                        // 如果没有自定义顺序，按标准顺序处理所有组件
                        LogHelper.Debug($"[BuildFileName] 使用标准顺序处理返单文件");
                        var standardOrder = new[] { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "行数", "列数", "序号" };

                        foreach (string componentType in standardOrder)
                        {
                            string componentValue = GetValueWithPreserveSupport(componentType, null);
                            if (!string.IsNullOrEmpty(componentValue))
                            {
                                string prefix = GetPrefixForComponent(componentType);
                                newNameParts.Add(prefix + componentValue);
                                LogHelper.Debug($"[BuildFileName] 混合逻辑添加 {componentType} = '{prefix}{componentValue}'");
                            }
                        }
                    }
                }
                // 如果提供了组件顺序列表，则按顺序添加组件
                else if (ComponentOrder != null && ComponentOrder.Count > 0)
                {
                    LogHelper.Debug($"[BuildFileName] 使用自定义组件顺序: [{string.Join(", ", ComponentOrder)}]");
                    System.Console.WriteLine($"BuildFileName: 使用自定义组件顺序 [{string.Join(", ", ComponentOrder)}]");

                    foreach (string componentType in ComponentOrder)
                    {
                        switch (componentType)
                        {
                            case "正则结果":
                                if (EnabledComponents?.RegexResultEnabled == true && !string.IsNullOrEmpty(RegexResult))
                                {
                                    string prefix = GetPrefixForComponent("正则结果");
                                    string value = GetValueWithPreserveSupport("正则结果", RegexResult);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加正则结果 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加正则结果 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过正则结果 - 启用:{EnabledComponents?.RegexResultEnabled}, 值:'{RegexResult}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过正则结果 - 启用:{EnabledComponents?.RegexResultEnabled}, 值:'{RegexResult}'");
                                }
                                break;
                            case "订单号":
                                if (EnabledComponents?.OrderNumberEnabled == true && !string.IsNullOrEmpty(OrderNumber))
                                {
                                    string prefix = GetPrefixForComponent("订单号");
                                    string value = GetValueWithPreserveSupport("订单号", OrderNumber);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加订单号 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加订单号 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过订单号 - 启用:{EnabledComponents?.OrderNumberEnabled}, 值:'{OrderNumber}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过订单号 - 启用:{EnabledComponents?.OrderNumberEnabled}, 值:'{OrderNumber}'");
                                }
                                break;
                            case "材料":
                                if (EnabledComponents?.MaterialEnabled == true && !string.IsNullOrEmpty(Material))
                                {
                                    string prefix = GetPrefixForComponent("材料");
                                    string value = GetValueWithPreserveSupport("材料", Material);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加材料 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加材料 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过材料 - 启用:{EnabledComponents?.MaterialEnabled}, 值:'{Material}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过材料 - 启用:{EnabledComponents?.MaterialEnabled}, 值:'{Material}'");
                                }
                                break;
                            case "数量":
                                if (EnabledComponents?.QuantityEnabled == true && !string.IsNullOrEmpty(Quantity))
                                {
                                    string prefix = GetPrefixForComponent("数量");
                                    string quantityValue = GetValueWithPreserveSupport("数量", Quantity);
                                    string quantityWithUnit = quantityValue;
                                    if (!string.IsNullOrEmpty(Unit))
                                        quantityWithUnit += Unit;
                                    newNameParts.Add(prefix + quantityWithUnit);
                                    LogHelper.Debug($"[BuildFileName] 添加数量 = '{prefix}{quantityWithUnit}'");
                                    System.Console.WriteLine($"BuildFileName: 添加数量 = '{prefix}{quantityWithUnit}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过数量 - 启用:{EnabledComponents?.QuantityEnabled}, 值:'{Quantity}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过数量 - 启用:{EnabledComponents?.QuantityEnabled}, 值:'{Quantity}'");
                                }
                                break;
                            case "尺寸":
                                if (EnabledComponents?.DimensionsEnabled == true && !string.IsNullOrEmpty(Dimensions))
                                {
                                    string prefix = GetPrefixForComponent("尺寸");
                                    string value = GetValueWithPreserveSupport("尺寸", Dimensions);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加尺寸 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加尺寸 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过尺寸 - 启用:{EnabledComponents?.DimensionsEnabled}, 值:'{Dimensions}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过尺寸 - 启用:{EnabledComponents?.DimensionsEnabled}, 值:'{Dimensions}'");
                                }
                                break;
                            case "工艺":
                                if (EnabledComponents?.ProcessEnabled == true && !string.IsNullOrEmpty(Process))
                                {
                                    string prefix = GetPrefixForComponent("工艺");
                                    string value = GetValueWithPreserveSupport("工艺", Process);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加工艺 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加工艺 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过工艺 - 启用:{EnabledComponents?.ProcessEnabled}, 值:'{Process}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过工艺 - 启用:{EnabledComponents?.ProcessEnabled}, 值:'{Process}'");
                                }
                                break;
                            case "序号":
                                if (EnabledComponents?.SerialNumberEnabled == true && !string.IsNullOrEmpty(SerialNumber))
                                {
                                    string prefix = GetPrefixForComponent("序号");
                                    string value = GetValueWithPreserveSupport("序号", SerialNumber);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加序号 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加序号 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过序号 - 启用:{EnabledComponents?.SerialNumberEnabled}, 值:'{SerialNumber}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过序号 - 启用:{EnabledComponents?.SerialNumberEnabled}, 值:'{SerialNumber}'");
                                }
                                break;
                            case "列组合":
                                if (EnabledComponents?.CompositeColumnEnabled == true && !string.IsNullOrEmpty(CompositeColumn))
                                {
                                    string prefix = GetPrefixForComponent("列组合");
                                    string value = GetValueWithPreserveSupport("列组合", CompositeColumn);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加列组合 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加列组合 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过列组合 - 启用:{EnabledComponents?.CompositeColumnEnabled}, 值:'{CompositeColumn}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过列组合 - 启用:{EnabledComponents?.CompositeColumnEnabled}, 值:'{CompositeColumn}'");
                                }
                                break;
                            case "行数":
                                if (EnabledComponents?.LayoutRowsEnabled == true && !string.IsNullOrEmpty(LayoutRows))
                                {
                                    string prefix = GetPrefixForComponent("行数");
                                    string value = GetValueWithPreserveSupport("行数", LayoutRows);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加行数 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加行数 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过行数 - 启用:{EnabledComponents?.LayoutRowsEnabled}, 值:'{LayoutRows}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过行数 - 启用:{EnabledComponents?.LayoutRowsEnabled}, 值:'{LayoutRows}'");
                                }
                                break;
                            case "列数":
                                if (EnabledComponents?.LayoutColumnsEnabled == true && !string.IsNullOrEmpty(LayoutColumns))
                                {
                                    string prefix = GetPrefixForComponent("列数");
                                    string value = GetValueWithPreserveSupport("列数", LayoutColumns);
                                    newNameParts.Add(prefix + value);
                                    LogHelper.Debug($"[BuildFileName] 添加列数 = '{prefix}{value}'");
                                    System.Console.WriteLine($"BuildFileName: 添加列数 = '{prefix}{value}'");
                                }
                                else
                                {
                                    LogHelper.Debug($"[BuildFileName] 跳过列数 - 启用:{EnabledComponents?.LayoutColumnsEnabled}, 值:'{LayoutColumns}'");
                                    System.Console.WriteLine($"BuildFileName: 跳过列数 - 启用:{EnabledComponents?.LayoutColumnsEnabled}, 值:'{LayoutColumns}'");
                                }
                                break;
                            default:
                                LogHelper.Debug($"[BuildFileName] 未知的组件类型 '{componentType}'");
                                System.Console.WriteLine($"BuildFileName: 未知的组件类型 '{componentType}'");
                                break;
                        }
                    }
                }
                else
                {
                    LogHelper.Debug("[BuildFileName] 使用默认组件顺序");
                    System.Console.WriteLine("BuildFileName: 使用默认组件顺序");

                    // 如果有保留分组数据，优先使用保留分组逻辑（即使是默认顺序）
                    if (PreserveGroupData != null && PreserveGroupData.Count > 0)
                    {
                        LogHelper.Debug($"[BuildFileName] 默认顺序：检测到保留分组数据，使用保留分组逻辑构建文件名");
                        System.Console.WriteLine($"BuildFileName: 默认顺序：检测到保留分组数据，使用保留分组逻辑构建文件名");

                        // 按保留分组前缀的标准顺序构建文件名
                        var preserveOrder = new[] { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "行数", "列数", "序号" };

                        foreach (string componentType in preserveOrder)
                        {
                            string componentValue = GetValueWithPreserveSupport(componentType, null);
                            if (!string.IsNullOrEmpty(componentValue))
                            {
                                string prefix = GetPrefixForComponent(componentType);
                                newNameParts.Add(prefix + componentValue);
                                LogHelper.Debug($"[BuildFileName] 默认保留分组添加 {componentType} = '{prefix}{componentValue}'");
                                System.Console.WriteLine($"BuildFileName: 默认保留分组添加 {componentType} = '{prefix}{componentValue}'");
                            }
                        }
                    }
                    else
                    {
                        // 如果没有提供组件顺序列表，也没有保留数据，则使用默认顺序
                        if (EnabledComponents?.RegexResultEnabled == true && !string.IsNullOrEmpty(RegexResult))
                        {
                            string prefix = GetPrefixForComponent("正则结果");
                            string value = GetValueWithPreserveSupport("正则结果", RegexResult);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加正则结果 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加正则结果 = '{prefix}{value}'");
                        }

                        if (EnabledComponents?.OrderNumberEnabled == true && !string.IsNullOrEmpty(OrderNumber))
                        {
                            string prefix = GetPrefixForComponent("订单号");
                            string value = GetValueWithPreserveSupport("订单号", OrderNumber);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加订单号 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加订单号 = '{prefix}{value}'");
                        }

                        if (EnabledComponents?.MaterialEnabled == true && !string.IsNullOrEmpty(Material))
                        {
                            string prefix = GetPrefixForComponent("材料");
                            string value = GetValueWithPreserveSupport("材料", Material);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加材料 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加材料 = '{prefix}{value}'");
                        }

                        if (EnabledComponents?.QuantityEnabled == true && !string.IsNullOrEmpty(Quantity))
                        {
                            string prefix = GetPrefixForComponent("数量");
                            string quantityValue = GetValueWithPreserveSupport("数量", Quantity);
                            string quantityWithUnit = quantityValue;
                            if (!string.IsNullOrEmpty(Unit))
                                quantityWithUnit += Unit;
                            newNameParts.Add(prefix + quantityWithUnit);
                            LogHelper.Debug($"[BuildFileName] 添加数量 = '{prefix}{quantityWithUnit}'");
                            System.Console.WriteLine($"BuildFileName: 添加数量 = '{prefix}{quantityWithUnit}'");
                        }

                        if (EnabledComponents?.DimensionsEnabled == true && !string.IsNullOrEmpty(Dimensions))
                        {
                            string prefix = GetPrefixForComponent("尺寸");
                            string value = GetValueWithPreserveSupport("尺寸", Dimensions);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加尺寸 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加尺寸 = '{prefix}{value}'");
                        }

                        if (EnabledComponents?.ProcessEnabled == true && !string.IsNullOrEmpty(Process))
                        {
                            string prefix = GetPrefixForComponent("工艺");
                            string value = GetValueWithPreserveSupport("工艺", Process);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加工艺 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加工艺 = '{prefix}{value}'");
                        }

                        if (EnabledComponents?.SerialNumberEnabled == true && !string.IsNullOrEmpty(SerialNumber))
                        {
                            string prefix = GetPrefixForComponent("序号");
                            string value = GetValueWithPreserveSupport("序号", SerialNumber);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加序号 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加序号 = '{prefix}{value}'");
                        }

                        // 添加组合列值
                        if (EnabledComponents?.CompositeColumnEnabled == true && !string.IsNullOrEmpty(CompositeColumn))
                        {
                            string prefix = GetPrefixForComponent("列组合");
                            string value = GetValueWithPreserveSupport("列组合", CompositeColumn);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加列组合 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加列组合 = '{prefix}{value}'");
                        }

                        // 添加布局行数和列数
                        if (EnabledComponents?.LayoutRowsEnabled == true && !string.IsNullOrEmpty(LayoutRows))
                        {
                            string prefix = GetPrefixForComponent("行数");
                            string value = GetValueWithPreserveSupport("行数", LayoutRows);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加行数 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加行数 = '{prefix}{value}'");
                        }

                        if (EnabledComponents?.LayoutColumnsEnabled == true && !string.IsNullOrEmpty(LayoutColumns))
                        {
                            string prefix = GetPrefixForComponent("列数");
                            string value = GetValueWithPreserveSupport("列数", LayoutColumns);
                            newNameParts.Add(prefix + value);
                            LogHelper.Debug($"[BuildFileName] 添加列数 = '{prefix}{value}'");
                            System.Console.WriteLine($"BuildFileName: 添加列数 = '{prefix}{value}'");
                        }
                    }
                }

                LogHelper.Debug($"[BuildFileName] 共添加了 {newNameParts.Count} 个组件部分: [{string.Join(", ", newNameParts)}]");
                System.Console.WriteLine($"BuildFileName: 共添加了 {newNameParts.Count} 个组件部分: [{string.Join(", ", newNameParts)}]");

                // ✅ 修改：按分组格式构建时，分组间不使用分隔符，直接相接
                // 因为newNameParts中的每一项都已经是完整的分组（前缀+值），不需要分隔符连接
                string newFileName;
                // 检查是否应该使用分组格式或混合逻辑（都产生了前缀+值的形式）
                // 混合逻辑场景：有保留分组数据
                bool isMixedLogic = (PreserveGroupData != null && PreserveGroupData.Count > 0);
                if (ShouldUseGroupedFormat() || isMixedLogic)
                {
                    // ... existing code ...
                    // 按分组格式或混合逻辑构建：分组间直接相接，不使用分隔符
                    newFileName = string.Concat(newNameParts) + FileExtension;
                    if (isMixedLogic && !ShouldUseGroupedFormat())
                    {
                        LogHelper.Debug("[BuildFileName] 按混合逻辑构建，分组间不使用分隔符，直接相接");
                        System.Console.WriteLine("BuildFileName: 按混合逻辑构建，分组间不使用分隔符，直接相接");
                    }
                    else
                    {
                        LogHelper.Debug("[BuildFileName] 按分组格式构建，分组间不使用分隔符，直接相接");
                        System.Console.WriteLine("BuildFileName: 按分组格式构建，分组间不使用分隔符，直接相接");
                    }
                }
                else
                {
                    // 非分组格式：使用分隔符连接各部分
                    // ✅ 修复：清空 newNameParts，重新构建只包含"值"的列表（不包含前缀）
                    newNameParts.Clear();
                    LogHelper.Debug("[BuildFileName] 非分组格式检测，清空前缀+值的列表，重新构建纯值列表");
                    System.Console.WriteLine("BuildFileName: 非分组格式检测，清空列表重新构建");
                    
                    // 按原来的组件顺序（不带前缀）重新添加到 newNameParts
                    if (ComponentOrder != null && ComponentOrder.Count > 0)
                    {
                        // 使用自定义组件顺序
                        foreach (string componentType in ComponentOrder)
                        {
                            string value = null;
                            switch (componentType)
                            {
                                case "正则结果":
                                    if (EnabledComponents?.RegexResultEnabled == true && !string.IsNullOrEmpty(RegexResult))
                                        value = GetValueWithPreserveSupport("正则结果", RegexResult);
                                    break;
                                case "订单号":
                                    if (EnabledComponents?.OrderNumberEnabled == true && !string.IsNullOrEmpty(OrderNumber))
                                        value = GetValueWithPreserveSupport("订单号", OrderNumber);
                                    break;
                                case "材料":
                                    if (EnabledComponents?.MaterialEnabled == true && !string.IsNullOrEmpty(Material))
                                        value = GetValueWithPreserveSupport("材料", Material);
                                    break;
                                case "数量":
                                    if (EnabledComponents?.QuantityEnabled == true && !string.IsNullOrEmpty(Quantity))
                                        value = GetValueWithPreserveSupport("数量", Quantity) + (string.IsNullOrEmpty(Unit) ? "" : Unit);
                                    break;
                                case "工艺":
                                    if (EnabledComponents?.ProcessEnabled == true && !string.IsNullOrEmpty(Process))
                                        value = GetValueWithPreserveSupport("工艺", Process);
                                    break;
                                case "尺寸":
                                    if (EnabledComponents?.DimensionsEnabled == true && !string.IsNullOrEmpty(Dimensions))
                                        value = GetValueWithPreserveSupport("尺寸", Dimensions);
                                    break;
                                case "序号":
                                    if (EnabledComponents?.SerialNumberEnabled == true && !string.IsNullOrEmpty(SerialNumber))
                                        value = GetValueWithPreserveSupport("序号", SerialNumber);
                                    break;
                                case "列组合":
                                    if (EnabledComponents?.CompositeColumnEnabled == true && !string.IsNullOrEmpty(CompositeColumn))
                                        value = GetValueWithPreserveSupport("列组合", CompositeColumn);
                                    break;
                                case "行数":
                                    if (EnabledComponents?.LayoutRowsEnabled == true && !string.IsNullOrEmpty(LayoutRows))
                                        value = GetValueWithPreserveSupport("行数", LayoutRows);
                                    break;
                                case "列数":
                                    if (EnabledComponents?.LayoutColumnsEnabled == true && !string.IsNullOrEmpty(LayoutColumns))
                                        value = GetValueWithPreserveSupport("列数", LayoutColumns);
                                    break;
                            }
                            if (!string.IsNullOrEmpty(value))
                            {
                                newNameParts.Add(value);
                                LogHelper.Debug($"[BuildFileName] 非分组格式添加 {componentType} = '{value}'");
                            }
                        }
                    }
                    else
                    {
                        // 使用默认顺序
                        var defaultOrder = new[] { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "序号", "列组合", "行数", "列数" };
                        foreach (string componentType in defaultOrder)
                        {
                            string value = GetComponentValue(componentType);
                            if (!string.IsNullOrEmpty(value))
                            {
                                newNameParts.Add(value);
                                LogHelper.Debug($"[BuildFileName] 非分组格式默认顺序添加 {componentType} = '{value}'");
                            }
                        }
                    }
                    
                    // 只有当分隔符不为空且不是合法文件名字符时才替换
                    if (!string.IsNullOrEmpty(Separator) && Path.GetInvalidFileNameChars().Contains(Separator[0]))
                    {
                        LogHelper.Debug($"[BuildFileName] 分隔符 '{Separator}' 包含非法字符，自动替换为 '_'");
                        System.Console.WriteLine($"BuildFileName: 分隔符 '{Separator}' 包含非法字符，自动替换为 '_'");
                        Separator = "_";
                    }

                    if (string.IsNullOrEmpty(Separator))
                    {
                        // 空分隔符：直接连接所有部分
                        newFileName = string.Concat(newNameParts) + FileExtension;
                        LogHelper.Debug("[BuildFileName] 使用空分隔符连接文件名");
                        System.Console.WriteLine("BuildFileName: 使用空分隔符连接文件名");
                    }
                    else
                    {
                        // 非空分隔符：使用指定分隔符连接
                        newFileName = string.Join(Separator, newNameParts) + FileExtension;
                        LogHelper.Debug($"[BuildFileName] 使用分隔符 '{Separator}' 连接文件名");
                        System.Console.WriteLine($"BuildFileName: 使用分隔符 '{Separator}' 连接文件名");
                    }
                }

                // 验证生成的文件名是否有效
                if (string.IsNullOrEmpty(newFileName) || newFileName == FileExtension)
                {
                    LogHelper.Debug("[BuildFileName] 生成的文件名无效，返回默认名称");
                    System.Console.WriteLine("BuildFileName: 生成的文件名无效，返回默认名称");
                    return $"未命名{FileExtension}";
                }

                LogHelper.Debug($"[BuildFileName] 最终生成的文件名: '{newFileName}'");
                System.Console.WriteLine($"BuildFileName: 最终生成的文件名: '{newFileName}'");
                return newFileName;
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[BuildFileName] 构建文件名时发生异常: {ex.Message}");
                LogHelper.Error($"[BuildFileName] 异常详情: {ex.StackTrace}");
                System.Console.WriteLine($"BuildFileName: 构建文件名时发生异常: {ex.Message}");
                System.Console.WriteLine($"BuildFileName: 异常详情: {ex.StackTrace}");
                return $"未命名{FileExtension}";
            }
        }

        /// <summary>
        /// 获取指定组件的前缀
        /// </summary>
        /// <param name="componentType">组件类型</param>
        /// <returns>前缀字符串，如果没有前缀则返回空字符串</returns>
        private string GetPrefixForComponent(string componentType)
        {
            if (Prefixes == null || Prefixes.Count == 0)
                return string.Empty;

            // 先尝试直接匹配组件类型
            if (Prefixes.ContainsKey(componentType))
                return Prefixes[componentType];

            // 如果直接匹配失败，尝试匹配带保留前缀的键
            string prefixedKey = $"[*] {componentType}";
            if (Prefixes.ContainsKey(prefixedKey))
                return Prefixes[prefixedKey];

            // 也尝试匹配[保留]前缀
            string preservedKey = $"[保留] {componentType}";
            if (Prefixes.ContainsKey(preservedKey))
                return Prefixes[preservedKey];

            LogHelper.Debug($"GetPrefixForComponent: 未找到组件 '{componentType}' 的前缀，尝试的键: '{componentType}', '{prefixedKey}', '{preservedKey}'");
            return string.Empty;
        }

        /// <summary>
        /// 按分组格式构建文件名（一个分组一个前缀，分组内多个项目用分隔符连接）
        /// </summary>
        private void BuildFileNameByGroupFormat(List<string> newNameParts)
        {
            try
            {
                LogHelper.Debug("[BuildFileNameByGroupFormat] 开始按分组格式构建文件名");
                LogHelper.Debug($"[BuildFileNameByGroupFormat] 当前Prefixes字典大小: {(Prefixes?.Count ?? 0)}");
                
                // 从实际的Prefixes字典构建组件到分组的映射（动态适配当前配置）
                var componentToGroupPrefix = BuildComponentToGroupPrefixMapping();

                // 按分组聚合组件
                var groupedComponents = new Dictionary<string, List<string>>();
                var groupedPrefixes = new List<string>(); // 按出现顺序记录前缀
                var ungroupedComponents = new List<string>(); // 未分组的组件

                // 确定要处理的组件顺序
                var componentsToProcess = ComponentOrder != null && ComponentOrder.Count > 0 
                    ? (IEnumerable<string>)ComponentOrder 
                    : (IEnumerable<string>)new[] { "正则结果", "订单号", "材料", "数量", "工艺", "尺寸", "行数", "列数", "序号", "列组合" };

                // 按分组聚合组件
                foreach (string componentType in componentsToProcess)
                {
                    string componentValue = GetComponentValue(componentType);
                    if (string.IsNullOrEmpty(componentValue))
                        continue;

                    if (componentToGroupPrefix.TryGetValue(componentType, out var groupPrefix))
                    {
                        if (string.IsNullOrEmpty(groupPrefix))
                        {
                            // 未分组的组件
                            string prefix = GetPrefixForComponent(componentType);
                            ungroupedComponents.Add(prefix + componentValue);
                            LogHelper.Debug($"[BuildFileNameByGroupFormat] 添加未分组组件: {componentType} = '{componentValue}' (前缀: {prefix})");
                        }
                        else
                        {
                            // 分组内的组件
                            if (!groupedComponents.ContainsKey(groupPrefix))
                            {
                                groupedComponents[groupPrefix] = new List<string>();
                                groupedPrefixes.Add(groupPrefix); // 记录前缀出现顺序
                            }
                            groupedComponents[groupPrefix].Add(componentValue);
                            LogHelper.Debug($"[BuildFileNameByGroupFormat] 添加到分组 {groupPrefix}: {componentType} = '{componentValue}'");
                        }
                    }
                    else
                    {
                        // 未知的组件类型，作为未分组处理
                        string prefix = GetPrefixForComponent(componentType);
                        ungroupedComponents.Add(prefix + componentValue);
                        LogHelper.Debug($"[BuildFileNameByGroupFormat] 添加未知组件: {componentType} = '{componentValue}' (前缀: {prefix})");
                    }
                }

                // 构建分组部分（按出现顺序，每个分组一个前缀，分组内多个项目用分隔符连接）
                // ✅ 修改：只在分组内有多个项目时才使用分隔符，分组间不使用分隔符
                foreach (var prefix in groupedPrefixes)
                {
                    if (groupedComponents.TryGetValue(prefix, out var items) && items.Count > 0)
                    {
                        // ✅ 只在分组内多个项目时使用分隔符
                        string groupValue = items.Count > 1 
                            ? string.Join(Separator ?? "", items)
                            : items[0]; // 单个项目不使用分隔符
                        newNameParts.Add(prefix + groupValue);
                        LogHelper.Debug($"[BuildFileNameByGroupFormat] 分组 {prefix}: 项目数={items.Count}, 分隔符使用={items.Count > 1}, 结果='{prefix}{groupValue}'");
                    }
                }

                // 添加未分组的组件（多个项目之间使用分隔符）
                if (ungroupedComponents.Count > 0)
                {
                    if (ungroupedComponents.Count == 1)
                    {
                        // 单个未分组项目，直接添加
                        newNameParts.Add(ungroupedComponents[0]);
                        LogHelper.Debug($"[BuildFileNameByGroupFormat] 添加单个未分组项: '{ungroupedComponents[0]}'");
                    }
                    else
                    {
                        // 多个未分组项目，使用分隔符连接
                        string ungroupedPart = string.Join(Separator ?? "", ungroupedComponents);
                        newNameParts.Add(ungroupedPart);
                        LogHelper.Debug($"[BuildFileNameByGroupFormat] 添加多个未分组项（使用分隔符 '{Separator}'）: '{ungroupedPart}'");
                    }
                }

                LogHelper.Debug($"[BuildFileNameByGroupFormat] 完成，共生成 {newNameParts.Count} 个部分");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[BuildFileNameByGroupFormat] 按分组格式构建文件名时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 从Prefixes字典构建组件到分组前缀的映射（动态适配当前配置）
        /// </summary>
        private Dictionary<string, string> BuildComponentToGroupPrefixMapping()
        {
            var mapping = new Dictionary<string, string>();
            
            if (Prefixes == null || Prefixes.Count == 0)
            {
                LogHelper.Debug("[BuildComponentToGroupPrefixMapping] Prefixes为空，返回空映射");
                return mapping;
            }

            // 直接从Prefixes字典构建映射，不使用GetPrefixForComponent来避免循环依赖
            // Prefixes的键格式为: "[*] 组件名" 或 "组件名"
            foreach (var kvp in Prefixes)
            {
                string componentKey = kvp.Key;   // e.g., "[*] 正则结果" 或 "数量"
                string prefix = kvp.Value;       // e.g., "&ID-"

                // 提取纯组件名（移除[*]标记）
                string componentName = componentKey.StartsWith("[*] ") 
                    ? componentKey.Substring(4).Trim() 
                    : componentKey.Trim();
                
                if (!string.IsNullOrEmpty(componentName))
                {
                    // 注意：这里直接用Prefixes中的前缀，即使为空也要记录
                    mapping[componentName] = prefix ?? "";
                    LogHelper.Debug($"[BuildComponentToGroupPrefixMapping] {componentName} -> '{prefix}'");
                }
            }

            LogHelper.Debug($"[BuildComponentToGroupPrefixMapping] 完成映射，共 {mapping.Count} 个组件: {string.Join(", ", mapping.Select(m => $"{m.Key}={m.Value}"))}");
            return mapping;
        }

        /// <summary>
        /// 获取指定组件的值（包括保留值处理）
        /// </summary>
        private string GetComponentValue(string componentType)
        {
            string value = null;
            switch (componentType)
            {
                case "正则结果":
                    if (EnabledComponents?.RegexResultEnabled == true)
                        value = GetValueWithPreserveSupport("正则结果", RegexResult);
                    break;
                case "订单号":
                    if (EnabledComponents?.OrderNumberEnabled == true)
                        value = GetValueWithPreserveSupport("订单号", OrderNumber);
                    break;
                case "材料":
                    if (EnabledComponents?.MaterialEnabled == true)
                        value = GetValueWithPreserveSupport("材料", Material);
                    break;
                case "数量":
                    if (EnabledComponents?.QuantityEnabled == true)
                    {
                        value = GetValueWithPreserveSupport("数量", Quantity);
                        if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(Unit))
                        {
                            if (!value.EndsWith(Unit))
                                value += Unit;
                        }
                    }
                    break;
                case "工艺":
                    if (EnabledComponents?.ProcessEnabled == true)
                        value = GetValueWithPreserveSupport("工艺", Process);
                    break;
                case "尺寸":
                    if (EnabledComponents?.DimensionsEnabled == true)
                        value = GetValueWithPreserveSupport("尺寸", Dimensions);
                    break;
                case "序号":
                    if (EnabledComponents?.SerialNumberEnabled == true)
                        value = GetValueWithPreserveSupport("序号", SerialNumber);
                    break;
                case "行数":
                    if (EnabledComponents?.LayoutRowsEnabled == true)
                        value = GetValueWithPreserveSupport("行数", LayoutRows);
                    break;
                case "列数":
                    if (EnabledComponents?.LayoutColumnsEnabled == true)
                        value = GetValueWithPreserveSupport("列数", LayoutColumns);
                    break;
                case "列组合":
                    if (EnabledComponents?.CompositeColumnEnabled == true)
                        value = GetValueWithPreserveSupport("列组合", CompositeColumn);
                    break;
            }
            return value;
        }

        /// <summary>
        /// 判断是否应该使用分组格式构建文件名
        /// </summary>
        private bool ShouldUseGroupedFormat()
        {
            // 方案 1: 检查 Prefixes 字典中是否有多个不同的前缀
            if (Prefixes != null && Prefixes.Count > 0)
            {
                var groupPrefixes = new HashSet<string>();
                foreach (var kvp in Prefixes)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        groupPrefixes.Add(kvp.Value);
                    }
                }

                if (groupPrefixes.Count >= 2)
                {
                    LogHelper.Debug($"[ShouldUseGroupedFormat] 检查 Prefixes: 分组数={groupPrefixes.Count} -> true");
                    return true;
                }
            }

            // 方案 2: 备用方案 - 从 PreserveGroupConfig 推断（应对 Prefixes 为空的情况）
            if (PreserveGroupConfig != null && PreserveGroupConfig.Count >= 2)
            {
                LogHelper.Debug($"[ShouldUseGroupedFormat] 检查 PreserveGroupConfig: 配置数={PreserveGroupConfig.Count} >= 2 -> true");
                return true;
            }

            LogHelper.Debug($"[ShouldUseGroupedFormat] 不满足分组格式条件 -> false");
            return false;
        }

        /// <summary>
        /// 验证组件配置
        /// </summary>
        /// <returns>验证结果</returns>
        public ValidationResult Validate()
        {
            // 检查必要字段
            if (string.IsNullOrEmpty(FileExtension))
            {
                return ValidationResult.Failure("文件扩展名不能为空", ValidationErrorType.InvalidParameters, "FileNameComponents.Validate");
            }

            // 检查扩展名是否包含点号
            if (!FileExtension.StartsWith("."))
            {
                return ValidationResult.Failure("文件扩展名必须以点号开头", ValidationErrorType.InvalidParameters, "FileNameComponents.Validate");
            }

            // 检查扩展名是否包含非法字符
            foreach (char c in FileExtension)
            {
                if (Path.GetInvalidFileNameChars().Contains(c))
                {
                    return ValidationResult.Failure($"文件扩展名包含非法字符: {c}", ValidationErrorType.InvalidParameters, "FileNameComponents.Validate");
                }
            }

            // 检查分隔符是否包含非法字符
            if (!string.IsNullOrEmpty(Separator))
            {
                foreach (char c in Separator)
                {
                    if (Path.GetInvalidFileNameChars().Contains(c))
                    {
                        return ValidationResult.Failure($"分隔符包含非法字符: {c}", ValidationErrorType.InvalidParameters, "FileNameComponents.Validate");
                    }
                }
            }

            return ValidationResult.Success("FileNameComponents验证通过");
        }

        /// <summary>
        /// 从原始文件名中提取保留分组数据（只提取配置为保留的分组）
        /// </summary>
        /// <param name="originalFileName">原始文件名（不含扩展名）</param>
        /// <returns>保留分组数据字典</returns>
        public Dictionary<string, string> ExtractPreserveGroupData(string originalFileName)
        {
            var preserveData = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(originalFileName))
                return preserveData;

            try
            {
                LogHelper.Debug($"[ExtractPreserveGroupData] 开始提取保留分组数据: '{originalFileName}'");
                LogHelper.Debug($"[ExtractPreserveGroupData] 保留分组配置: {string.Join(", ", PreserveGroupConfig.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

                // 从 Prefixes 字典动态构建前缀到组件的映射（反向映射）
                // 注意：可能一个前缀对应多个组件（如&MK-对应订单号和序号），这种情况需要特殊处理
                var prefixToComponentMapping = new Dictionary<string, string>();
                var prefixToComponentsMultiMapping = new Dictionary<string, List<string>>(); // 记录一个前缀对应的所有组件
                
                if (Prefixes != null && Prefixes.Count > 0)
                {
                    LogHelper.Debug($"[ExtractPreserveGroupData] Prefixes内容（调试）: {string.Join(", ", Prefixes.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
                    
                    foreach (var kvp in Prefixes)
                    {
                        string componentKey = kvp.Key;   // e.g., "[*] 正则结果" 或 "数量"
                        string prefix = kvp.Value;       // e.g., "&ID-"

                        if (!string.IsNullOrEmpty(prefix))
                        {
                            // 提取纯组件名（移除[*]标记）
                            string componentName = componentKey.StartsWith("[*] ")
                                ? componentKey.Substring(4).Trim()
                                : componentKey.Trim();

                            if (!string.IsNullOrEmpty(componentName))
                            {
                                // 记录到多重映射（一个前缀可能对应多个组件）
                                if (!prefixToComponentsMultiMapping.ContainsKey(prefix))
                                {
                                    prefixToComponentsMultiMapping[prefix] = new List<string>();
                                }
                                prefixToComponentsMultiMapping[prefix].Add(componentName);
                                LogHelper.Debug($"[ExtractPreserveGroupData] 多重映射: {prefix} -> {componentName}");

                                // 单一映射（保留第一个）
                                if (!prefixToComponentMapping.ContainsKey(prefix))
                                {
                                    prefixToComponentMapping[prefix] = componentName;
                                    LogHelper.Debug($"[ExtractPreserveGroupData] 单一映射（优先）: {prefix} -> {componentName}");
                                }
                            }
                        }
                    }
                }

                LogHelper.Debug($"[ExtractPreserveGroupData] 最终单一映射: {string.Join(", ", prefixToComponentMapping.Select(kvp => $"{kvp.Key}->{kvp.Value}"))}");
                LogHelper.Debug($"[ExtractPreserveGroupData] 最终多重映射: {string.Join(", ", prefixToComponentsMultiMapping.Select(kvp => $"{kvp.Key}->[{string.Join(",", kvp.Value)}]"))}");
                
                // 建立前缀到分组ID的映射（用于查找PreserveGroupConfig）
                // 这个映射对应于EventGroup中的分组ID
                var prefixToGroupIdMapping = new Dictionary<string, string>
                {
                    { "&ID-", "order" },      // 订单组
                    { "&MT-", "material" },  // 材料组
                    { "&DN-", "quantity" },  // 数量组
                    { "&DP-", "process" },   // 工艺组
                    { "&CU-", "customer" },  // 客户组（对应尺寸）
                    { "&MK-", "remark" },    // 备注组（对应订单号和序号）
                    { "&Row-", "row" },      // 行数组
                    { "&Col-", "column" }    // 列数组
                };
                LogHelper.Debug($"[ExtractPreserveGroupData] 前缀到分组ID映射: {string.Join(", ", prefixToGroupIdMapping.Select(kvp => $"{kvp.Key}->{kvp.Value}"))}");

                // 也保留旧的前缀应对，应对一些特殊对应关系
                var additionalPrefixMapping = new Dictionary<string, string>
                {
                    // 可以根据需要添加特殊映射，但最好是所有映射都从 Prefixes 中动态获取
                };

                // 使用正则表达式匹配保留分组模式
                var pattern = @"&([A-Z]+)-([^&]+)";
                var matches = System.Text.RegularExpressions.Regex.Matches(originalFileName, pattern);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var prefix = $"&{match.Groups[1].Value}-";
                    var value = match.Groups[2].Value;

                    // 优先从Prefixes动态映射中查找，然后处理重复前缀
                    string componentType = null;
                    if (prefixToComponentMapping.ContainsKey(prefix))
                    {
                        componentType = prefixToComponentMapping[prefix];
                        LogHelper.Debug($"[ExtractPreserveGroupData] 从动态映射查找: {prefix} -> {componentType}");
                    }
                    else if (additionalPrefixMapping.ContainsKey(prefix))
                    {
                        componentType = additionalPrefixMapping[prefix];
                        LogHelper.Debug($"[ExtractPreserveGroupData] 从预定义映射查找: {prefix} -> {componentType}");
                    }
                    else
                    {
                        // 如果前缀未找到映射，跳过此前缀
                        LogHelper.Debug($"[ExtractPreserveGroupData] 前缀 {prefix} 没有找到映射，跳过");
                        continue;
                    }

                    if (componentType != null)
                    {
                        // 从 PreserveGroupConfig 中查找是否需要保留此前缀的数据
                        bool shouldPreserve = false;
                        
                        // 根据前缀查找分组ID，然后查找PreserveGroupConfig
                        if (prefixToGroupIdMapping.ContainsKey(prefix))
                        {
                            var groupId = prefixToGroupIdMapping[prefix];
                            if (PreserveGroupConfig.ContainsKey(groupId))
                            {
                                shouldPreserve = PreserveGroupConfig[groupId];
                                LogHelper.Debug($"[ExtractPreserveGroupData] ✅ 优先根据分组ID查找: {prefix} -> {groupId} = {shouldPreserve}");
                            }
                            else
                            {
                                LogHelper.Debug($"[ExtractPreserveGroupData] ❌ PreserveGroupConfig中找不到分组ID: {groupId}");
                            }
                        }
                        // 备用方案：直接根据前缀查找（应对PreserveGroupConfig没有分组ID的情况）
                        if (!shouldPreserve && PreserveGroupConfig.ContainsKey(prefix))
                        {
                            shouldPreserve = PreserveGroupConfig[prefix];
                            LogHelper.Debug($"[ExtractPreserveGroupData] ✅ 备用根据前缀查找: {prefix} -> {shouldPreserve}");
                        }
                        // 再备用方案：根据组件类型查找（应对旧的配置格式）
                        if (!shouldPreserve && PreserveGroupConfig.ContainsKey(componentType))
                        {
                            shouldPreserve = PreserveGroupConfig[componentType];
                            LogHelper.Debug($"[ExtractPreserveGroupData] ✅ 根据组件类型查找: {componentType} -> {shouldPreserve}");
                        }

                        LogHelper.Debug($"[ExtractPreserveGroupData] 前缀 {prefix} -> {componentType}, 配置保留: {shouldPreserve}");

                        if (shouldPreserve)
                        {
                            // 为了处理一个前缀对应多个组件的情况。
                            // 当前逐次提取的偏好：什么都保留，但用支持多个组件的偏好（前一个组件优先）
                            // 注意：当一个前缀对应多个组件时，第一次遇到的会被保留，后面的会被添加到已有值中
                            if (!preserveData.ContainsKey(componentType))
                            {
                                preserveData[componentType] = value;
                                LogHelper.Debug($"[ExtractPreserveGroupData] ✅ 提取保留分组: {prefix} -> {componentType} = {value}");
                            }
                            else
                            {
                                // 不覆盖已有值，第一个会被保留
                                LogHelper.Debug($"[ExtractPreserveGroupData] ✅ 前缀 {prefix} 的组件 {componentType} 已经存在，跳过后续值");
                            }
                        }
                        else
                        {
                            LogHelper.Debug($"[ExtractPreserveGroupData] ❌ 跳过非保留分组: {prefix} -> {componentType}");
                        }
                    }
                }

                LogHelper.Debug($"[ExtractPreserveGroupData] 提取完成，共提取 {preserveData.Count} 个保留分组");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"[ExtractPreserveGroupData] 提取保留分组数据时发生异常: {ex.Message}");
            }

            return preserveData;
        }

/// <summary>
/// 从分组项目字符串中提取分组名称和项目名称
/// </summary>
/// <param name="groupItemString">分组项目字符串，如 "&ID-订单组"</param>
/// <returns>包含分组名称和项目名称的元组</returns>
private (string groupName, string itemName) ExtractGroupAndItem(string groupItemString)
{
    if (string.IsNullOrEmpty(groupItemString))
        return (string.Empty, string.Empty);

    try
    {
        // 使用正则表达式匹配分组项目格式：&ID-项目名
        var match = System.Text.RegularExpressions.Regex.Match(groupItemString, @"^&([A-Z]+)-(.+)$");
        if (match.Success)
        {
            var groupPrefix = match.Groups[1].Value;
            var itemName = match.Groups[2].Value;
            
            // 从Prefixes中动态查找前缀到组件的映射
            string groupName = groupPrefix; // 默认使用前缀代码
            
            if (Prefixes != null && Prefixes.Count > 0)
            {
                // 从Prefixes中反向查找前缀对应的组件名
                var prefix = $"&{groupPrefix}-";
                foreach (var kvp in Prefixes)
                {
                    if (kvp.Value == prefix)
                    {
                        // 提取纯组件名
                        groupName = kvp.Key.StartsWith("[*] ")
                            ? kvp.Key.Substring(4).Trim()
                            : kvp.Key.Trim();
                        break;
                    }
                }
            }
            
            // 如果还未找到映射，使用硬编码的备用映射（应对Prefixes为空的情况）
            if (groupName == groupPrefix)
            {
                var groupNames = new Dictionary<string, string>
                {
                    { "ID", "订单号" },
                    { "MT", "材料" },
                    { "DN", "数量" },
                    { "DP", "工艺" },
                    { "CU", "尺寸" },
                    { "MK", "订单号" },
                    { "Row", "行数" },
                    { "Col", "列数" }
                };

                groupName = groupNames.ContainsKey(groupPrefix) ? groupNames[groupPrefix] : groupPrefix;
            }
            
            LogHelper.Debug($"[ExtractGroupAndItem] 解析: {groupItemString} -> 分组: {groupName}, 项目: {itemName}");
            
            return (groupName, itemName);
        }
        else
        {
            // 如果不匹配标准格式，返回原始字符串
            LogHelper.Debug($"[ExtractGroupAndItem] 非标准格式: {groupItemString}");
            return (groupItemString, groupItemString);
        }
    }
    catch (Exception ex)
    {
        LogHelper.Error($"[ExtractGroupAndItem] 解析分组项目字符串时发生异常: {groupItemString}, 错误: {ex.Message}");
        return (string.Empty, string.Empty);
    }
}

        /// <summary>
        /// 获取保留分组的数据，如果没有保留数据则使用当前值
        /// </summary>
        /// <param name="componentType">组件类型</param>
        /// <param name="currentValue">当前值</param>
        /// <returns>保留值或当前值</returns>
        private string GetValueWithPreserveSupport(string componentType, string currentValue)
        {
            if (PreserveGroupData != null && PreserveGroupData.ContainsKey(componentType))
            {
                string preservedValue = PreserveGroupData[componentType];
                LogHelper.Debug($"[GetValueWithPreserveSupport] {componentType}: 使用保留值 '{preservedValue}' 替代当前值 '{currentValue}'");
                return preservedValue;
            }

            LogHelper.Debug($"[GetValueWithPreserveSupport] {componentType}: 使用当前值 '{currentValue}'");
            return currentValue;
        }
    }

    /// <summary>
    /// 文件名组件配置类
    /// </summary>
    public class FileNameComponentsConfig
    {
        /// <summary>
        /// 是否启用正则结果
        /// </summary>
        public bool RegexResultEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用订单号
        /// </summary>
        public bool OrderNumberEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用材料
        /// </summary>
        public bool MaterialEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用数量
        /// </summary>
        public bool QuantityEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用尺寸
        /// </summary>
        public bool DimensionsEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用工艺
        /// </summary>
        public bool ProcessEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用序号
        /// </summary>
        public bool SerialNumberEnabled { get; set; } = true;

        /// <summary>
        /// 是否启用组合列
        /// </summary>
        public bool CompositeColumnEnabled { get; set; } = false;

        /// <summary>
        /// 是否启用行数
        /// </summary>
        public bool LayoutRowsEnabled { get; set; } = false;

        /// <summary>
        /// 是否启用列数
        /// </summary>
        public bool LayoutColumnsEnabled { get; set; } = false;
    }
}