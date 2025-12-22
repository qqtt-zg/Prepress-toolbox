using System;
using System.Windows.Forms;
using WindowsFormsApp3; // 假设 Form1 在此命名空间
using WindowsFormsApp3.Forms.Main;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// 文件重命名面板 - 包装原 Form1
    /// </summary>
    public partial class FileRenamePanel : BasePanelControl
    {
        private Form1 _embeddedForm;

        public override string PanelKey => "rename";
        public override string DisplayName => "文件重命名";
        public override string IconName => "FolderOpenOutlined";

        public FileRenamePanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 面板初始化（懒加载）
        /// </summary>
        protected override void InitializePanel()
        {
            base.InitializePanel();
            
            try 
            {
                ShowLoading("正在加载文件重命名模块...");

                // 创建并嵌入 Form1
                // 注意：这里需要确保 Form1 不会尝试修改主窗口标题或图标等 TopLevel 操作
                _embeddedForm = new Form1();
                _embeddedForm.TopLevel = false;
                _embeddedForm.FormBorderStyle = FormBorderStyle.None;
                _embeddedForm.Dock = DockStyle.Fill;
                _embeddedForm.ShowInTaskbar = false;
                
                // 添加到面板
                this.Controls.Add(_embeddedForm);
                _embeddedForm.Show();
                
                // 调整 Form1 样式以适应面板
                // 例如隐藏不需要的边框等
            }
            catch (Exception ex)
            {
                ShowError($"加载功能模块失败: {ex.Message}");
            }
            finally
            {
                HideLoading();
            }
        }

        /// <summary>
        /// 面板刷新
        /// </summary>
        protected override void OnRefresh()
        {
            base.OnRefresh();
            // 可以在这里调用 Form1 的刷新方法
            // if (_embeddedForm != null) _embeddedForm.RefreshData();
        }
        
        /// <summary>
        /// 更新Excel数据
        /// </summary>
        public void UpdateExcelData()
        {
            if (_embeddedForm != null && !_embeddedForm.IsDisposed)
            {
                // 调用Form1的UpdateExcelData方法刷新显示
                _embeddedForm.UpdateExcelData();
            }
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_embeddedForm != null && !_embeddedForm.IsDisposed)
                {
                    _embeddedForm.Close();
                    _embeddedForm.Dispose();
                }
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
