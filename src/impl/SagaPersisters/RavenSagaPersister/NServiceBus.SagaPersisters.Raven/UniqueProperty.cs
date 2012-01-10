using System;
using System.Collections.Generic;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.Raven
{
    public class UniqueProperty
    {
        public UniqueProperty() { }

        public UniqueProperty(ISagaEntity saga, KeyValuePair<string, object> property)
        {
            Id = FormatId(saga, property);
            Name = property.Key;
            Value = property.Value;
            SagaId = saga.Id;
        }

        public string Id { get; set; }
        public Guid SagaId { get; private set; }        
        protected string Name { get; set; }
        protected object Value { get; set; }

        static string FormatId(ISagaEntity saga, KeyValuePair<string, object> property)
        {
            return string.Format(string.Format("UniqueProperties/{0}/{1}/{2}", saga.GetType().Name, property.Key, property.Value.GetHashCode()));
        }
    }
}