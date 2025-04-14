using Kingdee.BOS;
using Kingdee.BOS.App.Core.Warn.Data;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.MFG.SUB.Business.PlugIn.Bill
{
    [Description("【表单插件】更新MLD自定义字段(已消耗材料数, 在制材料数)"), HotUpdate]
    public class PPBOMFormPlugIn : AbstractDynamicFormPlugIn
    {
        const string ButtonKey = "F_MLD_Upd_Consume_n_Wip_Btn";

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.EqualsIgnoreCase(ButtonKey))
            {
                DBUtils.ExecuteStoreProcedure(this.Context, "SP_UPD_PPBOM_COMSUME_WIP_QTY_LHF", new List<SqlParam>());
                this.View.ShowMessage("已更新字段:MLD已消耗数量, MLD在制材料数", Kingdee.BOS.Core.DynamicForm.MessageBoxType.Notice);
                this.View.UpdateView();
            }
        }
    }
}
