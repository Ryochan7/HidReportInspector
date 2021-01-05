using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;

namespace HidReportInspector
{
    public class UsbIdsParser
    {
        private enum LineParseStatus : uint
        {
            None,
            InVendor,
            InProduct,
        }

        private static Regex vendorLineRegex = new Regex(@"^(?<vid>[a-fA-F0-9]{4})  (?<vendor>.+)$", RegexOptions.Compiled);
        private static Regex productLineRegex = new Regex(@"^\t(?<pid>[a-fA-F0-9]{4})  (?<product>.+)$", RegexOptions.Compiled);

        private LineParseStatus lineStatus;
        private List<UsbVendorInfo> vendorInfos = new List<UsbVendorInfo>();
        public List<UsbVendorInfo> VendorInfos { get => vendorInfos; }

        private UsbVendorInfo currentVendorInfo;

        public UsbIdsParser(string fileName)
        {
            using (StreamReader stream = new StreamReader(fileName))
            {
                while (!stream.EndOfStream)
                {
                    string temp = stream.ReadLine().TrimEnd();
                    ParseLine(temp);
                }
            }
        }

        private void ParseLine(string line)
        {
            bool ignoreline = string.IsNullOrWhiteSpace(line);
            ignoreline = ignoreline || line.StartsWith("#");
            if (!ignoreline)
            {
                switch(lineStatus)
                {
                    case LineParseStatus.None:
                        if (vendorLineRegex.IsMatch(line))
                        {
                            lineStatus = LineParseStatus.InVendor;
                            Match match = vendorLineRegex.Match(line);
                            int.TryParse(match.Groups["vid"].Value, System.Globalization.NumberStyles.HexNumber, null, out int vendorId);
                            string vendor = match.Groups["vendor"].Value;

                            currentVendorInfo = new UsbVendorInfo()
                            {
                                VendorId = vendorId,
                                VendorName = vendor,
                            };

                            vendorInfos.Add(currentVendorInfo);
                        }
                        break;
                    case LineParseStatus.InVendor:
                    case LineParseStatus.InProduct:
                        if (productLineRegex.IsMatch(line))
                        {
                            lineStatus = LineParseStatus.InProduct;
                            Match match = productLineRegex.Match(line);
                            int.TryParse(match.Groups["pid"].Value, System.Globalization.NumberStyles.HexNumber, null, out int productId);
                            string productName = match.Groups["product"].Value;

                            if (currentVendorInfo != null)
                            {
                                UsbPidInfo pidInfo = new UsbPidInfo()
                                {
                                    ProductId = productId,
                                    ProductName = productName,
                                };
                                currentVendorInfo.Devices.Add(pidInfo);
                            }
                        }
                        else
                        {
                            lineStatus = LineParseStatus.None;
                            goto case LineParseStatus.None;
                        }
                        break;
                    default:
                        lineStatus = LineParseStatus.None;
                        break;
                }
            }
        }

        public void Serialize(string outputPath)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };
            string rawJson = JsonSerializer.Serialize(vendorInfos, options);

            using (StreamWriter sw = new StreamWriter(outputPath))
            {
                sw.Write(rawJson);
                sw.Flush();
            }
        }
    }
}
