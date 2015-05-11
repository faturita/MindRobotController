using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace MindRobotController
{
    class Threshold
    {
        const double threshold0 = 2500D;

        public double T = 1500;

        int MIN_WINDOW_SIZE = 5;
        int MAX_WINDOW_SIZE = 20;

        List<double> M = new List<double>();


        List<double> PSDs = new List<double>();


        internal void add(double p)
        {
            Console.WriteLine("P=" + p);

            // Alpha poda.
            if (p > 10000.0) return;
            if (p < 100.0) return;
            M.Add(p);
        }

        internal double getT()
        {
            int ng1=0, ng2=0;

            foreach (double v in M)
            {
                if (v > T) ng1++;
                if (v < T) ng2++;
            }

            double m1=0;
            double m2=0;

            foreach (double v in M)
            {
                if (v > T) m1 += v;
                if (v < T) m2 += v;
            }

            if (ng1 > 0) m1 = 1 / (double)ng1 * m1;
            if (ng2 > 0) m2 = 1 / (double)ng2 * m2;

            /**
            ng1 = M.Sum(w => ((w>T)?1:0));
            ng2 = M.Sum(w => ((w<T)?1:0));

            double m1 = 0, m2 = 0;

            if (ng1>0)  m1=1/(double)ng1 *M.Sum(w => ((w>T)?w:0));
            if (ng2>0)  m2=1/(double)ng2 *M.Sum(w => ((w<T)?w:0));**/

            Console.WriteLine(T+ "-->" + ng1 + "." + ng2 + "," + m1 + "-" + m2);

            T = 1.0/2.0*(m1+m2);

            return T;
        }

        internal void calibrate(double s)
        {
            add(s);

            double T = getT();

            Console.WriteLine("T=" + T);
        }

        internal bool voteabove(double psd)
        {
            PSDs.Add(psd);

            if (PSDs.Count < MIN_WINDOW_SIZE)
            {
                return false;
            }

            int above = PSDs.Sum(w => ((w > T) ? 1 : 0));
            int bellow = PSDs.Sum(w => ((w < T) ? 1 : 0));

            Console.WriteLine("T:" + T + ".Above:" + above + " - Bellow:" + bellow);

            return (bellow < above);

        }

        internal bool votebellow(double psd)
        {
            PSDs.Add(psd);

            if (PSDs.Count < MIN_WINDOW_SIZE)
            {
                return false;
            }

            if (PSDs.Count > MAX_WINDOW_SIZE)
            {
                // Get ride of the oldest element
                PSDs.RemoveAt(0);
            }

            int above =   PSDs.Sum(w => ((w > T) ? 1 : 0));
            int bellow =  PSDs.Sum(w => ((w < T) ? 1 : 0));

            Console.WriteLine("T:" + T + ".Above:" + above + " - Bellow:" + bellow);

            return (bellow > above);
        }

        


        static void Mfain(string[] args)
        {
            Threshold t = new Threshold();

            for (int i = 1; i < 100; i++)
            {
                t.calibrate(1300.0);
            }

            for (int i = 1; i < 100; i++)
            {
                t.calibrate(4000.0);
            }

            Random r = new Random();

            for (int i = 0; i < 30; i++)
            {
                if (t.votebellow(3000.0))
                {
                    Console.WriteLine(".");
                }
            }

            for (int i = 0; i < 70; i++)
            {
                if (t.votebellow(2500.0))
                {
                    Console.WriteLine(".");
                }
            }

            Console.WriteLine(t.votebellow(2300.0));


        }

        static public void SerializeToXML(Threshold threshold)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Threshold));
            TextWriter textWriter = new StreamWriter(@"threshold.xml");
            serializer.Serialize(textWriter, threshold);
            textWriter.Close();
        }

        static Threshold DeserializeFromXML()
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(List<Threshold>));
            TextReader textReader = new StreamReader(@"threshold.xml");
            Threshold threshold;
            threshold = (Threshold)deserializer.Deserialize(textReader);
            textReader.Close();

            return threshold;
        }

        static List<Threshold> DeserializesFromXML()
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(List<Threshold>));
            TextReader textReader = new StreamReader(@"thresholds.xml");
            List<Threshold> thresholds;
            thresholds = (List<Threshold>)deserializer.Deserialize(textReader);
            textReader.Close();

            return thresholds;
        }
    }
}
