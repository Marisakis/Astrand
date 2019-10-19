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
    class Test: IClientMessageReceiver
    {
        private int cadence = 0;
        private int heartbeat = 0;

        public void HandleClientMessage(ClientMessage clientMessage)
        {
            this.cadence = clientMessage.Cadence;
            this.heartbeat = clientMessage.Heartbeat;
            System.Diagnostics.Debug.WriteLine("received bikedata in Test");
        }

        public void Start()
        {
            //start timer, connect to bike. Set bike resistance to a default value

            // start warming up
            var timerDelegate = new System.Timers.Timer(120000);
            timerDelegate.Elapsed += OnFinishWarmingUp;
            timerDelegate.AutoReset = false;
            timerDelegate.Enabled = true;
            
        }

        private void OnFinishWarmingUp(object sender, ElapsedEventArgs e)
        {


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


        /*public void Finish()
        {
            double watts = 1;
            double load = watts * 6.12;
            // int HFss = last hearthbeat
            //Calculate VO2
            if (gender = female)
            {
             //Vrouwen: VO2max[ml / kg / min] = (0.00193 x belasting +0.326) / (0.769 x HFss -56.1) x 1000
             double VOMax = (0.00193 * load + 0.326) / (0.769 * HFss - 56.1) * 1000
            } 
            else
            {
             //Mannen: VO2max[ml / kg / min] = (0.00212 x belasting +0.299) / (0.769 x HFss -48.5) x 1000
             double VOMax = (0.00212 x load +0.299) / (0.769 * HFss - 48.5) x 1000
            }
            return VOMax;
         }*/

        
    }

}
