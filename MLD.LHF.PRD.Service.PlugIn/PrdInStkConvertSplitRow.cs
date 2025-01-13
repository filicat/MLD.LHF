using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using Kingdee.BOS.DataEntity;
using Kingdee.BOS.Core.Bill;
using Kingdee.BOS.Contracts;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Util;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata.FieldElement;
using Kingdee.BOS.Core.Metadata.FormElement;
using Kingdee.BOS.Core.Metadata.EntityElement;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn;
using Kingdee.BOS.Core.Metadata.ConvertElement.PlugIn.Args;
using Kingdee.BOS.Orm.Metadata.DataEntity;
using Newtonsoft.Json;

namespace MLD.LHF.PRD.Service.PlugIn
{
    [Kingdee.BOS.Util.HotUpdate]
    [Description("单据A到单据B单转换插件")]
    public class PrdInStkConvertSplitRow : AbstractConvertPlugIn
    {
        public class LabelReq
        {
            public long EntryId { get; set; }
            public string Label { get; set; }
            public decimal Qty { get; set; }
        }
        //控制字段Key和属性集合
        private List<DynamicProperty> _lstProperys = null;
        /// <summary>
        /// 单据转换之后事件
        /// </summary>
        /// <param name="e"></param>
        public override void AfterConvert(AfterConvertEventArgs e)
        {
            //行拆分依据
            long splitNumber = 0;
            //  webapi下推接口中的自定义参数，CustomParams={"FBaseQty":10}
            this.Option.TryGetVariableValue<long>("FRealQty", out splitNumber);
            this.Option.TryGetVariableValue<JSONArray>("labels", out JSONArray labelJA);
            // 反序列化为 List<Entry>
            List<LabelReq> labelReqs = JsonConvert.DeserializeObject<List<LabelReq>>(labelJA.ToJSONString());
            // 将 List<LabelReq> 转换为 Dictionary<int, List<LabelReq>>
            Dictionary<long, List<LabelReq>> labelReqsMap = labelReqs
                .GroupBy(req => req.EntryId) // 按 EntryId 分组
                .ToDictionary(group => group.Key, group => group.ToList()); // 转换为字典
            if (splitNumber == 0) return; //分录行拆分数量
            var field = e.TargetBusinessInfo.GetField("FRealQty"); //基本单据数量字段
            LotField lotField = (LotField) e.TargetBusinessInfo.GetField("FLot");

            var entryEntity = field.Entity; //字段所在的单据体
            //得到单据数据包扩展集合
            var billDynObjExs = e.Result.FindByEntityKey("FBillHead");
            var tView = CreateView(e.TargetBusinessInfo.GetForm().Id); //创建目标单据视图
            var targetLinkSet = e.TargetBusinessInfo.GetForm().LinkSet;
            var targetLinConfig = targetLinkSet.LinkEntitys[0]; //平台只支持一个关联实体，故取第一个关联设置就可以
            var targetLinkEntity = e.TargetBusinessInfo.GetEntity(targetLinConfig.Key); //关联实体
            this.InitLinkFieldProperty(targetLinkEntity, targetLinConfig, e.TargetBusinessInfo); //初始化关联字段属性
            foreach (var billDynObjEx in billDynObjExs) //循环数据包扩展集合
            {
                var billDynObj = billDynObjEx.DataEntity; //单个单据数据包
                tView.Model.DataObject = billDynObj; //给模型设置数据包
                var entryDynObjs = entryEntity.DynamicProperty.GetValue(billDynObj) as DynamicObjectCollection; //得到字段所在实体的数据包
                int rowIndex = 0; //分录行索引
                                  //拆分信息；原分录行索引，新增拆分的行数，最后一行值,来源单内码，来源单分录内码
                Dictionary<int, Tuple<int, decimal, long, long, List<LabelReq>>> dicSplitInfo = new Dictionary<int, Tuple<int, decimal, long, long, List<LabelReq>>>();
                //foreach (var rowObj in entryDynObjs) //循环分录
                for (int i2 = 0; i2 < entryDynObjs.Count; i2++)
                {
                    DynamicObject rowObj = entryDynObjs[i2];
                    var linkObjs = rowObj[targetLinkEntity.Key] as DynamicObjectCollection;
                    long sBillId = Int64.Parse(linkObjs[0]["SBillId"].ToString()); //来源单据内码
                    long sId = Int64.Parse(linkObjs[0]["SId"].ToString());//来源分录内码
                    var sRealQty = decimal.Parse(Convert.ToString(field.DynamicProperty.GetValue(rowObj))); //得到字段在此分录下的值
                    //if (sRealQty > splitNumber) //如果值大于行拆分值
                    //{
                    //    var rowCount = (int)(sRealQty % splitNumber > 0 ? sRealQty / splitNumber : sRealQty / splitNumber - 1); //需要新增拆分的行数
                    //    decimal leaveValue = sRealQty - splitNumber * rowCount; //剩余值
                    //    dicSplitInfo.Add(rowIndex, Tuple.Create(rowCount, leaveValue, sBillId, sId));
                    //}
                    List<LabelReq> entrylabelReqs = labelReqsMap[sId];
                    dicSplitInfo.Add(rowIndex, Tuple.Create(entrylabelReqs.Count - 1, 0m, sBillId, sId, entrylabelReqs));
                    //复制拆分行,并设置拆分值
                    int offsetRow = 0; //偏移数
                    
                    foreach (var item in dicSplitInfo)
                    {
                        rowIndex = item.Key + offsetRow; //原分录行索引加偏移量在变动的数据包中的索引
                        var dynObj = entryDynObjs[rowIndex]; //原原分录行数据包
                        List<LabelReq> listLabelReq = item.Value.Item5;

                        DynamicObject lot = new DynamicObject(lotField.RefFormDynamicObjectType);
                        lot["Number"] = listLabelReq[0].Label;
                        tView.Model.SetValue(lotField, dynObj, lot);
                        tView.InvokeFieldUpdateService(field.Key, rowIndex);
                        tView.Model.SetValue(field, dynObj, listLabelReq[0].Qty);
                        //tView.Model.SetValue(field, dynObj, splitNumber); //这字段值并且会触发值更新事件

                        tView.InvokeFieldUpdateService(field.Key, rowIndex); //调用实体服务规则
                        offsetRow = 0; //偏移量重置
                        for (int i = 0; i < item.Value.Item1; i++)
                        {
                            tView.Model.CopyEntryRowFollowCurrent(field.Entity.Key, rowIndex + offsetRow, rowIndex, true); //rowIndex + offsetRow+1插入的位置；rowIndex复制行的位置;true,复制关联关系
                            int newRowIndex = rowIndex + offsetRow + 1; //新分录行索引
                            var fValue = i == item.Value.Item1 - 1 ? (item.Value.Item2 == 0 ? splitNumber : item.Value.Item2) : splitNumber;
                            //对单据体进行赋值
                            //tView.Model.SetValue(field, entryDynObjs[newRowIndex], fValue); //这字段值并且会触发值更新事件
                            DynamicObject lotF = new DynamicObject(lotField.RefFormDynamicObjectType);
                            lotF["Number"] = listLabelReq[i + 1].Label;
                            tView.Model.SetValue(lotField, entryDynObjs[newRowIndex], lotF);
                            tView.InvokeFieldUpdateService(field.Key, newRowIndex);
                            tView.Model.SetValue(field, entryDynObjs[newRowIndex], listLabelReq[i+1].Qty);

                            tView.InvokeFieldUpdateService(field.Key, newRowIndex); //调用实体服务规则
                                                                                    //关联数据包的处理
                                                                                    //处理关联数据包
                            var linkObjs2 = targetLinkEntity.DynamicProperty.GetValueFast(entryDynObjs[newRowIndex]) as DynamicObjectCollection;
                            // 控制字段处理
                            foreach (var fieldProperty in this._lstProperys)
                            {
                                foreach (var linkObj in linkObjs2)
                                {
                                    fieldProperty.SetValue(linkObj, fValue);
                                    linkObj["SBillId"] = item.Value.Item3;
                                    linkObj["SId"] = item.Value.Item4;
                                }
                            }
                            offsetRow++;
                            i2++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 初始化关联字段属性
        /// </summary>
        /// <param name="targetLinkEntity"></param>
        private void InitLinkFieldProperty(Entity targetLinkEntity, LinkEntity targetLinkEntitySet, BusinessInfo targetBInfo)
        {
            // 关联实体的字段属性
            DynamicObjectType linkEntityDT = targetLinkEntity.DynamicObjectType;
            this._lstProperys = new List<DynamicProperty>();
            foreach (var key in targetLinkEntitySet.WriteBackFieldKeys)
            {
                if (!this._lstProperys.ToDictionary(item => item.Name, item => item).ContainsKey(key)) //if (!this._lstProperys.ContainsKey(key))
                {
                    var field = targetBInfo.GetField(key);
                    this._lstProperys.Add(linkEntityDT.Properties[field.PropertyName]);
                }
            }
        }
        /// <summary>
        /// 创建单据视图  
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        public IDynamicFormView CreateView(string formId)
        {
            FormMetadata metadata = FormMetaDataCache.GetCachedFormMetaData(this.Context, formId);
            var OpenParameter = CreateOpenParameter(this.Context, metadata);
            var Provider = metadata.BusinessInfo.GetForm().GetFormServiceProvider(true);
            string importViewClass = "Kingdee.BOS.Web.Import.ImportBillView,Kingdee.BOS.Web";
            Type type = Type.GetType(importViewClass);
            IDynamicFormView view = (IDynamicFormView)Activator.CreateInstance(type);
            ((IDynamicFormViewService)view).Initialize(OpenParameter, Provider);
            return view;
        }
        /// <summary>
        /// 创建输入参数
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="metaData"></param>
        /// <returns></returns>
        private BillOpenParameter CreateOpenParameter(Context ctx, FormMetadata metaData)
        {
            Form form = metaData.BusinessInfo.GetForm();
            BillOpenParameter openPara = new BillOpenParameter(form.Id, metaData.GetLayoutInfo().Id);
            openPara = new BillOpenParameter(form.Id, string.Empty);
            openPara.Context = ctx;
            openPara.ServiceName = form.FormServiceName;
            openPara.PageId = Guid.NewGuid().ToString();
            // 单据
            openPara.FormMetaData = metaData;
            openPara.LayoutId = metaData.GetLayoutInfo().Id;
            // 操作相关参数
            openPara.Status = OperationStatus.ADDNEW;
            openPara.PkValue = null;
            openPara.CreateFrom = CreateFrom.Default;
            openPara.ParentId = 0;
            openPara.GroupId = "";
            openPara.DefaultBillTypeId = null;
            openPara.DefaultBusinessFlowId = null;
            // 修改主业务组织无须用户确认
            openPara.SetCustomParameter("ShowConfirmDialogWhenChangeOrg", false);
            // 插件
            List<AbstractDynamicFormPlugIn> plugins = form.CreateFormPlugIns();
            openPara.SetCustomParameter(FormConst.PlugIns, plugins);
            return openPara;
        }
    }
}