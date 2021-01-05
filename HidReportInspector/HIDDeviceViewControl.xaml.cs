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
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.IO;
using System.Threading;
using Microsoft.Win32;

using HidReportInspector.ViewModels;
using HidLibrary;


namespace HidReportInspector
{
    /// <summary>
    /// Interaction logic for HIDDeviceViewControl.xaml
    /// </summary>
    public partial class HIDDeviceViewControl : UserControl
    {
        private HidDeviceViewModel hidDeviceVM;
        public HidDeviceViewModel HidDeviceVM { get => hidDeviceVM; }

        public HIDDeviceViewControl()
        {
            InitializeComponent();

            Visibility = Visibility.Hidden;
            hidDeviceVM = new HidDeviceViewModel();
        }

        public void Reload(HidDevice device)
        {
            containerScrollViewer.DataContext = null;

            hidDeviceVM.CloseReader();
            hidDeviceVM.HidDevice = device;
            if (device != null)
            {
                hidDeviceVM.OpenReader();
                containerScrollViewer.DataContext = hidDeviceVM;
                Visibility = Visibility.Visible;
            }
            else
            {
                Visibility = Visibility.Hidden;
            }
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            string filename = $"{hidDeviceVM.HidDevice.Attributes.VendorHexId}-{hidDeviceVM.HidDevice.Attributes.ProductHexId}_{hidDeviceVM.HidDevice.Description}";
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".json";
            dialog.Filter = "JSON Files (*.json)|*.json";
            dialog.Title = "Select Export File";
            dialog.InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
            //dialog.FileName = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), filename);
            dialog.FileName = filename;
            if (dialog.ShowDialog() == true)
            {
                hidDeviceVM.ExportDeviceInfo(dialog.FileName);
            }
        }

        private void EditByteInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            Button senderBtn = sender as Button;
            int tag = Convert.ToInt32(senderBtn.Tag);
            ReportByteInfo byteInfo = hidDeviceVM.ReportBytesContainer.ReportBytes[tag];

            Popup tempPopup = inputReportByteItemsControl.Resources["itemPopup"]
                as Popup;
            tempPopup.PlacementTarget = senderBtn;
            tempPopup.DataContext = byteInfo;
            tempPopup.IsOpen = true;
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = ".json";
            dialog.Filter = "JSON Files (*.json)|*.json";
            dialog.Title = "Select File";
            dialog.InitialDirectory = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
            if (dialog.ShowDialog() == true)
            {
                hidDeviceVM.LoadReportBytes(dialog.FileName);
            }
        }
    }
}
