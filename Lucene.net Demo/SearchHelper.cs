using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Lucene.net_Demo
{
    public class SearchHelper
    {
        // 定义一个静态变量来保存类的实例,使用单例模式
        private static SearchHelper uniqueInstance;
        private static readonly string IndexDir = System.Configuration.ConfigurationManager.AppSettings["IndexDir"];
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

        private string[] GetIndexedPropertyNameByDescription(Type type)
        {
            List<string> list = new List<string>();
            PropertyInfo[] propertyInfos = type.GetProperties();
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                DescriptionAttribute descriptionAttribute = (DescriptionAttribute)propertyInfo.GetCustomAttribute(typeof(DescriptionAttribute));
                if (descriptionAttribute != null && descriptionAttribute.Description.ToLower().Contains("index"))
                {
                    list.Add(propertyInfo.Name);
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 根据Model对象生成一条Document记录
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private Document CreateDocumentByDescription(object obj)
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
                    if (attr == null)
                    {
                        continue;
                    }

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
        private bool CreatIndex(IndexWriter indexwriter, Document document)
        {
            bool success = false;

            if (document.GetFields().Count > 0)
            {
                indexwriter.AddDocument(document);
                success = true;
            }
            return success;
        }

        public void UpdateIndex(Term term, IndexWriter indexWriter, Document document)
        {
            try
            {
                indexWriter.UpdateDocument(term, document);
            }
            finally
            {
                if (indexWriter != null)
                {
                    indexWriter.Dispose();
                }
            }
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public int CreatIndexs(List<object> list)
        {

            IndexWriter indexwriter = CreateWriter();
            IndexReader indexReader = IndexReader.Open(CreateFSDirectory(), true);
            try
            {
                foreach (object obj in list)
                {
                    if (!CreatIndex(indexwriter, CreateDocumentByDescription(obj)))
                    {

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
            return indexReader.MaxDoc;
        }
        /// <summary>
        /// 搜索索引
        /// https://www.cnblogs.com/leeSmall/p/9027172.html
        /// </summary>
        /// <param name="keyword"></param>
        public Document[] SearchIndex(string keyword, Type type, int count, out int totalhits)
        {
            List<Document> results = new List<Document>();
            PanGuAnalyzer panGuAnalyzer = new PanGuAnalyzer();
            Net.Store.Directory directory = Net.Store.FSDirectory.Open(new DirectoryInfo(IndexDir));
            IndexReader indexreader = IndexReader.Open(directory, true);
            IndexSearcher indexsearch = new IndexSearcher(indexreader);

            //Query query = new TermQuery(new Term("Title", keyword));

            //QueryParser queryParser = new QueryParser(Net.Util.Version.LUCENE_30, "Title", panGuAnalyzer);
            //Query query = queryParser.Parse(keyword);

            //用法2 传统解析器-多默认字段  MultiFieldQueryParser：
            string[] multiDefaultFields = GetIndexedPropertyNameByDescription(type);
            MultiFieldQueryParser multiFieldQueryParser = new MultiFieldQueryParser(Net.Util.Version.LUCENE_30, multiDefaultFields, panGuAnalyzer);
            // 设置默认的操作
            //multiFieldQueryParser.setDefaultOperator(Operator.OR);
            Query query = multiFieldQueryParser.Parse(keyword);


            try
            {
                TopDocs topdocs = indexsearch.Search(query, count);
                totalhits = topdocs.TotalHits;
                foreach (ScoreDoc scoreDoc in topdocs.ScoreDocs)
                {
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
