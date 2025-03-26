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
using POSLinkAdmin;
using System.Net.Sockets;
using Renci.SshNet;
using POSLinkSemiIntegration;
using System.Xml;
using Newtonsoft.Json;
using POSLinkAdmin.Device;
using POSLinkAdmin.Form;
using POSLinkAdmin.Util;
using POSLinkCore.CommunicationSetting;


namespace POSLinkHelperApp
{
    class Program
    {
        private static readonly string POSLinkAPIUrl = "http://poslink.com/poslink/ws/process2.asmx";

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }
        static async Task MainAsync()
        {

            POSLinkSemiIntegration.POSLinkSemi poslink = POSLinkSemiIntegration.POSLinkSemi.GetPOSLinkSemi();
            POSLinkCore.LogSetting logSetting = new POSLinkCore.LogSetting();
            logSetting.Enabled = true;
            logSetting.Level = POSLinkCore.LogSetting.LogLevel.Debug;
            logSetting.Days = 30;
            logSetting.FilePath = ".\\";
            poslink.SetLogSetting(logSetting);
           

            // For UART 
            //UartSetting setting = new UartSetting() { SerialPortName = "COM4", BaudRate = 9600 };


            // For TCP/IP
            //POSLinkCore.CommunicationSetting.TcpSetting setting1 = new POSLinkCore.CommunicationSetting.TcpSetting()
            //{
            //    Ip = await GetDeviceLocalIPAsync("1851761554", "E3NX2QRE"),
            //    Port = 10009,
            //    Timeout = 30000
            //};

            //POSLinkSemiIntegration.Terminal terminal = poslink.GetTerminal(setting1);

            //POSLinkAdmin.Util.AmountRequest amountReq = new POSLinkAdmin.Util.AmountRequest() { TransactionAmount = "300", TaxAmount = "70" };

            //POSLinkSemiIntegration.Util.TraceRequest traceReq = new POSLinkSemiIntegration.Util.TraceRequest() { EcrReferenceNumber = "8" };

            //POSLinkSemiIntegration.Transaction.DoCreditRequest doCreditReq = new POSLinkSemiIntegration.Transaction.DoCreditRequest
            //{
            //    TransactionType = POSLinkAdmin.Const.TransactionType.Sale,
            //    AmountInformation = amountReq,
            //    TraceInformation = traceReq
            //};

            //POSLinkSemiIntegration.Transaction.DoCreditResponse doCreditRsp;
            //try {
            //    POSLinkAdmin.ExecutionResult executionResult = terminal.Transaction.DoCredit(doCreditReq, out doCreditRsp);         
            //}
            //catch (Exception ex)
            //{

            //    TransactionLog errorResponse = new TransactionLog()
            //    {
            //        DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            //        Success = false,
            //        Message = ex.Message,
            //    };

            //    string jsonErrorResponse = JsonConvert.SerializeObject(errorResponse, Newtonsoft.Json.Formatting.Indented);
            //}


            //if (executionResult.GetErrorCode() == POSLinkAdmin.ExecutionResult.Code.Ok)
            //{
            //    Console.WriteLine("Transaction Approved. " + doCreditRsp.TraceInformation.GlobalUid);
            //    //writer.WriteLine("Approved");

            //    // Here check doCreditRsp.HostInformation.HostResponseMessage for approval and decline

            //}
            //else
            //{
            //    Console.WriteLine("Transaction Failed. Error: " + executionResult.GetErrorCode());
            //    //writer.WriteLine("Declined");
            //}


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
                            string amount = reader.ReadLine();
                            string orderID = reader.ReadLine();

                        try             
                        
                            {
                            //UartSetting setting = new UartSetting() { SerialPortName = "COM4", BaudRate = 9600 };

                            // Use if connection is being made using TCP/IP 
                                POSLinkCore.CommunicationSetting.TcpSetting setting1 = new POSLinkCore.CommunicationSetting.TcpSetting()
                                {
                                    Ip = await GetDeviceLocalIPAsync("1851761554", "E3NX2QRE"),
                                    Port = 10009,
                                    Timeout = 30000
                                };

                                POSLinkSemiIntegration.Terminal terminal = poslink.GetTerminal(setting1);
                                

                                POSLinkAdmin.Util.AmountRequest amountReq = new POSLinkAdmin.Util.AmountRequest() { TransactionAmount = amount, TaxAmount = "70" };

                                POSLinkSemiIntegration.Util.TraceRequest traceReq = new POSLinkSemiIntegration.Util.TraceRequest() { EcrReferenceNumber = "8" };

                                POSLinkSemiIntegration.Transaction.DoCreditRequest doCreditReq = new POSLinkSemiIntegration.Transaction.DoCreditRequest
                                {
                                    TransactionType = POSLinkAdmin.Const.TransactionType.Sale,
                                    AmountInformation = amountReq,
                                    TraceInformation = traceReq
                                };

                                POSLinkSemiIntegration.Transaction.DoCreditResponse doCreditRsp;

                                POSLinkAdmin.ExecutionResult executionResult = terminal.Transaction.DoCredit(doCreditReq, out doCreditRsp);


                                TransactionLog transactionLog = new TransactionLog()
                                {
                                    DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    Amount = amount,
                                    OrderID = orderID,
                                    TransactionType = (doCreditRsp != null && doCreditRsp.EdcType != null) ? doCreditRsp.EdcType : "Unknown",
                                    TransactionID = (doCreditRsp != null && doCreditRsp.TraceInformation != null) ? doCreditRsp.TraceInformation.GlobalUid : null,
                                    Success = (executionResult.GetErrorCode() == ExecutionResult.Code.Ok),
                                    Code = (doCreditRsp != null && doCreditRsp.HostInformation != null) ? doCreditRsp.HostInformation.HostResponseCode : "N/A",
                                    Message = (doCreditRsp != null && doCreditRsp.HostInformation != null) ? doCreditRsp.HostInformation.HostResponseMessage : "N/A"
                                };

                                string jsonResponse = JsonConvert.SerializeObject(transactionLog, Newtonsoft.Json.Formatting.Indented);

                             //Send the entire JSON object to the main application
                             writer.WriteLine(jsonResponse);
                            //LogTransaction(transactionLog);

                            }

                        catch (Exception ex)
                        {

                            TransactionLog errorResponse = new TransactionLog()
                            {
                                DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                Success = false,
                                Message = ex.Message,
                            };

                            string jsonErrorResponse = JsonConvert.SerializeObject(errorResponse, Newtonsoft.Json.Formatting.Indented);
                            writer.WriteLine(jsonErrorResponse);
                            LogTransaction(errorResponse);
                        }

                        }
                    }
                
               
            }
        }



        public static async Task<string> GetDeviceLocalIPAsync(string serialNo,string terminalId)
        {
            //string soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            //<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
            //                 xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
            //                 xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
            //    <soap12:Body>
            //        <GetDeviceLocalIP xmlns=""http://poslink.com/"">
            //          <TerminalId>{terminalId}</TerminalId>
            //            <SerialNo>{serialNo}</SerialNo>
            //        </GetDeviceLocalIP>
            //    </soap12:Body>
            //</soap12:Envelope>";
            string soapRequest = string.Format(@"<?xml version=""1.0"" encoding=""utf-8""?>
        <soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
                         xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" 
                         xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
            <soap12:Body>
                <GetDeviceLocalIP xmlns=""http://poslink.com/"">
                  <TerminalId>{0}</TerminalId>
                  <SerialNo>{1}</SerialNo>
                </GetDeviceLocalIP>
            </soap12:Body>
        </soap12:Envelope>", terminalId, serialNo);


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


            /////////////////////////////// SOAP 1.1
            //    string soapRequest = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            //<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
            //    <soapenv:Body>
            //        <GetDeviceLocalIP xmlns=""http://poslink.com/"">
            //            <TerminalId>{terminalId}</TerminalId>
            //            <SerialNo>{serialNo}</SerialNo>
            //        </GetDeviceLocalIP>
            //    </soapenv:Body>
            //</soapenv:Envelope>";

            //            string soapR = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
            //<soap:Envelope xmlns:xsd=""http://www.w3.org/2001/XMLSchema""
            //               xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
            //               xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
            //    <soap:Body>
            //        <GetDeviceLocalIP xmlns=""http://poslink.com/"">
            //            <TerminalId>{terminalId}</TerminalId>
            //            <SerialNo>{serialNo}</SerialNo>
            //        </GetDeviceLocalIP>
            //    </soap:Body>
            //</soap:Envelope>";

            //< Token >{ token}</ Token >

            //using (HttpClient client = new HttpClient())
            //{
            //    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, POSLinkAPIUrl)
            //    {
            //        Content = new StringContent(soapRequest, Encoding.UTF8, "text/xml")
            //    };

            //    // Add SOAPAction Header (Required for SOAP 1.1)
            //    request.Headers.Add("SOAPAction", "http://poslink.com/GetDeviceLocalIP");

            //    try
            //    {
            //        HttpResponseMessage response = await client.SendAsync(request);
            //        string responseXml = await response.Content.ReadAsStringAsync();
            //        return ExtractIPAddress(responseXml);
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("Error: " + ex.Message);
            //        return null;
            //    }
            //}


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

        public static void LogTransaction(TransactionLog transactionLog)
        {
            try
            {
                string logPath = @"D:\Demo_POSLink_Integration\Demo_POSLink_Integrartion\TransactionLog.txt";
                // Ensure the header is written only once
                if (!File.Exists(logPath) || new FileInfo(logPath).Length == 0)
                {
                    using (StreamWriter writer = new StreamWriter(logPath, true))
                    {
                        writer.WriteLine("Time                 || Transaction ID || Order ID  || Amount  || Transaction Type || Success || Code  || Message               || ErrorCode");
                        writer.WriteLine("---------------------------------------------------------------------------------------------------------------");
                    }
                }

                // Format data in a structured manner (ensuring fixed column widths)
                //string logEntry = $"{transactionLog.DateTime,-20} || {transactionLog.TransactionID,-14} || {transactionLog.OrderID,-8} || {transactionLog.Amount,-7} || {transactionLog.TransactionType,-16} || {transactionLog.Success,-7} || {transactionLog.Code,-5} || {transactionLog.Message,-20}";
                string logEntry = string.Format("{0,-20} || {1,-14} || {2,-8} || {3,-7} || {4,-16} || {5,-7} || {6,-5} || {7,-20}",
               transactionLog.DateTime, transactionLog.TransactionID, transactionLog.OrderID, transactionLog.Amount,
               transactionLog.TransactionType, transactionLog.Success, transactionLog.Code, transactionLog.Message);

                // Append log entry to file
                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine(logEntry);
                }

                Console.WriteLine("Transaction logged successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Logging failed: " + ex.Message);
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


    public class TransactionLog
    {
        public string DateTime { get; set; }
        public string TransactionID { get; set; }
        public string OrderID { get; set; }
        public string Amount { get; set; }
        public string TransactionType { get; set; }
        public bool Success { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
       
    }
}
