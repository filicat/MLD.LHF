using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace MLD.LHF.SCM.Service.PlugIn.PurchaseOrder.Validator
{
    [HotUpdate, Description("[校验插件]校验采购订单的OA采购策略单号")]
    public class SaveValidator : AbstractOperationServicePlugIn
    {
        //public override void OnPreparePropertys(PreparePropertysEventArgs e)
        //{
        //    base.OnPreparePropertys(e);
        //    e.FieldKeys.Add("F_MLD_PurStrategy_BillNo");
        //}

        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);

            OaStrategyBillValidator validator = new OaStrategyBillValidator();
            validator.AlwaysValidate = true;

            validator.EntityKey = "FBillHead";

            e.Validators.Add(validator);
        }

        private class OaStrategyBillValidator : AbstractValidator
        {
            public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
            {
                if (dataEntities == null || dataEntities.Length <= 0)
                {
                    return;
                }
                foreach (ExtendedDataEntity obj in dataEntities)
                {
                    //object purchaserId = obj.DataEntity["F_POTZ_Remarks_BZ"];
                    string oaStrategyBill = string.Empty;
                    if (obj.DataEntity.TryGetValue("F_MLD_PurStrategy_BillNo", out var value) && value != null)
                    {
                        oaStrategyBill = value.ToString();
                    }
                    if (string.IsNullOrWhiteSpace(oaStrategyBill))
                    {
                        ValidatorAddError(validateContext, obj, "MLD-ERRCODE_001", "OA采购策略单号为空");
                    } else
                    {
                        string sql = $@"SELECT TOP 1 1 
                                        FROM formtable_main_543 T1 
                                        WHERE 
                                        T1.djbh='{oaStrategyBill}'";
                        DataTable dataTable = exe_Query(sql);
                        if (dataTable.Rows.Count == 0)
                        {
                            string msg = $@"OA采购策略单号[{oaStrategyBill}]在OA不存在";
                            ValidatorAddError(validateContext, obj, "MLD-ERRCODE_002", msg);
                        }
                    }
                }
            }
        }

        private static void ValidatorAddError(ValidateContext validateContext, ExtendedDataEntity obj, string id, string message)
        {
            validateContext.AddError(obj.DataEntity,
                            new ValidationErrorInfo(
                                "F_MLD_PurStrategy_BillNo", // 出现错误的字段, 可以为空
                                obj.DataEntity["Id"].ToString(),
                                obj.DataEntityIndex,
                                obj.RowIndex,
                                id,
                                message,
                                "保存" + obj.BillNo,
                                ErrorLevel.Error
                                ));
        }

        /// <summary>
        /// 数据的查询，返回一个数据表
        /// </summary>
        /// <param name="strSql"></param>
        /// <returns></returns>
        public static DataTable exe_Query(string strSql)
        {
            string strConn = "Server=10.10.1.31;Database=ecology;User Id=k3cloud;Password=k3cloud@2024;";
            SqlConnection sqlConn = new SqlConnection(strConn);           //实例化连接对象 
            SqlDataAdapter sqlDa = new SqlDataAdapter(strSql, sqlConn);    //实例化适配器对象，并指明要执行的SQl查询和要使用的连接
            DataTable dt = new DataTable();                 //实例化一个数据表，用于保存果询结果
            sqlDa.Fill(dt);                                 //让数据适配器去执行查询，并将结果填充到dt
            return dt;                                      //返回结果，dt这个数据表
        }
    }
}
