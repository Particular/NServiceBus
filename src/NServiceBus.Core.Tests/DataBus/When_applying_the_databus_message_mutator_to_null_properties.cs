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

            var serializer = new SystemJsonDataBusSerializer();
            var sendBehavior = new DataBusSendBehavior(null, serializer, new Conventions());

            using (var stream = new MemoryStream())
            {
                serializer.Serialize("test", stream);
                stream.Position = 0;

                await sendBehavior.Invoke(context, ctx => Task.CompletedTask);
            }
        }
    }
}