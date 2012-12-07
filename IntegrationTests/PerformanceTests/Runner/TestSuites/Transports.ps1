. .\TestSupport.ps1

RunTest -transport "msmq"
RunTest -transport "sql"
RunTest -transport "activemq"
RunTest -transport "azure"
