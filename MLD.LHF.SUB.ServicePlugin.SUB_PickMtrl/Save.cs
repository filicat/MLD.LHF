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

namespace MLD.LHF.SUB.ServicePlugin.SUB_PickMtrl
{
    [HotUpdate,System.ComponentModel.Description("Jzc委外领料单保存插件")]
    public class Save: AbstractOperationServicePlugIn
    {
        public override void OnPreparePropertys(PreparePropertysEventArgs e)
        {
            base.OnPreparePropertys(e);
            BusinessInfo.GetFieldList().ForEach(x => e.FieldKeys.Add(x.Key));
        }

        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);
            var saveValidator = new SaveValidator();//新增的校验器
            //saveValidator.EntityKey = "Requisition";
            e.Validators.Add(saveValidator);//添加校验器
        }
    }
}
