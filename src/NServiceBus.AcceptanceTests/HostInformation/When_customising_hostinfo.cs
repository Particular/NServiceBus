namespace NServiceBus.AcceptanceTests.HostInformation
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Utils;
    using NUnit.Framework;

    public class When_customising_hostinfo : NServiceBusAcceptanceTest
    {
        static Guid hostId = new Guid("6c0f50de-dac9-4693-b138-6d1033c15ed6");
        static string instanceName = "Foo";
        static string hostName = "Bar";

        [Test]
        public void UsingCustomIdentifier()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<UsingCustomIdentifier_Endpoint>(e => e.Given(b => b.SendLocal(new MyMessage())))
                .Done(c => c.HostId != Guid.Empty)
                .Run();

            Assert.AreEqual(hostId, context.HostId);
        }

        [Test]
        public void UsingNames()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<UsingNames_Endpoint>(e => e.Given(b => b.SendLocal(new MyMessage())))
                .Done(c => c.HostId != Guid.Empty)
                .Run();

            Assert.AreEqual(DeterministicGuid.Create(instanceName, hostName), context.HostId);
        }

        public class UsingNames_Endpoint : EndpointConfigurationBuilder
        {
            public UsingNames_Endpoint()
            {
                EndpointSetup<DefaultServer>(b => b.UniquelyIdentifyRunningInstance().UsingNames(instanceName, hostName));
            }
        }

        public class UsingCustomIdentifier_Endpoint : EndpointConfigurationBuilder
        {
            public UsingCustomIdentifier_Endpoint()
            {
                EndpointSetup<DefaultServer>(b => b.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(hostId));
            }
        }

        public class MyMessageHandler : IHandleMessages<MyMessage>
        {
            public IBus Bus { get; set; }

            public Context Context { get; set; }

            public void Handle(MyMessage message)
            {
                Context.HostDisplayName = Bus.GetMessageHeader(message, Headers.HostDisplayName);
                Context.HostId = new Guid(Bus.GetMessageHeader(message, Headers.HostId));
            }
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
    }
}