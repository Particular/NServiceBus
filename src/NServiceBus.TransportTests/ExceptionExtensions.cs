namespace NServiceBus
{
    using System;
    using System.Threading;

    static class ExceptionExtensions
    {
#pragma warning disable PS0003 // A parameter of type CancellationToken on a non-private delegate or method should be optional
        public static bool IsCausedBy(this Exception ex, CancellationToken cancellationToken) => ex is OperationCanceledException && cancellationToken.IsCancellationRequested;
#pragma warning restore PS0003 // A parameter of type CancellationToken on a non-private delegate or method should be optional
    }
}