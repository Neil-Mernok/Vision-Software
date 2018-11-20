using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Collections.ObjectModel;

namespace PDS_Master_Interface
{
    public enum Master_message_type
    {
        uid = 'u',
        status = 'r', 
        tag_data = 'p',
        tag_count = 'k',
        firmware_rev = 'v', 
        tag_types = 't',
        read_setting = 's',
        clear_log = 'x',
        all_tags = 'a',
        hardware_stat = 'h',
        test_mode = 'c',
        other = 'z'
    };
   
    public class Reader
    {
        #region constants
        // Com status definitions
        const Byte RF_Coms = 1;
        const Byte UART_Coms = 2;
        const Byte SPI_Coms = 4;
        const Byte CAN_Coms = 8;
        //Com CPU status definitions 
        const Byte CPU_Fstat = 1;
        const Byte CPU_Vstat = 2;
        const Byte CPU_Tstat = 4;
        #endregion

        #region fields

        public UInt32 ID;               // uid of the reader's processor
        public Byte Com_Stat;
        public Byte CPU_Stat;
        public Byte Num_Tags;           // number of tag messages its seem 
        public Byte Antenna;
        public Byte timeout;
        public Boolean Testmode;
        public DateTime Last_Seen;             // keeps track of the last message recieved
        
        public Master_message_type last_message_sent;

        public string TagReader_ID
        {
            get
            {
                return ID.ToString("X8");
            }
            set { }
        }

        public string UART_Status
        {
            get
            {
                if ((Com_Stat & UART_Coms) == UART_Coms)
                    return "UART GOOD";
                else
                    return "UART BAD";
            }
            set { }
        }

        public string CAN_Status
        {
            get
            {
                if ((Com_Stat & CAN_Coms) == CAN_Coms)
                    return "CAN GOOD";
                else
                    return "CAN BAD";
            }
            set { }
        }

        public string RF_Status
        {
            get
            {
                if ((Com_Stat & RF_Coms) == RF_Coms)
                    return "RF GOOD";
                else
                    return "RF BAD";
            }
            set { }
        }

        public string SPI_Status
        {
            get
            {
                if ((Com_Stat & SPI_Coms) == SPI_Coms)
                    return "SPI GOOD";
                else
                    return "SPI BAD";
            }
            set { }
        }

        public string CPU_Status
        {
            get
            {
                if ((CPU_Stat & CPU_Fstat) == CPU_Fstat)
                    return "CPU GOOD";
                else
                    return "CPU Frequency BAD";
            }
            set { }
        }

        public string Number_of_Tags_seen
        {
            get
            {
                return Num_Tags.ToString();
            }
            set { }
        }

        public string Antenna_Sensed
        {
            get
            {
                return Antenna.ToString();
            }
            set { }
        }

        #endregion
        
        #region Health check
        public string Reader_Health
        {
           get
            {
                if (((Com_Stat | ((Byte)7)) == 7) && ((CPU_Stat | ((Byte)1)) == 1) && (Num_Tags != 0))
                    return "Good";
                else
                    return "Bad";
            }
            set { }
        }
        public Boolean Healthy()
        {
            if (((Com_Stat & ((Byte)7)) == 7) && ((CPU_Stat & ((Byte)1)) == 1) && (Num_Tags != 0))
                return true;
            else
                return false;
        }
        #endregion

        #region contructor
        public Reader()
        {
            ID = 0;
            Com_Stat = 0;
            CPU_Stat = 0;
            Num_Tags = 0;
            Antenna = 0;
            timeout = 0;
            Num_Tags = 0;
            last_message_sent = Master_message_type.other;
            Last_Seen = DateTime.Now;
        }
        #endregion
    }

