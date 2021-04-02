﻿/****************************************************************************
*Copyright (c) 2018-2020 yswenli All Rights Reserved.
*CLR版本： 4.0.30319.42000
*机器名称：WENLI-PC
*公司名称：yswenli
*命名空间：SAEA.Http
*文件名： HttpSession
*版本号： v6.0.0.1
*唯一标识：2e43075f-a43d-4b60-bee1-1f9107e2d133
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/12/12 20:46:40
*描述：
*
*=====================================================================
*修改标记
*创建时间：2018/12/12 20:46:40
*修改人： yswenli
*版本号： v6.0.0.1
*描述：
******************************************************************************/

using SAEA.Common;
using SAEA.Common.Caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SAEA.Http
{
    /// <summary>
    /// HttpSession
    /// </summary>
    public class HttpSession : HttpSession<object>
    {
        Timer timer;

        /// <summary>
        /// 关联outputcache使用
        /// </summary>
        public string CacheCalcString { get; set; } = "-1,-1";


        internal event Action<HttpSession> OnExpired;


        internal HttpSession(string id) : base()
        {
            ID = id;
            Expired = DateTimeHelper.Now.AddMinutes(20);
            timer = new Timer(new TimerCallback((o) =>
            {
                OnExpired?.Invoke((HttpSession)o);
            }), this, (long)(new TimeSpan(0, 20, 0).TotalMilliseconds), -1);
        }
        /// <summary>
        /// 重新更新计时器
        /// </summary>
        internal void Refresh()
        {
            this.Expired = DateTimeHelper.Now.AddMinutes(20);
            timer.Change((long)(new TimeSpan(0, 20, 0).TotalMilliseconds), -1);
        }
    }


    public class HttpSession<T>
    {
        ConcurrentDictionary<string, HttpSessionItem<T>> _cache;

        public string ID
        {
            get;
            protected set;
        }

        public DateTime Expired
        {
            get; set;
        }

        public HttpSession()
        {
            _cache = new ConcurrentDictionary<string, HttpSessionItem<T>>();
        }

        public T this[string key]
        {
            get
            {
                if (_cache.TryGetValue(key, out HttpSessionItem<T> data))
                {
                    if (data != null && data.Value != null)
                    {
                        return data.Value;
                    }
                }
                return default(T);
            }
            set
            {
                _cache[key] = new HttpSessionItem<T>(key, value);
            }
        }

        public List<string> Keys
        {
            get
            {
                return _cache.Keys.ToList();
            }
        }


        public HttpSessionItem<T> Get(string key)
        {
            if (_cache.TryGetValue(key, out HttpSessionItem<T> t))
            {
                return t;
            }
            return null;
        }

        protected bool Contains(string id, string key)
        {
            return _cache.ContainsKey(key);
        }

        public void Add(string key, T val)
        {
            _cache.AddOrUpdate(key, new HttpSessionItem<T>(key, val), (k, v) =>
            {
                v.Value = val;
                v.Expires = DateTimeHelper.Now.AddMinutes(20);
                return v;
            });
        }

        public bool Contains(string key)
        {
            return _cache.ContainsKey(key);
        }

        public void Remove(string key)
        {
            _cache.TryRemove(key, out HttpSessionItem<T> t);
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// SessionItem
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HttpSessionItem<T>
    {
        public string Key
        {
            get; set;
        }

        public T Value
        {
            get; set;
        }

        public DateTime Expires
        {
            get; set;
        }

        public HttpSessionItem(string key, T value, DateTime expires)
        {
            this.Key = key;
            this.Value = value;
            this.Expires = expires;
        }

        public HttpSessionItem(string key, T value) : this(key, value, DateTimeHelper.Now.AddMinutes(20))
        {

        }

    }

    /// <summary>
    /// SessionManager
    /// </summary>
    internal static class HttpSessionManager
    {
        static ConcurrentDictionary<string, HttpSession> _cache = new ConcurrentDictionary<string, HttpSession>();

        static Random random = new Random();

        /// <summary>
        /// 生成sessionID
        /// </summary>
        /// <returns></returns>
        public static string GeneratID()
        {
            var bytes = new byte[15];

            random.NextBytes(bytes);

            return StringHelper.Substring(string.Join("", bytes), 0, 15);
        }

        /// <summary>
        /// 获取HttpSession
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static HttpSession GetIfNotExistsSet(string id)
        {
            var session = _cache.GetOrAdd(id, (k) =>
            {
                var httpSession = new HttpSession(k);
                httpSession.OnExpired += HttpSession_OnExpired;
                return httpSession;
            });
            session.Refresh();
            return session;
        }

        private static void HttpSession_OnExpired(HttpSession obj)
        {
            Remove(obj.ID);
        }

        /// <summary>
        /// 移除httpSession
        /// </summary>
        /// <param name="id"></param>
        public static void Remove(string id)
        {
            if (_cache.TryRemove(id, out HttpSession httpSession))
            {
                httpSession.Clear();
            }
        }

    }
}
