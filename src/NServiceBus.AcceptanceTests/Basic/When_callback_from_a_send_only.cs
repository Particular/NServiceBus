namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_callback_from_a_send_only : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            Scenario.Define<Context>()
                .WithEndpoint<SendOnlyEndpoint>(b => b.Given((bus, c) =>
                {
                    var exception = Assert.Throws<Exception>(() => bus.Send(new MyMessage()).Register(result => { }));
                    Assert.AreEqual("Callbacks are invalid in a sendonly endpoint.", exception.Message);

                }))
                .WithEndpoint<Receiver>()
                .Run();
        }

        public class Context : ScenarioContext
        {
        }

        public class SendOnlyEndpoint : EndpointConfigurationBuilder
        {
            public SendOnlyEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .SendOnly()
                    .AddMapping<MyMessage>(typeof(Receiver));
            }

        }
        public class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        [Serializable]
        public class MyMessage : ICommand
        {
        }

    }
}
