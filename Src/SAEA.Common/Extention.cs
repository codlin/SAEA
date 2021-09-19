﻿/****************************************************************************
*Copyright (c) 2018-2021yswenli All Rights Reserved.
*CLR版本： 4.0.30319.42000
*机器名称：WENLI-PC
*公司名称：yswenli
*命名空间：SAEA.Commom
*文件名： Extention
*版本号： v6.0.0.1
*唯一标识：0957f3bb-7462-4ff0-867d-0a8c9411f2eb
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/4/12 9:33:39
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/4/12 9:33:39
*修改人： yswenli
*版本号： v6.0.0.1
*描述：
*
*****************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAEA.Common
{
    /// <summary>
    /// 自定义扩展类
    /// </summary>
    public static class Extention
    {
        /// <summary>
        /// ConcurrentQueue&lt;T&gt; 清除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Clear<T>(this ConcurrentQueue<T> list)
        {
            if (list != null)
            {
                while (!list.IsEmpty)
                {
                    list.TryDequeue(out T t);
                }
            }
        }

        /// <summary>
        /// 清除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        public static void Clear<T>(this ICollection<T> data)
        {
            if (data != null && data.Any())
            {
                data.Clear();
                data = null;
            }
        }
    }
}
