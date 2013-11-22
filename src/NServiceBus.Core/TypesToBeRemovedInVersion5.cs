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

        public DataBusMessageMutator(IDataBus dataBus, IDataBusSerializer serializer)
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