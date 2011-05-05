using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.DataBus.Tests
{
	using System;

    [TestFixture]
    public class When_applying_the_databus_message_mutator_to_outgoing_messages : on_the_bus
    {

        [Test]
        public void Outgoing_databus_properties_should_be_dehydrated()
        {

            var message = new MessageWithDataBusProperty
                              {
								  DataBusProperty = new DataBusProperty<string>("test")
                              };

		
            outgoingMutator.MutateOutgoing(message);

            dataBus.AssertWasCalled(
                x => x.Put(Arg<Stream>.Is.Anything, Arg<TimeSpan>.Is.Equal(TimeSpan.MaxValue)));
        }

		[Test]
		public void Time_to_live_should_be_passed_on_the_databus()
		{

			var message = new MessageWithExplicitTimeToLive()
			{
				DataBusProperty = new DataBusProperty<string>("test")
			};


			outgoingMutator.MutateOutgoing(message);

			dataBus.AssertWasCalled(
				x => x.Put(Arg<Stream>.Is.Anything, Arg<TimeSpan>.Is.Equal(TimeSpan.FromMinutes(1))));
		}


    }

    [TestFixture]
	public class When_applying_the_databus_message_mutator_to_incoming_messages:on_the_bus
	{
		

		[Test]
		public void Incoming_databus_properties_should_be_hydrated()
		{
			var message = new MessageWithDataBusProperty
			{
				DataBusProperty = new DataBusProperty<string>("not used in this test")
			};

			using (var stream = new MemoryStream())
			{
				new BinaryFormatter().Serialize(stream, "test");
				stream.Position = 0;

				dataBus.Stub(s => s.Get(message.DataBusProperty.Key)).Return(stream);

				message = (MessageWithDataBusProperty)incomingMutator.MutateIncoming(message);
			}
			Assert.AreEqual(message.DataBusProperty.Value, "test");
		}

	}

    public class MessageWithDataBusProperty : IMessage
    {
        public DataBusProperty<string> DataBusProperty { get; set; }
    }

	[TimeToBeReceived("00:01:00")]
	public class MessageWithExplicitTimeToLive : IMessage
	{
		public DataBusProperty<string> DataBusProperty { get; set; }
	}
}