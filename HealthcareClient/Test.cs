using HealthcareClient.Bike;
using HealthcareClient.BikeConnection;
using HealthcareClient.Net;
using HealthcareClient.ServerConnection;
using Networking.HealthCare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace HealthcareClient
{
    public enum Gender { Male, Female };
    public enum TestPhase {Setup, WarmingUp, Testing, Aborted, Finished};

    class Test : IClientMessageReceiver
    {
        private const int testFactor = 10; //CHANGE TO SPEED UP ONLY FOr DEBUG 
        private HealthCareClient client;
        private RealBike bike; // only to send resistances. Data is received via HandleClientMessage
        private int cadence = 0;
        //private const int targetCadence = 55;
        private int heartbeat = 0;
        private byte resistance = 0;
        private int watts = 0;
        private int age = 0;
        private Gender gender;
        private int weight;
        private TestPhase testPhase = TestPhase.Setup;
        private IChatDisplay chatDisplay;
        private Testdata testdata;
        private int measurementCount;
        private Timer timerSecondHalf;

        public Test(HealthCareClient client, RealBike bike, int age, Gender gender, int weight, IChatDisplay chatDisplay)
        {
            this.client = client;
            this.bike = bike;
            this.age = age;
            this.gender = gender;
            this.weight = weight;
            this.chatDisplay = chatDisplay;
            this.testdata = new Testdata();
        }

        /// <summary>
        /// Receives data from bike and heartrate monitor, through datamanager class
        /// </summary>
        /// <param name="clientMessage"></param>
        public void HandleClientMessage(ClientMessage clientMessage)
        {
            if (clientMessage.HasPage25)
            {
                this.cadence = clientMessage.Cadence;
                this.watts = clientMessage.Power;
            }
            if(clientMessage.HasHeartbeat)
            {
                this.heartbeat = clientMessage.Heartbeat;
            }           
            if(clientMessage.HasPage17)
            {
                this.resistance = clientMessage.Resistance;
            }
            System.Diagnostics.Debug.WriteLine("Received bikedata in Test");
        }

        /// <summary>
        /// This method retrieves the current cadence and alets the client to keep cadence in the 55-65 range
        /// </summary>
        public void ApproachTargetCadence(object sender, ElapsedEventArgs e)
        {
            if (testPhase == TestPhase.Testing)
            {
                if (cadence > 80)
                {
                    chatDisplay.DisplayChat("Please slow down to 60RPM");

                }
                else if (cadence < 50)
                {
                    chatDisplay.DisplayChat("Please increase speed to 60RPM");
                }
                else
                    chatDisplay.DisplayChat("Please keep your current speed");
            }

        }

        /// <summary>
        /// This method must be called repeatedly to ensure the cliënts heartbeat remains within testing limits (over 130 /min). 
        /// This method will abort the test if the heartbeat exceeds safe limits (220 - age)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ApproachTargetHeartbeat(object sender, ElapsedEventArgs e)
        {
            if (testPhase != TestPhase.Aborted && testPhase != TestPhase.Finished)
            {
                if (this.heartbeat < 130)
                {
                    SetBikeResistance(++resistance);
                }
                if (this.heartbeat > 220 - age)
                {
                    chatDisplay.DisplayChat("Heartrate dangerously high. Aborting test.");
                    AbortTest();
                }
            }

        }

        private void AbortTest()
        {
            chatDisplay.DisplayChat("Test aborted. Please stop cycling.");
            System.Diagnostics.Debug.WriteLine("Aborting Test");
            this.testPhase = TestPhase.Aborted;
            SetBikeResistance(50);
        }

        public void InitializeTest()
        {
            //start timer, connect to bike. Set bike resistance to a default value
            System.Diagnostics.Debug.WriteLine("Initializing Test");

            {

                this.testPhase = TestPhase.WarmingUp;

                // start warming up
                chatDisplay.DisplayChat("2 minutes of warmup remain");
                var progressDelegate = new System.Timers.Timer(60000 / testFactor);
                progressDelegate.Elapsed += On1MinuteWarmupRemaining;
                progressDelegate.AutoReset = false;
                progressDelegate.Enabled = true;

                var timerDelegate = new System.Timers.Timer(120000 / testFactor);
                timerDelegate.Elapsed += OnFinishWarmingUp;
                timerDelegate.AutoReset = false;
                timerDelegate.Enabled = true;
                SetBikeResistance(50);

                //setup resistance monitoring and safety measures
                var heartbeatDelegate = new System.Timers.Timer(10000 / testFactor);
                heartbeatDelegate.Elapsed += ApproachTargetHeartbeat;
                heartbeatDelegate.AutoReset = true;
                heartbeatDelegate.Enabled = true;
            }
            
            
        }

        private void On1MinuteWarmupRemaining(object sender, ElapsedEventArgs e)
        {
            if (testPhase == TestPhase.WarmingUp)
            {
                chatDisplay.DisplayChat("1 minute of warmup remains");
            }
        }

        private void OnFinishWarmingUp(object sender, ElapsedEventArgs e)
        {
            if (this.testPhase == TestPhase.WarmingUp)
            {

                var cadenceDelegate = new System.Timers.Timer(10000 / testFactor);
                cadenceDelegate.Elapsed += ApproachTargetCadence;
                cadenceDelegate.AutoReset = true;
                cadenceDelegate.Enabled = true;

                StartTest();
            }
        }

        private void StartTest()
        {
            if (this.testPhase == TestPhase.WarmingUp)
            {
                this.testPhase = TestPhase.Testing;
                chatDisplay.DisplayChat("4 minutes remaining");
            // modify bike resistance to get cadence to around 60: increase resistance if cadence too high, decrease resistance if cadence too low
         
                var timerFirstMinute = new System.Timers.Timer(60000 / testFactor);
                timerFirstMinute.Elapsed += OnFirstMinute;
                timerFirstMinute.AutoReset = false;
                timerFirstMinute.Enabled = true;

                var timerSecondMinute = new System.Timers.Timer(120000 / testFactor);
                timerSecondMinute.Elapsed += onSecondMinute;
                timerSecondMinute.AutoReset = false;
                timerSecondMinute.Enabled = true;

                var timerDelegate = new System.Timers.Timer(240000 / testFactor);
                timerDelegate.Elapsed += OnFinishTest;
                timerDelegate.AutoReset = false;
                timerDelegate.Enabled = true;

                var progressDelegate = new System.Timers.Timer(60000 / testFactor);
                progressDelegate.Elapsed += On3MinutesTestRemaining;
                progressDelegate.AutoReset = false;
                progressDelegate.Enabled = true;

                var progressDelegate2 = new System.Timers.Timer(120000 / testFactor);
                progressDelegate2.Elapsed += On2MinutesTestRemaining;
                progressDelegate2.AutoReset = false;
                progressDelegate2.Enabled = true;

                var progressDelegate3 = new System.Timers.Timer(180000 / testFactor);
                progressDelegate3.Elapsed += On1MinuteTestRemaining;
                progressDelegate3.AutoReset = false;
                progressDelegate3.Enabled = true;
            }
            
        }

        private void OnFirstMinute(object sender, ElapsedEventArgs e)
        {
            if (heartbeat < 110)
            {
                chatDisplay.DisplayChat("Heart beat too low, aborting test");
                AbortTest();
            }
            testdata.minute1 = heartbeat;
        }

        private void onSecondMinute(object sender, ElapsedEventArgs e)
        {
            testdata.minute2 = heartbeat;
            measurementCount = 0;
            timerSecondHalf = new System.Timers.Timer(15000 / testFactor);
            timerSecondHalf.Elapsed += OnMeasuringPoint;
            timerSecondHalf.AutoReset = true;
            timerSecondHalf.Enabled = true;
            
        }

        private void OnMeasuringPoint(object sender, ElapsedEventArgs e)
        {
            testdata.heartbeats.Add(heartbeat);
            testdata.watts.Add(watts);
            measurementCount++;
            if (measurementCount >= 8)
                timerSecondHalf.Enabled = false;
        }

        private void On1MinuteTestRemaining(object sender, ElapsedEventArgs e)
        {
            if (testPhase == TestPhase.Testing)
            {
                chatDisplay.DisplayChat("1 minute remains");
            }
        }

        private void On2MinutesTestRemaining(object sender, ElapsedEventArgs e)
        {
            if (testPhase == TestPhase.Testing)
            {
                chatDisplay.DisplayChat("2 minutes remain");
            }
        }

        private void On3MinutesTestRemaining(object sender, ElapsedEventArgs e)
        {
            if (testPhase == TestPhase.Testing)
            {
                chatDisplay.DisplayChat("3 minutes remain");
            }
        }

        private void OnFinishTest(object sender, ElapsedEventArgs e)
        {
            if (this.testPhase == TestPhase.Testing)
            {
                double Vo2MAx = Finish();
                chatDisplay.DisplayChat("VO2MAX:" + Vo2MAx);
                this.testPhase = TestPhase.Finished;
                byte[] content = new byte[2];
                content[0] = (byte)Message.ValueId.VO2MAX;
                content[1] = (byte)Vo2MAx;
                Message Vo2Message = new Message(false, Message.MessageType.BIKEDATA, content);
                StartCoolingDown();
            }
        }

        private void StartCoolingDown()
        {
            if (this.testPhase == TestPhase.Finished)
            {
                chatDisplay.DisplayChat("Cooldown time!");
                SetBikeResistance(50);
                var timerCooldown = new System.Timers.Timer(120000 / testFactor);
                timerCooldown.Elapsed += OnFinishCoolDown;
                timerCooldown.AutoReset = false;
                timerCooldown.Enabled = true;
            }

            var timerDelegate = new System.Timers.Timer(60000);
            timerDelegate.Elapsed += OnFinishCoolDown;
            timerDelegate.AutoReset = false;
            timerDelegate.Enabled = true;

        }

        private void OnFinishCoolDown(object sender, ElapsedEventArgs e)
        {
            chatDisplay.DisplayChat("Cooldown ended, please step off");
        }

       

        public Double Finish()
        {
            SetBikeResistance(50);
            this.bike = null; // to stop changing of resistance after test finishes
            //AddTestValuestoTestData();
            chatDisplay.DisplayChat("Steady state reached: " + this.testdata.getSteadyStateReached());
            return this.testdata.getVO2MAX(gender, age);
            
        }

        private void AddTestValuestoTestData()
        {
            testdata.minute1 = 100;
            testdata.minute2 = 130;
            testdata.heartbeats = new List<int>(new int[] { 110,112,113, 116,113,115,117 });
            testdata.watts = new List<int>(new int[] { 100, 150, 200, 150, 100 });
        }

        private void SetBikeResistance(byte resistance)
        {
            if (this.bike != null)
                bike.SetResistence(resistance);
        }

    }

  
} 
