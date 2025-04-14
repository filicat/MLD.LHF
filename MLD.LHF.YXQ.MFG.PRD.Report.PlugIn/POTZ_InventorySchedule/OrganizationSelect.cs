using Kingdee.BOS;
using Kingdee.BOS.Core.CommonFilter.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm.PlugIn.ControlModel;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Permission;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.YXQ.MFG.PRD.Report.PlugIn.POTZ_InventorySchedule
{
    [Description("获取组织"), HotUpdate]
   public class OrganizationSelect : AbstractCommonFilterPlugIn
    {
        private List<long> lstOrgList = new List<long>();

        //TreeNodeClick事件,修改成业务组织
        public override void TreeNodeClick(TreeNodeArgs e)
        {
            base.TreeNodeClick(e);
            this.SetDefaultValue("F_MLD_ZZ");
        }
        private void SetDefaultValue(string sOrgFieldKey)
        {
            if (this.View.ParentFormView != null)
            {
                this.lstOrgList = this.GetPermissionOrg(this.View.ParentFormView.BillBusinessInfo.GetForm().Id);
                List<EnumItem> organization = this.GetOrganization(sOrgFieldKey);
                ComboFieldEditor fieldEditor = this.View.GetFieldEditor<ComboFieldEditor>(sOrgFieldKey, 0);
                fieldEditor.SetComboItems(organization);
                object value = this.Model.GetValue(sOrgFieldKey);
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    long item = sOrgFieldKey.ToUpperInvariant().Equals("FSALEORGID") ? 101L : 107L;
                    if (base.Context.CurrentOrganizationInfo.FunctionIds.Contains(item))
                    {
                        this.Model.SetValue(sOrgFieldKey, base.Context.CurrentOrganizationInfo.ID);
                    }
                }
            }
        }

        private List<EnumItem> GetOrganization(string sOrgFieldKey)
        {
            List<EnumItem> list = new List<EnumItem>();
            List<SelectorItemInfo> list2 = new List<SelectorItemInfo>();
            list2.Add(new SelectorItemInfo("FORGID"));
            list2.Add(new SelectorItemInfo("FNUMBER"));
            list2.Add(new SelectorItemInfo("FNAME"));
            long num = sOrgFieldKey.ToUpperInvariant().Equals("FSALEORGID") ? 101L : 107L;
            string text = (this.lstOrgList == null || this.lstOrgList.Count == 0) ? "FORGID=-1" : string.Format("FORGID in ({0})", string.Join<long>(",", this.lstOrgList.ToArray()));
            text += string.Format(" AND FORGFUNCTIONS LIKE '%{0}%' ", num.ToString());
            QueryBuilderParemeter para = new QueryBuilderParemeter
            {
                FormId = "ORG_Organizations",
                SelectItems = list2,
                FilterClauseWihtKey = text
            };
            DynamicObjectCollection dynamicObjectCollection = QueryServiceHelper.GetDynamicObjectCollection(base.Context, para, null);
            foreach (DynamicObject current in dynamicObjectCollection)
            {
                list.Add(new EnumItem(new DynamicObject(EnumItem.EnumItemType))
                {
                    EnumId = current["FORGID"].ToString(),
                    Value = current["FORGID"].ToString(),
                    Caption = new LocaleValue(Convert.ToString(current["FName"]), base.Context.UserLocale.LCID)

                });
            }
            return list;
        }

        private List<long> GetPermissionOrg(string formId)
        {
            BusinessObject bizObject = new BusinessObject
            {
                Id = formId,
                PermissionControl = this.View.ParentFormView.BillBusinessInfo.GetForm().SupportPermissionControl,
                SubSystemId = this.View.ParentFormView.Model.SubSytemId
            };
            return PermissionServiceHelper.GetPermissionOrg(base.Context, bizObject, "6e44119a58cb4a8e86f6c385e14a17ad");

        }

    }
}