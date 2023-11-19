/****************************************************************************
 * 
  ____    _    _____    _      ____             _        _   
 / ___|  / \  | ____|  / \    / ___|  ___   ___| | _____| |_ 
 \___ \ / _ \ |  _|   / _ \   \___ \ / _ \ / __| |/ / _ \ __|
  ___) / ___ \| |___ / ___ \   ___) | (_) | (__|   <  __/ |_ 
 |____/_/   \_\_____/_/   \_\ |____/ \___/ \___|_|\_\___|\__|
                                                             

*Copyright (c) 2018-2022yswenli All Rights Reserved.
*CLR版本： 2.1.4
*机器名称：WENLI-PC
*公司名称：wenli
*命名空间：SAEA.Sockets.Core.Tcp
*文件名： IocpServerSocket
*版本号： v7.0.0.1
*唯一标识：ef84e44b-6fa2-432e-90a2-003ebd059303
*当前的用户域：WENLI-PC
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2018/3/1 15:54:21
*描述：
*
*=====================================================================
*修改标记
*修改时间：2018/3/1 15:54:21
*修改人： yswenli
*版本号： v7.0.0.1
*描述：
*
*****************************************************************************/

using SAEA.Sockets.Handler;
using SAEA.Sockets.Interface;
using SAEA.Sockets.Model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SAEA.Sockets.Core.Tcp
{
    /// <summary>
    /// iocp 服务器 socket
    /// 支持使用自定义 IContext 来扩展
    /// Socket server 要完成的事大概分几类：
    /// 1. 记录监听的 socket；
    /// 2. Server 资源释放，例如监听的 socket 资源释放
    /// 3. 客户端会话管理及客户端个数 
    /// 4. Socket 选项配置
    /// 5. 启动和停止服务
    /// 6. Server 相关的一系列事件，如新连接到达、连接关闭、服务器处理错误、数据到达等等事件
    /// 7. 
    /// IOCP 模式请参考：https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketasynceventargs?view=net-7.0#remarks
    /// </summary>
    public class IocpServerSocket : IServerSocket, IDisposable
    {
        Socket _listener = null;

        int _clientCounts;

        private SessionManager _sessionManager;

        public SessionManager SessionManager
        {
            get { return _sessionManager; }
        }

        public bool IsDisposed
        {
            get; set;
        } = false;

        public int ClientCounts { get => _clientCounts; private set => _clientCounts = value; }

        public ISocketOption SocketOption { get; set; }

        #region events

        public event OnAcceptedHandler OnAccepted;

        // 对于 OnError 的设计个人感觉有些模糊，因为把 Server 的错误和处理具体的某个 socket 的错误混在了一起（譬如说接收数据时出错）。
        public event OnErrorHandler OnError;

        public event OnDisconnectedHandler OnDisconnected;

        public event OnReceiveHandler OnReceive;

        #endregion

        /// <summary>
        /// socket收到数据时的代理
        /// </summary>
        private OnServerReceiveBytesHandler OnServerReceiveBytes;

        /// <summary>
        /// iocp 服务器 socket
        /// </summary>>
        /// <param name="socketOption"></param>
        public IocpServerSocket(ISocketOption socketOption)
        {
            _sessionManager = new SessionManager(socketOption.Context,
                socketOption.ReadBufferSize,
                socketOption.Count, IO_Completed,
                new TimeSpan(0, 0, 0, 0, socketOption.FreeTime));
            _sessionManager.OnTimeOut += _sessionManager_OnTimeOut;
            OnServerReceiveBytes = new OnServerReceiveBytesHandler(OnReceiveBytes);
            SocketOption = socketOption;
        }


        /// <summary>
        /// 启动服务
        /// </summary>
        /// <param name="backlog"></param>
        public void Start(int backlog = 10 * 1000)
        {
            if (_listener == null)
            {
                IPEndPoint ipEndPoint;

                if (SocketOption.UseIPV6)
                {
                    _listener = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

                    if (string.IsNullOrEmpty(SocketOption.IP))
                        ipEndPoint = (new IPEndPoint(IPAddress.IPv6Any, SocketOption.Port));
                    else
                        ipEndPoint = (new IPEndPoint(IPAddress.Parse(SocketOption.IP), SocketOption.Port));
                } else
                {
                    _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    if (string.IsNullOrEmpty(SocketOption.IP))
                        ipEndPoint = (new IPEndPoint(IPAddress.Any, SocketOption.Port));
                    else
                        ipEndPoint = (new IPEndPoint(IPAddress.Parse(SocketOption.IP), SocketOption.Port));
                }

                if (SocketOption.ReusePort)
                    _listener.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, SocketOption.ReusePort);

                _listener.NoDelay = SocketOption.NoDelay;

                _listener.SendBufferSize = SocketOption.WriteBufferSize;
                _listener.ReceiveBufferSize = SocketOption.ReadBufferSize;

                _listener.Bind(ipEndPoint);

                _listener.Listen(backlog);

                ProcessAccept(null);
            }
        }

        private void AccepteArgs_Completed(object sender, SocketAsyncEventArgs acceptArgs)
        {
            if (acceptArgs.LastOperation == SocketAsyncOperation.Accept)
            {
                ProcessAccepted(acceptArgs);
            }
        }

        private void ProcessAccept(SocketAsyncEventArgs acceptArgs)
        {
            if (acceptArgs == null)
            {
                acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(AccepteArgs_Completed);
            } else
            {
                // 异步回调 AccepteArgs_Completed 过来的 acceptArgs， 需要清掉上次接受的 socket
                acceptArgs.AcceptSocket = null;
            }
            try
            {
                if (!IsDisposed && _listener != null)
                {
                    // 如果返回 true，说明是 I/O 阻塞，等 I/O 完成时会调用注册 Completed 事件注册的回调函数。
                    if (!_listener.AcceptAsync(acceptArgs))
                        // 如果返回 false，代表是同步返回，不会引发 SocketAsyncEventArgs.Completed 事件，即不会回到 AccepteArgs_Completed
                        ProcessAccepted(acceptArgs);
                }
            } catch (Exception ex)
            {
                OnError?.Invoke("IocpServerSocket.ProcessAccepte", ex);
            }
        }

        /// <summary>
        /// 该函数会在多个上下文环境中操作，如果在多线程间共享变量，需要加锁
        /// </summary>
        /// <param name="acceptArgs"></param>
        private void ProcessAccepted(SocketAsyncEventArgs acceptArgs)
        {
            try
            {
                var socket = acceptArgs.AcceptSocket;

                if (socket.Connected == false)
                {
                    return;
                }

                socket.NoDelay = SocketOption.NoDelay;

                socket.ReceiveBufferSize = SocketOption.ReadBufferSize;

                socket.SendBufferSize = SocketOption.WriteBufferSize;

                socket.ReceiveTimeout = socket.SendTimeout = SocketOption.TimeOut;

                // 从会话管理中取出一个用户令牌，同时把 socket 信息保存在缓存中。
                // 会话管理中用户令牌池用来控制最大可接入的 socket 个数。
                // 其大小由 SocketOption.Count 在设置的服务器支持的最大 socket 个数时指定，通常在创建 server 时指定。
                var userToken = _sessionManager.BindUserToken(socket, SocketOption.TimeOut);
                if (userToken == null)
                {
                    return;
                }

                var readArgs = userToken.ReadArgs;

                // Interlocked 为多线程的共享变量提供原子操作。
                Interlocked.Increment(ref _clientCounts);

                OnAccepted?.Invoke(userToken);

                ProcessReceive(readArgs);
            } catch (Exception ex)
            {
                OnError?.Invoke("IocpServerSocket.ProcessAccepted", ex);
            } finally
            {
                ProcessAccept(acceptArgs);
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceived(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSended(e);
                    break;
                default:
                    try
                    {
                        var userToken = (IUserToken)e.UserToken;
                        Disconnect(userToken, new KernelException("Operation-exceptions，SocketAsyncOperation：" + e.LastOperation));
                    } catch { }
                    break;
            }
        }

        /// <summary>
        /// 服务器收到数据时的处理方法
        /// 需要继承者自行实现具体逻辑
        /// </summary>
        /// <param name="userToken"></param>
        /// <param name="data"></param>
        protected virtual void OnReceiveBytes(IUserToken userToken, byte[] data)
        {
            OnReceive?.Invoke(userToken, data);
        }


        /// <summary>
        /// 该函数会根据情况运行在不同的上下文，如果 AcceptAsync 是同步返回，则运行在服务器启动的上下文，
        /// 如果是异步回调返回，则在线程池线程上下文（系统是否会切回到主上下文待查证）
        /// </summary>
        /// <param name="readArgs"></param>
        private void ProcessReceive(SocketAsyncEventArgs readArgs)
        {
            var userToken = (IUserToken)readArgs.UserToken;
            try
            {
                if (readArgs != null && userToken != null && userToken.Socket != null && userToken.Socket.Connected)
                {
                    // readArgs 是服务器接收新连接时从用户令牌池中取出的，在初始化令牌池时，Completed 事件的回调被注册为 IO_Completed 方法。
                    // 因此当异步调用返回时会调用 IO_Completed 方法，在该方法中检查并处理发送或接收。
                    if (!userToken.Socket.ReceiveAsync(readArgs))
                        ProcessReceived(readArgs);
                } else
                {
                    if (userToken.Socket != null)
                        Disconnect(userToken, new KernelException("The remote client has been disconnected."));
                }

            } catch (Exception exp)
            {
                var kex = new KernelException("An exception occurs when a message is received:" + exp.Message, exp);
                Disconnect(userToken, kex);
            }
        }

        /// <summary>
        /// 处理接收到数据
        /// 不同的线程可能会同时调用该函数，线程间共享变量需要加锁
        /// </summary>
        /// <param name="readArgs"></param>
        void ProcessReceived(SocketAsyncEventArgs readArgs)
        {
            var userToken = (IUserToken)readArgs.UserToken;

            if (userToken == null)
            {
                OnError?.Invoke("", new NullReferenceException("当前对象已回收!"));
                return;
            }

            try
            {
                if (readArgs.SocketError == SocketError.Success && readArgs.BytesTransferred > 0)
                {
                    /// 保活
                    _sessionManager.Active(userToken.ID);

                    try
                    {
                        var buffer = readArgs.Buffer.AsSpan().Slice(readArgs.Offset, readArgs.BytesTransferred).ToArray();
                        OnServerReceiveBytes.Invoke(userToken, buffer);

                        //在复用数组和线程切换之间
                        //using (var pooledBytes = new PooledBytes(readArgs.BytesTransferred))
                        //{
                        //    pooledBytes.BlockCopy(readArgs.Buffer, readArgs.Offset, readArgs.BytesTransferred);
                        //    OnServerReceiveBytes.Invoke(userToken, pooledBytes.Bytes);
                        //}
                    } catch (Exception ex)
                    {
                        OnError?.Invoke(userToken.ID, ex);
                    }

                    ProcessReceive(readArgs);
                } else
                {
                    Disconnect(userToken, null);
                }
            } catch (Exception exp)
            {
                var kex = new KernelException("An exception occurs when a message is received:" + exp.Message, exp);
                Disconnect(userToken, kex);
            }
        }

        private void ProcessSended(SocketAsyncEventArgs e)
        {
            var userToken = (IUserToken)e.UserToken;
            if (userToken == null) return;
            _sessionManager.Active(userToken.ID);
            userToken?.Set();
        }

        #region send method

        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="userToken"></param>
        /// <param name="data"></param>
        public void SendAsync(IUserToken userToken, byte[] data)
        {
            if (userToken.WaitOne(SocketOption.TimeOut))
            {
                try
                {
                    var writeArgs = userToken.WriteArgs;

                    if (writeArgs != null)
                    {
                        writeArgs.SetBuffer(data, 0, data.Length);

                        if (!userToken.Socket.SendAsync(writeArgs))
                        {
                            ProcessSended(writeArgs);
                        }
                    }
                } catch (Exception ex)
                {
                    OnError?.Invoke($"An exception occurs when a message is sending:{userToken?.ID}", ex);
                }
            } else
            {
                OnError?.Invoke($"An exception occurs when a message is sending:{userToken?.ID}", new TimeoutException("Sending data timeout"));
            }
        }


        /// <summary>
        /// 异步发送
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="data"></param>
        public void SendAsync(string sessionID, byte[] data)
        {
            var userToken = _sessionManager.Get(sessionID);

            if (userToken == null)
            {
                return;
            }
            SendAsync(userToken, data);
        }


        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="userToken"></param>
        /// <param name="data"></param>
        public void Send(IUserToken userToken, byte[] data)
        {
            KernelException kex = null;
            try
            {
                _sessionManager.Active(userToken.ID);

                int sendNum = 0, offset = 0;

                while (true)
                {
                    sendNum += userToken.Socket.Send(data, offset, data.Length - offset, SocketFlags.None);

                    offset += sendNum;

                    if (sendNum == data.Length)
                    {
                        break;
                    }
                }
            } catch (Exception ex)
            {
                kex = new KernelException("An exception occurs when a message is sending:" + ex.Message, ex);
            }
            if (kex != null)
            {
                Disconnect(userToken, kex);
            }
        }


        /// <summary>
        /// 同步发送
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="data"></param>
        public void Send(string sessionID, byte[] data)
        {
            var userToken = _sessionManager.Get(sessionID);
            if (userToken != null)
            {
                Send(userToken, data);
            }
        }

        /// <summary>
        /// APM方式发送
        /// </summary>
        /// <param name="userToken"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public IAsyncResult BeginSend(IUserToken userToken, byte[] data)
        {
            try
            {
                _sessionManager.Active(userToken.ID);

                return userToken.Socket.BeginSend(data, 0, data.Length, SocketFlags.None, null, null);
            } catch (Exception ex)
            {
                Disconnect(userToken);
                throw new KernelException("An exception occurs when a message is sending:" + ex.Message, ex);
            }
        }

        /// <summary>
        /// APM方式结束发送
        /// </summary>
        /// <param name="userToken"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public int EndSend(IUserToken userToken, IAsyncResult result)
        {
            return userToken.Socket.EndSend(result);
        }

        /// <summary>
        /// 回复并关闭连接
        /// 用于http
        /// </summary>
        /// <param name="sessionID"></param>
        /// <param name="data"></param>
        public void End(string sessionID, byte[] data)
        {
            var userToken = _sessionManager.Get(sessionID);

            if (userToken == null) return;

            if (userToken.Socket != null && userToken.Socket.Connected)
            {
                Send(userToken, data);
            }
            Disconnect(userToken);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="ipEndPoint"></param>
        /// <param name="data"></param>
        public void SendAsync(IPEndPoint ipEndPoint, byte[] data)
        {
            SendAsync(ipEndPoint.ToString(), data);
        }
        #endregion



        private void _sessionManager_OnTimeOut(IUserToken userToken)
        {
            Disconnect(userToken);
        }


        public object GetCurrentObj(string sessionID)
        {
            return SessionManager.Get(sessionID);
        }

        /// <summary>
        /// 断开客户端连接
        /// </summary>
        /// <param name="userToken"></param>
        /// <param name="ex"></param>
        public void Disconnect(IUserToken userToken, Exception ex = null)
        {
            if (_sessionManager.Free(userToken))
            {
                Interlocked.Decrement(ref _clientCounts);
                OnDisconnected?.Invoke(userToken.ID, ex);
            }
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="sessionID"></param>
        public void Disconnect(string sessionID)
        {
            var userToken = SessionManager.Get(sessionID);
            if (userToken != null)
                Disconnect(userToken);
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Stop()
        {

            try
            {
                _listener.Close(10 * 1000);
            } catch { }
            try
            {
                _sessionManager.Clear();
            } catch { }
            try
            {
                _listener.Dispose();
                _listener = null;
            } catch { }
        }

        public void Dispose()
        {
            try
            {
                Stop();
                IsDisposed = true;
            } catch { }
        }
    }
}
