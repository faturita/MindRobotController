using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Sockets;
using EEGLogger;
using Exocortex.DSP;
using System.Net;

namespace MindRobotController
{
    public partial class MindRobotVisualizer : Form, TickReceiver
    {
        EEGDataTickGenerator t;

        PortController p;

        public MindRobotVisualizer()
        {
            InitializeComponent();

            PortController.enabled = false;

            PortController.Init();

            PortController.Command("T,1");

            PortController.Command("T,1");

            window[0] = new ComplexF[128];
            window[1] = new ComplexF[128];

            fft[0] = new ComplexF[128];
            fft[1] = new ComplexF[128];

        }

        [DllImportAttribute("User32.dll")]

        private static extern int FindWindow(String ClassName, String
        WindowName);

        [DllImportAttribute("User32.dll")]
        private static extern int SetForegroundWindow(int hWnd);

        Random r = new Random();

        int counter = 0;

        int avgx = 1612, avgy = 1730;
        int samples = 10000;

        int changex, changey;

        int newx = 0, newy = 0;

        long refractory = 0;

        int speed=0, balance=0;

        //int avgx=0, avgy=0;
        //int samples = 0;

        float sensibility = 1;

        float[] psd = new float[2];

        public void SignalStop()

        {
            Console.WriteLine("Closing epuck port....");
            PortController.Command("S");
            PortController.Terminate();
        
        }

        // 1 second window

        ComplexF[][] window = new ComplexF[14][];
        ComplexF[][] fft = new ComplexF[14][];


        double avg = 0;



