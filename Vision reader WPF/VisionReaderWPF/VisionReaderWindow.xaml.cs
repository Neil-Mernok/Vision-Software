using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.IO.Ports;
using Vision.Parameter;
using Vision;
using vision_interface;
using System.Windows.Threading;
using Vision_config;
using System.Collections.ObjectModel;
using Vision_Libs.Vision;
using Vision_Libs.Properties;
using System.Threading;
using System.Globalization;
using System.Windows.Media.Animation;
using Be.Timvw.Framework.ComponentModel;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using vision_boot;
using AboutWindow;
using Vision_Libs;
using MernokAssets;
using System.Text.RegularExpressions;
using RFID;
using Vision_Libs.Params;
using System.Windows.Controls.Primitives;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VisionReader.CustomMarkers;
using GMap.NET.MapProviders;
using System.Windows.Shapes;
using System.IO;

namespace VisionReader
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow AppWindow;

        public MainWindow()
        {
            InitializeComponent();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 1);
            GPS_device = false;
            //searh for xml file in resourses folder, if not found open window to search for xml in other directory, copy file to resourses folder of application
            //process file, retrive info
            mernokAssetFile = MernokAssetManager.ReadMernokAssetFile(@"C:\MernokAssets\MernokAssetList.xml");
            if (mernokAssetFile == null)
            {
                string fileName = xMLFile.openfile();
                mernokAssetFile = MernokAssetManager.ReadMernokAssetFile(fileName);
            }
            //mernokAssetFile.mernokAssetList.Insert(80, mernokAssetFile.mernokAssetList[(int)TagType.Person - 1]);

            foreach (MernokAsset item in mernokAssetFile.mernokAssetList)
            {
                TagTypesL.MernokAssetType.Add(item);
                TagTypesL.MenokAssetTypeName.Add(item.TypeName);
            }

            VisionParams.Mernok_assetfile_rev = mernokAssetFile.version;


            PropertyGrid_tag_settings.ToolbarVisible = false;
            FormsHostPG.Child = PropertyGrid_tag_settings;
            System.Windows.Forms.Application.EnableVisualStyles();
        }

        private System.Windows.Forms.PropertyGrid PropertyGrid_tag_settings = new System.Windows.Forms.PropertyGrid();
        public int started = 0;
        public int Bytes_Received = 0;
        Report_writer test_report;
        public static MernokAssetFile mernokAssetFile =  new MernokAssetFile();
        public string[] comb3_vals;
        public double LF_Oppacity { get; set; }
        public string LF_Oppacity_str { get; set; }

        Vision_Interface Current_vision;
        SortableBindingList<TAG> Tags;

        bool GPS_device;

        SortableBindingList<_Navigation_Parameters> VISION_GPS_Device;

        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        

        XMLFileManipulation xMLFile = new XMLFileManipulation();
        byte[] Mernok_Asset_group_list;
        byte[] Mernok_Asset_group_list_test;



        //TestOperatorsInfo CurrentOperators;

        //List<string> Operators;
        //List<string> TOPasswords;

        #region construct destruct

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // cleanup_timer
            DispatcherTimer cleanup_timer = new DispatcherTimer();
            cleanup_timer.Interval = TimeSpan.FromMilliseconds(100);
            cleanup_timer.Tick += cleanup_timer_Tick;

            DispatcherTimer Vision_controller = new DispatcherTimer();
            Vision_controller.Interval = TimeSpan.FromMilliseconds(1000);
            Vision_controller.Tick += Vision_Control_Tick;

            DispatcherTimer Force_RF_Zone_Timer = new DispatcherTimer();
            Force_RF_Zone_Timer.Interval = TimeSpan.FromMilliseconds(1250);
            Force_RF_Zone_Timer.Tick += Force_RF_Zone_Tick;

            Current_vision = new Vision_Interface(vision_property_changed);

            //textbox_select_baud.Text = Settings.Default.Ranger_Baud.ToString();
            //comboBox_select_port.ItemsSource = SerialPort.GetPortNames();
            //comboBox_select_port.SelectedValue = Settings.Default.Last_Port;

            comb3_vals = Enum.GetNames(typeof(TagType));

            Tags = new SortableBindingList<TAG>();
            dataGrid.ItemsSource = Tags;

            VISION_GPS_Device = new SortableBindingList<_Navigation_Parameters>();

            //CurrentOperators = new TestOperatorsInfo();
            //Operators = new List<string>();
            //TOPasswords = new List<string>();

            Bytes_Received = 0;
            cleanup_timer.Start();
            Vision_controller.Start();
            Force_RF_Zone_Timer.Start();


        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.Closing -= Window_Closing;
            Thread CloseDown = new Thread(new ThreadStart(Current_vision.ClosePort));
            Thread.Sleep(100);
        }
        #endregion




        #region timers

        private void cleanup_timer_Tick(object sender, EventArgs e)
        {
            var s = sender as DispatcherTimer;
            if (started != 0)
            {
                ///////////////////////////////////////////////////////////////////
                //// remove unnecessary tags and add new ones. 
                //// its best to do this manually as auto-update can have a severe 
                //// performance impact with large lists.

                if(checkBox_auto_clear.IsChecked == true)
                    Current_vision.clean_taglist((int)exclusion_time.Value);

                List<TAG> rem = new List<TAG>(Tags);

                foreach (TAG t in rem)
                {
                    if (Current_vision.TAG_collection.Contains(t) == false)
                        Tags.Remove(t);
                }
                foreach (TAG t in Current_vision.TAG_collection)
                {
                    if (Tags.Contains(t) == false)
                        Tags.Add(t);
                }

                /////// Filter ////////////////////////////////////////////////////
                if ((bool)checkBox_Name.IsChecked || (bool)checkBox_LF.IsChecked || textBox_listfilter.Text != "")
                {
                    var filter = Tags.Where(t =>
                       /* filter string */
                       ((bool)radioButton_include.IsChecked ^ (t.ToString().IndexOf(textBox_listfilter.Text, StringComparison.OrdinalIgnoreCase) >= 0))
                       /* filter LF */
                       || ((bool)checkBox_LF.IsChecked && t.LF_Data.AgeOpacity <= 0)
                       /* filter Name */
                       || ((bool)checkBox_Name.IsChecked && t.Tag_Name == ""));

                    foreach (TAG t in filter.ToList())
                        Tags.Remove(t);
                }
                ///////////////////////////////////////////////////////////////////

                ///////////////////////////////////////////////////////////////////
                //Tags.ResetBindings();
                label_TagCount.Content = "List Total: " + Tags.Count;
                foreach (TAG t in Tags)
                    t.update_last_seen();

                if(checkBox_RF_log.IsChecked == true)
                {                   
                    Log_RF_count(Tags.Count);
                    Current_vision.clean_taglist((int)exclusion_time.Value);
                }
                if (checkBox_LF_log.IsChecked == true)
                {
                    Log_LF_count(Tags.Count);
                    Current_vision.clean_taglist((int)exclusion_time.Value);
                }
                

                // make the update less frequent when there are many tags
                s.Interval = TimeSpan.FromMilliseconds(40 + Tags.Count * 7); 
            }
        }

        int poll_count = 11;
        private void Vision_Control_Tick(object sender, EventArgs e)
        {
            if (started != 0 && poll_count++ > 6)
            {
                poll_count = 0;
                // send command every so often to keep the port open.
                Current_vision.PollUID();
            }            
            // update the port open/close buttons in case we lost the connection.
            update_port_status();
        }

        private void Force_RF_Zone_Tick(object sender, EventArgs e)
        {
            if (Force_RF_Repeat.Content.ToString() == "Repeat Zone (Presence)")
            {
                
            }
            else if (Force_RF_Repeat.Content.ToString() == "Repeat Zone (Warning)")
            {
                Current_vision.Force_RF_Zone(1);
            }
            else if (Force_RF_Repeat.Content.ToString() == "Repeat Zone (Critical)")
            {
                Current_vision.Force_RF_Zone(2);
            }
            else if(Force_RF_Repeat.Content.ToString() == "Stop Repeat")
            {
                Current_vision.Force_RF_Zone(3);
            }
        }
        #endregion

        #region button handlers


        private void button_bootloader_Click(object sender, RoutedEventArgs e)
        {
            vision_boot.BootWindow VB = new vision_boot.BootWindow();
            VB.Show();
        }

        private void button_set_pulse300_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.SetPulse300Activities();
            button_get_settings_Click(null, null);
        }

        private void button_set_mantag_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.SetPulseMantagActivities();
            button_get_settings_Click(null, null);
        }

        private void button_set_pulse500_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.SetPulse500Activities();
            button_get_settings_Click(null, null);
        }

        private void button_set_ranger_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.SetPulseRangerActivities();
            button_get_settings_Click(null, null);
        }

        private void button_set_gps_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.SetPulseGPSActivities();
            button_get_settings_Click(null, null);
        }
        private void button_set_reader_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.SetReaderActivities();
            button_get_settings_Click(null, null);
        }

        private void button_reset_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.ResetCommand();
        }

        private void button_revision_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.FWRevCommand();
        }

        private void button_get_lf_tags_Click(object sender, RoutedEventArgs e)
        {
            UInt16 db;
            if (UInt16.TryParse(textbox_db.Text, out db))
            {
                Current_vision.TAG_collection.Clear();
                Tags.Clear();
                Current_vision.GetTags(0, 20, db, 0);
            }
        }

        private void button_get_status_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.Getstatus();
        }

        private void button_clearlog_Click(object sender, RoutedEventArgs e)
        {
            TextBox_comlog.Text = "";
            TextBox_comlog.ScrollToEnd();
            Current_vision.Comlog = "";
            Bytes_Received = 0;
        }

        private void button_get_settings_Click(object sender, RoutedEventArgs e)
        {
            //Current_vision.FWRevCommand();
            Current_vision.GetSettings();
            textBlockVisionStatus.Text = "Reading settings";
            PropertyGrid_tag_settings.SelectedObject = null;
//             if (Current_vision.GetSettings() == false)
//                 MessageBox.Show("Failed to retrieve all settings. Sorry..."); 
        }

        private void button_send_Settings_Click(object sender, RoutedEventArgs e)
        {
            textBlockVisionStatus.Text = "Saving altered settings";
            Current_vision.SaveSettings(false);
        }

        private void button_send_all_settings_Click(object sender, RoutedEventArgs e)
        {
            textBlockVisionStatus.Text = "Saving all settings";
            Current_vision.SaveSettings(true);
        }

        private void button_get_tags_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.TAG_collection.Clear();
            Tags.Clear();
            dataGrid.InvalidateVisual();
            Current_vision.GetTags(0, 0, 0xFFFF, 0);
        }

        private void button_settings_Click(object sender, RoutedEventArgs e)
        {
            Interface_Settings InterfaceSettings = new Interface_Settings();
            if (InterfaceSettings.IsActive == false)
            {
                
                if (InterfaceSettings.ShowDialog() == true)
                {
                    UInt32 baud = 0, UID = 0;
                    baud = Convert.ToUInt32(InterfaceSettings.textbox_select_baud.Text);

                    try
                    {
                        bool res;
                        res = Current_vision.OpenPort(InterfaceSettings.comboBox_select_port.Text, baud, 0);

                        if (res != true)
                            System.Windows.MessageBox.Show("The selected port is already open", "Error", MessageBoxButton.OK);
                        else
                        {
                            update_port_status();
                            Refresh_Datagrid();
                            Settings.Default.Ranger_Baud = (int)baud;
                            Settings.Default.Last_Port = InterfaceSettings.comboBox_select_port.SelectedValue.ToString();
                            Settings.Default.Save();
                            started = 1;
                        }
                    }
                    catch (Exception x)
                    {
                        System.Windows.MessageBox.Show(x.Message);
                        Console.WriteLine("button_settings_Click catch Vision Interface");
                    }
                }
                else
                {
                    Current_vision.ClosePort();
                }
            }
                //if ( == true )
            //{
            //    InterfaceSettings.Show();
            //    try
            //    {

            //    }
            //    catch (InvalidOperationException)
            //    {
            //        //MessageBox.Show("Close the open COM Port before opening another", "Information", MessageBoxButtons.OK);
            //    }
            //}
        }
        private void button_open_port_Click(object sender, RoutedEventArgs e)
        {
            //UInt32 baud = 0, UID = 0;
            //baud = Convert.ToUInt32(textbox_select_baud.Text);

            try
            {
                bool res;
                    res = Current_vision.OpenPort(Settings.Default.Last_Port, (uint) Settings.Default.Ranger_Baud, 0);

                if (res != true)
                    System.Windows.MessageBox.Show("The selected port is already open", "Error", MessageBoxButton.OK);
                else
                {
                    update_port_status();
                    Refresh_Datagrid();
                    //Settings.Default.Ranger_Baud = (int)baud;
                    //Settings.Default.Last_Port = comboBox_select_port.SelectedValue.ToString();
                    //Settings.Default.Save();
                    started = 1;
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("Button open Port Click catch Vision Interface");
                System.Windows.MessageBox.Show(x.Message);
            }
        }

        private void button_close_port_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.ClosePort();
            update_port_status();
            Refresh_Datagrid();
        }

        private void update_port_status()
        {
            if(Current_vision.IsPortOpen())
            {
                //button_open_port.IsEnabled = false;
                //button_close_port.IsEnabled = true;

                RadialGradientBrush radialGradient = new RadialGradientBrush();
                radialGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0x00, 0x9B, 0xC9), 0.3));
                radialGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0x99, 0xDB, 0xFA, 0xFB), 1));
                ComActivity.Fill = radialGradient;
            }
            else
            {
                //button_open_port.IsEnabled = true;
                //button_close_port.IsEnabled = false;

                RadialGradientBrush radialGradient = new RadialGradientBrush();
                radialGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00), 0.2));
                radialGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF, 0xDB, 0xFA, 0xFB), 1));
                ComActivity.Fill = radialGradient;
            }
        }

        private void button_run_test_Click(object sender, RoutedEventArgs e)
        {
            if ((comboBox_Test_Type.SelectedIndex == -1) || comboBox_Test_Type.SelectedIndex == 0)
            {
                System.Windows.MessageBox.Show("Please select the test's unit type", "Error", MessageBoxButton.OK);
            }
            else
            {
                //if (Current_vision.IsPortOpen())
                //{
                    Current_vision.current_TAG.Level_Test_Type.Test_Type = comboBox_Test_Type.SelectedIndex;
                    Current_vision.RunTest();
                //}
                //else
                //{
                //     System.Windows.MessageBox.Show("Please open COM Port", "Information", MessageBoxButton.OK);
                // }
            }
        }

        private void button_generate_report_Click(object sender, RoutedEventArgs e)
        {

            //int index = TOPasswords.FindIndex(a => a == textBox_testperson.Text);

            if (test_report != null)
            {
                string[] Board_Info = Regex.Split(Current_vision.current_TAG.Params.PCB_ID, ", ");
                test_report.notes = textBox_test_notes.Text;
                test_report.PCB = textBox_board.Text;
                test_report.Test_Level = comboBox_Test_Type.Text;
                test_report.test_person = textBox_testperson.Text;
                test_report.PCB_Number = "ME-" + Board_Info[4];
                test_report.Firmware_Rev = Board_Info[1];
                test_report.SaveReport(Settings.Default.Report_path);
                System.Windows.MessageBox.Show("Sucessfully generated report for " + test_report.Test_Level + " 0x" + Current_vision.current_TAG.Params.UID.ToString("X2"), "Information", MessageBoxButton.OK);
                textBox_board.Clear();
                textBox_test_notes.Clear();
            }
            else
            {
                System.Windows.MessageBox.Show("Please run the test procedure before generating an report", "Information", MessageBoxButton.OK);
            }
                
        }

        private void button_RFID_config_Click(object sender, RoutedEventArgs e)
        {
            Vision_Tag_Config_Utility V = new Vision_Tag_Config_Utility();
            V.Show();
        }

        #endregion

        /// /////////////////////////////// /// ///////////////////////////////


        #region UI misc


        //private void comboBox_select_port_SourceUpdated(object sender, DataTransferEventArgs e)
        //{
        //    //   comboBox_port_select.DataSource = System.IO.Ports.SerialPort.GetPortNames();
        //    comboBox_select_port.SelectedValue = Settings.Default.Last_Port;
        //    textbox_select_baud.Text = Settings.Default.Ranger_Baud.ToString();
        //}

        private void Refresh_Datagrid()
        {
            dataGrid.ItemsSource = null;
            dataGrid.Columns.Clear();

            dataGrid.AutoGenerateColumns = true;
            Tags.unsort();
            dataGrid.ItemsSource = Tags;
            dataGrid.IsReadOnly = true;

            dataGrid.ColumnWidth = DataGridLength.Auto;

            dataGrid.UnselectAll();
            Tags.Clear();
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // reset the databinding of the datagridview
            if (tabControl.SelectedIndex == 0 && Current_vision != null)
            {
                Refresh_Datagrid();

                button_generate_report.IsEnabled = false;
                frame_test_col.Background = Brushes.White;
                label_module_name.Content = "Vision Module: ";
                label_test_success.Content = "Working: ";
                label_module_name.Foreground = Brushes.Black;
                label_test_success.Foreground = Brushes.Black;
            }
            else if (tabControl.SelectedIndex == 1)
            {
                comboBox_Test_Type.ItemsSource = Enum.GetValues(typeof(TAG._Level_Test));
                //comboBox_Test_Operators.ItemsSource = CurrentOperators.Operators_NameSurname;
            }
            else if (tabControl.SelectedIndex == 2)
            {

            }
            PropertyGrid_tag_settings.SelectedObject = null;
        }
        #endregion

        #region Vision


        private delegate void vision_update_delegate(string prop);
        private void vision_property_changed(string prop)
        {
            Dispatcher.Invoke(DispatcherPriority.Send, new vision_update_delegate(handle_vision_data), prop);
        }

         private void handle_vision_data(string prop)
        {
            if (prop == "tag_list")
            {

            }
            else if (prop == "UID")
            {
                textBoxCurUID.Text = Current_vision.current_TAG.Params.UID.ToString("X8");
            }
            else if (prop == "current_settings")
            {
                //PropertyGrid_tag_settings.SelectedObject = Current_vision.current_TAG.Params;
                //PropertyGrid_tag_settings.ExpandAllGridItems();
            }
            else if (prop == "new device")
            {
                if (Current_vision.isCantester)
                    textBlockVisionStatus.Text = "Connected to CAN bus VISION Device";
                else if (Current_vision.Remote_connect)
                    textBlockVisionStatus.Text = "Connected to remote VISION Device 0x" + Current_vision.Remote_Connect_UID.ToString("X8");
                else
                    textBlockVisionStatus.Text = "Connected to new VISION Device";
                PropertyGrid_tag_settings.SelectedObject = null;
            }
            else if (prop == "current_status")
            {
                PropertyGrid_tag_settings.SelectedObject = Current_vision.current_TAG.status;
                PropertyGrid_tag_settings.ExpandAllGridItems();
                textBlockVisionStatus.Text = "Successfully sent command: Get status";

                if (tabControl.SelectedIndex == 1)
                {
                    test_report = Current_vision.current_TAG.get_test_report();
                    if (test_report != null)
                    {
                        string[] Board_Info = Regex.Split(Current_vision.current_TAG.Params.PCB_ID, ", ");
                        test_report.Show_test_reportWPF(ref dataGrid);
                        if (test_report.Pass)
                        {
                            if (textBox_testperson.Text != "") button_generate_report.IsEnabled = true;
                            frame_test_col.Background = Brushes.Green;
                            if(Current_vision.Remote_connect && textBoxListUID.Text != "")
                            {
                                label_module_name.Content = "Vision Module UID: 0x" + Current_vision.Remote_Connect_UID.ToString("X8");
                            }
                            else
                                label_module_name.Content = "Vision Module UID: 0x" + Current_vision.current_TAG.Params.UID.ToString("X8");

                            label_test_success.Content = "Working: " + test_report.Pass.ToString();
                            label_PCB_Info.Content = "PCB Number: ME-" + Board_Info[4];
                            label_module_name.Foreground = Brushes.White;
                            label_test_success.Foreground = Brushes.White;
                            label_PCB_Info.Foreground = Brushes.White;
                            textBlockVisionStatus.Text = "Device test passed.";
                        }
                        else
                        {
                            if (textBox_testperson.Text != "") button_generate_report.IsEnabled = true;
                            frame_test_col.Background = Brushes.Red;
                            if (Current_vision.Remote_connect && textBoxListUID.Text != "")
                            {
                                label_module_name.Content = "Vision Module UID: 0x" + Current_vision.Remote_Connect_UID.ToString("X8");
                            }
                            else
                                label_module_name.Content = "Vision Module UID: 0x" + Current_vision.current_TAG.Params.UID.ToString("X8");

                            label_test_success.Content = "Working: " + test_report.Pass.ToString();
                            label_PCB_Info.Content = "PCB Number: ME-" + Board_Info[4];
                            label_module_name.Foreground = Brushes.White;
                            label_test_success.Foreground = Brushes.White;
                            label_PCB_Info.Foreground = Brushes.White;
                            textBlockVisionStatus.Text = "Device test failed.";
                        }
                    }
                }
            }
            else if (prop == "comlog")
            {
                Storyboard s = (Storyboard)TryFindResource("sb_com_activity");
                s.Begin();   // Stop animation

                if ((bool)checkBox_log_enable.IsChecked)
                {
                    //TextBox_comlog.Text = Current_vision.Comlog;
                    if(Current_vision.TAG_collection.Count>0)
                        TextBox_comlog.Text = Current_vision.TAG_collection[0].Latitude + " , " + Current_vision.TAG_collection[0].Longitude;

                    TextBox_comlog.ScrollToEnd();
                    TextBox_comlog.SelectionStart = TextBox_comlog.Text.Length;
                }
            }
            else if (prop.StartsWith("Failed"))
            {
                textBlockVisionStatus.Text = prop;
                MessageBox.Show(prop);
            }
            else if (prop == "Successfully sent command: Get settings")
            {
                PropertyGrid_tag_settings.SelectedObject = Current_vision.current_TAG.Params;
                //Console.WriteLine(Current_vision.current_TAG.Params.firmware_subrev);
                //PropertyGrid_tag_settings.ExpandAllGridItems();
                CollapseFunctions();
                textBlockVisionStatus.Text = prop;
            }
            else if (prop == "Firmware revision")
            {
                textBlockVisionStatus.Text = prop + ": " + Current_vision.current_TAG.Params.firmware_rev + "." + Current_vision.current_TAG.Params.firmware_subrev;
                Console.WriteLine(Current_vision.current_TAG.Params.firmware_subrev);
            }      
            else if (prop == "Data message UID")
                textBlockVisionStatus.Text = "Data message to me: " + Encoding.ASCII.GetString(Current_vision.remote_data_RX);
            else if (prop == "Data message VID")
                textBlockVisionStatus.Text = "Data message to our vehicle: " + Encoding.ASCII.GetString(Current_vision.remote_data_RX);
            else if (prop == "Data message Global")
            {
                textBlockVisionStatus.Text = "Data message to everyone: " + Encoding.ASCII.GetString(Current_vision.remote_data_RX);
                TextBox_comlog.AppendText(Encoding.ASCII.GetString(Current_vision.remote_data_RX) + "\n");
                TextBox_comlog.ScrollToEnd();
                if (TextBox_comlog.SelectionLength == 0)
                    TextBox_comlog.SelectionStart = TextBox_comlog.Text.Length;
            }
            else if (prop == "board info")
            {
                textBlockVisionStatus.Text = "Got board info: " + Current_vision.current_TAG.board_info;
            }
            else if (prop == "GPS Data")
            {
                if (tabControl.SelectedIndex != 1)
                {
                    Show_GPS_Information(ref dataGrid);
                    GPS_device = true;
                }                 
            }
            else if (prop == "Reset GPS Odometer")
            {
                textBlockVisionStatus.Text = "Successfully sent command: Reset GPS Odometer";
            }
            else
            {
                textBlockVisionStatus.Text = prop;
            }
        }

        public void CollapseFunctions()
        {
            System.Windows.Forms.GridItem root = this.PropertyGrid_tag_settings.SelectedGridItem;
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

        #endregion

        //private void comboBox_select_port_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        //{
        //    comboBox_select_port.ItemsSource = SerialPort.GetPortNames();
        //    comboBox_select_port.SelectedValue = Settings.Default.Last_Port;
        //}

        //private void comboBox_select_port_DropDownOpened(object sender, EventArgs e)
        //{
        //    comboBox_select_port.ItemsSource = SerialPort.GetPortNames();
        //    comboBox_select_port.SelectedValue = Settings.Default.Last_Port;
        //}

        private void Update_Remote()
        {
            UInt32 UID = 0;
            if (UInt32.TryParse(textBoxListUID.Text, NumberStyles.HexNumber, null, out UID))
            {
                Current_vision.Remote_Connect_UID = UID;
            }

            if (UID != 0)
            {
                if(Current_vision.Remote_connect = (bool)(checkBox_remote_connection.IsChecked))
                    textBlockVisionStatus.Text = "Try remote connect to " + UID.ToString("X8");
            }
            else
            {
                Current_vision.Remote_connect = false;
                checkBox_remote_connection.IsChecked = false;
            }
        }

        private TAG selectedTag;

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UInt32 selectedUID = 0;
            DataGrid dg = sender as DataGrid;
            var item = dg.SelectedItem;
            if (dg.SelectedItem is TAG)
            {
                selectedTag = item as TAG;
                selectedUID = ((TAG)(dg.SelectedItem))._UID;
                textBoxListUID.Text = selectedUID.ToString("X8");
                Update_Remote();
                PropertyGrid_tag_settings.SelectedObject = null;
            }
            else
                selectedTag = null;
            e.Handled = true;
        }

        private void checkBox_remote_connection_Checked_changed(object sender, RoutedEventArgs e)
        {
            Update_Remote();
            PropertyGrid_tag_settings.SelectedObject = null;
        }


        private void dataGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // Modify the header to remove all underscores
            e.Column.Header = e.Column.Header.ToString().Replace('_', ' ');

            // Replace the LF Data column with a custom template column.
            if (e.PropertyName == "LF_Data")
            {
                // Create a new template column.
                DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
                templateColumn.Header = "LF Data";
                templateColumn.CellTemplate = (DataTemplate)Resources["LFTemplate"];
                templateColumn.SortMemberPath = "LF_Data";
                // Replace the auto-generated column with the templateColumn.
                e.Column = templateColumn;
                e.Column.MinWidth = 100;
            }
            // Replace TypeIcon column with a image column.
            if (e.PropertyName == "typeIconUri")
            {
                // Create a new template column.
                DataGridTemplateColumn templateColumn = new DataGridTemplateColumn();
                templateColumn.Header = "Icon";
                templateColumn.CellTemplate = (DataTemplate)Resources["TypeIconTemplate"];
                templateColumn.SortMemberPath = "LF_Data";
                // Replace the auto-generated column with the templateColumn.
                e.Column = templateColumn;
            }
        }

        private void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // Get the DataRow corresponding to the DataGridRow that is loading.
            var item = e.Row.Item as report_test;
            if (item != null)
            {
                // Set the background color of the DataGrid row based on whatever data you like from the row.
                if (item.Result == "Pass")
                {
                    e.Row.Background = System.Windows.Media.Brushes.LightGreen;
                    //                     row.DefaultCellStyle.ForeColor = Color.White;
                }
                else if (item.Result == "Not used")
                {
                    e.Row.Background = System.Windows.Media.Brushes.LightYellow;
                    //                     row.DefaultCellStyle.ForeColor = Color.Black;
                }
                else 
                {
                    e.Row.Background = System.Windows.Media.Brushes.PaleVioletRed;
                    //                     row.DefaultCellStyle.ForeColor = Color.White;
                }
            }
        }

        private void checkBox_log_enable_Checked(object sender, RoutedEventArgs e)
        {
            Current_vision.ShowLog = true;
            //Col_Log.Width = new GridLength(4, GridUnitType.Star);
        }

        private void checkBox_log_enable_Unchecked(object sender, RoutedEventArgs e)
        {
            Current_vision.ShowLog = false;
            //Col_Log.Width = new GridLength(1, GridUnitType.Star);
        }

        private void dataGrid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var s = sender as DataGrid;
            // remove the auto-sort from the dataGrid when the focus is lost. 
            ICollectionView view = CollectionViewSource.GetDefaultView(s.ItemsSource);
            if (view != null && view.SortDescriptions != null)
            {
                view.SortDescriptions.Clear();
                foreach (var column in s.Columns)
                {
                    column.SortDirection = null;
                }
            }
        }

        private void dataGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var s = sender as FrameworkElement;
            var m = new ContextMenu();

            if (selectedTag != null)
            {
                if (selectedTag.Kind == TAG._kind.Pulse && (selectedTag.statusB & 0x20) != 0)
                {
                    var i0 = new MenuItem();
                    i0.Header = "Connect to Tag " + selectedTag.UID;
                    i0.Click += cm_click_connect;
                    var i1 = new MenuItem();
                    i1.Header = "Boot-load Tag " + selectedTag.UID;
                    i1.Click += cm_click_boot;

                    m.Items.Add(i0);
                    m.Items.Add(i1);
                    s.ContextMenu = m;
                }
                else
                {
                    e.Handled = true;
                    return;
                }
            }
            else
                e.Handled = true;
        }

        private void cm_click_connect(object sender, RoutedEventArgs e)
        {
            checkBox_remote_connection.IsChecked = true;
        }

        /// <summary>
        /// Open a boot-loader window and point it to this remote tag. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cm_click_boot(object sender, RoutedEventArgs e)
        {
            Current_vision.ClosePort();
            //BootWindow B = new BootWindow((string)comboBox_select_port.SelectedValue, selectedTag.UID);
            BootWindow B = new BootWindow(Settings.Default.Last_Port, selectedTag.UID);
            B.Show();
            B.Closed += bootloader_closed;
            update_port_status();
        }

        private void bootloader_closed(object sender, EventArgs e)
        {
            // reopen the port when the boot-loading is done. 
            button_open_port_Click(sender, null);
        }

        public void Show_GPS_Information(ref System.Windows.Controls.DataGrid DG)
        {
            DG.ItemsSource = null;

            DG.AutoGenerateColumns = true;
            DG.ItemsSource = VISION_GPS_Device;
            VISION_GPS_Device.Clear();

            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Latitude", Value = Current_vision.current_TAG.GPS_Data.Latitude_deg, Unit = "degrees", Raw_data = Current_vision.current_TAG.GPS_Data.Latitude.ToString(), Raw_unit = "degrees" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Longitude" , Value = Current_vision.current_TAG.GPS_Data.Longitude_deg, Unit= "degrees", Raw_data = Current_vision.current_TAG.GPS_Data.Longitude.ToString(), Raw_unit = "degrees" } );
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Horizontal accuracy", Value = Current_vision.current_TAG.GPS_Data.HorizontalAccuracy_m.ToString(), Unit = "m", Raw_data = Current_vision.current_TAG.GPS_Data.HorizontalAccuracy.ToString(), Raw_unit = "mm" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Vertical accuracy", Value = Current_vision.current_TAG.GPS_Data.VerticalAccuracy_m.ToString(), Unit = "m", Raw_data = Current_vision.current_TAG.GPS_Data.VerticalAccuracy.ToString(), Raw_unit = "mm" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Speed", Value = Current_vision.current_TAG.GPS_Data.Speed_km_h.ToString(), Unit = "km/h", Raw_data = Current_vision.current_TAG.GPS_Data.Speed.ToString(), Raw_unit = "mm/s"});
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Speed accuracy", Value = Current_vision.current_TAG.GPS_Data.SpeedAccuracy_km_h.ToString(), Unit = "km/h", Raw_data = Current_vision.current_TAG.GPS_Data.SpeedAccuracy.ToString(), Raw_unit = "mm/s"});
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Vehicle heading", Value = Current_vision.current_TAG.GPS_Data.HeadingVehicle_deg.ToString(), Unit = "degrees", Raw_data = Current_vision.current_TAG.GPS_Data.HeadingVehicle.ToString(), Raw_unit = "degrees"});
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Motion heading", Value = Current_vision.current_TAG.GPS_Data.HeadingMotion_deg.ToString(), Unit = "degrees", Raw_data = Current_vision.current_TAG.GPS_Data.HeadingMotion.ToString(), Raw_unit = "degrees"});
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Heading accuracy", Value = Current_vision.current_TAG.GPS_Data.HeadingAccuracy_deg.ToString(), Unit = "degrees", Raw_data = Current_vision.current_TAG.GPS_Data.HeadingAccuracy.ToString(), Raw_unit = "degrees" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Fix type", Value = (Current_vision.current_TAG.GPS_Data.FixType).ToString().Remove(0, 1), Unit = "-", Raw_data = ((int)Current_vision.current_TAG.GPS_Data.FixType).ToString(), Raw_unit = "-" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Fix age", Value = Current_vision.current_TAG.GPS_Data.FixAge.ToString(), Unit = "s", Raw_data = Current_vision.current_TAG.GPS_Data.FixAge.ToString(), Raw_unit = "s" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Number of satellites", Value = Current_vision.current_TAG.GPS_Data.NumberOfSat.ToString(), Unit = "-", Raw_data = ((int)Current_vision.current_TAG.GPS_Data.NumberOfSat).ToString(), Raw_unit = "-" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Height above sea level", Value = Current_vision.current_TAG.GPS_Data.Sealevel_m.ToString(), Unit = "m", Raw_data = Current_vision.current_TAG.GPS_Data.Sealevel.ToString(), Raw_unit = "mm" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Traveled Distance", Value = Current_vision.current_TAG.GPS_Data.TravelDistance_km.ToString(), Unit = "km", Raw_data = Current_vision.current_TAG.GPS_Data.TravelDistance.ToString(), Raw_unit = "m" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Total Traveled Distance", Value = Current_vision.current_TAG.GPS_Data.TotalTravelDistance_km.ToString(), Unit = "km", Raw_data = Current_vision.current_TAG.GPS_Data.TotalTravelDistance.ToString(), Raw_unit = "m"});
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Heading valid", Value = Current_vision.current_TAG.GPS_Data.HeadingValidity.ToString(), Unit = "bool", Raw_data = "0x" + Current_vision.current_TAG.GPS_Data.Flags.ToString("X2"), Raw_unit = "-" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Fix valid", Value = Current_vision.current_TAG.GPS_Data.GNSSFixOK.ToString(), Unit = "bool", Raw_data = "0x" + Current_vision.current_TAG.GPS_Data.Flags.ToString("X2"), Raw_unit = "-" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Antenna Status", Value = Current_vision.current_TAG.GPS_Data.AntennaState.ToString().Remove(0, 1), Unit = "-", Raw_data = "0x" + ((int)Current_vision.current_TAG.GPS_Data.AntennaState).ToString().PadLeft(2,'0'), Raw_unit = "-" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Power Save Mode", Value = Current_vision.current_TAG.GPS_Data.PSMState.ToString().Remove(0, 1), Unit = "-", Raw_data = "0x" + Current_vision.current_TAG.GPS_Data.Flags.ToString("X2"), Raw_unit = "-" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Date", Value = Current_vision.current_TAG.GPS_Data._DateString, Unit = "UTC", Raw_data = "0x" + Current_vision.current_TAG.GPS_Data._Date.ToString("X2"), Raw_unit = "-" });
            VISION_GPS_Device.Add(new _Navigation_Parameters { Navigation_Parameter = "Time", Value = Current_vision.current_TAG.GPS_Data._TimeString, Unit = "UTC", Raw_data = "0x" + Current_vision.current_TAG.GPS_Data._Time.ToString("X2"), Raw_unit = "-" });

            DG.UnselectAll();
        }

        private void Force_RF_Critical_Zone(object sender, RoutedEventArgs e)
        {
            Current_vision.Force_RF_Zone(3);
        }

        private void Force_RF_Warning_Zone(object sender, RoutedEventArgs e)
        {
            Current_vision.Force_RF_Zone(2);
        }

        private void Force_RF_Presence_Zone(object sender, RoutedEventArgs e)
        {
            Current_vision.Force_RF_Zone(1);
        }

        private void Force_LF_Critical_Zone(object sender, RoutedEventArgs e)
        {
            if(selectedTag != null)
            {
                Current_vision.Force_LF_Zone(20, selectedTag.VID, 12);
            }
            else
            {
                Current_vision.Remote_connect = true;
                foreach (TAG item in Tags)
                {
                    Current_vision.Remote_Connect_UID = item._UID;
                    Current_vision.Force_LF_Zone(20, item.VID, 12);
                    Thread.Sleep(10);
                }
                Current_vision.Remote_connect = false;
                Current_vision.Remote_Connect_UID = 0;
            }

        }

        private void Force_LF_Warning_Zone(object sender, RoutedEventArgs e)
        {
            if ((selectedTag != null)&&(checkBox_remote_connection.IsChecked==true))
            {
                Current_vision.Force_LF_Zone(3, selectedTag.VID, 12);
            }
            else
            {
                Current_vision.Remote_connect = true;
                foreach (TAG item in Tags)
                {                    
                    Current_vision.Remote_Connect_UID = item._UID;
                    Current_vision.Force_LF_Zone(3, item.VID, 12);
                    Thread.Sleep(10);     
                }
                Current_vision.Remote_connect = false;
                Current_vision.Remote_Connect_UID = 0;
            }
        }

        private void Force_LF_Presence_Zone(object sender, RoutedEventArgs e)
        {
            if (selectedTag != null)
            {
                Current_vision.Force_LF_Zone(1, selectedTag.VID, 12);
            }
            else
            {
                Current_vision.Remote_connect = true;
                foreach (TAG item in Tags)
                {
                    Current_vision.Remote_Connect_UID = item._UID;
                    Current_vision.Force_LF_Zone(1, item.VID, 12);
                    Thread.Sleep(10);
                }
                Current_vision.Remote_connect = false;
                Current_vision.Remote_Connect_UID = 0;
            }
        }

        private void Force_RF_Repeat_Zone(object sender, RoutedEventArgs e)
        {
            if (Force_RF_Repeat.Content.ToString() == "Repeat GPS Zone")
            {
                Force_RF_Repeat.Content = "Repeat Zone (Presence)";
                Force_RF_Repeat.Background = Brushes.LightBlue;
            }
            else if (Force_RF_Repeat.Content.ToString() == "Repeat Zone (Presence)")
            {
                Force_RF_Repeat.Content = "Repeat Zone (Warning)";
                Force_RF_Repeat.Background = Brushes.LightYellow;
            }
            else if (Force_RF_Repeat.Content.ToString() == "Repeat Zone (Warning)")
            {
                Force_RF_Repeat.Content = "Repeat Zone (Critical)";
                Force_RF_Repeat.Background = Brushes.HotPink;
            }
            else if(Force_RF_Repeat.Content.ToString() == "Repeat Zone (Critical)")
            {
                Force_RF_Repeat.Content = "Stop Repeat";
            }
            else if (Force_RF_Repeat.Content.ToString() == "Stop Repeat")
            {
                Force_RF_Repeat.Content = "Repeat GPS Zone";
            }
        }

        private void button_reset_odo_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.Reset_GPS_ODO();
        }

        private void button_Load_Click(object sender, RoutedEventArgs e)
        {
            //CurrentOperators.TestOperators_Info();
            
            //Operators = CurrentOperators.Operators_NameSurname;
            
            //TOPasswords = CurrentOperators.Get_TestOperators_Passwords;
            /*Excel.Application XL_TestOperators = new Excel.Application();
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            Excel.Range xlRange;
            xlWorkBook = XL_TestOperators.Workbooks.Open("D:/Users/FrancoisHattingh/Desktop/TITAN_VISION_TestOperators/TITAN_VISION_TestOperators.xlsx");

            try
            {
                xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);
                xlRange = xlWorkSheet.UsedRange;

                for (int rCnt = 2; rCnt <= xlRange.Rows.Count; rCnt++)
                {
                    //for (int cCnt = 1; cCnt <= xlRange.Columns.Count; cCnt++)
                    //{
                    Operators.Add(((xlRange.Cells[rCnt, 2] as Excel.Range).Value).ToString() + " " + ((xlRange.Cells[rCnt, 3] as Excel.Range).Value).ToString());
                    TOPasswords.Add(((xlRange.Cells[rCnt, 4] as Excel.Range).Value).ToString());
                    //}
                }
            }
            catch (Exception ex)
            {
                //some error msg
            }
            finally
            {
                comboBox_Test_Operators.ItemsSource = Operators;
                xlWorkBook.Close();
                XL_TestOperators.Quit();
            }*/
        }

        private void enable_Generate_Button(object sender, TextChangedEventArgs e)
        {
            if (button_generate_report.IsEnabled == false)
            {
                if ((textBox_testperson.Text != "") && (textBox_board.Text != ""))
                {
                    button_generate_report.IsEnabled = true;
                }
            }
            else
            {
                if ((textBox_testperson.Text == "") || (textBox_board.Text == ""))
                {
                    button_generate_report.IsEnabled = false;
                }
            }

        }

       
        private void _MenuExit_Click(object sender, RoutedEventArgs e)
        {
           System.Windows.Application.Current.MainWindow.Close();
        }

        private void ComActivity_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                bool res;
                res = Current_vision.IsPortOpen();

                if (res == true)
                    button_close_port_Click(sender, null);
                else
                {
                    button_open_port_Click(sender, null);
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("CommActivity_mouse catch Vision Interface");
                System.Windows.MessageBox.Show(x.Message);
            }
        }

        private void button_about_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow.MainWindow VisionAbout= new AboutWindow.MainWindow();

            VisionAbout.Show();
 
        }

        private void button_test_stuff_Click(object sender, RoutedEventArgs e)
        {
            Boolean B = false;

            Mernok_Asset_group_list = new byte[mernokAssetFile.mernokAssetList.Capacity];
            byte[] Mernok_tempGroup = new byte[4];
            
            ushort offset = 0;
            ushort j = 0;
            foreach (MernokAsset item in mernokAssetFile.mernokAssetList)
            {
                Mernok_Asset_group_list[j] = item.Group;
                //Mernok_Asset_group_list[j] = 0xFF;
                j++;
            }
            int kj = 0;
            for (int i = 0; i < (Mernok_Asset_group_list.Length) / 4; i++)
            {
                Array.Copy(Mernok_Asset_group_list, offset, Mernok_tempGroup, 0, 4);
                B = MernokRFID_interface.write_block((ushort)((0x100 + i * 4)/4), Mernok_tempGroup, 0);
                if (B)
                {
                    kj++;
                }
                offset = (UInt16)(offset + 4);


            }
            B = MernokRFID_interface.write_block((ushort)(((int)VisionParams.adr.Mernok_Asset_list_rev ) / 4), BitConverter.GetBytes(mernokAssetFile.version), 0);

            if (kj == Mernok_Asset_group_list.Length / 4)
            {
                MessageBox.Show("success!");
            }

        }

        private void button_test_stuff2_Click(object sender, RoutedEventArgs e)
        {

            Mernok_Asset_group_list_test = new byte[64];
            for (int i = 0; i < 16; i++)
            {
                byte[] Check_read = new byte[4];
                MernokRFID_interface.read_block((ushort)((0x100 + i * 4) / 4), ref Check_read, 0);
                Array.Copy(Check_read, 0, Mernok_Asset_group_list_test, i * 4, 4);
            }
        }

        #region GPSstuff

        public struct PointAndInfo
        {
            public PointLatLng Point;
            public string Info;

            public PointAndInfo(PointLatLng point, string info)
            {
                Point = point;
                Info = info;
            }
        }

        List<GMapMarker> Circles = new List<GMapMarker>();
        List<PointAndInfo> objects = new List<PointAndInfo>();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (((GPS_device) && (Current_vision.current_TAG.GPS_Data.FixType == TAG._GPS_FixType._3D)) || ((Current_vision.TAG_collection.Count > 0) && (Current_vision.TAG_collection[0].Fix_Type == TAG._GPS_FixType._3D)))
            {
                dispatcherTimer.Start();
            }
            else
            {
                dispatcherTimer.Stop();
                mapView.Markers.Clear();
            }

            GMap.NET.PointLatLng pointLatLng = new GMap.NET.PointLatLng();
            pointLatLng.Lng = 28.164986;
            pointLatLng.Lat = -25.881338;
            GMapMarker m = new GMapMarker(pointLatLng);
            m.Shape = new CustomMarkerRed(this, m, "Mernok Elektronik");
            m.ZIndex = GmarkerIndx;
            mapView.Markers.Add(m);
            mapView.ZoomAndCenterMarkers(null);
        }

        void UpdateCircle(Circle c)
        {
            var pxCenter = mapView.FromLatLngToLocal(c.Center);
            var pxBounds = mapView.FromLatLngToLocal(c.Bound);

            double a = (double)(pxBounds.X - pxCenter.X);
            double b = (double)(pxBounds.Y - pxCenter.Y);
            var pxCircleRadius = Math.Sqrt(a * a + b * b);

            c.Width = 55 + pxCircleRadius * 2;
            c.Height = 55 + pxCircleRadius * 2;
            (c.Tag as GMapMarker).Offset = new System.Windows.Point(-c.Width / 2, -c.Height / 2);
        }
        

        private void mapView_Loaded(object sender, RoutedEventArgs e)
        {
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;
            mapView.MapProvider = GMap.NET.MapProviders.GoogleSatelliteMapProvider.Instance;
            mapView.MinZoom = 0;
            mapView.MaxZoom = 50;
            mapView.Zoom = 18;
            mapView.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            mapView.CanDragMap = true;
            mapView.ShowCenter = false;
            mapView.SetPositionByKeywords("Mernok Elektronik");
        }


        Random rnd = new Random();
        
        int GmarkerIndx = 0;

        
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if((Current_vision.TAG_collection.Count>0)||(Current_vision.current_TAG.GPS_Data.FixType == TAG._GPS_FixType._3D))
            {
                
                PointLatLng pointLatLng = new PointLatLng();
                RotateTransform rotateTransform = new RotateTransform();

                if (!GPS_device)
                {
                    pointLatLng.Lng = Convert.ToDouble(Current_vision.TAG_collection[0].Longitude.Replace(".", ","));
                    pointLatLng.Lat = Convert.ToDouble(Current_vision.TAG_collection[0].Latitude.Replace(".", ","));
                    rotateTransform.Angle = Current_vision.TAG_collection[0].Heading_v;
                }
                else
                {
                    pointLatLng.Lng = Current_vision.current_TAG.GPS_Data.Longitude/ 10000000.0;
                    pointLatLng.Lat = Current_vision.current_TAG.GPS_Data.Latitude / 10000000.0;
                    rotateTransform.Angle = Current_vision.current_TAG.GPS_Data.HeadingVehicle / 100000.0;
                    //int Ang = rnd.Next(1, 360);
                    //rotateTransform.Angle = Ang;
                }
                GMapMarker m = new GMapMarker(pointLatLng)
                {
                    ZIndex = GmarkerIndx + 1
                };

                m.Shape = new CustomMarkerRed(this, m, pointLatLng.Lat.ToString() + " ' " + pointLatLng.Lng.ToString());
                rotateTransform.CenterX =  15;
                rotateTransform.CenterY =  0;
                m.Shape.RenderTransform = rotateTransform;

                GPS_points.Content = "Counter: " + mapView.Markers.Count().ToString();
                mapView.Markers.Add(m);
                if(mapView.Markers.Count()>(60*10))
                {
                    //mapView.Markers.Remove(mapView.Markers.First());
                    dispatcherTimer.Stop();
                    GmarkerIndx = 0;
                    MessageBox.Show("GPS coordinates taken for 600 points");
                }
            }
            
            //mapView.ZoomAndCenterMarkers(null);

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            mapView.Markers.Clear();
            GmarkerIndx = 0;
        }
        #endregion

        #region ZONE demo circles
        private void Forse_LF_GPS_zone_messages(uint LF_RSSI, uint SID, uint zone)
        {
            if (GPS_selector_lbl.Content.ToString() == "LF")
            {
                if (selectedTag != null)
                {
                    Current_vision.Force_LF_Zone(LF_RSSI, selectedTag.VID, SID);
                }
                else
                {
                    Current_vision.Remote_connect = true;
                    foreach (TAG item in Tags)
                    {
                        Current_vision.Remote_Connect_UID = item._UID;
                        Current_vision.Force_LF_Zone(LF_RSSI, VID: item.VID, SID: SID);
                        Thread.Sleep(10);
                    }
                    Current_vision.Remote_connect = false;
                    Current_vision.Remote_Connect_UID = 0;
                }
            }
            else
            {
                Current_vision.Force_RF_Zone(zone);
            }


                
        }

        private void Circle_Right_Blue_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(1, 3, 1);
        }

        private void Circle_Bottom_Blue_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(1, 6,1);
        }

        private void Circle_Left_Blue_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(1, 9,1);
        }

        private void Circle_Top_Blue_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(1, 12,1);
        }

        private void Circle_Right_Yellow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(4, 3,2);
        }

        private void Circle_Top_Yellow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(4, 12,2);
        }

        private void Circle_Left_Yellow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(4, 9,2);
        }

        private void Circle_Bottom_Yellow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(4, 6,2);
        }

        private void Circle_Right_Red_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(8, 3, 3);
        }

        private void Circle_Bottom_Red_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(8,  6,3);
        }

        private void Circle_Left_Red_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(8, 9,3);
        }

        private void Circle_Top_Red_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Forse_LF_GPS_zone_messages(8,  12, 3);
        }

        private void Circle_Middle_Black_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(GPS_selector_lbl.Content.ToString() == "LF")
            {
                GPS_selector_lbl.Content = "GPS";
            }
            else
            {
                GPS_selector_lbl.Content = "LF";
            }
        }

        private void GPS_selector_lbl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Circle_Middle_Black_MouseLeftButtonDown(null, null);
        }


        private void Rectangle_Section_MouseLeave(object sender, MouseEventArgs e)
        {
            Rectangle rectangle = (Rectangle)sender;
            rectangle.Opacity = 0.5;
        }

        private void Rectangle_Section_MouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle rectangle = (Rectangle)sender;
            rectangle.Opacity = 1;
        }

        #endregion

        #region RF logging functions
        private void Log_RF_count(int Count)
        {
            string path = @"D:\Users\NeilPretorius\Desktop\M-PSW-007-014-00\LoggingRF.txt";

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();

                using (TextWriter tw = new StreamWriter(path))
                {
                    tw.WriteLine("This file is generated to test the amount of times that a tag is lost for a log period of time");
                }

            }
            else if (File.Exists(path))
            {
                using (TextWriter tw = new StreamWriter(path,true))
                {
                    tw.WriteLine("Time: " + DateTime.Now.ToString() + "-- count:" + Count.ToString());
                }
            }
        }

        private void Log_LF_count(int Count)
        {
            string path = @"D:\Users\NeilPretorius\Desktop\M-PSW-007-014-00\LoggingLF.txt";

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();

                using (TextWriter tw = new StreamWriter(path))
                {
                    tw.WriteLine("This file is generated to test the amount of times that a tag is lost for a log period of time");
                }

            }
            else if (File.Exists(path))
            {
                using (TextWriter tw = new StreamWriter(path, true))
                {
                    tw.WriteLine("Time: " + DateTime.Now.ToString() + "-- count:" + Count.ToString());
                }
            }
        }


        #endregion
        int speeder = 0;
        private void Button_set_speed_Click(object sender, RoutedEventArgs e)
        {
            Current_vision.Set_Device_Speed((uint)(speeder+=10)* 50, (bool)Reverse.IsChecked);
        }
    }
}

