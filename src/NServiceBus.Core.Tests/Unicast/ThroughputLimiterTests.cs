namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
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
    }
}