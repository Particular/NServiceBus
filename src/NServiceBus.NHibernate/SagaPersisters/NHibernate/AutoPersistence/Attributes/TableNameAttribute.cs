namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute
    {
        public string TableName { get; private set; }

        public string Schema { get; set; }

        public TableNameAttribute(string tableName)
        {
            TableName = tableName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class RowVersionAttribute : Attribute
    {

    }
}