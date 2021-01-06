using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using HidLibrary;

namespace HidReportInspector.ViewModels
{
    public enum ReportByteDisplay : uint
    {
        Default,
        Decimal,
        Hexidecimal,
        Bits
    }

    // Not sure this will be used yet
    public enum ReportByteState : uint
    {
        Default,
        Untracked,
    }

    public class HidDeviceViewModel
    {
        private HidDevice hidDevice;
        public HidDevice HidDevice { get => hidDevice; set => hidDevice = value; }

        public bool HasDevice { get => hidDevice != null; }

        public string Description { get => hidDevice.Description; }
        public string DevicePath { get => hidDevice.DevicePath; }

        public int VidInt { get => hidDevice.Attributes.VendorId; }
        public string VidHex { get => hidDevice.Attributes.VendorHexId; }

        public int PidInt { get => hidDevice.Attributes.ProductId; }
        public string PidHex { get => hidDevice.Attributes.ProductHexId; }

        public int Version { get => hidDevice.Attributes.Version; }

        public int InputLength { get => hidDevice.Capabilities.InputReportByteLength; }
        public int OutputLength { get => hidDevice.Capabilities.OutputReportByteLength; }
        public int FeatureLength { get => hidDevice.Capabilities.FeatureReportByteLength; }
        public int LinkCollectionNodes { get => hidDevice.Capabilities.NumberLinkCollectionNodes; }

        private HidDeviceReader deviceReader;
        public List<HidDeviceReader.FeatureReport> FeatureReportData { get => deviceReader.CachedFeatureData; }

        private List<FeatureReportFormat> featureReportOutputList =
            new List<FeatureReportFormat>();
        public List<FeatureReportFormat> FeatureReportOutputList
        {
            get => featureReportOutputList;
        }

        private ReportBytesContainer reportBytesContainer;
        public ReportBytesContainer ReportBytesContainer { get => reportBytesContainer; }

        public HidDeviceViewModel()
        {
        }

        public void OpenReader()
        {
            if (deviceReader == null && hidDevice != null)
            {
                deviceReader = new HidDeviceReader(hidDevice);
                FormatFeatureReportData();
                PrepareReportBytesDisplay(deviceReader);
                deviceReader.StartUpdate();
            }
        }

        public void CloseReader()
        {
            if (deviceReader != null)
            {
                deviceReader.StopUpdate();
                deviceReader.Close();
                deviceReader = null;
                reportBytesContainer.ReportBytes.Clear();
                reportBytesContainer = null;
            }
        }

        private void FormatFeatureReportData()
        {
            featureReportOutputList.Clear();

            //int i = 0;
            foreach(HidDeviceReader.FeatureReport featureReport in deviceReader.CachedFeatureData)
            {
                FeatureReportFormat tempFeature = new FeatureReportFormat(featureReport);
                featureReportOutputList.Add(tempFeature);

                //byte[] tempBytes = featureReport.ReportBytes.ToArray();
                //string base64String = Convert.ToBase64String(tempBytes);
                //Debug.WriteLine(i);
                //Debug.WriteLine(base64String);
                //i++;
            }
        }

        private void PrepareReportBytesDisplay(HidDeviceReader reader)
        {
            reportBytesContainer = new ReportBytesContainer(hidDevice.Capabilities.InputReportByteLength,
                reader);
        }

        public void StopDataReport()
        {
            CloseReader();
        }

        public void ExportDeviceInfo(string filepath)
        {
            using (StreamWriter sw = new StreamWriter(filepath))
            {
                DeviceInfoReport infoReport = new DeviceInfoReport(hidDevice);
                DeviceReportExport exportReport = new DeviceReportExport(infoReport, reportBytesContainer, featureReportOutputList);

                JsonSerializerOptions options = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                };
                string json = JsonSerializer.Serialize(exportReport, options);
                sw.Write(json);
                sw.Flush();
            }
        }

