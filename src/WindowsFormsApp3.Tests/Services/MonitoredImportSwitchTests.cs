using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Moq;
using WindowsFormsApp3.Forms.Panels;
using WindowsFormsApp3.Forms.Utils;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Xunit;

namespace WindowsFormsApp3.Tests.Services
{
    public class MonitoredImportSwitchTests
    {
        [WinFormsTheory]
        [InlineData(true, true)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(false, false)]
        public async Task Import_ShouldOpenSelectionAfterSwitchingBothWays(bool floatingFirst, bool overwriteSameName)
        {
            string previousRoot = Environment.GetEnvironmentVariable("CODEX_APPDATA_ROOT");
            string root = Path.Combine(Path.GetTempPath(), "MonitoredImportSwitch_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", root);
            AppSettings.ResetForTesting();
            AppSettings.Initialize(new FileLogger(Path.Combine(root, "test.log")));
            AppSettings.EnableDailyJson = false;
            var locator = ServiceLocator.Instance;
            var originalMonitor = locator.GetFileMonitor();
            var monitor = new FileMonitor();
            using (var data = new MaterialSelectionPreviewData())
            {
                locator.RegisterFileMonitor(monitor);
                var view = new Mock<IFileRenamePanelView>();
                view.SetupProperty(candidate => candidate.IsMonitoring, false);
                view.SetupGet(candidate => candidate.InputDirectory).Returns(root);
                view.SetupGet(candidate => candidate.IsImmediateMode).Returns(true);
                view.SetupGet(candidate => candidate.FileList).Returns(new BindingList<FileRenameInfo>());
                view.SetReturnsDefault(DialogResult.Cancel);
                var presenter = new FileRenamePanelPresenter(view.Object);
                string target = Path.Combine(root, "同名工单.pdf");
                using (var floating = new FloatingDropZoneForm((source, effect) => File.Copy(source, target, true),
                    () => DropOperationMode.ForceCopy, root))
                {
                    try
                    {
                        presenter.StartMonitoring();
                        for (int index = 0; index < 3; index++)
                        {
                            target = Path.Combine(root, overwriteSameName ? "同名工单.pdf" : $"新工单{index}.pdf");
                            bool useFloating = index % 2 == 0 ? floatingFirst : !floatingFirst;
                            if (useFloating)
                            {
                                floating.Location = new System.Drawing.Point(-4000, -4000);
                                floating.Show();
                                var drop = new DataObject(DataFormats.FileDrop, new[] { data.FilePaths[index] });
                                typeof(FloatingDropZoneForm).GetMethod("OnDragDrop", BindingFlags.Instance | BindingFlags.NonPublic,
                                    null, new[] { typeof(object), typeof(DragEventArgs) }, null)
                                    .Invoke(floating, new object[] { floating, new DragEventArgs(drop, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy) });
                            }
                            else
                            {
                                floating.Hide();
                                File.Copy(data.FilePaths[index], target, true);
                            }
                            for (int poll = 0; poll < 160; poll++)
                            {
                                bool workerRunning = (bool)typeof(FileRenamePanelPresenter)
                                    .GetField("_isFileQueueWorkerRunning", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(presenter);
                                int dialogs = view.Invocations.Count(call => call.Method.Name == "ShowMaterialSelectionDialog");
                                if (dialogs >= index + 1 && !workerRunning) break;
                                await Task.Delay(50);
                            }
                            Assert.Equal(index + 1, view.Invocations.Count(call => call.Method.Name == "ShowMaterialSelectionDialog"));
                        }
                        Assert.DoesNotContain(view.Invocations, call => call.Method.Name == "ShowError");
                    }
                    finally
                    {
                        presenter.StopMonitoring();
                        monitor.Dispose();
                        var onExit = typeof(FileRenamePanelPresenter).GetMethod("OnApplicationExit", BindingFlags.Instance | BindingFlags.NonPublic);
                        Application.ApplicationExit -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), presenter, onExit);
                        locator.RegisterFileMonitor(originalMonitor);
                        AppSettings.ResetForTesting();
                        Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", previousRoot);
                    }
                }
            }
        }
    }
}
