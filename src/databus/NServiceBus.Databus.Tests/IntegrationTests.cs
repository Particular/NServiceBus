using System;
using System.Collections.Generic;
using System.IO;
using NServiceBus.DataBus;
using NServiceBus.DataBus.Tests;
using NServiceBus.MessageInterfaces;
using NServiceBus.MessageInterfaces.MessageMapper.Reflection;
using NServiceBus.MessageMutator;
using NServiceBus.Serialization;
using NUnit.Framework;

namespace NServiceBus.Databus.Tests
{
	using Serializers.XML;

	[TestFixture]
	public class IntegrationTests
	{
		private byte[] testBlob = new byte[4];
		string testString = "A looooooong string";
		private IDataBus dataBus;
		private TestMessage testMessage;
		private IMessageMutator messageMutator;

		[SetUp]
		public void SetUp()
		{
			dataBus = new InMemoryDataBus();
			messageMutator = new DataBusMessageMutator(dataBus, new DefaultDatabusSerializer());
			testMessage = new TestMessage
			              	{
			              		Blob = new DataBusProperty<byte[]>(testBlob),
			              		LargeString = new DataBusProperty<string>(testString),
			              		DataBusPropertyWithNullPayload = new DataBusProperty<string>(null)
							};
		}
		[Test]
		public void Using_xml_message_serialization()
		{
			IMessageMapper mapper = new MessageMapper();
			var serializer = new MessageSerializer
								 {
									 MessageMapper = mapper,
									 MessageTypes = new List<Type>(new[] { typeof(TestMessage) })
								 };

			ExecuteAndVerify(serializer);
		}


		[Test]
		public void Using_binary_message_serializer()
		{
			var serializer = new Serializers.Binary.MessageSerializer();

			ExecuteAndVerify(serializer);
		}

		void ExecuteAndVerify(IMessageSerializer serializer)
		{
			var result = InvokeMutators(testMessage, serializer) as TestMessage;


			Assert.AreEqual(result.Blob.Value.Length, 4);

			Assert.AreEqual(result.LargeString.Value, testString);
		}

		private IMessage InvokeMutators(IMessage message, IMessageSerializer serializer)
		{
			var messageToSerialize = messageMutator.MutateOutgoing(message);
			using (var stream = new MemoryStream())
			{
				serializer.Serialize(new[] { messageToSerialize }, stream);

				stream.Position = 0;

				var result = serializer.Deserialize(stream)[0];

				return messageMutator.MutateIncoming(result);
			}
		}
	}

	[Serializable]
	public class TestMessage : IMessage
	{
		public string Whatever { get; set; }
		public DataBusProperty<byte[]> Blob { get; set; }
		public DataBusProperty<string> LargeString { get; set; }
		public DataBusProperty<string> DataBusPropertyThatIsNull { get; set; }
		public DataBusProperty<string> DataBusPropertyWithNullPayload { get; set; }

	}
}