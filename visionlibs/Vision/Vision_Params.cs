using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Vision_Libs.Vision;
//using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using FlagsEnumTypeConverter;
using Vision.Parameter;
using Vision;
using System.Text.RegularExpressions;
using System.Windows;
using MernokAssets;
using Vision_Libs.Utility;

namespace Vision_Libs.Params
{
    public class VisionParams//: CollectionBase//, ICustomTypeDescriptor
    {
        public int firmware_rev = 1;
        public int firmware_subrev = 0;
        public static int Mernok_assetfile_rev = 0;
        public uint UID = 0;
        public string PCB_ID = "unknown" ;

        

        // 2017 - 09 - 06
        public uint kind = 0;

        public enum adr : int
        {
            firmware = 0x08,      	// byte indicating the firmware revision. used as sanity check   
            tag_type = 0x0c,      	// byte specifying the tag type.                                 
            rf_power = 0x10,      	// byte specifying RF output power. 0-7.                                
            slave_id = 0x14,		// byte CAN ID.only lower 4 bits are used for pod_ID in the LF tx message
            vehic_id = 0x18,		// short vehicle ID. 
            uartBaud = 0x1c,		// int uart baud in hz
            can_baud = 0x20,		// int baud rate in hz
            interval = 0x24,		// int RF transmission interval (ms)
            activity = 0x28,      	// binary values indicating the tag's activities: see _Transp_activities.
            rf_chanl = 0x2c,		// byte adjusts the RF frequency a little bit to allow certain tags to operate independently
            lfPeriod = 0x30,		// int LF_TX interval to send LF packets. 
            lf_power = 0x34,      	// byte allowing LF power to be adjusted. 0-100
            lf_hertz = 0x38,      	// allow the device to set the LF frequency. in hz
            max_dist = 0x3c,      	// int max dist to pass to master. not used by Pulse
            name_str = 0x40,		// string max 20 byte string name of the device. used in certain circumstances to indicate presence.  
            ack_time = 0x54,		// short number of milliseconds to stop sending warning after acknowledge button is pressed (mantag)
            ack_intv = 0x58,		// byte interval between LF warnings once acknowledged
            lf_filtr = 0x5c,		// int time constant of LF zone max rssi filter.
            vchrgmin = 0x60,		// int minimum source voltage from which to charge. 
            rssiCrit = 0x64,		// byte LF rssi critical Threshold.
            rssiWarn = 0x68,		// byte LF rssi warning Threshold.
            rssiPres = 0x6c,		// byte LF presence threshold. 	
            usrParam = 0x70,		// byte user assignable parameter for future use. 
            antOfset = 0x74,		// byte antenna offset value used for ranging to calibrate distance.
            //gpsCapab = 0x78,
            firmware_sub = 0x78,                //byte indicating the firmware sub revision
            CAN_heartbeat_time = 0x7c,          //32 - byte for CAN heatbeat monitoring timeout
            CAN_revert = 0x80,                  //byte to change tag type when heartbeat monitoring times out
            Mernok_Asset_list_rev = 0x84,       //byte used to show the version of the mernok asset file last read
            Mernok_Asset_Length = 0x88,
            Mernok_Asset_Width = 0x8C,        

            Mernok_Asset_Group = 0x100,          //byte that determines the Mernok Asset Group number
            

            //reserved space for Mernok Asset info:
            //Mernok_Asset_Groups = 0xFFF,

        };

        public enum Output_Powers
        {
            [Description("-30 dB")]
            min30 = 0,
            [Description("-20 dB")]
            min20 = 1,
            [Description("-15 dB")]
            min15 = 2,
            [Description("-10 dB")]
            min10 = 3,
            [Description("0 dB")]
            zero = 4,
            [Description("5 dB")]
            five = 5,
            [Description("7 dB")]
            seven = 6,
            [Description("10 dB")]
            ten = 7
        }

        public List<Parameter> Params;

        public List<Parameter> StatusVals;

        public VisionParams()
        {
            Params = new List<Parameter>();
            StatusVals = new List<Parameter>();
            load_zeros();
        }

        //public string Tag_Config_Name()
        //{
        //    string temp_tag_name = "";
        //    byte[] temp_name = new byte[15];
        //    for (int i = 0; i < 20; i++)
        //    {
        //        temp_name =  BitConverter.GetBytes(this[adr.name_str + i].Value);
        //        temp_tag_name = System.Text.ASCIIEncoding.ASCII.GetString(temp_name);
        //    }                      

