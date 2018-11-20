using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Input;
using CANTESTER;
using Vision_Bootloader;
using SerialFraming;
using System.Globalization;

namespace vision_boot
{
	public class Vision_Booter
    {
        #region Constants
        public enum boot_default
        {
            none = 0,
            mantag,
            module,
        };

        private const UInt32 CAN_ID_Vision_Poll = 0x12227100;
        private const UInt32 CAN_ID_Vision_Resp = 0x12227200;
        private const UInt32 CAN_ID_Vision_Sync = 0x12227300;
        private const UInt32 CAN_ID_VisionBootResp = 0x12227500;
        private const UInt32 CAN_ID_VisionBootPoll = 0x12227400;
        private const UInt32 CAN_ID_VisionNotEndMask = 0xFFFFF7FF;
        //public const UInt32 CAN_ID_VisionIs_EndMask     = 0x00000800;
        public const UInt32 CAN_ID_Vision_NotLast = 0x00000800;
        #endregion

        #region Fields
        const int slowBaud = 115200, highBaud = 115200, CanBaud = 250000;
		SerialPort P;
		STM32F10X_Flash S;
		public int progress = 0;
		public string last_file = "";
        public string error = "";
        Boolean isCantester = false;
        
        UInt32 CAN_ID_Number;
        UInt32 CAN_BOOT_ID_Number;
        UInt32 Remote_UID = 0;
    	#endregion


		#region Constructor

		public Vision_Booter(string portname, UInt32 Can_ID)
		{
            CAN_ID_Number = CAN_ID_Vision_Poll | Can_ID;
            CAN_BOOT_ID_Number = CAN_ID_VisionBootPoll;
            isCantester = CanTesterInterface.IsCantester(portname);

            if (isCantester)
                P = new SerialPort(portname, CanBaud);
            else
                P = new SerialPort(portname, slowBaud);
            
            //P.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);

            P.ReadTimeout = 500;
            P.WriteTimeout = 500;
            P.Open();
		}
        public Vision_Booter(string portname, String slaveTD)
        {
            Remote_UID = UInt32.Parse(slaveTD, NumberStyles.HexNumber);
            P = new SerialPort(portname, slowBaud);

            P.ReadTimeout = 500;
            P.WriteTimeout = 500;
            P.Open();
        }
        #endregion

        #region Functions

        public int Do_Bootload(bool last, boot_default default_file)
		{
            error = "";
            Run_Bootloader();           // switch to boot loader if in application
            if (!Send_Image(last, default_file))          // open a binary file and start sending it.  
            {
                progress = -1;
                return 0;
            }
            else
                return 1;
		}

		/// <summary>
		/// sends the image size to the micro
		/// </summary>
		/// <param name="b_size"> the number of bytes to be sent</param>
		private void send_size_data(UInt32 b_size)
		{
			Byte[] Data = new Byte[6];

			Data[0] = 1;
			Data[1] = 0x0A;
			Data[2] = (byte)((b_size >> 16) & 0xFF);
			Data[3] = (byte)((b_size >> 8) & 0xFF);
			Data[4] = (byte)(b_size & 0xFF);
			Data[5] = 1;

			if (P != null)
			{
                if (P.IsOpen)
                {
                    if (isCantester)
                    {
                        CanTesterInterface.sendCanMessage(ref P, Data, CAN_BOOT_ID_Number);
                    }
                    else if (Remote_UID != 0)
                    {
                        var buff = new List<Byte>(Encoding.ASCII.GetBytes("ZE").Concat(Data));
                        byte[] CRC_Array = SerialFrame.getFrame(buff.ToArray());
                        P.Write(CRC_Array.ToArray(), 0, CRC_Array.Length);
                       // P.Write(buff.ToArray(), 0, buff.Count);           // get the bootloader revision. 
                    }
                    else
                    {
                        P.Write(Data, 0, 6);
                    }
                }
			}
		}

