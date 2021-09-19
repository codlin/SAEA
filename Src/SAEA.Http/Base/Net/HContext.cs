﻿/****************************************************************************
*Copyright (c) 2018-2021yswenli All Rights Reserved.
*CLR版本： 4.0.30319.42000
*机器名称：WENLI-PC
*公司名称：yswenli
*命名空间：SAEA.Http.Base.Net
*文件名： HContext
*版本号： v6.0.0.1
*唯一标识：5977c7e0-64a5-44d5-8931-fcaeec6b203a
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/4/8 17:18:42
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/4/8 17:18:42
*修改人： yswenli
*版本号： v6.0.0.1
*描述：
*
*****************************************************************************/
using SAEA.Sockets.Base;
using SAEA.Sockets.Interface;

namespace SAEA.Http.Base.Net
{
    class HContext : IContext
    {
        public IUserToken UserToken { get; set; }

        public IUnpacker Unpacker { get; set; }

        /// <summary>
        /// 上下文
        /// </summary>
        public HContext()
        {
            this.UserToken = new BaseUserToken();
            this.Unpacker = new HUnpacker();
            this.UserToken.Unpacker = this.Unpacker;
        }
    }
}
