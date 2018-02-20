
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Packets;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Web.Script.Serialization;

using QuantConnect.Logging;

namespace CNGateway
{
    /// <summary>
    /// This is an implementation of <see cref="IDataQueueHandler"/> used for testing
    /// </summary>

    public class GatewayDataQueueHandler : IDataQueueHandler
    {

        private readonly ConcurrentDictionary<Symbol, string> _subscriptions = new ConcurrentDictionary<Symbol, string>();
        private readonly List<Tick> _ticks = new List<Tick>();

        private ConnectionFactory factory = new ConnectionFactory();
        private static IModel channel;
        private string queueName;
        private JsonParser jp;
        private String ExchangeName;

        #region IDataQueueHandler implementation
        public GatewayDataQueueHandler (){
            GatewaySetup();
        }

        /// <summary>
        /// Get the next ticks from the live trading data queue
        /// </summary>
        /// <returns>IEnumerable list of ticks since the last update.</returns>
        public IEnumerable<BaseData> GetNextTicks()
        {
            lock (_ticks)
            {
                var copy = _ticks.ToArray();

                //Console.WriteLine("GetNextTicks() _ticks size: {0}", _ticks.Count) ;
                _ticks.Clear();
                return copy;
            }
        }

        /// <summary>
        /// Adds the specified symbols to the subscription.
        /// CN Gateway push all symbols in the exchange to the queue. we only need define the routingKey
        /// to listen to a subset of the broadcast.
        /// </summary>
        /// <param name="job">Job we're subscribing for:</param>
        /// <param name="symbols">The symbols to be added keyed by SecurityType</param>
        public void Subscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {

            //Add the symbols to the list if they aren't there already.
            foreach (var symbol in symbols.Where(x => !x.Value.Contains("-UNIVERSE-")))
            {

                if (CanSubscribe(symbol))
                {
                    // _subscriptions stores all the routingKeys to bind with topicQueue
                    _subscriptions.TryAdd(symbol, symbol.Value);

                    SubscribeBinding(symbol);
                }
            }
        }

        private void SubscribeBinding(Symbol symbol)
        {
            //var bindingKey = "#" + symbol.Value;
            var bindingKey = "#";
            channel.QueueBind(queue: queueName,
                              exchange: ExchangeName,
                                routingKey: bindingKey);

            Console.WriteLine(" [*] RabbitMQ Add binding with '{0}' ", bindingKey);
        }

        private bool CanSubscribe(Symbol symbol)
        {
            var market = symbol.ID.Market;
            var securityType = symbol.ID.SecurityType;

            if (symbol.Value.Contains("-UNIVERSE-")) return false;

            return
                (securityType == SecurityType.Equity) ||
                (securityType == SecurityType.Forex && market == Market.FXCM) ||
                (securityType == SecurityType.Option && market == Market.USA) ||
                (securityType == SecurityType.Future);
        }

        /// <summary>
        /// Removes the specified symbols from the subscription
        /// </summary>
        /// <param name="job">Job we're processing.</param>
        /// <param name="symbols">The symbols to be removed keyed by SecurityType</param>
        public void Unsubscribe(LiveNodePacket job, IEnumerable<Symbol> symbols)
        {
            //NOP
        }


        #endregion


        /// <summary>
        /// Event handler: binded to gateway consumer.received
        /// Add the tick data to _ticks list.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="ea">E.</param>
        private void processTickEvent(object sender, BasicDeliverEventArgs ea)
        {

			var body = ea.Body;
			var routingKey = ea.RoutingKey;

			//Convert to string message to print
			var strMessage = Encoding.UTF8.GetString(body);
            //Console.WriteLine("[x] Received '{0}' Message:'{1}'",routingKey, strMessage);

            Tick tick = jp.ParseJson(strMessage, routingKey);

            lock (_ticks){
                //if (tick.IsValid()) _ticks.Add(tick);  
                _ticks.Add(tick);
			}
				
        }


        public void GatewaySetup()
        {
            // config data
            ExchangeName = "ticker";
            jp = new JsonParser("crypto");

            factory.UserName = "ctp";
            factory.Password = "ctp";
            factory.VirtualHost = "/";
            //factory.Protocol = Protocols.FromEnvironment();
            //factory.HostName = "192.168.199.164";
            factory.HostName = "localhost";
            factory.Port = AmqpTcpEndpoint.UseDefaultPort;

            IConnection connection = factory.CreateConnection();
            channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: ExchangeName, type: "topic");

            //define a queue with sys-genarated random name: 
            //1. only interested in the new message 
            //2. system will delete the queue when finish
            queueName = channel.QueueDeclare().QueueName;



            String bindingKey = "coinegg.#";
            channel.QueueBind(queue: queueName,
                              exchange: ExchangeName,
                                  routingKey: bindingKey);

            Console.WriteLine(" [*] RabbitMQ Waiting for messages. Bind with '{0}' ", bindingKey);


            // consumer setup 
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += processTickEvent;
            //{
            //    var body = ea.Body;
            //    var message = Encoding.UTF8.GetString(body);
            //    var routingKey = ea.RoutingKey;

            //    //add in the func you want to trigger
            //    Console.WriteLine(" [x] Received '{0}':'{1}'",
            //                routingKey,
            //                message);

            //};

            // start consume
            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();

        }
    }
}
