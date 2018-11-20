using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CANTESTER;
using System.Threading;
using SerialFraming;
using System.IO;

namespace Vision_Bootloader
{
    public class STM32F10X_Flash
    {
        const int Flash_page_size = 0x400;
        public Byte[] Flash_binary;
        public int byte_counter = 0;




        public STM32F10X_Flash()
        {
            Flash_binary = new Byte[Flash_page_size];

            for (int i = 0; i < Flash_binary.Length; i++)
            {
                Flash_binary[i] = (Byte)i;
            }
        }

        public STM32F10X_Flash(int data_len)
        {
            UInt32[] word_data = new UInt32[data_len / 4 + 1];

            Flash_binary = new Byte[data_len];

            for (uint i = 0; i < data_len / 4 + 1; i++)
            {
                word_data[i] = i;
            }

            Buffer.BlockCopy(word_data, 0, Flash_binary, 0, data_len);
        }

        public STM32F10X_Flash(Byte[] data)
        {
            Flash_binary = new Byte[data.Length];
            data.CopyTo(Flash_binary, 0);
        }

        public int Sendblock(ref SerialPort P, Boolean isCantester, UInt32 CAN_ID)
        {
            int bytes_sent = 0;
            int bytes_left = Flash_binary.Length - byte_counter;
            Byte[] buffer = new Byte[Flash_page_size];
            Byte[] inbuf = new Byte[8];

            if (bytes_left == 0) return byte_counter;

            //int bytesToSend = Math.Min(bytes_left, Flash_page_size);
            //bytesToSend = Math.Max(150, bytesToSend);
            //Buffer.BlockCopy(Flash_binary, byte_counter, buffer, 0, bytesToSend);

            // replaced original variable packet length. Rather send a full 1024 bytes in every packet for stability. 
            int bytesToSend = Math.Min(bytes_left, Flash_page_size);
            Buffer.BlockCopy(Flash_binary, byte_counter, buffer, 0, bytesToSend);

            // clean the uart incoming data to make sure we wait for the right byte
            P.DiscardInBuffer();
            P.ReadTimeout = 2000;

            if (isCantester)
                CanTesterInterface.sendCanMessage(ref P, buffer, 0, Flash_page_size, CAN_ID);       // always send 1024 bytes for stability.
            else
                P.Write(buffer, 0, Flash_page_size);

            while (P.BytesToWrite != 0)
            { }

            try
            {
                Byte c = 0;
                if (isCantester)
                {
                    if (CanTesterInterface.getCanMessage(ref P, out inbuf))
                        c = inbuf[0];
                    //else
                    //    throw(new TimeoutException());
                }
                else
                    c = (Byte)P.ReadChar();

                if (c == 7 || c == 6)       // for some reason the 7 gets sent as a 6...
                {
                    // successful transfer. increment the pointers.
                    byte_counter += bytesToSend;
                    bytes_sent = bytesToSend;
                }
                else if (c == 9)
                {
                    // Bad write operation. just leave the pointer unchanged to try again.
                    bytes_sent = 0;
                }
                else
                {
                    //if (isCantester)
                    //{
                    //    byte_counter += bytesToSend;    // seems to be necessary for can to assume timed out messages were received. 
                    //    bytes_sent = bytesToSend;
                    //}
                    //else
                        bytes_sent = 0;         // block acknowledge timeout.  
                }
            }
            catch (TimeoutException)
            {
                bytes_sent = 0;
                Console.WriteLine("SendBlock catch STM32F10X");
                //////////////System.Windows.MessageBox.Show("timed out in send operation");
            }

            if (byte_counter >= Flash_binary.Length)
            {
//                 System.Threading.Thread.Sleep(100);
                P.Close();
                System.Threading.Thread.Sleep(600);
                Stopwatch S = new Stopwatch();
                S.Restart();
                string[] ports = SerialPort.GetPortNames();
                do
                {
                    ports = SerialPort.GetPortNames();
                    if (S.ElapsedMilliseconds > 10000) break;
                }
                while (ports.Contains(P.PortName) == false);
                System.Threading.Thread.Sleep(100);
                try
                {
                    P.Open();
                }
                catch (Exception)
                {
                    Console.WriteLine("GetCANMessage catch CANTesterInterface");
                    //                     System.Windows.MessageBox.Show("could not reconnect to device." + ex.ToString());
                }
            }

            return bytes_sent;
        }