        //    return temp_tag_name;
        //}
                
        public void load_standard()
        {
            Params.Clear();
            Params.Add(new Parameter((int)adr.firmware, Parameter.Type.TypeByte, (UInt32)firmware_rev, "Firmware rev"));
            Params.Add(new Parameter((int)adr.tag_type, Parameter.Type.TypeByte, (UInt32)TagType.Loco, "Tag type"));
            Params.Add(new Parameter((int)adr.rf_power, Parameter.Type.TypeByte, 7, "RF output power"));
            Params.Add(new Parameter((int)adr.slave_id, Parameter.Type.TypeByte, 10, "CAN slave ID"));
            Params.Add(new Parameter((int)adr.vehic_id, Parameter.Type.TypeInt, 0x1234, "Vehicle ID"));
            Params.Add(new Parameter((int)adr.uartBaud, Parameter.Type.TypeInt, 9600, "Serial buadrate"));
            Params.Add(new Parameter((int)adr.can_baud, Parameter.Type.TypeInt, 250000, "CAN baudrate"));
            Params.Add(new Parameter((int)adr.interval, Parameter.Type.TypeInt, 2000, "RF ping interval"));
            Params.Add(new Parameter((int)adr.activity, Parameter.Type.TypeInt, 0, "Tag functions"));
//             Params.Add(new Parameter((int)adr.rf_chanl, Parameter.Type.TypeByte, 0, "RF channel select"));
//             Params.Add(new Parameter((int)adr.max_dist, Parameter.Type.TypeInt, 10000, "Max dist passed"));
            Params.Add(new Parameter((int)adr.lfPeriod, Parameter.Type.TypeInt, 800, "LF ping interval"));
            Params.Add(new Parameter((int)adr.lf_power, Parameter.Type.TypeByte, 100, "LF output power"));
            Params.Add(new Parameter((int)adr.lf_hertz, Parameter.Type.TypeInt, 125000, "LF frequency"));
            Params.Add(new Parameter((int)adr.name_str, "no name", "Tag name"));
            Params.Add(new Parameter((int)adr.ack_time, Parameter.Type.TypeInt, 60000, "Ack timeout"));
            Params.Add(new Parameter((int)adr.ack_intv, Parameter.Type.TypeByte, 4, "Ack'ed warn interval"));
            Params.Add(new Parameter((int)adr.lf_filtr, Parameter.Type.TypeInt, 5500, "Max LF timeout"));
            Params.Add(new Parameter((int)adr.vchrgmin, Parameter.Type.TypeInt, 4000, "min V-in charge"));
            Params.Add(new Parameter((int)adr.rssiCrit, Parameter.Type.TypeByte, 7, "LF critical threshold"));
            Params.Add(new Parameter((int)adr.rssiWarn, Parameter.Type.TypeByte, 3, "LF warning threshold"));
            Params.Add(new Parameter((int)adr.rssiPres, Parameter.Type.TypeByte, 0, "LF presence threshold"));
            //Params.Add(new Parameter((int)adr.usrParam, Parameter.Type.TypeByte, 0, "User settable value for future use"));
            Params.Add(new Parameter((int)adr.antOfset, Parameter.Type.TypeByte, 0, "Antenna offset used for range calibration"));
            //Params.Add(new Parameter((int)adr.gpsCapab, Parameter.Type.TypeByte, 0, "If the Pulse400 must send broadcast coordinates"));

            StatusVals.Add(new Parameter(9, Parameter.Type.TypeByte, 0, "Board ID"));
            StatusVals.Add(new Parameter(10, Parameter.Type.TypeInt, 0, "LF Coil frequency X"));
            StatusVals.Add(new Parameter(11, Parameter.Type.TypeInt, 0, "LF Coil frequency Y"));
            StatusVals.Add(new Parameter(12, Parameter.Type.TypeInt, 0, "LF Coil frequency Z"));

            Params.Add(new Parameter((int)adr.firmware_sub, Parameter.Type.TypeByte, (UInt32)firmware_subrev, "Firmware sub rev"));
            Params.Add(new Parameter((int)adr.CAN_heartbeat_time, Parameter.Type.TypeByte,10, "CAN heartbeat time"));
            Params.Add(new Parameter((int)adr.CAN_revert, Parameter.Type.TypeByte, (UInt32)TagType.Loco, "CAN revert type"));
            Params.Add(new Parameter((int)adr.Mernok_Asset_list_rev, Parameter.Type.TypeByte, 0, "Mernok Asset Group file rev"));
            Params.Add(new Parameter((int)adr.Mernok_Asset_Length, Parameter.Type.TypeByte, 0, "Mernok Asset Length"));
            Params.Add(new Parameter((int)adr.Mernok_Asset_Width, Parameter.Type.TypeByte, 0, "Mernok Asset Width"));

        }

