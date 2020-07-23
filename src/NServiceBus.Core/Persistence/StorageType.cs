namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The storage types used for NServiceBus needs.
    /// </summary>
    public abstract class StorageType
    {
        StorageType(string storage)
        {
            this.storage = storage;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return storage;
        }

        internal static List<Type> GetAvailableStorageTypes()
        {
            return typeof(StorageType).GetNestedTypes().Where(t => t != typeof(GatewayDeduplication)).ToList();
        }

        string storage;

        /// <summary>
        /// Storage for timeouts.
        /// </summary>
        public sealed class Timeouts : StorageType
        {
            internal Timeouts() : base("Timeouts")
            {
            }
        }

        /// <summary>
        /// Storage for subscriptions.
        /// </summary>
        public sealed class Subscriptions : StorageType
        {
            internal Subscriptions() : base("Subscriptions")
            {
            }
        }

        /// <summary>
        /// Storage for sagas.
        /// </summary>
        public sealed class Sagas : StorageType
        {
            internal Sagas() : base("Sagas")
            {
            }
        }

        /// <summary>
        /// Storage for gateway de-duplication.
        /// </summary>
        [ObsoleteEx(
            Message = "Gateway persistence has been moved to the NServiceBus.Gateway dedicated package.",
            RemoveInVersion = "9.0.0",
            TreatAsErrorFromVersion = "8.0.0")]
        public sealed class GatewayDeduplication : StorageType
        {
            internal GatewayDeduplication() : base("GatewayDeduplication")
            {
            }
        }

        /// <summary>
        /// Storage for outbox.
        /// </summary>
        public sealed class Outbox : StorageType
        {
            internal Outbox() : base("Outbox")
            {
            }
        }
    }
}