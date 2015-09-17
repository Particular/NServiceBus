﻿namespace NServiceBus.AcceptanceTests.ApiExtension
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Extensibility;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;

    public class When_extending_sendoptions : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_able_to_set_context_items_and_retrieve_it_via_a_behavior()
        {
            var context = await Scenario.Define<Context>()
                    .WithEndpoint<SendOptionsExtensions>(b => b.Given((bus, c) =>
                    {
                        var options = new SendOptions();

                        options.GetExtensions().Set(new SendOptionsExtensions.TestingSendOptionsExtensionBehavior.Context { SomeValue = "I did it" });
                        options.RouteToLocalEndpointInstance();

                        return bus.SendAsync(new SendMessage(), options);
                    }))
                    .Done(c => c.WasCalled)
                    .Run();

            Assert.AreEqual("I did it", context.Secret);
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
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register("TestingSendOptionsExtension", typeof(TestingSendOptionsExtensionBehavior), "Testing send options extensions"));
            }

            class SendMessageHandler : IHandleMessages<SendMessage>
            {
                public Context Context { get; set; }

                public Task Handle(SendMessage message)
                {
                    Context.Secret = message.Secret;
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }

            public class TestingSendOptionsExtensionBehavior : Behavior<OutgoingContext>
            {
                public override Task Invoke(OutgoingContext context, Func<Task> next)
                {
                    Context data;
                    if (context.TryGet(out data))
                    {
                        context.UpdateMessageInstance(new SendMessage { Secret = data.SomeValue });
                    }

                    return next();
                }

                public class Context
                {
                    public string SomeValue { get; set; }
                }
            }
        }

        [Serializable]
        public class SendMessage : ICommand
        {
            public string Secret { get; set; }
        }
    }
}
