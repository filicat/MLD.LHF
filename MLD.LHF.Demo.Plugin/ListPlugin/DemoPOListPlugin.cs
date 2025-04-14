using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.List.PlugIn;
using Kingdee.BOS.Core.List.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;

namespace MLD.LHF.Demo.Plugin.ListPlugin
{
    [HotUpdate, Description("[列表插件]DemoPO列表插件")]
    public class DemoPOListPlugin : AbstractListPlugIn
    {
        public override void OnFormatRowConditions(ListFormatConditionArgs args)
        {
            base.OnFormatRowConditions(args);

            // 定义FormatConditon实例
            FormatCondition fc = new FormatCondition();
            // 打开单据的时候触发
            fc.ApplayRow = true;
            // 加载后, 如果单据状态字段不等于已审核C
            if (args.DataRow["FDOCUMENTSTATUS"].ToString() != "C")
            {
                // 橙红色
                fc.BackColor = ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(255, 64, 64));
                args.FormatConditions.Add(fc);
            }
        }

        public override void PrepareFilterParameter(FilterArgs e)
        {
            base.PrepareFilterParameter(e);

            //string filterString = string.Format(" FBILLNO NOT LIKE {0}", "'GDCGDD241%'");

            //e.AppendQueryFilter(filterString);
            //e.AppendQueryOrderby("FBILLNO");
            e.AppendQueryOrderby("FBILLNO DESC");
        }

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            if (e.BarItemKey.EqualsIgnoreCase("POTZ_DEMO_BTN1")) // TODO
            {
                ListSelectedRowCollection selectedRows = this.ListView.SelectedRowsInfo;
                string[] ID = selectedRows.GetPrimaryKeyValues();

                // 定义弹窗界面
                BillShowParameter para = new BillShowParameter();
                para.OpenStyle.ShowType = ShowType.Modal;
                para.FormId = "PUR_PurchaseOrder";
                para.Status = OperationStatus.VIEW;
                para.PKey = ID[0];
                para.ParentPageId = this.View.ParentFormView.PageId;
                this.View.ShowForm(para);
            }
        }
    }
}
