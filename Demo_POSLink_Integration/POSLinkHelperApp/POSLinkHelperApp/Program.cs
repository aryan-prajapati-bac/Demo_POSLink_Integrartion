using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using POSLinkSemiIntegration;
using POSLinkCore;
using POSLinkAdmin;
using POSLinkSemiIntegration.Util;
using POSLinkSemiIntegration.Transaction;
using POSLinkCore.CommunicationSetting;
using POSLinkAdmin.Util;
using System.Net.Http;
using Microsoft.Win32;

namespace POSLinkHelperApp
{
    class Program
    {

        public static void Main()
        {
            MainAsync().Wait();
        }

        public static async Task MainAsync()
        {
            // Get poslink object to communicate with POSLink library
            POSLinkSemi poslink = POSLinkSemi.GetPOSLinkSemi();

            // System Logs
            ConfigureLogging(poslink);


            while (true)
            {
                try
                {
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("POSPipe", PipeDirection.InOut, 10))
                    {
                        Console.WriteLine("Waiting for connection...");
                        pipeServer.WaitForConnection();

                        if (!pipeServer.IsConnected) continue;

                        using (StreamReader reader = new StreamReader(pipeServer))
                        using (StreamWriter writer = new StreamWriter(pipeServer) { AutoFlush = true })
                        {
                            string amount = reader.ReadLine();
                            string orderID = reader.ReadLine();                           

                            try
                            {
                                if (string.IsNullOrWhiteSpace(amount) || string.IsNullOrWhiteSpace(orderID))
                                {
                                    writer.WriteLine(JsonConvert.SerializeObject(new { Success = false, Message = "Invalid input received" }));
                                    continue;
                                }

                                TransactionLog transactionLog = await ProcessTransaction(poslink, amount, orderID);
                                writer.WriteLine(JsonConvert.SerializeObject(transactionLog, Newtonsoft.Json.Formatting.Indented));
                                TransactionLog.LogTransaction(transactionLog);
                            }

                            catch (Exception ex)
                            {
                                writer.WriteLine(JsonConvert.SerializeObject(new { Success = false, Message = ex.Message }));
                            }
                        }
                    }
                }
                catch (Exception ex) 
                {
                    //Console.WriteLine("Unexpected Error: " + ex.Message);
                }
            }
        }

        private static void ConfigureLogging(POSLinkSemi poslink)
        {
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..","..", "Logs", "System Logs");
            string fullPath = Path.GetFullPath(logDirectory); // Resolves relative path to absolute

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }           

