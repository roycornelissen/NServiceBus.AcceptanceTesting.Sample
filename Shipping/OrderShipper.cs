using Messages;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shipping
{
    public class OrderShipper: IHandleMessages<OrderAccepted>
    {
        public IBus Bus { get; set; }

        public void Handle(OrderAccepted message)
        {
            Debug.WriteLine("Order accepted... shipping order {0}", message.OrderId);

            Bus.Publish<OrderShipped>(m => m.OrderId = message.OrderId);
        }
    }
}
