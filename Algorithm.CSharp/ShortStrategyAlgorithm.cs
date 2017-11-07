using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp
{
    class ShortStrategyAlgorithm : QCAlgorithm
    {
        // refer to IndicatorSuiteAlgorithm.cs
        private model.ShortParameters _parameters = new model.ShortParameters();

        private static double fixedSize = 1.0;
        private static decimal slMultiplier = 5.2m;
        private static decimal tpMultiplier = 5.2m;
        private static string _symbol = "SPY";

        public BollingerBands bb;
        public CommodityChannelIndex cci;
        public AverageTrueRange atr;

        public override void Initialize()
        {
            try
            {
                // 1. Get parameterMsg from rabbitmq
                string parameterMsg = consumeParams("worker_queue_test");
                //string parameterMsg = "bollWindow:2,bollDev:4.0,cciWindow:2,atrWindow:2";
                // 2. Deserilize message content 
                _parameters.Deserilize(parameterMsg);

                // 3. Set time and cash
                SetStartDate(2013, 10, 07);
                SetEndDate(2013, 10, 11);
                SetCash(100 * 1000);

                // 4. Set security and params
                AddSecurity(SecurityType.Equity, _symbol, Resolution.Minute);

                bb = BB(_symbol, _parameters.bollWindow, _parameters.bollDev, MovingAverageType.Simple);
                cci = CCI(_symbol, _parameters.cciWindow, MovingAverageType.Simple);
                atr = ATR(_symbol, _parameters.atrWindow, MovingAverageType.Wilders);
            }
            catch (Exception ex)
            {
                Logging.Log.Error(ex);
                Logging.Log.Error("Worker execute error, Exit!");
            }
        }

        // Note: Refer to BubbleAlgorithm.cs
        public void OnData(TradeBars data)
        {
            

            // wait for our indicators to ready
            if (!bb.IsReady || !cci.IsReady || !atr.IsReady) return;

            decimal tradePrice = 0.0m;

            if (!Portfolio.Invested)
            {
                if (cci.Current.Value < 0 && Securities[_symbol].Close < bb.LowerBand.Current.Value)
                {
                    SetHoldings(_symbol, -1.0 * fixedSize);
                    tradePrice = Securities[_symbol].Price;
        
                }
            }

            if (Portfolio.Invested)
            {
                var shortSL = Securities[_symbol].Low + atr * slMultiplier;
                var shortTP = tradePrice - atr * tpMultiplier;

                if (Securities[_symbol].Price >= shortSL)
                {
                    Liquidate(_symbol);
                }
                if (Securities[_symbol].Price <= shortTP)
                {
                    Liquidate(_symbol);
                }
            }
        }

        /// <summary>
        /// Consumes the parameters. the worker mether to get one task
        /// </summary>
        private String consumeParams(string queueName)
        {
            String parameterMsg;

            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // using the same queue 'task_queue' 
                // durable: set to 'true' will ensure the task will not be lost even if rabbit dies
                //      we're sure that the task_queue queue won't be lost even if RabbitMQ restarts.
                // 
                channel.QueueDeclare(queue: queueName,
                                     durable: true,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                // prefetchCount = 1: tells RabbitMQ don't dispatch a new message to a worker until 
                //     it has processed and acknowledged the previous one
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Logging.Log.Trace(" [*] Waiting for messages.");

                // Consumer is the key action for a worker/consumer; attach consumer to the queue.
                // autoAck by default is false (means default using manual ack)
                // set autoAck to false , will ensure task never lose even if you ctrl+c the worker,
                //     the rabbitMQ will find the next available worker instead
                var message = channel.BasicGet(queue: queueName, autoAck: false);
                if (message == null)
                {
                    throw new Exception("Error: Empty parameter set received, Worker Exit!");
                }

                // get the msg
                var body = message.Body;
                parameterMsg = Encoding.UTF8.GetString(body);

                // intercept the msg
                Logging.Log.Trace(" [x] Worker Received {0}", parameterMsg);

                // this line is important. rabbitMQ will not realease unless it's acked
                channel.BasicAck(deliveryTag: message.DeliveryTag, multiple: false);
            }

            return parameterMsg;
        }
    }
}

