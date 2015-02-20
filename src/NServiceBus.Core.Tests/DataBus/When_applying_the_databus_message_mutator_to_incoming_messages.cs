namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;
    using Rhino.Mocks;
    using Unicast.Messages;

    [TestFixture]
    class When_applying_the_databus_message_mutator_to_incoming_messages : on_the_bus
    {
        [Test]
        public void Incoming_databus_properties_should_be_hydrated()
        {
            var propertyKey = Guid.NewGuid().ToString();
            var databusKey = Guid.NewGuid().ToString();

            var message = new LogicalMessage(null, new MessageWithDataBusProperty
                              {
                                  DataBusProperty = new DataBusProperty<string>("not used in this test")
                                  {
                                      Key = propertyKey
                                  }
                              }, new Dictionary<string, string> { { "NServiceBus.DataBus." + propertyKey, databusKey } }, null);
                
            
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, "test");
                stream.Position = 0;

                dataBus.Stub(s => s.Get(databusKey)).Return(stream);

                receiveBehavior.Invoke(new LogicalMessageProcessingStageBehavior.Context(message,null), () => { });
            }

            var instance = (MessageWithDataBusProperty)message.Instance;

            Assert.AreEqual(instance.DataBusProperty.Value, "test");
        }

    }
}