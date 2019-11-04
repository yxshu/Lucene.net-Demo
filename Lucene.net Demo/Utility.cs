using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Lucene.net_Demo
{
    public static class Utility
    {
        /// <summary>
        /// 将datatable中的内容转化为对象
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="dt">要转换的datatable</param>
        /// <param name="type">要转换的类型</param>
        /// <returns>返回转换成功的对象list</returns>
        public static List<T> DataTableToModel<T>(DataTable dt, Type type)
        {
            List<T> list = new List<T>();
            foreach (DataRow dr in dt.Rows)
            {
                object obj = Activator.CreateInstance(type);
                foreach (DataColumn column in dt.Columns)
                {
                    foreach (PropertyInfo info in type.GetProperties())
                    {
                        if (string.Equals(column.ColumnName.ToLower(), info.Name.ToLower()))
                        {
                            try
                            {
                                info.SetValue(obj,Convert.ChangeType(dr[column],info.PropertyType));
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                        }
                    }
                }
                list.Add((T)obj);
            }
            return list;
        }
    }
}
