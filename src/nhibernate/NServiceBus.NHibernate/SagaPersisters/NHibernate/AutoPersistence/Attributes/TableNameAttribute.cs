namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Attributes
{
    using System;

    public class  TableNameAttribute : Attribute
    {
        public string TableName { get; private set; }

        public string Schema { get; set; }

        public TableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }
}