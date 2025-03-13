using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using POSLinkCore;
using System.IO.Ports;
using System.Threading;
using System.Xml.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace POSLinkHelperApp
{
    class Program
    {
        private static readonly string POSLinkAPIUrl = "http://poslink.com/poslink/ws/process2.asmx";
        static void Main(string[] args)
        {

            POSLinkSemiIntegration.POSLinkSemi poslink = POSLinkSemiIntegration.POSLinkSemi.GetPOSLinkSemi();
            POSLinkCore.LogSetting logSetting = new POSLinkCore.LogSetting();
            logSetting.Enabled = true;
            logSetting.Level = POSLinkCore.LogSetting.LogLevel.Debug;
            logSetting.Days = 30;
            logSetting.FilePath = ".\\";
            poslink.SetLogSetting(logSetting);


            POSLinkCore.CommunicationSetting.TcpSetting tcpSetting = new POSLinkCore.CommunicationSetting.TcpSetting();
            tcpSetting.Ip = "127.0.0.1";
            tcpSetting.Port = 10009;
            tcpSetting.Timeout = 60000;
            Console.WriteLine("Starting POSLink2 Helper Service..." +poslink);



            while (true)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("POSPipe", PipeDirection.InOut, 10))
                {
                    Console.WriteLine("Waiting for connection...");
                    pipeServer.WaitForConnection();

                    if (!pipeServer.IsConnected) return;

                    using (StreamReader reader = new StreamReader(pipeServer))
                    using (StreamWriter writer = new StreamWriter(pipeServer) { AutoFlush = true })
                    {
                        string request = reader.ReadLine();
                        UartSetting setting = new UartSetting() { SerialPortName = "COM4", BaudRate = 9600 };

                        // Use if connection is being made using TCP/IP 
                        //POSLinkCore.CommunicationSetting.TcpSetting setting1 = new POSLinkCore.CommunicationSetting.TcpSetting()
                        //{
                        //    Ip = await GetDeviceLocalIPAsync("token", "serialno"),
                        //    Port = 1009,
                        //    Timeout = 30000
                        //};

                        POSLinkSemiIntegration.Terminal terminal = poslink.GetTerminal(setting);

                        POSLinkAdmin.Util.AmountRequest amountReq = new POSLinkAdmin.Util.AmountRequest() { TransactionAmount = request, TaxAmount = "70" };

                        POSLinkSemiIntegration.Util.TraceRequest traceReq = new POSLinkSemiIntegration.Util.TraceRequest() { EcrReferenceNumber = "8" };

                        POSLinkSemiIntegration.Transaction.DoCreditRequest doCreditReq = new POSLinkSemiIntegration.Transaction.DoCreditRequest
                        {
                            TransactionType = POSLinkAdmin.Const.TransactionType.Sale,
                            AmountInformation = amountReq,
                            TraceInformation = traceReq
                        };

                        POSLinkSemiIntegration.Transaction.DoCreditResponse doCreditRsp;

                        POSLinkAdmin.ExecutionResult executionResult = terminal.Transaction.DoCredit(doCreditReq, out doCreditRsp);


                        if (executionResult.GetErrorCode() == POSLinkAdmin.ExecutionResult.Code.Ok)
                        {
                            Console.WriteLine("Transaction Approved. " + doCreditRsp.TraceInformation.GlobalUid);
                            writer.WriteLine("Approved");

                            // Here check doCreditRsp.HostInformation.HostResponseMessage for approval and decline

                        }
                        else
                        {
                            Console.WriteLine("Transaction Failed. Error: " + executionResult.ToString());
                            writer.WriteLine("Declined");
                        }

                    }
                }
            }          

        }

        public static async Task<string> GetDeviceLocalIPAsync(string token, string serialNo)
        {
            string soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                 xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                 xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
    <soap12:Body>
        <GetDeviceLocalIP xmlns=""http://poslink.com/"">
            <Token>{token}</Token>
            <SerialNo>{serialNo}</SerialNo>
        </GetDeviceLocalIP>
    </soap12:Body>
</soap12:Envelope>";


            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, POSLinkAPIUrl)
                {
                    Content = new StringContent(soapRequest, Encoding.UTF8, "application/soap+xml")
                };
                try
                {
                    HttpResponseMessage response = await client.SendAsync(request);
                    string responseXml = await response.Content.ReadAsStringAsync();
                    return ExtractIPAddress(responseXml);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                    return null;
                }
            }
        }
        private static string ExtractIPAddress(string responseXml)
        {
            try
            {
                XDocument doc = XDocument.Parse(responseXml);
                XNamespace ns = "http://poslink.com/";

                var ipElement = doc.Descendants(ns + "IPaddress").FirstOrDefault();
                return ipElement != null ? ipElement.Value : "IP not found";
            }
            catch (Exception ex)
            {
                Console.WriteLine("XML Parsing Error: " + ex.Message);
                return null;
            }
        }


    }


    public class UartSetting : POSLinkCore.CommunicationSetting.CustomerCommunicationSetting
    {
        SerialPort _serialPort = null;
        public string SerialPortName { get; set; }
        public int BaudRate { get; set; }

        public UartSetting()
        {
            SerialPortName = "";
            BaudRate = -1;
            _serialPort = new SerialPort();
        }

        public override bool IsSameCommunication(POSLinkCore.CommunicationSetting.CustomerCommunicationSetting setting)
        {
            if (setting.GetType() == typeof(UartSetting))
            {
                UartSetting tempUart = setting as UartSetting;
                if (tempUart.SerialPortName == this.SerialPortName)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsCommSettingLegal()
        {
            this.SerialPortName = this.SerialPortName.Trim();
            if (!this.SerialPortName.StartsWith("com") && !this.SerialPortName.StartsWith("COM"))
            {
                return false;
            }
            if (this.SerialPortName.Length <= 3)
            {
                return false;
            }
            this.SerialPortName = this.SerialPortName.Remove(0, 3);
            int port;
            try
            {
                port = Int32.Parse(this.SerialPortName, System.Globalization.NumberStyles.Integer);
            }
            catch (Exception)
            {
                return false;
            }

            if (port < 0)
            {
                return false;
            }
            this.SerialPortName = "com" + port;
            return true;
        }

        private void SetCommProperties()
        {
            _serialPort = new SerialPort();
            _serialPort.PortName = SerialPortName;
            _serialPort.BaudRate = BaudRate;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.ReadTimeout = 500;
        }

        public override void Open()
        {
            if (!_serialPort.IsOpen)
            {
                SetCommProperties();
                _serialPort.Open();
                _serialPort.ReadExisting();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _serialPort.Write(buffer, offset, count);
        }

        public override int Read(ref byte[] buffer, int offset, int count)
        {
            int ret;
            try
            {
                ret = _serialPort.Read(buffer, offset, count);
            }
            catch (TimeoutException)
            {
                ret = 0;
            }
            catch (Exception)
            {
                ret = -1;
            }
            return ret;
        }

        public override void Close()
        {
            _serialPort.Close();
            _serialPort.Dispose();
            Thread.Sleep(50);//Ensure that the serial port is closed.
        }
    }
}
