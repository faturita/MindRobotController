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
using System.Diagnostics;

namespace MindRobotController
{
    public partial class MindRobotVisualizer : Form, TickReceiver
    {
        EEGDataTickGenerator eegtickgen;

        PortController portcontroller = new PortController();

        public MindRobotVisualizer()
        {
            InitializeComponent();

            portcontroller.enabled = false;

            fft[0] = new double[WINDOWSIZE];
            fft[1] = new double[WINDOWSIZE];

            fs[0] = new double[WINDOWSIZE];
            fs[1] = new double[WINDOWSIZE];

            threshold[0] = new Threshold();
            threshold[1] = new Threshold();


            System.Windows.Forms.MessageBox.Show("1-Start this app.\r\n2-Start the ArDrone Server\r\n3-Start the Emergency Line\r\n4-Do Network Check\r\n5-Do Land Check");

        }

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


        private bool calibrating = true;

        public void SignalStart()
        {

            try { portcontroller.Init(); }
            catch (System.IO.IOException e) { portcontroller.enabled = false; }

            portcontroller.Beep();

            //t = new PlainTickGenerator();
            Experiment experiment = new Experiment();
            experiment.datadirectory = "C:\\Users\\User\\Desktop\\RobotMindController\\";

            eegtickgen = new EEGDataTickGenerator(experiment, textBox1.Text);
            //eegtickgen = new RandomTickGenerator();

            eegtickgen.SetStopTimer(4);

            eegtickgen.StartEvent();
            eegtickgen.Start(this);

        }


        public void SignalStop()

        {
            Console.WriteLine("Closing epuck port....");

            portcontroller.Stop();

            portcontroller.Terminate();
        
        }


        /**
         * Useme to communicate this program to another device.
         * 
         * This will send a UDP commando to the specified host:port which is MATLAB
         * 
         **/ 
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


        /**
         * Useme to communicate this program to another device.
         * 
         * This will send a UDP commando to the specified host:port.
         * 
         **/
        public void SendCommand(string message)
        {
            //UdpClient udpClient = new UdpClient("10.6.0.155", 7778);
            UdpClient udpClient = new UdpClient(ipTextBox.Text, 7778);
            Byte[] sendBytes = Encoding.ASCII.GetBytes(message);
            try
            {
                //Console.WriteLine("Sending Command:" + message);
                udpClient.Send(sendBytes, sendBytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }


        private int maxSpeed = 9210;
        private int minSpeed = -11970;

        private int minBalance = -13640;
        private int maxBalance = 16000;

        public void tick()
        {
            if (!this.IsDisposed)
            {
                ControlDrone();


                /**
                samples++;

                int newwx = (int)(eegtickgen.gyrox);
                int newwy = (int)(eegtickgen.gyroy);

                doFFT();

                this.Invoke(new Blink(blink), new object[] { });**/

            }
        }

        int signx = 0;
        int signy = 0;

        #region Controllers
        private void ControlDrone()
        {
            samples++;

            float result = 1;
            if (float.TryParse(textBox7.Text, out result))
                sensibility = result;

            int newwx = (int)(eegtickgen.gyrox);
            int newwy = (int)(eegtickgen.gyroy);

            newx = newwx;
            newy = newwy;

            this.Invoke(new Blink(blink), new object[] { });

            changex = (int)((((float)newx) - (float)avgx) * (-1) * sensibility);
            changey = (int)(((float)newy - (float)avgy) * sensibility);

            refractory++;

            doFFT();

            /**
            if (((Math.Abs(changex) > 100 || Math.Abs(changey) > 40)))  // 100,40
            {
                SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(rescale(changey, -800, 800) * 0.7F / 800.0F) + ", \"balance\":" + FormatNumber(linealdynamicrange(changex, minBalance, maxBalance, -800, 800) * 0.7F / 800.0F) + "}");
            } 
            else
            {
                portcontroller.Stop();

                //if (samples % 128 ==0) SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(0) + ", \"balance\":" + FormatNumber(0) + "}");
                
                balance = speed = 0;
            }**/

            if (((Math.Abs(changex) > 100 || Math.Abs(changey) > 40)))  // 100,40
            {
                balance += changex * 10;
                speed -= changey * 10;

                // Balance > 0 --> RIGTH
                // SPEED > 0 --> FORWARD


                int wheelright = 200 + speed - balance;
                int wheelleft = 200 + speed + balance;

                int max = 800;

                if (wheelright > max) wheelright = max; if (wheelright < -max) wheelright = -max;
                if (wheelleft > max) wheelleft = max; if (wheelleft < -max) wheelleft = -max;

                portcontroller.SetWheels(wheelleft, wheelright);

                //SendCommand("D," + wheelleft + "," + wheelright);

                if (speed > maxSpeed) maxSpeed = speed;
                if (speed < minSpeed) minSpeed = speed;

                //Console.WriteLine("[" + minSpeed + "," + maxSpeed + "]");

                if (balance > maxBalance) maxBalance = balance;
                if (balance < minBalance) minBalance = balance;

                //Console.WriteLine("[" + minBalance + "," + maxBalance + "]");

                int nspeed = linealdynamicrange(speed, minSpeed, maxSpeed, -max, max);
                int nbalance = linealdynamicrange(balance, minBalance, maxBalance, -max, max);

                // Just be sure that no value goes beyond the threshold.
                nspeed = rescale(nspeed, -max, max);
                nbalance = rescale(nbalance, -max, max);

                float MAXVALUE = 0.7F;

                double fspeed = nspeed * MAXVALUE / max;
                double fbalance = nbalance * MAXVALUE / max;


                // With parrot, this works very well.
                if (signx != Math.Sign(balance) && Math.Abs(changex) > 100 )
                {
                    fspeed = 0.0;
                    fbalance = (Math.Sign(balance) * 0.1);
                    SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(fspeed) + ", \"balance\":" + FormatNumber(fbalance) + "}");

                    signx = Math.Sign(balance);
                }

                if (signy != Math.Sign(speed) && Math.Abs(changey) > 40)
                {
                    fspeed = (Math.Sign(speed) * -0.1);
                    fbalance = 0.0;
                    SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(fspeed) + ", \"balance\":" + FormatNumber(fbalance) + "}");

                    signy = Math.Sign(speed);
                }

                //doSomeNoise();

                refractory = 0;

            }
            else
            {
                portcontroller.Stop();

                if (samples % 128 ==0) SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(0) + ", \"balance\":" + FormatNumber(0) + "}");
                
                balance = speed = 0;
            }
        }
        #endregion 