        public void SendPlot(string plot)
        {
            UdpClient udpClient = new UdpClient("localhost", 7788);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(plot);
            try
            {
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        public void tick()
        {
            if (!this.IsDisposed)
            {

                    samples++;

                    float result = 1;
                    if (float.TryParse(textBox7.Text, out result))
                        sensibility = result;

                    int newwx = (int)(t.gyrox);
                    int newwy = (int)(t.gyroy);

                    // Leaky integrator.
                    //newx = (int)((0.5 * (float)newx + (1 - 0.5) * (float)newwx));
                    //newy = (int)((0.5 * (float)newy + (1 - 0.5) * (float)newwy));

                    newx = newwx;
                    newy = newwy;


                    for(int c=0;c<2;c++)
                    {
                        if (samples % 128 == 0 )
                        {

                            /**
                            for (int d = 0; d < 128; d++)
                            {
                                window[c][d].Re = (float)(10 * Math.Sin(2 * Math.PI * (8F/128F) * d));
                                window[c][d].Re += (float)(7 * Math.Sin(2 * Math.PI * (15F / 128F) * d));
                            }**/


                            ComplexF[] temp = new ComplexF[128];

                            Array.Copy(window[c], temp, 128);


                            Fourier.FFT(temp, 128, FourierDirection.Forward);

                            Array.Copy(temp, fft[c], 128);




                            /**
                            for (int d = 0; d < 128; d++)
                            {
                                if (((d < 16 || d > 32) && d < 64) ||
                                ((d < 128 - 32 || d > 128 - 16) && d > 64))
                                {
                                    fft[c][d].Im = 0;
                                }
                            }**/


                            /**
                            float numBins = 128 / 2;  // Half the length of the FFT by symmetry
                            float binWidth = 128 / numBins; // Hz

                            float DCGain=1.0F;
                            float order = 3;
                            float f0 = 45;


                            // Filter
                            for(int i=0;i<128/2;i++)
                            {
                                float binFreq = binWidth * i;
                                float gain = (float)(DCGain / ( Math.Sqrt( ( 1 + 
                                Math.Pow( binFreq / f0, 2.0 * order ) ) ) ));
                                window[c][i].Re *= gain; window[c][i].Im *= gain;
                                window[c][128 - i].Re *= gain; window[c][128 - i].Im *= gain;
                            }


                            f0 = 72;
                            for (int i = 0; i < 128 / 2; i++)
                            {
                                float binFreq = binWidth * i;
                                float gain = (float)(DCGain / (Math.Sqrt((1 +
                                Math.Pow(binFreq / f0, 2.0 * order)))));
                                window[c][i].Re *= gain; window[c][i].Im *= gain;
                                window[c][128 - i].Re *= gain; window[c][128 - i].Im *= gain;
                            }**/



                            StringBuilder strb = new StringBuilder();

                            strb.Append("fftsignal = [");
                            for (int i = 0; i < 128; i++)
                            {
                                float val = fft[c][i].Im;

                                val = Math.Abs(val);
                                strb.Append(" " + val + " ");

                                fft[c][i].Im = val;
                            }

                            strb.Append("];");

                            SendPlot(strb.ToString());

                            psd[c] = GetPSD(c,15,25);

                            window[c] = new ComplexF[128];
                        }
                        else
                        {
                            ComplexF timepoint = new ComplexF();
                            timepoint.Im = 0;

                            switch (c) 
                            {
                                case 0: 
                                    timepoint.Re = (float)t.o1;
                                    break;
                                case 1:
                                    timepoint.Re = (float)t.o2;
                                    break;
                                default:break;
                            }


                            window[c][samples % 128] = timepoint;
                        }
                    }


                    //avgx += newx;
                    //avgy += newy;

                    this.Invoke(new Blink(blink), new object[] { });

                    changex = (int)(((newx) - avgx) * (-1) * sensibility);
                    changey = (int)((newy - avgy) * sensibility);

                    refractory++;

                    if (((Math.Abs(changex) > 5 || Math.Abs(changey) > 5)))  // 100,40
                    {
                        balance += changex*10;
                        speed -= changey*10;


                        int wheelright = 200 + speed - balance;
                        int wheelleft = 200 + speed + balance;

                        int max = 800;

                        if (wheelright > max) wheelright = max; if (wheelright < -max) wheelright = -max;
                        if (wheelleft > max) wheelleft = max; if (wheelleft < -max) wheelleft = -max;

                        PortController.Command("D," + wheelleft + "," + wheelright);

                        ControlMinecraft();

                        /**
                        if (changey < 0)
                        {
                            PortController.Command("T,3");
                            PortController.Command("D,100,100");
                        }
                        else if (changey > 0)
                        {
                            PortController.Command("T,5");
                            PortController.Command("S");
                        } else

                        if (changex < 0) // Right
                        {
                            PortController.Command("T,1");
                            PortController.Command("D,100,-100");
                        }
                        if (changex > 0)
                        {
                            PortController.Command("T,2");
                            PortController.Command("D,100,-100");
                        } **/

                        //(GetPSD(1, 16, 32) / ((32 - 16)))

                        avg += (GetPSD(0, 8, 15) / (15 - 8));

                        
                        if (samples > 600 && (GetPSD(0, 8, 15) / (15 - 8)) > (avg / samples * 1.5))
                        {
                            PortController.Command("b,1");
                            PortController.Command("T,5");
                            speed = (int) (speed*1.1);
                        }



                        if ((GetPSD(0, 8, 15) / ((15 - 8) * GetPSD(0, 0, 64))) > (GetPSD(1, 8, 15) / ((15 - 8) * GetPSD(1, 0, 64)))*1.5)
                        {
                            PortController.Command("f,1");
                            PortController.Command("T,1");
                        }
                        else if ((GetPSD(1, 8, 15) / ((15 - 8) * GetPSD(1, 0, 64))) > (GetPSD(0, 8, 15) / ((15 - 8) * GetPSD(0, 0, 64))) * 1.5)
                        {
                            PortController.Command("f,0");
                            PortController.Command("T,2");
                        }



                        refractory = 0;

                    }
                    else
                    {
                        PortController.Command("S");
                        balance = speed = 0;
                    }


            }
        }

        private float GetPSD(int c, int minRange, int maxRange)
        {
            float tot = 0;
            for (int i = minRange; i < maxRange; i++)
            {
                tot += fft[c][i].Im;
            }
            return tot;
        }

        public void ticdk()
        {
            if (!this.IsDisposed)
            {
                //to activate an application
                int hWnd = FindWindow(null, "Minecraft 1.8.1");
                if (hWnd > 0)
                {


                    /**
                    samples++;
                    if (avgx == 0)
                    {
                        avgx = (int)Math.Abs(t.gyrox);
                    }
                    else
                    {
                        avgx = (avgx*(samples-1) + (int)Math.Abs(t.gyrox)) / samples;
                    }

                    if (avgy == 0)
                    {
                        avgy = (int)Math.Abs(t.gyroy);
                    }
                    else
                    {
                        avgy = (avgy*(samples-1) + (int)Math.Abs(t.gyroy)) / samples;
                    }

                     ***/
                    samples++;

                    float result = 1;
                    if (float.TryParse(textBox7.Text, out result))
                        sensibility = result;

                    int newwx = (int)(t.gyrox);
                    int newwy = (int)(t.gyroy);

                    // Leaky integrator.
                    //newx = (int)((0.5 * (float)newx + (1 - 0.5) * (float)newwx));
                    //newy = (int)((0.5 * (float)newy + (1 - 0.5) * (float)newwy));

                    newx = newwx;
                    newy = newwy;



                    //avgx += newx;
                    //avgy += newy;

                    this.Invoke(new Blink(blink), new object[] { });

                    changex = (int)(((newx) - avgx) * (-1) * sensibility);
                    changey = (int)((newy - avgy) * sensibility);

                    if (Math.Abs(changex) > 1000 || Math.Abs(changey) > 1000)
                    {
                        SetForegroundWindow(hWnd);

                        Point p = GetCursorPosition();


                        //SetCursorPos(p.X + (changex), p.Y + changey);
                        SetCursorPos(800 + (changex), 441 + changey);
                    }

                }

                string command = "w";

                switch (r.Next(5))
                {
                    case 0: command = "a"; break;
                    case 1: command = "s"; break;
                    case 2: command = "d"; break;
                    case 3: command = " "; break;
                    case 4: command = "wwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwwww"; break;
                    default: command = "w"; break;
                }


                if (command == null || command.Equals("") || command.Length == 0)
                {

                    counter++;
                    Thread.Sleep(50);

                    if (counter > 1000) t.Stop(); ;
                }
                else
                {
                    command = command.Substring(0, command.Length - 1);
                    //counter = 0;

                    //SendKeys.Send(command);

                    //this.Invoke(new DoCommand(doCommand), new object[] { command });
                }

            }
        }

        public delegate void DoCommand(string command);

        public void doCommand(string command)
        {
            SendKeys.Send(command);
        }

        public delegate void Blink();




        public void blink()
        {
            textBox2.Text = avgx.ToString();
            textBox3.Text = avgy.ToString();
            
            textBox2.Text = speed.ToString();
            textBox3.Text = balance.ToString();



            textBox4.Text = samples.ToString();

            psdO1.Text = (GetPSD(0, 16, 32)/((32-16) )).ToString();
            psdO2.Text = (GetPSD(1, 16, 32) / ((32 - 16))).ToString();


            textBox5.Text = changex.ToString();
            textBox6.Text = changey.ToString();

            sensibility = float.Parse(textBox7.Text);

            textBox8.Text = String.Format("{0:00000.0000000}", (t.frequency / 1000.0F));
        }


        Thread threadC;

        private void button1_Click(object sender, EventArgs e)
        {
            //t = new PlainTickGenerator();
            Experiment experiment = new Experiment();
            experiment.datadirectory = "C:\\Users\\User\\Desktop\\RobotMindController\\";

            t = new EEGDataTickGenerator(experiment,textBox1.Text);

            t.SetStopTimer(4);

            t.StartEvent();
            t.Start(this);



            ThreadStart threadStartC = delegate() {
                int recv;
                byte[] data = new byte[1024];
                IPEndPoint ipep = new IPEndPoint(IPAddress.Any, 9050);

                Socket newsock = new Socket(AddressFamily.InterNetwork,
                                SocketType.Dgram, ProtocolType.Udp);

                newsock.Bind(ipep);
                Console.WriteLine("Waiting for a client...");

                IPEndPoint sendr = new IPEndPoint(IPAddress.Any, 0);
                EndPoint Remote = (EndPoint)(sendr);

                while (true)
                {
                    recv = newsock.ReceiveFrom(data, ref Remote);

                    string message = Encoding.ASCII.GetString(data, 0, recv);

                    Console.WriteLine("Message received from {0}:", Remote.ToString());
                    Console.WriteLine(message);

                    t.StartEvent(message);

                }

                Console.WriteLine("Finishing event thread....");
            
            };

            threadC = new Thread(threadStartC);

            threadC.Start();




        }
        private void buttdon1_Click(object sender, EventArgs e)
        {
            //t = new PlainTickGenerator();
            t = new EEGDataTickGenerator(textBox1.Text);

            t.experiment.datadirectory = "C:\\Users\\User\\Desktop\\RobotMindController\\";

            t.SetStopTimer(4);


            //to activate an application
            int hWnd = FindWindow(null, "Minecraft");
            if (hWnd > 0)
            {

                SetForegroundWindow(hWnd);
                Thread.Sleep(50); // intentional delay to ensure the window is in the foreground
                SendKeys.Send("{ESC}");
                Thread.Sleep(50); // intentional delay to ensure the window is in the foreground

                //SendKeys.Send(textBox1.Text);


                //SetCursorPos(Int32.Parse(textBox2.Text), Int32.Parse(textBox3.Text));

                /**
                AsynchronousSocketListener asl = new AsynchronousSocketListener();

                // 11000
                asl.StartListen();

                asl.GetOneConnection();


                **/

                /**
                int counter = 0;


                while (true)
                {
                    string command = asl.Read();


                    if (command == null || command.Equals("") || command.Length == 0)
                    {

                        counter++;
                        Thread.Sleep(50);

                        if (counter > 1000) break;
                    }
                    else
                    {
                        command = command.Substring(0, command.Length - 1);
                        //counter = 0;

                        SendKeys.Send(command);
                    }

                }


                textBox1.Text = "Ending...";
                 * 
                 * **/

                /**
                for (int i = 0; i < 1000; i++)
                {
                    //SetForegroundWindow(hWnd);
                    //Thread.Sleep(50); // intentional delay to ensure the window is in the foreground
                    //SendKeys.Send(textBox1.Text);


                    switch (r.Next(5))
                    {
                        case 0: SendKeys.Send(" "); break;
                        case 1: SendKeys.Send("w"); break;
                        case 2: SendKeys.Send("a"); break;
                        case 3: SendKeys.Send("d"); break;
                        case 4: SendKeys.Send("w"); break;
                        default: SendKeys.Send("z"); break;
                    }

                }**/
            }


            t.StartEvent();
            t.Start(this);
        }




        [Flags]
        public enum MouseEventFlags
        {
            LEFTDOWN = 0x00000002,
            LEFTUP = 0x00000004,
            MIDDLEDOWN = 0x00000020,
            MIDDLEUP = 0x00000040,
            MOVE = 0x00000001,
            ABSOLUTE = 0x00008000,
            RIGHTDOWN = 0x00000008,
            RIGHTUP = 0x00000010
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rectangle
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        private static void Click(IntPtr Handle)
        {
            lock (typeof(MouseEventFlags))
            {
                Rectangle buttonDesign;

                GetWindowRect(Handle, out buttonDesign);
                Random r = new Random();

                int curX = 10 + buttonDesign.X + r.Next(100 - 20);
                int curY = 10 + buttonDesign.Y + r.Next(60 - 20);

                SetCursorPos(curX, curY);
                //Mouse Right Down and Mouse Right Up
                mouse_event((uint)MouseEventFlags.LEFTDOWN, curX, curY, 0, 0);
                mouse_event((uint)MouseEventFlags.LEFTUP, curX, curY, 0, 0);
            }
        }

        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(
            long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out Rectangle rect);

        /// <summary>
        /// Struct representing a point.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            //bool success = User32.GetCursorPos(out lpPoint);
            // if (!success)

            return lpPoint;
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            //to activate an application
            int hWnd = FindWindow(null, "Minecraft");
            if (hWnd > 0)
            {
                SetForegroundWindow(hWnd);
                Thread.Sleep(50); // intentional delay to ensure the window is in the foreground
                SendKeys.Send("{ESC}");
                Thread.Sleep(50); // intentional delay to ensure the window is in the foreground

                SetCursorPos(Int32.Parse(textBox2.Text), Int32.Parse(textBox3.Text));
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            ControlMinecraft();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SendPlot("finishplot");
            t.Stop();
            Console.WriteLine("Closing epuck port....");
            PortController.Command("S");
            PortController.Command("t,0");
            PortController.Command("b,0");
            PortController.Command("f,0");
            PortController.Terminate();

            
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {
            ControlMinecraft();
        }

        private void ControlMinecraft()
        {
            int hWnd = FindWindow(null, "Minecraft 1.8.1");
            if (hWnd > 0)
            {

                SetForegroundWindow(hWnd);

                Point p = GetCursorPosition();


                //SetCursorPos(p.X + (changex), p.Y + changey);
                //SetCursorPos(800 + (changex), 441 + changey);
                SetCursorPos(683 + (changex), 384 + changey);
            }
        }

    }
}
