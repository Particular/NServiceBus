namespace NServiceBus.Core.Tests.Pipeline.Outgoing
{
    using System;
    using System.Collections.Generic;
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

            var context = new OutgoingLogicalMessageContext("message1234", new Dictionary<string, string>(), new OutgoingLogicalMessage(typeof(IMyMessage), message), null, null);

            var newMessageId = Guid.NewGuid();
            var newMessage = context.Message.Instance;
            newMessage.GetType().InvokeMember("Id",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                Type.DefaultBinder, newMessage, new object[]
                {
                    newMessageId
                });

            context.UpdateMessage(newMessage);

            Assert.AreEqual(typeof(IMyMessage), context.Message.MessageType);
            Assert.AreEqual(newMessageId, ((IMyMessage)context.Message.Instance).Id);
        }

        [Test]
        public void Updating_the_message_to_a_new_type_should_update_the_MessageType()
        {
            var mapper = new MessageMapper();
            var message = mapper.CreateInstance<IMyMessage>(m => m.Id = Guid.NewGuid());

            var context = new OutgoingLogicalMessageContext("message1234", new Dictionary<string, string>(), new OutgoingLogicalMessage(typeof(IMyMessage), message), null, null);

            var differentMessage = new MyDifferentMessage
            {
                Id = Guid.NewGuid()
            };

            context.UpdateMessage(differentMessage);

            Assert.AreEqual(typeof(MyDifferentMessage), context.Message.MessageType);
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
}