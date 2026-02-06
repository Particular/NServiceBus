namespace NServiceBus.AcceptanceTests.Serialization;

using System;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_xml_serializer_processes_message_without_type_header : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_work_in_unobtrusive()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Sender>(c => c.When(s => s.SendLocal(new MessageToBeDetectedByRootNodeName())))
            .Run();

        Assert.That(context.WasCalled, Is.True);
    }

    public class Context : ScenarioContext
    {
        public bool WasCalled { get; set; }
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningMessagesAs(t => t == typeof(MessageToBeDetectedByRootNodeName));
                    c.Pipeline.Register(typeof(RemoveTheTypeHeader), "Removes the message type header to simulate receiving a native message");
                    c.UseSerialization<XmlSerializer>();
                })
                //Need to include the message since it can't be nested inside the test class, see below
                .IncludeType<MessageToBeDetectedByRootNodeName>();

        [Handler]
        public class MyMessageHandler(Context testContext) : IHandleMessages<MessageToBeDetectedByRootNodeName>
        {
            public Task Handle(MessageToBeDetectedByRootNodeName message, IMessageHandlerContext context)
            {
                testContext.WasCalled = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
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
}

//Can't be nested inside the test class since the xml serializer can't deal with nested types
public class MessageToBeDetectedByRootNodeName
{
    public int Data { get; set; }
}