using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Search;
using Lucene.Net.Documents;

namespace CommonLibaray
{
    public class DataObject
    {
        public string Column { get; set; }
        public string Value { get; set; }
        public float Weight { get; set; }
        public bool IsStore { get; set; }
        public bool IsSort { get; set; }

        public DataTypes DataType { get; set; }

    }

    public enum DataTypes
    {
        String = 0,
        Int = 1,
        DateTime = 2,
        Other = 3
    }

    public class BulidIndex
    {
        private static IndexWriter writer;
        private static Lucene.Net.Analysis.Analyzer analyzer;

        /// <summary>
        /// 写入索引
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        public static IEnumerable<int> WriteIndex(string indexPath, IEnumerable<DataObject[]> dataList)
        {
            Init(indexPath);

            int index = 0;

            foreach (var item in dataList)
            {
                AddDocument(writer, item);

                yield return index++;
            }

            CloseWrite();
        }

        private static void Init(string indexPath)
        {
            analyzer = new Lucene.Net.Analysis.PanGu.PanGuAnalyzer();

            System.IO.DirectoryInfo info = new System.IO.DirectoryInfo(indexPath);

            var exsit = !Lucene.Net.Index.IndexReader.IndexExists(Lucene.Net.Store.FSDirectory.Open(indexPath));

            writer = new IndexWriter(FSDirectory.Open(info), analyzer, exsit, IndexWriter.MaxFieldLength.UNLIMITED);

        }

        private static void CloseWrite()
        {
            writer.Optimize();
            writer.Dispose();
        }

        private static void AddDocument(IndexWriter writer, DataObject[] t)
        {
            Document document = new Document();

            foreach (var item in t)
            {
                AbstractField field = null;
                if (item.DataType == DataTypes.Int)
                {
                    field = new NumericField(item.Column, Field.Store.YES, true).SetIntValue(int.Parse(item.Value));
                }
                else
                {
                    field = new Field(item.Column, item.Value, item.IsStore ? Field.Store.YES : Field.Store.NO, Field.Index.ANALYZED);
                }

                field.Boost = item.Weight;

                document.Add(field);

                if (item.IsSort)
                {
                    document.Add(new Field(item.Column + "_sort", item.Value, Field.Store.NO, Field.Index.NOT_ANALYZED));
                }
            }
            writer.AddDocument(document);
        }
    }
}
