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
    [Description("销售订单多方交易控制组织")]
    [HotUpdate]
    public class SaleOrderBillPlugIn : AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.Equals("F_MLD_MuiPartBuinessTmp")|| e.Field.Key.Equals("FMaterialId"))
            {
                if (e.NewValue != null)
                {
                    DynamicObject obj = this.Model.DataObject;
                    long stockOrgId = Convert.ToInt32(obj["SaleOrgId_Id"]);
                    if (obj["F_MLD_MuiPartBuinessTmp"] == null)
                    {
                        return;
                    }
                    DynamicObject buinessTmp = obj["F_MLD_MuiPartBuinessTmp"] as DynamicObject;                  
                    DynamicObjectCollection buinessEntry = buinessTmp["MILD_BuinessTemp_Entry"] as DynamicObjectCollection;
                    List<DynamicObject> stockOrgList = buinessEntry.Where(item => Convert.ToInt32(item["F_MILD_EntryType"]) == 2).ToList();
                    if (stockOrgList.Count() > 0)
                    {
                        stockOrgId = Convert.ToInt32(stockOrgList[0]["F_MILD_OrgId_Id"]);
                    }
                    if (e.Field.Key.Equals("FMaterialId"))
                    {
                        this.Model.SetValue("FStockOrgId", stockOrgId, e.Row);
                        this.Model.SetValue("FOwnerId", stockOrgId, e.Row);
                        this.Model.SetValue("F_MLD_OutOrg", obj["SaleOrgId_Id"],e.Row);
                    }
                    else {
                        //将多方交易供货方赋值到库存组织。货主还是销售方 增加理论货主
                        int rowCount = this.View.Model.GetEntryRowCount("FSaleOrderEntry");
                        for (int i = 0;i < rowCount; i++) {
                            this.Model.SetValue("FStockOrgId", stockOrgId,i);
                            this.Model.SetValue("FOwnerId", stockOrgId, i);
                            this.Model.SetValue("F_MLD_OutOrg", obj["SaleOrgId_Id"], i);
                        }
                    }
                }
            }
        }
        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);
            if (e.FieldKey.EqualsIgnoreCase("F_MLD_MuiPartBuinessTmp")) {
                DynamicObject obj = this.Model.DataObject;
                //过滤销售方为当前销售组织的多方交易配置
                e.ListFilterParameter.Filter= " F_MILD_ENTRYTYPE = 0 and F_MILD_ORGID="+obj["SaleOrgId_Id"];
            }
        }
    }
}