        void load_zeros()
        {
            Params.Clear();
            Params.Add(new Parameter((int)adr.firmware, Parameter.Type.TypeByte, 0, "Firmware rev"));
            Params.Add(new Parameter((int)adr.tag_type, Parameter.Type.TypeByte, 0, "Tag type"));
            Params.Add(new Parameter((int)adr.rf_power, Parameter.Type.TypeByte, 0, "RF output power"));
            Params.Add(new Parameter((int)adr.slave_id, Parameter.Type.TypeByte, 0, "CAN slave ID"));
            Params.Add(new Parameter((int)adr.vehic_id, Parameter.Type.TypeInt, 0, "Vehicle ID"));
            Params.Add(new Parameter((int)adr.uartBaud, Parameter.Type.TypeInt, 0, "Serial buadrate"));
            Params.Add(new Parameter((int)adr.can_baud, Parameter.Type.TypeInt, 0, "CAN baudrate"));
            Params.Add(new Parameter((int)adr.interval, Parameter.Type.TypeInt, 0, "RF ping interval"));
            Params.Add(new Parameter((int)adr.activity, Parameter.Type.TypeInt, 0, "Tag functions"));
//             Params.Add(new Parameter((int)adr.rf_chanl, Parameter.Type.TypeByte, 0, "RF channel select"));
//             Params.Add(new Parameter((int)adr.max_dist, Parameter.Type.TypeInt, 0, "Max dist passed"));
            Params.Add(new Parameter((int)adr.lfPeriod, Parameter.Type.TypeInt, 0, "LF ping interval"));
            Params.Add(new Parameter((int)adr.lf_power, Parameter.Type.TypeByte, 0, "LF output power"));
            Params.Add(new Parameter((int)adr.lf_hertz, Parameter.Type.TypeInt, 0, "LF frequency"));
            Params.Add(new Parameter((int)adr.name_str, "", "Tag name"));
            Params.Add(new Parameter((int)adr.ack_time, Parameter.Type.TypeInt, 0, "Ack timeout"));
            Params.Add(new Parameter((int)adr.ack_intv, Parameter.Type.TypeByte, 0, "Ack'ed warn interval"));
            Params.Add(new Parameter((int)adr.lf_filtr, Parameter.Type.TypeInt, 0, "Max LF timeout"));
            Params.Add(new Parameter((int)adr.vchrgmin, Parameter.Type.TypeInt, 0, "min V-in charge"));
            Params.Add(new Parameter((int)adr.rssiCrit, Parameter.Type.TypeByte, 0, "LF critical threshold"));
            Params.Add(new Parameter((int)adr.rssiWarn, Parameter.Type.TypeByte, 0, "LF warning threshold"));
            Params.Add(new Parameter((int)adr.rssiPres, Parameter.Type.TypeByte, 0, "LF presence threshold"));
            //Params.Add(new Parameter((int)adr.usrParam, Parameter.Type.TypeByte, 0, "User settable value for future use"));
            Params.Add(new Parameter((int)adr.antOfset, Parameter.Type.TypeByte, 0, "Antenna offset used for range calibration"));
            //Params.Add(new Parameter((int)adr.gpsCapab, Parameter.Type.TypeByte, 0, "If the Pulse400 must broadcast coordinates"));
            Params.Add(new Parameter((int)adr.firmware_sub, Parameter.Type.TypeByte, 0, "Firmware sub rev"));
            Params.Add(new Parameter((int)adr.CAN_heartbeat_time, Parameter.Type.TypeByte, 0, "CAN heartbeat time"));
            Params.Add(new Parameter((int)adr.CAN_revert, Parameter.Type.TypeByte, 0, "CAN revert type"));
            Params.Add(new Parameter((int)adr.Mernok_Asset_list_rev, Parameter.Type.TypeByte, (UInt32)Mernok_assetfile_rev, "Mernok Asset Group file rev"));
            Params.Add(new Parameter((int)adr.Mernok_Asset_Length, Parameter.Type.TypeByte, 0, "Mernok Asset Length"));
            Params.Add(new Parameter((int)adr.Mernok_Asset_Width, Parameter.Type.TypeByte, 0, "Mernok Asset Width"));


            StatusVals.Add(new Parameter(10, Parameter.Type.TypeInt, 0, "LF Coil frequency X"));
            StatusVals.Add(new Parameter(11, Parameter.Type.TypeInt, 0, "LF Coil frequency Y"));
            StatusVals.Add(new Parameter(12, Parameter.Type.TypeInt, 0, "LF Coil frequency Z"));

            
        }

