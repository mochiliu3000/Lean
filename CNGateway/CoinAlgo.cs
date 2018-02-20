/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/


using QuantConnect;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Parameters;

namespace CNGateway
{
    public class CoinAlgo : QCAlgorithm
    {

        private Symbol _sec = QuantConnect.Symbol.Create("eth", SecurityType.Crypto, "coinegg");

        // we place attributes on top of our fields or properties that should receive
        // their values from the job. The values 100 and 200 are just default values that
        // or only used if the parameters do not exist
        [Parameter("ema-fast")]
        public int FastPeriod = 100;

        [Parameter("ema-slow")]
        public int SlowPeriod = 200;

        public ExponentialMovingAverage Fast;
        public ExponentialMovingAverage Slow;

        public override void Initialize()
        {
           
            //SetTimeZone(TimeZones.Shanghai);
            AddSecurity(SecurityType.Crypto, "eth", Resolution.Tick, "coinegg", true, 0, false);

            SetStartDate(2017, 1, 1);
            SetEndDate(2018, 2, 16);
            SetCash(100 * 1000);



            Fast = EMA(_sec, FastPeriod);
            Slow = EMA(_sec, SlowPeriod);
        }

        public override void OnData(Slice slice)
        {
            QuantConnect.Logging.Log.Debug("Fast:" + Fast.Current.Value.ToString());
            // wait for our indicators to ready
            if (!Fast.IsReady || !Slow.IsReady) return;

            if (Fast > Slow * 1.001m)
            {
                SetHoldings(_sec, 1);
            }
            else if (Fast < Slow * 0.999m)
            {
                Liquidate(_sec);
            }
        }
    }
}
