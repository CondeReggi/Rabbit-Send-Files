using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cliente.Models
{
    public class RabbitMqSettings
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }    
        public string Password { get; set; }
        public string QueueName { get; set; }
    }
}
