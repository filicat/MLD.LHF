using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            test1();
        }

        public static void test1()
        {
            // 创建一个包含3个字典的数组
            Dictionary<string, object>[] dicts = new Dictionary<string, object>[4];

            // 初始化每个字典
            for (int i = 0; i < dicts.Length; i++)
            {
                dicts[i] = new Dictionary<string, object>();
            }

            dicts[0].Add("Mat", new Dictionary<string, object>
            {
                { "ID", 100 },
                { "NUM", "A" }
            });
            dicts[0].Add("Qty", 100);
            dicts[0].Add("Seq", 1);

            dicts[1].Add("Mat", new Dictionary<string, object>
            {
                { "ID", 100 },
                { "NUM", "A" }
            });
            dicts[1].Add("Qty", 100);
            dicts[1].Add("Seq", 2);

            dicts[2].Add("Mat", new Dictionary<string, object>
            {
                { "ID", 101 },
                { "NUM", "B" }
            });
            dicts[2].Add("Qty", 200);
            dicts[2].Add("Seq", 3);

            dicts[3].Add("Mat", new Dictionary<string, object>
            {
                { "ID", 102 },
                { "NUM", "C" }
            });
            dicts[3].Add("Qty", 300);
            dicts[3].Add("Seq", 4);

            var list = dicts.GroupBy(x => new { ID = Convert.ToInt32((x["Mat"] as Dictionary<string, object>)["ID"]), NUM = Convert.ToString((x["Mat"] as Dictionary<string, object>)["NUM"]), Qty = Convert.ToInt32(x["Qty"]) }).Select(g => g.Key).ToArray().ToList();
        }
    }
}
