using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Threading;
using Lucene.Net.Store;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using PanGu;
using PanGu.HighLight;
using Lucene.Net.Analysis;
using PanGu.Lucene.Analyzer;

namespace Lucene.net_Demo
{
    public class SearchHelper
    {

        #region 创建单例
        // 定义一个静态变量来保存类的实例
        private static SearchHelper uniqueInstance;

        // 定义一个标识确保线程同步
        private static readonly object locker = new object();


        // 定义私有构造函数，使外界不能创建该类实例
        private SearchHelper()
        {
            CustomerStart();
        }

        /// <summary>
        /// 定义公有方法提供一个全局访问点,同时你也可以定义公有属性来提供全局访问点
        /// </summary>
        /// <returns></returns>
        public static SearchHelper GetInstance()
        {
            // 当第一个线程运行到这里时，此时会对locker对象 "加锁"，
            // 当第二个线程运行该方法时，首先检测到locker对象为"加锁"状态，该线程就会挂起等待第一个线程解锁
            // lock语句运行完之后（即线程运行完之后）会对该对象"解锁"
            lock (locker)
            {
                // 如果类的实例不存在则创建，否则直接返回
                if (uniqueInstance == null)
                {
                    uniqueInstance = new SearchHelper();
                }
            }

            return uniqueInstance;
        }

        #endregion

        private Queue<IndexJob> jobs = new Queue<IndexJob>();       //任务队列,保存生产出来的任务和消费者使用,不使用list避免移除时数据混乱问题

        /// <summary>
        /// 任务类,包括任务的Id ,操作的类型
        /// </summary>
        class IndexJob
        {
            public int Id { get; set; }//写入索引的数据
            public JobType JobType { get; set; }//处理数据的方式
            public QuestionModel question { get; set; }//加处理的数据的类型
        }
        /// <summary>
        /// 枚举,操作类型是增加还是删除
        /// </summary>
        enum JobType { Add, Remove }

        #region 任务索引
        /// <summary>
        /// 启动消费者线程
        /// </summary>
        public void CustomerStart()
        {

           // PanGu.Segment.Init(PanGuPath);

            Thread threadIndex = new Thread(IndexOn);
            threadIndex.IsBackground = true;
            threadIndex.Start();
        }

        /// <summary>
        /// 索引任务线程
        /// </summary>
        private void IndexOn()
        {
            Console.WriteLine("索引任务线程启动");
            while (true)
            {
                if (jobs.Count <= 0)
                {
                    Thread.Sleep(5 * 1000);
                    continue;
                }
                //创建索引目录
                if (!System.IO.Directory.Exists(IndexDic))
                {
                    System.IO.Directory.CreateDirectory(IndexDic);
                }
                FSDirectory directory = FSDirectory.Open(new DirectoryInfo(IndexDic), new NativeFSLockFactory());
                bool isUpdate = IndexReader.IndexExists(directory);
                Console.WriteLine("索引库存在状态" + isUpdate);
                if (isUpdate)
                {
                    //如果索引目录被锁定（比如索引过程中程序异常退出），则首先解锁
                    if (IndexWriter.IsLocked(directory))
                    {
                        Console.WriteLine("开始解锁索引库");
                        IndexWriter.Unlock(directory);
                        Console.WriteLine("解锁索引库完成");
                    }
                }
                IndexWriter writer = new IndexWriter(directory, new PanGuAnalyzer(), !isUpdate, Lucene.Net.Index.IndexWriter.MaxFieldLength.UNLIMITED);//写索引记录器
                ProcessJobs(writer);//将job queue中的任务全部写入索引
                writer.Close();
                directory.Dispose();//不要忘了Close，否则索引结果搜不到
                Console.WriteLine("全部索引完毕");
            }
        }
        /// <summary>
        /// 写入索引
        /// </summary>
        /// <param name="writer"></param>
        private void ProcessJobs(IndexWriter writer)
        {
            while (jobs.Count != 0)
            {
                IndexJob job = jobs.Dequeue();//取得最上面的工作任务
                writer.DeleteDocuments(new Term("number", job.Id.ToString()));//删除索引，job.id为标识，一般的原则是先移除再添加
                //如果“添加文章”任务再添加，
                if (job.JobType == JobType.Add)
                {

                    //BLL.article bll = new BLL.article();
                    //if (bll == null)//有可能刚添加就被删除了
                    //{
                    //    continue;
                    //}
                    //Model.article art = bll.GetArticleModel(job.Id);
                    QuestionModel question = job.question;
                    //string channel_id = art.channel_id.ToString();
                    //string title = art.title;
                    //DateTime time = art.add_time;
                    //string content = Utils.DropHTML(art.content.ToString());
                    //string Addtime = art.add_time.ToString("yyyy-MM-dd");

                    Document document = new Document();
                    document.Add(new Field("ALLID",question.Allid.ToString(),Field.Store.YES,Field.Index.NOT_ANALYZED));
                    document.Add(new Field("TITLE",question.Title,Field.Store.NO,Field.Index.ANALYZED));
                    document.Add(new Field("CHOOSEA",question.Choosea,Field.Store.NO,Field.Index.ANALYZED));
                    document.Add(new Field("CHOOSEB", question.Chooseb, Field.Store.NO, Field.Index.ANALYZED));
                    document.Add(new Field("CHOOSEC", question.Choosec, Field.Store.NO, Field.Index.ANALYZED));
                    document.Add(new Field("CHOOSED", question.Choosed, Field.Store.NO, Field.Index.ANALYZED));
                    document.Add(new Field("ANSWER", question.Answer, Field.Store.NO, Field.Index.ANALYZED));
                    ////只有对需要全文检索的字段才ANALYZED
                    //document.Add(new Field("number", job.Id.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    //document.Add(new Field("title", title, Field.Store.YES, Field.Index.ANALYZED, Lucene.Net.Documents.Field.TermVector.WITH_POSITIONS_OFFSETS));
                    //document.Add(new Field("channel_id", channel_id, Field.Store.YES, Field.Index.NOT_ANALYZED));
                    //document.Add(new Field("Addtime", Addtime, Field.Store.YES, Field.Index.NOT_ANALYZED));
                    //document.Add(new Field("content", content, Field.Store.YES, Field.Index.ANALYZED, Lucene.Net.Documents.Field.TermVector.WITH_POSITIONS_OFFSETS));
                    writer.AddDocument(document);
                    Console.WriteLine("索引" + job.Id + "完毕");
                }
            }
        } 
        #endregion

