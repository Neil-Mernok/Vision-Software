using System;
using System.Collections.Generic;
using USB_PORTS;
using System.IO.Ports;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Text;

namespace RFID
{
    public enum Mifare_key
    {
        A = 0, B = 0x80
    };

    public enum Mode
    {
        Mifare = 0, icode = 3
    };


    public static class MernokRFID
    {
        static string VID = "0403", PID = "7E40";
        static SerialPort SP = new SerialPort();
        static bool porterror = false;
        #region Fields



        #endregion

        //#region Constructors
        //public MernokRFID()
        //{
        //}

        //#endregion


        #region Functions

        public static bool IsOpen()
        {
            List<string> ports = PortFromVIDPID.ComPortNames(VID, PID);
            if (ports.Count < 1)
                return false;
            else
            {
                if (porterror == false)
                    return SP.IsOpen;
                else
                {
                    porterror = false;
                    return false;
                }
            }
        }

        public static bool OpenRFID(Mode RFID_Mode)
        {
            List<string> ports = PortFromVIDPID.ComPortNames(VID, PID);
            if (ports.Count < 1)
                return false;               // no ports with the details specified, so fail
            else
            {
                foreach (string s in ports)
                {
                    try
                    {
                        if (SP.IsOpen) SP.Close();

                        SP.PortName = s;
                        if (SP.IsOpen != true)
                        {
                            SP.Open();
                            Thread.Sleep(10);
                            if (SP.IsOpen)
                            {
                                if (MernokRFID_interface.Mifair_ICode_Mode(RFID_Mode))
                                {
                                    byte[] retdata;
                                    byte[] command = new byte[] { 80, 0, 0 };                       //Send polling interval to 0: P', 0, 0                                                     // Tag answer OK
                                    if (MernokRFID.SendRec(out retdata, command))
                                        return true;
                                }

                                SP.Close();
                                return false;
                            }
                            else
                                return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Open RFID catch RFID Interface");
                        Console.WriteLine(ex.Message);
                        return false;
                    }
                }
            }
            return false;
        }

        public static void CloseRFID()
        {
            SP.Close();
        }

        public static bool SendRec(out byte[] ReturnData, byte[] SendData)
        {
            const int timeout = 500;
            Stopwatch StopW = new Stopwatch();

            StopW.Restart();

            if (IsOpen())
            {
                SP.DiscardInBuffer();
                SP.Write(SendData, 0, SendData.Length);
                StopW.Restart();
                while (SP.BytesToRead < 1)                  // wait until daat arrives, but timeout eventually
                {
                    if (StopW.ElapsedMilliseconds > timeout)
                    {
                        ReturnData = new byte[0];
                        return false;
                    }
                }

                /// assume we have data by now
                ReturnData = new byte[SP.BytesToRead];
                SP.Read(ReturnData, 0, ReturnData.Length);
                return true;
            }

            ReturnData = new byte[0];
            return false;
        }

        public static bool SendRec(out byte[] ReturnData, string SendData)
        {
            const int timeout = 500;
            Stopwatch StopW = new Stopwatch();

            StopW.Restart();

            if (IsOpen())
            {
                try
                {
                    SP.DiscardInBuffer();
                    SP.Write(SendData);
                    StopW.Restart();
                    while (SP.BytesToRead < 1)                  // wait until daat arrives, but timeout eventually
                    {
                        if (StopW.ElapsedMilliseconds > timeout)
                        {
                            ReturnData = new byte[0];
                            return false;
                        }
                    }

                    /// assume we have data by now
                    ReturnData = new byte[SP.BytesToRead];
                    SP.Read(ReturnData, 0, ReturnData.Length);
                    return true;
                }
                catch (IOException ex)
                {
                    porterror = true;
                    ReturnData = new byte[0];
                    Console.WriteLine("SendRec catch RFID interface");
                    System.Windows.MessageBox.Show("Error:" + ex.ToString());
                    return false;
                }
            }

            ReturnData = new byte[0];
            return false;
        }
        #endregion
    }


