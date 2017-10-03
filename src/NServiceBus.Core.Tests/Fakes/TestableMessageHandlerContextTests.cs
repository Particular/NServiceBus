namespace NServiceBus.Testing.Tests.Fakes
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class TestableMessageHandlerContextTests
    {
        [Test]
        public async Task Send_ShouldContainMessageInSentMessages()
        {
            var context = new TestableMessageHandlerContext();
            var messageInstance = new TestMessage();
            var sendOptions = new SendOptions();

            await context.Send(messageInstance, sendOptions);

            Assert.AreEqual(1, context.SentMessages.Length);
            Assert.AreSame(messageInstance, context.SentMessages[0].Message);
            Assert.AreSame(sendOptions, context.SentMessages[0].Options);
        }

        [Test]
        public async Task Send_ShouldInvokeMessageInitializer()
        {
            var context = new TestableMessageHandlerContext();

            await context.Send<ITestMessage>(m => m.Value = "initialized value");

            Assert.AreEqual("initialized value", context.SentMessages[0].Message<ITestMessage>().Value);
        }

        [Test]
        public async Task Publish_ShouldContainMessageInPublishedMessages()
        {
            var context = new TestableMessageHandlerContext();
            var messageInstance = new TestMessage();
            var publishOptions = new PublishOptions();

            await context.Publish(messageInstance, publishOptions);

            Assert.AreEqual(1, context.PublishedMessages.Length);
            Assert.AreSame(messageInstance, context.PublishedMessages[0].Message);
            Assert.AreSame(publishOptions, context.PublishedMessages[0].Options);
        }

        [Test]
        public async Task Publish_ShouldInvokeMessageInitializer()
        {
            var context = new TestableMessageHandlerContext();

            await context.Publish<ITestMessage>(m => m.Value = "initialized value");

            Assert.AreEqual("initialized value", context.PublishedMessages[0].Message<ITestMessage>().Value);
        }

        [Test]
        public async Task Reply_ShouldContainMessageInRepliedMessages()
        {
            var context = new TestableMessageHandlerContext();
            var messageInstance = new TestMessage();
            var publishOptions = new ReplyOptions();

            await context.Reply(messageInstance, publishOptions);

            Assert.AreEqual(1, context.RepliedMessages.Length);
            Assert.AreSame(messageInstance, context.RepliedMessages[0].Message);
            Assert.AreSame(publishOptions, context.RepliedMessages[0].Options);
        }

        [Test]
        public async Task Reply_ShouldInvokeMessageInitializer()
        {
            var context = new TestableMessageHandlerContext();

            await context.Reply<ITestMessage>(m => m.Value = "initialized value");

            Assert.AreEqual("initialized value", context.RepliedMessages[0].Message<ITestMessage>().Value);
        }

        [Test]
        public async Task ForwardCurrentMessageTo_ShouldContainDestinationsInForwardDestinations()
        {
            var context = new TestableMessageHandlerContext();

            await context.ForwardCurrentMessageTo("destination1");
            await context.ForwardCurrentMessageTo("destination2");

            Assert.Contains("destination1", context.ForwardedMessages);
            Assert.Contains("destination2", context.ForwardedMessages);
        }

        [Test]
        public void DoNotContinueDispatchingCurrentMessageToHandlers_WhenNotCalled_ShouldNotIndicateInvocation()
        {
            var context = new TestableMessageHandlerContext();

            Assert.IsFalse(context.DoNotContinueDispatchingCurrentMessageToHandlersWasCalled);
        }

        [Test]
        public void DoNotContinueDispatchingCurrentMessageToHandlers_WhenCalled_ShouldIndicateInvocation()
        {
            var context = new TestableMessageHandlerContext();

            context.DoNotContinueDispatchingCurrentMessageToHandlers();

            Assert.IsTrue(context.DoNotContinueDispatchingCurrentMessageToHandlersWasCalled);
        }

        [Test]
        public void ShouldAllowSettingMessageProperties()
        {
            var context = new TestableMessageHandlerContext();

            context.MessageId = "custom message id";
            context.ReplyToAddress = "custom reply address";
            context.MessageHeaders = new Dictionary<string, string>();
            context.MessageHeaders.Add("custom header", "custom value");
            context.Extensions = new ContextBag();
        }

        class TestMessage
        {
        }

        public interface ITestMessage
        {
            string Value { get; set; }
        }
    }
}