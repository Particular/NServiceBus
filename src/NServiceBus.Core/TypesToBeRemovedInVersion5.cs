namespace NServiceBus.Sagas
{
    using System;
    using MessageMutator;
    using Saga;

    [ObsoleteEx(TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class AutoCorrelateSagaOnReplyMutator : IMutateTransportMessages, INeedInitialization
    {
        public void MutateIncoming(TransportMessage transportMessage)
        {
        }

        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
        }

        public void Init()
        {
        }
    }

    [ObsoleteEx(TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class ReplyingToNullOriginatorDispatcher : IHandleReplyingToNullOriginator
    {
        public void TriedToReplyToNullOriginator()
        {

        }
    }

    [ObsoleteEx(TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class SagaContext
    {
        [ThreadStatic]
        public static ISaga Current;
    }

    [ObsoleteEx(TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class OriginatingSagaHeaderMutator : IMutateOutgoingTransportMessages, INeedInitialization
    {
        /// <summary>
        /// Set the header if we run in the context of a saga
        /// </summary>
        public void MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
        }

        public void Init()
        {
        }
    }
}


namespace NServiceBus.Sagas.Finders
{
    using Saga;

    [ObsoleteEx(Message = "Not used since 4.2", TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class NullSagaFinder<T> : IFindSagas<T>.Using<object> where T : IContainSagaData
    {
        public T FindBy(object message)
        {
            return default(T);
        }
    }
}


namespace NServiceBus.DataBus
{
    using MessageMutator;

    [ObsoleteEx(TreatAsErrorFromVersion = "4.3", RemoveInVersion = "5.0")]
    public class DataBusMessageMutator : IMessageMutator
    {

// ReSharper disable UnusedParameter.Local
        public DataBusMessageMutator(IDataBus dataBus, IDataBusSerializer serializer)
// ReSharper restore UnusedParameter.Local
        {
        }

        object IMutateOutgoingMessages.MutateOutgoing(object message)
        {

            return message;
        }

        object IMutateIncomingMessages.MutateIncoming(object message)
        {

            return message;
        }


    }
}

namespace NServiceBus.MessageHeaders
{
    using System;
    using System.Collections.Generic;
    using MessageMutator;
    using Unicast;

    /// <summary>
    /// Message Header Manager
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.3")]
    public class MessageHeaderManager : IMutateOutgoingTransportMessages
    {
        void IMutateOutgoingTransportMessages.MutateOutgoing(object[] messages, TransportMessage transportMessage)
        {
            throw new NotSupportedException("This class has been obsoleted and should not be used");
        }

        /// <summary>
        /// Gets the Header for the Message
        /// </summary>
        /// <param name="message">message for which Headers to be find</param>
        /// <param name="key">Key</param>
        public string GetHeader(object message, string key)
        {
            throw new NotSupportedException("This class has been obsoleted and should not be used");
        }

        /// <summary>
        /// Sets the Header for the Message
        /// </summary>
        public void SetHeader(object message, string key, string value)
        {
            throw new NotSupportedException("This class has been obsoleted and should not be used");
        }

        /// <summary>
        /// Gets Static Outgoing Headers
        /// </summary>
        public IDictionary<string, string> GetStaticOutgoingHeaders()
        {
            throw new NotSupportedException("This class has been obsoleted and should not be used");
        }

        /// <summary>
        /// Bus
        /// </summary>
        public IUnicastBus Bus
        {
            get
            {
                throw new NotSupportedException("This class has been obsoleted and should not be used");
            }
            set
            {
                throw new NotSupportedException("This class has been obsoleted and should not be used");
            }
        }
    }
}
