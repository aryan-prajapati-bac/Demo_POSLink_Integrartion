using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
using System.Xml.Linq;
namespace POSLinkClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private POSLinkClient1 client;
        private static Process process;
        public MainWindow()
        {
            InitializeComponent();
            RunHelperApp(); 

        }


        private void ProcessPayment_Click(object sender, RoutedEventArgs e)
        {
            decimal amount;
            if (decimal.TryParse(AmountTextBox.Text, out amount))
            {
                POSLinkClient1 client = new POSLinkClient1();
                try
                {
                    string result = client.SendPaymentCommand(amount);
                    ResultTextBlock.Text = result; // Display result

                    if (process != null && !process.HasExited)
                    {
                        process.Kill(); // Sends a close request (like clicking 'X')
                        process.WaitForExit(); // Waits for the process to exit
                    }
                }
                catch (TimeoutException)
                {
                    ResultTextBlock.Text = "Connection timeout with the POS Helper Service.";
                    if (process != null && !process.HasExited)
                    {
                        process.Kill(); // Sends a close request (like clicking 'X')
                        process.WaitForExit(); // Waits for the process to exit
                    }
                }
                catch (Exception ex)
                {
                    ResultTextBlock.Text = "An error occurred: " + ex.StackTrace;
                    if (process != null && !process.HasExited)
                    {
                        process.Kill(); // Sends a close request (like clicking 'X')
                        process.WaitForExit(); // Waits for the process to exit
                    }
                }
            }
            else
            {
                ResultTextBlock.Text = "Please enter a valid amount.";
            }
        }


        //private void ProcessPayment_Click(object sender, RoutedEventArgs e)
        //{
        //    decimal amount;
        //    if (decimal.TryParse(AmountTextBox.Text, out amount))
        //    {
        //        try
        //        {
        //            string result = client.SendPaymentCommand(amount);
        //            ResultTextBlock.Text = result; // Display result
        //        }
        //        catch (TimeoutException)
        //        {
        //            ResultTextBlock.Text = "Connection timeout with the POS Helper Service.";
        //        }
        //        catch (Exception ex)
        //        {
        //            ResultTextBlock.Text = "An error occurred: " + ex.Message;
        //        }
        //    }
        //    else
        //    {
        //        ResultTextBlock.Text = "Please enter a valid amount.";
        //    }
        //}

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            client.ClosePipe();
            if (process != null && !process.HasExited)
            {
                process.CloseMainWindow(); // Sends a close request (like clicking 'X')
                process.WaitForExit(); // Waits for the process to exit
            }
        }



        public static void RunHelperApp()
        {
            // Start the helper app as a background process
            if (File.Exists(@"D:\Demo_POSLink_Integration\Demo_POSLink_Integrartion\Demo_POSLink_Integration\POSLinkHelperApp\POSLinkHelperApp\bin\Debug\POSLinkHelperApp.exe"))
            {
                process = Process.Start(@"D:\Demo_POSLink_Integration\Demo_POSLink_Integrartion\Demo_POSLink_Integration\POSLinkHelperApp\POSLinkHelperApp\bin\Debug\POSLinkHelperApp.exe");
            }
            else
            {
                MessageBox.Show("Helper app not found at the specified path!");
            }

        }
    }
    //public class POSLinkClient1
    //{
    //    public string SendPaymentCommand(decimal amount)
    //    {
    //        string str = "";
    //        using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "POSPipe", PipeDirection.InOut))
    //        {
    //            //NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "POSPipe", PipeDirection.InOut);
    //            pipeClient.Connect(); // Set a timeout (5 seconds)

    //            using (StreamWriter writer = new StreamWriter(pipeClient) { AutoFlush = true })
    //            using (StreamReader reader = new StreamReader(pipeClient))
    //            {
    //                //StreamWriter writer = new StreamWriter(pipeClient) { AutoFlush = true };
    //                //StreamReader reader = new StreamReader(pipeClient);
    //                string command = amount.ToString();


    //                if (!pipeClient.IsConnected)
    //                    throw new IOException("Pipe connection was closed by the server.");


    //                writer.WriteLine(command);

    //                string response = reader.ReadLine(); // Read the response
    //                Console.WriteLine(response);

    //                if (response == null)
    //                    throw new IOException("No response from server, pipe might be closed.");

    //                str = response;
    //            }


    //        }
    //        return str;

    //    }
    //}


    public class POSLinkClient1
    {
        private NamedPipeClientStream pipeClient;
        private StreamWriter writer;
        private StreamReader reader;

        public POSLinkClient1()
        {
            pipeClient = new NamedPipeClientStream(".", "POSPipe", PipeDirection.InOut);
            pipeClient.Connect(); // Connect once and reuse

            writer = new StreamWriter(pipeClient) { AutoFlush = true };
            reader = new StreamReader(pipeClient);
        }

        public string SendPaymentCommand(decimal amount)
        {
            if (!pipeClient.IsConnected)
                throw new IOException("Pipe connection was closed by the server.");

            writer.WriteLine(amount.ToString());
            writer.WriteLine("444");

            //string response = reader.ReadLine();

            //if (response == null)
            //    throw new IOException("No response from server, pipe might be closed.");


            string jsonResponse = reader.ReadToEnd();

            // Check if response is valid JSON
            if (IsJsonObject(jsonResponse))
            {
                JObject transaction = JObject.Parse(jsonResponse);

                Console.WriteLine("Transaction Received:");
                foreach (var property in transaction.Properties())
                {
                    Console.WriteLine(string.Format("{0}: {1}", property.Name, property.Value));
                }
            }
            else
            {
                Console.WriteLine("Server response is not a valid JSON object.");
                Console.WriteLine("Raw Response: " + jsonResponse);
            }

            return jsonResponse;
        }

        static bool IsJsonObject(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}");
        }

        public void ClosePipe()
        {
            if (writer != null) writer.Dispose();
            if (reader != null) reader.Dispose();
            if (pipeClient != null) pipeClient.Dispose();
        }
    }

    }



