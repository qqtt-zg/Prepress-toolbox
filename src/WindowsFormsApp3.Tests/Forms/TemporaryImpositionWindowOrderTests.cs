using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Main;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.Utils;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class TemporaryImpositionWindowOrderTests : IDisposable
    {
        private readonly string _previousRoot;
        private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr window, uint command);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr window);

        public TemporaryImpositionWindowOrderTests()
        {
            _previousRoot = Environment.GetEnvironmentVariable("CODEX_APPDATA_ROOT");
            string root = Path.Combine(Path.GetTempPath(), "PrepressWindowOrder_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", root);
            AppSettings.ResetForTesting(); AppSettings.Initialize(new FileLogger(Path.Combine(root, "test.log")));
            AppSettings.MaterialFormTopMost = true;
        }

        private static bool Above(IntPtr higher, IntPtr lower)
        {
            for (IntPtr window = GetWindow(lower, 3); window != IntPtr.Zero; window = GetWindow(window, 3))
                if (window == higher) return true;
            return false;
        }

        private static bool NativeTopMost(Form form) => (GetWindowLong(form.Handle, -20) & 8) != 0;
        private static void ShowDialog(MaterialSelectFormModern owner, TemporaryImpositionParametersForm dialog) =>
            typeof(MaterialSelectFormModern).GetMethod("ShowTemporaryImpositionDialog", Private).Invoke(owner, new object[] { dialog });

        private static MaterialSelectFormModern MaterialOwner() => new MaterialSelectFormModern(
            new List<string> { "PET" }, "", "Job", 1, "90", "80", null, -1, -1, -1, -1, "A01");

        private static TemporaryImpositionParametersForm Dialog() => new TemporaryImpositionParametersForm(true, new TemporaryImpositionParameters
        {
            PaperWidth = 320, PaperHeight = 464, FixedWidth = 320, MinLength = 420,
            FlatSheetRotationMode = RollRotationMode.Auto, RollMaterialRotationMode = RollRotationMode.Auto
        });

        [WinFormsFact]
        public async Task ModalOrder_ShouldSurviveOwnerPromotionAndHandleRecreation()
        {
            using (var owner = MaterialOwner())
            using (var dialog = Dialog())
            {
                owner.StartPosition = dialog.StartPosition = FormStartPosition.Manual;
                owner.Location = dialog.Location = new Point(-4000, -4000);
                owner.Show();
                Exception failure = null;
                dialog.Shown += (s, e) => dialog.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        await Task.Delay(400);
                        Assert.Same(owner, dialog.Owner);
                        Assert.False(owner.Enabled);
                        Assert.False(IsWindowEnabled(owner.Handle));
                        Assert.True(dialog.TopMost && NativeTopMost(dialog) && Above(dialog.Handle, owner.Handle));
                        using (var duplicate = Dialog())
                        {
                            ShowDialog(owner, duplicate);
                            Assert.False(duplicate.Visible);
                            Assert.True(dialog.Visible);
                        }
                        typeof(MaterialSelectFormModern).GetMethod("ApplyTopMostState", Private).Invoke(owner, new object[] { true, false });
                        await Task.Delay(50);
                        Assert.True(Above(dialog.Handle, owner.Handle));
                        typeof(Control).GetMethod("RecreateHandle", Private).Invoke(owner, null);
                        await Task.Delay(50);
                        Assert.True(GetWindow(dialog.Handle, 4) == owner.Handle && Above(dialog.Handle, owner.Handle));
                        Assert.False(owner.Enabled || IsWindowEnabled(owner.Handle));
                        typeof(Control).GetMethod("RecreateHandle", Private).Invoke(dialog, null);
                        await Task.Delay(50);
                        Assert.True(NativeTopMost(dialog) && Above(dialog.Handle, owner.Handle));
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { dialog.DialogResult = DialogResult.Cancel; }
                }));
                ShowDialog(owner, dialog);
                Assert.True(owner.Enabled && IsWindowEnabled(owner.Handle));
                Assert.True(AppSettings.MaterialFormTopMost && owner.TopMost && NativeTopMost(owner));
                owner.Close();
                Assert.True(failure == null, failure?.ToString());
                await Task.Yield();
            }
        }

        [WinFormsFact]
        public async Task UnpinnedOwner_ShouldKeepDialogAboveWithoutPermanentTopmost()
        {
            AppSettings.MaterialFormTopMost = false;
            using (var owner = MaterialOwner())
            using (var dialog = Dialog())
            {
                owner.StartPosition = dialog.StartPosition = FormStartPosition.Manual;
                owner.Location = dialog.Location = new Point(-4000, -4000);
                owner.Show();
                typeof(MaterialSelectFormModern).GetMethod("ApplyTopMostState", Private).Invoke(owner, new object[] { false, false });
                Exception failure = null;
                dialog.Shown += (s, e) => dialog.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        await Task.Delay(50);
                        Assert.False(dialog.TopMost || NativeTopMost(dialog));
                        Assert.True(Above(dialog.Handle, owner.Handle));
                        typeof(Control).GetMethod("RecreateHandle", Private).Invoke(owner, null);
                        await Task.Delay(50);
                        Assert.False(dialog.TopMost || NativeTopMost(dialog));
                        Assert.True(Above(dialog.Handle, owner.Handle));
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { dialog.DialogResult = DialogResult.Cancel; }
                }));
                ShowDialog(owner, dialog);
                Assert.True(owner.Enabled && IsWindowEnabled(owner.Handle));
                Assert.False(AppSettings.MaterialFormTopMost);
                Assert.False(owner.TopMost || NativeTopMost(owner));
                owner.Close();
                Assert.True(failure == null, failure?.ToString());
                await Task.Yield();
            }
        }

        [WinFormsFact]
        public async Task NestedDialog_ShouldStayAboveTemporaryParametersWhenOwnerActivates()
        {
            using (var owner = MaterialOwner())
            using (var dialog = Dialog())
            using (var nested = new Form { StartPosition = FormStartPosition.Manual, Location = new Point(-4000, -4000) })
            {
                owner.StartPosition = dialog.StartPosition = FormStartPosition.Manual;
                owner.Location = dialog.Location = new Point(-4000, -4000);
                owner.Show();
                Exception failure = null;
                dialog.Shown += (s, e) => dialog.BeginInvoke(new Action(() =>
                {
                    try { nested.ShowDialog(dialog); }
                    catch (Exception ex) { failure = ex; }
                    finally { dialog.DialogResult = DialogResult.Cancel; }
                }));
                nested.Shown += (s, e) => nested.BeginInvoke(new Action(async () =>
                {
                    try
                    {
                        Assert.False(IsWindowEnabled(dialog.Handle));
                        typeof(Form).GetMethod("OnActivated", Private).Invoke(owner, new object[] { EventArgs.Empty });
                        await Task.Delay(50);
                        Assert.True(Above(nested.Handle, dialog.Handle));
                        Assert.True(Above(dialog.Handle, owner.Handle));
                    }
                    catch (Exception ex) { failure = ex; }
                    finally { nested.DialogResult = DialogResult.Cancel; }
                }));
                ShowDialog(owner, dialog);
                owner.Close();
                Assert.True(failure == null, failure?.ToString());
                await Task.Yield();
            }
        }

        public void Dispose()
        {
            AppSettings.ResetForTesting();
            Environment.SetEnvironmentVariable("CODEX_APPDATA_ROOT", _previousRoot);
        }
    }
}
