using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Xunit;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Tests.Presenters
{
    public class Form1PresenterTests
    {
        private Mock<IForm1View> _mockView;
        private Mock<IFileRenameService> _mockFileRenameService;
        private Mock<IPdfProcessingService> _mockPdfProcessingService;
        private Mock<WindowsFormsApp3.Interfaces.ILogger> _mockLogger;
        private Mock<BatchProcessingService> _mockBatchProcessingService;
        private Form1Presenter _presenter;

        public Form1PresenterTests()
        {
            // 创建模拟对象
            _mockView = new Mock<IForm1View>();
            _mockFileRenameService = new Mock<IFileRenameService>();
            _mockPdfProcessingService = new Mock<IPdfProcessingService>();
            _mockLogger = new Mock<WindowsFormsApp3.Interfaces.ILogger>();
            
            // 为BatchProcessingService提供所有必需的构造函数参数
            _mockBatchProcessingService = new Mock<BatchProcessingService>(
                _mockFileRenameService.Object, 
                _mockPdfProcessingService.Object, 
                _mockLogger.Object);

            // 重置ServiceLocator实例并注册模拟服务
            ServiceLocator.Reset();
            var serviceLocator = ServiceLocator.Instance;
            serviceLocator.RegisterBatchProcessingService(_mockBatchProcessingService.Object);
            serviceLocator.RegisterFileRenameService(_mockFileRenameService.Object);
            serviceLocator.RegisterPdfProcessingService(_mockPdfProcessingService.Object);
            serviceLocator.RegisterLogger(_mockLogger.Object);

            // 初始化演示器
            _presenter = new Form1Presenter(_mockView.Object);
        }

        [Fact]
        public void Constructor_Should_RegisterViewEvents()
        {
            // 验证所有事件都被注册
            _mockView.VerifyAdd(v => v.ImmediateRenameClick += It.IsAny<EventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.StopImmediateRenameClick += It.IsAny<EventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.ToggleModeClick += It.IsAny<EventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.MonitorClick += It.IsAny<EventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.ExportSettingsClick += It.IsAny<EventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.KeyDown += It.IsAny<KeyEventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.FormClosing += It.IsAny<FormClosingEventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.FormLoad += It.IsAny<EventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.Resize += It.IsAny<EventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.CellValueChanged += It.IsAny<DataGridViewCellEventHandler>(), Times.Once);
            _mockView.VerifyAdd(v => v.ColumnHeaderMouseClick += It.IsAny<DataGridViewCellMouseEventHandler>(), Times.Once);
        }

        [Fact]
        public void Initialize_Should_PerformAllInitializationSteps()
        {
            // 调用方法
            _presenter.Initialize();

            // 验证所有初始化方法都被调用
            _mockView.Verify(v => v.InitializeDataGridView(), Times.Once);
            _mockView.Verify(v => v.UpdateStatusStrip(), Times.Once);
            _mockView.Verify(v => v.UpdateDgvFilesEditMode(), Times.Once);
            _mockView.Verify(v => v.UpdateTrayMenuItems(), Times.Once);
        }

        [Fact]
        public void HandleImmediateRenameToggle_Should_UpdateUIComponents()
        {
            // 调用方法（现在这个方法只是更新UI，不再切换状态）
            _presenter.HandleImmediateRenameToggle();
            
            // 验证UI更新方法被调用
            _mockView.Verify(v => v.UpdateStatusStrip(), Times.Once);
            _mockView.Verify(v => v.UpdateDgvFilesEditMode(), Times.Once);
            _mockView.Verify(v => v.UpdateTrayMenuItems(), Times.Once);
        }

        [Fact]
        public void HandleFormClosing_Should_AllowClose_When_AllowCloseIsTrue()
        {
            // 设置模拟行为
            var closingEventArgs = new FormClosingEventArgs(CloseReason.UserClosing, false);
            
            // 通过反射设置私有字段_allowClose为true
            typeof(Form1Presenter).GetField("_allowClose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_presenter, true);

            // 调用方法
            _presenter.HandleFormClosing(closingEventArgs);

            // 验证未取消关闭
            Assert.False(closingEventArgs.Cancel);
        }

        [Fact]
        public void HandleFormClosing_Should_MinimizeToTray_When_UserClosing()
        {
            // 设置模拟行为
            var closingEventArgs = new FormClosingEventArgs(CloseReason.UserClosing, false);
            
            // 通过反射设置私有字段_allowClose为false
            typeof(Form1Presenter).GetField("_allowClose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_presenter, false);

            // 设置Hide方法的模拟行为
            _mockView.Setup(v => v.Hide());

            // 调用方法
            _presenter.HandleFormClosing(closingEventArgs);

            // 验证取消关闭并最小化到托盘
            Assert.True(closingEventArgs.Cancel);
            _mockView.VerifySet(v => v.WindowState = FormWindowState.Minimized, Times.Once);
            _mockView.Verify(v => v.Hide(), Times.Once);
        }

        [Fact]
        public void HandleKeyDown_Should_HandleUndoAndRedoShortcuts()
        {
            // 创建键盘事件参数，设置为Ctrl+Z（撤销）
            var undoKeyEventArgs = new KeyEventArgs(Keys.Z | Keys.Control);
            _presenter.HandleKeyDown(undoKeyEventArgs);

            // 创建键盘事件参数，设置为Ctrl+Y（重做）
            var redoKeyEventArgs = new KeyEventArgs(Keys.Y | Keys.Control);
            _presenter.HandleKeyDown(redoKeyEventArgs);

            // 目前没有明确的验证点，因为没有实现撤销重做的验证
            // 此测试主要是确保方法不会抛出异常
            Assert.True(true);
        }

        [Fact]
        public void HandleResize_Should_HideWindow_When_Minimized()
        {
            // 设置模拟行为
            _mockView.Setup(v => v.WindowState).Returns(FormWindowState.Minimized);
            
            // 通过反射设置私有字段_allowClose为false
            typeof(Form1Presenter).GetField("_allowClose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(_presenter, false);

            // 调用方法
            _presenter.HandleResize();

            // 验证隐藏窗口
            _mockView.Verify(v => v.Hide(), Times.Once);
        }
    }
}