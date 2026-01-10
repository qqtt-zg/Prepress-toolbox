using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApp3.Interfaces;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;

namespace WindowsFormsApp3.Services
{
    /// <summary>
    /// 服务定位器类，用于管理和获取应用程序中的各种服务
    /// </summary>
    public class ServiceLocator
    {
        private static ServiceLocator _instance;
        private static readonly object _lock = new object();

        // 服务实例
        private IServiceProvider _serviceProvider;
        private IServiceCollection _services;
        private bool _isInitialized = false;

        /// <summary>
        /// 获取ServiceLocator的单例实例
        /// </summary>
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ServiceLocator();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private ServiceLocator()
        {
            // 初始化服务实例
            InitializeServices();
        }

        /// <summary>
        /// 初始化所有服务
        /// </summary>
        private void InitializeServices()
        {
            if (_isInitialized)
                return;

            // 使用Microsoft.Extensions.DependencyInjection构建服务容器
            _services = new ServiceCollection();
            
            // 注册统一的日志服务接口
            _services.AddSingleton<Interfaces.ILogger>(provider =>
            {
                // 创建FileLogger实例，它会自动从LogConfigManager加载配置
                var fileLogger = new FileLogger();
                
                try
                {
                    // 确保配置已从配置文件加载
                    var config = LogConfigManager.GetConfig();
                    fileLogger.UpdateFromConfig();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to configure logger level: " + ex.Message);
                }
                
                return fileLogger;
            });
            
            // 注册事件总线服务（注入日志服务）- 必须在IFileRenameService之前注册
            _services.AddSingleton<IEventBus>(provider =>
            {
                var logger = provider.GetService<Interfaces.ILogger>();
                return new EventBus(logger);
            });

            // 注册不依赖其他服务的服务
            _services.AddSingleton<IExcelImportService, ExcelImportHelper>();
            _services.AddSingleton<IFileMonitor, FileMonitor>();
            _services.AddSingleton<IPdfProcessingService, PdfProcessingService>();
            _services.AddSingleton<ICompositeColumnService, CompositeColumnService>();

            // 注册尺寸计算服务
            _services.AddSingleton<IDimensionCalculationService>(provider =>
            {
                var pdfDimensionService = PdfDimensionServiceFactory.GetInstance();
                var logger = provider.GetService<Interfaces.ILogger>();
                return new DimensionCalculationService(pdfDimensionService, logger);
            });



            // 注册文件重命名服务（使用工厂方法传递事件总线依赖）
            _services.AddSingleton<Interfaces.IFileRenameService>(provider =>
            {
                var eventBus = provider.GetService<IEventBus>();
                return new FileRenameService(eventBus);
            });
            
            // 注册缓存服务
            _services.AddSingleton<ICacheService, MemoryCacheService>();

            // 注册错误恢复服务
            _services.AddSingleton<IErrorRecoveryService, ErrorRecoveryService>();

            // 注册撤销/重做服务
            _services.AddSingleton<IUndoRedoService, UndoRedoService>();

            // 注册增强的撤销/重做服务
            _services.AddSingleton<IEnhancedUndoRedoService, EnhancedUndoRedoService>();

            // 注册内存监控服务
            _services.AddSingleton<IMemoryMonitorService, MemoryMonitorService>();

            // 注册性能基准测试服务
            _services.AddSingleton<IPerformanceBenchmarkService>(provider =>
            {
                var logger = provider.GetService<Interfaces.ILogger>();
                var memoryMonitor = provider.GetService<IMemoryMonitorService>();
                return new PerformanceBenchmarkService(logger, memoryMonitor);
            });

            // 注册优化文件处理服务
            _services.AddSingleton<IOptimizedFileProcessingService>(provider =>
            {
                var logger = provider.GetService<Interfaces.ILogger>();
                var memoryMonitor = provider.GetService<IMemoryMonitorService>();
                var errorRecovery = provider.GetService<IErrorRecoveryService>();
                return new OptimizedFileProcessingService(logger, memoryMonitor, errorRecovery);
            });

            // 注册列组合服务
            _services.AddSingleton<ICompositeColumnService, CompositeColumnService>();

            // 注册配置服务
            _services.AddSingleton<IConfigService>(provider =>
            {
                var logger = provider.GetService<Interfaces.ILogger>();
                var cacheService = provider.GetService<ICacheService>();
                return new ConfigService(logger, cacheService);
            });

            // 注册批量处理服务（需要手动解析依赖）
            _services.AddSingleton<IBatchProcessingService>(provider =>
            {
                var fileRenameService = provider.GetService<Interfaces.IFileRenameService>();
                var pdfProcessingService = provider.GetService<IPdfProcessingService>();
                var logger = provider.GetService<Interfaces.ILogger>();
                var eventBus = provider.GetService<IEventBus>();
                return new BatchProcessingService(fileRenameService, pdfProcessingService, logger, eventBus);
            });
            
            // 注册主题管理服务
            _services.AddSingleton<ThemeManager>(provider =>
            {
                var logger = provider.GetService<Interfaces.ILogger>();
                return new ThemeManager(logger);
            });
            
            // 构建服务提供程序
            _serviceProvider = _services.BuildServiceProvider();
            _isInitialized = true;
            
            // 手动设置依赖（保持向后兼容性）
            var logger = _serviceProvider.GetService<Interfaces.ILogger>();
            var excelImportService = _serviceProvider.GetService<IExcelImportService>();
            var fileMonitor = _serviceProvider.GetService<IFileMonitor>();
            var fileRenameService = _serviceProvider.GetService<IFileRenameService>();
            
            if (excelImportService is ExcelImportHelper excelHelper)
            {
                excelHelper.SetLogger(logger);
            }
            if (fileMonitor is FileMonitor monitor)
            {
                // 需要查看FileMonitor.SetLogger方法的参数类型，如果是Services.ILogger则需要使用适配器
                try
                {
                    monitor.SetLogger(logger);
                }
                catch (Exception)
                {
                    // 如果直接设置失败，使用控制台输出警告
                    Console.WriteLine("Warning: Failed to set logger for FileMonitor. It may still be using the old Services.ILogger interface.");
                }
            }
            if (fileRenameService is FileRenameService renameService)
            {
                // 现在通过构造函数注入，不再需要手动设置
            }
        }

