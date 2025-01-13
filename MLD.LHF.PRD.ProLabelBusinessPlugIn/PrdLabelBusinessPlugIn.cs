using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceFacade;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Text;

namespace MLD.LHF.PRD.PrdLabelBusinessPlugIn
{

    [Description("生产订单标签表单插件"), HotUpdate]
    public class PrdLabelBussinessPlugIn : AbstractBillPlugIn
    {
        public const string MO_FK = "F_MLD_PrdMo_BillNo";
        private const string Date_FK = "F_MLD_Date";
        private const string GenLabelBtn = "POTZ_tbGenerateLabelEntry";
        private const string MAT_FK = "F_MLD_Material";
        private const string Org_FK = "F_MLD_Org";
        private const string PerQty_FK = "F_MLD_PerBoxQty";
        private const string PrdQty_FK = "F_MLD_PrdQty";
        private const string LABEL_SN_FK = "F_MLD_LABEL_SN";
        private const string PackageQty_FK = "F_MLD_PackageQty";
        private const int SERIAL_LEN = 6;
        private const string Entity_Key = "FEntity";

        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);

            if (e.BarItemKey.Equals(GenLabelBtn, StringComparison.OrdinalIgnoreCase))
            {
                // 校验日期, 生产数量, 每箱数量, 组织有效
                object pDate = Model.GetValue(Date_FK);
                if (null == pDate)
                {
                    View.ShowMessage("日期为空, 无法生成标签");
                    return;
                }
                DateTime billDate = (DateTime)pDate;
                decimal prdQtyDec = (decimal)Model.GetValue(PrdQty_FK);
                decimal perQtyDec = (decimal)Model.GetValue(PerQty_FK);
                DynamicObject org = Model.GetValue(Org_FK) as DynamicObject;
                if (Decimal.Zero == prdQtyDec)
                {
                    View.ShowMessage("生产数量为0, 无法生成标签");
                    return;
                }
                if (Decimal.Zero == perQtyDec)
                {
                    View.ShowMessage("每箱数量为0, 无法生成标签");
                    return;
                }
                int prdQty = (int)Math.Ceiling(prdQtyDec);
                int perQty = (int)Math.Ceiling(perQtyDec);
                EntryEntity entryEntity = this.View.BusinessInfo.GetEntryEntity(Entity_Key);
                DynamicObjectCollection entryRows = this.Model.GetEntityDataObject(entryEntity);
                int maxSerial = 0;
                int prdQtyGen = 0;
                bool hasErr = false;
                string errMsg = "";
                for (int i = 0; i < entryRows.Count; i++)
                {
                    DynamicObject row = entryRows[i];
                    if (null == row[LABEL_SN_FK] && 0 == (long)row[PackageQty_FK])
                    {
                        Model.DeleteEntryRow(Entity_Key, i);
                        i--;
                        continue;
                    }

                    //View.ShowMessage($"Row{i + 1} {DynamicObjectToJson(row)}");
                    //View.ShowMessage($"Row{i + 1}: SN {row[LABEL_SN_FK].GetType().FullName} {row[LABEL_SN_FK]}\nPQ: {row[PackageQty_FK].GetType().FullName} {row[PackageQty_FK]}");
                    long pq = (long)row[PackageQty_FK];
                    //View.ShowMessage($"PQ: {pq.GetType().FullName} {pq}");
                    prdQtyGen += (int)pq;
                    string sn = (string)(row[LABEL_SN_FK]??"");
                    if (sn.Length == 0)
                    {
                        continue;
                    }
                    else if (sn.Length < SERIAL_LEN)
                    {
                        hasErr = true;
                        errMsg = $"流水码{sn}长度异常, 无法获取到此流水码的流水部分";
                        break;
                    }
                    else
                    {
                        string snSerialPart = sn.Substring(sn.Length - SERIAL_LEN);
                        if (int.TryParse(snSerialPart, out int serial))
                        {
                            maxSerial = Math.Max(maxSerial, serial);
                        }
                        else
                        {
                            hasErr = true;
                            errMsg = $"流水码{sn}后{SERIAL_LEN}位无法解析成数字";
                            break;
                        }
                    }
                }
                if (hasErr)
                {
                    View.ShowErrMessage(errMsg, "检查原有流水项目时错误");
                    return;
                }
                if (prdQty <= prdQtyGen)
                {
                    View.ShowMessage($"标签上的总数量({prdQtyGen})已大于等于生产数量({prdQty})");
                    return;
                }
                int fullPackageQty = Math.DivRem(prdQty - prdQtyGen, perQty, out int remainder);
                bool hasTail = (remainder > 0);
                int packageQty = fullPackageQty + (hasTail ? 1 : 0);
                int perRowQty = entryRows.Count;
                string ymd = billDate.ToString("yyMMdd");
                maxSerial = Math.Max(maxSerial, GetDBMaxSerial((long)org["Id"], ymd));
                Model.BatchCreateNewEntryRow(Entity_Key, packageQty);
                //string msg = "";
                for (int i = 0; i < packageQty; i++)
                {
                    int idx = perRowQty + i;
                    //msg += $"\n{idx}";
                    base.Model.SetValue(LABEL_SN_FK, ymd + (maxSerial + i + 1).ToString("D6"), idx);
                    if (i == packageQty -1)
                    {
                        base.Model.SetValue(PackageQty_FK, hasTail ? remainder : perQty, idx);
                    }else
                    {
                        base.Model.SetValue(PackageQty_FK, perQty, idx);
                    }
                    this.View.UpdateView(LABEL_SN_FK, idx);
                    this.View.UpdateView(PackageQty_FK, idx);
                }
                //View.ShowMessage(msg);
            }
        }

        private int GetDBMaxSerial(long orgId, string ymd)
        {
            string sql = $@"/*dialect*/SELECT
                    MAX(
                            CASE
                                WHEN PATINDEX('%[^0-9]%', SUBSTRING(F_MLD_LABEL_SN, LEN('{ymd}') + 1, LEN(F_MLD_LABEL_SN))) > 0
                                    THEN CAST(SUBSTRING(F_MLD_LABEL_SN, LEN('{ymd}') + 1, PATINDEX('%[^0-9]%', SUBSTRING(F_MLD_LABEL_SN, LEN('{ymd}') + 1, LEN(F_MLD_LABEL_SN))) - 1) AS INT)
                                ELSE CAST(SUBSTRING(F_MLD_LABEL_SN, LEN('{ymd}') + 1, LEN(F_MLD_LABEL_SN)) AS INT) -- 如果右边全是数字，直接截取剩余部分
                                END
                    ) AS MaxValue
                FROM
                    T_MLD_PrdMoLabelEntry T1 LEFT JOIN T_MLD_PrdMoLabel T2 ON T1.FID = T2.FID
                WHERE
                    T1.F_MLD_LABEL_SN LIKE '{ymd}%' AND T2.F_MLD_ORG={orgId}";
            return DBServiceHelper.ExecuteScalar<int>(this.Context, sql, 0, null);
        }

        public override void BeforeF7Select(BeforeF7SelectEventArgs e)
        {
            base.BeforeF7Select(e);

            //const string MoKey = "F_MLD_PrdMo_BillNo";
            if (e.FieldKey.Equals(MO_FK, StringComparison.OrdinalIgnoreCase))
            {
                ListShowParameter param = new ListShowParameter();
                param.FormId = "Prd_MO";// 【物料】基础资料 业务对象标识，此处基础资料、单据类型都可以
                param.IsLookUp = true;
                param.ListFilterParameter.Filter = "FDOCUMENTSTATUS = 'C'";
                this.View.ShowForm(param, new Action<FormResult>((actionResult =>
                {
                    if (actionResult == null || actionResult.ReturnData == null) return;
                    ListSelectedRowCollection listData = actionResult.ReturnData as ListSelectedRowCollection;
                    if (listData.IsEmpty()) return;
                    if (listData.Count > 1)
                    {
                        // 报错
                        this.View.ShowErrMessage("选择生产订单数量不能大于1", "选择生产订单数错误", MessageBoxType.Error);
                        return;
                    }
                    ListSelectedRow selectedRow = listData[0];
                    string billNo = selectedRow.BillNo;
                    Model.SetValue(MO_FK, billNo);
                    DynamicObject dataRowDy = ((DynamicObjectDataRow)selectedRow.DataRow).DynamicObject;
                    //View.ShowMessage($"Model Get Mat: {DynamicObjectToJson(dataRow.DynamicObject)}");
                    Model.SetValue(MAT_FK, dataRowDy["FMaterialId_Id"]);
                    Model.SetValue(PrdQty_FK, dataRowDy["FQTY"]);
                    int matId = Convert.ToInt32((Model.GetValue(MAT_FK) as DynamicObject)["Id"]);
                    Model.SetValue(PerQty_FK, GetMatBoxStandardQty(matId));
                    Model.SetValue(Org_FK, dataRowDy["FPrdOrgId_Id"]);
                    View.UpdateView(MO_FK);
                    View.UpdateView(MAT_FK);
                    View.UpdateView(PerQty_FK);
                })));
            }
        }
        private static string DynamicObjectToJson(object obj)
        {
            var jsonSerializerProxy = new JsonSerializerProxy(Encoding.UTF8, false);
            var jsonData = jsonSerializerProxy.Serialize(obj);
            return jsonData;
        }

        private decimal GetMatBoxStandardQty(int matId)
        {
            string sql = $@"SELECT T0.F_MLD_QUANTITYCARTON
FROM T_BD_MATERIAL T0
         LEFT JOIN T_BD_MATERIALSTOCK T1 ON T0.FMATERIALID = T1.FMATERIALID
WHERE T0.FMATERIALID = {matId};";
            return DBServiceHelper.ExecuteScalar<decimal>(this.Context, sql, 0, null);
        }
    }
}