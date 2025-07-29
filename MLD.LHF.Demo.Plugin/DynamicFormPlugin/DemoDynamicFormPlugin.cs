using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
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
            decimal sum = 0;
            for (int i=0; i<dt.Rows.Count; i++)
            {
                DynamicObject row = new DynamicObject(entity.DynamicObjectType);
                entity.SeqDynamicProperty.SetValue(row, i + 1);
                row["F_POTZ_BILLNO"] = dt.Rows[i]["FBILLNO"] == null ? "空" : dt.Rows[i]["FBILLNO"].ToString();
                row["F_POTZ_POID"] = poid;
                decimal qty = dt.Rows[i]["FQTY"] == null ? 0 : Convert.ToDecimal(dt.Rows[i]["FQTY"]);
                row["F_POTZ_QTY"] = qty;
                rows.Add(row);
                sum += qty;
            }
            this.View.Model.SetValue("F_POTZ_SUM", sum);
            this.View.UpdateView("F_POTZ_SUM");
        }

        public override void EntityRowDoubleClick(EntityRowClickEventArgs e)
        {
            base.EntityRowDoubleClick(e);
            BillShowParameter para = new BillShowParameter();
            para.OpenStyle.ShowType = ShowType.NonModal;
            para.FormId = "PUR_PurchaseOrder";
            para.Status = OperationStatus.VIEW;
            para.PKey = this.View.Model.GetValue("F_POTZ_POID", e.Row).ToString();
            para.ParentPageId = this.View.ParentFormView.PageId;
            this.View.ShowForm(para);
        }

        private double GetSum()
        {
            double qty = 0;
            Entity entity = this.View.BillBusinessInfo.GetEntity("F_POTZ_Entity");
            DynamicObjectCollection rows = this.View.Model.GetEntityDataObject(entity);
            if (null == rows)
            {
                return qty;
            }
            for (int i = 0; i < rows.Count; i++)
            {
                object rowQty = this.View.Model.GetValue("F_POTZ_QTY");
                if (Convert.ToString(rowQty) != "")
                {
                    qty += Convert.ToDouble(rowQty);
                }
            }
            return qty;
        }

        public override void AfterDeleteRow(AfterDeleteRowEventArgs e)
        {
            base.AfterDeleteRow(e);
            this.View.Model.SetValue("F_POTZ_SUM", GetSum());
            this.View.UpdateView("F_POTZ_SUM");
        }
    }
}
