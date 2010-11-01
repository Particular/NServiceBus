using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using NServiceBus;
using NServiceBus.Saga;
using System.Collections.Generic;
using System.Threading;

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
            Thread.Sleep(MillisToSleepBetweenMessages);

            if (message.ClearTimeout)
            {
                SagaIdsToClear.Add(message.SagaId);

                if (SagaIdsToClear.Count > MaxSagaIdsToStore)
                    SagaIdsToClear.RemoveAt(0);

                return;
            }

            if (SagaIdsToClear.Contains(message.SagaId))
            {
                SagaIdsToClear.Remove(message.SagaId);
                return;
            }

            if (message.HasNotExpired())
                Bus.HandleCurrentMessageLater();
            else
                Bus.Send(Bus.CurrentMessageContext.ReturnAddress, CloneMessage(message));
        }

        private IMessage CloneMessage(IMessage source)
        {
            var clone = (IMessage)FullClone(source);
            this.AppendCurrentHeadersToClone(clone);
            return clone;
        }

        private static object FullClone(object value)
        {
            using (var stream = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, value);
                stream.Position = 0;
                return formatter.Deserialize(stream);
            }
        }

        private void AppendCurrentHeadersToClone(IMessage target)
        {
            foreach (var sourceKey in this.Bus.CurrentMessageContext.Headers.Keys)
                target.CopyHeaderFromRequest(sourceKey);
        }



        /// <summary>
        /// There are cases when the notification about clearing sagas
        /// arrives after the timeout has already occurred. Since we
        /// can't know that we won't get a timeout message with a given id,
        /// this caps the number that are stored so that memory doesn't leak.
        /// For simplicity, we don't try to decrease the list in the background.
        /// </summary>
        public int MaxSagaIdsToStore { get; set; }

        /// <summary>
        /// The time in milliseconds that the handler sleeps before processing a message.
        /// </summary>
        public int MillisToSleepBetweenMessages { get; set; }

        private static readonly List<Guid> SagaIdsToClear = new List<Guid>();
    }
}
