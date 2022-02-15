namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Transport;
    using Pipeline;

    class RecoverabilityContext : BehaviorContext, IRecoverabilityContext, IRecoverabilityActionContext, IRecoverabilityActionContextNotifications
    {
        public RecoverabilityContext(
            ErrorContext errorContext,
            RecoverabilityConfig recoverabilityConfig,
            Dictionary<string, string> metadata,
            RecoverabilityAction recoverabilityAction,
            IBehaviorContext parent) : base(parent)
        {
            FailedMessage = errorContext.Message;
            Exception = errorContext.Exception;
            ReceiveAddress = errorContext.ReceiveAddress;
            ImmediateProcessingFailures = errorContext.ImmediateProcessingFailures;

            RecoverabilityConfiguration = recoverabilityConfig;
            Metadata = metadata;
            RecoverabilityAction = recoverabilityAction;

            Extensions.Set(errorContext.TransportTransaction);
        }

        public IncomingMessage FailedMessage { get; }

        public Exception Exception { get; }

        public string ReceiveAddress { get; }

        public int ImmediateProcessingFailures { get; }

        IReadOnlyDictionary<string, string> IRecoverabilityActionContext.Metadata => Metadata;

        public RecoverabilityConfig RecoverabilityConfiguration { get; }

        public Dictionary<string, string> Metadata { get; }

        public RecoverabilityAction RecoverabilityAction
        {
            get => recoverabilityAction;
            set
            {
                if (locked)
                {
                    throw new InvalidOperationException("The RecoverabilityAction has already been executed and can't be changed");
                }
                recoverabilityAction = value;
            }
        }

        public IRecoverabilityActionContext PreventChanges()
        {
            locked = true;
            return this;
        }

        public void Add(object notification)
        {
            notifications ??= new List<object>();
            notifications.Add(notification);
        }

        public IEnumerator<object> GetEnumerator() => notifications?.GetEnumerator() ?? Enumerable.Empty<object>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        RecoverabilityAction recoverabilityAction;
        bool locked;
        List<object> notifications;
    }
}