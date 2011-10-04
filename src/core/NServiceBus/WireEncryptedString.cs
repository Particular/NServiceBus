﻿using System;
using System.Runtime.Serialization;

namespace NServiceBus
{
    /// <summary>
    /// A string whose value will be encrypted when sent over the wire.
    /// </summary>
    [Serializable]
    public class WireEncryptedString:ISerializable
    {
        /// <summary>
        /// Default contstructor
        /// </summary>
        public WireEncryptedString()
        {}

        /// <summary>
        /// Deseralizing contructor
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public WireEncryptedString(SerializationInfo info, StreamingContext context)
        {
            EncryptedValue = info.GetValue("EncryptedValue", typeof (EncryptedValue)) as EncryptedValue;
        }
        /// <summary>
        /// The unencrypted string.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The encrypted value of this string
        /// </summary>
        public EncryptedValue EncryptedValue { get; set; }

        /// <summary>
        /// Gets the string value from the WireEncryptedString.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static implicit operator string(WireEncryptedString s)
        {
            return s == null ? null : s.Value;
        }

        /// <summary>
        /// Creates a new WireEncryptedString from the given string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static implicit operator WireEncryptedString(string s)
        {
            return new WireEncryptedString { Value = s };
        }

        /// <summary>
        /// Method for making default XML serialization work properly for this type.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("EncryptedValue", EncryptedValue);
        }
        
        /// <summary>
        /// Returns a string representation of the address.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Value;
        }
    }

    /// <summary>
    /// Class used to represent an encrypted value with an initialization vector.
    /// </summary>
    [Serializable]
    public class EncryptedValue
    {
        /// <summary>
        /// The encrypted value represented as a Base64 string.
        /// </summary>
        public string EncryptedBase64Value { get; set; }

        /// <summary>
        /// The initialization vector represented as a Base64 string.
        /// </summary>
        public string Base64Iv { get; set; }
    }
}
