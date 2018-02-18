using System;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class GdaxAlgorithm : QCAlgorithm
    {
        private Symbol _btc = QuantConnect.Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);
        //private Symbol _btc = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        private static string Seperator = ",";

        private ExponentialMovingAverage _ema27;
        private RelativeStrengthIndex _rsi6;
        private RelativeStrengthIndex _rsi12;
        private RelativeStrengthIndex _rsi24;
        private BollingerBands _bb20;

        public override void Initialize()
        {
            /*
            SetStartDate(2013, 10, 07);  //Set Start Date
            SetEndDate(2013, 10, 11);    //Set End Date
            AddEquity("SPY", Resolution.Tick);
            */
           
            AddCrypto("BTCUSD", Resolution.Tick, Market.GDAX);

            var consolidator15s = new TickConsolidator(TimeSpan.FromSeconds(15));

            _ema27 = new ExponentialMovingAverage(27);
            _rsi6 = new RelativeStrengthIndex(6);
            _rsi12 = new RelativeStrengthIndex(12);
            _rsi24 = new RelativeStrengthIndex(24);
            _bb20 = new BollingerBands(20, 2);
            RegisterIndicator(_btc, _ema27, consolidator15s);
            RegisterIndicator(_btc, _rsi6, consolidator15s);
            RegisterIndicator(_btc, _rsi12, consolidator15s);
            RegisterIndicator(_btc, _rsi24, consolidator15s);
            RegisterIndicator(_btc, _bb20, consolidator15s);

            consolidator15s.DataConsolidated += OnDataConsolidated;
            SubscriptionManager.AddConsolidator(_btc, consolidator15s);

            Log("Added new consolidator for " + _btc.Value);
        }

        public override void OnData(Slice data)
        {
            // Log(data[_btc][0].Value + "; " + data[_btc][0].Time + "; " + data[_btc][0].Quantity);
        }

        public void OnDataConsolidated(object sender, TradeBar consolidated)
        {
            Log("OnDataConsolidated called");
            Error(
                consolidated.Time.ToString() + Seperator + consolidated.Open.ToString() + Seperator
                + consolidated.High.ToString() + Seperator + consolidated.Low.ToString() + Seperator
                + consolidated.Close.ToString() + Seperator + consolidated.Volume.ToString() + Seperator
                + _ema27 + Seperator + _rsi6 + Seperator + _rsi12 + Seperator + _rsi24 + Seperator 
                + _bb20.UpperBand + Seperator + _bb20.MiddleBand + Seperator + _bb20.LowerBand
            );
        }
    }
}
