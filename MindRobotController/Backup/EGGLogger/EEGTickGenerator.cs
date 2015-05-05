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
    public interface TickReceiver
    {
        void tick();

        void SignalStop();
    }

    public interface TickGenerator
    {
        void Start(TickReceiver listener);
        void Stop();
        void Run();
        void StartEvent();// This should be part of a different interface
    }

    public abstract class BaseTickGenerator : TickGenerator
    {
        protected Boolean stopped;

        Thread threadA;

        protected TickReceiver listener;

        public virtual void Start(TickReceiver listener)
        {
            this.listener = listener;

            ThreadStart threadStartA = delegate() { Run(); };

            threadA = new Thread(threadStartA);

            threadA.Start();

        }

        public void Stop()
        {
            stopped = true;
            threadA.Abort();
        }

        public abstract void Run();

        public virtual void StartEvent()
        {
        }

        public virtual void StartEvent(string eventName)
        {
        }
    }

    public class Experiment
    {
        public string datadirectory = "C:\\Users\\User\\Desktop\\Data\\";

        string pathString;

        public string Subject
        {
            set {
                pathString = datadirectory + "\\" + value;

                if (!System.IO.Directory.Exists(pathString))
                    System.IO.Directory.CreateDirectory(pathString);
            }
        }

        public string GetPath()
        {
            return pathString;
        }
    }

    public class EEGTickGenerator : BaseTickGenerator
    {
        EmoEngine engine; // Access to the EDK is viaa the EmoEngine 
        int userID = -1; // userID is used to uniquely identify a user's headset

        int eventCounter = 0;

        string filename = "eeg.dat"; // output filename

        public Experiment experiment = new Experiment();

        public EEGTickGenerator(string subject)
        {
            experiment.Subject = subject;
            filename = experiment.GetPath() + "\\" + filename;
        }

        public override void StartEvent()
        {
            filename = experiment.GetPath() + "\\" + "eeg_" + (eventCounter++) + ".dat";
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

        public override void Run()
        {
            Connect();

            while (!stopped)
            {
                RunEEGLoop();
                Thread.Sleep(100);
            }
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
                return;
            }

            int _bufferSize = data[EdkDll.EE_DataChannel_t.TIMESTAMP].Length;

            Console.WriteLine("Writing " + _bufferSize.ToString() + " lines of data ");

            // Write the data to a file
            TextWriter file = new StreamWriter(filename, true);

            for (int i = 0; i < _bufferSize; i++)
            {
                // now write the data
                foreach (EdkDll.EE_DataChannel_t channel in data.Keys)
                {
                    switch (channel)
                    {
                        case EdkDll.EE_DataChannel_t.INTERPOLATED:
                        case EdkDll.EE_DataChannel_t.RAW_CQ:
                        case EdkDll.EE_DataChannel_t.ES_TIMESTAMP: continue;
                        default:
                            String value = String.Format("{0:00000.0000000}", data[channel][i]);
                            file.Write(value + " "); break;
                    }
                }

                file.WriteLine("");

                listener.tick();
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

    }

    /**
     * This is the most complete class to handle Emotiv EEG Data.
     * 
     ***/ 
    public class EEGDataTickGenerator : BaseTickGenerator
    {
        // Emotiv references.
        EmoEngine engine; // Access to the EDK is viaa the EmoEngine 
        int userID = -1; // userID is used to uniquely identify a user's headset

        // Retrieved data
        // @TODO: Must be buffered !!!!
        public double gyrox, gyroy;


        public double o1, o2;


        // Capturing frequency.
        public double frequency=1;

        // Event Counter and Experiment information.
        int eventCounter = 0;

        public string filename = "eeg.dat"; // output filename

        public Experiment experiment = new Experiment();

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

        private long cycles = 0;

        public long beats = 0;

        public long MaxSamples = long.MaxValue;

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

                        case EdkDll.EE_DataChannel_t.O1: o1 = data[channel][i];
                            break;
                        case EdkDll.EE_DataChannel_t.O2: o2 = data[channel][i];
                            break;
                            
                        default: break;
                    }

                    switch (channel)
                    {
                        case EdkDll.EE_DataChannel_t.INTERPOLATED:
                        case EdkDll.EE_DataChannel_t.RAW_CQ:
                        case EdkDll.EE_DataChannel_t.ES_TIMESTAMP: continue;
                        default:
                            String value = String.Format("{0:00000.0000000}", data[channel][i]);
                            file.Write(value + " "); 
                            break;
                    }
                }

                file.WriteLine("");

                beats++;

                // Ticks should go into the data processing to avoid loosing data....
                // TODO Check if data synchronization is working properly.
                listener.tick();
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

    }

    public class EEGSyncTickGenerator : EEGDataTickGenerator
    {
        private string EventName;

        public EEGSyncTickGenerator(String subject, String eventname) : base(subject)
        {
            this.EventName = eventname;
        }
        public override void StartEvent()
        {
            filename = experiment.GetPath() + "\\" + "eeg_" + EventName + ".dat";
        }

    }

}
