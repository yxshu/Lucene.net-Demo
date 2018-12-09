﻿using Lucene.Net.Analysis;
using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using System.Reflection;
using System.ComponentModel;

namespace Lucene.net_Demo
{
    public class SearchHelper
    {
        // 定义一个静态变量来保存类的实例,使用单例模式
        private static SearchHelper uniqueInstance;

        private SearchHelper() { }// 定义私有构造函数，使外界不能创建该类实例

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
            string IndexDir = System.Configuration.ConfigurationManager.AppSettings["IndexDir"];
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
            writer = new IndexWriter(directory, new PanGuAnalyzer(), true, Lucene.Net.Index.IndexWriter.MaxFieldLength.LIMITED);//生成索引写手
            Console.WriteLine(string.Format("生成IndexWriter:{0}成功。", writer.Directory.ToString()));
            return writer;
        }

        /// <summary>
        ///写入索引 ,主要通过model属性上的Description中标注的store 和index进行识别和索引
        ///调用完成以后，记得关闭indexwriter
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        private Boolean CreatIndexByDescription(IndexWriter indexwriter, object obj)
        {
            bool success = false;
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
                if (document.GetFields().Count > 0)
                {
                    indexwriter.AddDocument(document);
                    success = true;
                    Console.WriteLine("{0}创建索引成功。", obj.ToString());
                }
                return success;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw e;
            }
        }

        /// <summary>
        /// 创建索引
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public Boolean CreatIndexsByDescription(List<object> list)
        {
            bool success = true;
            IndexWriter indexwriter = CreateWriter();
            try
            {
                foreach (object obj in list)
                {
                    if (!CreatIndexByDescription(indexwriter, obj))
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
                indexwriter.Close();
            }
            Console.WriteLine("成功创建索引{0}条。", list.Count);
            return success;
        }
    }
}
