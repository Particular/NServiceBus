namespace NServiceBus.Core.Tests.Serializers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Serialization;
    using Unicast.Messages;
    using Conventions = NServiceBus.Conventions;

    public class SerializeMessagesBehaviorTests
    {
        [Test]
        public async Task Should_set_content_type_header()
        {
            var registry = new MessageMetadataRegistry(new Conventions());

            registry.RegisterMessageType(typeof(MyMessage));

            var context = ContextHelpers.GetOutgoingContext(new MyMessage());
            var behavior = new SerializeMessageConnector(new FakeSerializer("myContentType"), registry);

            await behavior.Invoke(context, c => Task.FromResult(0));

            Assert.AreEqual("myContentType", context.GetOrCreate<OutgoingPhysicalToRoutingConnector.State>().Headers[Headers.ContentType]);
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

            public string ContentType { get; }
        }

        class MyMessage
        {
        }
    }
}