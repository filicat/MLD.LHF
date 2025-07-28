using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.Demo.Plugin.FilterPlugin
{
    [HotUpdate, Description("[过滤插件]Demo账表过滤插件:账表过滤窗体上查询查询基础资料时设置过滤条件")]
    public class DemoCommonFilterPlugIn : AbstractCommonFilterPlugIn
    {
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            // 供应商添加附加过滤条件
            if (e.FieldKey.EqualsIgnoreCase("FBeginSupplierId") && e.FormId.EqualsIgnoreCase("BD_Supplier"))
            {
                e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(" FNumber like '%02%'");
                return;
            }
            // 物料添加附加过滤条件
            if (e.FieldKey.EqualsIgnoreCase("FBeginMaterialId") && e.FormId.EqualsIgnoreCase("BD_Material"))
            {
                e.ListFilterParameter.Filter = e.ListFilterParameter.Filter.JoinFilterString(" FNumber like '1.C.%'");
                return;
            }
        }
    }
}
