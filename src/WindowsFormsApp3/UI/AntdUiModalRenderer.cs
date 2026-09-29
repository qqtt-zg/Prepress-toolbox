using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WindowsFormsApp3.UI
{
    /// <summary>
    /// 将短确认弹框契约映射到 AntdUI。业务逻辑只通过返回的 DialogResult 继续处理。
    /// </summary>
    public static class AntdUiModalRenderer
    {
        private const string BuiltInOkButtonName = "OK";

        public static System.Windows.Forms.DialogResult Show(ModalRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var result = AntdUI.Modal.open(CreateConfig(request));
            return NormalizeClosedResult(request, result);
        }

        public static AntdUI.Modal.Config CreateConfig(ModalRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var defaultButtons = request.Buttons.Where(button => button != null && button.IsDefault).ToArray();
            var declaredCancelButtons = request.Buttons.Where(button => button != null && button.IsCancel).ToArray();
            var cancelButton = FindCancelButton(request.Buttons);
            if (defaultButtons.Length > 1)
            {
                throw new ArgumentException("短确认弹框最多只能指定一个默认按钮。", nameof(request));
            }

            if (declaredCancelButtons.Length > 1)
            {
                throw new ArgumentException("短确认弹框最多只能指定一个取消按钮。", nameof(request));
            }

            if (declaredCancelButtons.Length == 1 && declaredCancelButtons[0].DialogResult != System.Windows.Forms.DialogResult.Cancel)
            {
                throw new ArgumentException("取消按钮必须返回 DialogResult.Cancel。", nameof(request));
            }

            var buttons = request.Buttons.Select(CreateButton).ToArray();
            var config = new AntdUI.Modal.Config(request.Owner, request.Title, request.Message)
            {
                Btns = buttons,
                ColorScheme = request.ColorScheme,
                Icon = request.Icon,
                Keyboard = request.Keyboard,
                MaskClosable = request.MaskClosable,
                // 自定义 Btns 是附加项；关闭内置按钮后，由下面的样式回调绑定键盘语义。
                CancelText = null,
                DefaultAcceptButton = false,
                DefaultFocus = false,
                // 原生确认框和已有短确认框均有安全关闭入口；关闭结果在 Show 中归一化。
                CloseIcon = cancelButton != null,
                OnButtonStyle = (name, button) => ConfigureButton(
                    name,
                    button,
                    request.Buttons,
                    defaultButtons,
                    cancelButton)
            };

            return config;
        }

        private static AntdUI.Modal.Btn CreateButton(ModalButtonSpec button)
        {
            if (button == null)
            {
                throw new ArgumentException("弹框按钮不能为 null。", nameof(button));
            }

            return new AntdUI.Modal.Btn(button.Id, button.Text, button.Type)
            {
                DialogResult = button.DialogResult,
                Tag = button
            };
        }

        private static void ConfigureButton(
            string name,
            AntdUI.Button button,
            IReadOnlyList<ModalButtonSpec> buttonSpecs,
            ModalButtonSpec[] defaultButtons,
            ModalButtonSpec cancelButton)
        {
            if (name == BuiltInOkButtonName)
            {
                // AntdUI 会始终生成内置 OK；自定义按钮模式下将其缩为不可见，避免重复页脚。
                button.AutoSizeMode = AntdUI.TAutoSize.None;
                button.Size = Size.Empty;
                button.Margin = System.Windows.Forms.Padding.Empty;
                button.Visible = false;
                return;
            }

            var spec = buttonSpecs.FirstOrDefault(item => item != null && item.Id == name);
            if (spec != null)
            {
                if (button is System.Windows.Forms.IButtonControl buttonControl)
                {
                    // LayeredFormModal 创建自定义按钮时不会读取 Modal.Btn.DialogResult，
                    // 但 IButtonControl 是 Esc/CancelButton 语义的最终入口。
                    buttonControl.DialogResult = spec.DialogResult;
                }

                // 统一高对比度文本配色：对于 Error(红)、Primary(蓝)、Success(绿) 实色按钮，确保赋予清晰纯白文字
                if (spec.Type == AntdUI.TTypeMini.Error || spec.Type == AntdUI.TTypeMini.Primary || spec.Type == AntdUI.TTypeMini.Success)
                {
                    button.ForeColor = Color.White;
                }
            }

            var isDefault = defaultButtons.Length == 1 && defaultButtons[0].Id == name;
            var isCancel = cancelButton != null && cancelButton.Id == name;
            if (!isDefault && !isCancel)
            {
                return;
            }

            void BindToModal()
            {
                var modal = button.FindForm();
                if (modal == null)
                {
                    return;
                }

                if (isDefault)
                {
                    modal.AcceptButton = button;
                }

                if (isCancel)
                {
                    modal.CancelButton = button;
                }
            }

            // OnButtonStyle 在 LayeredFormModal 将按钮加入最终窗体之前调用，
            // 因此不能只依赖 ParentChanged。HandleCreated/VisibleChanged 覆盖真实构造顺序。
            button.ParentChanged += (sender, args) => BindToModal();
            button.HandleCreated += (sender, args) => BindToModal();
            button.VisibleChanged += (sender, args) => BindToModal();
            BindToModal();
        }

        private static ModalButtonSpec FindCancelButton(IReadOnlyList<ModalButtonSpec> buttons)
        {
            return buttons.FirstOrDefault(button => button != null && button.IsCancel)
                ?? buttons.FirstOrDefault(button => button != null && button.DialogResult == System.Windows.Forms.DialogResult.Cancel)
                ?? buttons.FirstOrDefault(button => button != null && button.DialogResult == System.Windows.Forms.DialogResult.No);
        }

        private static System.Windows.Forms.DialogResult NormalizeClosedResult(
            ModalRequest request,
            System.Windows.Forms.DialogResult result)
        {
            var cancelButton = FindCancelButton(request.Buttons);
            if (result == System.Windows.Forms.DialogResult.No &&
                cancelButton != null &&
                cancelButton.DialogResult != System.Windows.Forms.DialogResult.No &&
                !request.Buttons.Any(button => button != null && button.DialogResult == System.Windows.Forms.DialogResult.No))
            {
                return cancelButton.DialogResult;
            }

            return result;
        }
    }
}
