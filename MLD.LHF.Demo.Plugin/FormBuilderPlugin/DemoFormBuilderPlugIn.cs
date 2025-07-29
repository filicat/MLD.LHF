using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.DynamicForm.PlugIn.Args;
using Kingdee.BOS.JSON;
using Kingdee.BOS.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MLD.LHF.Demo.Plugin.FormBuilderPlugin
{
    [HotUpdate, Description("[表单构建插件]多行文本控件输入字数提示; 面板背景图设置")]
    public class DemoFormBuilderPlugIn : AbstractDynamicWebFormBuilderPlugIn
    {
        public override void CreateControl(CreateControlEventArgs e)
        {
            base.CreateControl(e);
            if (e.ControlAppearance.Key.EqualsIgnoreCase("F_POTZ_Remarks_BZ"))
            {
                JSONObject item = e.Control["item"] as JSONObject;
                if (null != item)
                {
                    item["xtype"] = "kdtextareaext";
                    int maxLength = 50;
                    item["maxLength"] = maxLength; // 设置控件可输入的字符的最大数量
                    item["TipWordTemplate"] = "字数: 当前输入{EDITEDNUMBER}/" + maxLength + "行: {LINESELECTION}列:{LINECHARTSELECTION}当前位置字符: {CHARTSELECTION}";
                }
            }
            if (e.ControlAppearance.Key.EqualsIgnoreCase("F_POTZ_Panel_1"))
            {
                e.Control["backgroundimage"] = "https://img.shetu66.com/2023/07/04/1688453333865029.png";
            }
        }
    }
}