		public int Run_Bootloader()
		{
			if (P != null)
			{
				if (P.IsOpen)
				{
                    if(Remote_UID != 0)
                    {
                        // construct a remote message ([M][UID][message])
                        MemoryStream stream = new MemoryStream();
                        using (BinaryWriter writer = new BinaryWriter(stream))
                        {
                            byte[] Force_Master_Bootload = new byte[7];

                            byte[] builder = System.Text.Encoding.ASCII.GetBytes("M");
                            Force_Master_Bootload[0] = builder[0];
                            Force_Master_Bootload[1] = 0;

                            builder = System.BitConverter.GetBytes(Remote_UID);
                            Force_Master_Bootload[2] = builder[0];
                            Force_Master_Bootload[3] = builder[1];
                            Force_Master_Bootload[4] = builder[2];
                            Force_Master_Bootload[5] = builder[3];

                            builder = System.Text.Encoding.ASCII.GetBytes("B");
                            Force_Master_Bootload[6] = builder[0];

                            //writer.Write('M');
                            //writer.Write(Remote_UID);
                            //writer.Write('B');

                            
                            byte[] CRC_Array = SerialFrame.getFrame(Force_Master_Bootload);
                            writer.Write(CRC_Array);

                            var fd = stream.ToArray();
                            P.Write(fd, 0, fd.Length);
                            while (P.BytesToWrite != 0) ;
                            Thread.Sleep(10);
                            P.Write(fd, 0, fd.Length);
                            while (P.BytesToWrite != 0) ;
                            Thread.Sleep(10);
                            P.Write(fd, 0, fd.Length);
                            while (P.BytesToWrite != 0) ;
                            Thread.Sleep(10);
                        }

                        while (P.BytesToWrite != 0)
                            Thread.Sleep(1);
                        P.Close();
                    }
                    else if (isCantester)
                    {
                        CanTesterInterface.sendCanMessage(ref P, System.Text.Encoding.ASCII.GetBytes("B"), CAN_ID_Number); 
                        P.Close();
                    }
                    else
                    {
                        try
                        {
                            byte[] Force_Bootload = System.Text.Encoding.ASCII.GetBytes("B");
                            byte[] CRC_Array = SerialFrame.getFrame(Force_Bootload);
                            P.Write(CRC_Array.ToArray(), 0, CRC_Array.Length);
                            P.Close();
                        }
                        catch (Exception)
                        { }

                    }
				}
			}

			return 1;
		}

		private void Run_Application(object sender, RoutedEventArgs e)
		{
			if (P != null)
			{
                if (P.IsOpen)
                {
                    if (isCantester)
                        CanTesterInterface.sendCanMessage(ref P, System.Text.Encoding.ASCII.GetBytes("A"), CAN_BOOT_ID_Number);
                    else
                        P.Write("A"); 
                }
			}
		}

		public void close()
		{
			if (P != null)
			{
				if (P.IsOpen)
				{
					try
					{
						P.Close();
					}
					catch (Exception ex)
					{
                        System.Windows.MessageBox.Show("Error:" + ex.ToString());
                    }
				}
			}
		}
		

