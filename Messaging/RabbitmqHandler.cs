using QuantConnect.Logging;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Messaging
{
    class RabbitmqHandler
    {
        /*
    public void Publish(Algorithm.CSharp.model.Parameters parameters)
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            // Declaring a queue is idempotent - it will only be created if it doesn't exist already
            channel.QueueDeclare(queue: "worker_queue",
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var properties = channel.CreateBasicProperties();
            // this will mark the msg persistent by writing it to cache and disk
            properties.Persistent = true;

            // create the message; msg is a byte array, encode whatever u like
            var message = parameters.ToString();
            var body = Encoding.UTF8.GetBytes(message);

            // publish is the key action for producer; send to the queue
            // the task will only send once. once the task is sent to the queue, it will out of the channel.
            channel.BasicPublish(exchange: "",
                                 routingKey: "worker_queue",
                                 basicProperties: properties,
                                 body: body);
            Log.Trace("Published a parameter set to worker queue --- " + message);
        }

    }
    */

        public void Publish(string statistics, string queueName)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
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
                var body = Encoding.UTF8.GetBytes(statistics);

                // publish is the key action for producer; send to the queue
                // the task will only send once. once the task is sent to the queue, it will out of the channel.
                channel.BasicPublish(exchange: "",
                                     routingKey: queueName,
                                     basicProperties: properties,
                                     body: body);
                Log.Trace("Published a worker's log to " + queueName + ": " + statistics);
            }
        }

    }
}
