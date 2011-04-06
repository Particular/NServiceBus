using System;

namespace NServiceBus.Persistence.Raven
{
    public static class RavenPersistenceConstants
    {
        public const string DefaultDataDirectory = @".\NServiceBusData";
        public const string DefaultUrl = "http://localhost:8080";
        public static readonly Guid DefaultResourceManagerId = new Guid("2806A786-CAC5-404B-B6FA-B3780B4DDCBE");
    }
}