using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.List;

namespace MLD.LHF.JZC.ZL.MLD.Sale.Buiness.Bill.PlugIn
{
    [Description("控制多方交易生成的单据不允许手工反审核 列表插件")]
    [HotUpdate]
    public class SaleCommonListPlugIn : AbstractListPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            if (e.BarItemKey.EqualsIgnoreCase("tbReject"))
            {
                ListSelectedRowCollection selectRows = ((IListView)this.View).SelectedRowsInfo;
                DynamicObjectCollection dycoll = this.ListModel.GetData(selectRows);
                if (selectRows.Count == 0)
                {
                    return;
                }
                String billNoStr = "";
                for (int i = 0; i < dycoll.Count; i++)
                {
                    var endBillNo = dycoll[i]["F_MLD_EndBillNo"];
                    var billNo = dycoll[i]["FBillNo"];
                    if (endBillNo != null && !StringUtils.IsEmpty(endBillNo.ToString()))
                    {
                        if (!StringUtils.IsEmpty(billNoStr.ToString()))
                        {
                            billNoStr += ",";
                        }
                        billNoStr += billNo;
                    }
                }
                if (!StringUtils.IsEmpty(billNoStr))
                {
                    this.ListView.ShowErrMessage("多方交易单据 " + billNoStr + " 不允许手工反审核！");
                    e.Cancel = true;
                    return;
                }
                base.BarItemClick(e);
            }
            else {
                base.BarItemClick(e);
            }
            
        }
    }
}
