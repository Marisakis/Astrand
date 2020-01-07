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

    class Test : IClientMessageReceiver
    {

        private RealBike bike; // only to send resistances. Data is received via HandleClientMessage
        private int cadence = 0;
        private const int targetCadence = 55;
        private int heartbeat = 0;
        private byte resistance = 0;
        private double watts = 0;
        private int age = 0;
        private Gender gender;
        private int weight;


        public Test(RealBike bike, int age, Gender gender, int weight )
        {
            this.bike = bike;
            this.age = age;
            this.gender = gender;
            this.weight = weight;
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
            System.Diagnostics.Debug.WriteLine("received bikedata in Test");
        }

        /// <summary>
        /// This method retrieves the current cadence and modifies the resistance to keep cadence in the 55-65 range
        /// </summary>
        public void ApproachTargetCadence(object sender, ElapsedEventArgs e)
        {
            if(cadence > (targetCadence+5))
            {
                bike.SetResistence(++resistance);
                //Send message to client: slower
                
            }
            else if (cadence < (targetCadence - 5))
            {
                bike.SetResistence(--resistance);
                //send message to client: faster
            }
        }

        public void Start()
        {
            //start timer, connect to bike. Set bike resistance to a default value

            // start warming up
            var timerDelegate = new System.Timers.Timer(120000);
            timerDelegate.Elapsed += OnFinishWarmingUp;
            timerDelegate.AutoReset = false;
            timerDelegate.Enabled = true;
            bike.SetResistence(50);
            
            
        }

        private void OnFinishWarmingUp(object sender, ElapsedEventArgs e)
        {
            var cadenceDelegate = new System.Timers.Timer(1000);
            cadenceDelegate.Elapsed += ApproachTargetCadence;
            cadenceDelegate.AutoReset = true;
            cadenceDelegate.Enabled = true;

            StartTest();
        }

        private void StartTest()
        {
            // modify bike resistance to get cadence to around 60: increase resistance if cadence too high, decrease resistance if cadence too low
            var timerDelegate = new System.Timers.Timer(240000);
            timerDelegate.Elapsed += OnFinishTest;
            timerDelegate.AutoReset = false;
            timerDelegate.Enabled = true;

        }

        private void OnFinishTest(object sender, ElapsedEventArgs e)
        {

            StartCoolingDown();
        }

        private void StartCoolingDown()
        {

            var timerDelegate = new System.Timers.Timer(120000);
            timerDelegate.Elapsed += OnFinishCoolDown();
            timerDelegate.AutoReset = false;
            timerDelegate.Enabled = true;
            //Finish();

        }

        private ElapsedEventHandler OnFinishCoolDown()
        {
            throw new NotImplementedException();
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

                return heartratecorrected *= 0.65;
         
        }

        public Double Finish()
        {
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


    }

} 
