using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IEXTrading.Models
{
    public class Data
    {
        public string date { get; set; }
        public double open { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double close { get; set; }
        public int volume { get; set; }
        public int unadjustedVolume { get; set; }
        public float change { get; set; }
        public float changePercent { get; set; }
        public double vwap { get; set; }
        public string label { get; set; }
        public int changeOverTime { get; set; }
    }
}
