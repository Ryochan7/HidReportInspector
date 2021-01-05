using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Net;
using System.Net.Http;
using System.IO;
using HidLibrary;

namespace HidReportInspector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private UsbVendorIdsRepo idsRepo;
        public UsbVendorIdsRepo IdsRepo { get => idsRepo; set => idsRepo = value; }

        private HttpClient requestClient;
        public HttpClient RequestClient { get => requestClient; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Task.Run(() => CreateBackgroundObjects()).Wait();

            string configDirPath = Constants.AppDataFolder;
            if (!Directory.Exists(configDirPath))
            {
                Directory.CreateDirectory(configDirPath);
            }

            string outputIdsFilePath = Path.Combine(configDirPath, Constants.JSON_USB_IDS_FILENAME);
            if (File.Exists(outputIdsFilePath))
            {
                // USB Ids file exists. Attempt to read from it
                idsRepo = new UsbVendorIdsRepo(outputIdsFilePath);
            }
            else
            {
                // Create empty instance
                IdsRepo = new UsbVendorIdsRepo();
            }
        }

        private void CreateBackgroundObjects()
        {
            requestClient = new HttpClient();
        }

        public void RefreshUSBIdsRepo(string tempFilePath)
        {
            UsbIdsParser idsParser = new UsbIdsParser(tempFilePath);
            string configDirPath = Constants.AppDataFolder;
            string outputIdsFilePath = Path.Combine(configDirPath, Constants.JSON_USB_IDS_FILENAME);
            idsParser.Serialize(outputIdsFilePath);

            // Clear any existing data and parse new data
            idsRepo.ClearData();
            idsRepo.ReadFromFile(outputIdsFilePath);
        }
    }
}
