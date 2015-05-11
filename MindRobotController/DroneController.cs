using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MindRobotController
{
    public class DroneController
    {

        int sequence = 1;


        public void SendPlot(string plot)
        {
            SendPlot(5556, plot);
        }

        public void SendPlot(Int32 port, string plot)
        {
            SendPlot("192.168.1.1", port, plot);
        }

        public void SendPlot(String ipString, Int32 port, string plot)
        {
            UdpClient udpClient = new UdpClient();

            Byte[] sendBytes = Encoding.ASCII.GetBytes(plot);
            try
            {
                udpClient.Connect(ipString, port);
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public void SendEmergency()
        {
            //The AT commands are encoded as 8-bit ASCII characters with a carriage return "<CR>" as 
            //    a newline delimeter. All AT commands start with "AT*" followed by a command name, a 
            //        equals sign, a sequence number(starting with 1, which also resets the sequence number), 
            //and optionally a list of comma-seperated arguments for the command.

            //AT*REF=1,290717696<LF>AT*REF=2,290717952<LF>AT*REF=3,290717696<LF>


            // 290717696+2^8  + 2^9
            string emergency = String.Format("AT*REF={0},290717696\rAT*REF={1},290717952\rAT*REF={2},290717696\r", sequence++, sequence++, sequence++);
            string land = String.Format("AT*REF={0},290717696\rAT*REF={1},290717696\rAT*REF={2},290717696\r", sequence++, sequence++, sequence++);


            //string message = String.Format("AT*FTRIM={0}\n", sequence++);

            Console.WriteLine(emergency);

            SendPlot(emergency);
        }
    }
}
