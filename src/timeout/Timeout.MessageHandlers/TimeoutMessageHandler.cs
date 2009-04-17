using System;
using NServiceBus;
using NServiceBus.Saga;
using System.Collections.Generic;

namespace Timeout.MessageHandlers
{
    /// <summary>
    /// Handles TimeoutMessage.
    /// </summary>
    public class TimeoutMessageHandler : IMessageHandler<TimeoutMessage>
    {
        /// <summary>
        /// Used for sending responses.
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Handles the TimeoutMessage.
        /// </summary>
        /// <param name="message"></param>
        public void Handle(TimeoutMessage message)
        {
            if (message.ClearTimeout)
            {
                sagaIdsToClear.Add(message.SagaId);

                if (sagaIdsToClear.Count > this.maxSagaIdsToStore)
                    sagaIdsToClear.RemoveAt(0);

                return;
            }

            if (sagaIdsToClear.Contains(message.SagaId))
            {
                sagaIdsToClear.Remove(message.SagaId);
                return;
            }

            if (message.HasNotExpired())
                this.Bus.HandleCurrentMessageLater();
            else
                this.Bus.Send(this.Bus.SourceOfMessageBeingHandled, message);
        }

        private int maxSagaIdsToStore = 100;

        /// <summary>
        /// There are cases when the notification about clearing sagas
        /// arrives after the timeout has already occurred. Since we
        /// can't know that we won't get a timeout message with a given id,
        /// this caps the number that are stored so that memory doesn't leak.
        /// For simplicity, we don't try to decrease the list in the background.
        /// The default value is 100.
        /// </summary>
        public int MaxSagaIdsToStore
        {
            set { this.maxSagaIdsToStore = value; }
        }
        
        private static readonly List<Guid> sagaIdsToClear = new List<Guid>();
    }
}
