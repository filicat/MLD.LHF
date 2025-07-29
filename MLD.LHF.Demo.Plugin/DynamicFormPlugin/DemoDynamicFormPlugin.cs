using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;

namespace MLD.LHF.Demo.Plugin.DynamicFormPlugin
{
    [HotUpdate, Description("[动态表单插件]Demo")]
    public class DemoDynamicFormPlugin : AbstractDynamicFormPlugIn
    {
        string poid;
        DataTable dt;

        public override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.View.Model.SetValue("F_POTZ_SUM", 41);
            this.View.UpdateView("F_POTZ_SUM");
            poid = Convert.ToString(this.View.OpenParameter.GetCustomParameter("POID"));
            string sql = string.Format(@"SELECT T0.FID, T0.FBILLNO, T1.FQTY
                            FROM T_PUR_POORDER T0
                            INNER JOIN t_PUR_POOrderEntry T1 ON T0.FID=T1.FID
                            WHERE T0.FID={0}", poid);
            dt = DBUtils.ExecuteDataSet(this.Context, sql).Tables[0];
            if (dt.Rows.Count <= 0)
            {
                return;
            }
            Entity entity = this.View.BillBusinessInfo.GetEntity("F_POTZ_Entity"); // 定义一个单据体
            DynamicObjectCollection rows = this.Model.GetEntityDataObject(entity);
            for (int i=0; i<dt.Rows.Count; i++)
            {
                DynamicObject row = new DynamicObject(entity.DynamicObjectType);
                entity.SeqDynamicProperty.SetValue(row, i + 1);
                row["F_POTZ_BILLNO"] = dt.Rows[i]["FBILLNO"] == null ? "空" : dt.Rows[i]["FBILLNO"].ToString();
                row["F_POTZ_POID"] = poid;
                row["F_POTZ_QTY"] = dt.Rows[i]["FQTY"] == null ? 0 : Convert.ToDecimal(dt.Rows[i]["FQTY"]);
                rows.Add(row);
            }
        }
    }
}