        const int WINDOWSIZE = 1024;

        double[][] fs = new double[2][];
        double[][] fft = new double[2][];

        Threshold[] threshold = new Threshold[2];

        bool dofire = true;

        bool flying = false;

        private void doFFT()
        {
            fs[0][samples % WINDOWSIZE] = eegtickgen.o1;
            fs[1][samples % WINDOWSIZE] = eegtickgen.o2;

            // Skip the first shots.  Thery are noisier.
            if (samples > WINDOWSIZE*3 && samples % WINDOWSIZE == 0)
            {

                for (int c = 0; c < 2; c++)
                {

                    fft[c] = GetFFT(fs[c]);

                    if (calibrating)
                    {
                        threshold[c].calibrate(GetPSD(fft[c], 30, 34));
                    }
                    else
                    if (dofire && threshold[c].votebellow(GetPSD(fft[c], 30, 34)))
                    {
                        dofire = false;
                        flying = true;
                        Console.WriteLine("FIRED!");
                        SendCommand("{ \"status\":\"T\", \"speed\": " + FormatNumber(0) + ", \"balance\":" + FormatNumber(0) + "}");
                    }

                    /**if (flying && threshold[c].voteabove(GetPSD(fft[c], 30, 34)))
                    {
                        dofire = false;
                        flying = false;
                        Console.WriteLine("LAND!!!");
                        SendCommand("{ \"status\":\"L\", \"speed\": " + FormatNumber(0) + ", \"balance\":" + FormatNumber(0) + "}");
                    }**/


                    // USAME Para mandar esto a Matlab
                    //SendFFT2(s, 2);
                    SendFFT2(fft[0], WINDOWSIZE/2);
                }
            }
        }

        private void SendFFT2(double[] fs, int size)
        {
            StringBuilder strb = new StringBuilder();

            strb.Append("fftsignal = [");
            for (int i = 0; i < size; i++)
            {
                double val = fs[i];

                //val = Math.Abs(val);
                strb.Append(" " + String.Format(new System.Globalization.CultureInfo("en-GB"),"{0:00000.0000000}", val) + " ");
            }

            strb.Append("];");

            SendPlot(strb.ToString());
        }

        //private void ddoFFT()
        //{
        //    for (int c = 0; c < 2; c++)
        //    {
        //        if (samples % 128 == 0)
        //        {

