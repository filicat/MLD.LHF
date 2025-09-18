using Kingdee.BOS;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.Operation;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.ServiceArgs;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Resource;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.K3.Core.MFG.EntityHelper;
using Kingdee.K3.Core.MFG.Utils;
using Kingdee.K3.MFG.ServiceHelper.PRD;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.Demo.Plugin.DynamicFormPlugin
{
    [HotUpdate, Description("[动态表单插件]成套领料Test")]
    public class TestDynamicFormWholeSetPickMtrl : AbstractDynamicFormPlugIn
    {
        public override void BarItemClick(BarItemClickEventArgs e)
        {
            base.BarItemClick(e);
            string barItemKey;
            if ((barItemKey = e.BarItemKey) != null)
            {
                if (barItemKey == "tbLHFTest")
                {
                    this.TestCreatePickMtrl(e);
                    return;
                }
            }
        }

        private void TestCreatePickMtrl(BarItemClickEventArgs e)
        {
            DynamicObjectCollection dynamicObjectCollection = this.View.Model.DataObject["MoEntity"] as DynamicObjectCollection;
            if (dynamicObjectCollection.IsEmpty<DynamicObject>())
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("生产订单明细中没有数据！", "0151515153530000016512", SubSystemType.MFG, new object[0]), "", MessageBoxType.Notice);
                e.Cancel = true;
                return;
            }
            List<DynamicObject> list = new List<DynamicObject>();
            string arg = string.Empty;
            bool value = this.Model.GetValue("FSetsPickMtrl", -1, false, null);
            if (value)
            {
                list = (from w in dynamicObjectCollection
                        where w.GetDynamicValue("PickQty", 0m) == 0m
                        select w).ToList<DynamicObject>();
                arg = ResManager.LoadKDString("领料套数", "0151515153530030041576", SubSystemType.MFG, new object[0]);
            }
            else
            {
                list = (from w in dynamicObjectCollection
                        where w.GetDynamicValue("ReinforceQty", 0m) == 0m
                        select w).ToList<DynamicObject>();
                arg = ResManager.LoadKDString("补齐套数", "0151515153530030042105", SubSystemType.MFG, new object[0]);
            }
            if (!list.IsEmpty<DynamicObject>())
            {
                int[] values = (from s in list
                                select s.GetDynamicValue("Seq", 0)).ToArray<int>();
                this.View.ShowErrMessage(string.Format(ResManager.LoadKDString("第{0}行，{1}不能为0！", "0151515153530030041578", SubSystemType.MFG, new object[0]), string.Join<int>(",", values), arg), "", MessageBoxType.Notice);
                return;
            }
            List<long> moEntryIds = (from s in dynamicObjectCollection
                                     select s.GetDynamicValue("MoEntryId", 0L)).ToList<long>();
            StringBuilder stringBuilder = new StringBuilder();
            List<DynamicObject> list2 = SetOfPickMtrServiceHelper.GetPpBomInfo(base.Context, moEntryIds);
            IEnumerable<DynamicObject> enumerable = from x in list2
                                                    where x.GetDynamicValue("FMATERIALTYPE", string.Empty) == "2"
                                                    select x;
            if (!enumerable.IsEmpty<DynamicObject>())
            {
                IEnumerable<string> values2 = from x in enumerable
                                              select string.Format(ResManager.LoadKDString("生产用料清单编号【{0}】序号【{1}】项次【{2}】", "0151515153530030042107", SubSystemType.MFG, new object[0]), x.GetDynamicValue("FBILLNO", string.Empty), x.GetDynamicValue("FSEQ", 0), x.GetDynamicValue("FREPLACEGROUP", 0));
                stringBuilder.AppendLine(string.Join(",", values2) + ResManager.LoadKDString("的物料子项类型为返还件;", "0151515153530030042108", SubSystemType.MFG, new object[0]));
            }
            IEnumerable<DynamicObject> enumerable2 = from x in list2
                                                     where x.GetDynamicValue("FISSUETYPE", string.Empty) != "1" && x.GetDynamicValue("FISSUETYPE", string.Empty) != "3"
                                                     select x;
            if (!enumerable2.IsEmpty<DynamicObject>())
            {
                IEnumerable<string> values3 = from x in enumerable2
                                              select string.Format(ResManager.LoadKDString("生产用料清单编号【{0}】序号【{1}】项次【{2}】", "0151515153530030042107", SubSystemType.MFG, new object[0]), x.GetDynamicValue("FBILLNO", string.Empty), x.GetDynamicValue("FSEQ", 0), x.GetDynamicValue("FREPLACEGROUP", 0));
                stringBuilder.AppendLine(string.Join(",", values3) + ResManager.LoadKDString("的物料发料方式不为直接领料或者调拨领料;", "0151515153530030042109", SubSystemType.MFG, new object[0]));
            }
            list2 = (from x in list2
                     where x.GetDynamicValue("FMATERIALTYPE", string.Empty) != "2"
                     select x).ToList<DynamicObject>();
            list2 = (from x in list2
                     where x.GetDynamicValue("FISSUETYPE", string.Empty) == "1" || x.GetDynamicValue("FISSUETYPE", string.Empty) == "3"
                     select x).ToList<DynamicObject>();
            if (list2.IsEmpty<DynamicObject>())
            {
                this.View.ShowErrMessage(ResManager.LoadKDString("无符合条件的数据！", "0151515153530000016514", SubSystemType.MFG, new object[0]) + stringBuilder.ToString(), "", MessageBoxType.Notice);
                return;
            }
            List<BusinessObject> list3 = new List<BusinessObject>();
            List<long> list4 = (from i in list2
                                select i.GetDynamicValue("FSUPPLYORG", 0L)).Distinct<long>().ToList<long>();
            foreach (long lngOrgId in list4)
            {
                list3.Add(new BusinessObject(lngOrgId)
                {
                    Id = "PRD_PickMtrl",
                    SubSystemId = this.View.Model.SubSytemId
                });
            }
            List<PermissionAuthResult> source = PermissionServiceHelper.FuncPermissionAuth(this.View.Context, list3, "fce8b1aca2144beeb3c6655eaf78bc34");
            if ((from i in source
                 select i.Passed).Contains(false))
            {
                e.Cancel = true;
                this.View.ShowErrMessage(string.Format(ResManager.LoadKDString("用户“{0}”没有生产领料单的新增权限", "0151515153530030042106", SubSystemType.MFG, new object[0]), base.Context.UserName), "", MessageBoxType.Notice);
                return;
            }
            List<ListSelectedRow> list5 = new List<ListSelectedRow>();
            for (int j = 0; j < list2.Count<DynamicObject>(); j++)
            {
                string primaryKeyValue = Convert.ToString(list2[j].GetDynamicValue("FID", 0L));
                string dynamicValue = list2[j].GetDynamicValue("FBILLNO", string.Empty);
                list5.Add(new ListSelectedRow(primaryKeyValue, list2[j].GetDynamicValue("FENTRYID", 0L).ToString(), j + 1, "PRD_PPBOM")
                {
                    BillNo = dynamicValue,
                    EntryEntityKey = "FEntity"
                });
            }
            List<ConvertRuleElement> convertRules = ConvertServiceHelper.GetConvertRules(base.Context, "PRD_PPBOM", "PRD_PickMtrl");
            try
            {
                ConvertRuleElement convertRuleElement = (from w in convertRules
                                                         where w.Id.Equals("PRD_WholeSetPickMtrl")
                                                         select w).FirstOrDefault<ConvertRuleElement>();
                if (convertRuleElement != null && !list5.IsEmpty<ListSelectedRow>())
                {
                    PushArgs pushArgs = new PushArgs(convertRuleElement, list5.ToArray());
                    pushArgs.TargetBillTypeId = "f4f46eb78a7149b1b7e4de98586acb67";
                    OperateOption operateOption = OperateOption.Create();
                    operateOption.SetVariableValue("ValidatePermission", true);
                    operateOption.SetVariableValue("MoEntityData", dynamicObjectCollection.ToList<DynamicObject>());
                    operateOption.SetVariableValue("PPBomInfo", list2.ToList<DynamicObject>());
                    operateOption.SetVariableValue("isSetsPickMtrl", value);
                    ConvertOperationResult convertOperationResult = ConvertServiceHelper.Push(base.Context, pushArgs, operateOption);
                    List<DynamicObject> objs = (from p in convertOperationResult.TargetDataEntities
                                                select p.DataEntity).ToArray<DynamicObject>().ToList<DynamicObject>();
                    this.ShowResult(objs, "PRD_PickMtrl");
                }
            }
            catch (KDExceptionValidate kdexceptionValidate)
            {
                this.View.ShowErrMessage(kdexceptionValidate.Message, kdexceptionValidate.ValidateString, MessageBoxType.Notice);
            }
        }

        private void ShowResult(List<DynamicObject> objs, string targetFormId)
        {
            BillShowParameter billShowParameter = new BillShowParameter();
            billShowParameter.ParentPageId = this.View.PageId;
            if (objs.Count == 1)
            {
                billShowParameter.Status = OperationStatus.ADDNEW;
                string key = "_ConvertSessionKey";
                string text = "ConverOneResult";
                billShowParameter.CustomParams.Add(key, text);
                this.View.Session[text] = objs[0];
                billShowParameter.FormId = targetFormId;
            }
            else
            {
                if (objs.Count <= 1)
                {
                    return;
                }
                billShowParameter.FormId = "BOS_ConvertResultForm";
                string key2 = "ConvertResults";
                this.View.Session[key2] = objs.ToArray();
                billShowParameter.CustomParams.Add("_ConvertResultFormId", targetFormId);
            }
            if (this.View.Context.UserToken.ToLowerInvariant().Equals("bosidetest"))
            {
                billShowParameter.OpenStyle.ShowType = ShowType.Default;
            }
            else
            {
                billShowParameter.OpenStyle.ShowType = ShowType.MainNewTabPage;
            }
            this.View.ShowForm(billShowParameter);
        }
    }
}
