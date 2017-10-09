namespace NServiceBus.AcceptanceTests.EndpointTemplates
{
    class DefaultServerWithDiagnostics : DefaultServer
    {
        public DefaultServerWithDiagnostics()
        {
            EnableDiagnostics = true;
        }
    }
}