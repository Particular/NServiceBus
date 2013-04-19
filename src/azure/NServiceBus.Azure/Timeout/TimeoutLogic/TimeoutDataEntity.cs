using System;

namespace NServiceBus.Azure
{
    using Microsoft.WindowsAzure.Storage.Table.DataServices;

    public class TimeoutDataEntity : TableServiceEntity
    {
        public TimeoutDataEntity(){}

        public TimeoutDataEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }

        /// <summary>
        /// The address of the client who requested the timeout.
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// The saga ID.
        /// </summary>
        public Guid SagaId { get; set; }

        /// <summary>
        /// Additional state.
        /// </summary>
        public string StateAddress { get; set; }

        /// <summary>
        /// The time at which the saga ID expired.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The correlation id
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// The timeout manager that owns this particular timeout
        /// </summary>
        public string OwningTimeoutManager { get; set; }

        /// <summary>
        /// The serialized headers
        /// </summary>
        public string Headers { get; set; }
    }

    public class TimeoutManagerDataEntity : TableServiceEntity
    {
        public TimeoutManagerDataEntity() { }

        public TimeoutManagerDataEntity(string partitionKey, string rowKey)
            : base(partitionKey, rowKey)
        {
        }
        
        /// <summary>
        /// The last successfull chunk read.
        /// </summary>
        public DateTime LastSuccessfullRead { get; set; }
        
    }
}