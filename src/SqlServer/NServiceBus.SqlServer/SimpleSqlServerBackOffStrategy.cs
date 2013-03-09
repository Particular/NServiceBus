namespace NServiceBus.Transports.SQLServer
{
    using System.Threading;
    using Utils;

    public class SimpleSqlServerBackOffToken : SqlServerBackOffToken
    {
        readonly int maxBackOff;
        BackOff backOff;

        public SimpleSqlServerBackOffToken(int maxBackOff)
        {
            this.maxBackOff = maxBackOff;
            backOff = new BackOff(maxBackOff);
        }

        public override void ReceivedMessage()
        {
            backOff = new BackOff(maxBackOff);
        }

        public override void BackOff(CancellationToken cancellationToken)
        {
            // TODO: Enhance BackOff to take a cancellationToken too
            backOff.Wait(() => true);
        }
    }

    public class SimpleSqlServerServerBackOffStrategy : SqlServerServerBackOffStrategy
    {
        public SimpleSqlServerServerBackOffStrategy()
        {
            MaxBackOff = 1000;
        }

        public override SqlServerBackOffToken RegisterWorker()
        {
            return new SimpleSqlServerBackOffToken(MaxBackOff);
        }

        public int MaxBackOff { get; set; }
    }
}