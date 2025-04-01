using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Xml;

namespace POSLinkHelperApp
{
    public class DeviceNetworkHelper
    {

        private static readonly string POSLinkAPIUrl = "http://poslink.com/poslink/ws/process2.asmx";

        public static async Task<string> GetDeviceLocalIPAsync(string serialNo, string terminalId)
        {
            string soapRequest = "<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                + "<soap12:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" "
                + "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" "
                + "xmlns:soap12=\"http://www.w3.org/2003/05/soap-envelope\">"
                + "<soap12:Body>"
                + "<GetDeviceLocalIP xmlns=\"http://poslink.com/\">"
                + "<TerminalId>" + terminalId + "</TerminalId>"
                + "<SerialNo>" + serialNo + "</SerialNo>"
                + "</GetDeviceLocalIP>"
                + "</soap12:Body>"
                + "</soap12:Envelope>";


            using (HttpClient client = new HttpClient())
            {
                System.Net.Http.HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, POSLinkAPIUrl)
                {
                    Content = new StringContent(soapRequest, Encoding.UTF8, "text/xml")
                };

                // Add SOAPAction Header (Required for SOAP 1.1)
                request.Headers.Add("SOAPAction", "http://poslink.com/GetDeviceLocalIP");

                try
                {
                    HttpResponseMessage response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode(); // Throws exception if status code is not 2xx

                    string responseXml = await response.Content.ReadAsStringAsync();
                    return ExtractIPAddress(responseXml);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return "IP not found";
                }
            }          

        }

        private static string ExtractIPAddress(string responseXml)
        {
            if (string.IsNullOrWhiteSpace(responseXml))
                return "IP not found";

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(responseXml);
                XmlNodeList ipNodes = xmlDoc.GetElementsByTagName("IPaddress");

                if (ipNodes.Count > 0)
                    return ipNodes[0].InnerText;

                return "IP not found";
            }
            catch (Exception ex)
            {
                Console.WriteLine("XML Parsing Error: " + ex.Message);
                return "IP not found";
            }
        }    
    }
}
