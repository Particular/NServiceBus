using System;
using Microsoft.WindowsAzure.StorageClient;

namespace Timeout.MessageHandlers
{
    public class TimeoutDataEntity : TableServiceEntity
    {
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
    }
}