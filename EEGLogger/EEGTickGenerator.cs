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
     * Public Interface to Receive Ticks
     ***/
    public interface TickReceiver
    {
        void tick();

        void SignalStop();
    }

    /**
     * Public interface to generate Ticks
     * 
     **/ 
    public interface TickGenerator
    {
        void Start(TickReceiver listener);
        void Stop();
        void Run();
        void StartEvent();// This should be part of a different interface
    }


    public class RandomTickGenerator : EEGDataTickGenerator
    {
        public RandomTickGenerator() :  base("random")
        {
        }

        public override void Run()
        {
            while (!stopped)
            {
                Random r = new Random();

                o1 = r.Next(3);
                o2 = r.Next(3);

                gyrox = 0;
                gyroy = 0;
            }
        }
    }

    /**
     * Abstract Tick Generator.
     * 
     * Creates a thread and establish a relation with a TickReceiver listener.
     * 
     ***/
    public abstract class BaseTickGenerator : TickGenerator
    {
        public Boolean stopped;

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
        public string datadirectory = ".\\Data\\";

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

    /**
     * EEGTickGenerator
     * 
     * Main class.  It connects to the EmoEngine from the DotNetEmotivSDK to receive EEG data.
     * As long as the data is available, it fires a tick to the TickListener and DUMPS THE DATA IN A FILE.
     * 
     * This ticks can be used in a control program as a tick generator device
     * which will be coupled to the EpocEmotiv Sampling Frequency.
     * 
     * If you need the data, you will have to check the file or use EEGDataTickGenerator
     * 
     * 
     **/ 
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

        /**
         * Connect to the EmoEngine!
         * 
         **/ 
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

        /** 
         * Run!  Start receiving data from the Emotiv and loop until this class is stopped.
         * 
         **/ 
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
                        // You can filter here which field do you want.
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

            /** Emotiv Frame order.
            	ED_COUNTER,
		        ED_AF3, ED_F7, ED_F3, ED_FC5, ED_T7, 
		        ED_P7, ED_O1, ED_O2, ED_P8, ED_T8, 
		        ED_FC6, ED_F4, ED_F8, ED_AF4, ED_GYROX, ED_GYROY, ED_TIMESTAMP, 
		        ED_FUNC_ID, ED_FUNC_VALUE, ED_MARKER, ED_SYNC_SIGNAL
             * 
             ***/

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
