using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WindowsFormsApp3.Forms.Dialogs;
using WindowsFormsApp3.EventArguments;


namespace WindowsFormsApp3.Controls
{
    /// <summary>
    /// 支持预设管理的事件分组 TreeView
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    public class EventGroupsTreeView : UserControl
    {
        public TreeView treeView;
        public Panel buttonPanel;
        public Button btnSavePreset;
        public Button btnDeletePreset;
        public Button btnSaveAsPreset;
        public AntdUI.Select cboPresets;
        public Label lblPresets;

        // 保留状态功能相关
        private ContextMenuStrip _nodeContextMenu;
        private ToolStripMenuItem _miPreserveGroup;
        private ToolStripMenuItem _miClearAllPreserve;

        // 兼容性图标配置
        private static readonly string PRESERVE_ICON = "[保留]";  // Windows 7兼容
        private static readonly string ITEM_ICON = "[*]";          // 项目保留标识

        // 保留状态颜色配置
        private Color _preserveColor = Color.Blue;  // 默认蓝色（浅色模式）

        public TreeView TreeView => treeView;
        
        /// <summary>
        /// 应用主题
        /// </summary>
        public void ApplyTheme(bool isDark)
        {
            // 1. 设置保留颜色
            _preserveColor = isDark ? Color.CornflowerBlue : Color.Blue;

            // 2. 设置 TreeView 颜色
            var backColor = isDark ? Color.FromArgb(30, 30, 30) : Color.White;
            var foreColor = isDark ? Color.FromArgb(220, 220, 220) : Color.Black;
            
            treeView.BackColor = backColor;
            treeView.ForeColor = foreColor;

            // 3. 设置底部面板颜色
            buttonPanel.BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.FromArgb(248, 248, 248);
            lblPresets.ForeColor = foreColor;

            // 4. 设置按钮样式
            ApplyButtonTheme(btnSavePreset, isDark);
            ApplyButtonTheme(btnDeletePreset, isDark);
            ApplyButtonTheme(btnSaveAsPreset, isDark);
            
            // 5. 设置下拉框样式
            // 5. 设置下拉框样式 - AntdUI 控件通常会自动适配或由 ThemeHelper 处理
            // cboPresets.BackColor = isDark ? Color.FromArgb(60, 60, 60) : Color.White;
            // cboPresets.ForeColor = foreColor;
            // cboPresets.FlatStyle = FlatStyle.Flat;

            // 6. 刷新节点颜色（应用新的保留颜色）
            RefreshPreserveVisuals();
        }

        private void ApplyButtonTheme(Button btn, bool isDark)
        {
            if (btn == null) return;
            
            btn.BackColor = isDark ? Color.FromArgb(60, 60, 60) : Color.White;
            btn.ForeColor = isDark ? Color.FromArgb(220, 220, 220) : Color.Black;
            btn.FlatAppearance.BorderColor = isDark ? Color.FromArgb(80, 80, 80) : Color.Silver;
        }

        // 调试辅助方法 - 在运行时获取按钮信息
        public Button GetSavePresetButton() => btnSavePreset;
        public Button GetDeletePresetButton() => btnDeletePreset;
        public Button GetSaveAsPresetButton() => btnSaveAsPreset;
        public AntdUI.Select GetPresetsComboBox() => cboPresets;
        public Panel GetButtonPanel() => buttonPanel;

        // 调试方法：检查拖拽状态
        public void LogDragDropStatus()
        {
            System.Diagnostics.Debug.WriteLine($"[LogDragDropStatus] TreeView状态检查:");
            System.Diagnostics.Debug.WriteLine($"  - TreeView: {(treeView != null ? "存在" : "null")}");
            System.Diagnostics.Debug.WriteLine($"  - Name: {treeView?.Name}");
            System.Diagnostics.Debug.WriteLine($"  - AllowDrop: {treeView?.AllowDrop}");
            System.Diagnostics.Debug.WriteLine($"  - 节点数量: {treeView?.Nodes?.Count ?? 0}");

            if (treeView?.Nodes != null)
            {
                for (int i = 0; i < treeView.Nodes.Count; i++)
                {
                    System.Diagnostics.Debug.WriteLine($"  - 节点[{i}]: {treeView.Nodes[i].Text} (子节点: {treeView.Nodes[i].Nodes.Count})");
                }
            }

            // 测试触发一个简单的拖拽事件检查
            System.Diagnostics.Debug.WriteLine("  - 尝试手动触发拖拽测试...");
            try
            {
                if (treeView?.Nodes?.Count > 0)
                {
                    var testNode = treeView.Nodes[0];
                    System.Diagnostics.Debug.WriteLine($"  - 测试节点: {testNode?.Text}, Tag类型: {testNode?.Tag?.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"  - 测试异常: {ex.Message}");
            }
        }

        // 运行时获取所有按钮的边界矩形，便于调试
        public Rectangle GetSavePresetBounds() => btnSavePreset?.Bounds ?? Rectangle.Empty;
        public Rectangle GetDeletePresetBounds() => btnDeletePreset?.Bounds ?? Rectangle.Empty;
        public Rectangle GetSaveAsPresetBounds() => btnSaveAsPreset?.Bounds ?? Rectangle.Empty;

        public event EventHandler PresetSaveAs;
        public event EventHandler PresetSaved;
        public event EventHandler PresetLoaded;
        public event EventHandler PresetDeleted;

        // 保留状态变化事件
        public event EventHandler<PreserveStateChangedEventArgs> PreserveStateChanged;

        // 配置保存请求事件
        public event EventHandler ConfigurationSaveRequested;

        // 拖拽状态跟踪
        private TreeNode _draggedNode;
        private TreeNode _originalParentNode; // 记录原始父分组

        public EventGroupsTreeView()
        {
            InitializeComponent();
            SetupTreeView();
            InitializeContextMenu();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // 设置 UserControl 属性
            this.Size = new Size(320, 480);  // 增加整体宽度以适应更宽的TreeView

            // 创建 TreeView
            treeView = new TreeView
            {
                AllowDrop = true,
                CheckBoxes = true,
                Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134),
                FullRowSelect = true,
                HideSelection = false,
                Location = new Point(0, 0),
                Name = "treeViewEvents",
                RightToLeft = RightToLeft.No,
                Size = new Size(320, 335),  // 增加宽度以完整显示保留分组文本
                TabIndex = 0,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // 创建按钮面板（优化布局以容纳按钮）
            buttonPanel = new Panel
            {
                Location = new Point(0, 340),
                Size = new Size(320, 105), // 调整宽度和高度以适应新的TreeView宽度
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(248, 248, 248), // 更柔和的背景色
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Padding = new Padding(0, 0, 0, 5) // 添加底部内边距
            };

            // 预设标签
            lblPresets = new Label
            {
                Text = "预设方案:",
                Font = new Font("微软雅黑", 8F, FontStyle.Bold, GraphicsUnit.Point, 134),
                Location = new Point(6, 8),
                Size = new Size(60, 16),
                AutoSize = false
            };

            // 预设下拉框
            cboPresets = new AntdUI.Select
            {
                List = true, // 下拉列表模式
                Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134), // Slightly larger font
                Location = new Point(72, 2),
                Size = new Size(230, 30), // Increased width and height
                TabIndex = 1,
                PlaceholderText = "选择预设"
            };
            cboPresets.SelectedIndexChanged += CboPresets_SelectedIndexChanged;

            // 第一行按钮：保存当前预设、删除预设
            btnSavePreset = new Button
            {
                Text = "保存当前预设",
                Font = new Font("微软雅黑", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 134),
                Location = new Point(6, 35),
                Size = new Size(90, 24), // 增加宽度以完整显示文本
                TabIndex = 2,
                UseVisualStyleBackColor = true,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnSavePreset.Click += BtnSavePreset_Click;

            btnDeletePreset = new Button
            {
                Text = "删除预设",
                Font = new Font("微软雅黑", 8F, FontStyle.Regular, GraphicsUnit.Point, 134),
                Location = new Point(100, 35), // 调整位置以适应新宽度
                Size = new Size(80, 24),  // 略微缩小以平衡布局
                TabIndex = 3,
                UseVisualStyleBackColor = true,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnDeletePreset.Click += BtnDeletePreset_Click;

            // 第二行按钮：另存预设方案（占据第一行三个按钮的位置）
            btnSaveAsPreset = new Button
            {
                Text = "另存预设方案",
                Font = new Font("微软雅黑", 7.5F, FontStyle.Regular, GraphicsUnit.Point, 134),
                Location = new Point(6, 68),
                Size = new Size(174, 24), // 保持全宽
                TabIndex = 5,
                UseVisualStyleBackColor = true,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnSaveAsPreset.Click += BtnSaveAsPreset_Click;

            // 添加控件到面板
            buttonPanel.Controls.AddRange(new Control[] {
                lblPresets, cboPresets, btnSavePreset, btnDeletePreset, btnSaveAsPreset
            });

            // 添加控件到 UserControl
            this.Controls.AddRange(new Control[] { treeView, buttonPanel });

            this.ResumeLayout(false);
        }

        /// <summary>
        /// 初始化右键菜单
        /// </summary>
        private void InitializeContextMenu()
        {
            _nodeContextMenu = new ContextMenuStrip();

            // 项目保留设置
            _miPreserveGroup = new ToolStripMenuItem("设为保留分组");
            _miPreserveGroup.Click += (s, e) => ToggleGroupPreserve();
            _nodeContextMenu.Items.Add(_miPreserveGroup);

            _nodeContextMenu.Items.Add(new ToolStripSeparator());

            // 清除所有保留
            _miClearAllPreserve = new ToolStripMenuItem("清除所有保留");
            _miClearAllPreserve.Click += (s, e) => ClearAllPreserve();
            _nodeContextMenu.Items.Add(_miClearAllPreserve);

            // 绑定TreeView鼠标事件
            treeView.MouseDown += TreeView_MouseDown;
        }

        private void SetupTreeView()
        {
            // 启用拖拽功能
            treeView.AllowDrop = true;

            System.Diagnostics.Debug.WriteLine($"[SetupTreeView] 开始绑定拖拽事件，TreeView={treeView?.Name}, AllowDrop={treeView?.AllowDrop}");

            treeView.ItemDrag += TreeView_ItemDrag;
            treeView.DragEnter += TreeView_DragEnter;
            treeView.DragDrop += TreeView_DragDrop;
            
            // 添加节点勾选事件处理
            treeView.AfterCheck += TreeView_AfterCheck;

            System.Diagnostics.Debug.WriteLine("[SetupTreeView] 拖拽事件和勾选事件绑定完成");
        }

        /// <summary>
        /// TreeView节点勾选事件处理
        /// </summary>
        private void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;

            var nodeData = e.Node.Tag as TreeNodeData;
            if (nodeData == null) return;

            // 更新节点数据
            nodeData.IsEnabled = e.Node.Checked;

            if (nodeData.NodeType == TreeNodeType.Group)
            {
                // 分组节点：处理分组与子项目的联动
                HandleGroupCheckChanged(e.Node, nodeData, e.Node.Checked);
            }
            else if (nodeData.NodeType == TreeNodeType.Item)
            {
                // 项目节点：处理单个项目状态变化
                HandleItemCheckChanged(e.Node, nodeData, e.Node.Checked);
            }

            // 触发配置保存事件
            ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 处理分组勾选状态变化
        /// </summary>
        private void HandleGroupCheckChanged(TreeNode groupNode, TreeNodeData groupData, bool isChecked)
        {
            if (!isChecked)
            {
                // 分组取消勾选时，强制取消分组内的所有子节点
                foreach (TreeNode childNode in groupNode.Nodes)
                {
                    if (childNode.Tag is TreeNodeData childData)
                    {
                        childNode.Checked = false;
                        childData.IsEnabled = false;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"[分组勾选] 分组 '{groupData.Group}' 取消勾选，已同时取消所有子项目");
            }
            else
            {
                // 分组勾选时，可以选择不同的策略：
                // 策略1：保持子项目原有状态（当前实现）
                // 策略2：同时勾选所有子项目（可选）
                // 策略3：询问用户是否同时勾选子项目（可选）
                
                // 这里采用策略1：保持子项目原有状态，让用户自己决定
                System.Diagnostics.Debug.WriteLine($"[分组勾选] 分组 '{groupData.Group}' 已勾选，子项目保持原有状态");
                
                // 如果需要同时勾选所有子项目，可以取消注释下面的代码：
                /*
                foreach (TreeNode childNode in groupNode.Nodes)
                {
                    if (childNode.Tag is TreeNodeData childData)
                    {
                        childNode.Checked = true;
                        childData.IsEnabled = true;
                    }
                }
                */
            }
        }

        /// <summary>
        /// 处理项目勾选状态变化
        /// </summary>
        private void HandleItemCheckChanged(TreeNode itemNode, TreeNodeData itemData, bool isChecked)
        {
            // 项目状态变化时，检查父分组的状态
            var parentNode = itemNode.Parent;
            if (parentNode?.Tag is TreeNodeData parentData && parentData.NodeType == TreeNodeType.Group)
            {
                // 如果项目被勾选，但父分组未勾选，可以选择不同的策略：
                // 策略1：允许子项目独立于父分组状态（当前实现）
                // 策略2：自动勾选父分组
                // 策略3：提示用户是否勾选父分组
                
                if (isChecked && !parentNode.Checked)
                {
                    // 这里采用策略1：允许子项目独立状态
                    System.Diagnostics.Debug.WriteLine($"[项目勾选] 项目 '{itemData.ItemName}' 已勾选，但父分组 '{parentData.Group}' 未勾选");
                }
                else if (!isChecked)
                {
                    System.Diagnostics.Debug.WriteLine($"[项目勾选] 项目 '{itemData.ItemName}' 已取消勾选");
                }
            }
        }

        /// <summary>
        /// TreeView鼠标事件处理
        /// </summary>
        private void TreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var clickedNode = treeView.GetNodeAt(e.X, e.Y);
                if (clickedNode != null && clickedNode.Tag is TreeNodeData nodeData)
                {
                    // 选中节点
                    treeView.SelectedNode = clickedNode;

                    // 更新菜单状态
                    UpdateContextMenuState(clickedNode, nodeData);

                    // 显示右键菜单
                    _nodeContextMenu.Show(treeView, e.X, e.Y);
                }
            }
        }

        /// <summary>
        /// 更新右键菜单状态
        /// </summary>
        private void UpdateContextMenuState(TreeNode node, TreeNodeData nodeData)
        {
            if (nodeData.NodeType == TreeNodeType.Item)
            {
                // 项目节点不支持设置保留
                _miPreserveGroup.Text = "请右键点击分组设置保留";
                _miPreserveGroup.Enabled = false;
            }
            else if (nodeData.NodeType == TreeNodeType.Group)
            {
                // 分组节点可以设置保留状态
                _miPreserveGroup.Text = nodeData.IsPreserved ? "取消保留分组" : "设为保留分组";
                _miPreserveGroup.Enabled = true;
            }

            // 检查是否有保留的项目
            _miClearAllPreserve.Enabled = HasAnyPreservedItems();
        }

        /// <summary>
        /// 检查是否有保留的分组
        /// </summary>
        private bool HasAnyPreservedItems()
        {
            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Tag is TreeNodeData nodeData &&
                    nodeData.NodeType == TreeNodeType.Group)
                {
                    // 检查该分组下的子节点
                    foreach (TreeNode childNode in node.Nodes)
                    {
                        if (childNode.Tag is TreeNodeData childData &&
                            childData.NodeType == TreeNodeType.Item &&
                            childData.IsPreserved)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 切换分组保留状态
        /// </summary>
        private void ToggleGroupPreserve()
        {
            var selectedNode = treeView.SelectedNode;
            if (selectedNode?.Tag is TreeNodeData nodeData)
            {
                // 只允许分组节点设置保留状态
                if (nodeData.NodeType != TreeNodeType.Group)
                {
                    MessageBox.Show("请右键点击分组来设置保留状态。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 切换分组保留状态
                bool isSettingPreserve = !nodeData.IsPreserved;

                if (isSettingPreserve)
                {
                    // 设置分组为保留状态，自动选择或确保组内只有一个项目
                    SetGroupAsPreserved(selectedNode, nodeData);
                }
                else
                {
                    // 取消分组保留状态，清除组内所有项目的保留状态
                    CancelGroupPreserve(selectedNode, nodeData);
                }

                // 触发配置保存事件，让父窗体处理保存
                ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 设置分组为保留状态，自动确保组内只有一个保留项目
        /// </summary>
        /// <param name="groupNode">分组节点</param>
        /// <param name="groupData">分组数据</param>
        private void SetGroupAsPreserved(TreeNode groupNode, TreeNodeData groupData)
        {
            // 检查组内是否已有保留项目
            var existingPreservedItems = new List<TreeNode>();
            foreach (TreeNode childNode in groupNode.Nodes)
            {
                if (childNode.Tag is TreeNodeData childData &&
                    childData.NodeType == TreeNodeType.Item &&
                    childData.IsPreserved)
                {
                    existingPreservedItems.Add(childNode);
                }
            }

            // 如果没有保留项目，让用户选择
            if (!existingPreservedItems.Any())
            {
                if (groupNode.Nodes.Count > 1)
                {
                    // 多个项目时显示选择对话框
                    var selectedItem = ShowItemSelectionDialog(groupNode, groupData);
                    if (selectedItem != null)
                    {
                        if (selectedItem.Tag is TreeNodeData selectedData)
                        {
                            selectedData.IsPreserved = true;
                            UpdateItemNodeVisual(selectedItem, true);

                            // 将其他项目移动到"未分组"
                            var itemsToMove = new List<TreeNode>();
                            foreach (TreeNode childNode in groupNode.Nodes)
                            {
                                if (childNode != selectedItem && childNode.Tag is TreeNodeData childData)
                                {
                                    itemsToMove.Add(childNode);
                                    // 更新项目的分组信息为未分组
                                    childData.Group = EventGroup.Ungrouped;
                                }
                            }

                            // 找到"未分组"节点
                            TreeNode ungroupedNode = null;
                            foreach (TreeNode node in treeView.Nodes)
                            {
                                if (node.Tag is TreeNodeData nodeData && nodeData.Group == EventGroup.Ungrouped)
                                {
                                    ungroupedNode = node;
                                    break;
                                }
                            }

                            // 移动项目到"未分组"
                            if (ungroupedNode != null)
                            {
                                foreach (var item in itemsToMove)
                                {
                                    selectedItem.Parent.Nodes.Remove(item);
                                    ungroupedNode.Nodes.Add(item);
                                }
                            }

                            // 触发保留状态变化事件
                            PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
                            {
                                Group = groupData.Group.Value,
                                IsPreserved = true,
                                ItemName = selectedData.ItemName,
                                Source = "用户手动选择"
                            });

                            // 触发配置保存事件（因为项目移动了）
                            ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        // 用户取消了选择，不设置分组保留
                        MessageBox.Show("已取消设置保留分组。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                else if (groupNode.Nodes.Count == 1)
                {
                    // 只有一个项目，自动选择
                    var firstChild = groupNode.Nodes[0];
                    if (firstChild.Tag is TreeNodeData firstChildData)
                    {
                        firstChildData.IsPreserved = true;
                        UpdateItemNodeVisual(firstChild, true);

                        // 触发保留状态变化事件
                        PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
                        {
                            Group = groupData.Group.Value,
                            IsPreserved = true,
                            ItemName = firstChildData.ItemName,
                            Source = "分组保留自动设置"
                        });
                    }
                }
            }
            // 如果有多个保留项目，只保留第一个，其他移动到未分组
            else if (existingPreservedItems.Count > 1)
            {
                var itemToKeep = existingPreservedItems[0];
                var itemsToMove = existingPreservedItems.Skip(1).ToList();

                // 找到"未分组"节点
                TreeNode ungroupedNode = null;
                foreach (TreeNode node in treeView.Nodes)
                {
                    if (node.Tag is TreeNodeData nodeData && nodeData.Group == EventGroup.Ungrouped)
                    {
                        ungroupedNode = node;
                        break;
                    }
                }

                // 移动多余的项目到"未分组"
                if (ungroupedNode != null)
                {
                    foreach (var item in itemsToMove)
                    {
                        if (item.Tag is TreeNodeData itemData)
                        {
                            // 清除保留状态
                            itemData.IsPreserved = false;
                            UpdateItemNodeVisual(item, false);

                            // 更新分组信息为未分组
                            itemData.Group = EventGroup.Ungrouped;

                            // 移动节点
                            groupNode.Nodes.Remove(item);
                            ungroupedNode.Nodes.Add(item);

                            // 触发保留状态变化事件
                            PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
                            {
                                Group = itemData.Group.Value,
                                IsPreserved = false,
                                ItemName = itemData.ItemName,
                                Source = "分组保留冲突解决"
                            });
                        }
                    }
                }

                // 触发配置保存事件（因为项目移动了）
                ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
            }

            // 设置分组保留状态
            groupData.IsPreserved = true;
            UpdateGroupNodeVisual(groupNode, true);

            // 触发分组保留状态变化事件
            PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
            {
                Group = groupData.Group.Value,
                IsPreserved = true,
                Source = "用户手动设置"
            });
        }

        /// <summary>
        /// 取消分组保留状态，清除组内所有项目的保留状态
        /// </summary>
        /// <param name="groupNode">分组节点</param>
        /// <param name="groupData">分组数据</param>
        private void CancelGroupPreserve(TreeNode groupNode, TreeNodeData groupData)
        {
            // 清除组内所有项目的保留状态
            foreach (TreeNode childNode in groupNode.Nodes)
            {
                if (childNode.Tag is TreeNodeData childData &&
                    childData.NodeType == TreeNodeType.Item &&
                    childData.IsPreserved)
                {
                    childData.IsPreserved = false;
                    UpdateItemNodeVisual(childNode, false);

                    // 触发保留状态变化事件
                    PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
                    {
                        Group = groupData.Group.Value,
                        IsPreserved = false,
                        ItemName = childData.ItemName,
                        Source = "分组保留取消"
                    });
                }
            }

            // 清除分组保留状态
            groupData.IsPreserved = false;
            UpdateGroupNodeVisual(groupNode, false);

            // 触发分组保留状态变化事件
            PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
            {
                Group = groupData.Group.Value,
                IsPreserved = false,
                Source = "用户手动设置"
            });
        }

        /// <summary>
        /// 显示项目选择对话框
        /// </summary>
        /// <param name="groupNode">分组节点</param>
        /// <param name="groupData">分组数据</param>
        /// <returns>选择的项目节点，用户取消则返回null</returns>
        private TreeNode ShowItemSelectionDialog(TreeNode groupNode, TreeNodeData groupData)
        {
            var groupName = GetGroupDisplayName(groupData.Group.Value);
            var itemNames = new List<string>();
            var itemNodes = new List<TreeNode>();

            foreach (TreeNode childNode in groupNode.Nodes)
            {
                if (childNode.Tag is TreeNodeData childData && childData.NodeType == TreeNodeType.Item)
                {
                    itemNames.Add(childData.ItemName);
                    itemNodes.Add(childNode);
                }
            }

            // 简单的选择对话框
            var message = $"设置 '{groupName}' 为保留分组\n\n" +
                         $"请选择一个项目作为保留项目：\n\n" +
                         $"{string.Join("\n", itemNames.Select((name, index) => $"{index + 1}. {name}"))}\n\n" +
                         $"请输入要选择的项目的数字（1-{itemNames.Count}）：";

            var result = Microsoft.VisualBasic.Interaction.InputBox(
                message,
                "选择保留项目",
                "1",
                -1, -1);

            if (int.TryParse(result, out int selectedIndex) &&
                selectedIndex >= 1 && selectedIndex <= itemNames.Count)
            {
                return itemNodes[selectedIndex - 1];
            }

            return null; // 用户取消或输入无效
        }

        /// <summary>
        /// 清除所有保留
        /// </summary>
        private void ClearAllPreserve()
        {
            bool hasChanges = false;

            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Tag is TreeNodeData nodeData)
                {
                    // 清除分组级别的保留状态
                    if (nodeData.NodeType == TreeNodeType.Group && nodeData.IsPreserved)
                    {
                        nodeData.IsPreserved = false;
                        UpdateGroupNodeVisual(node, false);
                        hasChanges = true;
                    }

                    // 清除所有子项目级别的保留状态
                    foreach (TreeNode childNode in node.Nodes)
                    {
                        if (childNode.Tag is TreeNodeData childData &&
                            childData.NodeType == TreeNodeType.Item &&
                            childData.IsPreserved)
                        {
                            childData.IsPreserved = false;
                            UpdateNodeVisual(childNode, childData);

                            // 触发保留状态变化事件
                            PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
                            {
                                Group = childData.Group.Value,
                                ItemName = childData.ItemName,
                                IsPreserved = false,
                                Source = "清除所有保留"
                            });

                            hasChanges = true;
                        }
                    }
                }
            }

            if (hasChanges)
            {
                // 触发配置保存事件，让父窗体处理保存
                ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 更新分组节点视觉
        /// </summary>
        private void UpdateGroupNodeVisual(TreeNode node, bool isPreserved)
        {
            if (isPreserved)
            {
                // 先移除所有可能存在的保留前缀，确保只添加一个
                var cleanText = node.Text;

                // 循环移除所有重复的保留前缀
                while (cleanText.StartsWith(PRESERVE_ICON))
                {
                    cleanText = cleanText.Substring(PRESERVE_ICON.Length).Trim();
                }

                // 重新添加保留前缀（确保格式统一，无空格）
                node.Text = $"{PRESERVE_ICON}{cleanText}";
                node.ForeColor = _preserveColor;
                node.NodeFont = new Font(treeView.Font, FontStyle.Bold | FontStyle.Italic);

                // 分组内所有项目自动继承视觉提示
                foreach (TreeNode childNode in node.Nodes)
                {
                    childNode.ForeColor = _preserveColor;
                    // 给项目节点添加保留前缀
                    if (!childNode.Text.StartsWith(ITEM_ICON))
                    {
                        childNode.Text = $"{ITEM_ICON} {childNode.Text}";
                    }
                }
            }
            else
            {
                // 恢复分组默认外观 - 确保完全移除所有保留前缀
                var cleanText = node.Text;

                // 循环移除所有保留前缀
                while (cleanText.StartsWith(PRESERVE_ICON))
                {
                    cleanText = cleanText.Substring(PRESERVE_ICON.Length).Trim();
                }

                node.Text = cleanText;
                node.ForeColor = treeView.ForeColor;
                node.NodeFont = new Font(treeView.Font, FontStyle.Bold);

                // 恢复组内项目默认外观
                foreach (TreeNode childNode in node.Nodes)
                {
                    childNode.ForeColor = treeView.ForeColor;
                    // 移除项目节点的保留前缀
                    if (childNode.Text.StartsWith(ITEM_ICON))
                    {
                        childNode.Text = childNode.Text.Substring(ITEM_ICON.Length + 1);
                    }
                }
            }
        }

        /// <summary>
        /// 刷新所有保留分组视觉
        /// </summary>
        public void RefreshPreserveVisuals()
        {
            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Tag is TreeNodeData nodeData && nodeData.NodeType == TreeNodeType.Group)
                {
                    UpdateGroupNodeVisual(node, nodeData.IsPreserved);
                }
            }
        }

        /// <summary>
        /// 更新节点视觉（支持分组和项目节点）
        /// </summary>
        /// <param name="node">要更新的节点</param>
        /// <param name="nodeData">节点数据</param>
        private void UpdateNodeVisual(TreeNode node, TreeNodeData nodeData)
        {
            if (nodeData.NodeType == TreeNodeType.Group)
            {
                UpdateGroupNodeVisual(node, nodeData.IsPreserved);
            }
            else if (nodeData.NodeType == TreeNodeType.Item)
            {
                UpdateItemNodeVisual(node, nodeData.IsPreserved);
            }
        }

        /// <summary>
        /// 根据组内项目状态更新分组节点视觉
        /// </summary>
        /// <param name="groupNode">分组节点</param>
        private void UpdateGroupNodeVisualBasedOnItems(TreeNode groupNode)
        {
            if (groupNode?.Tag is TreeNodeData groupData)
            {
                // 检查组内是否有保留项目
                bool hasPreservedItem = false;
                foreach (TreeNode childNode in groupNode.Nodes)
                {
                    if (childNode.Tag is TreeNodeData childData &&
                        childData.NodeType == TreeNodeType.Item &&
                        childData.IsPreserved)
                    {
                        hasPreservedItem = true;
                        break;
                    }
                }

                // 更新分组节点视觉
                UpdateGroupNodeVisual(groupNode, hasPreservedItem);
            }
        }

        /// <summary>
        /// 更新项目节点视觉
        /// </summary>
        /// <param name="node">项目节点</param>
        /// <param name="isPreserved">是否保留</param>
        private void UpdateItemNodeVisual(TreeNode node, bool isPreserved)
        {
            if (isPreserved)
            {
                // 项目显示保留标识和蓝色
                if (!node.Text.StartsWith(ITEM_ICON))
                {
                    node.Text = $"{ITEM_ICON} {node.Text}";
                }
                node.ForeColor = _preserveColor;
            }
            else
            {
                // 移除项目节点的保留前缀
                if (node.Text.StartsWith(ITEM_ICON))
                {
                    node.Text = node.Text.Substring(ITEM_ICON.Length + 1);
                }
                node.ForeColor = treeView.ForeColor;
            }
        }

        /// <summary>
        /// 检查并自动清理启动时的保留项目冲突
        /// </summary>
        /// <returns>是否发现并处理了冲突</returns>
        public bool CheckAndResolveStartupConflicts()
        {
            // 基于分组的保留不需要检查冲突
            // 每个分组可以独立设置保留状态
            return false;
        }

        /// <summary>
        /// 获取当前已保留的分组列表
        /// </summary>
        /// <returns>已保留的分组节点列表</returns>
        private List<TreeNode> GetExistingPreservedGroups()
        {
            var preservedGroups = new List<TreeNode>();
            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Tag is TreeNodeData nodeData &&
                    nodeData.NodeType == TreeNodeType.Group &&
                    nodeData.IsPreserved)
                {
                    preservedGroups.Add(node);
                }
            }
            return preservedGroups;
        }

        /// <summary>
        /// 显示保留冲突对话框
        /// </summary>
        /// <param name="existingGroups">现有的保留分组</param>
        /// <param name="newNodeData">新要保留的分组数据</param>
        /// <returns>用户是否确认继续</returns>
        private bool ShowPreserveConflictDialog(List<TreeNode> existingGroups, TreeNodeData newNodeData)
        {
            var existingGroupNames = existingGroups.Select(n =>
                n.Text.Replace(PRESERVE_ICON + " ", "")).ToList();

            var groupName = GetGroupDisplayName(newNodeData.Group.Value);
          var message = $"检测到保留分组冲突！\n\n" +
                         $"以下分组已经设置为保留：\n" +
                         $"{string.Join("、", existingGroupNames)}\n\n" +
                         $"根据设计原则，为确保数据备份位置准确，系统只允许设置一个保留分组。\n\n" +
                         $"是否要清除现有保留状态，将 '{groupName}' 设置为新的保留分组？\n\n" +
                         $"• 点击【是】：清除现有保留状态，设置新保留分组\n" +
                         $"• 点击【否】：取消操作，保持现有设置";

            var result = MessageBox.Show(
                message,
                "保留分组冲突提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// 清除指定分组的保留状态
        /// </summary>
        /// <param name="preservedGroups">要清除保留状态的分组列表</param>
        private void ClearAllPreservedGroups(List<TreeNode> preservedGroups)
        {
            foreach (var node in preservedGroups)
            {
                if (node.Tag is TreeNodeData nodeData)
                {
                    nodeData.IsPreserved = false;
                    UpdateGroupNodeVisual(node, false);

                    // 触发保留状态变化事件
                    PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
                    {
                        Group = nodeData.Group.Value,
                        IsPreserved = false,
                        Source = "冲突解决"
                    });
                }
            }
        }

        /// <summary>
        /// 获取同一分组内已保留的项目列表
        /// </summary>
        /// <param name="group">指定分组</param>
        /// <returns>同一分组内已保留的项目节点列表</returns>
        private List<TreeNode> GetExistingPreservedItemsInGroup(EventGroup? group)
        {
            var preservedItems = new List<TreeNode>();
            if (group == null) return preservedItems;

            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Tag is TreeNodeData nodeData &&
                    nodeData.NodeType == TreeNodeType.Group &&
                    nodeData.Group == group)
                {
                    // 检查该分组下的子节点
                    foreach (TreeNode childNode in node.Nodes)
                    {
                        if (childNode.Tag is TreeNodeData childData &&
                            childData.NodeType == TreeNodeType.Item &&
                            childData.IsPreserved)
                        {
                            preservedItems.Add(childNode);
                        }
                    }
                }
            }
            return preservedItems;
        }

        /// <summary>
        /// 显示项目保留冲突对话框
        /// </summary>
        /// <param name="existingItems">现有的保留项目</param>
        /// <param name="newNodeData">新要保留的项目数据</param>
        /// <returns>用户是否确认继续</returns>
        private bool ShowItemPreserveConflictDialog(List<TreeNode> existingItems, TreeNodeData newNodeData)
        {
            var existingItemNames = existingItems.Select(n => n.Text.Replace(ITEM_ICON + " ", "")).ToList();
            var groupName = GetGroupDisplayName(newNodeData.Group.Value);

            var message = $"检测到保留项目冲突！\n\n" +
                         $"在 '{groupName}' 分组中，以下项目已经设置为保留：\n" +
                         $"{string.Join("、", existingItemNames)}\n\n" +
                         $"根据设计原则，每个分组内只能保留一个项目以确保数据备份位置准确。\n\n" +
                         $"是否要清除现有保留状态，将 '{newNodeData.ItemName}' 设置为新的保留项目？\n\n" +
                         $"• 点击【是】：清除现有保留状态，设置新保留项目\n" +
                         $"• 点击【否】：取消操作，保持现有设置";

            var result = MessageBox.Show(
                message,
                "保留项目冲突提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2
            );

            return result == DialogResult.Yes;
        }

        /// <summary>
        /// 获取分组的显示名称
        /// </summary>
        /// <param name="group">分组枚举</param>
        /// <returns>显示名称</returns>
        private string GetGroupDisplayName(EventGroup group)
        {
            return group switch
            {
                EventGroup.Order => "订单组",
                EventGroup.Material => "材料组",
                EventGroup.Quantity => "数量组",
                EventGroup.Process => "工艺组",
                EventGroup.Customer => "客户组",
                EventGroup.Remark => "备注组",
                EventGroup.Row => "行数组",
                EventGroup.Column => "列数组",
                EventGroup.Ungrouped => "未分组",
                _ => group.ToString()
            };
        }

        /// <summary>
        /// 清除同一分组内的保留项目
        /// </summary>
        /// <param name="preservedItems">要清除保留状态的项目列表</param>
        private void ClearPreservedItemsInGroup(List<TreeNode> preservedItems)
        {
            foreach (var node in preservedItems)
            {
                if (node.Tag is TreeNodeData nodeData)
                {
                    nodeData.IsPreserved = false;
                    UpdateNodeVisual(node, nodeData);

                    // 触发保留状态变化事件
                    PreserveStateChanged?.Invoke(this, new PreserveStateChangedEventArgs
                    {
                        Group = nodeData.Group.Value,
                        ItemName = nodeData.ItemName,
                        IsPreserved = false,
                        Source = "冲突解决"
                    });
                }
            }
        }

        /// <summary>
        /// 设置自定义保留图标（用于兼容不同系统）
        /// </summary>
        /// <param name="preserveIcon">分组保留图标</param>
        /// <param name="itemIcon">项目保留图标</param>
        public static void SetPreserveIcons(string preserveIcon = "[保留]", string itemIcon = "[*]")
        {
            // 注意：这些是静态常量，如果需要动态修改，需要改为实例字段
            // 目前使用编译时常量以确保兼容性
        }

        /// <summary>
        /// 获取当前使用的保留图标和颜色信息
        /// </summary>
        /// <returns>图标和颜色信息元组</returns>
        public (string PreserveIcon, string ItemIcon, Color PreserveColor) GetPreserveIcons()
        {
            return (PRESERVE_ICON, ITEM_ICON, _preserveColor);
        }

        /// <summary>
        /// 获取保留分组的颜色
        /// </summary>
        /// <returns>保留分组颜色</returns>
        public Color GetPreserveColor()
        {
            return _preserveColor;
        }

        private void BtnSavePreset_Click(object sender, EventArgs e)
        {
            PresetSaved?.Invoke(this, EventArgs.Empty);
        }

    
        private void BtnDeletePreset_Click(object sender, EventArgs e)
        {
            if (cboPresets.SelectedValue != null)
            {
                PresetDeleted?.Invoke(this, EventArgs.Empty);
            }
        }

private void BtnSaveAsPreset_Click(object sender, EventArgs e)
        {
            PresetSaveAs?.Invoke(this, EventArgs.Empty);
        }

        private void CboPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 当选择改变时，自动加载预设并更新按钮状态
            if (cboPresets.SelectedValue != null)
            {
                PresetLoaded?.Invoke(this, EventArgs.Empty);
            }
            SetPresetButtonsEnabled(true);
        }

        public void RefreshPresets(List<string> presetNames)
        {
            cboPresets.Items.Clear();
            foreach (var name in presetNames)
            {
                cboPresets.Items.Add(name);
            }

            // 不自动选择预设，让调用方控制选择
            // 这避免了在初始化时触发不必要的事件
            SetPresetButtonsEnabled(false);
        }

        public string SelectedPreset
        {
            get => cboPresets.SelectedValue?.ToString();
            set
            {
                if (value != null && cboPresets.Items.Contains(value))
                {
                    cboPresets.SelectedValue = value;
                }
            }
        }

        public void SetPresetButtonsEnabled(bool enabled)
        {
            // 内置预设不能删除
            var builtinPresets = new[] { "全功能配置" };
            var selectedItem = cboPresets.SelectedValue?.ToString();
            btnDeletePreset.Enabled = enabled && cboPresets.Items.Count > 0 &&
                                      cboPresets.SelectedValue != null &&
                                      !Array.Exists(builtinPresets, preset => preset == selectedItem);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 调整控件大小以适应新的尺寸
            if (treeView != null)
            {
                treeView.Size = new Size(this.Width, this.Height - 140);
            }
            if (buttonPanel != null)
            {
                buttonPanel.Location = new Point(0, treeView.Height);
                buttonPanel.Size = new Size(this.Width, 140);
            }
        }

        /// <summary>
        /// 开始拖拽项目或分组
        /// </summary>
        private void TreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[ItemDrag] 拖拽事件触发: 拖拽节点={e.Item?.GetType().Name}");

            var node = e.Item as TreeNode;
            if (node?.Tag is TreeNodeData nodeData)
            {
                // 支持拖拽项目节点和分组节点
                if (nodeData.NodeType == TreeNodeType.Item)
                {
                    System.Diagnostics.Debug.WriteLine($"[ItemDrag] 开始拖拽项目: {node.Text} (分组: {nodeData.Group})");

                    // 只允许拖拽项目节点
                    _draggedNode = node;
                    _originalParentNode = node.Parent; // 记录原始父分组

                    System.Diagnostics.Debug.WriteLine($"[ItemDrag] 调用 DoDragDrop 开始拖拽操作...");
                    treeView.DoDragDrop(node, DragDropEffects.Move);
                    System.Diagnostics.Debug.WriteLine($"[ItemDrag] DoDragDrop 调用完成");
                }
                else if (nodeData.NodeType == TreeNodeType.Group)
                {
                    System.Diagnostics.Debug.WriteLine($"[ItemDrag] 开始拖拽分组: {node.Text} (分组: {nodeData.Group})");

                    // 允许拖拽分组节点进行排序
                    _draggedNode = node;
                    _originalParentNode = node.Parent; // 记录原始位置

                    System.Diagnostics.Debug.WriteLine($"[ItemDrag] 调用 DoDragDrop 开始分组排序拖拽操作...");
                    treeView.DoDragDrop(node, DragDropEffects.Move);
                    System.Diagnostics.Debug.WriteLine($"[ItemDrag] 分组排序 DoDragDrop 调用完成");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ItemDrag] 不支持的节点类型拖拽: {nodeData.NodeType}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[ItemDrag] 拖拽被忽略: 节点类型={node?.Tag?.GetType().Name}, 节点文本={node?.Text}");
            }
        }

