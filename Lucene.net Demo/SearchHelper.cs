using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using System.Reflection;
using System.ComponentModel;

using PanGu;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;

namespace Lucene.net_Demo
{
    public class SearchHelper
    {
        // 定义一个静态变量来保存类的实例,使用单例模式
        private static SearchHelper uniqueInstance;
        private static string IndexDir = System.Configuration.ConfigurationManager.AppSettings["IndexDir"];
        private SearchHelper() { }// 定义私有构造函数，使外界不能创建该类实例
        private static readonly PanGuAnalyzer panGuAnalyzer = new PanGuAnalyzer();// 创建索引和查询统一使用这个分词器

        public static SearchHelper GetInstance()
        {
            if (uniqueInstance == null)
            {
                uniqueInstance = new SearchHelper();
            }
            return uniqueInstance;
        }
        /// <summary>
        /// 打开索引目录
        /// 索引的地址写在配置文件的appsettings中，以“IndexDir”命名
        /// </summary>
        /// <returns>返回打开的目录</returns>
        private FSDirectory CreateFSDirectory()
        {
            if (!System.IO.Directory.Exists(IndexDir))
            {
                System.IO.Directory.CreateDirectory(IndexDir);
            }
            FSDirectory directory = FSDirectory.Open(new DirectoryInfo(IndexDir), new NativeFSLockFactory());
            Console.WriteLine(string.Format("打开索引目录{0}成功。", IndexDir));
            return directory;
        }

        /// <summary>
        /// 生成索引创建器IndexWriter
        /// </summary>
        /// <returns>indexwriter</returns>
        private IndexWriter CreateWriter()
        {
            FSDirectory directory = CreateFSDirectory();
            IndexWriter writer = null;
            //如果索引目录被锁定（比如索引过程中程序异常退出），则首先解锁
            if (IndexWriter.IsLocked(directory))
            {
                IndexWriter.Unlock(directory);
            }
            writer = new IndexWriter(directory, panGuAnalyzer, true, Lucene.Net.Index.IndexWriter.MaxFieldLength.LIMITED);//生成索引写手
            Console.WriteLine(string.Format("生成IndexWriter:{0}成功。", writer.Directory.ToString()));
            return writer;
        }

        /// <summary>
        /// 根据Model对象生成一条Document记录
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Document CreateDocumentByDexscription(object obj)
        {
            Document document = new Document();//一条记录
            Type type = obj.GetType();
            PropertyInfo[] propertyinfos = type.GetProperties();
            try
            {
                foreach (PropertyInfo propertyinfo in propertyinfos)
                {
                    Field.Store store = Field.Store.NO;
                    Field.Index index = Field.Index.NO;
                    DescriptionAttribute attr = (DescriptionAttribute)propertyinfo.GetCustomAttribute(typeof(DescriptionAttribute));
                    if (attr == null) continue;
                    string[] str = attr.Description.ToString().Split();
                    foreach (string s in str)
                    {
                        if (string.Equals(s.Trim().ToLower(), "store"))
                        {
                            store = Field.Store.YES;
                        }
                        else if (string.Equals(s.Trim().ToLower(), "index"))
                        {
                            index = Field.Index.ANALYZED;
                        }
                    }
                    if (store != Field.Store.NO || index != Field.Index.NO)
                    {
                        document.Add(new Field(propertyinfo.Name, propertyinfo.GetValue(obj).ToString(), store, index));
                    }
                }
                return document;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        /// <summary>
        ///写入索引 ,主要通过model属性上的Description中标注的store 和index进行识别和索引
        ///调用完成以后，记得关闭indexwriter
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        private Boolean CreatIndex(IndexWriter indexwriter, Document document)
        {
            bool success = false;

            if (document.GetFields().Count > 0)
            {
                indexwriter.AddDocument(document);
                success = true;
                Console.WriteLine("{0}创建索引成功。", document.ToString());
            }
            return success;
        }


        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public Boolean CreatIndexs(List<object> list)
        {
            bool success = true;
            IndexWriter indexwriter = CreateWriter();
            try
            {
                foreach (object obj in list)
                {
                    if (!CreatIndex(indexwriter, CreateDocumentByDexscription(obj)))
                    {
                        success = false;
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("创建索引失败，原因：{0}", e.Message);
                throw e;

            }
            finally
            {
                indexwriter.Optimize();
                indexwriter.Dispose();
            }
            Console.WriteLine("成功创建索引{0}条。", list.Count);
            return success;
        }
        /// <summary>
        /// 搜索索引
        /// https://www.cnblogs.com/leeSmall/p/9027172.html
        /// </summary>
        /// <param name="keyword"></param>
        public Document[] SearchIndex(string keyword,int count)
        {
            List<Document> results = new List<Document>();
            PanGuAnalyzer panGuAnalyzer = new PanGuAnalyzer();
            Net.Store.Directory directory = Net.Store.FSDirectory.Open(new DirectoryInfo(IndexDir));
            IndexReader indexreader = IndexReader.Open(directory, true);
            IndexSearcher indexsearch = new IndexSearcher(indexreader);
            //Query query = new TermQuery(new Term("Title", keyword));
            QueryParser queryParser = new QueryParser( Net.Util.Version.LUCENE_30, "Title", panGuAnalyzer);
            Query query = queryParser.Parse(keyword);
            
            try
            {
                TopDocs topdocs = indexsearch.Search(query, count);
                foreach (ScoreDoc scoreDoc in topdocs.ScoreDocs) {
                    results.Add(indexsearch.Doc(scoreDoc.Doc));
                }
                return results.ToArray();
            }
            finally
            {
                indexsearch.Dispose();
                directory.Dispose();
            }    
        }


    }
}
