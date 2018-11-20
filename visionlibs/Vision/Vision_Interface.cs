

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using CANTESTER;
using Vision;
using System.Collections.ObjectModel;
using Vision_Libs.Params;
using SerialFraming;
using System.Windows;
using Be.Timvw.Framework.ComponentModel;
using System.Threading.Tasks;
using Vision.Parameter;
using Vision_Libs.Properties;

namespace vision_interface
{
    class out_message
    {
        public int retries = 0;
        public byte[] data;
        public Stopwatch SW = new Stopwatch();

        public string content = "";

        public out_message(byte[] d_in, int retry)
        {
            retries = retry;
            data = d_in;
        }
        public out_message(byte[] d_in, int retry, string content_description)
        {
            retries = retry;
            data = d_in;
            content = content_description;
        }
    }

    public class Vision_Interface
    {
        #region Constants
        private const UInt32 CAN_ID_Vision_Poll = 0x12227100;
        private const UInt32 CAN_ID_Vision_Resp = 0x12227200;
        private const UInt32 CAN_ID_Vision_Sync = 0x12227300;
        private const UInt32 CAN_ID_VisionBootResp = 0x12227500;
        private const UInt32 CAN_ID_VisionBootPoll = 0x12227400;
        public const UInt32 CAN_ID_Vision_NotLast = 0x00000800;
        #endregion

        #region Fields

        public Action<string> Property_changed;
        public List<TAG> TAG_collection;
        public VisionDevice current_TAG = new VisionDevice();

        const int slowBaud = 115200, canBaud = 250000;
		SerialPort P;
        StringBuilder comlog = new StringBuilder();
  		public int MessageCount = 0;
		public string last_file = "";
        public Boolean isCantester = false;                    // This indicates that we're connected through a mernok CAN tester;e i.e. not a direct connected device.

        public Boolean Remote_connect = false;          // this tells us to forware commands to a remote device.
        public UInt32 Remote_Connect_UID = 0;           // this is used to indicate which remote device to talk to
        //Vision_Module Remote_TAG = new Vision_Module();

        byte message_expected = 0;
        Queue<out_message> out_messages = new Queue<out_message>();

        UInt32 CAN_ID_Number;

        public Boolean ShowLog = false;

        BackgroundWorker Worker = new BackgroundWorker();
        BackgroundWorker Send_worker = new BackgroundWorker();

        #endregion

        #region properties

        public String Comlog
        {
            get
            { 
            if(ShowLog)
                return comlog.ToString();
            else
                return "";
            }
            set
            {
                if (value == "") comlog.Clear();
            }
        }

        private byte[] remote_data_received;
        private object data_RX_lock = new object(); 
        public byte[] remote_data_RX
        {
            get
            {
                byte[] temp;
                lock (data_RX_lock)
                {
                    temp = new byte[remote_data_received.Length];
                    remote_data_received.CopyTo(temp, 0);
                }
                return temp;
            }
        }

        #endregion

        #region Constructor
        public Vision_Interface(Action<string> change_notification)
        {
            Property_changed = change_notification;
            TAG_collection = new List<TAG>();
            Worker.WorkerSupportsCancellation = true;
            Worker.DoWork += WorkerDo;
            Worker.ProgressChanged += WorkerProgress;
            Worker.WorkerReportsProgress = true;
            P = new SerialPort();

            Send_worker.DoWork += SendDo;
            Send_worker.WorkerSupportsCancellation = true;
        }

        public void ClosePort()
        {
            if (P != null)
            {
                try
                {
                    Worker.CancelAsync();
                    Send_worker.CancelAsync();
                    P.DataReceived -= port_DataReceived;
                    Thread.Sleep(200);
                    if (P.IsOpen)
                        P.Close();
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("error closing port: \n" + ex.Message);
                    System.Windows.MessageBox.Show(ex.Message);
                }
                finally
                {
                    comlog.Clear();
                }
            }
        }

