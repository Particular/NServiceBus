namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    [TestFixture,Explicit("Slow tests")]
    public class ThroughputLimiterTests
    {
        [Test, Timeout(3500)]
        public void Can_StartAndStop_Multiple_Times_Without_Deadlocks()
        {
            var limiter = new ThroughputLimiter();
            var manualResetEventSlim = new ManualResetEventSlim(false);
            Console.Out.WriteLine("Starting");
            
            limiter.Start(5);
            Task.Factory.StartNew(() =>
            {
                while (!manualResetEventSlim.IsSet)
                {
                    limiter.MessageProcessed();
                    Thread.Sleep(10);
                }
                
            }, TaskCreationOptions.LongRunning);

            Thread.Sleep(TimeSpan.FromSeconds(1));
            Console.Out.WriteLine("Stopping");
            limiter.Stop();
            Console.Out.WriteLine("Starting");
            limiter.Start(10);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Console.Out.WriteLine("Stopping");
            limiter.Stop();
            Console.Out.WriteLine("Starting");
            limiter.Start(0);
            Thread.Sleep(TimeSpan.FromSeconds(1));
            Console.Out.WriteLine("Stopping");
            limiter.Stop();

            manualResetEventSlim.Set();
        }

        [Test,Explicit("Not stable")]
        public void One_message_limit_and_two_messages_should_take_more_than_one_second()
        {
            var stopwatch = Stopwatch.StartNew();
            var limiter = new ThroughputLimiter();
            limiter.Start(1);
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine("took {0}ms", elapsedMilliseconds);
            limiter.Stop();
            Assert.IsTrue(elapsedMilliseconds > 1000,string.Format("Expected more than 1000ms but received {0}ms", elapsedMilliseconds));
        }

        [Test]
        public void Two_message_limit_and_nine_messages_should_take_more_than_four_second()
        {
            var stopwatch = Stopwatch.StartNew();
            var limiter = new ThroughputLimiter();
            limiter.Start(2);
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            limiter.MessageProcessed();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine("took {0}ms", elapsedMilliseconds);
            limiter.Stop();
            Assert.IsTrue(elapsedMilliseconds > 4000,string.Format("Expected more than 4000ms but received {0}ms", elapsedMilliseconds));
        }

        [Test]
        public void Two_message_limit_and_one_messages_should_take_less_than_one_second()
        {
            var stopwatch = Stopwatch.StartNew();
            var limiter = new ThroughputLimiter();
            limiter.Start(2);
            limiter.MessageProcessed();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine("took {0}ms", elapsedMilliseconds);
            limiter.Stop();
            Assert.IsTrue(elapsedMilliseconds < 1000,string.Format("Expected less than 1000ms but received {0}ms", elapsedMilliseconds));
        }

        [Test]
        public void One_message_limit_and_one_messages_should_take_less_than_one_second()
        {
            var stopwatch = Stopwatch.StartNew();
            var limiter = new ThroughputLimiter();
            limiter.Start(1);
            limiter.MessageProcessed();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine("took {0}ms", elapsedMilliseconds);
            limiter.Stop();
            Assert.IsTrue(elapsedMilliseconds < 1000,string.Format("Expected less than 1000ms but received {0}ms", elapsedMilliseconds));
        }
    }
}