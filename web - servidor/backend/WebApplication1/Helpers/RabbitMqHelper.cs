using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Helpers
{
    public class RabbitMqHelper
    {
        private readonly RabbitMqSettings _rabbitMqSettings;
        public RabbitMqHelper(IOptions<RabbitMqSettings> rabbitMqSettings)
        {
            _rabbitMqSettings = rabbitMqSettings.Value;
        }

        public void SendMessageToQueue(string message)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _rabbitMqSettings.HostName,
                UserName = _rabbitMqSettings.Username,
                Password = _rabbitMqSettings.Password,
                Port = Convert.ToInt32(_rabbitMqSettings.Port),
            };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: _rabbitMqSettings.QueueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: _rabbitMqSettings.QueueName,
                                     basicProperties: null,
                                     body: body);
            }
        }
    }
}