        //            /**
        //            for (int d = 0; d < 128; d++)
        //            {
        //                window[c][d].Re = (float)(10 * Math.Sin(2 * Math.PI * (8F/128F) * d));
        //                window[c][d].Re += (float)(7 * Math.Sin(2 * Math.PI * (15F / 128F) * d));
        //            }**/


        //            ComplexF[] temp = new ComplexF[128];

        //            Array.Copy(window[c], temp, 128);


        //            Fourier.FFT(temp, 128, FourierDirection.Forward);

        //            Array.Copy(temp, fft[c], 128);




        //            /**
        //            for (int d = 0; d < 128; d++)
        //            {
        //                if (((d < 16 || d > 32) && d < 64) ||
        //                ((d < 128 - 32 || d > 128 - 16) && d > 64))
        //                {
        //                    fft[c][d].Im = 0;
        //                }
        //            }**/


        //            /**
        //            float numBins = 128 / 2;  // Half the length of the FFT by symmetry
        //            float binWidth = 128 / numBins; // Hz

        //            float DCGain=1.0F;
        //            float order = 3;
        //            float f0 = 45;


        //            // Filter
        //            for(int i=0;i<128/2;i++)
        //            {
        //                float binFreq = binWidth * i;
        //                float gain = (float)(DCGain / ( Math.Sqrt( ( 1 + 
        //                Math.Pow( binFreq / f0, 2.0 * order ) ) ) ));
        //                window[c][i].Re *= gain; window[c][i].Im *= gain;
        //                window[c][128 - i].Re *= gain; window[c][128 - i].Im *= gain;
        //            }


        //            f0 = 72;
        //            for (int i = 0; i < 128 / 2; i++)
        //            {
        //                float binFreq = binWidth * i;
        //                float gain = (float)(DCGain / (Math.Sqrt((1 +
        //                Math.Pow(binFreq / f0, 2.0 * order)))));
        //                window[c][i].Re *= gain; window[c][i].Im *= gain;
        //                window[c][128 - i].Re *= gain; window[c][128 - i].Im *= gain;
        //            }**/

        //            SendFFT(c);

        //            psd[c] = GetPSD(c, 15, 25);

        //            // Reset the window
        //            window[c] = new ComplexF[128];
        //        }
        //        else
        //        {
        //            ComplexF timepoint = new ComplexF();
        //            timepoint.Im = 0;

        //            switch (c)
        //            {
        //                case 0:
        //                    timepoint.Re = (float)eegtickgen.o1;
        //                    break;
        //                case 1:
        //                    timepoint.Re = (float)eegtickgen.o2;
        //                    break;
        //                default: break;
        //            }


        //            window[c][samples % 128] = timepoint;
        //        }
        //    }
        //}

        //private void SendFFT(int c)
        //{
        //    StringBuilder strb = new StringBuilder();

        //    strb.Append("fftsignal = [");
        //    for (int i = 0; i < 128; i++)
        //    {
        //        float val = fft[c][i].Im;

        //        val = window[c][i].Re;

        //        val = Math.Abs(val);
        //        strb.Append(" " + val + " ");

        //        fft[c][i].Im = val;
        //    }

        //    strb.Append("];");

        //    SendPlot(strb.ToString());
        //}


        private double[] GetFFT(double[] thisfs)
        {
            double[] thisfft = new double[WINDOWSIZE];

            ComplexF[] temp = new ComplexF[WINDOWSIZE];

            for (int i = 0; i < WINDOWSIZE; i++) temp[i].Re = (float)thisfs[i];

            Fourier.FFT(temp, WINDOWSIZE, FourierDirection.Forward);

            for (int i = 0; i < WINDOWSIZE / 2; i++) thisfft[i] = Math.Abs((double)temp[i].Im);

            return thisfft;
        }

        private double GetPSD(double []fft, int minRange, int maxRange)
        {
            double tot = 0;
            for (int i = minRange; i < maxRange; i++)
            {
                tot += fft[i];
            }
            return tot;
        }

        #region Minecraft

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

                    int newwx = (int)(eegtickgen.gyrox);
                    int newwy = (int)(eegtickgen.gyroy);

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

                    if (counter > 1000) eegtickgen.Stop(); ;
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

        private void buttdon1_Click(object sender, EventArgs e)
        {
            //t = new PlainTickGenerator();
            eegtickgen = new EEGDataTickGenerator(textBox1.Text);

            eegtickgen.experiment.datadirectory = "C:\\Users\\User\\Desktop\\RobotMindController\\";

            eegtickgen.SetStopTimer(4);


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


            eegtickgen.StartEvent();
            eegtickgen.Start(this);
        }

