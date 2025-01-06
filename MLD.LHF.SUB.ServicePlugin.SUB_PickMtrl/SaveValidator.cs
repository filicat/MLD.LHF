using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Log;
using Kingdee.BOS.App.Data;

namespace MLD.LHF.SUB.ServicePlugin.SUB_PickMtrl
{
    [HotUpdate]
    [System.ComponentModel.Description("Jzc委外领料保存时检验")]
    public class SaveValidator : AbstractValidator
    {
        /// <summary>
        /// 校验器初始化
        /// </summary>
        /// <param name="validateContext"></param>
        /// <param name="ctx"></param>
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
            /**
             1、委外订单对应的采购订单先审核的需要先领完料，才能做后面的委外订单的领料；前提条件：相同的供应商、委外物料编码、委外工序编码(F_MLD_GX)
             2、根据采购订单的审核时间做时间排序
             */
            foreach (var data in dataEntities)
            {
                DynamicObjectCollection entryObjs = data["Entity"] as DynamicObjectCollection;
                string fid = data["Id"].GetString();
                foreach (var item in entryObjs) 
                {

                    string SROBillEntryId = item["SubReqEntryId"].GetString();
                    if (!SROMatIsControl(SROBillEntryId))
                    {
                        continue;
                    }
                    string POBillNo = item["POOrderBillNo"].GetString();
                    string POBillSeq = item["POOrderSeq"].GetString();
                    string message = POApproveDateCompare(POBillNo, POBillSeq);
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        validateContext.AddError(data,new ValidationErrorInfo("", fid,data.DataEntityIndex,0,"MLD",message, "单据合法性检查", ErrorLevel.Error));
                    }
                }
            }
        }

        /// <summary>
        /// 委外订单产品料号是否需要管控
        /// </summary>
        /// <param name="SROBillEntryId"></param>
        /// <returns></returns>
        public bool SROMatIsControl(String SROBillEntryId)
        {
            string sql = $@"SELECT 1
FROM T_SUB_REQORDERENTRY T0
         LEFT JOIN T_BD_MATERIAL M0 ON T0.FMATERIALID = M0.FMATERIALID
WHERE 1=1 
  AND T0.FENTRYID = {SROBillEntryId}
  AND M0.FNUMBER NOT LIKE '1.L%'"; // 委外产品料号是1.L开头的，不作管控。
            DynamicObjectCollection SROrderObjs = DBUtils.ExecuteDynamicObject(Context, sql);
            if (SROrderObjs != null && SROrderObjs.Count > 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 采购订单审核日期对比
        /// </summary>
        /// <param name="POBillNo"></param>
        /// <param name="POBillSeq"></param>
        /// <returns></returns>
        public string POApproveDateCompare(string POBillNo, string POBillSeq)
        {
            string messages = "";
            string sql = $@"/*dialect*/
SELECT 
	T1.FBILLNO,t1.FBILLTYPEID,T1.FAPPROVEDATE,M1.FNUMBER 'MatNum',S1.FNUMBER 'SupNum',P1.FNUMBER 'ProNum',O1.FNUMBER 'OrgNum'
FROM T_PUR_POORDER T1
	JOIN T_PUR_POORDERENTRY T2 ON T1.FID=T2.FID
	JOIN T_BD_MATERIAL M1 ON T2.FMATERIALID=M1.FMATERIALID
	JOIN T_BD_SUPPLIER S1 ON T1.FSUPPLIERID=S1.FSUPPLIERID
	JOIN T_BAS_PREBDTHREE P1 ON T2.F_MLD_PROCESS=P1.FID
	JOIN T_ORG_ORGANIZATIONS O1 ON T1.FPURCHASEORGID=O1.FORGID
WHERE 1=1
	AND T1.FDOCUMENTSTATUS='C'
	AND T1.FBILLNO='{POBillNo}'
	AND T2.FSEQ='{POBillSeq}'";
            DynamicObjectCollection POOrderObjs = DBUtils.ExecuteDynamicObject(Context, sql);
            if (POOrderObjs != null && POOrderObjs.Count > 0)
            {
                string mixBillSql = $@"/*dialect*/
SELECT TOP 100
	T1.FBILLNO,t1.FBILLTYPEID,T1.FAPPROVEDATE,M1.FNUMBER 'MatNum',S1.FNUMBER 'SupNum',P1.FNUMBER 'ProNum',O1.FNUMBER 'OrgNum'
FROM T_PUR_POORDER T1
	JOIN T_PUR_POORDERENTRY T2 ON T1.FID=T2.FID
	JOIN T_BD_MATERIAL M1 ON T2.FMATERIALID=M1.FMATERIALID
	JOIN T_BD_SUPPLIER S1 ON T1.FSUPPLIERID=S1.FSUPPLIERID
	JOIN T_BAS_PREBDTHREE P1 ON T2.F_MLD_PROCESS=P1.FID
	JOIN T_SUB_PPBOM SP1 ON T1.FBILLNO=SP1.FPURORDERNO AND T2.FSEQ=SP1.FPURORDERENTRYSEQ
	JOIN T_SUB_PPBOMENTRY SP2 ON SP1.FID=SP2.FID 
	JOIN T_SUB_PPBOMENTRY_Q SP2Q ON SP2.FENTRYID=SP2Q.FENTRYID AND  SP2Q.FNOPICKEDQTY>0
	JOIN T_ORG_ORGANIZATIONS O1 ON T1.FPURCHASEORGID=O1.FORGID
WHERE 1=1
	AND T1.FDOCUMENTSTATUS='C'
	AND T1.FCLOSESTATUS='A'
	AND M1.FNUMBER='{POOrderObjs[0]["MatNum"].GetString()}'
	AND S1.FNUMBER='{POOrderObjs[0]["SupNum"].GetString()}'
	AND P1.FNUMBER='{POOrderObjs[0]["ProNum"].GetString()}'
    AND O1.FNUMBER='{POOrderObjs[0]["OrgNum"].GetString()}'
ORDER BY M1.FAPPROVEDATE";
                DynamicObjectCollection mixPOOrderObjs = DBUtils.ExecuteDynamicObject(Context, mixBillSql);
                if (mixBillSql != null && mixPOOrderObjs.Count > 0)
                {
                    DateTime currentApproveDate = Convert.ToDateTime(POOrderObjs[0]["FAPPROVEDATE"]);
                    DateTime mixcurrentApproveDate = Convert.ToDateTime(mixPOOrderObjs[0]["FAPPROVEDATE"]);
                    if (currentApproveDate > mixcurrentApproveDate)//当前行的采购订单审核日期》当前信息最小的审核日期
                    {
                        string mixPOOrderNo = mixPOOrderObjs[0]["FBILLNO"].GetString();
                        messages = $@"保存失败！
提示：因存在同供应商/同物料/同工序的前采购订单号为[{mixPOOrderNo}],还没有全部委外领料，故不能做新的委外领料。即要把之前的采购订单委外领完料才可作新的领料。";
                    }
                }

            }
            return messages;
        }
    }
}
