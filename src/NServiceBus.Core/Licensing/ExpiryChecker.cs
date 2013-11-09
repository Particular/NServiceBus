namespace NServiceBus.Licensing
{
    using System;

    static class ExpiryChecker
    {
        public static bool IsExpired(DateTime expiry)
        {
            var oneDayGrace = expiry.AddDays(1);
            return oneDayGrace < DateTime.UtcNow.Date;
        }
    }
}