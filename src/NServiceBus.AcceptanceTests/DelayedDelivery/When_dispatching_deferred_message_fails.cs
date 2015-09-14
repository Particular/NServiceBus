namespace NServiceBus.AcceptanceTests.DelayedDelivery
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Unicast.Queuing;
    using NUnit.Framework;

    public class When_dispatching_deferred_message_fails : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Delivery_should_be_delayed()
        {
            var delay = TimeSpan.FromSeconds(5);

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                {
                    var options = new SendOptions();

                    options.DelayDeliveryWith(delay);
                    options.SetDestination("invalidDestination");

                    bus.Send(new MyMessage(), options);
                    return Task.FromResult(0);
                }))
                .AllowExceptions(ex => ex is QueueNotFoundException)
                .Done(c =>
                {
                    Thread.Sleep(10000);
                    return true;
                })
                .Run();

            Assert.IsNotEmpty(context.PersistedTimeouts.DueTimeouts);
        }

        public class Context : ScenarioContext
        {
            public TimeoutsChunk PersistedTimeouts { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Transactions().DisableDistributedTransactions();
                });
            }

            public class Bla : IWantToRunWhenBusStartsAndStops
            {
                readonly Context context;
                readonly IQueryTimeouts timeoutsQuery;

                public Bla(Context context, IQueryTimeouts timeoutsQuery)
                {
                    this.context = context;
                    this.timeoutsQuery = timeoutsQuery;
                }

                public void Start()
                {
                }

                public void Stop()
                {
                    context.PersistedTimeouts = timeoutsQuery.GetNextChunk(DateTime.MinValue).GetAwaiter().GetResult();
                }
            }
        }

        [Serializable]
        public class MyMessage : IMessage
        {
        }
    }
}
