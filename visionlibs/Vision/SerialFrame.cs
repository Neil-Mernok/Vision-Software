using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace SerialFraming
{
    public class SerialFrame
    {
        const byte SOF1 = 0x7E;
        const byte SOF2 = 0xE7;
        const byte EOF1 = 0x0A;

        const byte SOF_RF_1 = 0x3A;

        public static bool getMessage(ref SerialPort P, out Byte[] data)
        {
            data = new Byte[0];
            var byteList = new List<byte>();
            Byte[] B;
            int check;
            Stopwatch S = new Stopwatch();
            S.Start();
            
            try
            {
                int first = 0;
                do
                {
                    first = P.ReadByte();       // wait for first header byte
                } while (first != SOF1);
                if ((check = P.ReadByte()) != SOF2)       // second header byte
                    return false;

                S.Restart();
                byte len = (byte)P.ReadByte();  // length of the message
                B = new Byte[len+3];
                while(P.BytesToRead < len+3)
                {
                    if (S.ElapsedMilliseconds > 500)
                        return false;
                }
                P.Read(B, 0, len+3);            // read the data of the frame, as well as CRC and EOF. 

                data = new byte[len];
                Buffer.BlockCopy(B, 0, data, 0, len);

                UInt16 crc_in = BitConverter.ToUInt16(B, len);
                Crc16Ccitt CRC16 = new Crc16Ccitt(InitialCrcValue.NonZero1);
                if ((crc_in == CRC16.ComputeChecksum(data)) && (B[len+2] == EOF1))
                {
                    return true;
                }
                return false;
            }
            catch (System.TimeoutException ex)
            {
                // todo: see if its necessary to flsuh the serialport here...
                Console.WriteLine(ex.Message);
                return false;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public static byte[] getFrame(byte[] frame_bytes)
        {
            Crc16Ccitt Frame_CRC16 = new Crc16Ccitt(InitialCrcValue.NonZero1);
            byte[] crc = Frame_CRC16.ComputeChecksumBytes(frame_bytes);  

            int message_length = frame_bytes.Length;
            byte[] framed_bytes = new byte[message_length + 6];

            framed_bytes[0] = SOF1;
            framed_bytes[1] = SOF2;
            framed_bytes[2] = (byte)message_length;
            Array.Copy(frame_bytes, 0, framed_bytes, 3, message_length);
            framed_bytes[message_length + 3] = crc[0];
            framed_bytes[message_length + 4] = crc[1];
            framed_bytes[message_length + 5] = EOF1;

            return framed_bytes;
        }

        public static byte[] get_RF_Frame(byte[] frame_bytes)
        {
            Crc16Ccitt Frame_CRC16 = new Crc16Ccitt(InitialCrcValue.NonZero1);
            byte[] crc = Frame_CRC16.ComputeChecksumBytes(frame_bytes);

            int message_length = frame_bytes.Length;
            byte[] framed_bytes = new byte[message_length + 4];

            framed_bytes[0] = SOF_RF_1;
            framed_bytes[1] = (byte)message_length;
            Array.Copy(frame_bytes, 0, framed_bytes, 2, message_length);
            framed_bytes[message_length + 2] = crc[0];
            framed_bytes[message_length + 3] = crc[1];

            return framed_bytes;
        }
    }

public enum InitialCrcValue { Zeros, NonZero1 = 0xffff, NonZero2 = 0x1D0F }

public class Crc16Ccitt
{
    const ushort poly = 0x1021;
    ushort[] table = new ushort[256];
    ushort initialValue = 0;

    public ushort ComputeChecksum(byte[] bytes)
    {
        ushort crc = this.initialValue;
        for (int i = 0; i < bytes.Length; ++i)
        {
            crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
        }
        return crc;
    }

    public byte[] ComputeChecksumBytes(byte[] bytes)
    {
        ushort crc = ComputeChecksum(bytes);
        return BitConverter.GetBytes(crc);
    }

    public Crc16Ccitt(InitialCrcValue initialValue)
    {
        this.initialValue = (ushort)initialValue;
        ushort temp, a;
        for (int i = 0; i < table.Length; ++i)
        {
            temp = 0;
            a = (ushort)(i << 8);
            for (int j = 0; j < 8; ++j)
            {
                if (((temp ^ a) & 0x8000) != 0)
                {
                    temp = (ushort)((temp << 1) ^ poly);
                }
                else
                {
                    temp <<= 1;
                }
                a <<= 1;
            }
            table[i] = temp;
        }
    }
}
}