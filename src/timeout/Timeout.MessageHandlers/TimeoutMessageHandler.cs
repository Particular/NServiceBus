using NServiceBus;
using NServiceBus.Saga;
using System;

namespace Timeout.MessageHandlers
{
    public class TimeoutMessageHandler : IMessageHandler<TimeoutMessage>
    {
        public IManageTimeouts Manager { get; set; }
        public IBus Bus { get; set; }

        public void Handle(TimeoutMessage message)
        {
            if (message.ClearTimeout)
            {
                Manager.ClearTimeout(message.SagaId);
            }
            else
            {
                var time = message.Expires;
                if (!message.IsUtc)
                    time = DateTime.SpecifyKind(message.Expires, DateTimeKind.Local).ToUniversalTime();

                var data = new TimeoutData
                               {
                                   Destination = Bus.CurrentMessageContext.ReturnAddress,
                                   SagaId = message.SagaId,
                                   State = message.State,
                                   Time = time
                               };

                Manager.PushTimeout(data);
            }

            Bus.DoNotContinueDispatchingCurrentMessageToHandlers();
        }
    }
}