        public int Sendblock_V2(ref SerialPort P, Boolean isCantester, UInt32 CAN_ID)
        {
            int bytes_sent = 0;
            int bytes_left = Flash_binary.Length - byte_counter;
            Byte[] buffer = new Byte[Flash_page_size];
            Byte[] inbuf = new Byte[8];

            if (bytes_left == 0) return byte_counter;

            // replaced original variable packet length. Rather send a full 1024 bytes in every packet for stability. 
            int bytesToSend = Math.Min(bytes_left, Flash_page_size);
            Buffer.BlockCopy(Flash_binary, byte_counter, buffer, 0, bytesToSend);

            // append a 4 byte word to indicate the address start of the block sent, and a 2 byte CRC 
            Crc16Ccitt CRC16 = new Crc16Ccitt(InitialCrcValue.NonZero1);
            var bytes_count = BitConverter.GetBytes(byte_counter);
            buffer = buffer.Concat(bytes_count).Concat(CRC16.ComputeChecksumBytes(buffer)).ToArray();

            try
            {
                // clean the uart incoming data to make sure we wait for the right byte
                P.DiscardInBuffer();
                P.ReadTimeout = 100;

                if (isCantester)
                    CanTesterInterface.sendCanMessage(ref P, buffer, 0, buffer.Length, CAN_ID);       // always send 1024 bytes for stability.
                else
                    P.Write(buffer, 0, buffer.Length);

                while (P.BytesToWrite != 0)
                { }
            }
            catch (Exception)
            {
                Console.WriteLine("SendBlockV2 catch STM32F10X");
                return 0;
            }
           
            try
            {
                Byte c = 0;
                if (isCantester)
                {
                    if (CanTesterInterface.getCanMessage(ref P, out inbuf))
                        c = inbuf[0];
                }
                else
                    c = (Byte)P.ReadChar();

                if (c == 7 || c == 6)       // for some reason the 7 gets sent as a 6...
                {
                    // successful transfer. increment the pointers.
                    byte_counter += bytesToSend;
                    bytes_sent = bytesToSend;
                }
                else if (c == 9)
                {
                    // Bad write operation. just leave the pointer unchanged to try again.
                    bytes_sent = 0;
                }
                else
                {
                     bytes_sent = 0;         // block acknowledge timeout.  
                }
            }
            catch (Exception)
            {
                bytes_sent = 0;
                Console.WriteLine("SendBlockV2 catch STM32F10X");
            }

            if (byte_counter >= Flash_binary.Length)
            {
                //                 System.Threading.Thread.Sleep(100);
                P.Close();
                System.Threading.Thread.Sleep(600);
                Stopwatch S = new Stopwatch();
                S.Restart();
                string[] ports = SerialPort.GetPortNames();
                do
                {
                    ports = SerialPort.GetPortNames();
                    if (S.ElapsedMilliseconds > 10000) break;
                }
                while (ports.Contains(P.PortName) == false);
                System.Threading.Thread.Sleep(100);
                try
                {
                    P.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("SendBlockV2 catch STM32F10X");
                    System.Windows.MessageBox.Show("could not reconnect to device." + ex.ToString());
                }
            }
            return bytes_sent;
        }

