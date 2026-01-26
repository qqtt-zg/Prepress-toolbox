using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp3.Controls;
using WindowsFormsApp3.Forms.Controls;
using WindowsFormsApp3.Utils;
using WinFormsPanel = System.Windows.Forms.Panel;
using IOPath = System.IO.Path;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// PDF检查器面板
    /// 集成PDF预览和检查器功能，类似Enfocus PitStop Pro
    /// </summary>
    public partial class PdfInspectorPanel : BasePanelControl
    {
        public override string PanelKey => "pdf_inspector";
        public override string DisplayName => "PDF检查器";
        public override string IconName => "FileSearchOutlined";

        private string _currentFilePath;

        public PdfInspectorPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 打开按钮点击事件
        /// </summary>
        private void OpenButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "PDF文件|*.pdf";
                dialog.Title = "选择PDF文件";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadPdf(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// 加载PDF文件
        /// </summary>
        /// <summary>
        /// 加载PDF文件
        /// </summary>
        private void LoadPdf(string filePath)
        {
            try
            {
                _currentFilePath = filePath;

                // 加载检查器
                _inspector.LoadPdf(filePath, 1);

                LogHelper.Info($"PDF检查器加载成功: {filePath}");
            }
            catch (Exception ex)
            {
                LogHelper.Error($"加载PDF失败: {ex.Message}");
                AntdUI.Notification.error(this.FindForm(), "加载失败", ex.Message);
            }
        }

        /// <summary>
        /// 检查器页面选择事件
        /// </summary>
        private void Inspector_PageSelected(object sender, int pageNumber)
        {
            // 以后如果有外部预览联动，可以在这里处理
            // 目前仅作为占位符
             LogHelper.Debug($"PDF检查器选中页面: {pageNumber}");
        }

        /// <summary>
        /// 面板激活时
        /// </summary>
        public override void OnActivated()
        {
            base.OnActivated();
            LogHelper.Debug("PDF检查器面板已激活");
        }

        /// <summary>
        /// 面板停用时
        /// </summary>
        public override void OnDeactivated()
        {
            base.OnDeactivated();
            LogHelper.Debug("PDF检查器面板已停用");
        }

    }
}
