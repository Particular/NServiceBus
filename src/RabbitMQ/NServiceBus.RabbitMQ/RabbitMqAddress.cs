namespace NServiceBus.Transports.RabbitMQ
{
    using System;

    public class RabbitMqAddress : Address, AddressParser
    {
        readonly string queue;

        public RabbitMqAddress()
        {
        }

        public RabbitMqAddress(string queue)
        {
            this.queue = queue;
        }

        public override Address SubScope(string qualifier)
        {
            return new RabbitMqAddress(String.Format("{0}.{1}", queue, qualifier));
        }

        public override string Name
        {
            get { return queue; }
        }

        public override string FullName
        {
            get { return queue; }
        }

        Address AddressParser.Parse(string destination)
        {
            return new RabbitMqAddress(destination);
        }
    }
}
