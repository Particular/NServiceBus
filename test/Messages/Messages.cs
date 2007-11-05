using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using NServiceBus;
using NServiceBus.Workflow;
using System.Xml.Serialization;

namespace Messages
{
    [Serializable]
    [Recoverable]
    public class Command : IMessage
    {
        public int i;
        public string[] alotofdata;

        [XmlAnyElement]
        public object extra;
    }

    [Serializable]
    [TimeToBeReceived("0:01:00.000")]
    public class Event : IMessage
    {
        public int j;
        public string[] alotofdata;

        [XmlAnyElement]
        public object extra;
    }

    [Serializable]
    [StartsWorkflow]
    public class PriceQuoteRequest : IMessage
    {

        [XmlAnyElement]
        public object extra;
    }

    [Serializable]
    public class PriceQuoteResponse : IMessage
    {
        private int quote;
        public int Quote
        {
            get { return quote; }
            set { quote = value; }
        }

        [XmlAnyElement]
        public object extra;
    }

    [Serializable]
    public class PartnerQuoteMessage : IWorkflowMessage
    {
        #region IWorkflowMessage Members

        private Guid workflowId;
        public Guid WorkflowId
        {
            get { return workflowId; }
            set { workflowId = value; }
        }

        #endregion

        private int quote;
        public int Quote
        {
            get { return quote; }
            set { quote = value; }
        }

        private int partnerNumber;
        public int PartnerNumber
        {
            get { return partnerNumber; }
            set { partnerNumber = value; }
        }

        [XmlAnyElement]
        public object extra;
    }
}
