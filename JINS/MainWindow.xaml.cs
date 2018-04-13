using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace JINS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region CONSTS
        //strings
        const string NEUTRAL = "neutral";
        const string BIG_UP = "big up";
        const string BIG_DOWN = "big down";
        const string BIG_LEFT = "big left";
        const string BIG_RIGHT = "big right";
        const string SMALL_UP = "small up";
        const string SMALL_DOWN = "small down";
        const string SMALL_LEFT = "small left";
        const string SMALL_RIGHT = "small right";
        const string NOISE = "noise";

        //options
        const int TIME_WINDOW = 200;
        const float MIN_POSITIVES_TO_DECLARE_MOVE = 0.8f; 
        #endregion

        //okno do testowania gdzie sa galki oczne
        BoardWindow bWin;

        public SeriesCollection Series { get; set; }
        static byte[] bytes = new byte[1024];
        static Socket senderSock;
        bool stopCondition = false;
        Queue<int> vQueue;
        Queue<int> hQueue;
        string lastHString;
        string lastVstring;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            vQueue = new Queue<int>(TIME_WINDOW);
            hQueue = new Queue<int>(TIME_WINDOW);

            Series = new SeriesCollection
            {
                new LineSeries
                {
                    //AreaLimit = -10,
                    Values = new ChartValues<ObservableValue>
                    {
                        new ObservableValue(100),
                        new ObservableValue(200),
                        new ObservableValue(300),
                        new ObservableValue(100),
                        new ObservableValue(300),
                        new ObservableValue(4),
                        new ObservableValue(2),
                        new ObservableValue(5),
                        new ObservableValue(8),
                        new ObservableValue(3),
                        new ObservableValue(5),
                        new ObservableValue(6),
                        new ObservableValue(7),
                        new ObservableValue(3),
                        new ObservableValue(4),
                        new ObservableValue(2),
                        new ObservableValue(5),
                        new ObservableValue(8)
                    }
                }
            };
            //senderSock.Close();
        }


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
                    PrepareData(Encoding.UTF8.GetString(bytes, 0, bytesRec));
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

        string currentLine;
        private void PrepareData(string raw)
        {
            char c;
            for (int i = 0; i < raw.Length; i++)
            {
                c = raw[i];
                if(c == '\n')
                {
                    ShowData(currentLine);
                    currentLine = "";
                }
                else
                {
                    currentLine += raw[i];
                }
            }
        }

        private void ShowData(string raw)
        {
            this.Dispatcher.Invoke(() =>
            {
                string[] data = raw.Split(',');
                if (axisH_radio.IsChecked==true)
                {
                    rawText.Text = data[10]+","+data[11];

                }
                else if(axisV_radio.IsChecked==true){
                     rawText.Text = data[12]+","+data[13];
                }
                PopulateVtext(data[12], data[13]);
                PopulateHtext(data[10], data[11]);
            });
        }

        private void PlotData(double data)
        {
            Action action = delegate
            {
                Series[0].Values.Add(new ObservableValue(data));
                OnPropertyChanged("Series");
                if (Series[0].Values.Count >= TIME_WINDOW)
                {
                    Series[0].Values.RemoveAt(0);
                }
            };
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, action);
        }

        static double alpha = 0.5;
        static int EMA_window=100;
        double lastFiltered_V = 0.0;
        double index_V = 0;
        double lastFiltered_H = 0.0;
        double index_H = 0;
        double[] sma_h = new double[10];
        double[] sma_v = new double[10];
        int sma_index_h = 0;
        int sma_index_v = 0;
        private double applySMA(double val, bool isV)
        {
            if (isV)
            {
                if (index_V < sma_v.Length)
                {
                    lastFiltered_V = val;
                    sma_v[sma_index_v] = lastFiltered_V;
                    sma_index_v++;
                    index_V++;
                    return val;
                }
                else
                {
                    if (sma_index_v == sma_v.Length)
                    {
                        sma_index_v = 0;
                    }
                    sma_v[sma_index_v] = val;
                    lastFiltered_V = sma_v.Average();
                    sma_index_v++;
                    return lastFiltered_V;
                }
            }
            else
            {
                if (index_H < sma_h.Length)
                {
                    lastFiltered_H = val;
                    sma_h[sma_index_h] = lastFiltered_H;
                    sma_index_h++;
                    index_H++;
                    return val;
                }
                else
                {
                    if (sma_index_h == sma_h.Length)
                    {
                        sma_index_h = 0;
                    }
                    sma_h[sma_index_h] = val;
                    lastFiltered_H = sma_h.Average();
                    sma_index_h++;
                    return lastFiltered_H;
                }
            }
        }

        private double applyEMA(double val, bool isV)
        {
            if (isV)
            {
                if (index_V < 1)
                {
                    lastFiltered_V = val;
                    index_V++;
                    return val;
                }
                 else if (index_V < EMA_window)
                {
                    lastFiltered_V = (lastFiltered_V+val)/2;
                    index_V++;
                    return val;
                }
                else
                {
                    lastFiltered_V = ((alpha * lastFiltered_V) + ((1-alpha)*val));
                    return lastFiltered_V;
                }
            }
            else
            {
                if (index_H < 1)
                {
                    lastFiltered_H = val;
                    index_H++;
                    return val;
                }
                else if (index_H < EMA_window)
                {
                    lastFiltered_H = (lastFiltered_H + val) / 2;
                    index_H++;
                    return val;
                }
                else
                {
                    lastFiltered_H = ((alpha * lastFiltered_H) + ((1 - alpha) * val));
                    return lastFiltered_H;
                }

            }

        }      

        private string DetermineV(string right, string left)
        {
            int v1;
            int v2;
            if (int.TryParse(right, out v1) && int.TryParse(left, out v2))
            {
                //średnia z obu oczu
                int avg = (v1 + v2) / 2;
                //nałóż filtr
                if (sma_radio.IsChecked==true)
                {
                    avg = (int)applySMA(avg, true);
                }
                else
                {
                    avg = (int)applyEMA(avg, true);
                }
                vQueue.Enqueue(avg);

                if (axisV_radio.IsChecked==true)
                    PlotData(avg);
                if (vQueue.Count >= TIME_WINDOW)
                {
                    vQueue.Dequeue();
                }

                return GuessV(avg);
            }
            return "?";
        }

        private string GuessV(int value)
        {

            if (CheckIfInRange(-80,-10,true))
            {
                return NEUTRAL;
            }
            else if (CheckIfInRange(-120,-80,true))
            {
                MoveRadios(BIG_DOWN);
                return BIG_DOWN;
            }
            else if (CheckIfInRange(30,100,true))
            {
                MoveRadios(BIG_UP);
                return BIG_UP;
            }
            else if (CheckIfInRange(-120,-80,true))
            {
                MoveRadios(SMALL_DOWN);
                return SMALL_DOWN;
            }
            else if (CheckIfInRange(-30,-10,true))
            {
                MoveRadios(SMALL_UP);
                return SMALL_UP;
            }
            else
            {
                return NOISE;
            }
        }

        private string DetermineH(string right, string left)
        {
            int v1;
            int v2;
            if (int.TryParse(right, out v1) && int.TryParse(left, out v2))
            {
                //średnia z obu oczu
                int avg = (v1 + v2) / 2;
                //nałóż filtr
                if (sma_radio.IsChecked==true)
                {
                    avg = (int)applySMA(avg, false);
                }
                else
                {
                    avg = (int)applyEMA(avg, false);
                }
                hQueue.Enqueue(avg);
                if (axisH_radio.IsChecked == true)
                    PlotData(avg);

                if (hQueue.Count >=TIME_WINDOW)
                {
                    hQueue.Dequeue();
                }

                return GuessH(avg);
            }
            return "?";
        }

        private string GuessH(int value)
        {

            if (CheckIfInRange(280, 420, false))
            {
                return NEUTRAL;
            }
            else if (CheckIfInRange(260, 280, false))
            {
                MoveRadios(SMALL_LEFT);
                return SMALL_LEFT;
            }
            else if (CheckIfInRange(420,450,false))
            {
                MoveRadios(SMALL_RIGHT);
                return SMALL_RIGHT;
            }
            else if (CheckIfInRange(220,260,false))
            {
                MoveRadios(BIG_LEFT);
                return BIG_LEFT;
            }
            else if (CheckIfInRange(450,520,false))
            {
                MoveRadios(BIG_RIGHT);
                return BIG_RIGHT;
            }
            else
            {
                return NOISE;
            }
        }
        /// <summary>
        /// sprawdza, czy procent danych w oknie czasowym jest w zakresie podanego ruchu
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="isV"></param>
        /// <returns></returns>
        bool CheckIfInRange(int min , int max, bool isV=false)
        {
            int positives = 0;
            float percentage = 0f;
            if (isV)
            {
                foreach(int i in vQueue)
                {
                    if(i>min && i < max)
                    {
                        positives++;
                    }
                }
                percentage = (float)positives / (float)vQueue.Count;
                if (percentage > MIN_POSITIVES_TO_DECLARE_MOVE)
                {
                    return true;
                }
                else return false;
            }
            else
            {
                foreach (int i in hQueue)
                {
                    if (i > min && i < max)
                    {
                        positives++;
                    }
                }
                percentage = (float)positives / (float)hQueue.Count;
                if (percentage > MIN_POSITIVES_TO_DECLARE_MOVE)
                {
                    return true;
                }
                else return false;
            }
        }

        private void PopulateVtext(string right, string left)
        {
            string text = DetermineV(left, right);
            if (text != "?")
            {
                V_label.Content = text;
            }
        }

        private void PopulateHtext(string right, string left)
        {
            string text = DetermineH(left, right);
            if (text != "?")
            {
                H_label.Content = text;
            }
        }

        private void MoveRadios(string direction)
        {
            if(bWin !=null && bWin.IsVisible)
            {
                bWin.SetRadio(direction);
            }
        }


        #region Events

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            Thread connectionThread = new Thread(DoSocketGet);
            connectionThread.Start();
        }

        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            stopCondition = true;
        }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void axis_radio_Checked(object sender, RoutedEventArgs e)
        {
            if(Series!=null && Series[0]!=null && Series[0].Values!=null)
            Series[0].Values.Clear();
        }

        private void showBoard_Click(object sender, RoutedEventArgs e)
        {

            //initializing test window
            bWin = new BoardWindow();
            bWin.Show();
            bWin.Owner = this;
        }
        #endregion
    }
}
