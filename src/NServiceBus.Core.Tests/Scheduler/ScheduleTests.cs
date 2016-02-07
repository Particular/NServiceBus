namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduleTests
    {
        const string ACTION_NAME = "my action";
        DefaultScheduler scheduler;
        IMessageSession session;

        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler();
            session = new FakeMessageSession(scheduler);
        }

        [Test]
        public async Task When_scheduling_an_action_with_a_name_the_task_should_get_that_name()
        {
            await session.ScheduleEvery(TimeSpan.FromMinutes(5), ACTION_NAME, c => TaskEx.CompletedTask);

            Assert.That(EnsureThatNameExists(ACTION_NAME));
        }

        [Test]
        public async Task When_scheduling_an_action_without_a_name_the_task_should_get_the_DeclaringType_as_name()
        {
            await session.ScheduleEvery(TimeSpan.FromMinutes(5), c => TaskEx.CompletedTask);

            Assert.That(EnsureThatNameExists("ScheduleTests"));
        }

        bool EnsureThatNameExists(string name)
        {
            return scheduler.scheduledTasks.Any(task => task.Value.Name.Equals(name));
        }

        class FakeMessageSession : IMessageSession
        {
            readonly DefaultScheduler defaultScheduler;

            public FakeMessageSession(DefaultScheduler defaultScheduler)
            {
                this.defaultScheduler = defaultScheduler;
            }

            public Task Send(object message, SendOptions options)
            {
                defaultScheduler.Schedule(options.Context.Get<ScheduleBehavior.State>().TaskDefinition);
                return TaskEx.CompletedTask;
            }

            public Task Send<T>(Action<T> messageConstructor, SendOptions options)
            {
                throw new NotImplementedException();
            }

            public Task Publish(object message, PublishOptions options)
            {
                throw new NotImplementedException();
            }

            public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
            {
                throw new NotImplementedException();
            }

            public Task Subscribe(Type eventType, SubscribeOptions options)
            {
                throw new NotImplementedException();
            }

            public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
            {
                throw new NotImplementedException();
            }
        }
    }
}