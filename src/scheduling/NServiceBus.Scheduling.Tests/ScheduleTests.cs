using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;

namespace NServiceBus.Scheduling.Tests
{
    [TestFixture]
    public class ScheduleTests
    {
        private const string ACTION_NAME = "my action";
        private FuncBuilder _builder = new FuncBuilder();
        private FakeBus _bus = new FakeBus();

        private readonly InMemoryScheduledTaskStorage _taskStorage = new InMemoryScheduledTaskStorage();        

        [SetUp]
        public void SetUp()
        {
            Configure.With(new Assembly[0]);
            Configure.Instance.Builder = _builder;

            _builder.Register<IBus>(() => _bus);
            _builder.Register<IScheduledTaskStorage>(() => _taskStorage);
            _builder.Register<IScheduler>(() => new DefaultScheduler(_bus, _taskStorage));
        }

        [Test]
        public void When_scheduling_an_action_with_a_name_the_task_should_get_that_name()
        {
            Schedule.Every(TimeSpan.FromMinutes(5)).Action(ACTION_NAME, () => { });
            Assert.That(EnsureThatNameExists(ACTION_NAME));
        }

        [Test]
        public void When_scheduling_an_action_without_a_name_the_task_should_get_the_DeclaringType_as_name()
        {
            Schedule.Every(TimeSpan.FromMinutes(5)).Action(() => {  });
            Assert.That(EnsureThatNameExists("ScheduleTests"));
        }

        [Test]
        public void Schedule_tasks_using_mutiple_threads()
        {
            var threads = new Thread[20];
            
            ScheduleTaskThreads(threads);

            threads = new Thread[20];
            
            _bus.DeferWasCalled = 0;
            InvokeHandlerThreads(threads);

            Assert.That(_bus.DeferWasCalled, Is.EqualTo(20));
        }

        private bool EnsureThatNameExists(string name)
        {
            return _taskStorage.Tasks.Where(task => task.Value.Name.Equals(name)).Any();
        }

        private static void ScheduleTaskThreads(IList<Thread> threads)
        {
            for (var i=0; i < threads.Count; i++)
            {
                threads[i] = new Thread(() => Schedule.Every(TimeSpan.FromSeconds(1)).Action(() => {}));
            }

            StartJoin(threads);
        }

        private void InvokeHandlerThreads(IList<Thread> threads)
        {
            var j = 0;

            foreach (var task in _taskStorage.Tasks)
            {
                var t = task;
                threads[j] = new Thread(() => new ScheduledTaskMessageHandler(new DefaultScheduler(_bus, _taskStorage)).Handle(new Messages.ScheduledTask { TaskId = t.Key }));
                j++;
            }

            StartJoin(threads);
        }

        private static void StartJoin(IEnumerable<Thread> threads)
        {
            foreach (var t in threads)
            {
                t.Start();
            }

            foreach (var t in threads)
            {
                t.Join();
            }
        }
    }
}