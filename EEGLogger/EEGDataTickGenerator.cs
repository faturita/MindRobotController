using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Emotiv;
using System.IO;
using System.Diagnostics;

namespace EEGLogger
{
    /**
     * EEGDataTickGenerator
     * 
     * Main class.  It connects to the EmoEngine from the DotNetEmotivSDK to receive EEG data.
     * As long as the data is available, it fires a tick to the TickListener with the received data.
     * 
     * This ticks can be used in a control program as a tick generator device
     * which will be coupled to the EpocEmotiv Sampling Frequency.
     * 
     * If you need the data, you will have to check the file or use EEGDataTickGenerator
     * 
     * 
     **/
    public class EEGDataTickGenerator : BaseTickGenerator
    {
        // Emotiv references.
        EmoEngine engine; // Access to the EDK is viaa the EmoEngine 
        int userID = -1; // userID is used to uniquely identify a user's headset

        // Retrieved data
        // @TODO: Must be buffered !!!!
        public double gyrox, gyroy;


        public double o1, o2;

        public double[] O1 = new double[300];
        public double[] O2 = new double[300];

        public bool BULK = false;

        // Data capture frequency.
        public double frequency = 1;

        // Event Counter and Experiment information.
        int eventCounter = 0;

        public string filename = "eeg.dat"; // output filename

        public Experiment experiment = new Experiment();

        // Cycle counter.  How many times did we actually execute?  It is used to calculate the sampling frequency.  Reset at 1000.
        private long cycles = 0;

        // How many times we did receive data from the Emotiv?
        public long beats = 0;

        // Can be used to stop processing information.
        public long MaxSamples = long.MaxValue;

        public EEGDataTickGenerator(string subject)
        {
            experiment.Subject = subject;
            filename = experiment.GetPath() + "\\" + filename;
        }

        public EEGDataTickGenerator(Experiment experiment, string subject)
        {
            this.experiment = experiment;
            this.experiment.Subject = subject;
            filename = experiment.GetPath() + "\\" + filename;
        }

        public override void StartEvent()
        {
            if (!Directory.Exists(experiment.GetPath()))
            {
                Directory.CreateDirectory(experiment.GetPath());
            }
            filename = experiment.GetPath() + "\\" + "eeg_" + (eventCounter++) + ".dat";
        }

        public override void StartEvent(string eventName)
        {
            if (!Directory.Exists(experiment.GetPath()))
            {
                Directory.CreateDirectory(experiment.GetPath());
            }
            filename = experiment.GetPath() + "\\" + "eeg_" + eventName + "_" + (eventCounter++) + ".dat";
        }

        public void Connect()
        {
            // create the engine
            engine = EmoEngine.Instance;
            engine.UserAdded += new EmoEngine.UserAddedEventHandler(engine_UserAdded_Event);

            // connect to Emoengine.            
            engine.Connect();

            // create a header for our output file
            //WriteHeader();
        }

        void engine_UserAdded_Event(object sender, EmoEngineEventArgs e)
        {
            Console.WriteLine("User Added Event has occured");

            // record the user 
            userID = (int)e.userId;

            // enable data aquisition for this user.
            engine.DataAcquisitionEnable((uint)userID, true);

            // ask for up to 1 second of buffered data
            engine.EE_DataSetBufferSizeInSec(1);

        }

        public void SetStopTimer(long counter)
        {
            MaxSamples = counter;
        }

        public override void Run()
        {
            Connect();

            while (!stopped)
            {
                Stopwatch st = new Stopwatch();

                st.Reset(); st.Start();
                cycles++;

                RunEEGLoop();
                //Thread.Sleep(100);



                /***
                if (beats>MaxSamples)
                {
                    Console.WriteLine("Stopped....");
                    stopped = true;
                }**/

                if (cycles > 1000)
                {
                    st.Stop();
                    frequency = cycles / ((st.Elapsed.TotalMilliseconds == 0) ? 1 : st.Elapsed.TotalMilliseconds);
                    cycles = 0;
                }

            }

            listener.SignalStop();
        }

        public void RunEEGLoop()
        {
            // Handle any waiting events
            engine.ProcessEvents();

            // If the user has not yet connected, do not proceed
            if ((int)userID == -1)
                return;

            Dictionary<EdkDll.EE_DataChannel_t, double[]> data = engine.GetData((uint)userID);


            if (data == null)
            {
                //Console.WriteLine(beats+":"+"No data To Read.");
                return;
            }

            int _bufferSize = data[EdkDll.EE_DataChannel_t.TIMESTAMP].Length;

            //Console.WriteLine(beats+":"+"Writing " + _bufferSize.ToString() + " lines of data ");

            // Write the data to a file
            TextWriter file = new StreamWriter(filename, true);

            int[] samps = new int[14];

            Array.Clear(O1, 0, 300);

            for (int i = 0; i < _bufferSize; i++)
            {
                // now write the data
                foreach (EdkDll.EE_DataChannel_t channel in data.Keys)
                {
                    switch (channel)
                    {
                        case EdkDll.EE_DataChannel_t.GYROX: gyrox = data[channel][i];
                            break;
                        case EdkDll.EE_DataChannel_t.GYROY: gyroy = data[channel][i]; 
                            break;

                        case EdkDll.EE_DataChannel_t.O1: O1[samps[0]] = data[channel][i]; samps[0]++;
                            break;
                        case EdkDll.EE_DataChannel_t.O2: O2[samps[1]] = data[channel][i]; samps[1]++;
                            break;
                        default: break;
                    }

                    switch (channel)
                    {
                        case EdkDll.EE_DataChannel_t.INTERPOLATED:
                        case EdkDll.EE_DataChannel_t.RAW_CQ:
                        case EdkDll.EE_DataChannel_t.ES_TIMESTAMP: 
                            // Do nothing for this fields.
                            continue;
                        default:
                            String value = String.Format("{0:00000.0000000}", data[channel][i]);
                            file.Write(value + " ");
                            break;
                    }
                }

                //Console.WriteLine("Samps:" + samps + "-" + samps2 + "-" + samps3);

                file.WriteLine("");

                beats++;

                if (BULK)
                {

                    o1 = O1[0];
                    o2 = O2[0];

                    listener.tick();
                }
                else
                {
                    for (int j = 0; j < samps[0]; j++)
                    {
                        // Ticks should go into the data processing to avoid loosing data....
                        // TODO Check if data synchronization is working properly.
                        o1 = O1[j];
                        o2 = O2[j];

                        listener.tick();
                    }
                }


            }
            file.Close();

        }

        public void WriteHeader()
        {
            TextWriter file = new StreamWriter(filename, false);

            string header = "COUNTER,INTERPOLATED,RAW_CQ,AF3,F7,F3, FC5, T7, P7, O1, O2,P8" +
                            ", T8, FC6, F4,F8, AF4,GYROX, GYROY, TIMESTAMP, ES_TIMESTAMP" +
                            "FUNC_ID, FUNC_VALUE, MARKER, SYNC_SIGNAL,";

            /**
            	ED_COUNTER,
		        ED_AF3, ED_F7, ED_F3, ED_FC5, ED_T7, 
		        ED_P7, ED_O1, ED_O2, ED_P8, ED_T8, 
		        ED_FC6, ED_F4, ED_F8, ED_AF4, ED_GYROX, ED_GYROY, ED_TIMESTAMP, 
		        ED_FUNC_ID, ED_FUNC_VALUE, ED_MARKER, ED_SYNC_SIGNAL
             * 
             * **/

            file.WriteLine(header);
            file.Close();
        }


        public bool NO_DATA_LOSS { get; set; }
    }
}
