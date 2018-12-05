using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Store;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using System.Threading;
using System.IO;
using PanGu;
using System.Data;
using PanGu.HighLight;

namespace CommonLibaray
{
    public class LuneceSearch
    {
        private static Analyzer analyzer = new Lucene.Net.Analysis.PanGu.PanGuAnalyzer();

        public static List<string> Search(string indexPath, List<SearchObject> searchObjectList, OrderBy orderBy, int start, int pageSize, out int total, int limitCount)
        {
            var index = GetIndexSearch(indexPath);

            return Search(index, searchObjectList, orderBy, start, pageSize, out total, limitCount);
        }

        public static List<string> MultiSearch(string[] indexPath, List<SearchObject> searchObjectList, OrderBy orderBy, int start, int pageSize, out int total, int limitCount)
        {
            var allIndexSearch = GetIndexSearches(indexPath);

            MultiSearcher multiSearch = new MultiSearcher(allIndexSearch.ToArray());

            return Search(multiSearch, searchObjectList, orderBy, start, pageSize, out total, limitCount);
        }


        public static List<string> ParallelMultiSearcher(string[] indexPath, List<SearchObject> searchObjectList, OrderBy orderBy, int start, int pageSize, out int total, int limitCount)
        {
            var allIndexSearch = GetIndexSearches(indexPath);

            ParallelMultiSearcher parallelMultiSearch = new ParallelMultiSearcher(allIndexSearch.ToArray());

            return Search(parallelMultiSearch, searchObjectList, orderBy, start, pageSize, out total, limitCount);
        }

        private static List<string> Search(Searcher searches, List<SearchObject> searchObjectList, OrderBy orderBy, int start, int pageSize, out int total, int limitCount)
        {
            total = 0;

            if (searchObjectList == null || searchObjectList.Count <= 0)
            {
                searchObjectList = new List<SearchObject>();
                SearchObject searchObject = new SearchObject();
                searchObject.Column = "*";
                searchObject.Value = string.Empty;
                searchObjectList.Add(searchObject);
            }

            var docs = SearchDocs(searches, searchObjectList, orderBy, limitCount, out total);

            int tempTotal = 0;

            var pageDocs = docs.GetPageList(start - 1, pageSize, out tempTotal);

            List<string> id = new List<string>();

            foreach (var item in pageDocs)
            {
                id.Add(searches.Doc(item.Doc).Get("id"));
            }

            return id;
        }

        public static string GetHightLighter(string keywords, string content, int length)
        {

            // 高亮显示设置
            SimpleHTMLFormatter simpleHTMLFormatter = new SimpleHTMLFormatter("<font color=\"red\">", "</font>");
            var highlighter = new Highlighter(simpleHTMLFormatter, new Segment());
            //关键内容显示大小设置 
            highlighter.FragmentSize = length;
            return highlighter.GetBestFragment(keywords, content);
        }

        private static List<IndexSearcher> GetIndexSearches(string[] indexPath)
        {
            List<IndexSearcher> allIndexSearch = new List<IndexSearcher>();

            foreach (var item in indexPath)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    allIndexSearch.Add(GetIndexSearch(item));
                }
            }

