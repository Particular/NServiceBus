namespace NServiceBus.AcceptanceTests.Serialization
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus.Pipeline;
    using NUnit.Framework;

    public class When_xml_serializer_process_message_without_type_header : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_work_in_unobtrusive()
        {
            var expectedData = 1;

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.When(s => s.SendLocal(new MyMessage
                {
                    Data = expectedData
                })))
                .Done(c => c.WasCalled)
                .Run();

            Assert.AreEqual(expectedData, context.Data);
        }


        public class Context : ScenarioContext
        {
            public bool WasCalled { get; set; }
            public int Data { get; set; }
        }

        public class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningMessagesAs(t => t == typeof(MyMessage));
                    c.Pipeline.Register(typeof(RemoveTheTypeHeader), "Removes the message type header to simulate receiving a native message");
                    c.UseSerialization<XmlSerializer>();
                });
            }

            public class MyMessageHandler : IHandleMessages<MyMessage>
            {
                public Context Context { get; set; }

                public Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    Context.Data = message.Data;
                    Context.WasCalled = true;
                    return Task.FromResult(0);
                }
            }

            public class RemoveTheTypeHeader : Behavior<IDispatchContext>
            {
                public override Task Invoke(IDispatchContext context, Func<Task> next)
                {
                    foreach (var op in context.Operations)
                    {
                        op.Message.Headers.Remove(Headers.EnclosedMessageTypes);
                    }
                    return next();
                }
            }
        }

        public class MyMessage
        {
            public int Data { get; set; }
        }
    }
}