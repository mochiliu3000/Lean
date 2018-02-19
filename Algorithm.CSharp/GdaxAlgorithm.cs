using System;
using System.Text;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using RabbitMQ.Client;

namespace QuantConnect.Algorithm.CSharp
{
    public class GdaxAlgorithm : QCAlgorithm
    {
        private Symbol _btc = QuantConnect.Symbol.Create("BTCUSD", SecurityType.Crypto, Market.GDAX);
        //private Symbol _btc = QuantConnect.Symbol.Create("SPY", SecurityType.Equity, Market.USA);

        private static string Seperator = ",";
        private ConnectionFactory connFactory = new ConnectionFactory() { HostName = "localhost" };

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
            var messageBody = consolidated.Time.ToString() + Seperator + consolidated.Open.ToString() + Seperator
                + consolidated.High.ToString() + Seperator + consolidated.Low.ToString() + Seperator
                + consolidated.Close.ToString() + Seperator + consolidated.Volume.ToString() + Seperator
                + _ema27 + Seperator + _rsi6 + Seperator + _rsi12 + Seperator + _rsi24 + Seperator
                + _bb20.UpperBand + Seperator + _bb20.MiddleBand + Seperator + _bb20.LowerBand;
            Error(messageBody);
            Publish(messageBody, "consolidate15sInd");
        }

        public void Publish(string messageBody, string queueName)
        {
            using (var connection = connFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // Declaring a queue is idempotent - it will only be created if it doesn't exist already
                channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var properties = channel.CreateBasicProperties();
                // this will mark the msg persistent by writing it to cache and disk
                properties.Persistent = true;

                // create the message; msg is a byte array, encode whatever u like
                var body = Encoding.UTF8.GetBytes(messageBody);

                // publish is the key action for producer; send to the queue
                // the task will only send once. once the task is sent to the queue, it will out of the channel.
                channel.BasicPublish(exchange: "",
                                     routingKey: queueName,
                                     basicProperties: properties,
                                     body: body);

            }
        }
    }
}