        #region Fields

        #endregion

        #region Properties

        //firmware = 0x08,
        [Category("Overview")]
        [DisplayName("Firmware revision")]
        [Description("The Tag's Firmware revision (Read only)")]
        public Byte Firmware_Rev
        {
            get
            {
                return (Byte)this[adr.firmware].Value;
            }
        }

        [Category("Overview")]
        [DisplayName("Firmware sub revision")]
        [Description("The Tag's Firmware sub revision (Read only)")]
        public Byte Firmware_subrev
        {
            get
            {
                if (this[adr.firmware_sub].Value != 255)
                    return (Byte)this[adr.firmware_sub].Value;
                else
                    return 0;
            }
        }

        [Category("Overview")]
        [DisplayName("Mernok Asset file revision")]
        [Description("The Mernok Asset file revision revision (Read only)")]
        public UInt16 Mernok_Assetfile_ref
        {
            get
            {
                //if ((this[adr.Mernok_Asset_list_rev].Value != 0)&&(this[adr.Mernok_Asset_list_rev].Value != 255))
                    return (UInt16)this[adr.Mernok_Asset_list_rev].Value;
                //else if(this[adr.Mernok_Asset_list_rev].Value == 255)
                //{
                //    //MessageBox.Show("No mernok asset file loaded");
                //    return 0;
                //}
                //else
                //{
                //    this[adr.Mernok_Asset_list_rev].Value = (UInt16)Mernok_assetfile_rev;
                //    return (UInt16)Mernok_assetfile_rev;
                //}
                    
            }
        }



