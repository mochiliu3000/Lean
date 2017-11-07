using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp.model
{
    class ShortParameters
    {
        public int bollWindow { get; set; }
        public decimal bollDev { get; set; }
        public int cciWindow { get; set; }
        public int atrWindow { get; set; }

        public override string ToString()
        {
            return "bollWindow:" + this.bollWindow + ",bollDev:" + this.bollDev +
                ",cciWindow:" + this.cciWindow + ",atrWindow:" + this.atrWindow;
        }

        public void Deserilize(string message)
        {
            string[] param = message.Split(',').Select(p => p.Split(':')[1]).ToArray();
            this.bollWindow = param[0].ToInt32();
            this.bollDev = param[1].ToDecimal();
            this.cciWindow = param[2].ToInt32();
            this.atrWindow = param[3].ToInt32();
        }
    }
}
