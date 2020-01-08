using HealthcareClient.Bike;
using HealthcareClient.BikeConnection;
using HealthcareClient.ServerConnection;
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

        private RealBike bike; // only to send resistances. Data is received via HandleClientMessage
        private int cadence = 0;
        //private const int targetCadence = 55;
        private int heartbeat = 0;
        private byte resistance = 0;
        private double watts = 0;
        private int age = 0;
        private Gender gender;
        private int weight;
        private TestPhase testPhase = TestPhase.Setup;
        private IChatDisplay chatDisplay;

        public Test(RealBike bike, int age, Gender gender, int weight, IChatDisplay chatDisplay)
        {
            this.bike = bike;
            this.age = age;
            this.gender = gender;
            this.weight = weight;
            this.chatDisplay = chatDisplay;
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
         
                var timerFirstMinute = new System.Timers.Timer(60000);


                var timerSecondMinute = new System.Timers.Timer(60000);


                for (int i = 0; i < 8; i++)
                {
                var timerSecondHalf = new System.Timers.Timer(15000);

                }

            var timerFinished = new System.Timers.Timer(1000);
             timerFinished.Elapsed += OnFinishTest;
             timerFinished.AutoReset = false;
             timerFinished.Enabled = true;

                var timerDelegate = new System.Timers.Timer(1000);
                timerDelegate.Elapsed += OnFinishTest;
                timerDelegate.AutoReset = false;
                timerDelegate.Enabled = true;

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
                Finish();
                this.testPhase = TestPhase.Finished;
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
            //Finish();

        }

        private void OnFinishCoolDown(object sender, ElapsedEventArgs e)
        {
            chatDisplay.DisplayChat("Cooldown ended, please step off");
        }

        public double HeartbeatCorrection(double heartratecorrected)
        {
            // age correction for maximum HF value
            if (age < 25)
            {
                return heartratecorrected *= 1.1;
            }

            if (35> age && age >= 25)
            {
                return heartratecorrected *= 1.0;
            }

            if (40 > age && age >= 35)
            {
                return heartratecorrected *= 0.87;
            }

            if (45 > age && age >= 40)
            {
                return heartratecorrected *= 0.83;
            }

            if (50 > age && age >= 45)
            {
                return heartratecorrected = 0.78;
            }

            if (55 > age  && age >= 50)
            {
                return heartratecorrected *= 0.75;
            }

            if (60 > age && age >= 55)
            {
                return heartratecorrected *= 0.71;
            }

            if (65 > age && age >= 60)
            {
                return heartratecorrected *= 0.68;
            }
            //if age over 65:
                return heartratecorrected *= 0.65;
         
        }

        public Double Finish()
        {
            SetBikeResistance(50);
            this.bike = null; // to stop changing of resistance after test finishes

            double load = watts * 6.12;
            double VOMax;
            //Calculate VO2
            if (gender == Gender.Female)
            {
                //Vrouwen: VO2max[ml / kg / min] = (0.00193 x belasting +0.326) / (0.769 x HFss -56.1) x 1000
                VOMax = (0.00193 * load + 0.326) / (0.769 * HeartbeatCorrection(this.heartbeat) - 56.1) * 1000;
            }
            else
            {
                //Mannen: VO2max[ml / kg / min] = (0.00212 x belasting +0.299) / (0.769 x HFss -48.5) x 1000
                VOMax = (0.00212 * load +0.299) / (0.769 * HeartbeatCorrection(this.heartbeat) - 48.5) * 1000;
            }
            return VOMax;
        }

        private void SetBikeResistance(byte resistance)
        {
            if (this.bike != null)
                bike.SetResistence(resistance);
        }

    }

  
} 
