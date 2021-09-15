using System.Collections.Generic;
using UnityEngine;

namespace ClientSever 
{
    public struct SocketNotify
    {
        public int notfiyType;
        public int subType;
        public System.Object pack;
    }

    public delegate void NotifyHandler(SocketNotify notify);

    public class LoopNotify
    {
        //服务端消息通知
        private Queue<SocketNotify> receiveQueues = new  Queue<SocketNotify>();
        private NotifyHandler receiveHandler;

        public void SetReceiveWork(NotifyHandler receiveHandler)
        {
            this.receiveHandler = receiveHandler;
        }

        //服务端消息队列
        public void EnqueueReceiveNotify(SocketNotify notify)
        {
            receiveQueues.Enqueue(notify);
        }

        public void LoopReceiveNotify()
        {
            if (receiveHandler == null)
            {
                Debug.LogWarning("LoopNofity receiveHandler haven't set!");
                return;
            }
            lock (receiveQueues)
            {
                if (receiveQueues.Count > 0)
                {
                    var notify = receiveQueues.Dequeue();
                    if (receiveHandler != null)
                    {
                        receiveHandler(notify);
                    }
                    if (notify.pack != null && notify.pack is Data)
                    {
                        var buffer = notify.pack as Data;
                        DataManager.GetInstance().Release(buffer);
                    }
                }
            }
        }

        public void Clear()
        {
            receiveQueues.Clear();
        }
    }
}
