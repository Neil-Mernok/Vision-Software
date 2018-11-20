using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace USB_PORTS
{
    public static class PortFromVIDPID
    {
         /// <summary>
        /// Compile an array of COM port names associated with given VID and PID
        /// </summary>
        /// <param name="VID"></param>
        /// <param name="PID"></param>
        /// <returns></returns>
        public static List<string> ComPortNames(String VID, String PID)
        {
            String pattern = String.Format("^VID_{0}.PID_{1}", VID, PID);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            List<string> comports = new List<string>();
            string[] ports = SerialPort.GetPortNames();

            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");

            foreach (String s3 in rk2.GetSubKeyNames())
            {

                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (String s in rk3.GetSubKeyNames())
                {
                    if (_rx.Match(s).Success)
                    {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (String s2 in rk4.GetSubKeyNames())
                        {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            string location = (string)rk5.GetValue("LocationInformation");
                            if (location != string.Empty)
                            {
                                //string port = location.Substring(location.IndexOf('#') + 1, 4).TrimStart('0');
                                //if (port != string.Empty) comports.Add(String.Format("COM{0:####}", port));
                                //}
                                try
                                {
                                    RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                                    if (rk6 != null)
                                    {
                                        string currentname = (string)rk6.GetValue("PortName");
                                        if (ports.Contains(currentname)) comports.Add(currentname);
                                    }
                                }
                                catch(Exception)
                                {}
                            }
                        }
                    }
                }
            }
            return comports;
        } 
    }
}
