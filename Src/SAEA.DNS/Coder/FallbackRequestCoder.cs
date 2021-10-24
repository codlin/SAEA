﻿/****************************************************************************
*项目名称：SAEA.DNS
*CLR 版本：3.0
*机器名称：WENLI-PC
*命名空间：SAEA.DNS.Coder
*类 名 称：FallbackRequestCoder
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
using System.Threading;
using System.Threading.Tasks;

namespace SAEA.DNS.Coder
{
    /// <summary>
    /// 回退请求编码器
    /// </summary>
    class FallbackRequestCoder : IRequestCoder
    {
        private IRequestCoder[] _coders;

        /// <summary>
        /// 回退请求编码器
        /// </summary>
        /// <param name="coders"></param>
        public FallbackRequestCoder(params IRequestCoder[] coders)
        {
            this._coders = coders;
        }

        public async Task<IResponse> Code(IRequest request, CancellationToken cancellationToken = default(CancellationToken))
        {
            IResponse response = null;

            foreach (IRequestCoder coder in _coders)
            {
                response = await coder.Code(request, cancellationToken);

                if (response.AnswerRecords.Count > 0) break;
            }

            return response;
        }
    }
}
