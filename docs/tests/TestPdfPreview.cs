using System;
using System.IO;
using System.Windows.Forms;
using WindowsFormsApp3.Utils;

namespace TestPdfPreview
{
    class TestForm : Form
    {
        private Button btnLoadPdf;
        private WindowsFormsApp3.Controls.PdfPreviewControl pdfControl;
        private TextBox txtLog;

        public TestForm()
        {
            this.Text = "PDF预览测试";
            this.Size = new System.Drawing.Size(800, 600);

            btnLoadPdf = new Button { Text = "加载测试PDF", Top = 10, Left = 10, Width = 150 };
            btnLoadPdf.Click += BtnLoadPdf_Click;

            // PDF预览控件
            pdfControl = new WindowsFormsApp3.Controls.PdfPreviewControl
            {
                Top = 60,
                Left = 10,
                Width = 760,
                Height = 400,
                BorderStyle = BorderStyle.FixedSingle
            };

            // 日志文本框
            txtLog = new TextBox
            {
                Top = 480,
                Left = 10,
                Width = 760,
                Height = 50,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = "准备就绪..."
            };

            this.Controls.AddRange(new Control[] { btnLoadPdf, pdfControl, txtLog });
        }

        private async void BtnLoadPdf_Click(object sender, EventArgs e)
        {
            try
            {
                string pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Test", "TestPdf.pdf");

                txtLog.AppendText($"\n尝试加载PDF: {pdfPath}");

                if (!File.Exists(pdfPath))
                {
                    txtLog.AppendText("\nPDF文件不存在！");
                    return;
                }

                txtLog.AppendText($"\nPDF文件大小: {new FileInfo(pdfPath).Length} 字节");

                bool success = await pdfControl.LoadPdfAsync(pdfPath);

                if (success)
                {
                    txtLog.AppendText($"\nPDF加载成功！页数: {pdfControl.PageCount}");
                }
                else
                {
                    txtLog.AppendText("\nPDF加载失败！");
                }
            }
            catch (Exception ex)
            {
                txtLog.AppendText($"\n加载异常: {ex.Message}");
            }
        }
    }

    class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TestForm());
        }
    }
}