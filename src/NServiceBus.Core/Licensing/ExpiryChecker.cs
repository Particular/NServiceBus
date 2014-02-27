namespace NServiceBus.Licensing
{
    using System;

    static class ExpiryChecker
    {
        public static bool IsExpired(DateTime expiry)
        {
            var oneDayGrace = expiry >= DateTime.MaxValue.Date ? expiry : expiry.AddDays(1);
            return oneDayGrace < DateTime.UtcNow.Date;
        }
    }
}