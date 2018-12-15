using System;

namespace Lucene.net_Demo
{
    internal class Program
    {
        private static void Main(string[] args)
        {

            //https://www.cnblogs.com/beimeng/p/3258967.html
            Console.WriteLine("****************** Lucene.net Demo Start ********************");

            ///利用NPOI从EXCEL中读取文件并存入DataTable中
            //ExcelHelper excelhelp = new ExcelHelper("./SourceDir/instruction-yuxiangshu.xlsx");
            //DataTable dt = excelhelp.ExcelToDataTable("Sheet1", true);

            ///利用反射和泛型将Datatable中的数据转换成对象对放入list中
            //List<QuestionModel> list = Utility.DataTableToModel<QuestionModel>(dt, typeof(QuestionModel));
            //Console.WriteLine("共获取内容{0}条。", list.Count);

            ///利用Lucene.NET生成索引文件Index
            //Console.WriteLine("----------------开始索引----------------");
            ///利用Lucene.NET查询索引文件并高亮显示以及按相关度排序
            SearchHelper searchhelper = SearchHelper.GetInstance();
            //searchhelper.CreatIndexs(new List<object>(list));
            while (true)
            {
                Console.WriteLine("请输入要查询的关键词：");
                string KeyWord = Console.ReadLine();
                Lucene.Net.Documents.Document[] topDocs = searchhelper.SearchIndex(KeyWord,typeof(QuestionModel), 10, out int totalhits);
                Console.WriteLine("共搜索到:{0}条记录。", totalhits);
                foreach (Lucene.Net.Documents.Document doc in topDocs)
                {
                    Console.WriteLine(doc.GetField("Title").StringValue);
                }
            }
        }
    }
}
