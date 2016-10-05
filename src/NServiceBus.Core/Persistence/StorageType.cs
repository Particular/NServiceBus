namespace NServiceBus.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The storage types used for NServiceBus needs.
    /// </summary>
    public abstract class StorageType
    {
        StorageType(Storage storage)
        {
            this.storage = storage;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return storage.ToString();
        }

        internal static Type FromEnum(Storage storageEnum)
        {
            switch (storageEnum)
            {
                case Storage.Timeouts:
                    return typeof(Timeouts);
                case Storage.Subscriptions:
                    return typeof(Subscriptions);
                case Storage.Sagas:
                    return typeof(Sagas);
                case Storage.GatewayDeduplication:
                    return typeof(GatewayDeduplication);
                case Storage.Outbox:
                    return typeof(Outbox);
                default:
                    throw new ArgumentOutOfRangeException(nameof(storageEnum), "Unknown storage that has no equivalent StorageType");
            }
        }

        internal static List<Type> GetAvailableStorageTypes()
        {
            return typeof(StorageType).GetNestedTypes().ToList();
        }

        Storage storage;

        /// <summary>
        /// Storage for timeouts.
        /// </summary>
        public sealed class Timeouts : StorageType
        {
            internal Timeouts() : base(Storage.Timeouts)
            {
            }
        }

        /// <summary>
        /// Storage for subscriptions.
        /// </summary>
        public sealed class Subscriptions : StorageType
        {
            internal Subscriptions() : base(Storage.Subscriptions)
            {
            }
        }

        /// <summary>
        /// Storage for sagas.
        /// </summary>
        public sealed class Sagas : StorageType
        {
            internal Sagas() : base(Storage.Sagas)
            {
            }
        }

        /// <summary>
        /// Storage for gateway de-duplication.
        /// </summary>
        public sealed class GatewayDeduplication : StorageType
        {
            internal GatewayDeduplication() : base(Storage.GatewayDeduplication)
            {
            }
        }

        /// <summary>
        /// Storage for outbox.
        /// </summary>
        public sealed class Outbox : StorageType
        {
            internal Outbox() : base(Storage.Outbox)
            {
            }
        }
    }
}