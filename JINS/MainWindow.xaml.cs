using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace JINS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //senderSock.Close();
        }

        static byte[] bytes = new byte[1024];
        static Socket senderSock;
        bool stopCondition = false;

        public void DoSocketGet()
        {
            stopCondition = false;
            try
            {
                // Create one SocketPermission for socket access restrictions 
                SocketPermission permission = new SocketPermission(
                    NetworkAccess.Connect,    // Connection permission 
                    TransportType.Tcp,        // Defines transport types 
                    "",                       // Gets the IP addresses 
                    SocketPermission.AllPorts // All ports 
                    );

                // Ensures the code to have permission to access a Socket 
                permission.Demand();

                // Resolves a host name to an IPHostEntry instance            
                IPHostEntry ipHost = Dns.GetHostEntry("");

                // Gets first IP address associated with a localhost 
                IPAddress ipAddr = IPAddress.Parse("192.168.52.1");
                // Creates a network endpoint 
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddr, 60000);

                // Create one Socket object to setup Tcp connection 
                senderSock = new Socket(
                    ipAddr.AddressFamily,// Specifies the addressing scheme 
                    SocketType.Stream,   // The type of socket  
                    ProtocolType.Tcp     // Specifies the protocols  
                    );

                senderSock.NoDelay = false;   // Using the Nagle algorithm 

                // Establishes a connection to a remote host 
                senderSock.Connect(ipEndPoint);

                // Receives data from a bound Socket. 
                int bytesRec = senderSock.Receive(bytes);

                // Converts byte array to string 
                String theMessageToReceive = Encoding.Unicode.GetString(bytes, 0, bytesRec);

                // Continues to read the data till data isn't available 
                while (!stopCondition)
                {
                    bytesRec = senderSock.Receive(bytes);
                    ShowData(Encoding.UTF8.GetString(bytes, 0, bytesRec));
                }

            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            finally
            {
                senderSock.Close();
                senderSock.Dispose();
            }
        }

        private void ShowData(string raw)
        {
            this.Dispatcher.Invoke(() =>
            {
                string[] data = raw.Split(',');
                if (axisH_radio.IsChecked==true)
                {
                    rawText.Text = data[5];
                }
                else if(axisV_radio.IsChecked==true){
                     rawText.Text = data[5];
                }
            });
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread connectionThread = new Thread(DoSocketGet);
            connectionThread.Start();
        }

        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            stopCondition = true;
        }
    }
}