        /// <summary>
        /// 拖拽进入时检查冲突和排序有效性
        /// </summary>
        private void TreeView_DragEnter(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[DragEnter] 拖拽进入事件触发: e.Data.GetDataPresent={e.Data.GetDataPresent(typeof(TreeNode))}, e.Effect={e.Effect}");

            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                var draggedNode = e.Data.GetData(typeof(TreeNode)) as TreeNode;
                var targetPoint = treeView.PointToClient(new Point(e.X, e.Y));
                var targetNode = treeView.GetNodeAt(targetPoint);

                System.Diagnostics.Debug.WriteLine($"[DragEnter] 拖拽数据解析: 拖拽节点={draggedNode?.Text}, 目标节点={targetNode?.Text}");

                // 处理分组节点排序
                if (draggedNode?.Tag is TreeNodeData draggedData &&
                    draggedData.NodeType == TreeNodeType.Group)
                {
                    // 分组排序：不允许拖拽到自己身上
                    if (draggedNode == targetNode)
                    {
                        e.Effect = DragDropEffects.None;
                        System.Diagnostics.Debug.WriteLine($"[DragEnter] 分组排序：不允许拖拽到自己");
                        return;
                    }

                    // 检查目标是否为根级分组节点或空白区域
                    if (targetNode?.Tag is TreeNodeData targetData && targetData.NodeType == TreeNodeType.Group)
                    {
                        e.Effect = DragDropEffects.Move;
                        System.Diagnostics.Debug.WriteLine($"[DragEnter] 分组排序有效：将 '{draggedData.Group.Value}' 排序到 '{targetData.Group.Value}' 附近");
                        return;
                    }

                    // 如果目标为null，可能是在空白区域拖拽，也允许
                    if (targetNode == null)
                    {
                        e.Effect = DragDropEffects.Move;
                        System.Diagnostics.Debug.WriteLine($"[DragEnter] 分组排序有效：拖拽到空白区域");
                        return;
                    }
                }

                // 处理项目节点拖拽
                if (draggedNode?.Tag is TreeNodeData draggedItemData &&
                    draggedItemData.NodeType == TreeNodeType.Item)
                {
                    // 情况1：拖拽到分组节点（跨分组移动）
                    if (targetNode?.Tag is TreeNodeData itemTargetData && itemTargetData.NodeType == TreeNodeType.Group)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DragEnter] 跨分组拖拽验证: 拖拽项目={draggedItemData.ItemName}, 目标分组={itemTargetData.Group}, 是否保留={itemTargetData.IsPreserved}");

                        // 检查目标是否为保留分组
                        if (itemTargetData.IsPreserved)
                        {
                            // 检查保留分组内是否已有任何项目
                            bool hasExistingItem = false;
                            var existingItemName = "";
                            foreach (TreeNode childNode in targetNode.Nodes)
                            {
                                if (childNode.Tag is TreeNodeData childData &&
                                    childData.NodeType == TreeNodeType.Item)
                                {
                                    hasExistingItem = true;
                                    existingItemName = childData.ItemName;
                                    break;
                                }
                            }

                            if (hasExistingItem)
                            {
                                // 允许拖拽继续，但标记为冲突状态
                                e.Effect = DragDropEffects.Move;
                                System.Diagnostics.Debug.WriteLine($"[DragEnter] 检测到保留分组 '{itemTargetData.Group.Value}' 已有项目 '{existingItemName}'，标记为冲突状态");
                                return;
                            }
                        }

                        e.Effect = DragDropEffects.Move;
                        System.Diagnostics.Debug.WriteLine($"[DragEnter] 跨分组拖拽有效");
                        return;
                    }

