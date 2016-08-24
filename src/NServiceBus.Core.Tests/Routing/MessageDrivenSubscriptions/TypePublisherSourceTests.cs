namespace NServiceBus.Core.Tests.Routing
{
    using System.Linq;
    using NServiceBus.Routing.MessageDrivenSubscriptions;
    using NUnit.Framework;

    [TestFixture]
    public class TypePublisherSourceTests
    {
        [Test]
        public void It_throws_if_specified_type_is_not_a_message()
        {
            var source = new TypePublisherSource(typeof(NonMessage), PublisherAddress.CreateFromEndpointName("Destination"));
            Assert.That(() => source.Generate(new Conventions()).ToArray(), Throws.Exception.Message.Contains("it is not considered a message"));
        }

        [Test]
        public void It_throws_if_specified_type_is_not_an_event()
        {
            var source = new TypePublisherSource(typeof(NonEvent), PublisherAddress.CreateFromEndpointName("Destination"));
            Assert.That(() => source.Generate(new Conventions()).ToArray(), Throws.Exception.Message.Contains("it is not considered an event"));
        }

        class NonMessage
        {
        }

        class NonEvent : IMessage
        {
        }
    }
}