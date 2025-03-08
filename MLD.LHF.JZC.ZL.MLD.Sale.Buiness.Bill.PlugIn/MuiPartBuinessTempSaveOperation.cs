using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core;
using Kingdee.BOS;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Log;
using Kingdee.BOS.App.Data;

namespace MLD.LHF.JZC.ZL.MLD.Sale.Buiness.Bill.PlugIn
{
    [Description("多方交易模板保存时自动生成交易方向；校验组织是否有内部客户/供应商")]
    [HotUpdate]
    public class MuiPartBuinessTempSaveOperation : AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            BusinessInfo.GetFieldList().ForEach(x => e.FieldKeys.Add(x.Key));
        }
       
        //拼接 交易方向
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);
            foreach (ExtendedDataEntity item in e.SelectedRows)
            {
                DynamicObject obj = item.DataEntity;
                DynamicObjectCollection entryCollect = obj["MILD_BuinessTemp_Entry"] as DynamicObjectCollection;
                String buinessDesc = "";
                foreach (DynamicObject entry in entryCollect) {
                    if (entry["F_MILD_ORGID"] == null) {
                        continue;
                    }
                    DynamicObject org = entry["F_MILD_ORGID"] as DynamicObject;
                    var nameValue = org["Name"] as LocaleValue;
                    string orgName = nameValue[2052];
                    if (!StringUtils.IsEmpty(buinessDesc)) {
                        buinessDesc+="→";
                    }
                    buinessDesc = buinessDesc + orgName;
                }                  
                obj["F_POTZ_BuinessDesc"] = buinessDesc;
            }
        }
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            OrgValidtor validator = new OrgValidtor();
            e.Validators.Add(validator);
        }
        //校验器，销售方和中间方组织要有对应的供应商和销售客户。
        private class OrgValidtor : AbstractValidator
        {
            public override void InitializeConfiguration(ValidateContext validateContext, Context ctx)
            {
                base.InitializeConfiguration(validateContext, ctx);
                if (validateContext.BusinessInfo != null)
                {
                    EntityKey = validateContext.BusinessInfo.GetEntity(0).Key;
                }
            }
            public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
            {
                if (dataEntities == null || dataEntities.Count() == 0) {
                    return;
                }
                foreach (ExtendedDataEntity obj in dataEntities) {
                    DynamicObjectCollection entryColl = obj.DataEntity["MILD_BuinessTemp_Entry"] as DynamicObjectCollection;
                   List<object> orgIdList = entryColl.Where(item=>!item["F_MILD_ENTRYTYPE"].ToString().EqualsIgnoreCase("2")).Select(item=>item["F_MILD_ORGID_Id"]).ToList();
                    string orgIdStr = string.Join(",", orgIdList);
                    string custSelectSql = @"select FCORRESPONDORGID orgId,FCUSTID from T_BD_CUSTOMER  where FCORRESPONDORGID in ("+ orgIdStr + ") ";
                    DynamicObjectCollection customerColl = DBUtils.ExecuteDynamicObject(Context, custSelectSql);
                    string supperSelectSql = @"select FCORRESPONDORGID orgId,FSUPPLIERID from T_BD_SUPPLIER  where FCORRESPONDORGID in (" + orgIdStr + ") ";
                    DynamicObjectCollection supperColl = DBUtils.ExecuteDynamicObject(Context, supperSelectSql);
                    foreach (DynamicObject entry in entryColl) {
                        string entryType = entry["F_MILD_ENTRYTYPE"].ToString();
                        DynamicObject org = entry["F_MILD_ORGID"] as DynamicObject;
                        LocaleValue nameValue = org["Name"] as LocaleValue;
                        if (!entryType.EqualsIgnoreCase("2")) { //供货组织不需要校验
                            List<DynamicObject> filterCust = customerColl.Where(item => item["orgId"].ToString().EqualsIgnoreCase(entry["F_MILD_ORGID_Id"].ToString())).ToList();
                            if (filterCust.Count == 0)
                            {
                                validateContext.AddError(obj, new ValidationErrorInfo("F_MILD_ORGID",Convert.ToString(obj.DataEntity[0]),obj.DataEntityIndex,obj.RowIndex,
                                    "MLDORG","【"+nameValue[2052]+"】下没有绑定内部客户，不允许配置","组织校验", ErrorLevel.Error
                                ));
                                break;
                            }
                            List<DynamicObject> filterSuppier = supperColl.Where(item => item["orgId"].ToString().EqualsIgnoreCase(entry["F_MILD_ORGID_Id"].ToString())).ToList();
                            if (filterSuppier.Count == 0)
                            {
                                validateContext.AddError(obj, new ValidationErrorInfo("F_MILD_ORGID", Convert.ToString(obj.DataEntity[0]), obj.DataEntityIndex, obj.RowIndex,
                                    "MLDORG", "【" + nameValue[2052] + "】下没有绑定内部供应商，不允许配置", "组织校验", ErrorLevel.Error
                                ));
                                break;
                            }
                        }                      
                    }               
                }               
            }
        }
    }
    
}
