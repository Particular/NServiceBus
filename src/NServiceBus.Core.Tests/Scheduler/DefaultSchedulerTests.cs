﻿namespace NServiceBus.Scheduling.Tests
{
    using System;
    using System.Threading;
    using Core.Tests.Fakes;
    using NUnit.Framework;

    [TestFixture]
    public class DefaultSchedulerTests
    {
        FakeHandlingContext handlingContext = new FakeHandlingContext();
        DefaultScheduler scheduler;

        [SetUp]
        public void SetUp()
        {
            scheduler = new DefaultScheduler();
        }

        [Test]
        public void When_scheduling_a_task_it_should_be_added_to_the_storage()
        {
            var task = new TaskDefinition{Every = TimeSpan.FromSeconds(5)};
            var taskId = task.Id;
            scheduler.Schedule(task);

            Assert.IsTrue(scheduler.scheduledTasks.ContainsKey(taskId));
        }

        [Test]
        public void When_starting_a_task_defer_should_be_called()
        {
            var task = new TaskDefinition
            {Every = TimeSpan.FromSeconds(5),
                Task = () => { }
            };
            var taskId = task.Id;

            scheduler.Schedule(task);

            var deferCount = handlingContext.DeferWasCalled;
            scheduler.Start(taskId, handlingContext);
            
            Assert.That(handlingContext.DeferWasCalled > deferCount);
        }

        [Test]
        public void When_starting_a_task_the_lambda_should_be_executed()
        {
            var i = 1;

            var task = new TaskDefinition
            {
                Every = TimeSpan.FromSeconds(5),
                Task = () => { i++; }
            };
            var taskId = task.Id;

            scheduler.Schedule(task);            
            scheduler.Start(taskId, handlingContext);

            Thread.Sleep(100); // Wait for the task...

            Assert.That(i == 2);
        }
    }
}
