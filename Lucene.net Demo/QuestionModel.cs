using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.net_Demo
{
    public class QuestionModel
    {
        private int allid;
        /// <summary>
        /// 所有试题的总编号
        /// </summary>
        [Description("store")]
        public int Allid
        {
            get { return allid; }
            set { allid = value; }
        }
        private string id;
        /// <summary>
        /// 按每章节进行编号-带有章节号
        /// </summary>
        public string Id
        {
            get { return id; }
            set { id = value; }
        }
        private string sn;
        /// <summary>
        /// 按每章节进行编号
        /// </summary>
        public string Sn
        {
            get { return sn; }
            set { sn = value; }
        }
        private string snid;
        /// <summary>
        /// 按每章节进行编号-带有章节号
        /// </summary>
        public string Snid
        {
            get { return snid; }
            set { snid = value; }
        }
        private string subject;
        /// <summary>
        /// 课程名称
        /// </summary>
        public string Subject
        {
            get { return subject; }
            set { subject = value; }
        }
        private string chapter;
        /// <summary>
        /// 章名称
        /// </summary>
        public string Chapter
        {
            get { return chapter; }
            set { chapter = value; }
        }
        private string node;
        /// <summary>
        /// 节名称
        /// </summary>
        public string Node
        {
            get { return node; }
            set { node = value; }
        }
        private string title;
        /// <summary>
        /// 试题标题
        /// </summary>
        [Description("store index")]
        public string Title
        {
            get { return title; }
            set { title = value; }
        }
        private string choosea;
        /// <summary>
        /// 选项A
        /// </summary>
        [Description("store index")]
        public string Choosea
        {
            get { return choosea; }
            set { choosea = value; }
        }
        private string chooseb;
        /// <summary>
        /// 选项B
        /// </summary>
        [Description("store index")]
        public string Chooseb
        {
            get { return chooseb; }
            set { chooseb = value; }
        }
        private string choosec;
        /// <summary>
        /// 选项C
        /// </summary>
        [Description("store index")]
        public string Choosec
        {
            get { return choosec; }
            set { choosec = value; }
        }
        private string choosed;
        /// <summary>
        /// 选项D
        /// </summary>
        [Description("store index")]
        public string Choosed
        {
            get { return choosed; }
            set { choosed = value; }
        }
        private string answer;
        /// <summary>
        /// 参考答案
        /// </summary>
        [Description("store index")]
        public string Answer
        {
            get { return answer; }
            set { answer = value; }
        }
        private string explain;
        /// <summary>
        /// 试题解析
        /// </summary>
        [Description("store index")]
        public string Explain
        {
            get { return explain; }
            set { explain = value; }
        }
        private string imageaddress;
        /// <summary>
        /// 试题图片地址
        /// </summary>
        public string Imageaddress
        {
            get { return imageaddress; }
            set { imageaddress = value; }
        }
        private string remark;
        /// <summary>
        /// 试题备注
        /// </summary>
        [Description("store index")]
        public string Remark
        {
            get { return remark; }
            set { remark = value; }
        }

    }
}
