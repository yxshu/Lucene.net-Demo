using Lucene.Net.Documents;
using System;
using System.Collections.Generic;

using System.Data;
using System.Text;

namespace Lucene.net_Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            //https://www.cnblogs.com/beimeng/p/3258967.html
            Console.WriteLine("****************** Lucene.net Demo Start ********************");

            /// 利用NPOI从EXCEL中读取文件并存入DataTable中
            ExcelHelper excelhelp = new ExcelHelper("./SourceDir/equipment-hedetao.xlsx");
            DataTable dt = excelhelp.ExcelToDataTable("Sheet1", true);

            /// 利用反射和泛型将Datatable中的数据转换成对象对放入list中
            List<QuestionModel> list = Utility.DataTableToModel<QuestionModel>(dt, typeof(QuestionModel));
            Console.WriteLine("共获取内容{0}条。", list.Count);

            ///生成索引文件
            SearchHelper searchHelper = SearchHelper.GetInstance();
            List<object> listo = new List<object>();
            foreach (object o in list) { listo.Add(o); }
            searchHelper.CreatIndexs(listo);
            Console.WriteLine("生成索引成功。");
            while (true)
            {
                Console.WriteLine("开始查询，请输入查询关键词:");
                string keyword = Console.ReadLine();
                searchHelper.SearchIndex(keyword, typeof(QuestionModel), 10, out List<object> scoreANDdoc, out int totalhits);
                Console.WriteLine("查询到{0}个结果。", totalhits);
                foreach (List<object> d in scoreANDdoc)
                {
                    Document document = (Document)d[1];
                    StringBuilder sb = new StringBuilder();
                    sb.Append(document.GetValues("Title")[0]);
                    sb.Append(document.GetValues("Choosea")[0]);
                    sb.Append(document.GetValues("Chooseb")[0]);
                    sb.Append(document.GetValues("Choosec")[0]);
                    sb.Append(document.GetValues("Choosed")[0]);
                    sb.Append(document.GetValues("Answer")[0]);
                    sb.Append(document.GetValues("Explain")[0]);
                    Console.WriteLine("Score:{0}--DOC:{1}", d[0], sb.ToString());
                }
            }
        }
    }
}
