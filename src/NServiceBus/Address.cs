namespace NServiceBus
{
    using System;

    ///<summary>
    /// Abstraction for an address on the NServiceBus network.
    ///</summary>
    public abstract class Address : IEquatable<Address>
    {
        static AddressParser parser;
        static Lazy<Address> delayedLocalAddress = new Lazy<Address>(() => null);
        static readonly UndefinedAddress undefinedAddress = new UndefinedAddress();

        /// <summary>
        /// Undefined address.
        /// </summary>
        public static Address Undefined
        {
            get { return undefinedAddress; }
        }

        static bool locked;

        /// <summary>
        /// Get the address of this endpoint.
        /// </summary>
        public static Address Local
        {
            get
            {
                return delayedLocalAddress.Value;
            }
        }

        /// <summary>
        /// Sets the address of this endpoint.
        /// Will throw an exception if overwriting a previous value (but value will still be set).
        /// </summary>
        public static void SetParser<T>()  where T : AddressParser, new()
        {
            parser = new T();
        }

        /// <summary>
        /// Prevents changes to all addresses.
        /// </summary>
        public static void PreventChanges()
        {
            locked = true;
        }

        /// <summary>
        /// Sets the address of this endpoint.
        /// </summary>
        /// <param name="localAddress">The local address.</param>
        public static void InitializeLocalAddress(string localAddress)
        {
            if (locked)
            {
                throw new InvalidOperationException("PreventChanges() has been called, you cannot change the local address anymore.");
            }

            delayedLocalAddress = new Lazy<Address>(() => Parse(localAddress));
        }

        /// <summary>
        /// Parses a string and returns an Address.
        /// </summary>
        /// <param name="destination">The full address to parse.</param>
        /// <returns>A new instance of <see cref="Address"/>.</returns>
        public static Address Parse(string destination)
        {
            return parser.Parse(destination);
        }

        /// <summary>
        /// Creates a new Address whose Queue is derived from the Queue of the existing Address
        /// together with the provided qualifier. For example: queue.qualifier@machine
        /// </summary>
        /// <param name="qualifier"></param>
        /// <returns></returns>
        public abstract Address SubScope(string qualifier);

        /// <summary>
        /// The short form of an <see cref="Address"/>.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The fully qualified form of an <see cref="Address"/>.
        /// </summary>
        public abstract string FullName { get; }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return FullName;
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other"/> parameter; otherwise, false.
        /// </returns>
        /// <param name="other">An object to compare with this object.</param>
        public bool Equals(Address other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Equals(other.FullName, FullName);
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"/> is equal to the current <see cref="T:System.Object"/>; otherwise, false.
        /// </returns>
        /// <param name="obj">The <see cref="T:System.Object"/> to compare with the current <see cref="T:System.Object"/>. </param><filterpriority>2</filterpriority>
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
            var other = obj as Address;
            return other != null && Equals(other);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override int GetHashCode()
        {
            return FullName != null ? FullName.GetHashCode() : 0;
        }

        /// <summary>
        /// Overloading for the == for the class <see cref="Address"/>.
        /// </summary>
        /// <param name="left">Left hand side of == operator.</param>
        /// <param name="right">Right hand side of == operator.</param>
        /// <returns>true if the LHS is equal to RHS.</returns>
        public static bool operator ==(Address left, Address right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Overloading for the != for the class <see cref="Address"/>.
        /// </summary>
        /// <param name="left">Left hand side of != operator.</param>
        /// <param name="right">Right hand side of != operator.</param>
        /// <returns>true if the LHS is not equal to RHS.</returns>
        public static bool operator !=(Address left, Address right)
        {
            return !Equals(left, right);
        }

        class UndefinedAddress : Address
        {
            public override Address SubScope(string qualifier)
            {
                throw new InvalidOperationException();
            }

            public override string Name
            {
                get { return String.Empty; }
            }

            public override string FullName
            {
                get { return String.Empty; }
            }
        }
    }
}
