using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vision_Libs.Vision;
using System.Windows.Media;
using System.ComponentModel;
using Vision_Libs.Utility;
using System.Reflection;
using Vision_Libs.Params;
using System.IO;

namespace Vision
{
    public class LF_data : IComparable<LF_data>, INotifyPropertyChanged, ICloneable
    {
        public DateTime Last_Seen = DateTime.MinValue;             // keeps track of the last message received
        public sbyte RSSI = -1;
        public Byte SlaveID;
        public UInt16 VehicleID;
        public int distance = 0;
        public bool is_range = false;

        public static sbyte CritThresh = 7;
        public static sbyte WarnThresh = 3;
        public static sbyte PresThresh = 0;
        public static TimeSpan timeout = new TimeSpan(0, 0, 8);

        public override string ToString()
        {
            if (is_range)
            {
                return (distance / 10.0) + " m";
            }
            else
            {
                if (RSSI == -1)// || DateTime.Now.Subtract(Last_Seen).TotalSeconds > 20)
                    return "none";
                else
                    return RSSI + " dB";
            }
        }

        public SByte LF { get { return RSSI; } set { RSSI = value; OnPropertyChanged("LF"); } }
        public Brush Color
        {
            get
            {
                if (RSSI > CritThresh)
                    return Brushes.OrangeRed;
                else if (RSSI > WarnThresh)
                    return Brushes.Yellow;
                else if (RSSI > PresThresh)
                    return Brushes.Blue;
                else
                    return Brushes.Transparent;
            }
            set { }
        }
        public override bool Equals(Object input)
        {
            if (input.GetType() != typeof(LF_data))
                return false;
            LF_data obj = input as LF_data;
            if ((obj.RSSI == this.RSSI))
                return true;
            return false;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        int IComparable<LF_data>.CompareTo(LF_data other)
        {
            if (is_range)
                return this.distance.CompareTo(other.distance);
            else
                return this.RSSI.CompareTo(other.RSSI);
        }

        public double AgeOpacity
        {
            get
            {
                TimeSpan age = DateTime.Now - Last_Seen;
                if (age.TotalSeconds > 2.0 * timeout.TotalSeconds || (Last_Seen == DateTime.MinValue))
                    return 0.0;
                else if (age < timeout)
                    return 1.0;
                else
                    return (timeout.TotalMilliseconds * 2.0 - age.TotalMilliseconds) / timeout.TotalMilliseconds;
            }
        }

        // boilerplate code supporting PropertyChanged events:
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

    public class TAG : INotifyPropertyChanged
    {
        public static UInt32 parse_message_UID(byte[] data)
        {
            if (data.Length > 5)
                return BitConverter.ToUInt32(data, 1);
            else
                return 0;
        }

        public enum _kind
        {
            unknown = 0, PDS = 1, Ranging = 2, Pulse = 3, Ranging_pulse = 4, Pulse_GPS = 5
        };

        public enum _productID
        {
            Undefined = 0,
            Commander100,
            Commander200,
            MaxusX,
            BooycoHMI
        }

        public enum _GPS_FixType
        {
            _NoFix = 0,
            _DeadReckoning = 1,
            _2D = 2,
            _3D = 3,
            _GNSSDeadReckoning = 4,
            _TimeOnly = 5
        };

        public enum _GPS_PSMState
        {
            _NotActive = 0,
            _Enabled = 1,
            _Acquisition = 2,
            _Tracking = 3,
            _PowerOptimized = 4,
            _Inactive = 5
        };

        public enum _GPS_AntennaState
        {
            _Init = 0,
            _DontKnow = 1,
            _OK = 2,
            _Short = 3,
            _Open = 4
        };

        public enum _Level_Test
        {
            None_Selected = 0,
            Module = 1,
            Pulse100 = 2,
            Pulse110 = 3,
            Pulse120 = 4,
            Pulse300 = 5,
            Pulse500 = 6
        };

        #region Fields

        //const Byte default_type = (Byte)TagType.NoGo;
        Byte default_type = (Byte)TagTypesL.MernokAssetType[0].Type;
        const UInt16 default_ping_Time = 1000;  // fifth WDT setting, = 2s

        public UInt32 _UID;                     //--
        public Byte TAGType;                   //  | 
        public Byte TAGGroup;
        public Byte voltage;
        public DateTime _LastSeen;             // keeps track of the last message received
        public int times_seen;                 // how many times its been seen
        public UInt16 Ping_Interval;           // how often the tag sends a message 
        public UInt16 dist = 0;
        public Byte statusB = 0;




        /////////////////////////////////////////////////////////////
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            else
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }
        }
        /////////////////////////////////////////////////////////////
        #endregion

