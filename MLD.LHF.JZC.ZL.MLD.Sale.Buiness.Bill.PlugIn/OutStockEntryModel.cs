using Kingdee.BOS.Orm.DataEntity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MLD.LHF.JZC.ZL.MLD.Sale.Buiness.Bill.PlugIn
{
    //拼装销售出库单实体
   public class OutStockModel
    {
        public string billType { get; set; }
        public long id { get; set; }
        public String billNo { get; set; }
        public String date { get; set; }
        /**币别编码**/
        public string currNumber { get; set; }
        public DynamicObject muiPartBuinessTmp { get; set; }

        /**库存组织**/
        public String stockOrgNumber { get; set; }
        /**销售/采购组织**/
        public String saleOrgNumber { get; set; }
        /**货主**/
        public String ownerOrgNumber { get; set; }

        public decimal priceDiscount{get;set;}

        public String custNumber { get; set; }

        public String endCustNumber { get;set; }
        public String endCustName { get; set; }
        public List<OutStockEntryModel> outStockEntryModelList { get; set; }

        public class OutStockEntryModel {
            public String materialNumber { get; set; }
            public decimal qty { get; set; }
            public decimal price { get; set; }
            public decimal taxRate { get; set; }
            public String lotNumber { get; set; }
            public String stockNumber { get; set; }
            public long stockLcId { get; set; }
            public String stockLocNumber_F1 { get; set; }
            public String stockLocNumber_F7 { get; set; }
            public String stockStatusNumber { get; set; }
            public String productLineNumber { get; set; }
            public String keeperTyperId { get; set; }
            public String keeperId { get; set; }
            public String mtoNo { get; set; }
            public bool freeFlag { get; set; }
   
        }
    }
}