        /// <summary>
        /// 获取Excel导入服务
        /// </summary>
        /// <returns>Excel导入服务实例</returns>
        public IExcelImportService GetExcelImportService()
        {
            return _serviceProvider.GetService<IExcelImportService>();
        }

        /// <summary>
        /// 获取文件监控服务
        /// </summary>
        /// <returns>文件监控服务实例</returns>
        public IFileMonitor GetFileMonitor()
        {
            return _serviceProvider.GetService<IFileMonitor>();
        }

        /// <summary>
        /// 获取文件重命名服务
        /// </summary>
        /// <returns>文件重命名服务实例</returns>
        public Interfaces.IFileRenameService GetFileRenameService()
        {
            return _serviceProvider.GetService<Interfaces.IFileRenameService>();
        }

        /// <summary>
        /// 获取PDF处理服务
        /// </summary>
        /// <returns>PDF处理服务实例</returns>
        public IPdfProcessingService GetPdfProcessingService()
        {
            return _serviceProvider.GetService<IPdfProcessingService>();
        }



        /// <summary>
        /// 获取日志服务
        /// </summary>
        /// <returns>日志服务实例</returns>
        public Interfaces.ILogger GetLogger()
        {
            return _serviceProvider.GetService<Interfaces.ILogger>();
        }

        /// <summary>
        /// 获取错误恢复服务
        /// </summary>
        /// <returns>错误恢复服务实例</returns>
        public IErrorRecoveryService GetErrorRecoveryService()
        {
            return _serviceProvider.GetService<IErrorRecoveryService>();
        }

        /// <summary>
        /// 获取撤销/重做服务
        /// </summary>
        /// <returns>撤销/重做服务实例</returns>
        public IUndoRedoService GetUndoRedoService()
        {
            return _serviceProvider.GetService<IUndoRedoService>();
        }

        /// <summary>
        /// 获取内存监控服务
        /// </summary>
        /// <returns>内存监控服务实例</returns>
        public IMemoryMonitorService GetMemoryMonitorService()
        {
            return _serviceProvider.GetService<IMemoryMonitorService>();
        }

        /// <summary>
        /// 获取性能基准测试服务
        /// </summary>
        /// <returns>性能基准测试服务实例</returns>
        public IPerformanceBenchmarkService GetPerformanceBenchmarkService()
        {
            return _serviceProvider.GetService<IPerformanceBenchmarkService>();
        }

        /// <summary>
        /// 获取优化文件处理服务
        /// </summary>
        /// <returns>优化文件处理服务实例</returns>
        public IOptimizedFileProcessingService GetOptimizedFileProcessingService()
        {
            return _serviceProvider.GetService<IOptimizedFileProcessingService>();
        }

        /// <summary>
        /// 获取批量处理服务
        /// </summary>
        /// <returns>批量处理服务实例</returns>
        public IBatchProcessingService GetBatchProcessingService()
        {
            return _serviceProvider.GetService<IBatchProcessingService>();
        }

        /// <summary>
        /// 获取事件总线服务
        /// </summary>
        /// <returns>事件总线服务实例</returns>
        public IEventBus GetEventBus()
        {
            return _serviceProvider.GetService<IEventBus>();
        }

        /// <summary>
        /// 获取列组合服务
        /// </summary>
        /// <returns>列组合服务实例</returns>
        public ICompositeColumnService GetCompositeColumnService()
        {
            return _serviceProvider.GetService<ICompositeColumnService>();
        }

        /// <summary>
        /// 获取配置服务
        /// </summary>
        /// <returns>配置服务实例</returns>
        public IConfigService GetConfigService()
        {
            return _serviceProvider.GetService<IConfigService>();
        }

