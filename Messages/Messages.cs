using NServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messages
{
    public class OrderAccepted: IEvent
    {
        public int OrderId { get; set; }
    }

    public class OrderShipped: IEvent
    {
        public int OrderId { get; set; }
    }

    public class OrderRefused : IEvent
    {
        public int OrderId { get; set; }
    }

    public class RegisterOrder: ICommand
    {
        public int OrderId { get; set; }

        public string CustomerName { get; set; }

        public decimal Amount { get; set; }
    }
}
