namespace NServiceBus.Testing.Samples
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Testing.Fakes;
    using NUnit.Framework;

    [TestFixture]
    public class CustomBehaviorTest
    {
        CustomBehavior testee;

        [SetUp]
        public void SetUp()
        {
            testee = new CustomBehavior();
        }

        [Test]
        public async Task ShouldHandleCurrentMessageLaterWhenHandlerInvocationWasAborted()
        {
            var context = new TestableInvokeHandlerContext();

            await testee.Invoke(context, ctx =>
            {
                ctx.DoNotContinueDispatchingCurrentMessageToHandlers();
                return Task.CompletedTask;
            });

            Assert.IsTrue(context.HandleCurrentMessageLaterCalled);
        }

        [Test]
        public async Task ShouldSendMessage()
        {
            var context = new TestableInvokeHandlerContext();

            await testee.Invoke(context, () => Task.CompletedTask);

            Assert.AreEqual(1, context.Sent.Count);
            var sentMessage = context.Sent.Single();
            Assert.IsAssignableFrom<SendMessage>(sentMessage.Message);

            //TODO Testing: State not accessible. Need to provide testing accessors like options.GetRequestedDelay()
            //Assert.AreEqual(TimeSpan.FromSeconds(42), sentMessage.Options.GetExtensions().Get<ApplyDelayedDeliveryConstraintBehavior.State>().RequestedDelay);

            //TODO Testing: provide testing accessor like options.GetHeaders() since options.OutgoingHeaders are internal
            //Assert.Contains("someHeader", sentMessage.Options.OutgoingHeaders);
        }

        //TODO tests which handle exceptions or similar.
    }

    #region testee

    public class CustomBehavior : Behavior<InvokeHandlerContext>
    {
        public override async Task Invoke(InvokeHandlerContext context, Func<Task> next)
        {
            var sendOptions = new SendOptions();
            sendOptions.DelayDeliveryWith(TimeSpan.FromSeconds(42));
            sendOptions.SetDestination("destinationQ");
            sendOptions.SetHeader("someHeader", "someValue");
            await context.SendAsync(new SendMessage(), sendOptions);

            //context.ScheduleEvery();
            //context.SendLocalAsync()
            //context.PublishAsync()
            //context.SubscribeAsync()
            //context.UnsubscribeAsync()
            //context.ForwardCurrentMessageToAsync()
            //context.ReplyAsync()
            //context.Extensions

            await next();

            if (context.HandlerInvocationAborted)
            {
                await context.HandleCurrentMessageLaterAsync();
            }
        }
    }

    public class SendMessage : ICommand
    {
    }

    #endregion
}