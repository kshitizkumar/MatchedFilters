using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using EricOulashin;
using AForge;
using AForge.Math;



namespace AudioMerger
{

    public class Wav2FFT
    {
        public Wav2FFT() { }

        public static int MaxSamples = 16384;

        public static float[] ConvertWav2FFTMag(string wavfile, string outfile)
        {
            WAVFile audioFile = new WAVFile();
            String warning = audioFile.Open(wavfile, WAVFile.WAVFileMode.READ);
            Complex[] indata = new Complex[MaxSamples];
            int IterSamples = (audioFile.NumSamples < MaxSamples) ? audioFile.NumSamples : MaxSamples;
            float[] indata_mag = null;

            if (warning == "")
            {                                
                //short audioSample = 0;
                for (int sampleNum = 0; sampleNum < IterSamples; ++sampleNum)
                {
                    //audioSample = audioFile.GetNextSampleAs16Bit();
                    indata[sampleNum].Re = audioFile.GetNextSampleAs16Bit();
                }
                FourierTransform.FFT(indata, FourierTransform.Direction.Forward);                   
                indata_mag = GetMagnitude(indata);

                if (outfile != null)
                {
                    WriteData(indata_mag, outfile);
                }
            }
            audioFile.Close();
            return indata_mag;
        }

        public static float [] GetMagnitude(Complex[] indata)
        { 
            int N = indata.Length;
            float [] indata_mag = new float [N];
            for (int i = 0; i < N; i++)
            {
                indata_mag[i] = (float) (indata[i].Magnitude / indata[0].Magnitude);
            } 

            return indata_mag;
        }


        public static void WriteData(float [] indata, string outfile)
        {            

            using (BinaryWriter writer = new BinaryWriter(File.Open(outfile, FileMode.Create)))
            {
                writer.Write(indata.Length);
                for (int i = 0; i < indata.Length; i++)
                {
                    writer.Write(indata[i]);
                }
                writer.Close();
            }
        }

        public static float[] ReadData(string infile)
        {

            using (BinaryReader reader = new BinaryReader(File.Open(infile, FileMode.Open)))
            {
                int N = (int)reader.ReadInt32();
                float[] outdata = new float[N];
                for (int i = 0; i < N; i++)
                {
                    outdata[i] = reader.ReadSingle();
                }
                reader.Close();
                return outdata;
            }
            
        }
    }



    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        static void Main()
        {
            // Step1 - Read a pair of (inputwav, outputfft), convert wav to fft magnitude coefficients, and write to outputfft file
            // This will be done for training samples for all classes

            StreamReader list = new StreamReader(new FileStream("C:\\Kshitiz\\train.txt", FileMode.Open));
            String ifname;
            String Trainifname;
            
            int i = 0;
            while (!list.EndOfStream)
            {
                ifname = list.ReadLine();
                String[] inout = ifname.Split();
                Wav2FFT.ConvertWav2FFTMag(inout[0], inout[1]);
                i++;
            }
            list.Close();

            // Step2 - Read (inputwav), convert wav to fft magnitude coefficients
            // This will be done for all test samples for all classes

            list = new StreamReader(new FileStream("C:\\Kshitiz\\test.txt", FileMode.Open));
            TextWriter Reslist = File.CreateText("C:\\Kshitiz\\test.txt.res");

            float[] testdata;
            float[] traindata;
                        
            // Read each test wav file
            while (!list.EndOfStream)
            {
                double sum = 0;
                double maxsum = 0;
                string bestMatch = null;
                ifname = list.ReadLine();
                String[] inout = ifname.Split();
                testdata = Wav2FFT.ConvertWav2FFTMag(inout[0], null); // To use overloading

                // Iterate over each of the training fft data (that was pre-computed)
                StreamReader Trainlist = new StreamReader(new FileStream("C:\\Kshitiz\\train.txt", FileMode.Open));
                while (!Trainlist.EndOfStream)
                {
                    Trainifname = Trainlist.ReadLine();
                    String[] Traininout = Trainifname.Split();
                    traindata = Wav2FFT.ReadData(Traininout[1]);

                    sum = 0;
                    // assert that the lengths for train and test are identical
                    if (traindata.Length != testdata.Length)
                    {
                        Console.WriteLine("Train and Test data file length mismatch -- unexpected");
                        continue;
                    }

                    for (int j = 0; j < traindata.Length; j++)
                    {
                        sum += traindata[j] * testdata[j];
                    }

                    if (sum > maxsum)
                    {
                        maxsum = sum;
                        bestMatch = Traininout[1];
                    }
                }
                Trainlist.Close();
                Reslist.WriteLine("Input = {0}, BestMatch = {1}", inout[0], bestMatch); 
            }
            list.Close();
            Reslist.Close();
        }
    }
}
