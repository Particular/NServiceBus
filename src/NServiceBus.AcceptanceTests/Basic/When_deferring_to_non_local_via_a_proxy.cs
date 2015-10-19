namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_deferring_to_non_local_via_a_proxy : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_be_received_by_proxy()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.When((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.DelayDeliveryWith(TimeSpan.FromSeconds(3));
                        return bus.SendAsync(new MyMessage(), options);
                    }))
                    .WithEndpoint<Receiver>()
                    .WithEndpoint<Proxy>()
                    .Done(c => c.MessagedDeliveredToProxy)
                    .Run();

            Assert.IsTrue(context.MessagedDeliveredToProxy);
            Assert.IsNotNull(context.UltimateDestination);
            Assert.IsNull(context.Route1);
        }

        public class Context : ScenarioContext
        {
            public bool MessagedDeliveredToProxy { get; set; }
            public string UltimateDestination { get; set; }
            public string Route1 { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((config, rt) =>
                {
                    config.EnableFeature<TimeoutManager>();
                    config.Routing().UnicastRoutingTable.AddStatic(typeof(MyMessage), rt[typeof(Receiver)], rt[typeof(Proxy)]);
                });
            }
        }

        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class Proxy : EndpointConfigurationBuilder
        {
            public Proxy()
            {
                EndpointSetup<DefaultServer>();
            }
            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }
                public IBus Bus { get; set; }

                public Task Handle(MyMessage message)
                {
                    Context.MessagedDeliveredToProxy = true;
                    Context.UltimateDestination = Bus.CurrentMessageContext.Headers[Headers.UltimateDestination];
                    string route1;
                    Bus.CurrentMessageContext.Headers.TryGetValue(Headers.SendVia+".1", out route1);
                    Context.Route1 = route1;
                    return Task.FromResult(0);
                }
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }
    }
}
