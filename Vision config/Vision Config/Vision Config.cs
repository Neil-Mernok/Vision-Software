using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Be.Timvw.Framework.ComponentModel;
using System.Windows.Forms;
using Vision;
using RFID;
using System.Diagnostics;
using Vision_Libs.Vision;
using Vision_Libs.Params;
using System.Threading;
using Vision.Parameter;
using System.Media;

namespace Vision_config
{
    public partial class Vision_Tag_Config_Utility : Form
    {
        Boolean Program_tag = false;
        VisionParams Last_Tag;
        VisionParams Programming_Tag = new VisionParams();
        UInt32 LastProgrammedTag = 0;

        public Vision_Tag_Config_Utility()
        {
            InitializeComponent();
            Worker_RFID.WorkerSupportsCancellation = true;
            System.Windows.Forms.Application.EnableVisualStyles();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            Boolean iresult;

            while (1 == 1)
            {
                iresult = MernokRFID.OpenRFID(Mode.icode);
                
                if (iresult == true)
                    break;
                else
                {
                    if (MessageBox.Show("Please plug in a Mernok RFID-RWM Module to use the config utility", "No RFID Module", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Cancel)
                    {
                        this.Close();
                        return;
                    }
                }
            }
            comboBox1.SelectedIndex = 0;
            Programming_Tag = new VisionParams();
//             propertyGrid2.SelectedObject = Programming_Tag;
            timer_periodic.Enabled = true;
        }

        private VisionParams Read_Tag_Data()
        {
            Byte[] block;
            Boolean B = false;
            int err_count = 0;
            VisionParams Current_Tag = new VisionParams();

            UInt32 uid = MernokRFID_interface.read_UID();
            if (uid != 0)
            {
                // get each parameter value from the eeprom based on its index (memory address).
                Current_Tag.UID = uid;
                foreach (var P in Current_Tag.Params)
                {
                    B = false;
                    if (P.type == Parameter.Type.TypeString)
                    {
                        block = new Byte[Parameter.string_max];
                        for (int bs = 0; bs < Parameter.string_max; bs += 4)
                            B |= MernokRFID_interface.read_block((ushort)((P.address + bs) / 4), ref block, bs);
                    }
                    else
                        B = MernokRFID_interface.read_block((ushort)(P.address / 4), out block);

                    if (B)
                    {
                        P.param_valid = true;

                        if (P.type == Parameter.Type.TypeString)
                        {
                            int len = Array.FindIndex(block, 0, (x) => x == 0);
                            if (len < 0) len = Math.Min(block.Length, Parameter.string_max);
                            P.Value_str = Encoding.ASCII.GetString(block, 0, len);
                        }
                        else
                            P.Value = BitConverter.ToUInt32(block, 0);
                    }
                    else
                    {
                        err_count++;
                    }
                }

                if (B)
                    return Current_Tag;
                else
                    return null;
            }
            return null;
        }

        static Boolean Program_Tag(ref VisionParams TP, Boolean Disable)
        {
            Boolean B = false;

            if (Disable)
                TP.Activity &= ~TagActivities.tag_enable;
            else
                TP.Activity |= TagActivities.tag_enable;

            TP[VisionParams.adr.firmware].param_valid = false;          // make sure the Firmware revision is not written. Otherwise the settings reset. 
            TP[VisionParams.adr.firmware_sub].param_valid = false;
            TP[VisionParams.adr.Mernok_Asset_list_rev].param_valid = false;

            foreach (var P in TP.Params.Where(n => n.param_valid))
            {
                Byte[] data = P.GetData();
                ushort offset = 0;
                do
                {
                    B = MernokRFID_interface.write_block((ushort)((P.address + offset) / 4), data, offset);
                    if (B) 
                        offset += 4;
                    else
                        break;
                } while (offset < data.Length);

                if (B == false)
                    break;
            }

            return B;
        }


        private void timer_periodic_Tick(object sender, EventArgs e)
        {
            if (!Worker_RFID.IsBusy)
            {
                RFID_params p = new RFID_params();
                p.program_tag = (Program_tag);
                if (p.program_tag)
                {
                    label3.Text = "Writing to tag...";
                    propertyGrid1.SelectedObject = null;
                }
                //else
                //label3.Text = "Reading...";
                p.tag_to_program = Programming_Tag;
                p.last_programmed = LastProgrammedTag;
                p.disable_tag = checkBox2.Checked;

                Worker_RFID.RunWorkerAsync(p);

                Program_tag = false;
            }

            if (textBox2.Text.Contains("Mp123456"))
                checkBox2.Enabled = true;
            else
            {
                checkBox2.Checked = false;
                checkBox2.Enabled = false;
            }
        }


        private void Worker_RFID_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            RFID_returns R = new RFID_returns();
            RFID_params P = e.Argument as RFID_params;

            if (worker.CancellationPending)
                return;

            UInt32 UID = MernokRFID_interface.read_UID();
            R.UID = UID;
            R.result = "";
            R.programmed = false;
           
            if (UID != 0)
            {
                R.USB_OK = true;
                // check if we should program tags
                if (P.program_tag == true)
                {
                    if (P.last_programmed != UID)
                    {
                        if (!Program_Tag(ref Programming_Tag, P.disable_tag))
                        {
                            R.result = "Write failed";
                        }
                        else
                        {
                            R.result = "Write successful";
                            R.programmed = true;
                        }
                    }
                }
                Thread.Sleep(10);
                R.Current_Tag = Read_Tag_Data();
            }
            else
            {
                if (MernokRFID.IsOpen())
                    R.USB_OK = true;
                else
                    R.USB_OK = false;
            }
           
            e.Result = R;
        }

