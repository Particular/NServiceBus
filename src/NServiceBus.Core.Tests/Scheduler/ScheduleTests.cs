namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class ScheduleTests
    {
        const string ACTION_NAME = "my action";
        DefaultScheduler scheduler;
        IBusContext context;

        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler();
            context = new FakeBus(scheduler).CreateBusContext();
        }

        [Test]
        public async Task When_scheduling_an_action_with_a_name_the_task_should_get_that_name()
        {
            await context.ScheduleEvery(TimeSpan.FromMinutes(5), ACTION_NAME, c => TaskEx.Completed);

            Assert.That(EnsureThatNameExists(ACTION_NAME));
        }

        [Test]
        public async Task When_scheduling_an_action_without_a_name_the_task_should_get_the_DeclaringType_as_name()
        {
            await context.ScheduleEvery(TimeSpan.FromMinutes(5), c => TaskEx.Completed);

            Assert.That(EnsureThatNameExists("ScheduleTests"));
        }

        bool EnsureThatNameExists(string name)
        {
            return scheduler.scheduledTasks.Any(task => task.Value.Name.Equals(name));
        }

        class FakeBus : IBusInterface
        {
            readonly DefaultScheduler defaultScheduler;
            public readonly List<Tuple<object, SendOptions>> SentMessages = new List<Tuple<object, SendOptions>>();

            public FakeBus(DefaultScheduler defaultScheduler)
            {
                this.defaultScheduler = defaultScheduler;
            }
            public IBusContext CreateBusContext()
            {
                return new FakeBusContext(defaultScheduler, SentMessages);
            }

            class FakeBusContext : IBusContext
            {
                readonly DefaultScheduler defaultScheduler;
                readonly List<Tuple<object, SendOptions>> sentMessages;

                public FakeBusContext(DefaultScheduler defaultScheduler, List<Tuple<object, SendOptions>> sentMessages)
                {
                    this.defaultScheduler = defaultScheduler;
                    this.sentMessages = sentMessages;
                }

                public ContextBag Extensions => null;
                public Task Send(object message, SendOptions options)
                {
                    sentMessages.Add(Tuple.Create(message, options));
                    defaultScheduler.Schedule(options.Context.Get<ScheduleBehavior.State>().TaskDefinition);
                    return Task.FromResult(0);
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
}