        #region properties

        public string UID
        {
            get
            {
                return _UID.ToString("X8");
            }
            set { OnPropertyChanged("UID"); }
        }
        public string Type
        {
            get
            {
                if ((TAGType - 1 < TagTypesL.MernokAssetType.Count) && (TAGType > 0))
                {
                    return TagTypesL.MernokAssetType[TAGType - 1].TypeName;
                }
                else
                {
                    //TAGType = (byte)TagType.Person;
                    //return TagTypesL.MernokAssetType[(int)TagType.Person - 1].TypeName;
                    return "unknown";

                }

            }
            set { OnPropertyChanged("Type"); }
        }

        public string Group
        {

            get
            {
                if (TAGGroup != 0)
                {
                    if ((TAGType - 1 < TagTypesL.MernokAssetType.Count) && (TAGType > 0))
                    {
                        return TagTypesL.MernokAssetType[TAGType - 1].GroupName;
                    }
                    else
                    {
                        return "unknown";
                    }
                }
                else
                {
                    return "unknown";
                }
            }
            set { OnPropertyChanged("Group"); }

        }

        //public Uri typeIconUri
        //{
        //    get
        //    {

        //        return 0;
        //        return new Uri(@"../visionlibs/TagTypes/assets/drawable/type_icon0.png", UriKind.Relative);
        //        //return new Uri("pack://application:,,,/Vision Libs;component/TagTypes/assets/drawable/type_icon" + TAGType + ".png");
        //    }
        //    set { OnPropertyChanged("typeIconUri"); }
        //}

        public string Last_seen
        {
            get
            {
                return (DateTime.Now - _LastSeen).TotalSeconds.ToString("F0") + " s";
            }
        }

        private int _MantagAck;
        public bool ManTagAck
        {
            get
            {
                return Convert.ToBoolean(_MantagAck);
            }

        }

        public string Voltage
        {
            get
            {
                return ((double)voltage / 20.0).ToString("F2");
            }
        }

        private byte CID = 0;
        public byte CAN_slave_ID
        {
            get { return CID; }
            set { SetField(ref CID, value, "CAN_Slave_ID"); }
        }

        public UInt32 VID = 0xFFFF;
        public string Vehicle_ID
        {
            get
            {
                if (TAGType == (byte)TagType.Person)
                {
                    return VID.ToString("X8");
                }
                else
                {
                    return VID.ToString();
                }
            }

        }

        public string Status
        {
            get
            {
                return "0x" + statusB.ToString("X2");
            }
        }
        private Byte FirmRev = 0;
        public string Rev
        {
            get
            {
                return "V" + FirmRev.ToString();
            }
        }
        public FixedSizedQueue<LF_data> LF_history = new FixedSizedQueue<LF_data>(20);
        LF_data latest = new LF_data();

        /// <summary>
        /// Return the first instance of LF data with the given VID, or null if none is found.  
        /// </summary>
        /// <param name="desiredVID"></param>
        /// <returns></returns>
        public LF_data LF_has_VID(UInt16 desiredVID, int agesecs)
        {
            var now = DateTime.Now;
            return LF_history.LastOrDefault(T => (T.VehicleID == desiredVID) && ((now - T.Last_Seen).TotalSeconds <= agesecs));
        }

        /// <summary>
        /// returns the biggest LF with the given VID, newer than the specified age. 
        /// </summary>
        /// <param name="desiredVID"></param>
        /// <param name="agesecs"></param>
        /// <returns></returns>
        public LF_data LF_biggest_VID(UInt16 desiredVID, int agesecs)
        {
            var now = DateTime.Now;
            var t = LF_history.Where(T => (now - T.Last_Seen).TotalSeconds <= agesecs && T.VehicleID == desiredVID);
            return t.Max();
        }

        /// <summary>
        /// returns all the LF results with the given VID, newer than the specified age, in order of  decreasing RSSI 
        /// </summary>
        /// <param name="desiredVID"> the VID we're interested in</param>
        /// <param name="agesecs"></param>
        /// <returns></returns>
        public LF_data[] LF_biggest_VIDs(UInt16 desiredVID, int agesecs, int threshold)
        {
            var now = DateTime.Now;
            // grab only applicable LF results according to our filters
            var t = LF_history.Where(T => (now - T.Last_Seen).TotalSeconds <= agesecs && T.VehicleID == desiredVID && T.RSSI > threshold);
            var sorted = t.OrderByDescending(T => T.RSSI).ToArray();
            // sort them, so strongest LF is at the top
            return sorted.GroupBy(T => T.SlaveID).Select(SID_Group => SID_Group.Max()).ToArray();
        }

