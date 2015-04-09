namespace NServiceBus.AcceptanceTests.Basic
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast;
    using NUnit.Framework;

    public class When_using_sendoptions : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_able_to_set_context_items_and_retrieve_it_via_a_behavior()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<SendOptionsExtensions>(b => b.Given((bus, c) =>
                    {
                        var sendOptions = new SendLocalOptions();
                        sendOptions.GetContext()["MySpecialExtension"] = "I did it!";

                        bus.SendLocal(new SendMessage(), sendOptions);
                    }))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.AreEqual("I did it!", context.Secret);
        }

        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public string Secret { get; set; }
        }

        public class SendOptionsExtensions : EndpointConfigurationBuilder
        {
            public SendOptionsExtensions()
            {
                EndpointSetup<DefaultServer>();
            }

            class SendMessageHandler : IHandleMessages<SendMessage>
            {
                public Context Context { get; set; }

                public void Handle(SendMessage message)
                {
                    Context.Secret = message.Secret;
                    Context.WasCalled = true;
                }
            }
        }

        [Serializable]
        public class SendMessage : ICommand
        {
            public string Secret { get; set; }
        }

        class TestingSendOptionsExtensionBehavior : Behavior<OutgoingContext>
        {
            public override void Invoke(OutgoingContext context, Action next)
            {
                var options = (SendMessageOptions)context.DeliveryMessageOptions;
                var secret = (string)options.Context["MySpecialExtension"];

                context.OutgoingLogicalMessage.UpdateMessageInstance(new SendMessage {Secret = secret});

                next();
            }

            class AddPipelineExtension : INeedInitialization
            {
                public void Customize(BusConfiguration configuration)
                {
                    configuration.Pipeline.Register("TestingSendOptionsExtension", typeof(TestingSendOptionsExtensionBehavior), "Crazy extensions");
                }
            }
        }
    }
}
