. .\TestSupport.ps1

RunTest -transport "msmq"
RunTest -transport "sqlserver"
RunTest -transport "activemq"
#RunTest -transport "azure"