        public LF_data LF_Data
        {
            get { return latest; }
            set
            {
                latest = value;
                LF_history.Enqueue(value);

                OnPropertyChanged("LF_Data");
                OnPropertyChanged("LF_VID");
                OnPropertyChanged("LF_SID");
            }
        }

        public string LF_VID
        {
            get
            {
                return latest.VehicleID.ToString();
            }
        }

        public string LF_SID
        {
            get
            {
                return latest.SlaveID.ToString();
            }
        }
       
        private Byte rssi = 0;
        public Byte RSSI
        {
            get { return rssi; }
            set { SetField(ref rssi, value, "RSSI"); }
        }

        private double Speed_km_h = 000000;
        public string Speed
        {
            get
            {
                return (Math.Round(Speed_km_h / 277.778,2)).ToString() + " km/h";
            }
            //return Latitude_deg.ToString().PadLeft(4, '0').Insert(2, "."); }
            // get { return Latitude_deg; }
            // set { SetField(ref Latitude_deg, value, "Latitude_Deg"); }
        }

        private int _reverse;
        public bool Reverse
        {
            get
            {
                return Convert.ToBoolean(_reverse);
            }

        }

        private DateTime _dateTime;

        public DateTime DateTime
        {
            get { return _dateTime; }
            set { SetField(ref _dateTime, value, "DateTime"); }
        }


        private _kind ikind;
        public _kind Kind
        {
            get { return ikind; }
            set { SetField(ref ikind, value, "kind"); }
        }


        private _productID productID;

        public _productID ProductID
        {
            get
            {
                if ((byte)productID == 255)
                    productID = 0;
                return productID;
            }
            set
            {
                SetField(ref productID, value, "ProductID");
            }
        }


        private Int32 Longitude_deg = 000000;
        public string Longitude
        {
            get
            {
                //string Longitude_abs = Math.Abs(TAG_GPS_Data.Longitude_deg).ToString();
                string Longitude_abs = Math.Abs(Longitude_deg).ToString();
                if (Longitude_deg >= 0)
                    return Longitude_abs.PadLeft(4, '0').Insert(2, ".");
                else
                    return "-" + Longitude_abs.PadLeft(4, '0').Insert(2, ".");
            } 
            //get { return Longitude_deg.ToString().PadLeft(4,'0').Insert(2, "."); }
            //set { SetField(ref Longitude_deg, value, "Longitude_Deg"); }
        }

        private Int32 Latitude_deg = 000000;
        public string Latitude
        {
            get {
                string Latitude_abs = Math.Abs(Latitude_deg).ToString();
                if (Latitude_deg >= 0)
                    return Latitude_abs.PadLeft(4, '0').Insert(2, ".");
                else
                    return "-" + Latitude_abs.PadLeft(4, '0').Insert(2, ".");
            }
            //return Latitude_deg.ToString().PadLeft(4, '0').Insert(2, "."); }
            // get { return Latitude_deg; }
            // set { SetField(ref Latitude_deg, value, "Latitude_Deg"); }
        }

        private double Horizontal_Acc = 000000;
        public string Horizontal_Accuracy
        {
            get
            {
                return (Horizontal_Acc/1000).ToString() + " m";
            }
            //return Latitude_deg.ToString().PadLeft(4, '0').Insert(2, "."); }
            // get { return Latitude_deg; }
            // set { SetField(ref Latitude_deg, value, "Latitude_Deg"); }
        }

        private double Vertical_Acc = 000000;
        public string Vertical_Accuracy
        {
            get
            {
                return (Vertical_Acc / 1000).ToString() + " m";
            }
            //return Latitude_deg.ToString().PadLeft(4, '0').Insert(2, "."); }
            // get { return Latitude_deg; }
            // set { SetField(ref Latitude_deg, value, "Latitude_Deg"); }
        }

        private double Vehicle_heading_ = 0000;
        public double Heading_v
        {
            get
            {
                return Vehicle_heading_ / 100000.0;
            }
        }

        private Int32 Sea_Level_m = 000000;
        public string Sea_Level
        {
            get
            {
                return Sea_Level_m.ToString() + " m";
            }
            //return Latitude_deg.ToString().PadLeft(4, '0').Insert(2, "."); }
            // get { return Latitude_deg; }
            // set { SetField(ref Latitude_deg, value, "Latitude_Deg"); }
        }