        /// <summary>
        /// 获取尺寸计算服务
        /// </summary>
        /// <returns>尺寸计算服务实例</returns>
        public IDimensionCalculationService GetDimensionCalculationService()
        {
            return _serviceProvider.GetService<IDimensionCalculationService>();
        }

        /// <summary>
        /// 获取主题管理服务
        /// </summary>
        /// <returns>主题管理服务实例</returns>
        public ThemeManager GetThemeManager()
        {
            return _serviceProvider.GetService<ThemeManager>();
        }

        /// <summary>
        /// 重置服务定位器（主要用于测试）
        /// </summary>
        internal static void Reset()
        {
            _instance = null;
        }

        /// <summary>
        /// 注册自定义服务实现
        /// </summary>
        /// <param name="service">服务实例</param>
        public void RegisterExcelImportService(IExcelImportService service)
        {
            RegisterService<IExcelImportService>(service);
        }

        /// <summary>
        /// 注册自定义文件监控服务实现
        /// </summary>
        /// <param name="service">服务实例</param>
        public void RegisterFileMonitor(IFileMonitor service)
        {
            RegisterService<IFileMonitor>(service);
        }

        /// <summary>
        /// 注册自定义文件重命名服务实现
        /// </summary>
        /// <param name="service">服务实例</param>
        public void RegisterFileRenameService(IFileRenameService service)
        {
            RegisterService<IFileRenameService>(service);
        }

        /// <summary>
        /// 注册自定义PDF处理服务实现
        /// </summary>
        /// <param name="service">服务实例</param>
        public void RegisterPdfProcessingService(IPdfProcessingService service)
        {
            RegisterService<IPdfProcessingService>(service);
        }



        /// <summary>
        /// 注册自定义批量处理服务实现
        /// </summary>
        /// <param name="service">服务实例</param>
        public void RegisterBatchProcessingService(IBatchProcessingService service)
        {
            RegisterService<IBatchProcessingService>(service);
        }

        /// <summary>
        /// 注册自定义日志服务实现
        /// </summary>
        /// <param name="service">服务实例</param>
        public void RegisterLogger(Interfaces.ILogger service)
        {
            RegisterService<Interfaces.ILogger>(service);
        }

        /// <summary>
        /// 注册自定义列组合服务实现
        /// </summary>
        /// <param name="service">服务实例</param>
        public void RegisterCompositeColumnService(ICompositeColumnService service)
        {
            RegisterService<ICompositeColumnService>(service);
        }

        /// <summary>
        /// 通用服务注册方法
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <param name="service">服务实例</param>
        private void RegisterService<T>(T service)
        {
            // 移除已存在的服务描述
            var existingDescriptor = _services.FirstOrDefault(d => d.ServiceType == typeof(T));
            if (existingDescriptor != null)
            {
                _services.Remove(existingDescriptor);
            }
            
            // 添加新的服务描述
            _services.AddSingleton(typeof(T), service);
            
            // 重新构建服务提供程序
            _serviceProvider = _services.BuildServiceProvider();
        }



        /// <summary>
        /// 获取日志服务
        /// </summary>
        public Interfaces.ILogger Logger
        {
            get
            {
                return _serviceProvider.GetService<Interfaces.ILogger>();
            }
        }



        /// <summary>
        /// 获取所有服务
        /// </summary>
        /// <returns>包含所有服务的字典</returns>
        public Dictionary<Type, object> GetAllServices()
        {
            return new Dictionary<Type, object>
            {
                { typeof(IExcelImportService), _serviceProvider.GetService<IExcelImportService>() },
                { typeof(IFileMonitor), _serviceProvider.GetService<IFileMonitor>() },
                { typeof(IFileRenameService), _serviceProvider.GetService<IFileRenameService>() },
                { typeof(IPdfProcessingService), _serviceProvider.GetService<IPdfProcessingService>() },

                { typeof(ILogger), _serviceProvider.GetService<ILogger>() },

                { typeof(IBatchProcessingService), _serviceProvider.GetService<IBatchProcessingService>() },
                { typeof(IEventBus), _serviceProvider.GetService<IEventBus>() },
                { typeof(ICompositeColumnService), _serviceProvider.GetService<ICompositeColumnService>() }
            };
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>是否已注册</returns>
        public bool IsServiceRegistered<T>()
        {
            return _services.Any(d => d.ServiceType == typeof(T));
        }

        /// <summary>
        /// 获取服务数量
        /// </summary>
        /// <returns>服务数量</returns>
        public int GetServiceCount()
        {
            return _services.Count;
        }

        /// <summary>
        /// 获取指定类型的服务
        /// </summary>
        /// <typeparam name="T">服务类型</typeparam>
        /// <returns>服务实例</returns>
        public T GetService<T>()
        {
            return _serviceProvider.GetService<T>();
        }

        /// <summary>
        /// 获取指定类型的服务
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <returns>服务实例</returns>
        public object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }
    }
}