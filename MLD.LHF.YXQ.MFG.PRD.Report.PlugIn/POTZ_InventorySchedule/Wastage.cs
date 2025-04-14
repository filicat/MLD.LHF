using Kingdee.BOS.App.Data;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.Contracts.Report;
using Kingdee.BOS.Core.Report;
using Kingdee.BOS.Core.SqlBuilder;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.YXQ.MFG.PRD.Report.PlugIn.POTZ_InventorySchedule
{
    [Description("委外在制表"), HotUpdate]
    public class Wastage : SysReportBaseService
    {
        public override void Initialize()
        {
            base.Initialize();
            //简单账表
            this.ReportProperty.ReportType = ReportType.REPORTTYPE_NORMAL;
            //是否通过临时表取数（设置为true临时表取值，false Sql取值）
            this.IsCreateTempTableByPlugin = true;
            //是否分组汇总
            this.ReportProperty.IsGroupSummary = true;
            //替代列
            this.ReportProperty.DspInsteadColumnsInfo.DefaultDspInsteadColumns.Add("F_MLD_CPXX", "CPXX");
            this.ReportProperty.DspInsteadColumnsInfo.DefaultDspInsteadColumns.Add("F_MLD_ZXWLXX", "ZXWLXX");
            this.ReportProperty.DspInsteadColumnsInfo.DefaultDspInsteadColumns.Add("F_MLD_OrgId", "OrgId");
            this.ReportProperty.DspInsteadColumnsInfo.DefaultDspInsteadColumns.Add("F_MLD_Supplier", "Supplier");
            this.ReportProperty.DspInsteadColumnsInfo.DefaultDspInsteadColumns.Add("F_MLD_CPDW", "CPDW");
            this.ReportProperty.DspInsteadColumnsInfo.DefaultDspInsteadColumns.Add("F_MLD_CLDW", "CLDW");
            //设置精度
            List<DecimalControlField> list = new List<DecimalControlField>();
            list.Add(new DecimalControlField("JD", "F_MLD_CPRKSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_YXHSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_DDSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_YFSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_YLSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_BFSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_ZZPSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_DWSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_SHL"));
            list.Add(new DecimalControlField("JD", "F_MLD_SYSL"));
            list.Add(new DecimalControlField("JD", "F_MLD_FScrapRate"));
            list.Add(new DecimalControlField("JD", "F_MLD_ConsumeQty"));
            list.Add(new DecimalControlField("JD", "F_MLD_WipQty"));
            ReportProperty.DecimalControlFieldList = list;

        }
        /// <summary>
        /// 表头赋值
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override ReportTitles GetReportTitles(IRptParams filter)
        {
            string FormId = filter.FormId;
            ReportTitles reportTitles = new ReportTitles();
            DynamicObject customFilter = filter.FilterParameter.CustomFilter;
            string multiOrgnNameValues = "";
            string startValue = "";
            string endValue = "";
            if (customFilter != null)
            {
                //组织
                if (FormId == "POTZ_InventorySchedule")
                {
                    multiOrgnNameValues = this.GetMultiOrgnNameValues(customFilter["F_POTZ_ZZ"].ToString());
                    startValue = customFilter["F_POTZ_Year"].GetString() + "-" + customFilter["F_POTZ_Month"].GetString();
                    endValue = customFilter["F_POTZ_Year"].GetString() + "-" + customFilter["F_POTZ_EndMonth"].GetString();
                }
                else
                {
                    multiOrgnNameValues = this.GetMultiOrgnNameValues(customFilter["F_MLD_ZZ"].ToString());
                    //开始时间
                    startValue = (customFilter["F_MLD_StartDate"] == null) ? string.Empty : Convert.ToDateTime(customFilter["F_MLD_StartDate"]).ToString("yyyy-MM-dd");
                    //结束时间
                    endValue = (customFilter["F_MLD_EndDate"] == null) ? string.Empty : Convert.ToDateTime(customFilter["F_MLD_EndDate"]).ToString("yyyy-MM-dd");
                }
                //供应商
                string GYS = this.GetBaseDataNameValue(customFilter["F_MLD_GYS"] as DynamicObjectCollection, "Name").ToString();
                //物料
                string CP = this.GetBaseDataNameValue(customFilter["F_MLD_CP"] as DynamicObjectCollection, "Name").ToString();
                //物料
                string ZXWL = this.GetBaseDataNameValue(customFilter["F_MLD_ZXWL"] as DynamicObjectCollection, "Name").ToString();
                //开始单号
                string FBeginRONumber = (customFilter["F_MLD_FBeginRONumber"] == null) ? string.Empty : customFilter["F_MLD_FBeginRONumber"].ToString();
                //截至单号
                string EndRONumber = (customFilter["F_MLD_EndRONumber"] == null) ? string.Empty : customFilter["F_MLD_EndRONumber"].ToString();
                reportTitles.AddTitle("F_MLD_WWZZ", multiOrgnNameValues);
                reportTitles.AddTitle("F_MLD_RQ", startValue + "-" + endValue);
                reportTitles.AddTitle("F_MLD_GYS", GYS);
                reportTitles.AddTitle("F_MLD_CP", CP);
                reportTitles.AddTitle("F_MLD_ZXWL", ZXWL);
                reportTitles.AddTitle("F_MLD_WWDDH", FBeginRONumber + (EndRONumber.Length > 0 ? ("-" + EndRONumber) : EndRONumber));
            }
            return reportTitles;
        }
        /// <summary>
        /// 向临时表插入数据
        /// </summary>
        /// <param name="filter">过滤框信息</param>
        /// <param name="tableName">临时表名</param>
        public override void BuilderReportSqlAndTempTable(IRptParams filter, string tableName)
        {
            //filter
            base.BuilderReportSqlAndTempTable(filter, tableName);
            var customFilter = filter.FilterParameter.CustomFilter;
            //获取筛选框的值
            string sqlStr = filter.FilterParameter.SortString;
            //排序
            KSQL_SEQ = string.Format(KSQL_SEQ, sqlStr.IsNullOrEmptyOrWhiteSpace() ? "F_MLD_DATE asc" : sqlStr);
            string Name = filter.FormId == "POTZ_InventorySchedule" ? "MLD_ArticlesInProcessReport" : "MLD_WastageReport";
            string sql = string.Format($@"exec {Name} '{tableName}','{GetCustomFilter(filter)}','{KSQL_SEQ}'");
            DBUtils.Execute(this.Context, sql);
        }
        /// <summary>
        /// 获取表单
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private string GetCustomFilter(IRptParams filter)
        {
            string FormId = filter.FormId;
            StringBuilder strwhere = new StringBuilder();
            //获取筛选框的快捷值
            var customFilter = filter.FilterParameter.CustomFilter;
            string filterStr = filter.FilterParameter.FilterString;
            if (!filterStr.IsNullOrEmptyOrWhiteSpace())
            {
                strwhere.AppendLine(string.Format(" AND {0}", filterStr));
            }
            if (FormId == "POTZ_InventorySchedule")
            {
                //组织
                string org = string.IsNullOrWhiteSpace(customFilter["F_POTZ_ZZ"].ToString())
                ? " " : string.Format(" And F_MLD_OrgId IN ({0}) ", Convert.ToString(customFilter["F_POTZ_ZZ"]));
                strwhere.AppendLine(org);
                //时间
                int Year = Convert.ToInt32(customFilter["F_POTZ_Year"]); 
                int Month =Convert.ToInt32(customFilter["F_POTZ_Month"]);
                int EndMonth = Convert.ToInt32(customFilter["F_POTZ_EndMonth"]);
                string Number1Name = string.Format(" AND YEAR(F_MLD_DATE)={0} and MONTH(F_MLD_DATE)>={1} and MONTH(F_MLD_DATE)<={2} ", Year, Month, EndMonth);
                strwhere.AppendLine(Number1Name);
                
            }
            else
            {
                //组织
                string org = string.IsNullOrWhiteSpace(customFilter["F_MLD_ZZ"].ToString())
                ? " " : string.Format(" And F_MLD_OrgId IN ({0}) ", Convert.ToString(customFilter["F_MLD_ZZ"]));
                strwhere.AppendLine(org);
                //开始时间
                string startValue = (customFilter["F_MLD_StartDate"] == null) ? string.Empty : Convert.ToDateTime(customFilter["F_MLD_StartDate"]).ToString("yyyy-MM-dd");
                if (startValue != "")
                {
                    string Number1Name = string.Format(" AND F_MLD_DATE >= ''{0}'' ", startValue);
                    strwhere.AppendLine(Number1Name);
                }
                //结束时间
                string endValue = (customFilter["F_MLD_EndDate"] == null) ? string.Empty : Convert.ToDateTime(customFilter["F_MLD_EndDate"]).ToString("yyyy-MM-dd");
                if (endValue != "")
                {
                    string Number1Name = string.Format(" AND F_MLD_DATE <= ''{0}'' ", endValue);
                    strwhere.AppendLine(Number1Name);
                }
            }

            //供应商
            string GYS = this.GetBaseDataNameValue(customFilter["F_MLD_GYS"] as DynamicObjectCollection, "Id").ToString();
            if (GYS != "")
            {
                string NumberName = string.Format(" AND F_MLD_Supplier in ({0}) ", GYS);
                strwhere.AppendLine(NumberName);
            }
            //成品
            string CP = this.GetBaseDataNameValue(customFilter["F_MLD_CP"] as DynamicObjectCollection, "Id").ToString();
            if (CP != "")
            {
                string NumberName = string.Format(" AND F_MLD_CPXX in ({0}) ", CP);
                strwhere.AppendLine(NumberName);
            }
            //子项物料
            string ZXWL = this.GetBaseDataNameValue(customFilter["F_MLD_ZXWL"] as DynamicObjectCollection, "Id").ToString();
            if (ZXWL != "")
            {
                string NumberName = string.Format(" AND F_MLD_ZXWLXX in ({0}) ", ZXWL);
                strwhere.AppendLine(NumberName);
            }
            //开始单号
            string FBeginRONumber = (customFilter["F_MLD_FBeginRONumber"] == null) ? string.Empty : customFilter["F_MLD_FBeginRONumber"].ToString();
            if (FBeginRONumber != "")
            {
                string NumberName = string.Format(" AND F_MLD_OutsourceFumber >= ''{0}'' ", FBeginRONumber);
                strwhere.AppendLine(NumberName);
            }
            //截至单号
            string EndRONumber = (customFilter["F_MLD_EndRONumber"] == null) ? string.Empty : customFilter["F_MLD_EndRONumber"].ToString();
            if (EndRONumber != "")
            {
                string Number1Name = string.Format(" AND F_MLD_OutsourceFumber <= ''{0}'' ", EndRONumber);
                strwhere.AppendLine(Number1Name);
            }
            return strwhere.ToString();
        }
        /// <summary>
        /// 设置汇总列
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public override List<SummaryField> GetSummaryColumnInfo(IRptParams filter)
        {
            string FormId = filter.FormId;
            var result = base.GetSummaryColumnInfo(filter);
            if (FormId == "POTZ_InventorySchedule")
            {
                //销售数量
                result.Add(new SummaryField("F_MLD_ZZPSL", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            }
            else
            {
                //销售数量
                result.Add(new SummaryField("F_MLD_YXHSL", Kingdee.BOS.Core.Enums.BOSEnums.Enu_SummaryType.SUM));
            }            
            return result;
        }
        /// <summary>
        /// 获取组织
        /// </summary>
        /// <param name="orgIdStrings"></param>
        /// <returns></returns>
        private string GetMultiOrgnNameValues(string orgIdStrings)
        {
            List<string> list = new List<string>();
            string result = string.Empty;
            if (orgIdStrings.Trim().Length > 0)
            {
                IQueryService service = Kingdee.BOS.Contracts.ServiceFactory.GetService<IQueryService>(base.Context);
                QueryBuilderParemeter para = new QueryBuilderParemeter
                {
                    FormId = "ORG_Organizations",
                    SelectItems = Kingdee.BOS.Core.Metadata.SelectorItemInfo.CreateItems("FNAME"),
                    FilterClauseWihtKey = string.Format(" FORGID IN ({0}) AND FLOCALEID={1}", orgIdStrings, base.Context.UserLocale.LCID)
                };
                DynamicObjectCollection dynamicObjectCollection = service.GetDynamicObjectCollection(base.Context, para, null);
                foreach (DynamicObject current in dynamicObjectCollection)
                {
                    list.Add(current["FNAME"].ToString());
                }
                if (list.Count > 0)
                {
                    result = string.Join(",", list.ToArray());
                }
            }
            return result;
        }
        /// <summary>
        /// 获取部门的值
        /// </summary>
        /// <param name="dyobj"></param>
        /// <returns></returns>
        private string GetBaseDataNameValue(DynamicObjectCollection dyobj, string Name)
        {
            string name = "";
            foreach (DynamicObject dynbj in dyobj)
            {
                if (dynbj != null || !dynbj.DynamicObjectType.Properties.Contains(Name))
                {
                    DynamicObject dynbj2 = (DynamicObject)dynbj[2];
                    name = name + ",''" + dynbj2[Name].ToString() + "''";
                }
            }
            if (name.Length > 0)
            {
                name = name.Substring(1, name.Length - 1);
            }
            return name;
        }
    }
}