        public bool OpenPort(string portname, UInt32 Baud, UInt32 Can_ID)
        {
            try
            {
                isCantester = CanTesterInterface.IsCantester(portname);
                P = new SerialPort(portname, (int)Baud);

                //P.DataReceived += port_DataReceived;
                if(!Worker.IsBusy)
                    Worker.RunWorkerAsync();

                P.ReadTimeout = 500;
                P.WriteTimeout = 500;
                Thread.Sleep(200);
                P.Open();
                P.DiscardInBuffer();
                current_TAG = new VisionDevice();           // when the port is opened, make no assumptions about the connected device. 
                TAG_collection = new List<TAG>();
                Property_changed("new device");             // send a message saying the device is considered to have changed. 
                CAN_ID_Number = CAN_ID_Vision_Poll | Can_ID;
            }
            catch (Exception e)
            {
                Console.WriteLine("Open port catch Vision Interface");
                ClosePort();
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        public bool IsPortOpen()
        {
            if (P == null)
                return false;
            else
                return P.IsOpen;
        }
        #endregion

        #region Functions

        /// <summary>
        /// Send data to the device through the connected serial port
        /// </summary>
        /// <param name="s">string to send</param>
        /// <returns>indication of success</returns>
        public bool SendMessage(string s)
        {
            return SendMessage(System.Text.Encoding.ASCII.GetBytes(s));
        }

        public bool SendMessage(Byte[] data)
        {
             List<Byte> buff = new List<Byte>(data);
            bool res = false;

            if (P != null)
            {
                try
                {
                    if (P.IsOpen)
                    {
                        // if the message is intended for a remote tag, append it with 'M' and the uid to send it to.
                        if (Remote_connect && Remote_Connect_UID != 0 && data[0] != 'u')
                        {
                            buff = new List<Byte>(BitConverter.GetBytes(Remote_Connect_UID).Concat(data));
                            buff.Insert(0, (Byte)'M');
                            buff.Insert(1, 0);
                            Console.WriteLine("Sending: " + Encoding.ASCII.GetString(buff.ToArray()) + "-UID: " + Remote_Connect_UID.ToString("X8"));
                        }

                        if (isCantester)
                            res = CanTesterInterface.sendCanMessage(ref P, buff.ToArray(), CAN_ID_Number);
                        else
                        {
                            byte[] CRC_Array = SerialFrame.getFrame(buff.ToArray());
                            P.Write(CRC_Array.ToArray(), 0, CRC_Array.Length);
                            Console.WriteLine("Sending: " + Encoding.ASCII.GetString(buff.ToArray()));
                            res = true;
                        }
                        while (P.BytesToWrite != 0) ;
                        return res;
                    }
                    else
                        return false;
                }
                catch (Exception)
                {
                    Console.WriteLine("Send Message catch Vision Interface");
                    return false;
                }
            }
            else
                return false;
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
            Byte[] B;
            while ((P.IsOpen) && (P.BytesToRead != 0))
            {
                if (GetMessage(out B))
                {
                    try
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() => { ProcessMessage(ref B); }));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("port_DataRecieved catch Vision Interface");
                    }
//                     ProcessMessage(ref B);
                }
            }
        }
                
        public bool SendReceiveMessage(Byte[] data)
        {
            if (P == null)
                return false;
         
            if (P.IsOpen == false)
                return false;

            message_expected = data[0];
            SendMessage(data);
            Stopwatch S = new Stopwatch();
            S.Start();

            while (message_expected != 0)
            {
                 Thread.Sleep(1);
                if (S.ElapsedMilliseconds > 500 || message_expected == 255)
                {
                    message_expected = 0;
                    //Debug.WriteLine("failed to send...");
                    return false;
                }
            }
            //Debug.WriteLine("Time waited: " + S.ElapsedMilliseconds.ToString());
            return true;
        }


        //private bool RetrySendReceiveMessage(Byte[] data)
        //{
        //    out_messages.Enqueue(new out_message(data, 4));                 // queue retry messages. 
        //    if(Send_worker.IsBusy == false) Send_worker.RunWorkerAsync();   // start the send process thread.
        //    return true;
        //}

        public bool RetrySendReceiveMessage(Byte[] data, string message)
        {
            out_messages.Enqueue(new out_message(data, 4, message));                 // queue retry messages. 
            if (Send_worker.IsBusy == false) Send_worker.RunWorkerAsync();   // start the send process thread.
            return true;
        }