        public void LoadReportBytes(string filepath)
        {
            string json = string.Empty;
            using (StreamReader sr = new StreamReader(filepath))
            {
                json = sr.ReadToEnd();
            }

            using (JsonDocument jsonDoc = JsonDocument.Parse(json))
            {
                JsonElement rootElement = jsonDoc.RootElement;
                if (rootElement.TryGetProperty("ReportLog", out JsonElement reportLogEl))
                {
                    if (reportLogEl.TryGetProperty("Bytes", out JsonElement bytes) &&
                        bytes.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement reportByte in bytes.EnumerateArray())
                        {
                            int temp = -1;
                            if (reportByte.TryGetProperty("index", out JsonElement indexEl))
                            {
                                int.TryParse(indexEl.ToString(), out temp);
                            }

                            if (temp >= 0 && temp < reportBytesContainer.ReportBytes.Count)
                            {
                                ReportByteInfo byteInfo = reportBytesContainer.ReportBytes[temp];
                                string tempLabel = string.Empty;
                                if (reportByte.TryGetProperty("label", out JsonElement labelEl)) tempLabel = labelEl.GetString();
                                if (!string.IsNullOrEmpty(tempLabel)) byteInfo.ByteTitle = tempLabel;

                                string tempNotes = string.Empty;
                                if (reportByte.TryGetProperty("notes", out JsonElement notesEl)) tempNotes = notesEl.GetString();
                                if (!string.IsNullOrEmpty(tempNotes)) byteInfo.Notes = tempNotes;
                            }
                        }
                    }
                }
            }
        }
    }

    public class FeatureReportFormat
    {
        private HidDeviceReader.FeatureReport featureReportData;
        [JsonIgnore]
        public HidDeviceReader.FeatureReport FeatureReportData { get => featureReportData; }

        public int FeatureID { get => featureReportData.ReportID; }

        private string formattedReportOutput;
        [JsonIgnore]
        public string FormattedReportOutput { get => formattedReportOutput; }

        private string base64ReportString;
        [JsonPropertyName("ReportBytes")]
        public string Base64ReportString
        {
            get => base64ReportString;
        }

        public FeatureReportFormat(HidDeviceReader.FeatureReport reportData)
        {
            featureReportData = reportData;
            PrepareViewOutput();
            base64ReportString = Base64String();
        }

        public void PrepareViewOutput()
        {
            StringBuilder builder = new StringBuilder();
            foreach (byte currentByte in featureReportData.ReportBytes)
            {
                builder.AppendFormat("{0,6:D}", currentByte);
            }

            formattedReportOutput = builder.ToString();
        }

        public string Base64String()
        {
            byte[] tempBytes = featureReportData.ReportBytes.ToArray();
            string base64String = Convert.ToBase64String(tempBytes);
            //Debug.WriteLine(featureReportData.ReportID);
            //Debug.WriteLine(base64String);
            return base64String;
        }
    }

    public class DeviceCapabilities
    {
        private HidDeviceCapabilities deviceCapabilities;

        public int Usage { get => deviceCapabilities.Usage; }
        public int UsagePage { get => deviceCapabilities.UsagePage; }
        public int InputReportByteLength { get => deviceCapabilities.InputReportByteLength; }
        public int OutputReportByteLength { get => deviceCapabilities.OutputReportByteLength; }
        public int FeatureReportByteLength { get => deviceCapabilities.FeatureReportByteLength; }
        public short[] Reserved { get => deviceCapabilities.Reserved; }
        public int NumberLinkCollectionNodes { get => deviceCapabilities.NumberLinkCollectionNodes; }
        public int NumberInputButtonCaps { get => deviceCapabilities.NumberInputButtonCaps; }
        public int NumberInputValueCaps { get => deviceCapabilities.NumberInputValueCaps; }
        public int NumberInputDataIndices { get => deviceCapabilities.NumberInputDataIndices; }
        public int NumberOutputButtonCaps { get => deviceCapabilities.NumberOutputButtonCaps; }
        public int NumberOutputValueCaps { get => deviceCapabilities.NumberOutputValueCaps; }
        public int NumberOutputDataIndices { get => deviceCapabilities.NumberOutputDataIndices; }
        public int NumberFeatureButtonCaps { get => deviceCapabilities.NumberFeatureButtonCaps; }
        public int NumberFeatureValueCaps { get => deviceCapabilities.NumberFeatureValueCaps; }
        public int NumberFeatureDataIndices { get => deviceCapabilities.NumberFeatureDataIndices; }

        public DeviceCapabilities(HidDeviceCapabilities capabilities)
        {
            deviceCapabilities = capabilities;
        }
    }

    public class DeviceInfoReport
    {
        private HidDevice hidDevice;
        private DeviceCapabilities hidDevCapabilities;
        //private HidDeviceAttributes hidDevAttributes;

        public string Description { get => hidDevice.Description; }

        public string DevicePath { get => hidDevice.DevicePath; }

        public DeviceCapabilities Capabilities
        {
            get => hidDevCapabilities;
        }

        [JsonPropertyName("Attributes")]
        public HidDeviceAttributes HidDeviceAttributes
        {
            //get => hidDevAttributes;
            get => hidDevice.Attributes;
        }

        public DeviceInfoReport(HidDevice device)
        {
            hidDevice = device;
            hidDevCapabilities = new DeviceCapabilities(hidDevice.Capabilities);
        }
    }

    public class DeviceReportExport
    {
        private DeviceInfoReport infoReport;
        [JsonPropertyName("Device")]
        public DeviceInfoReport InfoReport { get => infoReport; }

        private List<FeatureReportFormat> featureReportList;
        [JsonPropertyName("FeatureReports")]
        public List<FeatureReportFormat> FeatureReportList
        {
            get => featureReportList;
        }

        private ReportBytesContainer bytesContainer;
        [JsonPropertyName("ReportLog")]
        public ReportBytesContainer BytesContainer { get => bytesContainer; }

        public DeviceReportExport(DeviceInfoReport report,
            ReportBytesContainer reportBytes, List<FeatureReportFormat> featureReports)
        {
            infoReport = report;
            this.bytesContainer = reportBytes;
            featureReportList = featureReports;
        }
    }

    public class ReportBytesContainer
    {
        private List<ReportByteInfo> reportBytes = new List<ReportByteInfo>();
        [JsonPropertyName("Bytes")]
        public List<ReportByteInfo> ReportBytes { get => reportBytes; }

        public ReportBytesContainer(int length, HidDeviceReader reader)
        {
            reader.Report += CheckReportBytes;
            reader.Disconnect += ChangeBytesState;

            for (int i = 0; i < length; i++)
            {
                reportBytes.Add(new ReportByteInfo(i));
            }
        }

        private void ChangeBytesState(HidDeviceReader sender, EventArgs args)
        {
            foreach(ReportByteInfo current in reportBytes)
            {
                current.ChangeToUnchanged();
            }
        }

        private void CheckReportBytes(HidDeviceReader sender, EventArgs args)
        {
            byte[] inputBuffer = sender.InputBuffer;
            for(int i = 0, arlen = inputBuffer.Length; i < arlen; i++)
            {
                ReportByteInfo current = reportBytes[i];
                current.UpdateValue(inputBuffer);
            }
        }
    }

    public class ReportByteInfo
    {
        private int index;
        [JsonPropertyName("index")]
        public int Index
        {
            get => index;
        }

        private string byteTitle = "";
        [JsonPropertyName("label")]
        public string ByteTitle
        {
            get => byteTitle;
            set
            {
                if (byteTitle == value) return;
                byteTitle = value;
                ByteTitleChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ByteTitleChanged;

        private bool tracked = true;
        [JsonIgnore]
        public bool Tracked
        {
            get => tracked;
            set
            {
                if (tracked == value) return;
                tracked = value;
                TrackedChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler TrackedChanged;

        private ReportByteDisplay displayValueAs;
        [JsonIgnore]
        public int DisplayValueAsIndex
        {
            get => (int)displayValueAs;
            set
            {
                int temp = (int)displayValueAs;
                if (temp == value) return;
                displayValueAs = (ReportByteDisplay)value;
                DisplayValueAsIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler DisplayValueAsIndexChanged;

        private string notes = "";
        [JsonPropertyName("notes")]
        public string Notes
        {
            get => notes;
            set => notes = value;
        }

        private byte currentValue;
        [JsonIgnore]
        public byte CurrentValue
        {
            get => currentValue;
            set
            {
                if (currentValue == value) return;
                currentValue = value;
                CurrentValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler CurrentValueChanged;

        //private string valueDisplayString;
        [JsonIgnore]
        public string ValueDisplayString
        {
            get
            {
                string temp = "";
                switch(displayValueAs)
                {
                    case ReportByteDisplay.Default:
                    case ReportByteDisplay.Decimal:
                        temp = $"{currentValue:D}";
                        break;
                    case ReportByteDisplay.Hexidecimal:
                        temp = $"{currentValue:X2}";
                        break;
                    case ReportByteDisplay.Bits:
                        temp = Convert.ToString(currentValue, 2).PadLeft(8, '0');
                        break;
                    default:
                        break;
                }
                return temp;
            }
        }
        public event EventHandler ValueDisplayStringChanged;

        private bool changedFrame;
        [JsonIgnore]
        public bool ChangedFrame
        {
            get => changedFrame;
            set
            {
                if (changedFrame == value) return;
                changedFrame = value;
                ChangedFrameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public event EventHandler ChangedFrameChanged;

        private const int MAX_CHANGE_MS = 500;
        private DateTime lastTimeChanged;

        public ReportByteInfo(int index)
        {
            this.index = index;
            CurrentValueChanged += ReportByteInfo_CurrentValueChanged;
            DisplayValueAsIndexChanged += ReportByteInfo_DisplayValueAsIndexChanged;
        }

        private void ReportByteInfo_DisplayValueAsIndexChanged(object sender, EventArgs e)
        {
            ValueDisplayStringChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ReportByteInfo_CurrentValueChanged(object sender, EventArgs e)
        {
            ValueDisplayStringChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateValue(byte[] report)
        {
            if (tracked)
            {
                byte temp = report[index];
                if (temp != currentValue)
                {
                    ChangedFrame = true;
                    lastTimeChanged = DateTime.Now;
                }
                else if (lastTimeChanged + TimeSpan.FromMilliseconds(MAX_CHANGE_MS) < DateTime.Now)
                {
                    ChangedFrame = false;
                }

                CurrentValue = temp;
            }
            else
            {
                ChangedFrame = false;
            }
        }

        public void ChangeToUnchanged()
        {
            lastTimeChanged = DateTime.Now;
            ChangedFrame = false;
        }
    }
}
