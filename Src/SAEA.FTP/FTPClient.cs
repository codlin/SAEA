﻿/****************************************************************************
*项目名称：SAEA.FTP
*CLR 版本：4.0.30319.42000
*机器名称：WALLE-PC
*命名空间：SAEA.FTP
*类 名 称：FTPClient
*版 本 号：V1.0.0.0
*创建人： yswenli
*电子邮箱：yswenli@outlook.com
*创建时间：2019/9/27 15:08:55
*描述：
*=====================================================================
*修改时间：2019/9/27 15:08:55
*修 改 人： yswenli
*版本号： v7.0.0.1
*描    述：
*****************************************************************************/
using SAEA.Common;
using SAEA.Common.IO;
using SAEA.Common.Threading;
using SAEA.FTP.Model;
using SAEA.FTP.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SAEA.FTP
{
    public class FTPClient : IDisposable
    {
        ClientSocket _client;

        public event Action OnConnected;

        public event Action Ondisconnected;

        public bool Connected
        {
            get
            {
                return _client.Connected;
            }
        }

        DateTime _Actived;

        public FTPClient(ClientConfig config)
        {
            _client = new ClientSocket(config);
            _client.OnDisconnected += _client_OnDisconnected;
        }



        public FTPClient(string ip, int port, string userName, string password, int bufferSize = 10240) : this(new ClientConfig() { IP = ip, Port = port, UserName = userName, Password = password, BufferSize = bufferSize })
        {

        }

        public void Connect()
        {
            _client.Connect();
            OnConnected?.Invoke();
            _Actived = DateTimeHelper.Now;
            TaskHelper.Run(() =>
            {
                while (Connected)
                {
                    if (_Actived.AddMinutes(1) < DateTimeHelper.Now)
                    {
                        Noop();
                        _Actived = DateTimeHelper.Now;
                    }
                    ThreadHelper.Sleep(500);
                }
            });
        }

        public void Noop()
        {
            try
            {
                _client.BaseRequest($"{FTPCommand.NOOP}");
            }
            catch (Exception ex)
            {
                LogHelper.Error("FTPClient Noop", ex);
            }
        }

        /// <summary>
        /// 更改工作目录
        /// </summary>
        /// <param name="pathName"></param>
        /// <returns></returns>
        public bool ChangeDir(string pathName)
        {
            var sres = _client.BaseRequest($"{FTPCommand.CWD} {pathName}");

            Active();

            if (sres.Code == ServerResponseCode.文件行为完成)
            {
                return true;
            }
            if (sres.Code == ServerResponseCode.找不到文件或文件夹)
            {
                return false;
            }
            throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
        }
        /// <summary>
        /// 更改工作目录到父目录
        /// </summary>
        /// <returns></returns>
        public bool ChangeToParentDir()
        {
            var sres = _client.BaseRequest($"{FTPCommand.CDUP}");

            Active();

            if (sres.Code == ServerResponseCode.文件行为完成 || sres.Code == ServerResponseCode.成功)
            {
                return true;
            }
            if (sres.Code == ServerResponseCode.找不到文件或文件夹)
            {
                return false;
            }
            throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
        }
        /// <summary>
        /// 返回当前工作目录目录
        /// </summary>
        /// <returns></returns>
        public string CurrentDir()
        {
            var sres = _client.BaseRequest($"{FTPCommand.PWD}");

            Active();

            if (sres.Code == ServerResponseCode.路径名建立)
            {
                var dir = sres.Reply;

                dir = dir.Substring(dir.IndexOf("\"") + 1);

                dir = dir.Substring(0, dir.IndexOf("\""));

                return dir;
            }
            throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
        }

        /// <summary>
        /// 功能：返回指定路径下的子目录及文件列表，默认为当前工作地址
        /// </summary>
        /// <param name="pathName"></param>
        /// <param name="dirType"></param>
        /// <returns></returns>
        public List<string> Dir(string pathName = "/", DirType dirType = DirType.MLSD)
        {
            _client.FTPDataManager.IsFile = false;

            using (var dataSocket = _client.CreateDataConnection())
            {
                _client.FTPDataManager.Refresh();

                var sres = _client.BaseRequest($"{dirType.ToString()} {pathName}");

                Active();

                var str = _client.FTPDataManager.ReadAllText();

                if (string.IsNullOrEmpty(str))
                {
                    if (ChangeDir(pathName))
                    {
                        return new List<string>();
                    }
                    else
                    {
                        return null;
                    }
                }
                return str.Split(Environment.NewLine).ToList();
            }
        }

        public void MakeDir(string pathName)
        {
            var sres = _client.BaseRequest($"{FTPCommand.MKD} {pathName}");

            Active();

            if (sres.Code != ServerResponseCode.文件行为完成 && sres.Code != ServerResponseCode.路径名建立)
            {
                throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
            }
        }

        public void RemoveDir(string pathName)
        {
            var sres = _client.BaseRequest($"{FTPCommand.RMD} {pathName}");

            Active();

            if (sres.Code != ServerResponseCode.文件行为完成)
            {
                throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
            }
        }


        public void Rename(string oldName, string newName)
        {
            _client.BaseRequest($"{FTPCommand.RNFR} {oldName}");

            var sres = _client.BaseRequest($"{FTPCommand.RNTO} {newName}");

            Active();

            if (sres.Code != ServerResponseCode.文件行为完成)
            {
                throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
            }
        }

        public void Delete(string fileName)
        {
            var sres = _client.BaseRequest($"{FTPCommand.DELE} {fileName}");

            Active();

            if (sres.Code != ServerResponseCode.文件行为完成)
            {
                throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
            }
        }


        public void Upload(string filePath, Action<long, long> uploading = null)
        {
            var running = true;

            using (var dataSocket = _client.CreateDataConnection())
            {
                _client.FTPDataManager.IsFile = true;

                var fileName = PathHelper.GetFileName(filePath);

                var sres = _client.BaseRequest($"{FTPCommand.STOR} {fileName}");

                Active();

                if (sres.Code != ServerResponseCode.打开数据连接开始传输 && sres.Code != ServerResponseCode.打开连接)
                {
                    throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
                }

                long count = 1;

                long offset = 0;

                TaskHelper.Run(() =>
                {
                    while (running && _client.Connected)
                    {
                        uploading?.Invoke(offset, count);

                        if (offset == count)
                        {
                            uploading?.Invoke(offset, count);

                            break;
                        }
                        ThreadHelper.Sleep(1);
                    }
                });


                try
                {
                    using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
                    {
                        count = fs.Length;

                        byte[] data = new byte[_client.Config.BufferSize];

                        int numBytesRead = 0;

                        while (true)
                        {
                            int n = fs.Read(data, 0, _client.Config.BufferSize);

                            if (n == 0)
                                break;

                            offset = numBytesRead += n;

                            if (n == _client.Config.BufferSize)
                            {
                                dataSocket.Send(data);
                            }
                            else
                            {
                                dataSocket.Send(data.AsSpan().Slice(0, n).ToArray());
                                break;
                            }
                        }
                        dataSocket.Disconnect();
                    }
                }
                catch(Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    running = false;
                }
            }
        }

        public string Download(string fileName, string filePath, Action<long, long> downing = null)
        {
            var running = true;

            var count = FileSize(fileName);

            long offset = 0;

            TaskHelper.Run(() =>
            {
                while (running && _client.Connected)
                {
                    downing?.Invoke(offset, count);

                    if (offset == count)
                    {
                        downing?.Invoke(offset, count);
                        break;
                    }
                    ThreadHelper.Sleep(500);
                }
            });

            using (var dataSocket = _client.CreateDataConnection())
            {
                _client.FTPDataManager.IsFile = true;

                _client.FTPDataManager.New(filePath);

                var sres = _client.BaseRequest($"{FTPCommand.RETR} {fileName}");

                Active();

                if (sres.Code == ServerResponseCode.结束数据连接 || sres.Code == ServerResponseCode.打开连接)
                {
                    //确认长度后断开
                    _client.FTPDataManager.Checked(count, ref offset);

                    dataSocket.Disconnect();

                    running = false;

                    return filePath;
                }
                else
                {
                    running = false;
                    throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
                }
            }
        }


        public long FileSize(string fileName)
        {
            var sres = _client.BaseRequest($"{FTPCommand.SIZE} {fileName}");

            Active();

            if (sres.Code == ServerResponseCode.文件状态回复)
            {
                return long.Parse(sres.Reply);
            }
            else
            {
                throw new IOException($"code:{sres.Code},reply:{sres.Reply}");
            }
        }

        public void Quit()
        {
            if (_client != null && _client.Connected)
            {
                _client.BaseRequest($"{FTPCommand.QUIT}");

                _client.Disconnect();

                _client.Dispose();
            }
        }

        public void Dispose()
        {
            try
            {
                Quit();
            }
            catch { }
        }

        private void Active()
        {
            _Actived = DateTimeHelper.Now;
        }

        private void _client_OnDisconnected(string ID, Exception ex)
        {
            Ondisconnected?.Invoke();
        }
    }
}
