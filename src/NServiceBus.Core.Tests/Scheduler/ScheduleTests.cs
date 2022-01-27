#pragma warning disable 618
namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;

    [TestFixture]
    public class ScheduleTests
    {
        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler();
            session = new FakeMessageSession(scheduler);
        }

        [Test]
        public async Task When_scheduling_an_action_with_a_name_the_task_should_get_that_name()
        {
            var wasCalled = true;
            await session.ScheduleEvery(TimeSpan.FromMinutes(5), ACTION_NAME, c =>
            {
                wasCalled = true;
                return TaskEx.CompletedTask;
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(ACTION_NAME, session.ScheduledDefinition.Name);
        }

        [Test]
        public async Task When_scheduling_an_action_without_a_name_the_task_should_get_the_DeclaringType_as_name()
        {
            var wasCalled = true;
            await session.ScheduleEvery(TimeSpan.FromMinutes(5), c =>
            {
                wasCalled = true;
                return TaskEx.CompletedTask;
            });

            Assert.IsTrue(wasCalled);
            Assert.AreEqual(nameof(ScheduleTests), session.ScheduledDefinition.Name);
        }

        DefaultScheduler scheduler;
        FakeMessageSession session;
        const string ACTION_NAME = "my action";

        class FakeMessageSession : IMessageSession
        {
            public FakeMessageSession(DefaultScheduler defaultScheduler)
            {
                this.defaultScheduler = defaultScheduler;
            }

            public TaskDefinition ScheduledDefinition { get; private set; }

            public Task Send(object message, SendOptions options)
            {
                ScheduledDefinition = options.Context.Get<ScheduleBehavior.State>().TaskDefinition;
                defaultScheduler.Schedule(ScheduledDefinition);
                return defaultScheduler.Start(ScheduledDefinition.Id, new TestablePipelineContext());
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

            readonly DefaultScheduler defaultScheduler;
        }
    }
}
#pragma warning restore 618