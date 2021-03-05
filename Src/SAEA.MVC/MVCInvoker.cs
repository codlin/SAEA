﻿/****************************************************************************
*Copyright (c) 2018-2020 yswenli All Rights Reserved.
*CLR版本： 4.0.30319.42000
*机器名称：WENLI-PC
*公司名称：yswenli
*命名空间：SAEA.MVC
*文件名： MvcInvoker
*版本号： v6.0.0.1
*唯一标识：eb956356-8ea4-4657-aec1-458a3654c078
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/4/10 18:10:16
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/4/10 18:10:16
*修改人： yswenli
*版本号： v6.0.0.1
*描述：
*
*****************************************************************************/
using SAEA.Common;
using SAEA.Common.NameValue;
using SAEA.Common.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace SAEA.MVC
{
    /// <summary>
    /// saea.mvc实例方法映射处理类
    /// </summary>
    public static class MvcInvoker
    {
        /// <summary>
        /// MVC处理
        /// </summary>
        /// <param name="routeTable"></param>
        /// <param name="controller"></param>
        /// <param name="actionName"></param>
        /// <param name="nameValues"></param>
        /// <param name="isPost"></param>
        /// <returns></returns>
        public static ActionResult InvokeResult(RouteTable routeTable, Type controller, string actionName, NameValueCollection nameValues, bool isPost)
        {
            if (routeTable == null) return new ContentResult($"o_o，当前未注册任何Controller!", System.Net.HttpStatusCode.NotFound);

            var routing = routeTable.GetOrAdd(controller, actionName, isPost);

            if (routing == null)
            {
                return new ContentResult($"o_o，找不到：{controller.Name}/{actionName} 当前请求为:{(isPost ? ConstHelper.HTTPPOST : ConstHelper.HTTPGET)}", System.Net.HttpStatusCode.NotFound);
            }

            ActionResult result;

            //类过滤器
            if (routing.FilterAtrrs != null && routing.FilterAtrrs.Any())
            {
                foreach (var arr in routing.FilterAtrrs)
                {
                    var method = arr.GetType().GetMethod(ConstHelper.ONACTIONEXECUTING);

                    if (method != null)
                    {
                        var goOn = (bool)method.Invoke(arr, null);

                        if (!goOn)
                        {
                            return new ContentResult("o_o，拒绝访问！", System.Net.HttpStatusCode.Forbidden);
                        }
                    }
                }
            }

            //方法过滤器
            if (routing.ActionFilterAtrrs != null && routing.ActionFilterAtrrs.Any())
            {
                foreach (var arr in routing.ActionFilterAtrrs)
                {
                    if (arr.ToString() == ConstHelper.OUTPUTCACHEATTRIBUTE)
                    {
                        var method = arr.GetType().GetMethod(ConstHelper.ONACTIONEXECUTING);

                        if (method != null)
                        {
                            HttpContext.Current.Session.CacheCalcString = (string)arr.GetType().GetMethod(ConstHelper.ONACTIONEXECUTING).Invoke(arr, null);

                            if (HttpContext.Current.Session.CacheCalcString.IndexOf("1,") == 0)
                            {
                                return new ContentResult(string.Empty, System.Net.HttpStatusCode.NotModified);
                            }
                        }
                    }
                    else
                    {
                        var method = arr.GetType().GetMethod(ConstHelper.ONACTIONEXECUTING);

                        if (method != null)
                        {
                            var goOn = (bool)arr.GetType().GetMethod(ConstHelper.ONACTIONEXECUTING).Invoke(arr, null);

                            if (!goOn)
                            {
                                return new ContentResult("o_o，拒绝访问！", System.Net.HttpStatusCode.Forbidden);
                            }
                        }
                    }
                }
            }

            #region actionResult                

            if (!string.IsNullOrEmpty(HttpContext.Current.Request.ContentType) 
                && HttpContext.Current.Request.ContentType.IndexOf(ConstHelper.FORMENCTYPE3, StringComparison.InvariantCultureIgnoreCase) > -1
                && !string.IsNullOrEmpty(HttpContext.Current.Request.Json))
            {
                try
                {
                    var dic = SerializeHelper.Deserialize<Dictionary<string, string>>(HttpContext.Current.Request.Json);

                    if (HttpContext.Current.Request.Parmas == null || !HttpContext.Current.Request.Parmas.Any())
                    {
                        HttpContext.Current.Request.Parmas = dic;
                    }
                    else
                    {
                        if (dic != null && dic.Any())
                        {
                            foreach (var item in dic)
                            {
                                HttpContext.Current.Request.Parmas[item.Key] = item.Value;
                            }
                        }
                    }

                    nameValues = SerializeHelper.Deserialize<Dictionary<string, string>>(HttpContext.Current.Request.Json).ToNameValueCollection();
                }
                catch
                {
                    return new ContentResult("o_o，错误请求,Json:" + HttpContext.Current.Request.Json, System.Net.HttpStatusCode.BadRequest);
                }
            }

            result = MethodInvoke(routing.Action, routing.Instance, nameValues);

            #endregion

            var nargs = new object[] { result };

            if (routing.FilterAtrrs != null && routing.FilterAtrrs.Any())
            {
                foreach (var arr in routing.FilterAtrrs)
                {
                    var method = arr.GetType().GetMethod(ConstHelper.ONACTIONEXECUTED);
                    if (method != null)
                        method.Invoke(arr, nargs);
                }
            }

            if (routing.ActionFilterAtrrs != null && routing.ActionFilterAtrrs.Any())
            {
                foreach (var arr in routing.ActionFilterAtrrs)
                {
                    if (arr.ToString() == ConstHelper.OUTPUTCACHEATTRIBUTE)
                    {
                        continue;
                    }
                    else
                    {
                        var method = arr.GetType().GetMethod(ConstHelper.ONACTIONEXECUTED);
                        if (method != null)
                            method.Invoke(arr, nargs);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 代理执行方法
        /// </summary>
        /// <param name="action"></param>
        /// <param name="obj"></param>
        /// <param name="nameValues"></param>
        /// <returns></returns>
        static ActionResult MethodInvoke(MethodInfo action, object obj, NameValueCollection nameValues)
        {
            ActionResult result = null;

            object data;

            var @params = action.GetParameters();

            if (@params != null && @params.Any())
            {
                var list = ParamsHelper.FillPamars(@params, nameValues);

                data = action.Invoke(obj, list.ToArray());
            }
            else
            {
                data = action.Invoke(obj, null);
            }

            if (data.GetType().Name == "AsyncStateMachineBox`1" || data.GetType().Name == "Task`1")
            {
                var tdata = data as Task<ActionResult>;

                result = tdata.Result;
            }
            else
            {
                result = (ActionResult)data;
            }
            return result;
        }


    }
}
