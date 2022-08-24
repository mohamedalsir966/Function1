using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationFunction.Service.QueueService
{
    public interface IQueueService
    {
        void SendMessage(string message);
    }
}