        private void Worker_RFID_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            RFID_returns R = e.Result as RFID_returns;
            
            label3.Text = R.result;

            if (R.USB_OK)
            {
                if (R.UID != 0)
                {
                    if (R.Current_Tag != null)
                    {
                        Last_Tag = R.Current_Tag;
                        if (Programming_Tag.UID == 0)
                        {
                            Programming_Tag = Last_Tag;
                            propertyGrid2.SelectedObject = Programming_Tag;
                            //propertyGrid2.ExpandAllGridItems();
                            CollapseFunctions(propertyGrid2);
                        }
                    }

                    string UIDs = R.UID.ToString("X8");
                    if ((textBox1.Text != UIDs && R.Current_Tag != null))
                    {
                        try
                        {
                            propertyGrid1.SelectedObject = R.Current_Tag;
                            //propertyGrid1.ExpandAllGridItems();
                            CollapseFunctions(propertyGrid1);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("RFID worker catch RFID Interface");
                            Console.WriteLine(ex.Message);
                        }
                        label3.Text = "Read successful";
                    }

                    textBox1.Text = UIDs;
                    button3.Enabled = true;
                    button1.Enabled = true;
                    Detected_Tag_Button.BackColor = Color.DarkGreen;
                    Detected_Tag_Button.Text = "Tag Detected";

                    if (R.programmed)
                    {
                        LastProgrammedTag = R.UID;
                        if (R.Current_Tag != null)
                        {
                            propertyGrid1.SelectedObject = R.Current_Tag;
                            //propertyGrid1.ExpandAllGridItems();
                            CollapseFunctions(propertyGrid1);
                            label3.Text = "Write successful";
                            SystemSounds.Asterisk.Play();
                        }
                    }
                    if(R.result == "Write failed")
                    {
                        label3.Text = "Write FAILED!!";
                        SystemSounds.Hand.Play();
                    }
                }
                else
                {
                    LastProgrammedTag = 0;
                    textBox1.Text = "";
                    button1.Enabled = false;
                    button3.Enabled = false;
                    Detected_Tag_Button.BackColor = Color.DarkRed;
                    Detected_Tag_Button.Text = "No Tag";
                    label3.Text = "";
                    propertyGrid1.SelectedObject = null;
                }
            }
            else
            {
                timer_periodic.Stop();
                Boolean iresult;

                while (1 == 1)
                {
                    iresult = MernokRFID.OpenRFID(Mode.icode);

                    if (iresult == true)
                    {
                        timer_periodic.Start();
                        break;
                    }
                    else
                    {
                        if (MessageBox.Show("Please plug in Mernok RFID-RWM Module to use the PDS config utility.", "No RFID Module", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Cancel)
                        {
                            this.Close();
                            return;
                        }
                    }
                }
            }
        }

