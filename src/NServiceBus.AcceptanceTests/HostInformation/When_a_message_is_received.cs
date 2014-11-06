namespace NServiceBus.AcceptanceTests.HostInformation
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Config;
    using NUnit.Framework;
    using Unicast;

    public class When_a_message_is_received : NServiceBusAcceptanceTest
    {
        static Guid hostId = new Guid("39365055-daf2-439e-b84d-acbef8fd803d");
        const string displayName = "FooBar";

        [Test]
        public void Host_information_should_be_available_in_headers()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(e => e.Given(b => b.SendLocal(new MyMessage())))
                .Done(c => c.HostId != Guid.Empty)
                .Run();

            Assert.AreEqual(hostId, context.HostId);
            Assert.AreEqual(displayName, context.HostDisplayName);
        }

        public class Context : ScenarioContext
        {
            public Guid HostId { get; set; }
            public string HostDisplayName { get; set; }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public IBus Bus { get; set; }

            public Context Context { get; set; }

            public void Handle(MyMessage message)
            {
                Context.HostId = new Guid(Bus.GetMessageHeader(message, Headers.HostId));
                Context.HostDisplayName = Bus.GetMessageHeader(message, Headers.HostDisplayName);
            }
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        class OverrideHostInformation : IWantToRunWhenConfigurationIsComplete
        {
            public UnicastBus UnicastBus { get; set; }

            public void Run(Configure config)
            {
                var hostInformation = new Hosting.HostInformation(hostId, displayName);
#pragma warning disable 618
                UnicastBus.HostInformation = hostInformation;
#pragma warning restore 618
            }
        }
    }
}