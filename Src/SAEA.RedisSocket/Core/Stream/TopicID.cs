﻿/****************************************************************************
*项目名称：SAEA.RedisSocket.Core.Stream
*CLR 版本：4.0.30319.42000
*机器名称：WALLE-PC
*命名空间：SAEA.RedisSocket.Core.Stream
*类 名 称：TopicID
*版 本 号：V1.0.0.0
*创建人： yswenli
*电子邮箱：yswenli@outlook.com
*创建时间：2021/1/8 19:34:22
*描述：
*=====================================================================
*修改时间：2021/1/8 19:34:22
*修 改 人： yswenli
*版本号： v7.0.0.1
*描    述：
*****************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;

namespace SAEA.RedisSocket.Core.Stream
{
    public struct TopicID
    {
        public string Topic { get; set; }

        public string RedisID { get; set; }

        public TopicID(string topic,string redisID)
        {
            Topic = topic;
            RedisID = redisID;
        }
    }
}
