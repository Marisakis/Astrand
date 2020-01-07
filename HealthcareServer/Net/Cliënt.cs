using HealthcareServer.Files;
using Networking.HealthCare;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthcareServer.Net
{
    public enum Gender { Male, Female };
    public class Cliënt
    {
        public string BSN;
        public string ClientId;
        public string Name;
        public Gender gender;
        public int age;
        public int weight;
        

        public HistoryData HistoryData;

        public Cliënt(string bsn, string name, string clientId, Gender gender, int age, int weight)
        {
            this.BSN = bsn;
            this.Name = name;
            this.ClientId = clientId;
            this.gender = gender;
            this.age = age;
            this.weight = weight;
            this.HistoryData = new HistoryData();
        }
    }
}
