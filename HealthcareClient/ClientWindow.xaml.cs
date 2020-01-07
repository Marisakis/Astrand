﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Threading;
using HealthcareClient.Bike;
using HealthcareClient.BikeConnection;
using HealthcareClient.Net;
using HealthcareClient.ServerConnection;
using HealthcareServer.Vr;
using HealthcareServer.Vr.World;
using Microsoft.Win32;
using Networking.Client;
using Networking.HealthCare;
using Networking.Server;
using Networking.VrServer;
using Newtonsoft.Json.Linq;
using UIControls;
using UIControls.Charts;
using UIControls.Menu;

namespace HealthcareClient
{
    /// <summary>
    /// Interaction logic for ClientWindow.xaml
    /// </summary>
    public partial class ClientWindow : Window, IServerDataReceiver, IMessageReceiver, IClientMessageReceiver, IChatDisplay
    {
        private Client vrClient;
        private Session session;
        private DataManager dataManager;
        private RealBike bike;

        private HealthCareClient healthCareClient;
        private SceneManager sceneManager;

        private LiveChartControl liveChartControl;

        public ClientWindow(HealthCareClient healthCareClient)
        {
            InitializeComponent();

            this.vrClient = new Client("145.48.6.10", 6666, this, null);
            this.vrClient.Connect();
            this.healthCareClient = healthCareClient;
            this.healthCareClient.SetReciever(this);

            this.dataManager = new DataManager(this.healthCareClient, this);
            GetCurrentSessions();

            this.liveChartControl = new LiveChartControl("Hartslag", "", "", 40, 250, 180, 20, LiveChart.BlueGreenDarkTheme, true, true, true, true, false, false, true);
            Grid.SetColumn(this.liveChartControl, 1);
            grd_DataGrid.Children.Add(this.liveChartControl);

            this.Closed += ClientWindow_Closed;
        }

        private void ClientWindow_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private bool ConnectToBike()
        {
            if (!String.IsNullOrEmpty(txf_BikeId.Value))
            {
                this.bike = new RealBike(txf_BikeId.Value, this.dataManager);
                return true;
            }

            MessageBox.Show("FietsId veld mag niet leeg zijn!");
            return false;
        }

        private async Task Initialize(string sessionHost, string key)
        {
            this.session = new Session(ref vrClient);
            await this.session.Create(sessionHost, key);
        }

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            if (cmf_Sessions.SelectedValue != null)
            {
                string host = cmf_Sessions.SelectedValue.ToString();
                await Initialize(host, txf_Key.Value);

                if (!String.IsNullOrEmpty(this.session.GetTunnel().Id))
                {
                    lbl_Connected.Content = "Verbonden";
                    btn_Refresh.IsEnabled = false;
                    btn_ConnectToSession.IsEnabled = false;
                    btn_Refresh.Foreground = Brushes.Gray;
                    btn_ConnectToSession.Foreground = Brushes.Gray;

                    this.sceneManager = new SceneManager(this.session, this.vrClient);
                    this.sceneManager.Show();
                }
                else
                    MessageBox.Show("Could not start session invalid session or key!");
            }
            else
                MessageBox.Show("No session selected!");
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            GetCurrentSessions();
        }

        private void ConnectToBike_Click(object sender, RoutedEventArgs e)
        {
            if(ConnectToBike())
            {
                txf_BikeId.IsEnabled = false;
                btn_ConnectToBike.IsEnabled = false;
                btn_ConnectToBike.Foreground = Brushes.Gray;
            }
        }

        private void SendTestData_Click(object sender, RoutedEventArgs e)
        {
            SendTestBikeData();
            btn_SendTestData.IsEnabled = false;
            btn_SendTestData.Foreground = Brushes.Gray;
        }

        public void OnDataReceived(byte[] data)
        {
            if (this.session != null)
                this.session.OnDataReceived(data);
            else
                HandleRecieve(JObject.Parse(Encoding.UTF8.GetString(data)));
        }

