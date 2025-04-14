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
    [HotUpdate, Description("[服务插件]DemoPO保存服务插件")]
    public class DemoPOServiceOnAddPlugin : AbstractOperationServicePlugIn
    {
        public override void BeforeExecuteOperationTransaction(BeforeExecuteOperationTransaction e)
        {
            base.BeforeExecuteOperationTransaction(e);

            e.CancelMessage = "测试终止操作执行";
            e.Cancel = true;
        }
    }
}
