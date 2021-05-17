namespace NServiceBus.Core.Tests.DataBus
{
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    class When_applying_the_databus_message_mutator_to_null_properties
    {
        [Test]
        public async Task Should_not_blow_up()
        {
            var context = new TestableOutgoingLogicalMessageContext
            {
                Message = new OutgoingLogicalMessage(typeof(MessageWithNullDataBusProperty), new MessageWithNullDataBusProperty())
            };

            var sendBehavior = new DataBusSendBehavior(null, new XmlDataBusSerializer<string>(), new Conventions());

            using (var stream = new MemoryStream())
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(string));
                serializer.Serialize(stream, "test");
                stream.Position = 0;

                await sendBehavior.Invoke(context, ctx => Task.CompletedTask);
            }
        }
    }
}