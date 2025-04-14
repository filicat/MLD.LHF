using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.Demo.Plugin.ServicePlugin
{
    [HotUpdate, Description("[服务插件]DemoPO保存服务插件-校验器")]
    public class DemoPOServiceOnAddValidatorPlugin : AbstractOperationServicePlugIn
    {
        public override void OnAddValidators(AddValidatorsEventArgs e)
        {
            base.OnAddValidators(e);

            DemoValidator demoValidator = new DemoValidator();
            demoValidator.AlwaysValidate = true;

            e.Validators.Add(demoValidator);
        }

        private class DemoValidator : AbstractValidator
        {
            public override void Validate(ExtendedDataEntity[] dataEntities, ValidateContext validateContext, Context ctx)
            {
                foreach (ExtendedDataEntity obj in dataEntities)
                {
                    object purchaserId = obj.DataEntity["PurchaserId"];
                    if (null == purchaserId)
                    {
                        validateContext.AddError(obj.DataEntity,
                            new ValidationErrorInfo(
                                "PurchaserId", // 出现错误的字段, 可以为空
                                obj.DataEntity["Id"].ToString(),
                                obj.DataEntityIndex,
                                obj.RowIndex,
                                "DEMO_001",
                                "单据编号" + obj.BillNo + "采购订单没有录入采购员",
                                "提交" + obj.BillNo,
                                ErrorLevel.Error
                                ));
                    }
                }
            }
        }
    }
}
