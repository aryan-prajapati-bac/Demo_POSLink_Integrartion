using Microsoft.Win32;
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
namespace POSLinkClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
                }
                catch (TimeoutException)
                {
                    ResultTextBlock.Text = "Connection timeout with the POS Helper Service.";
                }
                catch (Exception ex)
                {
                    ResultTextBlock.Text = "An error occurred: "+ex.Message;
                }
            }
            else
            {
                ResultTextBlock.Text = "Please enter a valid amount.";
            }
        }

       

        public static void RunHelperApp()
        {
            // Start the helper app as a background process
            if (File.Exists(@"C:\Users\Bacancy\OneDrive\documents\visual studio 2012\Projects\POSLinkHelperApp\POSLinkHelperApp\bin\Debug\POSLinkHelperApp.exe"))
            {
                Process.Start(@"C:\Users\Bacancy\OneDrive\documents\visual studio 2012\Projects\POSLinkHelperApp\POSLinkHelperApp\bin\Debug\POSLinkHelperApp.exe");
            }
            else
            {
                MessageBox.Show("Helper app not found at the specified path!");
            }

        }
    }
    public class POSLinkClient1
    {
        public string SendPaymentCommand(decimal amount)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "POSPipe", PipeDirection.InOut))
            {
               pipeClient.Connect(5000); // Set a timeout (3 seconds)

                using (StreamWriter writer = new StreamWriter(pipeClient) { AutoFlush = true })
                using (StreamReader reader = new StreamReader(pipeClient))
                {
                    string command = amount+"";
                    writer.WriteLine(command);

                    if (!pipeClient.IsConnected)
                        throw new IOException("Pipe connection was closed by the server.");

                    string response = reader.ReadLine(); // Read the response

                    if (response == null)
                        throw new IOException("No response from server, pipe might be closed.");

                    return response;
                }

                pipeClient.Close();
            }
            
        }
    }


    public static class StartupManager
{
        public static void AddHelperAppToStartup(string appName, string pathToExe)
    {
        RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        if (key != null)
        {
            key.SetValue(appName, string.Format("\"{0}\"", pathToExe)); // Add entry to run on startup
        }
    }

        

        //public static void RunHelperApp(string helperAppPath)
        //{
        //    if (File.Exists(helperAppPath))
        //    {
        //        ProcessStartInfo startInfo = new ProcessStartInfo
        //        {
        //            FileName = helperAppPath,
        //            WorkingDirectory = System.IO.Path.GetDirectoryName(helperAppPath),
        //            UseShellExecute = false, // Allows redirection of input/output
        //            RedirectStandardOutput = true, // To read output from the helper app
        //            RedirectStandardInput = true,  // To send commands to the helper app
        //            CreateNoWindow = true // Prevents opening a new window
        //        };

        //        try
        //        {
        //            using (Process helperApp = Process.Start(startInfo))
        //            {
        //                if (helperApp != null)
        //                {
        //                    using (StreamWriter writer = helperApp.StandardInput)
        //                    using (StreamReader reader = helperApp.StandardOutput)
        //                    {
        //                        // Sending a command to the helper app
        //                        writer.WriteLine("PAY:100");
        //                        writer.Flush();

        //                        // Reading the response from the helper app
        //                        string response = reader.ReadLine();
        //                        Console.WriteLine("Response from Helper App: " + response);
        //                    }

        //                    helperApp.WaitForExit();
        //                }
        //                else
        //                {
        //                    Console.WriteLine("Failed to start the helper app.");
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("Error running helper app: " + ex.Message);
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Helper app not found at: " + helperAppPath);
        //    }
        //}

}
    }



