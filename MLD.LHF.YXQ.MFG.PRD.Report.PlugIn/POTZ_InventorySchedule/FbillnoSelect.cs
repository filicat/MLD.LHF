using Kingdee.BOS;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Report.PlugIn;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.YXQ.MFG.PRD.Report.PlugIn.POTZ_InventorySchedule
{
    [Description("根据单号查找对应单据"), HotUpdate]
    public  class FbillnoSelect : AbstractSysReportPlugIn
    {
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.Equals("F_MLD_FBeginRONumber") || (e.FieldKey.Equals("F_MLD_EndRONumber")))
            {
                string orgin = this.Model.GetValue("F_MLD_ZZ").ToString();
                string startValue = this.Model.GetValue("F_MLD_StartDate").ToString();
                string endValue = this.Model.GetValue("F_MLD_EndDate").ToString();
                ListSelBillShowParameter para = new ListSelBillShowParameter();
                para.FormId = "SUB_SUBREQORDER";
                para.ParentPageId = this.View.PageId;
                para.MultiSelect = false; // 是否多选
                string filter = string.Format("FSubOrgId in ({0}) and FDate>='{1}' and FDate<='{2}'", orgin, startValue, endValue);
                para.ListFilterParameter.Filter = filter;
                this.View.ShowForm(para, new Action<Kingdee.BOS.Core.DynamicForm.FormResult>(result =>
                {
                    ListSelectedRowCollection rows = (ListSelectedRowCollection)result.ReturnData;
                    if (rows == null)
                    {
                        return;
                    }
                    DynamicObjectDataRow data = (DynamicObjectDataRow)rows[0].DataRow;
                    if(e.FieldKey.Equals("F_MLD_FBeginRONumber")) this.Model.SetValue("F_MLD_FBeginRONumber", data["FBILLNO"]);
                    this.Model.SetValue("F_MLD_EndRONumber", data["FBILLNO"]);
                }));
            }
        }
    }
}