        private _GPS_FixType FixType = 0;
        public _GPS_FixType Fix_Type
        {
            get { return FixType; }
            set { SetField(ref FixType, value, "Fix_Type"); }
        }

        uint FixAge = 0;
        public uint Fix_Age
        {
            get { return FixAge; }
        }

        private string _tag_name = "";
        public string Tag_Name { get { return _tag_name; } set { SetField(ref _tag_name, value, "Tag_Name"); } }
        #endregion

        private PropertyInfo[] _PropertyInfos = null;
        public override string ToString()
        {
            if (_PropertyInfos == null)
                _PropertyInfos = this.GetType().GetProperties();

            var sb = new StringBuilder();

            foreach (var info in _PropertyInfos)
            {
                var value = info.GetValue(this, null) ?? "";
                if(value.ToString() != "" && value.ToString().Length < 50)
                    sb.Append(info.Name + ": " + value.ToString() + ", ");
            }

            return sb.ToString();
        }


        #region Constructors

        public TAG()
        {
            _UID = 0;
            TAGType = default_type;
            _LastSeen = DateTime.Now;
            times_seen = 0;
            Ping_Interval = default_ping_Time;
        }

        public TAG(UInt32 _UID, Byte _TAGType, Byte _voltage, UInt16 _dist, Byte age, UInt16 Vehc_ID, Byte Can_ID, Byte stat, Byte Kind, Byte Rssi, Byte _mernokAssetGroup)
        {
            this._UID = _UID;
            TAGType = _TAGType;
            voltage = _voltage;
            _LastSeen = DateTime.Now.AddSeconds(0 - (double)age);
            times_seen = 0;
            Ping_Interval = default_ping_Time;
            dist = _dist;
            CAN_slave_ID = Can_ID;
            VID = Vehc_ID;
            statusB = stat;
            this.Kind = (_kind)Kind;
            rssi = Rssi;
        }
        #endregion

        #region Functions

        /// <summary>
        /// This updates all properties of all tags in the list. can be processor intensive, so dont call too often. 
        /// </summary>
        public void updateAll()
        {
            OnPropertyChanged(null);
        }

