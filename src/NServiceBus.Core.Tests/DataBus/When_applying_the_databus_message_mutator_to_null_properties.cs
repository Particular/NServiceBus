namespace NServiceBus.Core.Tests.DataBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using NUnit.Framework;

    [TestFixture]
    public class When_applying_the_databus_message_mutator_to_null_properties : on_the_bus
    {
        [Test]
        public void Should_not_blow_up()
        {
            var message = new MessageWithNullDataBusProperty();

            outgoingMutator.MutateOutgoing(message); 
            
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, "test");
                stream.Position = 0;
                
                incomingMutator.MutateIncoming(message);
            }

            
        }

    }
}