        #region 任务添加
        public void AddArticle(QuestionModel question)
        {
            IndexJob job = new IndexJob();
            job.Id = question.Allid;
            job.JobType = JobType.Add;
            job.question = question;
            Console.WriteLine(question.Allid + "加入任务列表");
            jobs.Enqueue(job);//把任务加入商品库
        }

        public void RemoveArticle(QuestionModel question)
        {
            IndexJob job = new IndexJob();
            job.JobType = JobType.Remove;
            job.Id =question.Allid ;
            job.question = question;
            Console.WriteLine(question.Allid + "加入删除任务列表");
            jobs.Enqueue(job);//把任务加入商品库
        }
        #endregion

        //#region 从索引搜索结果
        ///// <summary>
        ///// 从索引搜索结果
        ///// </summary>
        //public List<Model.article> SearchIndex(string Words, int PageSize, int PageIndex, out int _totalcount)
        //{
        //    _totalcount = 0;
        //    Dictionary<string, string> dic = new Dictionary<string, string>();
        //    BooleanQuery bQuery = new BooleanQuery();
        //    string title = string.Empty;
        //    string content = string.Empty;
        //    title = GetKeyWordsSplitBySpace(Words);
        //    QueryParser parse = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "title", new PanGuAnalyzer());
        //    Query query = parse.Parse(title);
        //    parse.SetDefaultOperator(QueryParser.Operator.AND);
        //    bQuery.Add(query, BooleanClause.Occur.SHOULD);
        //    dic.Add("title", Words);

