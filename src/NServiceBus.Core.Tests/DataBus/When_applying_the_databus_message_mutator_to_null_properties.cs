namespace NServiceBus.Core.Tests.DataBus
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Conventions = NServiceBus.Conventions;

    [TestFixture]
    class When_applying_the_databus_message_mutator_to_null_properties 
    {
        [Test]
        public async Task Should_not_blow_up()
        {
            var context = ContextHelpers.GetOutgoingContext(new MessageWithNullDataBusProperty());
            var sendBehavior = new DataBusSendBehavior
            {
                DataBus = null,
                Conventions = new Conventions(),
                DataBusSerializer = new DefaultDataBusSerializer(),
            };
            
            using (var stream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(stream, "test");
                stream.Position = 0;

                await sendBehavior.Invoke(context, () => Task.FromResult(0));            
            }
        }

    }
}