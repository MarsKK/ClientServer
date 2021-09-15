using UnityEngine;

namespace ClientSever 
{
    public class CSManager : MonoBehaviour
    {
        //连接列表
        //private List<ClientSocket> sockets = new List<ClientSocket>();
        //连接
        ClientSocket socket = new ClientSocket();
        //循环通知
        private LoopNotify loopNotify = new LoopNotify();
        //CSManager单例
        protected static CSManager instance;
        //连接状态回调
        public delegate void SocketStatesCallBack(int code);
        //消息回调
        public delegate void ReceiveMessageCallBack(int main, int sub, Data buffer);
        public SocketStatesCallBack socketStatesCallBack;
        public ReceiveMessageCallBack receiveMessageCallBack;

        public static CSManager GetInstance()
        {
            if (instance == null)
            {
                instance = FindObjectOfType<CSManager>();
                var len = FindObjectsOfType<CSManager>().Length;
                if (len > 1)
                {
                    Debug.LogWarning("Scene have more than 1 CSManager!");
                }
                if (instance == null)
                {
                    var go = new GameObject(typeof(CSManager).Name);
                    instance = go.AddComponent<CSManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }

        //设置上层回调
        public void SetSocketStateCallBack(SocketStatesCallBack socketStatesCallBack)
        {
            this.socketStatesCallBack = socketStatesCallBack;
        }

        //设置上层回调
        public void SetReceiveMessageCallBack(ReceiveMessageCallBack receiveMessageCallBack)
        {
            this.receiveMessageCallBack = receiveMessageCallBack;
        }

        //建立连接
        public void Connect(string address, int port)
        {
            socket.Connect(address, port);
        }

        //关闭正在连接
        public void StopConnect()
        {
            socket.StopConnect();
        }

        //关闭连接
        public void CloseSocket()
        {
            loopNotify.Clear();
            socket.Close();
        }

        //加入通知队列
        public void AddNotfiyToClient(int notfiyType, int subType, System.Object pack = null)
        {
            SocketNotify notify = new SocketNotify();
            notify.notfiyType = notfiyType;
            notify.subType = subType;
            notify.pack = pack;
            loopNotify.EnqueueReceiveNotify(notify);
        }

        //处理从服务端接收的消息
        private void ReceiveHandler(SocketNotify notify)
        {
            int notfiyType = notify.notfiyType;
            switch (notfiyType)
            {
                case NotfiyType.NOTFIY_SOCKET_STATES:
                    socketStatesCallBack?.Invoke(notify.subType);
                    break;
                case NotfiyType.NOTFIY_SOCKET_RECEIVE:
                    var buffer = notify.pack as Data;
                    receiveMessageCallBack?.Invoke(buffer.GetMain(), buffer.GetSub(), buffer);
                    break;
                default:
                    Debug.LogWarning("Not define Notfiy");
                    break;
            }
        }

        //发送数据
        public void SendSocketData(Data dataBuffer)
        {
            socket.SendScoketData(dataBuffer);
        }

        private void Awake() {
            instance = this;
            //绑定通知委托
            loopNotify.SetReceiveWork(ReceiveHandler);
            DontDestroyOnLoad(gameObject);
        }

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if (loopNotify != null)
            {
                loopNotify.LoopReceiveNotify();
            }
        }

        private void OnDestroy() {
            CSManager.GetInstance().CloseSocket();
            instance = null;
            loopNotify = null;
        }
    }
}
