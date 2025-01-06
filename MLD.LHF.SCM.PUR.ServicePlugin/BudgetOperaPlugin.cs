using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core.List;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS;
using Kingdee.BOS.Core.Metadata.ConvertElement;
using Kingdee.BOS.ServiceHelper;
using System.Data;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.App;
using Kingdee.BOS.Core.Metadata.FormElement;

namespace MLD.LHF.SCM.PUR.ServicePlugin
{
    [System.ComponentModel.Description("Jzc预算校验插件-LHF改"),HotUpdate]
    public class BudgetOperaPlugin: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            BusinessInfo.GetFieldList().ForEach(x => e.FieldKeys.Add(x.Key));
        }

        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            var BudgetCheckValidator = new BudgetCheckValidatorPlugin();//新增的校验器
            BudgetCheckValidator.EntityKey = "Requisition";
            e.Validators.Add(BudgetCheckValidator);//添加校验器
        }


    }
}
