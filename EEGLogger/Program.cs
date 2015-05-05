using System;
using System.Collections.Generic;
using Emotiv;
using System.IO;
using System.Threading;
using System.Reflection;

namespace EEGLogger
{
    public enum EE_DataChannel_t
    {
        COUNTER = 0, INTERPOLATED, RAW_CQ,
        AF3, F7, F3, FC5, T7,
        P7, O1, O2, P8, T8,
        FC6, F4, F8, AF4, GYROX,
        GYROY,
        TIMESTAMP, ES_TIMESTAMP,
        FUNC_ID, FUNC_VALUE, MARKER,
        SYNC_SIGNAL
    } ;

    class EEG_Logger
    {
        EmoEngine engine; // Access to the EDK is viaa the EmoEngine 
        int userID = -1; // userID is used to uniquely identify a user's headset
        string filename = "eeg.log"; // output filename

        
        EEG_Logger()
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
        void Run()
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
            TextWriter file = new StreamWriter(filename,true);

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
                            file.Write( value + " "); break;
                    }
                }

                file.WriteLine("");

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

        static void Main(string[] args)
        {
            Console.WriteLine("EEG Data Reader Example");

            EEG_Logger p = new EEG_Logger();

            for (int i = 0; i < 128; i++)
            {
                p.Run();
                // This is a mandatory step, otherwise no alloc time is given to the driver to read data from the eeg.
                Thread.Sleep(100);
            }

            Console.ReadLine();

        }

    }
}
