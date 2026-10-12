using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using AntdUI;
using WindowsFormsApp3.Models;
using WindowsFormsApp3.Utils;
using WindowsFormsApp3.Services;
using WindowsFormsApp3.UI;
using Xunit;

namespace WindowsFormsApp3.Tests.Forms
{
    public class OrderNumberDropdownTests
    {
        public OrderNumberDropdownTests()
        {
            try
            {
                AppSettings.Initialize(new FileLogger(Path.Combine(Path.GetTempPath(), "OrderNumberDropdownTests")));
            }
            catch { }
        }

        [StaFact]
        public void Test_OrderNumberMode_Button_And_Submenu_Structure()
        {
            AppSettings.Set("OrderNumberMode1", "无");
            AppSettings.Save();

            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\PO998877_54x84.pdf",
                regexResult: "PO998877",
                opacity: 1.0,
                width: "54",
                height: "84",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "1"))
            {
                var handle = form.Handle;

                var modeBtn = form.BtnOrderNumberMode;
                var orderInput = typeof(MaterialSelectFormModern).GetField("orderNumberTextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form) as AntdUI.Input;
                var orderLabel = typeof(MaterialSelectFormModern).GetField("orderNumberLabel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form) as AntdUI.Label;
                var menu = form.OrderNumberModeMenuItems;

                Assert.NotNull(modeBtn);
                Assert.NotNull(orderInput);
                Assert.NotNull(orderLabel);
                Assert.NotNull(menu);

                // 1. 验证 V5 等宽组合框内的按钮与输入框位置。
                Assert.Equal(new Point(103, 1), modeBtn.Location);
                Assert.Equal(new Size(70, 22), modeBtn.Size);
                Assert.Equal(new Point(36, 1), orderInput.Location);
                Assert.Equal(new Size(67, 22), orderInput.Size);
                Assert.False(orderLabel.Visible);

                // 2. 验证 AntdUI 请求模型的主菜单结构
                Assert.Equal(3, menu.Count);
                Assert.Equal("order-mode-none", menu[0].Id);
                Assert.Equal("无", menu[0].Text);
                Assert.Equal("自动递增", menu[1].Text);
                Assert.Equal("正则提取", menu[2].Text);

                // 3. 验证正则提取二级子菜单
                var regexMenuItem = menu[2];
                Assert.NotNull(regexMenuItem);
                Assert.True(regexMenuItem.Items.Count > 0, "正则提取子菜单应包含已配置规则");

                // 4. 验证默认状态为【无】
                Assert.Equal(OrderNumberMode.None, form.CurrentOrderNumberMode);
                Assert.Equal("无 ▾", modeBtn.Text);
                Assert.True(menu[0].Checked);
                Assert.False(menu[1].Checked);
                Assert.False(menu[2].Checked);
            }
        }

        [StaFact]
        public void Test_OrderNumberMode_Mode_Switching_And_Regex_Extraction()
        {
            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET", "PP" },
                fileName: @"C:\test\PO998877_54x84.pdf",
                regexResult: "PO998877",
                opacity: 1.0,
                width: "54",
                height: "84",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "1"))
            {
                var handle = form.Handle;
                var modeBtn = form.BtnOrderNumberMode;
                var orderInput = typeof(MaterialSelectFormModern).GetField("orderNumberTextBox", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form) as AntdUI.Input;

                // 1. 切换为【自动递增】
                form.SelectOrderNumberMode(OrderNumberMode.AutoIncrement);
                Assert.Equal(OrderNumberMode.AutoIncrement, form.CurrentOrderNumberMode);
                Assert.Equal("自动递增 ▾", modeBtn.Text);
                Assert.Equal(new Point(103, 1), modeBtn.Location);

                // 2. 选择具体正则子规则【订单号_PO数字】
                form.SelectOrderRegexRule("订单号_PO数字", @"PO\d+");
                Assert.Equal(OrderNumberMode.RegexExtraction, form.CurrentOrderNumberMode);
                Assert.Equal("订单号_PO数字", form.SelectedOrderRegexName);
                Assert.Equal(@"PO\d+", form.SelectedOrderRegexPattern);
                Assert.Equal("订单号_PO数字 ▾", modeBtn.Text);
                Assert.Equal("PO998877", orderInput.Text);
                Assert.Equal("PO998877", form.OrderNumber);
                Assert.Equal(new Point(103, 1), modeBtn.Location);

                // 3. 切换回【无】
                form.SelectOrderNumberMode(OrderNumberMode.None);
                Assert.Equal(OrderNumberMode.None, form.CurrentOrderNumberMode);
                Assert.Equal("无 ▾", modeBtn.Text);
                Assert.Equal(new Point(103, 1), modeBtn.Location);
            }
        }

        [StaTheory]
        [InlineData("AutoIncrement", OrderNumberMode.AutoIncrement, "自动递增 ▾")]
        [InlineData("RegexExtraction", OrderNumberMode.RegexExtraction, null)]
        [InlineData("None", OrderNumberMode.None, "无 ▾")]
        [InlineData("True", OrderNumberMode.AutoIncrement, "自动递增 ▾")]
        [InlineData("False", OrderNumberMode.None, "无 ▾")]
        [InlineData("自动递增", OrderNumberMode.AutoIncrement, "自动递增 ▾")]
        [InlineData("正则提取", OrderNumberMode.RegexExtraction, null)]
        [InlineData("无", OrderNumberMode.None, "无 ▾")]
        public void Test_RestoreOrderNumberMode_With_Various_Config_Values(string configValue, OrderNumberMode expectedMode, string expectedBtnText)
        {
            AppSettings.Set("OrderNumberMode1", configValue);
            AppSettings.Save();

            using (var form = new MaterialSelectFormModern(
                materials: new List<string> { "PET" },
                fileName: @"C:\test\PO123456_54x84.pdf",
                regexResult: "PO123456",
                opacity: 1.0,
                width: "54",
                height: "84",
                excelData: null,
                searchColumnIndex: -1,
                returnColumnIndex: -1,
                serialColumnIndex: -1,
                newColumnIndex: -1,
                serialNumber: "1"))
            {
                var handle = form.Handle;
                var modeBtn = form.BtnOrderNumberMode;

                Assert.NotNull(modeBtn);
                Assert.Equal(expectedMode, form.CurrentOrderNumberMode);
                if (expectedMode == OrderNumberMode.RegexExtraction)
                {
                    Assert.Equal($"{form.SelectedOrderRegexName} ▾", modeBtn.Text);
                }
                else
                {
                    Assert.Equal(expectedBtnText, modeBtn.Text);
                }
            }
        }
    }
}
