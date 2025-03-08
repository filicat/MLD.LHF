using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.DynamicForm.OperationWebService;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Interaction;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLD.LHF.JZC.ZL.MLD.Sale.Buiness.Bill.PlugIn
{
    [Description("Web API返回接口插件")]
    [HotUpdate]
    public class OutStockBillPlugIn : AbstractBillPlugIn
    {
        public override void OnAfterWebApiOperation(AfterWebApiOperationArgs e)
        {
            base.OnAfterWebApiOperation(e);
            WebServiceContext webContext = e.WebContext;
            Dictionary<string, object> dictionary = webContext.ResponseDTO as Dictionary<string, object>;
            IInteractionResult operationResult = webContext.OperationResult;
            if (operationResult != null && operationResult.InteractionContext != null)
            {
                DynamicObject result = null;
                operationResult.InteractionContext.Option.TryGetVariableValue("STK_InvCheckResult", out result);
                if (result != null && result["Entry"] != null)
                {
                    DynamicObjectCollection dynamicObjectCollection = result["Entry"] as DynamicObjectCollection;
                    string text = "物料[";
                    foreach (DynamicObject item in dynamicObjectCollection)
                    {
                        DynamicObject dynamicObject = item;
                        text = text + item["MaterialNumber"] + ",";
                    }
                    Dictionary<string, object> dictionary2 = dictionary["Result"] as Dictionary<string, object>;
                    Dictionary<string, object> dictionary3 = dictionary2["ResponseStatus"] as Dictionary<string, object>;
                    List<object> list = dictionary3["Errors"] as List<object>;
                    Dictionary<string, object> dictionary4 = list[0] as Dictionary<string, object>;
                    dictionary4["Message"] = text + "]库存不足";
                }
            }
        }
    }
}
