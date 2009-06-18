using System;

namespace NServiceBus.Host
{
    ///<summary>
    /// Attribute to be used to override the name of a message endpoint.
    /// If a custom name is not specified using this attribute than the name
    /// of the type implementing IMessageEndpoint will be used in its place.
    ///</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class EndpointNameAttribute : Attribute
    {
        ///<summary>
        /// Specifies the name of the endpoint.
        ///</summary>
        ///<param name="name"></param>
        public EndpointNameAttribute(string name)
        {
            this.Name = name;
        }

        ///<summary>
        /// The endpoint name.
        ///</summary>
        public string Name { get; private set; }
    }
}