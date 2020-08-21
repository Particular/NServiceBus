namespace NServiceBus.Core.Tests.DataBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
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
            var context = new TestableOutgoingLogicalMessageContext();
            context.Message = new OutgoingLogicalMessage(typeof(MessageWithNullDataBusProperty), new MessageWithNullDataBusProperty());

            var sendBehavior = new DataBusSendBehavior(null, new DefaultDataBusSerializer(), new Conventions());

            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, "test");
                stream.Position = 0;

                await sendBehavior.Invoke(context, ctx => Task.CompletedTask);
            }
        }

    }
}