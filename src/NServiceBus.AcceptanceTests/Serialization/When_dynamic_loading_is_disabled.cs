namespace NServiceBus.AcceptanceTests.Serialization;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using NServiceBus.Pipeline;
using NUnit.Framework;

public class When_dynamic_loading_is_disabled : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_not_load_type_dynamically()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ReceivingEndpoint>(e => e
                .DoNotFailOnErrorMessages()
                .When(session => session.SendLocal(new Message()))
            )
            .Run();

        Assert.That(context.FailedMessages.Single().Value, Has.Count.EqualTo(1));
        Exception exception = context.FailedMessages.Single().Value.Single().Exception;
        Assert.That(exception, Is.InstanceOf<MessageDeserializationException>());
        Assert.That(exception.InnerException.Message, Is.EqualTo($"Could not determine the message type from the '{Headers.EnclosedMessageTypes}' header and message type inference from the message body has been disabled. Ensure the header is set or enable message type inference."));
    }

    class Context : ScenarioContext
    {
        public bool MessageReceived { get; set; }
    }

    class ReceivingEndpoint : EndpointConfigurationBuilder
    {
        public ReceivingEndpoint() =>
            EndpointSetup<DefaultServer>(cfg =>
            {
                cfg.Pipeline.Register(typeof(PatchEnclosedMessageTypeHeader), "Patches the EnclosedMessageTypeHeader to contain a type that requires Type.GetType to be invoked.");
                var serializerSettings = cfg.UseSerialization<XmlSerializer>();
                serializerSettings.DisableDynamicTypeLoading();
                serializerSettings.DisableMessageTypeInference(); // just throw when we can't find the message type
            });

        class PatchEnclosedMessageTypeHeader(Context testContext) : Behavior<IIncomingPhysicalMessageContext>
        {
            public override Task Invoke(IIncomingPhysicalMessageContext context, Func<Task> next)
            {
                testContext.MessageReceived = true;
                context.Message.Headers[Headers.EnclosedMessageTypes] = typeof(PatchMessage).FullName;
                testContext.MarkAsCompleted();
                return next();
            }
        }
    }

    public class Message : IMessage;

    public class PatchMessage : IMessage;
}