﻿using System;
using NServiceBus.Unicast.Transport;
using NUnit.Framework;

namespace NServiceBus.Satellites.Tests
{
    public class FakeSatellite : ISatellite
    {
        public bool IsMessageHandled = false;
        public void Handle(TransportMessage message)
        {
            IsMessageHandled = true;
        }

        public Address InputAddress { get; set; }
        public bool Disabled { get; set; }

        public bool IsStarted = false;
        public bool IsStopped = false;

        public virtual void Start()
        {
            IsStarted = true;
        }

        public void Stop()
        {
            IsStopped = true;
        }
    }

    public class SatelliteWithQueue : FakeSatellite
    {
        public SatelliteWithQueue()
        {
            InputAddress = new Address("input","machineName");
        }
    }

    [TestFixture]
    public class TransportEventTests : SatelliteLauncherContext
    {
        readonly SatelliteWithQueue _sat = new SatelliteWithQueue();
        readonly FakeTransport _transport = new FakeTransport();

        public override void BeforeRun()
        {
            TransportBuilder.TransportToReturn = _transport;
        }

        public override void RegisterTypes()
        {
            Builder.Register<ISatellite>(() => _sat);
        }

        [Test]
        public void When_a_message_is_received_the_Handle_method_should_called_on_the_satellite()
        {
            var tm = new TransportMessage();            
            _transport.RaiseEvent(tm);

            Assert.That(_sat.IsMessageHandled, Is.True);
        }
    }

    [TestFixture]
    public class TransportTests : SatelliteLauncherContext
    {
        readonly SatelliteWithQueue _satelliteWithQueue = new SatelliteWithQueue();
        readonly FakeTransport _transport = new FakeTransport();

        public override void BeforeRun()
        {
            TransportBuilder.TransportToReturn = _transport;
        }

        public override void RegisterTypes()
        {
            Builder.Register<ISatellite>(() => _satelliteWithQueue);
        }

        [Test]
        public void The_TransportMessageReceived_event_should_be_assigned()
        {
            Assert.That(_transport.IsEventAssiged, Is.True);
        }

        [Test]
        public void The_transport_should_be_started()
        {
            Assert.That(_transport.IsStarted, Is.True);
        }

        [Test]
        public void The_transport_should_be_started_with_the_satellites_inputQueueAddress()
        {
            Assert.AreEqual(_satelliteWithQueue.InputAddress, _transport.InputAddress);
        }
    }

    [TestFixture]
    public class SatelliteRestartTests : SatelliteLauncherContext
    {
        readonly SatelliteWithQueueThatThrowException _satellite = new SatelliteWithQueueThatThrowException();
        readonly FakeTransport _transport = new FakeTransport();

        public override void BeforeRun()
        {
            TransportBuilder.TransportToReturn = _transport;
        }

        public override void RegisterTypes()
        {
            Builder.Register<ISatellite>(() => _satellite);
        }

        [Test]
        public void Number_of_worker_threads_should_be_set_to_0()
        {
            Assert.That(_transport.NumberOfWorkerThreads, Is.EqualTo(0));
        }
        
        [Test]
        public void The_Transport_should_have_been_restarted()
        {
            Assert.That(_transport.HasChangedNumberOfThreadsNTimes, Is.GreaterThan(0));
        }
    }

    public class SatelliteWithQueueThatThrowException : SatelliteWithQueue
    {
        public override void Start()
        {            
            throw new Exception("This enpoint should not start!");
        }
    }

    [TestFixture]
    public class DefaultsTests : SatelliteLauncherContext
    {
        readonly FakeSatellite _fakeSatellite = new FakeSatellite();

        public override void BeforeRun() { }

        public override void RegisterTypes()
        {
            Builder.Register<ISatellite>(() => _fakeSatellite);
        }

        [Test]
        public void By_default_the_satellite_should_not_be_disabled()
        {
            Assert.That(_fakeSatellite.Disabled, Is.False);
        }

        [Test]
        public void The_satellite_should_be_started()
        {            
            Assert.That(_fakeSatellite.IsStarted, Is.True);
        }
    }
}
