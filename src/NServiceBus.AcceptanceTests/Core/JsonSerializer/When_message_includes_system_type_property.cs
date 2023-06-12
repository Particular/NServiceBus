namespace NServiceBus.AcceptanceTests.Core.JsonSerializer
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_message_includes_system_type_property : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_blow_up_on_send()
        {
            var ex = Assert.ThrowsAsync<NotSupportedException>(async () => await Scenario.Define<Context>()
               .WithEndpoint<Endpoint>(c => c
                   .When(b => b.SendLocal(new MyMessage { SystemTypeProperty = typeof(MyMessage) })))
               .Done(c => c.EndpointsStarted)
               .Run());

            StringAssert.Contains("System.Type", ex.Message);
        }

        public class Context : ScenarioContext
        {
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    c.UseSerialization<SystemJsonSerializer>();
                });
            }
        }

        public class MyMessage : IMessage
        {
            public Type SystemTypeProperty { get; set; }
        }
    }
}