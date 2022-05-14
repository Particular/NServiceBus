namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.DataBus;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Unicast.Messages;

    [TestFixture]
    class When_applying_the_databus_message_mutator_to_incoming_messages
    {
        [Test]
        public async Task Incoming_databus_properties_should_be_hydrated()
        {
            var propertyKey = Guid.NewGuid().ToString();
            var databusKey = Guid.NewGuid().ToString();

            var message = new LogicalMessage(new MessageMetadata(typeof(MessageWithDataBusProperty)), new MessageWithDataBusProperty
            {
                DataBusProperty = new DataBusProperty<string>("not used in this test")
                {
                    Key = propertyKey
                }
            });

            var fakeDatabus = new FakeDataBus();
            var serializer = new SystemJsonDataBusSerializer();
            var receiveBehavior = new DataBusReceiveBehavior(fakeDatabus, serializer, new Conventions());

            using (var stream = new MemoryStream())
            {
                serializer.Serialize("test", stream);
                stream.Position = 0;

                fakeDatabus.StreamsToReturn[databusKey] = stream;

                await receiveBehavior.Invoke(
                    new IncomingLogicalMessageContext(
                        message,
                        "messageId",
                        "replyToAddress",
                        new Dictionary<string, string>
                        {
                            {"NServiceBus.DataBus." + propertyKey, databusKey}
                        },
                        null),
                    ctx => Task.CompletedTask);
            }

            var instance = (MessageWithDataBusProperty)message.Instance;

            Assert.AreEqual(instance.DataBusProperty.Value, "test");
        }

        class FakeDataBus : IDataBus
        {
            public Dictionary<string, Stream> StreamsToReturn = new Dictionary<string, Stream>();

            public Task<Stream> Get(string key, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(StreamsToReturn[key]);
            }

            public Task<string> Put(Stream stream, TimeSpan timeToBeReceived, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task Start(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

    }
}