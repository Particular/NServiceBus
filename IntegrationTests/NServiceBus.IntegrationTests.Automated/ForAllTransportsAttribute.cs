namespace NServiceBus.IntegrationTests.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Transport;

    public class ForAllTransportsAttribute : Attribute
    {
        public ForAllTransportsAttribute()
        {
            Except = "";
        }

        public string Except { get; set; }

        public IEnumerable<ITransportDefinition> Transports
        {
            get
            {
                {
                    var excludes = Except.ToLower().Split(';');

                    return transports.Where(t => !excludes.Contains(t.GetType().Name.ToLower()));
                }
            }
        }

        static readonly IList<ITransportDefinition> transports = new List<ITransportDefinition> {new Msmq(),new RabbitMQ(),new SqlServer(),new ActiveMQ()};
    }
}