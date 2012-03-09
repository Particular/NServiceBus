using System;
using System.Linq;
using System.Threading;
using NServiceBus;
using NServiceBus.Saga;
using System.Transactions;
namespace Timeout.MessageHandlers
{
    public class Bootstrapper : IWantToRunAtStartup
    {
        public IManageTimeouts Manager { get; set; }
        public IBus Bus { get; set; }

        private Thread thread;
        private volatile bool StopRequested;

        public void Run()
        {
            Manager.Init(TimeSpan.FromMilliseconds(100));

            Manager.SagaTimedOut +=
                (o, e) => Bus.Send(e.Destination,
                                     new TimeoutMessage {SagaId = e.SagaId, Expires = DateTime.MinValue, State = e.State});
                            
            thread = new Thread(Poll);
            thread.Start();
        }

        private void Poll()
        {
            while(!StopRequested)
                Manager.PopTimeout();
        }

        public void Stop()
        {
            StopRequested = true;
        }
    }
}
