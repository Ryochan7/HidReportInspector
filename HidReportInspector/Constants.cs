using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace HidReportInspector
{
    public static class Constants
    {
        public const string APP_NAME = "HID Report Inspector";
        public const string APPDATA_FOLDER_NAME = "HID Report Inspector";
        public const string UPSTREAM_USB_IDS_URL = @"http://www.linux-usb.org/usb.ids";
        public const string JSON_USB_IDS_FILENAME = "USB_DeviceIds.json";

        public static string AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), APPDATA_FOLDER_NAME);
        public static string exelocation = Assembly.GetExecutingAssembly().Location;
        public static string exedirpath = Directory.GetParent(exelocation).FullName;
        public static string exeFileName = Path.GetFileName(exelocation);
        public static FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(exelocation);
        public static string exeversion = fileVersion.ProductVersion;
    }
}
