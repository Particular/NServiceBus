namespace NServiceBus
{
    using System;

    class WrappedException : Exception
    {
        public WrappedException(Exception exceptionThatIsWrapped)
            : base(String.Empty, exceptionThatIsWrapped)
        {
        }
    }
}