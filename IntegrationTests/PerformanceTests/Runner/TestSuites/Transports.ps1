. .\TestSupport.ps1

RunTest -transport "msmq"
RunTest -transport "sqlserver"
RunTest -transport "activemq"
RunTest -transport "rabbitmq"
#RunTest -transport "azure"
