﻿using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace JINS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        #region CONSTS
        //strings
        public const string NEUTRAL = "neutral";
        public const string RIGHT = "right";
        public const string LEFT = "left";
        public const string BIG_UP = "big up";
        public const string BIG_DOWN = "big down";
        public const string BIG_LEFT = "big left";
        public const string BIG_RIGHT = "big right";
        public const string SMALL_UP = "small up";
        public const string SMALL_DOWN = "small down";
        public const string SMALL_LEFT = "small left";
        public const string SMALL_RIGHT = "small right";
        public const string NOISE = "noise";
        public const string BOTH_EYES = "full";
        public const string LEFT_EYE = "left";
        public const string RIGHT_EYE = "right";

        //options
        const int TIME_WINDOW = 100;
        const float MIN_POSITIVES_TO_DECLARE_MOVE = 0.8f;
        /// <summary>
        /// f-full (both eyes), r- right, l-left
        /// </summary>
        const string EYE_MODE = BOTH_EYES;

        //TODO: USTAWIANE PRZEZ JAKIS INITIAL CONFIG NA STARCIE PROGRAMU
        //ZAKRESY SYGNALU DO RUCHOW H
        const int NEUTRAL_RANGE_H_MIN = 330;
        const int NEUTRAL_RANGE_H_max = 400;

        const int SMALL_LEFT_MIN = 290;
        const int SMALL_LEFT_MAX = 330;
        const int BIG_LEFT_MIN = 220;
        const int BIG_LEFT_MAX = 290;

        const int SMALL_RIGHT_MIN = 400;
        const int SMALL_RIGHT_MAX = 430;
        const int BIG_RIGHT_MIN = 430;
        const int BIG_RIGHT_MAX = 520;

        //ZAKRESY SYGNALU DO RUCHOW V
        const int NEUTRAL_RANGE_V_MIN = -60;
        const int NEUTRAL_RANGE_V_max = -30;

        const int SMALL_UP_MIN = -30;
        const int SMALL_UP_MAX = 0;
        const int BIG_UP_MIN = 0;
        const int BIG_UP_MAX = 100;

        const int SMALL_DOWN_MIN = -80;
        const int SMALL_DOWN_MAX = -60;
        const int BIG_DOWN_MIN = -160;
        const int BIG_DOWN_MAX = -80;


        #endregion

        //okno do testowania gdzie sa galki oczne
        BoardWindow bWin;

        public SeriesCollection Series { get; set; }
        static byte[] bytes = new byte[1024];
        static Socket senderSock;
        bool stopCondition = false;
        Queue<int> vQueue;
        Queue<int> hQueue;
        bool? useSMA = true;
        bool? isHAxisChecked = true;

        int hCounter = 0;
        int vCounter = 0;
        //string lastHString = "";


        CancellationTokenSource h_tokenSource;
        CancellationTokenSource v_tokenSource;

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
                        new ObservableValue(200),
                        new ObservableValue(300),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100),
                        new ObservableValue(100)
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
            catch (SocketException exc)
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
                if (c == '\n')
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
            string[] data = raw.Split(',');
            DetermineV(data[12], data[13]); 
            DetermineH(data[10], data[11]);

            this.Dispatcher.Invoke(() =>
            {
                if (axisH_radio.IsChecked == true)
                {
                    rawText.Text = data[10] + "," + data[11];

                }
                else if (axisV_radio.IsChecked == true)
                {
                    rawText.Text = data[12] + "," + data[13];
                }
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

        const double ALPHA = 0.5;
        const int EMA_WINDOW = 100;
        double lastFiltered_V = 0.0;
        double index_V = 0;
        double lastFiltered_H = 0.0;
        double index_H = 0;
        const int SMA_TIME_WINDOW = 10;
        double[] sma_h = new double[SMA_TIME_WINDOW];
        double[] sma_v = new double[SMA_TIME_WINDOW];
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
                else if (index_V < EMA_WINDOW)
                {
                    lastFiltered_V = (lastFiltered_V + val) / 2;
                    index_V++;
                    return val;
                }
                else
                {
                    lastFiltered_V = ((ALPHA * lastFiltered_V) + ((1 - ALPHA) * val));
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
                else if (index_H < EMA_WINDOW)
                {
                    lastFiltered_H = (lastFiltered_H + val) / 2;
                    index_H++;
                    return val;
                }
                else
                {
                    lastFiltered_H = ((ALPHA * lastFiltered_H) + ((1 - ALPHA) * val));
                    return lastFiltered_H;
                }

            }

        }

        private void DetermineV(string right, string left)
        {
            int v1;
            int v2;
            if (int.TryParse(right, out v1) && int.TryParse(left, out v2))
            {
                int avg;
                if (EYE_MODE == BOTH_EYES)
                {
                    avg = (v1 + v2) / 2;
                }
                else if (EYE_MODE == LEFT_EYE)
                {
                    avg = v2;
                }
                else if (EYE_MODE == RIGHT_EYE)
                {
                    avg = v1;
                }
                //nałóż filtr
                if (useSMA == true)
                {
                    avg = (int)applySMA(avg,true);
                }
                else
                {
                    avg = (int)applyEMA(avg, true);
                }
                vQueue.Enqueue(avg);
                if (isHAxisChecked == false)
                    PlotData(avg);

                if (vQueue.Count >= TIME_WINDOW)
                {
                    vQueue.Dequeue();
                }
                //nie sprawdzacj ruchu po ostatnio wykrytym
                if (vCounter > TIME_WINDOW / 5)
                {
                    vCounter--;
                }
                else
                {
                    if (vQueue.Max() <= NEUTRAL_RANGE_V_max && vQueue.Min() >= NEUTRAL_RANGE_V_MIN)
                    {
                        //return NEUTRAL;
                    }
                    else
                    {
                        v_tokenSource = new CancellationTokenSource();
                        CancellationToken ct;
                        var tasks = new ConcurrentBag<Task>();
                        ct = v_tokenSource.Token;
                        var t = Task.Factory.StartNew(() => CheckUp(), v_tokenSource.Token);
                        tasks.Add(t);
                        t = Task.Factory.StartNew(() => CheckDown(), v_tokenSource.Token);
                        tasks.Add(t);

                        try
                        {
                            Task.WaitAll(tasks.ToArray());
                        }
                        catch (AggregateException e)
                        {
                            //foreach (var v in e.InnerExceptions)
                            //    Console.WriteLine(e.Message + " " + v.Message);
                        }
                        finally
                        {
                            v_tokenSource.Dispose();
                        }

                    }
                }
            }
        }

        private void DetermineH(string right, string left)
        {
            int v1;
            int v2;
            if (int.TryParse(right, out v1) && int.TryParse(left, out v2))
            {
                int avg;
                if (EYE_MODE == BOTH_EYES)
                {
                    avg = (v1 + v2) / 2;
                }
                else if (EYE_MODE == LEFT_EYE)
                {
                    avg = v2;
                }
                else if (EYE_MODE == RIGHT_EYE)
                {
                    avg = v1;
                }
                //nałóż filtr
                if (useSMA == true)
                {
                    avg = (int)applySMA(avg, false);
                }
                else
                {
                    avg = (int)applyEMA(avg, false);
                }
                hQueue.Enqueue(avg);
                if (isHAxisChecked == true)
                    PlotData(avg);

                if (hQueue.Count >= TIME_WINDOW)
                {
                    hQueue.Dequeue();
                }
                //nie sprawdzacj ruchu po ostatnio wykrytym
                if (hCounter > TIME_WINDOW/5)
                {
                    hCounter--;
                }
                else
                {
                    if (hQueue.Max() <= NEUTRAL_RANGE_H_max && hQueue.Min() >= NEUTRAL_RANGE_H_MIN)
                    {

                        //return NEUTRAL;
                    }
                    else
                    {
                        h_tokenSource = new CancellationTokenSource();
                        CancellationToken ct;
                        var tasks = new ConcurrentBag<Task>();
                        ct = h_tokenSource.Token;
                        var t = Task.Factory.StartNew(() => CheckLeft(), h_tokenSource.Token);
                        tasks.Add(t);
                        t = Task.Factory.StartNew(() => CheckRight(), h_tokenSource.Token);
                        tasks.Add(t);

                        try
                        {
                            Task.WaitAll(tasks.ToArray());
                        }
                        catch (AggregateException e)
                        {
                            //foreach (var v in e.InnerExceptions)
                            //    Console.WriteLine(e.Message + " " + v.Message);
                        }
                        finally
                        {
                            h_tokenSource.Dispose();
                        }

                    }
                }
            }
        }

        public void EndHSearch(string val)
        {
            if (!string.IsNullOrEmpty(val))
            {
                Application.Current.Dispatcher.Invoke(() =>
                    PopulateText(val)
                );
                Application.Current.Dispatcher.Invoke(() => MoveRadios(val));
            }
        }

        void PopulateText(string val)
        {
                PopulateHtext(val);
                PopulateVtext(val);
        }


        string CheckUp()
        {
            //    if (lastHString != UP)
            //   {
            float min_percentage_up = vQueue.Count / 5;
            float min_percentage_down = vQueue.Count / 5;
            //procent wartosci rosnacych
            float percentage_up = 0;
            //procent wartowsi malejacych
            float percentage_down = 0;
            int prev_val = vQueue.First();
            int queue_max = vQueue.Max();
            int queue_min = vQueue.Min();
            if (queue_max > SMALL_UP_MIN)
            {
                for (int i = 0; i < vQueue.Count; i++)
                {
                    int val = vQueue.ElementAt(i);

                    if (val >= prev_val)
                    {
                        percentage_up++;
                    }
                    else if (val < prev_val && percentage_up >= min_percentage_up)
                    {
                        percentage_down++;
                        if (percentage_down >= min_percentage_down)
                        {
                            if (queue_max <= SMALL_UP_MAX)
                            {
                                EndHSearch(SMALL_UP);
                                vCounter = TIME_WINDOW;
                                v_tokenSource.Cancel();
                                return (SMALL_UP);
                            }
                            else if (queue_max <= BIG_UP_MAX)
                            {
                                EndHSearch(BIG_UP);
                                vCounter = TIME_WINDOW;
                                v_tokenSource.Cancel();
                                return (BIG_UP);
                            }
                        }
                    }
                    prev_val = val;
                }
            }
            else if (queue_max > BIG_UP_MAX)
            {
                return "";// NOISE;
            }
            //         }
            return "";
        }

        string CheckDown()
        {
            //  if (lastHString != DOWN)
            //  {
            float min_percentage_up = vQueue.Count / 5;
            float min_percentage_down = vQueue.Count / 5;
            float percentage_up = 0;
            float percentage_down = 0;
            int prev_val = vQueue.First();
            int queue_max = vQueue.Max();
            int queue_min = vQueue.Min();
            if (queue_min < SMALL_DOWN_MAX)
            {
                for (int i = 0; i < vQueue.Count; i++)
                {
                    int val = vQueue.ElementAt(i);

                    if (val <= prev_val)
                    {
                        percentage_down++;
                    }
                    else if (val > prev_val && percentage_down >= min_percentage_down)
                    {
                        percentage_up++;
                        if (percentage_up >= min_percentage_up)
                        {
                            if (queue_min >= SMALL_DOWN_MIN)
                            {
                                EndHSearch(SMALL_DOWN);
                                vCounter = TIME_WINDOW;
                                v_tokenSource.Cancel();
                                return (SMALL_DOWN);
                            }
                            else if (queue_min > BIG_DOWN_MIN)
                            {
                                EndHSearch(BIG_DOWN);
                                vCounter = TIME_WINDOW;
                                v_tokenSource.Cancel();
                                return (BIG_DOWN);
                            }
                        }
                    }
                    prev_val = val;
                }
            }
            else if (queue_min < BIG_DOWN_MIN)
            {
                return ""; //NOISE;
            }
            //    }
            return "";
        }

        string CheckRight()
        {
            //    if (lastHString != RIGHT)
            //   {
            float min_percentage_up = hQueue.Count / 5;
            float min_percentage_down = hQueue.Count / 5;
            float percentage_up = 0;
            float percentage_down = 0;
            int prev_val = hQueue.First();
            int queue_max = hQueue.Max();
            int queue_min = hQueue.Min();
            if (queue_max > SMALL_RIGHT_MIN)
            {
                for (int i = 0; i < hQueue.Count; i++)
                {
                    int val = hQueue.ElementAt(i);

                    if (val >= prev_val)
                    {
                        percentage_up++;
                    }
                    else if (val < prev_val && percentage_up >= min_percentage_up)
                    {
                        percentage_down++;
                        if (percentage_down >= min_percentage_down)
                        {
                            if (queue_max <= SMALL_RIGHT_MAX)
                            {
                                EndHSearch(SMALL_RIGHT);
                                hCounter = TIME_WINDOW;
                                h_tokenSource.Cancel();
                                return (SMALL_RIGHT);
                            }
                            else if (queue_max <= BIG_RIGHT_MAX)
                            {
                                EndHSearch(BIG_RIGHT);
                                hCounter = TIME_WINDOW;
                                h_tokenSource.Cancel();
                                return (BIG_RIGHT);
                            }
                        }
                    }
                    prev_val = val;
                }
            }
            else if (queue_max > BIG_RIGHT_MAX)
            {
                return "";// NOISE;
            }
            //         }
            return "";
        }

        string CheckLeft()
        {
            //  if (lastHString != LEFT)
            //  {
            float min_percentage_up = hQueue.Count / 5;
            float min_percentage_down = hQueue.Count / 5;
            float percentage_up = 0;
            float percentage_down = 0;
            int prev_val = hQueue.First();
            int queue_max = hQueue.Max();
            int queue_min = hQueue.Min();
            if (queue_min < SMALL_LEFT_MAX)
            {
                for (int i = 0; i < hQueue.Count; i++)
                {
                    int val = hQueue.ElementAt(i);

                    if (val <= prev_val)
                    {
                        percentage_down++;
                    }
                    else if (val > prev_val && percentage_down >= min_percentage_down)
                    {
                        percentage_up++;
                        if (percentage_up >= min_percentage_up)
                        {
                            if (queue_min >= SMALL_LEFT_MIN)
                            {
                                EndHSearch(SMALL_LEFT);
                                hCounter = TIME_WINDOW;
                                h_tokenSource.Cancel();
                                return (SMALL_LEFT);
                            }
                            else if (queue_min > BIG_LEFT_MIN)
                            {
                                EndHSearch(BIG_LEFT);
                                hCounter = TIME_WINDOW;
                                h_tokenSource.Cancel();
                                return (BIG_LEFT);
                            }
                        }
                    }
                    prev_val = val;
                }
            }
            else if (queue_min < BIG_LEFT_MIN)
            {
                return ""; //NOISE;
            }
            //    }
            return "";
        }

        private void PopulateVtext(string text)
        {
            if (text != "?" && !string.IsNullOrEmpty(text))
            {
                if (text == BIG_DOWN || text == SMALL_DOWN)
                {
                    //lastHString = DOWN;
                    V_label.Content = text;
                }
                else if (text == BIG_UP || text == SMALL_UP)
                {
                    //lastHString = UP;
                    V_label.Content = text;

                }
                else
                {
                    //lastHString = NEUTRAL;
                }
            }
        }

        private void PopulateHtext(string text)
        {
            //string text = DetermineH(left, right);
            if (text != "?" && !string.IsNullOrEmpty(text))
            {
                if (text == BIG_RIGHT || text == SMALL_RIGHT)
                {
                    //lastHString = RIGHT;
                    H_label.Content = text;
                }
                else if (text == SMALL_LEFT || text == BIG_LEFT)
                {
                    //lastHString = LEFT;
                    H_label.Content = text;

                }
                else
                {
                    //lastHString = NEUTRAL;
                }
            }
        }

        private void MoveRadios(string direction)
        {
            if (bWin != null && bWin.IsVisible)
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
            if((e.Source as RadioButton).Name == "axisH_radio")
            {
                isHAxisChecked = true;
            }
            else
            {
                isHAxisChecked = false;
            }
            if (Series != null && Series[0] != null && Series[0].Values != null)
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

        private void on_maMethod_check(object sender, RoutedEventArgs e)
        {
            if ((e.Source as RadioButton).Name == "sma_radio")
            {
                useSMA = true;

            }
            else
            {
                useSMA = false;
            }
        }

        public void Dispose()
        {
            h_tokenSource.Dispose();
            v_tokenSource.Dispose();
            
        }
    }
}
