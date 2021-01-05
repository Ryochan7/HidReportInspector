using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Text.Json.Serialization;

namespace HidReportInspector
{
    public class UsbVendorIdsRepo
    {
        //private string jsonFile;
        private string rawJson;
        private List<UsbVendorInfo> vendorInfos = new List<UsbVendorInfo>();
        // VID, UsbVendorInfo inst
        private Dictionary<int, UsbVendorInfo> vidDictionary =
            new Dictionary<int, UsbVendorInfo>();

        public UsbVendorIdsRepo()
        {
        }

        public UsbVendorIdsRepo(string jsonFile)
        {
            ReadFromFile(jsonFile);
        }

        public void ReadFromFile(string jsonFile)
        {
            using (StreamReader stream = new StreamReader(jsonFile))
            {
                rawJson = stream.ReadToEnd();
            }

            ConvertJson();
        }

        private void ConvertJson()
        {
            if (!string.IsNullOrEmpty(rawJson))
            {
                vendorInfos =
                    JsonSerializer.Deserialize<List<UsbVendorInfo>>(rawJson);
            }

            foreach(UsbVendorInfo vendorInfo in vendorInfos)
            {
                vidDictionary.Add(vendorInfo.VendorId, vendorInfo);
                vendorInfo.PopulatePidDict();
            }
        }

        public bool GetUsbVendorInfo(int vid, out UsbVendorInfo value)
        {
            bool result = vidDictionary.TryGetValue(vid, out value);
            return result;
        }

        public void ClearData()
        {
            vendorInfos.Clear();
            vidDictionary.Clear();
            rawJson = "";
        }
    }

    public class UsbVendorInfo
    {
        private int vendorId;
        [JsonPropertyName("VID")]
        public int VendorId { get => vendorId; set => vendorId = value; }
        
        private string vendorName;
        public string VendorName { get => vendorName; set => vendorName = value; }

        private List<UsbPidInfo> devices = new List<UsbPidInfo>();
        public List<UsbPidInfo> Devices { get => devices; set => devices = value; }

        private Dictionary<int, UsbPidInfo> pidDeviceDict =
            new Dictionary<int, UsbPidInfo>();

        public void PopulatePidDict()
        {
            pidDeviceDict.Clear();
            foreach(UsbPidInfo device in devices)
            {
                pidDeviceDict.Add(device.ProductId, device);
            }
        }

        public bool GetUsbProductInfo(int vid, out UsbPidInfo value)
        {
            bool result = pidDeviceDict.TryGetValue(vid, out value);
            return result;
        }
    }

    public class UsbPidInfo
    {
        private int productId;
        [JsonPropertyName("PID")]
        public int ProductId { get => productId; set => productId = value; }

        private string productName;
        public string ProductName
        {
            get => productName;
            set => productName = value;
        }
    }
}
