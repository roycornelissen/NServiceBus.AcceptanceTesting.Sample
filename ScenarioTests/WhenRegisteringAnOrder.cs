using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Customization;
using NServiceBus.Testing;
using NServiceBus.MessageMutator;
using NServiceBus.Features;
using Messages;
using ScenarioTests.Infrastructure;
using Shipping;
using Sales;

namespace ScenarioTests
{
    [TestClass]
    public class WhenRegisteringAnOrder
    {
        [TestMethod]
        public void Order_of_500_should_be_accepted_and_shipped()
        {
            Scenario.Define(() => new Context { })
                .WithEndpoint<Sales>(b => 
                    b.Given((bus, context) =>
                        // The SubscriptionBehavior will monitor for incoming subscription messages
                        // Here we want to track if Shipping is subscribing to our the OrderAccepted event
                        SubscriptionBehavior.OnEndpointSubscribed(s => 
                        {
                            if (s.SubscriberReturnAddress.Queue.Contains("Shipping"))
                            {
                                context.ShippingIsSubscribed = true;
                            }
                        }))
                        // As soon as ShippingIsSubscribed (guarded by the first expression), we'll
                        // fire off the test by sending a RegisterOrder command to the Sales endpoint
                    .When(context => context.ShippingIsSubscribed, bus => bus.Send<RegisterOrder>(m =>
                        {
                            m.Amount = 500;
                            m.CustomerName = "John";
                            m.OrderId = 1;
                        }))
                 )
                // No special actions for this endpoint, it just has to do its work
                .WithEndpoint<Shipping>() 
                // The test succeeds when the order is accepted by the Sales endpoint,
                // and subsequently the order is shipped by the Shipping endpoint
                .Done(c => c.OrderIsAccepted && c.OrderIsShipped && !c.OrderIsRefused)
                .Run();
        }

        [TestMethod]
        public void Order_under_500_should_be_accepted_and_shipped()
        {
            Scenario.Define(() => new Context { })
                .WithEndpoint<Sales>(b =>
                    b.Given((bus, context) =>
                        // The SubscriptionBehavior will monitor for incoming subscription messages
                        // Here we want to track if Shipping is subscribing to our the OrderAccepted event
                        SubscriptionBehavior.OnEndpointSubscribed(s =>
                        {
                            if (s.SubscriberReturnAddress.Queue.Contains("Shipping"))
                            {
                                context.ShippingIsSubscribed = true;
                            }
                        }))
                        // As soon as ShippingIsSubscribed (guarded by the first expression), we'll
                        // fire off the test by sending a RegisterOrder command to the Sales endpoint
                    .When(context => context.ShippingIsSubscribed, bus => bus.Send<RegisterOrder>(m =>
                    {
                        m.Amount = 499;
                        m.CustomerName = "Roy";
                        m.OrderId = 2;
                    }))
                 )
                // No special actions for this endpoint, it just has to do its work
                .WithEndpoint<Shipping>()
                // The test succeeds when the order is accepted by the Sales endpoint,
                // and subsequently the order is shipped by the Shipping endpoint
                .Done(c => c.OrderIsAccepted && c.OrderIsShipped && !c.OrderIsRefused)
                .Run();
        }

        [TestMethod]
        public void Order_over_500_should_be_refused_and_not_shipped()
        {
            Scenario.Define(() => new Context { })
                .WithEndpoint<Sales>(b =>
                    b.Given((bus, context) =>
                        // The SubscriptionBehavior will monitor for incoming subscription messages
                        // Here we want to track if Shipping is subscribing to our the OrderAccepted event
                        SubscriptionBehavior.OnEndpointSubscribed(s =>
                        {
                            if (s.SubscriberReturnAddress.Queue.Contains("Shipping"))
                            {
                                context.ShippingIsSubscribed = true;
                            }
                        }))
                        // As soon as ShippingIsSubscribed (guarded by the first expression), we'll
                        // fire off the test by sending a RegisterOrder command to the Sales endpoint
                    .When(context => context.ShippingIsSubscribed, bus => bus.Send<RegisterOrder>(m =>
                    {
                        m.Amount = 501;
                        m.CustomerName = "Udi";
                        m.OrderId = 3;
                    }))
                 )
                // No special actions for this endpoint, it just has to do its work
                .WithEndpoint<Shipping>()
                // The test succeeds when the order is accepted by the Sales endpoint,
                // and subsequently the order is shipped by the Shipping endpoint
                .Done(c => !c.OrderIsAccepted && c.OrderIsRefused && !c.OrderIsShipped)
                .Run();
        }

        class Context : ScenarioContext
        {
            public bool OrderIsAccepted { get; set; }
            public bool OrderIsRefused { get; set; }
            public bool OrderIsShipped { get; set; }
            public bool ShippingIsSubscribed { get; set; }
        }

        #region Endpoint configurations

        public class Shipping : EndpointConfigurationBuilder
        {
            public Shipping()
            {
                EndpointSetup<DefaultServer>()
                    // Makes sure that Shipping subscribes to OrderAccepted event from Sales endpoint
                    .AddMapping<OrderAccepted>(typeof(Sales));
            }

            class ShippingInspector : IMutateOutgoingMessages, INeedInitialization
            {
                public Context TestContext { get; set; }

                public object MutateOutgoing(object message)
                {
                    if (message is OrderShipped)
                    {
                        TestContext.OrderIsShipped = true;
                    }

                    return message;
                }

                public void Init(Configure config)
                {
                    config.Configurer.ConfigureComponent<ShippingInspector>(DependencyLifecycle.InstancePerCall);
                }
            }
        }

        public class Sales : EndpointConfigurationBuilder
        {
            public Sales()
            {
                EndpointSetup<DefaultServer>()
                    // Makes sure that the RegisterOrder command is mapped to the Sales endpoint
                    .AddMapping<RegisterOrder>(typeof(Sales));
            }

            class SalesInspector : IMutateOutgoingMessages, INeedInitialization
            {
                public Context TestContext { get; set; }

                public object MutateOutgoing(object message)
                {
                    if (message is OrderAccepted)
                    {
                        TestContext.OrderIsAccepted = true;
                    }

                    if (message is OrderRefused)
                    {
                        TestContext.OrderIsRefused = true;
                    }

                    return message;
                }

                public void Init(Configure config)
                {
                    config.Configurer.ConfigureComponent<SalesInspector>(DependencyLifecycle.InstancePerCall);
                }
            }
        }

        #endregion
    }
}
