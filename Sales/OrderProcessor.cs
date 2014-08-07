using Messages;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sales
{
    public class OrderProcessor: IHandleMessages<RegisterOrder>
    {
        public IBus Bus { get; set; }

        public void Handle(RegisterOrder message)
        {
            Debug.WriteLine("Received order {0}, processing...", message.OrderId, message.CustomerName);

            if (message.Amount <= 500)
            {
                Bus.Publish<OrderAccepted>(m => m.OrderId = message.OrderId);
            }
            else
            {
                Bus.Publish<OrderRefused>(m => m.OrderId = message.OrderId);
            }
        }
    }
}
