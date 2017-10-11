namespace NServiceBus.Core.Tests.Causation
{
    using System;
    using NServiceBus.Features;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class CustomConversationIdStrategyTests
    {
        [Test]
        public void Should_not_allow_null_or_empty_conversation_id()
        {
            Assert.Throws<Exception>(() => Invoke(_ => ConversationId.Custom(null)));
            Assert.Throws<Exception>(() => Invoke(_ => ConversationId.Custom("")));
        }

        [Test]
        public void Should_not_allow_returning_null()
        {
            Assert.Throws<Exception>(() => Invoke(_ => null));
        }

        [Test]
        public void Should_wrap_exceptions_for_better_debugging()
        {
            var ex = Assert.Throws<Exception>(() => Invoke(_ => throw new Exception("User invocation failed")));

            StringAssert.Contains("Failed to execute CustomConversationIdStrategy", ex.Message);
        }

        [Test]
        public void Should_default_to_combguid_id()
        {
            Assert.True(Guid.TryParse(Invoke(_ => ConversationId.Default), out var _));
        }

        string Invoke(Func<ConversationIdStrategyContext, ConversationId> strategy)
        {
            return MessageCausation.WrapUserDefinedInvocation(strategy)(new TestableOutgoingLogicalMessageContext());
        }
    }
}