using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.WebApi.FormService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace MLD.LHF.JZC.ZL.MLD.Sale.Buiness.Bill.PlugIn
{
    [Description("销售退货单反审核时，删除多方交易单据")]
    [HotUpdate]
    public class ReturnStockUnAuditOperation : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            BusinessInfo.GetFieldList().ForEach(x => e.FieldKeys.Add(x.Key));
        }

        public override void BeginOperationTransaction(BeginOperationTransactionArgs e)
        {
            foreach (DynamicObject item in e.DataEntitys)
            {
                if (item != null)
                {
                    DynamicObjectCollection muiPartBuinessColl = item["F_MLD_MuiPartBuinessReturn"] as DynamicObjectCollection;
                    if (muiPartBuinessColl.Count <= 0)
                    {
                        continue;
                    }
                    List<String> outBillNoList = new List<string>();
                    List<int> outBillIdList = new List<int>();

                    List<String> inStockBillNoList = new List<string>();
                    List<int> inStockBillIdList = new List<int>();
                    List<DynamicObject> muiPartBuinessCollDesc = muiPartBuinessColl.OrderByDescending(d => Convert.ToInt32(d["Id"])).ToList();
                    foreach (DynamicObject coll in muiPartBuinessCollDesc)
                    {
                        if (coll["F_MLD_BuinessType"].ToString().EqualsIgnoreCase("销售退货单"))
                        {
                            outBillNoList.Add(coll["F_MLD_BILLNO"].ToString());
                            outBillIdList.Add(Convert.ToInt32(coll["F_MLD_BillId"]));
                        }
                        else
                        {
                            inStockBillNoList.Add(coll["F_MLD_BILLNO"].ToString());
                            inStockBillIdList.Add(Convert.ToInt32(coll["F_MLD_BillId"]));
                        }
                    }
                    //反审核采购退料的下游单据 负数应付单
                    AfterBillOperation("T_AP_PAYABLEENTRY", "AP_Payable", inStockBillNoList);
                    //再反审采购入库
                    UnAudit("PUR_MRB", inStockBillIdList, inStockBillNoList);
                    Delete("PUR_MRB", inStockBillIdList, inStockBillNoList);

                    // 反审核销售出库单的下游单据 红字应收单
                    AfterBillOperation("t_AR_receivableEntry", "AR_receivable", outBillNoList);
                    //反审销售出库，
                    UnAudit("SAL_RETURNSTOCK", outBillIdList, outBillNoList);
                    Delete("SAL_RETURNSTOCK", outBillIdList, outBillNoList);

                    //删除原始销售出库单上的多方交易单据体数据
                    deleteEntry(Convert.ToInt32(item["id"]));
                }
            }
        }
        private void UnAudit(String formId, List<int> ids, List<String> billNoList)
        {
            JSONObject baseObj = new JSONObject();
            baseObj.Add("Ids", String.Join(",", ids));
            object returnObj = WebApiServiceCall.UnAudit(Context, formId, baseObj.ToString());
            JSONObject returnJson = getReturnJson(returnObj);
            if (!returnJson.GetJSONObject("ResponseStatus").GetBool("IsSuccess"))
            {
                throw new KDBusinessException("【" + String.Join(",", billNoList) + "】反审失败", returnJson.GetJSONObject("ResponseStatus").Get("Errors").ToString());
            }
        }
        private void Delete(String formId, List<int> ids, List<String> billNoList)
        {
            JSONObject baseObj = new JSONObject();
            baseObj.Add("Ids", String.Join(",", ids));
            object returnObj = WebApiServiceCall.Delete(Context, formId, baseObj.ToString());
            JSONObject returnJson = getReturnJson(returnObj);
            if (!returnJson.GetJSONObject("ResponseStatus").GetBool("IsSuccess"))
            {
                throw new KDBusinessException("【" + String.Join(",", billNoList) + "】删除失败", returnJson.GetJSONObject("ResponseStatus").Get("Errors").ToString());
            }
        }
        /**
          * 针对出入库下游的应收应付操作
          **/
        private void AfterBillOperation(String afterFormIdTable, String afterFormId, List<String> billNoList)
        {
            String sql = String.Format(@"select FID from " + afterFormIdTable + " where FSOURCEBILLNO in('" + string.Join("','", billNoList) + "')");
            DynamicObjectCollection billNOList = DBUtils.ExecuteDynamicObject(this.Context, sql);
            if (billNOList.Count > 0)
            {
                List<int> idList = billNOList.Select(item => Convert.ToInt32(item["FID"])).ToList();
                try
                {
                    UnAudit(afterFormId, idList, billNoList);
                    Delete(afterFormId, idList, billNoList);
                }
                catch (KDBusinessException e)
                {
                    throw e;
                }
            }
        }
        private void deleteEntry(long FID)
        {
            String inserSql = String.Format(@"delete from T_MLD_MuiPartBuinessReturn where FID={0}", FID);
            DBUtils.Execute(Context, inserSql);
        }
        private JSONObject getReturnJson(object returnObj)
        {
            JSONObject jbObj = JSONObject.Parse(JsonConvert.SerializeObject(returnObj));
            if (jbObj == null)
            {
                return new JSONObject();
            }
            return jbObj.GetJSONObject("Result");
        }
    }
}
