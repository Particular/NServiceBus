using System;
using NServiceBus.Saga;

namespace NServiceBus.SagaPersisters.Raven
{
    public class RavenSaga
    {
        public string Id { get; set; }

        public ISagaEntity Saga { get; set; }

        public static string FormatId(Guid id, string endpoint)
        {
            return string.Format("NServiceBus/Sagas/{0}/{1}", endpoint, id);
        }
    }
}