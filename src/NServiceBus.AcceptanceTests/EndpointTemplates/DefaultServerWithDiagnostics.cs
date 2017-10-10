namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    class DefaultServerWithDiagnostics : DefaultServer
    {
        public DefaultServerWithDiagnostics()
        {
            DisableDiagnostics = false;
        }
    }
}