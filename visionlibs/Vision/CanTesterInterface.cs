using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Text;
using USB_PORTS;
using System.Diagnostics;

namespace CANTESTER
{
    public class Can_Message
    {
        public UInt32 CAN_ID;
        public Boolean IsExtID;
        public Byte DataLength;
        public Byte[] Data;

        #region Constructor
        public Can_Message(Byte[] data_in, int offset)
        {
            int res = Parse_Message(data_in, offset);
            if(res == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public Can_Message(Byte[] data, Boolean isExtCAN_ID, UInt32 CanID)
        {
            DataLength = (Byte)Math.Min(data.Length, 8);
            IsExtID = isExtCAN_ID;

            Data = new Byte[DataLength];
            Buffer.BlockCopy(data, 0, Data, 0, DataLength);
            CAN_ID = CanID;
        }

        public Can_Message(Byte[] data, int offset, int len, Boolean isExtCAN_ID, UInt32 CanID)
        {
            DataLength = (Byte)Math.Min(len, 8);
            IsExtID = isExtCAN_ID;

            Data = new Byte[DataLength];
            Buffer.BlockCopy(data, offset, Data, 0, DataLength);
            CAN_ID = CanID;
        }

        #endregion

#region methods

        public int Parse_Message(Byte[] data_in, int offset)
        {
            if(data_in != null)
            {
                if (data_in.Length >= (offset+20))
                {
                    if(data_in[8] == 4)
                    {
                        IsExtID = true; 
                        CAN_ID = BitConverter.ToUInt32(data_in, 4);
                    }
                    else
                    {
                        IsExtID = false;
                        CAN_ID = BitConverter.ToUInt32(data_in, 0);
                    }
                    DataLength = Math.Min(data_in[10], (Byte)8);

                    Data = new byte[DataLength];
                    Buffer.BlockCopy(data_in, 11, Data, 0, DataLength);
                    return DataLength+1;
                }
                return 0;
            }
            else
                return 0;
        }

        public Byte[] get_Bytes()
        {
            Byte[] data = new Byte[20];
           
            if(IsExtID)
            {
                BitConverter.GetBytes(CAN_ID).CopyTo(data, 4);
                data[8] = 4;
            }
            else
            {
                BitConverter.GetBytes(CAN_ID).CopyTo(data, 0);
                data[8] = 0;
            }

            data[10] = DataLength;
            Data.CopyTo(data, 11);
            return data;
        }


#endregion

    }
    
    public class CanTesterInterface
    {
        public const UInt32 CAN_ID_Vision_EndMask       = 0x00000800;
        public const UInt32 CAN_ID_Vision_NotLast       = 0x00000800;

        private const string VID = "0403", PID = "7E42";

        public static Boolean IsCantester(string COM_name)
        {
            List<string> ports = PortFromVIDPID.ComPortNames(VID, PID);
            return ports.Contains(COM_name);
        }

        public static bool getCanMessage(ref SerialPort P, out Byte[] data, UInt32 ID)
        {
            int bytes_rec = 0;
            data = new Byte[1];
            var byteList = new List<byte>();
            Can_Message C;
            Byte[] B;

            try
            {
                do
                {
                    B = new Byte[20];
                    while (P.BytesToRead < 20)
                    {
                        if (P == null)
                            return false;
                        else if (P.IsOpen == false)
                            return false;
                    }
                    P.Read(B, 0, 20);
                    C = new Can_Message(B, 0);
                    //if ((C.CAN_ID & CAN_ID_Vision_EndMask) == CAN_ID_Vision_EndMask)
                    //{
                        byteList.AddRange(C.Data);
                        bytes_rec += C.DataLength;
                    //}
                } while ((C.CAN_ID & CAN_ID_Vision_NotLast) == CAN_ID_Vision_NotLast);
                //data = new Byte[bytes_rec];
                data = byteList.ToArray();
                return true;
            }
            catch (System.TimeoutException ex)
            {
                Console.WriteLine("GetCANMessage catch CANTesterInterface");
                System.Windows.MessageBox.Show("Error" + ex.ToString());
                return false;
            }
        }

        public static bool getCanMessage(ref SerialPort P, out Byte[] data)
        {
            int bytes_rec = 0;
            data = new Byte[1];
            var byteList = new List<byte>();
            Can_Message C;
            Byte[] B;
            Stopwatch S = Stopwatch.StartNew();
            uint Temp_CAN_ID = 0;
            try
            {
                do
                {
                    B = new Byte[20];
                    while (P.BytesToRead < 20)
                    {
                        Thread.Sleep(1);
                        if (P == null)
                            return false;
                        else if (P.IsOpen == false)
                            return false;
                        else if (S.ElapsedMilliseconds > 500)
                        {
                            P.DiscardInBuffer();
                            return false;
                        }
                    }
                    P.Read(B, 0, 20);
                    C = new Can_Message(B, 0);
                    if ((Temp_CAN_ID == 0)||((Temp_CAN_ID & CAN_ID_Vision_EndMask) == CAN_ID_Vision_EndMask))
                    {
                        Temp_CAN_ID = C.CAN_ID;
                    }
                    if ((C.CAN_ID == Temp_CAN_ID) && (C.CAN_ID != 0x12227300) && ((C.CAN_ID & 0x12227000) == 0x12227000))
                    {
                        byteList.AddRange(C.Data);
                        bytes_rec += C.DataLength;
                    }
                } while ((C.CAN_ID & CAN_ID_Vision_EndMask) == CAN_ID_Vision_EndMask);
                if ((bytes_rec > 0) && (bytes_rec < 80))
                {
                    data = new Byte[bytes_rec];
                    data = byteList.ToArray();
                    Console.WriteLine("Recieving: " + Convert.ToChar(data[0]) + " " + data.Length);
                    return true;
                }
                else
                {
                    data[0] =  0;
                    return false;
                }            
               
            }
            catch (System.TimeoutException ex)
            {
                Console.WriteLine("GetCANMessage catch CANTesterInterface");
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public static Boolean sendCanMessage(ref SerialPort P, Byte[] data, UInt32 ID)
        {
            int bytes_to_send = data.Length;
            int bytes_sent = 0;
            Can_Message C;
            
            if(P.IsOpen)
            {
                while(bytes_to_send > 0)
                {
                    if (bytes_to_send > 8)
                        C = new Can_Message(data, bytes_sent, 8, true, ID | CAN_ID_Vision_NotLast);
                    else
                        C = new Can_Message(data, bytes_sent, bytes_to_send, true, ID);

                    bytes_to_send -= C.DataLength;
                    bytes_to_send = Math.Max(bytes_to_send, 0);

                    while (P.BytesToWrite != 0) ;
                    P.Write(C.get_Bytes(), 0, 20);
                    bytes_sent += C.DataLength;
                    Thread.Sleep(1);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Boolean sendCanMessage(ref SerialPort P, Byte[] data, int offset, int count, UInt32 ID)
        {
            int bytes_to_send = count;
            int bytes_sent = 0;
            Can_Message C;

            if (P.IsOpen)
            {
                while (bytes_to_send > 0)
                {
                    if (bytes_to_send > 8)
                        C = new Can_Message(data, offset + bytes_sent, 8, true, ID | CAN_ID_Vision_NotLast);
                    else
                        C = new Can_Message(data, offset + bytes_sent, bytes_to_send, true, ID);

                    bytes_to_send -= C.DataLength;
                    bytes_to_send = Math.Max(bytes_to_send, 0);

                    while (P.BytesToWrite != 0) ;
                    P.Write(C.get_Bytes(), 0, 20);
                    bytes_sent += C.DataLength;
                    Thread.Sleep(1);
                }
                //Thread.Sleep(1);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
