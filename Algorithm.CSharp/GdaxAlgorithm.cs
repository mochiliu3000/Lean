using System;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    public class GdaxAlgorithm : QCAlgorithm
    {
        private Symbol _spy = QuantConnect.Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);

        public override void Initialize()
        {
            AddCrypto("BTCUSD", Resolution.Tick, Market.GDAX);
        }
        public override void OnData(Slice data)
        {
            Console.WriteLine(data[_spy].ticks[0].Values[0]);
        }
    }
}