            return allIndexSearch;
        }

        private static ScoreDoc[] SearchDocs(Searcher searcher, List<SearchObject> searchObject, OrderBy orderBy, int limitCount, out  int total)
        {
            ScoreDoc[] docs = null;

            var query = BulidQuery(searchObject, analyzer, searcher);

            total = 0;

            if (query == null) return new ScoreDoc[] { };

            if (orderBy == null || string.IsNullOrEmpty(orderBy.Coloumn))
            {
                var allDocs = searcher.Search(query, limitCount);
                total = allDocs.TotalHits;
                docs = allDocs.ScoreDocs;
            }
            else
            {
                var allDocs = searcher.Search(query, null, limitCount, new Sort(new SortField(orderBy.Coloumn + "_sort", SortField.STRING, !orderBy.IsDesc)));

                total = allDocs.TotalHits;
                docs = allDocs.ScoreDocs;
            }
            return docs;

        }

        private static Query BulidQuery(List<SearchObject> searchObject, Analyzer analyzer, Searcher searhcer)
        {
            BooleanQuery bq = new BooleanQuery();

            foreach (var item in searchObject)
            {
                if (item.Column == "*")
                {
                    var query = BulidQuery(item.Value.ToString(), analyzer, searhcer);
                    bq.Add(query, item.Logic == Logic.and ? Lucene.Net.Search.Occur.MUST : item.Logic == Logic.or ? Occur.SHOULD : Occur.MUST_NOT);
                }
                else
                {
                    var query = ParserOperatorTypeQuery(item, analyzer);
                    if (query != null)
                        bq.Add(query, item.Logic == Logic.and ? Occur.MUST : item.Logic == Logic.or ? Occur.SHOULD : Occur.MUST_NOT);
                }
            }

            if (bq.Clauses.Count == 0) return null;

            return bq;
        }

        private static Query ParserOperatorTypeQuery(SearchObject item, Analyzer analyzer)
        {
            Query query = null;
            if (string.IsNullOrEmpty(item.Value.ToString().Trim())) return query;
            if (item.OperatorType == Operator.Equal)
            {
                Term t = new Term(item.Column + "_sort", item.Value.ToString());

                query = new TermQuery(t);
            }
            else
            {
                QueryParser parser = new QueryParser(Lucene.Net.Util.Version.LUCENE_30, item.Column, analyzer);
                query = parser.Parse(item.Value.ToString());
            }
            return query;
        }

        private static Query BulidQueryForNoKeyword()
        {
            NumericRangeQuery<int> query = NumericRangeQuery.NewIntRange("ID", 0, 10000000, true, true);

            return query;
        }

        private static Query BulidQueryForKeyWord(string keyword, Analyzer analyzer, Searcher searhcer)
        {
            List<string> keys = new List<string>();
            List<string> values = new List<string>();

            var keywordsSplit = keyword;

            foreach (var item in IndexSearches.Select(x => x.Value).FirstOrDefault().IndexReader.GetFieldNames(IndexReader.FieldOption.ALL))
            {
                if (item.ToLower() == "id" || item.ToLower().Contains("_sort"))
                    continue;
                keys.Add(item);
                values.Add(keywordsSplit);
            }

            Query query = MultiFieldQueryParser.Parse(Lucene.Net.Util.Version.LUCENE_30,
                values.ToArray(), keys.ToArray(), analyzer);

            return query;
        }

        private static Query BulidQuery(string keyword, Analyzer analyzer, Searcher searhcer)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return BulidQueryForNoKeyword();
            }

            return BulidQueryForKeyWord(keyword, analyzer, searhcer);

        }

        public static string GetWord(string word, string split = " ")
        {
            var w = GetWords(word);

            return string.Join(split, w);

        }

        /// <summary>
        /// 获取分词集合
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static ICollection<string> GetWords(string word)
        {
            Segment segment = new Segment();
            return segment.DoSegment(word).Select(x => x.Word).ToList();
        }

        static Dictionary<string, IndexSearcher> IndexSearches;

        static object lockObj = new object();

        private static IndexSearcher GetIndexSearch(string indexPath)
        {
            if (!System.IO.Directory.Exists(indexPath))
            {
                throw new ArgumentException("当前索引路径不存在");
            }

            lock (lockObj)
            {
                if (IndexSearches == null)
                {
                    IndexSearches = new Dictionary<string, IndexSearcher>();
                }

                if (!IndexSearches.Keys.Contains(indexPath))
                {
                    System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(indexPath);

                    IndexSearcher searcher = new IndexSearcher(FSDirectory.Open(info), true);

                    IndexSearches.Add(indexPath, new IndexSearcher(FSDirectory.Open(info), true));
                }

                return IndexSearches[indexPath];

            }

        }
    }

    /// <summary>
    /// 检索的实体类
    /// </summary>
    public class SearchObject
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string Column { get; set; }

        /// <summary>
        /// 操作类型
        /// </summary>
        public Operator OperatorType { get; set; }

        /// <summary>
        /// 值
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 逻辑运算符，and or
        /// </summary>
        public Logic Logic { get; set; }

        public SearchObject Clone()
        {
            SearchObject obj = new SearchObject();

            obj.Column = this.Column;

            obj.Value = this.Value;

            obj.Logic = this.Logic;

            obj.OperatorType = this.OperatorType;

            return obj;
        }
    }

    public enum Logic
    {
        and,
        or
    }

    public enum Operator
    {
        Equal,
        NotEqual,
        Like,
        StartLike,
        EndLike,
        NotLike,
        In,
        NotIn,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }

    public class OrderBy
    {
        public string Coloumn { get; set; }
        public bool IsDesc { get; set; }
    }
}