                    // 情况2：拖拽到同分组内的其他项目节点（分组内排序）
                    if (targetNode?.Tag is TreeNodeData targetItemData && targetItemData.NodeType == TreeNodeType.Item)
                    {
                        // 检查是否在同一个分组内
                        if (draggedNode.Parent == targetNode.Parent)
                        {
                            e.Effect = DragDropEffects.Move;
                            System.Diagnostics.Debug.WriteLine($"[DragEnter] 分组内排序有效: 拖拽项目={draggedItemData.ItemName}, 目标项目={targetItemData.ItemName}");
                            return;
                        }
                        else
                        {
                            // 不同分组间的项目拖拽，不允许
                            e.Effect = DragDropEffects.None;
                            System.Diagnostics.Debug.WriteLine($"[DragEnter] 不同分组间项目拖拽，不允许");
                            return;
                        }
                    }

                    // 情况3：拖拽到空白区域或其他情况
                    e.Effect = DragDropEffects.Move;
                    System.Diagnostics.Debug.WriteLine($"[DragEnter] 项目拖拽到空白区域或其他情况");
                    return;
                }

                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 执行拖拽放置
        /// </summary>
        private void TreeView_DragDrop(object sender, DragEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[拖拽开始] TreeView_DragDrop 事件触发");

            if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                var draggedNode = e.Data.GetData(typeof(TreeNode)) as TreeNode;
                var targetPoint = treeView.PointToClient(new Point(e.X, e.Y));
                var targetNode = treeView.GetNodeAt(targetPoint);

                System.Diagnostics.Debug.WriteLine($"[拖拽信息] 拖拽节点: {draggedNode?.Text}, 目标节点: {targetNode?.Text}");

                // 处理分组节点排序
                if (draggedNode?.Tag is TreeNodeData draggedData &&
                    draggedData.NodeType == TreeNodeType.Group)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 开始处理分组排序: '{draggedData.Group.Value}'");

                    // 执行分组排序
                    bool sortSuccess = ReorderGroups(draggedNode, targetNode, targetPoint);

                    if (sortSuccess)
                    {
                        // 触发配置保存事件以保存排序后的配置
                        ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
                        System.Diagnostics.Debug.WriteLine($"[分组排序] 排序完成并触发保存事件");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[分组排序] 排序失败或被取消");
                    }

                    return;
                }

                // 处理项目节点拖拽
                if (draggedNode?.Tag is TreeNodeData draggedItemData &&
                    draggedItemData.NodeType == TreeNodeType.Item)
                {
                    // 情况1：拖拽到分组节点（跨分组移动）
                    if (targetNode?.Tag is TreeNodeData targetData && targetData.NodeType == TreeNodeType.Group)
                    {
                        System.Diagnostics.Debug.WriteLine($"[跨分组拖拽] 拖拽数据有效: 拖拽项目='{draggedItemData.ItemName}', 目标分组={targetData.Group}, 是否保留分组={targetData.IsPreserved}");

                        // 检查目标分组内是否已有项目
                        bool hasExistingItem = false;
                        var existingItemName = "";
                        foreach (TreeNode childNode in targetNode.Nodes)
                        {
                            if (childNode.Tag is TreeNodeData childData &&
                                childData.NodeType == TreeNodeType.Item)
                            {
                                hasExistingItem = true;
                                existingItemName = childData.ItemName;
                                break;
                            }
                        }

                        // 如果是保留分组且已有项目，将冲突项目移至未分组
                        if (targetData.IsPreserved && hasExistingItem)
                        {
                            System.Diagnostics.Debug.WriteLine($"[拖拽冲突] 检测到保留分组 '{targetData.Group.Value}' 已有项目: '{existingItemName}'，正在移动拖拽项目 '{draggedItemData.ItemName}' 到未分组");

                            // 将拖拽的项目移动到"未分组"
                            MoveItemToUngrouped(draggedNode, draggedItemData);

                            // 显示提示信息
                            var groupName = GetGroupDisplayName(targetData.Group.Value);
                            var message = $"无法将 '{draggedItemData.ItemName}' 拖拽到保留分组 '{groupName}'\n\n" +
                                         $"该分组已有项目：{existingItemName}\n\n" +
                                         $"根据设计原则，每个保留分组只能包含一个项目。\n\n" +
                                         $"项目已移动到'未分组'。";

                            MessageBox.Show(message, "拖拽冲突", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            System.Diagnostics.Debug.WriteLine($"[拖拽冲突] 已显示冲突提示信息，操作完成");
                            return;
                        }

                        // 执行正常的跨分组拖拽操作
                        draggedNode.Remove();
                        targetNode.Nodes.Add(draggedNode);

                        // 更新项目的分组信息
                        draggedItemData.Group = targetData.Group.Value;

                        // 如果是保留分组，设置项目为保留状态；否则清除保留状态
                        if (targetData.IsPreserved)
                        {
                            draggedItemData.IsPreserved = true;
                            UpdateItemNodeVisual(draggedNode, true);
                            System.Diagnostics.Debug.WriteLine($"[跨分组拖拽成功] 项目 '{draggedItemData.ItemName}' 已加入保留分组 '{targetData.Group.Value}' 并设置为保留状态");
                        }
                        else
                        {
                            draggedItemData.IsPreserved = false;
                            UpdateItemNodeVisual(draggedNode, false);
                            System.Diagnostics.Debug.WriteLine($"[跨分组拖拽成功] 项目 '{draggedItemData.ItemName}' 已加入普通分组 '{targetData.Group.Value}'");
                        }

                        // 更新分组节点显示
                        UpdateGroupNodeVisual(targetNode, targetData.IsPreserved);

                        // 触发配置保存事件
                        ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
                        return;
                    }

                    // 情况2：拖拽到同分组内的其他项目节点（分组内排序）
                    if (targetNode?.Tag is TreeNodeData targetItemData && targetItemData.NodeType == TreeNodeType.Item)
                    {
                        // 检查是否在同一个分组内
                        if (draggedNode.Parent == targetNode.Parent && draggedNode.Parent != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[分组内排序] 开始处理分组内项目排序: 拖拽项目='{draggedItemData.ItemName}', 目标项目='{targetItemData.ItemName}'");

                            // 执行分组内项目排序
                            bool sortSuccess = ReorderItemsWithinGroup(draggedNode, targetNode, targetPoint);

                            if (sortSuccess)
                            {
                                // 触发配置保存事件
                                ConfigurationSaveRequested?.Invoke(this, EventArgs.Empty);
                                System.Diagnostics.Debug.WriteLine($"[分组内排序] 排序完成并触发保存事件");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[分组内排序] 排序失败或被取消");
                            }
                            return;
                        }
                    }

                    // 情况3：其他情况的处理
                    System.Diagnostics.Debug.WriteLine($"[拖拽] 未匹配到具体的拖拽情况，使用默认处理");
                }
            }
        }

        /// <summary>
        /// 重新排序分组内的项目节点
        /// </summary>
        /// <param name="draggedNode">被拖拽的项目节点</param>
        /// <param name="targetNode">目标项目节点</param>
        /// <param name="targetPoint">拖拽目标位置</param>
        /// <returns>是否排序成功</returns>
        private bool ReorderItemsWithinGroup(TreeNode draggedNode, TreeNode targetNode, Point targetPoint)
        {
            try
            {
                var parentGroup = draggedNode.Parent;
                if (parentGroup == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组内排序] 拖拽节点没有父分组");
                    return false;
                }

                // 获取分组内的所有项目节点
                var itemNodes = new List<TreeNode>();
                foreach (TreeNode node in parentGroup.Nodes)
                {
                    if (node.Tag is TreeNodeData nodeData && nodeData.NodeType == TreeNodeType.Item)
                    {
                        itemNodes.Add(node);
                    }
                }

                if (itemNodes.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组内排序] 分组内项目数量不足，无需排序");
                    return false;
                }

                // 计算插入位置
                int draggedIndex = itemNodes.IndexOf(draggedNode);
                int targetIndex = itemNodes.IndexOf(targetNode);

                if (draggedIndex == -1 || targetIndex == -1)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组内排序] 无法找到拖拽节点或目标节点的索引");
                    return false;
                }

                // 如果位置没有变化，不需要排序
                if (draggedIndex == targetIndex)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组内排序] 位置未变化，无需排序");
                    return false;
                }

                // 计算实际插入位置（基于鼠标位置）
                int insertIndex = CalculateItemInsertIndex(draggedNode, targetNode, targetPoint, itemNodes);

                // 执行排序
                return ExecuteItemReordering(draggedNode, draggedIndex, insertIndex, itemNodes, parentGroup);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[分组内排序] 排序过程中发生异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 计算分组内项目的插入位置
        /// </summary>
        private int CalculateItemInsertIndex(TreeNode draggedNode, TreeNode targetNode, Point targetPoint, List<TreeNode> itemNodes)
        {
            int draggedIndex = itemNodes.IndexOf(draggedNode);
            int targetIndex = itemNodes.IndexOf(targetNode);

            try
            {
                // 获取目标节点的边界
                var targetBounds = targetNode.Bounds;
                var midPoint = targetBounds.Top + targetBounds.Height / 2;

                System.Diagnostics.Debug.WriteLine($"[分组内排序] 计算插入位置: 拖拽索引={draggedIndex}, 目标索引={targetIndex}, 鼠标Y={targetPoint.Y}, 目标中点Y={midPoint}");

                if (draggedIndex < targetIndex)
                {
                    // 从上往下拖拽
                    if (targetPoint.Y < midPoint)
                    {
                        // 插入到目标节点前面
                        return targetIndex;
                    }
                    else
                    {
                        // 插入到目标节点后面
                        return targetIndex + 1;
                    }
                }
                else
                {
                    // 从下往上拖拽
                    if (targetPoint.Y < midPoint)
                    {
                        // 插入到目标节点前面
                        return targetIndex;
                    }
                    else
                    {
                        // 插入到目标节点后面
                        return targetIndex + 1;
                    }
                }
            }
            catch
            {
                // 如果无法获取边界信息，使用简单的逻辑
                return targetIndex;
            }
        }

        /// <summary>
        /// 执行分组内项目的重新排序
        /// </summary>
        private bool ExecuteItemReordering(TreeNode draggedNode, int originalIndex, int insertIndex, List<TreeNode> itemNodes, TreeNode parentGroup)
        {
            try
            {
                // 记录原始顺序
                var oldOrder = itemNodes.Select(n => n.Text).ToList();

                // 从父分组中移除拖拽的节点
                parentGroup.Nodes.Remove(draggedNode);

                // 重新计算插入位置（因为移除了一个节点）
                int actualInsertIndex = insertIndex;
                if (originalIndex < insertIndex)
                {
                    actualInsertIndex = insertIndex - 1;
                }

                // 确保插入索引在有效范围内
                actualInsertIndex = Math.Max(0, Math.Min(actualInsertIndex, parentGroup.Nodes.Count));

                // 在新位置插入节点
                parentGroup.Nodes.Insert(actualInsertIndex, draggedNode);

                // 记录新顺序
                var newItemNodes = new List<TreeNode>();
                foreach (TreeNode node in parentGroup.Nodes)
                {
                    if (node.Tag is TreeNodeData nodeData && nodeData.NodeType == TreeNodeType.Item)
                    {
                        newItemNodes.Add(node);
                    }
                }
                var newOrder = newItemNodes.Select(n => n.Text).ToList();

                System.Diagnostics.Debug.WriteLine($"[分组内排序] 排序完成: '{draggedNode.Text}' 从位置 {originalIndex} 移动到 {actualInsertIndex}");
                System.Diagnostics.Debug.WriteLine($"[分组内排序] 原顺序: {string.Join(" -> ", oldOrder)}");
                System.Diagnostics.Debug.WriteLine($"[分组内排序] 新顺序: {string.Join(" -> ", newOrder)}");

                // 触发项目排序事件（如果需要）
                OnItemsReordered(parentGroup, draggedNode.Text, originalIndex, actualInsertIndex, oldOrder, newOrder);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[分组内排序] 执行排序失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 项目排序事件
        /// </summary>
        public event EventHandler<ItemsReorderedEventArgs> ItemsReordered;

        /// <summary>
        /// 触发项目排序事件
        /// </summary>
        private void OnItemsReordered(TreeNode parentGroup, string draggedItem, int fromIndex, int toIndex, List<string> oldOrder, List<string> newOrder)
        {
            if (parentGroup?.Tag is TreeNodeData groupData)
            {
                ItemsReordered?.Invoke(this, new ItemsReorderedEventArgs
                {
                    GroupName = groupData.Group?.ToString() ?? "未知分组",
                    DraggedItem = draggedItem,
                    FromIndex = fromIndex,
                    ToIndex = toIndex,
                    OldOrder = oldOrder,
                    NewOrder = newOrder,
                    Timestamp = DateTime.Now
                });
            }
        }

        /// <summary>
        /// 将项目移动到未分组
        /// </summary>
        /// <param name="itemNode">要移动的项目节点</param>
        /// <param name="itemData">项目数据</param>
        private void MoveItemToUngrouped(TreeNode itemNode, TreeNodeData itemData)
        {
            // 从当前分组中移除
            var originalParent = itemNode.Parent;
            originalParent?.Nodes.Remove(itemNode);

            // 记录移动前的位置信息（用于事件通知）
            var originalGroup = originalParent?.Tag as TreeNodeData;
            var originalGroupName = originalGroup?.Group?.ToString() ?? "未知";

            // 查找未分组节点
            TreeNode ungroupedNode = null;
            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Tag is TreeNodeData data && data.NodeType == TreeNodeType.Group && data.Group == EventGroup.Ungrouped)
                {
                    ungroupedNode = node;
                    break;
                }
            }

            // 如果未分组节点不存在，创建一个
            if (ungroupedNode == null)
            {
                ungroupedNode = new TreeNode("未分组")
                {
                    Tag = new TreeNodeData
                    {
                        NodeType = TreeNodeType.Group,
                        Group = EventGroup.Ungrouped,
                        IsEnabled = true,
                        IsPreserved = false
                    }
                };
                treeView.Nodes.Add(ungroupedNode);
            }

            // 添加到未分组
            ungroupedNode.Nodes.Add(itemNode);

            // 更新项目的分组信息
            itemData.Group = EventGroup.Ungrouped;
            itemData.IsPreserved = false; // 未分组中的项目不保留

            // 更新项目节点显示
            UpdateItemNodeVisual(itemNode, false);

            // 触发项目移动事件
            OnItemMoved(itemNode, originalGroupName, "未分组");

            // 展开未分组节点以便用户看到移动的项目
            ungroupedNode.Expand();

            // 记录日志
            System.Diagnostics.Debug.WriteLine($"项目 '{itemNode.Text}' 已从 '{originalGroupName}' 移动到 '未分组'");
        }

        /// <summary>
        /// 项目移动事件
        /// </summary>
        public event EventHandler<ItemMovedEventArgs> ItemMoved;

        /// <summary>
        /// 触发项目移动事件
        /// </summary>
        /// <param name="movedNode">移动的项目节点</param>
        /// <param name="fromGroup">源分组</param>
        /// <param name="toGroup">目标分组</param>
        private void OnItemMoved(TreeNode movedNode, string fromGroup, string toGroup)
        {
            ItemMoved?.Invoke(this, new ItemMovedEventArgs
            {
                ItemName = movedNode.Text,
                FromGroup = fromGroup,
                ToGroup = toGroup,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// 分组排序事件
        /// </summary>
        public event EventHandler<GroupReorderedEventArgs> GroupReordered;

        /// <summary>
        /// 重新排序分组节点
        /// </summary>
        /// <param name="draggedNode">被拖拽的分组节点</param>
        /// <param name="targetNode">目标节点</param>
        /// <param name="targetPoint">拖拽目标位置</param>
        /// <returns>是否排序成功</returns>
        private bool ReorderGroups(TreeNode draggedNode, TreeNode targetNode, Point targetPoint)
        {
            try
            {
                // 确保被拖拽的是根级分组节点
                if (draggedNode?.Parent == null || !(draggedNode.Tag is TreeNodeData draggedData))
                {
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 被拖拽节点不是根级分组节点");
                    return false;
                }

                // 获取当前根级分组列表（只包含TreeNodeType.Group的节点）
                var rootGroups = new List<TreeNode>();
                foreach (TreeNode node in treeView.Nodes)
                {
                    if (node.Tag is TreeNodeData nodeData && nodeData.NodeType == TreeNodeType.Group)
                    {
                        rootGroups.Add(node);
                    }
                }

                if (rootGroups.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 根级分组数量不足，无需排序");
                    return false;
                }

                // 计算插入位置
                int insertIndex = CalculateInsertIndex(draggedNode, targetNode, targetPoint, rootGroups);
                int currentIndex = rootGroups.IndexOf(draggedNode);

                if (currentIndex == -1)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 被拖拽分组不在根级分组列表中");
                    return false;
                }

                // 如果位置没有变化，不需要排序
                if (currentIndex == insertIndex)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 位置未变化，无需排序");
                    return false;
                }

                // 执行排序
                return ExecuteGroupReordering(draggedNode, currentIndex, insertIndex, rootGroups);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[分组排序] 排序过程中发生异常: {ex.Message}");
                MessageBox.Show($"分组排序失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 计算插入位置（真正的插入式排序：从上往下往后插入，从下往前往前插入）
        /// </summary>
        private int CalculateInsertIndex(TreeNode draggedNode, TreeNode targetNode, Point targetPoint, List<TreeNode> rootGroups)
        {
            int draggedIndex = rootGroups.IndexOf(draggedNode);

            // 如果目标节点为null（拖拽到空白区域），计算基于鼠标位置
            if (targetNode?.Tag is TreeNodeData targetData && targetData.NodeType == TreeNodeType.Group)
            {
                int targetIndex = rootGroups.IndexOf(targetNode);
                if (targetIndex == -1) return rootGroups.Count - 1;

                System.Diagnostics.Debug.WriteLine($"[分组排序] 插入式逻辑: 拖拽='{draggedNode.Text}'(索引:{draggedIndex}) -> 目标='{targetNode.Text}'(索引:{targetIndex})");

                // 真正的插入式逻辑：从上往下调整 = 往后插入，从下往上调整 = 往前插入
                if (draggedIndex < targetIndex)
                {
                    // 从上往下拖拽：draggedNode会从原位置移除，targetIndex会减1
                    // 所以要插入到目标位置+1
                    int finalIndex = targetIndex; // 删除原节点后，targetIndex会自动减1
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 从上往下拖拽，插入到目标节点后面，计算插入索引: {finalIndex}");
                    return finalIndex;
                }
                else if (draggedIndex > targetIndex)
                {
                    // 从下往上拖拽：直接插入到目标位置
                    int finalIndex = targetIndex;
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 从下往上拖拽，插入到目标节点前面，计算插入索引: {finalIndex}");
                    return finalIndex;
                }
                else
                {
                    // 位置相同，无需移动
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 位置相同，无需移动");
                    return draggedIndex;
                }
            }
            else
            {
                // 拖拽到空白区域，根据鼠标Y坐标计算位置
                for (int i = 0; i < rootGroups.Count; i++)
                {
                    var nodeItem = rootGroups[i];
                    try
                    {
                        var nodeBounds = nodeItem.Bounds;
                        if (nodeBounds.Height > 0 && targetPoint.Y < nodeBounds.Top + nodeBounds.Height / 2)
                        {
                            System.Diagnostics.Debug.WriteLine($"[分组排序] 拖拽到空白区域，插入到索引 {i} 前面");
                            return i;
                        }
                    }
                    catch
                    {
                        // 如果无法获取边界信息，继续下一个节点
                        continue;
                    }
                }
                // 如果没有找到合适的位置，放在最后
                System.Diagnostics.Debug.WriteLine($"[分组排序] 拖拽到空白区域底部，插入到最后");
                return rootGroups.Count;
            }
        }

        /// <summary>
        /// 执行分组重排序（真正的插入式排序）
        /// </summary>
        private bool ExecuteGroupReordering(TreeNode draggedNode, int currentIndex, int insertIndex, List<TreeNode> rootGroups)
        {
            try
            {
                var draggedData = draggedNode.Tag as TreeNodeData;
                var originalIndex = currentIndex;

                System.Diagnostics.Debug.WriteLine($"[分组排序] 开始执行真正的插入式排序: '{draggedData?.Group.Value}' 从位置 {originalIndex} 移动到位置 {insertIndex}");

                // 保存原始状态用于事件
                var oldOrder = rootGroups.Select(n => (n.Tag as TreeNodeData)?.Group.ToString() ?? "").ToList();

                // 如果插入位置就是当前位置，无需操作
                if (originalIndex == insertIndex)
                {
                    System.Diagnostics.Debug.WriteLine($"[分组排序] 位置未变化，无需排序");
                    return false;
                }

                // 执行实际的节点移动：先移除，再插入
                draggedNode.Remove();

                // 直接使用计算好的插入索引，无需额外调整
                // 因为CalculateInsertIndex已经考虑了删除节点后的索引变化
                int actualInsertIndex = insertIndex;
                System.Diagnostics.Debug.WriteLine($"[分组排序] 使用计算好的插入索引: {actualInsertIndex}");

                // 确保插入索引在有效范围内
                actualInsertIndex = Math.Max(0, Math.Min(actualInsertIndex, treeView.Nodes.Count));

                treeView.Nodes.Insert(actualInsertIndex, draggedNode);

                // 保存新的排序顺序
                var newOrder = new List<string>();
                foreach (TreeNode node in treeView.Nodes)
                {
                    if (node.Tag is TreeNodeData nodeData && nodeData.NodeType == TreeNodeType.Group)
                    {
                        newOrder.Add(nodeData.Group.ToString());
                    }
                }

                // 触发分组排序事件
                GroupReordered?.Invoke(this, new GroupReorderedEventArgs
                {
                    DraggedGroup = draggedData?.Group.ToString() ?? "",
                    FromIndex = originalIndex,
                    ToIndex = actualInsertIndex,
                    OldOrder = oldOrder,
                    NewOrder = newOrder,
                    Timestamp = DateTime.Now
                });

                System.Diagnostics.Debug.WriteLine($"[分组排序] 插入式排序完成: '{draggedData?.Group.Value}' 从 {originalIndex} 移动到 {actualInsertIndex}");
                System.Diagnostics.Debug.WriteLine($"[分组排序] 原顺序: {string.Join(" -> ", oldOrder)}");
                System.Diagnostics.Debug.WriteLine($"[分组排序] 新顺序: {string.Join(" -> ", newOrder)}");

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[分组排序] 执行插入式排序失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取分组排序配置
        /// </summary>
        public List<string> GetGroupOrder()
        {
            var groupOrder = new List<string>();
            foreach (TreeNode node in treeView.Nodes)
            {
                if (node.Tag is TreeNodeData nodeData && nodeData.NodeType == TreeNodeType.Group)
                {
                    groupOrder.Add(nodeData.Group.ToString());
                }
            }
            return groupOrder;
        }

        /// <summary>
        /// 应用分组排序配置
        /// </summary>
        public void ApplyGroupOrder(List<string> groupOrder)
        {
            if (groupOrder == null || groupOrder.Count == 0) return;

            try
            {
                var currentGroups = new Dictionary<string, TreeNode>();
                var nodesToRemove = new List<TreeNode>();

                // 收集当前所有分组节点
                foreach (TreeNode node in treeView.Nodes)
                {
                    if (node.Tag is TreeNodeData nodeData && nodeData.NodeType == TreeNodeType.Group)
                    {
                        currentGroups[nodeData.Group.ToString()] = node;
                        nodesToRemove.Add(node);
                    }
                }

                // 移除所有分组节点
                foreach (var node in nodesToRemove)
                {
                    treeView.Nodes.Remove(node);
                }

                // 按照新顺序重新添加分组节点
                foreach (var groupName in groupOrder)
                {
                    if (currentGroups.TryGetValue(groupName, out var node))
                    {
                        treeView.Nodes.Add(node);
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[分组排序] 应用配置完成，新顺序: {string.Join(" -> ", groupOrder)}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[分组排序] 应用排序配置失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 分组排序事件参数
    /// </summary>
    public class GroupReorderedEventArgs : EventArgs
    {
        public string DraggedGroup { get; set; }
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
        public List<string> OldOrder { get; set; } = new List<string>();
        public List<string> NewOrder { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// 分组内项目排序事件参数
    /// </summary>
    public class ItemsReorderedEventArgs : EventArgs
    {
        public string GroupName { get; set; }
        public string DraggedItem { get; set; }
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
        public List<string> OldOrder { get; set; } = new List<string>();
        public List<string> NewOrder { get; set; } = new List<string>();
        public DateTime Timestamp { get; set; }
    }
}