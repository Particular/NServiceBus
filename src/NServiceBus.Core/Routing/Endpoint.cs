namespace NServiceBus.Routing
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a name of a logical endpoint endpoint.
    /// </summary>
    public sealed class Endpoint
    {

        /// <summary>
        /// Creates a new initializable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static IInitializableEndpoint Prepare(BusConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var endpoint = configuration.Build();
            return endpoint;
        }

        /// <summary>
        /// Creates a new startable endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static Task<IStartableEndpoint> Create(BusConfiguration configuration)
        {
            Guard.AgainstNull(nameof(configuration), configuration);
            var initializable = Prepare(configuration);
            return initializable.Initialize();
        }

        /// <summary>
        /// Creates and starts a new endpoint based on the provided configuration.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        public static async Task<IEndpointInstance> Start(BusConfiguration configuration)
        {
            var initializable = await Create(configuration).ConfigureAwait(false);
            return await initializable.Start().ConfigureAwait(false);
        }

        readonly string name;

        /// <summary>
        /// Creates a new logical endpoint name.
        /// </summary>
        public Endpoint(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Returns the string representation of the endpoint name.
        /// </summary>
        public override string ToString()
        {
            return name;
        }

        bool Equals(Endpoint other)
        {
            return String.Equals(name, other.name);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj is Endpoint && Equals((Endpoint) obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            return name?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Tests for equality.
        /// </summary>
        public static bool operator ==(Endpoint left, Endpoint right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests for inequality.
        /// </summary>
        public static bool operator !=(Endpoint left, Endpoint right)
        {
            return !Equals(left, right);
        }

    }
}