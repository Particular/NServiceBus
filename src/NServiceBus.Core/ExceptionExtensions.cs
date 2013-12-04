namespace NServiceBus
{
    using System;

    static class ExceptionExtensions
    {
        public static string GetMessage(this Exception exception)
        {
            try
            {
                return exception.Message;
            }
            catch (Exception)
            {
                return string.Format("Could not read Message from exception type '{0}'.", exception.GetType());
            }
        }
    }
}