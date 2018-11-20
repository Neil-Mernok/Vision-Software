using System;
using System.ComponentModel;
using Vision.Parameter;
using Vision_Libs.Params;
using FlagsEnumTypeConverter;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using iTextSharp.text;
using System.Collections.Generic;

namespace Vision
{
    public class VisionDevice
    {
        //public TAG_settings settings;
        public TAG_status status;
        public VisionParams Params;
        public _GPS_Data GPS_Data;
        public _Level_Test_Type Level_Test_Type;
        public LF_data own_LF = new LF_data();
        public string board_info;

        public List<_Navigation_Parameters> GPS_PARMS_List = new List<_Navigation_Parameters>();

        public VisionDevice()
        {
            //settings = new TAG_settings();
            status = new TAG_status();
            Params = new VisionParams();
            GPS_Data = new _GPS_Data();
            Level_Test_Type = new _Level_Test_Type();
        }

        private static bool Tollerate(double s1, double s2)
        {
            if((s1 == 0) ||(s2 == 0))
                return false;

            double diff = Math.Abs(s1 - s2) / (s1+s2) * 200;
            if (diff <= 1)        // about 1%
                return true;
            else
                return false;
        }
        private static string Tollerate_error(double s1, double s2)
        {
            if ((s1 == 0) || (s2 == 0))
                return "Coil tuning error";

            double diff = (s1 - s2) / (s1 + s2) * 200;

            if (diff > 1)        // about 1%
                return "Coil tuned too low, decrease capacitance";
            else if (diff < -1)
                return "Coil tuned too high, increase capacitance";
            else
                return "-";
        }
        public Report_writer get_test_report()
        {
            Report_writer report = new Report_writer(Params.UID);

            string text;
            string error;
            Boolean working;
            string[] Board_Info = Regex.Split(Params.PCB_ID, ", ");
            string[] PCB_info = Regex.Split(Board_Info[4], "-");

            if (Level_Test_Type.Test_Type > (int)TAG._Level_Test.None_Selected)
            {
                //text = (status.HasFlag(TAG_status.crystal_working)) ? "Crystal is working" : "Crystal not working";
                text = "Microcontroller's crystal";
                error = (status.HasFlag(TAG_status.crystal_working)) ? "-" : "Crystal is not working";
                report.lines.Add(new report_test(text, status.HasFlag(TAG_status.crystal_working), true, error));
                
                //text = (status.HasFlag(TAG_status.RFID_working)) ? "EEPROM I2C working" : "Unable to access EEPROM"
                text = "EEPROM Interface";
                error = (status.HasFlag(TAG_status.RFID_working)) ? "-" : "EEPROM I2C failed";
                report.lines.Add(new report_test(text, status.HasFlag(TAG_status.RFID_working), true,error));

                TAG_status checker;

                checker = TAG_status.crystal_working | TAG_status.RFID_working;

                if (PCB_info[1] == "138")
                {
                    //text = (status.HasFlag(TAG_status.NN_SPI_Working)) ? "Nanotron SPI working" : "Nanotron SPI failed";
                    text = "Nanotron Module Interface";
                    error = (status.HasFlag(TAG_status.NN_SPI_Working)) ? "-" : "Nanotron SPI failed";
                    report.lines.Add(new report_test(text, status.HasFlag(TAG_status.NN_SPI_Working), true, error));
                    //text = (status.HasFlag(TAG_status.RF_working)) ? "Nanotron RF communication working" : "Nanotron RF communication not working or data not yet received";
                    text = "Nanotron RF Communication";
                    error = (status.HasFlag(TAG_status.RF_working)) ? "-" : "Nanotron RF communication not working or data not yet received";
                    report.lines.Add(new report_test(text, status.HasFlag(TAG_status.RF_working), true, error));
                }
                else if (PCB_info[1] == "217")
                {
                    //text = (status.HasFlag(TAG_status.Module_UART_Working)) ? "GNSS UART communication working" : "GNSS UART communication not working or data not yet received";
                    text = "GNSS Module Interface";
                    error = (status.HasFlag(TAG_status.Module_UART_Working)) ? "-" : "GNSS UART communication not working or data not yet received";
                    report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Module_UART_Working), true, error));
                    //text = (status.HasFlag(TAG_status.Module_RF_Working)) ? "GNSS RF communication working" : "GNSS RF communication not working or data not yet received";
                    text = "GNSS RF Communication";
                    error = (status.HasFlag(TAG_status.Module_RF_Working)) ? "-" : "GNSS RF communication not working or data not yet received";
                    report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Module_RF_Working), true, error));
                }
                else
                {
                    //text = (status.HasFlag(TAG_status.CC_SPI_working)) ? "CC1101 SPI working" : "CC1101 SPI failed";
                    text = "CC1101 Interface";
                    error = (status.HasFlag(TAG_status.CC_SPI_working)) ? "-" : "CC1101 SPI failed";
                    report.lines.Add(new report_test(text, status.HasFlag(TAG_status.CC_SPI_working), true, error));
                    //text = (status.HasFlag(TAG_status.RF_working)) ? "RF communication working" : "RF communication not working or data not yet received";
                    text = "CC1101 RF Communication";
                    error = (status.HasFlag(TAG_status.RF_working)) ? "-" : "RF communication not working or data not yet received";
                    report.lines.Add(new report_test(text, status.HasFlag(TAG_status.RF_working), true, error));

                    if (Params.Activity.HasFlag(TagActivities.GPS_Capable) && (PCB_info[1] == "203"))
                    {
                        //text = (status.HasFlag(TAG_status.Module_UART_Working)) ? "GNSS UART communication working" : "GNSS UART communication not working or data not yet received";
                        text = "GNSS Module Interface";
                        error = (status.HasFlag(TAG_status.Module_UART_Working)) ? "-" : "GNSS UART communication not working or data not yet received";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Module_UART_Working), true, error));
                        //text = (status.HasFlag(TAG_status.Module_RF_Working)) ? "GNSS RF communication working" : "GNSS RF communication not working or data not yet received";
                        text = "GNSS RF Communication";
                        error = (status.HasFlag(TAG_status.Module_RF_Working)) ? "-" : "GNSS RF communication not working or data not yet received";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Module_RF_Working), true, error));
                    }
                }

                if (PCB_info[1] == "138")
                {
                    checker |= TAG_status.NN_SPI_Working | TAG_status.RF_working;
                }
                else if ((PCB_info[1] == "217"))
                {
                    checker |= TAG_status.Module_UART_Working | TAG_status.Module_RF_Working;
                }
                else if ((PCB_info[1] == "203") && (PCB_info[2] == "02"))
                {
                    if (Params.Activity.HasFlag(TagActivities.GPS_Capable))
                        checker |= TAG_status.CC_SPI_working | TAG_status.RF_working | TAG_status.Module_UART_Working | TAG_status.Module_RF_Working;
                    else
                        checker |= TAG_status.CC_SPI_working | TAG_status.RF_working;
                }
                else
                {
                    checker |= TAG_status.CC_SPI_working | TAG_status.RF_working;
                }

                if ((Level_Test_Type.Test_Type == (int)TAG._Level_Test.Pulse300) || (Level_Test_Type.Test_Type == (int)TAG._Level_Test.Pulse500) || Level_Test_Type.Test_Type == (int)TAG._Level_Test.Module)
                {
                    //text = (status.HasFlag(TAG_status.CAN_working)) ? "CAN communication working" : "CAN not working or data not yet received";
                    text = "CAN Communication";
                    error = (status.HasFlag(TAG_status.CAN_working)) ? "-" : "CAN communication not working or data not yet received";
                    report.lines.Add(new report_test(text, status.HasFlag(TAG_status.CAN_working), status.HasFlag(TAG_status.CAN_capable),error));

                    if (status.HasFlag(TAG_status.CAN_capable)) checker |= TAG_status.CAN_working;

                    if (Level_Test_Type.Test_Type == (int)TAG._Level_Test.Module)
                    {
                        //text = (status.HasFlag(TAG_status.Uart_working)) ? "UART communication working" : "UART not working or data not yet received";
                        text = "UART Communication";
                        error = (status.HasFlag(TAG_status.Uart_working)) ? "-" : "UART communication not working or data not yet received";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Uart_working), status.HasFlag(TAG_status.UART_capable), error));
                        
                        //text = (status.HasFlag(TAG_status.USB_working)) ? "USB communication working" : "USB communication not working or inactive";
                        text = "USB Communication";
                        error = (status.HasFlag(TAG_status.USB_working)) ? "-" : "USB communication not working or inactive";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.USB_working), status.HasFlag(TAG_status.USB_Active | TAG_status.USB_capable), error));

                        if (status.HasFlag(TAG_status.UART_capable)) checker |= TAG_status.Uart_working;
                    }
                }

                //text = (status.HasFlag(TAG_status.EXT_Power)) ? "External powered" : "Battery powered";
                text = "External power";
                //error = (status.HasFlag(TAG_status.EXT_Power)) ? "-" : "Battery powered or powered via USB/UART";
                error = (status.HasFlag(TAG_status.EXT_Power)) ? "-" : "Battery powered";
                report.lines.Add(new report_test(text, status.HasFlag(TAG_status.EXT_Power), status.HasFlag(TAG_status.EXT_Power), error));
                //text = (status.HasFlag(TAG_status.Charging)) ? "Charging" : "Not charging";
                text = "Battery charging indication";
                if (PCB_info[1] == "203")
                {
                    if (status.HasFlag(TAG_status.EXT_Power))
                    {
                        error = (status.HasFlag(TAG_status.Charging)) ? "-" : "Battery full or battery charging indcation failed";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Charging), true, error));
                    }       
                    else
                    {
                        error = (status.HasFlag(TAG_status.Charging)) ? "-" : "Connect external power to test battery charging indcation";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Charging), true, error));
                    }
                        
                }
                else
                {
                    error = (status.HasFlag(TAG_status.Charging)) ? "-" : "Battery full, no battery connected or battery charging indcation failed";
                    report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Charging), true, error));
                }
                //error = (status.HasFlag(TAG_status.Charging)) ? "-" : "Battery full, no battery connected or battery charging indcation failed";
                //report.lines.Add(new report_test(text, status.HasFlag(TAG_status.Charging), true, error));
                //text = (status.HasFlag(TAG_status.low_power_mode)) ? "Low power mode" : "Not low power mode";
                text = "Low power mode";
                error = (status.HasFlag(TAG_status.low_power_mode)) ? "-" : "-";
                report.lines.Add(new report_test(text, status.HasFlag(TAG_status.low_power_mode), false, error));

                if ((PCB_info[1] == "182") || (PCB_info[1] == "203"))
                {
                    if (status.HasFlag(TAG_status.LF_SPI_working))
                    {
                        //text = (status.HasFlag(TAG_status.LF_SPI_working)) ? "AS3933 SPI is working" : "AS3933 SPI is not working or has not been configured";
                        text = "AS3933 Interface";
                        error = (status.HasFlag(TAG_status.LF_SPI_working)) ? "-" : "AS3933 SPI has not been configured";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.LF_SPI_working), true, error));
                        //text = (status.HasFlag(TAG_status.LF_tuned)) ? "LF coil passed tuning" : "LF coil tuning failed or has not been configured";

                        text = "LF Coil tuning";
                        error = (status.HasFlag(TAG_status.LF_tuned)) ? "-" : "LF coil tuning failed or has not been configured";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.LF_tuned), Params.Activity.HasFlag(TagActivities.LF_response), error));

                        text = Params.StatusVals[0].name + " : " + Params.StatusVals[0].Value.ToString();
                        error = Tollerate_error(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[0].Value);
                        report.lines.Add(new report_test(text, Tollerate(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[0].Value), Params.Activity.HasFlag(TagActivities.LF_response), error));
                        text = Params.StatusVals[1].name + " : " + Params.StatusVals[1].Value.ToString();
                        error = Tollerate_error(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[1].Value);
                        report.lines.Add(new report_test(text, Tollerate(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[1].Value), Params.Activity.HasFlag(TagActivities.LF_response), error));
                        text = Params.StatusVals[2].name + " : " + Params.StatusVals[2].Value.ToString();
                        error = Tollerate_error(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[2].Value);
                        report.lines.Add(new report_test(text, Tollerate(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[2].Value), Params.Activity.HasFlag(TagActivities.LF_response), error));

                        if (Params.Activity.HasFlag(TagActivities.LF_response))
                        {
                            checker |= TAG_status.LF_tuned;
                        }
                    }
                    else
                    {
                        //text = (status.HasFlag(TAG_status.LF_SPI_working)) ? "AS3933 SPI is working" : "AS3933 SPI not used";
                        text = "AS3933 interface";
                        error = (status.HasFlag(TAG_status.LF_SPI_working)) ? "-" : "AS3933 SPI not working";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.LF_SPI_working), true, error));
                        //text = (status.HasFlag(TAG_status.LF_tuned)) ? "LF coil passed tuning" : "LF coil is not used";
                        text = "LF coil tuning";
                        error = (status.HasFlag(TAG_status.LF_tuned) && status.HasFlag(TAG_status.LF_SPI_working)) ? "-" : "LF coil is not tuned due to AS3933 interface fail";
                        report.lines.Add(new report_test(text, status.HasFlag(TAG_status.LF_SPI_working), status.HasFlag(TAG_status.LF_SPI_working), error));

                        /*text = Params.StatusVals[0].name + " : " + Params.StatusVals[0].Value.ToString();
                        error = Tollerate_error(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[0].Value);
                        report.lines.Add(new report_test(text, Tollerate(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[0].Value), true, error));
                        text = Params.StatusVals[1].name + " : " + Params.StatusVals[1].Value.ToString();
                        error = Tollerate_error(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[1].Value);
                        report.lines.Add(new report_test(text, Tollerate(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[1].Value), true, error));
                        text = Params.StatusVals[2].name + " : " + Params.StatusVals[2].Value.ToString();
                        error = Tollerate_error(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[2].Value);
                        report.lines.Add(new report_test(text, Tollerate(Params[VisionParams.adr.lf_hertz].Value, Params.StatusVals[2].Value), true, error));*/
                    }
                    checker |= TAG_status.LF_SPI_working;
                }

                working = status.HasFlag(checker);
                report.Pass = working;
                
            }

            return report;
        }
    }

    public enum Board_ID
    {
        ME_PCB_182_01,
        ME_PCB_182_02,
        ME_PCB_182_03,
        ME_PCB_173_01,
        ME_PCB_173_02,
        ME_PCB_173_03,
        vision_reader,
        pulse300,
        pulse500,
        ME_PCB_182_04,
        ME_PCB_173_04,
        // ---- V12 ---- 
        ME_PCB_138_03,
        ME_PCB_182_05,
        ME_PCB_217_01,
        ME_PCB_203_02
    };

    [Category("Status")]
    [TypeConverter(typeof(FlagsEnumTypeConverter.FlagsEnumConverter))]
    [ReadOnly(true)]
    [Flags]
    public enum TAG_status
    {
        none = 0,
        CC_SPI_working = 1 << 0,
        LF_SPI_working = 1 << 1,
        LF_tuned = 1 << 2,
        ACC_SPI_working = 1 << 3,
        RFID_working = 1 << 4,
        RF_working = 1 << 5,
        Uart_working = 1 << 6,
        crystal_working = 1 << 7,
        USB_working = 1 << 8,
        CAN_working = 1 << 9,
        Charging = 1 << 10,
        EXT_Power = 1 << 11,
        low_power_mode = 1 << 12,
        USB_Active = 1 << 13,
        Antenna_connected = 1 << 14,
        Sleeping = 1 << 15,
        UART_capable = 1 << 16,
        USB_capable = 1 << 17,
        CAN_capable = 1 << 18,
        // --- V12 Flags ---
        RF_Recieving = 1 << 19,
        NN_SPI_Working = 1 << 20,
        Module_UART_Working = 1 << 21,
        Module_RF_Working = 1 << 22,
    }

    public class _Level_Test_Type
    {
        public int Test_Type { get; set; }
    }

    public class _GPS_Data
    {
        public Int32 Longitude { get; set; }
        
        public string Longitude_deg
        {
            get
            {
                double Longitude_degrees;
                Longitude_degrees = Convert.ToDouble(Longitude)/10000000.0000000;

                if (Longitude < 0)
                    return Longitude_degrees.ToString().Replace("-", "").PadRight(10, '0') + " W";
                else
                    return Longitude_degrees.ToString().PadRight(10, '0') + " E";
            }
        }

        public Int32 Latitude { get; set; }
        public string Latitude_deg
        {
            get
            {
                double Latitude_degrees;
                Latitude_degrees = Convert.ToDouble(Latitude) / 10000000.0000000;

                if (Latitude < 0)
                    return Latitude_degrees.ToString().Replace("-","").PadRight(10, '0') + " S";
                else
                    return Latitude_degrees.ToString().PadRight(10, '0') + " N";
            }
        }

        public Int32 HorizontalAccuracy { get; set; }
        public Int32 HorizontalAccuracy_m
        {
            get
            {
                return (HorizontalAccuracy / 1000);
            }
        }

        public Int32 VerticalAccuracy { get; set; }
        public Int32 VerticalAccuracy_m
        {
            get
            {
                return (VerticalAccuracy / 1000);
            }
        }

        public uint FixAge { get; set; }

        public TAG._GPS_FixType FixType { get; set; }

        public uint NumberOfSat { get; set; }

        public Int32 Speed { get; set; }
        public Int32 Speed_km_h {
            get
            {
                return (Speed / 278);
            }
        }

        public Int32 SpeedAccuracy { get; set; }
        public Int32 SpeedAccuracy_km_h
        {
            get
            {
                return (SpeedAccuracy / 278);
            }
        }

        public Int32 HeadingVehicle { get; set; }
        public double HeadingVehicle_deg
        {
            get
            {
  
               return Convert.ToDouble(HeadingVehicle) / 100000.0000000;
            }
        }

        public Int32 HeadingMotion { get; set; }
        public double HeadingMotion_deg
        {
            get
            {

                return Convert.ToDouble(HeadingMotion) / 100000.0000000;
            }
        }

        public UInt32 HeadingAccuracy { get; set; }
        public double HeadingAccuracy_deg
        {
            get
            {

                return Convert.ToDouble(HeadingAccuracy) / 100000.0000000;
            }
        }

        public UInt32 TravelDistance { get; set; }
        public double TravelDistance_km
        {
            get
            {

                return Convert.ToDouble(TravelDistance) / 1000.00;
            }
        }

        public Int32 Sealevel { get; set; }
        public double Sealevel_m
        {
            get
            {

                return Convert.ToDouble(Sealevel) / 1000.00;
            }
        }

        public UInt32 TotalTravelDistance { get; set; }
        public double TotalTravelDistance_km
        {
            get
            {

                return Convert.ToDouble(TotalTravelDistance) / 1000.00;
            }
        }

        public UInt16 Flags { get; set; }

        public bool HeadingValidity
        {
            get
            {
                byte[] flags_bytes = BitConverter.GetBytes(Flags);
                byte flags_1 = flags_bytes[0];

                return (1 == ((flags_1 >> 5) & 1));
            }
        }

        public bool GNSSFixOK
        {
            get
            {
                byte[] flags_bytes = BitConverter.GetBytes(Flags);
                byte flags_1 = flags_bytes[0];

                return (1 == ((flags_1 >> 0) & 1));
            }
        }

        public TAG._GPS_PSMState PSMState
        {
            get
            {
                byte[] flags_bytes = BitConverter.GetBytes(Flags);
                byte flags_1 = flags_bytes[0];
                flags_1 &= 0x1C;

                return (TAG._GPS_PSMState)(flags_1 >> 3);
            }
        }

        public byte Antenna_State;
        public TAG._GPS_AntennaState AntennaState
        {
            get
            {
                return (TAG._GPS_AntennaState)(Antenna_State);
            }
        }

        public UInt32 _Date { get; set; }
        public string _DateString
        {
            get
            {
                byte[] DateBytes = BitConverter.GetBytes(_Date);

                return DateBytes[3].ToString() + DateBytes[2].ToString().PadLeft(2, '0') + ":" + DateBytes[1].ToString().PadLeft(2,'0') + ":" + DateBytes[0].ToString().PadLeft(2, '0');
            }
        }

        public UInt32 _Time { get; set; }
        public string _TimeString
        {
            get
            {
                byte[] TimeBytes = BitConverter.GetBytes(_Time);

                return TimeBytes[2].ToString().PadLeft(2, '0') + ":" + TimeBytes[1].ToString().PadLeft(2, '0') + ":" + TimeBytes[0].ToString().PadLeft(2, '0');
            }
        }
    }

    public class _Navigation_Parameters
    {
        public string Navigation_Parameter { get; set; }
        public string Value { get; set; }
        public string Unit { get; set; }
        public string Raw_data { get; set; }
        public string Raw_unit { get; set; }
    }


}