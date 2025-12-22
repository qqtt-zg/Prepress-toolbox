using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp3.Forms.Panels
{
    /// <summary>
    /// 面板控件基类 - 所有功能面板的抽象基类
    /// 提供面板生命周期管理和通用功能
    /// </summary>
    public abstract class BasePanelControl : UserControl
    {
        /// <summary>
        /// 面板唯一标识键
        /// </summary>
        public abstract string PanelKey { get; }
        
        /// <summary>
        /// 面板显示名称
        /// </summary>
        public abstract string DisplayName { get; }
        
        /// <summary>
        /// 面板图标（可选）
        /// </summary>
        public virtual string IconName { get; } = "FileOutlined";
        
        /// <summary>
        /// 是否已初始化
        /// </summary>
        protected bool IsInitialized { get; private set; }
        
        /// <summary>
        /// 是否当前处于激活状态
        /// </summary>
        public bool IsActive { get; private set; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        protected BasePanelControl()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!IsInitialized)
            {
                OnActivated();
            }
        }
        
        /// <summary>
        /// 面板被激活时调用（切换到此面板时）
        /// </summary>
        public virtual void OnActivated()
        {
            IsActive = true;
            
            // 首次激活时进行初始化（懒加载）
            if (!IsInitialized)
            {
                InitializePanel();
                IsInitialized = true;
            }
            
            // 执行激活后的刷新操作
            OnRefresh();
        }
        
        /// <summary>
        /// 面板被停用时调用（切换到其他面板时）
        /// </summary>
        public virtual void OnDeactivated()
        {
            IsActive = false;
            
            // 保存面板状态
            SavePanelState();
        }
        
        /// <summary>
        /// 初始化面板（仅在首次激活时调用）
        /// 子类应重写此方法进行初始化
        /// </summary>
        protected virtual void InitializePanel()
        {
            // 子类重写以进行初始化
        }
        
        /// <summary>
        /// 刷新面板数据
        /// </summary>
        protected virtual void OnRefresh()
        {
            // 子类重写以刷新数据
        }
        
        /// <summary>
        /// 保存面板状态
        /// </summary>
        protected virtual void SavePanelState()
        {
            // 子类重写以保存状态
        }
        
        /// <summary>
        /// 获取主窗体引用
        /// </summary>
        protected Main.MainShellForm GetMainShellForm()
        {
            return this.FindForm() as Main.MainShellForm;
        }
        
        /// <summary>
        /// 更新主窗体状态栏
        /// </summary>
        protected void UpdateMainStatus(string message)
        {
            var mainForm = GetMainShellForm();
            mainForm?.UpdateStatus(message);
        }
        
        /// <summary>
        /// 显示加载遮罩
        /// </summary>
        protected void ShowLoading(string message = "加载中...")
        {
            // TODO: 实现加载遮罩
        }
        
        /// <summary>
        /// 隐藏加载遮罩
        /// </summary>
        protected void HideLoading()
        {
            // TODO: 实现加载遮罩隐藏
        }
        
        /// <summary>
        /// 显示提示消息
        /// </summary>
        protected void ShowMessage(string message, string title = "提示")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// 显示错误消息
        /// </summary>
        protected void ShowError(string message, string title = "错误")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        
        /// <summary>
        /// 显示确认对话框
        /// </summary>
        protected bool ShowConfirm(string message, string title = "确认")
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
    }
}
