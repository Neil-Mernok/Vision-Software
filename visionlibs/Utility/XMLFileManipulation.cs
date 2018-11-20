using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Win32;

namespace Vision_Libs
{
    public class XMLFileManipulation
    {

        public string openfile()
        {
            OpenFileDialog choofdlog = new OpenFileDialog();
            choofdlog.Filter = "All Files (*.xml)|*.*";
            choofdlog.FilterIndex = 1;
            choofdlog.Multiselect = true;

            Nullable<bool> result = choofdlog.ShowDialog();

            if (result == true)
            {
                // Save document
                string filename = choofdlog.FileName;
                //Create_SendingByte();
                //try
                //{
                //    using (var fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
                //    {
                //        fs.Write(SerialComms.SendByteCurrent, 0, SerialComms.SendByteCurrent.Length);

                //    }
                //}
                //    catch (Exception ex)
                //    {
                Console.WriteLine(filename);
                return filename;
                

                //}
            }
            else return "";

            //if (choofdlog.ShowDialog() == DialogResult.OK)
            //    sSelectedFile = choofdlog.FileName;
            //else
            //    sSelectedFile = string.Empty;
        }

    }
    
}
