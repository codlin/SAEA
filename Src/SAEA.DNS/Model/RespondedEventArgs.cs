﻿/****************************************************************************
*项目名称：SAEA.DNS
*CLR 版本：3.0
*机器名称：WENLI-PC
*命名空间：SAEA.DNS.Model
*类 名 称：RespondedEventArgs
*版 本 号：v5.0.0.1
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2019/11/28 22:43:28
*描述：
*=====================================================================
*修改时间：2019/11/28 22:43:28
*修 改 人： yswenli
*版本号： v7.0.0.1
*描    述：
*****************************************************************************/
using SAEA.DNS.Protocol;
using System;

namespace SAEA.DNS.Model
{
    public class RespondedEventArgs : EventArgs
    {
        public RespondedEventArgs(IRequest request, IResponse response, byte[] data)
        {
            Request = request;
            Response = response;
            Data = data;
        }

        public IRequest Request
        {
            get;
            private set;
        }

        public IResponse Response
        {
            get;
            private set;
        }

        public byte[] Data
        {
            get;
            private set;
        }
    }
}
