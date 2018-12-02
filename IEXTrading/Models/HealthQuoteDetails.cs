﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IEXTrading.Models
{
    public class HealthQuote
    {
        public float? week_52_Price_Range { get; set; }// To calculate 52-week Price Range for picking top 5 stocks
        [Key]
        public string symbol { get; set; }
        public string companyName { get; set; }
        public string primaryExchange { get; set; }
        public string sector { get; set; }
        public string calculationPrice { get; set; }
        public float? open { get; set; }
        public long? openTime { get; set; }
        public float? close { get; set; }
        public long? closeTime { get; set; }
        public float? high { get; set; }
        public float? low { get; set; }
        public float? latestPrice { get; set; }
        public string latestSource { get; set; }
        public string latestTime { get; set; }
        public long? latestUpdate { get; set; }
        public long? latestVolume { get; set; }
        public float? iexRealtimePrice { get; set; }
        public long? iexRealtimeSize { get; set; }
        public string iexLastUpdated { get; set; }
        public float? delayedPrice { get; set; }
        public long? delayedPriceTime { get; set; }
        public float? extendedPrice { get; set; }
        public float? extendedChange { get; set; }
        public float? extendedChangePercent { get; set; }
        public long? extendedPriceTime { get; set; }
        public float? previousClose { get; set; }
        public float? change { get; set; }
        public float? changePercent { get; set; }
        public float? iexMarketPercent { get; set; }
        public long? iexVolume { get; set; }
        public float? avgTotalVolume { get; set; }
        public float? iexBidPrice { get; set; }
        public long? iexBidSize { get; set; }
        public float? iexAskPrice { get; set; }
        public long? iexAskSize { get; set; }
        public float? marketCap { get; set; }
        public float? peRatio { get; set; }
        public float? week52High { get; set; }
        public float? week52Low { get; set; }
        public float? ytdChange { get; set; }
    }
}