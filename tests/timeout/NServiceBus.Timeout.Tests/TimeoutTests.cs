namespace NServiceBus.Timeout.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Core;
    using NUnit.Framework;

    [TestFixture]
    public class TimeoutTests
    {
        private List<Guid> sagaIds;
        private DateTime eventTime;
        private IManageTimeouts manager;
        private TimeSpan timeout;
        private TimeSpan interval;

        [SetUp]
        public void Setup()
        {
            timeout = TimeSpan.FromSeconds(1);
            interval = TimeSpan.FromSeconds(2);

            manager = new DefaultTimeoutManager();
            manager.Init(interval);

            manager.SagaTimedOut += (o, e) =>
            {
                sagaIds.Add(e.SagaId);
                eventTime = e.Time;
            };

            sagaIds = new List<Guid>();
            eventTime = DateTime.MinValue;
        }

        [Test]
        public void PopWithoutPush()
        {
            manager.PopTimeout();

            Assert.IsEmpty(sagaIds);
        }

        [Test]
        public void ClearWithoutPush()
        {
            manager.ClearTimeout(Guid.NewGuid());
        }

        [Test]
        public void ClearWithPush()
        {
            var sagaId = Guid.NewGuid();
            var time = DateTime.UtcNow + timeout;

            manager.PushTimeout(new TimeoutData { SagaId = sagaId, Time = time });
            manager.ClearTimeout(sagaId);

            manager.PopTimeout();
            Assert.AreEqual(0, sagaIds.Count);
        }

        [Test]
        public void ClearWithTwoPushWitDifferentTime()
        {
            var sagaId = Guid.NewGuid();
            var time = DateTime.UtcNow + timeout;

            manager.PushTimeout(new TimeoutData { SagaId = sagaId, Time = time });
            manager.PushTimeout(new TimeoutData { SagaId = sagaId, Time = time.AddSeconds(1) });
            manager.ClearTimeout(sagaId);

            manager.PopTimeout();
            Assert.AreEqual(0, sagaIds.Count);
        }


        [Test]
        public void OnePushThenPop()
        {
            var id = Guid.NewGuid();
            var time = DateTime.UtcNow + timeout;

            manager.PushTimeout(new TimeoutData { SagaId = id, Time = time });

            manager.PopTimeout();
            Assert.AreEqual(1, sagaIds.Count);
            Assert.AreEqual(id, sagaIds[0]);
            Assert.AreEqual(time, eventTime);
        }

        [Test]
        public void OnePushThenClearThenPop()
        {
            var id = Guid.NewGuid();
            var time = DateTime.UtcNow + timeout;

            manager.PushTimeout(new TimeoutData { SagaId = id, Time = time });

            manager.ClearTimeout(id);

            manager.PopTimeout();
            Assert.AreEqual(0, sagaIds.Count);
        }

        [Test]
        public void TwoPushesForSameTimeThenPop()
        {
            var time = DateTime.UtcNow + timeout;

            manager.PushTimeout(new TimeoutData { SagaId = Guid.NewGuid(), Time = time });
            manager.PushTimeout(new TimeoutData { SagaId = Guid.NewGuid(), Time = time });

            manager.PopTimeout();

            Assert.AreEqual(2, sagaIds.Count);
            Assert.AreEqual(time, eventTime);
        }

        [Test]
        public void TwoPushesForSameTimeThenClearThenPop()
        {
            var time = DateTime.UtcNow + timeout;
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            manager.PushTimeout(new TimeoutData { SagaId = id1, Time = time });
            manager.PushTimeout(new TimeoutData { SagaId = id2, Time = time });

            manager.ClearTimeout(id1);

            manager.PopTimeout();

            Assert.AreEqual(1, sagaIds.Count);
            Assert.AreEqual(id2, sagaIds[0]);
            Assert.AreEqual(time, eventTime);
        }

        [Test]
        public void TwoPushesForDifferentTimesThenPop()
        {
            var time1 = DateTime.UtcNow + timeout;
            var time2 = DateTime.UtcNow + timeout + timeout;

            manager.PushTimeout(new TimeoutData { SagaId = Guid.NewGuid(), Time = time2 });
            manager.PushTimeout(new TimeoutData { SagaId = Guid.NewGuid(), Time = time1 });

            manager.PopTimeout();

            Assert.AreEqual(1, sagaIds.Count);
            Assert.AreEqual(time1, eventTime);

            sagaIds.Clear();

            manager.PopTimeout();

            Assert.AreEqual(1, sagaIds.Count);
            Assert.AreEqual(time2, eventTime);
        }

        [Test]
        public void PopAfterDefinedIntervalShouldNotRaiseEvent()
        {
            var time = DateTime.UtcNow + timeout + interval;

            manager.PushTimeout(new TimeoutData { SagaId = Guid.NewGuid(), Time = time });

            manager.PopTimeout();

            Assert.AreEqual(0, sagaIds.Count);

            Thread.Sleep(interval);

            manager.PopTimeout();

            Assert.AreEqual(1, sagaIds.Count);
        }
    }
}
