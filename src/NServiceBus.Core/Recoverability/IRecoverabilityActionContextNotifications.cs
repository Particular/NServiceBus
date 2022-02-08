namespace NServiceBus
{
    using System.Collections.Generic;

    // This is deliberately internal and sneaky. We have a hunch with the introduction of the recoverability pipeline
    // many of the cases that today require notifications can be obsoleted over time.
    interface IRecoverabilityActionContextNotifications : IEnumerable<object>
    {
        void Add(object notification);
    }
}