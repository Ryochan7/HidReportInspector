using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HidLibrary;

namespace HidReportInspector.ViewModels
{
    public class MainWindowViewModel
    {
        private List<HidDevice> currentHidDevices;
        public List<HidDevice> CurrentHidDevices { get => currentHidDevices; }

        private List<TreeViewVIDGroup> vidDeviceGroup;
        public List<TreeViewVIDGroup> VidDeviceGroup { get => vidDeviceGroup; }

        private UsbVendorIdsRepo vendorIdsRepo;

        public MainWindowViewModel(UsbVendorIdsRepo usbIdsRepo)
        {
            vendorIdsRepo = usbIdsRepo;

            vidDeviceGroup = new List<TreeViewVIDGroup>();

            IEnumerable<HidDevice> hidDevices = HidDevices.Enumerate();
            currentHidDevices = hidDevices.ToList();

            BuildTreeData();
        }

        public void RefreshTreeData()
        {
            vidDeviceGroup.Clear();

            IEnumerable<HidDevice> hidDevices = HidDevices.Enumerate();
            currentHidDevices = hidDevices.ToList();

            BuildTreeData();
        }

        private void BuildTreeData()
        {
            // Run initial scan and find all HID devices for each VendorId
            Dictionary<int, List<HidDevice>> vidDictionary = new Dictionary<int, List<HidDevice>>();
            foreach (HidDevice device in currentHidDevices)
            {

                /*device.OpenDevice(false);
                // If device cannot be opened, skip it
                if (!device.IsOpen)
                {
                    continue;
                }
                */

                if (!vidDictionary.ContainsKey(device.Attributes.VendorId))
                {
                    vidDictionary.Add(device.Attributes.VendorId, new List<HidDevice>());
                }

                List<HidDevice> currentDevList = vidDictionary[device.Attributes.VendorId];
                currentDevList.Add(device);
            }

            // Further break down list into matches per VID
            foreach (KeyValuePair<int, List<HidDevice>> pair in vidDictionary)
            {
                string vendorName = "Unknown";
                if (vendorIdsRepo.GetUsbVendorInfo(pair.Key, out UsbVendorInfo tempVendorValue))
                {
                    vendorName = tempVendorValue.VendorName;
                }

                TreeViewVIDGroup vIDGroup = new TreeViewVIDGroup($"({pair.Key:X4}) {vendorName}");
                List <HidDevice> currentList = pair.Value;
                Dictionary<int, List<HidDevice>> pidDictionary = new Dictionary<int, List<HidDevice>>();

                // Grab HID device references and group them by PID
                foreach (HidDevice device in currentList)
                {
                    if (!pidDictionary.ContainsKey(device.Attributes.ProductId))
                    {
                        pidDictionary.Add(device.Attributes.ProductId, new List<HidDevice>());
                    }

                    List<HidDevice> pidDeviceList = pidDictionary[device.Attributes.ProductId];
                    pidDeviceList.Add(device);
                }

                // Start building data structure for TreeView
                foreach (KeyValuePair<int, List<HidDevice>> pidPair in pidDictionary)
                {
                    List<TreeViewHidDeviceInfo> pidHidDeviceInfo = new List<TreeViewHidDeviceInfo>();
                    List<HidDevice> pidDeviceList = pidPair.Value;
                    foreach (HidDevice device in pidDeviceList)
                    {
                        TreeViewHidDeviceInfo tempInfo = new TreeViewHidDeviceInfo(device, device.Description);
                        pidHidDeviceInfo.Add(tempInfo);
                    }

                    string productName = "Unknown";
                    if (tempVendorValue != null &&
                        tempVendorValue.GetUsbProductInfo(pidPair.Key, out UsbPidInfo tempPidValue))
                    {
                        productName = tempPidValue.ProductName;
                    }

                    TreeViewPIDGroup pIDGroup = new TreeViewPIDGroup(pidHidDeviceInfo, $"({pidPair.Key:X4}) {productName}");
                    vIDGroup.PidGroupList.Add(pIDGroup);
                }

                // Add sub-tree data to root
                vidDeviceGroup.Add(vIDGroup);
            }
        }
    }

    public class TreeViewVIDGroup
    {
        private string displayName;
        public string DisplayName { get => displayName; }

        private List<TreeViewPIDGroup> pidGroupList;
        public List<TreeViewPIDGroup> PidGroupList
        {
            get => pidGroupList;
            set => PidGroupList = value;
        }

        public TreeViewVIDGroup(string name)
        {
            displayName = name;
            pidGroupList = new List<TreeViewPIDGroup>();
        }
    }

    public class TreeViewPIDGroup
    {
        private string displayName;
        public string DisplayName { get => displayName; }

        private List<TreeViewHidDeviceInfo> hidDevicesInfo;
        public List<TreeViewHidDeviceInfo> HidDevices { get => hidDevicesInfo; }

        public TreeViewPIDGroup(List<TreeViewHidDeviceInfo> devicesInfo,
            string name)
        {
            hidDevicesInfo = devicesInfo;
            displayName = name;
        }
    }

    public class TreeViewHidDeviceInfo
    {
        private string displayName;
        public string DisplayName { get => displayName; }

        private HidDevice currentDevice;
        public HidDevice CurrentDevice { get => currentDevice; }

        public TreeViewHidDeviceInfo(HidDevice device, string name)
        {
            currentDevice = device;
            displayName = name;
        }
    }
}
