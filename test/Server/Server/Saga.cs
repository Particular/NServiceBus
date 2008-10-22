using System;
using Messages;
using NServiceBus;
using Common.Logging;
using NServiceBus.Saga;

namespace Server
{
    [Serializable]
    public class Saga : Saga<SagaData>,
        ISagaStartedBy<PriceQuoteRequest>,
        IMessageHandler<PartnerQuoteMessage>
    {
        public override void Timeout(object state)
        {
            if (this.Data.BestPartnerNumber == 0)
            {
                this.timeoutInSeconds /= 2;
                if (this.timeoutInSeconds != 0)
                {
                    this.RequestTimeout(TimeSpan.FromSeconds(this.timeoutInSeconds), null);
                    return;
                }
            }
            
            logger.Debug("Timed out.");

            this.AllDone();
        }

        public void Handle(PriceQuoteRequest message)
        {
            logger.Debug("Started.");

            for (int i = 1; i <= this.numberOfPartners; i++)
            {
                PartnerQuoteMessage msg = new PartnerQuoteMessage();
                msg.SagaId = this.Data.Id;
                msg.PartnerNumber = i;

                this.Bus.Send(msg);
            }

            this.Data.NumberOfPendingResponses = this.numberOfPartners;

            this.RequestTimeout(TimeSpan.FromSeconds(this.timeoutInSeconds), null);
        }

        public void Handle(PartnerQuoteMessage message)
        {
            logger.Debug(string.Format("Got response {0} from partner {1}.", message.Quote, message.PartnerNumber));

            if (message.Quote < this.Data.BestQuote)
            {
                this.Data.BestQuote = message.Quote;
                this.Data.BestPartnerNumber = message.PartnerNumber;
            }

            this.Data.NumberOfPendingResponses--;

            if (this.Data.NumberOfPendingResponses == 0)
                this.AllDone();
        }

        private void AllDone()
        {
            this.MarkAsComplete();

            this.SendResult();
        }

        private void SendResult()
        {
            logger.Debug(string.Format("Partner {0} gave best quote of {1}.", this.Data.BestPartnerNumber, this.Data.BestQuote));

            PriceQuoteResponse msg = new PriceQuoteResponse();
            msg.Quote = this.Data.BestQuote;

            this.ReplyToOriginator(msg);
        }

        #region config info

        private int numberOfPartners = 10;
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

        private static ILog logger = LogManager.GetLogger(typeof(Saga));
    }
}
