namespace NServiceBus.Config.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using NUnit.Framework;

    [TestFixture]
    public class When_applying_message_conventions_to_types
    {
        [Test]
        public void Should_cache_the_message_convention()
        {
            var timesCalled = 0;
            ExtensionMethods.IsMessageTypeAction = (t)=>
                                                       {
                                                           timesCalled++;
                                                           return false;
                                                       };

            this.IsMessage();
            Assert.AreEqual(1,timesCalled);

            this.IsMessage();
            Assert.AreEqual(1, timesCalled);
        }
    }

    [TestFixture]
    public class When_applying_message_conventions_to_events
    {
        [Test]
        public void Should_cache_the_message_convention()
        {
            var timesCalled = 0;
            ExtensionMethods.IsEventTypeAction = (t) =>
            {
                timesCalled++;
                return false;
            };

            this.IsEvent();
            Assert.AreEqual(1, timesCalled);

            this.IsEvent(); 
            Assert.AreEqual(1, timesCalled);
        }

        [Test,Explicit("Perfromance test")]
        public void Check_performance()
        {
            var sw = new Stopwatch();
            int numIterations = 1000000;
            sw.Start();
            for (int i = 0; i < numIterations; i++)
            {
                i.IsMessage();
            }
            sw.Stop();

            Console.WriteLine("Not cached: " + sw.ElapsedMilliseconds);
            sw.Reset();
            var hashTable = new Dictionary<Type, bool>();
            sw.Start();
            for (int i = 0; i < numIterations; i++)
            {
                hashTable[i.GetType()] = i.IsMessage();
            }

            sw.Stop();

            Console.WriteLine("Set dictionary: " + sw.ElapsedMilliseconds);
            sw.Reset();
            sw.Start();
            for (int i = 0; i < numIterations; i++)
            {
                var r = hashTable[i.GetType()];
            }

            sw.Stop();

            Console.WriteLine("Get dictionary: " + sw.ElapsedMilliseconds);
        }
    }


    [TestFixture]
    public class When_applying_message_conventions_to_commands
    {
        [Test]
        public void Should_cache_the_message_convention()
        {
            var timesCalled = 0;
            ExtensionMethods.IsCommandTypeAction = (t) =>
            {
                timesCalled++;
                return false;
            };

            this.IsCommand();
            Assert.AreEqual(1, timesCalled);

            this.IsCommand();
            Assert.AreEqual(1, timesCalled);
        }
    }
}