namespace NServiceBus
{
    using System;

    /// <summary>
    /// Message response wrapper used for compatibility purposes with previous versions of the core callback.
    /// </summary>
    public class LegacyEnumResponse<T>
    {
        T status;

        // ReSharper disable once NotAccessedField.Global
        internal string ReturnCode;

        /// <summary>
        /// Creates an instance of <see cref="LegacyEnumResponse{T}"/>.
        /// </summary>
        /// <param name="status">The enum to set.</param>
        public LegacyEnumResponse(T status)
        {
            this.status = status;
            var tType = status.GetType();
            if (!(tType.IsEnum || tType == typeof(Int32) || tType == typeof(Int16) || tType == typeof(Int64)))
            {
                throw new ArgumentException("The status can only be an enum or an integer.", "status");
            }

            ReturnCode = status.ToString();
            if (tType.IsEnum)
            {
                ReturnCode = Enum.Format(tType, status, "D");
            }
        }

        /// <summary>
        /// Contains the status value.
        /// </summary>
        public T Status { get { return status; } }
    }
}