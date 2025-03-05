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

namespace POSLinkHelperApp
{
    class Program
    {
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
            Console.WriteLine(logSetting.FilePath);
            Console.WriteLine(logSetting.FileName);

            //string[] ports = SerialPort.GetPortNames();
            //Console.WriteLine("Available COM Ports: " + string.Join(", ", ports));

            //UartSetting setting = new UartSetting() { SerialPortName = "COM3", BaudRate = 9600 };

            //POSLinkSemiIntegration.Terminal terminal = poslink.GetTerminal(setting);

            //POSLinkAdmin.Util.AmountRequest amountReq = new POSLinkAdmin.Util.AmountRequest() { TransactionAmount = "56780", TaxAmount = "70" };

            //POSLinkSemiIntegration.Util.TraceRequest traceReq = new POSLinkSemiIntegration.Util.TraceRequest() { EcrReferenceNumber = "8" };

            //POSLinkSemiIntegration.Transaction.DoCreditRequest doCreditReq = new POSLinkSemiIntegration.Transaction.DoCreditRequest
            //{
            //    TransactionType = POSLinkAdmin.Const.TransactionType.Sale,
            //    AmountInformation = amountReq,
            //    TraceInformation = traceReq
            //};

            //POSLinkSemiIntegration.Transaction.DoCreditResponse doCreditRsp;

            //POSLinkAdmin.ExecutionResult executionResult = terminal.Transaction.DoCredit(doCreditReq, out doCreditRsp);

            //if (executionResult.GetErrorCode() == POSLinkAdmin.ExecutionResult.Code.Ok)
            //{
            //    Console.WriteLine("Transaction Approved. Response Code: " + doCreditRsp.ResponseCode);
            //}
            //else
            //{
            //    Console.WriteLine("Transaction Failed. Error: " + executionResult.ToString());
            //}
            while (true)
            {
                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("POSPipe", PipeDirection.InOut))
                {
                    Console.WriteLine("Waiting for connection...");
                    pipeServer.WaitForConnection();

                    using (StreamReader reader = new StreamReader(pipeServer))
                    using (StreamWriter writer = new StreamWriter(pipeServer) { AutoFlush = true })
                    {
                        string request = reader.ReadLine();
                        UartSetting setting = new UartSetting() { SerialPortName = "COM4", BaudRate = 9600 };                       

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
                            Console.WriteLine("Transaction Approved. Response Code: " + doCreditRsp.ResponseCode);
                        }
                        else
                        {
                            Console.WriteLine("Transaction Failed. Error: " + executionResult.ToString());
                        }
                        Console.WriteLine("Received: " + request);

                        // Process commands
                        string response = HandleCommand(request);
                        writer.WriteLine(response);

                    }
                }
            }

           

        }

        static string HandleCommand(string command)
        {
            if (command.StartsWith("PAY:"))
            {
                string amountStr = command.Split(':')[1];
                decimal amount = decimal.Parse(amountStr);

                // Integrate POSLink2 SDK payment processing
                return String.Format("Payment of {0:C} processed successfully!", amount);
            }

            return "Unknown command.";
        }

        void MakeTransaction()
        {
        
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




//    class Program
//    {
//        static void Main(string[] args)
//        {
//            POSLinkSerialHandler serialHandler = new POSLinkSerialHandler();

//            Console.WriteLine("Starting POSLink Helper Service...");

//            while (true)
//            {
//                using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("POSPipe", PipeDirection.InOut))
//                {
//                    Console.WriteLine("Waiting for connection...");
//                    pipeServer.WaitForConnection();

//                    using (StreamReader reader = new StreamReader(pipeServer))
//                    using (StreamWriter writer = new StreamWriter(pipeServer) { AutoFlush = true })
//                    {
//                        string request = reader.ReadLine();
//                        Console.WriteLine("Received: " + request);

//                        string response = HandleCommand(request, serialHandler);
//                        writer.WriteLine(response);
//                    }
//                }
//            }
//        }

//        static string HandleCommand(string command, POSLinkSerialHandler serialHandler)
//        {
//            if (command.StartsWith("PAY:"))
//            {
//                string amountStr = command.Split(':')[1].Trim();
//                decimal amount = decimal.Parse(amountStr);

//                // Process payment using POSLink SDK
//                POSLinkResponse response = serialHandler.ProcessSaleTransaction(amount);

//                if (response.ResultCode == POSLinkResponse.RESP_SUCCESS)
//                {
//                    return $"SUCCESS|ApprovalCode:{response.ApprovalCode}|TransactionID:{response.TransactionID}";
//                }
//                else
//                {
//                    return $"FAIL|ErrorCode:{response.ResultCode}|Message:{response.ResultTxt}";
//                }
//            }

//            return "Unknown command.";
//        }
//    }

//    public class POSLinkSerialHandler
//{
//    private POSLinkCore.CommunicationSetting.TcpSetting posLink;

//    public POSLinkSerialHandler()
//    {
//        posLink = new POSLinkCore.CommunicationSetting.TcpSetting
//        {
//            CommSetting = new CustomSerialCommSetting("COM3", 19200); // Replace with your port and baud rate
//        };
//    }

//    public POSLinkResponse ProcessSaleTransaction(decimal amount)
//    {
//        SaleTransaction saleTransaction = new SaleTransaction
//        {
//            Amount = amount.ToString("F2"),
//            TransactionType = TransactionType.SALE
//        };

//        posLink.TransactionRequest = saleTransaction;

//        POSLinkResponse response = posLink.ProcessTrans();
//        return response;
//    }

//    public void HandleResponse(POSLinkResponse response)
//    {
//        if (response.ResultCode == POSLinkResponse.RESP_SUCCESS)
//        {
//            Console.WriteLine("Transaction Approved");
//            Console.WriteLine("Approval Code: " + response.ApprovalCode);
//            Console.WriteLine("Transaction ID: " + response.TransactionID);
//        }
//        else
//        {
//            Console.WriteLine("Transaction Failed");
//            Console.WriteLine("Error Code: " + response.ResultCode);
//            Console.WriteLine("Message: " + response.ResultTxt);
//        }
//    }
//}

   


}
