namespace NServiceBus.UniformSession
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    [SuppressMessage("Code", "PCR0019:A task-returning method should have a CancellationToken parameter or a parameter implementing ICancellableContext", Justification = "<Pending>")]
    public static class IUniformSessionExtensions
    {
        public static Task Send(this IUniformSession session, object message) => throw new NotImplementedException();

        public static Task Send<T>(this IUniformSession session, Action<T> messageConstructor) => throw new NotImplementedException();

        public static Task Send(this IUniformSession session, string destination, object message) => throw new NotImplementedException();

        public static Task Send<T>(this IUniformSession session, string destination, Action<T> messageConstructor) => throw new NotImplementedException();

        public static Task SendLocal(this IUniformSession session, object message) => throw new NotImplementedException();

        public static Task SendLocal<T>(this IUniformSession session, Action<T> messageConstructor) => throw new NotImplementedException();

        public static Task Publish(this IUniformSession session, object message) => throw new NotImplementedException();

        public static Task Publish<T>(this IUniformSession session) => throw new NotImplementedException();

        public static Task Publish<T>(this IUniformSession session, Action<T> messageConstructor) => throw new NotImplementedException();
    }
}
