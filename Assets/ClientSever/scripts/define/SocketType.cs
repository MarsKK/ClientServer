
namespace ClientSever 
{
    public class SocketType
    {
        public const int SOCKET_INVALID = 0;
        public const int SOCKET_CONNECT_SUCCESS = 1;
        public const int SOCKET_CONNECT_FAIL = 2;
        public const int SOCKET_CONNECT_TIMEOUT = 3;
        public const int SOCKET_CONNECT_CONNECTED = 4;
        public const int SOCKET_DISCONNECT_SUCCESS = 5;
    }

    public class SocketCmd
    {
        public const int SOCKET_MAIN_HEART = 1;
        public const int SOCKET_SUB_HEART = 0;
    }
}