        private void SendDo(object sender, DoWorkEventArgs e)
        {
            string LastFailed = "";
            BackgroundWorker worker = sender as BackgroundWorker;

            while (out_messages.Count > 0)
            {
                if ((P.IsOpen) && (out_messages.Count > 0) && (P.BytesToWrite == 0))
                {
                    out_message M = out_messages.Dequeue();
                    if (LastFailed == "" || LastFailed != M.content)
                    {
                        while (M.retries > 0)
                        {
                            M.retries--;
                            if (SendReceiveMessage(M.data) == false)
                            {
                                if (M.retries <= 0)
                                {
                                    //Debug.WriteLine("Failed to send command: " + M.content);
                                    // Console.WriteLine("Failed to send command: " + M.content);
                                    if (M.content != "")
                                    {
                                        LastFailed = M.content;
                                        Property_changed("Failed to send command: " + M.content);
                                    }
                                }
                            }
                            else
                            {
                                // successfully set command. if its the last message and has a string, report success. 
                                if (out_messages.Count == 0 && M.content != "")
                                {
                                    Console.WriteLine("Successfully sent command: " + M.content);                        
                                    Property_changed("Successfully sent command: " + M.content);
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
                if ((worker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        private void WorkerDo(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Byte[] B;
            while (true)
            {
                try
                {
                    if ((P.IsOpen) && (P.BytesToRead != 0))
                    {
                        if (GetMessage(out B))
//                            ProcessMessage(ref B);
                            Worker.ReportProgress(100, B);
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (System.Exception)
                {
                    Console.WriteLine("WorkerDo Vision Interface");

                }
                if ((worker.CancellationPending == true))
                {
                    e.Cancel = true;
                    break;
                }
            }
        }

        private void WorkerProgress(object sender, ProgressChangedEventArgs e)
        {
            Byte[] B = (Byte[])e.UserState;
            ProcessMessage(ref B);
        }

        private bool GetMessage(out Byte[] B)
        {
            if (isCantester)
            {
                return CanTesterInterface.getCanMessage(ref P, out B);
            }
            else
            {
                return SerialFrame.getMessage(ref P, out B);
            }
        }

        /// <summary>
        /// All incoming data gets decoded here. 
        /// </summary>
        /// <param name="data">single message data message</param>
        private void ProcessMessage(ref Byte[] data)
        {
            MessageCount++;
            //if(data.Length>0)
            Console.WriteLine("Recieving: " + Convert.ToChar(data[0]) + " " + data.Length);

            if (data.Length == 0)
                return;

            byte message = data[0];

            // starts with R, so this is a remote response. discard the R and UID.  
            if (message == (byte)'R')
            {
                data = data.Skip(6).ToArray();
            }

            message = data[0];
            if (message_expected == message)
                message_expected = 0;

            if ((message == (Byte)'P') || (message == (Byte)'p') || (message == (Byte)'A') || (message == 1) || (message == 2))
            {
                if (data.Length >= 8)
                {
                    UInt32 UID = TAG.parse_message_UID(data);
                    if (UID != 0)
                    {
                        if (UID < 1000)
                        {
                            //bool test = false;
                        }
                        // check if the item is present already
                        bool found = false;

                        TAG T = TAG_collection.SingleOrDefault(x => x._UID == UID);
                        if (T == null)
                            T = new TAG();
                        else
                            found = true;

                        T.parse_message_into_TAG(data);
                        if ((found == false) && (UID != 0))
                        {
                            TAG_collection.Add(T);
                        }
                        Property_changed("tag_list");
                    }
                }
            }
            else if (message == (Byte)'u' && data.Length >= 6)
            {
                current_TAG.Params.kind = data[1];
                current_TAG.Params.UID = BitConverter.ToUInt32(data, 2);
                Property_changed("UID");
            }
            else if (message == (Byte)'v' && data.Length == 3)
            {
                current_TAG.Params.firmware_rev = data[1];
                current_TAG.Params.firmware_subrev = data[2];
                Property_changed("Firmware revision");
            }
            else if ((message == (Byte)'s' || message == (Byte)'C') && data.Length >= 7)
            {
                if (message == (byte)'C' && data[2] == 255)           // this is a save message failure. tell the save thread that the message failed.
                    message_expected = 255;
                // check that the current tag is a valid device so we can write to it. 
                if (current_TAG.Params.UID != 0)
                {
                    /// get the index of the parameter in question
                    VisionParams.adr index = (VisionParams.adr)BitConverter.ToUInt16(data, 1);
                    if (current_TAG.Params[index] != null)
                    {
                        //if (data[3] <= 4)        // number type bytes returned. write it to the parameter. 
                        //{                         // needed to remove length checking here as writing 0 length name was gicing errors. 
                            current_TAG.Params[index].Value = BitConverter.ToUInt32(data, 4);
                        //}
                        //else /// this is a string type parameter.
                        //{
                            int len = Math.Min(Parameter.string_max, Math.Min(data[3], data.Length - 4));
                            current_TAG.Params[index].Value_str = Encoding.ASCII.GetString(data, 4, len);
                        //}
                        current_TAG.Params[index].param_changed = false;    // rest the changed flag so we know when a param is changed via the gui.
                        if (current_TAG.Params.UID != 0)
                        {
                            Property_changed("current_settings");
                        }
                    }
                }
            }
            else if(message == 'c' && data.Length == 2)
            {
                if (data[1] == 255)           // this is a save message failure. tell the save thread that the message failed.
                    message_expected = 255;
            }
            else if (message == (Byte)'F' && data.Length >= 8)
            {
                if (current_TAG.Params.UID != 0)
                {
                    /// get the index of the parameter in question
                    int index = (int)BitConverter.ToUInt16(data, 1);

                    if (data[3] <= 4)        // number type setting returned. write it to the parameter. 
                    {
                        current_TAG.Params.StatusVals.Find(x => x.address == index).Value = BitConverter.ToUInt32(data, 4);
                    }

                    if (current_TAG.Params.UID != 0)
                    {
                        Property_changed("current_status");
                    }
                }
            }
            else if (message == (Byte)'f' && data.Length >= 9)
            {
                UInt32 sts = BitConverter.ToUInt32(data, 1);
                if (current_TAG.Params.UID != 0)
                {
                    current_TAG.status = (TAG_status)sts;
                    Property_changed("current_status");
                }
            }
            else if (message == (Byte)'e' && data.Length >= 5)          // forwarded LF packet
            {   
                current_TAG.own_LF.VehicleID = BitConverter.ToUInt16(data, 1);
                current_TAG.own_LF.SlaveID = data[3];
                current_TAG.own_LF.RSSI = (sbyte)data[4];
                current_TAG.own_LF.Last_Seen = DateTime.Now;
                Property_changed("own lf");
            }
            else if (message == (Byte)'d')                              // Forwarded Data from a remote device to our VID
            {
                lock (data_RX_lock)
                {
                    remote_data_received = new byte[data.Length - 2];
                    Array.Copy(data, 2, remote_data_received, 0, data.Length - 2);
                }
                switch (data[1])
                {
                    case (Byte)'U':
                        Property_changed("Data message UID");
                        break;
                    case (Byte)'V':
                        Property_changed("Data message VID");
                        break;
                    case (Byte)'G':
                        Property_changed("Data message Global");
                        break;
                    default:
                        break;
                }
               
            }
            else if (message == (Byte)'w')                              // got boardinfo
            {
                current_TAG.Params.PCB_ID = Encoding.ASCII.GetString(data).Substring(1);
                Property_changed("board info");
            }

            else if ((message == (Byte)'G') && data.Length >= 54)                            // GPS Navigation Info
            {
                current_TAG.GPS_Data.Longitude = BitConverter.ToInt32(data, 1);
                current_TAG.GPS_Data.Latitude = BitConverter.ToInt32(data, 5);
                current_TAG.GPS_Data.HorizontalAccuracy = BitConverter.ToInt32(data, 9);
                current_TAG.GPS_Data.VerticalAccuracy = BitConverter.ToInt32(data, 13);
                current_TAG.GPS_Data.FixType = (TAG._GPS_FixType)data[17];
                current_TAG.GPS_Data.NumberOfSat = data[18];
                current_TAG.GPS_Data.Speed = BitConverter.ToInt32(data, 19);
                current_TAG.GPS_Data.SpeedAccuracy = BitConverter.ToInt32(data, 23);
                current_TAG.GPS_Data.HeadingVehicle = BitConverter.ToInt32(data, 27);
                current_TAG.GPS_Data.HeadingMotion = BitConverter.ToInt32(data, 31);
                current_TAG.GPS_Data.HeadingAccuracy = BitConverter.ToUInt32(data, 35);
                current_TAG.GPS_Data.Sealevel = BitConverter.ToInt32(data, 39);
                current_TAG.GPS_Data.Flags = BitConverter.ToUInt16(data, 43);
                current_TAG.GPS_Data._Date = BitConverter.ToUInt32(data, 45);
                current_TAG.GPS_Data._Time = BitConverter.ToUInt32(data, 49);
                current_TAG.GPS_Data.FixAge = data[53];

                Property_changed("GPS Data");
            }

            else if ((message == (Byte)'o') && data.Length >= 17)                               // GPS Odometer Info
            {
                current_TAG.GPS_Data.Speed = BitConverter.ToInt32(data, 1);
                current_TAG.GPS_Data.SpeedAccuracy = BitConverter.ToInt32(data, 5);
                current_TAG.GPS_Data.TravelDistance = BitConverter.ToUInt32(data,9);
                current_TAG.GPS_Data.TotalTravelDistance = BitConverter.ToUInt32(data, 13);

                Property_changed("GPS Data");
            }

            else if ((message == (Byte)'q') && data.Length >= 1)                               // GPS Odometer Info
            {
                current_TAG.GPS_Data.Antenna_State = data[1];
                Property_changed("GPS Data");
            }


            else if (message == (Byte)'y' && data.Length >= 1)                              // GPS Odometer Info
            {
                Property_changed("Reset GPS Odometer");
            }


            if (ShowLog)
            {
                foreach (byte d in data)
                {
                    comlog.Append("-" + d.ToString("X2"));
                }

                comlog.AppendLine();

                if (comlog.Length > comlog.MaxCapacity / 2)
                {
                    comlog.Remove(0, comlog.Length / 2);
                }

                Property_changed("comlog");
            }
            else
            {
                comlog.Clear();
                foreach (byte d in data)
                {
                    comlog.Append("-" + d.ToString("X2"));
                }

                comlog.AppendLine();
                Property_changed("comlog");
            }
        }

        public void clean_taglist(int age_seconds)
        {
            Collection<TAG> tags_to_remove = new Collection<TAG>();

            foreach (TAG T in TAG_collection)
            {
                //T.updateTime();         // tell the datagrid these values have changed
                TimeSpan Droptime = new TimeSpan(0, 0, age_seconds);
                TimeSpan time_since_seen = DateTime.Now - T._LastSeen;

                if (time_since_seen.Ticks > Droptime.Ticks)
                {
                    tags_to_remove.Add(T);
                }
            }

            foreach (TAG T in tags_to_remove)
            {
                TAG_collection.Remove(T);
            }
            // this is a way to force update all data of all tags. only needs to be called on one tag, so we do it on the first. 
            //if (TAG_collection.Count > 0)
            //    TAG_collection.First().updateAll();
            
            Property_changed("tag_list");
        }
#endregion

#region command messages
        public void RunTest()
        {
            if (P.IsOpen)
            {
                // --- V12 ---
                GetBoardInfo();
                GetSettings();
                GetStatusVals();
                RetrySendReceiveMessage(Encoding.ASCII.GetBytes("f"), "");
            }
        }

        public void PollUID()
        {
            if (P.IsOpen)
            {
                SendMessage("u");
            }
        }

        public void Getstatus()
        {
            if (P.IsOpen)
            {
                SendMessage("u");
                Thread.Sleep(20);
                SendMessage("f");
            }
        }

        public void GetTags(Byte type, Byte age, UInt16 dist, Byte kind)
        {
            Byte[] data = new Byte[] { (byte)'p', (Byte)dist, (Byte)(dist>>8), age, type, kind };
            if (isCantester == false)
                data[0] = (byte)'P';
            SendMessage(data);
        }

        public void GetTags(Byte type, Byte age, Byte LF_rssi, Byte kind)
        {
            Byte[] data = new Byte[] { (byte)'p', LF_rssi, 0, age, type, kind};
            if (LF_rssi == 255)
                data[2] = 255;
            if (isCantester == false) data[0] = (byte)'P';
            SendMessage(data);
        }

        /// <summary>
        /// sends a series of 'save setting' commands.
        /// </summary>
        /// <param name="all">if true, all valid parameters will be sent. Otherwise only changed values are sent.</param>
        /// <returns>false if the params are not valid to send.</returns>
        public bool SaveSettings(bool all)
        {
            // make sure the parameters are valid to be sent. 
            if (current_TAG.Params.UID != 0 && current_TAG.Params.Params.Count(p => p.param_valid)>0)
            {
                if (all)
                    foreach (var P in current_TAG.Params.Params.Where(n => n.param_valid == true))
                    {
                        //        RetrySendReceiveMessage(P.GetSaveMessage(), "Save Settings");
                        RetrySendReceiveMessage(P.GetSetGetMessage(), "Save settings");
                    }
                else
                    foreach (var P in current_TAG.Params.Params.Where(n => n.param_valid == true && n.param_changed == true))
                    {
                        //RetrySendReceiveMessage(P.GetSaveMessage(), "Save Settings");
                        RetrySendReceiveMessage(P.GetSetGetMessage(), "Save settings");
                    }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Send commands to get the connected device's settings.
        /// </summary>
        public void GetSettings()
        {
            RetrySendReceiveMessage(Encoding.ASCII.GetBytes("u"), "");
            foreach (var P in current_TAG.Params.Params)
            {
                RetrySendReceiveMessage(P.GetReadMessage(), "Get settings");
            }
        }

        /// <summary>
        /// Send command to get a specific setting from the connected device. The command can either wait for the response, or not. 
        /// </summary>
        public bool GetSetting(VisionParams.adr param_address, bool wait)
        {
            Byte[] data = { (byte)'s', 0, 0 };
            BitConverter.GetBytes((UInt16)param_address).CopyTo(data, 1);     // index of the parameter

            if (wait)
                return SendReceiveMessage(data);
            else 
                return RetrySendReceiveMessage(data, "Get settings");
        }

        public void ResetCommand()
        {
            SendMessage("r");
            Thread.Sleep(200);
            if ((!P.IsOpen)&&(Remote_Connect_UID==0))
            {  
                OpenPort(Settings.Default.Last_Port, (uint)Settings.Default.Ranger_Baud, 0);
            }
        }

        public void FWRevCommand()
        {
            SendMessage("v");
        }

        public void BoardInfoCommand()
        {
            SendMessage("w");
        }

        public void SetReaderActivities()
        {
            Byte[] arr = { (Byte)'c', 0xFF, 0xFF, 2, 0, 0, 0 };
            RetrySendReceiveMessage(arr, "Set Activities");    
        }

        public void SetPulse500Activities()
        {
            Byte[] arr = { (Byte)'c', 0xFF, 0xFF, 4, 0, 0, 0 };
            RetrySendReceiveMessage(arr, "Set Activities");
        }

        public void SetPulse300Activities()
        {
            Byte[] arr = { (Byte)'c', 0xFF, 0xFF, 3, 0, 0, 0 };
            RetrySendReceiveMessage(arr, "Set Activities");
        }

        public void SetPulseMantagActivities()
        {
            Byte[] arr = { (Byte)'c', 0xFF, 0xFF, 1, 0, 0, 0 };
            RetrySendReceiveMessage(arr, "Set Activities");
        }

        public void SetPulseRangerActivities()
        {
            Byte[] arr = { (Byte)'c', 0xFF, 0xFF, 5, 0, 0, 0 };
            RetrySendReceiveMessage(arr, "Set Activities");
        }

        public void SetPulseGPSActivities()
        {
            Byte[] arr = { (Byte)'c', 0xFF, 0xFF, 6, 0, 0, 0 };
            RetrySendReceiveMessage(arr, "Set Activities");
        }

        public bool GetStatusVals()
        {
            
            foreach (var P in current_TAG.Params.StatusVals)
            {
                if (RetrySendReceiveMessage(P.GetStatusMessage(), "") == false)
                    break;
            }
            return true;
        }

        public bool Send_Remote_Data(UInt32 dest, byte[] data)
        {
            // 'D' + DEST[4] + Data[<52]
            if(data.Length > 52)
                Array.Resize(ref data, 52);
            return SendMessage(Encoding.ASCII.GetBytes("D").Concat(BitConverter.GetBytes(dest)).Concat(data).ToArray());
        }

        public bool Send_Remote_Data(UInt32 dest, String data)
        {
            // 'D' + DEST[4] + Data[<52]
            if (data.Length > 52)
                data = data.Substring(0, 52);
            return SendMessage(Encoding.ASCII.GetBytes("D").Concat(BitConverter.GetBytes(dest)).Concat(Encoding.ASCII.GetBytes(data)).ToArray());
        }

        public bool Force_RF_broadcast(bool PDS, byte type)
        {
            Byte[] arr = { (Byte)'I', type };
            if (PDS)
                arr[0] = (Byte)'i';
            return SendMessage(arr);
        }

        public bool Force_RF_broadcast(byte type, String name)
        {
            if (name.Length > 20)
                name = name.Substring(0, 20);
            var message = Encoding.ASCII.GetBytes("Ix" + name);
            message[1] = type;
            return SendMessage(message);
        }

        public bool Force_LF(byte SID)
        {
            byte[] message = { (byte)'L', SID};
            return SendMessage(message);
        }

        public void GetBoardInfo()
        {
            //BoardInfoCommand();
            Byte[] arr = { (Byte)'w' };
            RetrySendReceiveMessage(arr, "board info");
        }

        public void Force_RF_Zone(uint Zone)
        {
            byte[] UID = BitConverter.GetBytes(Remote_Connect_UID);
            if(Remote_Connect_UID!=0)
            {
                byte[] VID = BitConverter.GetBytes(881);
                Byte[] arr = { (byte)'z', (byte)Zone, VID[0], VID[1], VID[2], VID[3], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                SendMessage(arr);
            }
            else
            {
                
                foreach (TAG item in TAG_collection)
                {
                    Remote_connect = true;
                    Remote_Connect_UID = item._UID;
                    byte[] VID = BitConverter.GetBytes(881);
                    Byte[] arr = { (byte)'z', (byte)Zone, VID[0], VID[1], VID[2], VID[3], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                    SendMessage(arr);
                    Remote_connect = false;
                    Remote_Connect_UID = 0;
                    Thread.Sleep(10);
                }
                
            }
            
        }

        public void Force_LF_Zone(uint LF_RSSI, uint VID, uint SID)
        {

            byte[] Tag_UID = BitConverter.GetBytes(current_TAG.Params.UID);
            byte Tag_kind = (byte)current_TAG.Params.kind;
            byte Tag_Type = (byte)current_TAG.Params[VisionParams.adr.tag_type].Value;
            byte Tag_Status = (byte)current_TAG.status;
            byte Tag_Firmware = (byte)current_TAG.Params[VisionParams.adr.firmware].Value;
            byte[] Tag_VID = BitConverter.GetBytes(VID);
            byte Tag_SID = (byte)SID;
            byte Tag_LF_RSSI = (byte)LF_RSSI;
            byte[] Tag_LF_VID = BitConverter.GetBytes(current_TAG.own_LF.VehicleID);

            Byte[] arr =     
            {
                (byte)'l',                          //0
                (byte)current_TAG.Params.kind,      //1
                Tag_UID[0],                         //2
                Tag_UID[1],                         //3
                Tag_UID[2],                         //4                            
                Tag_UID[3],                         //5                            
                5,                                  //6
                1,                                  //7
                0x2F,                               //8                     
                1,                                  //9                           
                0,                                  //10                           
                Tag_Firmware,                       //11                            
                Tag_VID[0],                         //12                            
                Tag_VID[1],                         //13                            
                Tag_VID[2],                         //14                            
                Tag_VID[3],                         //15                          
                Tag_SID,                            //16
                0,                                  //17
                0,                                  //18
                0,                                  //19
                Tag_LF_VID[0],                      //20                               
                Tag_LF_VID[1],                      //21                            
                12,                                 //22
                Tag_LF_RSSI                         //23                            
            };
            SendMessage(arr);
        }

        public void Reset_GPS_ODO()
        {
            Byte[] arr = { (Byte)'y' };
            RetrySendReceiveMessage(arr, "Reset GPS Odometer");
        }

        #endregion
    }
}
