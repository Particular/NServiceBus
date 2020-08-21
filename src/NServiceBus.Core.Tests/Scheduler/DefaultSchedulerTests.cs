namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class DefaultSchedulerTests
    {
        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler();
        }

        [Test]
        public async Task When_starting_a_task_defer_should_be_called()
        {
            var task = new TaskDefinition
            {
                Every = TimeSpan.FromSeconds(5),
                Task = c => Task.CompletedTask
            };
            var taskId = task.Id;

            scheduler.Schedule(task);

            await scheduler.Start(taskId, handlingContext);

            Assert.That(handlingContext.SentMessages.Any(message => message.Options.GetDeliveryDelay().HasValue));
        }

        [Test]
        public async Task When_starting_a_task_the_lambda_should_be_executed()
        {
            var i = 1;

            var task = new TaskDefinition
            {
                Every = TimeSpan.FromSeconds(5),
                Task = c =>
                {
                    i++;
                    return Task.CompletedTask;
                }
            };
            var taskId = task.Id;

            scheduler.Schedule(task);
            await scheduler.Start(taskId, handlingContext);

            Assert.That(i == 2);
        }

        TestableMessageHandlerContext handlingContext = new TestableMessageHandlerContext();
        DefaultScheduler scheduler;
    }
}