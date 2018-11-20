using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Vision_Libs.Properties;
using System.IO.Ports;

namespace VisionReader
{
    /// <summary>
    /// Interaction logic for Interface_Settings.xaml
    /// </summary>
    public partial class Interface_Settings : Window
    {
        public Interface_Settings()
        {
            InitializeComponent();
            textbox_select_baud.Text = "115200";
        }

        private void button_open_port_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void button_close_port_Click(object sender, RoutedEventArgs e)
        {
            //Current_vision.ClosePort();
            //update_port_status();
            //Refresh_Datagrid();

            this.DialogResult = false;
            this.Visibility = System.Windows.Visibility.Hidden;
        }

        private void comboBox_select_port_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            //   comboBox_port_select.DataSource = System.IO.Ports.SerialPort.GetPortNames();
            comboBox_select_port.SelectedValue = Settings.Default.Last_Port;
            textbox_select_baud.Text = Settings.Default.Ranger_Baud.ToString();
        }

        private void comboBox_select_port_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            comboBox_select_port.ItemsSource = SerialPort.GetPortNames();
            comboBox_select_port.SelectedValue = Settings.Default.Last_Port;
        }

        private void comboBox_select_port_DropDownOpened(object sender, EventArgs e)
        {
            comboBox_select_port.ItemsSource = SerialPort.GetPortNames();
            comboBox_select_port.SelectedValue = Settings.Default.Last_Port;
        }
    }
}
