namespace NServiceBus
{
    using System.Threading;
    using NServiceBus.Extensibility;

    static class ExtendableOptionsExtensions
    {
        public static void RegisterTokenInternal(this ExtendableOptions options, CancellationToken cancellationToken)
        {
            var extensions = options.GetExtensions();
            UpdateRequestResponseCorrelationTableBehavior.RequestResponseParameters data;
            if (extensions.TryGet(out data))
            {
                data.CancellationToken = cancellationToken;
            }
            else
            {
                data = new UpdateRequestResponseCorrelationTableBehavior.RequestResponseParameters { CancellationToken = cancellationToken };
                extensions.Set(data);
            }
        }
    }
}