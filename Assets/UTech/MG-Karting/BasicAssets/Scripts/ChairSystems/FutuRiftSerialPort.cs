using System;
using System.Collections.Generic;
using System.Linq;
using System.IO.Ports;
using System.Text;
using System.Threading.Tasks;


namespace KartGame.ChairSystems
{
    class FutuRiftSerialPort
    {
        private SerialPort port;
        public FutuRiftSerialPort(SerialPort port)
        {
            this.port = port;
        }
        public void Open() => port.Open();

        public void Control(float pitch, float roll)
        {
            var packet = new byte[]
            {
                33,
                12,
                (byte)Flag.OneBlock
            }
            .Concat(BitConverter.GetBytes(pitch))
            .Concat(BitConverter.GetBytes(-roll))
            .Concat(BitConverter.GetBytes(0f))
            .ToArray();
            var byteList = EncodePacket(packet).ToArray();
            port.Write(byteList, 0, byteList.Length);
        }

        public static FutuRiftSerialPort Default => new FutuRiftSerialPort(new SerialPort()
        {
            BaudRate = 115200,
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            ReadBufferSize = 4096,
            WriteBufferSize = 4096,
            ReadTimeout = 500,
            PortName = "COM8",
        });

        private IEnumerable<byte> EncodePacket(byte[] packet)
        {
            yield return MSG.SOM;
            yield return packet[0];
            yield return packet[1];
            yield return packet[2];
            var crc = BitConverter.GetBytes(FullCRC(packet, packet[1] + 3));
            foreach (var item in Clear(packet.Skip(3).Concat(new byte[] { crc[0], crc[1] })))
            {
                yield return item;
            }
            yield return MSG.EOM;
        }


        private IEnumerable<byte> Clear(IEnumerable<byte> source)
        {
            foreach (var b in source)
            {
                if (b >= MSG.ESC)
                {
                    yield return MSG.ESC;
                    yield return (byte)(b - MSG.ESC);
                }
                else
                    yield return b;
            }
        }

        private ushort FullCRC(byte[] p, int pSize)
        {
            ushort crc = 58005;
            for (int index = 0; index <= pSize - 1; ++index)
                crc = CRC16(crc, p[index]);
            return crc;
        }

        private ushort CRC16(ushort crc, byte b)
        {
            ushort num1 = (ushort)(byte.MaxValue & (crc >> 8 ^ b));
            ushort num2 = (ushort)(num1 ^ (uint)num1 >> 4);
            return (ushort)((crc ^ num2 << 4 ^ num2 >> 3) << 8 ^ (num2 ^ num2 << 5) & byte.MaxValue);
        }
    }
}
