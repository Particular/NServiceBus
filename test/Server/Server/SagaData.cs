using System;
using NServiceBus.Saga;

namespace Server
{
    [Serializable]
    public class SagaData : ISagaEntity
    {
        private Guid id;
        private string originator;

        public Guid Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Originator
        {
            get { return originator; }
            set { originator = value; }
        }

        public int BestQuote
        {
            get { return bestQuote; }
            set { bestQuote = value; }
        }

        public int BestPartnerNumber
        {
            get { return bestPartnerNumber; }
            set { bestPartnerNumber = value; }
        }

        public int NumberOfPendingResponses
        {
            get { return numberOfPendingResponses; }
            set { numberOfPendingResponses = value; }
        }


        private int bestQuote = int.MaxValue;
        private int bestPartnerNumber;

        private int numberOfPendingResponses;

    }
}
