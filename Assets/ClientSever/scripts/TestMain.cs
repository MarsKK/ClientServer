using System.Collections;
using System.Collections.Generic;
using ClientSever;
using UnityEngine;
using UnityEngine.UI;

public class TestMain : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //注册委托
        CSManager.GetInstance().SetSocketStateCallBack(SocketStatesCallBack);
        CSManager.GetInstance().SetReceiveMessageCallBack(MessageCallBack);

        GameObject.Find("Canvas/Button1").GetComponent<Button>().onClick.AddListener(()=>{
            CSManager.GetInstance().Connect("127.0.0.1", 13000);
        });
        GameObject.Find("Canvas/Button2").GetComponent<Button>().onClick.AddListener(()=>{
            CSManager.GetInstance().CloseSocket();
        });
        GameObject.Find("Canvas/Button3").GetComponent<Button>().onClick.AddListener(()=>{
            CSManager.GetInstance().StopConnect();
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SocketStatesCallBack(int code)
    {
        Debug.Log("SocketStatesCallBack:" + code);
        if (code == SocketType.SOCKET_CONNECT_SUCCESS)
        {
            var data = DataManager.GetInstance().Get();
            data.SetMainSub(1, 1);
            data.WriteString("123456", 256);
            data.WriteString("123456", 256);
            CSManager.GetInstance().SendSocketData(data);
        }
    }

    void MessageCallBack(int main, int sub, Data buffer)
    {
        Debug.Log(string.Format("MessageCallBack main:{0},sub:{1},Data len:{2}", main, sub, buffer.GetLen()));
    }
}
