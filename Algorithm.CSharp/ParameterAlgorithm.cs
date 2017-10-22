using QuantConnect.Data;
using QuantConnect.Algorithm;
using QuantConnect.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;

namespace QuantConnect.Algorithm.CSharp
{
	/// <summary>
	/// Basic template algorithm simply initializes the date range and cash
	/// </summary>
	public class ParameterAlgorithm : QCAlgorithm
	{

        private model.Parameters _parameters = new model.Parameters();

		public ExponentialMovingAverage Fast;
		public ExponentialMovingAverage Slow;

		public override void Initialize()
		{
            try
            {
                // 1. Get parameterMsg from rabbitmq
                String parameterMsg = consumeParams();

                // 2. Deserilize message content 
                _parameters.Deserilize(parameterMsg);

                SetStartDate(2013, 10, 07);
                SetEndDate(2013, 10, 11);
                SetCash(100 * 1000);

                AddSecurity(SecurityType.Equity, "SPY", Resolution.Minute);

                Fast = EMA("SPY", _parameters.FastPeriod);
                Slow = EMA("SPY", _parameters.SlowPeriod);
            }
            catch (Exception ex)
            {
                Logging.Log.Error(ex);
                Logging.Log.Error("Worker execute error, Exit!");
            }
		}

		public void OnData(TradeBars data)
		{
			// wait for our indicators to ready
			if (!Fast.IsReady || !Slow.IsReady) return;


			if (Fast > Slow * 1.001m)
			{
				SetHoldings("SPY", 1);
			}
			else if (Fast < Slow * 0.999m)
			{
                if (Portfolio.Invested)
                {
                    Liquidate("SPY");
                }
			}
		}


		/// <summary>
		/// Consumes the parameters. the worker mether to get one task
		/// </summary>
		private String consumeParams()
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
				channel.QueueDeclare(queue: "worker_queue",
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
				var message = channel.BasicGet(queue: "worker_queue", autoAck: false);
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