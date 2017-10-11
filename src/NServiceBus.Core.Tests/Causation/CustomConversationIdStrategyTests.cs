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

        void Invoke(Func<ConversationIdStrategyContext, ConversationId> strategy)
        {
            MessageCausation.WrapUserDefinedInvocation(strategy)(new TestableOutgoingLogicalMessageContext());
        }
    }
}