    public static class Master_interface
    {
        #region uart sending commmands
        // send the status request packet to slave
        public static void Master_Rq_Status(ref SerialPort p)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'r';
            MasterMessage[2] = (Byte)0;
            MasterMessage[3] = (Byte)0;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen) p.Write(MasterMessage, 0, 5);
        }

        // send the tag data request packet to slave, with specified tag
        public static void Master_Rq_Tag_Data(ref SerialPort p, Byte tag_number)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'t';
            MasterMessage[2] = (Byte)tag_number;
            MasterMessage[3] = (Byte)0;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen) p.Write(MasterMessage, 0, 5);
        }

        // send the tag types request packet to slave, with specified tag range (group of 8 tags)
        public static void Master_Rq_Tag_Types(ref SerialPort p, Byte tag_range)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'y';
            MasterMessage[2] = (Byte)tag_range;
            MasterMessage[3] = (Byte)0;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen) p.Write(MasterMessage, 0, 5);
        }

        // send the write reader setting command to slave, with specified setting
        public static void Master_Rq_Reader_Setting(ref SerialPort p, Byte addr, Byte value)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'s';
            MasterMessage[2] = (Byte)addr;
            MasterMessage[3] = (Byte)value;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen) p.Write(MasterMessage, 0, 5);
        }

        // send the command to clear the reader's log of tags
        public static void Master_Rq_Clear_log(ref SerialPort p)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'c';
            MasterMessage[2] = (Byte)0;
            MasterMessage[3] = (Byte)0;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen) p.Write(MasterMessage, 0, 5);
        }

        // send the request for all tag data
        public static void Master_Rq_All_Tags(ref SerialPort p)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'a';
            MasterMessage[2] = (Byte)0;
            MasterMessage[3] = (Byte)0;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen) p.Write(MasterMessage, 0, 5);
        }

        // send the hardware status request
        public static void Master_Rq_Hardware_stat(ref SerialPort p)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'h';
            MasterMessage[2] = (Byte)0;
            MasterMessage[3] = (Byte)0;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen)   p.Write(MasterMessage, 0, 5);
        }

        // send the request to put reader in test mode
        public static void Master_Rq_Test_Mode(ref SerialPort p)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'m';
            MasterMessage[2] = (Byte)0;
            MasterMessage[3] = (Byte)0;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen) p.Write(MasterMessage, 0, 5);
        }

        // send the request to put reader in test mode
        public static void Master_exit_bootloader(ref SerialPort p)
        {
            byte[] MasterMessage = new byte[1];
            MasterMessage[0] = (Byte)'A';
            
            if (p.IsOpen) p.Write(MasterMessage, 0, MasterMessage.Length);
        }
        // send the request to stop test mode
        public static void Master_Rq_Test_Mode_End(ref SerialPort p)
        {
            byte[] MasterMessage = new byte[5];
            MasterMessage[0] = (Byte)'M';
            MasterMessage[1] = (Byte)'e';
            MasterMessage[2] = (Byte)0;
            MasterMessage[3] = (Byte)0;
            MasterMessage[4] = (Byte)'F';

            if (p.IsOpen) p.Write(MasterMessage, 0, 5);
        }
        #endregion

        #region uart receiving functions
        public static Boolean Master_Parse_message(Byte[] data)
        {
            if ((data[0] == 'P') && (data[11] == 'F'))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
         
        
        public static void Master_parse_response(ref Reader R, Byte[] data)
        {
            if ((data[0] == 'P') && (data[11] == 'F'))
            {
                switch (data[1])
                {
                    case (Byte)'r':
                        R.Num_Tags = data[2];
                        R.Antenna = data[3];
                        R.timeout = data[4];
                        R.Last_Seen = DateTime.Now;
                        if (data[5] != 0) R.Testmode = true;
                        else R.Testmode = false;
                        break;
                   
                    case (Byte)'t':
                        //UInt32 tag_UID = BitConverter.ToUInt32(data, 2);
                        //Tag T = new Tag(tag_UID, data[7], data[6], data[8], data[9], (Byte)data[10]);
                        break;
                    case (Byte)'y':
                        break;
                    case (Byte)'s':
                        break;
                    case (Byte)'c':
                        break;
                    case (Byte)'a':
                        break;
                    
                    case (Byte)'h':
                        UInt32 Reader_ID = BitConverter.ToUInt32(data, 2);
                        if (Reader_ID != R.ID)
                        {
                            R = new Reader();
                            R.ID = Reader_ID;
                        }
                        R.Com_Stat = data[6];
                        R.CPU_Stat = data[7];
                        R.Last_Seen = DateTime.Now;
                        break;

                    case (Byte)'m':
                        break;

                    case (Byte)'e':
                        break;

                    default:
                        break;
                }
            }
        }
        #endregion

    }
}