		/// <summary>
		/// Opens and sends a binary file to the micro. 
		/// </summary>
		private bool Send_Image(bool last, boot_default default_file)
		{
			OpenFileDialog F = new OpenFileDialog();
            byte[] Can_data;
            Byte FW_rev = 0;
            Byte Module_Kind = 0;

            Uri mantag_file = new Uri("pack://application:,,,/Vision Libs;component/Bootloader/binaries/ME-VISION-MANTAG-PFW.elf.binary");          // use this for Resources (compiled into Dll)
            Uri module_file = new Uri("pack://application:,,,/Vision Libs;component/Bootloader/binaries/ME-VISION-PFW.elf.binary");                 // use this for Resources (compiled into Dll)
            Uri L4_module_file = new Uri("pack://application:,,,/Vision Libs;component/Bootloader/binaries/ME-VISION-L4-PFW.elf.binary");           // use this for Resources (compiled into Dll)
            Uri GPSmodule_L4_file = new Uri("pack://application:,,,/Vision Libs;component/Bootloader/binaries/M-PFW-038-13-00.elf.binary");         // use this for Resources (compiled into Dll)

            F.Filter = "Binary image (.binary)|*.binary|other image (*.*)|*.*";
			F.FilterIndex = 1;
			F.Multiselect = false;

			if (P != null)
			{

                if (last == false && default_file == boot_default.none)
                {
                    if(F.ShowDialog() == false)
                    {
                        error = "Unable to open file";
                        return false;
                    }
                }
                else
                {
                    // in the case where we use a default file or the same file, wait a bit to give the port some time to close/open.  
                    Thread.Sleep(100);
                }


                if(last == false)
					last_file = F.FileName;

                if (isCantester)
                    P.BaudRate = CanBaud;
                else
                    P.BaudRate = highBaud;

                // after the reset command, the USB port may be disconnected, so try a few times to reopen it. 
                int err_count = 0;
                do
                {
                    try
                    {
                        P.Open();
                        break;
                    }
                    catch
                    {
                        err_count++;
                        Thread.Sleep(300);
                    }
                } while (err_count < 15);

                Thread.Sleep(500);

                ////////////////////////////////////////////////////////////////////////////////////////
                /// get the bootloader revision
                bool is_L4 = false;

				if (P.IsOpen)
				{
                    try
                    {
                        P.DiscardInBuffer();
                        Thread.Sleep(20);
                        //P.ReadTimeout = 500;

                        if (isCantester)
                        {
                            Can_data = new Byte[] { (Byte)'C' };
                            CanTesterInterface.sendCanMessage(ref P, Can_data, CAN_BOOT_ID_Number);
                            if (CanTesterInterface.getCanMessage(ref P, out Can_data) == false)
                            {
                                error = "Unable to read bootloader firmware revision";
                                return false;
                            }
                            FW_rev = Can_data[0];

                            Can_data = new Byte[] { (Byte)'K' };
                            CanTesterInterface.sendCanMessage(ref P, Can_data, CAN_BOOT_ID_Number);
                            if (CanTesterInterface.getCanMessage(ref P, out Can_data) == false)
                            {
                                error = "Unable to read Module kind";
                                return false;
                            }
                            Module_Kind = Can_data[0];
                        }
                        else if (Remote_UID != 0)
                        {
                            byte[] in_frame;
                            for (int i = 0; i < 3; i++)
                            {
                                byte[] Bootloader_REV = System.Text.Encoding.ASCII.GetBytes("ZEC");
                                byte[] CRC_Array = SerialFrame.getFrame(Bootloader_REV);
                                //P.Write("ZEC");           // get the bootloader revision.
                                P.Write(CRC_Array.ToArray(), 0, CRC_Array.Length);           // get the bootloader revision. 
                                if (SerialFrame.getMessage(ref P, out in_frame))
                                {
                                    if (in_frame[0] == 'b')
                                    {
                                        FW_rev = in_frame[1];            // get the FW rev from the message frame
                                        break;
                                    }
                                    else if (i >= 2)
                                    {
                                        error = "Unexpected bootloader revision response";
                                        return false;
                                    }
                                }
                                else if (i >= 2)
                                {
                                    error = "Unable to connect to remote tag";
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            P.Write("C");                       // get the bootloader revision. 
                            FW_rev = (Byte)P.ReadByte();        // wait for a byte to be received

                            Thread.Sleep(10);

                            P.Write("K");                           // get the Module Kind 
                            Module_Kind = (Byte)P.ReadByte();        // wait for a byte to be received
                        }

                        if ((FW_rev & 0x80) != 0)                   // this device is a STM32L4 device. 
                        {
                            is_L4 = true;
                            FW_rev &= 0x7F;                         // leave only the rev part. 
                        }

                        
                        /// perform a sanity check on the file name if we're loading our own file.
                        if (default_file == boot_default.none)
                        {
                            if (last_file.Contains("L4") == false && is_L4)
                            {
                                error = "Please use the L4 Firmware file with this module";
                                return false;
                            }
                            else if (last_file.Contains("L4") && !is_L4)
                            {
                                error = "Please don't use the L4 Firmware file with this module";
                                return false;
                            }
                        }
                    
                        if (FW_rev != 5)
                        {
                            error = "Bootloader revision not supported";
                            return false;
                        }

                        //////////////////////////////////////////////////////////////////////////////////////////////
                        /// Need to sort out which file we are going to use...
                        byte[] data;
                        if (default_file == boot_default.none)
                            data = File.ReadAllBytes(last_file);
                        else
                        {
                            var memoryStream = new MemoryStream();
                            if (default_file == boot_default.mantag)
                                Application.GetResourceStream(mantag_file).Stream.CopyTo(memoryStream);
                            else if ((is_L4) && (Module_Kind == 6))
                                Application.GetResourceStream(GPSmodule_L4_file).Stream.CopyTo(memoryStream);
                            else if ((is_L4) && ((Module_Kind == 3) || (Module_Kind == 2)))
                                Application.GetResourceStream(L4_module_file).Stream.CopyTo(memoryStream);
                            else
                                Application.GetResourceStream(module_file).Stream.CopyTo(memoryStream);
                            data = memoryStream.ToArray();
                        }
                        S = new STM32F10X_Flash(data);
                        /// //////////////////////////////////////////////////////////////////////////////////////////
                        
                        Thread.Sleep(100);
                        P.DiscardInBuffer();

                        send_size_data((uint)data.Length);
                        //P.ReadTimeout = 3500;
                        if (isCantester)
                        {
                            //  while (Can_received == false) ;
                            if (CanTesterInterface.getCanMessage(ref P, out Can_data) == false)
                            {
                                error = "Unable to send file length";
                                return false;
                            }
                        }
                        else if (Remote_UID != 0)
                        {
                            byte[] in_frame;
                            SerialFrame.getMessage(ref P, out in_frame);
                        }
                        else
                            P.ReadChar();            // wait for a byte to be received
                    }
                    catch (TimeoutException)
                    {
                        error = "Bootloader start timeout";
                        return false;
                    }
                    int error_count = 0;

                    while (progress < 100)
                    {
                        int bytes_sent = 0;

                        if (Remote_UID != 0)
                            bytes_sent = S.Sendblock_Remote(ref P);
                        else if (FW_rev == 5)
                            bytes_sent = S.Sendblock_V2(ref P, isCantester, CAN_BOOT_ID_Number);
                        else if (FW_rev == 1)
                            bytes_sent = S.Sendblock(ref P, isCantester, CAN_BOOT_ID_Number);
                        else
                        {
                            error = "Unsupported bootloader revision";
                            return false;
                        }           // catch unsupported bootloader versions

                        progress = (100 * S.byte_counter) / S.Flash_binary.Length;

                        System.Threading.Thread.Sleep(100);
                        if (bytes_sent != 0)
                            error_count = 0;
                        else if(error_count++ > 5)
                        {
                            error = "Error sending sector at " + progress.ToString() + "%";
                            return false;
                        }
                    }
                    return true;
                }
			}
            error = "Unable to open COM port";
            return false;
		}

		#endregion
	}
}
