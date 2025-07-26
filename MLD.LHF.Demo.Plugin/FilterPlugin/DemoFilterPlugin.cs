using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.ListFilter;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.Demo.Plugin.FilterPlugin
{
    [HotUpdate, Description("[过滤插件]Demo过滤插件:列表过滤窗体上查询查询基础资料时设置过滤条件")]
    public class DemoFilterPlugin : AbstractListFilterPlugIn
    {
        public override void BeforeFilterGridF7Select(BeforeFilterGridF7SelectEventArgs e)
        {
            base.BeforeFilterGridF7Select(e);

            if (this.View.ParentFormView != null &&
                this.View.ParentFormView.BillBusinessInfo.GetForm().Id.EqualsIgnoreCase("PUR_PurchaseOrder"))
            {
                //if (e.FieldKey.StartsWith("FSupplierId"))
                //{
                //    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(" FNumber like '02.%' ");
                //    return;
                //}
                if (e.FieldKey.StartsWith("FPurchaseDeptId"))
                {
                    e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(" FNumber like '11.2%'");
                    return;
                }
            }
        }
    }
}
