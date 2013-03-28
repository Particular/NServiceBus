namespace NServiceBus
{
    public class AzureAddress: Address, AddressParser
    {
        readonly string queueAddress;
        readonly string connectionString;
        static string defaultConnectionString;

        public AzureAddress()
        {
            
        }

        public static void SetDefaultConnectionString(string connectionString)
        {
            defaultConnectionString = connectionString;
        }

        public AzureAddress(string queueAddress, string connectionString)
        {
            this.queueAddress = queueAddress;
            this.connectionString = connectionString;
        }

        public override Address SubScope(string qualifier)
        {
            throw new System.NotImplementedException();
        }

        public override string Name
        {
            get { return queueAddress; }
        }

        public override string FullName
        {
            get { return queueAddress; }
        }

        public string ConnectionString
        {
            get { return connectionString; }
        }

        public string QueueAddress
        {
            get { return queueAddress; }
        }

        Address AddressParser.Parse(string destination)
        {
            return new AzureAddress(destination, defaultConnectionString);
        }
    }
}