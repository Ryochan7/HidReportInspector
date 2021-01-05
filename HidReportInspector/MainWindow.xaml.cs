using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HidReportInspector.ViewModels;
using HidLibrary;
using System.Diagnostics;
using System.Net.Http;
using System.IO;

namespace HidReportInspector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel mainWinVM;

        public MainWindow()
        {
            InitializeComponent();

            App current = App.Current as App;

            mainWinVM = new MainWindowViewModel(current.IdsRepo);
            hidDeviceTreeView.DataContext = mainWinVM;

            // Invoke method after window has appeared
            Dispatcher.BeginInvoke((Action)(() =>
            {
                CheckUSBIdsFile();
            }));
        }

        private void SetupEvents()
        {
        }

        private void CheckUSBIdsFile()
        {
            string outputIdsFilePath = System.IO.Path.Combine(Constants.AppDataFolder,
                Constants.JSON_USB_IDS_FILENAME);

            if (!File.Exists(outputIdsFilePath))
            {
                MessageBoxResult result = MessageBox.Show("App will attempt to download USB vendor data from linux-usb.org and convert data for use. This task may take some time. Proceed?", "", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    DownloadAndParseUpstreamUSBIds();
                }
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //deviceViewControl.Reload(null);
            //hidDeviceTreeView.DataContext = null;
            ClearWinDisplay();

            Close();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new AboutWindow();
            aboutWindow.Owner = this;
            aboutWindow.Show();
        }

        private void HidDeviceTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            object item = hidDeviceTreeView.SelectedItem;
            if (item != null)
            {
                Console.WriteLine(item);
                if (item.GetType() == typeof(TreeViewHidDeviceInfo))
                {
                    TreeViewHidDeviceInfo tempInfo = item as TreeViewHidDeviceInfo;
                    deviceViewControl.Reload(tempInfo.CurrentDevice);
                }
            }
        }

        private void RescanHardwareMenuItem_Click(object sender, RoutedEventArgs e)
        {
            /*deviceViewControl.Reload(null);

            hidDeviceTreeView.DataContext = null;
            mainWinVM.RefreshTreeData();
            hidDeviceTreeView.DataContext = mainWinVM;
            */
            ClearWinDisplay();
            RefreshWinDisplay();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            deviceViewControl.Reload(null);
            //hidDeviceTreeView.DataContext = null;

            e.Cancel = false;
        }

        private async void DownloadAndParseUpstreamUSBIds()
        {
            App current = App.Current as App;
            HttpResponseMessage response = await current.RequestClient.GetAsync(Constants.UPSTREAM_USB_IDS_URL);
            if (response.IsSuccessStatusCode)
            {
                string tempFilePath = System.IO.Path.GetTempFileName();
                using (StreamWriter sw = new StreamWriter(tempFilePath))
                {
                    string content = await response.Content.ReadAsStringAsync();
                    sw.Write(content);
                    sw.Flush();
                }
                
                current.RefreshUSBIdsRepo(tempFilePath);
                File.Delete(tempFilePath);
            }

            //deviceViewControl.Reload(null);

            //hidDeviceTreeView.DataContext = null;
            //mainWinVM.RefreshTreeData();
            //hidDeviceTreeView.DataContext = mainWinVM;

            ClearWinDisplay();
            RefreshWinDisplay();
        }

        private void UpdateUsbIdsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("App will attempt to download USB vendor data from linux-usb.org and convert data for use. This task may take some time. Proceed?", "", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                ClearWinDisplay();
                DownloadAndParseUpstreamUSBIds();
            }
        }

        private void ClearWinDisplay()
        {
            deviceViewControl.Reload(null);
            hidDeviceTreeView.DataContext = null;
        }

        private void RefreshWinDisplay()
        {
            mainWinVM.RefreshTreeData();
            hidDeviceTreeView.DataContext = mainWinVM;
        }
    }
}