        private void HandleRecieve(JObject jsonData)
        {
            if (jsonData.GetValue("id").ToString() == "session/list")
            {
                List<string> sessions = new List<string>();
                foreach (JObject session in jsonData.GetValue("data").ToObject<JToken>().Children())
                {
                    JObject clientInfo = session.GetValue("clientinfo").ToObject<JObject>();

                    sessions.Add(clientInfo.GetValue("host").ToString());
                }

                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    this.cmf_Sessions.Value = sessions;
                }));
            }
        }

        private void GetCurrentSessions()
        {
            this.vrClient.Transmit(Encoding.UTF8.GetBytes(Session.GetSessionsListRequest().ToString()));
        }

        public void OnMessageReceived(Message message)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                switch (message.messageType)
                {
                    case Message.MessageType.CHAT_MESSAGE:
                        {
                            DisplayChat("Dokter" + Encoding.UTF8.GetString(message.Content));
                            break;
                        }
                    case Message.MessageType.CHANGE_RESISTANCE:
                        {
                            this.bike.SetResistence(message.Content[0]);
                            break;
                        }
                    case Message.MessageType.START_SESSION:
                        {

                            break;
                        }
                    case Message.MessageType.STOP_SESSION:
                        {

                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }));
        }

        public void DisplayChat(string chatMessage)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                txb_Chat.Text += chatMessage + Environment.NewLine;
                txb_Chat.ScrollToEnd();
            }));

            
        }

        private void SendTestBikeData()
        {
            new Thread(() =>
            {
                Random random = new Random();

                while (true)
                {
                    Thread.Sleep(1000);

                    ClientMessage clientMessage = new ClientMessage();
                    clientMessage.HasHeartbeat = true;
                    clientMessage.HasPage16 = true;
                    clientMessage.HasPage25 = true;
                    clientMessage.Heartbeat = (byte)random.Next(10, 300);
                    clientMessage.Distance = (byte)random.Next(10, 100);
                    clientMessage.Speed = (byte)random.Next(10, 100);
                    clientMessage.Cadence = (byte)random.Next(10, 100);
                    HandleClientMessage(clientMessage);
                    if(this.dataManager.astrandTest != null)
                    this.dataManager.astrandTest.HandleClientMessage(clientMessage);

                    List<byte> bytes = new List<byte>();
                    //bytes.Add((byte)Message.ValueId.HEARTRATE);
                    //bytes.Add((byte)random.Next(10, 100));
                    //bytes.Add((byte)Message.ValueId.DISTANCE);
                    //bytes.Add((byte)random.Next(5, 20));
                    //bytes.Add((byte)Message.ValueId.SPEED);
                    //bytes.Add((byte)random.Next(0, 50));
                    //bytes.Add((byte)Message.ValueId.CYCLE_RHYTHM);
                    //bytes.Add((byte)random.Next(20, 60));
                    bytes.Add((byte)Message.ValueId.HEARTRATE);
                    bytes.Add(clientMessage.Heartbeat);
                    bytes.Add((byte)Message.ValueId.DISTANCE);
                    bytes.Add(clientMessage.Distance);
                    bytes.Add((byte)Message.ValueId.SPEED);
                    bytes.Add(clientMessage.Speed);
                    bytes.Add((byte)Message.ValueId.CYCLE_RHYTHM);
                    bytes.Add(clientMessage.Cadence);

                    this.healthCareClient.Transmit(new Message(false, Message.MessageType.BIKEDATA, bytes.ToArray()));
                }
            }).Start();
        }

        public void HandleClientMessage(ClientMessage clientMessage)
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (clientMessage.HasHeartbeat)
                {
                    lbl_Heartrate.Content = clientMessage.Heartbeat;
                    this.liveChartControl.GetLiveChart().Update(clientMessage.Heartbeat);
                }
                if (clientMessage.HasPage16)
                {
                    lbl_Distance.Content = clientMessage.Distance;
                    lbl_Speed.Content = clientMessage.Speed;
                }
                if (clientMessage.HasPage25)
                {
                    lbl_CycleRyhthm.Content = clientMessage.Cadence;
                }
            }));
        }

        private void StartTest_Click(object sender, RoutedEventArgs e)
        {

            Test test = new Test(this.bike, 0, Gender.Male, 0, this);
            dataManager.astrandTest = test;
            test.InitializeTest();
        }

        
    }
}