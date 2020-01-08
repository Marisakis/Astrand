using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareClient
{
    class Testdata
    {
        public int minute1 { get; set; } 
        public int minute2 { get; set; } 
        public List<int> heartbeats { get; set; }
        public List<int> watts { get; set; }
       
        public Testdata()
        {
            this.minute1 = 0;
            this.minute2 = 0;
            this.heartbeats = new List<int>();
            this.watts = new List<int>();
        }
        

        public Boolean getSteadyStateReached()
        {
            Boolean steady = true;
            if (heartbeats.Count <= 2)
                return false;
            for(int i = 0; i < heartbeats.Count -1; i++)
            {
                if (Math.Abs(heartbeats[i] - heartbeats[i + 1]) > 5)
                    steady = false;
            }
            return steady;
        }

        public double getVO2MAX(Gender gender, int age)
        {
            if(getSteadyStateReached())
            {
                double averageWatts = watts.Average();
                double averageHeartbeat = heartbeats.Average();
                return CalculateVO2MAx(averageWatts, gender, HeartbeatCorrection(age, Convert.ToDouble(averageHeartbeat)));
            }
            return 0;
        }

        private double CalculateVO2MAx(double watts, Gender gender, double heartbeat)
        {
            double load = watts * 6.12;
            double VOMax;
            //Calculate VO2
            if (gender == Gender.Female)
            {
                //Vrouwen: VO2max[ml / kg / min] = (0.00193 x belasting +0.326) / (0.769 x HFss -56.1) x 1000
                VOMax = (0.00193 * load + 0.326) / (0.769 * heartbeat - 56.1) * 100;
            }
            else
            {
                //Mannen: VO2max[ml / kg / min] = (0.00212 x belasting +0.299) / (0.769 x HFss -48.5) x 1000
                VOMax = (0.00212 * load + 0.299) / (0.769 * heartbeat - 48.5) * 100;
            }
            return VOMax;
        }
        private double HeartbeatCorrection(int age, double heartratecorrected)
        {
            // age correction for maximum HF value
            if (age < 25)
            {
                return heartratecorrected *= 1.1;
            }

            if (35 > age && age >= 25)
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

            if (55 > age && age >= 50)
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
    }

}
