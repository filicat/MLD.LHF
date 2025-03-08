using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLD.LHF.JZC.ZL.MLD.Sale.Buiness.Bill.PlugIn
{
    [Description("控制多方交易生成的单据不允许手工反审核 表单插件")]
    [HotUpdate]
    public class SaleCommonBillPlugIn : AbstractBillPlugIn
    {
        public override void ToolBarItemClick(BarItemClickEventArgs e)
        {
            if (e.BarItemKey.EqualsIgnoreCase("tbReject"))
            {
                DynamicObject obj = this.Model.DataObject;
                if (obj["F_MLD_EndBillNo"] != null && !StringUtils.IsEmpty(obj["F_MLD_EndBillNo"].ToString()))
                {
                    this.View.ShowErrMessage("多方交易内部生成单据不允许手工反审核");
                    e.Cancel = true;
                    return;
                }
                base.ToolBarItemClick(e);
            }
            else {
                base.ToolBarItemClick(e);
            }
           
        }
    }
}
