using System;
using System.Collections.Generic;
using System.Text;
using Messages;
using NServiceBus.Workflow;
using NServiceBus;
using Common.Logging;

namespace Server
{
    public class WF : IWorkflow<PriceQuoteRequest>
    {
        #region IWorkflow<PriceQuoteRequest> Members

        private Guid id = Guid.NewGuid();
        public Guid Id
        {
            get
            {
                return id;
            }
        }

        private bool completed = false;
        public bool Completed
        {
            get { return completed; }
        }

        public void Timeout(object state)
        {
            if (this.BestPartnerNumber == 0)
            {
                this.timeoutInSeconds /= 2;
                if (this.timeoutInSeconds != 0)
                {
                    this.reminder.ExpireIn(TimeSpan.FromSeconds(this.timeoutInSeconds), this, null);
                    return;
                }
            }
            
            logger.Debug("Timed out.");

            this.AllDone();
        }

        public void Handle(PriceQuoteRequest message)
        {
            logger.Debug("Started.");

            this.clientAddress = this.bus.SourceOfMessageBeingHandled;

            for (int i = 1; i <= this.numberOfPartners; i++)
            {
                PartnerQuoteMessage msg = new PartnerQuoteMessage();
                msg.WorkflowId = this.id;
                msg.PartnerNumber = i;

                this.bus.Send(msg);
            }

            this.NumberOfPendingResponses = this.numberOfPartners;

            this.reminder.ExpireIn(TimeSpan.FromSeconds(this.timeoutInSeconds), this, null);
        }

        public void Handle(PartnerQuoteMessage message)
        {
            logger.Debug("Got response " + message.Quote.ToString() + " from partner " + message.PartnerNumber.ToString());

            if (message.Quote < this.BestQuote)
            {
                this.BestQuote = message.Quote;
                this.BestPartnerNumber = message.PartnerNumber;
            }

            this.NumberOfPendingResponses--;

            if (this.NumberOfPendingResponses == 0)
                this.AllDone();
        }

        private void AllDone()
        {
            this.completed = true;

            this.SendResult();
        }

        private void SendResult()
        {
            logger.Debug("Partner " + this.BestPartnerNumber.ToString() + " gave best quote of " + this.BestQuote.ToString());

            PriceQuoteResponse msg = new PriceQuoteResponse();
            msg.Quote = this.BestQuote;

            this.bus.Send(msg, this.clientAddress);
        }
        #endregion

        #region config info

        private Reminder reminder;
        public Reminder Reminder
        {
            set { reminder = value; }
        }

        private IBus bus;
        public IBus Bus
        {
            set { bus = value; }
        }

        private int numberOfPartners;
        public int NumberOfPartners
        {
            set { numberOfPartners = value; }
        }

        private int timeoutInSeconds = 10;
        public int TimeoutInSeconds
        {
            set { timeoutInSeconds = value; }
        }

        #endregion

        #region data

        public string clientAddress;
        public int BestQuote = int.MaxValue;
        public int BestPartnerNumber;

        public int NumberOfPendingResponses;

        #endregion

        private static ILog logger = LogManager.GetLogger(typeof(WF));
    }
}
