﻿using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Analysis.Tokenattributes;
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
using System.Text;

namespace Lucene.net_Demo
{
    public class SearchHelper
    {
        // 定义一个静态变量来保存类的实例,使用单例模式
        private static SearchHelper uniqueInstance;
        private static IndexReader indexreader;
        private static readonly string IndexDir = System.Configuration.ConfigurationManager.AppSettings["IndexDir"];
        private SearchHelper() { }// 定义私有构造函数，使外界不能创建该类实例
        private static readonly Analyzer panGuAnalyzer = new PanGuAnalyzer();// 创建索引和查询统一使用这个分词器

        public static SearchHelper GetInstance()
        {
            //indexreader = CreateReader();
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
        private static FSDirectory CreateFSDirectory()
        {
            if (!System.IO.Directory.Exists(IndexDir))
            {
                System.IO.Directory.CreateDirectory(IndexDir);
            }
            FSDirectory directory = FSDirectory.Open(new DirectoryInfo(IndexDir));
            //Console.WriteLine(string.Format("打开索引目录{0}成功。", IndexDir));
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
            writer = new IndexWriter(directory, panGuAnalyzer, true, IndexWriter.MaxFieldLength.LIMITED);//生成索引写手                                                                                                       
            //Console.WriteLine(string.Format("生成IndexWriter:{0}成功。", writer.Directory.ToString()));
            return writer;
        }
        private static IndexReader CreateReader()
        {
            if (indexreader == null)
            {
                indexreader = IndexReader.Open(CreateFSDirectory(), true);
            }
            return indexreader;
        }
        /// <summary>
        /// 根据对象的attribute属性，取得索引名
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <returns></returns>
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
        /// 其中是否索引和分词根据对象定义中的DescriptionAttribute,如：[Description("store index")]
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
        ///写入索引 ,主要通过model属性上的Description中标注的store 和index进行识别和索引，如：[Description("store index")]
        ///调用完成以后，记得关闭indexwriter
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        public bool CreatIndex(object obj)
        {
            bool success = false;
            Document document = CreateDocumentByDescription(obj);
            IndexWriter indexWriter = CreateWriter();
            if (document.GetFields().Count > 0)
            {
                indexWriter.AddDocument(document);
                try { indexWriter.Commit(); success = true; } finally { indexWriter.Dispose(); }
            }
            return success;
        }

        /// <summary>
        /// 根据对象的[Description("store index")] 其中第一个中存储不分词的设置为term
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static Term CreateTermByDescription(object obj)
        {
            Term term = null;
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
                    if (store == Field.Store.YES && index == Field.Index.NO)
                    {
                        term = new Term(propertyinfo.Name, propertyinfo.GetValue(obj).ToString());
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return term;
        }

        /// <summary>
        /// 更新索引 ，其中的term是根据对象模型的[Description("store index")]，其中的第一个中存储不词为标准
        /// </summary>
        /// <param name="obj"></param>
        public void UpdateIndex(object obj)
        {
            IndexWriter indexWriter = CreateWriter();
            Document document = CreateDocumentByDescription(obj);
            Term term = CreateTermByDescription(obj);
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
        /// 批量创建索引
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public bool CreatIndexs(List<object> list)
        {
            bool success = true;
            IndexWriter indexWriter = CreateWriter();
            try
            {
                foreach (object obj in list)
                {
                    indexWriter.AddDocument(CreateDocumentByDescription(obj));
                }
                indexWriter.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine("创建索引失败，原因：{0}", e.Message);
                throw e;

            }
            return success;
        }
        /// <summary>
        /// 对搜索的关键词进行分词
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        private static string GetKeyWordSplit(string keyword)
        {
            StringBuilder sb = new StringBuilder();
            TokenStream stream = panGuAnalyzer.TokenStream(keyword, new StringReader(keyword));
            ITermAttribute ita = null;
            bool hasnext = stream.IncrementToken();
            while (hasnext)
            {
                ita = stream.GetAttribute<ITermAttribute>();
                sb.Append(ita.Term + " ");
                hasnext = stream.IncrementToken();
            }
            return sb.ToString();
        }
        /// <summary>
        /// 查询索引  https://my.oschina.net/u/3728166?tab=newest&catalogId=5747400
        /// </summary>
        /// <param name="keyword">关键字</param>
        /// <param name="type">类型</param>
        /// <param name="count">输出条数</param>
        /// <param name="totalhits">查询到的总数量</param>
        /// <returns></returns>
        public void SearchIndex(string keyword, Type type, int count, out List<object> scoreANDdoc, out int totalhits)
        {
            indexreader = CreateReader();
            scoreANDdoc = new List<object>();
            List<Document> results = new List<Document>();
            PanGuAnalyzer panGuAnalyzer = new PanGuAnalyzer();
            FSDirectory directory = FSDirectory.Open(new DirectoryInfo(IndexDir));
            IndexSearcher indexsearch = new IndexSearcher(indexreader);

            //用法1 传统解析器-单默认字段   QueryParser
            QueryParser parser = new QueryParser(Net.Util.Version.LUCENE_30, "Title", panGuAnalyzer);
            //parser.PhraseSlop = 2;
            //parser.DefaultOperator = Operator.OR;
            Query query = parser.Parse(GetKeyWordSplit(keyword));

            ////用法2 传统解析器-多默认字段  MultiFieldQueryParser：
            //string[] multiDefaultFields = GetIndexedPropertyNameByDescription(type);
            //MultiFieldQueryParser multiFieldQueryParser = new MultiFieldQueryParser(Net.Util.Version.LUCENE_30, multiDefaultFields, panGuAnalyzer)
            //{
            //    // 设置默认的操作
            //    DefaultOperator = Operator.OR
            //};
            //Query query = multiFieldQueryParser.Parse(keyword);


            ////方法3 复杂的搜索
            //// 要搜索的字段，一般搜索时都不会只搜索一个字段
            //string[] multiDefaultFields = GetIndexedPropertyNameByDescription(type);
            //// 字段之间的与或非关系，MUST表示and，MUST_NOT表示not，SHOULD表示or，有几个fields就必须有几个clauses                                             
            ////BooleanClause.Occur[] clauses = { BooleanClause.Occur.SHOULD, BooleanClause.Occur.SHOULD };
            //// MultiFieldQueryParser表示多个域解析， 同时可以解析含空格的字符串，如果我们搜索"上海 中国"
            //MultiFieldQueryParser multiFieldQueryParser = new MultiFieldQueryParser(Net.Util.Version.LUCENE_30, multiDefaultFields, panGuAnalyzer)
            //{
            //    DefaultOperator = QueryParser.Operator.OR
            //};
            //Query multiFieldQuery = multiFieldQueryParser.Parse(keyword);
            //Query termQuery = new TermQuery(new Term("Title", keyword));// 词语搜索,完全匹配,搜索具体的域
            //Query wildqQuery = new WildcardQuery(new Term("Title", keyword));// 通配符查询
            //Query prefixQuery = new PrefixQuery(new Term("Title", keyword));// 字段前缀搜索
            //Query fuzzyQuery = new FuzzyQuery(new Term("Title", keyword));// 相似度查询,模糊查询比如OpenOffica，OpenOffice
            //BooleanQuery query = new BooleanQuery
            //{
            //    { multiFieldQuery, Occur.SHOULD },
            //    { termQuery, Occur.SHOULD },
            //    { wildqQuery, Occur.SHOULD },
            //    { prefixQuery, Occur.SHOULD },
            //    { fuzzyQuery, Occur.SHOULD }
            //};// 这才是最终的query

            ////方法4
            //PerFieldAnalyzerWrapper wrapper = new PerFieldAnalyzerWrapper(panGuAnalyzer);
            //wrapper.AddAnalyzer("Title", panGuAnalyzer);
            //string[] fields = GetIndexedPropertyNameByDescription(type);
            //QueryParser parser = new MultiFieldQueryParser(Lucene.Net.Util.Version.LUCENE_30, fields, wrapper);
            //Query query = parser.Parse(keyword);


            try
            {
                TopDocs topdocs = indexsearch.Search(query, count);
                totalhits = topdocs.TotalHits;
                foreach (ScoreDoc scoreDoc in topdocs.ScoreDocs)
                {
                    List<object> list = new List<object>();
                    double score = scoreDoc.Score;
                    Document document = indexsearch.Doc(scoreDoc.Doc);
                    list.Add(score);
                    list.Add(document);
                    scoreANDdoc.Add(list);
                }
            }
            finally
            {
                indexsearch.Dispose();
                directory.Dispose();
            }
        }


    }
}
