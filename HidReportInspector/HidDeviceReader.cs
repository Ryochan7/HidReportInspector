using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using HidLibrary;

namespace HidReportInspector
{
    public class HidDeviceReader
    {
        public class FeatureReport
        {
            private int reportID;
            public int ReportID { get => reportID; set => reportID = value; }

            private List<byte> reportBytes = new List<byte>();
            public List<byte> ReportBytes
            {
                get => reportBytes; set => reportBytes = value;
            }
        }

        private const int FALLBACK_OUTPUT_REPORT_LEN = 64;

        private HidDevice hidDevice;
        public HidDevice HidDevice { get => hidDevice; }
        private List<FeatureReport> cachedFeatureData = new List<FeatureReport>();
        public List<FeatureReport> CachedFeatureData { get => cachedFeatureData; }

        private byte[] inputBuffer;
        public byte[] InputBuffer { get => inputBuffer; }
        private byte[] outputBuffer;

        private Thread readerThread;
        private bool exitInputThread;

        public delegate void ReportHandler<TEventArgs>(HidDeviceReader sender, TEventArgs args);
        public event ReportHandler<EventArgs> Report;
        public event ReportHandler<EventArgs> Disconnect;

        public HidDeviceReader(HidDevice device)
        {
            hidDevice = device;
            hidDevice.OpenDevice(false);

            if (hidDevice.Capabilities.FeatureReportByteLength > 0)
            {
                GrabFeatureReports();
            }

            inputBuffer = new byte[hidDevice.Capabilities.InputReportByteLength];

            int tempOutputLength = hidDevice.Capabilities.OutputReportByteLength > 0 ?
                hidDevice.Capabilities.OutputReportByteLength : FALLBACK_OUTPUT_REPORT_LEN;
            outputBuffer = new byte[tempOutputLength];
            if (!hidDevice.IsFileStreamOpen())
            {
                hidDevice.OpenFileStream(tempOutputLength);
            }
        }

        private void GrabFeatureReports()
        {
            byte[] reportData =
                new byte[hidDevice.Capabilities.FeatureReportByteLength];

            for (int i = 0; i < hidDevice.Capabilities.NumberFeatureDataIndices; i++)
            {
                Array.Clear(reportData, 0, reportData.Length);
                reportData[0] = (byte)i;
                bool result = hidDevice.readFeatureData(reportData);
                if (!result)
                {
                    Array.Clear(reportData, 0, reportData.Length);
                }

                FeatureReport currentReport = new FeatureReport()
                {
                    ReportID = i,
                    ReportBytes = reportData.ToList(),
                };
                cachedFeatureData.Add(currentReport);

                //Thread.Sleep(1);
            }
        }

        public void StartUpdate()
        {
            if (hidDevice.IsOpen)
            {
                readerThread = new Thread(ReadInput);
                readerThread.Priority = ThreadPriority.AboveNormal;
                readerThread.Name = "HidDevice Input thread";
                readerThread.IsBackground = true;
                readerThread.Start();
            }
        }

        private void ReadInput()
        {
            unchecked
            {
                while (!exitInputThread)
                {
                    HidDevice.ReadStatus res = hidDevice.ReadWithFileStream(inputBuffer);
                    if (res != HidDevice.ReadStatus.Success)
                    {
                        Disconnect?.Invoke(this, EventArgs.Empty);
                        exitInputThread = true;
                        continue;
                    }

                    Report?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void StopUpdate()
        {
            if (readerThread != null &&
                readerThread.IsAlive && !readerThread.ThreadState.HasFlag(System.Threading.ThreadState.Stopped) &&
                !readerThread.ThreadState.HasFlag(System.Threading.ThreadState.AbortRequested))
            {
                try
                {
                    exitInputThread = true;
                    //ds4Input.Interrupt();
                    //if (!abortInputThread)
                    if (!readerThread.Join(1000))
                    {
                        // Terminate reader thread if possibly stuck on file stream
                        readerThread.Interrupt();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public void Close()
        {
            //hidDevice.fileStream.Close();
            StopUpdate();
            hidDevice.CloseDevice();
        }
    }
}
