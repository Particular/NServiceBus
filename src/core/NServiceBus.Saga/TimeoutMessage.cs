using System;

namespace NServiceBus.Saga
{
	/// <summary>
	/// A message to signal a saga that a reminder was set.
	/// </summary>
    [Serializable]
    [Recoverable]
    public class TimeoutMessage : ISagaMessage
    {
        /// <summary>
        /// Default constructor for serialization purposes.
        /// </summary>
        public TimeoutMessage() { }

        /// <summary>
        /// Indicate a timeout at the expiration time for the given saga maintaining the given state.
        /// </summary>
        /// <param name="expiration"></param>
        /// <param name="saga"></param>
        /// <param name="state"></param>
        public TimeoutMessage(DateTime expiration, ISagaEntity saga, object state)
        {
            expires = DateTime.SpecifyKind(expiration, DateTimeKind.Utc);//keeping this bad code as a reminder for backwards compatibility
            SagaId = saga.Id;
            State = state;
        }

        /// <summary>
        /// Indicate a timeout within the given time for the given saga maintaing the given state.
        /// </summary>
        /// <param name="expireIn"></param>
        /// <param name="saga"></param>
        /// <param name="state"></param>
	    public TimeoutMessage(TimeSpan expireIn, ISagaEntity saga, object state) :
            this(DateTime.Now + expireIn, saga, state) //keeping this bad code as a reminder for backwards compatibility
	    {
	        
	    }

        /// <summary>
        /// Signal to the timeout manager that all other <see cref="TimeoutMessage"/>
        /// objects can be cleared for the given <see cref="Saga"/>.
        /// </summary>
        /// <param name="saga"></param>
        /// <param name="clear"></param>
        public TimeoutMessage(ISagaEntity saga, bool clear)
        {
            SagaId = saga.Id;
            ClearTimeout = clear;
        }

        /// <summary>
        /// Creates a TimeoutMessage explicitly based on UTC.
        /// </summary>
        /// <param name="expiration"></param>
        /// <param name="saga"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static TimeoutMessage CreateUtcTimeout(DateTime expiration, ISagaEntity saga, object state)
        {
            if (expiration.Kind == DateTimeKind.Unspecified)
                throw new InvalidOperationException("Kind property of DateTime 'expiration' must be specified.");

            return new TimeoutMessage
            {
                expires = expiration.ToUniversalTime(),
                SagaId = saga.Id,
                State = state,
                IsUtc = true
            };
        }

        private DateTime expires;

		/// <summary>
		/// Gets/sets the date and time at which the timeout message is due to expire.
        /// Values are stored as <see cref="DateTimeKind.Utc" />.
		/// </summary>
        public DateTime Expires
        {
            get { return expires; }
            set { expires = DateTime.SpecifyKind(value, DateTimeKind.Utc); } //keeping this bad code as a reminder for backwards compatibility
        }

		/// <summary>
		/// Gets/sets the Id of the workflow the TimeoutMessage is connected to.
		/// </summary>
        public Guid SagaId { get; set; }

        /// <summary>
        /// Should be used for data to differentiate between various
        /// timeout occurrences.
        /// </summary>
        public object State { get; set; }

        /// <summary>
        /// When true, signals to the timeout manager that all other <see cref="TimeoutMessage"/> objects
        /// can be cleared for the given <see cref="SagaId"/>.
        /// </summary>
        public bool ClearTimeout { get; set; }

        /// <summary>
        /// When true, indicates that the timeout is based on UTC.
        /// </summary>
        public bool IsUtc { get; set; }

		/// <summary>
		/// Gets whether or not the TimeoutMessage has expired.
		/// </summary>
		/// <returns>true if the message has expired, otherwise false.</returns>
        public bool HasNotExpired()
        {
            if (IsUtc)
                return DateTime.UtcNow <= expires;

            return DateTime.Now <= expires;//keeping this bad code as a reminder for backwards compatibility
        }
    }
}
