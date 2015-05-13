namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using NServiceBus.DataBus;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    class When_applying_the_databus_message_mutator_to_incoming_messages
    {
        [Test]
        public async void Incoming_databus_properties_should_be_hydrated()
        {
            var propertyKey = Guid.NewGuid().ToString();
            var databusKey = Guid.NewGuid().ToString();

            var message = new LogicalMessage(null, new MessageWithDataBusProperty
                              {
                                  DataBusProperty = new DataBusProperty<string>("not used in this test")
                                  {
                                      Key = propertyKey
                                  }
                              }, null);

            var fakeDatabus = new FakeDataBus();
            var receiveBehavior = new DataBusReceiveBehavior
            {
                DataBus = fakeDatabus,
                DataBusSerializer = new DefaultDataBusSerializer(),
                Conventions = new Conventions(),
            };

            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, "test");
                stream.Position = 0;

                fakeDatabus.StreamsToReturn[databusKey] = stream;

              
                await receiveBehavior.Invoke(new LogicalMessageProcessingStageBehavior.Context(message,new Dictionary<string, string> { { "NServiceBus.DataBus." + propertyKey, databusKey } },null,null), () => Task.FromResult(true));
            }

            var instance = (MessageWithDataBusProperty)message.Instance;

            Assert.AreEqual(instance.DataBusProperty.Value, "test");
        }

        class FakeDataBus:IDataBus
        {
            public Dictionary<string,Stream> StreamsToReturn = new Dictionary<string, Stream>();
            public Stream Get(string key)
            {
                return StreamsToReturn[key];
            }

            public string Put(Stream stream, TimeSpan timeToBeReceived)
            {
                throw new NotImplementedException();
            }

            public void Start()
            {
                throw new NotImplementedException();
            }
        }

    }
}