using System;
using NServiceBus.Saga;

namespace InternalOrderMessages
{
    [Serializable]
    public class AuthorizeOrderResponseMessage : ISagaMessage
    {
        public Guid SagaId
        {
            get { return sagaId; }
            set { sagaId = value; }
        }

        private Guid sagaId;

        public Guid ManagerId;
        public bool Authorized;
    }
}
