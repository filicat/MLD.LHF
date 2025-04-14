using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Util;
using Kingdee.K3.MFG.SUB.App.ReportPlugIn.ROExecute;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.MFG.SUB.App.ReportIn.ReportPlugIn.ROExecute
{
    [Description("委外订单执行明细表二开添加字段"), HotUpdate]
    public class MLDROExecuteDetailRpt : ROExecuteDetailRpt
    {
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            base.BuilderReportSqlAndTempTable(filter, tableName);
            this.AddPOnPickBillsnDates(tableName);
        }

        private void AddPOnPickBillsnDates(string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("alter table {0} add F_MLD_POBILLNOS varchar(4000) default '' ;", tableName);
            sb.AppendFormat("alter table {0} add F_MLD_PODATES varchar(4000) default '' ;", tableName);
            sb.AppendFormat("alter table {0} add F_MLD_PICKBILLNOS varchar(4000) default '' ;", tableName);
            sb.AppendFormat("alter table {0} add F_MLD_PICKDATES varchar(4000) default '' ;", tableName);
            DBUtils.Execute(this.Context, sb.ToString());

            sb.Clear();
            sb.AppendFormat(" MERGE INTO {0} T0", tableName);
            sb.AppendFormat(@" using  (SELECT T1.FBILLNO, T0.FSEQ, T0.F_MLD_POBILLNOS, T0.F_MLD_PODATES, T0.F_MLD_PICKBILLNOS, T0.F_MLD_PICKDATES FROM T_SUB_REQORDERENTRY T0
                                        INNER JOIN T_SUB_REQORDER T1 ON T0.FID = T1.FID) T");
            sb.AppendFormat(" ON T0.FROBILLNO=T.FBILLNO AND T0.FROENTRYSEQ=T.FSEQ");
            sb.AppendFormat(" WHEN matched THEN UPDATE SET T0.F_MLD_POBILLNOS=T.F_MLD_POBILLNOS, T0.F_MLD_PODATES=T.F_MLD_PODATES, T0.F_MLD_PICKBILLNOS=T.F_MLD_PICKBILLNOS, T0.F_MLD_PICKDATES=T.F_MLD_PICKDATES;");
            DBUtils.Execute(this.Context, sb.ToString());
        }
    }
}
