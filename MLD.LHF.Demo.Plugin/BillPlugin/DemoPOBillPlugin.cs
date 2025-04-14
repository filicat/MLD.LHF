using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.Demo.Plugin.BillPlugin
{
    [HotUpdate, Description("[表单插件]DemoPO表单插件")]
    public class DemoPOBillPlugin : AbstractBillPlugIn
    {
        // 工具条按钮触发事件
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            if (e.BarItemKey.EqualsIgnoreCase("POTZ_DEMO_BTN1"))
            {
                // 打印消息
                this.View.ShowMessage("DEMO1!");
            }

            if (e.BarItemKey.EqualsIgnoreCase("POTZ_DEMO_BTN2"))
            {
                // 更新字段值
                this.View.Model.SetValue("F_POTZ_Remarks_BZ", "你好");
                this.View.UpdateView("F_POTZ_Remarks_BZ");
            }

            if (e.BarItemKey.EqualsIgnoreCase("POTZ_DEMO_BTN3"))
            {
                // 读取字段值
                string note = this.View.Model.GetValue("F_POTZ_Remarks_BZ").ToString();
                this.View.ShowMessage("读取到备注字段值: [" + note + "]");
            }

            // 基础资料设置
            if (e.BarItemKey.EqualsIgnoreCase("POTZ_DEMO_BTN4"))
            {
                this.View.Model.SetValue("FSupplierId", 1317586); // TODO
                this.View.UpdateView("FSupplierId"); // TODO
            }
        }

        // 值变更事件
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);

            if (e.Field.Key.EqualsIgnoreCase("F_MLD_Purchaser")) // TODO
            {
                this.View.ShowMessage("采购员值发生变化");
            }
        }
    }
}
