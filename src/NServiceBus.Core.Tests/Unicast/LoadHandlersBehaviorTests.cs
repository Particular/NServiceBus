namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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
                new LogicalMessage(new MessageMetadata(typeof(string)),null, null),new Dictionary<string, string>(),typeof(MyMessage),  null);

            Assert.Throws<InvalidOperationException>(async () => await behavior.Invoke(context, c => Task.FromResult(true)));
        }

        class MyMessage { }
    }
}