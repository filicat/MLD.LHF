using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
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
    [Description("销售退货单审核时，自动生成内部的销售退货和采购退料")]
    [HotUpdate]
    public class ReturnStockAuditOperation : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            BusinessInfo.GetFieldList().ForEach(x => e.FieldKeys.Add(x.Key));
        }

        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            var validators = e.Validators.FindAll(item=>item.GetType().Name.EqualsIgnoreCase("StockBillOwnerKeeperValidator"));
            if (validators.Count > 0) {
                for (int i = 0; i < validators.Count; i++)
                {
                    e.Validators.Remove(validators[i]);
                }
            }
        }

        public override void EndOperationTransaction(EndOperationTransactionArgs e)
        {
            if (e.DataEntitys.Count() > 0) {
                List<OutStockModel> outStockModelList = new List<OutStockModel>();
                foreach (DynamicObject item in e.DataEntitys)
                {
                    if (item != null)
                    {
                        DynamicObjectCollection entryColl = item["SAL_RETURNSTOCKENTRY"] as DynamicObjectCollection;
                        DynamicObjectCollection finColl = item["SAL_RETURNSTOCKFIN"] as DynamicObjectCollection;
                        DynamicObject finObj = finColl[0];
                        //获取销售退货单含多方交易配置的物料行
                        List<DynamicObject> filterList = entryColl.Where(d => d["F_MLD_MuiPartBuinessTmp_Id"] != null && Convert.ToInt32(d["F_MLD_MuiPartBuinessTmp_Id"]) != 0).ToList();
                        if (filterList.Count() <= 0) continue;
                        var groupBySaleOrderId = filterList.GroupBy(p => p["OrderNo"]); //根据销售订单号进行数据分组 考虑合并下推的情况
                        foreach (var groupBy in groupBySaleOrderId)
                        {
                            OutStockModel outStock = new OutStockModel();
                            List<OutStockModel.OutStockEntryModel> outStockEntryList = new List<OutStockModel.OutStockEntryModel>();
                            foreach (DynamicObject obj in groupBy)
                            {
                                outStock.id = Convert.ToInt32(item["id"]);
                                outStock.billNo = item["billNo"].ToString();
                                outStock.date = item["Date"].ToString();
                                outStock.muiPartBuinessTmp = obj["F_MLD_MuiPartBuinessTmp"] as DynamicObject;
                                outStock.stockOrgNumber = getBaseDataNumber(item["StockOrgId"]).ToString();
                                outStock.currNumber = getBaseDataNumber(finObj["LocalCurrID"]).ToString();
                                decimal exchangeRate = Convert.ToDecimal(finObj["ExchangeRate"]);
                                OutStockModel.OutStockEntryModel outStockEntry = new OutStockModel.OutStockEntryModel();
                                outStockEntry.materialNumber = getBaseDataNumber(obj["MaterialID"]).ToString();
                                outStockEntry.qty = Convert.ToDecimal(obj["RealQty"]);
                                outStockEntry.price = Math.Round(Convert.ToDecimal(obj["Price"]) * exchangeRate, 6);
                                outStockEntry.freeFlag = Convert.ToBoolean(obj["IsFree"]);
                                if (obj["StockLocID_Id"] != null && Convert.ToInt32(obj["StockLocID_Id"]) != 0)
                                {
                                    DynamicObject locObj = obj["StockLocID"] as DynamicObject;
                                    if (locObj["F100001"] != null)
                                    {
                                        DynamicObject loc001Obj = locObj["F100001"] as DynamicObject;
                                        outStockEntry.stockLocNumber_F1 = loc001Obj["Number"].ToString();
                                        outStockEntry.stockLcId = Convert.ToInt32(obj["StockLocID_Id"]);
                                    }
                                    if (locObj["F100007"] != null)
                                    {
                                        DynamicObject loc007Obj = locObj["F100007"] as DynamicObject;
                                        outStockEntry.stockLocNumber_F7 = loc007Obj["Number"].ToString();
                                        outStockEntry.stockLcId = Convert.ToInt32(obj["StockLocID_Id"]);
                                    }                                  
                                }
                                if (obj["Lot"] != null)
                                {
                                    outStockEntry.lotNumber = getBaseDataNumber(obj["Lot"]).ToString();
                                }
                                if (obj["MTONO"] != null)
                                {
                                    outStockEntry.mtoNo = obj["MTONO"].ToString();
                                }
                                outStockEntry.stockNumber = getBaseDataNumber(obj["StockID"]).ToString();
                                if (obj["StockstatusId"] != null) {
                                    outStockEntry.stockStatusNumber = getBaseDataNumber(obj["StockstatusId"]).ToString();
                                }
                                outStockEntry.keeperTyperId = obj["KeeperTypeId"].ToString();
                                outStockEntry.keeperId = getBaseDataNumber(obj["KeeperId"]).ToString();
                                if (obj["F_MLD_CPX"] != null && Convert.ToInt32(obj["F_MLD_CPX_Id"]) != 0)
                                {
                                    outStockEntry.productLineNumber = getBaseDataNumber(obj["F_MLD_CPX"]).ToString();
                                }
                                outStockEntryList.Add(outStockEntry);
                            }
                            outStock.outStockEntryModelList = outStockEntryList;
                            outStockModelList.Add(outStock);
                        }
                    }
                }
                if (outStockModelList.Count() > 0)
                {
                    //根据销售出库单的多方交易配置 去生成采购退料单/销售退货单
                    foreach (OutStockModel item in outStockModelList)
                    {
                        DynamicObject muiPartBuinessObj = item.muiPartBuinessTmp;
                        DynamicObjectCollection orgCollDesc = muiPartBuinessObj["MILD_BuinessTemp_Entry"] as DynamicObjectCollection;
                        List<int> orgIds = orgCollDesc.Select(c => Convert.ToInt32(c["F_MILD_ORGID_Id"])).ToList();//获取所有的多方交易组织Id
                        string orgIdStr = string.Join(",", orgIds);
                        string custSelectSql = @"select FCORRESPONDORGID orgId,FUseOrgId useOrgId,FCUSTID,FNumber from T_BD_CUSTOMER  where FCORRESPONDORGID in (" + orgIdStr + ")";
                        DynamicObjectCollection customerColl = DBUtils.ExecuteDynamicObject(Context, custSelectSql); //所有组织对应的客户编码
                        string supperSelectSql = @"select FCORRESPONDORGID orgId,FSUPPLIERID,FUseOrgId useOrgId,FNumber from T_BD_SUPPLIER  where FCORRESPONDORGID in (" + orgIdStr + ") ";
                        DynamicObjectCollection supperColl = DBUtils.ExecuteDynamicObject(Context, supperSelectSql);//所有组织对应的供应商编码
                    
                        String stockOrgNumber = "";  //库存组织编码
                        for (int i = 0; i < orgCollDesc.Count; i++)
                        {
                            DynamicObject currentObj = orgCollDesc[i];
                            DynamicObject orgObj = currentObj["F_MILD_OrgId"] as DynamicObject;
                            int entryType = Convert.ToInt32(currentObj["F_MILD_ENTRYTYPE"]);
                            DynamicObject nextObj = null;
                            DynamicObject beforeObj = null;
                            switch (entryType)
                            {
                                case 2:
                                    //供货方生成销售退货单 客户为中间方的客户 销售/结算组织/货主为供货方
                                    stockOrgNumber = orgObj["Number"].ToString();
                                    beforeObj = orgCollDesc[i - 1];
                                    List<DynamicObject> sellCustList = customerColl.Where(c => c["useOrgId"].ToString().EqualsIgnoreCase(currentObj["F_MILD_OrgId_Id"].ToString()) && c["orgId"].ToString().EqualsIgnoreCase(beforeObj["F_MILD_OrgId_Id"].ToString())).ToList();
                                    if (sellCustList.Count > 0)
                                    {
                                        item.saleOrgNumber = orgObj["Number"].ToString();
                                        item.ownerOrgNumber = item.saleOrgNumber;
                                        item.priceDiscount = Convert.ToDecimal(currentObj["F_MILD_Discount"]);
                                        item.custNumber = sellCustList[0]["FNumber"].ToString();
                                        JSONObject outStock = this.returnStockObj(item);
                                        object returnObj = WebApiServiceCall.Save(this.Context, "SAL_RETURNSTOCK", outStock.ToString());
                                        JSONObject returnJson = getReturnJson(returnObj);
                                        if (!returnJson.GetJSONObject("ResponseStatus").GetBool("IsSuccess"))
                                        {
                                            throw new KDBusinessException("供货方生成销售退货单失败", returnJson.GetJSONObject("ResponseStatus").Get("Errors").ToString());
                                        }
                                       InsertBillNo(item.id, currentObj["F_MILD_OrgId_Id"].ToString(), "销售退货单", returnJson.GetString("Number"), returnJson.GetLong("Id"));
                                    }
                                    else
                                    {
                                        throw new KDBusinessException("供货方生成销售退货单失败", "中间方的内部销售客户找不到");
                                    }
                                    break;
                                case 1:
                                    nextObj = orgCollDesc[i + 1];
                                    beforeObj = orgCollDesc[i - 1];
                                    //中间方 销售退货单
                                    sellCustList = customerColl.Where(c => c["useOrgId"].ToString().EqualsIgnoreCase(currentObj["F_MILD_OrgId_Id"].ToString())
                                    && c["orgId"].ToString().EqualsIgnoreCase(beforeObj["F_MILD_OrgId_Id"].ToString())).ToList();
                                    if (sellCustList.Count > 0)
                                    {
                                        item.saleOrgNumber = orgObj["Number"].ToString();
                                        item.ownerOrgNumber = item.saleOrgNumber;
                                        item.priceDiscount = Convert.ToDecimal(currentObj["F_MILD_Discount"]);
                                        item.custNumber = sellCustList[0]["FNumber"].ToString();
                                        JSONObject outStock = this.returnStockObj(item);
                                        object returnObj = WebApiServiceCall.Save(this.Context, "SAL_RETURNSTOCK", outStock.ToString());
                                        JSONObject returnJson = getReturnJson(returnObj);
                                        if (!returnJson.GetJSONObject("ResponseStatus").GetBool("IsSuccess"))
                                        {
                                            throw new KDBusinessException("中间方生成销售退货单失败", returnJson.GetJSONObject("ResponseStatus").Get("Errors").ToString());
                                        }
                                        InsertBillNo(item.id, currentObj["F_MILD_OrgId_Id"].ToString(), "销售退货单", returnJson.GetString("Number"), returnJson.GetLong("Id"));
                                    }
                                    else
                                    {
                                        throw new KDBusinessException("中间方生成销售退货单失败", "中间方的内部客户找不到");
                                    }
                                    //中间方 采购退料单                                  
                                    List<DynamicObject> purchaserList = supperColl.Where(c => c["useOrgId"].ToString().EqualsIgnoreCase(currentObj["F_MILD_OrgId_Id"].ToString())
                                         && c["orgId"].ToString().EqualsIgnoreCase(nextObj["F_MILD_OrgId_Id"].ToString())).ToList();
                                    if (purchaserList.Count > 0)
                                    {
                                        item.saleOrgNumber = orgObj["Number"].ToString();
                                        item.ownerOrgNumber = item.saleOrgNumber;
                                        item.priceDiscount = Convert.ToDecimal(nextObj["F_MILD_Discount"]);
                                        item.custNumber = purchaserList[0]["FNumber"].ToString();
                                        JSONObject inStock = this.instockReturn(item);
                                        object returnObj = WebApiServiceCall.Save(this.Context, "PUR_MRB", inStock.ToString());
                                        JSONObject returnJson = getReturnJson(returnObj);
                                        if (!returnJson.GetJSONObject("ResponseStatus").GetBool("IsSuccess"))
                                        {
                                            throw new KDBusinessException("中间方生成采购退料单失败", returnJson.GetJSONObject("ResponseStatus").Get("Errors").ToString());
                                        }
                                       InsertBillNo(item.id, currentObj["F_MILD_OrgId_Id"].ToString(), "采购退料单", returnJson.GetString("Number"), returnJson.GetLong("Id"));
                                    }
                                    else
                                    {
                                        throw new KDBusinessException("中间方生成采购退料单失败", "供货方的内部供应商找不到");
                                    }                                  
                                    break;
                                case 0:
                                    //销售方生成采购退料单 供应商为中间方组织的供应商 采购单价折扣为中间方的折扣
                                    nextObj = orgCollDesc[i + 1];
                                    //过滤供应商
                                    purchaserList = supperColl.Where(c => c["useOrgId"].ToString().EqualsIgnoreCase(currentObj["F_MILD_OrgId_Id"].ToString())
                                    && c["orgId"].ToString().EqualsIgnoreCase(nextObj["F_MILD_OrgId_Id"].ToString())).ToList();
                                    if (purchaserList.Count > 0)
                                    {
                                        item.saleOrgNumber = orgObj["Number"].ToString();
                                        item.ownerOrgNumber = item.saleOrgNumber;
                                        item.priceDiscount = Convert.ToDecimal(nextObj["F_MILD_Discount"]);
                                        item.custNumber = purchaserList[0]["FNumber"].ToString();
                                        JSONObject inStock = this.instockReturn(item);
                                        object returnObj = WebApiServiceCall.Save(this.Context, "PUR_MRB", inStock.ToString());
                                        JSONObject returnJson = getReturnJson(returnObj);
                                        if (!returnJson.GetJSONObject("ResponseStatus").GetBool("IsSuccess"))
                                        {
                                            throw new KDBusinessException("销售方生成采购退料单失败", returnJson.GetJSONObject("ResponseStatus").Get("Errors").ToString());
                                        }
                                       InsertBillNo(item.id, currentObj["F_MILD_OrgId_Id"].ToString(), "采购退料单", returnJson.GetString("Number"), returnJson.GetLong("Id"));
                                    }  
                                    else
                                    {
                                        throw new KDBusinessException("销售方生成采购退料单失败", "中间方的内部供应商找不到");
                                    }
                                    break;
                                default:
                                    continue;
                            }
                        }
                    }
                }
            }
        }

        //组装采购退料单
        private JSONObject instockReturn(OutStockModel item)
        {
            JSONObject saveObj = new JSONObject();
            saveObj.Add("IsAutoSubmitAndAudit", true);
            JSONObject modelObj = new JSONObject();
            modelObj.Add("FStockOrgId", getNumberObj(item.stockOrgNumber));
            modelObj.Add("FRequireOrgId", getNumberObj(item.saleOrgNumber));
            modelObj.Add("FDate", item.date);
            modelObj.Add("FOwnerTypeIdHead", "BD_OwnerOrg");
            modelObj.Add("FOwnerIdHead", getNumberObj(item.saleOrgNumber));
            modelObj.Add("FPurchaseOrgId", getNumberObj(item.saleOrgNumber));
            modelObj.Add("FSupplierId", getNumberObj(item.custNumber));
            modelObj.Add("F_MLD_EndBillNo", item.billNo);
            JSONObject finObj = new JSONObject();
            finObj.Add("FSettleCurrId", getNumberObj(item.currNumber));
            finObj.Add("FIsIncludedTax", "false");
            finObj.Add("FISPRICEEXCLUDETAX", "true");
            modelObj.Add("FPURMRBFIN", finObj);

            JSONArray entryArray = new JSONArray();
            foreach (OutStockModel.OutStockEntryModel model in item.outStockEntryModelList)
            {
                JSONObject entryObj = new JSONObject();
                entryObj.Add("FMaterialId", getNumberObj(model.materialNumber));
                entryObj.Add("FStockId ", getNumberObj(model.stockNumber));
                if (model.stockLcId != 0)
                {
                    entryObj.Add("FStockLocId", getIdObj(model));
                }
                if (!StringUtils.IsEmpty(model.lotNumber))
                {
                    entryObj.Add("FLot", getNumberObj(model.lotNumber));
                }
                if (!StringUtils.IsEmpty(model.stockStatusNumber)) {
                    entryObj.Add("FStockStatusId", getNumberObj(model.stockStatusNumber));
                }
                if (!StringUtils.IsEmpty(model.keeperId))
                {
                    entryObj.Add("FKeeperTypeID", model.keeperTyperId);
                    entryObj.Add("FKeeperID", getNumberObj(model.keeperId));
                }
                entryObj.Add("FGiveAway", model.freeFlag);
                entryObj.Add("FKEEPERTYPEID", model.keeperTyperId);
                entryObj.Add("FKEEPERID",getNumberObj(model.keeperId));
                entryObj.Add("FMTONO", model.mtoNo);
                entryObj.Add("FRMREALQTY", model.qty);
                entryObj.Add("FPrice", Math.Round(Convert.ToDecimal(model.price) * item.priceDiscount, 6));
                entryObj.Add("FOWNERTYPEID", "BD_OwnerOrg");
                entryObj.Add("FOWNERID", getNumberObj(item.ownerOrgNumber));
                //entryObj.Add("F_MLD_CPX", getNumberObj(model.productLineNumber));
                entryArray.Add(entryObj);
            }
            modelObj.Add("FPURMRBENTRY", entryArray);
            saveObj.Add("Model", modelObj);
            return saveObj;
        }

        //组装销售退货单
        private JSONObject returnStockObj(OutStockModel item)
        {
            JSONObject saveObj = new JSONObject();
            saveObj.Add("IsAutoSubmitAndAudit", true);
            JSONObject modelObj = new JSONObject();
            modelObj.Add("FSaleOrgId", getNumberObj(item.saleOrgNumber));
            modelObj.Add("FStockOrgId", getNumberObj(item.stockOrgNumber));
            modelObj.Add("FDate", item.date);
            modelObj.Add("FRetcustId", getNumberObj(item.custNumber));
            modelObj.Add("F_MLD_EndBillNo", item.billNo);
            JSONObject finObj = new JSONObject();
            finObj.Add("FSettleCurrId", getNumberObj(item.currNumber));
            finObj.Add("FIsIncludedTax", false);
            finObj.Add("FIsPriceExcludeTax", true);
            modelObj.Add("SubHeadEntity", finObj);
            JSONArray entryArray = new JSONArray();
            foreach (OutStockModel.OutStockEntryModel model in item.outStockEntryModelList)
            {
                JSONObject entryObj = new JSONObject();
                entryObj.Add("FMaterialID", getNumberObj(model.materialNumber));
                entryObj.Add("FStockId ", getNumberObj(model.stockNumber));
                if (model.stockLcId != 0)
                {
                    entryObj.Add("FStockLocId", getIdObj(model));
                }
                if (!StringUtils.IsEmpty(model.lotNumber))
                {
                    entryObj.Add("FLot", getNumberObj(model.lotNumber));
                }
                if (!StringUtils.IsEmpty(model.stockStatusNumber))
                {
                    entryObj.Add("FStockstatusId", getNumberObj(model.stockStatusNumber));
                }
                if (!StringUtils.IsEmpty(model.keeperId))
                {
                    entryObj.Add("FKeeperTypeID", model.keeperTyperId);
                    entryObj.Add("FKeeperID", getNumberObj(model.keeperId));
                }
                entryObj.Add("FIsFree", model.freeFlag);
                entryObj.Add("FMTONO", model.mtoNo);
                entryObj.Add("FRealQty", model.qty);
                entryObj.Add("FPrice", Math.Round(Convert.ToDecimal(model.price) * item.priceDiscount,6));
                entryObj.Add("FOwnerTypeID", "BD_OwnerOrg");
                entryObj.Add("FOWNERID", getNumberObj(item.ownerOrgNumber));
                entryObj.Add("F_MLD_CPX", getNumberObj(model.productLineNumber));
                entryArray.Add(entryObj);
            }
            modelObj.Add("FEntity", entryArray);
            saveObj.Add("Model", modelObj);
            return saveObj;
        }
        private void InsertBillNo(long FID, String orgId, String billType, String billNo, long billId)
        {
            var keyIds = Kingdee.BOS.ServiceHelper.DBServiceHelper.GetSequenceInt64(Context, "T_MLD_MuiPartBuinessReturn", 1);
            String inserSql = String.Format(@"insert into T_MLD_MuiPartBuinessReturn (FEntryId,FID,F_MLD_BuinessType,F_MLD_BuinessOrg,F_MLD_BILLNO,F_MLD_BillId) values ({0},{1},'{2}',{3},'{4}',{5})",
                keyIds.ElementAt(0), FID, billType, orgId, billNo, billId);
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
        //获取基础资料编码
        public object getBaseDataNumber(object entity)
        {
            if (entity == null)
            {
                return "";
            }
            DynamicObject entityObj = entity as DynamicObject;
            if (entityObj == null)
            {
                return "";
            }
            return entityObj["Number"];
        }
        private JSONObject getNumberObj(String number)
        {
            JSONObject obj = new JSONObject();
            obj.Add("FNumber", number);
            return obj;
        }
        private JSONObject getIdObj(OutStockModel.OutStockEntryModel model)
        {
            JSONObject obj = new JSONObject();
            if (model.stockLocNumber_F1 != null)
            {
                obj.Add("FSTOCKLOCID__FF100001", getNumberObj(model.stockLocNumber_F1));
            }
            if (model.stockLocNumber_F7 != null)
            {
                obj.Add("FSTOCKLOCID__FF100007", getNumberObj(model.stockLocNumber_F7));
            }
            return obj;
        }
    }
}
