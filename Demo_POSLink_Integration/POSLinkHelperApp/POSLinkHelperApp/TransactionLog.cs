using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace POSLinkHelperApp
{
    class TransactionLog
    {
        public string DateTime { get; set; }
        public string Amount { get; set; }
        public string OrderID { get; set; }
        public string TransactionType { get; set; }
        public string TransactionID { get; set; }
        public bool Success { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
       

        public static void LogTransaction(TransactionLog transactionLog)
        {
            try
            {
                // Define log directory (one level outside the project folder)
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Logs", "Transaction Logs");
                string fullPath = Path.GetFullPath(logDirectory); // Resolves relative path to absolute

                // Ensure directory exists
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                // Generate log file name based on the current date
                string logFileName = "TransactionLog_" + System.DateTime.UtcNow.ToString("yyyyMMdd") + ".txt";
                string logPath = Path.Combine(fullPath, logFileName);

                // Check if the file exists
                bool fileExists = File.Exists(logPath);

                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    // Write header only if file does not exist or is empty
                    if (!fileExists || new FileInfo(logPath).Length == 0)
                    {
                        writer.WriteLine("Time                 || Transaction ID   || Order ID  || Amount  || Transaction Type    || Success || Code  || Message               ");
                        writer.WriteLine("--------------------------------------------------------------------------------------------------------------------------");
                    }

                    // Build log entry string using StringBuilder
                    var logEntry = new StringBuilder();
                    logEntry.AppendFormat("{0,-20} || {1,-16} || {2,-8} || {3,-7} || {4,-20} || {5,-7} || {6,-5} || {7,-20}",
                        transactionLog.DateTime, transactionLog.TransactionID ?? "N/A", transactionLog.OrderID ?? "N/A",
                        transactionLog.Amount ?? "0.00", transactionLog.TransactionType ?? "Unknown",
                        transactionLog.Success ? "Yes" : "No", transactionLog.Code ?? "N/A", transactionLog.Message ?? "N/A");

                    // Write log entry
                    writer.WriteLine(logEntry.ToString());
                }

                //Console.WriteLine("Transaction logged successfully.");
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error logging transaction: " + ex.Message);
            }
        }


    }
}
