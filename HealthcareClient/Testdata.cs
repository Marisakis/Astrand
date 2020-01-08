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
        public List<int> steadyState { get; set; }
       
        public Testdata(int minute1, int minute2, List<int> steadyState)
        {
            this.minute1 = minute1;
            this.minute2 = minute2;
            this.steadyState = steadyState;
        }
    }

}
