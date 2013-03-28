namespace NServiceBus.Transports.SQLServer
{
    using System;

    public class SqlServerAddress : Address, AddressParser
    {
        readonly string tableName;

        public SqlServerAddress()
        {
        }

        public SqlServerAddress(string tableName)
        {
            this.tableName = tableName;
        }

        public override Address SubScope(string qualifier)
        {
            return new SqlServerAddress(String.Format("{0}.{1}", tableName, qualifier));
        }

        public override string Name
        {
            get { return tableName; }
        }

        public override string FullName
        {
            get { return tableName; }
        }

        Address AddressParser.Parse(string destination)
        {
            return new SqlServerAddress(destination);
        }
    }
}
