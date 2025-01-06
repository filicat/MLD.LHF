using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.ENG.ServicePlugin.BOM
{
    [HotUpdate, Description("标准工时校验器插件")]
    public class ProductWorkTimeValidator : AbstractValidator
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
            try
            {
                string formId = validateContext.BusinessInfo.GetForm().Id;
                if (dataEntities == null || dataEntities.Length <= 0)
                {
                    return;// 传入数据包为空
                }
                List<KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>> list_Error = new List<KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>>();
                foreach (var data in dataEntities)
                {
                    string matNum = GetBaseValue(data["MaterialId"], "Number");
                    if (matNum.StartsWith("3"))//只3开头的产成品才进行校验
                    {
                        if (IsExistsWorkTime(matNum))
                        {
                            return;
                        }
                        else
                        {
                            string fid = data["Id"].GetString();
                            validateContext.AddError(data, new ValidationErrorInfo("", fid, data.DataEntityIndex, 0, "E1", $"单据编号为【{data.BillNo}】的单据提交校验（标准工时维护）不通过，物料【{matNum}】未维护标准工时，请到【标准工时维护】中该产品标准工时！", "单据合法性检查", ErrorLevel.Error));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new KDException("WJW", "校验标准工时错误：" + ex.Message);
            }
        }

        /// <summary>
        /// 获取基础资料中的值
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string GetBaseValue(object obj, string value)
        {
            return obj == null ? "" : (obj as DynamicObject)[value].GetString();
        }
        /// <summary>
        /// 是否存在标准工时
        /// </summary>
        /// <param name="matNum"></param>
        /// <returns></returns>
        private bool IsExistsWorkTime(string matNum)
        {
            bool isExists = false;
            string sql = $@"/*dialect*/SELECT T1.FBILLNO,O1.FNUMBER 'ORGNUM',M1.FNUMBER 'MATNUM'
                    FROM T_CA_STDHOURSETTING T1
                    LEFT JOIN T_CA_STDHOURSETENTRY T2 ON T2.FID = T1.FID
                    LEFT JOIN T_ORG_ORGANIZATIONS O1 ON T1.FACCTGORGID=O1.FORGID
                    LEFT JOIN T_BD_MATERIAL M1 ON T2.FPRODUCTID=M1.FMATERIALID
                    WHERE 1=1
                    AND M1.FNUMBER='{matNum}'
            ";
            DynamicObjectCollection queryObjs = DBUtils.ExecuteDynamicObject(Context, sql);
            if (queryObjs != null && queryObjs.Count > 0)
                isExists = true;
            return isExists;
        }
    }
}
