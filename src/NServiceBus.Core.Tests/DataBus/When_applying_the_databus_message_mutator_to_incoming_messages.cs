namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using NUnit.Framework;
    using Rhino.Mocks;

    [TestFixture]
    public class When_applying_the_databus_message_mutator_to_incoming_messages : on_the_bus
    {
        [Test]
        public void Incoming_databus_properties_should_be_hydrated()
        {
            var message = new MessageWithDataBusProperty
                              {
                                  DataBusProperty = new DataBusProperty<string>("not used in this test")
                              };
            message.DataBusProperty.Key = Guid.NewGuid().ToString();

            var databusKey = Guid.NewGuid().ToString();

            message.SetHeader("NServiceBus.DataBus." + message.DataBusProperty.Key, databusKey);
                
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, "test");
                stream.Position = 0;

                dataBus.Stub(s => s.Get(databusKey)).Return(stream);

                message = (MessageWithDataBusProperty) incomingMutator.MutateIncoming(message);
            }
            Assert.AreEqual(message.DataBusProperty.Value, "test");
        }

    }
}