using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;

namespace MLD.LHF.JZC.ZL.MLD.Sale.Buiness.Bill.PlugIn
{
    [Description("多方交易模板根据多方交易类型自动生成分录行")]
    [HotUpdate]
    public class MuiPartBuinessTempBillPlugIn: AbstractBillPlugIn
    {
        public override void DataChanged(DataChangedEventArgs e)
        {
            base.DataChanged(e);
            if (e.Field.Key.Equals("F_MILD_BuinessType"))
            {
                if (e.NewValue != null) {
                    int buinessType =Convert.ToInt32(e.NewValue);
                    doAddEntryRow(buinessType);
                }
            }
        }

        private void doAddEntryRow(int buinessType)
        {        
            this.View.Model.DeleteEntryData("F_MILD_BuinessTemp_Entry");
            Model.BeginIniti();
            this.View.Model.BatchCreateNewEntryRow("F_MILD_BuinessTemp_Entry", buinessType - 1);
            this.View.Model.SetValue("F_MILD_EntryType", 0, 0);
            this.View.Model.SetValue("F_MILD_EntryType", 2, buinessType - 2);
            for (int i = 0; i < buinessType - 1; i++)
            {              
                if (i != 0 && i != buinessType - 2)
                {
                    this.View.Model.SetValue("F_MILD_EntryType", 1, i);
                }
            }
            Model.EndIniti();
           View.UpdateView("F_MILD_BuinessTemp_Entry");
        }
    }
}
