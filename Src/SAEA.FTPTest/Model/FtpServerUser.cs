﻿/****************************************************************************
*项目名称：SAEA.FTPTest.Model
*CLR 版本：4.0.30319.42000
*机器名称：WALLE-PC
*命名空间：SAEA.FTPTest.Model
*类 名 称：FtpServerUser
*版 本 号：V1.0.0.0
*创建人： yswenli
*电子邮箱：yswenli@outlook.com
*创建时间：2019/11/12 14:44:55
*描述：
*=====================================================================
*修改时间：2019/11/12 14:44:55
*修 改 人： yswenli
*版 本 号： V1.0.0.0
*描    述：
*****************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAEA.FTPTest.Model
{
    public class FtpServerUser
    {
        public string UserName { get; set; }

        public string Password { get; set; }

        public int DataPort { get; set; } = 22;

        public string Root { get; set; } = "/";
    }
}
