using System.Linq;
using System.Threading;
using NServiceBus.Saga;
using Timeout.MessageHandlers;

namespace NServiceBus.Timeout.Hosting.Azure
{
    public class Bootstrapper : IWantToRunAtStartup
    {
        public IPersistTimeouts Persister { get; set; }
        public IDetermineWhoCanSend I { get; set; }
        public IManageTimeouts Manager { get; set; }
        public IBus Bus { get; set; }

        private Thread thread;
        private volatile bool stopRequested;

        public void Run()
        {
            Manager.SagaTimedOut +=
                (o, e) =>
                    {
                        if (!I.CanSend(e)) return;

                        Bus.Send(e.Destination, new TimeoutMessage {SagaId = e.SagaId, Expires = e.Time, State = e.State});
                        Persister.Remove(e.SagaId);
                    };

            Persister.GetAll().ToList().ForEach(td => Manager.PushTimeout(td));

            thread = new Thread(Poll);
            thread.Start();
        }

        private void Poll()
        {
            while(!stopRequested)
                Manager.PopTimeout();
        }

        public void Stop()
        {
            stopRequested = true;
        }
    }
}
