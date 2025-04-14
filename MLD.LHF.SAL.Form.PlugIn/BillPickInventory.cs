using Kingdee.BOS;
using Kingdee.BOS.Core.Bill.PlugIn;
using Kingdee.BOS.Core.Bill.PlugIn.Args;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Metadata.BarElement;
using Kingdee.BOS.Core.Metadata.BusinessService;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Util;
using System;
using System.ComponentModel;
using System.Linq;



namespace MLD.LHF.SAL.Form.PlugIn

{

    [Kingdee.BOS.Util.HotUpdate]
    [Description("WebApi通过FNote字段修改触发匹配库存出库")]
    public class BillPickInventory : AbstractBillPlugIn

    {

        /// <summary>

        /// 匹配库存服务配置

        /// </summary>

        private FormBusinessService _pickInvService = null;

        /// <summary>

        /// 菜单，作为事件源

        /// </summary>

        protected BarItem _barItem = null;



        /// <summary>

        /// 单据体(标识)

        /// </summary>

        private string entryEntityKey = "FEntity";



        /// <summary>

        /// 单据体实体属性名

        /// </summary>

        private string entryEntityName = "SAL_OUTSTOCKENTRY";



        public override void OnBillInitialize(BillInitializeEventArgs e)

        {

            base.OnBillInitialize(e);

            //取得单据体匹配库存服务

            GetEntryPickInvService();

        }



        public override void DataChanged(DataChangedEventArgs e)

        {

            //单据头文本字段修改且在WebAPi对接时触发服务     

            if (this.Context.ClientType == ClientType.WebApi)

            {

                if ("FNote".Equals(e.Field.Key))

                {

                    InvokePickInv((DynamicObjectCollection)this.Model.DataObject[entryEntityName]);

                }

            }

        }



        /// <summary>

        /// 获取匹配库存菜单和服务

        /// </summary>

        private void GetEntryPickInvService()

        {

            _pickInvService = null;

            _barItem = null;

            foreach (Appearance ap in this.View.LayoutInfo.Appearances)

            {

                if (!entryEntityKey.Equals(ap.Key)) continue;



                BarDataManager menu = ((EntryEntityAppearance)ap).Menu;

                foreach (BarItem item in menu.BarItems)

                {

                    if (item.ClickActions.IsEmpty() == true) continue;

                    FormBusinessService service = item.ClickActions.FirstOrDefault(p => p.ActionId == 133);

                    if (service != null)

                    {

                        _pickInvService = (service.Clone()) as FormBusinessService;

                        ((LotPickingBusinessServiceMeta)_pickInvService).OnlyCurrentRow = false;

                        _pickInvService.ClassName = "Kingdee.K3.SCM.Business.DynamicForm.BusinessService.PickInventory, Kingdee.K3.SCM.Business.DynamicForm";

                        _barItem = item;

                        break;

                    }

                }

            }

        }



        /// <summary>

        /// 执行匹配库存服务

        /// </summary>

        public virtual void InvokePickInv(DynamicObjectCollection entrys)

        {

            if (entrys.Count < 1) return;



            if (_pickInvService != null && _pickInvService.IsEnabled && !_pickInvService.IsForbidden)

            {

                //检查单据状态，审核中、已审核、已作废不处理

                string strValue = Convert.ToString(this.Model.GetValue("FDocumentStatus"));

                if ("B".Equals(strValue) || "C".Equals(strValue)) return;

                strValue = Convert.ToString(this.Model.GetValue("FCANCELSTATUS"));

                if ("B".Equals(strValue)) return;

                FormBusinessServiceUtil.InvokeService(this.View, _barItem, _pickInvService, entryEntityKey, entrys[0], 0);

            }

        }



    }

}