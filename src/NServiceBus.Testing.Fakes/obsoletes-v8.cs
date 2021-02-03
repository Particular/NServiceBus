namespace NServiceBus.Testing
{
    using System;

    public partial class TestableTransportReceiveContext// : TestableBehaviorContext, ITransportReceiveContext
    {
        [Obsolete("The AbortReceiveOperation method is no longer supported. See the upgrade guide for more details. Will be removed in version 9.0.0.", true)]
        public bool ReceiveOperationAborted { get; set; }

        [Obsolete("The AbortReceiveOperation method is no longer supported. See the upgrade guide for more details. Will be removed in version 9.0.0.", true)]
        public virtual void AbortReceiveOperation() => throw new NotImplementedException();
    }
}