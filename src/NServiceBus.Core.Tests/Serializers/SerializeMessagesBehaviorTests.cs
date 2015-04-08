namespace NServiceBus.Core.Tests.Serializers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Serialization;
    using NServiceBus.Unicast;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;

    public class SerializeMessagesBehaviorTests
    {
        [Test]
        public void Should_set_content_type_header()
        {
            var behavior = new SerializeMessagesBehavior(new FakeSerializer("myContentType"));
          
            var context = new OutgoingContext(null, new SendMessageOptions("test"), new LogicalMessage(new MessageMetadata(),null,null),new Dictionary<string, string>(),"msg id",MessageIntentEnum.Send);

            behavior.Invoke(context, c =>
            {
                
            });

            Assert.AreEqual("myContentType", context.Headers[Headers.ContentType]);
        }

        public class FakeSerializer : IMessageSerializer
        {
            public FakeSerializer(string contentType)
            {
                ContentType = contentType;
            }

            public void Serialize(object message, Stream stream)
            {
                
            }

            public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
            {
                throw new NotImplementedException();
            }

            public string ContentType { get; private set; }
        }
    }
}