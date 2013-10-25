namespace NServiceBus.Unicast.Tests
{
    using System;
    using NUnit.Framework;
    using Unicast.Messages;

    [TestFixture]
	public class DefaultMessageRegistryTests
	{
		[TestFixture]
		public class When_getting_message_definition
		{
			[Test]
			public void Should_throw_an_exception_for_a_unmapped_type()
			{
				var defaultMessageRegistry = new MessageMetadataRegistry();
				Assert.Throws<Exception>(() => defaultMessageRegistry.GetMessageDefinition(typeof (int)));
			}

			[Test]
			public void Should_return_metadata_for_a_mapped_type()
			{
				var defaultMessageRegistry = new MessageMetadataRegistry();
				defaultMessageRegistry.RegisterMessageType(typeof(int));
				var messageMetadata = defaultMessageRegistry.GetMessageDefinition(typeof (int));
				Assert.AreEqual(typeof(int),messageMetadata.MessageType);
			}
		}
	}
}