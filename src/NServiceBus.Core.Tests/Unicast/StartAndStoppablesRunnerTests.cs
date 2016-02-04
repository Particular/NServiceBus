﻿namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class StartAndStoppablesRunnerTests
    {
        [Test]
        public async Task Should_start_all_startables()
        {
            var startable1 = new Startable1();
            var startable2 = new Startable2();
            var thingsToBeStarted = new IWantToRunWhenBusStartsAndStops[] { startable1, startable2 };

            var runner = new StartAndStoppablesRunner(thingsToBeStarted);

            await runner.Start(null);

            Assert.True(startable1.Started);
            Assert.True(startable2.Started);
        }

        [Test]
        public void Should_throw_if_startable_fails_synchronously()
        {
            var startable1 = new Startable1();
            var syncThrowable = new SyncThrowingStart();
            var startable2 = new Startable2();
            var thingsToBeStarted = new IWantToRunWhenBusStartsAndStops[] { startable1, syncThrowable, startable2 };

            var runner = new StartAndStoppablesRunner(thingsToBeStarted);

            Assert.That(async () => await runner.Start(null), Throws.InvalidOperationException);

            Assert.True(startable1.Started);
            Assert.False(startable2.Started);
        }

        [Test]
        public void Should_throw_if_startable_fails_asynchronously()
        {
            var startable1 = new Startable1();
            var asyncThrowable = new AsyncThrowingStart();
            var thingsToBeStarted = new IWantToRunWhenBusStartsAndStops[] { startable1, asyncThrowable };

            var runner = new StartAndStoppablesRunner(thingsToBeStarted);

            Assert.That(async () => await runner.Start(null), Throws.InvalidOperationException);

            Assert.True(startable1.Started);
        }

        [Test]
        public async Task Should_stop_all_stoppables()
        {
            var startable1 = new Startable1();
            var startable2 = new Startable2();
            var thingsToBeStarted = new IWantToRunWhenBusStartsAndStops[] { startable1, startable2 };

            var runner = new StartAndStoppablesRunner(thingsToBeStarted);
            await runner.Start(null);

            await runner.Stop(null);

            Assert.True(startable1.Stopped);
            Assert.True(startable2.Stopped);
        }

        [Test]
        public async Task Should_stop_only_successfully_started()
        {
            var startable1 = new Startable1();
            var startable2 = new Startable2();
            var syncThrows = new SyncThrowingStart();
            var thingsToBeStarted = new IWantToRunWhenBusStartsAndStops[] { startable1, startable2, syncThrows };

            var runner = new StartAndStoppablesRunner(thingsToBeStarted);
            try
            {
                await runner.Start(null);
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // ignored
            }

            await runner.Stop(null);

            Assert.True(startable1.Stopped);
            Assert.True(startable2.Stopped);
            Assert.False(syncThrows.Stopped);
        }

        [Test]
        public async Task Should_not_rethrow_sync_exceptions_when_stopped()
        {
            var startable1 = new Startable1();
            var startable2 = new Startable2();
            var syncThrows = new SyncThrowingStop();
            var thingsToBeStarted = new IWantToRunWhenBusStartsAndStops[] { startable1, syncThrows, startable2 };

            var runner = new StartAndStoppablesRunner(thingsToBeStarted);
            try
            {
                await runner.Start(null);
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // ignored
            }

            Assert.That(async () => await runner.Stop(null), Throws.Nothing);
            Assert.True(startable1.Stopped);
            Assert.True(startable2.Stopped);
        }

        [Test]
        public async Task Should_not_rethrow_async_exceptions_when_stopped()
        {
            var startable1 = new Startable1();
            var startable2 = new Startable2();
            var asyncThrows = new AsyncThrowingStop();
            var thingsToBeStarted = new IWantToRunWhenBusStartsAndStops[] { startable1, asyncThrows, startable2 };

            var runner = new StartAndStoppablesRunner(thingsToBeStarted);
            try
            {
                await runner.Start(null);
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
                // ignored
            }

            Assert.That(async () => await runner.Stop(null), Throws.Nothing);
            Assert.True(startable1.Stopped);
            Assert.True(startable2.Stopped);
        }

        class Startable1 : IWantToRunWhenBusStartsAndStops
        {
            public bool Started { get; set; }
            public bool Stopped { get; set; }

            public Task Start(IBusSession session)
            {
                Started = true;
                return TaskEx.CompletedTask;
            }

            public Task Stop(IBusSession session)
            {
                Stopped = true;
                return TaskEx.CompletedTask;
            }
        }

        class Startable2 : IWantToRunWhenBusStartsAndStops
        {
            public bool Started { get; set; }
            public bool Stopped { get; set; }

            public Task Start(IBusSession session)
            {
                Started = true;
                return TaskEx.CompletedTask;
            }

            public Task Stop(IBusSession session)
            {
                Stopped = true;
                return TaskEx.CompletedTask;
            }
        }

        class SyncThrowingStart : IWantToRunWhenBusStartsAndStops
        {
            public bool Stopped { get; set; }

            public Task Start(IBusSession session)
            {
                throw new InvalidOperationException("SyncThrowingStart");
            }

            public Task Stop(IBusSession session)
            {
                Stopped = true;
                return TaskEx.CompletedTask;
            }
        }

        class AsyncThrowingStart : IWantToRunWhenBusStartsAndStops
        {
            public bool Stopped { get; set; }

            public async Task Start(IBusSession session)
            {
                await Task.Yield();
                throw new InvalidOperationException("AsyncThrowingStart");
            }

            public Task Stop(IBusSession session)
            {
                Stopped = true;
                return TaskEx.CompletedTask;
            }
        }

        class SyncThrowingStop : IWantToRunWhenBusStartsAndStops
        {
            public bool Started { get; set; }

            public Task Start(IBusSession session)
            {
                Started = true;
                return TaskEx.CompletedTask;
            }

            public Task Stop(IBusSession session)
            {
                throw new InvalidOperationException("SyncThrowingStop");
            }
        }

        class AsyncThrowingStop : IWantToRunWhenBusStartsAndStops
        {
            public bool Started { get; set; }

            public Task Start(IBusSession session)
            {
                Started = true;
                return TaskEx.CompletedTask;
            }

            public async Task Stop(IBusSession session)
            {
                await Task.Yield();
                throw new InvalidOperationException("AsyncThrowingStop");
            }
        }
    }
}