        private void PDS_Tag_Config_Utility_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer_periodic.Stop();
            Worker_RFID.CancelAsync();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Programming_Tag = Last_Tag;
            propertyGrid2.SelectedObject = Programming_Tag;
            //propertyGrid2.ExpandAllGridItems();
            CollapseFunctions(propertyGrid2);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            propertyGrid2.SelectedObject = Programming_Tag;
            //propertyGrid2.ExpandAllGridItems();
            CollapseFunctions(propertyGrid2);
        }

        public void CollapseFunctions(PropertyGrid P)
        {
            System.Windows.Forms.GridItem root = P.SelectedGridItem;
            while (root.Parent != null)
                root = root.Parent;

            foreach (System.Windows.Forms.GridItem G in root.GridItems)
            {
                if (G.Label.Contains("TAG Function Flags"))
                    G.Expanded = false;
                else
                    G.Expanded = true;
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {
            Programming_Tag = new VisionParams();
            Programming_Tag.load_standard();
            Programming_Tag.UID = 1;            // make it non-zero to make sure we dont ignore/override this. 
            switch (comboBox1.SelectedIndex)
            {
                case 1:
                    Programming_Tag.Activity = TagActivities.Pulse300;
                    Programming_Tag.Type = TagTypesL.MernokAssetType[(int)TagType.Loco-1].Type;
                    break;
                case 2:
                    Programming_Tag.Activity = TagActivities.Pulse400;
                    Programming_Tag.Type = TagTypesL.MernokAssetType[(int)TagType.Loco-1].Type;
                    break;
                case 3:
                    Programming_Tag.Activity = TagActivities.Pulse500;
                    Programming_Tag.Interval = 5000;
                    Programming_Tag.LF_interval_Time = 500;
                    Programming_Tag.Type = Programming_Tag.Type = TagTypesL.MernokAssetType[(int)TagType.HaulTruckADT-1].Type; ;
                    break;
                case 4:
                    Programming_Tag.Activity = TagActivities.Ranger;
                    Programming_Tag.Type = Programming_Tag.Type = TagTypesL.MernokAssetType[(int)TagType.HaulTruckADT-1].Type; ;
                    break;
                case 5:
                    Programming_Tag.Activity = TagActivities.GPS_Module;
                    Programming_Tag.Type = Programming_Tag.Type = TagTypesL.MernokAssetType[(int)TagType.HaulTruckADT-1].Type;
                    break;
                default:
                    Programming_Tag.Activity = TagActivities.Pulse100;
                    Programming_Tag.Type = TagTypesL.MernokAssetType[(int)TagType.Person-1].Type;
                    break;
            }
            propertyGrid2.SelectedObject = Programming_Tag;
            //propertyGrid2.ExpandAllGridItems();
            CollapseFunctions(propertyGrid2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Program_tag = true;
        }
    }
    
    internal class RFID_params
    {
        internal bool program_tag { get; set; }
        internal bool disable_tag { get; set; }
        internal UInt32 last_programmed { get; set; }
        internal VisionParams tag_to_program { get; set; }
    }
    internal class RFID_returns
    {
        internal string result { get; set; }
        internal bool programmed { get; set; }
        internal UInt32 UID { get; set; }
        internal bool USB_OK { get; set; }
        internal VisionParams Current_Tag { get; set; }
}

}
