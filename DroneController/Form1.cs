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

namespace DroneController
{
    public class DroneController
    {

        int sequence = 1;

        UdpClient udpClient = null;

        public void SendPlot(string plot)
        {
            SendPlot(5556, plot);
        }

        public void SendPlot(Int32 port, string plot)
        {
            SendPlot("192.168.1.1", port, plot);
        }

        public void SendPlot(String ipString,Int32 port, string plot)
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
            string emergency = String.Format("AT*REF={0},290717696\rAT*REF={1},290717952\rAT*REF={2},290717696\r",sequence++,sequence++,sequence++);



            //string message = String.Format("AT*FTRIM={0}\n", sequence++);

            Console.WriteLine(emergency);

            SendPlot(emergency);
        }
    }
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        int sequence = 1;

        UdpClient udpClient = null;

        UdpClient navdataSock = null;

        Thread navdataThread;

        UdpClient videoStreamingSock = null;

        Thread videoThread;

        TcpClient controlSock = null;

        Thread controlThread;

        private void connect_Click(object sender, EventArgs e)
        {
            //The AT commands are encoded as 8-bit ASCII characters with a carriage return "<CR>" as 
            //    a newline delimeter. All AT commands start with "AT*" followed by a command name, a 
            //        equals sign, a sequence number(starting with 1, which also resets the sequence number), 
            //and optionally a list of comma-seperated arguments for the command.

            //AT*REF=1,290717696<LF>AT*REF=2,290717952<LF>AT*REF=3,290717696<LF>


            // 290717696+2^8  + 2^9
            string emergency = String.Format("AT*REF={0},290717696\rAT*REF={1},290717952\rAT*REF={2},290717696\r",sequence++,sequence++,sequence++);



            //string message = String.Format("AT*FTRIM={0}\n", sequence++);

            Console.WriteLine(emergency);

            SendPlot(emergency);
        }

        public void SendPlot(string plot)
        {
            SendPlot(5556, plot);
        }

        public void SendPlot(Int32 port, string plot)
        {
            SendPlot("192.168.1.1", port, plot);
        }

        public void SendPlot(String ipString,Int32 port, string plot)
        {
            //UdpClient udpClient = new UdpClient("192.168.150.128", 32000);

            //UdpClient udpClient = new UdpClient("192.168.1.1", 5556);
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

        private void Init_Click(object sdender, EventArgs e)
        {
            //Creates an instance of the UdpClient class using a local endpoint.
            IPAddress myIpAddress = Dns.Resolve(Dns.GetHostName()).AddressList[0];
            IPEndPoint ipLocalEndPoint = new IPEndPoint(myIpAddress, 5556);

            Console.WriteLine("Initializing Client Sockets at:" + myIpAddress);

            try
            {
                udpClient = new UdpClient(ipLocalEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in UDP Socket initialization...."+ex.ToString());
                return;
            }

            // comands send (and receive eventually) udp on 5556
            // navdata receive udp on 5554
            // video on receive udp on 5555
            // control on tcp on 5559


            initNavData(myIpAddress);

            initVideoStreaming(myIpAddress);

            //initControlComm(myIpAddress);


            /**
4804.869232 vp_com_server 6  879 Disconnection from atcmd port (5556)
4804.869354 vp_com_server 6  879 Keeping the current configuration until a new client connects.
5153.838867 vp_com_server 6  879 Incoming connection on navdata port (5554)
5153.838958 vp_com_server 6  879 Setting the navdata (5554) send buffer to 1kB
5153.839050 vp_com_server 6  879 192.168.1.2 connected to navdata (navdata is closed)
5514.266326 vp_com_server 6  879 Incoming connection on atcmd port (5556)
5514.266510 ATCmdServer   6  880 Mobile reconnection - Resetting com watchdog
5516.267547 vp_com_server 6  879 Disconnection from navdata port (5554)
5516.267639 vp_com_server 6  879 Disconnection from atcmd port (5556)
5516.267669 vp_com_server 6  879 Keeping the current configuration until a new client connects.
5535.417327 vp_com_server 6  879 Incoming connection on atcmd port (5556)
5535.417541 ATCmdServer   6  880 Mobile reconnection - Resetting com watchdog
5537.418212 vp_com_server 6  879 Disconnection from atcmd port (5556)
5537.418304 vp_com_server 6  879 Keeping the current configuration until a new client connects.
5537.681579 vp_com_server 6  879 Incoming connection on atcmd port (5556)
    **/


        }

        private void initNavData(IPAddress myIpAddress)
        {
            IPEndPoint ipep = new IPEndPoint(myIpAddress, 5554);
            navdataSock = new UdpClient(ipep);

            Console.WriteLine("UDP NavData on port 5554...");

            /**string welcome = "Welcome to my test server";
            data = Encoding.ASCII.GetBytes(welcome);
            newsock.Send(data, data.Length, sender);**/

            ThreadStart threadStartA = delegate()
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                byte[] data = new byte[1024];

                SendPlot(5554, "        ");

                //"AT*CONFIG=\"general:navdata_demo\",\"TRUE\"\\r".
                //string navdatainit = String.Format("AT*CONFIG=\"general:navdata_demo\",\"TRUE\"\r");

                //Console.WriteLine(navdatainit);

                //SendPlot(navdatainit);

                byte[] bf = new byte[4];

                bf[0] = 1;

                udpClient.Connect("192.168.1.1", 5556);
                udpClient.Send(bf, bf.Length);

                Console.WriteLine("Ready to loop navdata");

                while (true)
                {

                    //if (navdataSock.Available > 0)
                    {
                        data = navdataSock.Receive(ref sender);

                        //Console.WriteLine("," + Encoding.ASCII.GetString(data, 0, data.Length));
                        //newsock.Send(data, data.Length, sender);
                        Console.WriteLine(".");

                    }
                    reset();
                }

            };

            navdataThread = new Thread(threadStartA);
            navdataThread.Name = "NavData";

            navdataThread.Start();
        }

        private void initControlComm(IPAddress ipAddress)
        {
            Int32 port = 5559;
            controlSock = new TcpClient("192.168.1.1", port);

            // Translate the passed message into ASCII and store it as a Byte array.
            //Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);         

            // Get a client stream for reading and writing. 
           //  Stream stream = client.GetStream();

            NetworkStream stream = controlSock.GetStream();

            // Send the message to the connected TcpServer. 
            //stream.Write(data, 0, data.Length)

            ThreadStart threadStartA = delegate() {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                byte[] data = new byte[1024];
                Console.WriteLine("TCP Port 5559 Ready to send/receive control information");

                Int32 bytes = 1;

                Console.WriteLine("Sending bytes from control tcp.");
                Byte[] sendBytes = Encoding.ASCII.GetBytes("AT*CTRL=4\r");
                stream.Write(sendBytes, 0, sendBytes.Length);

                SendPlot("AT*CTRL=4\r");

                while (controlSock.Connected && bytes>0)
                {
   
                    // String to store the response ASCII representation.
                    String responseData = String.Empty;

                    // Read the first batch of the TcpServer response bytes.
                    bytes = stream.Read(data, 0, data.Length);

                    //Console.WriteLine(Encoding.ASCII.GetString(data, 0, data.Length));
                    //newsock.Send(data, data.Length, sender);

                    Console.WriteLine("Received {0} bytes at TCP Port", bytes);
                }

                Console.WriteLine("TCP Socket disconnected.");
                controlSock.Close();
            
            };

            controlThread = new Thread(threadStartA);
            controlThread.Name = "Control";

            controlThread.Start();

        }

        private void initVideoStreaming(IPAddress ipAddress)
        {
            IPEndPoint ipep = new IPEndPoint(ipAddress, 5555);
            videoStreamingSock = new UdpClient(ipep);

            Console.WriteLine("UDP Video on port 5555...");

            /**string welcome = "Welcome to my test server";
            data = Encoding.ASCII.GetBytes(welcome);
            newsock.Send(data, data.Length, sender);**/

            ThreadStart threadStartA = delegate()
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);

                byte[] data = new byte[1024];

                while (true)
                {

                    data = videoStreamingSock.Receive(ref sender);

                    //Console.WriteLine("-"+Encoding.ASCII.GetString(data, 0, data.Length));
                    //newsock.Send(data, data.Length, sender);

                    Console.Write("-");
                }

            };

            videoThread = new Thread(threadStartA);
            videoThread.Name = "Video";

            videoThread.Start();
        }

        private void turn360_Click(object sender, EventArgs e)
        {
            //SendPlot("AT*CTRL=4\r");
            reconnect();
        }

        private void reconnect()
        {
            SendPlot("192.168.1.3",5554,"AT*COMWDG\r");
        }

        private void turn()
        {
            //AT*PCMD=xx,xx,-1085485875,xx,xx

            //AT*REF=1,290717696<LF>AT*REF=2,290717952<LF>AT*REF=3,290717696<LF>

            string emergency = String.Format("AT*PCMD={0},290717696\rAT*REF={1},290717952\rAT*REF={2},290717696\r",sequence++,sequence++,sequence++);

            //string message = String.Format("AT*FTRIM={0}\n", sequence++);

            Console.WriteLine(emergency);

            SendPlot(emergency);
        }

        private void flattrim()
        {
            string ftrim = String.Format("AT*FTRIM={0}\r", sequence++);

            //Console.WriteLine(ftrim);

            SendPlot(ftrim);
        }

        private void config(string configoption, string configvalue)
        {
            //AT*CONFIG=%d,%s,%s<LF>

            string ftrim = String.Format("AT*CONFIG={0},\"{1}\",\"{2}\"\r", sequence++, configoption, configvalue);

            Console.WriteLine(ftrim);

            SendPlot(ftrim);
        }

        private void ftrim_Click(object sender, EventArgs e)
        {
            flattrim();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Reset sequence
            sequence = 1;
        }

        private void takeoff_Click(object sender, EventArgs e)
        {
            string takeoff = String.Format("AT*REF={0},290718208\rAT*REF={1},290718208\rAT*REF={2},290718208\r", sequence++, sequence++, sequence++);

            SendPlot(takeoff);
        }

        private void land_Click(object sender, EventArgs e)
        {
            string land = String.Format("AT*REF={0},290717696\rAT*REF={1},290717696\rAT*REF={2},290717696\r", sequence++, sequence++, sequence++);

            SendPlot(land);
        }

        private void reset()
        {
            string reset = String.Format("AT*REF={0},290717696\rAT*REF={1},290717696\rAT*REF={2},290717952\r", sequence++, sequence++, sequence++);

            SendPlot(reset);
        }


    }
}