            LogSetting logSetting = new LogSetting
            {
                Enabled = true,
                Level = LogSetting.LogLevel.Debug,
                Days = 30,
                FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Logs", "System Logs")
            };
            poslink.SetLogSetting(logSetting);
        }
   
        private static async Task<TransactionLog> ProcessTransaction(POSLinkSemi poslink, string amount, string orderID)
        {            

            try
            {
                TcpSetting setting = new TcpSetting
                {
                    Ip = await DeviceNetworkHelper.GetDeviceLocalIPAsync(RegistryHelper.GetRegistryValue(Registry.CurrentUser, @"Software\wpos\temp", "SN"), RegistryHelper.GetRegistryValue(Registry.CurrentUser, @"Software\wpos\temp", "TID")),            
                    Port = 10009,
                    Timeout = 30000
                };

                Terminal terminal = poslink.GetTerminal(setting);                             
               
                // Calculating the amount details for display purpose                  
                //string baseAmount = (decimal.Parse(amount)).ToString("0.00");
                //string taxAmount = "0.00";
                //string totalAmount = (decimal.Parse(amount) + decimal.Parse(taxAmount)).ToString("0.00");
                //string formattedText = string.Format(
                //                    "{0,-20}{1,10}\n{2,-20}{3,10}\n{4,-20}{5,10}",
                //                    "Total Amount:", baseAmount,
                //                    "Tax Amount:", taxAmount,
                //                    "Amount After Tax:", totalAmount
                //                    );


                // Generatig Amount request
                AmountRequest amountReq = new AmountRequest { TransactionAmount = (decimal.Parse(amount) * 100).ToString("000000000") };


                // Dialogue box (Showing Credit/Debit options)
                POSLinkAdmin.Form.ShowDialogRequest showTextBoxReq = new POSLinkAdmin.Form.ShowDialogRequest() { Title = "Payment Mode", Button1 = new SdButton() { Name = "CREDIT" }, Button2 = new SdButton() { Name = "DEBIT" }, Button3 = new SdButton() { Name = "" }, Button4 = new SdButton() { Name = "" }, Timeout = "100", ContinuousScreen = POSLinkAdmin.Const.ContinuousScreen.Default };
                POSLinkAdmin.Form.ShowDialogResponse showTextBoxRsp = new POSLinkAdmin.Form.ShowDialogResponse();       


                ExecutionResult executionResult = new ExecutionResult();
                TransactionLog transactionLog = new TransactionLog();
              
              
                ExecutionResult exe2 = terminal.Form.ShowDialog(showTextBoxReq, out showTextBoxRsp);

                // If Credit sale is selected
                if (showTextBoxRsp.ButtonNumber == "1") // CREDIT sale
                {
                    DoCreditRequest doCreditReq = new DoCreditRequest
                    {
                        TransactionType = POSLinkAdmin.Const.TransactionType.Sale,
                        AmountInformation = amountReq
                    };

                    DoCreditResponse doCreditRsp = new DoCreditResponse();

                    executionResult = terminal.Transaction.DoCredit(doCreditReq, out doCreditRsp);
                    transactionLog = GetTransactionResponse(executionResult, doCreditRsp , orderID);
                }

                // If Debit sale is selected
                else if (showTextBoxRsp.ButtonNumber == "2") // DEBIT sale
                {
                    DoDebitRequest doDebitReq = new DoDebitRequest
                    {
                        TransactionType = POSLinkAdmin.Const.TransactionType.Sale,
                        AmountInformation = amountReq
                    };

                    DoDebitResponse doDebitRsp = new DoDebitResponse();

                    executionResult = terminal.Transaction.DoDebit(doDebitReq, out doDebitRsp);
                    transactionLog = GetTransactionResponse(executionResult, doDebitRsp, orderID);
                }                

                // If any other operation is performed 
                else
                {
                    if (showTextBoxRsp != null && showTextBoxRsp.ResponseCode != "000000") { }
                    transactionLog = new TransactionLog
                    {
                        DateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                        OrderID = orderID,
                        TransactionType = "Unknown",
                        TransactionID = "N/A",
                        Success = false,
                        Code = showTextBoxRsp.ResponseCode,
                        Message = showTextBoxRsp.ResponseMessage
                    };
                }               


                return transactionLog;

            }
            catch (NullReferenceException ex)
            {
                return new TransactionLog
                {
                    DateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Success = false,
                    Code = "NullReferenceException",
                    Message = "Terminal not found"
                };
            }
            catch (Exception ex)
            {

                return new TransactionLog
                {

                    DateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Success = false,
                    Code=ex.GetType().ToString(),
                    Message = ex.Message
                };
            }
        }

        private static TransactionLog GetTransactionResponse(ExecutionResult executionResult, Response response, string orderID)
        {
            if (response == null)
            {
                return null; // Return null if response is invalid
            }

            string transactionType;
            string responseCode = "N/A";
            string responseMessage = "N/A";

            DoCreditResponse doCreditRsp = response as DoCreditResponse;
            DoDebitResponse doDebitRsp = response as DoDebitResponse;

            if (doCreditRsp != null)
            {
                transactionType = "Credit";
                responseCode = doCreditRsp.ResponseCode;
                responseMessage = doCreditRsp.ResponseMessage;
            }
            else if (doDebitRsp != null)
            {
                transactionType = "Debit";
                responseCode = doDebitRsp.ResponseCode;
                responseMessage = doDebitRsp.ResponseMessage;
            }
            else
            {
                return null; // If response is neither Credit nor Debit, return null
            }

            var traceInfo = doCreditRsp != null ? doCreditRsp.TraceInformation : doDebitRsp.TraceInformation;
            var amountInfo = doCreditRsp != null ? doCreditRsp.AmountInformation : doDebitRsp.AmountInformation;
            var hostInfo = doCreditRsp != null ? doCreditRsp.HostInformation : doDebitRsp.HostInformation;

            string transactionID = traceInfo != null ? traceInfo.GlobalUid : "N/A";
            string amount = "N/A";  // Default value
            if (amountInfo != null && amountInfo.ApprovedAmount != null)
            {
                try
                {
                    decimal parsedAmount = decimal.Parse(amountInfo.ApprovedAmount);
                    amount = (parsedAmount / 100).ToString("0.00");
                }
                catch (FormatException)
                {
                    // If parsing fails, amount remains "N/A"
                }
            }

            // Check if the payment is declined due to a connection error
            if (executionResult.GetErrorCode() != ExecutionResult.Code.Ok)
            {
                return new TransactionLog
                {
                    DateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Amount = amount,
                    OrderID = orderID,
                    TransactionType = transactionType,
                    TransactionID = transactionID,
                    Success = false,
                    Code = executionResult.GetErrorCode().ToString(),
                    Message = ErrorMessagesProvider.GetErrorMessage(executionResult.GetErrorCode().ToString())
                };
            }

            // Check if the payment is declined due to system error
            if (responseCode != "000000")
            {
                return new TransactionLog
                {
                    DateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                    Amount = amount,
                    OrderID = orderID,
                    TransactionType = transactionType,
                    TransactionID = transactionID,
                    Success = false,
                    Code = responseCode,
                    Message = responseMessage
                };
            }

            // Response from the host
            return new TransactionLog
            {
                DateTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                Amount = amount,
                OrderID = orderID,
                TransactionType = transactionType,
                TransactionID = transactionID,
                Success = hostInfo != null && hostInfo.HostResponseCode.ToString() == "0",
                Code = hostInfo != null ? hostInfo.HostResponseCode : "N/A",
                Message = hostInfo != null ? hostInfo.HostResponseMessage : "N/A"
            };
        }

       
    } 


}