        public int Sendblock_Remote(ref SerialPort P)
        {
            int bytes_sent = 0;
            int bytes_left = Flash_binary.Length - byte_counter;
            Byte[] buffer = new Byte[Flash_page_size];
            Byte[] inbuf = new Byte[8];

            if (bytes_left == 0) return byte_counter;

            // replaced original variable packet length. Rather send a full 1024 bytes in every packet for stability. 
            int bytesToSend = Math.Min(bytes_left, Flash_page_size);
            Buffer.BlockCopy(Flash_binary, byte_counter, buffer, 0, bytesToSend);

            // append a 4 byte word to indicate the address start of the block sent, and a 2 byte CRC 
            Crc16Ccitt CRC16 = new Crc16Ccitt(InitialCrcValue.NonZero1);
            var bytes_count = BitConverter.GetBytes(byte_counter);
            buffer = buffer.Concat(bytes_count).Concat(CRC16.ComputeChecksumBytes(buffer)).ToArray();

            try
            {
                // clean the uart incoming data to make sure we wait for the right byte
                P.DiscardInBuffer();
                P.ReadTimeout = 500;
                
                int inner_bytes_sent = 0;
                int inner_bytes_to_send = buffer.Length;
                const int chunk = 55;

                while (P.BytesToWrite != 0) ;

                Thread.Sleep(10);
                byte[] debug_buffer = new byte[1500];

                MemoryStream stream = new MemoryStream();
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    while (inner_bytes_to_send > 0)
                    {

                        if (inner_bytes_to_send > chunk)
                        {

                            //writer.Write('Z');
                            //writer.Write('B');
                            //writer.Write(buffer, inner_bytes_sent, chunk);

                            byte[] chunk_buffer = new byte[chunk + 2];
                            chunk_buffer[0] = (byte)'Z';
                            chunk_buffer[1] = (byte)'B';

                            Array.Copy(buffer, inner_bytes_sent, debug_buffer, inner_bytes_sent, chunk);
                            Array.Copy(buffer, inner_bytes_sent, chunk_buffer, 2, chunk);
                            writer.Write(SerialFrame.getFrame(chunk_buffer), 0, (chunk + 8));

                            inner_bytes_to_send -= chunk;
                            inner_bytes_sent += chunk;
                        }
                        else
                        {

                            //writer.Write('Z');
                            //writer.Write('E');
                            //writer.Write(buffer, inner_bytes_sent, inner_bytes_to_send);

                            byte[] chunk_buffer = new byte[inner_bytes_to_send + 2];
                            chunk_buffer[0] = (byte)'Z';
                            chunk_buffer[1] = (byte)'E';

                            Array.Copy(buffer, inner_bytes_sent, chunk_buffer, 2, inner_bytes_to_send);
                            writer.Write(SerialFrame.getFrame(chunk_buffer), 0, (inner_bytes_to_send + 8));

                            inner_bytes_sent += inner_bytes_to_send;
                            inner_bytes_to_send = 0;
                        }
                    }

                    P.Write(stream.ToArray(), 0, (int)stream.Length);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("SendBlock remote catch STM32F10X");
                return 0;
            }

            try
            {
                Byte c = 0;

                Thread.Sleep(500);

                if (SerialFrame.getMessage(ref P, out inbuf))
                {
                    if (inbuf[0] == 'b')
                        c = inbuf[1];
                    else
                        return 0;
                }
                else
                    return 0;

                if (c == 7 || c == 6)       // for some reason the 7 gets sent as a 6...
                {
                    // successful transfer. increment the pointers.
                    byte_counter += bytesToSend;
                    bytes_sent = bytesToSend;
                }
                else if (c == 9)
                {
                    // Bad write operation. just leave the pointer unchanged to try again.
                    bytes_sent = 0;
                }
                else
                {
                    bytes_sent = 0;         // block acknowledge timeout.  
                }
            }
            catch (Exception)
            {
                Console.WriteLine("SendBlock remote catch STM32F10X");
                bytes_sent = 0;
            }

            return bytes_sent;
        }
    }
}
