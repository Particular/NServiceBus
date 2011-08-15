using NServiceBus;
using NServiceBus.Saga;

namespace Timeout.MessageHandlers
{
    public class TimeoutMessageHandler : IMessageHandler<TimeoutMessage>
    {
        public IPersistTimeouts Persister { get; set; }
        public IManageTimeouts Manager { get; set; }
        public IBus Bus { get; set; }

        public void Handle(TimeoutMessage message)
        {
            if (message.ClearTimeout)
            {
                Manager.ClearTimeout(message.SagaId);
                Persister.Remove(message.SagaId);
            }
            else
            {
                var data = new TimeoutData
                               {
                                   Destination = Bus.CurrentMessageContext.ReplyToAddress,
                                   SagaId = message.SagaId,
                                   State = message.State,
                                   Time = message.Expires
                               };

                Manager.PushTimeout(data);
                Persister.Add(data);
            }

            Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
        }
    }}
