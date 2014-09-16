namespace NServiceBus.Core.Tests.DataBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;
    using Unicast;
    using Unicast.Messages;

    [TestFixture]
    class When_applying_the_databus_message_mutator_to_null_properties : on_the_bus
    {
        [Test]
        public void Should_not_blow_up()
        {
            var metadata = new MessageMetadata(timeToBeReceived: TimeSpan.FromDays(1));
            var message = new LogicalMessage(metadata, new MessageWithNullDataBusProperty(), new Dictionary<string, string>(), null);
            var context = new OutgoingContext(null,new SendOptions(Address.Parse("MyEndpoint")), message);

            
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, "test");
                stream.Position = 0;

                sendBehavior.Invoke(context, () => { });            
            }
        }

    }
}