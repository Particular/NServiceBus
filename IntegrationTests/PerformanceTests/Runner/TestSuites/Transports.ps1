. .\TestSupport.ps1

RunTest -transport "msmq"
RunTest -transport "msmq" -mode "volatile"

RunTest -transport "sqlserver"
RunTest -transport "activemq"
RunTest -transport "activemq" -mode "volatile"
RunTest -transport "rabbitmq"
RunTest -transport "rabbitmq" -mode "volatile"
#RunTest -transport "azure"
