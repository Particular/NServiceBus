namespace NServiceBus.Testing
{
    using System;
    using System.Threading.Tasks;
    
    public partial class TestableInvokeHandlerContext
    {
        /// <summary>
        /// Moves the message being handled to the back of the list of available
        /// messages so it can be handled later.
        /// </summary>
        [ObsoleteEx(
            Message = "HandleCurrentMessageLater has been deprecated.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public Task HandleCurrentMessageLater()
        {
            throw new NotSupportedException("HandleCurrentMessageLater has been deprecated and will be removed in NServiceBus.Core Version 8.");
        }

        /// <summary>
        /// Indicates if <see cref="IMessageHandlerContext.HandleCurrentMessageLater" /> has been called.
        /// </summary>
        [ObsoleteEx(
            Message = "HandleCurrentMessageLaterWasCalled has been deprecated.",
            RemoveInVersion = "8",
            TreatAsErrorFromVersion = "7")]
        public bool HandleCurrentMessageLaterWasCalled => throw new NotSupportedException("HandleCurrentMessageLater has been deprecated and will be removed in NServiceBus.Core Version 8.");
    }
}