        #endregion


        public delegate void Blink();

        /**
         * This Method is the only that gets executed and updated all the info in the UI.
         * 
         **/ 
        public void blink()
        {
            textBox2.Text = avgx.ToString();
            textBox3.Text = avgy.ToString();
            
            textBox2.Text = speed.ToString();
            textBox3.Text = balance.ToString();

            textBox9.Text = threshold[0].T.ToString();
            textBox10.Text = threshold[1].T.ToString();

            textBox4.Text = samples.ToString();

            //psdO1.Text = eegtickgen.o1.ToString(); //  (GetPSD(0, 16, 32) / ((32 - 16))).ToString();
            //psdO2.Text = eegtickgen.o2.ToString(); //  (GetPSD(1, 16, 32) / ((32 - 16))).ToString();

            textBox11.BackColor = Color.FromArgb(rescale((int)(GetPSD(fft[0], 30, 34) * 255.0D / 10000D),0,255), 0, 0);
            textBox12.BackColor = Color.FromArgb(rescale((int)(GetPSD(fft[1], 30, 34) * 255.0D / 10000D),0,255), 0, 0);

            

            psdO1.Text = GetPSD(fft[0], 30, 34).ToString();
            psdO2.Text = GetPSD(fft[1], 30, 34).ToString();


            textBox5.Text = changex.ToString();
            textBox6.Text = changey.ToString();

            sensibility = float.Parse(textBox7.Text);

            textBox8.Text = String.Format("{0:00000.0000000}", (eegtickgen.frequency / 1000.0F));
        }


        Thread threadC;

        private void button1_Click(object sender, EventArgs e)
        {
            SignalStart();

            /**ThreadStart threadStartC = delegate() {
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

                    eegtickgen.StartEvent(message);

                }

                Console.WriteLine("Finishing event thread....");
            
            };

            threadC = new Thread(threadStartC);

            threadC.Start();**/

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

        #region Windows Functions to Control the Cursor
        [DllImportAttribute("User32.dll")]

        private static extern int FindWindow(String ClassName, String
        WindowName);

        [DllImportAttribute("User32.dll")]
        private static extern int SetForegroundWindow(int hWnd);

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
        #endregion


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
            SendCommand("{ \"status\":\"L\", \"speed\": " + 0 + ", \"balance\":" + 0 + "}");
            SendPlot("finishplot");

            eegtickgen.Stop();

            Console.WriteLine("Shutting down everything...");

            portcontroller.Stop();

            portcontroller.Noise();

            portcontroller.Terminate();

            
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

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {

        }

        DroneController droneController = new DroneController();



        private void btnEmergency_Click(object sender, EventArgs e)
        {
            droneController.SendEmergency();
        }

        private String FormatNumber<T>(T value)
        {
            return String.Format(new System.Globalization.CultureInfo("en-GB"), "{0:0.0000000}", value);
        }

        private int linealdynamicrange(int value, int Lmin, int Lmax, int min, int max)
        {
            float gvalue = value;

            if (!(min < Lmin && Lmin < min && min < Lmax && Lmax < max))
            {
                gvalue = (((float)max - (float)min) / ((float)Lmax - (float)Lmin)) * ((float)value - (float)Lmax) + (float)max;

            }

            return (int)gvalue;
        }



        private int rescale(int value, int min, int max)
        {
            int newvalue = value;

            if (value > max) { newvalue = max; }
            if (value < -max) { newvalue = -max; }

            return newvalue;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {

            if (trackBar1.Value == 1)
            {
                portcontroller.enabled = true;
            }
            else portcontroller.enabled = false;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            calibrating = (trackBar2.Value==0);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(0.0F) + ", \"balance\":" + FormatNumber(-0.1F) + "}");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(0.0F) + ", \"balance\":" + FormatNumber(0.1F) + "}");
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(0.1F) + ", \"balance\":" + FormatNumber(0.0F) + "}");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(-0.1F) + ", \"balance\":" + FormatNumber(0.0F) + "}");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SendCommand("{ \"status\":\"A\", \"speed\": " + FormatNumber(0) + ", \"balance\":" + FormatNumber(0) + "}");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            SendCommand("{ \"status\":\"T\", \"speed\": " + FormatNumber(0) + ", \"balance\":" + FormatNumber(0) + "}");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            SendCommand("{ \"status\":\"L\", \"speed\": " + FormatNumber(0) + ", \"balance\":" + FormatNumber(0) + "}");
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }


    }
}
