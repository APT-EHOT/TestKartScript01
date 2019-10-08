using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KartGame.ChairSystems
{
    public class FromSource
    {

        private SerialPort port; byte Flag_NextBlock = 0;
        private byte Flag_FirstBlock = 1;
        private byte Flag_LastBlock = 2;
        private byte Flag_OneBlock = 3;
        private byte Flag_ErrBlock = 4;
        private byte MSG_SOM = byte.MaxValue;
        private byte MSG_EOM = 254;
        private byte MSG_ESC = 253;
        private static int ProtocolType;
        private static string InPacket;
        private static string OutPacket;

        public FromSource(SerialPort port)
        {
            this.port = port;
        }

        private string HexStr(string packet, bool space)
        {
            return BitConverter.ToString(Encoding.Default.GetBytes(packet)).Replace("-", space ? " " : "");
        }

        private ushort CRC16(ushort crc, byte b)
        {
            ushort num1 = (ushort)((int)byte.MaxValue & ((int)crc >> 8 ^ (int)b));
            ushort num2 = (ushort)((uint)num1 ^ (uint)num1 >> 4);
            return (ushort)(((int)crc ^ (int)num2 << 4 ^ (int)num2 >> 3) << 8 ^ ((int)num2 ^ (int)num2 << 5) & (int)byte.MaxValue);
        }

        private ushort FullCRC(byte[] p, int pSize)
        {
            ushort crc = 58005;
            for (int index = 0; index <= pSize - 1; ++index)
                crc = this.CRC16(crc, p[index]);
            return crc;
        }

        private byte[] ObjectToByteArray(object obj)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize((Stream)memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        private byte Lo(ushort number)
        {
            return (byte)(number & byte.MaxValue);
        }

        private byte Hi(ushort number)
        {
            return (byte)((uint)number >> 8);
        }

        public static ushort LoWord(int dwValue)
        {
            return (ushort)(dwValue & (int)ushort.MaxValue);
        }

        public static ushort HiWord(int dwValue)
        {
            return (ushort)(dwValue >> 16 & (int)ushort.MaxValue);
        }

        public List<byte> EncodePacket(PacketDataType Src)
        {
            List<byte> byteList = new List<byte>();
            byteList.Add(this.MSG_SOM);
            byteList.Add(Src.b[0]);
            byteList.Add(Src.b[1]);
            byteList.Add(Src.b[2]);
            ushort number = this.FullCRC(Src.b, (int)Src.b[1] + 3);
            Src.b[(int)Src.b[1] + 3] = this.Lo(number);
            Src.b[(int)Src.b[1] + 4] = this.Hi(number);
            for (int index = 3; index <= 4 + (int)Src.b[1]; ++index)
            {
                if ((int)Src.b[index] >= (int)this.MSG_ESC)
                {
                    byteList.Add(this.MSG_ESC);
                    byteList.Add((byte)((uint)Src.b[index] - (uint)this.MSG_ESC));
                }
                else
                    byteList.Add(Src.b[index]);
            }
            byteList.Add(this.MSG_EOM);
            return byteList;
        }

        public int DecodePacket(byte[] src, int length, ref PacketDataType Dst)
        {
            byte[] numArray = new byte[length - 2];
            if ((int)src[0] == (int)this.MSG_SOM && (int)src[length - 1] == (int)this.MSG_EOM)
            {
                for (int index = 1; index < length - 1; ++index)
                    numArray[index - 1] = src[index];
                if (numArray.Length < 5)
                {
                    Console.WriteLine("Minimal len of string can not be less 5 bytes!");
                    return -2;
                }
                if (numArray.Length < 3 + (int)numArray[1])
                {
                    Console.WriteLine("Len of string can not be less 3 + packet_size!");
                    return -3;
                }
                Dst.b[0] = numArray[0];
                Dst.b[1] = numArray[1];
                Dst.b[2] = numArray[2];
                if (numArray.Length < 5 + (int)Dst.b[1])
                {
                    Console.WriteLine("Len of data is smaller then needed!");
                    return -4;
                }
                for (int index = 3; index <= 4 + (int)Dst.b[1]; ++index)
                    Dst.b[index] = numArray[index];
                ushort uint16 = BitConverter.ToUInt16(new byte[2]
                {
          numArray[(int) Dst.b[1] + 3],
          numArray[(int) Dst.b[1] + 4]
                }, 0);
                if ((int)this.FullCRC(this.ObjectToByteArray((object)Dst), (int)Dst.b[1] + 3) == (int)uint16)
                    ;
                return 0;
            }
            Console.WriteLine("First or last symbol of packet is incorrect!");
            return -1;
        }

        public void DevReset()
        {
            PacketDataType Src = new PacketDataType();
            Src.b[0] = (byte)0;
            Src.b[1] = (byte)1;
            Src.b[2] = this.Flag_OneBlock;
            Src.b[3] = (byte)0;
            List<byte> byteList = this.EncodePacket(Src);
            try
            {
                port.Write(byteList.ToArray(), 0, byteList.Count);
            }
            catch
            {
            }
        }

        public string GetID()
        {
            PacketDataType Dst = new PacketDataType();
            byte[] numArray = new byte[512];
            try
            {
                port.Write(new byte[8]
                {
          byte.MaxValue,
          (byte) 1,
          (byte) 1,
          (byte) 3,
          (byte) 0,
          (byte) 156,
          (byte) 238,
          (byte) 254
                }, 0, 8);
                Thread.Sleep(100);
                int length = port.Read(numArray, 0, 512);
                this.DecodePacket(numArray, length, ref Dst);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка получения данных.");
                return "";
            }
            return BitConverter.ToString(Dst.b, 0, 12).Replace("-", string.Empty);
        }

        public string DevInfo()
        {
            PacketDataType Dst = new PacketDataType();
            byte[] numArray = new byte[512];
            port.Write(new byte[8]
            {
        byte.MaxValue,
        (byte) 1,
        (byte) 1,
        (byte) 3,
        (byte) 0,
        (byte) 156,
        (byte) 238,
        (byte) 254
            }, 0, 8);
            Thread.Sleep(100);
            int length = port.Read(numArray, 0, 512);
            this.DecodePacket(numArray, length, ref Dst);
            return Encoding.ASCII.GetString(Dst.b, 0, 12);
        }

        public void DevControlF(float pitch, float roll)
        {
            PacketDataType Src = new PacketDataType();
            Src.b[0] = (byte)33;
            Src.b[1] = (byte)12;
            Src.b[2] = this.Flag_OneBlock;
            byte[] bytes1 = BitConverter.GetBytes(pitch);
            Src.b[3] = bytes1[0];
            Src.b[4] = bytes1[1];
            Src.b[5] = bytes1[2];
            Src.b[6] = bytes1[3];
            roll = -roll;
            byte[] bytes2 = BitConverter.GetBytes(roll);
            Src.b[7] = bytes2[0];
            Src.b[8] = bytes2[1];
            Src.b[9] = bytes2[2];
            Src.b[10] = bytes2[3];
            Src.b[11] = (byte)0;
            Src.b[12] = (byte)0;
            Src.b[13] = (byte)0;
            Src.b[14] = (byte)0;
            string newLine = Environment.NewLine;
            List<byte> byteList = this.EncodePacket(Src);
            try
            {
                port.Write(byteList.ToArray(), 0, byteList.Count);
            }
            catch
            {
            }
        }

        public void DevControlA(ushort a, ushort b, ushort c, ushort d, ushort e, ushort f)
        {
            PacketDataType Src = new PacketDataType();
            Src.b[0] = (byte)33;
            Src.b[1] = (byte)15;
            Src.b[2] = this.Flag_OneBlock;
            Src.b[3] = (byte)0;
            Src.b[4] = (byte)0;
            Src.b[5] = (byte)20;
            Src.b[6] = this.Lo(a);
            Src.b[7] = this.Hi(a);
            Src.b[8] = this.Lo(b);
            Src.b[9] = this.Hi(b);
            Src.b[10] = this.Lo(c);
            Src.b[11] = this.Hi(c);
            Src.b[12] = this.Lo(d);
            Src.b[13] = this.Hi(d);
            Src.b[14] = this.Lo(e);
            Src.b[15] = this.Hi(e);
            Src.b[16] = this.Lo(f);
            Src.b[17] = this.Hi(f);
            List<byte> byteList = this.EncodePacket(Src);
            try
            {
                port.Write(byteList.ToArray(), 0, byteList.Count);
            }
            catch
            {
            }
        }

        public void MiniRele(byte Ch, byte MRState)
        {
            PacketDataType Src = new PacketDataType();
            Src.b[0] = (byte)35;
            Src.b[1] = (byte)2;
            Src.b[2] = this.Flag_OneBlock;
            Src.b[3] = Ch;
            Src.b[4] = MRState;
            List<byte> byteList = this.EncodePacket(Src);
            try
            {
                port.Write(byteList.ToArray(), 0, byteList.Count);
            }
            catch
            {
            }
        }

        private class Packet_01_In
        {
            private uint[] PID = new uint[2];
        }

        private class Packet_03_In
        {
            private byte ErrCode;
            private ushort DevType;
            private ushort Ver;
            private ushort Rev;
        }

        private class Packet_05_In
        {
            private uint ErrCode;
            private uint Spec;
        }

        private class Packet_20_In
        {
            private byte ErrCode;
            private byte Inputs;
            private float AX;
            private float AY;
            private float AZ;
        }

        private class Packet_21_Out
        {
            private float X;
            private float Y;
            private float Z;
        }

        private class Packet_22_Out
        {
            private int d;
        }

        [Serializable]
        public class PacketDataType
        {
            public byte[] b = new byte[73];
            public string s;
        }
    }
}