        public void parse_message_into_TAG(byte[] data)
        {
            int PacketSize = 39;
            int GPS_PacketSize = 64;
            SetField(ref _UID, parse_message_UID(data), "UID");

            if (data.Length == 8)
            {
                if (data[0] == 'p') // shortened tag poll response
                {
                    SetField(ref TAGType, data[5], "Type");
                    _LastSeen = DateTime.Now.AddSeconds(0 - (double)data[6]);
                    Kind = (TAG._kind)data[7];
                    //LF_Data = new LF_data();
                }
                else if (data[0] == 1) // shortened auto-forward rf ping
                {
                    SetField(ref TAGType, data[5], "Type");
                    RSSI = data[6];
                    _LastSeen = DateTime.Now;
                    Kind = (TAG._kind)data[7];
                    //LF_Data = new LF_data();
                }
                else if (data[0] == 2) // shortened autoforward lf response
                {
                    SetField(ref TAGType, data[5], "Type");
                    var Last_LF = new LF_data();
                    Last_LF.RSSI = (sbyte)data[6];
                    Last_LF.Last_Seen = DateTime.Now;
                    Last_LF.SlaveID = (byte)(data[7] & 0x0F);
                    LF_Data = Last_LF;
                    _LastSeen = DateTime.Now;
                    // ---- Removed _kind update V12 ---- 
                   // kind = TAG._kind.Pulse;
                }
            }
            else if ((data.Length >= PacketSize) && ((data[0] == 'P')||(data[0] == 'A')))
            {
                
                SetField(ref TAGType, data[5], "Type");
                SetField(ref voltage, data[6], "Voltage");

                _LastSeen = DateTime.Now.AddSeconds(0 - (double)data[7]);
                Kind = (TAG._kind)data[8];

                SetField(ref statusB, data[9], "Status");

                var Last_LF = new LF_data();
                //var Last_GPS = new _GPS_Data();

                //VID = BitConverter.ToUInt32(data, 10);
                SetField(ref VID, BitConverter.ToUInt32(data, 10), "Vehicle_ID");

                CAN_slave_ID = data[14];

                Last_LF.VehicleID = BitConverter.ToUInt16(data, 15);
                Last_LF.SlaveID = data[17];

                RSSI = data[18];

                Last_LF.RSSI = (sbyte)data[19];

                //if (Kind == TAG._kind.Ranging)
                //{
                //    dist = BitConverter.ToUInt16(data, 18);
                //    Last_LF.distance = dist;
                //    Last_LF.is_range = true;
                //    RSSI = 0;
                //}

                //TAGGroup = data[21];
                SetField(ref TAGGroup, data[20], "Group");

                if (data[21] == 0xFF)
                    Last_LF.Last_Seen = DateTime.MinValue;
                else
                    Last_LF.Last_Seen = DateTime.Now.AddSeconds(0 - data[21]);
                LF_Data = Last_LF;

                FirmRev = data[22];

                ProductID = (_productID)data[23];

                //_MantagAck = data[24];
                SetField(ref _MantagAck, data[24]&0x01, "MantagAck");
                //data24 = reverse status byte
                SetField(ref _reverse, data[25] & 0x01, "Reverse");
                //data25 = vehicle length
                //data26 = Vehicle width
                //data27 = stopping distance
                SetField(ref Speed_km_h, BitConverter.ToUInt32(data, 29), "Speed");

                int year, month, day, hour, min, sec;
                //int.TryParse((data[38]).ToString(),out year);
                //int.TryParse((data[37]).ToString(), out month);
                //int.TryParse((data[36]).ToString(), out day);
                //int.TryParse((data[35]).ToString(), out hour);
                //int.TryParse((data[34]).ToString(), out min);
                //int.TryParse((data[33]).ToString(), out sec);

                year = data[38];
                month = data[37];
                day = data[36];
                hour = data[35];
                min = data[34];
                sec = data[33];


                if (data[37] > 0 && data[36] > 0)
                {
                    DateTime = new DateTime(year + 2000, month, day, hour, min, sec);
                }
                else
                    DateTime = DateTime.MinValue;


                if ((Kind == _kind.Pulse_GPS)&&(data.Length>63))
                {

                    SetField(ref Longitude_deg, BitConverter.ToInt32(data, 39), "Longitude");
                    SetField(ref Latitude_deg, BitConverter.ToInt32(data, 43), "Latitude");
                    SetField(ref Vertical_Acc, BitConverter.ToUInt32(data, 47), "Vertical_Accuracy");
                    SetField(ref Horizontal_Acc, BitConverter.ToUInt32(data, 51), "Horizontal_Accuracy");
                    //SetField(ref Speed_km_h, BitConverter.ToInt32(data, 28), "Speed");
                    SetField(ref Vehicle_heading_, BitConverter.ToUInt32(data,55), "Heading_v");
                    
                    //Last_GPS.HeadingVehicle = BitConverter.ToInt32(data, 48);
                    FixType = (TAG._GPS_FixType)data[59];
                    SetField(ref FixAge, data[60], "Fix_Age");
                    SetField(ref Sea_Level_m, BitConverter.ToInt32(data, 61) / 1000, "Sea_Level");


                    if ((data.Length > GPS_PacketSize) &&(data.Length<79))
                        Tag_Name = Encoding.ASCII.GetString(data, GPS_PacketSize, Math.Min(GPS_PacketSize, data.Length - GPS_PacketSize));
                    else
                        Tag_Name = "";
                }
                else if(Kind != _kind.Pulse_GPS)
                {
                    FixAge = 255;
                    SetField(ref Longitude_deg, 0, "Longitude");
                    SetField(ref Latitude_deg, 0, "Latitude");
                    SetField(ref Vertical_Acc, 0, "Vertical_Accuracy");
                    SetField(ref Horizontal_Acc,0, "Horizontal_Accuracy");
                    //SetField(ref Speed_km_h, BitConverter.ToInt32(data, 28), "Speed");
                    SetField(ref Vehicle_heading_, 0, "Heading_v");

                    //Last_GPS.HeadingVehicle = BitConverter.ToInt32(data, 48);
                    FixType = (TAG._GPS_FixType)0;
                    SetField(ref Sea_Level_m, 0, "Sea_Level");

                    if (data.Length > PacketSize)
                        Tag_Name = Encoding.ASCII.GetString(data, PacketSize, Math.Min(PacketSize, data.Length - PacketSize));
                    else
                        Tag_Name = "";
                }
                
            }
            times_seen += 1;
            OnPropertyChanged("Last_Seen");
        }



        public void update_last_seen()
        {
            OnPropertyChanged("Last_Seen");
            OnPropertyChanged("LF_Data");
        }

        #endregion
    }
}
