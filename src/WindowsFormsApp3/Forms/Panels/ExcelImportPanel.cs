using System;
using System.Data;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Dialogs; // ExcelImportForm namespace

namespace WindowsFormsApp3.Forms.Panels
{
    public partial class ExcelImportPanel : BasePanelControl
    {
        private ExcelImportForm _embeddedForm;
        
        // 定义数据导入事件，用于传递数据给主窗体/其他面板
        // 定义数据导入事件，用于传递数据给主窗体/其他面板
        public event EventHandler DataImported;

        public override string PanelKey => "excel_import";
        public override string DisplayName => "Excel导入";
        public override string IconName => "FileExcelOutlined";

        public ExcelImportPanel()
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
                ShowLoading("正在加载Excel导入模块..."); // Changed message

                // 创建并嵌入 ExcelImportForm
                _embeddedForm = new ExcelImportForm(); // Changed constructor call
                
                // 设置嵌入属性
                _embeddedForm.TopLevel = false;
                _embeddedForm.FormBorderStyle = FormBorderStyle.None;
                _embeddedForm.Dock = DockStyle.Fill;
                _embeddedForm.ShowInTaskbar = false; // Added
                
                // 设置为嵌入模式
                _embeddedForm.IsEmbedded = true; // Added
                
                // 订阅数据导入事件
                _embeddedForm.OnDataImported += OnEmbeddedFormDataImported; // Subscribed to new handler
                
                // 添加到面板
                this.Controls.Add(_embeddedForm);
                _embeddedForm.Show();
            }
            catch (Exception ex)
            {
                ShowError($"加载功能模块失败: {ex.Message}"); // Changed error message
            }
            finally
            {
                HideLoading();
            }
        }
        
        private void OnEmbeddedFormDataImported(object sender, System.Data.DataTable data)
        {
            try
            {
                // 获取Excel导入服务
                var service = WindowsFormsApp3.Services.ServiceLocator.Instance.GetExcelImportService();
                
                // 更新服务中的数据
                service.ImportedData = data;
                service.SearchColumnIndex = _embeddedForm.SearchColumnIndex;
                service.ReturnColumnIndex = _embeddedForm.ReturnColumnIndex;
                service.SerialColumnIndex = _embeddedForm.NewColumnIndex;
                
                // 触发面板事件
                DataImported?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"同步Excel数据失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // The original BindImportEvent method is removed as per the instruction's implied replacement.
        
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
