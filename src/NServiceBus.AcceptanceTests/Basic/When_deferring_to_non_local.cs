﻿namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_deferring_to_non_local : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Message_should_be_received()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<Endpoint>(b => b.Given((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.DelayDeliveryWith(TimeSpan.FromSeconds(3));
                        return bus.SendAsync(new MyMessage(), options);
                    }))
                    .WithEndpoint<Receiver>()
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.IsTrue(context.WasCalled);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(config => config.EnableFeature<TimeoutManager>())
                    .AddMapping<MyMessage>(typeof(Receiver));
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
                    Context.WasCalled = true;
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
