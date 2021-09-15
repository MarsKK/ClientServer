using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ClientSever
{
    class Define {
        public static int RECYCLE_MAX_LEN = 1024;
    }

    public class Data {
        private MemoryStream ms;
        private int offset = 0;
        public int recycleMaxLen = 1024;
        public int main = 0;
        public int sub = 0;
        public int len = 0;

        public Data()
        {
            ms = new MemoryStream();
        }

        ~Data()
        {
            ms.Close();
        }

        public void SetLen(int len)
        {
            this.len = len;
        }

        public int GetLen()
        {
            return len;
        }

        public int GetMain()
        {
            return main;
        }

        public int GetSub()
        {
            return sub;
        }

        public void ReadMainSub()
        {
            main = ReadInt16();
            sub = ReadInt16();
        }

        public void SetMainSub(int main, int sub)
        {
            this.main = main;
            this.sub = sub;
            WriteInt16((short)main);
            WriteInt16((short)sub);
        }

        public void SetOffset(int offset)
        {
            this.offset = offset;
        }

        public MemoryStream GetMemoryStream()
        {
            return ms;
        }

        public void Clear()
        {
            var bytes = ms.GetBuffer();
            var bufferLen = bytes.Length;
            if (bufferLen >= Define.RECYCLE_MAX_LEN) //内存缓冲区达到最大临界值，回收内存。
            {
                ms.Close();
                ms = new MemoryStream();
                Debug.Log("释放内存:"+bufferLen+"字节.");
            }
            else
            {
                for (int i = 0; i < bufferLen; i++)
                {
                    bytes[i] = 0;
                }
            }
            ms.Seek(0, SeekOrigin.Begin);
            offset = 0;
            len = 0;
        }

        public byte[] GetBytes()
        {
            var bytes = ms.GetBuffer();
            return bytes.Skip(0).Take(offset).ToArray();
        }

        public int GetBytesLen(byte[] bytes)
        {
            int len = bytes.Length;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0)
                {
                    len = i;
                    break;
                }
            }
            return len;
        }

        #region   读数据包
        public void Read(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return;
            }
            ms.Write(bytes, 0, bytes.Length);
            offset = 0;
        }

        private byte[] Read(int len)
        {
            var bytes = ms.GetBuffer();
            var buffer = new byte[len];
            Array.Copy(bytes, offset, buffer, 0, len);
            offset += len; 
            return buffer;
        }

        public byte ReadByte()
        {
            var buffer = Read(1);
            return buffer[0];
        }

        public int ReadInt()
        {
            var buffer = Read(4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public Int16 ReadInt16()
        {
            var buffer = Read(2);
            return BitConverter.ToInt16(buffer, 0); 
        }

        public float ReadFloat()
        {
            var buffer = Read(4);
            return BitConverter.ToSingle(buffer, 0);
        }

        public double ReadDouble()
        {
            var buffer = Read(8);
            return BitConverter.ToDouble(buffer, 0);
        }

        public string ReadString(int len)
        {
            var buffer = Read(len);
            len = GetBytesLen(buffer);
            return Encoding.UTF8.GetString(buffer, 0, len);
        }
        #endregion

        #region   写入数据包
        public void WriteBytes(byte[] bytes)
        {
            if (bytes.Length == 0)
            {
                return;
            }
            var msBytes = ms.GetBuffer();
            var len = msBytes.Length;
            var reLen = len - offset;
            if (reLen < bytes.Length)
            {
                Array.Resize<byte>(ref msBytes, len+(bytes.Length-reLen));
            }
            Buffer.BlockCopy(bytes, 0, msBytes, offset, bytes.Length);
            ms.Write(msBytes, 0, msBytes.Length);
            offset += bytes.Length;
            len += bytes.Length;
        }

        public void WriteByte(byte b)  
        {
            WriteBytes(new byte[1]{b});
        }

        public void WriteInt(int i)
        {
            var bytes = BitConverter.GetBytes(i);
            WriteBytes(bytes);
        }

        public void WriteInt16(Int16 i)
        {
            var bytes = BitConverter.GetBytes(i);
            WriteBytes(bytes);
        }
        
        public void WriteFloat(float f)
        {
            var bytes = BitConverter.GetBytes(f);
            WriteBytes(bytes);
        }

        public void WriteDouble(double d)
        {
            var bytes = BitConverter.GetBytes(d);
            WriteBytes(bytes);
        }

        public void WriteString(string s, int len)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            var strBytes = new byte[len];
            Array.Copy(bytes, 0, strBytes, 0, bytes.Length < len ? bytes.Length : len);
            WriteBytes(strBytes);
        }
        #endregion
    }
}