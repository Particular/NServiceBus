﻿namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Pipeline.Contexts;
    using Unicast.Messages;
    using NUnit.Framework;

    [TestFixture]
    public class LoadHandlersBehaviorTests
    {
        [Test]
        public void Should_throw_when_there_are_no_registered_message_handlers()
        {
            var behavior = new LoadHandlersConnector(new MessageHandlerRegistry(new Conventions()));

            var context = new LogicalMessageProcessingContext(
                new LogicalMessage(new MessageMetadata(typeof(string)), null, null), 
                "messageId",
                "replyToAddress",
                new Dictionary<string, string>(), 
                null);

            Assert.Throws<InvalidOperationException>(async () => await behavior.Invoke(context, c => Task.FromResult(0)));
        }
    }
}