    public static class MernokRFID_interface
    {
        /// <summary>
        /// Gets the Tag UID of the tag in field
        /// </summary>
        /// <returns> Uint32 number representing the UID. 0 if error occurred</returns>
        public static UInt32 read_UID()
        {
            byte[] retdata;


            if (MernokRFID.IsOpen())
            {
                // send UID command
                if (MernokRFID.SendRec(out retdata, "U"))
                {
                    if ((retdata.Length == 9) && (retdata[0] == 0x86))                                                      // Tag answer OK
                        return BitConverter.ToUInt32(retdata, 1);
                    else
                        return 0;
                }
                else
                    return 0;
            }
            else
            {
                MernokRFID.OpenRFID(Mode.icode);
                if (MernokRFID.IsOpen())
                    return read_UID();
                else
                    return 0;
            }
        }



        /// <summary>
        /// Read the bock at the specified address
        /// </summary>
        /// <returns> Bool to indicate success</returns>
        public static Boolean read_block(UInt16 block_num, out Byte[] BR)
        {
            byte[] retdata;
            BR = new byte[4];

            if (MernokRFID.IsOpen())
            {
                // send Read command
                byte[] command = new byte[] { 82, 0, 0 };
                command[1] = (byte)(block_num & 0xFF);
                command[2] = (byte)(block_num >> 8 & 0xFF);

                if (MernokRFID.SendRec(out retdata, command))
                {
                    if ((retdata.Length == 5) && (retdata[0] == 0x86))                                                      // Tag answer OK
                    {
                        BR[0] = retdata[1];
                        BR[1] = retdata[2];
                        BR[2] = retdata[3];
                        BR[3] = retdata[4];
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {
                MernokRFID.OpenRFID(Mode.icode);
                if (MernokRFID.IsOpen())
                    return read_block(block_num, out BR);
                else
                    return false;
            }
        }

        /// <summary>
        /// Read the bock at the specified address, and place into destination array at specified offset
        /// </summary>
        /// <returns> Bool to indicate success</returns>
        public static Boolean read_block(UInt16 block_num, ref Byte[] BR, int offset)
        {
            byte[] retdata;

            if (MernokRFID.IsOpen())
            {
                // send Read command
                byte[] command = new byte[] { 82, 0, 0 };
                command[1] = (byte)(block_num & 0xFF);
                command[2] = (byte)(block_num >> 8 & 0xFF);

                if (MernokRFID.SendRec(out retdata, command))
                {
                    if ((retdata.Length == 5) && (retdata[0] == 0x86))                                                      // Tag answer OK
                    {
                        BR[offset + 0] = retdata[1];
                        BR[offset + 1] = retdata[2];
                        BR[offset + 2] = retdata[3];
                        BR[offset + 3] = retdata[4];
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {
                MernokRFID.OpenRFID(Mode.icode);
                if (MernokRFID.IsOpen())
                    return read_block(block_num, out BR);
                else
                    return false;
            }
        }


        /// <summary>
        /// Write the bock at the specified address, with data in the byte arry at the specified offset
        /// </summary>
        /// <returns> bool to indicate success</returns>
        public static Boolean write_block(UInt16 block_num, Byte[] data, UInt16 offset)
        {
            Byte[] BR = new Byte[7] { 87, 0, 0, 0, 0, 0, 0 };
            byte[] retdata;

            if (MernokRFID.IsOpen())
            {
                // send Write command
                BR[1] = (byte)(block_num & 0xFF);
                BR[2] = (byte)(block_num >> 8 & 0xFF);

                int len = Math.Min(4, data.Length - offset);
                // copy as many bytes as possible. 
                Array.Copy(data, offset, BR, 3, len);

                if (MernokRFID.SendRec(out retdata, BR))
                {
                    if ((retdata.Length == 1) && (retdata[0] == 0x86))                                                      // Tag answer OK
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {
                MernokRFID.OpenRFID(Mode.icode);
                if (MernokRFID.IsOpen())
                    return write_block(block_num, data, offset);
                else
                    return false;
            }
        }

        /// <summary>
        /// Send a command instructing the reader to go to either Mifare or Icode mode 
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static Boolean Mifair_ICode_Mode(Mode mode)
        {
            byte[] retdata;
            byte[] command = new byte[] { (byte)'P', 3, (byte)mode };                    //Send command to put in Mifare / Icode mode                                 ////////byte[] command = new byte[] { 81, 3 };                    //Read mifare/icode mode: 'Q', 3, 3

            return MernokRFID.SendRec(out retdata, command);
        }

        /// <summary>
        /// Write the block to a Mifare card at the specified address
        /// </summary>
        /// <returns> bool to indicate success</returns>
        public static Boolean Mifare_Write_Block(Mifare_key KeyAB, int key_num, int block_num, Byte[] data)
        {
            Byte[] BR = new Byte[20];
            byte[] retdata;

            if (MernokRFID.IsOpen())
            {
                // send Write command
                BR[0] = (byte)'W';
                BR[1] = (byte)(block_num);
                BR[2] = (byte)((int)KeyAB | key_num);

                int len = Math.Min(16, data.Length);
                // copy as many bytes as possible. 
                Array.Copy(data, 0, BR, 3, len);

                if (MernokRFID.SendRec(out retdata, BR))
                {
                    if ((retdata.Length == 1) && ((retdata[0] == 0x86) || (retdata[0] == 0x96)))                                                      // Tag answer OK
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {
                MernokRFID.OpenRFID(Mode.Mifare);
                if (MernokRFID.IsOpen())
                    return Mifare_Write_Block(KeyAB, key_num, block_num, data);
                else
                    return false;
            }
        }

        public static Boolean MiFair_Store_Access_Key(byte key_number, byte[] key)
        {
            Byte[] BR = new Byte[8];
            byte[] retdata;

            if (MernokRFID.IsOpen())
            {
                // send Write command
                BR[0] = (byte)'K';
                BR[1] = key_number;

                int len = Math.Min(6, key.Length);
                // copy as many bytes as possible. 
                Array.Copy(key, 0, BR, 2, len);

                if (MernokRFID.SendRec(out retdata, BR))
                {
                    if ((retdata.Length == 1) && ((retdata[0] == 0x86) || (retdata[0] == 0x96)))                                                      // Tag answer OK
                    {
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {
                MernokRFID.OpenRFID(Mode.Mifare);
                if (MernokRFID.IsOpen())
                    return MiFair_Store_Access_Key(key_number, key);
                else
                    return false;
            }
        }
        public static Boolean MiFair_Store_Access_Key(byte key_number, string key)
        {
            return MiFair_Store_Access_Key(key_number, Encoding.ASCII.GetBytes(key));
        }

        /// <summary>
		/// Read the bock at the specified address
		/// </summary>
		/// <returns> Bool to indicate success</returns>
		public static Boolean Mifair_Read_Block(Mifare_key KeyAB, int key_num, int block_num, out Byte[] BR)
        {
            byte[] retdata;
            BR = new byte[0];

            if (MernokRFID.IsOpen())
            {
                // send Read command
                if (KeyAB == Mifare_key.B) key_num |= 0x80;
                byte[] command = new byte[] { (byte)'R', (byte)block_num, (byte)key_num };

                if (MernokRFID.SendRec(out retdata, command))
                {
                    if ((retdata.Length == 17) && ((retdata[0] == 0x86) || (retdata[0] == 0x96)))                                                      // Tag answer OK
                    {
                        BR = new byte[16];
                        Array.Copy(retdata, 1, BR, 0, 16);
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {
                MernokRFID.OpenRFID(Mode.Mifare);
                if (MernokRFID.IsOpen())
                    return Mifair_Read_Block(KeyAB, key_num, block_num, out BR);
                else
                    return false;
            }
        }

        /// <summary>
        /// Read the bock at the specified address, and place into destination array at specified offset
        /// </summary>
        /// <returns> Bool to indicate success</returns>
        public static Boolean Mifair_Read_Block(Mifare_key KeyAB, int key_num, int block_num, ref Byte[] BR, int offset)
        {
            byte[] retdata;

            if (MernokRFID.IsOpen())
            {
                // send Read command
                if (KeyAB == Mifare_key.B) key_num |= 0x80;
                byte[] command = new byte[] { (byte)'R', (byte)block_num, (byte)key_num };

                if (MernokRFID.SendRec(out retdata, command))
                {
                    if ((retdata.Length == 17) && ((retdata[0] == 0x86) || (retdata[0] == 0x96)))                                                      // Tag answer OK
                    {
                        Array.Copy(retdata, 1, BR, offset, 16);
                        return true;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }
            else
            {
                MernokRFID.OpenRFID(Mode.Mifare);
                if (MernokRFID.IsOpen())
                    return Mifair_Read_Block(KeyAB, key_num, block_num, ref BR, offset);
                else
                    return false;
            }
        }
    }
}
