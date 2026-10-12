using System;
using System.Windows.Forms;
using WindowsFormsApp3.Presenters;
using WindowsFormsApp3.Services;

namespace WindowsFormsApp3.Forms.Controls.Settings
{
    public partial class SettingsImpositionControl : UserControl
    {
        public ImpositionParametersEditor Editor { get; }

        public SettingsImpositionControl()
        {
            InitializeComponent();
            Editor = new ImpositionParametersEditor(new ImpositionEditorPresenter(new ImpositionService(),
                true, true, null)) { Dock = DockStyle.Fill };
            Controls.Add(Editor);
        }

        public void SaveSettings()
        {
            // 外层设置页统一处理保存失败，不把错误误报为保存成功。
            if (!Editor.TryApply(out string error)) throw new InvalidOperationException(error);
        }
    }
}
