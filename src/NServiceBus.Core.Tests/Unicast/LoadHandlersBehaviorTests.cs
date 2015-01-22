namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Messages;
    using NUnit.Framework;

    [TestFixture]
    public class LoadHandlersBehaviorTests
    {
        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(new Conventions()));

            var context = new LogicalMessageProcessingStageBehavior.Context(
                new LogicalMessage(new MessageMetadata(typeof(string)),null, new Dictionary<string, string>(), null), null);

            Assert.Throws<InvalidOperationException>(() => behavior.Invoke(context, c => { }));
        }
    }
}