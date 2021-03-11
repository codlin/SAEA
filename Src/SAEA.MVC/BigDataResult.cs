﻿/****************************************************************************
*项目名称：SAEA.MVC
*CLR 版本：4.0.30319.42000
*机器名称：WALLE-PC
*命名空间：SAEA.MVC
*类 名 称：BigDataResult
*版 本 号：V1.0.0.0
*创建人： yswenli
*电子邮箱：yswenli@outlook.com
*创建时间：2020/5/20 13:21:42
*描述：
*=====================================================================
*修改时间：2020/5/20 13:21:42
*修 改 人： yswenli
*版 本 号： V1.0.0.0
*描    述：
*****************************************************************************/
using SAEA.Common.IO;
using SAEA.Http.Base;
using SAEA.Http.Model;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace SAEA.MVC
{
    /// <summary>
    /// 大数据结果
    /// </summary>
    public class BigDataResult : ActionResult, IBigDataResult
    {
        /// <summary>
        /// 大数据结果
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="contentType"></param>
        /// <param name="status"></param>
        public BigDataResult(Stream stream, string contentType = "", HttpStatusCode status = HttpStatusCode.OK)
        {
            var total = stream.Length;

            this.ContentEncoding = Encoding.UTF8;
            this.ContentType = contentType;
            this.Status = status;

            HttpContext.Current.Response.ContentType = contentType;
            HttpContext.Current.Response.Headers["Connection"] = "close";
            HttpContext.Current.Response.Status = status;
            HttpContext.Current.Response.SendHeader(total);

            var buffer = new byte[10240];
            stream.Position = 0;
            do
            {
                var count = stream.Read(buffer, 0, buffer.Length);

                if (count < 1)
                {
                    break;
                }

                if (count == 10240)
                {
                    HttpContext.Current.Response.SendData(buffer);
                }
                else
                {
                    var b = new byte[count];

                    Buffer.BlockCopy(buffer, 0, b, 0, count);

                    HttpContext.Current.Response.SendData(b);
                }

            }
            while (true);

            HttpContext.Current.Response.SendEnd();

            stream.Close();
        }


        /// <summary>
        /// 大数据结果
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="status"></param>
        public BigDataResult(string filePath, HttpStatusCode status = HttpStatusCode.OK) : this(FileHelper.GetStream(filePath), HttpMIME.GetType(filePath), status)
        {

        }
    }
}
