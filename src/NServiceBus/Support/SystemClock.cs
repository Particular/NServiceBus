namespace NServiceBus.Support
{
    using System;

    /// <summary>
    /// Abstracts the system clock and differ between time used for technical purposes and business purposes.
    /// During testing it can be useful to simulate time. To make that possible, we distinguish between
    /// <see cref="BusinessTime"/>, which represents the time used in business logic,
    ///  and <see cref="TechnicalTime"/>, which represents the actual time used for technical components.
    /// </summary>
    public static class SystemClock
    {
        /// <summary>
        /// Get the technical representation of a business time. Allows for overrides.
        /// </summary>
        public static Func<DateTime, DateTime> ConvertBusinessTimeToTechnicalTimeAction { get; set; }

        /// <summary>
        /// Get the business representation of a technical time. Allows for overrides.
        /// </summary>
        public static Func<DateTime, DateTime> ConvertTechnicalTimeToBusinessTimeAction { get; set; }

        /// <summary>
        /// Get the current date and time used for technical purposes. Allows for overrides.
        /// </summary>
        public static Func<DateTime> TechnicalTimeAction { get; set; }

        /// <summary>
        /// Get the current date and time used in business code. Allows for overrides.
        /// </summary>
        public static Func<DateTime> BusinessTimeAction { get; set; }


        static SystemClock()
        {
            TechnicalTimeAction = () => DateTime.UtcNow;
            BusinessTimeAction = () => DateTime.UtcNow;
            
            ConvertTechnicalTimeToBusinessTimeAction = technicalTime => technicalTime;
            ConvertBusinessTimeToTechnicalTimeAction = businessTime => businessTime.ToUniversalTime();
        }

        /// <summary>
        /// Returns the current date and time on this computer, expressed as Coordinated Universal Time (UTC).
        /// </summary>
        public static DateTime TechnicalTime
        {
            get { return TechnicalTimeAction(); }
        }

        /// <summary>
        /// Returns the current date and time represented as a business time.
        /// </summary>
        public static DateTime BusinessTime
        {
            get { return BusinessTimeAction(); }
        }

        /// <summary>
        /// Returns the technical representation of a business time.
        /// </summary>
        public static DateTime ConvertBusinessTimeToTechnicalTime(DateTime businessTime)
        {
            return ConvertBusinessTimeToTechnicalTimeAction(businessTime);
        }

        /// <summary>
        /// Returns the business representation of a technical time.
        /// </summary>
        public static DateTime ConvertTechnicalTimeToBusinessTime(DateTime technicalTime)
        {
            return ConvertTechnicalTimeToBusinessTimeAction(technicalTime);
        }
    }
}