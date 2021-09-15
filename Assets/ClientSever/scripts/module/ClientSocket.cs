using System.Threading.Tasks;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Linq;
using CipherTool;

namespace ClientSever 
{
    public class ClientSocket 
    {
        public string address { get; }
        public int port { get; }
        public TcpClient tcpClient = null;
        public NetworkStream stream = null;
        private Thread readThread = null;
        private Thread writeThread = null;
        private bool isRun = true;
        private CancellationTokenSource cs = null;

        public int CONNECT_TIMEOUT = 1000 * 5;
        public int SEND_TIMEOUT = 1000 * 5;
        public int RECEIVE_TIMEOUT = 1000 * 10;
        const int MAX_READ = 1024*8;
        private byte[] byteBuffer = new byte[MAX_READ];

        private Queue<Data> sendQueues = new Queue<Data>();

        public void SetConnectTimeOut(int timeOut)
        {
            CONNECT_TIMEOUT = timeOut;
        }

        public void SetSendTimeOut(int timeOut)
        {
            SEND_TIMEOUT = timeOut;
        }

        public void SetReceiveTimeOut(int timeOut)
        {
            RECEIVE_TIMEOUT = timeOut;
        }

        /// <summary>
        /// 建立连接
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public async void Connect(string address, int port)
        {
            if(cs != null)
            {
                Debug.LogError("正在连接中，无法再次开启连接...");
                return;
            }
            if (tcpClient != null && tcpClient.Connected)
            {
                Debug.LogError("已经处于连接中...");
                NotfiySocketStates(SocketType.SOCKET_CONNECT_CONNECTED);
                return;
            }
            IPAddress[] ips = Dns.GetHostAddresses(address);
            if (address.Length == 0) {
                Debug.LogError("连接地址无效...");
                NotfiySocketStates(SocketType.SOCKET_CONNECT_FAIL);
                return;
            }
            try
            {
                if (ips[0].AddressFamily == AddressFamily.InterNetworkV6) {
                    tcpClient = new TcpClient(AddressFamily.InterNetworkV6);
                }
                else {
                    tcpClient = new TcpClient(AddressFamily.InterNetwork);
                }
                tcpClient.SendTimeout = SEND_TIMEOUT;
                tcpClient.ReceiveTimeout = RECEIVE_TIMEOUT;
                tcpClient.NoDelay = true;

                cs = new CancellationTokenSource();
                cs.Token.Register(()=>{
                    Debug.Log("正在连接中，手动关闭连接...");
                    Close();
                });
                var taskDelay = Task.Delay(CONNECT_TIMEOUT, cs.Token);
                var taskCo = tcpClient.ConnectAsync(IPAddress.Parse(address), port);
                await await Task.WhenAny(taskDelay, taskCo);
                if (taskDelay.IsCompleted) //连接超时
                {
                    Debug.Log("建立连接超时...");
                    NotfiySocketStates(SocketType.SOCKET_CONNECT_TIMEOUT);
                }
                else if(taskCo.IsCompleted) 
                {
                    if (tcpClient.Connected) //连接成功
                    {
                        Debug.Log("连接建立成功.");
                        stream = tcpClient.GetStream();
                        #region 开启线程保持通讯
                        isRun = true;
                        readThread = new Thread(Receive);
                        readThread.Start();
                        Debug.Log("开启接收线程成功.");
                        writeThread = new Thread(Send);
                        writeThread.Start();
                        Debug.Log("开启发送线程成功.");
                        #endregion
                        NotfiySocketStates(SocketType.SOCKET_CONNECT_SUCCESS);
                    }else //连接失败
                    {
                        NotfiySocketStates(SocketType.SOCKET_CONNECT_FAIL);
                    }
                }    
                cs = null;
            }
            catch (TaskCanceledException){
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.ToString());
                NotfiySocketStates(SocketType.SOCKET_INVALID);
            }
        }

