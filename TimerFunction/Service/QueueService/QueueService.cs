using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationFunction.Service.QueueService
{
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Configuration;
    using System;

    namespace MessageSender.Services
    {
        public class QueueService : IQueueService
        {
            private readonly IConfiguration _configuration;

            public QueueService(IConfiguration configuration)
            {
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            }

            public void SendMessage( string message)
            {
                string connectionString = _configuration.GetValue<string>("AzureWebJobsStorage");
                string queueName = _configuration.GetValue<string>("QueueName");


                var queueClient = new QueueClient(connectionString, queueName, new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });

                queueClient.CreateIfNotExists();

                if (queueClient.Exists())
                {
                    queueClient.SendMessage(message);
                }
            }
        }
    }
}
