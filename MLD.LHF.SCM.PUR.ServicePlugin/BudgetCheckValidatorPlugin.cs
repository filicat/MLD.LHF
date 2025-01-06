using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Util;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Log;
using System.Data.SqlClient;
using System.Data;


namespace MLD.LHF.SCM.PUR.ServicePlugin
{
    [HotUpdate]
    [System.ComponentModel.Description("Jzc采购申请的预算校验-LHF改")]
    public class BudgetCheckValidatorPlugin : AbstractValidator
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
            string formId = validateContext.BusinessInfo.GetForm().Id;
            if (dataEntities == null || dataEntities.Length <= 0)
            {
                return;// 传入数据包为空
            }
            List<KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>> list_Error = new List<KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>>();
            bool hasError = false;
            foreach (var et in dataEntities)
            {
                /**
                 1、先获取物料编码、数量和采购组织
                 2、根据物料编码+采购组织获取到物料价格
                 3、获取历史订单  采购组织、数量和价格
                 4、计算出当前订单所需要的金额和历史占用金额以及可用金额
                 */

                /**
                 20240822取数逻辑：
                1、优先取PR单中OA传过来的单价，如果没有就取物料参考价（需要分组织）
                2、历史价格直接取采购申请单中的金额 
                3、因为参考成本价的单位是基本单位，所以这里的金额需要是  基本单位数量*参考成本价
                 */

                /**
                 20240824逻辑调整：
                1、历史价格取数改成单据字段：  OA或成本金额 F_MLD_HZJE
                2、目前价格取数也改成OA或成本金额
                3、物料没有OA或成本金额时不允许提交
                 
                 */
                string fid = et["Id"].GetString();
                string appDate = DateTime.Now.ToString("yyyyMM");
                string OAPlanNo = (et["F_MLD_OAJHDH"] as LocaleValue)[Context.UserLocale.LCID];
                string projectCategory = et["F_MLD_PROJECTCATEGORY"].GetString();
                string orgNum = (et["ApplicationOrgId"] as DynamicObject)["Number"].GetString();
                string billTypeNum = (et["BillTypeID"] as DynamicObject)["Number"].GetString();
                /**
                 20240711和黄主管确认预算不分组织
                 string orgNum = (et["ApplicationOrgId"] as DynamicObject)["Number"].GetString();
                 string appDate =Convert.ToDateTime(et["ApplicationDate"]).ToString("yyyyMM");
                 */

                /**
                 20240806 黄主管要求项目类别为其他的时候只校验  OA计划单号，不校验日期和产品线
                 */

                /*
                 20241008 LHF
                 HK组织特殊处理, HK的PR不算入已花费预算和申请预算
                 */

                /*
                 * 20250103 LHF
                 当PR的单据类型是：模具申请或者OA计划单号是“YF”开头的，则不受时间限制。
                 即不受字段年月限制
                 QT类别本就不收时间限制, 故主要根据是否检查年月修改"标准流程"
                 */
                bool skipYMChk = OAPlanNo.StartsWith("YF") || String.Equals(billTypeNum, "CGSQD08_SYS", StringComparison.OrdinalIgnoreCase);

                #region //其他的特殊处理 
                if (projectCategory.Equals("QT"))//其他项目类别特殊处理
                {
                    if (!IsExistOAPlanNoByOther(OAPlanNo))
                    {
                        hasError = true;
                        KeyValuePair<ExtendedDataEntity, ValidationErrorInfo> error = new KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>
                            (et, new ValidationErrorInfo("", fid, et.DataEntityIndex, 0, "E1", $"单据编号为【{et.BillNo}】的单据保存（预算校验）不通过，OA计划单号【{OAPlanNo}】在项目类别的为【其他】的预算中不存在！", "单据合法性检查", ErrorLevel.Error));
                        list_Error.Add(error);
                        break;
                    }
                    var entryObjs = et["ReqEntry"] as DynamicObjectCollection;
                    var entry = entryObjs.GroupBy(x => new { MatId = (x["MaterialId"] as DynamicObject) == null ? "" : (x["MaterialId"] as DynamicObject)["Id"].GetString(), MatNum = (x["MaterialId"] as DynamicObject) == null ? "" : (x["MaterialId"] as DynamicObject)["Number"].GetString(), Amount = Convert.ToDecimal(x["F_MLD_HZJE"]) }).Select(g =>
                    {
                        var p = new { MatNum = g.Key.MatNum, MatId = g.Key.MatId, Amoount = g.Key.Amount };
                        return p;
                    }).ToArray().ToList();
                    foreach (var item in entry)
                    {

                        decimal UsableAmount = Math.Round(GetOtherUsableAmount(fid, OAPlanNo, GetProjectCategory(projectCategory)), 6);
                        decimal ApplyAmount = Math.Round(item.Amoount, 6);
                        if (ApplyAmount == 0)
                        {
                            hasError = true;
                            KeyValuePair<ExtendedDataEntity, ValidationErrorInfo> error = new KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>
                                (et, new ValidationErrorInfo("", fid, et.DataEntityIndex, 0, "E1", $"单据编号为【{et.BillNo}】的单据保存（预算校验）不通过，物料【{item.MatNum}】申请金额为【{ApplyAmount}】，请到【物料】信息中维护参考成本价！", "单据合法性检查", ErrorLevel.Error));
                            list_Error.Add(error);
                            break;
                        }
                        // HK的PR不算入申请预算
                        ApplyAmount = String.Equals(orgNum, "HK", StringComparison.OrdinalIgnoreCase) ? 0 : ApplyAmount;
                        if (ApplyAmount > UsableAmount)
                        {
                            hasError = true;
                            KeyValuePair<ExtendedDataEntity, ValidationErrorInfo> error = new KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>
                                (et, new ValidationErrorInfo("", fid, et.DataEntityIndex, 0, "E1", $"单据编号为【{et.BillNo}】的单据保存（预算校验）不通过，项目类别为【其他】的物料【{item.MatNum}】当前申请金额为【{ApplyAmount}】可用预算金额为【{UsableAmount}】", "单据合法性检查", ErrorLevel.Error));
                            list_Error.Add(error);
                        }
                    }
                }
                #endregion
                #region //标准处理
                else
                {
                    if (!IsExistOAPlanNo(appDate, OAPlanNo, skipYMChk))
                    {
                        hasError = true;
                        // 构建错误信息
                        string errorMessageTemplate = $"单据编号为【{et.BillNo}】的单据保存（预算校验）不通过，OA计划单号【{OAPlanNo}】";
                        if (!skipYMChk)
                        {
                            errorMessageTemplate += "在年月【{appDate}】中";
                        }
                        errorMessageTemplate += "不存在！";
                        KeyValuePair<ExtendedDataEntity, ValidationErrorInfo> error = new KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>
                            (et, new ValidationErrorInfo("", fid, et.DataEntityIndex, 0, "E1", errorMessageTemplate, "单据合法性检查", ErrorLevel.Error));
                        list_Error.Add(error);
                        break;
                    }
                    var entryObjs = et["ReqEntry"] as DynamicObjectCollection;
                    var entry = entryObjs.GroupBy(x => new { ProductLine = (x["F_MLD_YSCPX"] as DynamicObject) == null ? "" : (x["F_MLD_YSCPX"] as DynamicObject)["FDataValue"].GetString(), MatId = (x["MaterialId"] as DynamicObject) == null ? "" : (x["MaterialId"] as DynamicObject)["Id"].GetString(), MatNum = (x["MaterialId"] as DynamicObject) == null ? "" : (x["MaterialId"] as DynamicObject)["Number"].GetString(), Amount = Convert.ToDecimal(x["F_MLD_HZJE"]) }).Select(g =>
                    {
                        var p = new { CPX = g.Key.ProductLine, MatNum = g.Key.MatNum, Amount = g.Key.Amount };
                        return p;
                    }).ToArray().ToList();
                    foreach (var item in entry)
                    {
                        //可使用预算金额
                        decimal UsableAmount = Math.Round(GetUsableAmount(fid, item.CPX, appDate, OAPlanNo, GetProjectCategory(projectCategory), skipYMChk), 6);
                        //申请预算金额
                        decimal ApplyAmount = Math.Round(item.Amount, 6);
                        if (ApplyAmount == 0)
                        {
                            hasError = true;
                            KeyValuePair<ExtendedDataEntity, ValidationErrorInfo> error = new KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>
                                (et, new ValidationErrorInfo("", fid, et.DataEntityIndex, 0, "E1", $"单据编号为【{et.BillNo}】的单据保存（预算校验）不通过，预算产品线为【{item.CPX}】的物料【{item.MatNum}】申请金额为【{ApplyAmount}】，请到【物料】信息中维护参考成本价！", "单据合法性检查", ErrorLevel.Error));
                            list_Error.Add(error);
                            break;
                        }
                        // HK的PR不算入申请预算
                        ApplyAmount = String.Equals(orgNum, "HK", StringComparison.OrdinalIgnoreCase) ? 0 : ApplyAmount;
                        if (ApplyAmount > UsableAmount)
                        {
                            hasError = true;
                            KeyValuePair<ExtendedDataEntity, ValidationErrorInfo> error = new KeyValuePair<ExtendedDataEntity, ValidationErrorInfo>
                                (et, new ValidationErrorInfo("", fid, et.DataEntityIndex, 0, "E1", $"单据编号为【{et.BillNo}】的单据保存（预算校验）不通过，预算产品线为【{item.CPX}】的物料【{item.MatNum}】当前申请金额为【{ApplyAmount}】可用预算金额为【{UsableAmount}】", "单据合法性检查", ErrorLevel.Error));
                            list_Error.Add(error);
                        }
                    }
                }
                #endregion

            }
            if (hasError)
            {
                foreach (var errorItem in list_Error)
                {
                    validateContext.AddError(errorItem.Key, errorItem.Value);
                }
            }
        }
        /// <summary>
        /// 获取已使用预算金额
        /// LHF 新增参数skipYMChk， 当skipYMChk为真，UsableAmount和BudgetAmount计算时不以ny为条件
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="cpxName"></param>
        /// <param name="ny"></param>
        /// <param name="planNo"></param>
        /// <param name="projectCategory"></param>
        /// <param name="skipYMChk"></param>
        /// <returns></returns>
        public decimal GetUsableAmount(string fid, string cpxName, string ny, string planNo, string projectCategory, bool skipYMChk)
        {
            decimal UsableAmount = 0;
            string sql = $@"SELECT *
                            FROM v_tyz_MRPPlanOrder10_31 T1
                            WHERE 1=1
                            AND T1.djbh='{planNo}'
                            AND T1.项目类别='{projectCategory}'
                            AND T1.cpxmc='{cpxName}'";
            if (!skipYMChk)
            {
                sql += $" AND T1.dy={ny}";
            }
            DataTable dt = exe_Query(sql);
            if (dt.Rows.Count > 0)
            {
                decimal BudgetAmount = Convert.ToDecimal(dt.Rows[0]["planfmount"]);
                string kdsql = $@"/*dialect*/ SELECT SUM(T2.F_MLD_HZJE) 'Amount',CPX.FNAME 'CPX',F2.FCAPTION 'projectCategory',T3.F_MLD_OAJHDH,LEFT(CONVERT(varchar, FAPPLICATIONDATE, 112), 6) 'dy'
                            FROM T_PUR_REQUISITION T1
                            LEFT JOIN T_PUR_REQUISITION_l T3 ON T1.FID=T3.FID AND T3.FLOCALEID=2052 AND T1.FMANUALCLOSE=0
                            LEFT JOIN T_PUR_REQENTRY T2 ON T1.FID=T2.FID AND T2.FMRPTERMINATESTATUS='A'
							LEFT JOIN T_BD_MATERIAL M1 ON T2.FMATERIALID=M1.FMATERIALID
                            LEFT JOIN T_BD_SUPPLIER S1 ON T2.FSUGGESTSUPPLIERID=S1.FSUPPLIERID AND S1.FCORRESPONDORGID=0
                            LEFT JOIN T_ORG_ORGANIZATIONS O1 ON T1.FAPPLICATIONORGID=O1.FORGID
                            LEFT JOIN (
								select a.FENTRYID,b.fdatavalue 'FNAME'
								FROM T_BAS_ASSISTANTDATAENTRY a
								join T_BAS_ASSISTANTDATAENTRY_L b on a.FENTRYID=b.FENTRYID AND b.FLOCALEID=2052
								JOIN T_BAS_ASSISTANTDATA T1 ON A.FID=T1.FID
								JOIN T_BAS_ASSISTANTDATA_L T2 ON T1.FID=T2.FID AND T2.FLOCALEID=2052 AND T2.FNAME='预算产品线'
							)CPX ON T2.F_MLD_YSCPX=CPX.FENTRYID
                            LEFT JOIN T_META_FORMENUMITEM F1 ON F1.FVALUE=T1.F_MLD_PROJECTCATEGORY   --枚举项主表
                            LEFT JOIN  T_META_FORMENUMITEM_L F2 ON F1.FENUMID=F2.FENUMID  AND F2.FLOCALEID=2052	--枚举项多语言表
                            JOIN T_META_FORMENUM_L F3L ON F3L.FID=F1.FID AND F2.FLOCALEID=2052 AND F3L.FNAME='MLD项目类别'
                            WHERE 1=1
                            AND (T1.FDOCUMENTSTATUS='B' OR T1.FDOCUMENTSTATUS='C')
                            AND F2.FCAPTION='{projectCategory}'
                            AND T3.F_MLD_OAJHDH='{planNo}'
                            AND CPX.FNAME='{cpxName}'
                            AND T1.FID<>{fid}
                            AND O1.FNumber<>'HK'";
                if (!skipYMChk)
                {
                    kdsql += $" AND LEFT(CONVERT(varchar, FAPPLICATIONDATE, 112), 6)='{ny}'";
                }
                kdsql += " GROUP BY CPX.FNAME,F2.FCAPTION,T3.F_MLD_OAJHDH,LEFT(CONVERT(varchar, FAPPLICATIONDATE, 112), 6)";
                decimal SpentBudgetAmount = DBServiceHelper.ExecuteScalar<decimal>(Context, kdsql, 0, null);
                UsableAmount = BudgetAmount - SpentBudgetAmount;
            }
            return UsableAmount;
        }
        /// <summary>
        /// 获取【其他】类别已使用预算金额
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="cpxName"></param>
        /// <param name="ny"></param>
        /// <param name="planNo"></param>
        /// <param name="projectCategory"></param>
        /// <returns></returns>
        public decimal GetOtherUsableAmount(string fid, string planNo, string projectCategory = "其他")
        {
            decimal UsableAmount = 0;
            string sql = $@"SELECT *
                            FROM v_tyz_MRPPlanOrder10_31 T1
                            WHERE 1=1
                            AND T1.djbh='{planNo}'
                            AND T1.项目类别='{projectCategory}'";
            DataTable dt = exe_Query(sql);
            if (dt.Rows.Count > 0)
            {
                decimal BudgetAmount = Convert.ToDecimal(dt.Rows[0]["planfmount"]);
                string kdsql = $@"/*dialect*/SELECT SUM(T2.F_MLD_HZJE) 'Amount',F2.FCAPTION 'projectCategory',T3.F_MLD_OAJHDH
                            FROM T_PUR_REQUISITION T1
                            LEFT JOIN T_PUR_REQUISITION_l T3 ON T1.FID=T3.FID AND T3.FLOCALEID=2052 AND T1.FMANUALCLOSE=0
                            LEFT JOIN T_PUR_REQENTRY T2 ON T1.FID=T2.FID AND T2.FMRPTERMINATESTATUS='A'
							LEFT JOIN T_BD_MATERIAL M1 ON T2.FMATERIALID=M1.FMATERIALID
                            LEFT JOIN T_BD_SUPPLIER S1 ON T2.FSUGGESTSUPPLIERID=S1.FSUPPLIERID AND S1.FCORRESPONDORGID=0
                            LEFT JOIN T_ORG_ORGANIZATIONS O1 ON T1.FAPPLICATIONORGID=O1.FORGID
                            LEFT JOIN (
								select a.FENTRYID,b.fdatavalue 'FNAME'
								FROM T_BAS_ASSISTANTDATAENTRY a
								join T_BAS_ASSISTANTDATAENTRY_L b on a.FENTRYID=b.FENTRYID AND b.FLOCALEID=2052
								JOIN T_BAS_ASSISTANTDATA T1 ON A.FID=T1.FID
								JOIN T_BAS_ASSISTANTDATA_L T2 ON T1.FID=T2.FID AND T2.FLOCALEID=2052 AND T2.FNAME='预算产品线'
							)CPX ON T2.F_MLD_YSCPX=CPX.FENTRYID
                            LEFT JOIN T_META_FORMENUMITEM F1 ON F1.FVALUE=T1.F_MLD_PROJECTCATEGORY   --枚举项主表
                            LEFT JOIN  T_META_FORMENUMITEM_L F2 ON F1.FENUMID=F2.FENUMID  AND F2.FLOCALEID=2052	--枚举项多语言表
                            JOIN T_META_FORMENUM_L F3L ON F3L.FID=F1.FID AND F2.FLOCALEID=2052 AND F3L.FNAME='MLD项目类别'
                            WHERE 1=1
                            AND (T1.FDOCUMENTSTATUS='B' OR T1.FDOCUMENTSTATUS='C')
                            AND F2.FCAPTION='{projectCategory}'
                            AND T3.F_MLD_OAJHDH='{planNo}'
                            AND T1.FID<>{fid}
                            AND O1.FNumber<>'HK'
                            GROUP BY F2.FCAPTION,T3.F_MLD_OAJHDH";
                decimal SpentBudgetAmount = DBServiceHelper.ExecuteScalar<decimal>(Context, kdsql, 0, null);
                UsableAmount = BudgetAmount - SpentBudgetAmount;
            }
            return UsableAmount;
        }
        /// <summary>
        /// 根据物料获取汇总金额
        /// </summary>
        /// <param name="orgNum"></param>
        /// <param name="MatNum"></param>
        /// <param name="Qty"></param>
        /// <returns></returns>
        public decimal GetAmountByMat(string MatId, decimal Qty)
        {
            string sql = $@"/*dialect*/SELECT T2.FTAXPRICE
	                                        ,ROW_NUMBER() OVER (PARTITION BY T2.FMATERIALID ORDER BY T2.FEFFECTIVEDATE DESC) AS rn
                                        FROM T_PUR_PRICELIST T1
	                                        JOIN T_PUR_PRICELISTENTRY T2 ON T1.FID=T2.FID
                                        WHERE 1=1
	                                        AND T1.FDOCUMENTSTATUS='C'
	                                        AND T2.FDISABLESTATUS='B'
	                                        AND T1.FPRICETYPE=2
	                                        AND T2.FEXPIRYDATE>=GETDATE()
	                                        AND T2.FMATERIALID='{MatId}'
                                        GROUP BY 
	                                        T2.FMATERIALID,T2.FTAXPRICE,T2.FEFFECTIVEDATE,T2.FEXPIRYDATE";
            decimal price = DBServiceHelper.ExecuteScalar<decimal>(Context, sql, 0, null);
            decimal amount = Qty * price;
            return amount;
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
        /// <summary>
        /// 获取项目类别
        /// </summary>
        /// <param name="projectValue"></param>
        /// <returns></returns>
        public string GetProjectCategory(string projectValue)
        {
            string sql = $@"/*dialect*/SELECT F2.FCAPTION
                    FROM T_META_FORMENUMITEM F1
                    JOIN  T_META_FORMENUMITEM_L F2 ON F1.FENUMID=F2.FENUMID  AND F2.FLOCALEID=2052	--枚举项多语言表
                    JOIN T_META_FORMENUM_L F3L ON F3L.FID=F1.FID AND F2.FLOCALEID=2052 AND F3L.FNAME='MLD项目类别'
                    WHERE 1=1
                    AND F1.FVALUE='{projectValue}'";
            return DBServiceHelper.ExecuteScalar<string>(Context, sql, "", null);
        }
        /// <summary>
        /// 查询当月是否存在该计划单号
        /// LHF 新增参数skipYMChk， 当skipYMChk为真，不要求年月一致
        /// </summary>
        /// <param name="yearMonth"></param>
        /// <param name="OAPlanNo"></param>
        /// <returns></returns>
        public bool IsExistOAPlanNo(string yearMonth, string OAPlanNo, bool skipYMChk)
        {
            bool isExist = false;
/*            string sql = $@"SELECT *
                            FROM v_tyz_MRPPlanOrder10_31 T1
                            WHERE 1=1
                            AND DY={yearMonth}
                            AND DJBH='{OAPlanNo}'";*/
            // 构建 SQL 查询语句
            string sql = $@"SELECT *
                    FROM v_tyz_MRPPlanOrder10_31 T1
                    WHERE 1=1
                          AND DJBH='{OAPlanNo}'";

            if (!skipYMChk)
            {
                sql += $" AND DY={yearMonth}";
            }
            DataTable dt = exe_Query(sql);
            if (dt.Rows.Count > 0)
            {
                isExist = true;
            }
            return isExist;
        }
        /// <summary>
        /// 查询其他类别是否存在该计划单号
        /// </summary>
        /// <param name="yearMonth"></param>
        /// <param name="OAPlanNo"></param>
        /// <returns></returns>
        public bool IsExistOAPlanNoByOther(string OAPlanNo)
        {
            bool isExist = false;
            string sql = $@"SELECT *
                            FROM v_tyz_MRPPlanOrder10_31 T1
                            WHERE 1=1
                            AND DJBH='{OAPlanNo}'";
            DataTable dt = exe_Query(sql);
            if (dt.Rows.Count > 0)
            {
                isExist = true;
            }
            return isExist;
        }
    }
}