        public void StopConnect()
        {
            if(cs != null && !cs.IsCancellationRequested)
            {
                cs.Cancel();
            }
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        private void Receive()
        {
            int count = 0;
            while (isRun)
            {
                try
                {
                    //判断连接是否正常
                    if (tcpClient.Client.Poll(10, SelectMode.SelectRead))
                    {
                        Debug.Log("连接已中断...");
                        NotfiySocketStates(SocketType.SOCKET_CONNECT_FAIL);
                        return;
                    }
                    //读取数据流
                    lock (stream)
                    {
                        count = stream.Read(byteBuffer, 0, MAX_READ);
                    }
                    if (count > 0)
                    {
                        //读数据包
                        byte[] data = byteBuffer.Skip(0).Take(count).ToArray();
                        //TODO 解密包
                        data = DesCipher.GetInstance().DesDecrypt(data);
                        count = data.Length;
                        if (count < 4) //包大小不对
                        {
                            throw new Exception("接收数据包大小不对!");
                        }
                        var buffer = DataManager.GetInstance().Get();
                        buffer.Read(data);
                        buffer.ReadMainSub();
                        buffer.SetLen(count-4);
                        if (buffer.GetMain() == SocketCmd.SOCKET_MAIN_HEART && buffer.GetSub() == SocketCmd.SOCKET_SUB_HEART) //心跳
                        {
                            Debug.Log("接收 Heart Main:"+buffer.GetMain()+",Sub:"+buffer.GetSub());
                            buffer.SetOffset(count);
                            SendScoketData(buffer);
                        }
                        else //将数据包交给逻辑层
                        {
                            Debug.Log("接收 Main:"+buffer.GetMain()+",Sub:"+buffer.GetSub()+",Len:"+(count-4));
                            CSManager.GetInstance().AddNotfiyToClient(NotfiyType.NOTFIY_SOCKET_RECEIVE, 0, buffer); //交给逻辑层
                        }
                        //清空数组
                        Array.Clear(byteBuffer, 0, byteBuffer.Length);
                        count = 0;
                    }
                }
                catch(ThreadAbortException){
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.ToString());
                    NotfiySocketStates(SocketType.SOCKET_CONNECT_FAIL);
                }
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        private void Send()
        {
            while (isRun)
            {
                try
                {
                    Data buffer = null;
                    lock (sendQueues)
                    {
                        if (sendQueues.Count > 0)
                        {
                            buffer = sendQueues.Dequeue();
                        }   
                    }
                    if (buffer != null)
                    {
                        var bytes = buffer.GetBytes();
                        var len = bytes.Length - 4;
                        if (bytes.Length < 0) //包大小不对
                        {
                            throw new Exception("发送数据包大小不对!");
                        }
                        //TODO 加密包
                        bytes = DesCipher.GetInstance().DesEncrypt(bytes);
                        stream.Write(bytes, 0, bytes.Length);
                        Debug.Log("发送 Main:"+buffer.GetMain()+",Sub:"+buffer.GetSub()+",Len:"+len);
                        DataManager.GetInstance().Release(buffer);
                    }
                }
                catch(ThreadAbortException){
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(ex.ToString());
                }
            }
        }

        public void SendScoketData(Data buffer)
        {
            sendQueues.Enqueue(buffer);
        }

        /// <summary>
        /// Socket状态通知前台
        /// </summary>
        /// <param name="states"></param>
        /// <param name="msg"></param>
        public void NotfiySocketStates(int states)
        {
            CSManager.GetInstance().AddNotfiyToClient(NotfiyType.NOTFIY_SOCKET_STATES, states);
            if (states != SocketType.SOCKET_CONNECT_SUCCESS
                 && states != SocketType.SOCKET_DISCONNECT_SUCCESS
                 && states != SocketType.SOCKET_CONNECT_CONNECTED)
            {
                Close();
            }
        }

        public void Close()
        {
            try
            {
                isRun = false;
                if (tcpClient != null)
                {
                    if (tcpClient.Connected)
                    {
                        tcpClient.Dispose();
                    }
                    tcpClient = null;
                }
                if (readThread != null)
                {
                    if (readThread.IsAlive)
                    {
                        readThread.Abort();
                    }
                    readThread = null;
                }
                if (writeThread != null)
                {
                    if (writeThread.IsAlive)
                    {
                        writeThread.Abort();
                    }
                    writeThread = null;
                }
                sendQueues.Clear();
                cs = null;
                Debug.Log("连接已关闭...");
            }
            catch (ThreadAbortException){
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
            finally
            {
                NotfiySocketStates(SocketType.SOCKET_DISCONNECT_SUCCESS);
            }
        }
    }
}
