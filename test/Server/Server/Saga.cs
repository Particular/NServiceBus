using System;
using Messages;
using NServiceBus;
using Common.Logging;
using NServiceBus.Saga;

namespace Server
{
    public class Saga : ISaga<PriceQuoteRequest>
    {
        #region ISaga<PriceQuoteRequest> Members

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
                msg.SagaId = this.id;
                msg.PartnerNumber = i;

                this.bus.Send(msg);
            }

            this.NumberOfPendingResponses = this.numberOfPartners;

            this.reminder.ExpireIn(TimeSpan.FromSeconds(this.timeoutInSeconds), this, null);
        }

        public void Handle(PartnerQuoteMessage message)
        {
            logger.Debug(string.Format("Got response {0} from partner {1}.", message.Quote, message.PartnerNumber));

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
            logger.Debug(string.Format("Partner {0} gave best quote of {1}.", this.BestPartnerNumber, this.BestQuote));

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

        private static ILog logger = LogManager.GetLogger(typeof(Saga));
    }
}