        #region tag activites
        [Category("TAG Function Flags")]
        [DisplayName("Tag Enable")]
        [Description("Setting this to false disables all tag functionality (storage mode)")]
        public bool tag_enable { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.tag_enable); } set { ActsSet(TagActivities.tag_enable, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Broadcast ID")]
        [Description("Enable broadcast periodic RF message")]
        public bool broadcast_ID { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.broadcast_ID); } set { ActsSet(TagActivities.broadcast_ID, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Heartbeat")]
        [Description("Tag sends a periodic heartbeat message to master (CAN/Serial/USB)")]
        public bool heartbeat { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.heartbeat); } set { ActsSet(TagActivities.heartbeat, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Transmit LF Field")]
        [Description("Tag will periodically transmit an LF field. Only possible with transmit coil, e.g. PULSE500")]
        public bool LF_TX { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.LF_TX); } set { ActsSet(TagActivities.LF_TX, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("LF response")]
        [Description("Tag will send an RF message whenever LF field is detected")]
        public bool LF_response { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.LF_response); } set { ActsSet(TagActivities.LF_response, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Enable RF Receiver")]
        [Description("Enabled tag RF receiver. This device will receive and interpret all PULSE RF (Reduces battery life)")]
        public bool receive_RF { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.receive_RF); } set { ActsSet(TagActivities.receive_RF, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Always on (disable sleep)")]
        [Description("Forcefully keeps the tag awake (Reduces battery life)")]
        public bool Always_on { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.Always_on); } set { ActsSet(TagActivities.Always_on, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Accept Data")]
        [Description("Used with remote data forwarding from one reader to another. If this device receives a data packet (over RF), it will be forwarded to master (CAN/Serial/USB)")]
        public bool accept_data { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.accept_data); } set { ActsSet(TagActivities.accept_data, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("AudioVisual LF indication")]
        [Description("If true, the tag will pulse LEDs, buzzer/vibrator when LF field is detected. Note that only PULSE1XX devices have buzzer/vibrator")]
        public bool output_critical { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.output_critical); } set { ActsSet(TagActivities.output_critical, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("CAN Termination")]
        [Description("Will enable 120R CAN bus termination on the unit")]
        public bool CAN_terminated { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.CAN_terminated); } set { ActsSet(TagActivities.CAN_terminated, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Forward All Tag RF")]
        [Description("Update master whenever ANY tag data is received over RF")]
        public bool forward_RF { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.forward_RF); } set { ActsSet(TagActivities.forward_RF, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Sync LF TX")]
        [Description("Tag will send a synchronize packet (via CAN and RF) whenever LF transmits a field. Prevents LF TX tags from interfering with each other. ** Note the RF Receiver needs to be enabled for other LF transmitters to receive the RF sync message")]
        public bool CAN_sync { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.CAN_sync); } set { ActsSet(TagActivities.CAN_sync, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Get range all")]
        [Description("Send range requests to all ranger devices detected")]
        public bool get_range_all { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.get_range_all); } set { ActsSet(TagActivities.get_range_all, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Get range select")]
        [Description("Send range requests to all ranger devices that match the type filter")]
        public bool get_range_select { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.get_range_select); } set { ActsSet(TagActivities.get_range_select, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Forward distance/LF info")]
        [Description("Update master whenever tag data is received over RF with distance information. LF indications for PULSE, distance info for Ranger")]
        public bool forward_dists { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.forward_dists); } set { ActsSet(TagActivities.forward_dists, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Shortened Tag data")]
        [Description("When sending forwarded tag data to master, a reduced length (8 byte) tag message is used (useful for CAN bus congestion)")]
        public bool use_shortened_fw { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.use_shortened_fw); } set { ActsSet(TagActivities.use_shortened_fw, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Name sending")]
        [Description("Sends the tag's name string in every other RF broadcast message")]
        public bool send_name { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.send_name); } set { ActsSet(TagActivities.send_name, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Legacy PDS broadcast")]
        [Description("Send Legacy PDS messages instead of regular PULSE ID broadcast. Useful when new/old vehicles operate together")]
        public bool legacy_PDS { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.legacy_PDS); } set { ActsSet(TagActivities.legacy_PDS, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Forward own LF")]
        [Description("Send a info to master (CAN/Serial/USB) when LF field is detected")]
        public bool forward_own_lf { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.forward_own_lf); } set { ActsSet(TagActivities.forward_own_lf, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Disable exclusion")]
        [Description("If true, the tag will send LF response RF messages, even when an exclusion field is detected. Useful for vehicle tags close to cabin")]
        public bool disable_exclusion { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.disable_exclusion); } set { ActsSet(TagActivities.disable_exclusion, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("Disable LF CRC")]
        [Description("If true, the tag will send LF response when LF field is detected, regardless of whether the LF field's CRC check failed. useful safety critical scenarios where LF TX info (VID/SID) is not important")]
        public bool disable_LF_CRC { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.disable_LF_CRC); } set { ActsSet(TagActivities.disable_LF_CRC, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("GPS Capable")]
        [Description("If true, the tag will include GPS cooridinates in the RF broadcast")]
        public bool GPS_capable { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.GPS_Capable); } set { ActsSet(TagActivities.GPS_Capable, value); } }
        [Category("TAG Function Flags")]
        [DisplayName("CAN Heartbeat monitoring")]
        [Description("If true, the tag will monitor heartbeats over CAN Bus, to see if communication with host is still active, if the heartbeat monitoring fails the tag will change to a default tag type.")]
        public bool Heartbeat_monitor { get { return ((TagActivities)this[adr.activity].Value).HasFlag(TagActivities.Heartbeat_monitor); } set { ActsSet(TagActivities.Heartbeat_monitor, value); } }


        private void ActsSet(TagActivities flag, bool value)
        {
            if (value == true)
                Activity |= flag;
            else
                Activity &= ~flag;
        }


        #endregion



        //tag_type = 0x0c,   
        //[TypeConverter(typeof(Jcl.Util.EnumDescConverter))]
        //[Category("Tag Settings")]
        //[DisplayName("Tag Type")]
        //[Description("The type indicated by the Tag. Several types have been defined, but any 8 bit value can be used.")]
        //public TagType Type
        //{
        //    get { return (TagType)this[adr.tag_type].Value; }
        //    set { this[adr.tag_type].Value = (UInt32)value; }
        //}
        public uint Type;

        [Category("Tag Settings")]
        [DisplayName("Tag Type")]
        [Description("The type indicated by the Tag. Several types have been defined, but any 8 bit value can be used.")]
        [TypeConverter(typeof(AssetListConverter))]
        public String AssetType
        {
            get { if((int)this[adr.tag_type].Value == 255 || (int)this[adr.tag_type].Value ==0 )
                {
                    return TagTypesL.MenokAssetTypeName[(int)TagType.Loco-1];
                }
            else
                return TagTypesL.MenokAssetTypeName[(int)this[adr.tag_type].Value-1]; }
            set
            {
                this[adr.tag_type].Value = (uint)(TagTypesL.MernokAssetType[TagTypesL.MenokAssetTypeName.IndexOf(value)].Type);
                Type = this[adr.tag_type].Value - 1;
            }
        }

        //rf_power = 0x10, 
        [TypeConverter(typeof(Jcl.Util.EnumDescConverter))]
        [Category("Tag Settings")]
        [DisplayName("Tag RF output power")]
        [Description("The transmit power of the Tag. Maximum is 10dBm which gives maximum range.")]
        public Output_Powers Power
        {
            get { return (Output_Powers)(this[adr.rf_power].Value); }
            set { this[adr.rf_power].Value = (UInt32)value; }
        }
         //slave_id = 0x14,	
         //[TypeConverter(typeof(Jcl.Util.EnumDescConverter))]
        [Category("Tag Settings")]
        [DisplayName("Slave ID")]
        [Description("The Tag's CAN device ID.")]
        public Byte Slave_ID
        {
            get
            {
                return (Byte)this[adr.slave_id].Value;
            }
            set
            {
                value = Math.Max(value, (Byte)1);
                this[adr.slave_id].Value = value;
            }
        }
        //vehic_id = 0x18,	
        [Category("Tag Settings")]
        [DisplayName("Vehicle ID")]
        [Description("The vehicle this Tag is associated with. Should be managed by the vehicle controller.")]
        public UInt32 VehicleID
        {
            get
            {
                return this[adr.vehic_id].Value;
            }
            set
            {
                this[adr.vehic_id].Value = value;
            }
        }
        //uartBaud = 0x1c,	
        [Category("Tag Settings")]
        [DisplayName("UART baud rate")]
        [Description("The serial interface baud rate.")]
        public uint UART_baud
        {
            get
            {
                return this[adr.uartBaud].Value;
            }
            set
            {
                if (value > 500000) value = 500000;
                if (value < 9600) value = 9600;
                this[adr.uartBaud].Value = value;
            }
        }
        //can_baud = 0x20,
        [Category("Tag Settings")]
        [DisplayName("CAN baud rate")]
        [Description("The CAN bus interface baud rate. this should be a fixed value of either: 250k, 125k, 100k, 50k, 20k, 10k.")]
        public uint CAN_baud
        {
            get
            {
                return (this[adr.can_baud].Value);
            }
            set
            {
                if (value > 500000) value = 500000;
                if (value < 10000) value = 10000;
                this[adr.can_baud].Value = value;
            }
        }

        [Category("Tag Settings")]
        [DisplayName("CAN heartbeat timeout (s)")]
        [Description("The CAN heartbeat is lost, this will be the time before a timeout is called")]
        public uint CAN_heatbeat_time
        {
            get
            {
                return (this[adr.CAN_heartbeat_time].Value);
            }
            set
            {
                if (value > 255) value = 255;
                if (value < 0) value = 0;
                this[adr.CAN_heartbeat_time].Value = value;
            }
        }

        //[TypeConverter(typeof(Jcl.Util.EnumDescConverter))]
        //[Category("Tag Settings")]
        //[DisplayName("CAN Revert Type")]
        //[Description("The type indicated by the Tag for the CAN device after timeout has triggered on heartbeat. Several types have been defined, but any 8 bit value can be used.")]
        //public TagType CANrevertType
        //{
        //    get { return (TagType)this[adr.CAN_revert].Value; }
        //    set { this[adr.CAN_revert].Value = (UInt32)value; }
        //}

        [Category("Tag Settings")]
        [DisplayName("CAN Revert Type")]
        [Description("The type indicated by the Tag for the CAN device after timeout has triggered on heartbeat. Several types have been defined, but any 8 bit value can be used.")]
        [TypeConverter(typeof(AssetListConverter))]
        public String CANrevertType
        {
            get
            {
                if (Firmware_Rev > 13) return TagTypesL.MenokAssetTypeName[(int)this[adr.CAN_revert].Value - 1];
                else return "N/A";
            }
            set
            {
                this[adr.CAN_revert].Value = TagTypesL.MernokAssetType[TagTypesL.MenokAssetTypeName.IndexOf(value)].Type;
            }
        }

        //interval = 0x24,	
        // [TypeConverter(typeof(Jcl.Util.EnumDescConverter))]
        [Category("Tag Settings")]
        [DisplayName("Transmit interval (ms)")]
        [Description("The amount of time between RF packets in ms. Also used for heartbeat and the CAN device changed to a fail to safe type")]
        public UInt16 Interval
        {
            get { return (UInt16)this[adr.interval].Value; }
            set
            {
                value = Math.Max(value, (UInt16)50);
                this[adr.interval].Value = value;
            }
        }

        [Category("Overview")]
        [DisplayName("Activities")]
        [Description("Adjust these to determine which tasks the TAG performs")]
        public TagActivities Activity
        {
            get { return (TagActivities)this[adr.activity].Value; }
            set { this[adr.activity].Value = (uint)value; }
        }

        [Category("Tag Settings")]
        [DisplayName("LF Transmit interval (ms)")]
        [Description("The amount of time between LF bursts in ms.")]
        public UInt16 LF_interval_Time
        {
            get { return (UInt16)Params.Find(x => x.address == (int)adr.lfPeriod).Value; }
            set
            {
                value = Math.Max(value, (UInt16)500);
                value = Math.Min(value, (UInt16)2000);
                Params.Find(x => x.address == (int)adr.lfPeriod).Value = value;
            }
        }
        //lf_power = 0x34,   
        [Category("Tag Settings")]
        [DisplayName("LF Power")]
        [Description("The output power for the LF transmitter")]
        public Byte LF_Power
        {
            get { return (Byte)this[adr.lf_power].Value; }
            set
            {
                value = Math.Min(value, (Byte)100);
                value = Math.Max(value, (Byte)0);

                this[adr.lf_power].Value = value;
            }
        }
        //lf_hertz = 0x38,   
        [Category("Tag Settings")]
        [DisplayName("LF Frequency (Hz)")]
        [Description("The frequency for the LF transmitter/receiver")]
        public uint LF_Hertz
        {
            get { return this[adr.lf_hertz].Value; }
            set
            {
                value = Math.Min(value, 150000);
                value = Math.Max(value, 30000);

                this[adr.lf_hertz].Value = value;
            }
        }
        //max_dist = 0x3c,    
        //name_str = 0x40,	
        [Category("Tag Settings")]
        [DisplayName("Tag Name")]
        [Description("The string reported for the tag name")]
        public String Tag_Name
        {
            //get { return Tag_Config_Name(); }
            get { return this[adr.name_str].Value_str; }
            set
            {
                //string name = this[adr.name_str].Value_str;
                string name = value;
                string[] name2;
                Regex regexItem = new Regex(@"[^A-Z0-9 _]");
                if (name.Length <= 15)
                {
                    name = name.ToUpper();
            }
                else
                {
                name = name.Substring(0, 15).ToUpper();
                MessageBox.Show("Tag name my not exceed a length of 115");
            }
                    

                if(!regexItem.IsMatch(name))
                {
                    value = name;
                }
                else
                {
                    MessageBox.Show("Tag name my not not contain any special charcters");
                    name2 = regexItem.Split(name);
                    name = "";
                    for (int i = 0; i < name2.Length; i++)
                    {
                        if(name2[i]!="")
                        {
                            name = name + name2[i];
                        }
                        
                    }
                    //name = regexItem.Replace(name, "$");
                    value = name;
                }

                //value = value.Length <= 15 ? value.ToUpper() : value.Substring(0, 15).ToUpper();
                //value = regexItem.IsMatch(value) ? value : regexItem.Replace(value, "");
                this[adr.name_str].Value_str = value;
            }
        }
        //ack_time = 0x54,	
        [Category("Tag Settings")]
        [DisplayName("Acknowledge timeout (ms)")]
        [Description("The time a tag remains silent after pushing acknowledge button")]
        public uint Acknowledge_timeout
        {
            get { return this[adr.ack_time].Value; }
            set
            {
                value = Math.Min(value, 500000);
                value = Math.Max(value, 5000);
                this[adr.ack_time].Value = value;
            }
        }
        //ack_intv = 0x58,	
        [Category("Tag Settings")]
        [DisplayName("Acknowledge flash interval")]
        [Description("how many <transmit interval> periods pass between flashes for an acknowledged tag")]
        public uint Acknowledge_interval
        {
            get { return this[adr.ack_intv].Value; }
            set
            {
                value = Math.Min(value, 100);
                value = Math.Max(value, 1);
                this[adr.ack_intv].Value = value;
            }
        }

        //lf_filtr = 0x5c,	
        //vchrgmin = 0x60,	
        //rssiCrit = 0x64,	
        [Category("Tag Settings")]
        [DisplayName("LF RSSI Critical")]
        [Description("The lowest LF value to take as critical")]
        public Byte LF_RSSI_Crit
        {
            get { return (Byte)this[adr.rssiCrit].Value; }
            set
            {
                value = Math.Min(value, (Byte)31);
                value = Math.Max(value, (Byte)0);

                this[adr.rssiCrit].Value = value;
            }
        }
        //rssiWarn = 0x68,	
        [Category("Tag Settings")]
        [DisplayName("LF RSSI Warn")]
        [Description("The lowest LF value to take as warning")]
        public Byte LF_RSSI_Warn
        {
            get { return (Byte)this[adr.rssiWarn].Value; }
            set
            {
                value = Math.Min(value, (Byte)31);
                value = Math.Max(value, (Byte)0);

                this[adr.rssiWarn].Value = value;
            }
        }
         //rssiPres = 0x6c,	
        [Category("Tag Settings")]
        [DisplayName("LF RSSI Presence")]
        [Description("The lowest LF value to take as present")]
        public Byte LF_RSSI_Pres
        {
            get { return (Byte)this[adr.rssiPres].Value; }
            set
            {
                value = Math.Min(value, (Byte)31);
                value = Math.Max(value, (Byte)0);

                this[adr.rssiPres].Value = value;
            }
        }
        //usrParam = 0x70
        //antOfset = 0x74,	
        [Category("Tag Settings")]
        [DisplayName("Antenna length offset")]
        [Description("For ranging only. Used to calibrate distance values to compensate for antenna cable length (in 0.1m increments)")]
        public Byte Ant_Offset
        {
            get { return (Byte)this[adr.antOfset].Value; }
            set
            {
                value = Math.Min(value, (Byte)255);
                value = Math.Max(value, (Byte)0);

                this[adr.antOfset].Value = value;
            }
        }

        [Category("Tag Settings")]
        [DisplayName("Mernok Asset Length")]
        [Description("The commander firmware uses this, the width and the stopping distance for the Level 9 calculations (in 0.1m increments)")]
        public UInt32 Mernok_Asset_Length_
        {
            get
            {
                return this[adr.Mernok_Asset_Length].Value;
            }
            set
            {
                value = Math.Min(value, (Byte)255);
                value = Math.Max(value, (Byte)0);
                this[adr.Mernok_Asset_Length].Value = value;
            }
        }

        [Category("Tag Settings")]
        [DisplayName("Mernok Asset Width")]
        [Description("The commander firmware uses this, the length and the stopping distance for the Level 9 calculations (in 0.1m increments)")]
        public UInt32 Mernok_Asset_Width_
        {
            get
            {
                return this[adr.Mernok_Asset_Width].Value;
            }
            set
            {
                value = Math.Min(value, (Byte)255);
                value = Math.Max(value, (Byte)0);
                this[adr.Mernok_Asset_Width].Value = value;
            }
        }


        #endregion

        #region Constructors

        #endregion

        #region Functions

        #endregion

        #region Collection methods

        public void Add(Parameter P)
        {
            this.Params.Add(P);
        }
        public void Remove(Parameter P)
        {
            this.Params.Remove(P);
        }
        public Parameter this[adr index]
        {
            get
            {
                return (Parameter)(Params.Find(x => x.address == (int)index));
            }
            set
            {
                Params[Params.FindIndex(x => x.address == (int)index)] = value;
            }
        }
        #endregion

    }
}
