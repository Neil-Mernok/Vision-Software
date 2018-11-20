using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using System.IO;
using System.Windows.Threading;
using System.Threading;
using Microsoft.Win32;
using System.Diagnostics;
using Vision_Bootloader;
using USB_PORTS;
using CANTESTER;


namespace vision_boot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BootWindow : Window
    {
        public const string VID = "0403", PID = "7E44";
        Vision_Booter Bootloader;
        bool update_portlist = true;                // tell the timer we can update the port list.
        Task boot_task;
        String incomming_port = "";

        public BootWindow()
        {
            InitializeComponent();
        }

        public BootWindow(String Port, String remoteTag)
        {
            InitializeComponent();
            incomming_port = Port;
            TextBox_SlaveID.Text = remoteTag;
        }

        private void Bootloader_window_Loaded(object sender, RoutedEventArgs e)
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(check_progress);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            dispatcherTimer.Start();
            ComboBox_Com_Port.SelectedIndex = 0;
        }

       
        private void Bootloader_window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(Bootloader != null)
                Bootloader.close();
        }
                     

        private void ComboBox_Com_Port_MouseEnter(object sender, MouseEventArgs e)
        {
            update_portlist = false;
        }

        private void disable_buttons()
        {
            Button_RunBooter.IsEnabled = false;
            Button_RunBooter_last.IsEnabled = false;
            button_default_mantag.IsEnabled = false;
            button_default_module.IsEnabled = false;
        }

        private bool boot_open_and_check()
        {
            // try to open the selected port...
            try
            {
                update_portlist = false;
                // check for remote UID.
                if (TextBox_SlaveID.Text.Length == 8 && TextBox_SlaveID.Text.All(c => "0123456789abcdefABCDEF".Contains(c)))
                    Bootloader = new Vision_Booter((string)ComboBox_Com_Port.SelectedValue, TextBox_SlaveID.Text);
                else
                    Bootloader = new Vision_Booter((string)ComboBox_Com_Port.SelectedValue, (UInt32)ComboBox_CAN_ID.SelectedIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open COM port \n" + ex.Message);
                return false;
            }
            return true;
        }

        private void Button_RunBooter_Click(object sender, RoutedEventArgs e)
        {
            if(boot_open_and_check() == false)
                return;

            // do the boot loader 
            disable_buttons();
            boot_task = new Task(new Action(bootrun));
            boot_task.Start();
        }

        void bootrun()
        {
            Bootloader.Do_Bootload(false, Vision_Booter.boot_default.none);
        }
        void lastrun()
        {
            Bootloader.Do_Bootload(true, Vision_Booter.boot_default.none);
        }
        void mantag_run()
        {
            Bootloader.Do_Bootload(false, Vision_Booter.boot_default.mantag);
        }
        void module_run()
        {
            Bootloader.Do_Bootload(false, Vision_Booter.boot_default.module);
        }

        void check_progress(object sender, EventArgs e)
        {
            if (Bootloader != null)
            {
                Loader_progress.Value = (double)Bootloader.progress;
                if (Bootloader.progress == 100)
                {
                    Bootloader.progress = 0;
                    Bootloader.close();
                    MessageBox.Show("Bootloading completed successfully");
                    
                    Button_RunBooter.IsEnabled = true;
                    button_default_mantag.IsEnabled = true;
                    button_default_module.IsEnabled = true;
                    if (Bootloader.last_file != "")
						Button_RunBooter_last.IsEnabled = true;
                    update_portlist = true;
                }
                else if (Bootloader.progress == -1)
                {
                    Bootloader.progress = 0;
                    Bootloader.close();
                    MessageBox.Show("Bootloading process failed\n" + Bootloader.error + "\nplease try again.", "Failed", MessageBoxButton.OK, MessageBoxImage.Warning );

                    Button_RunBooter.IsEnabled = true;
                    button_default_mantag.IsEnabled = true;
                    button_default_module.IsEnabled = true;
                    if (Bootloader.last_file != "")
                        Button_RunBooter_last.IsEnabled = true;
                    update_portlist = true;
                }
            }
            // check if we should refresh the port dropdown.
            if (update_portlist)
            {
                ComboBox_Com_Port.ItemsSource = System.IO.Ports.SerialPort.GetPortNames();
                List<string> ports = PortFromVIDPID.ComPortNames(VID, PID);

                // BootWindow was started with an external instruction containing port name. 
                if (incomming_port != "")
                    ComboBox_Com_Port.SelectedValue = incomming_port;

                else if (ports.Count > 0)
                    ComboBox_Com_Port.SelectedValue = (string)ports.ElementAt(0);
            }
        }

        private void ComboBox_Com_Port_MouseLeave(object sender, MouseEventArgs e)
        {
            update_portlist = false;
        }

		private void Button_RunBooter_last_Click(object sender, RoutedEventArgs e)
		{
			string F = Bootloader.last_file;
			if (F == "") return;

            if (boot_open_and_check() == false)
                return;

            // do the boot loader
            disable_buttons();
            boot_task = new Task(new Action(lastrun));
            boot_task.Start();
        }

        private void button_default_module_Click(object sender, RoutedEventArgs e)
        {
            if (boot_open_and_check() == false)
                return;

            // do the boot loader
            disable_buttons();
            boot_task = new Task(new Action(module_run));
            boot_task.Start();
        }

        private void button_default_mantag_Click(object sender, RoutedEventArgs e)
        {
            if (boot_open_and_check() == false)
                return;

            if (!(TextBox_SlaveID.Text.Length == 8 && TextBox_SlaveID.Text.All(c => "0123456789abcdefABCDEF".Contains(c))))
            {
                MessageBox.Show("Mantag firmware can only be flashed to a remote device. Please enter remote UID \n");
                return;
            }

            // do the boot loader 
            disable_buttons();
            boot_task = new Task(new Action(mantag_run));
            boot_task.Start();
        }

        private void ComboBox_Com_Port_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox_CAN_ID.IsEnabled = CanTesterInterface.IsCantester((string)ComboBox_Com_Port.SelectedValue);
        }
    }
}