        //    content = GetKeyWordsSplitBySpace(Words);
        //    QueryParser parseC = new QueryParser(Lucene.Net.Util.Version.LUCENE_29, "content", new PanGuAnalyzer());
        //    Query queryC = parseC.Parse(content);
        //    parseC.SetDefaultOperator(QueryParser.Operator.AND);
        //    bQuery.Add(queryC, BooleanClause.Occur.SHOULD);
        //    dic.Add("content", Words);
        //    if (bQuery != null && bQuery.GetClauses().Length > 0)
        //    {
        //        return GetSearchResult(bQuery, dic, PageSize, PageIndex, out _totalcount);
        //    }
        //    return null;
        //}
        ///// <summary>
        ///// 获取
        ///// </summary>
        ///// <param name="bQuery"></param>
        //private List<Model.article> GetSearchResult(BooleanQuery bQuery, Dictionary<string, string> dicKeywords, int PageSize, int PageIndex, out int totalCount)
        //{
        //    List<Model.article> list = new List<Model.article>();
        //    FSDirectory directory = FSDirectory.Open(new DirectoryInfo(IndexDic), new NoLockFactory());
        //    IndexReader reader = IndexReader.Open(directory, true);
        //    IndexSearcher searcher = new IndexSearcher(reader);
        //    TopScoreDocCollector collector = TopScoreDocCollector.create(1000, true);
        //    Sort sort = new Sort(new SortField("Addtime", SortField.DOC, true));
        //    searcher.Search(bQuery, null, collector);
        //    totalCount = collector.GetTotalHits();//返回总条数
        //    TopDocs docs = searcher.Search(bQuery, (Filter)null, PageSize * PageIndex, sort);
        //    if (docs != null && docs.totalHits > 0)
        //    {
        //        for (int i = 0; i < docs.totalHits; i++)
        //        {
        //            if (i >= (PageIndex - 1) * PageSize && i < PageIndex * PageSize)
        //            {
        //                Document doc = searcher.Doc(docs.scoreDocs[i].doc);
        //                Model.article model = new Model.article()
        //                {
        //                    id = int.Parse(doc.Get("number").ToString()),
        //                    title = doc.Get("title").ToString(),
        //                    content = doc.Get("content").ToString(),
        //                    add_time = DateTime.Parse(doc.Get("Addtime").ToString()),
        //                    channel_id = int.Parse(doc.Get("channel_id").ToString())
        //                };
        //                list.Add(SetHighlighter(dicKeywords, model));
        //            }
        //        }
        //    }
        //    return list;
        //}
        ///// <summary>
        ///// 设置关键字高亮
        ///// </summary>
        ///// <param name="dicKeywords">关键字列表</param>
        ///// <param name="model">返回的数据模型</param>
        ///// <returns></returns>
        //private Model.article SetHighlighter(Dictionary<string, string> dicKeywords, Model.article model)
        //{
        //    SimpleHTMLFormatter simpleHTMLFormatter = new PanGu.HighLight.SimpleHTMLFormatter("<font color=\"red\">", "</font>");
        //    Highlighter highlighter = new PanGu.HighLight.Highlighter(simpleHTMLFormatter, new Segment());
        //    highlighter.FragmentSize = 250;
        //    string strTitle = string.Empty;
        //    string strContent = string.Empty;
        //    dicKeywords.TryGetValue("title", out strTitle);
        //    dicKeywords.TryGetValue("content", out strContent);
        //    if (!string.IsNullOrEmpty(strTitle))
        //    {
        //        string title = model.title;
        //        model.title = highlighter.GetBestFragment(strTitle, model.title);
        //        if (string.IsNullOrEmpty(model.title))
        //        {
        //            model.title = title;
        //        }
        //    }
        //    if (!string.IsNullOrEmpty(strContent))
        //    {
        //        string content = model.content;
        //        model.content = highlighter.GetBestFragment(strContent, model.content);
        //        if (string.IsNullOrEmpty(model.content))
        //        {
        //            model.content = content;
        //        }
        //    }
        //    return model;
        //}
        ///// <summary>
        ///// 处理关键字为索引格式
        ///// </summary>
        ///// <param name="keywords"></param>
        ///// <returns></returns>
        //private string GetKeyWordsSplitBySpace(string keywords)
        //{
        //    PanGuTokenizer ktTokenizer = new PanGuTokenizer();
        //    StringBuilder result = new StringBuilder();
        //    ICollection<WordInfo> words = ktTokenizer.SegmentToWordInfos(keywords);
        //    foreach (WordInfo word in words)
        //    {
        //        if (word == null)
        //        {
        //            continue;
        //        }
        //        result.AppendFormat("{0}^{1}.0 ", word.Word, (int)Math.Pow(3, word.Rank));
        //    }
        //    return result.ToString().Trim();
        //} 
        //#endregion


        /// <summary>
        /// 索引存放目录
        /// </summary>
        protected string IndexDic
        {
            get
            {
                return System.Configuration.ConfigurationManager.AppSettings["IndexDic"];
            }
        }
        /// <summary>
        /// 盘古分词配置目录
        /// </summary>
        protected string PanGuPath
        {
            get
            {
                return System.Configuration.ConfigurationManager.AppSettings["PanGuPath"];
            }
        }
    }
}
