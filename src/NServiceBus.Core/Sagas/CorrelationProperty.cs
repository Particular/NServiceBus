namespace NServiceBus.Sagas
{
    using System;

    /// <summary>
    /// Details about a saga data property used to correlate messages hitting the saga.
    /// </summary>
    public class CorrelationProperty
    {
        /// <summary>
        /// Creates a new instance of <see cref="CorrelationProperty" />.
        /// </summary>
        /// <param name="name">The name of the correlation property.</param>
        /// <param name="type">The type of the correlation property.</param>
        public CorrelationProperty(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// The name of the correlation property.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of the correlation property.
        /// </summary>
        public Type Type { get; private set; }
    }
}