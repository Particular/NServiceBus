namespace NServiceBus.Transports.ActiveMQ
{
    using System;

    public class ActiveMQAddress : Address, AddressParser
    {
        readonly string queue;

        public ActiveMQAddress()
        {
        }

        public ActiveMQAddress(string queue)
        {
            this.queue = queue;
        }

        public override Address SubScope(string qualifier)
        {
            return new ActiveMQAddress(String.Format("{0}.{1}", queue, qualifier));
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
            return new ActiveMQAddress(destination);
        }
    }
}
