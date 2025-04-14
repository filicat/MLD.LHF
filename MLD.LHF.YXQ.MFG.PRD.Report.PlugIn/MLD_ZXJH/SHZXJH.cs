using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Core;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.WebApi.Client;
using Kingdee.BOS.Log;

namespace MLD.LHF.YXQ.MFG.PRD.Report.PlugIn.MLD_ZXJH
{
    [Description("自动插入数据到损耗表"), HotUpdate]
    public class SHZXJH : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            try
            {
                string DMSsql = string.Format(@"/*dialect*/ exec MLD_Wastage '1'");
                DBUtils.Execute(ctx,DMSsql);
            }
            catch (Exception ex)
            {
                Logger.Error("自动插入数据到损耗表异常错误提示:", ex.Message, ex);
                throw;
            }
        }
    }
}
