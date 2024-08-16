namespace NServiceBus.Core.Tests.Pipeline.Outgoing;

using System;
using System.Reflection;
using MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.Pipeline;
using NUnit.Framework;

[TestFixture]
public class OutgoingLogicalMessageContextTests
{
    [Test]
    public void Updating_the_message_proxy_instance_with_a_new_property_value_should_retain_the_original_interface_type()
    {
        var mapper = new MessageMapper();
        var message = mapper.CreateInstance<IMyMessage>(m => m.Id = Guid.NewGuid());

        var context = new OutgoingLogicalMessageContext("message1234", [], new OutgoingLogicalMessage(typeof(IMyMessage), message), null, new FakeRootContext());

        var newMessageId = Guid.NewGuid();
        var newMessage = context.Message.Instance;
        newMessage.GetType().InvokeMember("Id",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
            Type.DefaultBinder, newMessage, new object[]
            {
                newMessageId
            });

        context.UpdateMessage(newMessage);

        Assert.That(context.Message.MessageType, Is.EqualTo(typeof(IMyMessage)));
        Assert.That(((IMyMessage)context.Message.Instance).Id, Is.EqualTo(newMessageId));
    }

    [Test]
    public void Updating_the_message_to_a_new_type_should_update_the_MessageType()
    {
        var mapper = new MessageMapper();
        var message = mapper.CreateInstance<IMyMessage>(m => m.Id = Guid.NewGuid());

        var context = new OutgoingLogicalMessageContext("message1234", [], new OutgoingLogicalMessage(typeof(IMyMessage), message), null, new FakeRootContext());

        var differentMessage = new MyDifferentMessage
        {
            Id = Guid.NewGuid()
        };

        context.UpdateMessage(differentMessage);

        Assert.That(context.Message.MessageType, Is.EqualTo(typeof(MyDifferentMessage)));
    }

    class MyDifferentMessage
    {
        public Guid Id { get; set; }
    }

    //public required for proxy magic to happen
    public interface IMyMessage
    {
        Guid Id { get; set; }
    }
}