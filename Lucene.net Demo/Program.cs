using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.net_Demo
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("****************** Lucene.net Demo Start ********************");

            ///利用NPOI从EXCEL中读取文件并存入DataTable中
            ExcelHelper excelhelp = new ExcelHelper("./SourceDir/instruction-yuxiangshu.xlsx");
            DataTable dt = excelhelp.ExcelToDataTable("Sheet1", true);

            ///利用反射和泛型将Datatable中的数据转换成对象对放入list中
            List<QuestionModel> list = Utility.DataTableToModel<QuestionModel>(dt, typeof(QuestionModel));

            ///利用Lucene.NET生成索引文件Index
            ///


            ///利用Lucene.NET查询索引文件并高亮显示以及按相关度排序

            Console.WriteLine(list.Count);
            Console.ReadLine();
        }
    }
}
